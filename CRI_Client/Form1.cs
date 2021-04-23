using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using log4net;


// Configure log4net using the .config file
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
// This will cause log4net to look for a configuration file
// called ConsoleApp.exe.config in the application base
// directory (i.e. the directory containing ConsoleApp.exe)



namespace CRI_Client
{
    public partial class Form1 : Form
    {


        // Create a logger for use in this class
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        HardwareProtocolClient itf;

        double[] jogValues = new double[9];     // 6 for the robot arm, 3 for the gripper - joint or cartesian [-100..100] 
        


        public Form1()
        {
            InitializeComponent();

            System.Globalization.CultureInfo culInf = new System.Globalization.CultureInfo("en-US");
            Application.CurrentCulture = culInf;

            //string ipAddress = "127.0.0.1";           // local host
            string ipAddress = "192.168.3.11";          // remote computer with TinyCtrl
            //string ipAddress = "172.17.165.161";      // WSL


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


        //***************************************************************
        private void timer1_Tick(object sender, EventArgs e)
        {

            //log.Debug("Test");

            //Jog-Werte an das Interface weiterreichen
            //itf.SetJogValues(jogValues);
            //itf.SendPos();
           
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
            labelError123.Text = "123: " + itf.errrorCodes[0] + " " + itf.errrorCodes[1] + " " + itf.errrorCodes[2];
            labelError456.Text = "456: " + itf.errrorCodes[3] + " " + itf.errrorCodes[4] + " " + itf.errrorCodes[5];
            labelError789.Text = "789: " + itf.errrorCodes[6] + " " + itf.errrorCodes[7] + " " + itf.errrorCodes[8];

            // das LogMessage-Fenster am Boden updaten
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
            catch (Exception ex)
            {
                ;
            }
        }


        //************************************************************************************
        // Established the CRI interface connection to the CRI server
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


        private void buttonJogXPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[0] <= 90.0)
                jogValues[0] += 10.0;
            itf.SetJogValues(jogValues);
        }

        private void buttonJogXMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[0] >= -90.0)
                jogValues[0] -= 10.0;
            itf.SetJogValues(jogValues);
        }

        private void buttonJogYPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[1] <= 90.0)
                jogValues[1] += 10.0;
            itf.SetJogValues(jogValues);
        }

        private void buttonJogYMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[1] >= -90.0)
                jogValues[1] -= 10.0;
            itf.SetJogValues(jogValues);
        }
        private void buttonJogZPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[2] <= 90.0)
                jogValues[2] += 10.0;
            itf.SetJogValues(jogValues);
        }

        private void buttonJogZMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[2] >= -90.0)
                jogValues[2] -= 10.0;
            itf.SetJogValues(jogValues);
        }

        private void buttonJogAMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[3] >= -90.0)
                jogValues[3] -= 10.0;
            itf.SetJogValues(jogValues);
        }

        private void buttonJogAPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[3] <= 90.0)
                jogValues[3] += 10.0;
            itf.SetJogValues(jogValues);
        }

        private void buttonJogBMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[4] >= -90.0)
                jogValues[4] -= 10.0;
            itf.SetJogValues(jogValues);
        }

        private void buttonJogBPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[4] <= 90.0)
                jogValues[4] += 10.0;
            itf.SetJogValues(jogValues);
        }

        private void buttonJogCMinus_Click(object sender, EventArgs e)
        {
            if (jogValues[5] >= -90.0)
                jogValues[5] -= 10.0;
            itf.SetJogValues(jogValues);
        }

        private void buttonJogCPlus_Click(object sender, EventArgs e)
        {
            if (jogValues[5] <= 90.0)
                jogValues[5] += 10.0;
            itf.SetJogValues(jogValues);
        }



        private void buttonJogStop_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 9; i++)
                jogValues[i] = 0.0;
            itf.SetJogValues(jogValues);
        }



        private void buttonJogOverrideMinus_Click(object sender, EventArgs e)
        {
            double ovr = itf.overrideValue;
            if (ovr >= 10.0)
                ovr -= 10.0;
            itf.SetOverride(ovr);
        }

        private void buttonJogOverridePlus_Click(object sender, EventArgs e)
        {
            double ovr = itf.overrideValue;
            if (ovr <= 90.0)
                ovr += 10.0;
            itf.SetOverride(ovr);
        }

        private void buttonGripperOpen_Click(object sender, EventArgs e)
        {
            jogValues[6] = 100.0;
            itf.SetJogValues(jogValues);
        }
        private void buttonGripperClose_Click(object sender, EventArgs e)
        {
            jogValues[6] = -100.0;
            itf.SetJogValues(jogValues);
        }

       


        #endregion



        private void buttonMoveRelativeSend_Click(object sender, EventArgs e)
        {
            double x = double.Parse(textBoxMoveRelativeX.Text);
            double y = double.Parse(textBoxMoveRelativeY.Text);
            double a = double.Parse(textBoxMoveRelativeA.Text);
            double vel = double.Parse(textBoxMoveRelVel.Text);

            itf.SendAddRelativeLin(x, y, a, vel);
        }

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


        //******************************************************************+
        // Get the text of the button and send the according command to the CRI server
        private void buttonSendCmd(object sender, EventArgs e)
        {
            if (!itf.GetConnectionStatus())
            {
                log.Error("Send Command: Cannot send while not connected!");
                return;
            }

            string buttonText = ((System.Windows.Forms.ButtonBase)sender).Text;
            string cmdText = "none";
            switch(buttonText){
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
                case "Delete Prog"   : cmdText = "CMD DeleteProgram"; break;

                case "Open Grp": cmdText = "PROG 55 GRIPPER 100.0 0.0 0.0"; break;
                case "Close Grp": cmdText = "PROG 66 GRIPPER 0.0 0.0 0.0"; break;
                case "Wait 1s": cmdText = "PROG 77 WAIT 1000"; break;
                case "DOut21 true": cmdText = "PROG 88 DOUT 20 true"; break;
                case "DOut21 false": cmdText = "PROG 88 DOUT 20 false"; break;

                case "Shutdown RobotControl": cmdText = "SYSTEM Shutdown 99"; break;
               
            }
            itf.SendCommand(cmdText);
        }

        //*****************************************************************************
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

        
        //*****************************************************************************
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
            for(int i=0; i<nrOfLines; i++)
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




       

        //*******************************************************************************
        // The three check boxes for Motion: joint, cartesian base and cartesian tool
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



        //***************************************************************************
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            itf.StopCRIClient();        // Necessary to allow a clean closing of the communication line
        }


        //*****************************************************************************
        private void buttonStartTinyCtrl_Click(object sender, EventArgs e)
        {
            itf.Ping();
            itf.StartRobotControl();
        }

        

        //*******************************************************************************
        // Storage Command
        // to be used with definitions file only
        private void buttonStorePart_Click(object sender, EventArgs e)
        {
            int[] from = {1,0,0};
            int[] to = {10, 29, 29};
            int hoehe = 5;
            itf.SetStoreCmd(from, to, hoehe);
        }


        //**********************************************************************************
        // Change the digital output 0
        private void checkBoxDOut0_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBoxDOut0.Checked)
                itf.SendCommand("CMD DOUT 0 true");
            else
                itf.SendCommand("CMD DOUT 0 false");

        }

        private void checkBoxDout1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxDout1.Checked)
                itf.SendCommand("CMD DOUT 1 true");
            else
                itf.SendCommand("CMD DOUT 1 false");
        }

        private void buttonRequestNrVar_Click(object sender, EventArgs e)
        {
            string nm = textBoxVarNameNr.Text;
            string msg = "VAR GetNrVariable " + nm;
            itf.SendCommand(msg);
        }
        private void buttonRequestPosVar_Click(object sender, EventArgs e)
        {
            string nm = textBoxVarNamePos.Text;
            string msg = "VAR GetPosVariable " + nm;
            itf.SendCommand(msg);
        }

        

        private void buttonSetNrVar_Click(object sender, EventArgs e)
        {
            string nm = textBoxVarNameNr.Text;
            //float value = float.Parse(textBoxNrVarValue.Text);
            string msg = "VAR SetVariableSingle " + nm + " " + textBoxNrVarValue.Text;//value;
            itf.SendCommand(msg);
        }

        private void buttonSetPosVarJoint_Click(object sender, EventArgs e)
        {
            string nm = textBoxVarNamePos.Text;
            string value = textBoxPosVarJointValue.Text;
            string valueExt = textBoxPosVarExtValues.Text;
            string msg = "VAR SetVariablePosJoint " + nm + " " + value + " " + valueExt;
            itf.SendCommand(msg);
        }

        private void buttonSetPosVarCart_Click(object sender, EventArgs e)
        {
            string nm = textBoxVarNamePos.Text;
            string value = textBoxPosVarCartValue.Text;
            string valueExt = textBoxPosVarExtValues.Text;
            string msg = "VAR SetVariablePosCart " + nm + " " + value + " " + valueExt;
            itf.SendCommand(msg);

        }


        private void checkBoxShowAliveMessages_CheckedChanged(object sender, EventArgs e)
        {
            itf.flagHideAliveMessages = checkBoxHideAliveMessages.Checked;
        }

        private void checkBoxHideStatusMessages_CheckedChanged(object sender, EventArgs e)
        {
            itf.flagHideBasicStatusMessages = checkBoxHideStatusMessages.Checked;
        }

        private void checkBoxHideUnknownMessages_CheckedChanged(object sender, EventArgs e)
        {
            itf.flagHideUnknownMessages = checkBoxHideUnknownMessages.Checked;
        }

        private void checkBoxHideFurtherStatusMessages_CheckedChanged(object sender, EventArgs e)
        {
            itf.flagHideFurtherStatusMessages = checkBoxHideFurtherStatusMessages.Checked;
        }

        private void buttonSendCustomCommand_Click(object sender, EventArgs e)
        {
            string userCommand = ((TextBox)textBoxCustomCommand).Text;
            itf.SendCommand(userCommand);
        }

        private void textBoxCustomCommand_TextChanged(object sender, EventArgs e)
        {
            string userCommand = ((TextBox)textBoxCustomCommand).Text;
            labelCustomCommand.Text = "Command: \"CRISTART 123 " + userCommand + " CRIEND\"";
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            itf.Disconnect();
        }
    }
}
