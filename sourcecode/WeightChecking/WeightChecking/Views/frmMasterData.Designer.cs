
namespace WeightChecking
{
    partial class frmMasterData
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
            this.grc = new DevExpress.XtraGrid.GridControl();
            this.grv = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.grc)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grv)).BeginInit();
            this.SuspendLayout();
            // 
            // grc
            // 
            this.grc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grc.EmbeddedNavigator.Buttons.Edit.Enabled = false;
            this.grc.EmbeddedNavigator.Buttons.EndEdit.Enabled = false;
            this.grc.EmbeddedNavigator.Buttons.Remove.Enabled = false;
            this.grc.Location = new System.Drawing.Point(0, 0);
            this.grc.MainView = this.grv;
            this.grc.Name = "grc";
            this.grc.Size = new System.Drawing.Size(1299, 817);
            this.grc.TabIndex = 0;
            this.grc.UseEmbeddedNavigator = true;
            this.grc.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.grv});
            // 
            // grv
            // 
            this.grv.Appearance.HeaderPanel.Font = new System.Drawing.Font("Tahoma", 10F);
            this.grv.Appearance.HeaderPanel.Options.UseFont = true;
            this.grv.Appearance.Row.Font = new System.Drawing.Font("Tahoma", 10F);
            this.grv.Appearance.Row.Options.UseFont = true;
            this.grv.GridControl = this.grc;
            this.grv.Name = "grv";
            this.grv.OptionsBehavior.Editable = false;
            this.grv.OptionsPrint.AutoWidth = false;
            this.grv.OptionsView.ColumnAutoWidth = false;
            this.grv.RowClick += new DevExpress.XtraGrid.Views.Grid.RowClickEventHandler(this.grv_RowClick);
            // 
            // frmMasterData
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1299, 817);
            this.Controls.Add(this.grc);
            this.Name = "frmMasterData";
            this.Text = "Master Data";
            ((System.ComponentModel.ISupportInitialize)(this.grc)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grv)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl grc;
        private DevExpress.XtraGrid.Views.Grid.GridView grv;
    }
}