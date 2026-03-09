using CognexLibrary_NETFramework;
using Snap7ClientLib.Core;
using Snap7ClientLib.Historian;
using Snap7ClientLib.Tags;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private CognexLibrary_NETFramework.DriverTelnet driverTelnet = new CognexLibrary_NETFramework.DriverTelnet();

        PlcManager _manager = new PlcManager();
        PlcRuntime _plcRuntime;
        PlcClient _plc1Client;
        PlcSubscriptionManager _sub;
        SqliteHistorian _historian;

        private CancellationTokenSource _readModbusCts;
        private Task _readModbusTask;

        string _scaleValue = string.Empty;
        string _isCheck = string.Empty;
        string _qrMetal = string.Empty;

        public Form1()
        {
            InitializeComponent();

            //driverTelnet.HostName = "192.168.80.4";
            //driverTelnet.Port = 23;
            //driverTelnet.DataEvent.EventHandleValueChange += DataEvent_EventHandleValueChange;
            //driverTelnet.ConnectDevices();

            this.FormClosing += Form1_FormClosing;
            Load += Form1_Load;
        }

        

        private void DataEvent_EventHandleValueChange(object sender, ValueChangeEventArgs e)
        {
            Debug.WriteLine(e.NewValue);

            if (_labQR1.InvokeRequired)
            {
                _labQR1.Invoke(new Action(() =>
                {
                    _labQR1.Text = e.NewValue;
                }));
            }
            else _labQR1.Text = e.NewValue;
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // 1.Dừng vòng lặp Modbus
            _readModbusCts?.Cancel();

            // 2. Dừng Polling của Snap7
            _sub?.Stop();

            // 3. Hủy kết nối PLC (Dừng Watchdog và ngắt TCP)
            _plc1Client.Dispose(); // Sẽ dừng watchdog và ngắt kết nối

        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            _manager.LoadFromConfig("tags.json");

            //tùy vào hệ thống kết nối bao nhiêu PLC để gọi kết nối đến PLC tương ứng
            _plcRuntime = _manager.GetPlc("PLC_1");
            _plc1Client = _plcRuntime.Client; // Lưu vào biến toàn cục

            // ĐĂNG KÝ SỰ KIỆN TRƯỚC KHI KẾT NỐI
            _plc1Client.StateChanged += Client_StateChanged;

            await _plcRuntime.Reader.ReadGroupAsync(_plcRuntime.Tags);


            // 1) Tạo và gán handler
            _sub = new PlcSubscriptionManager(_plcRuntime.Reader);
            _sub.OnValueChanged += Sub_OnValueChanged;

            // Ví dụ đăng ký cho từng tag cụ thể trong Form_Load
            var tagScale_Stable_Trigger = _plcRuntime.Tags.FirstOrDefault(t => t.Name == "Scale_Stable_Trigger");
            if (tagScale_Stable_Trigger != null)
            {
                tagScale_Stable_Trigger.ValueChanged += (tag) =>
                {
                    Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");
                };
            }

            var tagSccale_Value = _plcRuntime.Tags.FirstOrDefault(t => t.Name == "Sccale_Value");
            if (tagSccale_Value != null)
            {
                tagSccale_Value.ValueChanged += (tag) =>
                {
                    Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");
                };
            }


            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "Sccale_Value").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");
            };

            // 2) Kết nối PLC, chạy Polling
            await _plc1Client.ConnectAsync();
            _plc1Client.StartWatchdog(2000);

            // 3) (Tuỳ chọn) cập nhật UI lần đầu
            foreach (var tag in _plcRuntime.Tags)
                tag.RaiseValueChanged();//Tự "bắn" sự kiện để UI cập nhật ngay giá trị ban đầu

            // 4) BẮT ĐẦU POLLING (rất quan trọng)
            _sub.Subscribe(_plcRuntime.Tags, intervalMs: 200);


            //run thread đọc modbus, để đọc các giá trị cân
            _readModbusCts = new CancellationTokenSource();
            _readModbusTask = Task.Run(() => TaskReadPlcAsync(_readModbusCts.Token));

            _cbTagName.Items.AddRange(_plcRuntime.Tags.Select(x => x.Name).ToArray());

            button1.Click += Button1_Click;
        }

        public async Task TaskReadPlcAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (InvokeRequired)
                    {
                        BeginInvoke((Action)(() =>
                        {
                            //listBox1.Items.Clear();
                            //foreach (var t in _manager.GetPlc("PLC_1").Tags)
                            //    listBox1.Items.Add($"{t.Name} = {t.NewValue}");

                            label1.Text = $"Scale_Stable_Trigger: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "Scale_Stable_Trigger").NewValue.ToString()}";
                            label2.Text = $"Sccale_Value: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "Sccale_Value").NewValue.ToString()}";
                            label3.Text = $"Sccale_Value_Stable: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "Sccale_Value_Stable").NewValue.ToString()}";
                        }));
                    }
                    else
                    {
                        //listBox1.Items.Clear();
                        //foreach (var t in _manager.GetPlc("PLC_1").Tags)
                        //    listBox1.Items.Add($"{t.Name} = {t.NewValue}");

                        label1.Text = $"Scale_Stable_Trigger: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "Scale_Stable_Trigger").NewValue.ToString()}";
                        label2.Text = $"Sccale_Value: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "Sccale_Value").NewValue.ToString()}";
                        label3.Text = $"Sccale_Value_Stable: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "Sccale_Value_Stable").NewValue.ToString()}";
                    }

                    await Task.Delay(1000, token); // nhịp kiểm tra, đủ nhẹ nhàng
                }
                catch (OperationCanceledException)
                {
                    // token.Cancel() => thoát vòng lặp
                    break;
                }
                catch (Exception ex)
                {
                    // Không để task chết âm thầm
                    await Task.Delay(500, token); // tạm nghỉ rồi thử lại
                }
            }
        }

        private void Sub_OnValueChanged(PlcTag tag)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateListBoxItem(tag)));
            }
            else
            {
                UpdateListBoxItem(tag);
            }
        }

        private void UpdateListBoxItem(PlcTag tag)
        {
            string itemText = $"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType})";

            int foundIndex = -1;

            // 1. Tìm xem Tag này đã tồn tại trong ListBox chưa
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                if (listBox1.Items[i].ToString().Contains(tag.Name))
                {
                    foundIndex = i;
                    break;
                }
            }

            // 2. Nếu đã có thì cập nhật giá trị mới, nếu chưa thì thêm vào cuối
            if (foundIndex != -1)
            {
                // Chỉ cập nhật nếu giá trị hiển thị thực sự khác đi để tránh vẽ lại vô ích
                if (listBox1.Items[foundIndex].ToString() != itemText)
                {
                    listBox1.Items[foundIndex] = itemText;
                }
            }
            else
            {
                listBox1.Items.Add(itemText);
            }
        }

        private void Client_StateChanged(PlcConnectionState obj)
        {

            if (InvokeRequired)
            {
                BeginInvoke((Action)(() =>
                {
                    lblStatus.Text = obj.ToString();
                }));
            }
            else
            {
                lblStatus.Text = obj.ToString();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            // Giả sử bạn đã có đối tượng writer được khởi tạo từ PlcClient
            var writer = _plcRuntime.Writer;

            //// 1. Tạo danh sách các tag muốn ghi
            //var tagsToWrite = new List<PlcTag>();

            // Ghi số thực (Real) - Ví dụ thiết lập ngưỡng cân
            //var tagWeightSet = _plcRuntime.Tags.First(t => t.Name == "ScaleValue");
            //tagWeightSet.NewValue = 50.5f; // Gán giá trị mới
            //tagsToWrite.Add(tagWeightSet);

            //// Ghi giá trị Logic (Bool) - Ví dụ kích hoạt lệnh Check
            //var tagCheck = _plcRuntime.Tags.First(t => t.Name == "IsCheck");
            //tagCheck.NewValue = true;
            //tagsToWrite.Add(tagCheck);

            //// Ghi chuỗi ký tự (String) - Ví dụ mã QR
            //var tagQr = _plcRuntime.Tags.First(t => t.Name == "BoxIdScale");
            //tagQr.NewValue = "S7-1200-OK-2026";
            //tagsToWrite.Add(tagQr);

            //// 2. Thực thi ghi xuống PLC bất đồng bộ (không treo UI)
            //await writer.WriteGroupAsync(tagsToWrite);

            var tag = _plcRuntime.Tags.FirstOrDefault(t => t.Name == _cbTagName.Text);

            if (tag == null) return;

            // 1. Chuyển đổi dữ liệu an toàn
            object newValue = tag.DataType switch
            {
                PlcDataType.String => _txtNewValue.Text,
                PlcDataType.Bool => _txtNewValue.Text.ToLower() == "true" || _txtNewValue.Text == "1",
                PlcDataType.Real  => double.TryParse(_txtNewValue.Text, out var d) ? d : 0.0,
                PlcDataType.LReal => double.TryParse(_txtNewValue.Text, out var d) ? d : 0.0,
                _ => int.TryParse(_txtNewValue.Text, out var i) ? i : 0
            };

            // 2. CHỈ GHI TAG ĐANG CHỌN (Tối ưu cực quan trọng)
            tag.NewValue = newValue;
            var tagsToWrite = new List<PlcTag> { tag };

            try
            {
                // Ghi bất đồng bộ để tránh đơ UI và không làm gián đoạn luồng đọc
                await Task.Run(() => writer.WriteGroup(tagsToWrite));

                // Cập nhật ngay giá trị LastWritten trong Dictionary của Writer (nếu có dùng WriteGroupOnlyChanged ở chỗ khác)
                // writer.UpdateCache(tag); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Write PLC error: {ex.Message}");
            }

            //// Sử dụng hàm này nếu bạn gọi lệnh ghi trong một vòng lặp liên tục, ghi tất cả các tag.s
            //writer.WriteGroupOnlyChanged(_plcRuntime.Tags);
        }
    }
}
