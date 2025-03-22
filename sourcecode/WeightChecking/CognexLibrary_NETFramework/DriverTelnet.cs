using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

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
        /// Telnet port. Default port 23
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

        public bool IsDisconect
        {
            get => _isDisconnect;
            set
            {
                if (_isDisconnect != value)
                    _isDisconnect = value;
            }
        }
        #endregion
        private static string _hostName = "192.168.1.100";
        private static int _port = 23;
        private static DataEvent _dataEvent = new DataEvent();

        private static bool _isDisconnect = false;

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
                _isDisconnect = false;//disable disconnect flag to reconnect when connection is lost.
                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                }
                client = new TcpClient();
                await client.ConnectAsync(_hostName, _port);
                Console.WriteLine($"Connected to {_hostName}:{_port}");

                stream = client.GetStream();
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                reader = new StreamReader(stream, Encoding.ASCII);

                // Set up a timer to read data every 2 seconds
                if (timer != null)
                {
                    timer.Close();
                    timer.Dispose();
                }
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
                if (!_isDisconnect)
                    await Reconnect();
                //Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public async Task Reconnect()
        {
            Debug.WriteLine("Reconnecting...");
            try
            {
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
            catch (Exception ex)
            {
                _dataEvent.ExceptionLog = ex;
                _dataEvent.Status = "Error";
            }
        }

        /// <summary>
        /// Need to set _isDisconnect = true to avoid reconnecting when disconnecting directly.
        /// </summary>
        public void DisconnectDevices()
        {
            Debug.WriteLine("Disconnecting...");
            try
            {
                timer?.Stop();
                reader?.Dispose();
                stream?.Dispose();
                client?.Close();

                //_dataEvent.ExceptionLog = null;
                //_dataEvent.Status = "Disconnected";
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
                if (!_isDisconnect)
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
