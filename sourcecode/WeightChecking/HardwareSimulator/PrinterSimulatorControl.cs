using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HardwareSimulator
{
    /// <summary>
    /// Emulates the AnserU2 SmartU2 printer side that WeightChecking.Class.AnserU2TcpDriver
    /// connects to as a TCP client. Frames are STX (0x02) ... ETX (0x03) delimited, mirroring
    /// AnserU2TcpDriver.ReceiveLoopAsync's parser. Command bytes decoded per frmScaleNewUI.cs
    /// StartPrint()/StopPrint()/SendDynamicString().
    /// </summary>
    public partial class PrinterSimulatorControl : UserControl
    {
        private const byte STX = 0x02;
        private const byte ETX = 0x03;

        private static readonly byte[] AckFrame = { 0x02, 0x00, 0x02, 0x00, 0x4F, 0x03 };
        private static readonly byte[] PrintOkFrame = { 0x02, 0x00, 0x02, 0x00, 0x30, 0x03 };

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private bool _isListening;

        public PrinterSimulatorControl()
        {
            InitializeComponent();
        }

        private async void btnListen_Click(object sender, EventArgs e)
        {
            if (_isListening) return;

            if (!IPAddress.TryParse(txtIp.Text.Trim(), out var ip))
            {
                Log("Invalid IP address.");
                return;
            }
            if (!int.TryParse(txtPort.Text.Trim(), out var port))
            {
                Log("Invalid port.");
                return;
            }

            _listener = new TcpListener(ip, port);
            try
            {
                _listener.Start();
            }
            catch (Exception ex)
            {
                Log("Listen failed: " + ex.Message);
                return;
            }

            _cts = new CancellationTokenSource();
            _isListening = true;
            UpdateButtons();
            Log($"Listening on {ip}:{port}...");

            await AcceptLoopAsync(_cts.Token);
        }

        private async Task AcceptLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync();
                }
                catch
                {
                    break;
                }

                _client = client;
                _stream = client.GetStream();
                Log("Client connected: " + client.Client.RemoteEndPoint);
                UpdateButtons();

                await ReceiveLoopAsync(_stream, token);

                _stream = null;
                _client = null;
                if (!token.IsCancellationRequested)
                {
                    Log("Client disconnected. Waiting for next connection...");
                }
                UpdateButtons();
            }
        }

        private async Task ReceiveLoopAsync(NetworkStream stream, CancellationToken token)
        {
            var buffer = new byte[1024];
            var frameBuffer = new System.Collections.Generic.List<byte>();
            var inFrame = false;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var n = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (n == 0) break; // remote closed

                    for (var i = 0; i < n; i++)
                    {
                        var b = buffer[i];

                        if (b == STX)
                        {
                            inFrame = true;
                            frameBuffer.Clear();
                            frameBuffer.Add(b);
                        }
                        else if (inFrame)
                        {
                            frameBuffer.Add(b);
                            if (b == ETX)
                            {
                                var frame = frameBuffer.ToArray();
                                inFrame = false;
                                HandleFrame(frame);
                            }
                        }
                    }
                }
            }
            catch
            {
                // connection dropped/cancelled
            }
        }

        private void HandleFrame(byte[] frame)
        {
            Log("RX [" + frame.Length + " bytes]: " + ToHex(frame) + "  => " + Decode(frame));

            if (chkAutoAck.Checked)
            {
                SendFrame(AckFrame, "auto-ACK (0x4F)");
                var delayMs = (int)numAutoDelayMs.Value;
                var stream = _stream;
                Task.Delay(delayMs).ContinueWith(_ =>
                {
                    if (stream == _stream) SendFrame(PrintOkFrame, "auto-Print Success (0x30)");
                }, TaskScheduler.Default);
            }
        }

        private static string Decode(byte[] f)
        {
            if (f.Length > 5 && f[4] == 0x46 && f[5] == 2)
                return "StartPrint (template #2)";
            if (f.Length > 9 && f[4] == 0x46 && f[9] == 0x4C)
                return "StopPrint";
            if (f.Length > 9 && f[4] == 0xCA)
            {
                int lenGross = f[7], lenDate = f[8], lenLabel = f[9];
                var need = 12 + lenGross + lenDate + lenLabel;
                if (f.Length >= need)
                {
                    var grossWeight = Encoding.ASCII.GetString(f, 12, lenGross);
                    var createdDate = Encoding.ASCII.GetString(f, 12 + lenGross, lenDate);
                    var idLabel = Encoding.ASCII.GetString(f, 12 + lenGross + lenDate, lenLabel);
                    return $"SetDynamicString: grossWeight='{grossWeight}', createdDate='{createdDate}', idLabel='{idLabel}'";
                }
                return "SetDynamicString: (truncated frame, cannot parse payload)";
            }
            return "Unknown cmd 0x" + (f.Length > 4 ? f[4].ToString("X2") : "?");
        }

        private static string ToHex(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 3);
            foreach (var b in data) sb.Append(b.ToString("X2")).Append(' ');
            return sb.ToString().TrimEnd();
        }

        private void btnSendAck_Click(object sender, EventArgs e) => SendFrame(AckFrame, "ACK (0x4F)");

        private void btnSendPrintOk_Click(object sender, EventArgs e) => SendFrame(PrintOkFrame, "Print Success (0x30)");

        private void btnSendFail_Click(object sender, EventArgs e)
        {
            byte errCode = 1;
            if (!string.IsNullOrWhiteSpace(txtFailCode.Text) && byte.TryParse(txtFailCode.Text.Trim(), out var parsed))
                errCode = parsed;

            var frame = new byte[] { 0x02, 0x00, 0x03, 0x00, 0x31, errCode, 0x03 };
            SendFrame(frame, "Fail (0x31), errCode=" + errCode);
        }

        private void SendFrame(byte[] frame, string label)
        {
            var stream = _stream;
            if (stream == null)
            {
                Log("Cannot send: no client connected.");
                return;
            }

            try
            {
                stream.Write(frame, 0, frame.Length);
                stream.Flush();
                Log("TX: " + label + "  " + ToHex(frame));
            }
            catch (Exception ex)
            {
                Log("Send failed: " + ex.Message);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopListening();
        }

        private void StopListening()
        {
            if (!_isListening) return;
            _isListening = false;

            try { _cts?.Cancel(); } catch { }
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            try { _listener?.Stop(); } catch { }

            _stream = null;
            _client = null;
            Log("Stopped listening.");
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateButtons));
                return;
            }

            btnListen.Enabled = !_isListening;
            btnStop.Enabled = _isListening;
            txtIp.Enabled = !_isListening;
            txtPort.Enabled = !_isListening;

            var connected = _stream != null;
            btnSendAck.Enabled = connected;
            btnSendPrintOk.Enabled = connected;
            btnSendFail.Enabled = connected;
            lblStatus.Text = connected ? "Client connected" : (_isListening ? "Listening, no client" : "Not listening");
            lblStatus.ForeColor = connected ? System.Drawing.Color.SeaGreen : (_isListening ? System.Drawing.Color.DarkOrange : System.Drawing.Color.Firebrick);
        }

        private void Log(string msg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Log(msg)));
                return;
            }

            lstLog.Items.Add(DateTime.Now.ToString("HH:mm:ss.fff") + "  " + msg);
            lstLog.TopIndex = lstLog.Items.Count - 1;
        }
    }
}
