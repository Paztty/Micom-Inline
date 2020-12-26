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
    public partial class PassWorldForm : Form
    {
        AMW_CONFIG admin = new AMW_CONFIG();
        public PassWorldForm()
        {
            InitializeComponent();
        }

        private void PassWorldForm_Load(object sender, EventArgs e)
        {
            panelChangePass.Visible = false;
            panelChangePass.Height = 0;
        }

        private void Apply_Click(object sender, EventArgs e)
        {
            if (!panelChangePass.Visible)
            {
                if (textBox1.Text == admin.MANAGER_PASS)
                    this.DialogResult = DialogResult.OK;
                else if (textBox1.Text == admin.ADMIN_PASS)
                    this.DialogResult = DialogResult.Ignore;
                else
                {
                    lbPassResult.Text = "Passwold not correct!";
                }
            }
            else
            {
                if (textBox1.Text == admin.MANAGER_PASS)
                {
                    if (textBox2.Text == textBox3.Text)
                    {
                        admin.MANAGER_PASS = textBox3.Text;
                        admin.SaveConfig();
                        panelChangePass.Visible = false;
                        lbPassResult.Text = "Successful change";
                    }
                    else
                    {
                        lbPassResult.Text = "New password is not match";
                    }
                }
                else if (textBox1.Text == admin.ADMIN_PASS)
                {
                    if (textBox2.Text == textBox3.Text)
                    {
                        admin.ADMIN_PASS = textBox3.Text;
                        admin.SaveConfig();
                        panelChangePass.Visible = false;
                        lbPassResult.Text = "Successful change";
                    }
                    else
                    {
                        lbPassResult.Text = "New password is not match";
                    }
                }
                else
                {
                    lbPassResult.Text = "Passwold not correct!";
                }
            }
            
        }
        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!panelChangePass.Visible)
            {
                if (e.KeyChar == 13)
                {
                    if (textBox1.Text == admin.MANAGER_PASS)
                    {
                        this.DialogResult = DialogResult.OK;
                    }
                    else if (textBox1.Text == admin.ADMIN_PASS)
                    {
                        this.DialogResult = DialogResult.Ignore;
                    }
                    else
                    {
                        lbPassResult.Text = "Passwold not correct!";
                    }
                }
            }
        }
        private void btChangePassword_Click(object sender, EventArgs e)
        {
            if(!panelChangePass.Visible)
            {
                panelChangePass.Visible = true;
                panelChangePass.Height = 100;
            }
                
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
