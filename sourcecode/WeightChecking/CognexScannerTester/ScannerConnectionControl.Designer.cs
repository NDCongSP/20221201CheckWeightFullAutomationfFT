namespace CognexScannerTester
{
    partial class ScannerConnectionControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.ListBox lstLog;

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblIp = new System.Windows.Forms.Label();
            this.txtIp = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(12, 10);
            this.lblTitle.Size = new System.Drawing.Size(140, 20);
            this.lblTitle.Text = "Cognex Scanner";
            //
            // lblIp
            //
            this.lblIp.AutoSize = true;
            this.lblIp.Location = new System.Drawing.Point(12, 45);
            this.lblIp.Text = "Camera IP:";
            //
            // txtIp
            //
            this.txtIp.Location = new System.Drawing.Point(90, 42);
            this.txtIp.Size = new System.Drawing.Size(120, 23);
            this.txtIp.Text = "192.168.80.4";
            //
            // lblPort
            //
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(225, 45);
            this.lblPort.Text = "Port:";
            //
            // txtPort
            //
            this.txtPort.Location = new System.Drawing.Point(265, 42);
            this.txtPort.Size = new System.Drawing.Size(60, 23);
            this.txtPort.Text = "23";
            //
            // btnConnect
            //
            this.btnConnect.Location = new System.Drawing.Point(340, 41);
            this.btnConnect.Size = new System.Drawing.Size(85, 25);
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            //
            // btnDisconnect
            //
            this.btnDisconnect.Enabled = false;
            this.btnDisconnect.Location = new System.Drawing.Point(430, 41);
            this.btnDisconnect.Size = new System.Drawing.Size(85, 25);
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Firebrick;
            this.lblStatus.Location = new System.Drawing.Point(525, 46);
            this.lblStatus.Size = new System.Drawing.Size(120, 15);
            this.lblStatus.Text = "Not connected";
            //
            // btnClearLog
            //
            this.btnClearLog.Location = new System.Drawing.Point(620, 78);
            this.btnClearLog.Size = new System.Drawing.Size(85, 25);
            this.btnClearLog.Text = "Clear Log";
            this.btnClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            //
            // lstLog
            //
            this.lstLog.Location = new System.Drawing.Point(12, 115);
            this.lstLog.Size = new System.Drawing.Size(693, 260);
            this.lstLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstLog.HorizontalScrollbar = true;
            this.lstLog.IntegralHeight = false;
            this.lstLog.Font = new System.Drawing.Font("Consolas", 9F);
            //
            // ScannerConnectionControl
            //
            this.Controls.Add(this.lstLog);
            this.Controls.Add(this.btnClearLog);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this.txtIp);
            this.Controls.Add(this.lblIp);
            this.Controls.Add(this.lblTitle);
            this.Size = new System.Drawing.Size(720, 390);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
