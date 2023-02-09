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
            trangthai = _plcPi.S7Ethernet.Server.Khoitao();

            if (trangthai == "GOOD")
            {
                panel1.BackColor = Color.Green;
            }
            else
            {
                panel1.BackColor = Color.Red;
            }

            _timer.Interval = 100;
            _timer.Tick += _timer_Tick;
            _timer.Enabled = true;
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            _timer.Enabled = false;

            label2.Text = Convert.ToString(_plcPi.S7Ethernet.Server.DataBlock[1]);
            label4.Text = Convert.ToString(_plcPi.S7Ethernet.Server.DataBlock[0]);

            //_plcPi.S7Ethernet.Server.DataBlock[0] = Convert.ToByte(textBox2.Text);
            
            _timer.Enabled = true;
        }
    }
}
