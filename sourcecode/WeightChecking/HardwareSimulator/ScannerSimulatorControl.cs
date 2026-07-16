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
    /// Emulates the "camera" side of CognexLibrary_NETFramework.DriverTelnet: a plain TCP
    /// server that sends newline-terminated ASCII lines to whichever client connects
    /// (the real DriverTelnet reads lines via StreamReader.ReadLineAsync()).
    /// </summary>
    public partial class ScannerSimulatorControl : UserControl
    {
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private bool _isListening;

        public ScannerSimulatorControl()
        {
            InitializeComponent();
        }

        public ScannerSimulatorControl(string title, string defaultIp, int defaultPort) : this()
        {
            lblTitle.Text = title;
            txtIp.Text = defaultIp;
            txtPort.Text = defaultPort.ToString();
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
                    break; // listener was stopped
                }

                _client = client;
                _stream = client.GetStream();
                Log("Client connected: " + client.Client.RemoteEndPoint);
                UpdateButtons();

                try
                {
                    var buf = new byte[64];
                    while (!token.IsCancellationRequested)
                    {
                        var n = await _stream.ReadAsync(buf, 0, buf.Length, token);
                        if (n == 0) break; // remote closed
                    }
                }
                catch
                {
                    // connection dropped/cancelled - fall through to cleanup and re-accept
                }

                _stream = null;
                _client = null;
                if (!token.IsCancellationRequested)
                {
                    Log("Client disconnected. Waiting for next connection...");
                }
                UpdateButtons();
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

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (_stream == null)
            {
                Log("Cannot send: no client connected.");
                return;
            }

            try
            {
                var payload = ExpandControlCharPlaceholders(txtBarcode.Text);
                var bytes = Encoding.ASCII.GetBytes(payload + "\r\n");
                _stream.Write(bytes, 0, bytes.Length);
                _stream.Flush();
                Log("TX: " + txtBarcode.Text);
            }
            catch (Exception ex)
            {
                Log("Send failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Cognex's DataMan Result History panel displays embedded control characters as
        /// literal placeholder text (e.g. "&lt;0x0D&gt;&lt;0x0A&gt;") rather than actual bytes.
        /// When testing a merged multi-code telegram, testers copy that placeholder text
        /// straight out of the panel — so it must be expanded back into real CR/LF bytes here,
        /// otherwise the whole placeholder is sent as literal text inside a single line.
        /// </summary>
        private static string ExpandControlCharPlaceholders(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s
                .Replace("<0x0D><0x0A>", "\r\n")
                .Replace("<0x0D>", "\r")
                .Replace("<0x0A>", "\n")
                .Replace("\\r\\n", "\r\n")
                .Replace("\\r", "\r")
                .Replace("\\n", "\n");
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
            btnSend.Enabled = connected;
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
