using AutoUpdaterDotNET;
using CognexLibrary_NETFramework;
using CoreScanner;
using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Data;
using System.Data.Entity;
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


        private ScaleHelper _scaleHelper;
        private Task _ckTask, _ckQRTask, _ckQrWeightScanTask;//task kiểm tra tại các trạm scanner để check xem có đoc đc QR code ko
        private bool _isStartCountTimer = false;
        private int _metalScannerStatus = 0;

        private bool[] _readQrStatus = { false, false, false };//biến báo đọc được QR hay không. metal-weight-print

        private int _stableScale = 0;//biến báo trạng thái cân ổn định, get khối lượng cân về
        private double _scaleValue = 0;//biến chứa giá trị cân realTime đọc từ đầu cân về
        private double _scaleValueStable = 0;//biến chứa giá trị cân ổn định được đọc về khi biến stable báo on
        private int _metalCheckResult = 0;//biến chứa giá trị metalCheck 

        //tạo các biến để lưu giá trị theo QR code tại từng trạm
        //private tblScanDataModel _scanData = new tblScanDataModel();
        private tblScanData _scanDataMetal = new tblScanData();
        private tblScanData _scanDataWeight = new tblScanData();
        private tblScanData _scanDataPrint = new tblScanData();

        private string _idLabel = null;
        private string _plr = null;// kiểu đóng thùng, P-đôi; L/R-left righ
        private double _weight = 0, _boxWeight = 0, _accessoriesWeight = 0;

        private bool _approveUpdateActMetalScan = false;

        // Declare CoreScannerClass
        private CCoreScanner _cCoreScannerClass;
        private string _barcodeString1 = null, _barcodeString2 = null, _barcodeString3 = null;//checkMetal--checkWeight--printing
        private bool[] _scannerIsBussy = { false, false, false };

        private SerialPort _serialPort;

        private bool _firstLoad = true;

        private bool _approvePrint = false;// lệnh cho phép in hay không, chỉ khi nào pass cân thì active lên cho in.

        //20250510 upgrade system to use scanner cogned DM290-X at station check weight
        private static CognexLibrary_NETFramework.DriverTelnet _driverTelnet = new CognexLibrary_NETFramework.DriverTelnet();

        private bool isUpdateClicked = false;
        byte[] _readHoldingRegisterArr = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        byte[] _writeHoldingRegisterArr = { 0, 1 };
        int _countDisconnectPlc = 0;
        private System.Threading.Tasks.Task _tskModbus, _tskProfinet;

        private bool _resetCounter = false;

        string _stationReport = "All";

        int _metalScan = 0, _metalPusher = 0, _weightPusher = 0, _printPusher = 0;

        private CancellationTokenSource _readModbus;
        private Task _readModbusTask;

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


        private void FrmScale_Load(object sender, EventArgs e)
        {
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
                Debug.WriteLine($"Event Sensor before metal scan đã qua vòng chờ: {o.NewValue} |{_isStartCountTimer}");
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
                Debug.WriteLine($"Event Sensor before weight scan: {o.NewValue}");
                //chạy task đếm thời gian cho việc quét tem, hết thời gian mà chưa nhận đc tín hiệu từ Scanner cognex
                //thì ghi tín hiêu xuống PLC conveyor để reject với lý do là không đọc đc QR
                if (o.NewValue == 1)
                {
                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        labQrScale.Text = string.Empty;
                        _labLastResultMessage.Text = string.Empty;
                    });

                    //bật biến báo đọc đc QR code từ label
                    //_readQrStatus[1] = true;

                    _ckQrWeightScanTask = new Task(() => CheckReadQrWeight());
                    _ckQrWeightScanTask.Start();


                    //reset các control để qua cân mẻ mới
                    ResetControl();
                }
                //else
                //{
                //    if (_ckQrWeightScanTask != null)
                //    {
                //        _ckQrWeightScanTask.Wait();
                //        _ckQrWeightScanTask.Dispose();
                //    }
                //}
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
                }

                Debug.WriteLine($"Event Sensor after scale: {o.NewValue}|ScannerBussy{_scannerIsBussy[1]}");
            };

            //khi thùng đụng cảm biến sau printing scanner thì reset biến báo bận cho scanner trạm print quét tiếp
            GlobalVariables.MyEvent.EventHandlerSensorAfterPrintScanner += (s, o) =>
            {
                if (o.NewValue == 1)
                {
                    _scannerIsBussy[2] = false;
                }
                Debug.WriteLine($"Event Sensor after scale: {o.NewValue}|ScannerBussy{_scannerIsBussy[2]}");
            };

            GlobalVariables.MyEvent.EventHandleSensorMiddleMetal += (s, o) =>
            {
                if (o.NewValue == 1)
                {
                    //xáo báo bận để cho phép scanner quét tiếp thùng.
                    _scannerIsBussy[0] = false;

                    _isStartCountTimer = false;
                    GlobalVariables.MyEvent.MetalPusher = _metalScannerStatus;
                }

                Debug.WriteLine($"Sensor middle metal: {o.NewValue}|ScannerBussy{_scannerIsBussy[0]}");
            };

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
                            GlobalVariables.MyEvent.MetalPusher1 = 0;

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

            if (!GlobalVariables.ConfigJson.IsTest)
            {
                #region Ket noi modbus RTU PLC: Scale, Metal scan
                if (GlobalVariables.ConfigJson.IsScale)
                {
                    GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.KetNoi(GlobalVariables.ConfigJson.ComPortScale, 9600, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One);

                    Debug.WriteLine($"PLC Status: {GlobalVariables.ModbusStatus}");

                    if (GlobalVariables.ModbusStatus)
                    {
                        //ghi thông số delay trước khi chạy vào máy in
                        //thanh ghi D500 cua PLC Delta DPV14SS2 co dia chi la 4596
                        //D511 -11FF = 4607
                        //thanh ghi D508,D509,D510,D511 cua PLC Delta DPV14SS2 co dia chi la 4604


                        byte[] mangGhi = { 0, 0, 0, 0, 0, 0, 0, 0 };
                        GlobalVariables.MyDriver.SetWord(mangGhi, 0, GlobalVariables.ConfigJson.DelayTimer2);
                        GlobalVariables.MyDriver.SetWord(mangGhi, 0, GlobalVariables.ConfigJson.DelayTimer1);
                        GlobalVariables.MyDriver.SetWord(mangGhi, 0, GlobalVariables.ConfigJson.DelayTimer3);
                        GlobalVariables.MyDriver.SetWord(mangGhi, 0, GlobalVariables.ConfigJson.DelayTimer4);

                        GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.WriteHoldingRegisters(1, 4604, 4, mangGhi);

                        //if (GlobalVariables.ModbusStatus)
                        {
                            MessageBox.Show($"Ghi modbus: {GlobalVariables.ModbusStatus}");
                        }

                        //thanh ghi D500 cua PLC Delta DPV14SS2 co dia chi la 4596
                        GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.ReadHoldingRegisters(1, 4596, 9, ref _readHoldingRegisterArr);

                        //GlobalVariables.RememberInfo.CountMetalScan = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 0);
                        ////update gia tri count vao sự kiện để trong frmScal  nó update lên giao diện
                        //GlobalVariables.MyEvent.CountValue = GlobalVariables.RememberInfo.CountMetalScan;

                        //GlobalVariables.MyEvent.CountValue = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 0);
                        GlobalVariables.MyEvent.ScaleValueStable = GlobalVariables.MyDriver.GetShortAt(_readHoldingRegisterArr, 2);
                        GlobalVariables.MyEvent.ScaleValue = GlobalVariables.MyDriver.GetShortAt(_readHoldingRegisterArr, 4);
                        GlobalVariables.MyEvent.StableScale = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 6);
                        GlobalVariables.MyEvent.SensorBeforeWeightScan = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 8);
                        GlobalVariables.MyEvent.SensorAfterWeightScan = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 10);
                        GlobalVariables.D506Value = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 12);
                        GlobalVariables.DelayPrintInterval = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 16);
                        //var delayConveyor = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 14);

                        //đăng ký sự kiện bật tắt đèn tháp báo cân pass/fail
                        GlobalVariables.MyEvent.EventHandleStatusLightPLC += MyEvent_EventHandleStatusLightPLC;

                        //ghi giá trị tắt đèn tháp xuống PLC
                        //GlobalVariables.MyDriver.ModbusRTUMaster.WriteHoldingRegisters(1, 4602, 1, _writeHoldingRegisterArr);
                        GlobalVariables.MyEvent.StatusLightPLC = 0;

                        //run thread đọc modbus, để đọc các giá trị cân
                        _readModbus = new CancellationTokenSource();
                        _readModbusTask = Task.Run(() => TaskReadModbusAsync(_readModbus.Token));
                    }
                    else
                    {
                        MessageBox.Show($"Không thể kết nối được cân (Modbus RTU).{Environment.NewLine}Tắt phần mềm, kiểm tra lại kết nối với PLC rồi mở lại phần mềm.",
                                        "CẢNH BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                #endregion

                #region Ket noi conveyor
                GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.KetNoi(GlobalVariables.ConfigJson.IpConveyor);
                //GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.KetNoi("10.40.0.112");
                Console.WriteLine($"Conveyor Status: {GlobalVariables.ConveyorStatus}");

                if (GlobalVariables.ConveyorStatus == "GOOD")
                {
                    GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.GhiDB(1, 0, 3, GlobalVariables.DataWriteDb1);

                    var resultData = GlobalVariables.MyDriver.S7Ethernet.Client.DocDB(1, 0, 10);

                    if (resultData.TrangThai == "GOOD")
                    {
                        GlobalVariables.ConveyorStatus = resultData.TrangThai;
                        //vùng nhớ chứa trạng thái của sensor là DB1[3], truoc vị trí metal scan, để tính thời gian quét QR code. 1-On;0-off
                        GlobalVariables.MyEvent.SensorBeforeMetalScan = resultData.MangGiaTri[3];
                        //sensor đặt ngay sau máy quét kim loại, báo là thùng hàng đã qua metal scan. 1-On;0-off
                        GlobalVariables.MyEvent.SensorAfterMetalScan = resultData.MangGiaTri[5];
                        //vùng nhớ báo kết quả check metal. 0-pass; 1-Fail
                        GlobalVariables.MyEvent.MetalCheckResult = resultData.MangGiaTri[4];
                        //vùng nhớ báo tin hiệu sensor ngay vị trí bàn nâng chuyển 3 hướng sau vị trí metal scanner
                        GlobalVariables.MyEvent.SensorMiddleMetal = resultData.MangGiaTri[7];

                        //Vùng nhớ báo tín hiệu sensor 2 vị trí sau scannerPrint
                        GlobalVariables.MyEvent.SensorAfterPrintScannerFG = resultData.MangGiaTri[8];
                        GlobalVariables.MyEvent.SensorAfterPrintScannerPrinting = resultData.MangGiaTri[9];
                    }

                    //run thread đọc profinet
                    _readProfinet = new CancellationTokenSource();
                    _readProfinetTask = Task.Run(() => TaskReadProfinetAsync(_readProfinet.Token));
                }
                else
                {
                    MessageBox.Show($"Không thể kết nối được băng tải.{Environment.NewLine}Tắt phần mềm, kiểm tra lại kết nối với PLC rồi mở lại phần mềm.",
                                    "CẢNH BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                ////run thread đọc modbus, để đọc các giá trị cân
                //_tskModbus = new System.Threading.Tasks.Task(() => ReadModbus());
                //_tskModbus.Start();

                //đăng ký các sự kiện ghi giá trị điều khiển Pusher
                //vùng nhớ dataBlock 1(DB1.DB0 byte). before metal scan
                GlobalVariables.MyEvent.EventHandlerMetalPusher += (s, o) =>
                {
                    //if (o.NewValue != 0)
                    {
                        GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.GhiDB(1, 0, 1, new byte[] { (byte)o.NewValue });
                    }
                    Debug.WriteLine($"Event ghi DB metal pusher {o.NewValue}. status {GlobalVariables.ConveyorStatus}");
                    //GlobalVariables.MyEvent.MetalPusher = 0;
                };
                //vùng nhớ dataBlock 1(DB1.DB1 byte). weight pusher
                GlobalVariables.MyEvent.EventHandlerWeightPusher += (s, o) =>
                {
                    if (o.NewValue == 1)
                    {
                        GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.GhiDB(1, 1, 1, new byte[] { 1 });
                        Debug.WriteLine($"Event ghi DB weight pusher {o.NewValue}. status {GlobalVariables.ConveyorStatus}");
                        //GlobalVariables.MyEvent.WeightPusher = 0;
                    }
                    else
                    {
                        Debug.WriteLine($"Event ghi DB weight pusher {o.NewValue}. status {GlobalVariables.ConveyorStatus}");
                    }
                };
                //vùng nhớ dataBlock 1(DB1.DB2 byte). printing pusher
                GlobalVariables.MyEvent.EventHandlerPrintPusher += (s, o) =>
                {
                    if (o.NewValue != 0)
                    {
                        GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.GhiDB(1, 2, 1, new byte[] { (byte)o.NewValue });
                    }
                    Debug.WriteLine($"Event ghi DB Print pusher {o.NewValue}. status {GlobalVariables.ConveyorStatus}");
                    GlobalVariables.MyEvent.PrintPusher = 0;
                };
                //vungf nho DB1.DB6/ dieu khien pusher reject quét kim loại lỗi
                GlobalVariables.MyEvent.EventHandleMetalePusher1 += (s, o) =>
                {
                    if (o.NewValue != 0)
                    {
                        GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.GhiDB(1, 6, 1, new byte[] { (byte)o.NewValue });
                    }
                    Debug.WriteLine($"Event ghi DB Metal pusher 1 {o.NewValue}. status {GlobalVariables.ConveyorStatus}");
                    GlobalVariables.MyEvent.MetalPusher1 = 0;
                };

                #endregion
            }

            //khởi tạo scanner
            InitializeScaner();

            //Khởi tạo máy in AnserU2 Smart one
            if (!GlobalVariables.ConfigJson.IsTest)
            {
                SerialPortOpen();
                Thread.Sleep(10000);
                SendDynamicString(" ", " ", " ");
            }

            #region 20250310 update to use scanner Cognex
            _driverTelnet.HostName = GlobalVariables.ConfigJson.IpCognexCamScale;
            //_driverTelnet.Port = 23;

            _driverTelnet.DataEvent.EventHandleValueChange += DataEvent_EventHandleValueChange;
            _driverTelnet.DataEvent.EventHandleStatusChange += DataEvent_EventHandleStatusChange;

            if (!GlobalVariables.ConfigJson.IsTest)
                _driverTelnet.ConnectDevices();

            #endregion

            this.ActiveControl = null;
            this.ActiveControl = _labResult;

            GlobalVariables.AppStatus = "READY";

            //tạo 1 task chạy độc lập để get data từ Hydra
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

        }

        private void DataEvent_EventHandleStatusChange(object sender, StatusChangeEventArgs e)
        {
            GlobalVariables.CognexCam_2Status = e.Status;
            Debug.WriteLine($"[{DateTime.Now}]: {e.Status}|{e.Exception?.Message}");
        }

        private void DataEvent_EventHandleValueChange(object sender, ValueChangeEventArgs e)
        {
            Debug.WriteLine($"[{DateTime.Now}]: {e.NewValue}|{e.OldValue}");
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
            else
            {
                Log.Error("The sensor clears the busy flag, it is not active", "Lỗi scale form");

                //MessageBox.Show($"Quantity over the BX1 box limit ({res.BoxQtyBx1}).", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labResultMessage.Text = "The sensor clears the busy flag, it is not active.";
                    _labResult.Text = "NG";
                    _labResult.BackColor = Color.Red;
                    _labResult.ForeColor = Color.White;
                });
            }
        }


        private void FrmScale_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                //huy đối tượng máy in
                SerialPortClose();

                if (_ckQRTask != null)
                {
                    _ckQRTask.Wait();
                    _ckQRTask.Dispose();
                }

                _driverTelnet.DataEvent.EventHandleValueChange -= DataEvent_EventHandleValueChange;
                _driverTelnet.DataEvent.EventHandleStatusChange -= DataEvent_EventHandleStatusChange;

                _driverTelnet.IsDisconect = true;
                _driverTelnet?.DisconnectDevices();
                //huy doi tuong can
                //_scaleHelper.StopScale = true;
                //_ckTask.Wait();
                //_ckTask.Dispose();
                //_scaleHelper.Dispose();
                GlobalVariables.ScaleStatus = "Disconnect";

                _readModbus?.Cancel();
                _readModbusTask?.Wait(1000); // đợi nhẹ, tránh treo UI

                _readProfinet?.Cancel();
                _readProfinetTask?.Wait(1000);

                _timer?.Cancel();
                _timerTask?.Wait(1000);

                _resetUiCts?.Cancel();
                _resetUiTask?.Wait(1000); // đợi nhẹ, tránh treo UI
            }
            catch
            {

            }
            finally
            {
                _readModbus?.Dispose();
                _readModbus = null;
                _readModbusTask = null;

                _readProfinet?.Dispose();
                _readProfinet = null;
                _readProfinetTask = null;

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
                    _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

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
                    _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

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
                    _scanDataWeight.Unit = _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

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
                    _scanDataWeight.Unit = _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R


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
                    _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

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
                    _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

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

        #region Read scanner using SDK
        void InitializeScaner()
        {
            //Instantiate CoreScanner Class
            _cCoreScannerClass = new CCoreScanner();
            //Call Open API
            short[] scannerTypes = new short[1]; // Scanner Types you are interested in
            scannerTypes[0] = 2; // 1 for all scanner types
            short numberOfScannerTypes = 1; // Size of the scannerTypes array
            int status; // Extended API return code
            _cCoreScannerClass.Open(0, scannerTypes, numberOfScannerTypes, out status);
            // Lets list down all the scanners connected to the host
            short numberOfScanners; // Number of scanners expect to be used
            int[] connectedScannerIDList = new int[255];
            // List of scanner IDs to be returned
            string outXML; //Scanner details output
            _cCoreScannerClass.GetScanners(out numberOfScanners, connectedScannerIDList,
            out outXML, out status);
            Console.WriteLine(outXML);

            // Subscribe for barcode events in cCoreScannerClass
            _cCoreScannerClass.BarcodeEvent += new
            _ICoreScannerEvents_BarcodeEventEventHandler(OnBarcodeEvent);

            // Let's subscribe for events
            int opcode = 1001; // Method for Subscribe events

            string inXML = "<inArgs>" +
            "<cmdArgs>" +
            "<arg-int>1</arg-int>" + // Number of events you want to subscribe
            "<arg-int>1</arg-int>" + // Comma separated event IDs
            "</cmdArgs>" +
            "</inArgs>";
            _cCoreScannerClass.ExecCommand(opcode, ref inXML, out outXML, out status);
            Console.WriteLine(outXML);

            inXML = "<inArgs>" +
           "<cmdArgs>" +
           "<arg-int>2</arg-int>" + // Number of events you want to subscribe
           "<arg-int>1</arg-int>" + // Comma separated event IDs
           "</cmdArgs>" +
           "</inArgs>";
            _cCoreScannerClass.ExecCommand(opcode, ref inXML, out outXML, out status);
            Console.WriteLine(outXML);

            inXML = "<inArgs>" +
           "<cmdArgs>" +
           "<arg-int>3</arg-int>" + // Number of events you want to subscribe
           "<arg-int>1</arg-int>" + // Comma separated event IDs
           "</cmdArgs>" +
           "</inArgs>";
            _cCoreScannerClass.ExecCommand(opcode, ref inXML, out outXML, out status);
            Console.WriteLine(outXML);
        }

        void OnBarcodeEvent(short eventType, ref string pscanData)
        {
            var r = eventType;
            string barcode = pscanData;//string từ scanner trả về

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(barcode);

            //Get ra Id của scanner
            var scannerId = xmlDoc.GetElementsByTagName("scannerID");

            using (var connection = GlobalVariables.GetDbConnection())
            {
                if (scannerId[0].InnerText == GlobalVariables.ConfigJson.ScannerIdMetal.ToString())//vị trí check metal. đầu chuyền
                {
                    _barcodeString1 = string.Empty;
                    if (!_scannerIsBussy[0])
                    {
                        //bật biến báo bận lên ko cho scan tiếp, chặn trường hợp thùng dán 2 tem.
                        _scannerIsBussy[0] = true;

                        //bật biến báo đọc đc QR code từ label
                        _readQrStatus[0] = true;

                        //this?.Invoke((MethodInvoker)delegate { txtDataAscii1.Text = xmlDoc.GetElementsByTagName("datalabel")[0].InnerText; });
                        _barcodeString1 = AsciiToString(xmlDoc.GetElementsByTagName("datalabel")[0].InnerText);

                        //reset model;
                        _scanDataMetal = null;
                        _scanDataMetal = new tblScanData();

                        BarcodeScanner1Handle(1, _barcodeString1);
                    }
                }
                else if (scannerId[0].InnerText == GlobalVariables.ConfigJson.ScannerIdPrint.ToString())//vị trí phân loại hàng sơn cuối chuyền
                {
                    if (!_scannerIsBussy[2])
                    {
                        //bật biến báo bận lên ko cho scan tiếp, chặn trường hợp thùng dán 2 tem.
                        _scannerIsBussy[2] = true;

                        //bật biến báo đọc đc QR code từ label
                        _readQrStatus[2] = true;

                        _barcodeString3 = AsciiToString(xmlDoc.GetElementsByTagName("datalabel")[0].InnerText);

                        //reset model;
                        _scanDataPrint = null;
                        _scanDataPrint = new tblScanData();

                        BarcodeScanner3Handle(3, _barcodeString3);

                        //reset model;
                        _scanDataPrint = null;
                        _scanDataPrint = new tblScanData();
                    }
                }
            }
        }

        string AsciiToString(string contentStr)
        {
            string returnValue = null;

            string[] splitStr = contentStr.Split(' ');

            foreach (var item in splitStr)
            {
                int n = Convert.ToInt32(item, 16);//chuyển đổi từ HEX --> DEC

                returnValue = returnValue + (char)n;//get ky tu ASCII
            }

            return returnValue;
        }
        #endregion

        #region Printing AnserU2 smart one
        public void SerialPortOpen()
        {
            DateTime dt = DateTime.Now;
            String dtn = dt.ToShortTimeString();

            _serialPort = new System.IO.Ports.SerialPort(GlobalVariables.ConfigJson.ComPortPrinter, 57600, Parity.None, 8, StopBits.One);
            try
            {
                _serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
                _serialPort.Open();
                Console.WriteLine("[" + dtn + "] " + "Connected\n");

                GlobalVariables.PrintConnectionStatus = "Good";

                StartPrint();
            }
            catch (Exception ex)
            {
                GlobalVariables.PrintConnectionStatus = "Bad";
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        private void SerialPortClose()
        {
            DateTime dt = DateTime.Now;
            String dtn = dt.ToShortTimeString();

            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                System.Threading.Thread.Sleep(100);
                string dataRCV = _serialPort.ReadExisting(); // Read
                if (string.IsNullOrEmpty(dataRCV))
                {
                    return;
                }

                var rcvArr = Encoding.ASCII.GetBytes(dataRCV);

                Console.WriteLine(dataRCV);

                //xet phan tu thu 4 trong mang rcvArr[4] de check status
                //0x4F-79--> OK
                //0x31-49-->Fail
                //0x30-48-->may in phan hoi in thanh cong

                if (rcvArr[4] == 0x30)
                {
                    Console.WriteLine($"in thanh cong!!!");
                    GlobalVariables.PrintedResult = $"In thành công.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}";

                    //reset model;
                    _scanDataWeight = null;
                    _scanDataWeight = new tblScanData();
                    //xoa string
                    //SendDynamicString(" ", " ", " ");
                }
                else if (rcvArr[4] == 0x4F)
                {
                    Console.WriteLine($"Gui lenh xuong may in thanh cong!!!");
                    GlobalVariables.PrintResult = $"Gửi lệnh xuống máy in thành công.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}";
                }
                else if (rcvArr[4] == 0x31)
                {
                    Console.WriteLine($"Loi. Error Code: {rcvArr[5]}. Kết nối lại máy in.");
                    // MessageBox.Show($"Send command error: Error code: {rcvArr[5]}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GlobalVariables.PrintResult = $"Lỗi in.{rcvArr[5]}|{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}";

                    //ghi giá trị xuống PLC cân reject
                    GlobalVariables.MyEvent.WeightPusher = 1;

                    //bat den đỏ 
                    GlobalVariables.MyEvent.StatusLightPLC = 1;
                    //hien thi mau label

                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        _labResult.Text = "NG";
                        _labResult.BackColor = Color.Red;
                        _labResult.ForeColor = Color.White;
                        _labResultMessage.Text = "Fail Printing. IN KHÔNG THÀNH CÔNG.";

                        //hiển thị cho trạng thái log
                        _labLastResultMessage.Text = "Fail printing.";
                        _labLastResultMessage.ForeColor = Color.Red;
                    });

                    Log.Error("In không thành công.");

                    StopPrint();
                    Thread.Sleep(10000);
                    StartPrint();
                    Thread.Sleep(10000);
                }
                else
                {
                    //Console.WriteLine($"Loi. Error Code: {rcvArr[5]}. Kết nối lại máy in.");
                    // MessageBox.Show($"Send command error: Error code: {rcvArr[5]}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    GlobalVariables.PrintResult = $"IN KHÔNG THÀNH CÔNG.{rcvArr[5]}|{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}";

                    //ghi giá trị xuống PLC cân reject
                    GlobalVariables.MyEvent.WeightPusher = 1;

                    //bat den đỏ 
                    GlobalVariables.MyEvent.StatusLightPLC = 1;

                    //hien thi mau label
                    GlobalVariables.InvokeIfRequired(this, () =>
                    {
                        _labResult.Text = "NG";
                        _labResult.BackColor = Color.Red;
                        _labResult.ForeColor = Color.White;
                        _labResultMessage.Text = "Fail Printing. IN KHÔNG THÀNH CÔNG.";
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
                GlobalVariables.MyEvent.WeightPusher = 1;

                //bat den đỏ 
                GlobalVariables.MyEvent.StatusLightPLC = 1;
                //hien thi mau label

                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labResult.Text = "NG";
                    _labResult.BackColor = Color.Red;
                    _labResult.ForeColor = Color.White;
                    _labLastResultMessage.Text = "EXCEPTION of printing.";
                    _labLastResultMessage.ForeColor = Color.Red;
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
        loop1:
            try
            {
                byte[] SetPtinting = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x46, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3 };
                // Gán số thứ tự của bản tin cần in vào array
                SetPtinting[5] = 2;//chon ban in so 2
                                   // Tính checksum
                byte chkSUM = 0;
                for (var i = 1; i <= SetPtinting.Length - 3; i++)
                    chkSUM = (byte)(chkSUM + SetPtinting[i]);
                // Gán giá trị checksum vào array
                SetPtinting[9] = chkSUM;
                // Gửi array xuống máy in
                _serialPort.Write(SetPtinting, 0, SetPtinting.Length);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Start print error: {ex.ToString()}");

                Thread.Sleep(10000);

                goto loop1;

                System.Threading.Thread.Sleep(10000);
            }
            finally
            {
                System.Threading.Thread.Sleep(500);
            }
        }

        private void StopPrint()
        {
            try
            {
                byte[] SetPtinting = new byte[] { 0x2, 0x0, 0x6, 0x0, 0x46, 0x0, 0x0, 0x0, 0x0, 0x4C, 0x3 };

                _serialPort.Write(SetPtinting, 0, SetPtinting.Length);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Stop print error: {ex.ToString()}");
            }
            finally
            {
                System.Threading.Thread.Sleep(500);
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

                _serialPort.Write(SetDynamicString, 0, SetDynamicString.Length);
            }
            catch (Exception ex)
            {
                //ghi giá trị xuống PLC cân reject
                GlobalVariables.MyEvent.WeightPusher = 1;

                //bat den đỏ 
                GlobalVariables.MyEvent.StatusLightPLC = 1;

                //hien thi mau label
                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labResult.Text = "NG";
                    _labResult.BackColor = Color.Red;
                    _labResult.ForeColor = Color.White;
                    _labResultMessage.Text = "System fail.Fail Printing. Lỗi khi đang truyền dữ liệu xuống máy in.";
                });

                Log.Error(ex, $"System fail. Lỗi khi đang truyền dữ liệu xuống máy in. Ex:{ex.ToString()}.");
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
                // Gửi xuống máy in
                _serialPort.Write(setSpeed, 0, setSpeed.Length);
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
                _serialPort.Write(GetSpeed, 0, GetSpeed.Length);
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
                _serialPort.Write(GetDelay, 0, GetDelay.Length);
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
                // Gửi xuống máy in
                _serialPort.Write(setDelay, 0, setDelay.Length);
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
                Debug.WriteLine($"Ghi tin hieu bao reject do ko doc dc QR code tram metal");

                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    _labResultIdentification.Text = "Không đọc được QR code, Kiểm tra lại tem.";
                    _labResultIdentification.ForeColor = Color.Red;
                    _labQrIdentification.Text = string.Empty;
                });

                //hết thời gian đọc QR code mà chưa đọc được
                //gui data xuong PLC báo reject metalPusher
                _metalScannerStatus = 1;

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
                        Reason = "Không đọc được QR code, Kiểm tra lại tem."
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
                GlobalVariables.MyEvent.WeightPusher = 1;
                //bật đèn đỏ
                GlobalVariables.MyEvent.StatusLightPLC = 1;


                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    labQrScale.Text = string.Empty;
                    _labResult.Text = "NG";
                    _labResult.BackColor = Color.Red;
                    _labResult.ForeColor = Color.White;
                    _labResultMessage.Text = "Không đọc được QR code, Kiểm tra lại tem.";
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
                        Reason = "Không đọc được QR code, Kiểm tra lại tem."
                    };
                    dbContextSSFG.TblScanDataRejects.Add(rejectLine);
                    dbContextSSFG.SaveChanges();
                }
            }
            //_readQrStatus[1] = false;//xóa biến này cho lần đọc kế tiếp
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
                              $"| {GlobalVariables.UserLoginInfo.UserName} | Cognex cam: {GlobalVariables.CognexCam_2Status}" +
                              $" | ConveyorStatus: {GlobalVariables.ConveyorStatus}. S1-{GlobalVariables.MyEvent.SensorBeforeMetalScan}. Sm-{GlobalVariables.MyEvent.SensorMiddleMetal}" +
                              $";MC-{GlobalVariables.MyEvent.MetalCheckResult};S2-{GlobalVariables.MyEvent.SensorAfterMetalScan};PL-{GlobalVariables.MyEvent.SensorAfterPrintScannerFG};PR-{GlobalVariables.MyEvent.SensorAfterPrintScannerPrinting}" +
                              $". Pusher: MS-{_metalScan};M-{_metalPusher};W-{_weightPusher};P-{_printPusher}" +
                              $" | ModbusRTUStatus: {GlobalVariables.ModbusStatus}. SV:{GlobalVariables.MyEvent.ScaleValue}-ST:{GlobalVariables.MyEvent.ScaleValueStable}" +
                              $"-Stable:{GlobalVariables.MyEvent.StableScale}-SIn:{GlobalVariables.MyEvent.SensorBeforeWeightScan}-result:{GlobalVariables.D506Value}-timer:{GlobalVariables.DelayPrintInterval}"
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

        public async Task TaskReadModbusAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    #region Đọc các giá trị từ PLC Cân
                    if (GlobalVariables.ConfigJson.IsScale)
                    {
                        if (GlobalVariables.ModbusStatus)
                        {
                            //thanh ghi D500 cua PLC Delta DPV14SS2 co dia chi la 4596
                            GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.ReadHoldingRegisters(1, 4596, 12, ref _readHoldingRegisterArr);

                            //GlobalVariables.MyEvent.CountValue = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 0);
                            GlobalVariables.MyEvent.ScaleValue = GlobalVariables.MyDriver.GetShortAt(_readHoldingRegisterArr, 2);
                            GlobalVariables.MyEvent.ScaleValueStable = GlobalVariables.MyDriver.GetShortAt(_readHoldingRegisterArr, 4);
                            GlobalVariables.MyEvent.StableScale = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 6);
                            GlobalVariables.MyEvent.SensorBeforeWeightScan = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 8);
                            GlobalVariables.MyEvent.SensorAfterWeightScan = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 10);
                            GlobalVariables.D506Value = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 12);
                            GlobalVariables.DelayPrintInterval = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 16);
                        }
                        else
                        {
                            _countDisconnectPlc += 1;
                            Debug.WriteLine($"Dem mat ket noi modbus RTU:{_countDisconnectPlc}");
                            if (_countDisconnectPlc >= 3)
                            {
                                _countDisconnectPlc = 0;
                                GlobalVariables.MyDriver.ModbusRTUMaster.NgatKetNoi();

                                GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.KetNoi(GlobalVariables.ConfigJson.ComPortScale, 9600, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One);

                                Debug.WriteLine($"Ket noi lai modbus RTU. Result: {GlobalVariables.ModbusStatus}");
                            }
                        }
                    }
                    #endregion

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
                    Log.Error(ex, "TaskReadModbusAsync loop error.");
                    await Task.Delay(500, token); // tạm nghỉ rồi thử lại
                }
            }
        }

        public async Task TaskReadProfinetAsync(CancellationToken token)
        {
            bool weightPushFlag = false;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    #region Đọc các giá trị từ PLC conveyor s7-1200, profinet
                    if (GlobalVariables.ConveyorStatus == "GOOD")
                    {
                        var resultData = GlobalVariables.MyDriver.S7Ethernet.Client.DocDB(1, 0, 10);

                        GlobalVariables.ConveyorStatus = resultData.TrangThai;

                        if (resultData.TrangThai == "GOOD")
                        {
                            GlobalVariables.ConveyorStatus = resultData.TrangThai;

                            _metalScan = resultData.MangGiaTri[0];
                            _weightPusher = resultData.MangGiaTri[1];
                            _printPusher = resultData.MangGiaTri[2];
                            _metalPusher = resultData.MangGiaTri[6];

                            //if (_weightPusher == 0 && weightPushFlag == false)
                            //{
                            //    weightPushFlag = true;
                            //    GlobalVariables.MyEvent.WeightPusher = _weightPusher;
                            //}
                            //else if (_weightPusher == 0 && weightPushFlag == false)
                            //{
                            //    weightPushFlag = false;
                            //}

                            //vùng nhớ chứa trạng thái của sensor là DB1[3], truoc vị trí metal scan, để tính thời gian quét QR code. 1-On;0-off
                            GlobalVariables.MyEvent.SensorBeforeMetalScan = resultData.MangGiaTri[3];
                            //sensor đặt ngay sau máy quét kim loại, báo là thùng hàng đã qua metal scan. 1-On;0-off
                            GlobalVariables.MyEvent.SensorAfterMetalScan = resultData.MangGiaTri[5];
                            //vùng nhớ báo kết quả check metal. 0-pass; 1-Fail
                            GlobalVariables.MyEvent.MetalCheckResult = resultData.MangGiaTri[4];
                            //vùng nhớ báo tin hiệu sensor ngay vị trí bàn nâng chuyển 3 hướng sau vị trí metal scanner
                            GlobalVariables.MyEvent.SensorMiddleMetal = resultData.MangGiaTri[7];

                            //Vùng nhớ báo tín hiệu sensor 2 vị trí sau scannerPrint
                            GlobalVariables.MyEvent.SensorAfterPrintScannerFG = resultData.MangGiaTri[8];
                            GlobalVariables.MyEvent.SensorAfterPrintScannerPrinting = resultData.MangGiaTri[9];
                        }
                    }
                    else
                    {
                        GlobalVariables.MyDriver.S7Ethernet.Client.NgatKetNoi();

                        GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.KetNoi(GlobalVariables.ConfigJson.IpConveyor);
                    }
                    #endregion

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
                    Log.Error(ex, "TaskReadProfinetAsync loop error.");
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

        #region Event PLC
        private void MyEvent_EventHandleStatusLightPLC(object sender, TagValueChangeEventArgs e)
        {
            _writeHoldingRegisterArr[1] = (byte)e.NewValue;
        Loop1:
            GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.WriteHoldingRegisters(1, 4602, 1, _writeHoldingRegisterArr);

            if (!GlobalVariables.ModbusStatus)
            {
                goto Loop1;
            }
        }
        #endregion

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