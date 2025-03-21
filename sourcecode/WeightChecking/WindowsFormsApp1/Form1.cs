using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CognexLibrary_NETFramework;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private CognexLibrary_NETFramework.DriverTelnet driverTelnet = new CognexLibrary_NETFramework.DriverTelnet();
        public Form1()
        {
            InitializeComponent();

            driverTelnet.HostName = "192.168.80.4";
            driverTelnet.Port = 23;
            driverTelnet.DataEvent.EventHandleValueChange += DataEvent_EventHandleValueChange;
            driverTelnet.ConnectDevices();
        }

        private void DataEvent_EventHandleValueChange(object sender, ValueChangeEventArgs e)
        {
            Debug.WriteLine(e.NewValue);
        }
    }
}
