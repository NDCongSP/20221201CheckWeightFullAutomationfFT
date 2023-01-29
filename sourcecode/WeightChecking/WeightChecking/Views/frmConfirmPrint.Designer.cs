
namespace WeightChecking
{
    partial class frmConfirmPrint
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmConfirmPrint));
            this.txtQrCode = new DevExpress.XtraEditors.TextEdit();
            this.labQrCode = new DevExpress.XtraEditors.LabelControl();
            this.btnConfirm = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.labIdLabel = new DevExpress.XtraEditors.LabelControl();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.labWeight = new DevExpress.XtraEditors.LabelControl();
            this.labelControl6 = new DevExpress.XtraEditors.LabelControl();
            this.labOcNo = new DevExpress.XtraEditors.LabelControl();
            this.labelControl8 = new DevExpress.XtraEditors.LabelControl();
            this.labBoxNo = new DevExpress.XtraEditors.LabelControl();
            this.labProductCode = new DevExpress.XtraEditors.LabelControl();
            this.labProCode1 = new DevExpress.XtraEditors.LabelControl();
            this.labQty = new DevExpress.XtraEditors.LabelControl();
            this.labelControl5 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.txtActualDeviation = new DevExpress.XtraEditors.TextEdit();
            ((System.ComponentModel.ISupportInitialize)(this.txtQrCode.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtActualDeviation.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // txtQrCode
            // 
            this.txtQrCode.Location = new System.Drawing.Point(11, 89);
            this.txtQrCode.Name = "txtQrCode";
            this.txtQrCode.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.txtQrCode.Properties.Appearance.Options.UseFont = true;
            this.txtQrCode.Properties.AutoHeight = false;
            this.txtQrCode.Properties.PasswordChar = '*';
            this.txtQrCode.Properties.UseSystemPasswordChar = true;
            this.txtQrCode.Size = new System.Drawing.Size(622, 30);
            this.txtQrCode.TabIndex = 1;
            // 
            // labQrCode
            // 
            this.labQrCode.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labQrCode.Appearance.Options.UseFont = true;
            this.labQrCode.Location = new System.Drawing.Point(12, 56);
            this.labQrCode.Name = "labQrCode";
            this.labQrCode.Size = new System.Drawing.Size(135, 24);
            this.labQrCode.TabIndex = 1;
            this.labQrCode.Text = "Scan QR Label:";
            // 
            // btnConfirm
            // 
            this.btnConfirm.Appearance.BackColor = DevExpress.LookAndFeel.DXSkinColors.FillColors.Success;
            this.btnConfirm.Appearance.Font = new System.Drawing.Font("Tahoma", 30F);
            this.btnConfirm.Appearance.Options.UseBackColor = true;
            this.btnConfirm.Appearance.Options.UseFont = true;
            this.btnConfirm.Location = new System.Drawing.Point(11, 302);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(622, 51);
            this.btnConfirm.TabIndex = 3;
            this.btnConfirm.Text = "Confirm";
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(12, 137);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(82, 24);
            this.labelControl2.TabIndex = 3;
            this.labelControl2.Text = "ID Label:";
            // 
            // labIdLabel
            // 
            this.labIdLabel.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labIdLabel.Appearance.Options.UseFont = true;
            this.labIdLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.labIdLabel.Location = new System.Drawing.Point(100, 137);
            this.labIdLabel.Name = "labIdLabel";
            this.labIdLabel.Size = new System.Drawing.Size(157, 24);
            this.labIdLabel.TabIndex = 4;
            this.labIdLabel.Text = "0";
            // 
            // labelControl4
            // 
            this.labelControl4.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labelControl4.Appearance.Options.UseFont = true;
            this.labelControl4.Location = new System.Drawing.Point(322, 217);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(213, 24);
            this.labelControl4.TabIndex = 3;
            this.labelControl4.Text = "Real Gross Weight (kg):";
            // 
            // labWeight
            // 
            this.labWeight.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labWeight.Appearance.Options.UseFont = true;
            this.labWeight.Appearance.Options.UseTextOptions = true;
            this.labWeight.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.labWeight.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.labWeight.Location = new System.Drawing.Point(543, 217);
            this.labWeight.Name = "labWeight";
            this.labWeight.Size = new System.Drawing.Size(90, 24);
            this.labWeight.TabIndex = 4;
            this.labWeight.Text = "0";
            // 
            // labelControl6
            // 
            this.labelControl6.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labelControl6.Appearance.Options.UseFont = true;
            this.labelControl6.Location = new System.Drawing.Point(12, 177);
            this.labelControl6.Name = "labelControl6";
            this.labelControl6.Size = new System.Drawing.Size(63, 24);
            this.labelControl6.TabIndex = 3;
            this.labelControl6.Text = "OC No:";
            // 
            // labOcNo
            // 
            this.labOcNo.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labOcNo.Appearance.Options.UseFont = true;
            this.labOcNo.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.labOcNo.Location = new System.Drawing.Point(100, 177);
            this.labOcNo.Name = "labOcNo";
            this.labOcNo.Size = new System.Drawing.Size(157, 24);
            this.labOcNo.TabIndex = 4;
            this.labOcNo.Text = "0";
            // 
            // labelControl8
            // 
            this.labelControl8.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labelControl8.Appearance.Options.UseFont = true;
            this.labelControl8.Location = new System.Drawing.Point(11, 216);
            this.labelControl8.Name = "labelControl8";
            this.labelControl8.Size = new System.Drawing.Size(70, 24);
            this.labelControl8.TabIndex = 3;
            this.labelControl8.Text = "Box No:";
            // 
            // labBoxNo
            // 
            this.labBoxNo.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labBoxNo.Appearance.Options.UseFont = true;
            this.labBoxNo.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.labBoxNo.Location = new System.Drawing.Point(99, 216);
            this.labBoxNo.Name = "labBoxNo";
            this.labBoxNo.Size = new System.Drawing.Size(158, 24);
            this.labBoxNo.TabIndex = 4;
            this.labBoxNo.Text = "0";
            // 
            // labProductCode
            // 
            this.labProductCode.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labProductCode.Appearance.Options.UseFont = true;
            this.labProductCode.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.labProductCode.Location = new System.Drawing.Point(147, 256);
            this.labProductCode.Name = "labProductCode";
            this.labProductCode.Size = new System.Drawing.Size(487, 24);
            this.labProductCode.TabIndex = 6;
            this.labProductCode.Text = "0";
            // 
            // labProCode1
            // 
            this.labProCode1.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labProCode1.Appearance.Options.UseFont = true;
            this.labProCode1.Location = new System.Drawing.Point(12, 256);
            this.labProCode1.Name = "labProCode1";
            this.labProCode1.Size = new System.Drawing.Size(129, 24);
            this.labProCode1.TabIndex = 5;
            this.labProCode1.Text = "ProItem Code:";
            // 
            // labQty
            // 
            this.labQty.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labQty.Appearance.Options.UseFont = true;
            this.labQty.Appearance.Options.UseTextOptions = true;
            this.labQty.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.labQty.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.labQty.Location = new System.Drawing.Point(543, 256);
            this.labQty.Name = "labQty";
            this.labQty.Size = new System.Drawing.Size(90, 24);
            this.labQty.TabIndex = 8;
            this.labQty.Text = "0";
            // 
            // labelControl5
            // 
            this.labelControl5.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labelControl5.Appearance.Options.UseFont = true;
            this.labelControl5.Location = new System.Drawing.Point(403, 256);
            this.labelControl5.Name = "labelControl5";
            this.labelControl5.Size = new System.Drawing.Size(132, 24);
            this.labelControl5.TabIndex = 7;
            this.labelControl5.Text = "Quantity (prs):";
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(10, 15);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(202, 24);
            this.labelControl1.TabIndex = 9;
            this.labelControl1.Text = "Actual Deviation (prs):";
            // 
            // txtActualDeviation
            // 
            this.txtActualDeviation.EditValue = "0";
            this.txtActualDeviation.Location = new System.Drawing.Point(218, 12);
            this.txtActualDeviation.Name = "txtActualDeviation";
            this.txtActualDeviation.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 15F);
            this.txtActualDeviation.Properties.Appearance.Options.UseFont = true;
            this.txtActualDeviation.Properties.Appearance.Options.UseTextOptions = true;
            this.txtActualDeviation.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.txtActualDeviation.Properties.AutoHeight = false;
            this.txtActualDeviation.Size = new System.Drawing.Size(127, 30);
            this.txtActualDeviation.TabIndex = 2;
            // 
            // frmConfirmPrint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(651, 407);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this.labQty);
            this.Controls.Add(this.labelControl5);
            this.Controls.Add(this.labProductCode);
            this.Controls.Add(this.labProCode1);
            this.Controls.Add(this.labWeight);
            this.Controls.Add(this.labelControl4);
            this.Controls.Add(this.labBoxNo);
            this.Controls.Add(this.labelControl8);
            this.Controls.Add(this.labOcNo);
            this.Controls.Add(this.labelControl6);
            this.Controls.Add(this.labIdLabel);
            this.Controls.Add(this.labelControl2);
            this.Controls.Add(this.btnConfirm);
            this.Controls.Add(this.labQrCode);
            this.Controls.Add(this.txtActualDeviation);
            this.Controls.Add(this.txtQrCode);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmConfirmPrint";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Confirm Print";
            ((System.ComponentModel.ISupportInitialize)(this.txtQrCode.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtActualDeviation.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraEditors.TextEdit txtQrCode;
        private DevExpress.XtraEditors.LabelControl labQrCode;
        private DevExpress.XtraEditors.SimpleButton btnConfirm;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labIdLabel;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.LabelControl labWeight;
        private DevExpress.XtraEditors.LabelControl labelControl6;
        private DevExpress.XtraEditors.LabelControl labOcNo;
        private DevExpress.XtraEditors.LabelControl labelControl8;
        private DevExpress.XtraEditors.LabelControl labBoxNo;
        private DevExpress.XtraEditors.LabelControl labProductCode;
        private DevExpress.XtraEditors.LabelControl labProCode1;
        private DevExpress.XtraEditors.LabelControl labQty;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.TextEdit txtActualDeviation;
    }
}