using System;
using System.IO;
using System.Windows.Forms;

// Configure log4net using the .config file
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
// This will cause log4net to look for a configuration file
// called ConsoleApp.exe.config in the application base
// directory (i.e. the directory containing ConsoleApp.exe)

namespace CRI_Client
{
    public partial class MainForm : Form
    {

        // Create a logger for use in this class
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly HardwareProtocolClient itf;

        readonly double[] jogValues = new double[9];     // 6 for the robot arm, 3 for the gripper - joint or cartesian [-100..100] 

        /// <summary>
        /// Constructor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            System.Globalization.CultureInfo culInf = new System.Globalization.CultureInfo("en-US");
            Application.CurrentCulture = culInf;

            string ipAddress = "192.168.3.11";          // remote computer with TinyCtrl
            textBoxIPAddress.Text = ipAddress;

            buttonJogXPlus.Text = "A1+"; buttonJogXMinus.Text = "A1-";
            buttonJogYPlus.Text = "A2+"; buttonJogYMinus.Text = "A2-";
            buttonJogZPlus.Text = "A3+"; buttonJogZMinus.Text = "A3-";
            buttonJogAMinus.Text = "A4+"; buttonJogAPlus.Text = "A4-";
            buttonJogBPlus.Text = "A5+"; buttonJogBMinus.Text = "A5-";
            buttonJogCPlus.Text = "A6+"; buttonJogCMinus.Text = "A6-";


            timer1.Interval = 100;
            timer1.Enabled = true;
            log.Info("CPR CRI test client");


            itf = new HardwareProtocolClient();
        }

        /// <summary>
        /// UI update timer method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (itf.flagConnected)
                labelConnectionStatus.Text = "Connected";
            else
                labelConnectionStatus.Text = "Not connected";



            string jointsString = "Joints SetPoint:";
            string jointsCurrentString = "Joints Current:";
            for (int i = 0; i < 7; i++)
            {
                jointsString += System.Environment.NewLine;
                jointsString += itf.posJointsSetPoint[i].ToString("0.0");
                jointsCurrentString += System.Environment.NewLine;
                jointsCurrentString += itf.posJointsCurrent[i].ToString("0.0");
            }
            labelPositionJoints.Text = jointsString;
            labelPositionJointsCurrent.Text = jointsCurrentString;



            string posString = "Position Cart:";
            for (int i = 0; i < 6; i++)
            {
                posString += System.Environment.NewLine;
                posString += itf.posCartesian[i].ToString("0.0");
            }
            labelPositionCart.Text = posString;




            string overrideString = "Override: " + itf.overrideValue.ToString("0.0");
            string currentAllString = "CurrentAll [mA]: " + itf.currentAll.ToString();
            string SupplyVoltageString = "Supply [mV]: " + itf.supplyVoltage.ToString();
            string EStopString = "EStop: " + itf.emergencyStopStatus.ToString();
            string CntString = "Cnt: " + itf.statusCnt.ToString();

            // Show the error states, in combination and every single error code
            labelCnt.Text = CntString;
            labelEStop.Text = EStopString;

            labelOverride.Text = overrideString;
            labelErrorStatus.Text = itf.errorString;
            labelError123.Text = "123: " + itf.errorCodes[0] + " " + itf.errorCodes[1] + " " + itf.errorCodes[2];
            labelError456.Text = "456: " + itf.errorCodes[3] + " " + itf.errorCodes[4] + " " + itf.errorCodes[5];
            labelError789.Text = "789: " + itf.errorCodes[6] + " " + itf.errorCodes[7] + " " + itf.errorCodes[8];

            // updates the log message box at the bottom
            try
            {
                if (ListViewAppender.instance != null)
                {
                    while (ListViewAppender.instance.nrNewMessages > 0)
                    {
                        textBoxLogMessages.AppendText(ListViewAppender.instance.GetMessage());
                        textBoxLogMessages.AppendText("\r\n");
                    }
                }
            }
            catch (Exception)
            {
                ;
            }
        }

        /// <summary>
        /// Established the CRI interface connection to the CRI server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonItfConnect_Click(object sender, EventArgs e)
        {
            itf.SetIPAddress(textBoxIPAddress.Text);
            if (!itf.GetConnectionStatus())
            {
                itf.Connect();
                log.Info("Interface: Connecting...");
            }
            else
            {
                log.Info("Interface: already connected");
            }
        }



        //******************** REGION JOG **********************
        #region jog

        /// <summary>
        /// Handles the jog X+ button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogXPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[0] <= 90.0)
                jogValues[0] += 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog X- button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogXMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[0] >= -90.0)
                jogValues[0] -= 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog Y+ button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogYPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[1] <= 90.0)
                jogValues[1] += 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog Y- button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogYMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[1] >= -90.0)
                jogValues[1] -= 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog Z+ button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogZPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[2] <= 90.0)
                jogValues[2] += 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog Z- button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogZMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[2] >= -90.0)
                jogValues[2] -= 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog A- button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogAMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[3] >= -90.0)
                jogValues[3] -= 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog A+ button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogAPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[3] <= 90.0)
                jogValues[3] += 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog B- button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogBMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[4] >= -90.0)
                jogValues[4] -= 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog B+ button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogBPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[4] <= 90.0)
                jogValues[4] += 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog C- button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogCMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[5] >= -90.0)
                jogValues[5] -= 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog C+ button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogCPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[5] <= 90.0)
                jogValues[5] += 10.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog stop button (sets all jog values to 0)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogStop_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 9; i++)
                jogValues[i] = 0.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog velocity override - button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogOverrideMinus_Click(object sender, EventArgs e)
        {
            double ovr = itf.overrideValue;
            if (ovr >= 10.0)
                ovr -= 10.0;
            itf.SetOverride(ovr);
        }

        /// <summary>
        /// Handles the jog velocity override + button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonJogOverridePlus_Click(object sender, EventArgs e)
        {
            double ovr = itf.overrideValue;
            if (ovr <= 90.0)
                ovr += 10.0;
            itf.SetOverride(ovr);
        }

        /// <summary>
        /// Handles the jog gripper open button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonGripperOpen_Click(object sender, EventArgs e)
        {
            jogValues[6] = 100.0;
            itf.SetJogValues(jogValues);
        }

        /// <summary>
        /// Handles the jog gripper close button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonGripperClose_Click(object sender, EventArgs e)
        {
            jogValues[6] = -100.0;
            itf.SetJogValues(jogValues);
        }

        #endregion

        /// <summary>
        /// Handles the move relative send button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMoveRelativeSend_Click(object sender, EventArgs e)
        {
            double x = double.Parse(textBoxMoveRelativeX.Text);
            double y = double.Parse(textBoxMoveRelativeY.Text);
            double a = double.Parse(textBoxMoveRelativeA.Text);
            double vel = double.Parse(textBoxMoveRelVel.Text);

            itf.SendAddRelativeLin(x, y, a, vel);
        }

        /// <summary>
        /// Handles the move joint send button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMoveJointSend_Click(object sender, EventArgs e)
        {
            double[] j = new double[6];
            j[0] = double.Parse(textBoxMoveJoint1.Text);            // for later applications checking the user input makes sense
            j[1] = double.Parse(textBoxMoveJoint2.Text);
            j[2] = double.Parse(textBoxMoveJoint3.Text);
            j[3] = double.Parse(textBoxMoveJoint4.Text);
            j[4] = double.Parse(textBoxMoveJoint5.Text);
            j[5] = double.Parse(textBoxMoveJoint6.Text);

            double vel = double.Parse(textBoxMoveJointVel.Text);


            itf.SendAddJoint(j, vel);
        }


        /// <summary>
        /// Get the text of the button and send the according command to the CRI server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSendCmd(object sender, EventArgs e)
        {
            if (!itf.GetConnectionStatus())
            {
                log.Error("Send Command: Cannot send while not connected!");
                return;
            }

            string buttonText = ((System.Windows.Forms.ButtonBase)sender).Text;
            string cmdText = "none";
            switch (buttonText)
            {
                case "Connect": cmdText = "CMD Connect"; break;
                case "Disconnect": cmdText = "CMD Disconnect"; break;
                case "Zero Joints": cmdText = "CMD SetJointsToZero"; break;
                case "Reset": cmdText = "CMD Reset"; break;
                case "Enable": cmdText = "CMD Enable"; break;
                case "Disable": cmdText = "CMD Disable"; break;
                case "Set Joints to Zero": cmdText = "CMD SetJointsToZero"; break;
                case "Reference J1": cmdText = "CMD ReferenceSingleJoint 0"; break;
                case "Reference All Joints": cmdText = "CMD ReferenceAllJoints "; break;

                case "Start": cmdText = "CMD StartProgram"; break;
                case "Stop": cmdText = "CMD StopProgram"; break;
                case "Pause": cmdText = "CMD PauseProgram"; break;
                case "Delete Prog": cmdText = "CMD DeleteProgram"; break;

                case "Open Grp": cmdText = "PROG 55 GRIPPER 100.0 0.0 0.0"; break;
                case "Close Grp": cmdText = "PROG 66 GRIPPER 0.0 0.0 0.0"; break;
                case "Wait 1s": cmdText = "PROG 77 WAIT 1000"; break;
                case "DOut21 true": cmdText = "PROG 88 DOUT 20 true"; break;
                case "DOut21 false": cmdText = "PROG 88 DOUT 20 false"; break;

                case "Shutdown RobotControl": cmdText = "SYSTEM Shutdown 99"; break;

            }
            itf.SendCommand(cmdText);
        }

        /// <summary>
        /// Handles the load program button (to load a program on the robot control)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLoadProg_Click(object sender, EventArgs e)
        {
            if (!itf.GetConnectionStatus())
            {
                log.Error("Send Command: Cannot send while not connected!");
                return;
            }

            string cmdText = "CMD LoadProgram ";
            cmdText += "test_matrix.xml";
            itf.SendCommand(cmdText);

        }

        /// <summary>
        /// Handles the send program button (to transmit a program to the robot control)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCmdSendProgram_Click(object sender, EventArgs e)
        {
            //if (!itf.GetConnectionStatus())
            //{
            //    log.Error("Send Program: Cannot send while not connected!");
            //    return;
            //}

            string progName = "test_matrix.xml";
            StreamReader sr;
            string line;
            string msg;

            // anzahl der Zeilen im Programm herausbekommen
            int nrOfLines = 0;
            sr = new StreamReader(progName);
            while (!sr.EndOfStream)
            {
                line = sr.ReadLine();
                nrOfLines++;
            }

            // dann übertragen
            msg = "CMD UploadFileInit ";
            msg += "Programs/" + progName;
            msg += " ";
            msg += nrOfLines;
            itf.SendCommand(msg);

            sr = new StreamReader(progName);
            for (int i = 0; i < nrOfLines; i++)
            {
                System.Threading.Thread.Sleep(10);
                line = sr.ReadLine();

                msg = "CMD UploadFileLine ";
                msg += line;
                itf.SendCommand(msg);

            }
            System.Threading.Thread.Sleep(10);
            msg = "CMD UploadFileFinish";
            itf.SendCommand(msg);
        }

        /// <summary>
        /// Handles the motion type checkbox for joint motion
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxJoint_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxJoint.Checked)
            {
                checkBoxCartBase.Checked = false;
                checkBoxCartTool.Checked = false;
                checkBoxPlatform.Checked = false;

                buttonJogXPlus.Text = "A1+"; buttonJogXMinus.Text = "A1-";
                buttonJogYPlus.Text = "A2+"; buttonJogYMinus.Text = "A2-";
                buttonJogZPlus.Text = "A3+"; buttonJogZMinus.Text = "A3-";
                buttonJogAPlus.Text = "A4+"; buttonJogAMinus.Text = "A4-";
                buttonJogBPlus.Text = "A5+"; buttonJogBMinus.Text = "A5-";
                buttonJogCPlus.Text = "A6+"; buttonJogCMinus.Text = "A6-";

                itf.SendCommand("CMD MotionTypeJoint");
            }
        }

        /// <summary>
        /// Handles the motion type checkbox for cartesian base motion
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxCartBase_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxCartBase.Checked)
            {
                checkBoxJoint.Checked = false;
                checkBoxCartTool.Checked = false;
                checkBoxPlatform.Checked = false;


                buttonJogXPlus.Text = "X+"; buttonJogXMinus.Text = "X-";
                buttonJogYPlus.Text = "Y+"; buttonJogYMinus.Text = "Y-";
                buttonJogZPlus.Text = "Z+"; buttonJogZMinus.Text = "Z-";
                buttonJogAPlus.Text = "A+"; buttonJogAMinus.Text = "A-";
                buttonJogBPlus.Text = "B+"; buttonJogBMinus.Text = "B-";
                buttonJogCPlus.Text = "C+"; buttonJogCMinus.Text = "C-";

                itf.SendCommand("CMD MotionTypeCartBase");
            }
        }

        /// <summary>
        /// Handles the motion type checkbox for cartesian tool motion
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxCartTool_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxCartTool.Checked)
            {
                checkBoxCartBase.Checked = false;
                checkBoxJoint.Checked = false;
                checkBoxPlatform.Checked = false;

                buttonJogXPlus.Text = "X+"; buttonJogXMinus.Text = "X-";
                buttonJogYPlus.Text = "Y+"; buttonJogYMinus.Text = "Y-";
                buttonJogZPlus.Text = "Z+"; buttonJogZMinus.Text = "Z-";
                buttonJogAPlus.Text = "A+"; buttonJogAMinus.Text = "A-";
                buttonJogBPlus.Text = "B+"; buttonJogBMinus.Text = "B-";
                buttonJogCPlus.Text = "C+"; buttonJogCMinus.Text = "C-";

                itf.SendCommand("CMD MotionTypeCartTool");
            }
        }

        /// <summary>
        /// Handles the motion type checkbox for platform motion
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxPlatform_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxPlatform.Checked)
            {
                checkBoxCartBase.Checked = false;
                checkBoxJoint.Checked = false;
                checkBoxCartTool.Checked = false;

                buttonJogXPlus.Text = "Forward"; buttonJogXMinus.Text = "Backward";
                buttonJogYPlus.Text = "Right-Lat"; buttonJogYMinus.Text = "Left-Lat";
                buttonJogZPlus.Text = "Right-Rot"; buttonJogZMinus.Text = "Left-Rot";
                buttonJogAPlus.Text = "NC"; buttonJogAMinus.Text = "NC";
                buttonJogBPlus.Text = "NC"; buttonJogBMinus.Text = "NC";
                buttonJogCPlus.Text = "NC"; buttonJogCMinus.Text = "NC";

                itf.SendCommand("CMD MotionTypePlatform");
            }
        }

        /// <summary>
        /// Handles the window close button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            itf.StopCRIClient();        // Necessary to allow a clean closing of the communication line
        }

        /// <summary>
        /// Handles the start TinyCtrl button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonStartTinyCtrl_Click(object sender, EventArgs e)
        {
            itf.Ping();
            itf.StartRobotControl();
        }

        /// <summary>
        /// Change the digital output 0
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxDOut0_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxDOut0.Checked)
                itf.SendCommand("CMD DOUT 0 true");
            else
                itf.SendCommand("CMD DOUT 0 false");
        }

        /// <summary>
        /// Change the digital output 1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxDout1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxDout1.Checked)
                itf.SendCommand("CMD DOUT 1 true");
            else
                itf.SendCommand("CMD DOUT 1 false");
        }

        /// <summary>
        /// Requests a number variable by name
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRequestNrVar_Click(object sender, EventArgs e)
        {
            string nm = textBoxVarNameNr.Text;
            string msg = "VAR GetNrVariable " + nm;
            itf.SendCommand(msg);
        }

        /// <summary>
        /// Requests a position variable by name
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRequestPosVar_Click(object sender, EventArgs e)
        {
            string nm = textBoxVarNamePos.Text;
            string msg = "VAR GetPosVariable " + nm;
            itf.SendCommand(msg);
        }

        /// <summary>
        /// Sets a number variable
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSetNrVar_Click(object sender, EventArgs e)
        {
            string nm = textBoxVarNameNr.Text;
            string msg = "VAR SetVariableSingle " + nm + " " + textBoxNrVarValue.Text;
            itf.SendCommand(msg);
        }

        /// <summary>
        /// Sets the joint values of a position variable
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSetPosVarJoint_Click(object sender, EventArgs e)
        {
            string nm = textBoxVarNamePos.Text;
            string value = textBoxPosVarJointValue.Text;
            string valueExt = textBoxPosVarExtValues.Text;
            string msg = "VAR SetVariablePosJoint " + nm + " " + value + " " + valueExt;
            itf.SendCommand(msg);
        }

        /// <summary>
        /// Sets the cartesian values of a position variable
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSetPosVarCart_Click(object sender, EventArgs e)
        {
            string nm = textBoxVarNamePos.Text;
            string value = textBoxPosVarCartValue.Text;
            string valueExt = textBoxPosVarExtValues.Text;
            string msg = "VAR SetVariablePosCart " + nm + " " + value + " " + valueExt;
            itf.SendCommand(msg);

        }

        /// <summary>
        /// Handles the show alive messages checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxShowAliveMessages_CheckedChanged(object sender, EventArgs e)
        {
            itf.flagHideAliveMessages = checkBoxHideAliveMessages.Checked;
        }

        /// <summary>
        /// Handles the hide status messages checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxHideStatusMessages_CheckedChanged(object sender, EventArgs e)
        {
            itf.flagHideBasicStatusMessages = checkBoxHideStatusMessages.Checked;
        }

        /// <summary>
        /// Handles the hide unknown messages checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxHideUnknownMessages_CheckedChanged(object sender, EventArgs e)
        {
            itf.flagHideUnknownMessages = checkBoxHideUnknownMessages.Checked;
        }

        /// <summary>
        /// Handles the hide further status messages checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxHideFurtherStatusMessages_CheckedChanged(object sender, EventArgs e)
        {
            itf.flagHideFurtherStatusMessages = checkBoxHideFurtherStatusMessages.Checked;
        }

        /// <summary>
        /// Handles the hide platform messages checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxHidePlatformMessages_CheckedChanged(object sender, EventArgs e)
        {
            itf.flagHidePlatformStatusMessages = checkBoxHidePlatformMessages.Checked;
        }

        /// <summary>
        /// Handles the send custom command button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSendCustomCommand_Click(object sender, EventArgs e)
        {
            string userCommand = ((TextBox)textBoxCustomCommand).Text;
            itf.SendCommand(userCommand);
        }

        /// <summary>
        /// Handles the custom command text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxCustomCommand_TextChanged(object sender, EventArgs e)
        {
            string userCommand = ((TextBox)textBoxCustomCommand).Text;
            labelCustomCommand.Text = "Command: \"CRISTART 123 " + userCommand + " CRIEND\"";
        }

        /// <summary>
        /// Handles the disconnect button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            itf.Disconnect();
        }

        /// <summary>
        /// Handles the move to by joint motion button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMoveToJoint_Click(object sender, EventArgs e)
        {
            double.TryParse(tbMoveToJ1.Text, out double j1);
            double.TryParse(tbMoveToJ2.Text, out double j2);
            double.TryParse(tbMoveToJ3.Text, out double j3);
            double.TryParse(tbMoveToJ4.Text, out double j4);
            double.TryParse(tbMoveToJ5.Text, out double j5);
            double.TryParse(tbMoveToJ6.Text, out double j6);
            double.TryParse(tbMoveToVelPerc.Text, out double velperc);

            string userCommand = string.Format("CMD Move Joint {0} {1} {2} {3} {4} {5} 0 0 0 {6}", j1, j2, j3, j4, j5, j6, velperc);
            itf.SendCommand(userCommand);
        }

        /// <summary>
        /// Handles the move to by relative joint motion button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMoveToRelativeJoint_Click(object sender, EventArgs e)
        {
            double.TryParse(tbMoveToJ1.Text, out double j1);
            double.TryParse(tbMoveToJ2.Text, out double j2);
            double.TryParse(tbMoveToJ3.Text, out double j3);
            double.TryParse(tbMoveToJ4.Text, out double j4);
            double.TryParse(tbMoveToJ5.Text, out double j5);
            double.TryParse(tbMoveToJ6.Text, out double j6);
            double.TryParse(tbMoveToVelPerc.Text, out double velperc);

            string userCommand = string.Format("CMD Move RelativeJoint {0} {1} {2} {3} {4} {5} 0 0 0 {6}", j1, j2, j3, j4, j5, j6, velperc);
            itf.SendCommand(userCommand);
        }

        /// <summary>
        /// Handles the move to by cartesian motion button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMoveToCart_Click(object sender, EventArgs e)
        {
            double.TryParse(tbMoveToX.Text, out double x);
            double.TryParse(tbMoveToY.Text, out double y);
            double.TryParse(tbMoveToZ.Text, out double z);
            double.TryParse(tbMoveToA.Text, out double a);
            double.TryParse(tbMoveToB.Text, out double b);
            double.TryParse(tbMoveToC.Text, out double c);
            double.TryParse(tbMoveToVelMms.Text, out double velmms);

            string userCommand = string.Format("CMD Move Cart {0} {1} {2} {3} {4} {5} 0 0 0 {6}", x, y, z, a, b, c, velmms);
            itf.SendCommand(userCommand);
        }

        /// <summary>
        /// Handles the move to by relative cartesian base motion button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMoveToRelativeBase_Click(object sender, EventArgs e)
        {
            double.TryParse(tbMoveToX.Text, out double x);
            double.TryParse(tbMoveToY.Text, out double y);
            double.TryParse(tbMoveToZ.Text, out double z);
            double.TryParse(tbMoveToA.Text, out double a);
            double.TryParse(tbMoveToB.Text, out double b);
            double.TryParse(tbMoveToC.Text, out double c);
            double.TryParse(tbMoveToVelMms.Text, out double velmms);

            string userCommand = string.Format("CMD Move RelativeBase {0} {1} {2} {3} {4} {5} 0 0 0 {6}", x, y, z, a, b, c, velmms);
            itf.SendCommand(userCommand);
        }

        /// <summary>
        /// Handles the move to by relative cartesian tool motion button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMoveToRelativeTool_Click(object sender, EventArgs e)
        {
            double.TryParse(tbMoveToX.Text, out double x);
            double.TryParse(tbMoveToY.Text, out double y);
            double.TryParse(tbMoveToZ.Text, out double z);
            double.TryParse(tbMoveToA.Text, out double a);
            double.TryParse(tbMoveToB.Text, out double b);
            double.TryParse(tbMoveToC.Text, out double c);
            double.TryParse(tbMoveToVelMms.Text, out double velmms);

            string userCommand = string.Format("CMD Move RelativeTool {0} {1} {2} {3} {4} {5} 0 0 0 {6}", x, y, z, a, b, c, velmms);
            itf.SendCommand(userCommand);
        }

        /// <summary>
        /// Handles the move stop button (stops a move-to motion)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonMoveToStop_Click(object sender, EventArgs e)
        {
            itf.SendCommand("CMD Move Stop");
        }

        /// <summary>
        /// Handles the set CRI connection active button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSetActive_Click(object sender, EventArgs e)
        {
            itf.SendCommand("CMD SetActive true");
        }

    }
}
