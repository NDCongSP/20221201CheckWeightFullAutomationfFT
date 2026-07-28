using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AnserU2_cSharp
{
    public partial class frmTcpTest : Form
    {
        private AnserU2TcpDriver _driver;

        public frmTcpTest()
        {
            InitializeComponent();
            Load += FrmTcpTest_Load;
            FormClosing += FrmTcpTest_FormClosing;
        }

        private void FrmTcpTest_Load(object sender, EventArgs e)
        {
            for (int i = 1; i <= 10; i++)
                cbbIDMSG.Items.Add(i);
            cbbIDMSG.SelectedIndex = 0;

            btnDisconnect.Enabled = false;
            SetPrintButtonsEnabled(false);
        }

        private void FrmTcpTest_FormClosing(object sender, FormClosingEventArgs e)
        {
            _driver?.Dispose();
        }

        // ─── Connection ────────────────────────────────────────────────────

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtPort.Text.Trim(), out int port))
            {
                MessageBox.Show("Invalid port.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _driver?.Dispose();
            _driver = new AnserU2TcpDriver
            {
                IpAddress = txtIp.Text.Trim(),
                Port = port
            };
            _driver.DataReceived += DriverDataReceived;
            _driver.ConnectionStatusChanged += DriverConnectionStatusChanged;
            _driver.Connect();

            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            SetPrintButtonsEnabled(true);
            AppendLog($"Connecting to {txtIp.Text.Trim()}:{port}...");
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            _driver?.Dispose();
            _driver = null;

            btnConnect.Enabled = true;
            btnDisconnect.Enabled = false;
            SetPrintButtonsEnabled(false);
            UpdateConnectionLabel("Disconnected");
            AppendLog("Disconnected by user.");
        }

        private void DriverConnectionStatusChanged(string status)
        {
            InvokeIfRequired(() =>
            {
                UpdateConnectionLabel(status);
                AppendLog($"Connection: {status}");
            });
        }

        private void UpdateConnectionLabel(string status)
        {
            labConnection.Text = status;
            labConnection.ForeColor = status == "Connected"
                ? System.Drawing.Color.Green
                : System.Drawing.Color.Red;
        }

        // ─── Data received ─────────────────────────────────────────────────

        private void DriverDataReceived(byte[] rcvArr)
        {
            if (rcvArr == null || rcvArr.Length < 5) return;

            string hex = string.Join(" ", rcvArr.Select(b => b.ToString("X2")));
            AppendLog($"RX [{rcvArr.Length} bytes]: {hex}");

            // May in tra ve 2 dang frame khac nhau cho status:
            //  - Frame ACK/FAIL ngan (5-6 byte, phan hoi lenh StartPrint/SetDynamicString): status nam o byte[2].
            //    VD ACK:  02 01 4F <chk> 03           (5 byte)
            //    VD FAIL: 02 02 31 <err> <chk> 03      (6 byte)
            //  - Frame phan hoi dai (Get Speed/Get Delay/event Print Completed): status nam o byte[4].
            //    VD: 02 00 06 01 5D <data...> <chk> 03
            // rcvArr[4] chi dung voi dang dai; voi dang ngan rcvArr[4] roi vao byte ETX (0x03) nen khong bao gio
            // khop 0x30/0x4F/0x31.
            bool isShortFrame = rcvArr.Length <= 6;
            byte statusByte = isShortFrame ? rcvArr[2] : rcvArr[4];
            int errCodeIdx = isShortFrame ? 3 : 5;

            if (statusByte == 0x30) // in thành công
            {
                AppendLog("Print successful!");
                SendDynamicString(" ", " ", " ", " ");
            }
            else if (statusByte == 0x4F) // lệnh được chấp nhận
            {
                AppendLog("Command sent successfully.");
            }
            else if (statusByte == 0x31) // lỗi
            {
                string errCode = rcvArr.Length > errCodeIdx ? rcvArr[errCodeIdx].ToString() : "?";
                AppendLog($"Error. Error code: {errCode}");
                // Stop print không cần UI thread
                byte[] stopCmd = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x46, 0x0, 0x0, 0x0, 0x0, 0x4C, 0x3 };
                _driver?.Write(stopCmd);
                InvokeIfRequired(() =>
                    MessageBox.Show($"Send command error. Error code: {errCode}", "ERROR",
                        MessageBoxButtons.OK, MessageBoxIcon.Error));
            }
            else if (statusByte == 0x5D && rcvArr.Length >= 9) // 0x5D = get speed response
            {
                var speedPV = (double)(rcvArr[5] + rcvArr[6] * 0x100 + rcvArr[7] * 0x1000 + rcvArr[8] * 0x10000);
                speedPV = Math.Round(speedPV / 1000, 2);
                InvokeIfRequired(() => txt_speedPV.Text = speedPV.ToString());
            }
            else if (statusByte == 0x64 && rcvArr.Length >= 9) // 0x64 = get delay response
            {
                var delayPV = (double)(rcvArr[5] + rcvArr[6] * 0x100 + rcvArr[7] * 0x1000 + rcvArr[8] * 0x10000);
                delayPV = Math.Round(delayPV / 100, 2);
                InvokeIfRequired(() => txtDelayPV.Text = delayPV.ToString());
            }

            string statusText = string.Join(" ", rcvArr.Select(b => b.ToString()));
            InvokeIfRequired(() => labStatus.Text = statusText);
        }

        // ─── Print commands ────────────────────────────────────────────────

        private void btn_startprint_Click(object sender, EventArgs e)
        {
            byte[] SetPtinting = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x46, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3 };
            SetPtinting[5] = Convert.ToByte(cbbIDMSG.SelectedItem);
            byte chkSUM = 0;
            for (var i = 1; i <= SetPtinting.Length - 3; i++)
                chkSUM = (byte)(chkSUM + SetPtinting[i]);
            SetPtinting[9] = chkSUM;
            _driver?.Write(SetPtinting);
        }

        private void btn_stopprint_Click(object sender, EventArgs e)
        {
            byte[] SetPtinting = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x46, 0x0, 0x0, 0x0, 0x0, 0x4C, 0x3 };
            _driver?.Write(SetPtinting);
        }

        private void btn_sendSTRING1_Click(object sender, EventArgs e)
        {
            SendDynamicString(txtString1.Text, txtString2.Text, txtString3.Text, txtString4.Text);
        }

        private void SendDynamicString(string string1, string string2, string string3, string string4)
        {
            int i = 0, j = 0, k = 0, l = 0;
            int chkSUM = 0;

            byte[] arr = new byte[14 + string1.Length + string2.Length + string3.Length + string4.Length];
            arr[0] = 0x2;
            arr[1] = 0x0;
            arr[2] = (byte)(9 + string1.Length + string2.Length + string3.Length + string4.Length);
            arr[3] = 0x0;
            arr[4] = 0xCA;
            arr[5] = 0;
            arr[6] = 0;
            arr[7] = (byte)string1.Length;
            arr[8] = (byte)string2.Length;
            arr[9] = (byte)string3.Length;
            arr[10] = (byte)string4.Length;
            arr[11] = 0;

            byte[] s1 = Encoding.ASCII.GetBytes(string1.ToCharArray());
            byte[] s2 = Encoding.ASCII.GetBytes(string2.ToCharArray());
            byte[] s3 = Encoding.ASCII.GetBytes(string3.ToCharArray());
            byte[] s4 = Encoding.ASCII.GetBytes(string4.ToCharArray());

            for (i = 0; i < s1.Length; i++) arr[12 + i] = s1[i];
            for (j = 0; j < s2.Length; j++) arr[12 + i + j] = s2[j];
            for (k = 0; k < s3.Length; k++) arr[12 + i + j + k] = s3[k];
            for (l = 0; l < s4.Length; l++) arr[12 + i + j + k + l] = s4[l];

            for (var c = 1; c <= i + j + k + l + 12; c++)
                chkSUM += arr[c];
            chkSUM &= 0xFF;
            arr[i + j + k + l + 12] = Convert.ToByte(chkSUM);
            arr[i + j + k + l + 13] = 0x3;

            _driver?.Write(arr);
        }

        // ─── Speed / Delay ─────────────────────────────────────────────────

        private void btn_GetSpeed_Click(object sender, EventArgs e)
        {
            _driver?.Write(new byte[] { 0x2, 0x0, 0x2, 0x0, 0x5D, 0x5F, 0x3 });
        }

        private void btn_Setspeed_Click(object sender, EventArgs e)
        {
            byte[] setSpeed = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x5E, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3 };
            int speedSV = (int)(double.TryParse(txt_SpeedSV.Text, out double sv) ? sv * 1000 : 0);
            byte[] speedBytes = BitConverter.GetBytes(speedSV);
            for (var i = 0; i < speedBytes.Length; i++)
                setSpeed[5 + i] = speedBytes[i];
            int chksum = 0;
            for (var j = 1; j <= setSpeed.Length - 2; j++)
                chksum += setSpeed[j];
            setSpeed[9] = Convert.ToByte(chksum & 0xFF);
            _driver?.Write(setSpeed);
        }

        private void btnGetDelay_Click(object sender, EventArgs e)
        {
            _driver?.Write(new byte[] { 0x2, 0x0, 0x4, 0x0, 0x64, 0x2, 0x0, 0x6A, 0x3 });
        }

        private void btnSetDelay_Click(object sender, EventArgs e)
        {
            byte[] setDelay = new byte[] { 0x2, 0x0, 0x8, 0x0, 0x65, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3 };
            setDelay[5] = 0x2;
            int delaySV = (int)(double.TryParse(txtDelaySV.Text, out double dv) ? dv * 10 : 0);
            byte[] delayBytes = BitConverter.GetBytes(delaySV);
            for (var i = 0; i < delayBytes.Length; i++)
                setDelay[7 + i] = delayBytes[i];
            int chksum = 0;
            for (var j = 1; j <= setDelay.Length - 2; j++)
                chksum += setDelay[j];
            setDelay[11] = Convert.ToByte(chksum & 0xFF);
            _driver?.Write(setDelay);
        }

        // ─── Helpers ───────────────────────────────────────────────────────

        private void btnClearLog_Click(object sender, EventArgs e)
        {
            rtbLog.Clear();
        }

        private void SetPrintButtonsEnabled(bool enabled)
        {
            btn_startprint.Enabled = enabled;
            btn_stopprint.Enabled = enabled;
            btn_sendSTRING1.Enabled = enabled;
            btn_GetSpeed.Enabled = enabled;
            btn_Setspeed.Enabled = enabled;
            btnGetDelay.Enabled = enabled;
            btnSetDelay.Enabled = enabled;
        }

        private void AppendLog(string msg)
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
            if (rtbLog.InvokeRequired)
                rtbLog.Invoke(new Action(() => { rtbLog.AppendText(line + Environment.NewLine); rtbLog.ScrollToCaret(); }));
            else
            {
                rtbLog.AppendText(line + Environment.NewLine);
                rtbLog.ScrollToCaret();
            }
        }

        private void InvokeIfRequired(Action action)
        {
            if (InvokeRequired) Invoke(action);
            else action();
        }
    }
}
