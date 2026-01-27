using AutoUpdaterDotNET;
using CognexLibrary_NETFramework;
using CoreScanner;
using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using Newtonsoft.Json;
using Serilog;
using Snap7ClientLib.Core;
using Snap7ClientLib.Historian;
using Snap7ClientLib.Tags;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using WeightChecking.StaticClass;

namespace WeightChecking
{
    public partial class frmScaleNewUI : DevExpress.XtraEditors.XtraForm
    {

        // Import để cho phép kéo form
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private Panel titleBar;
        private Button btnClose;
        private Button btnMaximize;
        private Button btnMinimize;
        private Button btnUpdateVersion;

        private int _metalScannerStatus = 0;

        private int _stableScale = 0;//biến báo trạng thái cân ổn định, get khối lượng cân về
        private double _scaleValue = 0;//biến chứa giá trị cân realTime đọc từ đầu cân về
        private double _scaleValueStable = 0;//biến chứa giá trị cân ổn định được đọc về khi biến stable báo on
        private int _metalCheckResult = 0;//biến chứa giá trị metalCheck 

        //tạo các biến để lưu giá trị theo QR code tại từng trạm
        //private tblScanDataModel _scanData = new tblScanDataModel();
        private tblScanData _scanDataMetal = new tblScanData();
        private tblScanData _scanDataWeight = new tblScanData();

        private string _idLabel = null;
        private string _unit = null;// kiểu đóng thùng, P-đôi; L/R-left righ
        private double _weight = 0, _boxWeight = 0, _accessoriesWeight = 0;

        private bool _approveUpdateActMetalScan = false;

        private string _barcodeString1 = null, _barcodeString2 = null;

        private bool _firstLoad = true;

        private bool _approvePrint = false;// lệnh cho phép in hay không, chỉ khi nào pass cân thì active lên cho in.

        //20250510 upgrade system to use scanner cogned DM290-X at station check weight
        private static CognexLibrary_NETFramework.DriverTelnet _driverTelnet = new CognexLibrary_NETFramework.DriverTelnet();

        private bool isUpdateClicked = false;

        private CancellationTokenSource _timer;
        private Task _timerTask;

        private CancellationTokenSource _resetUiCts;
        private Task _resetUiTask;

        private CancellationTokenSource _processMetalCts;
        private Task _processMetalTask;

        private CancellationTokenSource _processScaleCts;
        private Task _processScaleTask;

        private EnumBoxType _boxType;

        private bool _resetUI = false;

        private string _unitLabel = string.Empty;
        private string _color = string.Empty;
        private string _sizeName = string.Empty;

        //khai báo kết nối PLC S7
        PlcManager _manager = new PlcManager();
        PlcRuntime _plcRuntime;
        PlcClient _plc1Client;
        PlcSubscriptionManager _sub;
        SqliteHistorian _historian;

        private CancellationTokenSource _readPlcCts;
        private Task _readPlcTask;

        private PlcConnectionState _plcConnectionState;

        public frmScaleNewUI()
        {
            InitializeComponent();

            #region add header
            // Cấu hình form
            this.Text = "Custom Title Bar";
            this.FormBorderStyle = FormBorderStyle.None; // Bỏ header mặc định
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1920, 1080);

            // Tạo panel làm thanh tiêu đề
            titleBar = new Panel();
            titleBar.Dock = DockStyle.Top;
            titleBar.Height = 40;
            titleBar.BackColor = Color.Black;
            titleBar.MouseDown += TitleBar_MouseDown;
            this.Controls.Add(titleBar);

            // Nút Close
            btnClose = new Button();
            btnClose.Text = "";
            btnClose.ForeColor = Color.White;
            btnClose.BackColor = Color.Black;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Size = new Size(40, 40);
            btnClose.Location = new Point(this.Width - 40, 0);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnClose.Image = Properties.Resources.close_white_30;  // PNG từ Resources
            btnClose.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnClose.Padding = new Padding(0);                    // tránh lệch
            btnClose.TextImageRelation = TextImageRelation.Overlay; // chỉ icon
            btnClose.Click += BtnClose_Click;
            titleBar.Controls.Add(btnClose);

            // Nút Maximize
            btnMaximize = new Button();
            btnMaximize.Text = "";
            btnMaximize.ForeColor = Color.White;
            btnMaximize.BackColor = Color.Black;
            btnMaximize.FlatStyle = FlatStyle.Flat;
            btnMaximize.FlatAppearance.BorderSize = 0;
            btnMaximize.Size = new Size(40, 40);
            btnMaximize.Location = new Point(this.Width - 80, 0);
            btnMaximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnMaximize.Image = Properties.Resources.maximize_white_30;  // PNG từ Resources
            btnMaximize.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnMaximize.Padding = new Padding(0);                    // tránh lệch
            btnMaximize.TextImageRelation = TextImageRelation.Overlay; // chỉ icon
            btnMaximize.Click += BtnMaximize_Click;
            titleBar.Controls.Add(btnMaximize);

            // Nút Minimize
            btnMinimize = new Button();
            btnMinimize.Text = "";
            btnMinimize.ForeColor = Color.White;
            btnMinimize.BackColor = Color.Black;
            btnMinimize.FlatStyle = FlatStyle.Flat;
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.Size = new Size(40, 40);
            btnMinimize.Location = new Point(this.Width - 120, 0);
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnMinimize.Image = Properties.Resources.minimize_white_30;  // PNG từ Resources
            btnMinimize.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnMinimize.Padding = new Padding(0);                    // tránh lệch
            btnMinimize.TextImageRelation = TextImageRelation.Overlay; // chỉ icon
            btnMinimize.Click += BtnMinimize_Click;
            titleBar.Controls.Add(btnMinimize);


            // Nút update version
            btnUpdateVersion = new Button();
            btnUpdateVersion.Text = "";                      // Không cần chữ, chỉ hiển thị icon
            btnUpdateVersion.ForeColor = Color.White;
            btnUpdateVersion.BackColor = Color.Black;
            btnUpdateVersion.FlatStyle = FlatStyle.Flat;
            btnUpdateVersion.FlatAppearance.BorderSize = 0;
            btnUpdateVersion.Size = new Size(40, 40);
            btnUpdateVersion.Location = new Point(this.Width - 160, 0);
            btnUpdateVersion.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnUpdateVersion.Cursor = Cursors.Hand;

            // 1) Gán icon từ Resources (đặt tên hình là "updateVersion" như trong Resource)
            btnUpdateVersion.Image = Properties.Resources.arrow_upward_white_30;  // PNG từ Resources
            btnUpdateVersion.ImageAlign = ContentAlignment.MiddleCenter;  // căn giữa
            btnUpdateVersion.Padding = new Padding(0);                    // tránh lệch
            btnUpdateVersion.TextImageRelation = TextImageRelation.Overlay; // chỉ icon

            // Tùy chọn: scale icon nếu quá lớn/nhỏ (WinForms Button không có ImageLayout)
            // => bạn có thể dùng phiên bản icon 24x24 hoặc 32x32 trong file PNG để vừa với nút 40x40.

            // 2) Tooltip khi hover
            var tip = new ToolTip();
            tip.AutoPopDelay = 5000;     // hiển thị tối đa 5 giây
            tip.InitialDelay = 300;      // trễ 300ms
            tip.ReshowDelay = 100;       // xuất hiện lại nhanh
            tip.ShowAlways = true;       // luôn hiển thị tooltip
            tip.SetToolTip(btnUpdateVersion, "Click to update version");  // nội dung tooltip

            // Tùy chọn: hiệu ứng hover (đổi nền cho dễ nhìn)
            btnUpdateVersion.MouseEnter += (s, e) => btnUpdateVersion.BackColor = Color.FromArgb(30, 30, 30);
            btnUpdateVersion.MouseLeave += (s, e) => btnUpdateVersion.BackColor = Color.Black;

            // Sự kiện Click (giữ nguyên như bạn đã có)
            btnUpdateVersion.Click += BtnUpdateVersion_Click; ; // hoặc sự kiện update version thực tế của bạn
            titleBar.Controls.Add(btnUpdateVersion);


            // Đảm bảo tất cả có cùng Height = 30 và Y = 5
            btnClose.Size = btnMaximize.Size = btnMinimize.Size = btnUpdateVersion.Size = new Size(30, 30);


            // Anchor cho cả 3 nút
            btnClose.Anchor = btnMaximize.Anchor = btnMinimize.Anchor = btnUpdateVersion.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // Logo
            PictureBox logo = new PictureBox();
            logo.Image = Properties.Resources.framas__white_; // logo từ Resources
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.Size = new Size(100, 30); // kích thước logo
            logo.Location = new Point(10, 5); // vị trí bên trái
            titleBar.Controls.Add(logo);

            // Text
            Label titleText = new Label();
            titleText.Text = $"fFT - SSFG Station";
            titleText.ForeColor = Color.White;
            titleText.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            titleText.AutoSize = true;
            titleText.Location = new Point(120, 10); // ngay sau logo
            titleBar.Controls.Add(titleText);
            #endregion

            Load += FrmScale_Load;
            FormClosing += FrmScale_FormClosing;
            _labResult.Focus();
        }


        // Cho phép kéo form bằng panel
        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        private void BtnUpdateVersion_Click(object sender, EventArgs e)
        {
            try
            {
                isUpdateClicked = true;
                string UUrl = GlobalVariables.ConfigJson.UpdatePath;
                SplashScreenManager.ShowForm(typeof(WaitForm1));
                System.Threading.Thread.Sleep(3000);
                AutoUpdater.Start(UUrl);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{ex.Message}", "Error");
            }
            finally
            {
                SplashScreenManager.CloseForm(false);
            }
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
                this.WindowState = FormWindowState.Maximized;
            else
                this.WindowState = FormWindowState.Normal;
        }

        private void BtnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }


        private async void FrmScale_Load(object sender, EventArgs e)
        {
            #region Connect to PL Seimens
            _manager.LoadFromConfig("tags.json");
            //_manager.LoadFromConfig("tags1.json");

            //tùy vào hệ thống kết nối bao nhiêu PLC để gọi kết nối đến PLC tương ứng
            _plcRuntime = _manager.GetPlc("PLC_1");
            _plc1Client = _plcRuntime.Client; // Lưu vào biến toàn cục

            // ĐĂNG KÝ SỰ KIỆN TRƯỚC KHI KẾT NỐI
            _plc1Client.StateChanged += Client_StateChanged;

            await _plcRuntime.Reader.ReadGroupAsync(_plcRuntime.Tags);

            _sub = new PlcSubscriptionManager((_plcRuntime.Reader));
            _sub.OnValueChanged += Sub_OnValueChanged;//sự kiện trả ra tất cả các tags khi có 1 tag bất kỳ thay đổi giá trị.

            //foreach (var t in _plcRuntime.Tags)
            //    listBox1.Items.Add($"{t.Name} = {t.Value}");

            // Ví dụ đăng ký cho từng tag cụ thể trong Form_Load
            var tagScaleValue = _plcRuntime.Tags.FirstOrDefault(t => t.Name == "ScaleValue");
            if (tagScaleValue != null)
            {
                tagScaleValue.ValueChanged += (tag) =>
                {
                    _resetUiCts = new CancellationTokenSource();
                    _resetUiTask = Task.Run(() => TaskCheckResetUIAsync(_resetUiCts.Token));
                };
            }

            var tagIsChecck = _plcRuntime.Tags.FirstOrDefault(t => t.Name == "IsCheck");
            if (tagIsChecck != null)
            {
                tagIsChecck.ValueChanged += (tag) =>
                {
                    //_isCheck = $"{tag.Value} (Trước đó: {tag.LastValue})";
                };
            }

            // Sau khi đăng ký xong hết mới bắt đầu chạy Polling
            await _plc1Client.ConnectAsync();
            _plc1Client.StartWatchdog(2000);

            // Chạy vòng lặp Subscription không chặn (Non-blocking)
            _ = Task.Run(() => _sub.SubscribeAsync(_plcRuntime.Tags, 200));

            // Duyệt qua danh sách tag của PLC để cập nhật UI lần đầu tiên
            foreach (var tag in _plcRuntime.Tags)
            {
                tag.RaiseValueChanged(); //Tự "bắn" sự kiện để UI cập nhật ngay giá trị ban đầu
            }

            //run thread đọc modbus, để đọc các giá trị cân
            _readPlcCts = new CancellationTokenSource();
            _readPlcTask = Task.Run(() => TaskReadPlcAsync(_readPlcCts.Token));
            #endregion

            this.ActiveControl = null;
            this.ActiveControl = _labResult;

            GlobalVariables.AppStatus = "READY";

            //tạo 1 task chạy độc lập.
            _timer = new CancellationTokenSource();
            _timerTask = Task.Run(() => TaskTimerAsync(_timer.Token));

            _resetUiCts = new CancellationTokenSource();
            _resetUiTask = Task.Run(() => TaskCheckResetUIAsync(_resetUiCts.Token));

            #region Fake data to debug
            //layoutControlGroup3.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;

            //BarcodeScanner1Handle(1, "A129059,6818442301-ON01-2651,10,6,P,6/8,1900070,1/1|2,683388.2024,0,0,18");
            //BarcodeScanner1Handle(1, "A123609,6817012201-2626-D228,95,3,P,3/5,1900022,1/1|2,436866.2024,1,0,99");

            //GlobalVariables.MyEvent.SensorBeforeMetalScan = 1;
            //GlobalVariables.MyEvent.SensorBeforeMetalScan = 0;
            //GlobalVariables.MyEvent.SensorAfterMetalScan = 1;
            //GlobalVariables.MyEvent.MetalCheckResult = 1;

            //using (var con=GlobalVariables.GetDbConnection())
            //{
            //    AutoPostingHelper.AutoTransfer("", "A123631,6817012201-3265-D228,100,3,P,6/11,1900022,1/4|2,435752.2024,1,0,99", 1185, 2,con);
            //}


            //_scaleValueStable = 8777;
            //GlobalVariables.MyEvent.StableScale = 1;
            //BarcodeScanner2Handle(2, "C111085,6812012209-4251-2502,12,1,P,1/3,1900021,1/1|2,1253747.2025,0,0,12,BX2");


            //GlobalVariables.MyEvent.SensorBeforeWeightScan = 1;


            #endregion

            ResetControl();
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
                            //    listBox1.Items.Add($"{t.Name} = {t.Value}");

                            //label1.Text = $"BoxIdScale: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "BoxIdScale").Value.ToString()}";
                            //label2.Text = $"BoxIdMetal: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "BoxIdMetal").Value.ToString()}";
                            //label3.Text = $"ScaleValue: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "ScaleValue").Value.ToString()}";
                        }));
                    }
                    else
                    {
                        //listBox1.Items.Clear();
                        //foreach (var t in _manager.GetPlc("PLC_1").Tags)
                        //    listBox1.Items.Add($"{t.Name} = {t.Value}");

                        //label1.Text = $"BoxIdScale: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "BoxIdScale").Value.ToString()}";
                        //label2.Text = $"BoxIdMetal: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "BoxIdMetal").Value.ToString()}";
                        //label3.Text = $"ScaleValue: {_plcRuntime.Tags.FirstOrDefault(t => t.Name == "ScaleValue").Value.ToString()}";
                    }

                    await Task.Delay(100, token); // nhịp kiểm tra, đủ nhẹ nhàng
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

        private void Sub_OnValueChanged(PlcTag obj)
        {

        }

        private void Client_StateChanged(PlcConnectionState obj)
        {
            _plcConnectionState = obj;
        }

        private void FrmScale_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _timer?.Cancel();
                _timerTask?.Wait(1000);

                _resetUiCts?.Cancel();
                _resetUiTask?.Wait(1000); // đợi nhẹ, tránh treo UI

                // 1.Dừng vòng lặp Modbus
                _readPlcCts?.Cancel();

                // 2. Dừng Polling của Snap7
                _sub?.Stop();

                // 3. Hủy kết nối PLC (Dừng Watchdog và ngắt TCP)
                _plc1Client.Dispose(); // Sẽ dừng watchdog và ngắt kết nối
            }
            catch
            {

            }
            finally
            {
                _resetUiCts?.Dispose();
                _resetUiCts = null;
                _resetUiTask = null;

                _timer?.Dispose();
                _timer = null;
                _timerTask = null;
            }
        }

        #region Barcode handle
        private void BarcodeScanner1Handle(int station, string barcodeString)
        {
            GlobalVariables.AutoPostingStatus1 = string.Empty;

            try
            {
                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labQrIdentification.Text = barcodeString;
                    _labQrIdentification.BackColor = Color.White;
                });

                #region Xử lý data ban đầu theo QR code
                _scanDataMetal.CreatedBy = GlobalVariables.UserLoginInfo.Id;
                _scanDataMetal.Station = GlobalVariables.ConfigJson.Station;

                bool specialCaseMetal = false;//dùng có các trường hợp hàng PU, trên WL decpration là 0, nhưng QC phân ra printing 0-1. beforePrinting thì get theo
                                              //printing=0; afterPrinting thì get theo printing=1. 6112012228

                #region xử lý barcode lấy ra các giá trị theo code
                _scanDataMetal.BarcodeString = barcodeString;
                var ocFirstCharMetal = barcodeString.Substring(0, 2);

                if (_scanDataMetal.BarcodeString.Contains("|"))
                {
                    var s = barcodeString.Split('|');
                    var s1 = s[0].Split(',');
                    _unit = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    //Check xem  QR code quét vào có đúng định dạng hay ko

                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstCharMetal);

                    if (resultCheckOc != null)
                    {
                        _scanDataMetal.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            _labResultIdentification.Text = "OC không đúng định dạng";
                            _labResultIdentification.ForeColor = Color.Red;
                        });

                        //ghi lệnh reject do ko quet đc tem
                        _metalScannerStatus = 1;

                        //log vao bang reject
                        using (var dbContext = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                        {
                            var nl = new tblScanDataReject()
                            {
                                Id = Guid.NewGuid(),
                                IsActived = 1,
                                CreatedDate = DateTime.Now,
                                CreatedMachine = Environment.MachineName,
                                BarcodeString = _scanDataMetal.BarcodeString,
                                IdLabel = _scanDataMetal.IdLabel,
                                OcNo = _scanDataMetal.OcNo,
                                BoxId = _scanDataMetal.BoxNo,
                                ProductNumber = _scanDataMetal.ProductNumber,
                                ProductName = _scanDataMetal.ProductName,
                                Quantity = _scanDataMetal.Quantity,
                                ScannerStation = "Identification",
                                Reason = "OC is not in the correct format.",
                                GrossWeight = _scanDataMetal.GrossWeight,
                                DeviationPairs = _scanDataMetal.DeviationPairs,
                                DeviationWeight = _scanDataMetal.Deviation
                            };

                            dbContext.TblScanDataRejects.Add(nl);
                            dbContext.SaveChanges();
                        }

                        return;
                    }

                    _scanDataMetal.ProductNumber = s1[1];

                    _scanDataMetal.Quantity = Convert.ToInt32(s1[2]);
                    _scanDataMetal.LinePosNo = s1[3];
                    _scanDataMetal.BoxNo = s1[5];
                    _scanDataMetal.CustomerNo = s1[6];
                    _scanDataMetal.BoxPosNo = s1[7];

                    if (s[1].Contains(","))
                    {
                        var s2 = s[1].Split(',');

                        GlobalVariables.IdLabel = s2[1];
                        _scanDataMetal.IdLabel = GlobalVariables.IdLabel;

                        if (s2[0] == "1")
                        {
                            _scanDataMetal.Location = LocationEnum.fVN;
                        }
                        else if (s2[0] == "2")
                        {
                            _scanDataMetal.Location = LocationEnum.fFT;
                        }
                        else if (s2[0] == "3")
                        {
                            _scanDataMetal.Location = LocationEnum.fKV;
                        }
                    }
                    else
                    {
                        if (s[1] == "1")
                        {
                            _scanDataMetal.Location = LocationEnum.fVN;
                        }
                        else if (s[1] == "2")
                        {
                            _scanDataMetal.Location = LocationEnum.fFT;
                        }
                        else if (s[1] == "3")
                        {
                            _scanDataMetal.Location = LocationEnum.fKV;
                        }
                    }
                }
                else
                {
                    var s1 = _scanDataMetal.BarcodeString.Split(',');
                    _unit = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    //Check xem  QR code quét vào có đúng định dạng hay ko
                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstCharMetal);

                    if (resultCheckOc != null)
                    {
                        _scanDataMetal.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            _labResultIdentification.Text = "OC is not in the correct format.";
                            _labResultIdentification.ForeColor = Color.Red;
                        });

                        //ghi lệnh reject do ko quet đc tem
                        _metalScannerStatus = 1;

                        //log vao bang reject
                        using (var dbContext = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                        {
                            var nl = new tblScanDataReject()
                            {
                                Id = Guid.NewGuid(),
                                IsActived = 1,
                                CreatedDate = DateTime.Now,
                                CreatedMachine = Environment.MachineName,
                                BarcodeString = _scanDataMetal.BarcodeString,
                                IdLabel = _scanDataMetal.IdLabel,
                                OcNo = _scanDataMetal.OcNo,
                                BoxId = _scanDataMetal.BoxNo,
                                ProductNumber = _scanDataMetal.ProductNumber,
                                ProductName = _scanDataMetal.ProductName,
                                Quantity = _scanDataMetal.Quantity,
                                ScannerStation = "Identification",
                                Reason = "OC is not in the correct format.",
                                GrossWeight = _scanDataMetal.GrossWeight,
                                DeviationPairs = _scanDataMetal.DeviationPairs,
                                DeviationWeight = _scanDataMetal.Deviation
                            };

                            dbContext.TblScanDataRejects.Add(nl);
                            dbContext.SaveChanges();
                        }

                        return;
                    }

                    //_scanDataMetal.OcNo = s1[0];
                    _scanDataMetal.ProductNumber = s1[1];

                    _scanDataMetal.Quantity = Convert.ToInt32(s1[2]);
                    _scanDataMetal.LinePosNo = s1[3];
                    _scanDataMetal.BoxNo = s1[5];
                }

                #region check special case
                foreach (var item in GlobalVariables.SpecialCaseList)
                {
                    if (_scanDataMetal.ProductNumber.Split('-')[0].Equals(item.MainItem))
                    {
                        specialCaseMetal = true;

                        break;
                    }
                }
                #endregion

                GlobalVariables.OcNo = _scanDataMetal.OcNo;
                GlobalVariables.BoxNo = _scanDataMetal.BoxNo;
                #endregion
                #endregion

                using (var dbContext = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                {
                    var para = new DynamicParameters();

                    #region Auto Stock In to 1223 if Box come to QC, update 20240819
                    //kiểm tra thùng hàng ko có trong kho production hand ove WH (1185) là cho stock in vao kho QC hand over WH (1223)
                    var res1 = AutoPostingHelper.CheckIn(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, barcodeString, dbContext);
                    var accept = res1?.FirstOrDefault();

                    var logLineInsert = new tblLog
                    {
                        Message = $"Before2|Scanner 1 sp_lmpScannerClient_ScanningLabel_CheckIn = {res1?.Count}.",
                        Level = "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn",
                        TimeStamp = DateTime.Now,
                    };
                    dbContext.TblLogs.Add(logLineInsert);
                    dbContext.SaveChanges();

                    //khi nào mở lại tính năng auto posting thì mở đoạn comment dưới ra lại
                    if (accept == null)
                    {
                        //para1 = new DynamicParameters();
                        //para1.Add("itemCode", _scanDataMetal.ProductNumber);
                        //var checkDecoration = dbContext.Query<WinlineDataModel>("sp_IdcScanScaleGetCoreDataByItemCode", param: para1, commandType: CommandType.StoredProcedure).FirstOrDefault();

                        //if (checkDecoration != null)
                        //{
                        //    if (checkDecoration.Decoration == 0)
                        //    {
                        //        GlobalVariables.InvokeIfRequired(this, () =>
                        //        {
                        //            labErrInfoMetal.Text = "Hàng từ sản xuất nhưng chưa nhập bất kỳ kho nào.";
                        //        });

                        //        _metalScannerStatus = 1;//bao reject cho PLC

                        //        //log vao bang reject
                        //        para = null;
                        //        para = new DynamicParameters();
                        //        para.Add("_barcodeString", _scanDataMetal.BarcodeString);
                        //        para.Add("_idLabel", _scanDataMetal.IdLabel);
                        //        para.Add("_ocNo", _scanDataMetal.OcNo);
                        //        para.Add("_boxId", _scanDataMetal.BoxNo);
                        //        para.Add("_productNumber", _scanDataMetal.ProductNumber);
                        //        para.Add("_productName", _scanDataMetal.ProductName);
                        //        para.Add("_quantity", _scanDataMetal.Quantity);
                        //        para.Add("_scannerStation", "Identification");
                        //        para.Add("_reason", "Hàng từ sản xuất nhưng chưa nhập bất kỳ kho nào.");
                        //        para.Add("_grossWeight", _scanDataMetal.GrossWeight);
                        //        para.Add("@_deviationPairs", _scanDataMetal.DeviationPairs);
                        //        para.Add("@_deviationWeight", _scanDataMetal.Deviation);

                        //        dbContext.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                        //        return;
                        //    }
                        //}
                        //else if (checkDecoration == null) { _metalScannerStatus = 1; return; }

                        //para1 = new DynamicParameters();
                        //para1.Add("@Message", $"After|Scanner 1 sp_lmpScannerClient_ScanningLabel_CheckIn = {res1?.Count}. to 1808.");
                        //para1.Add("@MessageTemplate", $"{barcodeString}");
                        //para1.Add("Level", "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn");
                        //dbContext.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                        ////nếu tem ko có trong kho nào, và item hàng sơn thì stockIn vào kho 1808 kho repacking hàng đi sơn về.
                        //GlobalVariables.AutoPostingStatus1 = AutoPostingHelper.AutoStockIn(false, _scanDataMetal.ProductNumber, barcodeString, 1808, dbContext);
                        //Log.Information($"Auto post Scanner 1 | {GlobalVariables.AutoPostingStatus1}");

                        //GlobalVariables.InvokeIfRequired(this, () =>
                        //{
                        //    labErrInfoMetal.Text = GlobalVariables.AutoPostingStatus1;
                        //});
                    }
                    //nếu tem nằm trong kho sản xuất, tức là công nhân quên transfer qua kho 1185, vào transfer tự động qua kho 1185
                    else if (accept != null && accept.C004 == "4")
                    {
                        logLineInsert = new tblLog
                        {
                            Message = $"After2 2|Scanner 1 sp_lmpScannerClient_ScanningLabel_CheckIn = {res1?.Count}. to 1185.",
                            Level = "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn",
                            TimeStamp = DateTime.Now,
                        };
                        dbContext.TblLogs.Add(logLineInsert);
                        dbContext.SaveChanges();

                        GlobalVariables.AutoPostingStatus1 = AutoPostingHelper.AutoTransfer(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, barcodeString
                            , Convert.ToInt16(accept.C004), 1185, dbContext, DateTime.Now);
                        Log.Information($"Auto post Scanner 1 | {GlobalVariables.AutoPostingStatus1}");

                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            _labResultIdentification.Text = GlobalVariables.AutoPostingStatus1;
                            _labResultIdentification.ForeColor = Color.Green;
                        });
                    }
                    #endregion

                    #region kiểm tra xem oc boxno đã có trong hệ thống hay chưa
                    //para.Add("oc", _scanDataMetal.OcNo);
                    //para.Add("boxNo", _scanDataMetal.BoxNo);

                    //var checkBox = dbContext.Query<tblScanData>("sp_tblScanDataGetByOcBoxNo", para, commandType: CommandType.StoredProcedure).ToList();
                    var checkBox = dbContext.TblScanDatas
                        .Where(x => x.Actived == 1 && x.OcNo == _scanDataMetal.OcNo && x.BoxNo == _scanDataMetal.BoxNo)
                        .ToList();

                    if (checkBox != null && checkBox.Count > 0)
                    {
                        //kiểm tra xem tem vừa quét nó là quét lại hay tem mới.  nếu là null là tem mới
                        var box = checkBox.FirstOrDefault(x => x.BarcodeString == _scanDataMetal.BarcodeString);

                        //trường hợp quét tem đã đc in lại tem (tem mới), deactive các tem trước đó đã đi qua băng tải đi để check lại thông tin theo tem mới.
                        if (box == null)
                        {
                            checkBox.ForEach(x =>
                            {
                                x.Actived = 0;
                            });
                            dbContext.SaveChanges();

                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                _labQrIdentification.BackColor = Color.Yellow;
                            });
                        }
                        else//trường hợp quét lại chính thùng trước đó đã đi qua băng tải
                        {
                            if (box.Pass == 1
                                || (box.Pass == 0 && box.Status == 2 && box.ActualDeviationPairs == 0)
                                )
                            {
                                #region Auto Stock In to 1223 if Box come to QC, update 20240819
                                //kiểm tra thùng hàng ko có trong kho production hand ove WH (1185) là cho stock in vao kho QC hand over WH (1223)
                                res1 = AutoPostingHelper.CheckIn(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, barcodeString, dbContext);
                                accept = res1?.FirstOrDefault();

                                var logNl = new tblLog()
                                {
                                    Message = $"After|Scanner 1 sp_lmpScannerClient_ScanningLabel_CheckIn = {res1?.Count}. to 2 .",
                                    MessageTemplate = $"{barcodeString}",
                                    Level = "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn",
                                    Exception = null,
                                    TimeStamp = DateTime.Now
                                };
                                dbContext.TblLogs.Add(logNl);
                                dbContext.SaveChanges();

                                if (accept != null)
                                {
                                    var whTo = 2;
                                    if (_scanDataMetal.OcNo.Substring(0, 2) == "PR") whTo = 10;

                                    //nếu tem ko có trong kho nào, hoặc đã có trong kho mà khác kho 4(production) thì stockIn vào kho 1223
                                    GlobalVariables.AutoPostingStatus1 = AutoPostingHelper.AutoTransfer(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, barcodeString
                                        , Convert.ToInt16(accept.C004), whTo, dbContext, DateTime.Now);
                                    Log.Information($"Auto post Scanner 1 | {GlobalVariables.AutoPostingStatus1}");

                                    GlobalVariables.InvokeIfRequired(this, () =>
                                    {
                                        _labResultIdentification.Text = GlobalVariables.AutoPostingStatus1;
                                        _labResultIdentification.ForeColor = Color.Green;
                                    });
                                }
                                #endregion

                                Debug.WriteLine($"ProductNumber: {box.ProductNumber} đã kiểm tra OK, không kiểm tra lại.");

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labResultIdentification.Text = "Thùng này đã check OK. không kiểm tra lại";
                                    _labResultIdentification.ForeColor = Color.Red;
                                });

                                _metalScannerStatus = 1;

                                ////log vao bang reject
                                var scanReject = new tblScanDataReject
                                {
                                    Id = Guid.NewGuid(),
                                    IsActived = 1,
                                    CreatedDate = DateTime.Now,
                                    CreatedMachine = Environment.MachineName,
                                    BarcodeString = _scanDataMetal.BarcodeString,
                                    IdLabel = _scanDataMetal.IdLabel,
                                    OcNo = _scanDataMetal.OcNo,
                                    BoxId = _scanDataMetal.BoxNo,
                                    ProductNumber = _scanDataMetal.ProductNumber,
                                    ProductName = _scanDataMetal.ProductName,
                                    Quantity = _scanDataMetal.Quantity,
                                    ScannerStation = "Identification",
                                    Reason = "Thùng này đã check OK.",
                                    GrossWeight = _scanDataMetal.GrossWeight,
                                    DeviationPairs = _scanDataMetal.DeviationPairs,
                                    DeviationWeight = _scanDataMetal.Deviation
                                };
                                dbContext.TblScanDataRejects.Add(scanReject);
                                dbContext.SaveChanges();

                                return;
                            }
                        }
                    }

                    #region Kiểm tra xem thùng này đã được log vào scanData chưa
                    //para = null;
                    //para = new DynamicParameters();
                    //para.Add("_QrCode", _scanDataMetal.BarcodeString);
                    //var checkInfo = dbContext.Query<tblScanDataModel>("sp_tblScanDataGetByQrCode", para, commandType: CommandType.StoredProcedure).ToList();
                    //foreach (var item in checkInfo)
                    //{
                    //    if (
                    //        (item.Pass == 1 && (item.Status == 2 || GlobalVariables.Station == EnumStation.Identification))
                    //        //|| (item.Pass == 0 && item.ActualDeviationPairs == 0 && item.ApprovedBy != Guid.Empty)
                    //        || (item.Pass == 0 && item.Status == 2 && item.ActualDeviationPairs == 0)
                    //        )
                    //    {
                    //        Debug.WriteLine($"ProductNumber: {item.ProductNumber} đã kiểm tra OK, không được.");

                    //        GlobalVariables.InvokeIfRequired(this, () =>
                    //        {
                    //            labErrInfoMetal.Text = "Thùng này đã check OK.";
                    //        });

                    //        _metalScannerStatus = 1;

                    //        //log vao bang reject
                    //        para = null;
                    //        para = new DynamicParameters();
                    //        para.Add("_barcodeString", _scanDataMetal.BarcodeString);
                    //        para.Add("_idLabel", _scanDataMetal.IdLabel);
                    //        para.Add("_ocNo", _scanDataMetal.OcNo);
                    //        para.Add("_boxId", _scanDataMetal.BoxNo);
                    //        para.Add("_productNumber", _scanDataMetal.ProductNumber);
                    //        para.Add("_productName", _scanDataMetal.ProductName);
                    //        para.Add("_quantity", _scanDataMetal.Quantity);
                    //        para.Add("_scannerStation", "Identification");
                    //        para.Add("_reason", "Thùng này đã check OK.");
                    //        para.Add("_grossWeight", _scanDataMetal.GrossWeight);
                    //        para.Add("@_deviationPairs", _scanDataMetal.DeviationPairs);
                    //        para.Add("@_deviationWeight", _scanDataMetal.Deviation);

                    //        dbContext.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                    //        return;
                    //    }
                    //}
                    #endregion
                    #endregion

                    #region process normally 
                    // 2023-07-26:
                    //ghi nhận In commming SSFG
                    var incomingLine = new tblIncomingIDC()
                    {
                        Actived = 1,
                        CreatedBy = Environment.MachineName,
                        CreatedDate = DateTime.Now,
                        QRCode = barcodeString,
                        OCNo = _scanDataMetal.OcNo,
                        BoxNo = _scanDataMetal.BoxNo,
                        IdLabel = _scanDataMetal.IdLabel
                    };
                    dbContext.TblIncomingIDCs.Add(incomingLine);
                    dbContext.SaveChanges();

                    // 2023-07-26:
                    var res = dbContext.Database.SqlQuery<ProductInfoModel>(
                        "sp_vProductItemInfoGet @ProductNumber = {0}, @SpecialCase = {1}"
                        , _scanDataMetal.ProductNumber, specialCaseMetal
                        )
                        .FirstOrDefault();

                    if (res != null)
                    {
                        _scanDataMetal.ProductName = res.ProductName;

                        if (res.AveWeight1Prs > 0)
                        {
                            if (res.MetalScan == 1 && ocFirstCharMetal != "PR")
                            {
                                _metalScannerStatus = 0;
                                //GlobalVariables.MyEvent.MetalPusher = 0;

                                Debug.WriteLine($"ProductNumber: {res.ProductNumber} có kiểm tra kim loại.");

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labResultIdentification.Text = "Hàng kiểm kim loại.";
                                });
                            }
                            else if (res.MetalScan == 0 || (res.MetalScan == 1 && ocFirstCharMetal == "PR"))
                            {
                                // gui data xuong PLC để điều khiển băng tải phân luồng chạy vòng qua máy quét kim loại.
                                _metalScannerStatus = 2;
                                //GlobalVariables.MyEvent.MetalPusher = 2;

                                Debug.WriteLine($"ProductNumber: {res.ProductNumber} không kiểm tra kim loại.");

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labResultIdentification.Text = "Hàng không kiểm kim loại.";
                                });
                            }
                        }
                        else
                        {
                            _metalScannerStatus = 1;//bao reject cho PLC

                            Debug.WriteLine($"Item '{_scanDataWeight.ProductNumber}' không có khối lượng/1 đôi. Xin hãy kiểm tra lại thông tin."
                                , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                _labResultIdentification.Text = "Không có khối lượng đôi. Weight/Prs.";
                                _labResultIdentification.ForeColor = Color.Red;
                            });

                            //log vao bang reject

                            var rejectLine = new tblScanDataReject
                            {
                                Id = Guid.NewGuid(),
                                IsActived = 1,
                                CreatedDate = DateTime.Now,
                                CreatedMachine = Environment.MachineName,
                                BarcodeString = _scanDataMetal.BarcodeString,
                                IdLabel = _scanDataMetal.IdLabel,
                                OcNo = _scanDataMetal.OcNo,
                                BoxId = _scanDataMetal.BoxNo,
                                ProductNumber = _scanDataMetal.ProductNumber,
                                ProductName = _scanDataMetal.ProductName,
                                Quantity = _scanDataMetal.Quantity,
                                ScannerStation = "Identification",
                                Reason = "Không có khối lượng đôi. Average Weight/prs.",
                                GrossWeight = _scanDataMetal.GrossWeight,
                                DeviationPairs = _scanDataMetal.DeviationPairs,
                                DeviationWeight = _scanDataMetal.Deviation
                            };
                            dbContext.TblScanDataRejects.Add(rejectLine);

                            var missingLine = new tblItemMissingInfo
                            {
                                Id = Guid.NewGuid(),
                                IsActive = true,
                                CreatedDate = DateTime.Now,
                                ProductNumber = _scanDataWeight.ProductNumber,
                                ProductName = _scanDataWeight.ProductName,
                                OcNum = _scanDataWeight.OcNo,
                                Note = "Chưa có data trong file QC.",
                                QrCode = _scanDataWeight.BarcodeString
                            };
                            dbContext.TblItemMissingInfos.Add(missingLine);

                            dbContext.SaveChanges();
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Product number {_scanDataMetal.ProductNumber} không có trong hệ thống. Hãy báo quản lý để lấy lại dữ liệu mới nhất từ Winline về."
                            , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        throw new Exception($"Product number {_scanDataMetal.ProductNumber} không có trong hệ thống. Hãy báo quản lý để lấy lại dữ liệu mới nhất từ Winline về.");
                    }
                    #endregion
                }

            }
            catch (Exception ex)
            {
                //gui data xuong PLC
                _metalScannerStatus = 1;

                //hien thi mau label
                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labResultIdentification.Text = ex.Message;
                    _labResultIdentification.ForeColor = Color.Red;
                });

                using (var dbContextSSFG = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                {
                    var logLine = new tblLog()
                    {
                        Message = $"Lỗi check label.Scanner: 1 (Identification)|{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}",
                        Level = "LogError",
                        Exception = ex.ToString(),
                    };
                    dbContextSSFG.TblLogs.Add(logLine);

                    var rejectLine = new tblScanDataReject()
                    {
                        Id = Guid.NewGuid(),
                        IsActived = 1,
                        CreatedDate = DateTime.Now,
                        CreatedMachine = Environment.MachineName,
                        BarcodeString = _scanDataMetal.BarcodeString,
                        IdLabel = _scanDataMetal.IdLabel,
                        OcNo = _scanDataMetal.OcNo,
                        BoxId = _scanDataMetal.BoxNo,
                        ProductNumber = _scanDataMetal.ProductNumber,
                        ProductName = _scanDataMetal.ProductName,
                        Quantity = _scanDataMetal.Quantity,
                        ScannerStation = "Identification",
                        Reason = $"Ex:{ex.ToString()}.",
                        GrossWeight = _scanDataMetal.GrossWeight,
                        DeviationPairs = _scanDataMetal.DeviationPairs,
                        DeviationWeight = _scanDataMetal.Deviation
                    };
                    dbContextSSFG.TblScanDataRejects.Add(rejectLine);
                    dbContextSSFG.SaveChanges();
                }

                Log.Error(ex.ToString(), ex.Message);
            }
            finally
            {
                _scannerIsBussy[0] = false;
            }
        }

        private void BarcodeScanner2Handle(int station, string barcodeString)
        {
            GlobalVariables.AutoPostingStatus3 = string.Empty;
            var errorFlag = false;

            try
            {
                SendDynamicString(" ", " ", " ");
                //reset model để lưu cho thùng mới
                _scanDataWeight = null;
                _scanDataWeight = new tblScanData();
                _approvePrint = false;
                GlobalVariables.IdLabel = string.Empty;

                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    labQrScale.Text = barcodeString;
                });

                #region Xử lý data ban đầu theo QR code
                _scanDataWeight.CreatedBy = GlobalVariables.UserLoginInfo.Id;
                _scanDataWeight.Station = GlobalVariables.ConfigJson.Station;

                bool specialCase = false;//dùng có các trường hợp hàng PU, trên WL decpration là 0, nhưng QC phân ra printing 0-1. beforePrinting thì get theo
                                         //printing=0; afterPrinting thì get theo printing=1. 6112012228

                //biến dùng để check xem thùng đó có trong bảng scanData hay chưa.
                int statusLogData = 0;//0-chưa có;1-đã có dòng fail;2-đã có dòng pass;3-đã có cả fail và pass
                bool isFail = false;
                bool isPass = false;

                double lowerToleranceOfBox = 0, upperToleranceOfBox = 0;
                double nwPlus = 0;
                double nwSub = 0;

                double ratioFailWeight = 0;//biến chứa ratioFailWeight của lần fail trước

                #region xử lý barcode lấy ra các giá trị theo code
                _scanDataWeight.BarcodeString = barcodeString;
                var ocFirstChar = barcodeString.Substring(0, 2);

                if (_scanDataWeight.BarcodeString.Contains("|"))
                {
                    var s = barcodeString.Split('|');
                    var s1 = s[0].Split(',');
                    _scanDataWeight.Unit = _unit = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    //Check xem  QR code quét vào có đúng định dạng hay ko

                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstChar);

                    if (resultCheckOc != null)
                    {
                        _scanDataWeight.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        //ghi lệnh reject do ko quet đc tem
                        GlobalVariables.MyEvent.WeightPusher = 1;

                        throw new Exception("OC không đúng định dạng.");
                    }

                    _scanDataWeight.ProductNumber = s1[1];

                    _scanDataWeight.Quantity = Convert.ToInt32(s1[2]);
                    _scanDataWeight.LinePosNo = s1[3];
                    _scanDataWeight.BoxNo = s1[5];
                    _scanDataWeight.CustomerNo = s1[6];
                    _scanDataWeight.BoxPosNo = s1[7];

                    if (s[1].Contains(","))
                    {
                        var s2 = s[1].Split(',');

                        GlobalVariables.IdLabel = s2[1];
                        _scanDataWeight.IdLabel = GlobalVariables.IdLabel;

                        if (s2[0] == "1")
                        {
                            _scanDataWeight.Location = LocationEnum.fVN;
                        }
                        else if (s2[0] == "2")
                        {
                            _scanDataWeight.Location = LocationEnum.fFT;
                        }
                        else if (s2[0] == "3")
                        {
                            _scanDataWeight.Location = LocationEnum.fKV;
                        }
                    }
                    else
                    {
                        if (s[1] == "1")
                        {
                            _scanDataWeight.Location = LocationEnum.fVN;
                        }
                        else if (s[1] == "2")
                        {
                            _scanDataWeight.Location = LocationEnum.fFT;
                        }
                        else if (s[1] == "3")
                        {
                            _scanDataWeight.Location = LocationEnum.fKV;
                        }
                    }
                }
                else
                {
                    var s1 = _scanDataWeight.BarcodeString.Split(',');
                    _scanDataWeight.Unit = _unit = s1[4];//get Thung này đóng theo đôi (P) hay L/R


                    //Check xem  QR code quét vào có đúng định dạng hay ko
                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstChar);

                    if (resultCheckOc != null)
                    {
                        _scanDataWeight.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        //ghi lệnh reject do ko quet đc tem
                        GlobalVariables.MyEvent.WeightPusher = 1;

                        throw new Exception("OC không đúng định dạng.");
                    }

                    //_scanData.OcNo = s1[0];
                    _scanDataWeight.ProductNumber = s1[1];

                    _scanDataWeight.Quantity = Convert.ToInt32(s1[2]);
                    _scanDataWeight.LinePosNo = s1[3];
                    _scanDataWeight.BoxNo = s1[5];
                }

                #region check special case
                foreach (var item in GlobalVariables.SpecialCaseList)
                {
                    if (_scanDataWeight.ProductNumber.Split('-')[0].Equals(item.MainItem))
                    {
                        specialCase = true;

                        break;
                    }
                }
                #endregion

                GlobalVariables.OcNo = _scanDataWeight.OcNo;
                GlobalVariables.BoxNo = _scanDataWeight.BoxNo;
                #endregion
                #endregion

                #region truy vấn data và xử lý
                //lấy thông tin khối lượng cân sau khi cân đã báo stable
                //Debug.WriteLine($"da vao can,dang doi stable {_stableScale}");
                while (_stableScale == 0 && GlobalVariables.ConfigJson.IsScale)
                {
                    Thread.Yield();//cho nó qua 1 luồng khác chạy để tránh làm treo luồng hiện tại
                }
                //Debug.WriteLine($"da can xong. stable {_stableScale}");

                _scanDataWeight.GrossWeight = GlobalVariables.RealWeight = _scaleValueStable;
                //truy vấn thông tin 
                using (var dbContextSSFG = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                {
                    #region Kiểm tra xem thùng này đã được log vào scanData chưa
                    var checkInfo = dbContextSSFG.TblScanDatas.Where(x => x.Actived == 1 && x.BarcodeString == _scanDataWeight.BarcodeString).ToList();
                    foreach (var item in checkInfo)
                    {
                        if (
                            (item.Pass == 1)
                            //|| (item.Pass == 0 && item.ActualDeviationPairs == 0 && item.ApprovedBy != Guid.Empty)
                            || (item.Pass == 0 && item.Status == 2 && item.ActualDeviationPairs == 0)
                            )
                        {
                            isPass = true;
                        }
                        else if (
                                    (item.Pass == 0 && item.Status == 0)// && item.ActualDeviationPairs != 0 && item.ApprovedBy != Guid.Empty)
                                    || (item.Pass == 0 && item.Status == 2 && item.ActualDeviationPairs != 0)
                                )
                        {
                            isFail = true;

                            //tính tỷ lệ khối lượng số đôi lỗi/ StdGrossWeight
                            ratioFailWeight = Math.Round((Math.Abs(item.DeviationPairs) * item.AveWeight1Prs) / item.StdGrossWeight, 3);
                        }
                    }

                    if (!isPass && !isFail)
                    {
                        statusLogData = 0;
                    }
                    else if (!isPass && isFail)
                    {
                        statusLogData = 1;
                    }
                    else if (isPass && !isFail)
                    {
                        statusLogData = 2;
                    }
                    else if (isPass && isFail)
                    {
                        statusLogData = 3;
                    }
                    #endregion

                    //đối với hàng sơn PU, thì trước sơn lấy các giá trị theo printing =0. Sau sơn thì lấy các giá trị theo printing =1
                    //nếu checkOc == null --> hàng sơn- trước sơn (PRT).
                    var checkOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstChar && ocFirstChar != "PR");

                    var printingCheck = 0;
                    if (specialCase)
                    {
                        //after printing
                        if (checkOc != null || (ocFirstChar == "PR" && GlobalVariables.ConfigJson.AfterPrinting != 0))
                        {
                            printingCheck = 1;
                        }
                        else//before printing
                        {
                            printingCheck = 0;
                        }
                    }
                    var res = dbContextSSFG.Database.SqlQuery<ProductInfoModel>(
                                "sp_vProductItemInfoGet @ProductNumber= {0}, @SpecialCase = {1}, @Printing = {2}",
                               _scanDataWeight.ProductNumber, specialCase, printingCheck
                           )
                           .FirstOrDefault();

                    if (res != null)
                    {
                        _unitLabel = _scanDataWeight.Unit == "P" ? "prs" : "pcs";
                        _color = res.Color;
                        _sizeName = res.SizeName;

                        _scanDataWeight.ProductName = res.ProductName;
                        _scanDataWeight.Decoration = (int)res.Decoration;
                        _scanDataWeight.MetalScan = (int)res.MetalScan;
                        _scanDataWeight.Brand = res.Brand;
                        _scanDataWeight.AveWeight1Prs = (double)res.AveWeight1Prs;
                        _scanDataWeight.ProductCategory = (int)res.ProductCategory;

                        if (_scanDataWeight.AveWeight1Prs != 0)
                        {
                            #region Fill data from coreData to scanData, tính toán ra NetWeight và GrossWeight
                            //Xét điều kiện để lấy boxWeight. Nếu là hàng đi sơn thì dùng thùng nhựa
                            if ((_scanDataWeight.Decoration == 0 || (_scanDataWeight.Decoration == 1 && checkOc != null)) && checkOc.OcFirstChar != "BF")
                            {
                                _scanDataWeight.Status = 2;//báo trạng thái hàng ko đi sơn, hoặc hàng sơn đã được sơn rồi

                                //lấy tolerance theo thùng giấy
                                lowerToleranceOfBox = (double)res.LowerToleranceOfCartonBox;
                                upperToleranceOfBox = (double)res.UpperToleranceOfCartonBox;

                                if (_scanDataWeight.Quantity <= res.BoxQtyBx4)
                                {
                                    _scanDataWeight.BoxWeight = (double)res.BoxWeightBx4;
                                    _boxType = EnumBoxType.BX4;
                                }
                                else if (_scanDataWeight.Quantity > res.BoxQtyBx4 && _scanDataWeight.Quantity <= res.BoxQtyBx3)
                                {
                                    _scanDataWeight.BoxWeight = (double)res.BoxWeightBx3;
                                    _boxType = EnumBoxType.BX3;
                                }
                                else if (_scanDataWeight.Quantity > res.BoxQtyBx3 && _scanDataWeight.Quantity <= res.BoxQtyBx2)
                                {
                                    _scanDataWeight.BoxWeight = (double)res.BoxWeightBx2;
                                    _boxType = EnumBoxType.BX2;
                                }
                                else if (_scanDataWeight.Quantity > res.BoxQtyBx2 && _scanDataWeight.Quantity <= res.BoxQtyBx1)
                                {
                                    _scanDataWeight.BoxWeight = (double)res.BoxWeightBx1;
                                    _boxType = EnumBoxType.BX1;
                                }
                                else if (_scanDataWeight.Quantity > res.BoxQtyBx1)
                                {
                                    Debug.WriteLine($"Số lượng vượt quá giới hạn thùng BX1 ({res.BoxQtyBx1})", "CẢNH BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                    var missingLine = new tblItemMissingInfo
                                    {
                                        Id = Guid.NewGuid(),
                                        IsActive = true,
                                        CreatedDate = DateTime.Now,
                                        ProductNumber = _scanDataWeight.ProductNumber,
                                        ProductName = _scanDataWeight.ProductName,
                                        OcNum = _scanDataWeight.OcNo,
                                        Note = $"Số lượng vượt quá giới hạn thùng BX1 ({res.BoxQtyBx1})",
                                        QrCode = _scanDataWeight.BarcodeString
                                    };
                                    dbContextSSFG.TblItemMissingInfos.Add(missingLine);
                                    dbContextSSFG.SaveChanges();

                                    throw new Exception("Số lượng vượt quá giới hạn thùng BX1.");
                                }

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labPrinting.Text = _scanDataWeight.Decoration == 0 ? "NO" : "YES";
                                });
                            }
                            else //if (_scanDataWeight.Decoration == 1 && _scanDataWeight.OcNo.Contains("PR"))//hàng trước sơn. chỉ có trạm SSFG01 mới nhảy vào đây
                            {
                                //lấy tolerance theo thùng nhựa
                                lowerToleranceOfBox = (double)res.LowerToleranceOfPlasticBox;
                                upperToleranceOfBox = (double)res.UpperToleranceOfPlasticBox;

                                if (GlobalVariables.ConfigJson.AfterPrinting == 0 && _scanDataWeight.OcNo.Contains("PR"))
                                {
                                    _scanDataWeight.Status = 1;// báo trạng thái hàng sơn cần đưa đi sơn, trạm SSFG01
                                }
                                else
                                {
                                    _scanDataWeight.Status = 2;// báo trạng thái hàng sơn đã được sơn, trạm SSFG02 và SSFG03(Kerry)
                                }

                                _scanDataWeight.BoxWeight = (double)res.PlasticBoxWeight;

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labPrinting.Text = _scanDataWeight.Decoration == 0 ? "NO" : "YES";
                                    _labBoxType.Text = "Plastic";
                                });
                            }

                            if (_scanDataWeight.MetalScan == 0)
                            {
                                _approveUpdateActMetalScan = false;
                            }
                            else
                            {
                                GlobalVariables.RememberInfo.MetalScan += 1;

                                _approveUpdateActMetalScan = true;
                            }

                            _scanDataWeight.StdNetWeight = Math.Round(_scanDataWeight.Quantity * _scanDataWeight.AveWeight1Prs, 3);

                            //_scanDataWeight.Tolerance = Math.Round(_scanDataWeight.StdNetWeight * (res.Tolerance / 100), 3);
                            _scanDataWeight.LowerTolerance = -Math.Round(_scanDataWeight.StdNetWeight * (lowerToleranceOfBox / 100), 3);
                            _scanDataWeight.UpperTolerance = Math.Round(_scanDataWeight.StdNetWeight * (upperToleranceOfBox / 100), 3);

                            //luu ý các Quantity partition-Plastic-WrapSheet trên DB nó là tính số Prs
                            //sau khi đọc về phải lấy QtyPrs quét trên label / Quantity partition-Plastic-WrapSheet ==> qty * weight ==> Weight package weight
                            double partitionWeight = 0;
                            var p = res.PartitionQty != 0 ? ((double)_scanDataWeight.Quantity / (double)res.PartitionQty) : 0;
                            if (_scanDataWeight.Quantity <= res.BoxQtyBx3 || p < 1)
                            {
                                partitionWeight = 0;
                            }
                            else if (p >= 1)
                            {
                                partitionWeight = Math.Floor(p) * (double)res.PartitionWeight;
                            }
                            //partitionWeight = res.PartitionQty != 0 ? (_scanDataWeight.Quantity / res.PartitionQty) * res.PartitionWeight : 0;
                            var PlasticBag1Weight = res.PlasticBag1Qty != 0 ? Math.Ceiling(((double)_scanDataWeight.Quantity / (double)res.PlasticBag1Qty)) * res.PlasticBag1Weight : 0;
                            var PlasticBag2Weight = res.PlasticBag2Qty != 0 ? Math.Ceiling(((double)_scanDataWeight.Quantity / (double)res.PlasticBag2Qty)) * res.PlasticBag2Weight : 0;
                            var wrapSheetWeight = res.WrapSheetQty != 0 ? Math.Ceiling(((double)_scanDataWeight.Quantity / (double)res.WrapSheetQty)) * res.WrapSheetWeight : 0;
                            var foamSheetWeight = res.FoamSheetQty != 0 ? Math.Ceiling(((double)_scanDataWeight.Quantity / (double)res.FoamSheetQty)) * res.FoamSheetWeight : 0;

                            _scanDataWeight.PackageWeight = Math.Round((double)(partitionWeight + PlasticBag1Weight + PlasticBag2Weight + wrapSheetWeight + foamSheetWeight), 3);

                            _scanDataWeight.StdGrossWeight = Math.Round(_scanDataWeight.StdNetWeight + _scanDataWeight.PackageWeight + _scanDataWeight.BoxWeight, 3);

                            #region tinh toán standardWeight theo Pair/Left/Right. lưu ý để sau này có áp dụng thì làm
                            //if (_unit == "P")
                            //{
                            //    _scanDataWeight.GrossdWeight = res.Weight * res.QtyPerbag + res.BagWeight;
                            //}
                            //else if (_unit == "L")
                            //{
                            //    if (res.LeftWeight == 0)
                            //    {
                            //        _scanDataWeight.StandardWeight = res.Weight * res.QtyPerbag + res.BagWeight;
                            //    }
                            //    else
                            //    {
                            //        _scanDataWeight.StandardWeight = res.LeftWeight * res.QtyPerbag + res.BagWeight;
                            //    }
                            //}
                            //else if (_unit == "R")
                            //{
                            //    if (res.RightWeight == 0)
                            //    {
                            //        _scanDataWeight.StandardWeight = res.Weight * res.QtyPerbag + res.BagWeight;
                            //    }
                            //    else
                            //    {
                            //        _scanDataWeight.StandardWeight = res.RightWeight * res.QtyPerbag + res.BagWeight;
                            //    }
                            //}
                            #endregion

                            #endregion

                            #region xử lý so sánh khối lượng cân thực tế với kế hoạch để xử lý
                            _scanDataWeight.NetWeight = Math.Round(_scanDataWeight.GrossWeight - _scanDataWeight.BoxWeight - _scanDataWeight.PackageWeight, 3);
                            _scanDataWeight.Deviation = Math.Round(_scanDataWeight.NetWeight - _scanDataWeight.StdNetWeight, 3);

                            #region tính toán số pairs chênh lệch và hiển thị label
                            //var nwPlus = _scanDataWeight.StdNetWeight + _scanDataWeight.Tolerance;
                            //var nwSub = _scanDataWeight.StdNetWeight - _scanDataWeight.Tolerance;
                            nwPlus = _scanDataWeight.StdNetWeight + _scanDataWeight.UpperTolerance;
                            nwSub = _scanDataWeight.StdNetWeight + _scanDataWeight.LowerTolerance;

                            if (((_scanDataWeight.NetWeight > nwPlus) && (_scanDataWeight.NetWeight - nwPlus < _scanDataWeight.AveWeight1Prs / 2))
                            || ((_scanDataWeight.NetWeight < nwSub) && (nwSub - _scanDataWeight.NetWeight < _scanDataWeight.AveWeight1Prs / 2))
                            )
                            {
                                _scanDataWeight.CalculatedPairs = _scanDataWeight.Quantity;
                            }
                            else if (_scanDataWeight.NetWeight > nwPlus)//roundDown
                            {
                                _scanDataWeight.CalculatedPairs = (int)(_scanDataWeight.Quantity + Math.Floor((_scanDataWeight.NetWeight - nwPlus) / _scanDataWeight.AveWeight1Prs));
                            }
                            else if (_scanDataWeight.NetWeight < nwSub)//RoundUp
                            {
                                _scanDataWeight.CalculatedPairs = (int)(_scanDataWeight.Quantity - Math.Ceiling((nwSub - _scanDataWeight.NetWeight) / _scanDataWeight.AveWeight1Prs));
                            }
                            else
                            {
                                _scanDataWeight.CalculatedPairs = _scanDataWeight.Quantity;
                            }

                            _scanDataWeight.DeviationPairs = _scanDataWeight.CalculatedPairs - _scanDataWeight.Quantity;
                            #endregion

                            ShowUI();

                            #region xét nếu xem thùng hàng là outsole hay heelcounter để vào check khối lượng cân pass/fail. nếu là hàng outsole thì mới check khối lượng
                            //ProductCategory = 11 nó là hàng heelcounter
                            if (_scanDataWeight.ProductCategory != 11)
                            {
                                if (_scanDataWeight.DeviationPairs == 0)
                                {
                                    //para = null;
                                    //para = new DynamicParameters();
                                    //para.Add("@Message", $"Hàng Outsole check weight OK.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    //para.Add("Level", "Log");
                                    //dbContext.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                                    _scanDataWeight.Pass = 1;//báo thùng pass
                                    _scanDataWeight.CreatedDate = GlobalVariables.CreatedDate = DateTime.Now;//lấy thời gian để đồng bộ giữa in tem và log DB Printing
                                                                                                             //bật tín hiệu để PLC on đèn xanh
                                                                                                             //GlobalVariables.MyEvent.StatusLightPLC = 2;

                                    if (_scanDataWeight.Decoration == 0)
                                    {
                                        GlobalVariables.RememberInfo.GoodBoxPrinting += 1;
                                    }
                                    else
                                    {
                                        GlobalVariables.RememberInfo.GoodBoxNoPrinting += 1;
                                    }

                                    //kiểm tra xem data đã có trên hệ thống hay chưa
                                    if (statusLogData == 0 || statusLogData == 1)
                                    {
                                        _approvePrint = true;//cho phép in

                                        //truyền nội dùn xuống máy in trước
                                        if (checkOc != null)//neu khong phai tem OC 'PRT' thì mới in tem
                                        {
                                            //20240202 update cho in tất cả các thùng passes quality check
                                            var passMetal = "Passed quality check";
                                            var idLabel = !string.IsNullOrEmpty(_scanDataWeight.IdLabel) ? _scanDataWeight.IdLabel : $"{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}";

                                            SendDynamicString($"{idLabel}  {passMetal}"
                                                               , $"{(_scanDataWeight.GrossWeight / 1000).ToString("#,#0.00")} Kg"
                                                               , _scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
                                                              );
                                        }
                                        else
                                        {
                                            //nếu là hàng sơn thì chỉ in ra khối lượng
                                            var passMetal = "Passed quality check";
                                            var idLabel = !string.IsNullOrEmpty(_scanDataWeight.IdLabel) ? _scanDataWeight.IdLabel : $"{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}";
                                            SendDynamicString(" "
                                                          , $" {(_scanDataWeight.GrossWeight / 1000).ToString("#,#0.00")}"
                                                          , " "
                                                         );
                                        }

                                        //bat den xanh 
                                        GlobalVariables.MyEvent.StatusLightPLC = 2;
                                        GlobalVariables.MyEvent.WeightPusher = 0;

                                        //hien thi mau label
                                        GlobalVariables.InvokeIfRequired(this, () =>
                                        {
                                            _labResult.Text = "OK";
                                            _labResult.BackColor = Color.Green;
                                            _labResult.ForeColor = Color.White;
                                            _labResultMessage.Text = "Khối lượng OK. In tem.";
                                        });

                                        LogDataScan(dbContextSSFG);

                                        //Auto transfer tem về kho 2 hoặc 10 ( hàng đi sơn)
                                        var res1 = AutoPostingHelper.CheckIn(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, barcodeString, dbContextSSFG);
                                        var accept = res1?.FirstOrDefault();

                                        var logLine = new tblLog
                                        {
                                            Message = $"Check Weight sp_lmpScannerClient_ScanningLabel_CheckIn =  {res1?.Count}.",
                                            MessageTemplate = barcodeString,
                                            Level = "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn",
                                            TimeStamp = DateTime.Now,
                                        };
                                        dbContextSSFG.TblLogs.Add(logLine);
                                        dbContextSSFG.SaveChanges();

                                        if (accept != null)
                                        {
                                            var whFrom = Convert.ToInt16(accept.C004);
                                            var whTo = 2;

                                            if (_scanDataWeight.OcNo.Substring(0, 2) == "PR") whTo = 10;

                                            #region Kiểm tra xem tem có nằm ở kho lỗi trước đó thì chuyển về 1185/1223 rồi mới chuyển vào kho 2/10
                                            if (accept.C004 == "964" || accept.C004 == "965")
                                            {
                                                GlobalVariables.AutoPostingStatus3 = AutoPostingHelper.AutoTransfer(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, barcodeString
                                               , Convert.ToInt16(accept.C004), Convert.ToInt16(accept.C021), dbContextSSFG, DateTime.Now);

                                                //lấy lại code kho đi
                                                whFrom = Convert.ToInt16(accept.C021);
                                            }
                                            #endregion

                                            GlobalVariables.AutoPostingStatus3 = AutoPostingHelper.AutoTransfer(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, barcodeString
                                               , whFrom, whTo, dbContextSSFG, DateTime.Now.AddSeconds(5));

                                            GlobalVariables.InvokeIfRequired(this, () =>
                                            {
                                                _labLastResultMessage.Text = GlobalVariables.AutoPostingStatus3;
                                                _labLastResultMessage.ForeColor = Color.Green;
                                            });
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"Thùng OC đã được quét ghi nhận khối lượng OK rồi, không được phép cân lại." +
                                            $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                        throw new Exception($"{_scanDataWeight.OcNo} - {_scanDataWeight.BoxNo} - {_unitLabel} - {_scanDataWeight.IdLabel} đã được quét ghi nhận khối lượng OK rồi.");
                                    }
                                }
                                else//thung fail
                                {
                                    //bật đèn đỏ
                                    GlobalVariables.MyEvent.StatusLightPLC = 1;
                                    GlobalVariables.MyEvent.WeightPusher = 1;//ghi xuong PLC bao reject

                                    _scanDataWeight.Pass = 0;
                                    _scanDataWeight.Status = 0;
                                    _scanDataWeight.CreatedDate = GlobalVariables.CreatedDate = DateTime.Now;//lấy thời gian để đồng bộ giữa in tem và log DB

                                    GlobalVariables.PrintApprove = false;
                                    if (_scanDataWeight.Decoration == 1)
                                    {
                                        GlobalVariables.RememberInfo.FailBoxPrinting += 1;
                                    }
                                    else
                                    {
                                        GlobalVariables.RememberInfo.FailBoxNoPrinting += 1;
                                    }

                                    //transfer from WH in comming to 965
                                    //đến đây thì chắc chắn nó đang nằm ở 1185 hoặc 1223
                                    #region Auto transfer 
                                    var res1 = AutoPostingHelper.CheckIn(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, barcodeString, dbContextSSFG);
                                    var accept = res1?.FirstOrDefault();

                                    var logLine = new tblLog
                                    {
                                        Message = $"Check Weight sp_lmpScannerClient_ScanningLabel_CheckIn =  {res1?.Count}.",
                                        MessageTemplate = barcodeString,
                                        Level = "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn",
                                        TimeStamp = DateTime.Now,
                                    };
                                    dbContextSSFG.TblLogs.Add(logLine);
                                    dbContextSSFG.SaveChanges();

                                    if (accept != null)
                                    {
                                        GlobalVariables.AutoPostingStatus3 = AutoPostingHelper.AutoTransfer(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, barcodeString
                                            , Convert.ToInt16(accept.C004), 965, dbContextSSFG, DateTime.Now);

                                        GlobalVariables.InvokeIfRequired(this, () =>
                                        {
                                            _labLastResultMessage.Text = GlobalVariables.AutoPostingStatus3;
                                            _labLastResultMessage.ForeColor = Color.Red;
                                        });
                                    }
                                    #endregion

                                    if (statusLogData == 0)
                                    {
                                        #region Log data
                                        //mỗi thùng chỉ cho log vào tối da là 2 dòng trong scanData, 1 dòng pass và fail (nếu có)
                                        //tính lại tỷ lệ khối lượng số đôi lỗi/ StdGrossWeight của lần scan này để log
                                        //_scanDataWeight.RatioFailWeight = Math.Round((Math.Abs(_scanDataWeight.DeviationPairs) * _scanDataWeight.AveWeight1Prs) / _scanDataWeight.StdGrossWeight, 3);
                                        LogDataScan(dbContextSSFG);
                                        #endregion

                                        throw new Exception("Khối lượng lỗi.");
                                    }
                                    else if (statusLogData == 1)
                                    {
                                        Debug.WriteLine($"Thùng OC đã được quét ghi nhận khối lượng lỗi rồi, không được phép cân lại." +
                                             $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information); ;

                                        throw new Exception($"{_scanDataWeight.OcNo} - {_scanDataWeight.BoxNo} - {_unitLabel} - {_scanDataWeight.IdLabel} đã được quét ghi nhận khối lượng lỗi rồi.");
                                    }
                                    else// if (statusLogData == 2)
                                    {
                                        Debug.WriteLine($"Thùng OC đã được quét ghi nhận khối lượng OK rồi, không được phép cân lại." +
                                            $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                        throw new Exception($"{_scanDataWeight.OcNo} - {_scanDataWeight.BoxNo} - {_unitLabel} - {_scanDataWeight.IdLabel} đã được quét ghi nhận khối lượng OK rồi.");
                                    }
                                }
                            }
                            //Hàng HC
                            else//hàng heelcounter thì chỉ ghi nhận khối lượng cân và in ra tem ko có check weight
                            {
                                //para = null;
                                //para = new DynamicParameters();
                                //para.Add("@Message", $"Hàng HeelCounter check weight OK.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                                //para.Add("Level", "Log");
                                //dbContext.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                                _scanDataWeight.Pass = 1;//báo thùng pass
                                _scanDataWeight.CreatedDate = GlobalVariables.CreatedDate = DateTime.Now;//lấy thời gian để đồng bộ giữa in tem và log DB Printing
                                                                                                         //bật tín hiệu để PLC on đèn xanh
                                                                                                         //GlobalVariables.MyEvent.StatusLightPLC = 2;
                                if (_scanDataWeight.Decoration == 0)
                                {
                                    GlobalVariables.RememberInfo.GoodBoxPrinting += 1;
                                }
                                else
                                {
                                    GlobalVariables.RememberInfo.GoodBoxNoPrinting += 1;
                                }

                                //kiểm tra xem data đã có trên hệ thống hay chưa
                                if (statusLogData == 0 || statusLogData == 1)
                                {
                                    //gui lenh in
                                    //var passMetal = _scanDataWeight.MetalScan == 1 && ocFirstChar != "PR" ? "Passed quality check" : " ";
                                    var passMetal = "Passed quality check";
                                    var idLabel = !string.IsNullOrEmpty(_scanDataWeight.IdLabel) ? _scanDataWeight.IdLabel : $"{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}";

                                    #region get LotNo Brooks, printing label
                                    var brooksResult = dbContextSSFG.Database
                                        .SqlQuery<string>("sp_GetLotOfBrooksHC @ocNo = {0}, @boxNo = {1}"
                                            , _scanDataWeight.OcNo, _scanDataWeight.BoxNo
                                        )
                                        .FirstOrDefault();

                                    if (brooksResult != null)
                                    {
                                        _scanDataWeight.LotNo = brooksResult;
                                    }
                                    #endregion

                                    _approvePrint = true;

                                    SendDynamicString($"{idLabel}  {passMetal}"
                                                        , $"{(_scanDataWeight.GrossWeight / 1000).ToString("#,#0.00")} Kg"
                                                        , $"{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")} {_scanDataWeight.LotNo}"
                                                      );

                                    GlobalVariables.MyEvent.StatusLightPLC = 2;
                                    //hien thi mau label

                                    GlobalVariables.InvokeIfRequired(this, () =>
                                    {
                                        _labResult.Text = "OK";
                                        _labResult.BackColor = Color.Green;
                                        _labResult.ForeColor = Color.White;
                                        _labResultMessage.Text = "Hàng heel counter OK. Không kiểm tra khối lượng.";

                                        //hiển thị cho trạng thái log
                                        _labLastResultMessage.Text = "The HC is OK. Don't check the weight.";
                                        _labLastResultMessage.ForeColor = Color.Green;
                                    });

                                    LogDataScan(dbContextSSFG);
                                }
                                else
                                {
                                    Debug.WriteLine($"Thùng HC này đã được quét ghi nhận khối lượng OK rồi, không được phép cân lại." +
                                        $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                    throw new Exception($"{_scanDataWeight.OcNo} - {_scanDataWeight.BoxNo} - {_unitLabel} - {_scanDataWeight.IdLabel} đã được quét ghi nhận khối lượng OK rồi.");
                                }
                            }
                            #endregion

                            #endregion
                        }
                        else
                        {
                            Debug.WriteLine($"Item '{_scanDataWeight.ProductNumber}' không có khối lượng/1 đôi. Xin hãy kiểm tra lại thông tin."
                                , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            throw new Exception($"Product number '{_scanDataWeight.ProductNumber}' không có khối lượng trên 1 prs/pcs. Vui lòng kiểm tra lại thông tin.");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Product number {_scanDataWeight.ProductNumber} không có trong hệ thống. Báo cho quản lý để update data mới từ winline về."
                            , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        throw new Exception($"Product item '{_scanDataWeight.ProductNumber}' chưa có trong hệ thống. Vui lòng kiểm tra lại thông tin.");
                    }
                }
                #endregion

                string json = JsonConvert.SerializeObject(GlobalVariables.RememberInfo);
                File.WriteAllText(@"./RememberInfo.json", json);
                //_readQrStatus[1] = false;//trả lại bit này để quét lần sau
            }
            catch (Exception ex)
            {
                //bật đèn đỏ
                GlobalVariables.MyEvent.StatusLightPLC = 1;
                //ghi giá trị xuống PLC seimens reject
                GlobalVariables.MyEvent.WeightPusher = 1;

                errorFlag = true;//bật biến này lên để thay đổi màu chữ thành NG cho các label message.

                //hien thi mau label
                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labResultMessage.Text = ex.Message;
                    _labResult.Text = "NG";
                    _labResult.BackColor = Color.Red;
                    _labResult.ForeColor = Color.White;
                });

                using (var dbContextSSFG = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                {
                    var rejectLine = new tblScanDataReject()
                    {
                        Id = Guid.NewGuid(),
                        IsActived = 1,
                        CreatedMachine = Environment.MachineName,
                        BarcodeString = _scanDataWeight.BarcodeString,
                        IdLabel = _scanDataWeight.IdLabel,
                        OcNo = _scanDataWeight.OcNo,
                        BoxId = _scanDataWeight.BoxNo,
                        ProductNumber = _scanDataWeight.ProductNumber,
                        ProductName = _scanDataWeight.ProductName,
                        Quantity = _scanDataWeight.Quantity,
                        ScannerStation = "Scale",
                        Reason = $"Ex:{ex.ToString()}.",
                        GrossWeight = _scanDataWeight.GrossWeight,
                        DeviationPairs = _scanDataWeight.DeviationPairs,
                        DeviationWeight = _scanDataWeight.Deviation,
                        CreatedDate = DateTime.Now,
                    };
                    dbContextSSFG.TblScanDataRejects.Add(rejectLine);

                    dbContextSSFG.SaveChanges();
                }

                Log.Error(ex.ToString(), "Lỗi scale form tại trạm scanner 2 scale.");
            }
            finally
            {
                //hien thi cac thong so dem
                ShowUI(errorFlag: errorFlag);

                _scanDataWeight = new tblScanData();
                _resetUI = true;
            }
        }

        private void BarcodeScanner3Handle(int station, string barcodeString)
        {
            try
            {
                if (_labQrDistribution.InvokeRequired)
                {
                    _labQrDistribution.Invoke(new Action(() =>
                    {
                        _labQrDistribution.Text = barcodeString;
                    }));
                }
                else
                {
                    _labQrDistribution.Text = barcodeString;
                }

                #region Xử lý data ban đầu theo QR code
                bool specialCasePrint = false;//dùng có các trường hợp hàng PU, trên WL decpration là 0, nhưng QC phân ra printing 0-1. beforePrinting thì get theo
                                              //printing=0; afterPrinting thì get theo printing=1. 6112012228

                #region xử lý barcode lấy ra các giá trị theo code
                _scanDataPrint.BarcodeString = barcodeString;
                var ocFirstCharPrint = barcodeString.Substring(0, 2);

                if (_scanDataPrint.BarcodeString.Contains("|"))
                {
                    var s = barcodeString.Split('|');
                    var s1 = s[0].Split(',');
                    _unit = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    //Check xem  QR code quét vào có đúng định dạng hay ko

                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstCharPrint);

                    if (resultCheckOc != null)
                    {
                        _scanDataPrint.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            _labResultDistribution.Text = "OC không đúng định dạng.";
                            _labResultDistribution.ForeColor = Color.Red;
                        });

                        //ghi lệnh reject do ko quet đc tem
                        GlobalVariables.MyEvent.PrintPusher = 0;
                        //_scannerIsBussy[2] = false;
                        return;
                    }

                    _scanDataPrint.ProductNumber = s1[1];
                }
                else
                {
                    var s1 = _scanDataPrint.BarcodeString.Split(',');
                    _unit = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    //Check xem  QR code quét vào có đúng định dạng hay ko
                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstCharPrint);

                    if (resultCheckOc != null)
                    {
                        _scanDataPrint.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            _labResultDistribution.Text = "OC không đúng định dạng.";
                            _labResultDistribution.ForeColor = Color.Red;
                        });

                        //ghi lệnh reject do ko quet đc tem
                        GlobalVariables.MyEvent.PrintPusher = 0;
                        //_scannerIsBussy[2] = false;
                        return;
                    }

                    //_scanDataPrint.OcNo = s1[0];
                    _scanDataPrint.ProductNumber = s1[1];
                }

                #region check special case
                foreach (var item in GlobalVariables.SpecialCaseList)
                {
                    if (_scanDataPrint.ProductNumber.Split('-')[0].Equals(item.MainItem))
                    {
                        specialCasePrint = true;

                        break;
                    }
                }
                #endregion

                #endregion
                #endregion

                using (var connection = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                {
                    var res = connection.Database.SqlQuery<ProductInfoModel>("sp_vProductItemInfoGet @ProductNumber = {0}, @SpecialCase = {1}"
                        , _scanDataPrint.BarcodeString, specialCasePrint).FirstOrDefault();

                    if (res != null)
                    {
                        var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstCharPrint && ocFirstCharPrint == "PR");

                        if (resultCheckOc != null)
                        {
                            Debug.WriteLine($"ProductNumber: {res.ProductNumber} là hàng sơn.");

                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                _labResultDistribution.Text = "Hàng đi sơn.";
                                _labResultDistribution.ForeColor = Color.Green;
                            });

                            GlobalVariables.MyEvent.PrintPusher = 1;

                            // xử lý insert RackStorage cho hàng sơn (nếu là hàng đi sơn thì vào kho 10)
                            //GlobalVariables.AutoPostingStatus = AutoPostingHelper.AutoTransfer(_scanDataPrint.ProductNumber, barcodeString, 1185, 10, dbContext);
                        }
                        else// không phải hàng sơn thì transfer vào kho 2
                        {
                            GlobalVariables.MyEvent.PrintPusher = 0;

                            // xử lý insert RackStorage cho hàng sơn (nếu là hàng đi sơn thì vào kho 2)

                            //var accept = AutoPostingHelper.CheckIn(_scanDataPrint.ProductNumber, barcodeString, dbContext).FirstOrDefault();

                            //GlobalVariables.AutoPostingStatus = AutoPostingHelper.AutoTransfer(_scanDataPrint.ProductNumber, barcodeString, 1223, 2, dbContext);

                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                _labResultDistribution.Text = "Hàng FG.";
                                _labResultDistribution.ForeColor = Color.Green;
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //hien thi mau label
                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labResultDistribution.Text = "System fail.";
                    _labResultDistribution.ForeColor = Color.Red;
                });
                Log.Error(ex.ToString(), "Lỗi scale form tại trạm scanner 3 print.");
            }
            finally
            {
                _readQrStatus[2] = false;//trả lại bit này để quét lần sau
            }
        }
        #endregion

        #region Task and other methos
        private async Task TaskTimerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        _labStatus.Text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} " +
                              $"| {GlobalVariables.UserLoginInfo.UserName} | PLC Status: {_plcConnectionState}";
                        _labDateTime.Text = $"{GlobalVariables.AppStatus}|{Application.ProductVersion}";
                    });

                    await Task.Delay(300, token); // nhịp kiểm tra, đủ nhẹ nhàng
                }
                catch (OperationCanceledException)
                {
                    // token.Cancel() => thoát vòng lặp
                    break;
                }
                catch (Exception ex)
                {
                    // Không để task chết âm thầm
                    Log.Error(ex, "TaskTimerAsync loop error.");
                    await Task.Delay(500, token); // tạm nghỉ rồi thử lại
                }
            }
        }

        private async Task TaskCheckResetUIAsync(CancellationToken token)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var lastResetAt = TimeSpan.Zero;
            var intervalSeconds = GlobalVariables.ConfigJson.ResetUiInterval; // ví dụ: 2 giây

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_resetUI)
                    {
                        var now = sw.Elapsed;
                        var canReset = (now - lastResetAt).TotalSeconds >= intervalSeconds;

                        if (canReset)
                        {
                            // Chuyển về UI thread để reset control
                            if (this.IsHandleCreated && !this.IsDisposed)
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        ResetControl(); // đảm bảo hàm này không ném exception
                                    }
                                    catch (Exception ex)
                                    {
                                        // Log nếu cần
                                        Log.Error(ex, "ResetControl error.");
                                    }
                                }));
                            }

                            lastResetAt = now;
                            _resetUI = false; // tiêu thụ yêu cầu reset
                        }
                        else
                        {
                            // Vẫn trong thời gian chặn reset, bỏ qua lần này
                            //_resetUI = false; // tuỳ: nếu muốn giữ yêu cầu, đừng reset flag
                        }
                    }
                    else
                    {
                        lastResetAt = sw.Elapsed;
                    }

                    await Task.Delay(200, token); // nhịp kiểm tra, đủ nhẹ nhàng
                }
                catch (OperationCanceledException)
                {
                    // token.Cancel() => thoát vòng lặp
                    break;
                }
                catch (Exception ex)
                {
                    // Không để task chết âm thầm
                    Log.Error(ex, "TaskCheckResetUIAsync loop error.");
                    await Task.Delay(500, token); // tạm nghỉ rồi thử lại
                }
            }
        }

        private async Task TaskScaleAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        _labStatus.Text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} " +
                              $"| {GlobalVariables.UserLoginInfo.UserName} | PLC Status: {_plcConnectionState}";
                        _labDateTime.Text = $"{GlobalVariables.AppStatus}|{Application.ProductVersion}";
                    });

                    await Task.Delay(200, token); // nhịp kiểm tra, đủ nhẹ nhàng
                }
                catch (OperationCanceledException)
                {
                    // token.Cancel() => thoát vòng lặp
                    break;
                }
                catch (Exception ex)
                {
                    // Không để task chết âm thầm
                    Log.Error(ex, "TaskTimerAsync loop error.");
                    await Task.Delay(500, token); // tạm nghỉ rồi thử lại
                }
            }
        }
        private async Task TaskMetalAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        _labStatus.Text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} " +
                              $"| {GlobalVariables.UserLoginInfo.UserName} | PLC Status: {_plcConnectionState}";
                        _labDateTime.Text = $"{GlobalVariables.AppStatus}|{Application.ProductVersion}";
                    });

                    await Task.Delay(200, token); // nhịp kiểm tra, đủ nhẹ nhàng
                }
                catch (OperationCanceledException)
                {
                    // token.Cancel() => thoát vòng lặp
                    break;
                }
                catch (Exception ex)
                {
                    // Không để task chết âm thầm
                    Log.Error(ex, "TaskTimerAsync loop error.");
                    await Task.Delay(500, token); // tạm nghỉ rồi thử lại
                }
            }
        }
        #endregion

        private void LogDataScan(ApplicationDbContextSSFG dbContext)
        {
            var scanLine = new tblScanData()
            {
                Id = Guid.NewGuid(),
                CreatedDate = _scanDataWeight.CreatedDate,
                CreatedBy = _scanDataWeight.CreatedBy,
                Actived = 1,
                BarcodeString = _scanDataWeight.BarcodeString,
                IdLabel = _scanDataWeight.IdLabel,
                OcNo = _scanDataWeight.OcNo,
                ProductNumber = _scanDataWeight.ProductNumber,
                ProductName = _scanDataWeight.ProductName,
                Quantity = _scanDataWeight.Quantity,
                LinePosNo = _scanDataWeight.LinePosNo,
                Unit = _scanDataWeight.Unit,
                BoxNo = _scanDataWeight.BoxNo,
                CustomerNo = _scanDataWeight.CustomerNo,
                Location = _scanDataWeight.Location,
                BoxPosNo = _scanDataWeight.BoxPosNo,
                Note = _scanDataWeight.Note,
                Brand = _scanDataWeight.Brand,
                Decoration = _scanDataWeight.Decoration,
                MetalScan = _scanDataWeight.MetalScan,
                ActualMetalScan = _scanDataWeight.ActualMetalScan,
                AveWeight1Prs = _scanDataWeight.AveWeight1Prs,
                StdNetWeight = _scanDataWeight.StdNetWeight,
                LowerTolerance = _scanDataWeight.LowerTolerance,
                UpperTolerance = _scanDataWeight.UpperTolerance,
                BoxWeight = _scanDataWeight.BoxWeight,
                PackageWeight = _scanDataWeight.PackageWeight,
                StdGrossWeight = _scanDataWeight.StdGrossWeight,
                GrossWeight = _scanDataWeight.GrossWeight,
                NetWeight = _scanDataWeight.NetWeight,
                Deviation = _scanDataWeight.Deviation,
                Pass = _scanDataWeight.Pass,
                Status = _scanDataWeight.Status,
                CalculatedPairs = _scanDataWeight.CalculatedPairs,
                DeviationPairs = _scanDataWeight.DeviationPairs,
                ApprovedBy = _scanDataWeight.ApprovedBy,
                ActualDeviationPairs = _scanDataWeight.ActualDeviationPairs,
                RatioFailWeight = _scanDataWeight.RatioFailWeight,
                ProductCategory = _scanDataWeight.ProductCategory,
                LotNo = _scanDataWeight.LotNo,
                Station = _scanDataWeight.Station
            };

            dbContext.TblScanDatas.Add(scanLine);
            dbContext.SaveChanges();
        }

        private async void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.IsUpdateAvailable)
            {
                DialogResult dialogResult;
                dialogResult =
                        MessageBox.Show(
                            $@"SSFG App có phiên bản mới {args.CurrentVersion}. SSFG App bản hiện tại là {args.InstalledVersion}. Bạn có muốn lên phiên bản mới không?", @"Thông Báo",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                if (dialogResult.Equals(DialogResult.Yes) || dialogResult.Equals(DialogResult.OK))
                {
                    SplashScreenManager.ShowForm(typeof(WaitForm1));
                    await System.Threading.Tasks.Task.Delay(3000);
                    //AutoZipFolder();

                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                        {
                            SplashScreenManager.CloseForm(false);
                            Application.Exit();
                        }
                        else
                        {
                            SplashScreenManager.ShowForm(typeof(WaitForm1));
                        }
                    }
                    catch (Exception exception)
                    {
                        SplashScreenManager.CloseForm(false);
                        MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                if (isUpdateClicked)
                {
                    MessageBox.Show(@"SSFG App đang chạy phiên bản mới nhất.", @"Thông Báo",
                   MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ShowUI(bool errorFlag = false)
        {
            #region hien thi cac thong so dem
            this.Invoke((MethodInvoker)delegate
            {
                #region Standard
                labRealWeight.Text = _scanDataWeight.GrossWeight.ToString();
                labNetWeight.Text = _scanDataWeight.StdNetWeight.ToString();
                _labBoxId.Text = _scanDataWeight.BoxNo;
                labOcNo.Text = _scanDataWeight.OcNo.Trim();
                labProductCode.Text = _scanDataWeight.ProductNumber;
                labProductName.Text = _scanDataWeight.ProductName;
                labQuantity.Text = _scanDataWeight.Quantity.ToString();
                labColor.Text = _color;
                labSize.Text = _sizeName;
                labAveWeight.Text = _scanDataWeight.AveWeight1Prs.ToString();
                labLowerTolerance.Text = _scanDataWeight.LowerTolerance.ToString();
                labUpperTolerance.Text = _scanDataWeight.UpperTolerance.ToString();
                labBoxWeight.Text = _scanDataWeight.BoxWeight.ToString();
                labAccessoriesWeight.Text = _scanDataWeight.PackageWeight.ToString();
                labGrossWeight.Text = _scanDataWeight.StdGrossWeight.ToString();
                _labCheckMetal.Text = _scanDataWeight.MetalScan == 0 ? "NO" : "YES";
                _labPrinting.Text = _scanDataWeight.Decoration == 0 ? "NO" : "YES";

                _labQtyStandard.Text = $"Quantity ({_unitLabel})";
                _labUnitCalculatQty.Text = $"Calculated Qty ({_unitLabel})";
                _labUnitDeviation.Text = $"Deviation ({_unitLabel})";

                _labUnitStandard.Text = _unitLabel;
                _labFGW.Text = $"Weight (g)/{_unitLabel}";

                _labBoxType.Text = _boxType.ToString();
                _labLableId.Text = _scanDataWeight.IdLabel;
                #endregion

                #region Scaled

                labNetRealWeight.Text = $"{_scanDataWeight.NetWeight}";
                labDeviation.Text = $"{_scanDataWeight.Deviation}";
                labCalculatedPairs.Text = _scanDataWeight.CalculatedPairs.ToString();
                labDeviationPairs.Text = _scanDataWeight.DeviationPairs.ToString();

                labDeviation.ForeColor = errorFlag == false ? Color.Green : Color.Red;
                _labResultMessage.ForeColor = errorFlag == false ? Color.Green : Color.Red;
                labDeviationPairs.ForeColor = errorFlag == false ? Color.Green : Color.Red;
                #endregion

                errorFlag = false;
            });
            #endregion
        }

        private void ResetControl()
        {
            GlobalVariables.InvokeIfRequired(this, () =>
            {
                _unitLabel = string.Empty;
                _color = _sizeName = string.Empty;

                _labLastResultMessage.Text = _labResultMessage.Text;
                _labLastResultMessage.ForeColor = _labResultMessage.ForeColor;

                #region Standard
                _labBoxId.Text = string.Empty;
                labOcNo.Text = string.Empty;
                labProductCode.Text = string.Empty;
                labProductName.Text = string.Empty;
                labQuantity.Text = "0";
                labColor.Text = string.Empty;
                labSize.Text = string.Empty;
                labAveWeight.Text = "0";
                labLowerTolerance.Text = "0";
                labUpperTolerance.Text = "0";
                labBoxWeight.Text = "0";
                labAccessoriesWeight.Text = "0";
                labGrossWeight.Text = "0";
                _labCheckMetal.Text = "NO";
                _labPrinting.Text = "NO";

                _labQtyStandard.Text = $"Quantity (-)";
                _labUnitCalculatQty.Text = $"Calculated Qty (-)";
                _labUnitDeviation.Text = $"Quantity (-)";

                _labUnitStandard.Text = string.Empty;
                _labFGW.Text = $"Weight (g)/-";

                _labBoxType.Text = null;
                _labLableId.Text = string.Empty;
                #endregion

                #region Scaled
                labRealWeight.Text = "0";
                labNetWeight.Text = "0";

                labNetRealWeight.Text = "0";
                labDeviation.Text = "0";

                labCalculatedPairs.Text = "0";
                labDeviationPairs.Text = "0";

                _labResultMessage.Text = string.Empty;
                _labResult.Text = string.Empty;
                _labResult.BackColor = Color.Gray;

                labDeviation.ForeColor = default;
                labDeviationPairs.ForeColor = default;
                #endregion
            });
        }
    }
}