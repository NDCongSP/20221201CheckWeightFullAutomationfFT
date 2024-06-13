
namespace ScannerCam
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
            this._labQRCode = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _labQRCode
            // 
            this._labQRCode.AutoSize = true;
            this._labQRCode.Location = new System.Drawing.Point(23, 43);
            this._labQRCode.Name = "_labQRCode";
            this._labQRCode.Size = new System.Drawing.Size(35, 13);
            this._labQRCode.TabIndex = 0;
            this._labQRCode.Text = "label1";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1368, 310);
            this.Controls.Add(this._labQRCode);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _labQRCode;
    }
}

