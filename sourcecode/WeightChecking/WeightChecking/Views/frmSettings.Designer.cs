
namespace WeightChecking
{
    partial class frmSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSettings));
            this._labDescription = new DevExpress.XtraEditors.LabelControl();
            this._btnSave = new DevExpress.XtraEditors.SimpleButton();
            this._propertyGridControlConfig = new DevExpress.XtraVerticalGrid.PropertyGridControl();
            ((System.ComponentModel.ISupportInitialize)(this._propertyGridControlConfig)).BeginInit();
            this.SuspendLayout();
            // 
            // _labDescription
            // 
            this._labDescription.Appearance.Options.UseTextOptions = true;
            this._labDescription.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this._labDescription.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this._labDescription.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            this._labDescription.Location = new System.Drawing.Point(12, 552);
            this._labDescription.Name = "_labDescription";
            this._labDescription.Size = new System.Drawing.Size(844, 85);
            this._labDescription.TabIndex = 1;
            this._labDescription.Text = "labelControl1";
            // 
            // _btnSave
            // 
            this._btnSave.Appearance.BackColor = DevExpress.LookAndFeel.DXSkinColors.FillColors.Success;
            this._btnSave.Appearance.Options.UseBackColor = true;
            this._btnSave.ImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("_btnSave.ImageOptions.SvgImage")));
            this._btnSave.ImageOptions.SvgImageSize = new System.Drawing.Size(20, 20);
            this._btnSave.Location = new System.Drawing.Point(12, 5);
            this._btnSave.Name = "_btnSave";
            this._btnSave.Size = new System.Drawing.Size(75, 23);
            this._btnSave.TabIndex = 2;
            this._btnSave.Text = "Save";
            // 
            // _propertyGridControlConfig
            // 
            this._propertyGridControlConfig.Cursor = System.Windows.Forms.Cursors.Default;
            this._propertyGridControlConfig.Location = new System.Drawing.Point(12, 34);
            this._propertyGridControlConfig.Name = "_propertyGridControlConfig";
            this._propertyGridControlConfig.OptionsView.AllowReadOnlyRowAppearance = DevExpress.Utils.DefaultBoolean.True;
            this._propertyGridControlConfig.Size = new System.Drawing.Size(844, 512);
            this._propertyGridControlConfig.TabIndex = 3;
            // 
            // frmSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(868, 649);
            this.Controls.Add(this._propertyGridControlConfig);
            this.Controls.Add(this._btnSave);
            this.Controls.Add(this._labDescription);
            this.Name = "frmSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)(this._propertyGridControlConfig)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private DevExpress.XtraEditors.LabelControl _labDescription;
        private DevExpress.XtraEditors.SimpleButton _btnSave;
        private DevExpress.XtraVerticalGrid.PropertyGridControl _propertyGridControlConfig;
    }
}