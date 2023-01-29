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

namespace ModbusRTUMaster
{
    public partial class Form1 : Form
    {
        //Tao doi tuong myPLC
        PLCPi myPLC = new PLCPi();
        byte[] Mang = new byte[20];
        bool[] MangDoc = new bool[10];
        bool[] Mang1 = new bool[60];
        byte[] value = new byte[10];

        bool[] coidValue = { true, true, true, true, true, true, true, true };
        bool[] coidValueOff = { false, false, false, false, false, false, false, false };

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            myPLC.ModbusRTUMaster.ResponseTimeout = 1000;
            if (myPLC.ModbusRTUMaster.KetNoi("/dev/ttyUSB0", 9600, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One) == true)
                label3.BackColor = Color.Green;
            else
                label3.BackColor = Color.Red;
            timer1.Enabled = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            textBox3.Text = "";
            textBox2.Text = "";
            textBox6.Text = "";
            textBox7.Text = "";
            ////coil
            if (myPLC.ModbusRTUMaster.ReadCoils(1, 0, 8, ref MangDoc) == true)
            {
                label3.BackColor = Color.Green;
                foreach (bool x in MangDoc)
                    textBox3.Text = textBox3.Text + "|" + Convert.ToString(x);
            }
            //else
            //{
            //    label3.BackColor = Color.Yellow;
            //}
            ////input
            //if (myPLC.ModbusRTUMaster.ReadDiscreteInputContact(1, 0, 10, ref MangDoc) == true)
            //{
            //    label3.BackColor = Color.Green;
            //    foreach (bool x in MangDoc)
            //        textBox2.Text = textBox2.Text + "|" + Convert.ToString(x);
            //}
            //else
            //{
            //    label3.BackColor = Color.Yellow;
            //}
            //hoding
            if (myPLC.ModbusRTUMaster.ReadHoldingRegisters(1, 0, 3, ref Mang) == true)
            {
                label3.BackColor = Color.Green;

                Console.WriteLine("doc nhiet do thanh cong{0};{1};{2}", myPLC.GetShortAt(Mang, 0), myPLC.GetShortAt(Mang, 2), myPLC.GetShortAt(Mang, 4));
            }
            else
            {
                label3.BackColor = Color.Yellow;
            }
            ////ngo vao tt
            //if (myPLC.ModbusRTUMaster.ReadInputRegisters(1, 0, 10, ref Mang) == true)
            //{
            //    label3.BackColor = Color.Green;
            //    textBox6.Text = Convert.ToString(myPLC.GetUshortAt(Mang, 0)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 2))
            //            + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 4)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 6))
            //            + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 8)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 10))
            //            + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 12)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 14))
            //            + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 16)) + "|" + Convert.ToString(myPLC.GetUshortAt(Mang, 18));
            //}
            //else
            //{
            //    label3.BackColor = Color.Yellow;
            //}

            timer1.Start();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            myPLC.SetDWord(value, 0, 1);
            myPLC.SetDWord(value, 2, 1);
            myPLC.SetDWord(value, 4, 1);

            myPLC.ModbusRTUMaster.WriteHoldingRegisters(1, 0, 3, value);
            myPLC.ModbusRTUMaster.WriteMultipleCoils(1, 0, 1, coidValue);

            //if ((textBox4.Text != "") && (textBox5.Text != ""))
            //{
            //    myPLC.SetDWord_LSB(value, 0, Convert.ToUInt32(textBox5.Text));
            //    if (textBox4.Text == "1")
            //    {
            //        for (int i = 0; i < 32; i++)
            //            Mang1[i] = true;
            //    }
            //    else
            //    {
            //        for (int i = 0; i < 32; i++)
            //            Mang1[i] = false;
            //    }
            //    myPLC.ModbusRTUMaster.WriteMultipleCoils(1, 0, 32, Mang1);
            //    Thread.Sleep(1000);
            //    myPLC.ModbusRTUMaster.WriteHoldingRegisters(1, 0, 2, value);
            //    Thread.Sleep(1000);
            //}
            timer1.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            myPLC.ModbusRTUMaster.WriteMultipleCoils(1, 0, 1, coidValueOff);

            //if ((textBox4.Text != "") && (textBox5.Text != ""))
            //{
            //    myPLC.SetDWord(value, 0, Convert.ToUInt32(textBox5.Text));
            //    if (textBox4.Text == "1")
            //    {
            //        for (int i = 0; i < 32; i++)
            //            Mang1[i] = true;
            //    }
            //    else
            //    {
            //        for (int i = 0; i < 32; i++)
            //            Mang1[i] = false;
            //    }
            //    myPLC.ModbusRTUMaster.WriteMultipleCoils(1, 0, 32, Mang1);
            //    Thread.Sleep(1000);
            //    myPLC.ModbusRTUMaster.WriteHoldingRegisters(1, 0, 2, value);
            //    Thread.Sleep(1000);
            //}
            timer1.Enabled = true;
        }

    }
}
