using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WeightChecking
{
    //disable properties multi-Packet trong UART-Control of converter (ATC 1200)
    public class ScaleHelper : IDisposable
    {
        System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        int bytesRec = 0;
        Socket sender;
        byte[] bytes = new byte[50];
        string valueStr = "";
        Regex digits;
        Match mx;
        Task tskReadScale;
        static object _lockObj = new object();
        public CancellationTokenSource cancelTokenSrc = new CancellationTokenSource();
        public string Ip = "192.168.100.123";
        public int Port = 23;
        public int ScaleDelay = 1000;//thong so de delay moi lan yeu cau laay du lieu can
        public bool StopScale { get; set; } = false;

        int countReadData = 0, countReadDataOld = 0, CountDisconnect = 0;

        public double oldScale = 0;

        private double _value = 0, _valueNew = 0;
        private string _status = "Dis";

        public double ScaleValue
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnValueChanged(value);
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnStatusChanged(value);
                }
            }
        }

        private event EventHandler<ScaleValueChangedEventArgs> _valueChanged;
        public event EventHandler<ScaleValueChangedEventArgs> ValueChanged
        {
            add { _valueChanged += value; }
            remove { _valueChanged -= value; }
        }

        private void OnValueChanged(double value)
        {
            _valueChanged?.Invoke(this, new ScaleValueChangedEventArgs(value));
        }

        private event EventHandler<ScaleValueChangedEventArgs> _statusChanged;
        public event EventHandler<ScaleValueChangedEventArgs> StatusChanged
        {
            add { _statusChanged += value; }
            remove { _statusChanged -= value; }
        }
        private void OnStatusChanged(string value)
        {
            _statusChanged?.Invoke(this, new ScaleValueChangedEventArgs(value));
        }


        public ScaleHelper()
        {

        }
        public void CheckConnect([CallerMemberName] string caller = null)
        {
            //Task.Factory.StartNew(() =>
            //{
            //    StartReadScale();
            //});
            tskReadScale = new Task(() => StartReadScale(), cancelTokenSrc.Token);
            tskReadScale.Start();

            while (true)
            {
                if (StopScale)
                {
                    //tskReadScale.Wait();
                    //tskReadScale.Dispose();

                    break;
                }
                try
                {
                    Ping ping = new Ping();
                    PingReply reply = ping.Send(Ip);

                    if (reply.Status != IPStatus.Success)
                    {
                        CountDisconnect += 1;
                    }
                    else
                    {
                        CountDisconnect = 0;
                    }
                    //if (countReadData != countReadDataOld)
                    //{
                    //    countReadDataOld = countReadData;
                    //    CountDisconnect = 0;
                    //}
                    //else
                    //{
                    //    CountDisconnect += 1;
                    //}

                    if (CountDisconnect >= 3)
                    {
                        Status = "Disconnection";
                        CountDisconnect = 0;
                        cancelTokenSrc.Cancel();
                        //Task.Factory.StartNew(()=>StartReadScale());
                        tskReadScale = new Task(() => StartReadScale());
                        tskReadScale.Start();
                    }

                    //Debug.WriteLine($"Check conection scale: {_status} | thang nao goi: {caller}");
                }
                catch { }
                Thread.Sleep(1000);
            }
            //Debug.WriteLine($"thoat vong lap kiem tra ket noi: StopScale = {StopScale}");
        }

        public void StartReadScale()
        {
            // Create a TCP/IP  socket.    
            sender = new Socket(SocketType.Stream, ProtocolType.Tcp);
            // Connect the socket to the remote endpoint. Catch any errors.    
            try
            {
                // Connect to Remote EndPoint  
                sender.Connect(Ip, Port);
                //sender.Connect("192.168.1.237", Port);
                Status = "Connected";
                string rawData = "";
                sender.ReceiveBufferSize = 30;
                while (true)
                {
                    if (StopScale)
                    {
                        break;
                    }

                    using (NetworkStream _stream = new NetworkStream(sender))
                    {

                        if (_stream != null)
                        {
                            using (StreamReader _reader = new StreamReader(_stream))
                            {
                                if (_reader != null)
                                {
                                    rawData = _reader.ReadLine();
                                    rawData?.ToCharArray().Select((o, i) => o == '\r' ? i : -1).Where(i => i != -1).ToList();
                                    //Debug.WriteLine(rawData);
                                }
                            }
                        }

                    }
                    //bytesRec = sender.Receive(bytes);

                    //valueStr = Reverse(Encoding.ASCII.GetString(bytes, 0, bytesRec));//dùng với trường hợp cân ở cookie
                    //string valueStr = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    //Debug.WriteLine($"Value String Nguyen Mau: {valueStr}");


                    //Debug.WriteLine($"Value String Nguyen Mau: {valueStr}");

                    //xử lý data đọc về
                    //if (valueStr.Substring(0, 1) != "+" && valueStr.Substring(0, 1) != "-")
                    //if (((valueStr.Substring(0, 7) != "ST,GS,+" && valueStr.Substring(0, 7) != "ST,GS,-")
                    //    || (valueStr.Substring(0, 4) != "ST,+" && valueStr.Substring(0, 4) != "ST,-")
                    //    || (valueStr.Substring(0, 1) != "+" && valueStr.Substring(0, 1) != "-"&& valueStr.Substring(11, 1) != "S"))
                    //    && valueStr.Length > 7)
                    //{
                    //    int _indexChar = 0;
                    //    if (valueStr.Contains("+"))
                    //    {
                    //        _indexChar = valueStr.IndexOf("+");
                    //    }
                    //    else
                    //    {
                    //        _indexChar = valueStr.IndexOf("-");
                    //    }

                    //    if (_indexChar > 0)
                    //    {
                    //        valueStr = valueStr.Substring(_indexChar);
                    //    }
                    //}
                    //if (valueStr.Length > 7)
                    //{
                    //    if (((valueStr.Substring(0, 7) != "ST,GS,+" && valueStr.Substring(0, 7) != "ST,GS,-")
                    //    || (valueStr.Substring(0, 4) != "ST,+" && valueStr.Substring(0, 4) != "ST,-")
                    //    || (valueStr.Substring(0, 1) != "+" && valueStr.Substring(0, 1) != "-" && valueStr.Substring(11, 1) != "S"))
                    //    )
                    //    {
                    //        int _indexChar = 0;
                    //        if (valueStr.Contains("+"))
                    //        {
                    //            _indexChar = valueStr.IndexOf("+");
                    //        }
                    //        else
                    //        {
                    //            _indexChar = valueStr.IndexOf("-");
                    //        }

                    //        if (_indexChar > 0)
                    //        {
                    //            valueStr = valueStr.Substring(_indexChar);
                    //        }
                    //    }
                    //}

                    //if (valueStr?.Length >= 7)
                    //{
                    //    int plusIndex = valueStr.IndexOf("+");
                    //    //Debug.WriteLine($"Plus Index: {plusIndex}");
                    //    int subIndex = valueStr.IndexOf("-");
                    //    //Debug.WriteLine($"Sub Index: {subIndex}");
                    //    int newLineIndex = valueStr.IndexOf("\r\n");
                    //    if ((plusIndex > -1 || subIndex > -1) && valueStr.Length >= 7 && newLineIndex >-1)
                    //    {
                    //        if (newLineIndex > plusIndex && newLineIndex > subIndex)
                    //        {
                    //            if (plusIndex > -1)
                    //            {

                    //                valueStr = valueStr.Substring(plusIndex, newLineIndex - plusIndex);

                    //            }
                    //            if (subIndex > -1)
                    //            {
                    //                valueStr = valueStr.Substring(subIndex, newLineIndex - subIndex);
                    //            }
                    //            //Debug.WriteLine($"Value String Nguyen Mau: {valueStr}");
                    //            digits = new Regex(@"^\D*?((-?(\d+(\.\d+)?))|(-?\.\d+)).*");
                    //            mx = digits.Match(valueStr);
                    //            //Debug.WriteLine($"Match String: {mx}");
                    //            try
                    //            {
                    //                ScaleValue = mx.Success ? subIndex > -1 ? -Convert.ToDouble(mx.Groups[1].Value) : Convert.ToDouble(mx.Groups[1].Value) : oldScale;
                    //                //ScaleValue = mx.Success ?  Convert.ToDouble(mx.Groups[1].Value) : oldScale;
                    //                oldScale = ScaleValue;
                    //            }
                    //            catch (Exception ex)
                    //            {
                    //                Debug.WriteLine($"Error: {ex}");
                    //            }
                    //        }

                    //    }

                    //    //ScaleValue = mx.Success ? Convert.ToDouble(mx.Groups[1].Value) : 0;
                    //    //Debug.WriteLine($"Read Scale value ++++: {ScaleValue}|{valueStr}");

                    //}


                    if (rawData?.Length >= 7)
                    {
                        //List<int> res = rawData.ToCharArray().Select((o, i) => o == '\r' ? i : -1).Where(i => i != -1).ToList();
                        //if (res.Count > 2)
                        //{
                        //    rawData = rawData.Substring(res[0], res[1] - res[0]);
                        //    Debug.WriteLine(valueStr);
                        //}
                        digits = new Regex(@"^\D*?((-?(\d+(\.\d+)?))|(-?\.\d+)).*");
                        mx = digits.Match(rawData);
                        //Debug.WriteLine($"Match String: {mx}");
                        try
                        {
                            ScaleValue = mx.Success ? Convert.ToDouble(mx.Groups[1].Value) : oldScale;
                            //ScaleValue = mx.Success ?  Convert.ToDouble(mx.Groups[1].Value) : oldScale;
                            oldScale = ScaleValue;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error: {ex}");
                        }
                    }

                    Thread.Sleep(ScaleDelay);

                    //countReadData += 1;

                    //if (countReadData > 1000)
                    //{
                    //    countReadData = 0;
                    //}
                }

                //Debug.WriteLine($"thoat vong lap doc gia tri can: StopScale = {StopScale}");

                sender.Close();
                sender.Dispose();
            }
            catch
            {
                //Debug.WriteLine($"CATCH hoat vong lap doc gia tri can: StopScale = {StopScale}");

                sender.Close();
                sender.Dispose();

                tskReadScale.Wait();
                tskReadScale.Dispose();
            }
        }
        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public void Dispose()
        {
            Debug.WriteLine("Huy DOI TUONG CAN");
            sender.Close();
            sender.Dispose();
        }
    }

    public class ScaleValueChangedEventArgs : EventArgs
    {
        private double _value;
        private string statusConnection;

        public double Value { get => _value; set => _value = value; }
        public string StatusConnection { get => statusConnection; set => statusConnection = value; }

        public ScaleValueChangedEventArgs(double value)
        {
            this._value = value;
        }
        public ScaleValueChangedEventArgs(string status)
        {
            this.statusConnection = status;
        }
    }
}
