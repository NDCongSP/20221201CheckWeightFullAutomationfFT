
namespace AnserU2_cSharp
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
            this.btn_Setspeed = new System.Windows.Forms.Button();
            this.txt_SpeedSV = new System.Windows.Forms.TextBox();
            this.btn_GetSpeed = new System.Windows.Forms.Button();
            this.Label3 = new System.Windows.Forms.Label();
            this.Label2 = new System.Windows.Forms.Label();
            this.Label1 = new System.Windows.Forms.Label();
            this.txt_speedPV = new System.Windows.Forms.TextBox();
            this.txtString1 = new System.Windows.Forms.TextBox();
            this.cbbIDMSG = new System.Windows.Forms.ComboBox();
            this.btn_sendSTRING1 = new System.Windows.Forms.Button();
            this.btn_stopprint = new System.Windows.Forms.Button();
            this.btn_startprint = new System.Windows.Forms.Button();
            this.Label_counter = new System.Windows.Forms.Label();
            this.labStatus = new System.Windows.Forms.Label();
            this.txtString2 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtDelayPV = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnGetDelay = new System.Windows.Forms.Button();
            this.txtDelaySV = new System.Windows.Forms.TextBox();
            this.btnSetDelay = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Setspeed
            // 
            this.btn_Setspeed.FlatAppearance.BorderColor = System.Drawing.Color.Aqua;
            this.btn_Setspeed.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_Setspeed.Location = new System.Drawing.Point(403, 112);
            this.btn_Setspeed.Margin = new System.Windows.Forms.Padding(2);
            this.btn_Setspeed.Name = "btn_Setspeed";
            this.btn_Setspeed.Size = new System.Drawing.Size(78, 28);
            this.btn_Setspeed.TabIndex = 26;
            this.btn_Setspeed.Text = "Set speed";
            this.btn_Setspeed.UseVisualStyleBackColor = true;
            this.btn_Setspeed.Click += new System.EventHandler(this.btn_Setspeed_Click);
            // 
            // txt_SpeedSV
            // 
            this.txt_SpeedSV.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_SpeedSV.Location = new System.Drawing.Point(485, 120);
            this.txt_SpeedSV.Margin = new System.Windows.Forms.Padding(2);
            this.txt_SpeedSV.Name = "txt_SpeedSV";
            this.txt_SpeedSV.Size = new System.Drawing.Size(119, 23);
            this.txt_SpeedSV.TabIndex = 25;
            // 
            // btn_GetSpeed
            // 
            this.btn_GetSpeed.FlatAppearance.BorderColor = System.Drawing.Color.Aqua;
            this.btn_GetSpeed.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_GetSpeed.Location = new System.Drawing.Point(403, 66);
            this.btn_GetSpeed.Margin = new System.Windows.Forms.Padding(2);
            this.btn_GetSpeed.Name = "btn_GetSpeed";
            this.btn_GetSpeed.Size = new System.Drawing.Size(78, 28);
            this.btn_GetSpeed.TabIndex = 24;
            this.btn_GetSpeed.Text = "Get speed";
            this.btn_GetSpeed.UseVisualStyleBackColor = true;
            this.btn_GetSpeed.Click += new System.EventHandler(this.btn_GetSpeed_Click);
            // 
            // Label3
            // 
            this.Label3.AutoSize = true;
            this.Label3.Location = new System.Drawing.Point(483, 58);
            this.Label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(41, 13);
            this.Label3.TabIndex = 23;
            this.Label3.Text = "Speed:";
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Location = new System.Drawing.Point(159, 153);
            this.Label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(43, 13);
            this.Label2.TabIndex = 22;
            this.Label2.Text = "String 1";
            // 
            // Label1
            // 
            this.Label1.AutoSize = true;
            this.Label1.Location = new System.Drawing.Point(174, 86);
            this.Label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.Label1.Name = "Label1";
            this.Label1.Size = new System.Drawing.Size(53, 13);
            this.Label1.TabIndex = 21;
            this.Label1.Text = "ID bản tin";
            // 
            // txt_speedPV
            // 
            this.txt_speedPV.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txt_speedPV.Location = new System.Drawing.Point(485, 73);
            this.txt_speedPV.Margin = new System.Windows.Forms.Padding(2);
            this.txt_speedPV.Name = "txt_speedPV";
            this.txt_speedPV.Size = new System.Drawing.Size(119, 23);
            this.txt_speedPV.TabIndex = 20;
            // 
            // txtString1
            // 
            this.txtString1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtString1.Location = new System.Drawing.Point(161, 169);
            this.txtString1.Margin = new System.Windows.Forms.Padding(2);
            this.txtString1.Name = "txtString1";
            this.txtString1.Size = new System.Drawing.Size(172, 23);
            this.txtString1.TabIndex = 19;
            this.txtString1.Text = "30.98 kg";
            // 
            // cbbIDMSG
            // 
            this.cbbIDMSG.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbbIDMSG.FormattingEnabled = true;
            this.cbbIDMSG.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5"});
            this.cbbIDMSG.Location = new System.Drawing.Point(161, 105);
            this.cbbIDMSG.Margin = new System.Windows.Forms.Padding(2);
            this.cbbIDMSG.Name = "cbbIDMSG";
            this.cbbIDMSG.Size = new System.Drawing.Size(92, 24);
            this.cbbIDMSG.TabIndex = 18;
            // 
            // btn_sendSTRING1
            // 
            this.btn_sendSTRING1.FlatAppearance.BorderColor = System.Drawing.Color.Aqua;
            this.btn_sendSTRING1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_sendSTRING1.Location = new System.Drawing.Point(64, 162);
            this.btn_sendSTRING1.Margin = new System.Windows.Forms.Padding(2);
            this.btn_sendSTRING1.Name = "btn_sendSTRING1";
            this.btn_sendSTRING1.Size = new System.Drawing.Size(92, 28);
            this.btn_sendSTRING1.TabIndex = 17;
            this.btn_sendSTRING1.Text = "Send string";
            this.btn_sendSTRING1.UseVisualStyleBackColor = true;
            this.btn_sendSTRING1.Click += new System.EventHandler(this.btn_sendSTRING1_Click);
            // 
            // btn_stopprint
            // 
            this.btn_stopprint.FlatAppearance.BorderColor = System.Drawing.Color.Aqua;
            this.btn_stopprint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_stopprint.Location = new System.Drawing.Point(54, 105);
            this.btn_stopprint.Margin = new System.Windows.Forms.Padding(2);
            this.btn_stopprint.Name = "btn_stopprint";
            this.btn_stopprint.Size = new System.Drawing.Size(92, 28);
            this.btn_stopprint.TabIndex = 16;
            this.btn_stopprint.Text = "Stop Print";
            this.btn_stopprint.UseVisualStyleBackColor = true;
            this.btn_stopprint.Click += new System.EventHandler(this.btn_stopprint_Click);
            // 
            // btn_startprint
            // 
            this.btn_startprint.FlatAppearance.BorderColor = System.Drawing.Color.Aqua;
            this.btn_startprint.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_startprint.Location = new System.Drawing.Point(54, 71);
            this.btn_startprint.Margin = new System.Windows.Forms.Padding(2);
            this.btn_startprint.Name = "btn_startprint";
            this.btn_startprint.Size = new System.Drawing.Size(92, 28);
            this.btn_startprint.TabIndex = 15;
            this.btn_startprint.Text = "Start Print";
            this.btn_startprint.UseVisualStyleBackColor = true;
            this.btn_startprint.Click += new System.EventHandler(this.btn_startprint_Click);
            // 
            // Label_counter
            // 
            this.Label_counter.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Label_counter.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label_counter.Location = new System.Drawing.Point(590, 22);
            this.Label_counter.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.Label_counter.Name = "Label_counter";
            this.Label_counter.Size = new System.Drawing.Size(162, 32);
            this.Label_counter.TabIndex = 27;
            this.Label_counter.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labStatus
            // 
            this.labStatus.Cursor = System.Windows.Forms.Cursors.Default;
            this.labStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labStatus.Location = new System.Drawing.Point(1, 282);
            this.labStatus.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labStatus.Name = "labStatus";
            this.labStatus.Size = new System.Drawing.Size(798, 100);
            this.labStatus.TabIndex = 28;
            this.labStatus.Text = "Status:";
            this.labStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtString2
            // 
            this.txtString2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtString2.Location = new System.Drawing.Point(162, 214);
            this.txtString2.Margin = new System.Windows.Forms.Padding(2);
            this.txtString2.Name = "txtString2";
            this.txtString2.Size = new System.Drawing.Size(172, 23);
            this.txtString2.TabIndex = 19;
            this.txtString2.Text = "2023-01-28 10:30:00";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(160, 198);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(43, 13);
            this.label4.TabIndex = 22;
            this.label4.Text = "String 2";
            // 
            // txtDelayPV
            // 
            this.txtDelayPV.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDelayPV.Location = new System.Drawing.Point(485, 184);
            this.txtDelayPV.Margin = new System.Windows.Forms.Padding(2);
            this.txtDelayPV.Name = "txtDelayPV";
            this.txtDelayPV.Size = new System.Drawing.Size(119, 23);
            this.txtDelayPV.TabIndex = 20;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(483, 169);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(37, 13);
            this.label5.TabIndex = 23;
            this.label5.Text = "Delay:";
            // 
            // btnGetDelay
            // 
            this.btnGetDelay.FlatAppearance.BorderColor = System.Drawing.Color.Aqua;
            this.btnGetDelay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGetDelay.Location = new System.Drawing.Point(403, 177);
            this.btnGetDelay.Margin = new System.Windows.Forms.Padding(2);
            this.btnGetDelay.Name = "btnGetDelay";
            this.btnGetDelay.Size = new System.Drawing.Size(78, 28);
            this.btnGetDelay.TabIndex = 24;
            this.btnGetDelay.Text = "Get delay";
            this.btnGetDelay.UseVisualStyleBackColor = true;
            this.btnGetDelay.Click += new System.EventHandler(this.btnGetDelay_Click);
            // 
            // txtDelaySV
            // 
            this.txtDelaySV.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDelaySV.Location = new System.Drawing.Point(485, 231);
            this.txtDelaySV.Margin = new System.Windows.Forms.Padding(2);
            this.txtDelaySV.Name = "txtDelaySV";
            this.txtDelaySV.Size = new System.Drawing.Size(119, 23);
            this.txtDelaySV.TabIndex = 25;
            // 
            // btnSetDelay
            // 
            this.btnSetDelay.FlatAppearance.BorderColor = System.Drawing.Color.Aqua;
            this.btnSetDelay.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSetDelay.Location = new System.Drawing.Point(403, 223);
            this.btnSetDelay.Margin = new System.Windows.Forms.Padding(2);
            this.btnSetDelay.Name = "btnSetDelay";
            this.btnSetDelay.Size = new System.Drawing.Size(78, 28);
            this.btnSetDelay.TabIndex = 26;
            this.btnSetDelay.Text = "Set delay";
            this.btnSetDelay.UseVisualStyleBackColor = true;
            this.btnSetDelay.Click += new System.EventHandler(this.btnSetDelay_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 418);
            this.Controls.Add(this.labStatus);
            this.Controls.Add(this.Label_counter);
            this.Controls.Add(this.btnSetDelay);
            this.Controls.Add(this.btn_Setspeed);
            this.Controls.Add(this.txtDelaySV);
            this.Controls.Add(this.txt_SpeedSV);
            this.Controls.Add(this.btnGetDelay);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.btn_GetSpeed);
            this.Controls.Add(this.Label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.txtDelayPV);
            this.Controls.Add(this.Label1);
            this.Controls.Add(this.txt_speedPV);
            this.Controls.Add(this.txtString2);
            this.Controls.Add(this.txtString1);
            this.Controls.Add(this.cbbIDMSG);
            this.Controls.Add(this.btn_sendSTRING1);
            this.Controls.Add(this.btn_stopprint);
            this.Controls.Add(this.btn_startprint);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.Button btn_Setspeed;
        internal System.Windows.Forms.TextBox txt_SpeedSV;
        internal System.Windows.Forms.Button btn_GetSpeed;
        internal System.Windows.Forms.Label Label3;
        internal System.Windows.Forms.Label Label2;
        internal System.Windows.Forms.Label Label1;
        internal System.Windows.Forms.TextBox txt_speedPV;
        internal System.Windows.Forms.TextBox txtString1;
        internal System.Windows.Forms.ComboBox cbbIDMSG;
        internal System.Windows.Forms.Button btn_sendSTRING1;
        internal System.Windows.Forms.Button btn_stopprint;
        internal System.Windows.Forms.Button btn_startprint;
        internal System.Windows.Forms.Label Label_counter;
        internal System.Windows.Forms.Label labStatus;
        internal System.Windows.Forms.TextBox txtString2;
        internal System.Windows.Forms.Label label4;
        internal System.Windows.Forms.TextBox txtDelayPV;
        internal System.Windows.Forms.Label label5;
        internal System.Windows.Forms.Button btnGetDelay;
        internal System.Windows.Forms.TextBox txtDelaySV;
        internal System.Windows.Forms.Button btnSetDelay;
    }
}

