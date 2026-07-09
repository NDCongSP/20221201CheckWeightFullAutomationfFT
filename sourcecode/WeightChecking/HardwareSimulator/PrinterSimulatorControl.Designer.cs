namespace HardwareSimulator
{
    partial class PrinterSimulatorControl
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
        private System.Windows.Forms.Button btnSendAck;
        private System.Windows.Forms.Button btnSendPrintOk;
        private System.Windows.Forms.Button btnSendFail;
        private System.Windows.Forms.TextBox txtFailCode;
        private System.Windows.Forms.Label lblFailCode;
        private System.Windows.Forms.CheckBox chkAutoAck;
        private System.Windows.Forms.NumericUpDown numAutoDelayMs;
        private System.Windows.Forms.Label lblAutoDelay;
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
            this.btnSendAck = new System.Windows.Forms.Button();
            this.btnSendPrintOk = new System.Windows.Forms.Button();
            this.btnSendFail = new System.Windows.Forms.Button();
            this.txtFailCode = new System.Windows.Forms.TextBox();
            this.lblFailCode = new System.Windows.Forms.Label();
            this.chkAutoAck = new System.Windows.Forms.CheckBox();
            this.numAutoDelayMs = new System.Windows.Forms.NumericUpDown();
            this.lblAutoDelay = new System.Windows.Forms.Label();
            this.lstLog = new System.Windows.Forms.ListBox();
            ((System.ComponentModel.ISupportInitialize)(this.numAutoDelayMs)).BeginInit();
            this.SuspendLayout();
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(12, 10);
            this.lblTitle.Size = new System.Drawing.Size(150, 20);
            this.lblTitle.Text = "AnserU2 Printer Simulator";
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
            this.txtIp.Text = "127.0.0.1";
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
            this.txtPort.Text = "4001";
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
            // btnSendAck
            //
            this.btnSendAck.Location = new System.Drawing.Point(12, 80);
            this.btnSendAck.Size = new System.Drawing.Size(160, 27);
            this.btnSendAck.Text = "Send ACK (0x4F)";
            this.btnSendAck.UseVisualStyleBackColor = true;
            this.btnSendAck.Click += new System.EventHandler(this.btnSendAck_Click);
            //
            // btnSendPrintOk
            //
            this.btnSendPrintOk.Location = new System.Drawing.Point(180, 80);
            this.btnSendPrintOk.Size = new System.Drawing.Size(160, 27);
            this.btnSendPrintOk.Text = "Send Print Success (0x30)";
            this.btnSendPrintOk.UseVisualStyleBackColor = true;
            this.btnSendPrintOk.Click += new System.EventHandler(this.btnSendPrintOk_Click);
            //
            // btnSendFail
            //
            this.btnSendFail.Location = new System.Drawing.Point(348, 80);
            this.btnSendFail.Size = new System.Drawing.Size(140, 27);
            this.btnSendFail.Text = "Send Fail (0x31)";
            this.btnSendFail.UseVisualStyleBackColor = true;
            this.btnSendFail.Click += new System.EventHandler(this.btnSendFail_Click);
            //
            // lblFailCode
            //
            this.lblFailCode.AutoSize = true;
            this.lblFailCode.Location = new System.Drawing.Point(496, 86);
            this.lblFailCode.Text = "Err code:";
            //
            // txtFailCode
            //
            this.txtFailCode.Location = new System.Drawing.Point(560, 82);
            this.txtFailCode.Size = new System.Drawing.Size(40, 23);
            this.txtFailCode.Text = "1";
            //
            // chkAutoAck
            //
            this.chkAutoAck.AutoSize = true;
            this.chkAutoAck.Location = new System.Drawing.Point(12, 118);
            this.chkAutoAck.Text = "Auto ACK + Auto Print Success after";
            this.chkAutoAck.UseVisualStyleBackColor = true;
            //
            // numAutoDelayMs
            //
            this.numAutoDelayMs.Location = new System.Drawing.Point(245, 116);
            this.numAutoDelayMs.Size = new System.Drawing.Size(70, 23);
            this.numAutoDelayMs.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            this.numAutoDelayMs.Value = new decimal(new int[] { 800, 0, 0, 0 });
            //
            // lblAutoDelay
            //
            this.lblAutoDelay.AutoSize = true;
            this.lblAutoDelay.Location = new System.Drawing.Point(320, 119);
            this.lblAutoDelay.Text = "ms (on every received frame)";
            //
            // lstLog
            //
            this.lstLog.Location = new System.Drawing.Point(12, 150);
            this.lstLog.Size = new System.Drawing.Size(693, 225);
            this.lstLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstLog.HorizontalScrollbar = true;
            this.lstLog.IntegralHeight = false;
            this.lstLog.Font = new System.Drawing.Font("Consolas", 9F);
            //
            // PrinterSimulatorControl
            //
            this.Controls.Add(this.lstLog);
            this.Controls.Add(this.lblAutoDelay);
            this.Controls.Add(this.numAutoDelayMs);
            this.Controls.Add(this.chkAutoAck);
            this.Controls.Add(this.txtFailCode);
            this.Controls.Add(this.lblFailCode);
            this.Controls.Add(this.btnSendFail);
            this.Controls.Add(this.btnSendPrintOk);
            this.Controls.Add(this.btnSendAck);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnListen);
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this.txtIp);
            this.Controls.Add(this.lblIp);
            this.Controls.Add(this.lblTitle);
            this.Size = new System.Drawing.Size(720, 390);
            ((System.ComponentModel.ISupportInitialize)(this.numAutoDelayMs)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
