using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PLCPiProject;

namespace S7Server
{
    public partial class Form1 : Form
    {
        private byte _metalSensorBefore = 0;
        private byte _metalSensorAfter = 0;
        private byte _metalCheckResult = 0;
        private byte _mp = 0;
        private byte _cp = 0;
        private byte _pp = 0;
        private byte _m = 0;
        private byte _sensorMiddleMetal = 0;
        byte _printLeftSensor = 0, _printRightSensor = 0;


        private PLCPi _plcPi = new PLCPi();
        string trangthai;

        private Timer _timer = new Timer();
        public Form1()
        {
            InitializeComponent();
            Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            trangthai = _plcPi.S7Ethernet.Server.Khoitao("192.168.80.201", 105);

            if (trangthai == "GOOD")
            {
                panel1.BackColor = Color.Green;
            }
            else
            {
                panel1.BackColor = Color.Red;
            }

            textBox1.TextChanged += TextBox1_TextChanged;
            textBox2.TextChanged += TextBox2_TextChanged;
            textBox3.TextChanged += TextBox3_TextChanged;

            _txtSensorPrintLeft.TextChanged += (s, o) =>
            {
                TextBox t = (TextBox)s;

                try
                {
                    _printLeftSensor = Convert.ToByte(t.Text);
                }
                catch (Exception)
                {

                    throw;
                }
            };
            _txtSensorPrintRight.TextChanged += (s, o) =>
            {
                TextBox t = (TextBox)s;

                try
                {
                    _printRightSensor = Convert.ToByte(t.Text);
                }
                catch (Exception)
                {

                    throw;
                }
            };


            txtmp.TextChanged += (s, o) =>
            {
                TextBox t = (TextBox)s;

                try
                {
                    _mp = Convert.ToByte(t.Text);
                }
                catch (Exception)
                {

                    throw;
                }
            };
            txtsp.TextChanged += (s, o) =>
            {
                TextBox t = (TextBox)s;

                try
                {
                    _cp = Convert.ToByte(t.Text);
                }
                catch (Exception)
                {

                    throw;
                }
            };
            txtpp.TextChanged += (s, o) =>
            {
                TextBox t = (TextBox)s;

                try
                {
                    _pp = Convert.ToByte(t.Text);
                }
                catch (Exception)
                {

                    throw;
                }
            };
            textBox4.TextChanged += (s, o) =>
            {
                TextBox t = (TextBox)s;

                try
                {
                    _m = Convert.ToByte(t.Text);
                }
                catch (Exception)
                {

                    throw;
                }
            };

            txtSensoeMiddleMetal.TextChanged += (s, o) =>
            {
                TextBox t = (TextBox)s;

                try
                {
                    _sensorMiddleMetal = Convert.ToByte(t.Text);
                }
                catch (Exception)
                {

                    throw;
                }
            };

            _timer.Interval = 1000;
            _timer.Tick += _timer_Tick;
            _timer.Enabled = true;
        }

        private void TextBox3_TextChanged(object sender, EventArgs e)
        {
            TextBox t = (TextBox)sender;

            try
            {
                _metalCheckResult = Convert.ToByte(t.Text);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void TextBox2_TextChanged(object sender, EventArgs e)
        {
            TextBox t = (TextBox)sender;

            try
            {
                _metalSensorAfter = Convert.ToByte(t.Text);
            }
            catch (Exception)
            {

                throw;
            }
        }

        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            TextBox t = (TextBox)sender;
            try
            {
                _metalSensorBefore = Convert.ToByte(t.Text);
            }
            catch (Exception)
            {

                throw;
            }
        }


        private void _timer_Tick(object sender, EventArgs e)
        {
            _timer.Enabled = false;

            label4.Text = Convert.ToString(_plcPi.S7Ethernet.Server.DataBlock[0]);
            label2.Text = Convert.ToString(_plcPi.S7Ethernet.Server.DataBlock[1]);
            label5.Text = Convert.ToString(_plcPi.S7Ethernet.Server.DataBlock[2]);
            label10.Text = Convert.ToString(_plcPi.S7Ethernet.Server.DataBlock[3]);
            label14.Text = Convert.ToString(_plcPi.S7Ethernet.Server.DataBlock[4]);
            label12.Text = Convert.ToString(_plcPi.S7Ethernet.Server.DataBlock[5]);
            label16.Text = Convert.ToString(_plcPi.S7Ethernet.Server.DataBlock[6]);
            labSensorMiddleMetal.Text = Convert.ToString(_plcPi.S7Ethernet.Server.DataBlock[7]);

            _plcPi.S7Ethernet.Server.DataBlock[3] = _metalSensorBefore;
            _plcPi.S7Ethernet.Server.DataBlock[4] = _metalCheckResult;
            _plcPi.S7Ethernet.Server.DataBlock[5] = _metalSensorAfter;
            _plcPi.S7Ethernet.Server.DataBlock[7] = _sensorMiddleMetal;
            _plcPi.S7Ethernet.Server.DataBlock[8] = _printLeftSensor;
            _plcPi.S7Ethernet.Server.DataBlock[9] = _printRightSensor;

            _timer.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _plcPi.S7Ethernet.Server.DataBlock[0] = _mp;
            _plcPi.S7Ethernet.Server.DataBlock[1] = _cp;
            _plcPi.S7Ethernet.Server.DataBlock[2] = _pp;
            _plcPi.S7Ethernet.Server.DataBlock[6] = _m;
        }
    }
}
