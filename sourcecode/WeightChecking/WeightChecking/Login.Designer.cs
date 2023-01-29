
namespace WeightChecking
{
    partial class Login
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Login));
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.btnSubmit = new DevExpress.XtraEditors.SimpleButton();
            this.chkRemember = new DevExpress.XtraEditors.CheckEdit();
            this.txtPass = new DevExpress.XtraEditors.TextEdit();
            this.txtUseName = new DevExpress.XtraEditors.TextEdit();
            this.labPass = new DevExpress.XtraEditors.LabelControl();
            this.labUserName = new DevExpress.XtraEditors.LabelControl();
            this.labStatus = new DevExpress.XtraEditors.LabelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.labTime = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chkRemember.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPass.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtUseName.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // panelControl1
            // 
            this.panelControl1.Controls.Add(this.btnSubmit);
            this.panelControl1.Controls.Add(this.chkRemember);
            this.panelControl1.Controls.Add(this.txtPass);
            this.panelControl1.Controls.Add(this.txtUseName);
            this.panelControl1.Controls.Add(this.labPass);
            this.panelControl1.Controls.Add(this.labUserName);
            this.panelControl1.Location = new System.Drawing.Point(579, 164);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(804, 492);
            this.panelControl1.TabIndex = 0;
            // 
            // btnSubmit
            // 
            this.btnSubmit.Appearance.BackColor = DevExpress.LookAndFeel.DXSkinColors.FillColors.Success;
            this.btnSubmit.Appearance.Font = new System.Drawing.Font("Tahoma", 30F);
            this.btnSubmit.Appearance.Options.UseBackColor = true;
            this.btnSubmit.Appearance.Options.UseFont = true;
            this.btnSubmit.Location = new System.Drawing.Point(30, 374);
            this.btnSubmit.Name = "btnSubmit";
            this.btnSubmit.Size = new System.Drawing.Size(744, 60);
            this.btnSubmit.TabIndex = 4;
            this.btnSubmit.Text = "Login";
            this.btnSubmit.Click += new System.EventHandler(this.btnSubmit_Click);
            // 
            // chkRemember
            // 
            this.chkRemember.Location = new System.Drawing.Point(30, 303);
            this.chkRemember.Name = "chkRemember";
            this.chkRemember.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.chkRemember.Properties.Appearance.Options.UseFont = true;
            this.chkRemember.Properties.Caption = "Remember";
            this.chkRemember.Size = new System.Drawing.Size(123, 28);
            this.chkRemember.TabIndex = 3;
            // 
            // txtPass
            // 
            this.txtPass.EditValue = "";
            this.txtPass.Location = new System.Drawing.Point(30, 206);
            this.txtPass.Name = "txtPass";
            this.txtPass.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 30F);
            this.txtPass.Properties.Appearance.Options.UseFont = true;
            this.txtPass.Properties.NullText = "UserName";
            this.txtPass.Properties.PasswordChar = '*';
            this.txtPass.Properties.UseSystemPasswordChar = true;
            this.txtPass.Size = new System.Drawing.Size(744, 54);
            this.txtPass.TabIndex = 2;
            // 
            // txtUseName
            // 
            this.txtUseName.EditValue = "";
            this.txtUseName.Location = new System.Drawing.Point(30, 80);
            this.txtUseName.Name = "txtUseName";
            this.txtUseName.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 30F);
            this.txtUseName.Properties.Appearance.Options.UseFont = true;
            this.txtUseName.Properties.NullText = "UserName";
            this.txtUseName.Size = new System.Drawing.Size(744, 54);
            this.txtUseName.TabIndex = 1;
            // 
            // labPass
            // 
            this.labPass.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labPass.Appearance.Options.UseFont = true;
            this.labPass.Location = new System.Drawing.Point(30, 176);
            this.labPass.Name = "labPass";
            this.labPass.Size = new System.Drawing.Size(84, 24);
            this.labPass.TabIndex = 0;
            this.labPass.Text = "Password";
            // 
            // labUserName
            // 
            this.labUserName.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labUserName.Appearance.Options.UseFont = true;
            this.labUserName.Location = new System.Drawing.Point(30, 50);
            this.labUserName.Name = "labUserName";
            this.labUserName.Size = new System.Drawing.Size(96, 24);
            this.labUserName.TabIndex = 0;
            this.labUserName.Text = "User name";
            // 
            // labStatus
            // 
            this.labStatus.Appearance.ForeColor = System.Drawing.Color.White;
            this.labStatus.Appearance.Options.UseForeColor = true;
            this.labStatus.Location = new System.Drawing.Point(1843, 1023);
            this.labStatus.Name = "labStatus";
            this.labStatus.Size = new System.Drawing.Size(63, 13);
            this.labStatus.TabIndex = 1;
            this.labStatus.Text = "labelControl1";
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 50F, System.Drawing.FontStyle.Bold);
            this.labelControl1.Appearance.ForeColor = System.Drawing.Color.White;
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Appearance.Options.UseForeColor = true;
            this.labelControl1.Location = new System.Drawing.Point(895, 68);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(173, 81);
            this.labelControl1.TabIndex = 2;
            this.labelControl1.Text = "SSFG";
            // 
            // labTime
            // 
            this.labTime.Appearance.ForeColor = System.Drawing.Color.White;
            this.labTime.Appearance.Options.UseForeColor = true;
            this.labTime.Location = new System.Drawing.Point(12, 1023);
            this.labTime.Name = "labTime";
            this.labTime.Size = new System.Drawing.Size(63, 13);
            this.labTime.TabIndex = 3;
            this.labTime.Text = "labelControl2";
            // 
            // Login
            // 
            this.AcceptButton = this.btnSubmit;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayoutStore = System.Windows.Forms.ImageLayout.Stretch;
            this.BackgroundImageStore = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImageStore")));
            this.ClientSize = new System.Drawing.Size(1918, 1048);
            this.Controls.Add(this.labTime);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this.labStatus);
            this.Controls.Add(this.panelControl1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.IconOptions.ImageUri.Uri = "AlignCenter";
            this.MaximizeBox = false;
            this.Name = "Login";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Login";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            this.panelControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chkRemember.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPass.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtUseName.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.SimpleButton btnSubmit;
        private DevExpress.XtraEditors.CheckEdit chkRemember;
        private DevExpress.XtraEditors.TextEdit txtPass;
        private DevExpress.XtraEditors.TextEdit txtUseName;
        private DevExpress.XtraEditors.LabelControl labPass;
        private DevExpress.XtraEditors.LabelControl labUserName;
        private DevExpress.XtraEditors.LabelControl labStatus;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labTime;
    }
}