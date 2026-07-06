using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblStatus = new System.Windows.Forms.Label();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this._txtNewValue = new System.Windows.Forms.TextBox();
            this._cbTagName = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this._labQR1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(10, 8);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(35, 13);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "label1";
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(10, 157);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(647, 212);
            this.listBox1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "label1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "label1";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 94);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "label1";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(469, 113);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(197, 20);
            this.button1.TabIndex = 5;
            this.button1.Text = "Write to PLC";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // _txtNewValue
            // 
            this._txtNewValue.Location = new System.Drawing.Point(469, 66);
            this._txtNewValue.Name = "_txtNewValue";
            this._txtNewValue.Size = new System.Drawing.Size(198, 20);
            this._txtNewValue.TabIndex = 6;
            // 
            // _cbTagName
            // 
            this._cbTagName.FormattingEnabled = true;
            this._cbTagName.Location = new System.Drawing.Point(471, 28);
            this._cbTagName.Name = "_cbTagName";
            this._cbTagName.Size = new System.Drawing.Size(187, 21);
            this._cbTagName.TabIndex = 7;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(413, 30);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(57, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Tag Name";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(411, 68);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(56, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "NewValue";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Italic);
            this.label6.Location = new System.Drawing.Point(471, 89);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(150, 12);
            this.label6.TabIndex = 10;
            this.label6.Text = "(According to the data type of tag)";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 385);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(105, 13);
            this.label7.TabIndex = 11;
            this.label7.Text = "QR Scanner Cognex";
            // 
            // _labQR1
            // 
            this._labQR1.AutoSize = true;
            this._labQR1.Location = new System.Drawing.Point(136, 385);
            this._labQR1.Name = "_labQR1";
            this._labQR1.Size = new System.Drawing.Size(34, 13);
            this._labQR1.TabIndex = 12;
            this._labQR1.Text = "Value";
            // 
            // Form1
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(686, 517);
            this.Controls.Add(this._labQR1);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this._cbTagName);
            this.Controls.Add(this._txtNewValue);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.lblStatus);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Label lblStatus;
        private ListBox listBox1;
        private Label label1;
        private Label label2;
        private Label label3;
        private Button button1;
        private TextBox _txtNewValue;
        private ComboBox _cbTagName;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label _labQR1;
    }
}

