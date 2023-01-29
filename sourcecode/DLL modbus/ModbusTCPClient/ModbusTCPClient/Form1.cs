using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using PLCPiProject;

namespace ModbusTCPClient
{
    public partial class Form1 : Form
    {
        //Tao doi tuong myPLC
        PLCPi myPLC = new PLCPi();
        byte[] Mang = new byte[20];
        byte[] value = { 0, 0, 0, 0 };
        byte[] value3 = new byte[4];

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                timer1.Stop();
                myPLC.ModbusTCPClient.ReadHoldingRegister(0, 0, 10, ref Mang);
                if (Mang != null)
                {
                    textBox7.Text = Convert.ToString(myPLC.GetUshortAt(Mang, 0)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 2))
                        + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 4)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 6))
                        + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 8)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 10))
                        + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 12)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 14))
                        + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 16)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 18));
                }
                else
                    label3.BackColor = Color.Yellow;
                ////////////////////////////////////////////////////////////////
                myPLC.ModbusTCPClient.ReadCoils(0, 0, 10, ref Mang);
                if (Mang != null)
                {
                    textBox3.Text = Convert.ToString(myPLC.GetBoolAt(Mang, 0, 0))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 1))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 2))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 3))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 4))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 5))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 6))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 7))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 1, 0))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 1, 1));
                }
                else
                    label3.BackColor = Color.Yellow;
                /////////////////////////////////////////////////////////////
                myPLC.ModbusTCPClient.ReadDiscreteInputs(0, 0, 10, ref Mang);
                if (Mang != null)
                    textBox2.Text = Convert.ToString(myPLC.GetBoolAt(Mang, 0, 0))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 1))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 2))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 3))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 4))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 5))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 6))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 0, 7))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 1, 0))
                    + "|" + Convert.ToString(myPLC.GetBoolAt(Mang, 1, 1));
                else
                    label3.BackColor = Color.Yellow;
                ///////////////////////////////////////////////////////////////////////
                myPLC.ModbusTCPClient.ReadInputRegister(0, 0, 10, ref Mang);
                if (Mang != null)
                {
                    textBox6.Text = Convert.ToString(myPLC.GetUshortAt(Mang, 0)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 2))
                        + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 4)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 6))
                        + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 8)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 10))
                        + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 12)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 14))
                        + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 16)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 18));
                }
                else
                    label3.BackColor = Color.Yellow;

                timer1.Start();
            }
            catch { }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            myPLC.ModbusTCPClient.KetNoi(textBox1.Text, 502);
            if (myPLC.ModbusTCPClient.connected == true)
            {
                label3.BackColor = Color.Green;
                timer1.Enabled = true;
            }
            else
                label3.BackColor = Color.Red;

        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            if ((textBox4.Text != "") && (textBox5.Text != ""))
            {
                myPLC.SetDWord(value3, 0, Convert.ToUInt32(textBox5.Text));

                if (textBox4.Text == "1")
                {
                    for (byte i = 0; i < 4; i++)
                        value[i] = 255;
                }
                else
                {
                    for (byte i = 0; i < 4; i++)
                        value[i] = 0;
                }
                myPLC.ModbusTCPClient.WriteMultipleCoils(0, 0, 29, value, ref Mang);
                myPLC.ModbusTCPClient.WriteMultipleRegister(0, 0, value3, ref Mang);
                if (Mang == null)
                    label3.BackColor = Color.Orange;
            }
            timer1.Enabled = true;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            if ((textBox4.Text != "") && (textBox5.Text != ""))
            {
                myPLC.SetDWord_LSB(value3, 0, Convert.ToUInt32(textBox5.Text));

                if (textBox4.Text == "1")
                {
                    for (byte i = 0; i < 4; i++)
                        value[i] = 255;
                }
                else
                {
                    for (byte i = 0; i < 4; i++)
                        value[i] = 0;
                }
                myPLC.ModbusTCPClient.WriteMultipleCoils(0, 0, 29, value, ref Mang);
                myPLC.ModbusTCPClient.WriteMultipleRegister(0, 0, value3, ref Mang);
                if (Mang == null)
                    label3.BackColor = Color.Orange;
            }
            timer1.Enabled = true;
        }

    }
}
