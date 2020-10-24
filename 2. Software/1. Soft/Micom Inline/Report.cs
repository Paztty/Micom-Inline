using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Micom_Inline
{
    public partial class Report : Form
    {

        public int Ok, NG, Total;
        string exReport = "";
        public string ExportReportPath = "";
        List<string> ModelList = new List<string>();
        string[] modelCode = new string[100];
        public Report()
        {
            InitializeComponent();
            if(Main.Permissions == Main.TECH)
                btExport.Click -= btExport_Click;
            if (Main.Permissions == Main.MANAGER)
            {
                btExport.Click -= btExport_Click;
                btExport.Click += btExport_Click;
            }
                

            ModelList.Add("All");
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

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btApplyFillter_Click(object sender, EventArgs e)
        {
            dgReport.Rows.Clear();
            dgReport.Refresh();
            DateTime dayFrom = dtpFrom.Value;
                DateTime dayTo = dtpTo.Value;
                lbFormName.Text = "Report from: " + dayFrom.ToString("yyyy-MM-dd") + " to: " + dayTo.ToString("yyyy-MM-dd");
                while (dayFrom <= dayTo)
                {
                    loadReport(dayFrom);
                    dayFrom = dayFrom.AddDays(1);
                }
        }

        private void cbbPCBcode_SelectedIndexChanged(object sender, EventArgs e)
        {
            Ok = 0;
            NG = 0;
            Total = 0;
            dgReport.Rows.Clear();
            dgReport.Refresh();
            DateTime dayFrom = dtpFrom.Value;
            DateTime dayTo = dtpTo.Value;
            lbFormName.Text = "Report from: " + dayFrom.ToString("yyyy-MM-dd") + " to: " + dayTo.ToString("yyyy-MM-dd");
            while (dayFrom <= dayTo)
            {
                String[] dataInLine;
                string pathReport = @"C:\Auto Micom Writing\AMW Report\Report-" + dayFrom.ToString("yyyy-MM-dd") + ".txt";
                if (File.Exists(pathReport)) // if computer has report file, push it on data grit view
                {
                    var lines = File.ReadAllLines(pathReport);
                    for (int i = lines.Length - 1; i >= 0; i--)
                    {
                        dataInLine = lines[i].Split('|');
                        if (dataInLine[0].Contains("L") && cbbPCBcode.SelectedValue.ToString() == "All")
                        {
                            Total++;
                            dgReport.Rows.Add(Total.ToString(), dataInLine[1], dataInLine[2], "not user", dataInLine[3], dataInLine[4], dataInLine[5], dataInLine[6], dataInLine[7]);
                        }
                        if (dataInLine[0].Contains("L") && dataInLine[2] == cbbPCBcode.SelectedValue.ToString())
                        {
                            Total++;
                            dgReport.Rows.Add(Total.ToString(), dataInLine[1], dataInLine[2], "not user", dataInLine[3], dataInLine[4], dataInLine[5], dataInLine[6], dataInLine[7]);
                        }

                        if (dataInLine[1].Contains("OK")) Ok++;
                        if (dataInLine[1].Contains("FAIL")) NG++;
                    }

                }
                dayFrom = dayFrom.AddDays(1);
            }
            for (int i = 0; i < dgReport.Rows.Count - 1; i++)
            {
                if (dgReport.Rows[i].Cells[1].Value.ToString() == "FAIL")
                    dgReport.Rows[i].Selected = true;
            }


            lbStaTTnum.Text = Total.ToString();
            lbStaOKnum.Text = Ok.ToString("D");
            lbStaNGnum.Text = NG.ToString("D");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            dtpFrom.Value = DateTime.Now;
            dtpTo.Value = DateTime.Now;
        }

        public void loadReport(DateTime date)
        {
            Ok = 0;
            NG = 0;
            Total = 0;

            String[] dataInLine;
            string pathReport = @"C:\Auto Micom Writing\AMW Report\Report-" + date.ToString("yyyy-MM-dd") + ".txt";
            if (File.Exists(pathReport)) // if computer has report file, push it on data grit view
            {
                var lines = File.ReadAllLines(pathReport);
                for (int i = lines.Length - 1; i >= 0; i--)
                {
                    dataInLine = lines[i].Split('|');

                        if (dataInLine[0].Contains("L"))
                        {
                            Total++;
                            dgReport.Rows.Add(Total.ToString(), dataInLine[1], dataInLine[2], "not user", dataInLine[3], dataInLine[4], dataInLine[5], dataInLine[6], dataInLine[7]);
                        }

                    if (dataInLine[1].Contains("OK")) Ok++;
                    if (dataInLine[1].Contains("FAIL")) NG++;
                }
                lbStaTTnum.Text = Total.ToString();
                lbStaOKnum.Text = Ok.ToString("D");
                lbStaNGnum.Text = NG.ToString("D");
            }
            for (int i = 0; i < dgReport.Rows.Count - 1; i++)
            {
                if (!ModelList.Contains(dgReport.Rows[i].Cells[2].Value))
                {
                    ModelList.Add(dgReport.Rows[i].Cells[2].Value.ToString());
                }
                if(dgReport.Rows[i].Cells[1].Value.ToString() == "FAIL")
                    dgReport.Rows[i].Selected = true;
            }
            modelCode = ModelList.ToArray();
            cbbPCBcode.DataSource = modelCode;

        }

        private void Report_Load(object sender, EventArgs e)
        {
            loadReport(DateTime.Now);
        }

        private void btExport_Click(object sender, EventArgs e)
        {
            exReport = "STT|" + "Final result|" + "Model " + "|" + "Bar code " + "|"  + "Time" + "|" + "Site 1" + "|" + "Site 2" + "|" + "Site 3" + "|" + "Site 4" + "\n";
            for (int i = 0; i < dgReport.Rows.Count - 1; i++)
                {
                    for (int j = 0; j < dgReport.Columns.Count; j++)
                    {
                        exReport += dgReport.Rows[i].Cells[j].Value.ToString ();
                        exReport += "|";
                    }
                    exReport += System.Environment.NewLine;
                }
            Console.WriteLine(exReport);
            saveFileReport.DefaultExt =".txt";
            saveFileReport.ShowDialog();
        }

        private void saveFileReport_FileOk(object sender, CancelEventArgs e)
        {
            ExportReportPath = saveFileReport.FileName.ToString();
            {
                using (StreamWriter sw = File.AppendText(ExportReportPath))
                {
                    sw.WriteLine(exReport);
                }
            }
        }
    }
}
