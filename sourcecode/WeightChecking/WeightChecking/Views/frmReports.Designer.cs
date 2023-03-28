
namespace WeightChecking
{
    partial class frmReports
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
            this.grcReports = new DevExpress.XtraGrid.GridControl();
            this.grvReports = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.xtraTabControl1 = new DevExpress.XtraTab.XtraTabControl();
            this.xtraTabPage1 = new DevExpress.XtraTab.XtraTabPage();
            this.xtraTabPage2 = new DevExpress.XtraTab.XtraTabPage();
            this.grcApprove = new DevExpress.XtraGrid.GridControl();
            this.grvApprove = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.xtraTabPage3 = new DevExpress.XtraTab.XtraTabPage();
            this.grcMissInfo = new DevExpress.XtraGrid.GridControl();
            this.grvMissInfo = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.xtraTabPage4 = new DevExpress.XtraTab.XtraTabPage();
            this.grcReject = new DevExpress.XtraGrid.GridControl();
            this.grvReject = new DevExpress.XtraGrid.Views.Grid.GridView();
            ((System.ComponentModel.ISupportInitialize)(this.grcReports)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grvReports)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xtraTabControl1)).BeginInit();
            this.xtraTabControl1.SuspendLayout();
            this.xtraTabPage1.SuspendLayout();
            this.xtraTabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grcApprove)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grvApprove)).BeginInit();
            this.xtraTabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grcMissInfo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grvMissInfo)).BeginInit();
            this.xtraTabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grcReject)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.grvReject)).BeginInit();
            this.SuspendLayout();
            // 
            // grcReports
            // 
            this.grcReports.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grcReports.EmbeddedNavigator.Buttons.Edit.Enabled = false;
            this.grcReports.EmbeddedNavigator.Buttons.EndEdit.Enabled = false;
            this.grcReports.EmbeddedNavigator.Buttons.Remove.Enabled = false;
            this.grcReports.Location = new System.Drawing.Point(0, 0);
            this.grcReports.MainView = this.grvReports;
            this.grcReports.Name = "grcReports";
            this.grcReports.Size = new System.Drawing.Size(1408, 754);
            this.grcReports.TabIndex = 0;
            this.grcReports.UseEmbeddedNavigator = true;
            this.grcReports.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.grvReports});
            // 
            // grvReports
            // 
            this.grvReports.GridControl = this.grcReports;
            this.grvReports.Name = "grvReports";
            this.grvReports.OptionsBehavior.ReadOnly = true;
            this.grvReports.OptionsDetail.ShowEmbeddedDetailIndent = DevExpress.Utils.DefaultBoolean.False;
            this.grvReports.OptionsView.ColumnAutoWidth = false;
            // 
            // xtraTabControl1
            // 
            this.xtraTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.xtraTabControl1.Location = new System.Drawing.Point(0, 0);
            this.xtraTabControl1.Name = "xtraTabControl1";
            this.xtraTabControl1.SelectedTabPage = this.xtraTabPage1;
            this.xtraTabControl1.Size = new System.Drawing.Size(1410, 785);
            this.xtraTabControl1.TabIndex = 1;
            this.xtraTabControl1.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] {
            this.xtraTabPage1,
            this.xtraTabPage2,
            this.xtraTabPage3,
            this.xtraTabPage4});
            // 
            // xtraTabPage1
            // 
            this.xtraTabPage1.Appearance.Header.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xtraTabPage1.Appearance.Header.Options.UseFont = true;
            this.xtraTabPage1.Controls.Add(this.grcReports);
            this.xtraTabPage1.Name = "xtraTabPage1";
            this.xtraTabPage1.Size = new System.Drawing.Size(1408, 754);
            this.xtraTabPage1.Text = "Scan Data";
            // 
            // xtraTabPage2
            // 
            this.xtraTabPage2.Appearance.Header.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xtraTabPage2.Appearance.Header.Options.UseFont = true;
            this.xtraTabPage2.Controls.Add(this.grcApprove);
            this.xtraTabPage2.Name = "xtraTabPage2";
            this.xtraTabPage2.Size = new System.Drawing.Size(1408, 754);
            this.xtraTabPage2.Text = "Approved Print";
            // 
            // grcApprove
            // 
            this.grcApprove.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grcApprove.EmbeddedNavigator.Buttons.Edit.Enabled = false;
            this.grcApprove.EmbeddedNavigator.Buttons.EndEdit.Enabled = false;
            this.grcApprove.EmbeddedNavigator.Buttons.Remove.Enabled = false;
            this.grcApprove.Location = new System.Drawing.Point(0, 0);
            this.grcApprove.MainView = this.grvApprove;
            this.grcApprove.Name = "grcApprove";
            this.grcApprove.Size = new System.Drawing.Size(1408, 754);
            this.grcApprove.TabIndex = 1;
            this.grcApprove.UseEmbeddedNavigator = true;
            this.grcApprove.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.grvApprove});
            // 
            // grvApprove
            // 
            this.grvApprove.GridControl = this.grcApprove;
            this.grvApprove.Name = "grvApprove";
            this.grvApprove.OptionsBehavior.ReadOnly = true;
            this.grvApprove.OptionsDetail.ShowEmbeddedDetailIndent = DevExpress.Utils.DefaultBoolean.True;
            this.grvApprove.OptionsView.ColumnAutoWidth = false;
            // 
            // xtraTabPage3
            // 
            this.xtraTabPage3.Appearance.Header.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xtraTabPage3.Appearance.Header.Options.UseFont = true;
            this.xtraTabPage3.Controls.Add(this.grcMissInfo);
            this.xtraTabPage3.Name = "xtraTabPage3";
            this.xtraTabPage3.Size = new System.Drawing.Size(1408, 754);
            this.xtraTabPage3.Text = "Missing Infomation";
            // 
            // grcMissInfo
            // 
            this.grcMissInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grcMissInfo.EmbeddedNavigator.Buttons.Edit.Enabled = false;
            this.grcMissInfo.EmbeddedNavigator.Buttons.EndEdit.Enabled = false;
            this.grcMissInfo.EmbeddedNavigator.Buttons.Remove.Enabled = false;
            this.grcMissInfo.Location = new System.Drawing.Point(0, 0);
            this.grcMissInfo.MainView = this.grvMissInfo;
            this.grcMissInfo.Name = "grcMissInfo";
            this.grcMissInfo.Size = new System.Drawing.Size(1408, 754);
            this.grcMissInfo.TabIndex = 2;
            this.grcMissInfo.UseEmbeddedNavigator = true;
            this.grcMissInfo.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.grvMissInfo});
            // 
            // grvMissInfo
            // 
            this.grvMissInfo.GridControl = this.grcMissInfo;
            this.grvMissInfo.Name = "grvMissInfo";
            this.grvMissInfo.OptionsBehavior.ReadOnly = true;
            this.grvMissInfo.OptionsDetail.ShowEmbeddedDetailIndent = DevExpress.Utils.DefaultBoolean.True;
            this.grvMissInfo.OptionsView.ColumnAutoWidth = false;
            // 
            // xtraTabPage4
            // 
            this.xtraTabPage4.Appearance.Header.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xtraTabPage4.Appearance.Header.Options.UseFont = true;
            this.xtraTabPage4.Controls.Add(this.grcReject);
            this.xtraTabPage4.Name = "xtraTabPage4";
            this.xtraTabPage4.Size = new System.Drawing.Size(1408, 754);
            this.xtraTabPage4.Text = "Reject";
            // 
            // grcReject
            // 
            this.grcReject.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grcReject.Location = new System.Drawing.Point(0, 0);
            this.grcReject.MainView = this.grvReject;
            this.grcReject.Name = "grcReject";
            this.grcReject.Size = new System.Drawing.Size(1408, 754);
            this.grcReject.TabIndex = 0;
            this.grcReject.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.grvReject});
            // 
            // grvReject
            // 
            this.grvReject.GridControl = this.grcReject;
            this.grvReject.Name = "grvReject";
            this.grvReject.OptionsBehavior.ReadOnly = true;
            // 
            // frmReports
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1410, 785);
            this.Controls.Add(this.xtraTabControl1);
            this.Name = "frmReports";
            this.Text = "Report";
            ((System.ComponentModel.ISupportInitialize)(this.grcReports)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grvReports)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xtraTabControl1)).EndInit();
            this.xtraTabControl1.ResumeLayout(false);
            this.xtraTabPage1.ResumeLayout(false);
            this.xtraTabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grcApprove)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grvApprove)).EndInit();
            this.xtraTabPage3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grcMissInfo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grvMissInfo)).EndInit();
            this.xtraTabPage4.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grcReject)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.grvReject)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl grcReports;
        private DevExpress.XtraGrid.Views.Grid.GridView grvReports;
        private DevExpress.XtraTab.XtraTabControl xtraTabControl1;
        private DevExpress.XtraTab.XtraTabPage xtraTabPage1;
        private DevExpress.XtraTab.XtraTabPage xtraTabPage2;
        private DevExpress.XtraGrid.GridControl grcApprove;
        private DevExpress.XtraGrid.Views.Grid.GridView grvApprove;
        private DevExpress.XtraTab.XtraTabPage xtraTabPage3;
        private DevExpress.XtraGrid.GridControl grcMissInfo;
        private DevExpress.XtraGrid.Views.Grid.GridView grvMissInfo;
        private DevExpress.XtraTab.XtraTabPage xtraTabPage4;
        private DevExpress.XtraGrid.GridControl grcReject;
        private DevExpress.XtraGrid.Views.Grid.GridView grvReject;
    }
}