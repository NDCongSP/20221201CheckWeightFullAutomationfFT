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
        private string _hostName = "192.168.1.100";
        private int _port = 23;
        private DataEvent _dataEvent = new DataEvent();

        private bool _isDisconnect = false;

        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private System.Timers.Timer timer;

        private bool isReading = false;
        private Task _task;

        // Multi-code telegrams (e.g. Main QR + box-info QR sent together as one physical scan
        // event) arrive as several CR/LF-terminated lines. If a ReadLineAsync() started while
        // draining a telegram doesn't complete within this window, it's treated as belonging to
        // a future telegram rather than the current one, and is carried over via _pendingLine
        // instead of being abandoned (StreamReader is not safe for overlapping reads).
        private const int MultiCodeGraceMs = 50;
        private const int MaxLinesPerTelegram = 5;
        private Task<string> _pendingLine;

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
                _pendingLine = null;

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
                    // First line: reuse a read left in-flight from a previous tick's grace
                    // window, or start a new one. Either way this awaits until a line is
                    // actually available (unchanged blocking behavior from before).
                    Task<string> firstLineTask = _pendingLine ?? reader.ReadLineAsync();
                    _pendingLine = null;
                    string response = await firstLineTask;

                    if (!string.IsNullOrEmpty(response))
                    {
                        var lines = new List<string> { response };

                        // A single physical scan trigger can produce multiple QR codes
                        // (e.g. Main QR + box-info QR2) sent as separate CR/LF-terminated
                        // lines back-to-back. Drain any further lines that show up within a
                        // short grace window so they're delivered as ONE batched event
                        // instead of being split across separate 100ms timer ticks.
                        // A failure while OPPORTUNISTICALLY draining extra lines (e.g. the socket
                        // hiccups) must not discard the line already captured in `response` above,
                        // nor be mistaken for a hard read error by the outer catch (which would
                        // reconnect unnecessarily). Treat it the same as "nothing more arrived" —
                        // whatever real problem caused it will surface again on the next tick's
                        // primary read.
                        try
                        {
                            while (lines.Count < MaxLinesPerTelegram)
                            {
                                var nextLineTask = reader.ReadLineAsync();
                                var completed = await Task.WhenAny(nextLineTask, Task.Delay(MultiCodeGraceMs));

                                if (completed != nextLineTask)
                                {
                                    // Nothing arrived within the grace window. The read itself is
                                    // still in flight against the shared reader (StreamReader
                                    // doesn't support overlapping reads), so it must be carried
                                    // over rather than started again on the next tick.
                                    _pendingLine = nextLineTask;
                                    break;
                                }

                                string nextLine = await nextLineTask;
                                if (string.IsNullOrEmpty(nextLine))
                                    break;

                                lines.Add(nextLine);
                            }
                        }
                        catch (Exception drainEx)
                        {
                            Debug.WriteLine($"[DriverTelnet] Extra-line drain failed, delivering telegram with {lines.Count} line(s) collected so far: {drainEx}");
                        }

                        // Dispatching QRCodeValue runs the app's scanner event handler chain
                        // synchronously (e.g. DataEvent_EventHandleValueChange -> BarcodeScanner2Handle
                        // -> DB lookups/printing). Exceptions from THAT app-level code (bad QR format,
                        // missing master data, etc.) must not be caught by the network-error handler
                        // below: previously they were, which made ReadData treat an application bug as
                        // a Telnet read error and call Reconnect() — tearing down a perfectly healthy
                        // connection (dropping any bytes already buffered by the OS for it) just
                        // because one scan failed to process. That looked like "the scanner event
                        // fires once and then never again" even though the camera kept sending data.
                        try
                        {
                            _dataEvent.QRCodeValue = string.Join("\r\n", lines);
                            //Debug.WriteLine($"[{DateTime.Now}]: {_dataEvent.QRCodeValue}");
                        }
                        catch (Exception handlerEx)
                        {
                            Debug.WriteLine($"[DriverTelnet] QRCodeValue event handler threw (app-level, connection left intact): {handlerEx}");
                        }
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
