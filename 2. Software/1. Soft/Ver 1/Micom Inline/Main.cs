using Micom_Inline.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Text.Json;
using Elnec.Pg4uw.RemotelbNET;
using System.Drawing.Text;
using Microsoft.Win32;

namespace Micom_Inline
{
    public partial class Main : Form
    {

        public const string Version = "1.8.4";
        bool matchServer = true;
        // Permissions 
        public const string OP = "OP";
        public const string TECH = "TECH";
        public const string MANAGER = "MANAGER";

        public static string Permissions = OP;

        // const color
        public Color activeColor = Color.FromArgb(30, 136, 221);
        public Color nonactiveColor = Color.FromArgb(62, 62, 62);
        public Color deactiveColor = Color.FromArgb(0, 164, 0);
        public Color busyColor = Color.DarkGray;

        public Color fistROMsellect = Color.FromArgb(30, 136, 221);
        public Color secondROMsellect = Color.Blue;
        public Color thirdROMsellect = Color.OliveDrab;
        public Color forthROMsellect = Color.Brown;

        //sever config
        const string SERVER_ON = "severon";
        const string SERVER_OFF = "severoff";
        public string ServerStatus = SERVER_ON;

        public int ROMsellectCounter = 0;

        string MachineStatus = "RUNNING";

        //elnec
        ElnecSite Site1 = new ElnecSite(1);
        ElnecSite Site2 = new ElnecSite(2);
        ElnecSite Site3 = new ElnecSite(3);
        ElnecSite Site4 = new ElnecSite(4);

        public string RemoteIP = "127.0.0.1";
        public int RemotePort = 21;
        public int ElnecAddress = 0;

        //arduino
        const int Cmd_startValue = 64;

        const string String_getOK = "@010011*76";
        const string String_getNG = "@010000*65";

        const string Data_sendTest = "@010100*66";
        const string Result_ngPBA = "@010200*67";
        const string Result_okPBA1 = "@010201*68";
        const string Result_okPBA2 = "@010210*77";
        const string Result_okPBA = "@010211*78";

        const string Data_sendQR = "@010300*68";
        const string Data_skipQR = "@010400*69";
        const string Data_enaQR = "@010401*70";
        const string Result_ngQR = "@010410*79";
        const string Result_okQR = "@010411*80";

        const string Mode_1Array = "@010501*71";
        const string Mode_2Array = "@010511*81";

        public string ResultRespoonse = "";
        private bool Timeout = false;
        bool startTest = false;
        bool endTest = true;

        public bool Site1IsTalking = false;
        public bool Site2IsTalking = false;
        public bool Site3IsTalking = false;
        public bool Site4IsTalking = false;

        // chart animations
        public int CharCircle = 1;
        public int percentProcess = 0;

        public string PortReciver = string.Empty;
        public string[] BaudRate = { "300", "1200", "2400", "4800", "9600", "19200", "38400", "57600", "74880", "115200" };

        PCB_Model model = new PCB_Model
        {
            ModelName = "",
            PCBcode = "",
        };

        DateTime lastWorkingTime = new DateTime();

        public static AMW_CONFIG _CONFIG = new AMW_CONFIG();

        WorkProcess AMWsProcess = new WorkProcess();

        bool TestingFlag = false;

        //Text box tbLog line number
        public int tbLogLineNumber = 0;

        public string[] ElnecCMD = { "bringtofront","showmainform","hidemainform",
                                    "blankcheck","readdevice","verifydevice",
                                    "programdevice", "erasedevice",
                                    "rundeviceop","stopoperation",
                                    "closeapp", "getprogstatus",
                                    "selectdevice:","autoseldevice:",
                                    "cmdlineparams:program /noanyquest","refindpgm",
                                    "selftest", "selftestplus",
                                    "programisbusy","clienprogramisready",
                                    "loadfile:", "savefile:",
                                    "loadproject:","loadprjpasswd:","getdevchecksum",
                                    "savelogtofile:", "readbuffer:","writebuffer:",
                                    "readbufferex:","writebufferex:", "readbufferresult:",
                                    "writebufferresult:", "good","outofrange",
                                    "sizeoutofrange","protectedmodeact", "savelogtofileresult:", "getprojectfilechecksum:",
                                    "repetmode:off"};


        MySQLDatabase Database = new MySQLDatabase();
        /// <summary>
        /// start main function
        /// </summary>
        /// 
        private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var requestedNameAssembly = new System.Reflection.AssemblyName(args.Name);
            var requestedName = requestedNameAssembly.Name;
            if (requestedName.EndsWith(".resources")) return null;
            var binFolder = Application.StartupPath;
            var fullPath = System.IO.Path.Combine(binFolder, requestedName) + ".dll";
            if (System.IO.File.Exists(fullPath))
            {
                return System.Reflection.Assembly.LoadFrom(fullPath);
            }

            return null;
        }

        private static readonly string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private static readonly string StartupValue = "A_MS";
        private static void SetStartup()
        {
            //Set the application to run at startup
            RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, true);
            key.SetValue(StartupValue, Application.ExecutablePath.ToString());
        }


        public Main()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            tslPreviewName.Text = "Auto Multi Writing System(A-MS) V" + Version;
            this.SetStyle(ControlStyles.Selectable, false);

            tbStRomCsSite1.AutoSize = false;
            tbStRomCsSite1.Size = new System.Drawing.Size(122, 24);
            tbStRomCsSite2.AutoSize = false;
            tbStRomCsSite2.Size = new System.Drawing.Size(122, 24);
            tbStRomCsSite3.AutoSize = false;
            tbStRomCsSite3.Size = new System.Drawing.Size(122, 24);
            tbStRomCsSite4.AutoSize = false;
            tbStRomCsSite4.Size = new System.Drawing.Size(122, 24);
            pnWriting.Hide();
            Port.DataReceived += new SerialDataReceivedEventHandler(DataReciver);

            cbbComBaurate.DataSource = BaudRate;
            cbbComName.DataSource = SerialPort.GetPortNames();

            ElnecAddress = _CONFIG.ElnecAddress;
            cbServerCompare.Checked = _CONFIG.ServerCompare;
            cbInlineMachine.Checked = _CONFIG.InlineMachine;
            tbHistory.AppendText("<<<<<< AUTO MICOM WRITING SYSTEM >>>>>>" + Environment.NewLine);
            tsslPermissions.Text = "User: " + Permissions;

            gbLog.Visible = false;
            gbTestHistory.Visible = true;
            gbSetting.Visible = false;

            gbSetting.BringToFront();
            pnResultFinal.BringToFront();
            pnResultFinal.Visible = false;

            lastWorkingTime = DateTime.Now;
            timerUpdateChar.Start();
            timerCheckCom.Start();
            checkPermision();
            SkipBarCode();

            RemoteIP = _CONFIG.RemoteIP;
            RemotePort = _CONFIG.RemotePort;
            tbStTCPIP.Text = RemoteIP;
            tbStTCPPort.Text = RemotePort.ToString();

            Site1.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
            Site2.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
            Site3.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
            Site4.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);

            try
            {
                Port.PortName = SearchCom();
                tsslbCOM.Text = Port.PortName + "        ";
                Port.Open();
            }
            catch (Exception)
            {
                tsslbCOM.Text = "COM ERROR               ";
            }
        }

        public bool LosingTime = true;
        public int LostTimeSet = 60;
        public void DateTimeShow()
        {
            while (true)
            {
                string t = DateTime.Now.ToString();
                TimeSpan lostTime = DateTime.Now.Subtract(lastWorkingTime);
                lbClock.Invoke(new MethodInvoker(delegate
                {
                    lbClock.Text = t;
                    if (LosingTime)
                    {
                        lbFreeTime.Text = "Lost time: " + lostTime.TotalSeconds.ToString("f0") + " S";
                        if (lostTime.TotalSeconds > LostTimeSet && MachineStatus == "RUNNING")
                        {
                            MachineStatus = "STOP";
                        }
                    }
                    else
                    {
                        lbWorkingtime.Text = "Test time: " + lostTime.TotalSeconds.ToString("f1") + " S";
                        if (MachineStatus == "STOP")
                        {
                            MachineStatus = "RUNNING";
                        }
                    }
                }));
                Thread.Sleep(100);
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            //this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;

            DgwTestMode_Init();
            DrawChart(AMWsProcess.Statitis_OK, AMWsProcess.Statitis_NG, CharCircle);
            Thread showTime = new Thread(DateTimeShow);
            pnManualControl.Enabled = false;
            gbManualTest.Hide();
            gbSerialTest.Hide();

            showTime.Start();
            timerUpdateMes.Start();
            lbAdressSite1.Text = _CONFIG.ElnecStrAddress.ToString() + "-" + ElnecAddress.ToString("d5");
            lbAdressSite2.Text = _CONFIG.ElnecStrAddress.ToString() + "-" + (ElnecAddress + 1).ToString("d5");
            lbAdressSite3.Text = _CONFIG.ElnecStrAddress.ToString() + "-" + (ElnecAddress + 2).ToString("d5");
            lbAdressSite4.Text = _CONFIG.ElnecStrAddress.ToString() + "-" + (ElnecAddress + 3).ToString("d5");

            timerReleaseBoard.Interval = 2500;
            timerReleaseBoard.Start();

            _CONFIG.RemotePort = Convert.ToInt32(tbStTCPPort.Text);
            _CONFIG.RemoteIP = tbStTCPIP.Text;

            gbServerCompare.Enabled = _CONFIG.ServerCompare;
            cbInlineMachine.Checked = _CONFIG.InlineMachine;
            cbInlineMachine_CheckedChanged(cbInlineMachine, EventArgs.Empty);

            Thread communicationSite1 = new Thread(ElnecComuncationBackgroudSite1);
            communicationSite1.Start();
            Thread communicationSite2 = new Thread(ElnecComuncationBackgroudSite2);
            communicationSite2.Start();
            Thread communicationSite3 = new Thread(ElnecComuncationBackgroudSite3);
            communicationSite3.Start();
            Thread communicationSite4 = new Thread(ElnecComuncationBackgroudSite4);
            communicationSite4.Start();
            if (_CONFIG.ServerCompare)
            {
                if (Database.Connect())
                {
                    lbServerConnect.Text = "Server connected   ";
                    List<string> Lines = new List<string>();
                    Database.getLineList(Lines);
                    cbbLineLock.Items.AddRange(Lines.ToArray());
                    for (int i = 0; i < cbbLineLock.Items.Count; i++)
                    {
                        if (cbbLineLock.Items[i].ToString() == _CONFIG.Line)
                        {
                            cbbLineLock.SelectedIndex = i;
                            break;
                        }
                    }
                    timerUpdateStatus.Start();
                }
                else
                {
                    lbServerConnect.Text = "Server not available   ";
                }
            }

            gbTestCounter.ContextMenuStrip = contextMenu;
        }

        // Serial reciver
        private void DataReciver(object obj, SerialDataReceivedEventArgs e)
        {
            if (!Port.IsOpen) return;
            string Frame = "";
            try
            {
                Frame = Port.ReadLine();
                Port.DiscardInBuffer();
                Port.DiscardOutBuffer();
                tbSerialData.Invoke(
                    new MethodInvoker(
                        delegate
                        {
                            tbSerialData.AppendText("[--RX] " + Frame + Environment.NewLine);
                        }));

                if (Frame.Contains(Data_sendTest))
                {
                    Port.Write(String_getOK);
                    tbSerialData.Invoke(
                        new MethodInvoker(
                            delegate
                            {
                                tbSerialData.AppendText("[TX--] " + String_getOK + Environment.NewLine);
                                pnWriting.Show();
                                pnWriting.BringToFront();
                            }));

                    if (lbAutoManual.Text == "Auto mode" && !TestingFlag)
                    {
                        TestingFlag = true;
                        pnWriting.Show();
                        pnWriting.BringToFront();
                        Site1.WorkProcess.ClearCMDQueue();
                        Site2.WorkProcess.ClearCMDQueue();
                        Site3.WorkProcess.ClearCMDQueue();
                        Site4.WorkProcess.ClearCMDQueue();

                        Site1.progressValue = 0;
                        Site2.progressValue = 0;
                        Site3.progressValue = 0;
                        Site4.progressValue = 0;

                        lbMachineStatus.Invoke(
                        new MethodInvoker(
                            delegate
                            {
                                if (lbROM1checkSum.Text != lbSite1Checksum.Text
                                    || lbROM2checkSum.Text != lbSite2Checksum.Text
                                    || lbROM3checkSum.Text != lbSite3Checksum.Text
                                    || lbROM4checkSum.Text != lbSite4Checksum.Text)
                                {
                                    lbResultA.BackColor = Color.Red;
                                    lbResultB.BackColor = Color.Red;
                                    lbResultC.BackColor = Color.Red;
                                    lbResultD.BackColor = Color.Red;
                                    lbMachineStatus.Text = "ERROR";
                                    lbMachineStatus.BackColor = Color.Red;
                                    MessageBox.Show("Ckecksum not match or not have program loader, do not program");
                                }
                                else
                                {
                                    Site1.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                                    Site2.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                                    Site3.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                                    Site4.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);

                                    ActiveLabel(lbResultA);
                                    ActiveLabel(lbResultB);
                                    ActiveLabel(lbResultC);
                                    ActiveLabel(lbResultD);

                                    if (!cbSkipSite1.Checked)
                                        Site1.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
                                    else
                                    {
                                        OK_label(lbResultA);
                                        OK_label(lbResultA);
                                        Site1.Result = ElnecSite.RESULT_OK;
                                    }
                                    if (!cbSkipSite2.Checked)
                                        Site2.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
                                    else
                                    {
                                        OK_label(lbResultB);
                                        OK_label(lbResultB);
                                        Site2.Result = ElnecSite.RESULT_OK;
                                    }
                                    if (!cbSkipSite3.Checked)
                                        Site3.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
                                    else
                                    {
                                        OK_label(lbResultC);
                                        OK_label(lbResultC);
                                        Site3.Result = ElnecSite.RESULT_OK;
                                    }
                                    if (!cbSkipSite4.Checked)
                                        Site4.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
                                    else
                                    {
                                        OK_label(lbResultD);
                                        OK_label(lbResultD);
                                        Site4.Result = ElnecSite.RESULT_OK;
                                    }

                                    startTest = true;
                                    endTest = false;
                                    lastWorkingTime = DateTime.Now;
                                    Timeout = false;
                                    timerTimeOut.Stop();
                                    timerTimeOut.Interval = model.TimeOut * 1000;
                                    timerTimeOut.Start();

                                    lbMachineStatus.Text = "WRITTING"; lbMachineStatus.BackColor = activeColor;
                                    tbHistory.AppendText(DateTime.Now.ToString("#----------------------------------------" +
                                        Environment.NewLine + "dd/MM/yyyy - hh/mm/ss : ") +
                                        "Model " + model.ModelName +
                                        Environment.NewLine);
                                    FinalTestBigLabel(false);
                                    resetdgwTestMode();
                                    highlinedgwTestMode(0);
                                    pbTesting.Value = 0;
                                    pbTesting.Maximum = 800;
                                    LosingTime = false;
                                }
                            }));
                    }
                }
                else if (Frame.Contains(Data_sendQR))
                {
                    Port.Write(String_getOK);
                    tbSerialData.Invoke(
                        new MethodInvoker(
                            delegate
                            {
                                tbSerialData.AppendText("[TX--] " + String_getOK + Environment.NewLine);
                            }));
                    Thread senQRresult = new Thread(SendOKQR);
                    senQRresult.Start();
                }
                else if (Frame.Contains(String_getNG))
                {
                    Port.Write(ResultRespoonse);
                    tbSerialData.Invoke(
                        new MethodInvoker(
                            delegate
                            {
                                tbSerialData.AppendText("[TX--] " + String_getOK + Environment.NewLine);
                            }));
                    Console.WriteLine(ResultRespoonse);
                }
                else if (Frame.Contains(String_getOK))
                {
                    ResultRespoonse = "";
                }
                else if (Frame.Contains("@") && Frame.Contains("*"))
                {
                    Port.Write(String_getNG);
                    tbSerialData.Invoke(
                        new MethodInvoker(
                            delegate
                            {
                                tbSerialData.AppendText("[TX--] " + String_getNG + Environment.NewLine);
                            }));
                }
            }
            catch (Exception)
            { }
        }

        public void SendOKQR()
        {
            Thread.Sleep(1000);
            Port.Write(Result_okQR);
            tbSerialData.Invoke(
                new MethodInvoker(
                    delegate
                    {
                        tbSerialData.AppendText("[TX--] " + String_getNG + Environment.NewLine);
                    }));
            ResultRespoonse = Result_okQR;
        }


        //get QR code from scaner 
        DateTime _lastKeystroke = new DateTime(0);
        List<char> _barcode = new List<char>();
        private void _KeyPress(object sender, KeyPressEventArgs e)
        {
            // check timing (keystrokes within 100 ms)
            TimeSpan elapsed = (DateTime.Now - _lastKeystroke);
            if (elapsed.TotalMilliseconds > 100)
            {
                _barcode.Clear();
                // process barcode
            }

            else if (e.KeyChar == 13)
            {
                string msg = new String(_barcode.ToArray());
                lbModelName.Invoke(new MethodInvoker(delegate { lbModelName.Text = msg.Substring(2, 10); }));
                _barcode.Clear();
            }
            else
            {
                _barcode.Add(e.KeyChar);
                _lastKeystroke = DateTime.Now;
            }
        }


        private void BtnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void BtClose_Click(object sender, EventArgs e)
        {
            Closing closing = new Closing();
            closing.TopLevel = true;
            closing.Show();

            _CONFIG.SaveConfig();
            if (_CONFIG.ServerCompare)
            {
                Database.UpdateRunStopStatus("STOP", _CONFIG.Line);
            }


            if (Port.IsOpen) Port.Close();

            Site1.WorkProcess.PushComandToFist(ElnecSite.CLOSE_APP);
            Site2.WorkProcess.PushComandToFist(ElnecSite.CLOSE_APP);
            Site3.WorkProcess.PushComandToFist(ElnecSite.CLOSE_APP);
            Site4.WorkProcess.PushComandToFist(ElnecSite.CLOSE_APP);

            Thread.Sleep(2000);
            CloseElnec();
            closing.Hide();
            Environment.Exit(Environment.ExitCode);
            this.Close();
            Application.Exit();
        }

        private void BtnMaximize_Click(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                btnMaximize.BackgroundImage = Resources.masinize;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.None;
                this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea;
                this.WindowState = FormWindowState.Maximized;
                btnMaximize.BackgroundImage = Resources.minimize;
            }


        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        private void LbFormName_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();

                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
            if (WindowState == FormWindowState.Maximized)
            {
                this.FormBorderStyle = FormBorderStyle.None;
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
            }
        }

        private void Main_Resize(object sender, System.EventArgs e)
        {
            DrawChart(AMWsProcess.Statitis_OK, AMWsProcess.Statitis_NG, 360);

            if (_CONFIG.InlineMachine)
            {
                model.Layout.drawPCBLayout(pbPCBLayout);
                model.Layout.drawPCBLayout(pbLayout);
            }
            else
            {
                model.Layout.drawPCBLayout(pbPCBLayout, true);
                model.Layout.drawPCBLayout(pbLayout, true);
            }

        }

        private void BtLoadModel_Click(object sender, EventArgs e)
        {
            btLoadModel.BackColor = Color.FromArgb(50, 50, 50);
            btAuto.BackColor = Color.FromArgb(30, 30, 30);
            btManual.BackColor = Color.FromArgb(30, 30, 30);
            btReportFolder.BackColor = Color.FromArgb(30, 30, 30);
            btSetting.BackColor = Color.FromArgb(30, 30, 30);

            OPForm form = new OPForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                openFileModel.InitialDirectory = _CONFIG.recentModelPath;
                openFileModel.ShowDialog();
            }
        }

        public string SearchCom()
        {
            string ComName = "";
            String Com_Scope = "root\\CIMV2";
            String Query_String = "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(Com_Scope, Query_String);
            foreach (ManagementObject queryObj in searcher.Get())
            {
                if (queryObj["Caption"] != null)
                {
                    if (queryObj["Caption"].ToString().Contains("CH340")
                        || queryObj["Caption"].ToString().Contains("Arduino")
                        || queryObj["Caption"].ToString().Contains("Serial"))
                    {
                        ComName = queryObj["Caption"].ToString();
                        ComName = ComName.Substring(ComName.LastIndexOf('(') + 1, ComName.LastIndexOf(')') - 1 - ComName.LastIndexOf('('));
                        break;
                    }
                }
            }
            return ComName;
        }
        private void BtAuto_Click(object sender,
                                  EventArgs e)
        {
            Timeout = false;

            Site1.ClearSiteParam();
            Site2.ClearSiteParam();
            Site3.ClearSiteParam();
            Site4.ClearSiteParam();

            ActiveLabel(lbResultA);
            ActiveLabel(lbResultB);
            ActiveLabel(lbResultC);
            ActiveLabel(lbResultD);

            gbSetting.Visible = false;
            btAuto.BackColor = Color.FromArgb(50, 50, 50);
            btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
            btManual.BackColor = Color.FromArgb(30, 30, 30);
            btReportFolder.BackColor = Color.FromArgb(30, 30, 30);
            btSetting.BackColor = Color.FromArgb(30, 30, 30);
            pnManualControl.Enabled = false;

            if (lbAutoManual.Text == "Auto mode" || lbAutoManual.ForeColor == deactiveColor)
            {
                lbAutoManual.Text = "IDE";
                lbAutoManual.ForeColor = Color.White;
            }
            else
            {
                resetdgwTestMode();
                try
                {
                    tsslbCOM.Text = Port.PortName + "        ";
                    if (!Port.IsOpen) Port.Open();
                }
                catch (Exception)
                {
                    tsslbCOM.Text = "COM ERROR    ";
                }
                if (Port.IsOpen && model != null)
                {
                    if (_CONFIG.InlineMachine == true) // Micom inline: send mode 1 array or 2 arry to control ler board
                    {
                        if (model.Layout.PCB1 && !model.Layout.PCB2)
                        {
                            Port.Write(Mode_1Array);
                            tbSerialData.AppendText("[TX--] " + Mode_1Array + Environment.NewLine);
                            //    tbHistory.AppendText("Mode 1 array had set.\r\n");
                        }
                        else if (model.Layout.PCB1 && model.Layout.PCB2)
                        {
                            Port.Write(Mode_2Array);
                            tbSerialData.AppendText("[TX--] " + Mode_2Array + Environment.NewLine);
                            //    tbHistory.AppendText("Mode 2 array had set.\r\n");
                        }
                    }
                    else
                    {
                        string sendCMD = "";
                        if (model.Layout.ArrayCount == 2 && model.Layout.MicomNumber == 2)
                        {
                            sendCMD = command.GetCMDByName("Mode_2PCB");
                        }
                        else
                        {
                            sendCMD = command.GetCMDByName("Mode_4PCB");
                        }
                        Port.Write(sendCMD);
                        tbSerialData.AppendText("[TX--] " + sendCMD + Environment.NewLine);
                    }
                }
                else
                {
                    MessageBox.Show("Com not connect, Array setting maybe not apply.");
                }
                if (_CONFIG.ServerCompare)
                {
                    if (matchServer)
                    {
                        lbAutoManual.Text = "Auto mode";
                        lbAutoManual.ForeColor = activeColor;
                    }
                }
                else
                {
                    lbAutoManual.Text = "Auto mode";
                    lbAutoManual.ForeColor = activeColor;
                }

            }

        }

        private void BtManual_Click(object sender, EventArgs e)
        {
            if (pnManualControl.Enabled != true)
            {
                PassWorldForm formPass = new PassWorldForm();
                DialogResult dialogResult = formPass.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    Permissions = MANAGER;

                    gbSetting.Visible = false;
                    btManual.BackColor = Color.FromArgb(50, 50, 50);
                    btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
                    btAuto.BackColor = Color.FromArgb(30, 30, 30);
                    btReportFolder.BackColor = Color.FromArgb(30, 30, 30);
                    btSetting.BackColor = Color.FromArgb(30, 30, 30);

                    lbAutoManual.Text = "Manual mode";
                    lbMachineStatus.Text = "Manual";
                    lbAutoManual.ForeColor = deactiveColor;

                    pnManualControl.Enabled = true;
                }
                else if (dialogResult == DialogResult.Ignore)
                {
                    Permissions = TECH;

                    gbSetting.Visible = false;
                    btManual.BackColor = Color.FromArgb(50, 50, 50);
                    btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
                    btAuto.BackColor = Color.FromArgb(30, 30, 30);
                    btReportFolder.BackColor = Color.FromArgb(30, 30, 30);
                    btSetting.BackColor = Color.FromArgb(30, 30, 30);

                    lbAutoManual.Text = "Manual mode";
                    lbMachineStatus.Text = "Manual";
                    lbAutoManual.ForeColor = deactiveColor;

                    pnManualControl.Enabled = true;
                }
                else
                    Permissions = OP;
                checkPermision();
            }
            else
                pnManualControl.Enabled = false;

        }

        private void BtReportFolder_Click(object sender, EventArgs e)
        {
            PassWorldForm formPass = new PassWorldForm();
            DialogResult dialogResult = formPass.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                Permissions = MANAGER;
                checkPermision();
                gbSetting.Visible = false;
                btReportFolder.BackColor = Color.FromArgb(50, 50, 50);
                btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
                btAuto.BackColor = Color.FromArgb(30, 30, 30);
                btManual.BackColor = Color.FromArgb(30, 30, 30);
                btSetting.BackColor = Color.FromArgb(30, 30, 30);

                Report form = new Report();
                form.ShowDialog();
            }
            else if (dialogResult == DialogResult.Ignore)
            {
                Permissions = TECH;
                checkPermision();
                gbSetting.Visible = false;
                btReportFolder.BackColor = Color.FromArgb(50, 50, 50);
                btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
                btAuto.BackColor = Color.FromArgb(30, 30, 30);
                btManual.BackColor = Color.FromArgb(30, 30, 30);
                btSetting.BackColor = Color.FromArgb(30, 30, 30);

                Report form = new Report();
                form.ShowDialog();
            }
            else
                Permissions = OP;

            Permissions = OP;
            checkPermision();
            formPass.Dispose();
        }

        private void BtSetting_Click(object sender, EventArgs e)
        {
            if (gbSetting.Visible == false)
            {
                PassWorldForm formPass = new PassWorldForm();
                DialogResult dialogResult = formPass.ShowDialog();
                _CONFIG = new AMW_CONFIG();
                if (dialogResult == DialogResult.OK)
                {
                    Permissions = MANAGER;
                    checkPermision();
                    gbSetting.Visible = true;
                    btSetting.BackColor = Color.FromArgb(50, 50, 50);
                    btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
                    btAuto.BackColor = Color.FromArgb(30, 30, 30);
                    btManual.BackColor = Color.FromArgb(30, 30, 30);
                    btReportFolder.BackColor = Color.FromArgb(30, 30, 30);

                    LoadToSettingPanel();

                    //ElnecEndAdd.Text = ElnecAddress.ToString("d5");

                    //radioButton2.Checked = model.Layout.PCB2;

                    //if (MicomArray.Maximum < model.Layout.MicomNumber)
                    //{
                    //    MicomArray.Maximum = model.Layout.MicomNumber;
                    //}
                    //MicomArray.Value = model.Layout.MicomNumber;

                    //if (PCBarrayCount.Maximum < model.Layout.ArrayCount)
                    //{
                    //    PCBarrayCount.Maximum = model.Layout.ArrayCount;
                    //}
                    //PCBarrayCount.Value = model.Layout.ArrayCount;

                    //if (nbUDXarrayCount.Maximum < model.Layout.XasixArrayCount)
                    //{
                    //    nbUDXarrayCount.Maximum = model.Layout.XasixArrayCount;
                    //}
                    //nbUDXarrayCount.Value = model.Layout.XasixArrayCount;

                    //if (_CONFIG.InlineMachine)
                    //{
                    //    model.Layout.drawPCBLayout(pbPCBLayout);
                    //}
                    //else
                    //{
                    //    model.Layout.drawPCBLayout(pbPCBLayout, true);
                    //}
                }
                else if (dialogResult == DialogResult.Ignore)
                {
                    Permissions = TECH;
                    checkPermision();
                    gbSetting.Visible = true;
                    btSetting.BackColor = Color.FromArgb(50, 50, 50);
                    btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
                    btAuto.BackColor = Color.FromArgb(30, 30, 30);
                    btManual.BackColor = Color.FromArgb(30, 30, 30);
                    btReportFolder.BackColor = Color.FromArgb(30, 30, 30);

                    LoadToSettingPanel();
                    //ElnecEndAdd.Text = ElnecAddress.ToString("d5");
                    //if (PCBarrayCount.Maximum < model.Layout.ArrayCount)
                    //{
                    //    PCBarrayCount.Maximum = model.Layout.ArrayCount;
                    //}
                    //PCBarrayCount.Value = model.Layout.ArrayCount;

                    //if (nbUDXarrayCount.Maximum < model.Layout.XasixArrayCount)
                    //{
                    //    nbUDXarrayCount.Maximum = model.Layout.XasixArrayCount;
                    //}
                    //nbUDXarrayCount.Value = model.Layout.XasixArrayCount;
                    //radioButton2.Checked = model.Layout.PCB2;
                    //if (_CONFIG.InlineMachine)
                    //{
                    //    model.Layout.drawPCBLayout(pbPCBLayout);
                    //}
                    //else
                    //{
                    //    model.Layout.drawPCBLayout(pbPCBLayout, true);
                    //}
                }
                else
                    Permissions = OP;
                formPass.Dispose();
            }
            else
                gbSetting.Visible = false;
        }

        public void LoadToSettingPanel()
        {
            try
            {
                tbQRname.Text = model.ModelName.Split('_')[0]; ;
                tbVersion.Text = model.Version;
            }
            catch (Exception)
            {}


            lbStRomNameSite1.Text = lbRomNameSite1.Text;
            lbStRomNameSite2.Text = lbRomNameSite2.Text;
            lbStRomNameSite3.Text = lbRomNameSite3.Text;
            lbStRomNameSite4.Text = lbRomNameSite4.Text;

            tbStRomCsSite1.Text = model.ROMs[0].ROM_CHECKSUM;
            tbStRomCsSite2.Text = model.ROMs[1].ROM_CHECKSUM;
            tbStRomCsSite3.Text = model.ROMs[2].ROM_CHECKSUM;
            tbStRomCsSite4.Text = model.ROMs[3].ROM_CHECKSUM;

            ElnecEndAdd.Text = ElnecAddress.ToString("d5");
            radioButton2.Checked = model.Layout.PCB2;

            if (MicomArray.Maximum < model.Layout.MicomNumber)
            {
                MicomArray.Maximum = model.Layout.MicomNumber;
            }
            MicomArray.Value = model.Layout.MicomNumber;

            if (PCBarrayCount.Maximum < model.Layout.ArrayCount)
            {
                PCBarrayCount.Maximum = model.Layout.ArrayCount;
            }
            PCBarrayCount.Value = model.Layout.ArrayCount;

            if (nbUDXarrayCount.Maximum < model.Layout.XasixArrayCount)
            {
                nbUDXarrayCount.Maximum = model.Layout.XasixArrayCount;
            }
            nbUDXarrayCount.Value = model.Layout.XasixArrayCount;

            if (_CONFIG.InlineMachine)
            {
                model.Layout.drawPCBLayout(pbPCBLayout);
            }
            else
            {
                model.Layout.drawPCBLayout(pbPCBLayout, true);
            }
        }

        public void ActiveLabel(System.Windows.Forms.Label label)
        {
            label.BackColor = activeColor;
        }
        public void DeactiveLabel(System.Windows.Forms.Label label)
        {
            label.BackColor = deactiveColor;
        }

        public void OK_label(System.Windows.Forms.Label label)
        {
            label.BackColor = Color.Green;
        }
        public void NG_label(System.Windows.Forms.Label label)
        {
            label.BackColor = Color.Red;
        }
        public void FinalTestLabel()
        {
            pnWriting.Hide();
            TestingFlag = false;
            Port.DiscardInBuffer();
            Port.DiscardOutBuffer();
            string final = "";
            if (_CONFIG.InlineMachine)
            {
                if (model.Layout.PCB1 && !model.Layout.PCB2)
                {
                    if (model.Layout.MicomNumber == 1)
                    {
                        if (Site1.Result != ElnecSite.EMPTY && Site2.Result != ElnecSite.EMPTY && Site3.Result != ElnecSite.EMPTY && Site4.Result != ElnecSite.EMPTY)
                        {
                            string now = DateTime.Now.ToString();

                            if (Site1.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK)
                            {
                                AMWsProcess.Statitis_OK += 1;
                                lbMachineStatus.Invoke(new MethodInvoker(delegate { lbMachineStatus.Text = "OK"; lbMachineStatus.BackColor = Color.Green; }));
                                ResultRespoonse = Result_okPBA1;
                            }
                            else if (Site1.Result == ElnecSite.RESULT_NG || Site3.Result == ElnecSite.RESULT_NG)
                            {
                                AMWsProcess.Statitis_NG += 1;
                                ResultRespoonse = Result_okPBA2;
                                lbMachineStatus.Invoke(new MethodInvoker(delegate { lbMachineStatus.Text = "FAIL"; lbMachineStatus.BackColor = Color.Red; }));
                            }

                            if (Site1.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK)
                                final = "OK";
                            else
                                final = "FAIL";

                            _CONFIG.reportWriteLine(now, lbModelName.Text, final, Site1.Result, Site2.Result, Site3.Result, Site4.Result);

                            tbHistory.Invoke(new MethodInvoker(delegate
                            {
                                timerTimeOut.Stop();
                                timerTimeOut.Dispose();
                                FinalTestBigLabel(true);
                                percentProcess = tbLogLineNumber;
                                tbHistory.AppendText("        A: " + Site1.Result + "  B: " + Site2.Result + "  C: " + Site3.Result + "  D: " + Site4.Result + Environment.NewLine);
                                tbHistory.AppendText(ResultRespoonse + Environment.NewLine);
                                Console.WriteLine(ResultRespoonse);
                                CharCircle = 1;
                                timerUpdateChar.Start();
                                highlinedgwTestMode(2);
                                timerReleaseBoard.Interval = 5;
                                timerReleaseBoard.Start();

                                Site1.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                                Site2.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                                Site3.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                                Site4.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);

                                Site1.ClearSiteParam();
                                Site2.ClearSiteParam();
                                Site3.ClearSiteParam();
                                Site4.ClearSiteParam();

                            }));
                            startTest = false;
                            endTest = true;
                        }
                    }
                    else if (model.Layout.MicomNumber == 2)
                    {
                        if (Site1.Result != ElnecSite.EMPTY && Site2.Result != ElnecSite.EMPTY && Site3.Result != ElnecSite.EMPTY && Site4.Result != ElnecSite.EMPTY)
                        {
                            string now = DateTime.Now.ToString();
                            //if (Site1.Result == ElnecSite.RESULT_OK && Site2.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                            if (Site1.Result == ElnecSite.RESULT_OK && Site2.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                            {
                                AMWsProcess.Statitis_OK += 1;
                                lbMachineStatus.Invoke(new MethodInvoker(delegate { lbMachineStatus.Text = "OK"; lbMachineStatus.BackColor = Color.Green; }));
                                final = "OK";
                                ResultRespoonse = Result_okPBA1;
                            }
                            else
                            {
                                AMWsProcess.Statitis_NG += 1;
                                ResultRespoonse = Result_okPBA2;
                                lbMachineStatus.Invoke(new MethodInvoker(delegate { lbMachineStatus.Text = "FAIL"; lbMachineStatus.BackColor = Color.Red; }));
                                final = "FAIL";
                            }

                            if (Site1.Result == ElnecSite.RESULT_OK && Site2.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                                final = "OK";
                            else
                                final = "FAIL";

                            _CONFIG.reportWriteLine(now, lbModelName.Text, final, Site1.Result, Site2.Result, Site3.Result, Site4.Result);

                            tbHistory.Invoke(new MethodInvoker(delegate
                            {
                                timerTimeOut.Stop();
                                timerTimeOut.Dispose();
                                FinalTestBigLabel(true);
                                percentProcess = tbLogLineNumber;
                                tbHistory.AppendText("        A: " + Site1.Result + "  B: " + Site2.Result + "  C: " + Site3.Result + "  D: " + Site4.Result + Environment.NewLine);
                                tbHistory.AppendText(ResultRespoonse + Environment.NewLine);
                                Console.WriteLine(ResultRespoonse);
                                CharCircle = 1;
                                timerUpdateChar.Start();
                                highlinedgwTestMode(2);
                                timerReleaseBoard.Interval = 5;
                                timerReleaseBoard.Start();

                                Site1.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                                Site2.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                                Site3.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                                Site4.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);

                                Site1.ClearSiteParam();
                                Site2.ClearSiteParam();
                                Site3.ClearSiteParam();
                                Site4.ClearSiteParam();
                            }));
                            startTest = false;
                            endTest = true;
                        }
                    }
                }
                else if (model.Layout.PCB1 && model.Layout.PCB2)
                {
                    if (Site1.Result != ElnecSite.EMPTY && Site2.Result != ElnecSite.EMPTY && Site3.Result != ElnecSite.EMPTY && Site4.Result != ElnecSite.EMPTY)
                    {
                        string now = DateTime.Now.ToString();
                        //if (Site1.Result == ElnecSite.RESULT_OK && Site2.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                        if (Site1.Result == ElnecSite.RESULT_OK && Site2.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                        {
                            AMWsProcess.Statitis_OK += 2;
                            lbMachineStatus.Invoke(new MethodInvoker(delegate { lbMachineStatus.Text = "OK"; lbMachineStatus.BackColor = Color.Green; }));
                            ResultRespoonse = Result_okPBA;
                        }
                        else if (Site1.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK)
                        {
                            ResultRespoonse = Result_okPBA1;
                        }

                        else if (Site2.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                        {
                            ResultRespoonse = Result_okPBA2;
                        }

                        else if ((Site1.Result == ElnecSite.RESULT_NG || Site3.Result == ElnecSite.RESULT_NG) && (Site2.Result == ElnecSite.RESULT_NG || Site4.Result == ElnecSite.RESULT_NG))
                        {
                            ResultRespoonse = Result_ngPBA;
                            lbMachineStatus.Invoke(new MethodInvoker(delegate { lbMachineStatus.Text = "FAIL"; lbMachineStatus.BackColor = Color.Red; }));
                        }

                        if (model.Layout.PCB1 && model.Layout.PCB2)
                        {
                            if (Site1.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK)
                                AMWsProcess.Statitis_OK += 1;
                            else
                                AMWsProcess.Statitis_NG += 1;

                            if (Site2.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                                AMWsProcess.Statitis_OK += 1;
                            else
                                AMWsProcess.Statitis_NG += 1;
                        }
                        else
                        {
                            if (Site1.Result == ElnecSite.RESULT_OK && Site2.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                                AMWsProcess.Statitis_OK += 1;
                            else
                                AMWsProcess.Statitis_NG += 1;
                        }

                        if (Site1.Result == ElnecSite.RESULT_OK && Site2.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                            final = "OK";
                        else
                            final = "FAIL";

                        _CONFIG.reportWriteLine(now, lbModelName.Text, final, Site1.Result, Site2.Result, Site3.Result, Site4.Result);

                        tbHistory.Invoke(new MethodInvoker(delegate
                        {
                            timerTimeOut.Stop();
                            timerTimeOut.Dispose();
                            FinalTestBigLabel(true);
                            percentProcess = tbLogLineNumber;
                            tbHistory.AppendText("        A: " + Site1.Result + "  B: " + Site2.Result + "  C: " + Site3.Result + "  D: " + Site4.Result + Environment.NewLine);
                            tbHistory.AppendText(ResultRespoonse + Environment.NewLine);
                            Console.WriteLine(ResultRespoonse);
                            CharCircle = 1;
                            timerUpdateChar.Start();
                            highlinedgwTestMode(2);
                            timerReleaseBoard.Interval = 5;
                            timerReleaseBoard.Start();

                            Site1.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                            Site2.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                            Site3.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                            Site4.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);

                            Site1.ClearSiteParam();
                            Site2.ClearSiteParam();
                            Site3.ClearSiteParam();
                            Site4.ClearSiteParam();
                        }));
                        startTest = false;
                        endTest = true;
                    }
                }
            }
            else
            {
                if (Site1.Result != ElnecSite.EMPTY && Site2.Result != ElnecSite.EMPTY && Site3.Result != ElnecSite.EMPTY && Site4.Result != ElnecSite.EMPTY)
                {
                    byte data = 0b00000000;
                    string dataResponse = "@0103data*checksum";
                    if (Site1.Result == ElnecSite.RESULT_OK)
                    {
                        data = (byte)(data | 0b00000001);
                        AMWsProcess.Statitis_OK += 1;
                    }
                    else
                        AMWsProcess.Statitis_NG += 1;

                    if (Site2.Result == ElnecSite.RESULT_OK)
                    {
                        data = (byte)(data | 0b00000010);
                        AMWsProcess.Statitis_OK += 1;
                    }
                    else
                        AMWsProcess.Statitis_NG += 1;

                    if (Site3.Result == ElnecSite.RESULT_OK)
                    {
                        data = (byte)(data | 0b00000100);
                        AMWsProcess.Statitis_OK += 1;
                    }
                    else
                        AMWsProcess.Statitis_NG += 1;

                    if (Site4.Result == ElnecSite.RESULT_OK)
                    {
                        data = (byte)(data | 0b00001000);
                        AMWsProcess.Statitis_OK += 1;
                    }
                    else
                        AMWsProcess.Statitis_NG += 1;

                    if (Site1.Result == ElnecSite.RESULT_OK && Site2.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                    {
                        final = "OK";
                        lbMachineStatus.Invoke(new MethodInvoker(delegate { lbMachineStatus.Text = "OK"; lbMachineStatus.BackColor = Color.Green; }));
                    }
                    else
                    {
                        final = "FAIL";
                        lbMachineStatus.Invoke(new MethodInvoker(delegate { lbMachineStatus.Text = "FAIL"; lbMachineStatus.BackColor = Color.Red; }));
                    }

                    int checksum = (64 + 1 + 03 + data) % 256;

                    ResultRespoonse = dataResponse.Replace("data", data.ToString("D2")).Replace("checksum", checksum.ToString());
                    string now = DateTime.Now.ToString();
                    _CONFIG.reportWriteLine(now, lbModelName.Text, final, Site1.Result, Site2.Result, Site3.Result, Site4.Result);

                    tbHistory.Invoke(new MethodInvoker(delegate
                    {
                        timerTimeOut.Stop();
                        timerTimeOut.Dispose();
                        FinalTestBigLabel(true);
                        percentProcess = tbLogLineNumber;
                        tbHistory.AppendText("        A: " + Site1.Result + "  B: " + Site2.Result + "  C: " + Site3.Result + "  D: " + Site4.Result + Environment.NewLine);
                        tbHistory.AppendText(ResultRespoonse + Environment.NewLine);
                        Console.WriteLine(ResultRespoonse);
                        CharCircle = 1;
                        timerUpdateChar.Start();
                        highlinedgwTestMode(2);
                        timerReleaseBoard.Interval = 5;
                        timerReleaseBoard.Start();

                        Site1.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                        Site2.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                        Site3.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                        Site4.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);

                        Site1.ClearSiteParam();
                        Site2.ClearSiteParam();
                        Site3.ClearSiteParam();
                        Site4.ClearSiteParam();

                    }));
                    startTest = false;
                    endTest = true;
                }
            }
        }

        public void FinalTestBigLabel(bool show)
        {
            pnResultFinal.Visible = false;
            lbResultAbig.Visible = false;
            lbResultBbig.Visible = false;
            lbResultCbig.Visible = false;
            lbResultDbig.Visible = false;

            lbResultAbig.Text = Site1.Result;
            lbResultBbig.Text = Site2.Result;
            lbResultCbig.Text = Site3.Result;
            lbResultDbig.Text = Site4.Result;

            if (cbSkipSite1.Checked)
            {

                lbResultAbig.Text = "SKIP";
                ActiveLabel(lbResultAbig);
                lbResultAbig.BackColor = Color.Gray;
            }

            if (cbSkipSite2.Checked)
            {

                lbResultBbig.Text = "SKIP";
                ActiveLabel(lbResultBbig);
                lbResultBbig.BackColor = Color.Gray;
            }
            if (cbSkipSite3.Checked)
            {

                lbResultCbig.Text = "SKIP";
                ActiveLabel(lbResultCbig);
                lbResultCbig.BackColor = Color.Gray;
            }
            if (cbSkipSite4.Checked)
            {

                lbResultDbig.Text = "SKIP";
                ActiveLabel(lbResultDbig);
                lbResultDbig.BackColor = Color.Gray;
            }
            pnResultFinal.Visible = show;
            lbResultAbig.Visible = show;
            lbResultBbig.Visible = show;
            lbResultCbig.Visible = show;
            lbResultDbig.Visible = show;
        }


        private void DrawChart(int okNumber, int ngNumber, int charCicle)
        {
            int Total = okNumber + ngNumber;

            lbCounterNumberNG.Text = ngNumber.ToString();
            lbCounterNumberOK.Text = okNumber.ToString();
            lbCounterNumberTotal.Text = Total.ToString();
            float persentOk;
            if (Total > 0)
                persentOk = (float)okNumber / (float)Total * 100;
            else
                persentOk = 0;
            lbCounterNumberDef.Text = persentOk.ToString("f1");

            if (Total == 0) Total = 10000000;
            float okRadian = (float)charCicle / Total * okNumber;
            float ngRadian = (float)charCicle - okRadian;

            int startRectY = pBChar.Size.Height / 2 - pBChar.Size.Width / 2;
            int startRectX = pBChar.Size.Width / 2 - pBChar.Size.Height / 2;
            int rectDimemtions = pBChar.Size.Width;

            if (startRectY < 0)
            {
                startRectY = 0;
                rectDimemtions = pBChar.Size.Height;
            }
            if (startRectX < 0)
            {
                startRectX = 0;
                rectDimemtions = pBChar.Size.Width;
            }

            if (pBChar.Size.Width > 50 && pBChar.Size.Height > 50)
            {
                Rectangle rect = new Rectangle(startRectX, startRectY, rectDimemtions, rectDimemtions);
                Rectangle rectInside = new Rectangle(startRectX + rectDimemtions / 4, startRectY + rectDimemtions / 4, rectDimemtions / 2, rectDimemtions / 2);
                Bitmap custormChart = new Bitmap(pBChar.Size.Width, pBChar.Size.Height);
                Graphics g = Graphics.FromImage(custormChart);

                Color okColor = Color.FromArgb(30, 136, 221);
                Color bacgroudColor = Color.FromArgb(62, 62, 62);
                SolidBrush brush = new SolidBrush(okColor);
                SolidBrush brushNumber = new SolidBrush(Color.White);
                SolidBrush brushInside = new SolidBrush(bacgroudColor);

                g.FillPie(brush, rect, 0, okRadian);
                g.FillPie(Brushes.Red, rect, okRadian, ngRadian);
                g.FillPie(brushInside, rectInside, 0, 360);

                string persenOkString = persentOk.ToString("F1") + " %";
                Font persentOkFont = new Font("Microsoft YaHei UI", rectDimemtions / 14, FontStyle.Bold);
                g.DrawString(persenOkString, persentOkFont, brushNumber, startRectX + rectDimemtions / 2 - (persenOkString.Length * 4 * rectDimemtions / 14 / 10), startRectY + rectDimemtions / 2 - rectDimemtions / 14);

                if (pBChar.Image != null)
                    pBChar.Image.Dispose();

                pBChar.Image = custormChart;
                brush.Dispose();
                brushInside.Dispose();
                brushNumber.Dispose();
                g.Dispose();
                //custormChart.Dispose();
            }
        }

        public void DgwTestMode_Init()
        {
            dgtTestMode.Rows.Add("1", "STA", "command", "WRITE", "", "", "", "");
            dgtTestMode.Rows.Add("2", "ROM", "program", "READ", "", "", "", "");
            dgtTestMode.Rows.Add("3", "DLY", "delay 500ms", "DELAY", "", "", "", "");
            dgtTestMode.Rows.Add("4", "FIN", "release", "CMD", "", "", "", "");
        }

        public void resetdgwTestMode()
        {
            dgtTestMode.Rows[0].Selected = false;
            dgtTestMode.Rows[1].Selected = false;
            dgtTestMode.Rows[2].Selected = false;
            dgtTestMode.Rows[3].Selected = false;
        }
        public void highlinedgwTestMode(int line)
        {
            if (line >= 0 && line < dgtTestMode.Rows.Count)
                dgtTestMode.Rows[line].Selected = true;
        }
        private void TbLog_TextChanged(object sender, EventArgs e)
        {
            tbLog.SelectionStart = tbLog.Text.Length;
            tbLog.AppendText(tbLog.SelectedText);
        }
        private void TbHistory_TextChanged(object sender, EventArgs e)
        {
            tbHistory.SelectionStart = tbHistory.Text.Length;
            tbHistory.AppendText(tbHistory.SelectedText);
        }
        // Elnec control funtions

        public void CloseElnec()
        {
            string strCmdText = "/c taskkill /im pg4uw.exe /t /f";
            ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd.exe")
            {
                ErrorDialog = false,
                WorkingDirectory = @"C:",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = strCmdText
            };

            ///command contains the command to be executed in cmd
            Process proc = new Process
            {
                StartInfo = procStartInfo
            };
            proc.Start();
            proc.WaitForExit();
        }


        private const int BUFFER_SIZE = 32000;
        static readonly ASCIIEncoding encoding = new ASCIIEncoding();
        //public void ElnecComuncationBackgroudSite1()
        //{

        //IPAddress address = IPAddress.Parse(_CONFIG.RemoteIP);
        //TcpListener listener = new TcpListener(address, _CONFIG.RemotePort);
        //    listener.Start();
        //    while (true)
        //    {
        //        Site1IsTalking = true;
        //        Socket socket = listener.AcceptSocket();
        //        socket.ReceiveTimeout = Site1.TCP_TimeOut;
        //        var stream = new NetworkStream(socket);
        //        var reader = new StreamReader(stream);
        //        var writer = new StreamWriter(stream);
        //        writer.AutoFlush = false;
        //        while (true)
        //        {
        //            if (Site1.WorkProcess.Process == WorkProcess.Ready)
        //            {
        //                if (ServerStatus == SERVER_OFF)
        //                    break;
        //                Site1.Command = Site1.WorkProcess.GetCommandFIFO();
        //                if (Site1.Command != "null")
        //                {
        //                    writer.WriteLine(Site1.Command);
        //                    try
        //                    {
        //                        writer.Flush();
        //                        if (Site1.Command != ElnecSite.GET_PRG_STATUS)
        //                        {
        //                            tbLog.Invoke(new MethodInvoker(delegate
        //                            {
        //                                if (tbLog.TextLength > 1000000) tbLog.Clear();
        //                                tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site1: " + Site1.Command + System.Environment.NewLine);
        //                            }));

        //                        }
        //                    }
        //                    catch (Exception err)
        //                    {
        //                        Site1.WorkProcess.PushComandToFist(Site1.Command);
        //                        if (!IsFileLocked(GetFileInfo))
        //                        {
        //                            File.AppendAllText(@"D:\A-MS_log_" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt", DateTime.Now.ToString("HH-mm-ss: ") + "Site 1 comunication - " + err.Message + Environment.NewLine);
        //                        }
        //                    }
        //                }
        //                // 2. receive
        //                string recive = "";

        //                while (stream.DataAvailable)
        //                {
        //                    recive += reader.ReadLine();
        //                }
        //                if (recive != null && recive.Length > 2)
        //                {
        //                    if (recive.Contains("\\*/n\\*/"))
        //                    {
        //                        recive = recive.Replace("\\*/n\\*/", System.Environment.NewLine);
        //                    }
        //                    if (tbLog.InvokeRequired)
        //                    {
        //                        tbLog.Invoke(new MethodInvoker(delegate
        //                        {
        //                            ProcessSite(Site1, lbSiteName1, lbSite1Checksum, lbROM1checkSum, lbRomNameSite1, lbResultA, lbResultAbig, recive, lbManualROM1, progressBarSite1);
        //                        }));
        //                    }
        //                    else
        //                    {
        //                        ProcessSite(Site1, lbSiteName1, lbSite1Checksum, lbROM1checkSum, lbRomNameSite1, lbResultA, lbResultAbig, recive, lbManualROM1, progressBarSite1);
        //                    }

        //                }
        //            }

        //        }
        //        socket.Close();
        //        Site1IsTalking = false;
        //    }
        //}
        //public void ElnecComuncationBackgroudSite2()
        //{
        //    IPAddress address = IPAddress.Parse(_CONFIG.RemoteIP);
        //    TcpListener listener = new TcpListener(address, _CONFIG.RemotePort + 1);
        //    listener.Start();
        //    // 1. listen
        //    while (true)
        //    {
        //        Site2IsTalking = true;
        //        Socket socket = listener.AcceptSocket();
        //        socket.ReceiveTimeout = Site2.TCP_TimeOut;
        //        var stream = new NetworkStream(socket);
        //        var reader = new StreamReader(stream);
        //        var writer = new StreamWriter(stream);
        //        writer.AutoFlush = false;
        //        while (true)
        //        {
        //            if (ServerStatus == SERVER_OFF)
        //                break;
        //            //send
        //            Site2.Command = Site2.WorkProcess.GetCommandFIFO();
        //            if (Site2.Command != "null")
        //            {
        //                writer.WriteLine(Site2.Command);
        //                try
        //                {
        //                    writer.Flush();
        //                    if (Site2.Command != ElnecSite.GET_PRG_STATUS)
        //                    {
        //                        tbLog.Invoke(new MethodInvoker(delegate
        //                    {
        //                        if (tbLog.TextLength > 1000000) tbLog.Clear();
        //                        tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site2: " + Site2.Command + System.Environment.NewLine);
        //                    }));
        //                    }
        //                }
        //                catch (Exception err)
        //                {
        //                    Site2.WorkProcess.PushComandToFist(Site2.Command);
        //                    if (!IsFileLocked(GetFileInfo))
        //                    {
        //                        File.AppendAllText(@"D:\A-MS_log_" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt", DateTime.Now.ToString("HH-mm-ss: ") + "Site 2 comunication - " + err.Message + Environment.NewLine);
        //                    }
        //                }

        //            }
        //            //receive
        //            string recive = "";

        //            while (stream.DataAvailable)
        //            {
        //                recive += reader.ReadLine();
        //            }
        //            if (recive != null && recive.Length > 2)
        //            {
        //                if (recive.Contains("\\*/n\\*/"))
        //                {
        //                    recive = recive.Replace("\\*/n\\*/", System.Environment.NewLine);
        //                }
        //                if (tbLog.InvokeRequired)
        //                {
        //                    tbLog.Invoke(new MethodInvoker(delegate
        //                    {
        //                        ProcessSite(Site2, lbSiteName2, lbSite2Checksum, lbROM2checkSum, lbRomNameSite2, lbResultB, lbResultBbig, recive, lbManualROM2, progressBarSite2);
        //                    }));
        //                }
        //                else
        //                {
        //                    ProcessSite(Site2, lbSiteName2, lbSite2Checksum, lbROM2checkSum, lbRomNameSite2, lbResultB, lbResultBbig, recive, lbManualROM2, progressBarSite2);
        //                }
        //            }
        //        }
        //        socket.Close();
        //        Site2IsTalking = false;
        //    }
        //}
        //public void ElnecComuncationBackgroudSite3()
        //{
        //    IPAddress address = IPAddress.Parse(_CONFIG.RemoteIP);
        //    TcpListener listener = new TcpListener(address, _CONFIG.RemotePort + 2);
        //    // 1. listen
        //    listener.Start();
        //    while (true)
        //    {
        //        Site3IsTalking = true;
        //        Socket socket = listener.AcceptSocket();
        //        socket.ReceiveTimeout = Site3.TCP_TimeOut;
        //        var stream = new NetworkStream(socket);
        //        var reader = new StreamReader(stream);
        //        var writer = new StreamWriter(stream)
        //        {
        //            AutoFlush = false
        //        };
        //        while (true)
        //        {
        //            if (ServerStatus == SERVER_OFF)
        //                break;
        //            Site3.Command = Site3.WorkProcess.GetCommandFIFO();
        //            if (Site3.Command != "null")
        //            {
        //                writer.WriteLine(Site3.Command);
        //                try
        //                {
        //                    writer.Flush();
        //                    if (Site3.Command != ElnecSite.GET_PRG_STATUS)
        //                    {
        //                        tbLog.Invoke(new MethodInvoker(delegate
        //                    {
        //                        if (tbLog.TextLength > 1000000) tbLog.Clear();
        //                        tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site3: " + Site3.Command + System.Environment.NewLine);
        //                    }));
        //                    }
        //                }
        //                catch (Exception err)
        //                {
        //                    Site3.WorkProcess.PushComandToFist(Site3.Command);
        //                    if (!IsFileLocked(GetFileInfo))
        //                    {
        //                        File.AppendAllText(@"D:\A-MS_log_" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt", DateTime.Now.ToString("HH-mm-ss: ") + "Site 3 comunication - " + err.Message + Environment.NewLine);
        //                    }
        //                }

        //            }
        //            // 2. receive
        //            string recive = "";

        //            while (stream.DataAvailable)
        //            {
        //                recive += reader.ReadLine();
        //            }
        //            if (recive != null && recive.Length > 2)
        //            {
        //                if (recive.Contains("\\*/n\\*/"))
        //                {
        //                    recive = recive.Replace("\\*/n\\*/", System.Environment.NewLine);
        //                }
        //                if (tbLog.InvokeRequired)
        //                {
        //                    tbLog.Invoke(new MethodInvoker(delegate
        //                    {
        //                        ProcessSite(Site3, lbSiteName3, lbSite3Checksum, lbROM3checkSum, lbRomNameSite3, lbResultC, lbResultCbig, recive, lbManualROM3, progressBarSite3);
        //                    }));
        //                }
        //                else
        //                {
        //                    ProcessSite(Site3, lbSiteName3, lbSite3Checksum, lbROM3checkSum, lbRomNameSite3, lbResultC, lbResultCbig, recive, lbManualROM3, progressBarSite3);

        //                }
        //            }

        //        }
        //        socket.Close();
        //        Site3IsTalking = false;
        //    }
        //}
        //public void ElnecComuncationBackgroudSite4()
        //{
        //    IPAddress address = IPAddress.Parse(_CONFIG.RemoteIP);
        //    TcpListener listener = new TcpListener(address, _CONFIG.RemotePort + 3);
        //    listener.Start();

        //    while (true)
        //    {
        //        Site4IsTalking = true;
        //        Socket socket = listener.AcceptSocket();
        //        socket.ReceiveTimeout = Site4.TCP_TimeOut;
        //        var stream = new NetworkStream(socket);
        //        var reader = new StreamReader(stream);
        //        var writer = new StreamWriter(stream)
        //        {
        //            AutoFlush = false
        //        };
        //        while (true)
        //        {
        //            if (ServerStatus == SERVER_OFF)
        //                break;
        //            Site4.Command = Site4.WorkProcess.GetCommandFIFO();
        //            if (Site4.Command != "null")
        //            {
        //                writer.WriteLine(Site4.Command);
        //                try
        //                {
        //                    writer.Flush();
        //                    if (Site4.Command != ElnecSite.GET_PRG_STATUS)
        //                    {
        //                        tbLog.Invoke(new MethodInvoker(delegate
        //                            {
        //                                if (tbLog.TextLength > 1000000) tbLog.Clear();
        //                                tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site4: " + Site4.Command + System.Environment.NewLine);
        //                            }));
        //                    }
        //                }
        //                catch (Exception err)
        //                {
        //                    Site4.WorkProcess.PushComandToFist(Site4.Command);
        //                    if (!IsFileLocked(GetFileInfo))
        //                    {
        //                        File.AppendAllText(@"D:\A-MS_log_" + DateTime.Now.ToString("dd-MM-yyyy") + ".txt", DateTime.Now.ToString("HH-mm-ss: ") + "Site 4 comunication - " + err.Message + Environment.NewLine);
        //                    }
        //                }
        //            }
        //            // 2. receive
        //            string recive = "";

        //            while (stream.DataAvailable)
        //            {
        //                recive += reader.ReadLine();
        //            }
        //            if (recive != null && recive.Length > 2)
        //            {
        //                if (recive.Contains("\\*/n\\*/"))
        //                {
        //                    recive = recive.Replace("\\*/n\\*/", System.Environment.NewLine);
        //                }
        //                if (tbLog.InvokeRequired)
        //                {
        //                    tbLog.Invoke(new MethodInvoker(delegate
        //                    {
        //                        ProcessSite(Site4, lbSiteName4, lbSite4Checksum, lbROM4checkSum, lbRomNameSite4, lbResultD, lbResultDbig, recive, lbManualROM4, progressBarSite4);
        //                    }));
        //                }
        //                else
        //                {
        //                    ProcessSite(Site4, lbSiteName4, lbSite4Checksum, lbROM4checkSum, lbRomNameSite4, lbResultD, lbResultDbig, recive, lbManualROM4, progressBarSite4);

        //                }
        //            }
        //        }
        //        socket.Close();
        //        Site4IsTalking = false;
        //    }
        //}



        public void ElnecComuncationBackgroudSite1()
        {

            IPAddress address = IPAddress.Parse(_CONFIG.RemoteIP);
            TcpListener listener = new TcpListener(address, _CONFIG.RemotePort);
            listener.Start();
            while (true)
            {
                Site1IsTalking = true;
                Socket socket = listener.AcceptSocket();
                socket.ReceiveTimeout = Site1.TCP_TimeOut;
                var stream = new NetworkStream(socket);
                var reader = new StreamReader(stream);
                var writer = new StreamWriter(stream);
                writer.AutoFlush = false;
                while (true)
                {
                    if (ServerStatus == SERVER_OFF)
                        break;
                    Site1.Command = Site1.WorkProcess.GetCommandFIFO();
                    if (Site1.Command != "null")
                    {
                        writer.WriteLine(Site1.Command);
                        try
                        {
                            writer.Flush();
                        }
                        catch (Exception)
                        {
                            Site1.WorkProcess.PushComandToFist(Site1.Command);
                            break;
                        }
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 1000000) tbLog.Clear();
                            tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site1: " + Site1.Command + System.Environment.NewLine);
                        }));
                    }
                    // 2. receive
                    string recive = "";
                    try
                    {
                        recive = reader.ReadLine();
                    }
                    catch (Exception e) { Console.Write(' '); }
                    if (recive != null && recive.Length > 2)
                    {
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 100000) tbLog.Clear();
                            tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + recive.Replace("\\*/n\\*/", System.Environment.NewLine) + System.Environment.NewLine);
                            ProcessSite(Site1, lbSiteName1, lbSite1Checksum, lbROM1checkSum, lbRomNameSite1, lbResultA, lbResultAbig, recive.Replace("\\*/n\\*/", System.Environment.NewLine), lbManualROM1, progressBarSite1);
                        }));
                    }
                }
                socket.Close();
                Site1IsTalking = false;
            }
        }
        public void ElnecComuncationBackgroudSite2()
        {
            IPAddress address = IPAddress.Parse(_CONFIG.RemoteIP);
            TcpListener listener = new TcpListener(address, _CONFIG.RemotePort + 1);
            listener.Start();
            // 1. listen
            while (true)
            {
                Site2IsTalking = true;
                Socket socket = listener.AcceptSocket();
                socket.ReceiveTimeout = Site2.TCP_TimeOut;
                var stream = new NetworkStream(socket);
                var reader = new StreamReader(stream);
                var writer = new StreamWriter(stream);
                writer.AutoFlush = false;
                while (true)
                {
                    if (ServerStatus == SERVER_OFF)
                        break;
                    //send
                    Site2.Command = Site2.WorkProcess.GetCommandFIFO();
                    if (Site2.Command != "null")
                    {
                        writer.WriteLine(Site2.Command);
                        try
                        {
                            writer.Flush();
                        }
                        catch (Exception)
                        {
                            Site2.WorkProcess.PushComandToFist(Site2.Command);
                            break;
                        }
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 1000000) tbLog.Clear();
                            tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site2: " + Site2.Command + System.Environment.NewLine);
                        }));
                    }
                    //receive
                    string recive = "";
                    try
                    {
                        recive = reader.ReadLine();
                    }
                    catch (Exception e) { Console.Write(' '); }
                    if (recive != null && recive.Length > 2)
                    {
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 100000) tbLog.Clear();
                            tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + recive.Replace("\\*/n\\*/", System.Environment.NewLine) + System.Environment.NewLine);
                            ProcessSite(Site2, lbSiteName2, lbSite2Checksum, lbROM2checkSum, lbRomNameSite2, lbResultB, lbResultBbig, recive.Replace("\\*/n\\*/", System.Environment.NewLine), lbManualROM2, progressBarSite2);

                        }));
                    }
                }
                socket.Close();
                Site2IsTalking = false;
            }
        }
        public void ElnecComuncationBackgroudSite3()
        {
            IPAddress address = IPAddress.Parse(_CONFIG.RemoteIP);
            TcpListener listener = new TcpListener(address, _CONFIG.RemotePort + 2);
            // 1. listen
            listener.Start();
            while (true)
            {
                Site3IsTalking = true;
                Socket socket = listener.AcceptSocket();
                socket.ReceiveTimeout = Site3.TCP_TimeOut;
                var stream = new NetworkStream(socket);
                var reader = new StreamReader(stream);
                var writer = new StreamWriter(stream);
                writer.AutoFlush = false;
                while (true)
                {
                    if (ServerStatus == SERVER_OFF)
                        break;
                    Site3.Command = Site3.WorkProcess.GetCommandFIFO();
                    if (Site3.Command != "null")
                    {
                        writer.WriteLine(Site3.Command);
                        try
                        {
                            writer.Flush();
                        }
                        catch (Exception)
                        {
                            Site3.WorkProcess.PushComandToFist(Site3.Command);
                            break;
                        }
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 1000000) tbLog.Clear();
                            tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site3: " + Site3.Command + System.Environment.NewLine);
                        }));
                    }
                    // 2. receive
                    string recive = "";
                    try
                    {
                        recive = reader.ReadLine();
                    }
                    catch (Exception e) { Console.Write(' '); }
                    if (recive != null && recive.Length > 2)
                    {
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 100000) tbLog.Clear();
                            tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + recive.Replace("\\*/n\\*/", System.Environment.NewLine) + System.Environment.NewLine);
                            ProcessSite(Site3, lbSiteName3, lbSite3Checksum, lbROM3checkSum, lbRomNameSite3, lbResultC, lbResultCbig, recive.Replace("\\*/n\\*/", System.Environment.NewLine), lbManualROM3, progressBarSite3);

                        }));
                    }
                }
                socket.Close();
                Site3IsTalking = false;
            }
        }
        public void ElnecComuncationBackgroudSite4()
        {
            IPAddress address = IPAddress.Parse(_CONFIG.RemoteIP);
            TcpListener listener = new TcpListener(address, _CONFIG.RemotePort + 3);
            // 1. listen
            listener.Start();

            while (true)
            {
                Site4IsTalking = true;
                Socket socket = listener.AcceptSocket();
                socket.ReceiveTimeout = Site4.TCP_TimeOut;
                var stream = new NetworkStream(socket);
                var reader = new StreamReader(stream);
                var writer = new StreamWriter(stream);
                writer.AutoFlush = false;
                while (true)
                {
                    if (ServerStatus == SERVER_OFF)
                        break;
                    Site4.Command = Site4.WorkProcess.GetCommandFIFO();
                    if (Site4.Command != "null")
                    {
                        writer.WriteLine(Site4.Command);
                        try
                        {
                            writer.Flush();
                        }
                        catch (Exception)
                        {
                            Site4.WorkProcess.PushComandToFist(Site4.Command);
                            break;
                        }
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 1000000) tbLog.Clear();
                            tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site4: " + Site4.Command + System.Environment.NewLine);
                        }));
                    }
                    // 2. receive
                    string recive = "";
                    try
                    {
                        recive = reader.ReadLine();
                    }
                    catch (Exception e) { Console.Write(' '); }
                    if (recive != null && recive.Length > 2)
                    {
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 100000) tbLog.Clear();
                            tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + recive.Replace("\\*/n\\*/", System.Environment.NewLine) + System.Environment.NewLine);
                            ProcessSite(Site4, lbSiteName4, lbSite4Checksum, lbROM4checkSum, lbRomNameSite4, lbResultD, lbResultDbig, recive.Replace("\\*/n\\*/", System.Environment.NewLine), lbManualROM4, progressBarSite4);
                        }));
                    }
                }
                socket.Close();
                Site4IsTalking = false;
            }
        }


        public void ProcessSite(ElnecSite Site, Label lbSiteName, Label lbSiteChecksum, Label lbROMcheckSum, Label lbRomNameSite, Label lbResult, Label lbResultBig, string Response, Label lbROMmanual, ToolStripProgressBar progressBarSite)
        {
            // get infor from site 
            string[] ElnecResponses = Response.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

            for (int function = 0; function < ElnecResponses.Length; function++)
            {
                string ElnecResponse = ElnecResponses[function];
                if (ElnecResponse.Contains("Programming device:"))
                {
                    highlinedgwTestMode(1);
                }
                if (ElnecResponse.StartsWith("cindex:"))
                {
                    ElnecResponse = ElnecResponse.Remove(0, 9);
                }


                string[] ElnecResponse_data = ElnecResponse.Split(' ');
                for (int i = 0; i < ElnecResponse_data.Length; i++)
                {
                    string[] data = ElnecResponse_data[i].Split(':');
                    switch (data[0])
                    {
                        case "CreditBoxDeviceCreditDecrementValue":
                            {
                                Site.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                                break;
                            }
                        case ElnecSite.PROG_IS_BUSY:
                            {
                                lbSiteName.BackColor = busyColor;
                                break;
                            }
                        case ElnecSite.GETDEVCHECKSUM_RESULT:
                            {
                                lbSiteChecksum.Invoke(new MethodInvoker(delegate { lbSiteChecksum.Text = data[1]; }));
                                break;
                            }
                        case ElnecSite.GETDEVCHECKSUM_REQ_RESULT:
                            {
                                lbSiteChecksum.Invoke(new MethodInvoker(delegate { lbSiteChecksum.Text = data[1]; }));
                                break;
                            }
                        case ElnecSite.CLIENT_READY_ANSWER + ElnecSite.KEY_CLIENT_READY_YES:
                            {
                                lbSiteName.BackColor = activeColor;
                                break;
                            }
                        case ElnecSite.CLIENT_READY_ANSWER + ElnecSite.KEY_CLIENT_READY_NO:
                            {
                                lbSiteName.BackColor = Color.Black;
                                break;
                            }
                        case ElnecSite.PROGRAMMER_READY_STATUS:
                            {
                                if (data[1] == ElnecSite.KEY_PROGRAMMER_READY)
                                {
                                    lbSiteName.BackColor = activeColor;
                                }
                                else if (data[1] == ElnecSite.KEY_PROGRAMMER_NOTFOUND)
                                {
                                    lbSiteName.BackColor = Color.Black;
                                    lbROMmanual.BackColor = Color.Black;
                                }
                                break;
                            }
                        case ElnecSite.LOAD_FILE_PRJ_RESULT:
                            {
                                Site.SITE_LOADPRJRESULT = data[1];
                                break;
                            }
                        case ElnecSite.DETAILED_OPRESULT:
                            {
                                Site.SITE_DETAILE = data[1];
                                break;
                            }
                        case ElnecSite.PROGRESS:
                            {
                                Site.SITE_PROGRESS = data[1];
                                break;
                            }
                        case ElnecSite.OPRESULT:
                            {
                                Site.SITE_OPRESULT = data[1];
                                break;
                            }
                        case ElnecSite.OPTYPE:
                            {
                                Site.SITE_OPTYPE = data[1];
                                break;
                            }
                        case "Programmer":
                            {
                                if (ElnecResponse_data[i + 1] == "connection" && ElnecResponse_data[i + 2] == "closed.")
                                {
                                    lbSiteName.BackColor = Color.Black;
                                }
                                break;
                            }

                    }
                }
                // processing data
                if (startTest)
                {
                    if (Site.SITE_OPTYPE == "5" && Site.Result == ElnecSite.EMPTY)
                    {
                        Site.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                        if (Site.SITE_PROGRESS != "")
                        {
                            int a = Convert.ToInt32(Site.SITE_PROGRESS);
                            if (a > 0)
                            {
                                progressBarSite.Value = a;
                                if (Site.progressValue < a)
                                    Site.progressValue = a;
                                else
                                    Site.progressValue = 100 + a;
                            }
                        }
                    }
                }

                if (tbLog.TextLength > 100000) tbLog.Clear();
                tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + Response + System.Environment.NewLine);

                if (Site.SITE_OPTYPE == "10")
                {
                    Site.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                    Site.SITE_OPTYPE = "";
                }

                if (Site.SITE_LOADPRJRESULT == ElnecSite.FILE_LOAD_GOOD)
                {
                    OK_label(lbRomNameSite);
                    progressBarSite.Value = 100;
                    if (lbROMcheckSum.Text == lbSiteChecksum.Text)
                    {
                        lbROMcheckSum.BackColor = Color.Green;
                        lbSiteChecksum.BackColor = Color.Green;
                    }
                    else
                    {
                        if (lbSiteChecksum.BackColor != Color.Red)
                            tbHistory.Invoke(new MethodInvoker(delegate { tbHistory.AppendText(Environment.NewLine + "Site " + Site.Name + ": Checksum did not match." + Environment.NewLine); }));
                        lbROMcheckSum.BackColor = Color.Red;
                        lbSiteChecksum.BackColor = Color.Red;
                    }
                    Site.SITE_LOADPRJRESULT = "";
                    Site.WorkProcess.PushComandToFist(ElnecSite.GETDEVCHECKSUM);
                }

                if (Site.SITE_LOADPRJRESULT == ElnecSite.FILE_LOAD_ERROR)
                {
                    NG_label(lbRomNameSite);
                    progressBarSite.BackColor = Color.Black;
                    Site.SITE_LOADPRJRESULT = "";
                    Site.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                }

                if (!Timeout)
                {
                    if (Site.SITE_DETAILE == "1")
                    {
                        if (Site.SITE_PROGRAMRESULT != ElnecSite.RESULT_OK)
                        {
                            Site.SITE_PROGRAMRESULT = ElnecSite.RESULT_OK;
                            Site.Result = ElnecSite.RESULT_OK;
                        }
                        OK_label(lbResult);
                        OK_label(lbResultBig);
                        Site.progressValue = 200;
                        Site.WorkProcess.ClearCMDQueue();
                        Site.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                        Site.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                        Site.SITE_DETAILE = "";
                    }
                    else if (Site.SITE_DETAILE != "")
                    {
                        if (Site.SITE_PROGRAMRESULT != ElnecSite.RESULT_NG)
                        {
                            Site.SITE_PROGRAMRESULT = ElnecSite.RESULT_NG;
                            Site.Result = ElnecSite.RESULT_NG;
                        }
                        NG_label(lbResult);
                        NG_label(lbResultBig);
                        Site.progressValue = 200;
                        Site.WorkProcess.ClearCMDQueue();
                        Site.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                        Site.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                        Site.SITE_DETAILE = "";
                    }
                }
                else
                {
                    Site.SITE_DETAILE = "";
                }

                if (lbAutoManual.Text == "Auto mode")
                {
                    if (startTest == true && endTest == false)
                    {
                        FinalTestLabel();
                    }
                }
            }
            pbTesting.Invoke(new MethodInvoker(delegate
            {
                int a = Site1.progressValue + Site2.progressValue + Site3.progressValue + Site4.progressValue;
                if (a < pbTesting.Maximum)
                    pbTesting.Value = a;
                else
                    pbTesting.Value = pbTesting.Maximum;
                //Console.WriteLine("Site 1: {0}  Site 2: {1}  Site 3: {2}  Site 4: {3}",Site1.progressValue, Site2.progressValue, Site3.progressValue, Site4.progressValue);
            }));

        }
        private void tableLayoutPanel7_Paint(object sender, PaintEventArgs e)
        {

        }
        private void lbROMsellected_Click(object sender, EventArgs e)
        {
            ROMsellectCounter++;
            switch (ROMsellectCounter)
            {
                case 1:
                    {
                        lbROMsellected.BackColor = fistROMsellect;
                        break;
                    }
                case 2:
                    {
                        lbROMsellected.BackColor = secondROMsellect;
                        break;
                    }
                case 3:
                    {
                        lbROMsellected.BackColor = thirdROMsellect;
                        break;
                    }
                case 4:
                    {
                        ROMsellectCounter = 0;
                        lbROMsellected.BackColor = forthROMsellect;
                        break;
                    }

            }
        }

        private void lbSite1Sellect_Click(object sender, EventArgs e)
        {
            if (ROMsellectCounter != 0 && lbSite1Sellect.BackColor != lbROMsellected.BackColor)
                lbSite1Sellect.BackColor = lbROMsellected.BackColor;
            else
                lbSite1Sellect.BackColor = Color.Black;

        }

        private void lbSite2Sellect_Click(object sender, EventArgs e)
        {
            if (ROMsellectCounter != 0 && lbSite2Sellect.BackColor != lbROMsellected.BackColor)
                lbSite2Sellect.BackColor = lbROMsellected.BackColor;
            else
                lbSite2Sellect.BackColor = Color.Black;
        }

        private void lbSite3Sellect_Click(object sender, EventArgs e)
        {
            if (ROMsellectCounter != 0 && lbSite3Sellect.BackColor != lbROMsellected.BackColor)
                lbSite3Sellect.BackColor = lbROMsellected.BackColor;
            else
                lbSite3Sellect.BackColor = Color.Black;
        }

        private void lbSite4Sellect_Click(object sender, EventArgs e)
        {
            if (ROMsellectCounter != 0 && lbSite4Sellect.BackColor != lbROMsellected.BackColor)
                lbSite4Sellect.BackColor = lbROMsellected.BackColor;
            else
                lbSite4Sellect.BackColor = Color.Black;
        }



        private void lbROM1checkSum_TextChanged(object sender, EventArgs e)
        {
            model.ROMs[0].ROM_CHECKSUM = lbROM1checkSum.Text;
        }

        private void lbROM2checkSum_TextChanged(object sender, EventArgs e)
        {
            model.ROMs[1].ROM_CHECKSUM = lbROM2checkSum.Text;
        }

        private void lbROM3checkSum_TextChanged(object sender, EventArgs e)
        {
            model.ROMs[2].ROM_CHECKSUM = lbROM3checkSum.Text;
        }

        private void lbROM4checkSum_TextChanged(object sender, EventArgs e)
        {
            model.ROMs[3].ROM_CHECKSUM = lbROM4checkSum.Text;
        }

        private void OpenFileDialogSite1_FileOk(object sender, CancelEventArgs e)
        {
            string path = Path.GetDirectoryName(openFileDialogSite1.FileName);
            _CONFIG.recentWorkPath = path;

            lbStRomNameSite1.Text = Path.GetFileNameWithoutExtension(openFileDialogSite1.FileName);
            model.ROMs[0].ROM_PATH = Path.GetDirectoryName(openFileDialogSite1.FileName) + "\\" + Path.GetFileName(openFileDialogSite1.FileName);
            model.ROMs[0].ROM_CHECKSUM = lbStRomNameSite1.Text;

            if (lbSite2Sellect.BackColor == lbSite1Sellect.BackColor)
            {
                lbStRomNameSite2.Text = Path.GetFileNameWithoutExtension(openFileDialogSite1.FileName);
                model.ROMs[1].ROM_PATH = Path.GetDirectoryName(openFileDialogSite1.FileName) + "\\" + Path.GetFileName(openFileDialogSite1.FileName);
                model.ROMs[1].ROM_CHECKSUM = lbStRomNameSite2.Text;
            }
            if (lbSite3Sellect.BackColor == lbSite1Sellect.BackColor)
            {
                lbStRomNameSite3.Text = Path.GetFileNameWithoutExtension(openFileDialogSite1.FileName);
                model.ROMs[2].ROM_PATH = Path.GetDirectoryName(openFileDialogSite1.FileName) + "\\" + Path.GetFileName(openFileDialogSite1.FileName);
                model.ROMs[2].ROM_CHECKSUM = lbStRomNameSite3.Text;
            }
            if (lbSite4Sellect.BackColor == lbSite1Sellect.BackColor)
            {
                lbStRomNameSite4.Text = Path.GetFileNameWithoutExtension(openFileDialogSite1.FileName);
                model.ROMs[3].ROM_PATH = Path.GetDirectoryName(openFileDialogSite1.FileName) + "\\" + Path.GetFileName(openFileDialogSite1.FileName);
                model.ROMs[3].ROM_CHECKSUM = lbStRomNameSite4.Text;
            }

        }
        private void openFileDialogSite2_FileOk(object sender, CancelEventArgs e)
        {
            string path = Path.GetDirectoryName(openFileDialogSite2.FileName);
            _CONFIG.recentWorkPath = path;

            lbStRomNameSite2.Text = Path.GetFileNameWithoutExtension(openFileDialogSite2.FileName);
            model.ROMs[1].ROM_PATH = Path.GetDirectoryName(openFileDialogSite2.FileName) + "\\" + Path.GetFileName(openFileDialogSite2.FileName);
            model.ROMs[1].ROM_CHECKSUM = lbStRomNameSite2.Text;

            if (lbSite1Sellect.BackColor == lbSite2Sellect.BackColor)
            {
                lbStRomNameSite1.Text = Path.GetFileNameWithoutExtension(openFileDialogSite2.FileName);
                model.ROMs[0].ROM_PATH = Path.GetDirectoryName(openFileDialogSite2.FileName) + "\\" + Path.GetFileName(openFileDialogSite2.FileName);
                model.ROMs[0].ROM_CHECKSUM = lbStRomNameSite1.Text;
            }
            if (lbSite3Sellect.BackColor == lbSite2Sellect.BackColor)
            {
                lbStRomNameSite3.Text = Path.GetFileNameWithoutExtension(openFileDialogSite2.FileName);
                model.ROMs[2].ROM_PATH = Path.GetDirectoryName(openFileDialogSite2.FileName) + "\\" + Path.GetFileName(openFileDialogSite2.FileName);
                model.ROMs[2].ROM_CHECKSUM = lbStRomNameSite3.Text;
            }
            if (lbSite4Sellect.BackColor == lbSite2Sellect.BackColor)
            {
                lbStRomNameSite4.Text = Path.GetFileNameWithoutExtension(openFileDialogSite2.FileName);
                model.ROMs[3].ROM_PATH = Path.GetDirectoryName(openFileDialogSite2.FileName) + "\\" + Path.GetFileName(openFileDialogSite2.FileName);
                model.ROMs[3].ROM_CHECKSUM = lbStRomNameSite4.Text;
            }
        }
        private void openFileDialogSite3_FileOk(object sender, CancelEventArgs e)
        {
            string path = Path.GetDirectoryName(openFileDialogSite3.FileName);
            _CONFIG.recentWorkPath = path;

            lbStRomNameSite3.Text = Path.GetFileNameWithoutExtension(openFileDialogSite3.FileName);
            model.ROMs[2].ROM_PATH = Path.GetDirectoryName(openFileDialogSite3.FileName) + "\\" + Path.GetFileName(openFileDialogSite3.FileName);
            model.ROMs[2].ROM_CHECKSUM = lbStRomNameSite3.Text;

            if (lbSite1Sellect.BackColor == lbSite3Sellect.BackColor)
            {
                lbStRomNameSite1.Text = Path.GetFileNameWithoutExtension(openFileDialogSite3.FileName);
                model.ROMs[0].ROM_PATH = Path.GetDirectoryName(openFileDialogSite3.FileName) + "\\" + Path.GetFileName(openFileDialogSite3.FileName);
                model.ROMs[0].ROM_CHECKSUM = lbStRomNameSite1.Text;
            }
            if (lbSite2Sellect.BackColor == lbSite3Sellect.BackColor)
            {
                lbStRomNameSite2.Text = Path.GetFileNameWithoutExtension(openFileDialogSite3.FileName);
                model.ROMs[1].ROM_PATH = Path.GetDirectoryName(openFileDialogSite3.FileName) + "\\" + Path.GetFileName(openFileDialogSite3.FileName);
                model.ROMs[1].ROM_CHECKSUM = lbStRomNameSite2.Text;
            }
            if (lbSite4Sellect.BackColor == lbSite3Sellect.BackColor)
            {
                lbStRomNameSite4.Text = Path.GetFileNameWithoutExtension(openFileDialogSite3.FileName);
                model.ROMs[3].ROM_PATH = Path.GetDirectoryName(openFileDialogSite3.FileName) + "\\" + Path.GetFileName(openFileDialogSite3.FileName);
                model.ROMs[3].ROM_CHECKSUM = lbStRomNameSite4.Text;
            }
        }

        private void openFileDialogSite4_FileOk(object sender, CancelEventArgs e)
        {
            string path = Path.GetDirectoryName(openFileDialogSite4.FileName);
            _CONFIG.recentWorkPath = path;
            string[] paths = path.Split('\\');

            lbStRomNameSite4.Text = Path.GetFileNameWithoutExtension(openFileDialogSite4.FileName);
            model.ROMs[3].ROM_PATH = Path.GetDirectoryName(openFileDialogSite4.FileName) + "\\" + Path.GetFileName(openFileDialogSite4.FileName);
            model.ROMs[3].ROM_CHECKSUM = lbStRomNameSite4.Text;

            if (lbSite1Sellect.BackColor == lbSite4Sellect.BackColor)
            {
                lbStRomNameSite1.Text = Path.GetFileNameWithoutExtension(openFileDialogSite4.FileName);
                model.ROMs[0].ROM_PATH = Path.GetDirectoryName(openFileDialogSite4.FileName) + "\\" + Path.GetFileName(openFileDialogSite4.FileName);
                model.ROMs[0].ROM_CHECKSUM = lbStRomNameSite1.Text;
            }
            if (lbSite2Sellect.BackColor == lbSite4Sellect.BackColor)
            {
                lbStRomNameSite2.Text = Path.GetFileNameWithoutExtension(openFileDialogSite4.FileName);
                model.ROMs[1].ROM_PATH = Path.GetDirectoryName(openFileDialogSite4.FileName) + "\\" + Path.GetFileName(openFileDialogSite4.FileName);
                model.ROMs[1].ROM_CHECKSUM = lbStRomNameSite2.Text;
            }
            if (lbSite3Sellect.BackColor == lbSite4Sellect.BackColor)
            {
                lbStRomNameSite4.Text = Path.GetFileNameWithoutExtension(openFileDialogSite4.FileName);
                model.ROMs[2].ROM_PATH = Path.GetDirectoryName(openFileDialogSite4.FileName) + "\\" + Path.GetFileName(openFileDialogSite4.FileName);
                model.ROMs[2].ROM_CHECKSUM = lbStRomNameSite3.Text;
            }
        }
        private void Main_MouseMove(object sender, MouseEventArgs e)
        {
            btNameReview.Text = "Technical Team";
        }

        private void btLoadModel_MouseMove(object sender, MouseEventArgs e)
        {
            btNameReview.Text = "Load model";
        }
        private void btLoadModel_MouseLeave(object sender, EventArgs e)
        {
            btNameReview.Text = "Technical Team";
        }
        private void lbModelName_MouseLeave(object sender, EventArgs e)
        {
            btNameReview.Text = "Technical Team";
        }
        private void lbModelName_MouseMove(object sender, MouseEventArgs e)
        {
            btNameReview.Text = "Model name";
        }
        private void btAuto_MouseLeave(object sender, EventArgs e)
        {
            btNameReview.Text = "Technical Team";
        }
        private void btAuto_MouseMove(object sender, MouseEventArgs e)
        {
            btNameReview.Text = "Auto";
        }
        private void btManual_MouseLeave(object sender, EventArgs e)
        {
            btNameReview.Text = "Technical Team";
        }
        private void btManual_MouseMove(object sender, MouseEventArgs e)
        {
            btNameReview.Text = "Manual";
        }
        private void btSWUser_MouseMove(object sender, MouseEventArgs e)
        {
            btNameReview.Text = "Switch User";
        }

        private void btSWUser_MouseLeave(object sender, EventArgs e)
        {
            btNameReview.Text = "Technical Team";
        }
        private void btReportFolder_MouseLeave(object sender, EventArgs e)
        {
            btNameReview.Text = "Technical Team";
        }

        private void btReportFolder_MouseMove(object sender, MouseEventArgs e)
        {
            btNameReview.Text = "Open Report Manage";
        }

        private void btSetting_MouseLeave(object sender, EventArgs e)
        {
            btNameReview.Text = "Technical Team";
        }

        private void btSetting_MouseMove(object sender, MouseEventArgs e)
        {
            btNameReview.Text = "Setting";
        }

        private void btDataLog_MouseLeave(object sender, EventArgs e)
        {
            btNameReview.Text = "Technical Team";
        }

        private void btDataLog_MouseMove(object sender, MouseEventArgs e)
        {
            btNameReview.Text = "Save current setting";
        }

        DateTime startLoad = DateTime.Now;
        private void openFileModel_FileOk(object sender, CancelEventArgs e)
        {
            _CONFIG.recentModelPath = Path.GetDirectoryName(openFileModel.FileName);
            model.ModelPath = _CONFIG.recentModelPath;
            string[] config;
            try
            {
                startLoad = DateTime.Now;
                config = File.ReadAllLines(openFileModel.FileName);
                
                var fullName = Path.GetFileNameWithoutExtension(openFileModel.FileName);
                model.ModelName = fullName;
                model.Version = fullName.Split('_')[2];
                lbModelName.Text = model.ModelName;

                if (config[5] == "True") model.Layout.PCB1 = true; else if (config[5] == "False") model.Layout.PCB1 = false;
                if (config[6] == "True") model.Layout.PCB2 = true; else if (config[6] == "False") model.Layout.PCB2 = false;

                if (config.Length > 10)
                {
                    if (Convert.ToInt32(config[10]) > 5)
                        model.TimeOut = Convert.ToInt32(config[10]);
                    else
                        model.TimeOut = 30;
                }
                else
                {
                    model.TimeOut = 30;
                }
                timerTimeOut.Interval = model.TimeOut * 1000;

                model.Layout.ArrayCount = Convert.ToInt32(config[7]);
                model.Layout.XasixArrayCount = Convert.ToInt32(config[8]);
                model.Layout.MicomNumber = Convert.ToInt32(config[9]);
                if (_CONFIG.InlineMachine)
                {
                    model.Layout.drawPCBLayout(pbLayout);
                }
                else
                {
                    model.Layout.drawPCBLayout(pbLayout, true);
                }

                tbHistory.AppendText("Load model config: PCB 1: " + model.Layout.PCB1.ToString() + "    PCB 2: " + model.Layout.PCB2.ToString() + "   Micom count: " + model.Layout.MicomNumber.ToString() + "  Program timeout: " + model.TimeOut.ToString() + Environment.NewLine);

                if (Port.IsOpen && model != null)
                {
                    if (_CONFIG.InlineMachine == true) // Micom inline: send mode 1 array or 2 arry to control ler board
                    {
                        if (model.Layout.PCB1 && !model.Layout.PCB2)
                        {
                            Port.Write(Mode_1Array);
                            tbSerialData.AppendText("[TX--] " + Mode_1Array + Environment.NewLine);
                            tbHistory.AppendText("Mode 1 array had set.\r\n");
                        }
                        else if (model.Layout.PCB1 && model.Layout.PCB2)
                        {
                            Port.Write(Mode_2Array);
                            tbSerialData.AppendText("[TX--] " + Mode_2Array + Environment.NewLine);
                            if (Port.IsOpen && model != null)
                            {
                                if (_CONFIG.InlineMachine == true) // Micom inline: send mode 1 array or 2 arry to control ler board
                                {
                                    if (model.Layout.PCB1 && !model.Layout.PCB2)
                                    {
                                        Port.Write(Mode_1Array);
                                        tbSerialData.AppendText("[TX--] " + Mode_1Array + Environment.NewLine);
                                        tbHistory.AppendText("Mode 1 array had set.\r\n");
                                    }
                                    else if (model.Layout.PCB1 && model.Layout.PCB2)
                                    {
                                        Port.Write(Mode_2Array);
                                        tbSerialData.AppendText("[TX--] " + Mode_2Array + Environment.NewLine);
                                        tbHistory.AppendText("Mode 2 array had set.\r\n");
                                    }
                                }
                                else
                                {
                                    string sendCMD = "";
                                    if (model.Layout.ArrayCount == 2 && model.Layout.MicomNumber == 2)
                                    {
                                        sendCMD = command.GetCMDByName("Mode_2PCB");
                                        tbHistory.AppendText("Mode 2 PCB had set.\r\n");
                                    }
                                    else
                                    {
                                        sendCMD = command.GetCMDByName("Mode_4PCB");
                                        tbHistory.AppendText("Mode 4 PCB had set.\r\n");
                                    }
                                    Port.Write(sendCMD);
                                    tbSerialData.AppendText("[TX--] " + sendCMD + Environment.NewLine);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Com not connect, Array setting maybe not apply.");
                            }
                        }
                    }
                    else
                    {
                        string sendCMD = "";
                        if (model.Layout.ArrayCount == 2 && model.Layout.MicomNumber == 2)
                        {
                            sendCMD = command.GetCMDByName("Mode_2PCB");
                        }
                        else
                        {
                            sendCMD = command.GetCMDByName("Mode_4PCB");
                        }
                        Port.Write(sendCMD);
                        tbSerialData.AppendText("[TX--] " + sendCMD + Environment.NewLine);
                    }
                }
                else
                {
                    MessageBox.Show("Com not connect, Array setting maybe not apply.");
                }


                model.ROMs[0].ROM_PATH = config[1].Remove(config[1].IndexOf('@'), config[1].Length - config[1].IndexOf('@'));
                lbRomNameSite1.Text = config[1].Split('\\')[config[1].Split('\\').Length - 1];
                lbROM1checkSum.Text = lbRomNameSite1.Text.Remove(0, lbRomNameSite1.Text.IndexOf('@') + 1);
                model.ROMs[0].ROM_CHECKSUM = lbROM1checkSum.Text;
                lbRomNameSite1.Text = lbRomNameSite1.Text.Remove(lbRomNameSite1.Text.IndexOf('@'), lbRomNameSite1.Text.Length - lbRomNameSite1.Text.IndexOf('@'));

                model.ROMs[1].ROM_PATH = config[2].Remove(config[2].IndexOf('@'), config[2].Length - config[2].IndexOf('@'));
                lbRomNameSite2.Text = config[2].Split('\\')[config[1].Split('\\').Length - 1];
                lbROM2checkSum.Text = lbRomNameSite2.Text.Remove(0, lbRomNameSite2.Text.IndexOf('@') + 1);
                model.ROMs[1].ROM_CHECKSUM = lbROM2checkSum.Text;
                lbRomNameSite2.Text = lbRomNameSite2.Text.Remove(lbRomNameSite2.Text.IndexOf('@'), lbRomNameSite2.Text.Length - lbRomNameSite2.Text.IndexOf('@'));

                model.ROMs[2].ROM_PATH = config[3].Remove(config[3].IndexOf('@'), config[3].Length - config[3].IndexOf('@'));
                lbRomNameSite3.Text = config[3].Split('\\')[config[3].Split('\\').Length - 1];
                lbROM3checkSum.Text = lbRomNameSite3.Text.Remove(0, lbRomNameSite3.Text.IndexOf('@') + 1);
                model.ROMs[2].ROM_CHECKSUM = lbROM3checkSum.Text;
                lbRomNameSite3.Text = lbRomNameSite3.Text.Remove(lbRomNameSite3.Text.IndexOf('@'), lbRomNameSite3.Text.Length - lbRomNameSite3.Text.IndexOf('@'));

                model.ROMs[3].ROM_PATH = config[4].Remove(config[4].IndexOf('@'), config[4].Length - config[4].IndexOf('@'));
                lbRomNameSite4.Text = config[4].Split('\\')[config[4].Split('\\').Length - 1];
                lbROM4checkSum.Text = lbRomNameSite4.Text.Remove(0, lbRomNameSite4.Text.IndexOf('@') + 1);
                model.ROMs[3].ROM_CHECKSUM = lbROM4checkSum.Text;
                lbRomNameSite4.Text = lbRomNameSite4.Text.Remove(lbRomNameSite4.Text.IndexOf('@'), lbRomNameSite4.Text.Length - lbRomNameSite4.Text.IndexOf('@'));

                tbHistory.AppendText("Site 1 ROM file: " + model.ROMs[0].ROM_PATH + Environment.NewLine + Environment.NewLine);
                tbHistory.AppendText("Site 2 ROM file: " + model.ROMs[1].ROM_PATH + Environment.NewLine + Environment.NewLine);
                tbHistory.AppendText("Site 3 ROM file: " + model.ROMs[2].ROM_PATH + Environment.NewLine + Environment.NewLine);
                tbHistory.AppendText("Site 4 ROM file: " + model.ROMs[3].ROM_PATH + Environment.NewLine + Environment.NewLine);

                LoadToSettingPanel();
                if (model.Layout.PCB2 && !_CONFIG.InlineMachine)
                {
                    MessageBox.Show("Warning: This project only use for Inline machine. Layout can't load correct.");
                    tbHistory.ForeColor = Color.Yellow;
                    tbHistory.AppendText("Warning: This project only use for Inline machine. Layout can't load correct.\n");
                    tbHistory.ForeColor = Color.White;
                }
            }
            catch (Exception err)
            {
                tbHistory.AppendText("Have an error when load model: " + err.Message + Environment.NewLine);
            }

            lbRomNameSite1.BackColor = nonactiveColor;
            lbRomNameSite2.BackColor = nonactiveColor;
            lbRomNameSite3.BackColor = nonactiveColor;
            lbRomNameSite4.BackColor = nonactiveColor;

            progressBarSite1.BackColor = Color.FromArgb(80, 80, 80);
            progressBarSite2.BackColor = Color.FromArgb(80, 80, 80);
            progressBarSite3.BackColor = Color.FromArgb(80, 80, 80);
            progressBarSite4.BackColor = Color.FromArgb(80, 80, 80);

            lbSite1Sellect.BackColor = activeColor;
            lbSite2Sellect.BackColor = activeColor;
            lbSite3Sellect.BackColor = activeColor;
            lbSite4Sellect.BackColor = activeColor;

            Site1.WorkProcess.ClearCMDQueue();
            Site2.WorkProcess.ClearCMDQueue();
            Site3.WorkProcess.ClearCMDQueue();
            Site4.WorkProcess.ClearCMDQueue();

            Site1.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
            Site2.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
            Site3.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
            Site4.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);

            lbSVCPmodel.BackColor = nonactiveColor;
            lbSVCPchecksum.BackColor = nonactiveColor;

            if (_CONFIG.ServerCompare)
            {
                matchServer = true;
                int resulCompare = 00;
                Model SVmodel = new Model();
                if (Database.Connect())
                {
                    resulCompare = Database.GetModelLikeThis(model.ModelName.Split('_')[0], model.ROMs[0].ROM_CHECKSUM, SVmodel);
                    Console.WriteLine("connected server" + resulCompare);
                }
                Console.WriteLine(model.ModelName.Split('_')[0]);

                switch (resulCompare)
                {
                    case 00:
                        lbSVCPmodel.BackColor = Color.Red;
                        lbSVCPchecksum.BackColor = Color.Red;
                        tbHistory.AppendText("No have this model...");
                        matchServer = false;
                        break;
                    case 10:
                        lbSVCPmodel.BackColor = Color.Green;
                        lbSVCPchecksum.BackColor = Color.Red;
                        tbHistory.AppendText("Model " + model.ModelName + " not match checksum.");
                        matchServer = false;
                        break;
                    case 11:
                        lbSVCPmodel.BackColor = Color.Green;
                        lbSVCPchecksum.BackColor = Color.Green;
                        matchServer = true;
                        if (Database.Connect())
                        {
                            Database.UpdateUsedModel(SVmodel, _CONFIG.Line);
                        }
                        break;
                }
                if (matchServer)
                {
                    tbHistory.AppendText(" load model to site. ");
                    Site1.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[0].ROM_PATH);
                    Site2.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[1].ROM_PATH);
                    Site3.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[2].ROM_PATH);
                    Site4.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[3].ROM_PATH);
                    timerReleaseBoard.Interval = 749;
                    timerReleaseBoard.Start();
                }

            }
            else
            {
                Site1.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[0].ROM_PATH);
                Site2.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[1].ROM_PATH);
                Site3.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[2].ROM_PATH);
                Site4.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[3].ROM_PATH);
                timerReleaseBoard.Interval = 749;
                timerReleaseBoard.Start();
            }

        }

        public void openLastWokingModel(string path)
        {
            string[] config = File.ReadAllLines(path);
            lbROM1checkSum.Invoke(new MethodInvoker(delegate
            {
                lbROM1checkSum.Text = config[1].Split('\\')[config[1].Split('\\').Length - 1];
                lbRomNameSite1.Text = lbROM1checkSum.Text.Split('_')[1].Replace(" ", string.Empty);
                lbRomNameSite2.Text = config[2].Split('\\')[config[2].Split('\\').Length - 1];
                lbROM2checkSum.Text = lbRomNameSite2.Text.Split('_')[1].Replace(" ", string.Empty);
                lbROM3checkSum.Text = config[3].Split('\\')[config[3].Split('\\').Length - 1];
                lbROM3checkSum.Text = lbROM3checkSum.Text.Split('_')[1].Replace(" ", string.Empty);
                lbROM4checkSum.Text = config[4].Split('\\')[config[4].Split('\\').Length - 1];
                lbROM4checkSum.Text = lbROM4checkSum.Text.Split('_')[1].Replace(" ", string.Empty);

                model.ModelName = lbROM1checkSum.Text.Remove(lbROM1checkSum.Text.Length - 5, 5);

                if (lbROM1checkSum.Text != lbRomNameSite2.Text)
                {
                    model.ModelName += "/" + lbRomNameSite2.Text.Remove(lbRomNameSite2.Text.Length - 5, 5);
                }
                else if (lbROM1checkSum.Text != lbROM3checkSum.Text)
                {
                    model.ModelName += "/" + lbROM3checkSum.Text.Remove(lbROM3checkSum.Text.Length - 5, 5);
                }
                else if (lbROM1checkSum.Text != lbROM4checkSum.Text)
                {
                    model.ModelName += "/" + lbROM4checkSum.Text.Remove(lbROM4checkSum.Text.Length - 5, 5);
                }
                lbModelName.Text = model.ModelName;
            }));

            Site1.WorkProcess.ClearCMDQueue();
            Site2.WorkProcess.ClearCMDQueue();
            Site3.WorkProcess.ClearCMDQueue();
            Site4.WorkProcess.ClearCMDQueue();

            Site1.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
            Site2.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
            Site3.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
            Site4.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);

            Site1.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + config[1]);
            Site2.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + config[2]);
            Site3.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + config[3]);
            Site4.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + config[4]);

            for (int i = 5; i < config.Length; i++)
            {
                AMWsProcess.PutComandToFIFO(config[i]);
                Console.WriteLine(config[i]);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            if (Permissions == MANAGER || Permissions == TECH)
            {
                if (label1.BackColor != activeColor)
                    label1.BackColor = activeColor;
                else
                    label1.BackColor = deactiveColor;

                Site1.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                Site2.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                Site3.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                Site4.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
            }
        }

        private void lbSiteName1_Click(object sender, EventArgs e)
        {
            Site1.OpenSite("1180-" + ElnecAddress.ToString("d5"), RemoteIP, RemotePort);
        }

        private void lbSiteName2_Click(object sender, EventArgs e)
        {
            Site2.OpenSite("1180-" + (ElnecAddress + 1).ToString("d5"), RemoteIP, RemotePort + 1);
        }

        private void lbSiteName3_Click(object sender, EventArgs e)
        {
            Site3.OpenSite("1180-" + (ElnecAddress + 2).ToString("d5"), RemoteIP, RemotePort + 2);
        }

        private void lbSiteName4_Click(object sender, EventArgs e)
        {
            Site4.OpenSite("1180-" + (ElnecAddress + 3).ToString("d5"), RemoteIP, RemotePort + 3);
        }

        public void checkPermision()
        {
            tsslPermissions.Text = "User: " + Permissions;
            switch (Permissions)
            {
                case OP:
                    {
                        btUserBarcode.Click -= btUserBarcode_Click;
                        btSkipBarcode.Click -= btSkipBarcode_Click;

                        lbROM.Click -= lbROMsellected_Click;

                        lbSite1Sellect.Click -= lbSite1Sellect_Click;
                        lbSite2Sellect.Click -= lbSite2Sellect_Click;
                        lbSite3Sellect.Click -= lbSite3Sellect_Click;
                        lbSite4Sellect.Click -= lbSite4Sellect_Click;

                        label1.Click -= label1_Click;
                        lbSiteName1.Click -= lbSiteName1_Click;
                        lbSiteName2.Click -= lbSiteName2_Click;
                        lbSiteName3.Click -= lbSiteName3_Click;
                        lbSiteName4.Click -= lbSiteName4_Click;
                        siteCheckSumRefrest.Click -= siteCheckSumRefrest_Click;

                        cbSkipSite1.Enabled = false;
                        cbSkipSite2.Enabled = false;
                        cbSkipSite3.Enabled = false;
                        cbSkipSite4.Enabled = false;
                        break;
                    }
                case TECH:
                    {
                        btUserBarcode.Click -= btUserBarcode_Click;
                        btSkipBarcode.Click -= btSkipBarcode_Click;

                        lbROM.Click -= lbROMsellected_Click;

                        lbSite1Sellect.Click -= lbSite1Sellect_Click;
                        lbSite2Sellect.Click -= lbSite2Sellect_Click;
                        lbSite3Sellect.Click -= lbSite3Sellect_Click;
                        lbSite4Sellect.Click -= lbSite4Sellect_Click;

                        label1.Click -= label1_Click;
                        lbSiteName1.Click -= lbSiteName1_Click;
                        lbSiteName2.Click -= lbSiteName2_Click;
                        lbSiteName3.Click -= lbSiteName3_Click;
                        lbSiteName4.Click -= lbSiteName4_Click;
                        siteCheckSumRefrest.Click -= siteCheckSumRefrest_Click;

                        cbSkipSite1.Enabled = true;
                        cbSkipSite2.Enabled = true;
                        cbSkipSite3.Enabled = true;
                        cbSkipSite4.Enabled = true;

                        break;
                    }
                case MANAGER:
                    {
                        lbROM.Click -= lbROMsellected_Click;

                        lbSite1Sellect.Click -= lbSite1Sellect_Click;
                        lbSite2Sellect.Click -= lbSite2Sellect_Click;
                        lbSite3Sellect.Click -= lbSite3Sellect_Click;
                        lbSite4Sellect.Click -= lbSite4Sellect_Click;

                        label1.Click -= label1_Click;
                        lbSiteName1.Click -= lbSiteName1_Click;
                        lbSiteName2.Click -= lbSiteName2_Click;
                        lbSiteName3.Click -= lbSiteName3_Click;
                        lbSiteName4.Click -= lbSiteName4_Click;

                        siteCheckSumRefrest.Click -= siteCheckSumRefrest_Click;

                        btUserBarcode.Click += btUserBarcode_Click;
                        btSkipBarcode.Click += btSkipBarcode_Click;

                        lbROM.Click += lbROMsellected_Click;

                        lbSite1Sellect.Click += lbSite1Sellect_Click;
                        lbSite2Sellect.Click += lbSite2Sellect_Click;
                        lbSite3Sellect.Click += lbSite3Sellect_Click;
                        lbSite4Sellect.Click += lbSite4Sellect_Click;

                        label1.Click += label1_Click;
                        lbSiteName1.Click += lbSiteName1_Click;
                        lbSiteName2.Click += lbSiteName2_Click;
                        lbSiteName3.Click += lbSiteName3_Click;
                        lbSiteName4.Click += lbSiteName4_Click;
                        siteCheckSumRefrest.Click += siteCheckSumRefrest_Click;

                        cbSkipSite1.Enabled = true;
                        cbSkipSite2.Enabled = true;
                        cbSkipSite3.Enabled = true;
                        cbSkipSite4.Enabled = true;
                        break;
                    }
            }

        }
        private void tslPreviewName_Click(object sender, EventArgs e)
        {

        }

        private void logo_Click(object sender, EventArgs e)
        {
            Permissions = OP;
            checkPermision();
            gbSetting.Visible = false;
            //FinalTestBigLabel(false);
        }

        private void logoDEV_Click(object sender, EventArgs e)
        {
            if (Form.ModifierKeys == Keys.Control)
            {
                if (gbLog.Visible == true)
                {
                    gbLog.Visible = false;
                    gbTestHistory.Visible = true;
                }
                else
                {
                    gbLog.Visible = true;
                    gbTestHistory.Visible = false;
                }
            }
        }

        private void timerUpdateChar_Tick(object sender, EventArgs e)
        {
            if (CharCircle <= 360)
            {
                DrawChart(AMWsProcess.Statitis_OK, AMWsProcess.Statitis_NG, CharCircle);
                CharCircle = CharCircle + (360 - CharCircle) / 50 + 1;
                timerUpdateChar.Start();
            }
            else
            {
                timerUpdateChar.Stop();
                //timerUpdateChar.Dispose();
            }

        }

        private void timerReleaseBoard_Tick(object sender, EventArgs e)
        {
            if (timerReleaseBoard.Interval == 5)
            {

                highlinedgwTestMode(3);
                if (Port.IsOpen && lbAutoManual.Text == "Auto mode")
                {
                    Port.Write(ResultRespoonse);
                    tbSerialData.AppendText("[TX--] " + ResultRespoonse + Environment.NewLine);
                }
                tbHistory.AppendText((DateTime.Now - lastWorkingTime).TotalSeconds.ToString("F2") + "s" + "           DONE\r\n");
                lastWorkingTime = DateTime.Now;
                LosingTime = true;
                timerReleaseBoard.Stop();
            }
            else if (timerReleaseBoard.Interval == 2500)
            {
                if (lbSiteName1.BackColor != Color.Black || lbSiteName2.BackColor != Color.Black || lbSiteName3.BackColor != Color.Black || lbSiteName4.BackColor != Color.Black)
                {
                    if (lbSiteName1.BackColor != activeColor || lbSiteName2.BackColor != activeColor || lbSiteName3.BackColor != activeColor || lbSiteName4.BackColor != activeColor)
                    {
                        CloseElnec();
                        tbHistory.AppendText("Openning Elnect");

                        Site1.OpenSite("1180-" + (ElnecAddress).ToString("d5"), RemoteIP, RemotePort);

                        Site2.OpenSite("1180-" + (ElnecAddress + 1).ToString("d5"), RemoteIP, RemotePort + 1);

                        Site3.OpenSite("1180-" + (ElnecAddress + 2).ToString("d5"), RemoteIP, RemotePort + 2);

                        Site4.OpenSite("1180-" + (ElnecAddress + 3).ToString("d5"), RemoteIP, RemotePort + 3);

                    }
                    timerReleaseBoard.Interval = 750;
                }
            }

            else if (timerReleaseBoard.Interval == 750)
            {
                if (lbSiteName1.BackColor == activeColor && lbSiteName2.BackColor == activeColor && lbSiteName3.BackColor == activeColor && lbSiteName4.BackColor == activeColor)
                {
                    tbHistory.AppendText(" success." + Environment.NewLine);
                    timerReleaseBoard.Stop();
                }
                else if (lbSiteName1.BackColor == Color.Black && lbSiteName2.BackColor == Color.Black && lbSiteName3.BackColor == Color.Black && lbSiteName4.BackColor == Color.Black)
                {
                    tbHistory.AppendText(" Not find any programer." + Environment.NewLine);
                    timerReleaseBoard.Stop();
                }
                else
                {
                    tbHistory.AppendText(". ");
                }
            }
            else if (timerReleaseBoard.Interval == 749)
            {
                if (lbRomNameSite1.BackColor == Color.Green && lbRomNameSite2.BackColor == Color.Green && lbRomNameSite3.BackColor == Color.Green && lbRomNameSite4.BackColor == Color.Green)
                {
                    if (lbROM1checkSum.BackColor == Color.Green && lbRomNameSite2.BackColor == Color.Green && lbROM3checkSum.BackColor == Color.Green && lbROM4checkSum.BackColor == Color.Green)
                    {
                        tbHistory.AppendText("success." + Environment.NewLine);
                        timerReleaseBoard.Stop();
                    }
                    else
                    {
                        tbHistory.AppendText(" load done. Check sum not match." + Environment.NewLine);
                        timerReleaseBoard.Stop();
                    }
                }
                else if (lbRomNameSite1.BackColor == Color.Red && lbRomNameSite2.BackColor == Color.Red && lbRomNameSite3.BackColor == Color.Red && lbRomNameSite4.BackColor == Color.Red)
                {
                    tbHistory.AppendText("fail. Please check Elnec connect or your program path and try again." + Environment.NewLine);
                    timerReleaseBoard.Stop();
                }
                else
                {

                    tbHistory.AppendText(". ");
                    if (DateTime.Now.Subtract(startLoad).TotalSeconds > 15)
                    {
                        tbHistory.AppendText("fail: \"load model timeout\" " + Environment.NewLine);
                        timerReleaseBoard.Stop();
                    }
                    if (DateTime.Now.Subtract(startLoad).TotalSeconds > 5)
                    {
                        Site1.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                        Site2.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                        Site3.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                        Site4.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                    }
                }

            }

        }

        private void btUserBarcode_Click(object sender, EventArgs e)
        {
            btSkipBarcode.BackColor = nonactiveColor;
            btUserBarcode.BackColor = activeColor;

            lbBC1.BackColor = deactiveColor;
            lbBC2.BackColor = deactiveColor;
            lbBC3.BackColor = deactiveColor;
            lbBC4.BackColor = deactiveColor;

            lbBarcodeTesting1.BackColor = activeColor;
            lbBarcodeTesting2.BackColor = activeColor;
            lbBarcodeTesting3.BackColor = activeColor;
            lbBarcodeTesting4.BackColor = activeColor;

            lbBarCode1.BackColor = deactiveColor;
            lbBarCode2.BackColor = deactiveColor;
            lbBarCode3.BackColor = deactiveColor;
            lbBarCode4.BackColor = deactiveColor;

            lbBarcodeWaiting1.BackColor = activeColor;
            lbBarcodeWaiting2.BackColor = activeColor;
            lbBarcodeWaiting3.BackColor = activeColor;
            lbBarcodeWaiting4.BackColor = activeColor;
            Port.Write(Data_enaQR);
            tbSerialData.AppendText("[TX--] " + Data_enaQR + Environment.NewLine);
        }

        private void btSkipBarcode_Click(object sender, EventArgs e)
        {
            SkipBarCode();
            Port.Write(Data_skipQR);
            tbSerialData.AppendText("[TX--] " + Data_skipQR + Environment.NewLine);
        }
        public void SkipBarCode()
        {
            btSkipBarcode.BackColor = activeColor;
            btUserBarcode.BackColor = nonactiveColor;

            lbBC1.BackColor = nonactiveColor;
            lbBC2.BackColor = nonactiveColor;
            lbBC3.BackColor = nonactiveColor;
            lbBC4.BackColor = nonactiveColor;

            lbBarcodeTesting1.BackColor = nonactiveColor;
            lbBarcodeTesting2.BackColor = nonactiveColor;
            lbBarcodeTesting3.BackColor = nonactiveColor;
            lbBarcodeTesting4.BackColor = nonactiveColor;

            lbBarcodeTesting1.Text = "Skip";
            lbBarcodeTesting2.Text = "Skip";
            lbBarcodeTesting3.Text = "Skip";
            lbBarcodeTesting4.Text = "Skip";

            lbBarCode1.BackColor = nonactiveColor;
            lbBarCode2.BackColor = nonactiveColor;
            lbBarCode3.BackColor = nonactiveColor;
            lbBarCode4.BackColor = nonactiveColor;

            lbBarcodeWaiting1.BackColor = nonactiveColor;
            lbBarcodeWaiting2.BackColor = nonactiveColor;
            lbBarcodeWaiting3.BackColor = nonactiveColor;
            lbBarcodeWaiting4.BackColor = nonactiveColor;

            lbBarcodeWaiting1.Text = "Skip";
            lbBarcodeWaiting2.Text = "Skip";
            lbBarcodeWaiting3.Text = "Skip";
            lbBarcodeWaiting4.Text = "Skip";

        }
        private void siteCheckSumRefrest_Click(object sender, EventArgs e)
        {
            if (Permissions == MANAGER || Permissions == TECH)
            {
                if (siteCheckSumRefrest.BackColor != activeColor)
                    siteCheckSumRefrest.BackColor = activeColor;
                else
                    siteCheckSumRefrest.BackColor = deactiveColor;

                Site1.WorkProcess.PutComandToFIFO(ElnecSite.GETDEVCHECKSUM);
                Site2.WorkProcess.PutComandToFIFO(ElnecSite.GETDEVCHECKSUM);
                Site3.WorkProcess.PutComandToFIFO(ElnecSite.GETDEVCHECKSUM);
                Site4.WorkProcess.PutComandToFIFO(ElnecSite.GETDEVCHECKSUM);
            }
        }

        int NumberArray = 4;
        private void radioButton2_Click(object sender, EventArgs e)
        {
            if (radioButton2.Checked == true)
            {
                model.Layout.PCB2 = false;
                radioButton2.Checked = false;
                if (model.Layout.PCB1)
                {
                    NumberArray = 1;
                }
                else
                {
                    NumberArray = 0;
                }

            }
            else
            {
                model.Layout.PCB2 = true;
                radioButton2.Checked = true;
                if (model.Layout.PCB1)
                {
                    NumberArray = 2;
                }
                else
                {
                    NumberArray = 1;
                }
            }

            if (NumberArray > 0)
                PCBarrayCount.Maximum = 4 / (MicomArray.Value * NumberArray);
            else
                PCBarrayCount.Maximum = 1;

            if (model.Layout.ArrayCount > PCBarrayCount.Maximum)
                model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Maximum);

            model.Layout.XasixArrayCount = Convert.ToInt32(nbUDXarrayCount.Value);
            model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Value);
            if (_CONFIG.InlineMachine)
            {
                model.Layout.drawPCBLayout(pbPCBLayout);
            }
            else
            {
                model.Layout.drawPCBLayout(pbPCBLayout, true);
            }
        }

        public int[] xcountArray = new int[4];
        private void PCBarrayCount_ValueChanged(object sender, EventArgs e)
        {
            nbUDXarrayCount.Maximum = PCBarrayCount.Value;
            model.Layout.XasixArrayCount = Convert.ToInt32(nbUDXarrayCount.Value);
            model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Value);
            if (_CONFIG.InlineMachine)
            {
                model.Layout.drawPCBLayout(pbPCBLayout);
            }
            else
            {
                model.Layout.drawPCBLayout(pbPCBLayout, true);
            }
        }

        private void MicomArray_ValueChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                NumberArray = 2;
            }
            else
            {
                NumberArray = 1;
            }



            if (NumberArray > 0)
            {
                PCBarrayCount.Maximum = 4 / (MicomArray.Value * NumberArray);
                if (PCBarrayCount.Maximum == 0) PCBarrayCount.Maximum = 1;
                nbUDXarrayCount.Maximum = PCBarrayCount.Value;
                nbUDXarrayCount.Minimum = 1;
                nbUDXarrayCount.Value = 1;
            }
            else
            {
                nbUDXarrayCount.Maximum = PCBarrayCount.Value;
                nbUDXarrayCount.Maximum = 1;
                nbUDXarrayCount.Minimum = 1;
                nbUDXarrayCount.Value = 1;
            }

            model.Layout.MicomNumber = Convert.ToInt32(MicomArray.Value);
            model.Layout.XasixArrayCount = Convert.ToInt32(nbUDXarrayCount.Value);
            model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Value);

            if (model.Layout.ArrayCount > PCBarrayCount.Maximum)
                model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Maximum);
            if (model.Layout.XasixArrayCount > nbUDXarrayCount.Maximum)
                model.Layout.XasixArrayCount = Convert.ToInt32(nbUDXarrayCount.Maximum);

            if (_CONFIG.InlineMachine)
            {
                model.Layout.drawPCBLayout(pbPCBLayout);
            }
            else
            {
                model.Layout.drawPCBLayout(pbPCBLayout, true);
            }
        }

        private void nbUDXarrayCount_ValueChanged(object sender, EventArgs e)
        {
            model.Layout.XasixArrayCount = Convert.ToInt32(nbUDXarrayCount.Value);
            model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Value);
            if (_CONFIG.InlineMachine)
            {
                model.Layout.drawPCBLayout(pbPCBLayout);
            }
            else
            {
                model.Layout.drawPCBLayout(pbPCBLayout, true);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string CS1 = tbStRomCsSite1.Text;
            string CS2 = tbStRomCsSite3.Text;

            if (CS2 != CS1)
            {
                model.ModelName = tbQRname.Text + "_" + CS1 + "-" + CS2 + "_" + tbVersion.Text;
            }
            else
            {
                model.ModelName = tbQRname.Text + "_" + CS1 + "_" + tbVersion.Text;
            }

            model.ROMs[0].ROM_CHECKSUM = tbStRomCsSite1.Text;
            model.ROMs[1].ROM_CHECKSUM = tbStRomCsSite2.Text;
            model.ROMs[2].ROM_CHECKSUM = tbStRomCsSite3.Text;
            model.ROMs[3].ROM_CHECKSUM = tbStRomCsSite4.Text;

            model.TimeOut = (int)nudTCPTimeOut.Value;

            model.saveFileDialog.InitialDirectory = _CONFIG.recentModelPath;
            model.SaveAsNew(_CONFIG.recentModelPath);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialogSite1.InitialDirectory = _CONFIG.recentWorkPath;
            openFileDialogSite1.ShowDialog();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            openFileDialogSite2.InitialDirectory = _CONFIG.recentWorkPath;
            openFileDialogSite2.ShowDialog();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            openFileDialogSite3.InitialDirectory = _CONFIG.recentWorkPath;
            openFileDialogSite3.ShowDialog();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            openFileDialogSite4.InitialDirectory = _CONFIG.recentWorkPath;
            openFileDialogSite4.ShowDialog();
        }

        private void ElnecEndAdd_TextChanged(object sender, EventArgs e)
        {

        }
        private void btApplyConnectSettup_Click(object sender, EventArgs e)
        {
            _CONFIG.defaulBaudrate = Convert.ToInt32(BaudRate[cbbComBaurate.SelectedIndex]);
            _CONFIG.defaulComPort = cbbComName.Text;
            _CONFIG.ElnecStrAddress = Convert.ToInt32(ElnecStartAdd.Text);
            _CONFIG.ElnecAddress = Convert.ToInt32(ElnecEndAdd.Text);
            _CONFIG.ServerCompare = cbServerCompare.Checked;
            _CONFIG.InlineMachine = cbInlineMachine.Checked;
            _CONFIG.Line = cbbLineLock.Text;
            gbServerCompare.Enabled = _CONFIG.ServerCompare;
            _CONFIG.SaveConfig();

            if (_CONFIG.ServerCompare)
            {
                if (Database.Connect())
                {
                    lbServerConnect.Text = "Server connected   ";
                    List<string> Lines = new List<string>();
                    Database.getLineList(Lines);
                    cbbLineLock.Items.AddRange(Lines.ToArray());
                    for (int i = 0; i < cbbLineLock.Items.Count; i++)
                    {
                        if (cbbLineLock.Items[i].ToString() == _CONFIG.Line)
                        {
                            cbbLineLock.SelectedIndex = i;
                            break;
                        }
                    }
                    timerUpdateStatus.Start();
                }
                else
                {
                    lbServerConnect.Text = "Server not available   ";
                }
            }
        }

        private void gbLog_Enter(object sender, EventArgs e)
        {

        }

        private void timerCheckCom_Tick(object sender, EventArgs e)
        {
            if (Port.IsOpen)
            {
                tsslbCOM.ForeColor = Color.White;
                tsslbCOM.Text = Port.PortName + "                        ";
            }
            else
            {
                try
                {
                    tsslbCOM.ForeColor = Color.White;
                    Port.PortName = SearchCom();
                    tsslbCOM.Text = Port.PortName + "                        ";
                    Port.Open();
                }
                catch (Exception)
                {
                    tsslbCOM.ForeColor = Color.Red;
                    tsslbCOM.Text = "COM ERROR               ";
                }

            }
        }

        private void timerQR_Tick(object sender, EventArgs e)
        {

            Console.WriteLine("OK QR");
            if (Port.IsOpen)
            {
                Port.Write(Result_okQR);
                tbSerialData.AppendText("[TX--] " + Result_okQR + Environment.NewLine);
            }
            timerQR.Stop();
        }

        private void btReloadElnec_Click(object sender, EventArgs e)
        {

            ServerStatus = SERVER_OFF;
            while (Site1IsTalking && Site2IsTalking && Site3IsTalking && Site4IsTalking) ;
            ServerStatus = SERVER_ON;

            //Thread communicationSite1 = new Thread(ElnecComuncationBackgroudSite1);
            //communicationSite1.Start();
            //Thread communicationSite2 = new Thread(ElnecComuncationBackgroudSite2);
            //communicationSite2.Start();
            //Thread communicationSite3 = new Thread(ElnecComuncationBackgroudSite3);
            //communicationSite3.Start();
            //Thread communicationSite4 = new Thread(ElnecComuncationBackgroudSite4);
            //communicationSite4.Start();

            CloseElnec();
            tbHistory.AppendText("Restart Elnec.");

            Site1.OpenSite("1180-" + (ElnecAddress).ToString("d5"), _CONFIG.RemoteIP, _CONFIG.RemotePort);

            Site2.OpenSite("1180-" + (ElnecAddress + 1).ToString("d5"), _CONFIG.RemoteIP, _CONFIG.RemotePort + 1);

            Site3.OpenSite("1180-" + (ElnecAddress + 2).ToString("d5"), _CONFIG.RemoteIP, _CONFIG.RemotePort + 2);

            Site4.OpenSite("1180-" + (ElnecAddress + 3).ToString("d5"), _CONFIG.RemoteIP, _CONFIG.RemotePort + 3);
            _CONFIG.ElnecAddress = Convert.ToInt32(ElnecEndAdd.Text);

            lbAdressSite1.Text = _CONFIG.ElnecStrAddress.ToString() + "-" + _CONFIG.ElnecAddress.ToString("d5");
            lbAdressSite2.Text = _CONFIG.ElnecStrAddress.ToString() + "-" + (_CONFIG.ElnecAddress + 1).ToString("d5");
            lbAdressSite3.Text = _CONFIG.ElnecStrAddress.ToString() + "-" + (_CONFIG.ElnecAddress + 2).ToString("d5");
            lbAdressSite4.Text = _CONFIG.ElnecStrAddress.ToString() + "-" + (_CONFIG.ElnecAddress + 3).ToString("d5");
        }

        private void pnResultFinal_Click(object sender, EventArgs e)
        {
            FinalTestBigLabel(false);
        }


        // Manual control
        public void StartManualTest(object sender, EventArgs e)
        {
            Timeout = false;

            Site1.ClearSiteParam();
            Site2.ClearSiteParam();
            Site3.ClearSiteParam();
            Site4.ClearSiteParam();

            ActiveLabel(lbResultA);
            ActiveLabel(lbResultB);
            ActiveLabel(lbResultC);
            ActiveLabel(lbResultD);

            lbMachineStatus.Text = "Manual Test";

            pbTesting.Maximum = 0;
            pbTesting.Value = 0;

            string logHistory = " ";

            if (lbManualROM1.BackColor == activeColor || lbManualROM2.BackColor == activeColor || lbManualROM3.BackColor == activeColor || lbManualROM4.BackColor == activeColor)
                tbHistory.AppendText(Environment.NewLine + " Manual program: ");
            if (lbManualROM1.BackColor == activeColor)
            {
                Site1.WorkProcess.ClearCMDQueue();
                Site1.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                Site1.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
                tbHistory.AppendText("ROM 1");
                logHistory = "ROM 1";
                pbTesting.Maximum = pbTesting.Maximum + 200;
            }
            if (lbManualROM2.BackColor == activeColor)
            {
                Site2.WorkProcess.ClearCMDQueue();
                Site2.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                Site2.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
                if (logHistory.Length > 4) tbHistory.AppendText(", ");
                tbHistory.AppendText("ROM 2");
                logHistory = "ROM 2";
                pbTesting.Maximum = pbTesting.Maximum + 200;
            }
            if (lbManualROM3.BackColor == activeColor)
            {
                Site3.WorkProcess.ClearCMDQueue();
                Site3.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                Site3.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
                if (logHistory.Length > 4) tbHistory.AppendText(", ");
                tbHistory.AppendText("ROM 3");
                logHistory = "ROM 3";
                pbTesting.Maximum = pbTesting.Maximum + 200;
            }
            if (lbManualROM4.BackColor == activeColor)
            {
                Site4.WorkProcess.ClearCMDQueue();
                Site4.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                Site4.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
                if (logHistory.Length > 4) tbHistory.AppendText(", ");
                tbHistory.AppendText("ROM 4");
                logHistory = "ROM 4";
                pbTesting.Maximum = pbTesting.Maximum + 200;
            }
            tbHistory.AppendText(".");
        }
        public void StopManualTest(object sender, EventArgs e)
        {
            pbTesting.Maximum = 800;
            pbTesting.Value = 0;

            tbHistory.AppendText(Environment.NewLine + " Stop manual. ");
            if (lbManualROM1.BackColor == activeColor)
            {
                Site1.WorkProcess.ClearCMDQueue();
                Site1.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                Site1.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
            }
            if (lbManualROM2.BackColor == activeColor)
            {
                Site2.WorkProcess.ClearCMDQueue();
                Site2.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                Site2.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
            }
            if (lbManualROM3.BackColor == activeColor)
            {
                Site3.WorkProcess.ClearCMDQueue();
                Site3.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                Site3.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
            }
            if (lbManualROM4.BackColor == activeColor)
            {
                Site4.WorkProcess.ClearCMDQueue();
                Site4.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
                Site4.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
            }
        }

        private void lbManualROM1_Click(object sender, EventArgs e)
        {
            if (lbManualROM1.BackColor == activeColor)
                lbManualROM1.BackColor = Color.FromArgb(60, 60, 60);
            else
                lbManualROM1.BackColor = activeColor;
        }

        private void lbManualROM2_Click(object sender, EventArgs e)
        {
            if (lbManualROM2.BackColor == activeColor)
                lbManualROM2.BackColor = Color.FromArgb(60, 60, 60);
            else
                lbManualROM2.BackColor = activeColor;
        }

        private void lbManualROM3_Click(object sender, EventArgs e)
        {
            if (lbManualROM3.BackColor == activeColor)
                lbManualROM3.BackColor = Color.FromArgb(60, 60, 60);
            else
                lbManualROM3.BackColor = activeColor;
        }

        private void lbManualROM4_Click(object sender, EventArgs e)
        {
            if (lbManualROM4.BackColor == activeColor)
                lbManualROM4.BackColor = Color.FromArgb(60, 60, 60);
            else
                lbManualROM4.BackColor = activeColor;
        }

        private void timerTimeOut_Tick(object sender, EventArgs e)
        {
            if (Site1.Result == ElnecSite.EMPTY || Site2.Result == ElnecSite.EMPTY || Site3.Result == ElnecSite.EMPTY || Site4.Result == ElnecSite.EMPTY)
            {
                if (lbResultA.BackColor == activeColor)
                {
                    Site1.WorkProcess.ClearCMDQueue();
                    Site1.Result = ElnecSite.RESULT_NG;
                    Site1.SITE_PROGRAMRESULT = ElnecSite.RESULT_NG;
                    NG_label(lbResultA);
                    NG_label(lbResultAbig);
                    tbHistory.AppendText(Environment.NewLine + "Site 1: Timeout error." + Environment.NewLine);
                }

                if (lbResultB.BackColor == activeColor)
                {
                    Site2.WorkProcess.ClearCMDQueue();
                    Site2.Result = ElnecSite.RESULT_NG;
                    Site2.SITE_PROGRAMRESULT = ElnecSite.RESULT_NG;
                    NG_label(lbResultB);
                    NG_label(lbResultBbig);
                    tbHistory.AppendText(Environment.NewLine + "Site 2: Timeout error." + Environment.NewLine);
                }
                if (lbResultC.BackColor == activeColor)
                {
                    Site3.WorkProcess.ClearCMDQueue();
                    Site3.Result = ElnecSite.RESULT_NG;
                    Site3.SITE_PROGRAMRESULT = ElnecSite.RESULT_NG;
                    NG_label(lbResultC);
                    NG_label(lbResultCbig);
                    tbHistory.AppendText(Environment.NewLine + "Site 3: Timeout error." + Environment.NewLine);
                }
                if (lbResultD.BackColor == activeColor)
                {
                    Site4.WorkProcess.ClearCMDQueue();
                    Site4.Result = ElnecSite.RESULT_NG;
                    Site4.SITE_PROGRAMRESULT = ElnecSite.RESULT_NG;
                    NG_label(lbResultD);
                    NG_label(lbResultDbig);
                    tbHistory.AppendText(Environment.NewLine + "Site 4: Timeout error." + Environment.NewLine);
                }
                FinalTestLabel();
                Timeout = true;
            }
            timerTimeOut.Stop();
            timerTimeOut.Dispose();
        }

        protected virtual bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            //file is not locked
            return false;
        }

        public string bufferRP = "";
        // copy history to MES
        public void copyHistoryToMES()
        {
            string fileResult = "Report-" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";
            if (!File.Exists(_CONFIG.configPath + "buffer.txt"))
                File.WriteAllText(_CONFIG.configPath + "buffer.txt", bufferRP);
            if (!Directory.Exists(_CONFIG.MESpath)) Directory.CreateDirectory(_CONFIG.MESpath);
            if (!File.Exists(_CONFIG.MESpath + DateTime.Now.ToString("yyyy_MM_dd") + ".txt"))
            {
                if (Directory.Exists(_CONFIG.reportPath))
                {
                    FileInfo fileInfo = new FileInfo(_CONFIG.reportPath + fileResult);
                    if (!IsFileLocked(fileInfo))
                    {
                        bufferRP = File.ReadAllText(_CONFIG.configPath + "buffer.txt");
                        string alldata = File.ReadAllText(_CONFIG.reportPath + fileResult);
                        string newdata = "";
                        if (bufferRP.Length < alldata.Length)
                            newdata = alldata.Remove(0, bufferRP.Length);
                        else if (bufferRP.Length > alldata.Length)
                            newdata = alldata;
                        bufferRP = alldata;
                        if (newdata.Length > 0)
                            File.WriteAllText(_CONFIG.MESpath + @"\" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt", newdata);
                        File.WriteAllText(_CONFIG.configPath + "buffer.txt", bufferRP);
                    }
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            copyHistoryToMES();
        }

        private void label8_Click(object sender, EventArgs e)
        {
            if (Permissions == MANAGER || Permissions == TECH)
            {
                if (label8.BackColor == activeColor)
                {
                    label8.BackColor = deactiveColor;
                }
                else
                {
                    label8.BackColor = activeColor;
                }

                Site1.WorkProcess.PutComandToFIFO(ElnecSite.REFIND_PGM);
                Site2.WorkProcess.PutComandToFIFO(ElnecSite.REFIND_PGM);
                Site3.WorkProcess.PutComandToFIFO(ElnecSite.REFIND_PGM);
                Site4.WorkProcess.PutComandToFIFO(ElnecSite.REFIND_PGM);
            }
        }

        private void label18_Click(object sender, EventArgs e)
        {
            if (tbStRomCsSite1.Text.Length == 8 && model.Layout.MicomNumber == 1)
            {
                tbStRomCsSite2.Text = tbStRomCsSite1.Text;
                tbStRomCsSite3.Text = tbStRomCsSite1.Text;
                tbStRomCsSite4.Text = tbStRomCsSite1.Text;
            }
        }

        #region Manual test 
        MachineCommand command = new MachineCommand();
        string dataManualTest = "";
        private void btManualTest_Click(object sender, EventArgs e)
        {
            if (Permissions != "OP")
            {
                gbManualTest.Visible = !gbManualTest.Visible;
                gbSerialTest.Visible = gbManualTest.Visible;
                if (gbManualTest.Visible)
                {

                    for (int i = 0; i < command.CMDs.Count; i++)
                    {
                        cbbMachineCommand.Items.Add(command.CMDs[i].Name);
                    }
                }
            }
            else
            {
                PassWorldForm passWorldForm = new PassWorldForm();
                if (passWorldForm.ShowDialog() == DialogResult.OK)
                {
                    Permissions = MANAGER;
                    tsslPermissions.Text = "User: " + Permissions;
                    gbManualTest.Visible = !gbManualTest.Visible;
                    gbSerialTest.Visible = gbManualTest.Visible;
                    if (gbManualTest.Visible)
                    {

                        for (int i = 0; i < command.CMDs.Count; i++)
                        {
                            cbbMachineCommand.Items.Add(command.CMDs[i].Name);
                        }
                    }
                }

            }
        }

        private void cbbMachineCommand_SelectedIndexChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < command.CMDs.Count; i++)
            {
                if (command.CMDs[i].Name == cbbMachineCommand.Text)
                {
                    dataManualTest = command.CMDs[i].Data;
                    if (Port.IsOpen)
                    {
                        Port.Write(dataManualTest);
                        tbSerialData.AppendText("[TX--] " + dataManualTest + Environment.NewLine);
                    }
                }
            }
        }

        private void btManualTestSendData_Click(object sender, EventArgs e)
        {
            if (Port.IsOpen)
            {
                Port.Write(dataManualTest);
                tbSerialData.AppendText("[TX--] " + dataManualTest + Environment.NewLine);
            }
            else
            {
                MessageBox.Show("Port not connect");
            }
        }


        #endregion

        private void cbInlineMachine_CheckedChanged(object sender, EventArgs e)
        {
            lbArrow1.Visible = cbInlineMachine.Checked;
            lbArrow2.Visible = cbInlineMachine.Checked;
            lbArrow3.Visible = cbInlineMachine.Checked;
            lbArrow4.Visible = cbInlineMachine.Checked;
            lbPrevious.Visible = cbInlineMachine.Checked;
            lbCamera.Visible = cbInlineMachine.Checked;
            lbNext.Visible = cbInlineMachine.Checked;
            lbBufferNG.Visible = cbInlineMachine.Checked;

            radioButton2.Enabled = cbInlineMachine.Checked;

            if (_CONFIG.InlineMachine)
            {
                model.Layout.drawPCBLayout(pbPCBLayout);
            }
            else
            {
                model.Layout.drawPCBLayout(pbPCBLayout, true);
            }

            if (cbInlineMachine.Checked)
            {
                lbFormName.Text = " Auto Multi Writing System (A-MS) - Inline";
                panelMachineImage.BackgroundImage = Resources.Screenshot_2021_03_08_180014_removebg_preview;

                tlpSiteResult.SetCellPosition(lbResultA, new TableLayoutPanelCellPosition(2, 0));
                tlpSiteResult.SetCellPosition(lbResultB, new TableLayoutPanelCellPosition(1, 0));
                tlpSiteResult.SetCellPosition(lbResultC, new TableLayoutPanelCellPosition(2, 1));
                tlpSiteResult.SetCellPosition(lbResultD, new TableLayoutPanelCellPosition(1, 1));

                tlpBigResult.SetCellPosition(pnBigResultA, new TableLayoutPanelCellPosition(1, 0));
                tlpBigResult.SetCellPosition(pnBigResultB, new TableLayoutPanelCellPosition(0, 0));
                tlpBigResult.SetCellPosition(pnBigResultC, new TableLayoutPanelCellPosition(1, 1));
                tlpBigResult.SetCellPosition(pnBigResultD, new TableLayoutPanelCellPosition(0, 1));

                lbArray2.Show();
                lbArray1.Show();
            }
            else
            {
                radioButton2.Checked = cbInlineMachine.Checked;
                lbFormName.Text = " Auto Multi Writing System (A-MS) - Offline";

                tlpSiteResult.SetCellPosition(lbResultA, new TableLayoutPanelCellPosition(1, 0));
                tlpSiteResult.SetCellPosition(lbResultB, new TableLayoutPanelCellPosition(2, 0));
                tlpSiteResult.SetCellPosition(lbResultC, new TableLayoutPanelCellPosition(1, 1));
                tlpSiteResult.SetCellPosition(lbResultD, new TableLayoutPanelCellPosition(2, 1));

                tlpBigResult.SetCellPosition(pnBigResultA, new TableLayoutPanelCellPosition(0, 0));
                tlpBigResult.SetCellPosition(pnBigResultB, new TableLayoutPanelCellPosition(1, 0));
                tlpBigResult.SetCellPosition(pnBigResultC, new TableLayoutPanelCellPosition(0, 1));
                tlpBigResult.SetCellPosition(pnBigResultD, new TableLayoutPanelCellPosition(1, 1));

                lbArray1.Hide();
                lbArray2.Hide();

                panelMachineImage.BackgroundImage = Resources.OfflineMachine;
                Updatelayout();
            }
        }

        public void Updatelayout()
        {
            if (radioButton2.Checked == true)
            {
                model.Layout.PCB2 = true;
                radioButton2.Checked = true;
                if (model.Layout.PCB1)
                {
                    NumberArray = 2;
                }
                else
                {
                    NumberArray = 1;
                }
            }
            else
            {
                model.Layout.PCB2 = false;
                radioButton2.Checked = false;
                if (model.Layout.PCB1)
                {
                    NumberArray = 1;
                }
                else
                {
                    NumberArray = 0;
                }
            }

            if (NumberArray > 0)
                PCBarrayCount.Maximum = 4 / (MicomArray.Value * NumberArray);
            else
                PCBarrayCount.Maximum = 1;

            if (model.Layout.ArrayCount > PCBarrayCount.Maximum)
                model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Maximum);

            model.Layout.XasixArrayCount = Convert.ToInt32(nbUDXarrayCount.Value);
            model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Value);
            if (_CONFIG.InlineMachine)
            {
                model.Layout.drawPCBLayout(pbPCBLayout);
            }
            else
            {
                model.Layout.drawPCBLayout(pbPCBLayout, true);
            }

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void ProgressPain(object sender, PaintEventArgs e)
        {

        }

        string LastStatus = "";
        private void timerUpdateStatus_Tick(object sender, EventArgs e)
        {
            if (LastStatus != MachineStatus)
            {
                LastStatus = MachineStatus;
                if (_CONFIG.ServerCompare)
                {
                    if (Database.Connect())
                    {
                        Database.UpdateRunStopStatus(MachineStatus, _CONFIG.Line);
                        LostTimeSet = Database.checkUpdateStopTime(LostTimeSet);
                        lbLostTimeSet.Text = LostTimeSet.ToString() + 's';
                    }
                }
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            radioButton2.CheckedChanged -= radioButton2_CheckedChanged;

            radioButton2.CheckedChanged += radioButton2_CheckedChanged;
        }

        private void rad(object sender, EventArgs e)
        {
            radioButton2.CheckedChanged -= radioButton2_CheckedChanged;
            if (radioButton2.Checked == true)
            {
                model.Layout.PCB2 = false;
                radioButton2.Checked = false;
                if (model.Layout.PCB1)
                {
                    NumberArray = 1;
                }
                else
                {
                    NumberArray = 0;
                }

            }
            else
            {
                model.Layout.PCB2 = true;
                radioButton2.Checked = true;
                if (model.Layout.PCB1)
                {
                    NumberArray = 2;
                }
                else
                {
                    NumberArray = 1;
                }
            }

            if (NumberArray > 0)
                PCBarrayCount.Maximum = 4 / (MicomArray.Value * NumberArray);
            else
                PCBarrayCount.Maximum = 1;

            if (model.Layout.ArrayCount > PCBarrayCount.Maximum)
                model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Maximum);

            model.Layout.XasixArrayCount = Convert.ToInt32(nbUDXarrayCount.Value);
            model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Value);
            if (_CONFIG.InlineMachine)
            {
                model.Layout.drawPCBLayout(pbPCBLayout);
            }
            else
            {
                model.Layout.drawPCBLayout(pbPCBLayout, true);
            }
            radioButton2.CheckedChanged += radioButton2_CheckedChanged;
        }

        private void SkipSiteChange(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
                ((CheckBox)sender).BackColor = Color.Gray;
            else
                ((CheckBox)sender).BackColor = Color.Black;

            string ctrl = ((CheckBox)sender).Name;
            switch (ctrl)
            {
                case "cbSkipSite1":
                    btRomSite1.BackColor = ((CheckBox)sender).BackColor;
                    lbRomNameSite1.BackColor = ((CheckBox)sender).BackColor;
                    break;
                case "cbSkipSite2":
                    btRomSite2.BackColor = ((CheckBox)sender).BackColor;
                    lbRomNameSite2.BackColor = ((CheckBox)sender).BackColor;
                    break;
                case "cbSkipSite3":
                    btRomSite3.BackColor = ((CheckBox)sender).BackColor;
                    lbRomNameSite3.BackColor = ((CheckBox)sender).BackColor;
                    break;
                case "cbSkipSite4":
                    btRomSite4.BackColor = ((CheckBox)sender).BackColor;
                    lbRomNameSite4.BackColor = ((CheckBox)sender).BackColor;
                    break;
            }
        }

        private void tsmClearCounter_Click(object sender, EventArgs e)
        {
            AMWsProcess.Statitis_OK = 0;
            AMWsProcess.Statitis_NG = 0;
            CharCircle = 1;
            DrawChart(AMWsProcess.Statitis_OK, AMWsProcess.Statitis_NG, CharCircle);
            timerUpdateChar.Start();
        }
    }

    public class MachineCommand
    {
        public class CMD
        {
            public string Name = "";
            public string Data = "";
            public CMD(string Name, string Data)
            {
                this.Name = Name;
                this.Data = Data;
            }
        }
        public string BuidCommand(int commandCode, int Value)
        {
            int checkSum = (64 + 1 + commandCode + Value) % 256;
            string returnCommand = "@01" + commandCode.ToString("D2") + Value.ToString("D2") + "*" + checkSum.ToString("D2");
            return returnCommand;
        }
        public List<CMD> CMDs = new List<CMD>();
        public string GetCMDByName(string name)
        {
            foreach (var cmd in CMDs)
            {
                if (cmd.Name == name)
                {
                    return cmd.Data;
                }
            }
            return " ";
        }
        public MachineCommand()
        {
            // Common command
            CMDs.Add(new CMD("NG_all", "@010300*68"));
            CMDs.Add(new CMD("OK_all", "@010315*83"));
            CMDs.Add(new CMD("Ready_pcb", "@010100*66"));
            CMDs.Add(new CMD("String_getOK", "@010011*76"));
            CMDs.Add(new CMD("String_getNG", "@010000*65"));
            CMDs.Add(new CMD("Data_sendTest", "@010100*66"));
            CMDs.Add(new CMD("Result_ngPBA", "@010200*67"));
            CMDs.Add(new CMD("Result_okPBA1", "@010201*68"));
            CMDs.Add(new CMD("Result_okPBA2", "@010210*77"));
            CMDs.Add(new CMD("Result_okPBA", "@010211*78"));
            CMDs.Add(new CMD("Data_sendQR", "@010300*68"));
            CMDs.Add(new CMD("Data_skipQR", "@010400*69"));
            CMDs.Add(new CMD("Data_enaQR", "@010401*70"));
            CMDs.Add(new CMD("Result_ngQR", "@010410*79"));
            CMDs.Add(new CMD("Result_okQR", "@010411*80"));
            //Inline machine mode
            CMDs.Add(new CMD("Mode_1Array", "@010501*71"));
            CMDs.Add(new CMD("Mode_2Array", "@010511*81"));
            //Off line machine mode
            CMDs.Add(new CMD("Mode_2PCB", "@010602*73"));
            CMDs.Add(new CMD("Mode_4PCB", "@010604*75"));
            CMDs.Add(new CMD("OK_PCB1", "@010301*69"));
            CMDs.Add(new CMD("OK_PCB2", "@010302*70"));
            CMDs.Add(new CMD("OK_PCB12", "@010303*71"));
            CMDs.Add(new CMD("OK_PCB3", "@010304*72"));
            CMDs.Add(new CMD("OK_PCB13", "@010305*73"));
            CMDs.Add(new CMD("OK_PCB23", "@010306*74"));
            CMDs.Add(new CMD("OK_PCB123", "@010307*75"));
            CMDs.Add(new CMD("OK_PCB4", "@010308*76"));
            CMDs.Add(new CMD("OK_PCB14", "@010309*77"));
            CMDs.Add(new CMD("OK_PCB24", "@010310*78"));
            CMDs.Add(new CMD("OK_PCB124", "@010311*79"));
            CMDs.Add(new CMD("OK_PCB34", "@010312*80"));
            CMDs.Add(new CMD("OK_PCB134", "@010313*81"));
            CMDs.Add(new CMD("OK_PCB235", "@010314*82"));
        }
    }


    public class ElnecSite
    {
        // const command control Elnec program
        //----- commands Server -> Client  -----
        public const string BRING_TO_FRONT = "bringtofront";   // code for "Bring to front" command
        public const string SHOWMAINFORM = "showmainform";   // code for "Show main form" command
        public const string HIDEMAINFORM = "hidemainform";   // code for "Hide main form" command
                                                             // command codes for device operations
        public const string BLANK_DEVICE = "blankcheck";     // code for "Blank check device"
        public const string READ_DEVICE = "readdevice";     // code for "Read device"
        public const string VERIFY_DEVICE = "verifydevice";   // code for "Verify device"
        public const string PROGRAM_DEVICE = "programdevice";  // code for "Program device"
        public const string ERASE_DEVICE = "erasedevice";    // code for "Erase device"
        public const string RUN_DEVICE_OP = "rundeviceop";    // code for "Run device operation"
        public const string STOP_OPERATION = "stopoperation";  // code for "Stop device operation"
        public const string CLOSE_APP = "closeapp";       // code for "close application"
                                                          // command codes for other commands
        public const string GET_PRG_STATUS = "getprogstatus";  // code for "Get program status"
        public const string SELECT_DEVICE = "selectdevice:";  // code for "Select device"
        public const string AUTSEL_EPRFLSH = "autoseldevice:"; // code for "Auto-Select device"
        public const string PROCESS_CMDL = "cmdlineparams:"; // code for "Process command line params"
                                                             //public const string  FIND_PGM       = "findpgm:";    // code for "Find programmer"
        public const string REFIND_PGM = "refindpgm";      // code for "Refind programmer"
        public const string SELFTEST = "selftest";       // code for "Self-Test of programmer"
        public const string SELFTESTPLUS = "selftestplus";   // code for "Self-Test Plus of programmer"
        public const string PROG_IS_BUSY = "programisbusy";  // code for "Program is busy" state command
        public const string CLIENT_READY_QUEST = "clienprogramisready"; //"client is ready" state command
                                                                        // command codes for file operations
        public const string LOAD_FILE = "loadfile:";      // code for "Load file"
        public const string SAVE_FILE = "savefile:";      // code for "Save file"
        public const string LOAD_PROJECT = "loadproject:";   // code for "Load project"
        public const string LOAD_PROJECT_WITH_PASSWORD = "loadprjpasswd:"; // code for "Load project" including password

        //public const string  ERASE_BUFFER   = "erasebuffer"; // code for "Erase buffer"
        public const string GETDEVCHECKSUM = "getdevchecksum"; // code for "Get device checksum request" command
        public const string SAVELOGTOFILE = "savelogtofile:"; // code for "Save Log to file" command

        // command codes for read/write buffer from/to remote client
        public const string RBUFFER = "readbuffer:";               // code for "Read buffer" command
        public const string WBUFFER = "writebuffer:";              // code for "Write buffer" command
        public const string RBUFFER_EX = "readbufferex:";          // code for "Read buffer Ex" command
        public const string WBUFFER_EX = "writebufferex:";         // code for "Write buffer Ex" command

        // codes for read/write buffer operation result
        // syntax is rbufferresult:<result>:<B0><B1><B2>...<Bn>
        // where <result> is result code with same values as for wbufferresult:<result>
        // <B0><B1><B2>...<Bn> - Bytes of read data
        public const string RBUFFER_RESULT = "readbufferresult:";  // code for "Read buffer" command
        public const string WBUFFER_RESULT = "writebufferresult:"; // code for "Write buffer" command
        public const string RWBUFFER_RES_GOOD = "good";                        // code for "good" result
        public const string RWBUFFER_RES_OUT_OF_RANGE = "outofrange";          // code for "address out of range" result
        public const string RWBUFFER_RES_SIZE_OUT_OF_RANGE = "sizeoutofrange"; // code for "address or size out of range" result
        public const string RWBUFFER_RES_ERR_PROTECTED = "protectedmodeact";   // code for "protected mode active" result

        public const string SAVELOGTOFILE_RESULT = "savelogtofileresult:";     // code for "Save Programmer activity log to file result" command

        public const string GET_PROJECT_FILE_CHECKSUM = "getprojectfilechecksum:";  //code for "Get project checksum" command  

        //--------------------------------------
        //----- commands Client -> Server  -----
        public const string OPTYPE = "optype";
        public const string PROGRESS = "progress";
        public const string LOG_LINE = "logline";         // code for "log line is comming"
        public const string INFO_LINE = "infoline";        // code for "info window line is comming"
        public const string INFO_LINE_2nd = "infoline2nd";     // code for "second info window line is comming"
        public const string CUR_DEVICE = "curdevice";       // code for "currently selected device name"

        public const string DEV_SERIAL_NUMBER = "devserialnumber "; // code for "serialization serial number of current device
        public const string DEV_MASTER_SMEM_SERIAL_NUMBER = "devmastersnum"; // code for "master serialization" serial number following on the next programming of device
                                                                             // Save file command result commands
        public const string SAVEFILE_RESULT = "savefileresult";    // code for "save file result"
        public const string SAVEFILE_OK = "ok";
        public const string SAVEFILE_ERR = "err";
        // Save file format types
        public const int FILEFORMAT_BINARY = 1;  // Binary file format
        public const int FILEFORMAT_INTELHEX = 2;  // IntelHex file format
        public const int FILEFORMAT_MOTOROLA = 3;  // Motorola file format
        public const int FILEFORMAT_ASCIISPACE = 4;  // ASCII Space file format
                                                     // codes for operation results
        public const string OPRESULT = "opresult";
        public const string OPRESULT_GOOD = "oprGood";
        public const string OPRESULT_FAIL = "oprFail";
        public const string OPRESULT_HWERR = "oprHWError";
        public const string OPRESULT_NONE = "oprNone";

        // Following command is used to receive more detailed device operation result statuses.
        // It is sent automaticaly from PG4UW to remote control application,
        // when device operation controled by PG4UW was finished.
        // Status values are defined by enumeration type TDetailedOpResultValues
        public const string DETAILED_OPRESULT = "detailedopresult";


        // codes for Load file/project result
        public const string LOAD_FILE_PRJ_RESULT = "loadresult";
        public const string FILE_LOAD_GOOD = "frgood";
        public const string FILE_LOAD_ERROR = "frerror";
        public const string FILE_LOAD_CANCELLED = "frcancelled";

        // codes for Select device result
        public const string SELECT_DEVICE_RESULT = "selectdeviceresult";
        public const string SELECT_DEVICE_GOOD = "good";
        public const string SELECT_DEVICE_ERROR = "error";

        // codes for Auto Select of EPROM/FLASH device
        public const string AUTSEL_EPRFLSH_RESULT = "autoseldeviceresult";

        // codes for server to client "ready" question
        public const string CLIENT_READY_ANSWER = "clienprogramisreadyanswer";
        public const string KEY_CLIENT_READY_YES = "isready";
        public const string KEY_CLIENT_READY_NO = "isnotready";

        public const string GET_PROGRAMMER_READY_STATUS = "getprogreadystatu";
        public const string PROGRAMMER_READY_STATUS = "programmerreadystatus";
        public const string KEY_PROGRAMMER_NOTFOUND = "notfound";
        public const string KEY_PROGRAMMER_READY = "ready";

        public const string GET_PROGRAMMER_NAME_AND_SN = "getprognameandsn";
        public const string GET_PROGRAMMER_NAME_AND_SN_RES = "prognameandsnres";

        // codes for command line params result
        public const string PROCESS_CMDL_RESULT = "cmdlineparamsresult";
        public const string PROCESS_CMDL_GOOD = "good";
        public const string PROCESS_CMDL_ERROR = "error";

        // return code for Get device checksum
        public const string GETDEVCHECKSUM_RESULT = "getdevchecksumresult";

        // similar to previous, but used by request function only
        public const string GETDEVCHECKSUM_REQ_RESULT = "getdevchecksumreqresult";

        // return code for Get special device checksum (e.g. PICmicro checksum
        // for some Microchip PIC devices)
        public const string GETDEVSPECIALCHECKSUM_RESULT = "getspecdevchecksumres";

        public const string GET_JOB_SUMMARY_REC = "getjobsummaryrecord";

        public const string GET_SITE_STATISTIC = "getsitestatistic";
        // return result of connected USB programmers found
        public const string SEARCH_USB_RESULT = "searchusb";

        public const string SEARCH_USB_CODE_DELIMITER = "~";

        public const string REFRESHTRAYICON = "refreshtrayicon";

        public const string AUTOYESMODE = "autoyes";

        public const string SOUNDMODE = "soundmode";

        public const string ALLOWSOUND_OK = "allowsoundok";
        public const string ALLOWSOUND_ERR = "allowsounderr";

        public const string CHECKSUMMODE = "checksummode";
        public const string CHECKSUMFORM = "checksumform";

        public const string SERVER_CAN_RUN_ANOTHER_SITE = "canrunanothersite";

        public const string REPETITIVEMODE = "repetmode";
        public const string REPMODE_OFF = "off";
        public const string REPMODE_ON = "on";
        public const string REPMODE_ON_ERR_STOP = "onerrstop";
        public const string REPMODE_FILLRAN = "fillran";
        public const string REPMODE_FILLRAN_ERR_STOP = "fillranerrstop";

        public const string PROTECTEDMODE = "pmode";

        public const string MSGBOX_CONNECT_ISP = "msgboxconnectisp";
        public const string ISP_MSGBOX_CLOSED = "msgispcntclosed";
        public const string MSGBOX_CONNECT_ISP_ANSWER = "msgboxconnectispanswer";



        // status const 
        public const string STATUS_READY = "ready";
        public const string STATUS_BUSY = "busy";
        public const string STATUS_PROCESSING = "processing";

        public const string RESULT_OK = "OK";
        public const string RESULT_NG = "FAIL";
        public const string EMPTY = "NONE";

        public int TCP_TimeOut = 1000;

        // simple params at one site
        public byte Name { get; }
        public string Address { get; set; }
        public string Command { get; set; }
        public string Response { get; set; }
        public string CheckSum { get; set; }
        public string Status { get; set; }
        public string ProjectPath { get; set; }
        public string Result { get; set; }
        public string History { get; set; }


        public string SITE_OPTYPE { get; set; }
        public string SITE_PROGRESS { get; set; }
        public string SITE_PRGREADYSTATUS { get; set; }
        public string SITE_DETAILE { get; set; } = "";
        public string SITE_OPRESULT { get; set; }
        public string SITE_LOADPRJRESULT { get; set; }
        public string SITE_PROGRAMRESULT { get; set; }

        public int progressValue { get; set; } = 0;

        public WorkProcess WorkProcess = new WorkProcess();

        public ElnecSite(byte Name)
        {
            this.Name = Name;
            this.Address = "";
            this.CheckSum = "00000000h";
            this.Status = STATUS_BUSY;
            this.Result = EMPTY;
            this.Response = "";
        }

        public void ClearSiteParam()
        {
            this.Result = EMPTY;
            this.SITE_OPTYPE = "";
            this.SITE_PROGRESS = "";
            this.SITE_PRGREADYSTATUS = "";
            this.SITE_DETAILE = "";
            this.SITE_OPRESULT = "";
            this.SITE_LOADPRJRESULT = "";
            this.SITE_PROGRAMRESULT = EMPTY;
        }

        public void OpenSite(string Address, string RemoteIP, int RemotePort)
        {
            string strCmdText = "/c --quiet & pg4uw #" + this.Name.ToString() + " /usb:" + this.Name.ToString() + ":" + Address.ToString() + " /enableremote:autoanswer /remoteport:" + RemotePort.ToString() + " /remoteaddr:" + RemoteIP;
            Console.WriteLine(strCmdText);
            ProcessStartInfo procStartInfo = new ProcessStartInfo("cmd.exe")
            {
                ErrorDialog = false,
                WorkingDirectory = @"C:\\Program Files (x86)\\Elnec_sw\\Programmer",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = strCmdText
            };

            ///command contains the command to be executed in cmd
            Process proc = new Process
            {
                StartInfo = procStartInfo
            };
            proc.Start();
        }

        public void AddToHistory(string NewLineHistory)
        {
            this.History += NewLineHistory + Environment.NewLine;
        }

    }

    class PCB_Model
    {
        public string PROGRAM_PATH = @"C:\Auto Micom Writing\AMW Programs\";
        public string PCBcode;
        public string ModelPath;
        public string ModelName;
        public string Version;


        public int TimeOut = 30;

        public Layout Layout = new Layout();

        public SaveFileDialog saveFileDialog = new SaveFileDialog();

        public ROM[] ROMs = {   new ROM(),
                                new ROM(),
                                new ROM(),
                                new ROM()};

        public PCB_Model()
        {
        }
        public void save(object sender, System.EventArgs e)
        {
            string configModel =
                  this.ModelName + Environment.NewLine
                + this.ROMs[0].ROM_PATH + "@" + this.ROMs[0].ROM_CHECKSUM + Environment.NewLine
                + this.ROMs[1].ROM_PATH + "@" + this.ROMs[1].ROM_CHECKSUM + Environment.NewLine
                + this.ROMs[2].ROM_PATH + "@" + this.ROMs[2].ROM_CHECKSUM + Environment.NewLine
                + this.ROMs[3].ROM_PATH + "@" + this.ROMs[3].ROM_CHECKSUM + Environment.NewLine
                + this.Layout.PCB1.ToString() + Environment.NewLine
                + this.Layout.PCB2.ToString() + Environment.NewLine
                + this.Layout.ArrayCount.ToString() + Environment.NewLine
                + this.Layout.XasixArrayCount.ToString() + Environment.NewLine
                + this.Layout.MicomNumber.ToString() + Environment.NewLine
                + this.TimeOut.ToString() + Environment.NewLine;

            saveFileDialog.InitialDirectory = ModelPath;
            File.WriteAllText(saveFileDialog.FileName, configModel);
        }
        public void saveUpdate(string path)
        {
            string configModel =
                  this.ModelName + Environment.NewLine
                + this.ROMs[0].ROM_PATH + "@" + this.ROMs[0].ROM_CHECKSUM + Environment.NewLine
                + this.ROMs[1].ROM_PATH + "@" + this.ROMs[1].ROM_CHECKSUM + Environment.NewLine
                + this.ROMs[2].ROM_PATH + "@" + this.ROMs[2].ROM_CHECKSUM + Environment.NewLine
                + this.ROMs[3].ROM_PATH + "@" + this.ROMs[3].ROM_CHECKSUM + Environment.NewLine
                + this.Layout.PCB1.ToString() + Environment.NewLine
                + this.Layout.PCB2.ToString() + Environment.NewLine
                + this.Layout.ArrayCount.ToString() + Environment.NewLine
                + this.Layout.XasixArrayCount.ToString() + Environment.NewLine
                + this.Layout.MicomNumber.ToString() + Environment.NewLine
                + this.TimeOut.ToString() + Environment.NewLine;

            File.WriteAllText(path, configModel);
        }
        public void SaveAsNew(string Directory)
        {
            this.saveFileDialog.InitialDirectory = Directory;
            this.saveFileDialog.DefaultExt = "a_ms";
            this.saveFileDialog.FileOk += new CancelEventHandler(save);
            this.saveFileDialog.FileName = this.ModelName;
            this.saveFileDialog.ShowDialog();

        }
    }

    class Layout
    {
        public bool PCB1;
        public bool PCB2;
        public int ArrayCount, XasixArrayCount;
        public int MicomNumber = 1;

        public string[] Name = new string[] { "A", "B", "C", "D" };

        public Layout()
        {
            this.PCB1 = true;
            this.PCB2 = true;
            this.ArrayCount = 2;
            this.XasixArrayCount = 1;
            this.MicomNumber = 1;
        }
        public void drawPCBLayout(PictureBox pbPCBLayout)
        {

            if (MicomNumber == 2)
            {
                Name = new string[] { "A C", "B D" };
            }
            else
            {
                Name = new string[] { "A", "B", "C", "D" };
            }

            int x = pbPCBLayout.Size.Width;
            int y = pbPCBLayout.Size.Height;

            int x1 = 0, x2 = 0;
            int y1, y2;

            y1 = y / (this.ArrayCount / this.XasixArrayCount);
            y2 = y / (this.ArrayCount / this.XasixArrayCount);

            if (this.PCB1 && this.PCB2)
            {
                x1 = x / 2;
                x2 = x / 2;
            }
            if (this.PCB1 && !this.PCB2)
            {
                x1 = x;
            }
            if (!this.PCB1 && this.PCB2)
            {
                x2 = x;
            }
            if (x > 50 && y > 50)
            {
                int nameCounter = 0;
                if (PCB1 || PCB2)
                {
                    nameCounter = this.ArrayCount - 1;
                }

                if (PCB1 && PCB2)
                {
                    nameCounter = 2 * this.ArrayCount - 1;
                }

                Bitmap custormChart = new Bitmap(x, y);
                Graphics g = Graphics.FromImage(custormChart);

                SolidBrush[] brush = { new SolidBrush(Color.FromArgb(93, 106, 104)), new SolidBrush(Color.FromArgb(138, 108, 137)) };
                SolidBrush brush_char = new SolidBrush(Color.White);

                int charHeight = y1 / 2;
                if (x / (2 * this.XasixArrayCount) < y1)
                    charHeight = x / (4 * this.XasixArrayCount);

                Font nameFont = new Font("Microsoft YaHei UI", charHeight, FontStyle.Bold);

                for (int j = this.ArrayCount / this.XasixArrayCount; j >= 1; j--)
                {

                    if (PCB2)
                    {
                        for (int i = 1; i <= this.XasixArrayCount; ++i)
                        {
                            g.FillRectangle(brush[0], (i - 1) * (x2 / this.XasixArrayCount), (j - 1) * y2, x2 / this.XasixArrayCount - 3, y2 - 3);
                            g.DrawString(this.Name[nameCounter], nameFont, brush_char, (x2 / (2 * this.XasixArrayCount)) + (i - 1) * (x2 / this.XasixArrayCount) - 3 - this.Name[nameCounter].Length * charHeight / (float)1.8, (2 * j - 1) * (y2 / 2) - charHeight);
                            if (nameCounter > 0) nameCounter--;
                        }
                    }
                    if (PCB1)
                    {
                        for (int i = 1; i <= this.XasixArrayCount; ++i)
                        {
                            g.FillRectangle(brush[1], (i - 1) * (3 + x1 / this.XasixArrayCount) + x2, (j - 1) * y1, x1 / this.XasixArrayCount, y1 - 3);
                            g.DrawString(this.Name[nameCounter], nameFont, brush_char, x2 + (x1 / (2 * this.XasixArrayCount)) + (i - 1) * (x1 / this.XasixArrayCount) - this.Name[nameCounter].Length * charHeight / (float)1.8, (2 * j - 1) * (y1 / 2) - charHeight);
                            if (nameCounter > 0) nameCounter--;
                        }
                    }
                }

                if (pbPCBLayout.Image != null)
                    pbPCBLayout.Image.Dispose();

                pbPCBLayout.Image = custormChart;
                brush[0].Dispose();
                g.Dispose();
            }
        }

        public void drawPCBLayout(PictureBox pbPCBLayout, bool OfflineMachine)
        {
            if (MicomNumber == 2)
            {
                Name = new string[] { "A C", "B D" };
            }
            else
            {
                Name = new string[] { "A", "B", "C", "D" };
            }

            int x = pbPCBLayout.Size.Width;
            int y = pbPCBLayout.Size.Height;

            int x1 = 0, x2 = 0;
            int y1, y2;

            y1 = y / (this.ArrayCount / this.XasixArrayCount);
            y2 = y / (this.ArrayCount / this.XasixArrayCount);

            x1 = x;

            if (x > 50 && y > 50)
            {
                int nameCounter = 0;
                //if (PCB1 || PCB2)
                //{
                //    nameCounter = this.ArrayCount - 1;
                //}

                //if (PCB1 && PCB2)
                //{
                //    nameCounter = 2 * this.ArrayCount - 1;
                //}

                Bitmap custormChart = new Bitmap(x, y);
                Graphics g = Graphics.FromImage(custormChart);

                SolidBrush[] brush = { new SolidBrush(Color.FromArgb(93, 106, 104)), new SolidBrush(Color.FromArgb(138, 108, 137)) };
                SolidBrush brush_char = new SolidBrush(Color.White);

                int charHeight = y1 / 2;
                if (x / (2 * this.XasixArrayCount) < y1)
                    charHeight = x / (4 * this.XasixArrayCount);

                Font nameFont = new Font("Microsoft YaHei UI", charHeight, FontStyle.Bold);

                for (int j = 1; j <= this.ArrayCount / this.XasixArrayCount; j++)
                {
                    if (PCB1)
                    {
                        for (int i = 1; i <= this.XasixArrayCount; ++i)
                        {
                            g.FillRectangle(brush[1], (i - 1) * (3 + x1 / this.XasixArrayCount) + x2, (j - 1) * y1, x1 / this.XasixArrayCount, y1 - 3);
                            g.DrawString(this.Name[nameCounter],
                                nameFont,
                                brush_char,
                                x2 + (x1 / (2 * this.XasixArrayCount)) + (i - 1) * (x1 / this.XasixArrayCount) - this.Name[nameCounter].Length * charHeight / (float)1.8,
                                (2 * j - 1) * (y1 / 2) - charHeight);
                            if (nameCounter < 2 * this.ArrayCount - 1) nameCounter++;
                        }
                    }
                }

                if (pbPCBLayout.Image != null)
                    pbPCBLayout.Image.Dispose();

                pbPCBLayout.Image = custormChart;
                brush[0].Dispose();
                g.Dispose();
            }
        }
    }

    class ROM
    {
        public string ROM_PATH = "";
        public string ROM_CHECKSUM = "";
        public string ROM_VERSTION = "";

        public ROM() { }
    }


    public class AMW_CONFIG
    {
        public string recentModelPath = @"C:\Auto Micom Writing\AMW Programs\";
        public string recentWorkPath = @"C:\Auto Micom Writing\AMW Programs\";
        public string reportPath = @"C:\Auto Micom Writing\AMW Report\";
        public string configPath = @"C:\Auto Micom Writing\AMW\";
        public string modelPath = @"C:\Auto Micom Writing\AMW Programs\";
        public string MESpath = @"C:\Auto Micom Writing\MES\";

        public string defaulComPort = "COM 1";
        public int defaulBaudrate = 9600;

        public int ElnecStrAddress = 1180;
        public int ElnecAddress = 11227;

        public string RemoteIP = "127.0.0.1";
        public int RemotePort = 21;

        public string ADMIN_ACC = "admin";
        public string ADMIN_PASS = "123456";

        public string MANAGER_ACC = "manager";
        public string MANAGER_PASS = "123654789";

        public bool ServerCompare = false;
        public bool InlineMachine = true;
        public string Line = "";

        private string ReportBuffer = "";
        public AMW_CONFIG()
        {
            if (!Directory.Exists(modelPath)) Directory.CreateDirectory(modelPath);
            if (!Directory.Exists(reportPath)) Directory.CreateDirectory(reportPath);
            if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);

            if (!File.Exists(configPath + "config.cfg"))
            {
                string config =
                    "recentModePath@" + this.recentModelPath + Environment.NewLine
                  + "recentWorkPath@" + this.recentWorkPath + Environment.NewLine
                  + "reportPath@" + this.reportPath + Environment.NewLine
                  + "defautComPort@" + this.defaulComPort + Environment.NewLine
                  + "defaultBaudrate@" + this.defaulBaudrate.ToString() + Environment.NewLine
                  + "defaultADMIN_ACC@" + this.ADMIN_ACC + Environment.NewLine
                  + "defaultADMIN_PASS@" + this.ADMIN_PASS + Environment.NewLine
                  + "defaultMANAGER_ACC@" + this.MANAGER_ACC + Environment.NewLine
                  + "defaultMANAGER_PASS@" + this.MANAGER_PASS + Environment.NewLine
                  + "ElnecAddress@" + this.ElnecAddress + Environment.NewLine
                  + "ElnecStrAddress@" + this.ElnecStrAddress + Environment.NewLine
                  + "ServerCompare@" + this.ServerCompare + Environment.NewLine
                  + "InlineMachine@" + this.InlineMachine + Environment.NewLine
                  + "RemoteIP@" + this.RemoteIP + Environment.NewLine
                  + "RemotePort@" + this.RemotePort + Environment.NewLine
                  + "Line@" + this.Line + Environment.NewLine;
                File.WriteAllText(configPath + "config.cfg", config);
            }
            else
            {
                string[] config = File.ReadAllLines(configPath + "config.cfg");
                for (int i = 0; i < config.Length; i++)
                {
                    string[] configData = config[i].Split('@');
                    Console.WriteLine(configData[1]);
                    switch (configData[0])
                    {
                        case "recentModePath":
                            this.recentModelPath = configData[1];
                            break;
                        case "recentWorkPath":
                            this.recentWorkPath = configData[1];
                            break;
                        case "reportPath":
                            this.reportPath = configData[1];
                            break;
                        case "defaultComPort":
                            this.defaulComPort = configData[1];
                            break;
                        case "defaultBaudrate":
                            this.defaulBaudrate = Convert.ToInt32(configData[1]);
                            break;
                        case "defaultADMIN_ACC":
                            this.ADMIN_ACC = configData[1];
                            break;
                        case "defaultADMIN_PASS":
                            this.ADMIN_PASS = configData[1];
                            break;
                        case "defaultMANAGER_ACC":
                            this.MANAGER_ACC = configData[1];
                            break;
                        case "defaultMANAGER_PASS":
                            this.MANAGER_PASS = configData[1];
                            break;
                        case "ElnecStrAddress":
                            this.ElnecStrAddress = Convert.ToInt32(configData[1]);
                            break;
                        case "ElnecAddress":
                            this.ElnecAddress = Convert.ToInt32(configData[1]);
                            break;
                        case "ServerCompare":
                            this.ServerCompare = configData[1].Contains("True");
                            break;
                        case "InlineMachine":
                            this.InlineMachine = configData[1].Contains("True");
                            break;
                        case "RemoteIP":
                            this.RemoteIP = configData[1];
                            break;
                        case "RemotePort":
                            this.RemotePort = Convert.ToInt32(configData[1]);
                            break;
                        case "Line":
                            this.Line = configData[1];
                            break;
                    }
                }
            }
        }

        public void SaveConfig()
        {
            if (!Directory.Exists(this.configPath)) Directory.CreateDirectory(this.configPath);
            if (File.Exists(this.configPath + "config.cfg"))
            {
                string config =
                "recentModePath@" + this.recentModelPath + Environment.NewLine
                + "recentWorkPath@" + this.recentWorkPath + Environment.NewLine
                + "reportPath@" + this.reportPath + Environment.NewLine
                + "defautComPort@" + this.defaulComPort + Environment.NewLine
                + "defaultBaudrate@" + this.defaulBaudrate.ToString() + Environment.NewLine
                + "defaultADMIN_ACC@" + this.ADMIN_ACC + Environment.NewLine
                + "defaultADMIN_PASS@" + this.ADMIN_PASS + Environment.NewLine
                + "defaultMANAGER_ACC@" + this.MANAGER_ACC + Environment.NewLine
                + "defaultMANAGER_PASS@" + this.MANAGER_PASS + Environment.NewLine
                + "ElnecAddress@" + this.ElnecAddress + Environment.NewLine
                + "ElnecStrAddress@" + this.ElnecStrAddress + Environment.NewLine
                + "ServerCompare@" + this.ServerCompare + Environment.NewLine
                + "InlineMachine@" + this.InlineMachine + Environment.NewLine
                + "RemoteIP@" + this.RemoteIP + Environment.NewLine
                + "RemotePort@" + this.RemotePort + Environment.NewLine
                + "Line@" + this.Line + Environment.NewLine;
                File.WriteAllText(configPath + "config.cfg", config);
            }
        }


        public void reportWriteLine(string now, string model, string Result, string site1Result, string site2Result, string site3Result, string site4Result)
        {
            string path = this.reportPath;
            string moment = now;
            string today_txt = "Report-" + DateTime.Now.ToString("yyyy-MM-dd");

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            if (File.Exists(path + today_txt + ".txt")) // Nếu file lịch sử tồn tại thì lưu thông tin vào
            {
                try
                {
                    using (StreamWriter sw = File.AppendText(path + today_txt + ".txt"))
                    {
                        string reportData = "";
                        if (this.ReportBuffer != "")
                            reportData = this.ReportBuffer;
                        reportData += "L" + "|" + Result + "|" + model + "|" + "not user" + "|" + moment + "|" + site1Result + "|" + site2Result + "|" + site3Result + "|" + site4Result;
                        sw.WriteLine(reportData);
                        this.ReportBuffer = "";
                    }
                }
                catch (Exception)
                {
                    this.ReportBuffer = "L" + "1" + "|" + Result + "|" + model + "|" + "not user" + "|" + moment + "|" + site1Result + "|" + site2Result + "|" + site3Result + "|" + site4Result + Environment.NewLine;
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(path + today_txt + ".txt"))
                {
                    string reportData = "STT|" + "Final result|" + "Model " + "|" + "Bar code " + "|" + "Time" + "|" + "Site 1" + "|" + "Site 2" + "|" + "Site 3" + "|" + "Site 4" + "\n";
                    reportData += "L" + "1" + "|" + Result + "|" + model + "|" + "not user" + "|" + moment + "|" + site1Result + "|" + site2Result + "|" + site3Result + "|" + site4Result;
                    sw.WriteLine(reportData);
                }
            }
        }
    }

    public class WorkProcess
    {
        public const int Site1_OK = 1;
        public const int Site2_OK = 1;
        public const int Site3_OK = 1;
        public const int Site4_OK = 1;

        public const int Ready = 0;
        public const int Start = 1;
        public const int Stop = 2;
        public const int Processing = 3;

        public int Statitis_OK;
        public int Statitis_NG;

        public int WorkingSite = 0;

        public int Process = 0;
        public bool Interrup = false;

        public string[] ComandQueue = new string[100];

        public WorkProcess()
        {
            for (int i = 0; i < this.ComandQueue.Length; i++)
            {
                this.ComandQueue[i] = "null";
            }
        }

        public string GetCommandFIFO()
        {
            string comandOldest = this.ComandQueue[0];

            for (int i = 0; i < this.ComandQueue.Length - 1; i++)
            {
                this.ComandQueue[i] = this.ComandQueue[i + 1];
            }
            this.ComandQueue[ComandQueue.Length - 1] = "null";
            return comandOldest;
        }

        public int GetSlotCommandAvailble()
        {
            int slotAvailble = 0;
            for (int i = 0; i < this.ComandQueue.Length - 1; i++)
            {
                if (this.ComandQueue[i] == "null")
                {
                    slotAvailble = this.ComandQueue.Length - i - 1;
                    break;
                }
            }
            return slotAvailble;
        }
        public int PutComandToFIFO(string Comand)
        {
            if (this.GetSlotCommandAvailble() > 0)
            {
                this.ComandQueue[this.ComandQueue.Length - this.GetSlotCommandAvailble() - 1] = Comand;
                return this.GetSlotCommandAvailble();
            }
            else
                return -1;
        }

        public void PushComandToFist(string Command)
        {
            for (int i = this.ComandQueue.Length - 1; i >= 1; i--)
            {
                this.ComandQueue[i] = this.ComandQueue[i - 1];
            }
            this.ComandQueue[0] = Command;
        }

        public void ClearCMDQueue()
        {
            for (int i = 0; i < this.ComandQueue.Length - 1; i++)
            {
                this.ComandQueue[i] = "null";
            }
        }
    }

}
