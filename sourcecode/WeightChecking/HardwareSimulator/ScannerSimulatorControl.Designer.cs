namespace HardwareSimulator
{
    partial class ScannerSimulatorControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopListening();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Button btnListen;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblBarcode;
        private System.Windows.Forms.TextBox txtBarcode;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.ListBox lstLog;

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblIp = new System.Windows.Forms.Label();
            this.txtIp = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.btnListen = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblBarcode = new System.Windows.Forms.Label();
            this.txtBarcode = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.lstLog = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(12, 10);
            this.lblTitle.Size = new System.Drawing.Size(120, 20);
            this.lblTitle.Text = "Scanner Simulator";
            //
            // lblIp
            //
            this.lblIp.AutoSize = true;
            this.lblIp.Location = new System.Drawing.Point(12, 45);
            this.lblIp.Text = "Listen IP:";
            //
            // txtIp
            //
            this.txtIp.Location = new System.Drawing.Point(90, 42);
            this.txtIp.Size = new System.Drawing.Size(120, 23);
            this.txtIp.Text = "127.0.0.2";
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
            // btnListen
            //
            this.btnListen.Location = new System.Drawing.Point(340, 41);
            this.btnListen.Size = new System.Drawing.Size(85, 25);
            this.btnListen.Text = "Listen";
            this.btnListen.UseVisualStyleBackColor = true;
            this.btnListen.Click += new System.EventHandler(this.btnListen_Click);
            //
            // btnStop
            //
            this.btnStop.Enabled = false;
            this.btnStop.Location = new System.Drawing.Point(430, 41);
            this.btnStop.Size = new System.Drawing.Size(85, 25);
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Firebrick;
            this.lblStatus.Location = new System.Drawing.Point(525, 46);
            this.lblStatus.Size = new System.Drawing.Size(120, 15);
            this.lblStatus.Text = "Not listening";
            //
            // lblBarcode
            //
            this.lblBarcode.AutoSize = true;
            this.lblBarcode.Location = new System.Drawing.Point(12, 82);
            this.lblBarcode.Text = "Barcode / QR string:";
            //
            // txtBarcode
            //
            this.txtBarcode.Location = new System.Drawing.Point(140, 79);
            this.txtBarcode.Size = new System.Drawing.Size(470, 23);
            this.txtBarcode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            //
            // btnSend
            //
            this.btnSend.Location = new System.Drawing.Point(620, 78);
            this.btnSend.Size = new System.Drawing.Size(85, 25);
            this.btnSend.Text = "Send";
            this.btnSend.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSend.UseVisualStyleBackColor = true;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
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
            // ScannerSimulatorControl
            //
            this.Controls.Add(this.lstLog);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.txtBarcode);
            this.Controls.Add(this.lblBarcode);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnListen);
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
