using AutoUpdaterDotNET;
using CognexLibrary_NETFramework;
using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using DevExpress.XtraSpreadsheet.Model;
using Newtonsoft.Json;
using Serilog;
using Snap7ClientLib.Core;
using Snap7ClientLib.Historian;
using Snap7ClientLib.Tags;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        private Label titleText;

        private ScaleHelper _scaleHelper;
        private Task _ckTask, _ckQRTask, _ckQrWeightScanTask;//task kiểm tra tại các trạm scanner để check xem có đoc đc QR code ko
        private bool _isStartCountTimer = false;
        private bool _isStartCountTimerWeight = false;//guard chống spawn trùng CheckReadQrWeight khi sensor before weight scan bắn lại nhiều lần cho cùng 1 thùng
        private int _metalScannerStatus = 0;

        private bool[] _readQrStatus = { false, false };//biến báo đọc được QR hay không. metal-weight

        private int _stableScale = 0;//biến báo trạng thái cân ổn định, get khối lượng cân về
        private double _scaleValue = 0;//biến chứa giá trị cân realTime đọc từ đầu cân về
        private double _scaleValueStable = 0;//biến chứa giá trị cân ổn định được đọc về khi biến stable báo on
        private int _metalCheckResult = 0;//biến chứa giá trị metalCheck 

        //tạo các biến để lưu giá trị theo QR code tại từng trạm
        //private tblScanDataModel _scanData = new tblScanDataModel();
        private tblScanData _scanDataMetal = new tblScanData();
        private tblScanData _scanDataWeight = new tblScanData();

        private string _idLabel = null;
        private string _plr = null;// kiểu đóng thùng, P-đôi; L/R-left righ
        private double _weight = 0, _boxWeight = 0, _accessoriesWeight = 0;

        private bool _approveUpdateActMetalScan = false;

        private string _barcodeString1 = null, _barcodeString2 = null;//checkMetal--checkWeight
        private bool[] _scannerIsBussy = { false, false };

        private AnserU2TcpDriver _printerDriver;

        private bool _firstLoad = true;

        private bool _approvePrint = false;// lệnh cho phép in hay không, chỉ khi nào pass cân thì active lên cho in.

        //20250510 upgrade system to use scanner cogned DM290-X at station check weight
        private static CognexLibrary_NETFramework.DriverTelnet _driverTelnet = new CognexLibrary_NETFramework.DriverTelnet();
        //Metal station scanner: migrated from Zebra CoreScanner SDK to Cognex Telnet (same pattern as the scale station).
        private CognexLibrary_NETFramework.DriverTelnet _driverTelnetMetal = new CognexLibrary_NETFramework.DriverTelnet();

        private bool isUpdateClicked = false;
        private System.Threading.Tasks.Task _tskModbus, _tskProfinet;

        private bool _resetCounter = false;

        string _stationReport = "All";

        int _metalScan = 0, _metalPusher = 0, _weightPusher = 0;

        private CancellationTokenSource _readProfinet;
        private Task _readProfinetTask;

        private CancellationTokenSource _timer;
        private Task _timerTask;

        private CancellationTokenSource _resetUiCts;
        private Task _resetUiTask;

        private EnumBoxType _boxType;

        private bool _resetUI = false;

        private string _unitLabel = string.Empty;
        private string _color = string.Empty;
        private string _sizeName = string.Empty;

        //Dung thu vien S7 moi
        //khai báo kết nối PLC S7
        PlcManager _manager = new PlcManager();
        PlcRuntime _plcRuntime;
        PlcClient _plc1Client;
        PlcSubscriptionManager _sub;
        SqliteHistorian _historian;

        private PlcConnectionState _plcConnectionState;

        //private CancellationTokenSource _inspectionMetalCts;
        //private Task _triggerInspectionMetal;
        private readonly AsyncAutoResetEvent _triggerInspectionMetal = new AsyncAutoResetEvent();
        private CancellationTokenSource _inspectionMetalCts;

        //private CancellationTokenSource _inspectionWeightCts;
        //private Task _triggerInspectionWeight;
        private readonly AsyncAutoResetEvent _triggerInspectionWeight = new AsyncAutoResetEvent();
        private CancellationTokenSource _inspectionWeightCts;

        private string _version = string.Empty;
        private MesoInfoModel _mesoinfo = new MesoInfoModel();
        private int _rj1;
        private int _checkWeightResult;

        /// <summary>
        /// giá trị boxWeightQR này sẽ lấy từ QR code trên thùng, nếu ko có thông tin này thì báo reject.
        /// </summary>
        private double _boxWeightQR = 0;
        private EnumBoxType? _boxTypeQR;

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
            titleText = new Label();
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
            using var dbContext = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString);
            _mesoinfo = dbContext.Database.SqlQuery<MesoInfoModel>($"sp_GetMesoInfo").AsEnumerable().FirstOrDefault();

            var location = _mesoinfo.MESOCOMP == "VNT1" ? "fVN" :
                          _mesoinfo.MESOCOMP == "FKV" ? "fKV" :
                          _mesoinfo.MESOCOMP == "FTT1" ? "fFT" :
                          _mesoinfo.MESOCOMP == "05FI" ? "fIN" :
                          _mesoinfo.MESOCOMP == "fGE" ? "fGE" : "Unknown";

            if (Enum.TryParse<EnumLocation>(location, ignoreCase: true, out var loc))
            {
                titleText.Text = $"{loc} - SSFG Station";
            }
            _version = System.Windows.Forms.Application.ProductVersion.Split('+')[0];

            #region Test get LotNo Brooks
            //using (var dbContext = GlobalVariables.GetDbConnection())
            //{
            //    var para = new DynamicParameters();
            //    para.Add("ocNo", "DTOTEST002");
            //    para.Add("boxNo", "1/1");

            //    var reader = dbContext.ExecuteReader("sp_GetLotOfBrooksHC", param: para, commandType: CommandType.StoredProcedure);
            //    DataTable tableResult = new DataTable();
            //    tableResult.Load(reader);

            //    if (tableResult.Rows.Count > 0)
            //    {
            //        _scanDataWeight.LotNo = tableResult.Rows[0]["LotNo"].ToString();
            //    }
            //}
            #endregion

            #region đăng ký sự kiện từ cac PLC
            //sự kiện lấy số cân hiện tại của đầu cân (real time)
            GlobalVariables.MyEvent.EventHandleScaleValue += (s, o) =>
            {
                //Debug.WriteLine($"Event Scale value real time: {o.ScaleValue}");
                _scaleValue = o.ScaleValue;

                if (labScaleValue.InvokeRequired)
                {
                    labScaleValue.Invoke(new Action(() =>
                    {
                        labScaleValue.Text = _scaleValue.ToString();
                    }));
                }
                else
                {
                    labScaleValue.Text = _scaleValue.ToString();
                }
            };
            //sự kiến lấy khối lượng cân đã chốt ổn định
            GlobalVariables.MyEvent.EventHandlerScaleValueStable += (s, o) =>
            {
                //Debug.WriteLine($"Event Scale value stable: {o.ScaleValue}");

                _scaleValueStable = o.ScaleValue;

                GlobalVariables.RealWeight = _scaleValueStable;

                //this?.Invoke((MethodInvoker)delegate { labScaleValue.Text = _scanData.GrossWeight.ToString(); });
            };

            //sự kiện báo cân đã ổn định, chốt số cân.
            GlobalVariables.MyEvent.EventHandlerStableScale += (s, o) =>
            {
                Debug.WriteLine($"Event Scale stable: {o.NewValue}");
                _stableScale = o.NewValue;
            };

            //sự kiện ghi nhận thừng đêbs trước vị trí metalScan, lấy cánh xuống để tác động tính thời gian để báo ko đọc đc QR code
            GlobalVariables.MyEvent.EventHandleSensorBeforeMetalScan += (s, o) =>
            {
                Debug.WriteLine($"Event Sensor before metal scan passed the wait loop: {o.NewValue} |{_isStartCountTimer}");
                //chạy task đếm thời gian cho việc quét tem, hết thời gian mà chưa nhận đc tín hiệu từ metal scanner
                //thì ghi tín hiêu xuống PLC conveyor để reject với lý do là không đọc đc QR
                if (_isStartCountTimer == false)
                {
                    if (o.NewValue == 0)
                    {
                        _isStartCountTimer = true;
                        _ckQRTask = new Task(() => CheckReadQr((int)(GlobalVariables.ConfigJson.TimerCheckQrMetal)));
                        _ckQRTask.Start();
                    }
                }

                if (o.NewValue == 1)
                {
                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        _labQrIdentification.Text = string.Empty;
                        _labResultIdentification.Text = string.Empty;
                    });
                }
            };

            GlobalVariables.MyEvent.EventHandleSensorBeforeWeightScan += (s, o) =>
            {
                Debug.WriteLine($"Event Sensor before weight scan: {o.NewValue}|{_isStartCountTimerWeight}");
                //chạy task đếm thời gian cho việc quét tem, hết thời gian mà chưa nhận đc tín hiệu từ Scanner cognex
                //thì ghi tín hiêu xuống PLC conveyor để reject với lý do là không đọc đc QR
                //guard bằng _isStartCountTimerWeight (cùng pattern với _isStartCountTimer của trạm metal) để tránh
                //spawn nhiều CheckReadQrWeight task chồng lấn khi tag sensor bắn lại nhiều lần cho cùng 1 thùng
                if (o.NewValue == 1 && _isStartCountTimerWeight == false)
                {
                    _isStartCountTimerWeight = true;

                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        labQrScale.Text = string.Empty;
                        _labLastResultMessage.Text = string.Empty;
                    });

                    _ckQrWeightScanTask = new Task(() => CheckReadQrWeight());
                    _ckQrWeightScanTask.Start();

                    //reset các control để qua cân mẻ mới
                    ResetControl();
                }
            };

            //khi thùng đụng cảm biến out của cân thì reset biến báo bận cho scanner trạm cân quét tiếp
            GlobalVariables.MyEvent.EventHandleSensorAfterWeightScan += (s, o) =>
            {
                if (o.NewValue == 1)
                {
                    //reset biến báo bận cho scanner trạm cân quét tiếp
                    _scannerIsBussy[1] = false;
                    //tắt biến báo đọc đc QR code từ label
                    _readQrStatus[1] = false;
                    //mở lại guard để cho phép CheckReadQrWeight chạy cho thùng kế tiếp
                    _isStartCountTimerWeight = false;
                }

                Debug.WriteLine($"Event Sensor after scale: {o.NewValue}|ScannerBussy{_scannerIsBussy[1]}");
            };

            GlobalVariables.MyEvent.EventHandleSensorMiddleMetal += (s, o) =>
            {
                if (o.NewValue == 1)
                {
                    //xáo báo bận để cho phép scanner quét tiếp thùng.
                    _scannerIsBussy[0] = false;

                    _isStartCountTimer = false;
                    //GlobalVariables.MyEvent.MetalPusher = _metalScannerStatus;
                }

                Debug.WriteLine($"Sensor middle metal: {o.NewValue}|ScannerBussy{_scannerIsBussy[0]}");
            };

            //sự kiện tính toán chốt cập nhật trạng thái cho hàng có kiểm tra kim loại hay là không.
            GlobalVariables.MyEvent.EventHandleSensorAfterMetalScan += (s, o) =>
            {
                GlobalVariables.AutoPostingStatus2 = string.Empty;
                Debug.WriteLine($"Event Sensor After metal scan: {o.NewValue}");
                GlobalVariables.RememberInfo.CountMetalScan += 1;//đếm số thùng đi qua máy metalScan

                if (o.NewValue == 1)
                {
                    using (var dbContext = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                    {
                        var para = new DynamicParameters();
                        if (_metalCheckResult == 1)//Check metal fail
                        {
                            GlobalVariables.MyEvent.MetalPusher1 = 1;

                            ////log vao bang reject
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
                                ProductName = _scanDataMetal.ProductName,
                                ProductNumber = _scanDataMetal.ProductNumber,
                                Quantity = _scanDataMetal.Quantity,
                                ScannerStation = EnumStation.Identification.ToString(),
                                Reason = "Metal checking failure.",
                                GrossWeight = _scanDataMetal.GrossWeight,
                                DeviationPairs = _scanDataMetal.DeviationPairs,
                                DeviationWeight = _scanDataMetal.Deviation
                            };
                            dbContext.TblScanDataRejects.Add(rejectLine);

                            //transfer from WH in comming to 964
                            #region Auto Stock In to 1223 if Box come to QC
                            //kiểm tra thùng hàng ko có trong kho production hand ove WH (1185) là cho stock in vao kho QC hand over WH (1223)

                            var res1 = AutoPostingHelper.CheckIn(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, _scanDataMetal.BarcodeString, dbContext);
                            var accept = res1?.FirstOrDefault();

                            var logNl = new tblLog()
                            {
                                Message = $"Metal sp_lmpScannerClient_ScanningLabel_CheckIn = {res1?.Count}.",
                                MessageTemplate = $"{_scanDataMetal.BarcodeString}",
                                Level = "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn",
                                Exception = null,
                                TimeStamp = DateTime.Now
                            };
                            dbContext.TblLogs.Add(logNl);

                            if (accept != null)
                            {
                                logNl = new tblLog()
                                {
                                    Message = $"Metal checking failure.",
                                    Level = "Auto post metal.",
                                    Exception = null,
                                    TimeStamp = DateTime.Now
                                };
                                dbContext.TblLogs.Add(logNl);

                                GlobalVariables.AutoPostingStatus2 = AutoPostingHelper.AutoTransfer(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, _scanDataMetal.BarcodeString
                                    , Convert.ToInt16(accept.C004), 964, dbContext, DateTime.Now);

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labResultIdentification.Text = $"{GlobalVariables.AutoPostingStatus2}";
                                    _labResultIdentification.ForeColor = Color.Red;
                                });
                            }
                            #endregion
                        }
                        else
                        {
                            //với fIN thì PLC tự điều khiển pusher theo tín hiệu Metal_Result.
                            //GlobalVariables.MyEvent.MetalPusher1 = 0;

                            ////transfer from WH 964 to WH in comming
                            /////kiểm tra xem có trong 964 ko? nếu có thì mới transfer. không có thì ko làm gì cả
                            #region Auto Stock In to 1223 if Box come to QC
                            //kiểm tra thùng hàng ko có trong kho production hand ove WH (1185) là cho stock in vao kho QC hand over WH (1223)
                            var res1 = AutoPostingHelper.CheckIn(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, _scanDataMetal.BarcodeString, dbContext);
                            var accept = res1?.FirstOrDefault(x => x.C004 == "964");

                            var logNl = new tblLog()
                            {
                                Message = $"Metal sp_lmpScannerClient_ScanningLabel_CheckIn = {res1?.Count}.",
                                MessageTemplate = $"{_scanDataMetal.BarcodeString}",
                                Level = "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn",
                                Exception = null,
                                TimeStamp = DateTime.Now
                            };
                            dbContext.TblLogs.Add(logNl);

                            if (accept != null)
                            {
                                logNl = new tblLog()
                                {
                                    Message = $"Check metal OK.",
                                    Level = "Auto post metal.",
                                    Exception = null,
                                    TimeStamp = DateTime.Now
                                };
                                dbContext.TblLogs.Add(logNl);

                                GlobalVariables.AutoPostingStatus2 = AutoPostingHelper.AutoTransfer(GlobalVariables.ConfigJson.FlagAutoPost, _scanDataMetal.ProductNumber, _scanDataMetal.BarcodeString
                                    , Convert.ToInt16(accept.C004), Convert.ToInt16(accept.C021), dbContext, DateTime.Now);

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labResultIdentification.Text = $"{GlobalVariables.AutoPostingStatus2}";
                                    _labResultIdentification.ForeColor = Color.Green;
                                });
                            }
                            #endregion
                        }

                        //log gia thông tin check metal vào bảng tblMetalScanResult
                        var metalScanLine = new tblMetalScanResult()
                        {
                            Id = Guid.NewGuid(),
                            IsActived = 1,
                            CreatedDate = DateTime.Now,
                            CreatedMachine = Environment.MachineName,
                            BarcodeString = _scanDataMetal.BarcodeString,
                            ProductItemCode = _scanDataMetal.ProductNumber,
                            IdLabel = _scanDataMetal.IdLabel,
                            Oc = _scanDataMetal.OcNo,
                            BoxNo = _scanDataMetal.BoxNo,
                            Qty = _scanDataMetal.Quantity,
                            MetalCheckResult = _metalCheckResult == 0 ? false : true
                        };
                        dbContext.TblMetalScanResults.Add(metalScanLine);

                        dbContext.SaveChanges();//lưu các thay đổi vào database
                    }
                }
            };

            GlobalVariables.MyEvent.EventHandleMetalCheckResult += (s, o) =>
            {
                _metalCheckResult = o.NewValue;
                Debug.WriteLine($"Event Metal check result: {o.NewValue}");
            };
            #endregion

            #region Connect to PL Seimens
            #region đăng ký các sự kiện ghi giá trị xuống PLC seimens để điều khiển các pusher
            //vùng nhớ dataBlock 1(DB1.DB1 byte). weight pusher
            GlobalVariables.MyEvent.EventHandlerWeightPusher += (s, o) =>
            {
                if (o.NewValue != 0)
                {
                    //GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.GhiDB(1, 1, 1, new byte[] { 1 });
                    Debug.WriteLine($"Event ghi DB weight pusher {o.NewValue}. status {_plcConnectionState}");
                    //GlobalVariables.MyEvent.WeightPusher = 0;
                    WriteData2PlcSeimens("Check_Weight_Result", o.NewValue);
                }
                else
                {
                    Debug.WriteLine($"Event ghi DB weight pusher {o.NewValue}. status {_plcConnectionState}");
                }
            };

            GlobalVariables.MyEvent.EventHandlerMetalPusher += (s, o) =>
            {
                if (o.NewValue != 0)
                {
                    //GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.GhiDB(1, 1, 1, new byte[] { 1 });
                    Debug.WriteLine($"Event ghi DB identity {o.NewValue}. status {_plcConnectionState}");
                    //GlobalVariables.MyEvent.WeightPusher = 0;
                    WriteData2PlcSeimens("RJ1", o.NewValue);
                }
                else
                {
                    Debug.WriteLine($"Event ghi DB weight pusher {o.NewValue}. status {_plcConnectionState}");
                }
            };

            #endregion

            _manager.LoadFromConfig("tags.json");
            //_manager.LoadFromConfig("tags1.json");

            //tùy vào hệ thống kết nối bao nhiêu PLC để gọi kết nối đến PLC tương ứng
            _plcRuntime = _manager.GetPlc("PLC_1");
            _plc1Client = _plcRuntime.Client; // Lưu vào biến toàn cục

            // ĐĂNG KÝ SỰ KIỆN TRƯỚC KHI KẾT NỐI
            _plc1Client.StateChanged += Client_StateChanged;

            await _plcRuntime.Reader.ReadGroupAsync(_plcRuntime.Tags);

            // 1) Tạo và gán handler
            _sub = new PlcSubscriptionManager(_plcRuntime.Reader);
            _sub.OnValueChanged += Sub_OnValueChanged;

            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "S1").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                GlobalVariables.MyEvent.SensorBeforeMetalScan = Convert.ToInt16(tag.NewValue);
            };

            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "S_MD_OUT").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                GlobalVariables.MyEvent.SensorAfterMetalScan = Convert.ToInt16(tag.NewValue);
            };

            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "S2").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                GlobalVariables.MyEvent.SensorMiddleMetal = Convert.ToInt16(tag.NewValue);
            };

            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "Metal_Result").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                GlobalVariables.MyEvent.MetalCheckResult = Convert.ToInt16(tag.NewValue);
            };

            //20260707 migrated off Modbus RTU (PLC Delta) - scale value/stable/sensors now come from the Siemens S7 tags below.
            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "Scale_Value").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                GlobalVariables.MyEvent.ScaleValue = Convert.ToDouble(tag.NewValue);
            };

            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "Scale_Value_Stable").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                GlobalVariables.MyEvent.ScaleValueStable = Convert.ToDouble(tag.NewValue);
            };

            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "Scale_Stable_Trigger").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                GlobalVariables.MyEvent.StableScale = Convert.ToInt16(tag.NewValue);
            };

            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "S5").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                GlobalVariables.MyEvent.SensorBeforeWeightScan = Convert.ToInt16(tag.NewValue);
            };

            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "S6").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                GlobalVariables.MyEvent.SensorAfterWeightScan = Convert.ToInt16(tag.NewValue);
            };

            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "Delay_Time_To_Print").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                GlobalVariables.D506Value = Convert.ToInt32(tag.NewValue);
            };

            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "Check_Weight_Result").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                _checkWeightResult = Convert.ToInt32(tag.NewValue);
            };

            _plcRuntime.Tags.FirstOrDefault(t => t.Name == "RJ1").ValueChanged += (tag) =>
            {
                Debug.WriteLine($"{DateTime.Now:O} [{tag.Name}] {tag.LastValue} -> {tag.NewValue} ({tag.DataType}) -> Deadband:{tag.Deadband}");

                _rj1 = Convert.ToInt32(tag.NewValue);
            };

            // 2) Kết nối PLC, chạy Polling
            await _plc1Client.ConnectAsync();
            _plc1Client.StartWatchdog(2000);

            // 3) (Tuỳ chọn) cập nhật UI lần đầu
            foreach (var tag in _plcRuntime.Tags)
                tag.RaiseValueChanged();//Tự "bắn" sự kiện để UI cập nhật ngay giá trị ban đầu

            // 4) BẮT ĐẦU POLLING (rất quan trọng)
            _sub.Subscribe(_plcRuntime.Tags, intervalMs: 200);
            #endregion

            //Khởi tạo máy in AnserU2 Smart one (TCP)
            if (!GlobalVariables.ConfigJson.IsTest)
            {
                PrinterOpen();
                Thread.Sleep(10000);
                SendDynamicString(" ", " ", " ");
            }

            if (!GlobalVariables.ConfigJson.IsTest)
            {
                #region 20250310 update to use scanner Cognex
                _driverTelnet.HostName = GlobalVariables.ConfigJson.IpCognexCamScale;
                //_driverTelnet.Port = 23;

                _driverTelnet.DataEvent.EventHandleValueChange += DataEvent_EventHandleValueChange;
                _driverTelnet.DataEvent.EventHandleStatusChange += DataEvent_EventHandleStatusChange;

                if (!GlobalVariables.ConfigJson.IsTest)
                    _driverTelnet.ConnectDevices();

                #endregion

                #region 20260707 Metal station scanner migrated from Zebra CoreScanner SDK to Cognex Telnet (same driver, second instance)
                _driverTelnetMetal.HostName = GlobalVariables.ConfigJson.IpCognexCamMetal;

                _driverTelnetMetal.DataEvent.EventHandleValueChange += DataEventMetal_EventHandleValueChange;
                _driverTelnetMetal.DataEvent.EventHandleStatusChange += DataEventMetal_EventHandleStatusChange;

                if (!GlobalVariables.ConfigJson.IsTest)
                    _driverTelnetMetal.ConnectDevices();

                #endregion
            }

            this.ActiveControl = null;
            this.ActiveControl = _labResult;

            GlobalVariables.AppStatus = "READY";

            //tạo 1 task chạy độc lập để get data từ Hydra
            _timer = new CancellationTokenSource();
            _timerTask = Task.Run(() => TaskTimerAsync(_timer.Token));

            _resetUiCts = new CancellationTokenSource();
            _resetUiTask = Task.Run(() => TaskCheckResetUIAsync(_resetUiCts.Token));

            ResetControl();

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

            _labResultMessage.Text = string.Empty;
            _labQrIdentification.Text = string.Empty;
            _labResultIdentification.Text = string.Empty;
        }

        private void Sub_OnValueChanged(PlcTag obj)
        {
            Debug.WriteLine($"{obj.Name} = {obj.NewValue}");
        }

        private void Client_StateChanged(PlcConnectionState obj)
        {
            _plcConnectionState = obj;
            Debug.WriteLine($"S7 Client Statuc: {_plcConnectionState}");
        }

        private void DataEvent_EventHandleStatusChange(object sender, StatusChangeEventArgs e)
        {
            GlobalVariables.CognexCam_2Status = e.Status;
            Debug.WriteLine($"[{DateTime.Now}]: {e.Status}|{e.Exception?.Message}");
        }

        private void DataEvent_EventHandleValueChange(object sender, ValueChangeEventArgs e)
        {
            Debug.WriteLine($"[{DateTime.Now}]: {e.NewValue}|{e.OldValue}");

            if (TryParseBoxInfoQr(e.NewValue, out _, out _))
            {
                Debug.WriteLine($"Box info QR ignored at Scale sensor handler (handled separately in BarcodeScanner2Handle): {e.NewValue}");
                return;
            }

            if (!_scannerIsBussy[1])
            {
                GlobalVariables.AutoPostingStatus3 = string.Empty;

                //bật biến báo bận lên ko cho scan tiếp, chặn trường hợp thùng dán 2 tem.
                _scannerIsBussy[1] = true;

                //bật biến báo đọc đc QR code từ label
                _readQrStatus[1] = true;

                _barcodeString2 = e.NewValue;

                BarcodeScanner2Handle(2, _barcodeString2);

                using (var dbContext = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                {
                    var nl = new tblLog
                    {
                        Message = $"After2 1|Barcode Id Cognex|{_barcodeString2}",
                        Level = "Scanner trigger.",
                        TimeStamp = DateTime.Now,
                    };

                    dbContext.TblLogs.Add(nl);
                    dbContext.SaveChanges();
                }
            }
        }


        private void DataEventMetal_EventHandleStatusChange(object sender, StatusChangeEventArgs e)
        {
            GlobalVariables.CognexCam_1Status = e.Status;
            Debug.WriteLine($"[{DateTime.Now}] Metal Cognex: {e.Status}|{e.Exception?.Message}");
        }

        private void DataEventMetal_EventHandleValueChange(object sender, ValueChangeEventArgs e)
        {
            Debug.WriteLine($"[{DateTime.Now}] Metal Cognex: {e.NewValue}|{e.OldValue}");

            if (TryParseBoxInfoQr(e.NewValue, out _, out _))
            {
                Debug.WriteLine($"Box info QR ignored at Metal station: {e.NewValue}");
                return;
            }

            if (!_scannerIsBussy[0])
            {
                //bật biến báo bận lên ko cho scan tiếp, chặn trường hợp thùng dán 2 tem.
                _scannerIsBussy[0] = true;

                //bật biến báo đọc đc QR code từ label
                _readQrStatus[0] = true;

                _barcodeString1 = e.NewValue;

                //reset model;
                _scanDataMetal = null;
                _scanDataMetal = new tblScanData();

                BarcodeScanner1Handle(1, _barcodeString1);
            }
            else
            {
                Log.Error("The sensor clears the busy flag, it is not active", "Scale form error at metal station.");
            }
        }

        private void FrmScale_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //huy đối tượng máy in
                PrinterClose();

                if (_ckQRTask != null)
                {
                    _ckQRTask.Wait();
                    _ckQRTask.Dispose();
                }

                _driverTelnet.DataEvent.EventHandleValueChange -= DataEvent_EventHandleValueChange;
                _driverTelnet.DataEvent.EventHandleStatusChange -= DataEvent_EventHandleStatusChange;

                _driverTelnet.IsDisconect = true;
                _driverTelnet?.DisconnectDevices();

                _driverTelnetMetal.DataEvent.EventHandleValueChange -= DataEventMetal_EventHandleValueChange;
                _driverTelnetMetal.DataEvent.EventHandleStatusChange -= DataEventMetal_EventHandleStatusChange;

                _driverTelnetMetal.IsDisconect = true;
                _driverTelnetMetal?.DisconnectDevices();
                //huy doi tuong can
                //_scaleHelper.StopScale = true;
                //_ckTask.Wait();
                //_ckTask.Dispose();
                //_scaleHelper.Dispose();
                GlobalVariables.ScaleStatus = "Disconnect";

                _timer?.Cancel();
                _timerTask?.Wait(1000);

                _resetUiCts?.Cancel();
                _resetUiTask?.Wait(1000); // đợi nhẹ, tránh treo UI

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

        /// <summary>
        /// Nhận diện QR thứ 2 (box info) trên thùng: Carton "BoxType,Supplier,BoxWeight" (vd "BX1,DKP,987.3Gr")
        /// hoặc Plastic "Name,BoxWeight" (vd "G250001,1320"). Khác với QR chính (Main QR) luôn có 2 ký tự đầu
        /// khớp GlobalVariables.OcUsingList — QR box info thì không.
        /// </summary>
        private bool TryParseBoxInfoQr(string barcodeString, out EnumBoxType boxType, out double boxWeightGrams)
        {
            boxType = default;
            boxWeightGrams = 0;

            if (string.IsNullOrEmpty(barcodeString) || barcodeString.Contains("|"))
                return false;

            var tokens = barcodeString.Split(',');
            if (tokens.Length != 2 && tokens.Length != 3)
                return false;

            var prefix = barcodeString.Length >= 2 ? barcodeString.Substring(0, 2) : barcodeString;
            if (GlobalVariables.OcUsingList.Any(x => x.OcFirstChar == prefix))
                return false; // khớp OC thật -> đây là Main QR, không phải box info

            string weightToken;
            if (tokens.Length == 3)
            {
                // Carton: BoxType,Supplier,BoxWeight
                if (!Enum.TryParse(tokens[0].Trim(), true, out boxType))
                    return false;
                weightToken = tokens[2];
            }
            else
            {
                // Plastic: Name,BoxWeight
                boxType = EnumBoxType.Plastic;
                weightToken = tokens[1];
            }

            var numericPart = Regex.Match(weightToken.Trim(), @"^[0-9]+(\.[0-9]+)?");
            if (!numericPart.Success)
                return false;

            boxWeightGrams = double.Parse(numericPart.Value, CultureInfo.InvariantCulture);
            _boxWeightQR = boxWeightGrams;
            return true;
        }

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
                    _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    //Check xem  QR code quét vào có đúng định dạng hay ko

                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstCharMetal);

                    if (resultCheckOc != null)
                    {
                        _scanDataMetal.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code is invalid. Please delete and scan again.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            _labResultIdentification.Text = "OC is not in the correct format.";
                            _labResultIdentification.ForeColor = Color.Red;
                        });

                        //ghi lệnh reject do ko quet đc tem
                        GlobalVariables.MyEvent.MetalPusher = 3;

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
                    _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    //Check xem  QR code quét vào có đúng định dạng hay ko
                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstCharMetal);

                    if (resultCheckOc != null)
                    {
                        _scanDataMetal.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code is invalid. Please delete and scan again.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            _labResultIdentification.Text = "OC is not in the correct format.";
                            _labResultIdentification.ForeColor = Color.Red;
                        });

                        //ghi lệnh reject do ko quet đc tem
                        GlobalVariables.MyEvent.MetalPusher = 3;

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

                                Debug.WriteLine($"ProductNumber: {box.ProductNumber} already checked OK, not checking again.");

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labResultIdentification.Text = "This box already checked OK. Not checking again.";
                                    _labResultIdentification.ForeColor = Color.Red;
                                });

                                GlobalVariables.MyEvent.MetalPusher = 3;

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
                                    Reason = "This box already checked OK.",
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
                                GlobalVariables.MyEvent.MetalPusher = 1;

                                Debug.WriteLine($"Product Number: {res.ProductNumber} requires metal detection.");

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labResultIdentification.Text = "Metal Detection Item.";
                                });
                            }
                            else if (res.MetalScan == 0 || (res.MetalScan == 1 && ocFirstCharMetal == "PR"))
                            {
                                // gui data xuong PLC để điều khiển băng tải phân luồng chạy vòng qua máy quét kim loại.
                                GlobalVariables.MyEvent.MetalPusher = 2;

                                Debug.WriteLine($"Product Number: {res.ProductNumber} does not require metal detection.");

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labResultIdentification.Text = "Item does not require metal detection.";
                                });
                            }
                        }
                        else
                        {
                            GlobalVariables.MyEvent.MetalPusher = 3;//bao reject cho PLC

                            Debug.WriteLine($"Item '{_scanDataWeight.ProductNumber}' has no weight/pair. Please check the information again."
                                , "WARNING.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                _labResultIdentification.Text = "No weight/pair data. Weight/Prs.";
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
                                Reason = "No weight/pair data. Average Weight/prs.",
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
                                Note = "No data in QC file yet.",
                                QrCode = _scanDataWeight.BarcodeString
                            };
                            dbContext.TblItemMissingInfos.Add(missingLine);

                            dbContext.SaveChanges();
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Product number {_scanDataMetal.ProductNumber} does not exist in the system. Please notify the manager to get the latest data from Winline."
                            , "WARNING.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        throw new Exception($"Product number {_scanDataMetal.ProductNumber} does not exist in the system. Please notify the manager to get the latest data from Winline.");
                    }
                    #endregion
                }

            }
            catch (Exception ex)
            {
                //gui data xuong PLC
                GlobalVariables.MyEvent.MetalPusher = 3;

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
                        Message = $"Error checking label. Scanner: 1 (Identification)|{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}",
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
            if (TryParseBoxInfoQr(barcodeString, out var boxTypeFromQr, out var boxWeightFromQr))
            {
                _boxTypeQR = boxTypeFromQr;
                _boxWeightQR = boxWeightFromQr;

                Debug.WriteLine($"Box info QR read at Scale station: boxType={boxTypeFromQr}, boxWeight={boxWeightFromQr}g");

                try
                {
                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        labQrScale.Text = barcodeString;
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString(), "Scale form error updating labQrScale for box info QR.");
                }

                return;
            }

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
                    _scanDataWeight.Unit = _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    //Check xem  QR code quét vào có đúng định dạng hay ko

                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstChar);

                    if (resultCheckOc != null)
                    {
                        _scanDataWeight.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code is wrong, delete it and scan again", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        //ghi lệnh reject do ko quet đc tem
                        GlobalVariables.MyEvent.WeightPusher = 2;

                        throw new Exception("OC has invalid format.");
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
                        else if (s2[0] == "4")
                        {
                            _scanDataWeight.Location = LocationEnum.fIN;
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
                        else if (s[1] == "4")
                        {
                            _scanDataWeight.Location = LocationEnum.fIN;
                        }
                    }
                }
                else
                {
                    var s1 = _scanDataWeight.BarcodeString.Split(',');
                    _scanDataWeight.Unit = _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R


                    //Check xem  QR code quét vào có đúng định dạng hay ko
                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstChar);

                    if (resultCheckOc != null)
                    {
                        _scanDataWeight.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code is wrong, delete it and scan again", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        //ghi lệnh reject do ko quet đc tem
                        GlobalVariables.MyEvent.WeightPusher = 2;

                        throw new Exception("OC has invalid format.");
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
                                    Debug.WriteLine($"Quantity exceeds BX1 box limit ({res.BoxQtyBx1})", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                    var missingLine = new tblItemMissingInfo
                                    {
                                        Id = Guid.NewGuid(),
                                        IsActive = true,
                                        CreatedDate = DateTime.Now,
                                        ProductNumber = _scanDataWeight.ProductNumber,
                                        ProductName = _scanDataWeight.ProductName,
                                        OcNum = _scanDataWeight.OcNo,
                                        Note = $"Quantity exceeds BX1 box limit ({res.BoxQtyBx1})",
                                        QrCode = _scanDataWeight.BarcodeString
                                    };
                                    dbContextSSFG.TblItemMissingInfos.Add(missingLine);
                                    dbContextSSFG.SaveChanges();

                                    throw new Exception("Quantity exceeds BX1 box limit.");
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
                                _boxType = EnumBoxType.Plastic;

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labPrinting.Text = _scanDataWeight.Decoration == 0 ? "NO" : "YES";
                                    _labBoxType.Text = "Plastic";
                                });
                            }

                            if (_boxTypeQR.HasValue)
                            {
                                if (_boxTypeQR.Value != _boxType)
                                {
                                    Debug.WriteLine($"Box type mismatch: label/master-data={_boxType}, QR box info={_boxTypeQR.Value}. Using QR box info value.");
                                }

                                _boxType = _boxTypeQR.Value;
                                _scanDataWeight.BoxWeight = _boxWeightQR;

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    _labBoxType.Text = _boxType.ToString();
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
                            //if (_plr == "P")
                            //{
                            //    _scanDataWeight.GrossdWeight = res.Weight * res.QtyPerbag + res.BagWeight;
                            //}
                            //else if (_plr == "L")
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
                            //else if (_plr == "R")
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
                                        //GlobalVariables.MyEvent.StatusLightPLC = 2;
                                        GlobalVariables.MyEvent.WeightPusher = 1;

                                        //hien thi mau label
                                        GlobalVariables.InvokeIfRequired(this, () =>
                                        {
                                            _labResult.Text = "PASSED";
                                            _labResult.BackColor = Color.Green;
                                            _labResult.ForeColor = Color.White;
                                            _labResultMessage.Text = "Weight OK. Printing label.";
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
                                        Debug.WriteLine($"This OC box has already been scanned and recorded as weight OK, cannot be weighed again." +
                                            $"{Environment.NewLine}Scan a different box.", "NOTICE", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                        throw new Exception($"{_scanDataWeight.OcNo} - {_scanDataWeight.BoxNo} - {_unitLabel} - {_scanDataWeight.IdLabel} has already been scanned and recorded as weight OK.");
                                    }
                                }
                                else//thung fail
                                {
                                    //bật đèn đỏ
                                    //GlobalVariables.MyEvent.StatusLightPLC = 1;
                                    GlobalVariables.MyEvent.WeightPusher = 2;//ghi xuong PLC bao reject

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

                                        throw new Exception($"Weight failed. {_scanDataWeight.OcNo} - {_scanDataWeight.BoxNo} - {_scanDataWeight.Unit} - deviation: {_scanDataWeight.DeviationPairs} {_unitLabel}.");
                                    }
                                    else if (statusLogData == 1)
                                    {
                                        Debug.WriteLine($"This OC box has already been scanned and recorded as weight failed, cannot be weighed again." +
                                             $"{Environment.NewLine}Scan a different box.", "NOTICE", MessageBoxButtons.OK, MessageBoxIcon.Information); ;

                                        throw new Exception($"{_scanDataWeight.OcNo} - {_scanDataWeight.BoxNo} - {_unitLabel} - {_scanDataWeight.IdLabel} has already been scanned and recorded as weight failed.");
                                    }
                                    else// if (statusLogData == 2)
                                    {
                                        Debug.WriteLine($"This OC box has already been scanned and recorded as weight OK, cannot be weighed again." +
                                            $"{Environment.NewLine}Scan a different box.", "NOTICE", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                        throw new Exception($"{_scanDataWeight.OcNo} - {_scanDataWeight.BoxNo} - {_unitLabel} - {_scanDataWeight.IdLabel} has already been scanned and recorded as weight OK.");
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
                                        _labResult.Text = "PASSED";
                                        _labResult.BackColor = Color.Green;
                                        _labResult.ForeColor = Color.White;
                                        _labResultMessage.Text = "Heel counter item OK. Weight not checked.";

                                        //hiển thị cho trạng thái log
                                        _labLastResultMessage.Text = "The HC is OK. Don't check the weight.";
                                        _labLastResultMessage.ForeColor = Color.Green;
                                    });

                                    LogDataScan(dbContextSSFG);
                                }
                                else
                                {
                                    Debug.WriteLine($"This HC box has already been scanned and recorded as weight OK, cannot be weighed again." +
                                        $"{Environment.NewLine}Scan a different box.", "NOTICE", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                    throw new Exception($"{_scanDataWeight.OcNo} - {_scanDataWeight.BoxNo} - {_unitLabel} - {_scanDataWeight.IdLabel} has already been scanned and recorded as weight OK.");
                                }
                            }
                            #endregion

                            #endregion
                        }
                        else
                        {
                            Debug.WriteLine($"Item '{_scanDataWeight.ProductNumber}' has no weight/1 pair. Please check the information again."
                                , "WARNING.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            throw new Exception($"Product number '{_scanDataWeight.ProductNumber}' has no weight per 1 prs/pcs. Please check the information again.");
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Product number {_scanDataWeight.ProductNumber} does not exist in the system. Notify the manager to update new data from winline."
                            , "WARNING.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        throw new Exception($"Product item '{_scanDataWeight.ProductNumber}' does not exist in the system yet. Please check the information again.");
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
                //GlobalVariables.MyEvent.StatusLightPLC = 1;
                //ghi giá trị xuống PLC seimens reject
                GlobalVariables.MyEvent.WeightPusher = 2;

                errorFlag = true;//bật biến này lên để thay đổi màu chữ thành NG cho các label message.

                //hien thi mau label
                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labResultMessage.Text = ex.Message;
                    _labResult.Text = "FAILED";
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

                Log.Error(ex.ToString(), "Scale form error at scanner 2 scale station.");
            }
            finally
            {
                //hien thi cac thong so dem
                ShowUI(errorFlag: errorFlag);

                _scanDataWeight = new tblScanData();
                _resetUI = true;

                //đảm bảo QR box info không bị rò rỉ sang thùng kế tiếp nếu thùng hiện tại bị reject/exception trước khi tới được đoạn override
                _boxTypeQR = null;
                _boxWeightQR = 0;
            }
        }

        #endregion

        #region Printing AnserU2 smart one (TCP)
        private void PrinterOpen()
        {
            try
            {
                _printerDriver = new AnserU2TcpDriver
                {
                    IpAddress = GlobalVariables.ConfigJson.IpPrinter,
                    Port = GlobalVariables.ConfigJson.PortPrinter
                };
                _printerDriver.DataReceived += PrinterDataReceived;
                _printerDriver.ConnectionStatusChanged += status =>
                {
                    GlobalVariables.PrintConnectionStatus = status == "Connected" ? "Good" : status;
                    Debug.WriteLine($"[AnserU2 TCP] {status}");
                };
                _printerDriver.Connect();

                GlobalVariables.PrintConnectionStatus = "Connecting...";
                StartPrint();
            }
            catch (Exception ex)
            {
                GlobalVariables.PrintConnectionStatus = "Bad";
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        private void PrinterClose()
        {
            if (_printerDriver == null) return;
            _printerDriver.DataReceived -= PrinterDataReceived;
            _printerDriver.Dispose();
            _printerDriver = null;
        }

        private void PrinterDataReceived(byte[] rcvArr)
        {
            try
            {
                if (rcvArr == null || rcvArr.Length < 5) return;

                //xet phan tu thu 4 trong mang rcvArr[4] de check status
                //0x4F-79--> OK
                //0x31-49-->Fail
                //0x30-48-->may in phan hoi in thanh cong

                if (rcvArr[4] == 0x30)
                {
                    Console.WriteLine($"Print successful!!!");
                    GlobalVariables.PrintedResult = $"Print successful.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}";

                    //reset model;
                    _scanDataWeight = null;
                    _scanDataWeight = new tblScanData();
                    //xoa string
                    //SendDynamicString(" ", " ", " ");
                }
                else if (rcvArr[4] == 0x4F)
                {
                    Console.WriteLine($"Command sent to printer successfully!!!");
                    GlobalVariables.PrintResult = $"Command sent to printer successfully.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}";
                }
                else if (rcvArr[4] == 0x31)
                {
                    Console.WriteLine($"Error. Error Code: {rcvArr[5]}. Reconnecting to printer.");
                    // MessageBox.Show($"Send command error: Error code: {rcvArr[5]}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GlobalVariables.PrintResult = $"Print error.{rcvArr[5]}|{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}";

                    //ghi giá trị xuống PLC cân reject
                    GlobalVariables.MyEvent.WeightPusher = 2;

                    //bat den đỏ 
                    //GlobalVariables.MyEvent.StatusLightPLC = 1;
                    //hien thi mau label

                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        _labResult.Text = "FAILED";
                        _labResult.BackColor = Color.Red;
                        _labResult.ForeColor = Color.White;
                        _labResultMessage.Text = "Fail Printing. PRINT NOT SUCCESSFUL.";

                        //hiển thị cho trạng thái log
                        _labLastResultMessage.Text = "Fail printing.";
                        _labLastResultMessage.ForeColor = Color.Red;
                    });

                    Log.Error("Print not successful.");

                    StopPrint();
                    Thread.Sleep(10000);
                    StartPrint();
                    Thread.Sleep(10000);
                }
                else
                {
                    //Console.WriteLine($"Loi. Error Code: {rcvArr[5]}. Kết nối lại máy in.");
                    // MessageBox.Show($"Send command error: Error code: {rcvArr[5]}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GlobalVariables.PrintResult = $"PRINT NOT SUCCESSFUL.{rcvArr[5]}|{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}";

                    //ghi giá trị xuống PLC cân reject
                    GlobalVariables.MyEvent.WeightPusher = 2;

                    //bat den đỏ 
                    //GlobalVariables.MyEvent.StatusLightPLC = 1;

                    //hien thi mau label
                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        _labResult.Text = "FAILED";
                        _labResult.BackColor = Color.Red;
                        _labResult.ForeColor = Color.White;
                        _labResultMessage.Text = "Fail Printing. PRINT NOT SUCCESSFUL.";
                    });

                    Log.Error(GlobalVariables.PrintResult);

                    using (var dbContextSSFG = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                    {
                        DynamicParameters para = new DynamicParameters();
                        //kiem tra neu co log vao thi xoa di
                        var checkEixst = dbContextSSFG.TblScanDatas.Where(x => x.Actived == 1 && x.BarcodeString == _scanDataWeight.BarcodeString).ToList();
                        checkEixst?.ForEach(x => x.Actived = 0);

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
                            ProductName = _scanDataMetal.ProductName,
                            ProductNumber = _scanDataMetal.ProductNumber,
                            Quantity = _scanDataMetal.Quantity,
                            ScannerStation = EnumStation.Scale.ToString(),
                            Reason = GlobalVariables.PrintedResult,
                            GrossWeight = _scanDataMetal.GrossWeight,
                            DeviationPairs = _scanDataMetal.DeviationPairs,
                            DeviationWeight = _scanDataMetal.Deviation
                        };
                        dbContextSSFG.TblScanDataRejects.Add(rejectLine);
                        dbContextSSFG.SaveChanges();
                    }
                }

                //else if (rcvArr[4] == 0x5D)//93 get speed
                //{
                //    var speedPV = (double)(rcvArr[5] + rcvArr[6] * 0x100 + rcvArr[7] * 0x1000 + rcvArr[8] * 0x10000);
                //    speedPV = Math.Round(speedPV / 1000, 2);
                //}
                //else if (rcvArr[4] == 0x64)//100 get delay
                //{
                //    var delayPV = (double)(rcvArr[5] + rcvArr[6] * 0x100 + rcvArr[7] * 0x1000 + rcvArr[8] * 0x10000);
                //    delayPV = Math.Round(delayPV / 100, 2);
                //}

                //GlobalVariables.PrintResult = string.Empty;
                //foreach (var item in rcvArr)
                //{
                //    GlobalVariables.PrintResult += item;
                //}
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Printing data received error: {ex.ToString()}");
                GlobalVariables.PrintResult = $"Printing data received error: {ex.ToString()}";

                //ghi giá trị xuống PLC cân reject
                GlobalVariables.MyEvent.WeightPusher = 2;

                //bat den đỏ 
                //GlobalVariables.MyEvent.StatusLightPLC = 1;
                //hien thi mau label

                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labResult.Text = "FAILED";
                    _labResult.BackColor = Color.Red;
                    _labResult.ForeColor = Color.White;
                    _labResultMessage.Text = "EXCEPTION of printing.";
                    _labResultMessage.ForeColor = Color.Red;
                });
            }
            finally
            {

            }
        }

        //private void btn_sendSTRING1_Click(object sender, EventArgs e)
        //{
        //    string string1 = txtString1.Text;
        //    string string2 = txtString2.Text;

        //    SendDynamicString(string1, string2);
        //}

        private void StartPrint()
        {
            try
            {
                byte[] SetPtinting = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x46, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3 };
                SetPtinting[5] = 2; // chon ban in so 2
                byte chkSUM = 0;
                for (var i = 1; i <= SetPtinting.Length - 3; i++)
                    chkSUM = (byte)(chkSUM + SetPtinting[i]);
                SetPtinting[9] = chkSUM;
                _printerDriver?.Write(SetPtinting);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Start print error: {ex.ToString()}");
            }
            finally
            {
                Thread.Sleep(500);
            }
        }

        private void StopPrint()
        {
            try
            {
                byte[] SetPtinting = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x46, 0x0, 0x0, 0x0, 0x0, 0x4C, 0x3 };
                _printerDriver?.Write(SetPtinting);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Stop print error: {ex.ToString()}");
            }
            finally
            {
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Method truyen noi dung xuong may in.
        /// In ok thì mới log vào DB.
        /// </summary>
        /// <param name="grossWeight">Khối lượng cân thực tế.</param>
        /// <param name="createdDate">Thời điểm cân.</param>
        /// <param name="idLabel">'IdLabel' hoặc là 'OC|BoxNo'.</param>
        private void SendDynamicString(string idLabel, string grossWeight, string createdDate)
        {
            try
            { 
                //hiện tại yêu cầu bỏ hết thông tin ngày tháng khi in tem.
                createdDate = " ";

                int i = 0, j = 0, k = 0;
                int chkSUM = 0;

                byte[] SetDynamicString = new byte[14 + grossWeight.Length + createdDate.Length + idLabel.Length];
                SetDynamicString[0] = 0x2;
                SetDynamicString[1] = 0x0;
                SetDynamicString[2] = (byte)(9 + grossWeight.Length + createdDate.Length + idLabel.Length);
                SetDynamicString[3] = 0x0;
                SetDynamicString[4] = 0xCA; // Mã lệnh Set dynamic string
                SetDynamicString[5] = 0;
                SetDynamicString[6] = 0;
                SetDynamicString[7] = (byte)(grossWeight.Length); // Chiều dài của string 1
                SetDynamicString[8] = (byte)(createdDate.Length); // Chiều dài của string 2
                SetDynamicString[9] = (byte)(idLabel.Length); // Chiều dài của string 3
                SetDynamicString[10] = 0;// (byte)(passMetal.Length); // Chiều dài của string 4
                SetDynamicString[11] = 0; // Chiều dài của string 5

                //chuyen string sang ASCII
                var string1Arr = grossWeight.ToCharArray();
                var string2Arr = createdDate.ToCharArray();
                var string3Arr = idLabel.ToCharArray();

                byte[] string1Ascii = Encoding.ASCII.GetBytes(string1Arr);
                byte[] string2Ascii = Encoding.ASCII.GetBytes(string2Arr);
                byte[] string3Ascii = Encoding.ASCII.GetBytes(string3Arr);

                for (i = 0; i <= string1Ascii.Length - 1; i++)
                {
                    SetDynamicString[12 + i] = string1Ascii[i];// Nội dung của string 1
                }

                for (j = 0; j <= string2Ascii.Length - 1; j++)
                {
                    SetDynamicString[12 + i + j] = string2Ascii[j];// Nội dung của string 2
                }

                for (k = 0; k <= string3Ascii.Length - 1; k++)
                {
                    SetDynamicString[12 + i + j + k] = string3Ascii[k];// Nội dung của string 3
                }

                // Tính check SUM
                for (var c = 1; c <= i + j + k + 12; c++)
                    chkSUM = chkSUM + SetDynamicString[c];
                chkSUM = chkSUM & 0xFF;
                SetDynamicString[i + j + k + 12] = System.Convert.ToByte(chkSUM); // Gán byte checksum vào arr
                SetDynamicString[i + j + k + 12 + 1] = 0x3;

                _printerDriver?.Write(SetDynamicString);
            }
            catch (Exception ex)
            {
                //ghi giá trị xuống PLC cân reject
                GlobalVariables.MyEvent.WeightPusher = 2;

                //hien thi mau label
                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labResult.Text = "FAILED";
                    _labResult.BackColor = Color.Red;
                    _labResult.ForeColor = Color.White;
                    _labResultMessage.Text = "System fail.Fail Printing. Error while sending data to the printer.";
                });

                Log.Error(ex, $"System fail. Error while sending data to the printer. Ex:{ex.ToString()}.");
            }
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            //BarcodeHandle(2, "C100028,6817012205-2397-D243,1,2,P,2/2,1900068,1/1|2,22421.2023,,,");
        }

        private void btn_Setspeed_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] setSpeed = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x5E, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3 };

                int speedSV = 10 * 1000;// (int)(double.TryParse(txt_SpeedSV.Text, out double value) ? value * 1000 : 0);

                byte[] speedSVarr = BitConverter.GetBytes(speedSV);

                // Gán giá trị tốc độ vào arr
                var i = 0;
                foreach (var item in speedSVarr)
                {
                    setSpeed[5 + i] = item;
                    i += 1;
                }

                // Tính checksum
                int chksum = 0;
                for (var j = 1; j <= setSpeed.Length - 2; j++)
                    chksum = chksum + setSpeed[j];
                chksum = chksum & 0xFF;
                // Gán checksum vào arr
                setSpeed[9] = System.Convert.ToByte(chksum);
                _printerDriver?.Write(setSpeed);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Printing set speed error: {ex.ToString()}");
            }
            finally
            {
                System.Threading.Thread.Sleep(500);
            }
        }

        private void labErrInfoMetal_Click(object sender, EventArgs e)
        {

        }

        private void labelControl40_Click(object sender, EventArgs e)
        {

        }

        private void btn_GetSpeed_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] GetSpeed = new byte[] { 0x2, 0x0, 0x2, 0x0, 0x5D, 0x5F, 0x3 };
                _printerDriver?.Write(GetSpeed);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Printing get speed error: {ex.ToString()}");
            }
            finally
            {
                System.Threading.Thread.Sleep(500);
            }
        }

        private void labSize_Click(object sender, EventArgs e)
        {

        }

        private void btnGetDelay_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] GetDelay = new byte[] { 0x2, 0x0, 0x4, 0x0, 0x64, 0x2, 0x0, 0x6A, 0x3 };
                // Id ban tin =2
                _printerDriver?.Write(GetDelay);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Printing get delay error: {ex.ToString()}");
            }
            finally
            {
                System.Threading.Thread.Sleep(500);
            }
        }

        private void btnSetDelay_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] setDelay = new byte[] { 0x2, 0x0, 0x8, 0x0, 0x65, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3 };
                // Gán IDbản tin
                setDelay[5] = 0x2;

                int delaySV = 10 * 10;// (int)(double.TryParse(txtDelaySV.Text, out double value) ? value * 10 : 0);

                byte[] delaySVarr = BitConverter.GetBytes(delaySV);

                // Gán giá trị tốc độ vào arr
                var i = 0;
                foreach (var item in delaySVarr)
                {
                    setDelay[7 + i] = item;
                    i += 1;
                }

                // Tính checksum
                int chksum = 0;
                for (var j = 1; j <= setDelay.Length - 2; j++)
                    chksum = chksum + setDelay[j];
                chksum = chksum & 0xFF;
                // Gán checksum vào arr
                setDelay[11] = System.Convert.ToByte(chksum);
                _printerDriver?.Write(setDelay);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Printing set delay error: {ex.ToString()}");
            }
            finally
            {
                System.Threading.Thread.Sleep(500);
            }
        }
        #endregion


        #region Task and other methos
        /// <summary>
        /// Chạy method này để tính thời gian quét Qr.
        /// Sau thời gian này mà chưa đọc đọc QR Thì báo không đọc được, ghi tín hiệu xuống cho MetalPusher reject.
        /// </summary>
        public void CheckReadQr(int timeCheckSettings)
        {
            Debug.WriteLine("Bat dau vao dem thoi gian doc barcode");
            double timeCheck = 0;
            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now;

            while (timeCheck <= timeCheckSettings)
            {
                timeCheck = (endTime - startTime).TotalSeconds;
                endTime = DateTime.Now;
                //Debug.WriteLine($"Dem thoi gian bao Metal Scanner fail: {timeCheck}");
                Thread.Sleep(10);
            }

            //hết thời gian mà vẫn chưa có tín hiệu từ scanner metal thì ghi tín hiệu xuống PLC conveyor báo reject
            if (!_readQrStatus[0])
            {
                Debug.WriteLine($"Writing reject signal because QR code was not read at metal station");

                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labResultIdentification.Text = "Could not read QR code, check the label again.";
                    _labResultIdentification.ForeColor = Color.Red;
                    _labQrIdentification.Text = string.Empty;
                });

                //hết thời gian đọc QR code mà chưa đọc được
                //gui data xuong PLC báo reject metalPusher
                GlobalVariables.MyEvent.MetalPusher = 3;

                _scanDataMetal = null;
                _scanDataMetal = new tblScanData();
                //log vao bang reject
                using (var dbContextSSFG = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                {
                    var rejectLine = new tblScanDataReject()
                    {
                        Id = Guid.NewGuid(),
                        IsActived = 1,
                        CreatedDate = DateTime.Now,
                        CreatedMachine = Environment.MachineName,
                        ScannerStation = EnumStation.Identification.ToString(),
                        Reason = "Could not read QR code, check the label again."
                    };
                    dbContextSSFG.TblScanDataRejects.Add(rejectLine);
                    dbContextSSFG.SaveChanges();
                }
            }
            _readQrStatus[0] = false;//xóa biến này cho lần đọc kế tiếp
        }

        /// <summary>
        /// Chạy method này để tính thời gian quét Qr.
        /// Sau thời gian này mà chưa đọc đọc QR Thì báo không đọc được, ghi tín hiệu xuống cho MetalPusher reject.
        /// </summary>
        public void CheckReadQrWeight()
        {
            double timeCheck = 0;
            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now;

            while (timeCheck <= GlobalVariables.ConfigJson.TimerCheckQrScale)
            {
                timeCheck = (endTime - startTime).TotalSeconds;
                endTime = DateTime.Now;
                //Debug.WriteLine($"Dem thoi gian bao weight Scanner fail: {timeCheck}");
                Thread.Sleep(100);
            }

            //hết thời gian mà vẫn chưa có tín hiệu từ scanner metal thì ghi tín hiệu xuống PLC conveyor báo reject
            if (!_readQrStatus[1])
            {
                SendDynamicString(" ", " ", " ");

                // Debug.WriteLine($"Ghi tin hieu bao reject do ko doc dc QR code tram scale");

                //hết thời gian đọc QR code mà chưa đọc được
                //gui data xuong PLC báo reject Weight Pusher
                GlobalVariables.MyEvent.WeightPusher = 2;


                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    labQrScale.Text = string.Empty;
                    _labResult.Text = "FAILED";
                    _labResult.BackColor = Color.Red;
                    _labResult.ForeColor = Color.White;
                    _labResultMessage.Text = "Could not read QR code, check the label again.";
                });

                //_scanDataWeight = null;
                //_scanDataWeight = new tblScanDataModel();

                //log vao bang reject
                using (var dbContextSSFG = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                {
                    var rejectLine = new tblScanDataReject()
                    {
                        Id = Guid.NewGuid(),
                        IsActived = 1,
                        CreatedDate = DateTime.Now,
                        CreatedMachine = Environment.MachineName,
                        ScannerStation = EnumStation.Scale.ToString(),
                        Reason = "Could not read QR code, check the label again."
                    };
                    dbContextSSFG.TblScanDataRejects.Add(rejectLine);
                    dbContextSSFG.SaveChanges();
                }
            }
            _readQrStatus[1] = false;//xóa biến này cho lần đọc kế tiếp, đồng bộ với CheckReadQr() của trạm metal
        }

        private async Task TaskTimerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        _labStatus.Text = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} " +
                              $"| {GlobalVariables.UserLoginInfo.UserName} | Cognex 1: {GlobalVariables.CognexCam_1Status} Cognex 2: {GlobalVariables.CognexCam_2Status}" +
                              $" | ConveyorStatus: {_plcConnectionState}; S1-{GlobalVariables.MyEvent.SensorBeforeMetalScan}; RJ1:{_rj1}; S2:{GlobalVariables.MyEvent.SensorMiddleMetal}" +
                              $";S_MD_Out:{GlobalVariables.MyEvent.SensorAfterMetalScan};Metal_Result:{GlobalVariables.MyEvent.MetalCheckResult}" +
                              $" | SV:{GlobalVariables.MyEvent.ScaleValue};ST:{GlobalVariables.MyEvent.ScaleValueStable};" +
                              $"Stable:{GlobalVariables.MyEvent.StableScale}-S5:{GlobalVariables.MyEvent.SensorBeforeWeightScan}-result:{_checkWeightResult}-S6:{GlobalVariables.MyEvent.SensorAfterWeightScan}-timer:{GlobalVariables.DelayPrintInterval}"
                              + $" | PrintStatus: {GlobalVariables.PrintConnectionStatus} | AP1: {GlobalVariables.AutoPostingStatus1}|APM:{GlobalVariables.AutoPostingStatus2} | APW: {GlobalVariables.AutoPostingStatus3}";
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
            var sw = Stopwatch.StartNew();
            var lastResetAt = sw.Elapsed;
            var intervalSeconds = GlobalVariables.ConfigJson.ResetUiInterval;

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
                            if (this.IsHandleCreated && !this.IsDisposed)
                            {
                                this.BeginInvoke(new Action(() =>
                                {
                                    try
                                    {
                                        ResetControl();
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, "ResetControl error.");
                                    }
                                }));
                            }

                            lastResetAt = now;
                            _resetUI = false;   // tiêu thụ yêu cầu reset
                        }

                        // else: KHÔNG xoá _resetUI
                    }

                    await Task.Delay(200, token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Log.Error(ex, "TaskCheckResetUIAsync error");
                    await Task.Delay(500, token);
                }
            }
        }
        private async Task TaskCheckResetUIAsync1(CancellationToken token)
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
                            $@"SSFG App has a new version {args.CurrentVersion}. The current SSFG App version is {args.InstalledVersion}. Do you want to update to the new version?", @"Notice",
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
                    MessageBox.Show(@"SSFG App is already running the latest version.", @"Notice",
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
                //labLowerTolerance.Text = _scanDataWeight.LowerTolerance.ToString();
                //labUpperTolerance.Text = _scanDataWeight.UpperTolerance.ToString();
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
                labDeviation.Text = $"{_scanDataWeight.Deviation} (g)";
                labCalculatedPairs.Text = _scanDataWeight.CalculatedPairs.ToString();
                labDeviationPairs.Text = $"{_scanDataWeight.DeviationPairs.ToString()} ({_unitLabel})";

                //labDeviation.ForeColor = errorFlag == false ? Color.Green : Color.Red;
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
                _labQrIdentification.Text = string.Empty;
                labQrScale.Text = string.Empty;
                _labResultIdentification.Text = string.Empty;
                //_labResultIdentification.BackColor = _labResultMessage.ForeColor;

                #region Standard
                _labBoxId.Text = string.Empty;
                labOcNo.Text = string.Empty;
                labProductCode.Text = string.Empty;
                labProductName.Text = string.Empty;
                labQuantity.Text = "0";
                labColor.Text = string.Empty;
                labSize.Text = string.Empty;
                labAveWeight.Text = "0";
                //labLowerTolerance.Text = "0";
                //labUpperTolerance.Text = "0";
                labBoxWeight.Text = "0";
                labAccessoriesWeight.Text = "0";
                labGrossWeight.Text = "0";
                _labCheckMetal.Text = "NO";
                _labPrinting.Text = "NO";

                _labQtyStandard.Text = $"Quantity (-)";
                _labUnitCalculatQty.Text = $"Calculated Qty (-)";
                _labUnitDeviation.Text = $"Deviation (-)";

                _labUnitStandard.Text = string.Empty;
                _labFGW.Text = $"Weight (g)/-";

                _labBoxType.Text = null;
                _labLableId.Text = string.Empty;
                #endregion

                #region Scaled
                labScaleValue.Text = "0";
                labRealWeight.Text = "0";
                labNetWeight.Text = "0";

                labNetRealWeight.Text = "0";
                labDeviation.Text = "0 (g)";

                labCalculatedPairs.Text = "0";
                labDeviationPairs.Text = "0 (-)";

                _labResultMessage.Text = string.Empty;
                _labResult.Text = string.Empty;
                _labResult.BackColor = Color.Gray;

                labDeviationPairs.ForeColor = default;
                #endregion
            });
        }

        private async void WriteData2PlcSeimens(string tagName, object value)
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

            var tag = _plcRuntime.Tags.FirstOrDefault(t => t.Name == tagName);

            if (tag == null) return;

            // 1. Chuyển đổi dữ liệu an toàn
            object newValue = tag.DataType switch
            {
                PlcDataType.String => value,
                PlcDataType.Bool => value == "true" || value == "1",
                PlcDataType.Real or PlcDataType.LReal => double.TryParse(value.ToString(), out var d) ? d : 0.0,
                _ => int.TryParse(value.ToString(), out var i) ? i : 0
            };

            // 2. CHỈ GHI TAG ĐANG CHỌN (Tối ưu cực quan trọng)
            tag.NewValue = newValue;
            var tagsToWrite = new List<PlcTag> { tag };

            try
            {
                // Ghi bất đồng bộ để tránh đơ UI và không làm gián đoạn luồng đọc
                await Task.Run(() => writer.WriteGroupAsync(tagsToWrite));

                // Cập nhật ngay giá trị LastWritten trong Dictionary của Writer (nếu có dùng WriteGroupOnlyChanged ở chỗ khác)
                // writer.UpdateCache(tag); 
            }
            catch (Exception ex)
            {
                Log.Error($"Write PLC Seimens error: {ex.Message}");
            }

            //// Sử dụng hàm này nếu bạn gọi lệnh ghi trong một vòng lặp liên tục, ghi tất cả các tag.s
            //writer.WriteGroupOnlyChanged(_plcRuntime.Tags);
        }
    }
}