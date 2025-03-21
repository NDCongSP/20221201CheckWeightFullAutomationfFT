namespace TEST
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._btnDisconnect = new System.Windows.Forms.Button();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this._labOldValue = new DevExpress.XtraEditors.LabelControl();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this._labNewValue = new DevExpress.XtraEditors.LabelControl();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this._labStatus = new DevExpress.XtraEditors.LabelControl();
            this._btnConnect = new System.Windows.Forms.Button();
            this._btnReconnect = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _btnDisconnect
            // 
            this._btnDisconnect.Location = new System.Drawing.Point(217, 32);
            this._btnDisconnect.Name = "_btnDisconnect";
            this._btnDisconnect.Size = new System.Drawing.Size(75, 23);
            this._btnDisconnect.TabIndex = 0;
            this._btnDisconnect.Text = "Disconnect";
            this._btnDisconnect.UseVisualStyleBackColor = true;
            this._btnDisconnect.Click += new System.EventHandler(this.button1_Click);
            // 
            // labelControl1
            // 
            this.labelControl1.Location = new System.Drawing.Point(111, 127);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(45, 13);
            this.labelControl1.TabIndex = 1;
            this.labelControl1.Text = "Old value";
            // 
            // _labOldValue
            // 
            this._labOldValue.Location = new System.Drawing.Point(187, 127);
            this._labOldValue.Name = "_labOldValue";
            this._labOldValue.Size = new System.Drawing.Size(63, 13);
            this._labOldValue.TabIndex = 2;
            this._labOldValue.Text = "labelControl2";
            // 
            // labelControl3
            // 
            this.labelControl3.Location = new System.Drawing.Point(111, 164);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(50, 13);
            this.labelControl3.TabIndex = 1;
            this.labelControl3.Text = "New value";
            // 
            // _labNewValue
            // 
            this._labNewValue.Location = new System.Drawing.Point(187, 164);
            this._labNewValue.Name = "_labNewValue";
            this._labNewValue.Size = new System.Drawing.Size(63, 13);
            this._labNewValue.TabIndex = 2;
            this._labNewValue.Text = "labelControl2";
            // 
            // labelControl2
            // 
            this.labelControl2.Location = new System.Drawing.Point(111, 96);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(31, 13);
            this.labelControl2.TabIndex = 3;
            this.labelControl2.Text = "Status";
            // 
            // _labStatus
            // 
            this._labStatus.Location = new System.Drawing.Point(187, 96);
            this._labStatus.Name = "_labStatus";
            this._labStatus.Size = new System.Drawing.Size(31, 13);
            this._labStatus.TabIndex = 4;
            this._labStatus.Text = "Status";
            // 
            // _btnConnect
            // 
            this._btnConnect.Location = new System.Drawing.Point(111, 32);
            this._btnConnect.Name = "_btnConnect";
            this._btnConnect.Size = new System.Drawing.Size(75, 23);
            this._btnConnect.TabIndex = 5;
            this._btnConnect.Text = "Connect";
            this._btnConnect.UseVisualStyleBackColor = true;
            this._btnConnect.Click += new System.EventHandler(this._btnConnect_Click);
            // 
            // _btnReconnect
            // 
            this._btnReconnect.Location = new System.Drawing.Point(328, 32);
            this._btnReconnect.Name = "_btnReconnect";
            this._btnReconnect.Size = new System.Drawing.Size(75, 23);
            this._btnReconnect.TabIndex = 6;
            this._btnReconnect.Text = "Reconnect";
            this._btnReconnect.UseVisualStyleBackColor = true;
            this._btnReconnect.Click += new System.EventHandler(this._btnReconnect_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this._btnReconnect);
            this.Controls.Add(this._btnConnect);
            this.Controls.Add(this._labStatus);
            this.Controls.Add(this.labelControl2);
            this.Controls.Add(this._labNewValue);
            this.Controls.Add(this.labelControl3);
            this.Controls.Add(this._labOldValue);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this._btnDisconnect);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _btnDisconnect;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl _labOldValue;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.LabelControl _labNewValue;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl _labStatus;
        private System.Windows.Forms.Button _btnConnect;
        private System.Windows.Forms.Button _btnReconnect;
    }
}

