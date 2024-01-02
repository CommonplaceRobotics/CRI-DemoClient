using System;
using System.Diagnostics;
using System.Net.NetworkInformation;    // for Ping
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace CRI_Client
{
    //***********************************************
    // This class connects with a CRI client at the specified IP address, port 3920
    // Then it establishes functionality to act as a CRI interface client for Commonplace Robotics robot controller (TinyCtrl or CPRog)
    // Usage:
    // Including the class, then connecting, then sending commands.
    // HardwareProtocolClient itf = new HardwareProtocolClient();
    // itf.Connect();
    // itf.SendCommand("TestCommand for robot...");
    // The lines with log4net or log.Debug can be removed, they are only used for debugging
    //
    public class HardwareProtocolClient
    {
        // Create a logger for use in this class
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool flagConnected = false;

        // These define what types of messages are not written to the log. Set by the UI.
        public bool flagHideAliveMessages = true;
        public bool flagHideBasicStatusMessages = true;
        public bool flagHidePlatformStatusMessages = true;
        public bool flagHideFurtherStatusMessages = true;
        public bool flagHideUnknownMessages = false;

        /// <summary>
        /// Version of the CRI protocol (TinyCtrl/CPRog/iRC V12/13 -> 16, RobotControl V14 -> 17)
        /// </summary>
        public int CRIVersion { get; private set; } = 17;

        TcpClient clientSocket;
        NetworkStream serverStream;
        string ipAddress;                       // Server address 
        Thread commThread;
        Thread writeThread;
        bool flagStopRequest = false;
        bool flagThreadWriteRunning = false;
        bool flagThreadReadRunning = false;

        //information on the connected robot arm
        public double[] posJointsSetPoint = new double[9];
        public double[] posJointsCurrent = new double[9];
        public double[] posCartesian = new double[6];
        public string errorString = "n/a";
        public int[] errorCodes = new int[9];           // error codes for 6 robot arm joints and 3 gripper joints
        public int supplyVoltage = 0;                   // in mV
        public int[] currentJoints = new int[9];        // in mA
        public int currentAll = 0;                      // in mA
        public ulong digialInputs = 0;                  // bitfield
        public ulong digialOutputs = 0;                 // bitfield
        public double overrideValue = 40.0;             // the robots override
        public int emergencyStopStatus = 0;             // Bit1: MainRelais Bit2: ES-Button Bit3: Periphery
        public int statusCnt = -1;

        //Information on the users commands
        readonly double[] jogValues = new double[9];

        int sendCnt = 1;                                // counts the number of send messages [1 .. 9999]
        int cmdCnt = 20;                                // reference for the transmitted robot program commands


        /// <summary>
        /// Constructor
        /// </summary>
        public HardwareProtocolClient()
        {
            for (int i = 0; i < 9; i++)
                errorCodes[i] = -1;
        }

        /// <summary>
        /// Stops the read and write loops with the CRI server.
        /// Calling this function before closing the application is necessary because the read and write loops run in seperate threads.
        /// </summary>
        public void StopCRIClient()
        {
            log.Info("Stopping CRI communication.");

            flagStopRequest = true;
            while (flagThreadReadRunning || flagThreadWriteRunning)
                System.Threading.Thread.Sleep(50);

            serverStream?.Close();
            clientSocket?.Close();

            log.Info("CRI communication stopped.");
        }

        /// <summary>
        /// Sets the server address
        /// </summary>
        /// <param name="ipa"></param>
        public void SetIPAddress(string ipa)
        {
            ipAddress = ipa;
        }

        /// <summary>
        /// Set the 9 jog values (6 for the robot, 3 for the gripper)
        /// </summary>
        /// <param name="jValues"></param>
        public void SetJogValues(double[] jValues)
        {
            try
            {
                for (int i = 0; i < 9; i++)
                {
                    if (jValues[i] > 100.0) jValues[i] = 100.0;
                    if (jValues[i] < -100.0) jValues[i] = -100.0;
                    this.jogValues[i] = jValues[i];
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Sets the override [0..100]
        /// </summary>
        /// <param name="ovr"></param>
        public void SetOverride(double ovr)
        {
            if (ovr > 100.0) ovr = 100.0;
            if (ovr < 0.0) ovr = 0.0;
            string cmdString = "CMD Override " + ovr.ToString("0.0");
            SendCommand(cmdString);
        }

        /// <summary>
        /// Requests the CRI version
        /// </summary>
        private void RequestCRIVersion()
        {
            SendCommand("CMD GetVersion");
        }

        //************** Thread Write Loop ****************************************
        /// <summary>
        /// Writes the ALIVEJOG message in a 10 Hz loop to the CRI server
        /// </summary>
        static void writeLoop(object Context)
        {
            int sleepTime = 100;
            HardwareProtocolClient itf = (HardwareProtocolClient)Context;
            itf.flagThreadWriteRunning = true;
            try
            {
                // request the CRI version (not necessary if you only connect to a specific major robot control version)
                itf.RequestCRIVersion();

                while (!itf.flagStopRequest)
                {
                    // this string should be 256 char long max, otherwise it may not be read completly
                    string msg = "ALIVEJOG "
                        + itf.jogValues[0].ToString("0.0") + " " + itf.jogValues[1].ToString("0.0") + " " + itf.jogValues[2].ToString("0.0") + " "
                        + itf.jogValues[3].ToString("0.0") + " " + itf.jogValues[4].ToString("0.0") + " " + itf.jogValues[5].ToString("0.0") + " "
                        + itf.jogValues[6].ToString("0.0") + " " + itf.jogValues[7].ToString("0.0") + " " + itf.jogValues[8].ToString("0.0");
                    itf.SendCommand(msg, itf.flagHideAliveMessages);
                    Thread.Sleep(sleepTime);
                }
            }
            catch (Exception ex)
            {
                log.ErrorFormat("CRI write loop: {0}", ex.Message);
            }
            itf.flagConnected = false;
            itf.flagThreadWriteRunning = false;
            itf.flagStopRequest = true;
            itf.writeThread = null;
        }

        /// <summary>
        /// Main read loop. Sets up a connection to the CRI server on port 3920.
        /// Receives the messages from the CRI server and triggers the parsing.
        /// </summary>
        static void readLoop(object Context)
        {
            HardwareProtocolClient itf = (HardwareProtocolClient)Context;
            itf.flagThreadReadRunning = true;
            try
            {
                itf.clientSocket = new TcpClient();
                itf.clientSocket.Connect(itf.ipAddress, 3920);       // establish a connection on port 3920
                itf.serverStream = itf.clientSocket.GetStream();
                itf.serverStream.ReadTimeout = 100;
                log.InfoFormat("Interface: connected to {0}", itf.ipAddress);
                System.Threading.Thread.Sleep(100);
                byte[] buffer = new byte[4096];
                log.DebugFormat("ReadLoop started...");

                while (!itf.flagStopRequest)                            // Main read loop
                {
                    if (itf.serverStream.DataAvailable)
                    {
                        try
                        {
                            for (int i = 0; i < 4096; i++)
                                buffer[i] = 0x00;

                            //********************** Read Loop *******************************************
                            // CRISTART 1234 msgtype content CRIEND
                            // wait for "CRISTART" and then read until "CRIEND"
                            while (true)
                            {
                                buffer[0] = buffer[1];
                                buffer[1] = buffer[2];
                                buffer[2] = buffer[3];
                                buffer[3] = buffer[4];
                                buffer[4] = buffer[5];
                                buffer[5] = buffer[6];
                                buffer[6] = buffer[7];
                                buffer[7] = (byte)itf.serverStream.ReadByte();

                                if ((buffer[0] == 'C') && (buffer[1] == 'R') && (buffer[2] == 'I') && (buffer[3] == 'S') && (buffer[4] == 'T') && (buffer[5] == 'A') && (buffer[6] == 'R') && (buffer[7] == 'T'))
                                    break;
                            }
                            // read the content until CRIEND
                            int cnt = 8;
                            while (true)
                            {
                                buffer[cnt] = (byte)itf.serverStream.ReadByte();
                                cnt++;
                                if ((buffer[cnt - 6] == 'C') && (buffer[cnt - 5] == 'R') && (buffer[cnt - 4] == 'I') && (buffer[cnt - 3] == 'E') && (buffer[cnt - 2] == 'N') && (buffer[cnt - 1] == 'D'))
                                    break;
                            }

                            // evaluate the content
                            string res = Encoding.ASCII.GetString(buffer, 0, cnt);
                            itf.ParseString(res);
                        }
                        catch (Exception)
                        {
                            Thread.Sleep(5);
                        }
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                }   // end of while()

            }
            catch (Exception E)
            {
                log.ErrorFormat("Could not connect to robot controller: {0}", E.Message);
            }

            itf.flagConnected = false;
            itf.flagThreadReadRunning = false;
            itf.flagStopRequest = true;
            itf.commThread = null;
            return;
        }

        /// <summary>
        /// Starts the read and write loops which then connect to the CRI server
        /// </summary>
        public void Connect()
        {
            log.DebugFormat("Connecting to the remote control...");

            //********************* START READ AND WRITE LOOP ***************************************
            if (!flagConnected)
            {
                flagStopRequest = false;
                flagConnected = true;

                if (writeThread == null)
                {
                    writeThread = new Thread(writeLoop);
                    writeThread.Priority = ThreadPriority.Normal;
                    writeThread.Name = "WriteLoop";
                    System.Globalization.CultureInfo culInf = new System.Globalization.CultureInfo("en-US");   // um Zahlen mit . zu trennen
                    writeThread.CurrentCulture = culInf;
                    writeThread.Start(this);
                }

                if (commThread == null)
                {
                    commThread = new Thread(readLoop);
                    commThread.Priority = ThreadPriority.Normal;
                    commThread.Name = "ReadLoop";
                    System.Globalization.CultureInfo culInf = new System.Globalization.CultureInfo("en-US");   // um Zahlen mit . zu trennen
                    commThread.CurrentCulture = culInf;
                    commThread.Start(this);
                }
            }
            else
            {
                string msg = "Cannot reconnect - already connected!";
                log.ErrorFormat(msg);
                //System.Windows.Forms.MessageBox.Show(msg, "Connection Error");
            }
        }

        /// <summary>
        /// Sends a CRI command
        /// </summary>
        /// <param name="cmd"></param>
        public void SendCommand(string cmd)
        {
            SendCommand(cmd, false);
        }

        /// <summary>
        /// Sends the cmd string to the server when connected
        /// Before sending prefix "CRI cCnt " and postfix " END" are added
        /// </summary>
        public void SendCommand(string cmd, bool silent)
        {
            if (!flagConnected || serverStream == null) return;

            string msg = "CRISTART " + sendCnt.ToString() + " " + cmd + " CRIEND";
            try
            {
                byte[] outStream = Encoding.ASCII.GetBytes(msg);
                serverStream.Write(outStream, 0, outStream.Length);
                serverStream.Flush();

                sendCnt++;
                if (sendCnt >= 10000)
                    sendCnt = 1;

                if (!silent)
                    log.Debug(msg);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Requests the CRI server to add a RelativeLinear command to the current robot program
        /// Parameters are x, y, z in mm and the desired velocity in mm / sec.
        /// Return value is the cmdCnt of the transmitted command
        /// </summary>
        public int SendAddRelativeLin(double x, double y, double z, double velmms)
        {
            string msg = "PROG " + cmdCnt.ToString() + " RELATIVELINEAR "
                + x.ToString("0.0") + " "
                + y.ToString("0.0") + " "
                + z.ToString("0.0") + " "
                + velmms.ToString("0.0");
            SendCommand(msg);
            cmdCnt++;
            if (cmdCnt >= 10000)
                cmdCnt = 0;
            return cmdCnt;
        }

        /// <summary>
        /// Requests the robot control to att a joint motion command to the current program
        /// </summary>
        /// <param name="joints">joint values, must be at least 6 entries</param>
        /// <param name="velpercent">velocity in percent (0.0 - 100.0)</param>
        /// <returns>CRI command number</returns>
        public int SendAddJoint(double[] joints, double velpercent)
        {
            string msg = "PROG " + cmdCnt.ToString() + " JOINT "
                + joints[0].ToString("0.00") + " "
                + joints[1].ToString("0.00") + " "
                + joints[2].ToString("0.00") + " "
                + joints[3].ToString("0.00") + " "
                + joints[4].ToString("0.00") + " "
                + joints[5].ToString("0.00") + " "
                + "EXT 0.0 0.0 0.0 VEL "
                + velpercent.ToString("0.0");
            SendCommand(msg);
            cmdCnt++;
            if (cmdCnt >= 10000)
                cmdCnt = 0;
            return cmdCnt;

        }

        /// <summary>
        /// Requests the robot control to add a wait command to the current program
        /// </summary>
        /// <param name="sec">Number of seconds to wait</param>
        /// <returns>CRI command number</returns>
        public int SendAddWait(double sec)
        {
            string msg = "PROG " + cmdCnt.ToString() + " WAIT"
                + sec.ToString("0.0");
            SendCommand(msg);
            cmdCnt++;
            if (cmdCnt >= 10000)
                cmdCnt = 0;
            return cmdCnt;
        }

        /// <summary>
        /// Requests the robot control to add a gripper command to the current program
        /// </summary>
        /// <param name="a1">position of gripper axis 1 (0 - 100)</param>
        /// <param name="a2">position of gripper axis 2 (not used: 0)</param>
        /// <param name="a3">position of gripper axis 3 (not used: 0)</param>
        /// <returns>CRI command number</returns>
        public int SendAddGripper(double a1, double a2 = 0, double a3 = 0)
        {
            string msg = "PROG " + cmdCnt.ToString() + " GRIPPER"
                + a1.ToString("0.00") + " "
                + a2.ToString("0.00") + " "
                + a3.ToString("0.00");
            SendCommand(msg);
            cmdCnt++;
            if (cmdCnt >= 10000)
                cmdCnt = 0;
            return cmdCnt;
        }


        /// <summary>
        /// Parses a received CRI message
        /// </summary>
        /// <param name="msg">CRI message</param>
        private void ParseString(string msg)
        {
            try
            {
                // Split the string into parts
                string[] parts = msg.Split(' ');
                if ((parts.Length < 3) || (parts[0] != "CRISTART")) return;     // Check for the correct start token

                // The start token is followed by the command number and the message type
                string msgType = parts[2];

                if (msgType == "STATUS")
                {
                    ParseStatusString(msg);
                    if (!flagHideBasicStatusMessages) log.Info(msg);
                    return;
                }

                // Program done: CRISTART sCnt EXECACK cmdNr progNr CRIEND
                else if ((msgType == "EXECACK"))
                {
                    log.Info("Program finished");
                }

                else if (msgType == "VARINFO")
                {
                    log.InfoFormat("Variable information: {0}", msg);
                }

                else if (msgType == "RUNSTATE")
                {
                    if (!flagHideFurtherStatusMessages)
                        log.InfoFormat("Program state is: {0}", msg);
                }

                else if (msgType == "GRIPPERSTATE")
                {
                    if (!flagHideFurtherStatusMessages)
                        log.InfoFormat("Gripper state is: {0}", msg);
                }

                else if (msgType == "GSIG")
                {
                    if (!flagHideFurtherStatusMessages)
                        log.InfoFormat("GSig state is: {0}", msg);
                }

                else if (msgType == "OPINFO") // operation info
                {
                    if (!flagHideFurtherStatusMessages)
                        log.InfoFormat("OPINFO state is: {0}", msg);
                }

                else if (msgType == "CYCLESTAT") // cycle statistics
                {
                    if (!flagHideFurtherStatusMessages) log.Info(msg);
                }

                else if (msgType == "VARIABLES") // variable updates
                {
                    if (!flagHideFurtherStatusMessages) log.Info(msg);
                }

                else if (msgType == "PLATFORM")
                {
                    if (!flagHidePlatformStatusMessages) log.Info(msg);
                }

                else if (msgType == "LOGMSG") // log messages
                {
                    // ignore
                }

                else if (msgType == "APP") // app messages
                {
                    if (!flagHideFurtherStatusMessages) log.Info(msg);
                }

                else if (msgType == "INFO") // info messages
                {
                    if (parts[3] == "Version")
                    {
                        if (int.TryParse(parts[5], out int criVersion))
                        {
                            CRIVersion = criVersion;
                            log.InfoFormat("CRI version: {0}", CRIVersion);
                        }
                    }
                    else
                    {
                        log.Info(msg);
                    }
                }

                else if (!flagHideUnknownMessages)
                {
                    log.Info(msg);
                }
            }
            catch (Exception) { }
        }



        /// <summary>
        /// Parses a STATUS message
        /// </summary>
        /// <param name="msg">CRI message</param>
        private void ParseStatusString(string msg)
        {
            try
            {

                //CRISTART 1234 STATUS MODE joint                            parts[0] to parts[4]
                //POSJOINTSETPOINT 1.00 2.00 3.00 …. 15.00 16.00        parts[5] to parts[21]
                //POSJOINTCURRENT 1.00 2.00 3.00 …. 15.00 16.00         parts[22] to parts[38]
                //POSCARTROBOT 10.0 20.0 30.0 0.00 90.00 0.00           parts[39] to parts[45]
                //POSCARTPLATFORM 10.0 20.0 180.00                      parts[46] to parts[49]
                //OVERRIDE 80.0                                         parts[50] to parts[51]
                //DIN 0 DOUT 0                                          parts[52] to parts[55]
                //ESTOP 3 SUPPLY 13.2 CURRENTALL 2.6                    parts[56] to parts[61]
                //CURRENTJOINTS 0.1 0.2 …  1.5 1.6                      parts[62] to parts[78]
                //ERROR no_error 8 8 8 … 8 8 8                          parts[79] to parts[97]
                //KINSTATE 3 CRIEND                                        parts[99]

                string[] parts = msg.Split(' ');
                if ((parts.Length < 3) || (parts[0] != "CRISTART")) return;
                int nr = int.Parse(parts[1]);

                statusCnt = int.Parse(parts[1]);
                string mode = parts[4];
                int kinState = 0;

                System.Globalization.CultureInfo culInf = System.Globalization.CultureInfo.InvariantCulture;

                //robot arm set point values
                posJointsSetPoint[0] = double.Parse(parts[6], culInf);
                posJointsSetPoint[1] = double.Parse(parts[7], culInf);
                posJointsSetPoint[2] = double.Parse(parts[8], culInf);
                posJointsSetPoint[3] = double.Parse(parts[9], culInf);
                posJointsSetPoint[4] = double.Parse(parts[10], culInf);
                posJointsSetPoint[5] = double.Parse(parts[11], culInf);
                // gripper
                posJointsSetPoint[6] = double.Parse(parts[12], culInf);
                // the other values are not read...

                // robot arm physical values
                posJointsCurrent[0] = double.Parse(parts[23], culInf);
                posJointsCurrent[1] = double.Parse(parts[24], culInf);
                posJointsCurrent[2] = double.Parse(parts[25], culInf);
                posJointsCurrent[3] = double.Parse(parts[26], culInf);
                posJointsCurrent[4] = double.Parse(parts[27], culInf);
                posJointsCurrent[5] = double.Parse(parts[28], culInf);
                // gripper
                posJointsCurrent[6] = double.Parse(parts[29], culInf);

                // cartesian position
                posCartesian[0] = double.Parse(parts[40], culInf);
                posCartesian[1] = double.Parse(parts[41], culInf);
                posCartesian[2] = double.Parse(parts[42], culInf);
                posCartesian[3] = double.Parse(parts[43], culInf);
                posCartesian[4] = double.Parse(parts[44], culInf);
                posCartesian[5] = double.Parse(parts[45], culInf);

                overrideValue = float.Parse(parts[51]);

                // DIO: 52 - 55
                if (CRIVersion < 17)
                {
                    // until CRI V16 the DIO are sent as 64 bit decimal numbers
                    digialInputs = ulong.Parse(parts[53], culInf);
                    digialOutputs = ulong.Parse(parts[55], culInf);
                }
                else
                {
                    // since CRI V17 the DIO are sent as 64 bit hex numbers
                    digialInputs = ulong.Parse(parts[53], System.Globalization.NumberStyles.HexNumber, culInf);
                    digialOutputs = ulong.Parse(parts[55], System.Globalization.NumberStyles.HexNumber, culInf);
                }

                emergencyStopStatus = int.Parse(parts[57], culInf);
                supplyVoltage = int.Parse(parts[59], culInf);
                currentAll = int.Parse(parts[61], culInf);

                for (int i = 0; i < 9; i++)
                    currentJoints[i] = int.Parse(parts[63 + i], culInf);

                errorString = parts[80];
                for (int i = 0; i < 9; i++)
                    errorCodes[i] = int.Parse(parts[81 + i], culInf);

                kinState = int.Parse(parts[98], culInf);

            }
            catch (Exception ex)
            {
                log.ErrorFormat("Error parsing server answer: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Disconnects the CRI client
        /// </summary>
        public void Disconnect()
        {
            flagConnected = false;
            flagStopRequest = true;
        }

        /// <summary>
        /// Gets the connection status
        /// </summary>
        /// <returns>true if connected</returns>
        public bool GetConnectionStatus()
        {
            return flagConnected;
        }

        /// <summary>
        /// Pings the target computer to test whether connecting is possible.
        /// </summary>
        /// <returns>true on success</returns>
        public bool Ping()
        {
            bool res = false;

            string msg = "Ping for robot control on IP ";
            msg += ipAddress;

            Ping Sender = new Ping();
            PingReply Result = Sender.Send(ipAddress);
            if (Result.Status == IPStatus.Success)
            {
                res = true;
                msg += " - successfull";
            }
            else
            {
                msg += " - failure!";
            }

            log.Info(msg);
            return res;
        }

        /// <summary>
        /// Preliminary!
        /// Start the robot controller on the remote linux platform
        /// A SSH connection is used to do so. This requires the user to accept the safety certificate. Before using 
        /// this function a manual SSH connection should be established to check if manual operations are necessary to connect.
        /// </summary>
        /// <returns></returns>
        public bool StartRobotControl()
        {
            bool res = false;
            // call putty via plink to start the robot control via SSH
            // plink -ssh root@192.168.3.11 /home/root/TinyCtrl/startBatch.sh
            string progfn = "/home/root/TinyCtrl/startBatch.sh"; // Note: Since RobotControl V14 the path should be /home/root/RobotControl/...
            //string args = "-ssh -pw password login@" + ipAdress + " " + progfn;
            string args = "-ssh root@" + ipAddress + " " + progfn;

            try
            {
                ProcessStartInfo start = new ProcessStartInfo();            // Prepare the process to run
                start.Arguments = args;                                     // command line arguments
                start.FileName = "plink.exe";                               // executable to run, including the complete path
                //start.WorkingDirectory = workingDirectory;
                start.WindowStyle = ProcessWindowStyle.Normal;              //show a console window?
                start.CreateNoWindow = true;
                // Run the external process & wait for it to finish
                Process proc = Process.Start(start);
                proc.Close();
                //proc.WaitForExit();
                int result = proc.ExitCode;
                log.InfoFormat("Starting remote controller: {0}", args);
                log.InfoFormat("Result: {0}", result);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("Could not start remote controller: {0}", ex.Message);
            }
            return res;
        }

    }
}
