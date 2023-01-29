Public Class Form1


    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        OpenCOMPORT()

    End Sub
    Private Sub OpenCOMPORT()
        Try
            'Chuẩn giao tiếp là rs485
            SerialPort1.PortName = "COM4"
            SerialPort1.BaudRate = 57600
            SerialPort1.DataBits = 8
            SerialPort1.StopBits = 1
            SerialPort1.Parity = IO.Ports.Parity.None
            If SerialPort1.IsOpen = False Then
                SerialPort1.Open()
                Label_status.Text = "Port open: " & SerialPort1.IsOpen
            End If
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Information, "OpenCOMport")
        End Try


    End Sub

    Private Sub SerialPort1_DataReceived(sender As Object, e As IO.Ports.SerialDataReceivedEventArgs) Handles SerialPort1.DataReceived
        Threading.Thread.Sleep(100)

        Dim dataRCV As String = SerialPort1.ReadExisting 'Read
        If dataRCV = "" Then
            Exit Sub
        End If

        Dim RCVarr As Byte() = System.Text.Encoding.ASCII.GetBytes(dataRCV)

        'Check command error
        If RCVarr(4) = &H31 Then
            MsgBox("Send command error: Error code " & RCVarr(5), MsgBoxStyle.Information, "Data from Anser U2") ' Show mã lỗi
        End If

        'Print Completed Report 30h (Setup in printer 180341= En/ 180340=Dis)
        Dim NumCounter As Integer = 0
        If RCVarr(4) = &H30 Then 'Event print completed
            NumCounter = RCVarr(5) + RCVarr(6) * &H100 + RCVarr(7) * &H1000 + RCVarr(8) * &H10000
        End If

        'Get speed
        Dim Numspeed As Double = 0
        If RCVarr(4) = &H5D Then 'Get speed
            Numspeed = RCVarr(5) + RCVarr(6) * &H100 + RCVarr(7) * &H1000 + RCVarr(8) * &H10000
            Numspeed = Numspeed / 1000
        End If

        Me.Invoke(Sub()
                      Label_status.Text = ""
                      For b = 0 To dataRCV.Length - 1
                          Label_status.Text = Label_status.Text & " " & Conversion.Hex(RCVarr(b)).ToString ' Hiển thị giá trị phản hồi từ máy in
                      Next

                      Label_counter.Text = NumCounter.ToString
                      txt_speedPV.Text = Numspeed.ToString
                  End Sub)

    End Sub
    Private Sub Form1_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        Try
            If SerialPort1.IsOpen Then
                SerialPort1.Close()
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub btn_startprint_Click(sender As Object, e As EventArgs) Handles btn_startprint.Click
        Try
            Dim SetPtinting() As Byte = {&H2, &H0, &H6, &H0, &H46, &H0, &H0, &H0, &H0, &H0, &H3}
            'Gán số thứ tự của bản tin cần in vào array
            SetPtinting(5) = CByte(cbbIDMSG.SelectedItem.ToString)
            'Tính checksum
            Dim chkSUM As Byte
            For i = 1 To SetPtinting.Length - 3
                chkSUM = chkSUM + SetPtinting(i)
            Next
            'Gán giá trị checksum vào array
            SetPtinting(9) = chkSUM
            'Gửi array xuống máy in
            SerialPort1.Write(SetPtinting, 0, SetPtinting.Length)
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Information, "Start print")
        End Try  

    End Sub

    Private Sub btn_stopprint_Click(sender As Object, e As EventArgs) Handles btn_stopprint.Click
        Try
            Dim SetPtinting() As Byte = {&H2, &H0, &H6, &H0, &H46, &H0, &H0, &H0, &H0, &H4C, &H3}

            SerialPort1.Write(SetPtinting, 0, SetPtinting.Length)
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Information, "Stop print")
        End Try
    End Sub

    Private Sub btn_sendSTRING_Click(sender As Object, e As EventArgs) Handles btn_sendSTRING1.Click
        Try
            Dim string1 As String = txt_STRING1.Text
            Dim i As Integer = 0
            Dim chkSUM As Integer

            Dim SetDynamicString(14 + string1.Length) As Byte
            SetDynamicString(0) = &H2
            SetDynamicString(1) = &H0
            SetDynamicString(2) = &H9 + string1.Length
            SetDynamicString(3) = &H0
            SetDynamicString(4) = &HCA 'Mã lệnh Set dynamic string
            SetDynamicString(5) = 0
            SetDynamicString(6) = 0
            SetDynamicString(7) = string1.Length 'Chiều dài của string 1
            SetDynamicString(8) = 0 'Chiều dài của string 2
            SetDynamicString(9) = 0 'Chiều dài của string 3
            SetDynamicString(10) = 0 'Chiều dài của string 4
            SetDynamicString(11) = 0 'Chiều dài của string 5

            For i = 0 To string1.Length - 1
                SetDynamicString(12 + i) = Asc(string1.Substring(i, 1)) ' Nội dung của string 1
            Next

            'Tính check SUM
            For c = 1 To i + 12
                chkSUM = chkSUM + SetDynamicString(c)
            Next
            chkSUM = chkSUM And &HFF
            SetDynamicString(i + 12) = CByte(chkSUM) ' Gán byte checksum vào arr
            SetDynamicString(i + 12 + 1) = &H3

            SerialPort1.Write(SetDynamicString, 0, SetDynamicString.Length)
        Catch ex As Exception
            MsgBox(ex.Message, MsgBoxStyle.Information, "Set Dynamic String")
        End Try
       
    End Sub

    Private Sub btn_GetSpeed_Click(sender As Object, e As EventArgs) Handles btn_GetSpeed.Click
        Dim GetSpeed As Byte() = {&H2, &H0, &H2, &H0, &H5D, &H5F, &H3}
        SerialPort1.Write(GetSpeed, 0, GetSpeed.Length)
    End Sub

    Private Sub btn_Setspeed_Click(sender As Object, e As EventArgs) Handles btn_Setspeed.Click
        Dim SetSpeed As Byte() = {&H2, &H0, &H6, &H0, &H5E, &H0, &H0, &H0, &H0, &H0, &H3}

        Dim SpeedSV As Integer = CSng(txt_SpeedSV.Text) * 1000
        Dim speedSVarr As Byte() = BitConverter.GetBytes(SpeedSV)

        'Gán giá trị tốc độ vào arr
        For i = 0 To 3
            SetSpeed(5 + i) = speedSVarr(i)
        Next

        'Tính checksum
        Dim chksum As Integer = 0
        For i = 1 To SetSpeed.Length - 2
            chksum = chksum + SetSpeed(i)
        Next
        chksum = chksum And &HFF
        'Gán checksum vào arr
        SetSpeed(9) = CByte(chksum)
        'Gửi xuống máy in
        SerialPort1.Write(SetSpeed, 0, SetSpeed.Length)
    End Sub
End Class
