using System.Net.Sockets;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using System.IO;
using System.Threading.Tasks;
using System;

namespace CognexLibrary_NETFramework
{
    public class DriverTelnet
    {
        #region Public properties
        /// <summary>
        /// Change to your device IP.
        /// </summary>
        public string HostName
        {
            set
            {
                if (_hostName != value)
                    _hostName = value;
            }
            get
            {
                return _hostName;
            }
        }
        /// <summary>
        /// Telnet port.
        /// </summary>
        public int Port
        {
            set
            {
                if (_port != value)
                    _port = value;
            }
            get
            {
                return _port;
            }
        }

        public DataEvent DataEvent
        {
            get => _dataEvent;
            set
            {
                if (_dataEvent != value)
                    _dataEvent = value;
            }
        }
        #endregion
        private static string _hostName = "192.168.1.100";
        private static int _port = 24;
        private static DataEvent _dataEvent = new DataEvent();

        private static TcpClient client;
        private static NetworkStream stream;
        private static StreamReader reader;
        private static System.Timers.Timer timer;

        private static bool isReading = false;
        private static Task _task;

        public async Task ConnectDevices()
        {
            //string host = "192.168.80.4"; // Change to your device IP
            //int port = 23; // Telnet port

            try
            {
                Debug.WriteLine("Connecting...");
                client = new TcpClient();
                await client.ConnectAsync(_hostName, _port);
                Console.WriteLine($"Connected to {_hostName}:{_port}");

                stream = client.GetStream();
                reader = new StreamReader(stream, Encoding.ASCII);

                // Set up a timer to read data every 2 seconds
                timer = new System.Timers.Timer(100); // 2000ms = 2 seconds
                timer.Elapsed += ReadData;
                timer.AutoReset = true;
                timer.Start();

                _dataEvent.ExceptionLog = null;
                _dataEvent.Status = "Connected";
                Debug.WriteLine($"Cognex status: Connected");
            }
            catch (Exception ex)
            {
                _dataEvent.ExceptionLog = ex;
                _dataEvent.Status = "Error";

                // Directly await the reconnect method
                await Reconnect();
                //Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task Reconnect()
        {
            Debug.WriteLine("Reconnecting...");
            //while (_dataEvent.Status != "Connected")
            {
                DisconnectDevices();
                await Task.Delay(1000);
                await ConnectDevices();

                // Check if connection is restored
                //if (_dataEvent.Status == "Connected")
                //    break;

                //await Task.Delay(1000); // Non-blocking alternative to Thread.Sleep(1000)
            }
        }
        private void DisconnectDevices()
        {
            Debug.WriteLine("Disconnecting...");
            try
            {
                timer?.Stop();
                reader?.Dispose();
                stream?.Dispose();
                client?.Close();

                _dataEvent.ExceptionLog = null;
                _dataEvent.Status = "Disconnected";
            }
            catch (Exception ex)
            {
                _dataEvent.ExceptionLog = ex;
                _dataEvent.Status = "Error";
            }
        }

        private async void ReadData(object sender, ElapsedEventArgs e)
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
                        _dataEvent.QRCodeValue = response;
                        //Debug.WriteLine($"[{DateTime.Now}]: {response}");
                    }
                }
            }
            catch (Exception ex)
            {
                _dataEvent.ExceptionLog = ex;
                _dataEvent.Status = "Error";

                // Directly await the reconnect method
                await Reconnect();
                //Console.WriteLine($"Read error: {ex.Message}");
            }
            finally
            {
                isReading = false; // Reset flag after operation completes
            }
        }
    }
}
