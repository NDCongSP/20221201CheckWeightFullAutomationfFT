using PLCPiProject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace S7Client
{
    public partial class ReadCognex : Form
    {
        //Tao doi tuong myPLC
        PLCPi myPLC = new PLCPi();

        public ReadCognex()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            DocS7 myDocS72 = myPLC.S7Ethernet.Client.DocIB(68, 19);
            if (myDocS72 != null)
            {
                if (myDocS72.TrangThai == "GOOD")
                {
                    label20.BackColor = Color.Green;
                    label14.Text = "GOOD";
                    foreach (byte b in myDocS72.MangGiaTri)
                        textBox12.Text = string.Join("|", textBox12.Text, b.ToString());

                }
                else
                {
                    label14.Text = "BAD";
                    label20.BackColor = Color.Red;
                }
            }
            else
            {
                label14.Text = "BAD";
                label20.BackColor = Color.Red;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Sharp7.S7Client.ISOTCP = int.Parse(_txbPort.Text);
            Sharp7.S7Client s7Client = new Sharp7.S7Client();
            var result = s7Client.ConnectTo(textBox5.Text, 0, 1);
            var text = s7Client.ErrorText(result);

            //s7Client.ConnectTo()

            if (myPLC.S7Ethernet.Client.KetNoi(textBox5.Text, 102) == "GOOD")
            {
                label20.BackColor = Color.Green;
            }
            else
                label20.BackColor = Color.Red;
            timer1.Enabled = true;
        }
    }
}
