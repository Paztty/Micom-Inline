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

namespace Micom_Inline
{
    public partial class Main : Form
    {
        // Permissions 
        public const string OP = "OP";
        public const string TECH = "TECH";
        public const string MANAGER = "MANAGER";

        public static string Permissions = OP;

        // const color
        public Color activeColor = Color.FromArgb(30, 136, 221);
        public Color nonactiveColor = Color.FromArgb(62, 62, 62);
        public Color deactiveColor = Color.Green;
        public Color busyColor = Color.DarkGray;

        public Color fistROMsellect = Color.FromArgb(30, 136, 221);
        public Color secondROMsellect = Color.Blue;
        public Color thirdROMsellect = Color.OliveDrab;
        public Color forthROMsellect = Color.Brown;


        const string SERVER_ON = "severon";
        const string SERVER_OFF = "severoff";
        public string ServerStatus = SERVER_ON;

        public int ROMsellectCounter = 0;


        ElnecSite Site1 = new ElnecSite(1);
        ElnecSite Site2 = new ElnecSite(2);
        ElnecSite Site3 = new ElnecSite(3);
        ElnecSite Site4 = new ElnecSite(4);

        public string RemoteIP = "127.0.0.1";
        public int RemotePort = 8881;
        public int ElnecAddress = 11227;
        public int TCP_TimeOut = 3000;

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


        public int CharCircle = 1;


        public string PortReciver = string.Empty;

        PCB_Model model = new PCB_Model
        {
            ModelName = "",
            PCBcode = "",
        };

        DateTime lastWorkingTime = new DateTime();

        public static AMW_CONFIG _CONFIG = new AMW_CONFIG();

        WorkProcess AMWsProcess = new WorkProcess();

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

        /// <summary>
        /// start main function
        /// </summary>
        public Main()
        {
            InitializeComponent();

            this.SetStyle(ControlStyles.Selectable, false);

            lbROM1checkSum.AutoSize = false;
            lbROM1checkSum.Size = new System.Drawing.Size(122, 24);
            lbROM2checkSum.AutoSize = false;
            lbROM2checkSum.Size = new System.Drawing.Size(122, 24);
            lbROM3checkSum.AutoSize = false;
            lbROM3checkSum.Size = new System.Drawing.Size(122, 24);
            lbROM4checkSum.AutoSize = false;
            lbROM4checkSum.Size = new System.Drawing.Size(122, 24);

            Port.DataReceived += new SerialDataReceivedEventHandler(DataReciver);

            pnLogin.Visible = false;
            tbHistory.AppendText("<<<<<< AUTO MICOM WRITING SYSTEM >>>>>>" + Environment.NewLine);
            //Permissions = OP;
            tsslPermissions.Text = "User: " + Permissions;

            gbLog.Visible = true;
            gbTestHistory.Visible = false;
            gbSetting.Visible = false;

            lastWorkingTime = DateTime.Now;
            timerUpdateChar.Start();
            checkPermision();
            SkipBarCode();

            Site1.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
            Site2.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
            Site3.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
            Site4.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);

            Site1.WorkProcess.Process = WorkProcess.Ready;
            Site2.WorkProcess.Process = WorkProcess.Ready;
            Site3.WorkProcess.Process = WorkProcess.Ready;
            Site4.WorkProcess.Process = WorkProcess.Ready;

            try
            {
                Port.PortName = SearchCom();
                tsslbCOM.Text = Port.PortName + "                ";
                Port.Open();
            }
            catch (Exception) { }
        }

        public void DateTimeShow()
        {
            while (true)
            {
                string t = DateTime.Now.ToString();
                TimeSpan lostTime = DateTime.Now.Subtract(lastWorkingTime);
                lbClock.Invoke(new MethodInvoker(delegate
                {
                    lbClock.Text = t;
                    if ((Site1.WorkProcess.Process == WorkProcess.Ready && Site2.WorkProcess.Process == WorkProcess.Ready) || (Site3.WorkProcess.Process == WorkProcess.Ready && Site4.WorkProcess.Process == WorkProcess.Ready))
                        lbFreeTime.Text = "Lost time: " + lostTime.TotalSeconds.ToString("f0") + " S";
                }));
                Thread.Sleep(1000);
            }

        }


        private void Main_Load(object sender, EventArgs e)
        {
            DgwTestMode_Init();
            DrawChart(AMWsProcess.Statitis_OK, AMWsProcess.Statitis_NG, CharCircle);
            Thread showTime = new Thread(DateTimeShow);
            showTime.Start();
            timerReleaseBoard.Interval = 2500;
            timerReleaseBoard.Start();
            Thread communicationSite1 = new Thread(ElnecComuncationBackgroudSite1);
            communicationSite1.Start();
            Thread communicationSite2 = new Thread(ElnecComuncationBackgroudSite2);
            communicationSite2.Start();
            Thread communicationSite3 = new Thread(ElnecComuncationBackgroudSite3);
            communicationSite3.Start();
            Thread communicationSite4 = new Thread(ElnecComuncationBackgroudSite4);
            communicationSite4.Start();
        }

        // Serial reciver
        private void DataReciver(object obj, SerialDataReceivedEventArgs e)
        {
            if (!Port.IsOpen) return;
            string Frame = " ";
            try
            {
                Frame = Port.ReadLine();
                Console.WriteLine(Frame);

                if (Frame.Contains(Data_sendTest))
                {
                    Port.Write(String_getOK);

                    if (lbAutoManual.Text == "Auto mode")
                    {
                        resetdgwTestMode();
                        Site1.WorkProcess.ClearCMDQueue();
                        Site2.WorkProcess.ClearCMDQueue();
                        Site3.WorkProcess.ClearCMDQueue();
                        Site4.WorkProcess.ClearCMDQueue();

                        Site1.WorkProcess.Process = WorkProcess.Interrup;
                        Site2.WorkProcess.Process = WorkProcess.Interrup;
                        Site3.WorkProcess.Process = WorkProcess.Interrup;
                        Site4.WorkProcess.Process = WorkProcess.Interrup;

                        Site1.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
                        Site2.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
                        Site3.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
                        Site4.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);

                        highlinedgwTestMode(0);

                        ActiveLabel(lbResultA);
                        ActiveLabel(lbResultB);
                        ActiveLabel(lbResultC);
                        ActiveLabel(lbResultD);

                        lbMachineStatus.Invoke(new MethodInvoker(delegate { lbMachineStatus.Text = "WRITTING"; lbMachineStatus.BackColor = activeColor; }));

                        Site1.SITE_PROGRAMRESULT = ElnecSite.EMPTY;
                        Site2.SITE_PROGRAMRESULT = ElnecSite.EMPTY;
                        Site3.SITE_PROGRAMRESULT = ElnecSite.EMPTY;
                        Site4.SITE_PROGRAMRESULT = ElnecSite.EMPTY;
                    }

                }
                else if (Frame.Contains(Data_sendQR))
                {
                    Port.Write(String_getOK);
                    Thread.Sleep(500);
                    Port.Write(Result_okQR);
                }
                else if (Frame.Contains(String_getNG))
                {
                    Port.Write(ResultRespoonse);
                    Console.WriteLine(ResultRespoonse);
                }
                else if (Frame.Contains(String_getOK))
                {
                    ResultRespoonse = "";
                }
            }
            catch (Exception) { }

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
            _CONFIG.SaveConfig();

            CloseElnec();
            Environment.Exit(0);
            this.Close();
            Application.Exit();
        }

        private void BtnMaximize_Click(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
                btnMaximize.BackgroundImage = Resources.masinize;
            }
            else
            {
                //this.MaximumSize = new System.Drawing.Size(Screen.PrimaryScreen.WorkingArea.Width + 20  , Screen.PrimaryScreen.WorkingArea.Height + 17);
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
        }

        private void Main_Resize(object sender, System.EventArgs e)
        {
            DrawChart(AMWsProcess.Statitis_OK, AMWsProcess.Statitis_NG, 360);
            model.Layout.drawPCBLayout(pbPCBLayout);
        }



        private void BtLoadModel_Click(object sender, EventArgs e)
        {

            btLoadModel.BackColor = Color.FromArgb(50, 50, 50);
            btAuto.BackColor = Color.FromArgb(30, 30, 30);
            btManual.BackColor = Color.FromArgb(30, 30, 30);
            btReportFolder.BackColor = Color.FromArgb(30, 30, 30);
            btSetting.BackColor = Color.FromArgb(30, 30, 30);
            btDataLog.BackColor = Color.FromArgb(30, 30, 30);

            OPForm form = new OPForm();
            form.ShowDialog();

            openFileModel.InitialDirectory = _CONFIG.recentModelPath;
            openFileModel.ShowDialog();
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
                    if (queryObj["Caption"].ToString().Contains("CH340") || queryObj["Caption"].ToString().Contains("Arduino") || queryObj["Caption"].ToString().Contains("Serial"))
                    {
                        ComName = queryObj["Caption"].ToString();
                        Console.WriteLine(ComName);
                        ComName = ComName.Substring(ComName.LastIndexOf('(') + 1, ComName.LastIndexOf(')') - 1 - ComName.LastIndexOf('('));
                        Console.WriteLine(ComName);
                        break;
                    }
                }
            }
            return ComName;
        }
        private void BtAuto_Click(object sender,
                                  EventArgs e)
        {
            btAuto.BackColor = Color.FromArgb(50, 50, 50);
            btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
            btManual.BackColor = Color.FromArgb(30, 30, 30);
            btReportFolder.BackColor = Color.FromArgb(30, 30, 30);
            btSetting.BackColor = Color.FromArgb(30, 30, 30);
            btDataLog.BackColor = Color.FromArgb(30, 30, 30);

            if (lbAutoManual.Text == "Auto mode")
            {
                lbAutoManual.Text = "IDE";
                lbAutoManual.ForeColor = Color.Black;
            }
            else
            {
                try
                {
                    Port.PortName = SearchCom();
                    tsslbCOM.Text = Port.PortName + "                ";
                    if (!Port.IsOpen) Port.Open();
                }
                catch (Exception) { }

                lbAutoManual.Text = "Auto mode";
                lbAutoManual.ForeColor = activeColor;
                try
                {


                    btSite1Open.Text = "not used";
                    btSite2Open.Text = "not used";
                    btSite3Open.Text = "not used";
                    btSite4Open.Text = "not used";

                }
                catch (Exception) { }
            }


        }

        private void BtManual_Click(object sender, EventArgs e)
        {
            btManual.BackColor = Color.FromArgb(50, 50, 50);
            btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
            btAuto.BackColor = Color.FromArgb(30, 30, 30);
            btReportFolder.BackColor = Color.FromArgb(30, 30, 30);
            btSetting.BackColor = Color.FromArgb(30, 30, 30);
            btDataLog.BackColor = Color.FromArgb(30, 30, 30);

            Port.Close();

            lbAutoManual.Text = "Manual mode";
            lbAutoManual.ForeColor = deactiveColor;

            Site1.WorkProcess.ClearCMDQueue();
            Site2.WorkProcess.ClearCMDQueue();
            Site3.WorkProcess.ClearCMDQueue();
            Site4.WorkProcess.ClearCMDQueue();

            btSite1Open.Text = "PROGRAM";
            btSite2Open.Text = "PROGRAM";
            btSite3Open.Text = "PROGRAM";
            btSite4Open.Text = "PROGRAM";
        }

        private void BtReportFolder_Click(object sender, EventArgs e)
        {
            btReportFolder.BackColor = Color.FromArgb(50, 50, 50);
            btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
            btAuto.BackColor = Color.FromArgb(30, 30, 30);
            btManual.BackColor = Color.FromArgb(30, 30, 30);
            btSetting.BackColor = Color.FromArgb(30, 30, 30);
            btDataLog.BackColor = Color.FromArgb(30, 30, 30);

            Form form = new Report();
            form.ShowDialog();

        }

        private void BtSetting_Click(object sender, EventArgs e)
        {

            btSetting.BackColor = Color.FromArgb(50, 50, 50);
            btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
            btAuto.BackColor = Color.FromArgb(30, 30, 30);
            btManual.BackColor = Color.FromArgb(30, 30, 30);
            btReportFolder.BackColor = Color.FromArgb(30, 30, 30);
            btDataLog.BackColor = Color.FromArgb(30, 30, 30);


            if (gbSetting.Visible == false)
                gbSetting.Visible = true;
            else
                gbSetting.Visible = false;





            PCBarrayCount.Value = model.Layout.ArrayCount;
            nbUDXarrayCount.Value = model.Layout.XasixArrayCount;
            radioButton1.Checked = model.Layout.PCB1;
            radioButton2.Checked = model.Layout.PCB2;
            model.Layout.drawPCBLayout(pbPCBLayout);

        }

        private void BtDataLog_Click(object sender, EventArgs e)
        {
            btDataLog.BackColor = Color.FromArgb(50, 50, 50);
            btLoadModel.BackColor = Color.FromArgb(30, 30, 30);
            btAuto.BackColor = Color.FromArgb(30, 30, 30);
            btManual.BackColor = Color.FromArgb(30, 30, 30);
            btReportFolder.BackColor = Color.FromArgb(30, 30, 30);
            btSetting.BackColor = Color.FromArgb(30, 30, 30);

            model.ROMs[0].ROM_CHECKSUM = lbROM1checkSum.Text;
            model.ROMs[1].ROM_CHECKSUM = lbROM2checkSum.Text;
            model.ROMs[2].ROM_CHECKSUM = lbROM3checkSum.Text;
            model.ROMs[3].ROM_CHECKSUM = lbROM4checkSum.Text;

            model.SaveAsNew();
            _CONFIG.recentModelPath = model.ModelPath;
            _CONFIG.SaveConfig();
            model.saveFileDialog.InitialDirectory = _CONFIG.recentModelPath;

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
            if (Site1.Result != ElnecSite.EMPTY && Site2.Result != ElnecSite.EMPTY && Site3.Result != ElnecSite.EMPTY && Site4.Result != ElnecSite.EMPTY)
            {
                if (Site1.Result == ElnecSite.RESULT_OK && Site2.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                //if (Site1.Result == ElnecSite.RESULT_OK && Site2.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                {
                    AMWsProcess.Statitis_OK += 2;
                    lbMachineStatus.Invoke(new MethodInvoker(delegate { lbMachineStatus.Text = "OK"; lbMachineStatus.BackColor = Color.Green; }));
                    ResultRespoonse = Result_okPBA;
                }
                else if ((Site1.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK) && (Site2.Result == ElnecSite.RESULT_NG || Site4.Result == ElnecSite.RESULT_NG))
                    ResultRespoonse = Result_okPBA1;
                else if ((Site1.Result == ElnecSite.RESULT_NG || Site3.Result == ElnecSite.RESULT_NG) && (Site2.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK))
                    ResultRespoonse = Result_okPBA2;
                else if (Site1.Result == ElnecSite.RESULT_NG && Site2.Result == ElnecSite.RESULT_NG && Site3.Result == ElnecSite.RESULT_NG && Site4.Result == ElnecSite.RESULT_NG)
                {
                    ResultRespoonse = Result_ngPBA;
                    lbMachineStatus.Invoke(new MethodInvoker(delegate { lbMachineStatus.Text = "FAIL"; lbMachineStatus.BackColor = Color.Red; }));
                }


                if (Site1.Result == ElnecSite.RESULT_OK && Site3.Result == ElnecSite.RESULT_OK)
                    AMWsProcess.Statitis_OK += 1;
                else
                    AMWsProcess.Statitis_NG += 1;

                if (Site2.Result == ElnecSite.RESULT_OK && Site4.Result == ElnecSite.RESULT_OK)
                    AMWsProcess.Statitis_OK += 1;
                else
                    AMWsProcess.Statitis_NG += 1;


                tbHistory.Invoke(new MethodInvoker(delegate
                {
                    string now = DateTime.Now.ToString();
                    tbHistory.AppendText(now + "    " + model.ModelName + Environment.NewLine + "        A: " + Site1.Result + "  B: " + Site2.Result + "  C: " + Site3.Result + "  D: " + Site4.Result + Environment.NewLine);
                    _CONFIG.reportWrite(now, lbModelName.Text, lbMachineStatus.Text, Site1.Result, Site2.Result, Site3.Result, Site4.Result);

                    Console.WriteLine(ResultRespoonse);

                    CharCircle = 1;
                    timerUpdateChar.Start();
                    lastWorkingTime = DateTime.Now;
                    highlinedgwTestMode(2);
                    timerReleaseBoard.Interval = 2000;
                    timerReleaseBoard.Start();
                    Site1.ClearSiteParam();
                    Site2.ClearSiteParam();
                    Site3.ClearSiteParam();
                    Site4.ClearSiteParam();
                }));
            }
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

            //Site1.Command = ElnecSite.CLOSE_APP;
            //Site2.Command = ElnecSite.CLOSE_APP;
            //Site3.Command = ElnecSite.CLOSE_APP;
            //Site4.Command = ElnecSite.CLOSE_APP;

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

        private const int BUFFER_SIZE = 10000;
        static readonly ASCIIEncoding encoding = new ASCIIEncoding();

        public void ElnecComuncationBackgroudSite1()
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            TcpListener listener = new TcpListener(address, 8881);
            listener.Start();
            while (true)
            {
                if (ServerStatus == SERVER_OFF)
                    break;
                Socket socket = listener.AcceptSocket();
                socket.ReceiveTimeout = TCP_TimeOut;

                if (Site1.WorkProcess.Process == WorkProcess.Ready)
                {
                    Site1.Command = Site1.WorkProcess.GetCommandFIFO();
                    if (Site1.Command != "null")
                    {
                        socket.Send(encoding.GetBytes(Site1.Command));
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 1000000) tbLog.Clear();
                            tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site1: " + Site1.Command + System.Environment.NewLine);
                        }));
                        Site1.WorkProcess.Process = WorkProcess.Start;
                    }
                }
                else if (Site1.WorkProcess.Process == WorkProcess.Interrup)
                {
                    socket.Send(encoding.GetBytes(ElnecSite.STOP_OPERATION));
                    tbLog.Invoke(new MethodInvoker(delegate
                    {
                        if (tbLog.TextLength > 1000000) tbLog.Clear();
                        tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site1: " + ElnecSite.STOP_OPERATION + System.Environment.NewLine);
                    }));

                    Site1.WorkProcess.Process = WorkProcess.Ready;
                }

                while (true)
                {
                    byte[] data = new byte[BUFFER_SIZE];
                    int d = 0;
                    try
                    {
                        d = socket.Receive(data);
                    }
                    catch (Exception)
                    {
                        //Site1.WorkProcess.Process = WorkProcess.Ready;
                        break;
                    }
                    if (d > 0)
                    {
                        string recive = encoding.GetString(data, 0, d);
                        ProcessSite(Site1, lbSiteName1, lbSite1Checksum, lbROM1checkSum, lbRomNameSite1, lbResultA, recive.Replace("\\*/n\\*/", System.Environment.NewLine));
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 1000000) tbLog.Clear();
                            tbLog.AppendText(recive.Replace("\\*/n\\*/", System.Environment.NewLine));
                        }));

                    }
                }
                socket.Close();
            }
            listener.Stop();
        }
        public void ElnecComuncationBackgroudSite2()
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            TcpListener listener = new TcpListener(address, 8882);
            listener.Start();
            // 1. listen

            while (true)
            {
                if (ServerStatus == SERVER_OFF)
                    break;
                Socket socket = listener.AcceptSocket();
                socket.ReceiveTimeout = TCP_TimeOut;
                if (Site2.WorkProcess.Process == WorkProcess.Ready)
                {
                    Site2.Command = Site2.WorkProcess.GetCommandFIFO();
                    if (Site2.Command != "null")
                    {
                        socket.Send(encoding.GetBytes(Site2.Command));
                        tbLog.Invoke(new MethodInvoker(delegate { if (tbLog.TextLength > 1000000) tbLog.Clear(); tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site2: " + Site2.Command + System.Environment.NewLine); }));
                        Site2.WorkProcess.Process = WorkProcess.Start;
                    }
                }

                if (Site2.WorkProcess.Process == WorkProcess.Interrup)
                {
                    socket.Send(encoding.GetBytes(ElnecSite.STOP_OPERATION));
                    tbLog.Invoke(new MethodInvoker(delegate { if (tbLog.TextLength > 1000000) tbLog.Clear(); tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site2: " + ElnecSite.STOP_OPERATION + System.Environment.NewLine); }));
                    Site2.WorkProcess.Process = WorkProcess.Ready;
                }

                while (true)
                {
                    byte[] data = new byte[BUFFER_SIZE];
                    int d = 0;
                    try
                    {
                        d = socket.Receive(data);
                    }
                    catch (Exception)
                    {
                        //Site2.WorkProcess.Process = WorkProcess.Ready;
                        break;
                    }
                    if (d > 0)
                    {
                        string recive = encoding.GetString(data, 0, d);
                        ProcessSite(Site2, lbSiteName2, lbSite2Checksum, lbROM2checkSum, lbRomNameSite2, lbResultB, recive.Replace("\\*/n\\*/", System.Environment.NewLine));
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 1000000) tbLog.Clear();
                            tbLog.AppendText(recive.Replace("\\*/n\\*/", System.Environment.NewLine));
                        }));


                    }
                }
                socket.Close();
            }
            listener.Stop();
        }
        public void ElnecComuncationBackgroudSite3()
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            TcpListener listener = new TcpListener(address, 8883);
            // 1. listen
            listener.Start();
            while (true)
            {
                if (ServerStatus == SERVER_OFF)
                    break;
                Socket socket = listener.AcceptSocket();
                socket.ReceiveTimeout = TCP_TimeOut;
                if (Site3.WorkProcess.Process == WorkProcess.Ready)
                {
                    Site3.Command = Site3.WorkProcess.GetCommandFIFO();
                    if (Site3.Command != "null")
                    {
                        socket.Send(encoding.GetBytes(Site3.Command));
                        tbLog.Invoke(new MethodInvoker(delegate { if (tbLog.TextLength > 1000000) tbLog.Clear(); tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site3: " + Site3.Command + System.Environment.NewLine); }));
                        Site3.WorkProcess.Process = WorkProcess.Start;
                    }
                }

                if (Site3.WorkProcess.Process == WorkProcess.Interrup)
                {
                    socket.Send(encoding.GetBytes(ElnecSite.STOP_OPERATION));
                    tbLog.Invoke(new MethodInvoker(delegate { if (tbLog.TextLength > 1000000) tbLog.Clear(); tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site3: " + ElnecSite.STOP_OPERATION + System.Environment.NewLine); }));
                    Site3.WorkProcess.Process = WorkProcess.Ready;
                }

                while (true)
                {
                    byte[] data = new byte[BUFFER_SIZE];
                    int d = 0;
                    try
                    {
                        d = socket.Receive(data);
                    }
                    catch (Exception)
                    {
                        // Site3.WorkProcess.Process = WorkProcess.Ready;
                        break;
                    }
                    if (d > 0)
                    {
                        string recive = encoding.GetString(data, 0, d);
                        ProcessSite(Site3, lbSiteName3, lbSite3Checksum, lbROM3checkSum, lbRomNameSite3, lbResultC, recive.Replace("\\*/n\\*/", System.Environment.NewLine));
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 1000000) tbLog.Clear();
                            tbLog.AppendText(recive.Replace("\\*/n\\*/", System.Environment.NewLine));
                        }));


                    }
                }
                socket.Close();
            }
            listener.Stop();
        }
        public void ElnecComuncationBackgroudSite4()
        {
            IPAddress address = IPAddress.Parse("127.0.0.1");
            TcpListener listener = new TcpListener(address, 8884);
            // 1. listen
            listener.Start();
            while (true)
            {
                if (ServerStatus == SERVER_OFF)
                    break;
                Socket socket = listener.AcceptSocket();
                socket.ReceiveTimeout = TCP_TimeOut;
                if (Site4.WorkProcess.Process == WorkProcess.Ready)
                {
                    Site4.Command = Site4.WorkProcess.GetCommandFIFO();
                    if (Site4.Command != "null")
                    {
                        socket.Send(encoding.GetBytes(Site4.Command));
                        tbLog.Invoke(new MethodInvoker(delegate { if (tbLog.TextLength > 1000000) tbLog.Clear(); tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site4: " + Site4.Command + System.Environment.NewLine); }));
                        Site4.WorkProcess.Process = WorkProcess.Start;
                    }
                }

                if (Site4.WorkProcess.Process == WorkProcess.Interrup)
                {
                    socket.Send(encoding.GetBytes(ElnecSite.STOP_OPERATION));
                    tbLog.Invoke(new MethodInvoker(delegate { if (tbLog.TextLength > 1000000) tbLog.Clear(); tbLog.AppendText("L" + tbLogLineNumber++.ToString() + ": " + "Site4: " + ElnecSite.STOP_OPERATION + System.Environment.NewLine); }));
                    Site4.WorkProcess.Process = WorkProcess.Ready;
                }
                while (true)
                {
                    byte[] data = new byte[BUFFER_SIZE];
                    int d = 0;
                    try
                    {
                        d = socket.Receive(data);
                    }
                    catch (Exception)
                    {
                        //Site4.WorkProcess.Process = WorkProcess.Ready;
                        break;
                    }
                    if (d > 0)
                    {
                        string recive = encoding.GetString(data, 0, d);
                        ProcessSite(Site4, lbSiteName4, lbSite4Checksum, lbROM4checkSum, lbRomNameSite4, lbResultD, recive.Replace("\\*/n\\*/", System.Environment.NewLine));
                        tbLog.Invoke(new MethodInvoker(delegate
                        {
                            if (tbLog.TextLength > 1000000) tbLog.Clear();
                            tbLog.AppendText(recive.Replace("\\*/n\\*/", System.Environment.NewLine));
                        }));



                    }
                }
                socket.Close();
            }
            listener.Stop();
        }
        // Site1 process
        public void ProcessSite(ElnecSite Site, Label lbSiteName, Label lbSiteChecksum, TextBox lbROMcheckSum, Label lbRomNameSite, Label lbResult, string Response)
        {
            // get infor from site 
            Response = Response.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] ElnecResponses = Response.Split('\n');

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
                                Site.WorkProcess.Process = WorkProcess.Ready;
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
                        case ElnecSite.PROGRAMMER_READY_STATUS:
                            {
                                if (data[1] == ElnecSite.KEY_PROGRAMMER_READY)
                                {
                                    lbSiteName.BackColor = activeColor;
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
                                Site.WorkProcess.Process = WorkProcess.Interrup;
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
                if (Site.SITE_OPTYPE == "5")
                {
                    if (Site.SITE_PROGRESS == "99" && Site.Command == ElnecSite.GET_PRG_STATUS)
                    {
                        Site.WorkProcess.Process = WorkProcess.Interrup;
                        // Site.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                        // Site.WorkProcess.Process = WorkProcess.Ready;
                    }

                }
                if (Site.SITE_OPTYPE == "0")
                {
                    Site.WorkProcess.Process = WorkProcess.Ready;
                }

                if (Site.SITE_OPRESULT == ElnecSite.OPRESULT_FAIL || Site.SITE_OPRESULT == ElnecSite.OPRESULT_HWERR || Site.SITE_OPRESULT == ElnecSite.OPRESULT_NONE)
                {
                    if (Site.Command == ElnecSite.PROGRAM_DEVICE)
                    {
                        if (Site.SITE_PROGRAMRESULT != ElnecSite.RESULT_NG)
                        {
                            Site.WorkProcess.Process = WorkProcess.Interrup;
                            //Site.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                            Site.SITE_PROGRAMRESULT = ElnecSite.RESULT_NG;
                            Site.Result = ElnecSite.RESULT_NG;
                        }
                        NG_label(lbResult);
                    }
                }
                if (Site.SITE_OPRESULT == ElnecSite.OPRESULT_GOOD)
                {
                    if (Site.Command == ElnecSite.PROGRAM_DEVICE)
                    {
                        if (Site.SITE_PROGRAMRESULT != ElnecSite.RESULT_OK)
                        {
                            Site.WorkProcess.Process = WorkProcess.Interrup;
                            //Site.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                            Site.SITE_PROGRAMRESULT = ElnecSite.RESULT_OK;
                            Site.Result = ElnecSite.RESULT_OK;
                        }
                        OK_label(lbResult);
                    }
                }
                if (Site.SITE_LOADPRJRESULT == ElnecSite.FILE_LOAD_GOOD)
                {
                    OK_label(lbRomNameSite);
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
                }
                if (Site.SITE_LOADPRJRESULT == ElnecSite.FILE_LOAD_ERROR)
                {
                    NG_label(lbRomNameSite);
                    Site.SITE_LOADPRJRESULT = "";
                }
            }
            FinalTestLabel();
        }
        private void btSite1Open_Click(object sender, EventArgs e)
        {
            if (btSite1Open.Text == "OPEN")
            {
                Site1.OpenSite("1180-11227", "127.0.0.1", 8881);
                btSite1Open.Text = "PROGRAM";
            }
            else
            {
                //Site1.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                Site1.SITE_PROGRAMRESULT = ElnecSite.EMPTY;
                lbResultA.BackColor = activeColor;
                Site1.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
            }

        }

        private void btSite2Open_Click(object sender, EventArgs e)
        {
            if (btSite2Open.Text == "OPEN")
            {
                Site2.OpenSite("1180-11228", "127.0.0.1", 8882);
                btSite2Open.Text = "PROGRAM";
            }
            else
            {
                //Site2.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                Site2.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
            }
        }

        private void btSite3Open_Click(object sender, EventArgs e)
        {
            if (btSite3Open.Text == "OPEN")
            {
                Site3.OpenSite("1180-11229", "127.0.0.1", 8883);
                btSite3Open.Text = "PROGRAM";
            }
            else
            {
                //Site3.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                Site3.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
            }
        }

        private void btSite4Open_Click(object sender, EventArgs e)
        {
            if (btSite4Open.Text == "OPEN")
            {
                Site4.OpenSite("1180-11230", "127.0.0.1", 8884);
                btSite4Open.Text = "PROGRAM";
            }
            else
            {
                //Site4.WorkProcess.PutComandToFIFO(ElnecSite.STOP_OPERATION);
                Site4.WorkProcess.PutComandToFIFO(ElnecSite.PROGRAM_DEVICE);
            }
        }

        private void tableLayoutPanel7_Paint(object sender, PaintEventArgs e)
        {

        }
        private void lbROMsellected_Click(object sender, EventArgs e)
        {
            switch (ROMsellectCounter)
            {
                case 0:
                    {
                        ROMsellectCounter++;
                        lbROMsellected.BackColor = Color.Black;
                        break;
                    }
                case 1:
                    {
                        ROMsellectCounter++;
                        lbROMsellected.BackColor = fistROMsellect;
                        break;
                    }
                case 2:
                    {
                        ROMsellectCounter++;
                        lbROMsellected.BackColor = secondROMsellect;
                        break;
                    }
                case 3:
                    {
                        ROMsellectCounter++;
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
            if (ROMsellectCounter != 0)
                lbSite1Sellect.BackColor = lbROMsellected.BackColor;
        }

        private void lbSite2Sellect_Click(object sender, EventArgs e)
        {
            if (ROMsellectCounter != 0)
                lbSite2Sellect.BackColor = lbROMsellected.BackColor;
        }

        private void lbSite3Sellect_Click(object sender, EventArgs e)
        {
            if (ROMsellectCounter != 0)
                lbSite3Sellect.BackColor = lbROMsellected.BackColor;
        }

        private void lbSite4Sellect_Click(object sender, EventArgs e)
        {
            if (ROMsellectCounter != 0)
                lbSite4Sellect.BackColor = lbROMsellected.BackColor;
        }
        private void btRomSite1_Click(object sender, EventArgs e)
        {
            openFileDialogSite1.InitialDirectory = _CONFIG.recentWorkPath;
            openFileDialogSite1.ShowDialog();
        }

        private void btRomSite2_Click(object sender, EventArgs e)
        {
            if (lbSite2Sellect.BackColor == deactiveColor)
                openFileDialogSite1.InitialDirectory = _CONFIG.recentWorkPath;
            openFileDialogSite2.ShowDialog();
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
            string[] paths = path.Split('\\');


            if (lbSite1Sellect.BackColor == lbROMsellected.BackColor)
            {
                lbRomNameSite1.Text = Path.GetFileNameWithoutExtension(openFileDialogSite1.FileName);
                model.ROMs[0].ROM_PATH = Path.GetDirectoryName(openFileDialogSite1.FileName) + "\\" + Path.GetFileName(openFileDialogSite1.FileName);
                model.ROMs[0].ROM_CHECKSUM = lbROM1checkSum.Text;
            }
            if (lbSite2Sellect.BackColor == lbROMsellected.BackColor)
            {
                lbRomNameSite2.Text = Path.GetFileNameWithoutExtension(openFileDialogSite1.FileName);
                model.ROMs[1].ROM_PATH = Path.GetDirectoryName(openFileDialogSite1.FileName) + "\\" + Path.GetFileName(openFileDialogSite1.FileName);
                model.ROMs[1].ROM_CHECKSUM = lbROM2checkSum.Text;
            }
            if (lbSite3Sellect.BackColor == lbROMsellected.BackColor)
            {
                lbRomNameSite3.Text = Path.GetFileNameWithoutExtension(openFileDialogSite1.FileName);
                model.ROMs[2].ROM_PATH = Path.GetDirectoryName(openFileDialogSite1.FileName) + "\\" + Path.GetFileName(openFileDialogSite1.FileName);
                model.ROMs[2].ROM_CHECKSUM = lbROM2checkSum.Text;
            }
            if (lbSite4Sellect.BackColor == lbROMsellected.BackColor)
            {
                lbRomNameSite4.Text = Path.GetFileNameWithoutExtension(openFileDialogSite1.FileName);
                model.ROMs[3].ROM_PATH = Path.GetDirectoryName(openFileDialogSite1.FileName) + "\\" + Path.GetFileName(openFileDialogSite1.FileName);
            }


            model.ModelName = lbRomNameSite1.Text;

            if (lbRomNameSite1.Text != lbRomNameSite2.Text)
            {
                model.ModelName += "/" + lbRomNameSite2.Text;
            }
            else if (lbRomNameSite1.Text != lbRomNameSite3.Text)
            {
                model.ModelName += "/" + lbRomNameSite3.Text;
            }
            else if (lbRomNameSite1.Text != lbRomNameSite4.Text)
            {
                model.ModelName += "/" + lbRomNameSite4.Text;
            }

            lbModelName.Text = model.ModelName;
        }
        private void openFileDialogSite2_FileOk(object sender, CancelEventArgs e)
        {
            _CONFIG.recentWorkPath = Path.GetDirectoryName(openFileDialogSite2.FileName);
            lbRomNameSite2.Text = Path.GetFileNameWithoutExtension(openFileDialogSite2.FileName);
            lbRomNameSite4.Text = Path.GetFileNameWithoutExtension(openFileDialogSite2.FileName);
            model.ROMs[1].ROM_PATH = Path.GetDirectoryName(openFileDialogSite2.FileName) + "\\" + Path.GetFileName(openFileDialogSite2.FileName);
            model.ROMs[3].ROM_PATH = Path.GetDirectoryName(openFileDialogSite2.FileName) + "\\" + Path.GetFileName(openFileDialogSite2.FileName);
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

        private void openFileModel_FileOk(object sender, CancelEventArgs e)
        {
            _CONFIG.recentModelPath = Path.GetDirectoryName(openFileModel.FileName);

            string[] config = File.ReadAllLines(openFileModel.FileName);

            tbHistory.AppendText("Load project to Elnect site..");
            model.ROMs[0].ROM_PATH = config[1].Remove(config[1].IndexOf('@'), config[1].Length - config[1].IndexOf('@'));
            lbRomNameSite1.Text = config[1].Split('\\')[config[1].Split('\\').Length - 1];
            lbROM1checkSum.Text = lbRomNameSite1.Text.Remove(0, lbRomNameSite1.Text.IndexOf('@') + 1);
            lbRomNameSite1.Text = lbRomNameSite1.Text.Remove(lbRomNameSite1.Text.IndexOf('@'), lbRomNameSite1.Text.Length - lbRomNameSite1.Text.IndexOf('@'));

            model.ROMs[1].ROM_PATH = config[2].Remove(config[2].IndexOf('@'), config[2].Length - config[2].IndexOf('@'));
            lbRomNameSite2.Text = config[2].Split('\\')[config[1].Split('\\').Length - 1];
            lbROM2checkSum.Text = lbRomNameSite2.Text.Remove(0, lbRomNameSite2.Text.IndexOf('@') + 1);
            lbRomNameSite2.Text = lbRomNameSite2.Text.Remove(lbRomNameSite2.Text.IndexOf('@'), lbRomNameSite2.Text.Length - lbRomNameSite2.Text.IndexOf('@'));

            model.ROMs[2].ROM_PATH = config[3].Remove(config[3].IndexOf('@'), config[3].Length - config[3].IndexOf('@'));
            lbRomNameSite3.Text = config[3].Split('\\')[config[1].Split('\\').Length - 1];
            lbROM3checkSum.Text = lbRomNameSite3.Text.Remove(0, lbRomNameSite3.Text.IndexOf('@') + 1);
            lbRomNameSite3.Text = lbRomNameSite3.Text.Remove(lbRomNameSite3.Text.IndexOf('@'), lbRomNameSite3.Text.Length - lbRomNameSite3.Text.IndexOf('@'));

            model.ROMs[3].ROM_PATH = config[4].Remove(config[4].IndexOf('@'), config[4].Length - config[4].IndexOf('@'));
            lbRomNameSite4.Text = config[4].Split('\\')[config[4].Split('\\').Length - 1];
            lbROM4checkSum.Text = lbRomNameSite4.Text.Remove(0, lbRomNameSite4.Text.IndexOf('@') + 1);
            lbRomNameSite4.Text = lbRomNameSite4.Text.Remove(lbRomNameSite4.Text.IndexOf('@'), lbRomNameSite4.Text.Length - lbRomNameSite4.Text.IndexOf('@'));

            lbRomNameSite1.BackColor = Color.FromArgb(80, 80, 80);
            lbRomNameSite2.BackColor = Color.FromArgb(80, 80, 80);
            lbRomNameSite3.BackColor = Color.FromArgb(80, 80, 80);
            lbRomNameSite4.BackColor = Color.FromArgb(80, 80, 80);

            timerReleaseBoard.Interval = 749;
            timerReleaseBoard.Start();

            model.ModelName = lbRomNameSite1.Text.Remove(lbRomNameSite1.Text.Length - 5, 5);
            lbSite1Sellect.BackColor = activeColor;
            lbSite2Sellect.BackColor = activeColor;
            lbSite3Sellect.BackColor = activeColor;
            lbSite4Sellect.BackColor = activeColor;

            if (lbRomNameSite1.Text != lbRomNameSite2.Text)
            {
                model.ModelName += "/" + lbRomNameSite2.Text.Remove(lbRomNameSite2.Text.Length - 5, 5);
                lbSite2Sellect.BackColor = deactiveColor;
            }
            if (lbRomNameSite1.Text != lbRomNameSite3.Text)
            {
                model.ModelName += "/" + lbRomNameSite3.Text.Remove(lbRomNameSite3.Text.Length - 5, 5);
                lbSite3Sellect.BackColor = deactiveColor;
            }
            if (lbRomNameSite1.Text != lbRomNameSite4.Text)
            {
                model.ModelName += "/" + lbRomNameSite4.Text.Remove(lbRomNameSite4.Text.Length - 5, 5);
                lbSite4Sellect.BackColor = deactiveColor;
            }

            lbModelName.Text = model.ModelName;

            Site1.WorkProcess.Process = WorkProcess.Interrup;
            Site2.WorkProcess.Process = WorkProcess.Interrup;
            Site3.WorkProcess.Process = WorkProcess.Interrup;
            Site4.WorkProcess.Process = WorkProcess.Interrup;

            Site1.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[0].ROM_PATH);
            Site2.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[1].ROM_PATH);
            Site3.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[2].ROM_PATH);
            Site4.WorkProcess.PutComandToFIFO(ElnecSite.LOAD_PROJECT + model.ROMs[3].ROM_PATH);

            for (int i = 5; i < config.Length; i++)
            {
                AMWsProcess.PutComandToFIFO(config[i]);
                Console.WriteLine(config[i]);
            }
        }

        public void openLastWokingModel(string path)
        {
            try
            {
                string[] config = File.ReadAllLines(path);
                lbRomNameSite1.Invoke(new MethodInvoker(delegate
                {
                    lbRomNameSite1.Text = config[1].Split('\\')[config[1].Split('\\').Length - 1];
                    lbROM1checkSum.Text = lbRomNameSite1.Text.Split('_')[1].Replace(" ", string.Empty);
                    lbRomNameSite2.Text = config[2].Split('\\')[config[2].Split('\\').Length - 1];
                    lbROM2checkSum.Text = lbRomNameSite2.Text.Split('_')[1].Replace(" ", string.Empty);
                    lbRomNameSite3.Text = config[3].Split('\\')[config[3].Split('\\').Length - 1];
                    lbROM3checkSum.Text = lbRomNameSite3.Text.Split('_')[1].Replace(" ", string.Empty);
                    lbRomNameSite4.Text = config[4].Split('\\')[config[4].Split('\\').Length - 1];
                    lbROM4checkSum.Text = lbRomNameSite4.Text.Split('_')[1].Replace(" ", string.Empty);

                    model.ModelName = lbRomNameSite1.Text.Remove(lbRomNameSite1.Text.Length - 5, 5);

                    if (lbRomNameSite1.Text != lbRomNameSite2.Text)
                    {
                        model.ModelName += "/" + lbRomNameSite2.Text.Remove(lbRomNameSite2.Text.Length - 5, 5);
                    }
                    else if (lbRomNameSite1.Text != lbRomNameSite3.Text)
                    {
                        model.ModelName += "/" + lbRomNameSite3.Text.Remove(lbRomNameSite3.Text.Length - 5, 5);
                    }
                    else if (lbRomNameSite1.Text != lbRomNameSite4.Text)
                    {
                        model.ModelName += "/" + lbRomNameSite4.Text.Remove(lbRomNameSite4.Text.Length - 5, 5);
                    }

                    lbModelName.Text = model.ModelName;
                }));
                Site1.WorkProcess.Process = WorkProcess.Interrup;
                Site2.WorkProcess.Process = WorkProcess.Interrup;
                Site3.WorkProcess.Process = WorkProcess.Interrup;
                Site4.WorkProcess.Process = WorkProcess.Interrup;

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
            catch (Exception) { }
        }

        private void label1_Click(object sender, EventArgs e)
        {
            if (label1.BackColor != activeColor)
                label1.BackColor = activeColor;
            else
                label1.BackColor = deactiveColor;

            Site1.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
            Site1.WorkProcess.Process = WorkProcess.Ready;
            Site2.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
            Site2.WorkProcess.Process = WorkProcess.Ready;
            Site3.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
            Site3.WorkProcess.Process = WorkProcess.Ready;
            Site4.WorkProcess.PutComandToFIFO(ElnecSite.GET_PRG_STATUS);
            Site4.WorkProcess.Process = WorkProcess.Ready;
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

        private void btLoinCancle_Click(object sender, EventArgs e)
        {
            pnLogin.Visible = false;

        }

        private void btLogin_Click(object sender, EventArgs e)
        {
            if (tbAcc.Text == _CONFIG.ADMIN_ACC && tbPass.Text == _CONFIG.ADMIN_PASS && Permissions != TECH)
            {
                MessageBox.Show("Change permissions to technical");
                Permissions = TECH;
                tsslPermissions.Text = "User: " + Permissions;
                pnLogin.Visible = false;
                checkPermision();
            }
            else if (tbAcc.Text == _CONFIG.ADMIN_ACC && tbPass.Text == _CONFIG.ADMIN_PASS && Permissions == TECH)
            {
                MessageBox.Show("You are already Technical");
            }
            else if (tbAcc.Text == _CONFIG.MANAGER_ACC && tbPass.Text == _CONFIG.MANAGER_PASS && Permissions != MANAGER)
            {
                MessageBox.Show("Change permissions to manager");
                Permissions = MANAGER;
                tsslPermissions.Text = "User: " + Permissions;
                pnLogin.Visible = false;
                checkPermision();
            }
            else if (tbAcc.Text == _CONFIG.MANAGER_ACC && tbPass.Text == _CONFIG.MANAGER_PASS && Permissions == MANAGER)
            {
                MessageBox.Show("You are already Manager");
            }
            else if (tbAcc.Text != _CONFIG.MANAGER_ACC && tbPass.Text != _CONFIG.MANAGER_PASS && Permissions != OP)
            {
                MessageBox.Show("Incorrect user name or password.\n By default you are OP.");
                Permissions = OP;
                tsslPermissions.Text = "User: " + Permissions;
                checkPermision();
            }
            else
            {
                MessageBox.Show("Incorrect user name or password");
            }


            tbAcc.Text = "";
            tbPass.Text = "";

        }
        public void checkPermision()
        {
            tsslPermissions.Text = "User: " + Permissions;
            switch (Permissions)
            {
                case OP:
                    {
                        btManual.Click -= BtManual_Click;
                        btReportFolder.Click -= BtReportFolder_Click;
                        btSetting.Click -= BtSetting_Click;
                        btDataLog.Click -= BtDataLog_Click;

                        btUserBarcode.Click -= btUserBarcode_Click;
                        btSkipBarcode.Click -= btSkipBarcode_Click;

                        lbROMsellected.Click -= lbROMsellected_Click;
                        btRomSite1.Click -= btRomSite1_Click;
                        btRomSite2.Click -= btRomSite2_Click;

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

                        lbROM1checkSum.ReadOnly = true;
                        lbROM2checkSum.ReadOnly = true;
                        lbROM3checkSum.ReadOnly = true;
                        lbROM4checkSum.ReadOnly = true;

                        break;
                    }
                case TECH:
                    {
                        btManual.Click += BtManual_Click;
                        btReportFolder.Click += BtReportFolder_Click;
                        btSetting.Click -= BtSetting_Click;
                        btDataLog.Click -= BtDataLog_Click;

                        btUserBarcode.Click -= btUserBarcode_Click;
                        btSkipBarcode.Click -= btSkipBarcode_Click;

                        lbROMsellected.Click -= lbROMsellected_Click;
                        btRomSite1.Click -= btRomSite1_Click;
                        btRomSite2.Click -= btRomSite2_Click;

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

                        lbROM1checkSum.ReadOnly = false;
                        lbROM2checkSum.ReadOnly = false;
                        lbROM3checkSum.ReadOnly = false;
                        lbROM4checkSum.ReadOnly = false;

                        break;
                    }
                case MANAGER:
                    {
                        btManual.Click -= BtManual_Click;
                        btReportFolder.Click -= BtReportFolder_Click;
                        btSetting.Click -= BtSetting_Click;
                        btDataLog.Click -= BtDataLog_Click;

                        btUserBarcode.Click -= btUserBarcode_Click;
                        btSkipBarcode.Click -= btSkipBarcode_Click;

                        lbROMsellected.Click -= lbROMsellected_Click;
                        btRomSite1.Click -= btRomSite1_Click;
                        btRomSite2.Click -= btRomSite2_Click;

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

                        lbROM1checkSum.ReadOnly = false;
                        lbROM2checkSum.ReadOnly = false;
                        lbROM3checkSum.ReadOnly = false;
                        lbROM4checkSum.ReadOnly = false;

                        btManual.Click += BtManual_Click;
                        btReportFolder.Click += BtReportFolder_Click;
                        btSetting.Click += BtSetting_Click;
                        btDataLog.Click += BtDataLog_Click;

                        btUserBarcode.Click += btUserBarcode_Click;
                        btSkipBarcode.Click += btSkipBarcode_Click;

                        lbROMsellected.Click += lbROMsellected_Click;
                        btRomSite1.Click += btRomSite1_Click;
                        btRomSite2.Click += btRomSite2_Click;

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
            if (timerReleaseBoard.Interval == 2000)
            {
                highlinedgwTestMode(3);
                if (Port.IsOpen)
                    Port.Write(ResultRespoonse);

                tbHistory.AppendText("DONE\r\n");
                lbMachineStatus.Text = "Waitting";
                lbMachineStatus.BackColor = activeColor;
                timerReleaseBoard.Stop();
            }
            else if (timerReleaseBoard.Interval == 2500)
            {
                if (lbSiteName1.BackColor != activeColor || lbSiteName2.BackColor != activeColor || lbSiteName3.BackColor != activeColor || lbSiteName4.BackColor != activeColor)
                {
                    tbHistory.Text = "Openning Elnect";
                    Site1.OpenSite("1180-" + (ElnecAddress).ToString("d5"), RemoteIP, RemotePort);
                    Site2.OpenSite("1180-" + (ElnecAddress + 1).ToString("d5"), RemoteIP, RemotePort + 1);
                    Site3.OpenSite("1180-" + (ElnecAddress + 2).ToString("d5"), RemoteIP, RemotePort + 2);
                    Site4.OpenSite("1180-" + (ElnecAddress + 3).ToString("d5"), RemoteIP, RemotePort + 3);
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
                else
                {
                    tbHistory.AppendText(". ");
                }
            }
            else if (timerReleaseBoard.Interval == 749)
            {
                if (lbRomNameSite1.BackColor == Color.Green && lbRomNameSite2.BackColor == Color.Green && lbRomNameSite3.BackColor == Color.Green && lbRomNameSite4.BackColor == Color.Green)
                {
                    tbHistory.AppendText("success." + Environment.NewLine);
                    timerReleaseBoard.Stop();
                }
                else if (lbRomNameSite1.BackColor == Color.Red && lbRomNameSite2.BackColor == Color.Red && lbRomNameSite3.BackColor == Color.Red && lbRomNameSite4.BackColor == Color.Red)
                {
                    tbHistory.AppendText("fail. Please check Elnec connect or your program path and try again." + Environment.NewLine);
                    timerReleaseBoard.Stop();
                }
                else
                {
                    tbHistory.AppendText(". ");
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

            lbBarCode1Value.BackColor = activeColor;
            lbBarCode2Value.BackColor = activeColor;
            lbBarCode3Value.BackColor = activeColor;
            lbBarCode4Value.BackColor = activeColor;
            Port.Write(Data_enaQR);
        }

        private void btSkipBarcode_Click(object sender, EventArgs e)
        {
            SkipBarCode();
            Port.Write(Data_skipQR);
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

            lbBarCode1.BackColor = nonactiveColor;
            lbBarCode2.BackColor = nonactiveColor;
            lbBarCode3.BackColor = nonactiveColor;
            lbBarCode4.BackColor = nonactiveColor;

            lbBarCode1Value.BackColor = nonactiveColor;
            lbBarCode2Value.BackColor = nonactiveColor;
            lbBarCode3Value.BackColor = nonactiveColor;
            lbBarCode4Value.BackColor = nonactiveColor;

        }
        private void siteCheckSumRefrest_Click(object sender, EventArgs e)
        {
            if (siteCheckSumRefrest.BackColor != activeColor)
                siteCheckSumRefrest.BackColor = activeColor;
            else
                siteCheckSumRefrest.BackColor = deactiveColor;


            Site1.WorkProcess.PutComandToFIFO(ElnecSite.GETDEVCHECKSUM);
            Site1.WorkProcess.Process = WorkProcess.Ready;
            Site2.WorkProcess.PutComandToFIFO(ElnecSite.GETDEVCHECKSUM);
            Site2.WorkProcess.Process = WorkProcess.Ready;
            Site3.WorkProcess.PutComandToFIFO(ElnecSite.GETDEVCHECKSUM);
            Site3.WorkProcess.Process = WorkProcess.Ready;
            Site4.WorkProcess.PutComandToFIFO(ElnecSite.GETDEVCHECKSUM);
            Site4.WorkProcess.Process = WorkProcess.Ready;

        }

        private void btSWUser_Click(object sender, EventArgs e)
        {
            if (pnLogin.Visible != true)
                pnLogin.Visible = true;
            else
                pnLogin.Visible = false;
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
            model.Layout.drawPCBLayout(pbPCBLayout);
        }

        private void radioButton1_Click(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true)
            {
                model.Layout.PCB1 = false;
                radioButton1.Checked = false;
                if (model.Layout.PCB2)
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
                model.Layout.PCB1 = true;
                radioButton1.Checked = true;
                if (model.Layout.PCB2)
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
            model.Layout.drawPCBLayout(pbPCBLayout);
        }

        public int[] xcountArray = new int[4];
        private void PCBarrayCount_ValueChanged(object sender, EventArgs e)
        {
            nbUDXarrayCount.Maximum = PCBarrayCount.Value;
            model.Layout.XasixArrayCount = Convert.ToInt32(nbUDXarrayCount.Value);
            model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Value);
            model.Layout.drawPCBLayout(pbPCBLayout);
        }

        private void MicomArray_ValueChanged(object sender, EventArgs e)
        {
            if (NumberArray > 0)
                PCBarrayCount.Maximum = 4 / (MicomArray.Value * NumberArray);
            else
                PCBarrayCount.Maximum = 1;

            if (model.Layout.ArrayCount > PCBarrayCount.Maximum)
                model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Maximum);

            model.Layout.XasixArrayCount = Convert.ToInt32(nbUDXarrayCount.Value);
            model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Value);
            model.Layout.drawPCBLayout(pbPCBLayout);
        }

        private void nbUDXarrayCount_ValueChanged(object sender, EventArgs e)
        {
            model.Layout.XasixArrayCount = Convert.ToInt32(nbUDXarrayCount.Value);
            model.Layout.ArrayCount = Convert.ToInt32(PCBarrayCount.Value);
            model.Layout.drawPCBLayout(pbPCBLayout);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (model.Layout.PCB2 && model.Layout.PCB1)
            {
                Port.Write(Mode_2Array);
                tbLog.AppendText("2 array had set.");
            }
            else if (model.Layout.PCB2 && !model.Layout.PCB1 || !model.Layout.PCB2 && model.Layout.PCB1)
            {
                Port.Write(Mode_1Array);
                tbLog.AppendText("1 array had set.");
            }
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
        public string SITE_DETAILE { get; set; }
        public string SITE_OPRESULT { get; set; }
        public string SITE_LOADPRJRESULT { get; set; }
        public string SITE_PROGRAMRESULT { get; set; }



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
        public const string PROGRAM_PATH = @"C:\Auto Micom Writing\AMW Programs\";
        public string PCBcode;
        public string ModelPath;
        public string ModelName;
        public string CheckSum;

        public Layout Layout = new Layout();

        public SaveFileDialog saveFileDialog = new SaveFileDialog();

        public ROM[] ROMs = {   new ROM(),
                                new ROM(),
                                new ROM(),
                                new ROM()};

        public PCB_Model() { }
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
                + this.Layout.XasixArrayCount.ToString() + Environment.NewLine;
            saveFileDialog.DefaultExt = "a_ms";
            File.WriteAllText(saveFileDialog.FileName, configModel);
        }

        public void SaveAsNew()
        {
            this.saveFileDialog.FileOk += new CancelEventHandler(save);
            saveFileDialog.ShowDialog();
            //if (!Directory.Exists(PROGRAM_PATH + this.PCBcode)) Directory.CreateDirectory(PROGRAM_PATH + this.PCBcode);
            //File.WriteAllText(PROGRAM_PATH + this.PCBcode + @"\" + this.ModelName + ".a_ms", configModel);
        }
    }

    class Layout
    {
        public bool PCB1;
        public bool PCB2;
        public int ArrayCount, XasixArrayCount;

        public char[] Name = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };

        public Layout()
        {
            this.PCB1 = true;
            this.PCB2 = true;
            this.ArrayCount = 2;
            this.XasixArrayCount = 1;
        }
        public void drawPCBLayout(PictureBox pbPCBLayout)
        {

            int x = pbPCBLayout.Size.Width - 10;
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
                Bitmap custormChart = new Bitmap(x, y);
                Graphics g = Graphics.FromImage(custormChart);

                SolidBrush[] brush = { new SolidBrush(Color.FromArgb(30, 136, 221)), new SolidBrush(Color.Green), new SolidBrush(Color.Red) };
                SolidBrush brush_char = new SolidBrush(Color.White);
                Font nameFont = new Font("Microsoft YaHei UI", y1 / 2, FontStyle.Bold);

                int nameCounter = 0;
                if (PCB1 || PCB2)
                    nameCounter = this.ArrayCount - 1;
                if (PCB1 && PCB2)
                    nameCounter = 2 * this.ArrayCount - 1;

                for (int j = this.ArrayCount / this.XasixArrayCount; j >= 1; j--)
                {
                    for (int i = 1; i <= this.XasixArrayCount; ++i)
                    {
                        if (PCB2)
                        {
                            g.FillRectangle(brush[1], (i - 1) * (x2 / this.XasixArrayCount) + 3, (j - 1) * y2 + 3, (x2 / this.XasixArrayCount) - 3, y2 - 3);
                            g.DrawString(this.Name[nameCounter].ToString(), nameFont, brush_char, (x2 / (2 * this.XasixArrayCount)) + (i - 1) * (x2 / this.XasixArrayCount) - y2 / 3, (j - 1) * y2 + 3);
                            if (nameCounter > 0) nameCounter--;
                        }

                        if (PCB1)
                        {
                            g.FillRectangle(brush[2], (i - 1) * (x1 / this.XasixArrayCount) + x2 + 13, (j - 1) * y1 + 3, (x1 / this.XasixArrayCount) - 3, y1 - 3);
                            g.DrawString(this.Name[nameCounter].ToString(), nameFont, brush_char, x2 + (x1 / (2 * this.XasixArrayCount)) + (i - 1) * (x1 / this.XasixArrayCount) - y1 / 3, (j - 1) * y1 + 3);
                            if (nameCounter > 0) nameCounter--;
                        }
                    }
                }

                if (pbPCBLayout.Image != null)
                    pbPCBLayout.Image.Dispose();

                pbPCBLayout.Image = custormChart;
                brush[0].Dispose();
                brush[1].Dispose();
                brush[2].Dispose();
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

        public string defaulComPort = "COM 1";
        public int defaulBaudrate = 9600;

        public int ElnecAddress = 11227;

        public string ADMIN_ACC = "admin";
        public string ADMIN_PASS = "123456";

        public string MANAGER_ACC = "manager";
        public string MANAGER_PASS = "123654789";

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
                  + "defaultMANAGER_PASS@" + this.MANAGER_PASS + Environment.NewLine;
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
                            {
                                this.recentModelPath = configData[1];
                                break;
                            }
                        case "recentWorkPath":
                            {
                                this.recentWorkPath = configData[1];
                                break;
                            }
                        case "reportPath":
                            {
                                this.reportPath = configData[1];
                                break;
                            }
                        case "defaultComPort":
                            {
                                this.defaulComPort = configData[1];
                                break;
                            }
                        case "defaultBaudrate":
                            {
                                this.defaulBaudrate = Convert.ToInt32(configData[1]);
                                break;
                            }
                        case "defaultADMIN_ACC":
                            {
                                this.ADMIN_ACC = configData[1];
                                break;
                            }
                        case "defaultADMIN_PASS":
                            {
                                this.ADMIN_PASS = configData[1];
                                break;
                            }
                        case "defaultMANAGER_ACC":
                            {
                                this.MANAGER_ACC = configData[1];
                                break;
                            }
                        case "defaultMANAGER_PASS":
                            {
                                this.MANAGER_PASS = configData[1];
                                break;
                            }
                    }
                }
            }
        }

        public void SaveConfig()
        {
            if (!Directory.Exists(configPath)) Directory.CreateDirectory(configPath);
            string config =
                    "recentModePath@" + this.recentModelPath + Environment.NewLine
                  + "recentWorkPath@" + this.recentWorkPath + Environment.NewLine
                  + "reportPath@" + this.reportPath + Environment.NewLine
                  + "defautComPort@" + this.defaulComPort + Environment.NewLine
                  + "defaultBaudrate@" + this.defaulBaudrate.ToString() + Environment.NewLine
                  + "defaultADMIN_ACC@" + this.ADMIN_ACC + Environment.NewLine
                  + "defaultADMIN_PASS@" + this.ADMIN_PASS + Environment.NewLine
                  + "defaultMANAGER_ACC@" + this.MANAGER_ACC + Environment.NewLine
                  + "defaultMANAGER_PASS@" + this.MANAGER_PASS + Environment.NewLine;
            File.WriteAllText(configPath + "config.cfg", config);
        }


        public void reportWrite(string now, string model, string Result, string site1Result, string site2Result, string site3Result, string site4Result)
        {
            string path = this.reportPath;
            string moment = now;
            string today_txt = "Report-" + DateTime.Now.ToString("yyyy-MM-dd");

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            if (File.Exists(path + today_txt + ".txt")) // Nếu file lịch sử tồn tại thì lưu thông tin vào
            {
                int line = File.ReadAllLines(path + today_txt + ".txt").Length;
                using (StreamWriter sw = File.AppendText(path + today_txt + ".txt"))
                {
                    string reportData = "L" + line.ToString() + "|" + Result + "|" + model + "|" + moment + "|" + site1Result + "|" + site2Result + "|" + site3Result + "|" + site4Result;
                    sw.WriteLine(reportData);
                }

            }
            else
            {
                using (StreamWriter sw = File.AppendText(path + today_txt + ".txt"))
                {
                    string reportData = "STT|" + "Final result|" + "Model " + "|" + "Time" + "|" + "Site 1" + "|" + "Site 2" + "|" + "Site 3" + "|" + "Site 4" + "\n";
                    reportData += "L" + "1" + "|" + Result + "|" + model + "|" + moment + "|" + site1Result + "|" + site2Result + "|" + site3Result + "|" + site4Result;
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
        public const int Interrup = 4;

        public int Statitis_OK;
        public int Statitis_NG;


        public int WorkingSite = 0;

        public int Process = 0;

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

        public void ClearCMDQueue()
        {
            for (int i = 0; i < this.ComandQueue.Length - 1; i++)
            {
                this.ComandQueue[i] = "null";
            }
        }
    }
}
