namespace CognexScannerTester
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabMetal;
        private System.Windows.Forms.TabPage tabScale;

        private void InitializeComponent()
        {
            this.tabControl = new System.Windows.Forms.TabControl();
            this.tabMetal = new System.Windows.Forms.TabPage();
            this.tabScale = new System.Windows.Forms.TabPage();
            this.tabControl.SuspendLayout();
            this.SuspendLayout();
            //
            // tabControl
            //
            this.tabControl.Controls.Add(this.tabMetal);
            this.tabControl.Controls.Add(this.tabScale);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Size = new System.Drawing.Size(760, 460);
            //
            // tabMetal
            //
            this.tabMetal.Text = "Metal Scanner";
            this.tabMetal.UseVisualStyleBackColor = true;
            this.tabMetal.Padding = new System.Windows.Forms.Padding(8);
            //
            // tabScale
            //
            this.tabScale.Text = "Scale Scanner";
            this.tabScale.UseVisualStyleBackColor = true;
            this.tabScale.Padding = new System.Windows.Forms.Padding(8);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(760, 460);
            this.Controls.Add(this.tabControl);
            this.MinimumSize = new System.Drawing.Size(700, 420);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Cognex Scanner Tester";
            this.tabControl.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
