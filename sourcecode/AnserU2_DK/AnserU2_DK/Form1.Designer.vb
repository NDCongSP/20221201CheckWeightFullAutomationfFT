<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.Panel_status = New System.Windows.Forms.Panel()
        Me.Label_status = New System.Windows.Forms.Label()
        Me.Panel_Fill = New System.Windows.Forms.Panel()
        Me.Label_counter = New System.Windows.Forms.Label()
        Me.btn_GetSpeed = New System.Windows.Forms.Button()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.txt_speedPV = New System.Windows.Forms.TextBox()
        Me.txt_STRING1 = New System.Windows.Forms.TextBox()
        Me.cbbIDMSG = New System.Windows.Forms.ComboBox()
        Me.btn_sendSTRING1 = New System.Windows.Forms.Button()
        Me.btn_stopprint = New System.Windows.Forms.Button()
        Me.btn_startprint = New System.Windows.Forms.Button()
        Me.SerialPort1 = New System.IO.Ports.SerialPort(Me.components)
        Me.btn_Setspeed = New System.Windows.Forms.Button()
        Me.txt_SpeedSV = New System.Windows.Forms.TextBox()
        Me.Panel_status.SuspendLayout()
        Me.Panel_Fill.SuspendLayout()
        Me.SuspendLayout()
        '
        'Panel_status
        '
        Me.Panel_status.BackColor = System.Drawing.Color.FromArgb(CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(224, Byte), Integer))
        Me.Panel_status.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Panel_status.Controls.Add(Me.Label_status)
        Me.Panel_status.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.Panel_status.Location = New System.Drawing.Point(0, 362)
        Me.Panel_status.Name = "Panel_status"
        Me.Panel_status.Size = New System.Drawing.Size(957, 41)
        Me.Panel_status.TabIndex = 0
        '
        'Label_status
        '
        Me.Label_status.Cursor = System.Windows.Forms.Cursors.Default
        Me.Label_status.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label_status.Location = New System.Drawing.Point(0, 0)
        Me.Label_status.Name = "Label_status"
        Me.Label_status.Size = New System.Drawing.Size(1064, 37)
        Me.Label_status.TabIndex = 1
        Me.Label_status.Text = "Status:"
        Me.Label_status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'Panel_Fill
        '
        Me.Panel_Fill.BackColor = System.Drawing.SystemColors.ActiveCaption
        Me.Panel_Fill.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Panel_Fill.Controls.Add(Me.btn_Setspeed)
        Me.Panel_Fill.Controls.Add(Me.txt_SpeedSV)
        Me.Panel_Fill.Controls.Add(Me.Label_counter)
        Me.Panel_Fill.Controls.Add(Me.btn_GetSpeed)
        Me.Panel_Fill.Controls.Add(Me.Label3)
        Me.Panel_Fill.Controls.Add(Me.Label2)
        Me.Panel_Fill.Controls.Add(Me.Label1)
        Me.Panel_Fill.Controls.Add(Me.txt_speedPV)
        Me.Panel_Fill.Controls.Add(Me.txt_STRING1)
        Me.Panel_Fill.Controls.Add(Me.cbbIDMSG)
        Me.Panel_Fill.Controls.Add(Me.btn_sendSTRING1)
        Me.Panel_Fill.Controls.Add(Me.btn_stopprint)
        Me.Panel_Fill.Controls.Add(Me.btn_startprint)
        Me.Panel_Fill.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Panel_Fill.Location = New System.Drawing.Point(0, 0)
        Me.Panel_Fill.Name = "Panel_Fill"
        Me.Panel_Fill.Size = New System.Drawing.Size(957, 362)
        Me.Panel_Fill.TabIndex = 1
        '
        'Label_counter
        '
        Me.Label_counter.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Label_counter.Font = New System.Drawing.Font("Microsoft Sans Serif", 20.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label_counter.Location = New System.Drawing.Point(727, 7)
        Me.Label_counter.Name = "Label_counter"
        Me.Label_counter.Size = New System.Drawing.Size(216, 40)
        Me.Label_counter.TabIndex = 12
        Me.Label_counter.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'btn_GetSpeed
        '
        Me.btn_GetSpeed.FlatAppearance.BorderColor = System.Drawing.Color.Aqua
        Me.btn_GetSpeed.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btn_GetSpeed.Location = New System.Drawing.Point(602, 144)
        Me.btn_GetSpeed.Name = "btn_GetSpeed"
        Me.btn_GetSpeed.Size = New System.Drawing.Size(104, 35)
        Me.btn_GetSpeed.TabIndex = 11
        Me.btn_GetSpeed.Text = "Get speed"
        Me.btn_GetSpeed.UseVisualStyleBackColor = True
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(709, 134)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(53, 17)
        Me.Label3.TabIndex = 10
        Me.Label3.Text = "Speed:"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(161, 231)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(57, 17)
        Me.Label2.TabIndex = 9
        Me.Label2.Text = "String 1"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(221, 107)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(68, 17)
        Me.Label1.TabIndex = 8
        Me.Label1.Text = "ID bản tin"
        '
        'txt_speedPV
        '
        Me.txt_speedPV.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txt_speedPV.Location = New System.Drawing.Point(712, 153)
        Me.txt_speedPV.Name = "txt_speedPV"
        Me.txt_speedPV.Size = New System.Drawing.Size(157, 26)
        Me.txt_speedPV.TabIndex = 7
        '
        'txt_STRING1
        '
        Me.txt_STRING1.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txt_STRING1.Location = New System.Drawing.Point(164, 251)
        Me.txt_STRING1.Name = "txt_STRING1"
        Me.txt_STRING1.Size = New System.Drawing.Size(228, 26)
        Me.txt_STRING1.TabIndex = 6
        '
        'cbbIDMSG
        '
        Me.cbbIDMSG.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.cbbIDMSG.FormattingEnabled = True
        Me.cbbIDMSG.Items.AddRange(New Object() {"1", "2", "3", "4", "5"})
        Me.cbbIDMSG.Location = New System.Drawing.Point(204, 130)
        Me.cbbIDMSG.Name = "cbbIDMSG"
        Me.cbbIDMSG.Size = New System.Drawing.Size(121, 28)
        Me.cbbIDMSG.TabIndex = 5
        '
        'btn_sendSTRING1
        '
        Me.btn_sendSTRING1.FlatAppearance.BorderColor = System.Drawing.Color.Aqua
        Me.btn_sendSTRING1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btn_sendSTRING1.Location = New System.Drawing.Point(35, 242)
        Me.btn_sendSTRING1.Name = "btn_sendSTRING1"
        Me.btn_sendSTRING1.Size = New System.Drawing.Size(123, 35)
        Me.btn_sendSTRING1.TabIndex = 3
        Me.btn_sendSTRING1.Text = "Send string 1"
        Me.btn_sendSTRING1.UseVisualStyleBackColor = True
        '
        'btn_stopprint
        '
        Me.btn_stopprint.FlatAppearance.BorderColor = System.Drawing.Color.Aqua
        Me.btn_stopprint.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btn_stopprint.Location = New System.Drawing.Point(61, 130)
        Me.btn_stopprint.Name = "btn_stopprint"
        Me.btn_stopprint.Size = New System.Drawing.Size(123, 35)
        Me.btn_stopprint.TabIndex = 1
        Me.btn_stopprint.Text = "Stop Print"
        Me.btn_stopprint.UseVisualStyleBackColor = True
        '
        'btn_startprint
        '
        Me.btn_startprint.FlatAppearance.BorderColor = System.Drawing.Color.Aqua
        Me.btn_startprint.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btn_startprint.Location = New System.Drawing.Point(61, 89)
        Me.btn_startprint.Name = "btn_startprint"
        Me.btn_startprint.Size = New System.Drawing.Size(123, 35)
        Me.btn_startprint.TabIndex = 0
        Me.btn_startprint.Text = "Start Print"
        Me.btn_startprint.UseVisualStyleBackColor = True
        '
        'SerialPort1
        '
        '
        'btn_Setspeed
        '
        Me.btn_Setspeed.FlatAppearance.BorderColor = System.Drawing.Color.Aqua
        Me.btn_Setspeed.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.btn_Setspeed.Location = New System.Drawing.Point(602, 201)
        Me.btn_Setspeed.Name = "btn_Setspeed"
        Me.btn_Setspeed.Size = New System.Drawing.Size(104, 35)
        Me.btn_Setspeed.TabIndex = 14
        Me.btn_Setspeed.Text = "Set speed"
        Me.btn_Setspeed.UseVisualStyleBackColor = True
        '
        'txt_SpeedSV
        '
        Me.txt_SpeedSV.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.txt_SpeedSV.Location = New System.Drawing.Point(712, 210)
        Me.txt_SpeedSV.Name = "txt_SpeedSV"
        Me.txt_SpeedSV.Size = New System.Drawing.Size(157, 26)
        Me.txt_SpeedSV.TabIndex = 13
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.ActiveCaption
        Me.ClientSize = New System.Drawing.Size(957, 403)
        Me.Controls.Add(Me.Panel_Fill)
        Me.Controls.Add(Me.Panel_status)
        Me.Name = "Form1"
        Me.Text = "Anser U2 test protocol"
        Me.Panel_status.ResumeLayout(False)
        Me.Panel_Fill.ResumeLayout(False)
        Me.Panel_Fill.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Panel_status As System.Windows.Forms.Panel
    Friend WithEvents Label_status As System.Windows.Forms.Label
    Friend WithEvents Panel_Fill As System.Windows.Forms.Panel
    Friend WithEvents txt_speedPV As System.Windows.Forms.TextBox
    Friend WithEvents txt_STRING1 As System.Windows.Forms.TextBox
    Friend WithEvents cbbIDMSG As System.Windows.Forms.ComboBox
    Friend WithEvents btn_sendSTRING1 As System.Windows.Forms.Button
    Friend WithEvents btn_stopprint As System.Windows.Forms.Button
    Friend WithEvents btn_startprint As System.Windows.Forms.Button
    Friend WithEvents SerialPort1 As System.IO.Ports.SerialPort
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents btn_GetSpeed As System.Windows.Forms.Button
    Friend WithEvents Label_counter As System.Windows.Forms.Label
    Friend WithEvents btn_Setspeed As System.Windows.Forms.Button
    Friend WithEvents txt_SpeedSV As System.Windows.Forms.TextBox

End Class
