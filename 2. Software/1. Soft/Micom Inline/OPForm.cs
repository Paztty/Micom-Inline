using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Micom_Inline
{
    public partial class OPForm : Form
    {
        public OPForm()
        {
            InitializeComponent();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            if (textBox1.Text == "OP")
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }

        }

        private void OPForm_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                if (textBox1.Text == "OP")
                    this.DialogResult = DialogResult.OK;
                else
                    this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void OPForm_Load(object sender, EventArgs e)
        {

        }
    }
}
