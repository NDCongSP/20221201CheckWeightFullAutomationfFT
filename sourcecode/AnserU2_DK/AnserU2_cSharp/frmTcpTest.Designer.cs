namespace AnserU2_cSharp
{
    partial class frmTcpTest
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.lblIp = new System.Windows.Forms.Label();
            this.txtIp = new System.Windows.Forms.TextBox();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.labConnection = new System.Windows.Forms.Label();
            this.btn_startprint = new System.Windows.Forms.Button();
            this.btn_stopprint = new System.Windows.Forms.Button();
            this.lblIDMSG = new System.Windows.Forms.Label();
            this.cbbIDMSG = new System.Windows.Forms.ComboBox();
            this.btn_sendSTRING1 = new System.Windows.Forms.Button();
            this.lblStr1 = new System.Windows.Forms.Label();
            this.txtString1 = new System.Windows.Forms.TextBox();
            this.lblStr2 = new System.Windows.Forms.Label();
            this.txtString2 = new System.Windows.Forms.TextBox();
            this.lblStr3 = new System.Windows.Forms.Label();
            this.txtString3 = new System.Windows.Forms.TextBox();
            this.lblStr4 = new System.Windows.Forms.Label();
            this.txtString4 = new System.Windows.Forms.TextBox();
            this.btn_GetSpeed = new System.Windows.Forms.Button();
            this.lblSpeedPV = new System.Windows.Forms.Label();
            this.txt_speedPV = new System.Windows.Forms.TextBox();
            this.lblSpeedSV = new System.Windows.Forms.Label();
            this.txt_SpeedSV = new System.Windows.Forms.TextBox();
            this.btn_Setspeed = new System.Windows.Forms.Button();
            this.btnGetDelay = new System.Windows.Forms.Button();
            this.lblDelayPV = new System.Windows.Forms.Label();
            this.txtDelayPV = new System.Windows.Forms.TextBox();
            this.lblDelaySV = new System.Windows.Forms.Label();
            this.txtDelaySV = new System.Windows.Forms.TextBox();
            this.btnSetDelay = new System.Windows.Forms.Button();
            this.lblStatusLbl = new System.Windows.Forms.Label();
            this.labStatus = new System.Windows.Forms.Label();
            this.lblLog = new System.Windows.Forms.Label();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();

            // ── lblIp ────────────────────────────────────────────────────
            this.lblIp.AutoSize = true;
            this.lblIp.Location = new System.Drawing.Point(8, 14);
            this.lblIp.Name = "lblIp";
            this.lblIp.Size = new System.Drawing.Size(20, 13);
            this.lblIp.TabIndex = 0;
            this.lblIp.Text = "IP:";

            // ── txtIp ────────────────────────────────────────────────────
            this.txtIp.Location = new System.Drawing.Point(30, 10);
            this.txtIp.Name = "txtIp";
            this.txtIp.Size = new System.Drawing.Size(135, 20);
            this.txtIp.TabIndex = 1;
            this.txtIp.Text = "192.168.4.70";

            // ── lblPort ──────────────────────────────────────────────────
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(174, 14);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(29, 13);
            this.lblPort.TabIndex = 2;
            this.lblPort.Text = "Port:";

            // ── txtPort ──────────────────────────────────────────────────
            this.txtPort.Location = new System.Drawing.Point(206, 10);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(55, 20);
            this.txtPort.TabIndex = 3;
            this.txtPort.Text = "4001";

            // ── btnConnect ───────────────────────────────────────────────
            this.btnConnect.Location = new System.Drawing.Point(270, 8);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(85, 26);
            this.btnConnect.TabIndex = 4;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);

            // ── btnDisconnect ────────────────────────────────────────────
            this.btnDisconnect.Location = new System.Drawing.Point(362, 8);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(90, 26);
            this.btnDisconnect.TabIndex = 5;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.UseVisualStyleBackColor = true;
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);

            // ── labConnection ────────────────────────────────────────────
            this.labConnection.AutoSize = false;
            this.labConnection.ForeColor = System.Drawing.Color.Red;
            this.labConnection.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.labConnection.Location = new System.Drawing.Point(460, 13);
            this.labConnection.Name = "labConnection";
            this.labConnection.Size = new System.Drawing.Size(360, 17);
            this.labConnection.TabIndex = 6;
            this.labConnection.Text = "Disconnected";

            // ── btn_startprint ───────────────────────────────────────────
            this.btn_startprint.Location = new System.Drawing.Point(8, 43);
            this.btn_startprint.Name = "btn_startprint";
            this.btn_startprint.Size = new System.Drawing.Size(85, 26);
            this.btn_startprint.TabIndex = 10;
            this.btn_startprint.Text = "Start Print";
            this.btn_startprint.UseVisualStyleBackColor = true;
            this.btn_startprint.Click += new System.EventHandler(this.btn_startprint_Click);

            // ── btn_stopprint ────────────────────────────────────────────
            this.btn_stopprint.Location = new System.Drawing.Point(100, 43);
            this.btn_stopprint.Name = "btn_stopprint";
            this.btn_stopprint.Size = new System.Drawing.Size(85, 26);
            this.btn_stopprint.TabIndex = 11;
            this.btn_stopprint.Text = "Stop Print";
            this.btn_stopprint.UseVisualStyleBackColor = true;
            this.btn_stopprint.Click += new System.EventHandler(this.btn_stopprint_Click);

            // ── lblIDMSG ─────────────────────────────────────────────────
            this.lblIDMSG.AutoSize = true;
            this.lblIDMSG.Location = new System.Drawing.Point(194, 47);
            this.lblIDMSG.Name = "lblIDMSG";
            this.lblIDMSG.Size = new System.Drawing.Size(68, 13);
            this.lblIDMSG.TabIndex = 12;
            this.lblIDMSG.Text = "ID bản tin:";

            // ── cbbIDMSG ─────────────────────────────────────────────────
            this.cbbIDMSG.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbIDMSG.FormattingEnabled = true;
            this.cbbIDMSG.Location = new System.Drawing.Point(266, 44);
            this.cbbIDMSG.Name = "cbbIDMSG";
            this.cbbIDMSG.Size = new System.Drawing.Size(55, 21);
            this.cbbIDMSG.TabIndex = 13;

            // ── btn_sendSTRING1 ──────────────────────────────────────────
            this.btn_sendSTRING1.Location = new System.Drawing.Point(8, 78);
            this.btn_sendSTRING1.Name = "btn_sendSTRING1";
            this.btn_sendSTRING1.Size = new System.Drawing.Size(100, 26);
            this.btn_sendSTRING1.TabIndex = 20;
            this.btn_sendSTRING1.Text = "Send String";
            this.btn_sendSTRING1.UseVisualStyleBackColor = true;
            this.btn_sendSTRING1.Click += new System.EventHandler(this.btn_sendSTRING1_Click);

            // ── lblStr1 ──────────────────────────────────────────────────
            this.lblStr1.AutoSize = true;
            this.lblStr1.Location = new System.Drawing.Point(115, 82);
            this.lblStr1.Name = "lblStr1";
            this.lblStr1.Size = new System.Drawing.Size(46, 13);
            this.lblStr1.TabIndex = 21;
            this.lblStr1.Text = "String 1:";

            // ── txtString1 ───────────────────────────────────────────────
            this.txtString1.Location = new System.Drawing.Point(165, 79);
            this.txtString1.Name = "txtString1";
            this.txtString1.Size = new System.Drawing.Size(155, 20);
            this.txtString1.TabIndex = 22;

            // ── lblStr2 ──────────────────────────────────────────────────
            this.lblStr2.AutoSize = true;
            this.lblStr2.Location = new System.Drawing.Point(328, 82);
            this.lblStr2.Name = "lblStr2";
            this.lblStr2.Size = new System.Drawing.Size(46, 13);
            this.lblStr2.TabIndex = 23;
            this.lblStr2.Text = "String 2:";

            // ── txtString2 ───────────────────────────────────────────────
            this.txtString2.Location = new System.Drawing.Point(378, 79);
            this.txtString2.Name = "txtString2";
            this.txtString2.Size = new System.Drawing.Size(155, 20);
            this.txtString2.TabIndex = 24;

            // ── lblStr3 ──────────────────────────────────────────────────
            this.lblStr3.AutoSize = true;
            this.lblStr3.Location = new System.Drawing.Point(115, 112);
            this.lblStr3.Name = "lblStr3";
            this.lblStr3.Size = new System.Drawing.Size(46, 13);
            this.lblStr3.TabIndex = 25;
            this.lblStr3.Text = "String 3:";

            // ── txtString3 ───────────────────────────────────────────────
            this.txtString3.Location = new System.Drawing.Point(165, 109);
            this.txtString3.Name = "txtString3";
            this.txtString3.Size = new System.Drawing.Size(155, 20);
            this.txtString3.TabIndex = 26;

            // ── lblStr4 ──────────────────────────────────────────────────
            this.lblStr4.AutoSize = true;
            this.lblStr4.Location = new System.Drawing.Point(328, 112);
            this.lblStr4.Name = "lblStr4";
            this.lblStr4.Size = new System.Drawing.Size(46, 13);
            this.lblStr4.TabIndex = 27;
            this.lblStr4.Text = "String 4:";

            // ── txtString4 ───────────────────────────────────────────────
            this.txtString4.Location = new System.Drawing.Point(378, 109);
            this.txtString4.Name = "txtString4";
            this.txtString4.Size = new System.Drawing.Size(155, 20);
            this.txtString4.TabIndex = 28;

            // ── btn_GetSpeed ─────────────────────────────────────────────
            this.btn_GetSpeed.Location = new System.Drawing.Point(8, 145);
            this.btn_GetSpeed.Name = "btn_GetSpeed";
            this.btn_GetSpeed.Size = new System.Drawing.Size(85, 26);
            this.btn_GetSpeed.TabIndex = 30;
            this.btn_GetSpeed.Text = "Get Speed";
            this.btn_GetSpeed.UseVisualStyleBackColor = true;
            this.btn_GetSpeed.Click += new System.EventHandler(this.btn_GetSpeed_Click);

            // ── lblSpeedPV ───────────────────────────────────────────────
            this.lblSpeedPV.AutoSize = true;
            this.lblSpeedPV.Location = new System.Drawing.Point(100, 149);
            this.lblSpeedPV.Name = "lblSpeedPV";
            this.lblSpeedPV.Size = new System.Drawing.Size(59, 13);
            this.lblSpeedPV.TabIndex = 31;
            this.lblSpeedPV.Text = "SpeedPV:";

            // ── txt_speedPV ──────────────────────────────────────────────
            this.txt_speedPV.Location = new System.Drawing.Point(162, 146);
            this.txt_speedPV.Name = "txt_speedPV";
            this.txt_speedPV.ReadOnly = true;
            this.txt_speedPV.Size = new System.Drawing.Size(72, 20);
            this.txt_speedPV.TabIndex = 32;

            // ── lblSpeedSV ───────────────────────────────────────────────
            this.lblSpeedSV.AutoSize = true;
            this.lblSpeedSV.Location = new System.Drawing.Point(242, 149);
            this.lblSpeedSV.Name = "lblSpeedSV";
            this.lblSpeedSV.Size = new System.Drawing.Size(55, 13);
            this.lblSpeedSV.TabIndex = 33;
            this.lblSpeedSV.Text = "SpeedSV:";

            // ── txt_SpeedSV ──────────────────────────────────────────────
            this.txt_SpeedSV.Location = new System.Drawing.Point(300, 146);
            this.txt_SpeedSV.Name = "txt_SpeedSV";
            this.txt_SpeedSV.Size = new System.Drawing.Size(72, 20);
            this.txt_SpeedSV.TabIndex = 34;

            // ── btn_Setspeed ─────────────────────────────────────────────
            this.btn_Setspeed.Location = new System.Drawing.Point(380, 145);
            this.btn_Setspeed.Name = "btn_Setspeed";
            this.btn_Setspeed.Size = new System.Drawing.Size(85, 26);
            this.btn_Setspeed.TabIndex = 35;
            this.btn_Setspeed.Text = "Set Speed";
            this.btn_Setspeed.UseVisualStyleBackColor = true;
            this.btn_Setspeed.Click += new System.EventHandler(this.btn_Setspeed_Click);

            // ── btnGetDelay ──────────────────────────────────────────────
            this.btnGetDelay.Location = new System.Drawing.Point(8, 180);
            this.btnGetDelay.Name = "btnGetDelay";
            this.btnGetDelay.Size = new System.Drawing.Size(85, 26);
            this.btnGetDelay.TabIndex = 40;
            this.btnGetDelay.Text = "Get Delay";
            this.btnGetDelay.UseVisualStyleBackColor = true;
            this.btnGetDelay.Click += new System.EventHandler(this.btnGetDelay_Click);

            // ── lblDelayPV ───────────────────────────────────────────────
            this.lblDelayPV.AutoSize = true;
            this.lblDelayPV.Location = new System.Drawing.Point(100, 184);
            this.lblDelayPV.Name = "lblDelayPV";
            this.lblDelayPV.Size = new System.Drawing.Size(55, 13);
            this.lblDelayPV.TabIndex = 41;
            this.lblDelayPV.Text = "DelayPV:";

            // ── txtDelayPV ───────────────────────────────────────────────
            this.txtDelayPV.Location = new System.Drawing.Point(162, 181);
            this.txtDelayPV.Name = "txtDelayPV";
            this.txtDelayPV.ReadOnly = true;
            this.txtDelayPV.Size = new System.Drawing.Size(72, 20);
            this.txtDelayPV.TabIndex = 42;

            // ── lblDelaySV ───────────────────────────────────────────────
            this.lblDelaySV.AutoSize = true;
            this.lblDelaySV.Location = new System.Drawing.Point(242, 184);
            this.lblDelaySV.Name = "lblDelaySV";
            this.lblDelaySV.Size = new System.Drawing.Size(51, 13);
            this.lblDelaySV.TabIndex = 43;
            this.lblDelaySV.Text = "DelaySV:";

            // ── txtDelaySV ───────────────────────────────────────────────
            this.txtDelaySV.Location = new System.Drawing.Point(300, 181);
            this.txtDelaySV.Name = "txtDelaySV";
            this.txtDelaySV.Size = new System.Drawing.Size(72, 20);
            this.txtDelaySV.TabIndex = 44;

            // ── btnSetDelay ──────────────────────────────────────────────
            this.btnSetDelay.Location = new System.Drawing.Point(380, 180);
            this.btnSetDelay.Name = "btnSetDelay";
            this.btnSetDelay.Size = new System.Drawing.Size(85, 26);
            this.btnSetDelay.TabIndex = 45;
            this.btnSetDelay.Text = "Set Delay";
            this.btnSetDelay.UseVisualStyleBackColor = true;
            this.btnSetDelay.Click += new System.EventHandler(this.btnSetDelay_Click);

            // ── lblStatusLbl ─────────────────────────────────────────────
            this.lblStatusLbl.AutoSize = true;
            this.lblStatusLbl.Location = new System.Drawing.Point(8, 215);
            this.lblStatusLbl.Name = "lblStatusLbl";
            this.lblStatusLbl.Size = new System.Drawing.Size(60, 13);
            this.lblStatusLbl.TabIndex = 50;
            this.lblStatusLbl.Text = "Phản hồi:";

            // ── labStatus ────────────────────────────────────────────────
            this.labStatus.AutoSize = false;
            this.labStatus.Location = new System.Drawing.Point(72, 215);
            this.labStatus.Name = "labStatus";
            this.labStatus.Size = new System.Drawing.Size(748, 17);
            this.labStatus.TabIndex = 51;
            this.labStatus.Text = "";

            // ── lblLog ───────────────────────────────────────────────────
            this.lblLog.AutoSize = true;
            this.lblLog.Location = new System.Drawing.Point(8, 244);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new System.Drawing.Size(25, 13);
            this.lblLog.TabIndex = 60;
            this.lblLog.Text = "Log:";

            // ── btnClearLog ──────────────────────────────────────────────
            this.btnClearLog.Location = new System.Drawing.Point(40, 239);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(75, 23);
            this.btnClearLog.TabIndex = 61;
            this.btnClearLog.Text = "Xóa log";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);

            // ── rtbLog ───────────────────────────────────────────────────
            this.rtbLog.Anchor = ((System.Windows.Forms.AnchorStyles)(
                (System.Windows.Forms.AnchorStyles.Top
                | System.Windows.Forms.AnchorStyles.Bottom
                | System.Windows.Forms.AnchorStyles.Left
                | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLog.BackColor = System.Drawing.Color.Black;
            this.rtbLog.ForeColor = System.Drawing.Color.Lime;
            this.rtbLog.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtbLog.Location = new System.Drawing.Point(8, 268);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.rtbLog.Size = new System.Drawing.Size(814, 250);
            this.rtbLog.TabIndex = 62;
            this.rtbLog.Text = "";

            // ── frmTcpTest ───────────────────────────────────────────────
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(830, 530);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this.lblIp,
                this.txtIp,
                this.lblPort,
                this.txtPort,
                this.btnConnect,
                this.btnDisconnect,
                this.labConnection,
                this.btn_startprint,
                this.btn_stopprint,
                this.lblIDMSG,
                this.cbbIDMSG,
                this.btn_sendSTRING1,
                this.lblStr1,
                this.txtString1,
                this.lblStr2,
                this.txtString2,
                this.lblStr3,
                this.txtString3,
                this.lblStr4,
                this.txtString4,
                this.btn_GetSpeed,
                this.lblSpeedPV,
                this.txt_speedPV,
                this.lblSpeedSV,
                this.txt_SpeedSV,
                this.btn_Setspeed,
                this.btnGetDelay,
                this.lblDelayPV,
                this.txtDelayPV,
                this.lblDelaySV,
                this.txtDelaySV,
                this.btnSetDelay,
                this.lblStatusLbl,
                this.labStatus,
                this.lblLog,
                this.btnClearLog,
                this.rtbLog
            });
            this.MinimumSize = new System.Drawing.Size(848, 569);
            this.Name = "frmTcpTest";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Anser SmartU2 - TCP Test (192.168.4.70:4001)";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label labConnection;
        private System.Windows.Forms.Button btn_startprint;
        private System.Windows.Forms.Button btn_stopprint;
        private System.Windows.Forms.Label lblIDMSG;
        private System.Windows.Forms.ComboBox cbbIDMSG;
        private System.Windows.Forms.Button btn_sendSTRING1;
        private System.Windows.Forms.Label lblStr1;
        private System.Windows.Forms.TextBox txtString1;
        private System.Windows.Forms.Label lblStr2;
        private System.Windows.Forms.TextBox txtString2;
        private System.Windows.Forms.Label lblStr3;
        private System.Windows.Forms.TextBox txtString3;
        private System.Windows.Forms.Label lblStr4;
        private System.Windows.Forms.TextBox txtString4;
        private System.Windows.Forms.Button btn_GetSpeed;
        private System.Windows.Forms.Label lblSpeedPV;
        private System.Windows.Forms.TextBox txt_speedPV;
        private System.Windows.Forms.Label lblSpeedSV;
        private System.Windows.Forms.TextBox txt_SpeedSV;
        private System.Windows.Forms.Button btn_Setspeed;
        private System.Windows.Forms.Button btnGetDelay;
        private System.Windows.Forms.Label lblDelayPV;
        private System.Windows.Forms.TextBox txtDelayPV;
        private System.Windows.Forms.Label lblDelaySV;
        private System.Windows.Forms.TextBox txtDelaySV;
        private System.Windows.Forms.Button btnSetDelay;
        private System.Windows.Forms.Label lblStatusLbl;
        private System.Windows.Forms.Label labStatus;
        private System.Windows.Forms.Label lblLog;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.RichTextBox rtbLog;
    }
}
