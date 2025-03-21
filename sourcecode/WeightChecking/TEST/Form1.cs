using CognexLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace TEST
{
    public partial class Form1 : Form
    {
        private static TcpClient client;
        private static NetworkStream stream;
        private static StreamReader reader;
        private static System.Timers.Timer timer;
        private static CognexLibrary.DriverTelnet _driverTelnet = new CognexLibrary.DriverTelnet();

        private static bool isReading = false;


        public Form1()
        {
            InitializeComponent();
            Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _driverTelnet.HostName = "192.168.80.4";
            _driverTelnet.Port = 23;

            _driverTelnet.DataEvent.EventHandleValueChange += DataEvent_EventHandleValueChange;
            _driverTelnet.DataEvent.EventHandleStatusChange += DataEvent_EventHandleStatusChange;

            _driverTelnet.ConnectDevices();

            //string host = "192.168.80.4"; // Change to your device IP
            //int port = 23; // Telnet port

            //try
            //{
            //    client = new TcpClient();
            //    await client.ConnectAsync(host, port);
            //    Console.WriteLine($"Connected to {host}:{port}");

            //    stream = client.GetStream();
            //    reader = new StreamReader(stream, Encoding.ASCII);

            //    // Set up a timer to read data every 2 seconds
            //    timer = new System.Timers.Timer(100); // 2000ms = 2 seconds
            //    timer.Elapsed += ReadData;
            //    timer.AutoReset = true;
            //    timer.Start();

            //    //Console.WriteLine("Press Enter to exit...");
            //    //Console.ReadLine();

            //    //// Cleanup
            //    //timer.Stop();
            //    //reader.Dispose();
            //    //stream.Dispose();
            //    //client.Close();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error: {ex.Message}");
            //}
        }

        private void DataEvent_EventHandleStatusChange(object sender, StatusChangeEventArgs e)
        {
            Debug.WriteLine($"[{DateTime.Now}]: {e.Status}|{e.Exception?.Message}");

            SafeInvoke(this, () =>
            {
                _labStatus.Text = $"[{DateTime.Now}]: {e.Status}|{e.Exception?.Message}";                
            });
        }

        private void DataEvent_EventHandleValueChange(object sender, ValueChangeEventArgs e)
        {
            Debug.WriteLine($"[{DateTime.Now}]: {e.NewValue}|{e.OldValue}");

            SafeInvoke(this, () =>
            {
                _labNewValue.Text = $"[{DateTime.Now}]: {e.NewValue}";
                _labOldValue.Text = $"[{DateTime.Now}]: {e.OldValue}";
            });
        }

        private static async void ReadData(object sender, ElapsedEventArgs e)
        {
            if (isReading) return; // Skip execution if a read is already in progress
            isReading = true;

            try
            {
                if (reader != null)
                {
                    string response = await reader?.ReadLineAsync();
                    if (!string.IsNullOrEmpty(response))
                    {
                        Debug.WriteLine($"[{DateTime.Now}]: {response}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Read error: {ex.Message}");
            }
            finally
            {
                isReading = false; // Reset flag after operation completes
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            _driverTelnet.IsDisconect = true;
            _driverTelnet?.DisconnectDevices();
        }

        public static void SafeInvoke(Control control, Action action)
        {
            if (control.InvokeRequired)
                control.Invoke(action);
            else
                action();
        }

        private void _btnConnect_Click(object sender, EventArgs e)
        {
            _driverTelnet?.ConnectDevices();
        }

        private void _btnReconnect_Click(object sender, EventArgs e)
        {
            _driverTelnet.Reconnect();
        }
    }
}
