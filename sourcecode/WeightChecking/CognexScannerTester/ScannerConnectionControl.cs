using CognexLibrary_NETFramework;
using System;
using System.Net;
using System.Windows.Forms;

namespace CognexScannerTester
{
    /// <summary>
    /// Connects to a real Cognex camera via CognexLibrary_NETFramework.DriverTelnet (the same
    /// production driver used by frmScaleNewUI.cs) and logs every RX/status event.
    /// </summary>
    public partial class ScannerConnectionControl : UserControl
    {
        private DriverTelnet _driverTelnet;
        private bool _isConnected;

        public ScannerConnectionControl()
        {
            InitializeComponent();
        }

        public ScannerConnectionControl(string title, string defaultIp, int defaultPort) : this()
        {
            lblTitle.Text = title;
            txtIp.Text = defaultIp;
            txtPort.Text = defaultPort.ToString();
        }

        private async void btnConnect_Click(object sender, EventArgs e)
        {
            if (_isConnected) return;

            if (!IPAddress.TryParse(txtIp.Text.Trim(), out _))
            {
                Log("Invalid IP address.");
                return;
            }
            if (!int.TryParse(txtPort.Text.Trim(), out var port))
            {
                Log("Invalid port.");
                return;
            }

            _driverTelnet = new DriverTelnet
            {
                HostName = txtIp.Text.Trim(),
                Port = port
            };
            _driverTelnet.DataEvent.EventHandleValueChange += DataEvent_EventHandleValueChange;
            _driverTelnet.DataEvent.EventHandleStatusChange += DataEvent_EventHandleStatusChange;

            _isConnected = true;
            UpdateButtons();
            Log($"Connecting to {txtIp.Text.Trim()}:{port}...");

            await _driverTelnet.ConnectDevices();
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            Disconnect();
        }

        public void Disconnect()
        {
            if (_driverTelnet == null) return;

            _driverTelnet.IsDisconect = true;
            _driverTelnet.DisconnectDevices();
            _driverTelnet.DataEvent.EventHandleValueChange -= DataEvent_EventHandleValueChange;
            _driverTelnet.DataEvent.EventHandleStatusChange -= DataEvent_EventHandleStatusChange;
            _driverTelnet = null;

            _isConnected = false;
            UpdateButtons();
            Log("Disconnected.");
        }

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            lstLog.Items.Clear();
        }

        private void DataEvent_EventHandleValueChange(object sender, ValueChangeEventArgs e)
        {
            var lines = SplitTelegramLines(e.NewValue);
            foreach (var line in lines)
            {
                Log("RX: " + line);
            }
        }

        private void DataEvent_EventHandleStatusChange(object sender, StatusChangeEventArgs e)
        {
            var msg = "STATUS: " + e.Status;
            if (e.Exception != null)
                msg += " — " + e.Exception.Message;
            Log(msg);
            SetStatusLabel(e.Status);
        }

        private void SetStatusLabel(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetStatusLabel(status)));
                return;
            }

            lblStatus.Text = status;
            lblStatus.ForeColor = status == "Connected"
                ? System.Drawing.Color.SeaGreen
                : (status == "Error" ? System.Drawing.Color.Firebrick : System.Drawing.Color.DarkOrange);
        }


        /// <summary>
        /// A single physical scan trigger can arrive as several CR/LF-terminated lines merged into
        /// one telegram (e.g. Main QR + box-info QR2, per DriverTelnet's line-batching).
        /// </summary>
        private static string[] SplitTelegramLines(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
            return raw.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void UpdateButtons()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateButtons));
                return;
            }

            btnConnect.Enabled = !_isConnected;
            btnDisconnect.Enabled = _isConnected;
            txtIp.Enabled = !_isConnected;
            txtPort.Enabled = !_isConnected;

            lblStatus.Text = _isConnected ? "Connecting/Connected" : "Not connected";
            lblStatus.ForeColor = _isConnected ? System.Drawing.Color.DarkOrange : System.Drawing.Color.Firebrick;
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
