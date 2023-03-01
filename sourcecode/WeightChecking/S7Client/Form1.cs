using PLCPiProject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace S7Client
{
    public partial class Form1 : Form
    {
        //Tao doi tuong myPLC
        PLCPi myPLC = new PLCPi();

        byte[] Data = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        byte[] Data1 = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
        byte[] Data2 = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        byte DemLoi = 0, BienDemGuiSMS = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            if (textBox4.Text != myPLC.S7Ethernet.Client.SoLanDoc.ToString())
                myPLC.S7Ethernet.Client.SoLanDoc = Convert.ToByte(textBox4.Text);
            timer1.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox4.Text = myPLC.S7Ethernet.Client.SoLanDoc.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (myPLC.S7Ethernet.Client.KetNoi(textBox5.Text) == "GOOD")
            {
                label20.BackColor = Color.Green;
            }
            else
                label20.BackColor = Color.Red;
            //timer1.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Data[0] = 1;
            Data[1] = 2;
            Data[2] = 3;
            Data[3] = 4;
            Data[4] = 5;
            Data[5] = 6;
            Data[6] = 7;
            Data[7] = 8;
            Data[8] = 9;
            Data[9] = 10;

            myPLC.S7Ethernet.Client.GhiMB(0, 10, new byte[] { 1,2,3,4,5,6,7,8,9,10});

            //ghi vùng nhớ data block
            if (myPLC.S7Ethernet.Client.GhiDB(1, 0, 2, Data) == "GOOD")
            {
                label20.BackColor = Color.Green;
                label4.Text = "GOOD";
                DemLoi = 0;
            }
            else
            {
                label4.Text = "BAD";
                label20.BackColor = Color.Red;
                DemLoi++;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                timer1.Stop();

                textBox1.Text = "";
                textBox2.Text = "";
                textBox3.Text = "";
                textBox12.Text = "";
                if (Data2[0] == 255)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        Data[i] = 0;
                        Data1[i] = 1;
                        Data2[i] = 2;
                    }
                }

                label20.BackColor = Color.Green;
                
                //đọc vùng nhớ data block
                DocS7 myDocS72 = myPLC.S7Ethernet.Client.DocDB(1, 0, 10);
                if (myDocS72 != null)
                {
                    if (myDocS72.TrangThai == "GOOD")
                    {
                        label20.BackColor = Color.Green;
                        label14.Text = "GOOD";
                        foreach (byte b in myDocS72.MangGiaTri)
                            textBox2.Text += "|" + b.ToString();
                        DemLoi = 0;
                    }
                    else
                    {
                        label14.Text = "BAD";
                        label20.BackColor = Color.Red;
                        DemLoi++;
                    }
                }
                else
                {
                    label14.Text = "BAD";
                    label20.BackColor = Color.Red;
                    DemLoi++;
                }

                //Ghi                

                //tang gia tri de ghi xuong cac vung nho

                for (byte i2 = 0; i2 < 10; i2++)
                {
                    Data[i2] = Convert.ToByte(Data[i2] + 1);
                    Data1[i2] = Convert.ToByte(Data1[i2] + 1);
                    Data2[i2] = Convert.ToByte(Data2[i2] + 1);
                }
                if (DemLoi >= 14)
                {
                    DemLoi = 0;
                    
                }
                label7.Text = DemLoi.ToString();

                timer1.Start();
            }
            catch { timer1.Start(); }
        }
    }
}
