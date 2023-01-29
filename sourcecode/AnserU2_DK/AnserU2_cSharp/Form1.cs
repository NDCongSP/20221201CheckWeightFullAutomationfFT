using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnserU2_cSharp
{
    public partial class Form1 : Form
    {
        public SerialPort _serialPort;

        public Form1()
        {
            InitializeComponent();
            Load += Form1_Load;
            FormClosing += Form1_FormClosing;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SerialClose();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SerialportConnect();
        }

        public void SerialportConnect()
        {
            DateTime dt = DateTime.Now;
            String dtn = dt.ToShortTimeString();

            _serialPort = new System.IO.Ports.SerialPort("COM4", 57600, Parity.None, 8, StopBits.One);
            try
            {
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
                _serialPort.Open();
                Console.WriteLine("[" + dtn + "] " + "Connected\n");
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString(), "Error"); }
        }

        private void SerialClose()
        {
            DateTime dt = DateTime.Now;
            String dtn = dt.ToShortTimeString();

            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //try
            {
                System.Threading.Thread.Sleep(100);
                string dataRCV = _serialPort.ReadExisting(); // Read
                if (string.IsNullOrEmpty(dataRCV))
                {
                    return;
                }

                var rcvArr = Encoding.ASCII.GetBytes(dataRCV);

                Console.WriteLine(dataRCV);

                //xet phan tu thu 4 trong mang rcvArr[4] de check status
                //0x4F-79--> OK
                //0x31-49-->Fail
                //0x30-49-->may in phan hoi in thanh cong

                //Printed event
                if (rcvArr[4] == 0x30)
                {
                    Console.WriteLine($"in thanh cong!!!");

                    //xoa string
                    SendDynamicString(" ", " ");
                }
                else if (rcvArr[4] == 0x4F)
                {
                    Console.WriteLine($"Gui lenh xuong may in thanh cong!!!");
                }
                else if (rcvArr[4] == 0x31)
                {
                    Console.WriteLine($"Loi. Error Code: {rcvArr[5]}");
                    MessageBox.Show($"Send command error: Error code: {rcvArr[5]}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (rcvArr[4] == 0x5D)//93 get speed
                {
                    var speedPV = (double)(rcvArr[5] + rcvArr[6] * 0x100 + rcvArr[7] * 0x1000 + rcvArr[8] * 0x10000);
                    speedPV = Math.Round(speedPV / 1000, 2);

                    if (txt_speedPV.InvokeRequired)
                    {
                        txt_speedPV.Invoke(new Action(() =>
                        {
                            txt_speedPV.Text = speedPV.ToString();
                        }));
                    }
                    else
                    {
                        txt_speedPV.Text = speedPV.ToString();
                    }
                }
                else if (rcvArr[4] == 0x64)//100 get delay
                {
                    var delayPV = (double)(rcvArr[5] + rcvArr[6] * 0x100 + rcvArr[7] * 0x1000 + rcvArr[8] * 0x10000);
                    delayPV = Math.Round(delayPV / 100, 2);

                    if (txtDelayPV.InvokeRequired)
                    {
                        txtDelayPV.Invoke(new Action(() =>
                        {
                            txtDelayPV.Text = delayPV.ToString();
                        }));
                    }
                    else
                    {
                        txtDelayPV.Text = delayPV.ToString();
                    }
                }

                if (labStatus.InvokeRequired)
                {
                    labStatus.Invoke(new Action(() =>
                    {
                        labStatus.Text = string.Empty;
                        foreach (var item in rcvArr)
                        {
                            labStatus.Text += $"{item} ";
                        }
                    }));
                }
                else
                {
                    labStatus.Text = string.Empty;
                    foreach (var item in rcvArr)
                    {
                        labStatus.Text += $"{item} ";
                    }
                }
            }
            //catch (Exception)
            //{

            //    throw;
            //}
        }

        private void btn_sendSTRING1_Click(object sender, EventArgs e)
        {
            string string1 = txtString1.Text;
            string string2 = txtString2.Text;

            SendDynamicString(string1, string2);
        }

        private void btn_startprint_Click(object sender, EventArgs e)
        {
            byte[] SetPtinting = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x46, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3 };
            // Gán số thứ tự của bản tin cần in vào array
            SetPtinting[5] = System.Convert.ToByte(cbbIDMSG.SelectedItem);
            // Tính checksum
            byte chkSUM = 0;
            for (var i = 1; i <= SetPtinting.Length - 3; i++)
                chkSUM = (byte)(chkSUM + SetPtinting[i]);
            // Gán giá trị checksum vào array
            SetPtinting[9] = chkSUM;
            // Gửi array xuống máy in
            _serialPort.Write(SetPtinting, 0, SetPtinting.Length);
        }

        private void btn_stopprint_Click(object sender, EventArgs e)
        {
            byte[] SetPtinting = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x46, 0x0, 0x0, 0x0, 0x0, 0x4C, 0x3 };

            _serialPort.Write(SetPtinting, 0, SetPtinting.Length);
        }

        private void SendDynamicString(string string1, string string2)
        {
            int i = 0, j = 0;
            int chkSUM = 0;

            byte[] SetDynamicString = new byte[14 + string1.Length + string2.Length];
            SetDynamicString[0] = 0x2;
            SetDynamicString[1] = 0x0;
            SetDynamicString[2] = (byte)(9 + string1.Length + string2.Length);
            SetDynamicString[3] = 0x0;
            SetDynamicString[4] = 0xCA; // Mã lệnh Set dynamic string
            SetDynamicString[5] = 0;
            SetDynamicString[6] = 0;
            SetDynamicString[7] = (byte)(string1.Length); // Chiều dài của string 1
            SetDynamicString[8] = (byte)(string2.Length); // Chiều dài của string 2
            SetDynamicString[9] = 0; // Chiều dài của string 3
            SetDynamicString[10] = 0; // Chiều dài của string 4
            SetDynamicString[11] = 0; // Chiều dài của string 5

            //chuyen string sang ASCII
            var string1Arr = string1.ToCharArray();
            var string2Arr = string2.ToCharArray();

            byte[] string1Ascii = Encoding.ASCII.GetBytes(string1Arr);
            byte[] string2Ascii = Encoding.ASCII.GetBytes(string2Arr);

            for (i = 0; i <= string1Ascii.Length - 1; i++)
            {
                SetDynamicString[12 + i] = string1Ascii[i];// Nội dung của string 1
            }

            for (j = 0; j <= string2Ascii.Length - 1; j++)
            {
                SetDynamicString[12 + i + j] = string2Ascii[j];// Nội dung của string 1
            }

            // Tính check SUM
            for (var c = 1; c <= i + j + 12; c++)
                chkSUM = chkSUM + SetDynamicString[c];
            chkSUM = chkSUM & 0xFF;
            SetDynamicString[i + j + 12] = System.Convert.ToByte(chkSUM); // Gán byte checksum vào arr
            SetDynamicString[i + j + 12 + 1] = 0x3;

            _serialPort.Write(SetDynamicString, 0, SetDynamicString.Length);
        }

        private void btn_Setspeed_Click(object sender, EventArgs e)
        {
            byte[] setSpeed = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x5E, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3 };

            int speedSV = (int)(double.TryParse(txt_SpeedSV.Text, out double value) ? value * 1000 : 0);

            byte[] speedSVarr = BitConverter.GetBytes(speedSV);

            // Gán giá trị tốc độ vào arr
            var i = 0;
            foreach (var item in speedSVarr)
            {
                setSpeed[5 + i] = item;
                i += 1;
            }

            // Tính checksum
            int chksum = 0;
            for (var j = 1; j <= setSpeed.Length - 2; j++)
                chksum = chksum + setSpeed[j];
            chksum = chksum & 0xFF;
            // Gán checksum vào arr
            setSpeed[9] = System.Convert.ToByte(chksum);
            // Gửi xuống máy in
            _serialPort.Write(setSpeed, 0, setSpeed.Length);
        }

        private void btn_GetSpeed_Click(object sender, EventArgs e)
        {
            byte[] GetSpeed = new byte[] { 0x2, 0x0, 0x2, 0x0, 0x5D, 0x5F, 0x3 };
            _serialPort.Write(GetSpeed, 0, GetSpeed.Length);
        }

        private void btnGetDelay_Click(object sender, EventArgs e)
        {
            byte[] GetDelay = new byte[] { 0x2, 0x0, 0x4, 0x0, 0x64, 0x2, 0x0, 0x6A, 0x3 };
            // ID ban tin =2
            _serialPort.Write(GetDelay, 0, GetDelay.Length);
        }

        private void btnSetDelay_Click(object sender, EventArgs e)
        {
            byte[] setDelay = new byte[] { 0x2, 0x0, 0x8, 0x0, 0x65, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3 };
            // Gán IDbản tin
            setDelay[5] = 0x2;

            int delaySV = (int)(double.TryParse(txtDelaySV.Text, out double value) ? value * 10 : 0);

            byte[] delaySVarr = BitConverter.GetBytes(delaySV);

            // Gán giá trị tốc độ vào arr
            var i = 0;
            foreach (var item in delaySVarr)
            {
                setDelay[7 + i] = item;
                i += 1;
            }

            // Tính checksum
            int chksum = 0;
            for (var j = 1; j <= setDelay.Length - 2; j++)
                chksum = chksum + setDelay[j];
            chksum = chksum & 0xFF;
            // Gán checksum vào arr
            setDelay[11] = System.Convert.ToByte(chksum);
            // Gửi xuống máy in
            _serialPort.Write(setDelay, 0, setDelay.Length);
        }
    }
}
