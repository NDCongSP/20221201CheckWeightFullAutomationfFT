using Dapper;
using DevExpress.Spreadsheet;
using DevExpress.XtraPrinting.Export;
using DevExpress.XtraBars.Docking2010.Views.Tabbed;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using DevExpress.XtraWaitForm;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AutoUpdaterDotNET;
using System.Diagnostics;
using DevExpress.XtraPrinting;

namespace WeightChecking
{
    public partial class frmMain : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        #region Static Properties
        private bool isUpdateClicked = false;

        frmScale _frmScale;
        frmSettings _frmSettings;
        frmMasterData _frmMasterData;
        frmReports _frmReports;

        string _scale = null;
        string _settings = null;
        string _masterData = null;
        string _report = null;

        Timer _timer = new Timer() { Interval = 1000 };

        byte[] _readHoldingRegisterArr = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        byte[] _writeHoldingRegisterArr = { 0, 1 };
        int _countDisconnectPlc = 0;
        private System.Threading.Tasks.Task _tskModbus, _tskProfinet;

        private bool _resetCounter = false;

        string _stationReport = "All";

        int _metalScan = 0, _metalPusher = 0, _weightPusher = 0, _printPusher = 0;
        #endregion

        public frmMain()
        {
            InitializeComponent();

            Load += frmMain_Load;

            FormClosing += MainForm_FormClosing;
        }

        #region Events
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult = DialogResult.OK;
            this.Close();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            barButtonItemTest.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;

            barStaticItemVersion.Caption = Application.ProductVersion;

            AutoUpdater.RunUpdateAsAdmin = false;
            AutoUpdater.DownloadPath = Environment.CurrentDirectory;

            AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;

            //register ribbon barButton click
            this.barButtonItemMain.ItemClick += BarButtonItemMain_ItemClick;
            this.barButtonItemSettings.ItemClick += BarButtonItemSettings_ItemClick;
            this.barButtonItemPrint.ItemClick += BarButtonItemPrint_ItemClick;
            //this.barButtonItemResetCountMetal.ItemClick += BarButtonItemResetCountMetal_ItemClick;
            this._barButtonItemUpVersion.ItemClick += _barButtonItemUpVersion_ItemClick;
            this._barButtonItemRefreshReport.ItemClick += _barButtonItemRefreshReport_ItemClick;
            this._barButtonItemExportExcel.ItemClick += _barButtonItemExportExcel_ItemClick;
            this._barButtonItemExportMasterData.ItemClick += _barButtonItemExportMasterData_ItemClick;
            this._barButtonItemExportMissItem.ItemClick += _barButtonItemExportMissItem_ItemClick;
            _barButtonItemDeleteBox.ItemClick += _barButtonItemDeleteBox_ItemClick;

            this._barCheckItemOutsole.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            this._barCheckItemOutsole.Checked = GlobalVariables.IsOutsoleMode;

            this._barCheckItemOutsole.CheckedChanged += (s, o) =>
            {
                GlobalVariables.IsOutsoleMode = _barCheckItemOutsole.Checked;
            };

            //chon chế độ chỉ hiển thị tab ribbon, ẩn chi tiết group
            //ribbonControl1.Minimized = true;//show tabs
            //this.RibbonVisibility = DevExpress.XtraBars.Ribbon.RibbonVisibility.Hidden;

            this.ribbonControl1.RibbonCaptionAlignment = DevExpress.XtraBars.Ribbon.RibbonCaptionAlignment.Center;

            //đăng ký sự kiện documentClose để xóa cờ báo document đang actived đi, để lần sau bấm chọn barButtonItem mở lại document đó.
            tabbedView1.DocumentClosing += (s, o) =>
            {
                try
                {
                    //TabbedView tabbed = s as TabbedView;
                    if (o.Document.Caption == "Weight Checking")
                    {
                        if (_scale != null)
                        {
                            _scale = null;
                        }
                    }
                    else if (o.Document.Caption == "Settings")
                    {
                        if (_settings != null)
                        {
                            _settings = null;
                        }
                    }
                    else if (o.Document.Caption == "Master Data")
                    {
                        if (_masterData != null)
                        {
                            _masterData = null;
                        }
                    }
                    else if (o.Document.Caption == "Report")
                    {
                        if (_report != null)
                        {
                            _report = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    XtraMessageBox.Show("Lỗi MainForm: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            };

            #region Check role
            if (GlobalVariables.UserLoginInfo.Role == RolesEnum.Operator)
            {
                #region actived form scale
                if (_scale == null)
                {
                    _scale = "Actived";

                    _frmScale = new frmScale();
                    tabbedView1.AddDocument(_frmScale);
                    tabbedView1.ActivateDocument(_frmScale);
                }
                else
                {
                    tabbedView1.ActivateDocument(_frmScale);
                }
                #endregion

                ribbonPageMasterData.Visible = false;
                ribbonPageReports.Visible = false;
            }
            else if (GlobalVariables.UserLoginInfo.Role == RolesEnum.Admin|| GlobalVariables.UserLoginInfo.Role == RolesEnum.Admin1)//Admin
            {
                if (_masterData == null)
                {
                    _masterData = "Actived";

                    _frmMasterData = new frmMasterData();
                    tabbedView1.AddDocument(_frmMasterData);
                    tabbedView1.ActivateDocument(_frmMasterData);
                }
                else
                {
                    tabbedView1.ActivateDocument(_frmMasterData);
                }

                ribbonControl1.SelectedPage = ribbonPageMasterData;

                if (GlobalVariables.UserLoginInfo.Role==RolesEnum.Admin1)
                {
                    _barButtonItemDeleteBox.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
                }
            }
            else if (GlobalVariables.UserLoginInfo.Role == RolesEnum.Report)//report
            {
                if (_report == null)
                {
                    _report = "Actived";

                    _frmReports = new frmReports();
                    tabbedView1.AddDocument(_frmReports);
                    tabbedView1.ActivateDocument(_frmReports);
                }
                else
                {
                    tabbedView1.ActivateDocument(_frmReports);
                }

                ribbonControl1.SelectedPage = ribbonPageReports;

                ribbonPageMasterData.Visible = false;
                _barButtonItemDeleteBox.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            }
            #endregion

            if (!GlobalVariables.IsTest)
            {
                #region Ket noi modbus RTU PLC: Scale, Metal scan
                if (GlobalVariables.IsScale)
                {
                    GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.KetNoi(GlobalVariables.ComPortScale, 9600, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One);

                    Debug.WriteLine($"PLC Status: {GlobalVariables.ModbusStatus}");

                    if (GlobalVariables.ModbusStatus)
                    {
                        //ghi thông số delay trước khi chạy vào máy in
                        //thanh ghi D500 cua PLC Delta DPV14SS2 co dia chi la 4596
                        //D511 -11FF = 4607
                        //thanh ghi D508 cua PLC Delta DPV14SS2 co dia chi la 4604

                        //byte[] mangGhi = new byte[2];
                        //GlobalVariables.MyDriver.SetWord(mangGhi, 0, 2000);

                        //GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.WriteHoldingRegisters(1, 4604, 1, mangGhi);

                        //if (GlobalVariables.ModbusStatus)
                        //{
                        //    MessageBox.Show("Ghi gia tri thanh cong");
                        //}

                        //thanh ghi D500 cua PLC Delta DPV14SS2 co dia chi la 4596
                        GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.ReadHoldingRegisters(1, 4596, 8, ref _readHoldingRegisterArr);

                        //GlobalVariables.RememberInfo.CountMetalScan = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 0);
                        ////update gia tri count vao sự kiện để trong frmScal  nó update lên giao diện
                        //GlobalVariables.MyEvent.CountValue = GlobalVariables.RememberInfo.CountMetalScan;

                        //GlobalVariables.MyEvent.CountValue = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 0);
                        GlobalVariables.MyEvent.ScaleValueStable = GlobalVariables.MyDriver.GetShortAt(_readHoldingRegisterArr, 2);
                        GlobalVariables.MyEvent.ScaleValue = GlobalVariables.MyDriver.GetShortAt(_readHoldingRegisterArr, 4);
                        GlobalVariables.MyEvent.StableScale = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 6);
                        GlobalVariables.MyEvent.SensorBeforeWeightScan = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 8);
                        GlobalVariables.MyEvent.SensorAfterWeightScan = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 10);

                        //var delayConveyor = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 14);

                        //đăng ký sự kiện bật tắt đèn tháp báo cân pass/fail
                        GlobalVariables.MyEvent.EventHandleStatusLightPLC += MyEvent_EventHandleStatusLightPLC;

                        //ghi giá trị tắt đèn tháp xuống PLC
                        //GlobalVariables.MyDriver.ModbusRTUMaster.WriteHoldingRegisters(1, 4602, 1, _writeHoldingRegisterArr);
                        GlobalVariables.MyEvent.StatusLightPLC = 0;

                        //run thread đọc modbus, để đọc các giá trị cân
                        _tskModbus = new System.Threading.Tasks.Task(() => ReadModbus());
                        _tskModbus.Start();
                    }
                    else
                    {
                        MessageBox.Show($"Không thể kết nối được cân (Modbus RTU).{Environment.NewLine}Tắt phần mềm, kiểm tra lại kết nối với PLC rồi mở lại phần mềm.",
                                        "CẢNH BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                #endregion

                #region Ket noi conveyor
                GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.KetNoi(GlobalVariables.IpConveyor);
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
                    _tskProfinet = new System.Threading.Tasks.Task(() => ReadProfinet());
                    _tskProfinet.Start();
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

            _barButtonItemExportMissItem.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            this._barEditItemFromDate.EditValue = this._barEditItemToDate.EditValue = DateTime.Now;

            this._barEditItemCombStation.EditValueChanged += _barEditItemCombStation_EditValueChanged;
            this._barEditItemCombStation.EditValue = "All";
            _timer.Enabled = true;
            _timer.Tick += _timer_Tick;
        }

        private void _barButtonItemDeleteBox_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                frmDeleteBox frmUpdate = new frmDeleteBox();
                frmUpdate.StartPosition = FormStartPosition.CenterScreen;
                frmUpdate.ShowDialog();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Delete Box Fail." + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Method ghi tag điều khiển đèn tháp báo thùng cân Pass/Fail.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyEvent_EventHandleStatusLightPLC(object sender, TagValueChangeEventArgs e)
        {
            _writeHoldingRegisterArr[1] = (byte)e.NewValue;
        Loop1:
            GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.WriteHoldingRegisters(1, 4602, 1, _writeHoldingRegisterArr);

            if (!GlobalVariables.ModbusStatus)
            {
                goto Loop1;
            }

            //else//thùng cân fail
            //{
            //    _writeHoldingRegisterArr[1] = 1;
            //    GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.WriteHoldingRegisters(1, 4602, 1, _writeHoldingRegisterArr);
            //}
        }

        private void _barButtonItemExportMissItem_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel File|*.xlsx";
                    sfd.Title = "Chọn chổ để xuất";
                    sfd.FileName = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}SSFGReportMissItem.xlsx";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {

                        SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                        SplashScreenManager.Default.SetWaitFormCaption("Vui lòng chờ trong giây lát");
                        SplashScreenManager.Default.SetWaitFormDescription("Loading...");

                        var fromDate = (DateTime)_barEditItemFromDate.EditValue;
                        var toDate = (DateTime)_barEditItemToDate.EditValue;
                        //var station = _barEditItemCombStation.EditValue.ToString();

                        using (var connection = GlobalVariables.GetDbConnection())
                        {
                            var parametters = new DynamicParameters();
                            parametters.Add("FromDate", fromDate.ToString("yyyy/MM/dd 00:00:00"));
                            parametters.Add("ToDate", toDate.ToString("yyyy/MM/dd 23:59:59"));
                            parametters.Add("Station", _stationReport);

                            var res = connection.Query<MissProItemModel>("sp_MissingInfoGets", parametters, commandType: CommandType.StoredProcedure).ToList();

                            using (Workbook wb = new Workbook())
                            {
                                //wb.Worksheets.Remove(wb.Worksheets["Sheet1"]);
                                wb.Worksheets["Sheet1"].Name = "MissProItems";

                                #region Data
                                Worksheet ws = wb.Worksheets["MissProItems"];

                                ws.Cells[0, 0].Value = "OC";
                                ws.Cells[0, 1].Value = "Product Code";
                                ws.Cells[0, 2].Value = "Product Name";
                                ws.Cells[0, 3].Value = "QR Code";
                                ws.Cells[0, 4].Value = "Note";
                                ws.Cells[0, 5].Value = "Created Date";

                                CellRange rHeader = ws.Range.FromLTRB(0, 0, 5, 0);//Col-Row;Col-Row. do created new WB nen ko lây theo hàng cot chũ cái đc
                                rHeader.FillColor = Color.Orange;
                                rHeader.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
                                rHeader.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
                                rHeader.Font.Bold = true;


                                ws.Import(res, 1, 0);

                                //ws.Range[$"Q2:Y{res.Count}"].NumberFormat = "#,#0.00";
                                //ws.Range[$"AB2:AC{res.Count}"].NumberFormat = "#,#0";
                                ws.Range[$"E2:E{res.Count + 1}"].NumberFormat = "yyyy/MM/dd HH:mm:ss";

                                ws.Range.FromLTRB(0, 0, 5, res.Count).Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);
                                //ws.FreezeRows(0);
                                //ws.FreezeColumns(3);
                                //ws.FreezePanes(0, 3);
                                ws.Columns.AutoFit(0, 5);
                                #endregion

                                wb.SaveDocument(sfd.FileName);

                                Process.Start(sfd.FileName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi Report exception.");
                XtraMessageBox.Show("Lỗi Report: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SplashScreenManager.CloseForm(false);
            }
        }

        private void _barButtonItemExportMasterData_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel File|*.xlsx";
                sfd.Title = "Chọn chổ để xuất";
                sfd.FileName = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}FinalToBuy.xlsx";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    #region cach 1
                    //grcForecast.DefaultView.ExportToXlsx(sfd.FileName);
                    //if (File.Exists(sfd.FileName))
                    //{
                    //    var mbr = XtraMessageBox.Show("Export thành công !!!\n Bạn có muốn mở file?", "Thông báo", MessageBoxButtons.YesNo);
                    //    if (mbr == DialogResult.Yes)
                    //    {
                    //        Process.Start(sfd.FileName);
                    //    }
                    //}
                    //else
                    //{
                    //    XtraMessageBox.Show("Không tìm thấy file.");
                    //}
                    #endregion

                    //// Create a PrintingSystem component.
                    //DevExpress.XtraPrinting.PrintingSystem ps = new DevExpress.XtraPrinting.PrintingSystem();

                    //// Create a link that will print a control.
                    //DevExpress.XtraPrinting.PrintableComponentLink link = new PrintableComponentLink(ps);

                    //// Specify the control to be printed.
                    //link.Component = grcForecast;
                    //// Generate a report.
                    //link.CreateDocument();

                    //// Export the report to a PDF file.
                    //link.PrintingSystem.ExportToXlsx(sfd.FileName);

                    //Process.Start(sfd.FileName);//open file
                }
            }
        }

        private void _barEditItemCombStation_EditValueChanged(object sender, EventArgs e)
        {
            _stationReport = _barEditItemCombStation.EditValue.ToString();
        }

        private void _barButtonItemExportExcel_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel File|*.xlsx";
                    sfd.Title = "Chọn chổ để xuất";
                    sfd.FileName = $"{DateTime.Now.ToString("yyyyMMddHHmmss")}SSFGReport.xlsx";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {

                        SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                        SplashScreenManager.Default.SetWaitFormCaption("Vui lòng chờ trong giây lát");
                        SplashScreenManager.Default.SetWaitFormDescription("Loading...");

                        var fromDate = (DateTime)_barEditItemFromDate.EditValue;
                        var toDate = (DateTime)_barEditItemToDate.EditValue;
                        //var station = _barEditItemCombStation.EditValue.ToString();

                        using (var connection = GlobalVariables.GetDbConnection())
                        {
                            var parametters = new DynamicParameters();
                            parametters.Add("FromDate", fromDate.ToString("yyyy/MM/dd 00:00:00"));
                            parametters.Add("ToDate", toDate.ToString("yyyy/MM/dd 23:59:59"));
                            parametters.Add("Station", _stationReport);

                            var reportModel = new List<ScanDataReport1Model>();
                            var res = connection.Query<ScanDataReportModel>("sp_tblScanDataGets", parametters, commandType: CommandType.StoredProcedure).ToList();

                            //var reportApproved = new List<ApprovedReportModel>();
                            //var resApproved = connection.Query<ApprovedModel>("sp_tblApprovedPrintLableSelect", parametters, commandType: CommandType.StoredProcedure).ToList();

                            var resMissInfo = connection.Query<MissProItemModel>("sp_MissingInfoGets", parametters, commandType: CommandType.StoredProcedure).ToList();

                            var resScanDataRejectReport = new List<ScanDataRejectReportModel>();
                            var resScanDataReject = connection.Query<ScanDataRejectModel>("sp_tblScanDataRejectSelectFromTo", parametters, commandType: CommandType.StoredProcedure).ToList();
                            var resMetalScanResult = connection.Query<MetalScanResultModel>("sp_tblMetalScanResultSelectFromTo", parametters, commandType: CommandType.StoredProcedure).ToList();

                            using (Workbook wb = new Workbook())
                            {
                                //wb.Worksheets.Remove(wb.Worksheets["Sheet1"]);
                                wb.Worksheets["Sheet1"].Name = "DataScan";
                                //wb.Worksheets.Add("ApprovedPrintLable");
                                wb.Worksheets.Add("DataScanReject");
                                wb.Worksheets.Add("MissProItems");
                                wb.Worksheets.Add("MetalScanResult");

                                #region Data scan
                                Worksheet ws = wb.Worksheets["DataScan"];

                                ws.Cells[0, 0].Value = "BarcodeString";
                                ws.Cells[0, 1].Value = "IdLable";
                                ws.Cells[0, 2].Value = "OcNo";
                                ws.Cells[0, 3].Value = "ProductNumber";
                                ws.Cells[0, 4].Value = "ProductName";
                                ws.Cells[0, 5].Value = "Quantity";
                                ws.Cells[0, 6].Value = "LinePosNo";
                                ws.Cells[0, 7].Value = "Unit";
                                ws.Cells[0, 8].Value = "BoxNo";
                                ws.Cells[0, 9].Value = "CustomerNo";
                                ws.Cells[0, 10].Value = "Location";
                                ws.Cells[0, 11].Value = "BoxPosNo";
                                ws.Cells[0, 12].Value = "Note";
                                ws.Cells[0, 13].Value = "Brand";
                                ws.Cells[0, 14].Value = "Decoration";
                                ws.Cells[0, 15].Value = "MetalScan";
                                ws.Cells[0, 16].Value = "ActualMetalScan";
                                ws.Cells[0, 17].Value = "AveWeight1Prs (g)";
                                ws.Cells[0, 18].Value = "StdNetWeight (g)";
                                ws.Cells[0, 19].Value = "Lower Tolerance (g)";
                                ws.Cells[0, 20].Value = "Upper Tolerance (g)";
                                ws.Cells[0, 21].Value = "BoxWeight (g)";
                                ws.Cells[0, 22].Value = "PackageWeight (g)";
                                ws.Cells[0, 23].Value = "StdGrossWeight (g)";
                                ws.Cells[0, 24].Value = "GrossWeight (g)";
                                ws.Cells[0, 25].Value = "NetWeight (g)";
                                ws.Cells[0, 26].Value = "Deviation (g)";
                                ws.Cells[0, 27].Value = "Pass";
                                ws.Cells[0, 28].Value = "Status";
                                ws.Cells[0, 29].Value = "Calculated (prs)";
                                ws.Cells[0, 30].Value = "DeviationPairs";
                                ws.Cells[0, 31].Value = "CreatedDate";
                                ws.Cells[0, 32].Value = "Station";
                                ws.Cells[0, 33].Value = "UserName";
                                ws.Cells[0, 34].Value = "ApprovedName";
                                ws.Cells[0, 35].Value = "ActualDeviationPairs";
                                ws.Cells[0, 36].Value = "RatioFailWeight";
                                ws.Cells[0, 37].Value = "ProductCategory";

                                CellRange rHeader = ws.Range.FromLTRB(0, 0, 37, 0);//Col-Row;Col-Row. do created new WB nen ko lây theo hàng cot chũ cái đc
                                rHeader.FillColor = Color.Orange;
                                rHeader.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
                                rHeader.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
                                rHeader.Font.Bold = true;

                                foreach (var item in res)
                                {
                                    reportModel.Add(new ScanDataReport1Model()
                                    {
                                        BarcodeString = item.BarcodeString,
                                        IdLabel = item.IdLabel,
                                        OcNo = item.OcNo,
                                        ProductNumber = item.ProductNumber,
                                        ProductName = item.ProductName,
                                        Quantity = item.Quantity,
                                        LinePosNo = item.LinePosNo,
                                        Unit = item.Unit,
                                        BoxNo = item.BoxNo,
                                        CustomerNo = item.CustomerNo,
                                        Location = item.Location.ToString(),
                                        BoxPosNo = item.BoxPosNo,
                                        Note = item.Note,
                                        Brand = item.Brand,
                                        Decoration = item.Decoration,
                                        MetalScan = item.MetalScan,
                                        ActualMetalScan = item.ActualMetalScan,
                                        AveWeight1Prs = item.AveWeight1Prs,
                                        StdNetWeight = item.StdNetWeight,
                                        LowerTolerance = item.LowerTolerance,
                                        UpperTolerance = item.UpperTolerance,
                                        BoxWeight = item.BoxWeight,
                                        PackageWeight = item.PackageWeight,
                                        StdGrossWeight = item.StdGrossWeight,
                                        GrossWeight = item.GrossWeight,
                                        NetWeight = item.NetWeight,
                                        Deviation = item.Deviation,
                                        Pass = item.Pass,
                                        Status = item.Status,
                                        CalculatedPairs = item.CalculatedPairs,
                                        DeviationPairs = item.DeviationPairs,
                                        CreatedDate = item.CreatedDate,
                                        Station = item.Station.ToString(),
                                        UserName = item.UserName,
                                        ApprovedName = item.ApprovedName,
                                        ActualDeviationPairs = item.ActualDeviationPairs,
                                        RatioFailWeight = item.RatioFailWeight,
                                        ProductCategory = item.ProductCategory
                                    });
                                }
                                ws.Import(reportModel, 1, 0);

                                ws.Range[$"R2:Z{res.Count}"].NumberFormat = "#,#0.00";
                                ws.Range[$"AC2:AE{res.Count}"].NumberFormat = "#,#0";
                                ws.Range[$"AF2:AF{res.Count}"].NumberFormat = "yyyy/MM/dd HH:mm:ss";

                                ws.Range.FromLTRB(0, 0, 37, res.Count).Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);
                                //ws.FreezeRows(0);
                                //ws.FreezeColumns(3);
                                ws.FreezePanes(0, 3);
                                ws.Columns.AutoFit(0, 37);
                                #endregion

                                #region data scan reject
                                ws = wb.Worksheets["DataScanReject"];

                                ws.Cells[0, 0].Value = "QR Label";
                                ws.Cells[0, 1].Value = "ID Lable";
                                ws.Cells[0, 2].Value = "OC";
                                ws.Cells[0, 3].Value = "Box No";
                                ws.Cells[0, 4].Value = "Product Code";
                                ws.Cells[0, 5].Value = "Scanner Station";
                                ws.Cells[0, 6].Value = "Reason";
                                ws.Cells[0, 7].Value = "Quantity (Prs)";
                                ws.Cells[0, 8].Value = "GrossWeight";
                                ws.Cells[0, 9].Value = "DeviationPairs";
                                ws.Cells[0, 10].Value = "DeviationWeight";
                                ws.Cells[0, 11].Value = "CreatedDate";
                                ws.Cells[0, 12].Value = "Product Name";

                                rHeader = ws.Range.FromLTRB(0, 0, 12, 0);//Col-Row;Col-Row. do created new WB nen ko lây theo hàng cot chũ cái đc
                                rHeader.FillColor = Color.Orange;
                                rHeader.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
                                rHeader.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
                                rHeader.Font.Bold = true;

                                foreach (var item in resScanDataReject)
                                {
                                    resScanDataRejectReport.Add(new ScanDataRejectReportModel()
                                    {
                                        BarcodeString = item.BarcodeString,
                                        IdLabel = item.IdLabel,
                                        OcNo = item.OcNo,
                                        BoxId = item.BoxId,
                                        ProductNumber = item.ProductNumber,
                                        ProductName = item.ProductName,
                                        Quantity = item.Quantity,
                                        ScannerStation = item.ScannerStation,
                                        Reason = item.Reason,
                                        CreatedDate = item.CreatedDate,
                                        GrossWeight = item.GrossWeight,
                                        DeviationPairs = item.DeviationPairs,
                                        DeviationWeight = item.DeviationWeight,
                                    });
                                }

                                ws.Import(resScanDataRejectReport, 1, 0);

                                //ws.Range[$"Q2:Y{res.Count}"].NumberFormat = "#,#0.00";
                                ws.Range[$"H2:K{res.Count}"].NumberFormat = "#,#0";
                                ws.Range[$"L2:L{res.Count}"].NumberFormat = "yyyy/MM/dd HH:mm:ss";

                                ws.Range.FromLTRB(0, 0, 12, resScanDataRejectReport.Count).Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);
                                //ws.FreezeRows(0);
                                //ws.FreezeColumns(3);
                                ws.FreezePanes(0, 3);
                                ws.Columns.AutoFit(0, 12);
                                #endregion

                                #region Approved print lable
                                //ws = wb.Worksheets["ApprovedPrintLable"];

                                //ws.Cells[0, 0].Value = "User Name";
                                //ws.Cells[0, 1].Value = "ID Lable";
                                //ws.Cells[0, 2].Value = "OC";
                                //ws.Cells[0, 3].Value = "Box No";
                                //ws.Cells[0, 4].Value = "Gross Weight";
                                //ws.Cells[0, 5].Value = "Station";
                                //ws.Cells[0, 6].Value = "Created Date";
                                //ws.Cells[0, 7].Value = "QR Label";
                                //ws.Cells[0, 8].Value = "Aprrove Type";

                                //rHeader = ws.Range.FromLTRB(0, 0, 8, 0);//Col-Row;Col-Row. do created new WB nen ko lây theo hàng cot chũ cái đc
                                //rHeader.FillColor = Color.Orange;
                                //rHeader.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
                                //rHeader.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
                                //rHeader.Font.Bold = true;

                                //foreach (var itemApproved in resApproved)
                                //{
                                //    reportApproved.Add(new ApprovedReportModel()
                                //    {
                                //        UserName = itemApproved.UserName,
                                //        IdLable = itemApproved.IdLable,
                                //        OC = itemApproved.OC,
                                //        BoxNo = itemApproved.BoxNo,
                                //        GrossWeight = itemApproved.GrossWeight,
                                //        Station = itemApproved.Station.ToString(),
                                //        CreatedDate = itemApproved.CreatedDate,
                                //        QRLabel = itemApproved.QRLabel,
                                //        ApproveType = itemApproved.ApproveType,
                                //    });
                                //}
                                //ws.Import(reportApproved, 1, 0);

                                ////ws.Range[$"Q2:Y{res.Count}"].NumberFormat = "#,#0.00";
                                ////ws.Range[$"AB2:AC{res.Count}"].NumberFormat = "#,#0";
                                //ws.Range[$"G2:G" +
                                //    $"{res.Count}"].NumberFormat = "yyyy/MM/dd HH:mm:ss";

                                //ws.Range.FromLTRB(0, 0, 8, reportApproved.Count).Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);
                                ////ws.FreezeRows(0);
                                ////ws.FreezeColumns(3);
                                ////ws.FreezePanes(0, 3);
                                //ws.Columns.AutoFit(0, 8);
                                #endregion

                                #region Missing infomation
                                ws = wb.Worksheets["MissProItems"];

                                ws.Cells[0, 0].Value = "OC";
                                ws.Cells[0, 1].Value = "Product Code";
                                ws.Cells[0, 2].Value = "Product Name";
                                ws.Cells[0, 3].Value = "QR Code";
                                ws.Cells[0, 4].Value = "Note";
                                ws.Cells[0, 5].Value = "Created Date";

                                rHeader = ws.Range.FromLTRB(0, 0, 5, 0);//Col-Row;Col-Row. do created new WB nen ko lây theo hàng cot chũ cái đc
                                rHeader.FillColor = Color.Orange;
                                rHeader.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
                                rHeader.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
                                rHeader.Font.Bold = true;


                                ws.Import(resMissInfo, 1, 0);

                                //ws.Range[$"Q2:Y{res.Count}"].NumberFormat = "#,#0.00";
                                //ws.Range[$"AB2:AC{res.Count}"].NumberFormat = "#,#0";
                                ws.Range[$"F2:F{resMissInfo.Count + 1}"].NumberFormat = "yyyy/MM/dd HH:mm:ss";

                                ws.Range.FromLTRB(0, 0, 5, resMissInfo.Count).Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);
                                //ws.FreezeRows(0);
                                //ws.FreezeColumns(3);
                                //ws.FreezePanes(0, 3);
                                ws.Columns.AutoFit(0, 5);
                                #endregion

                                #region Metal scan result
                                // sửa lại thứ tự cột cho đúng..
                                ws = wb.Worksheets["MetalScanResult"];
                                ws.Cells[0, 0].Value = "Id";
                                ws.Cells[0, 1].Value = "QR Label";
                                ws.Cells[0, 2].Value = "ID Label";
                                ws.Cells[0, 3].Value = "OC";
                                ws.Cells[0, 4].Value = "Box No";
                                ws.Cells[0, 5].Value = "Quantity (Prs)";
                                ws.Cells[0, 6].Value = "Metal check result";
                                ws.Cells[0, 7].Value = "Product item code";

                                ws.Cells[0, 8].Value = "Created Date";
                                ws.Cells[0, 9].Value = "Created machine";
                                ws.Cells[0, 10].Value = "Is actived";

                                rHeader = ws.Range.FromLTRB(0, 0, 10, 0);//Col-Row;Col-Row. do created new WB nen ko lây theo hàng cot chũ cái đc
                                rHeader.FillColor = Color.Orange;
                                rHeader.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
                                rHeader.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
                                rHeader.Font.Bold = true;

                                // format column BoxNo as string value
                                ws[$"C6:C{resMetalScanResult.Count + 1}"].NumberFormat = "@";
                         
                                ws.Import(resMetalScanResult, 1, 0);

                                //ws.Range[$"Q2:Y{res.Count}"].NumberFormat = "#,#0.00";
                                //ws.Range[$"H2:K{res.Count}"].NumberFormat = "#,#0";
                                //ws.Range[$"L2:L{res.Count}"].NumberFormat = "yyyy/MM/dd HH:mm:ss";

                                ws.Range.FromLTRB(0, 0, 9, resScanDataRejectReport.Count).Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);
                                //ws.FreezeRows(0);
                                //ws.FreezeColumns(3);
                                ws.FreezePanes(0, 3);
                                ws.Columns.AutoFit(0, 9);
                                #endregion

                                wb.Worksheets.ActiveWorksheet = wb.Worksheets["DataScan"];

                                wb.SaveDocument(sfd.FileName);

                                Process.Start(sfd.FileName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi Report exception.");
                XtraMessageBox.Show("Lỗi Report: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SplashScreenManager.CloseForm(false);
            }
        }

        private void _barButtonItemRefreshReport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                var fromDate = (DateTime)_barEditItemFromDate.EditValue;
                var toDate = (DateTime)_barEditItemToDate.EditValue;

                if (_report == null)
                {
                    _report = "Actived";

                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                    SplashScreenManager.Default.SetWaitFormCaption("Vui lòng chờ trong giây lát");
                    SplashScreenManager.Default.SetWaitFormDescription("Loading...");

                    _frmReports = new frmReports();
                    _frmReports.FromDate = fromDate.ToString("yyyy/MM/dd 00:00:00");
                    _frmReports.ToDate = toDate.ToString("yyyy/MM/dd 23:59:59");
                    _frmReports.Station = _stationReport;

                    tabbedView1.AddDocument(_frmReports);
                    tabbedView1.ActivateDocument(_frmReports);
                }
                else
                {
                    _frmReports.FromDate = fromDate.ToString("yyyy/MM/dd 00:00:00");
                    _frmReports.ToDate = toDate.ToString("yyyy/MM/dd 23:59:59");
                    _frmReports.Station = _stationReport;

                    tabbedView1.ActivateDocument(_frmReports);
                    GlobalVariables.MyEvent.RefreshReport = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi MainForm ReFresh masterData exception.");
                XtraMessageBox.Show("Lỗi MainForm: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SplashScreenManager.CloseForm(false);
            }
        }

        private void _barButtonItemUpVersion_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                isUpdateClicked = true;
                //string UUrl = "\\10.40.10.9\\Public$\\05_IT\\01_Update\\21- IDCScaleSystem\\Update.xml";
                string UUrl = GlobalVariables.UpdatePath;
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

        private void _timer_Tick(object sender, EventArgs e)
        {
            Timer t = (Timer)sender;
            try
            {
                t.Enabled = false;

                if (this.InvokeRequired)
                {
                    this?.Invoke(new Action(()=> {
                        barStaticItemStatus.Caption = $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm")} " +
                           $"| {GlobalVariables.UserLoginInfo.UserName}" +
                           $" | ConveyorStatus: {GlobalVariables.ConveyorStatus}. S1-{GlobalVariables.MyEvent.SensorBeforeMetalScan}. Sm-{GlobalVariables.MyEvent.SensorMiddleMetal}" +
                           $";MC-{GlobalVariables.MyEvent.MetalCheckResult};S2-{GlobalVariables.MyEvent.SensorAfterMetalScan};PL-{GlobalVariables.MyEvent.SensorAfterPrintScannerFG};PR-{GlobalVariables.MyEvent.SensorAfterPrintScannerPrinting}" +
                           $". Pusher: MS-{_metalScan};M-{_metalPusher};W-{_weightPusher};P-{_printPusher}" +
                           $" | ModbusRTUStatus: {GlobalVariables.ModbusStatus}. SV:{GlobalVariables.MyEvent.ScaleValue}-ST:{GlobalVariables.MyEvent.ScaleValueStable}" +
                           $"-Stable:{GlobalVariables.MyEvent.StableScale}-SIn:{GlobalVariables.MyEvent.SensorBeforeWeightScan}" 
                           + $" | PrintStatus: {GlobalVariables.PrintConnectionStatus}";

                        barStaticItemVersion.Caption = $"{GlobalVariables.AppStatus}|{Application.ProductVersion}";
                    }));
                }
                else
                {
                    this?.Invoke(new Action(() => {
                        barStaticItemStatus.Caption = $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm")} " +
                           $"| {GlobalVariables.UserLoginInfo.UserName}" +
                           $" | ConveyorStatus: {GlobalVariables.ConveyorStatus}. S1-{GlobalVariables.MyEvent.SensorBeforeMetalScan}. Sm-{GlobalVariables.MyEvent.SensorMiddleMetal}" +
                           $";MC-{GlobalVariables.MyEvent.MetalCheckResult};S2-{GlobalVariables.MyEvent.SensorAfterMetalScan};PL-{GlobalVariables.MyEvent.SensorAfterPrintScannerFG};PR-{GlobalVariables.MyEvent.SensorAfterPrintScannerPrinting}" +
                           $". Pusher: MS-{_metalScan};M-{_metalPusher};W-{_weightPusher};P-{_printPusher}" +
                           $" | ModbusRTUStatus: {GlobalVariables.ModbusStatus}. SV:{GlobalVariables.MyEvent.ScaleValue}-ST:{GlobalVariables.MyEvent.ScaleValueStable}" +
                           $"-Stable:{GlobalVariables.MyEvent.StableScale}-SIn:{GlobalVariables.MyEvent.SensorBeforeWeightScan}" +
                           $" | PrintStatus: {GlobalVariables.PrintConnectionStatus}.";// PrintResult: {GlobalVariables.PrintResult}";//--{GlobalVariables.PrintedResult}

                        barStaticItemVersion.Caption = $"{GlobalVariables.AppStatus}|{Application.ProductVersion}";
                    }));
                }
            }
            catch (Exception ex)
            {

            }
            finally { t.Enabled = true; }
        }

        private void BarButtonItemSettings_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                if (_settings == null)
                {
                    _settings = "Actived";

                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                    SplashScreenManager.Default.SetWaitFormCaption("Vui lòng chờ trong giây lát");
                    SplashScreenManager.Default.SetWaitFormDescription("Loading...");

                    _frmSettings = new frmSettings();
                    tabbedView1.AddDocument(_frmSettings);
                    tabbedView1.ActivateDocument(_frmSettings);

                    SplashScreenManager.CloseForm(false);
                }
                else
                {
                    tabbedView1.ActivateDocument(_frmSettings);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Lỗi MainForm: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BarButtonItemMain_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                if (_scale == null)
                {
                    _scale = "Actived";

                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                    SplashScreenManager.Default.SetWaitFormCaption("Vui lòng chờ trong giây lát");
                    SplashScreenManager.Default.SetWaitFormDescription("Loading...");

                    _frmScale = new frmScale();
                    tabbedView1.AddDocument(_frmScale);
                    tabbedView1.ActivateDocument(_frmScale);

                    SplashScreenManager.CloseForm(false);
                }
                else
                {
                    tabbedView1.ActivateDocument(_frmScale);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show("Lỗi MainForm: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BarButtonItemPrint_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmConfirmPrint nf = new frmConfirmPrint();
            //nf.ConfirmPrintInfo.IdLabel = GlobalVariables.IdLabel;
            //nf.ConfirmPrintInfo.OcNo = GlobalVariables.OcNo;
            //nf.ConfirmPrintInfo.BoxNo = GlobalVariables.BoxNo;
            //nf.ConfirmPrintInfo.Weight = GlobalVariables.RealWeight;
            nf.ShowDialog();
        }

        private void BarButtonItemResetCountMetal_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

        }

        //Get data from winline update to local Databases.
        private void barButtonItemGetDataWL_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                using (var connection = GlobalVariables.GetDbConnectionWinline())
                {
                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                    SplashScreenManager.Default.SetWaitFormCaption("Vui lòng chờ trong giây lát");
                    SplashScreenManager.Default.SetWaitFormDescription("Loading...");

                    var res = connection.Query<WinlineDataModel>("sp_IdcScanScaleGetCoreData").ToList();

                    if (res != null && res.Count > 0)
                    {
                        using (var con = GlobalVariables.GetDbConnection())
                        {
                            //truncate data
                            con.Execute("truncate table tblWinlineProductsInfo");

                            var _insertCount = con.Execute($"Insert into tblWinlineProductsInfo (CodeItemSize,ProductNumber," +
                            $"ProductName,ProductCategory,Brand,Decoration,MainProductNo,MainProductName,Color,SizeCode," +
                            $"SizeName,Weight,LeftWeight,RightWeight,BoxType,ToolingNo,PackingBoxType,CustomeUsePb) " +
                       $"values (@CodeItemSize,@ProductNumber,@ProductName,@ProductCategory,@Brand,@Decoration,@MainProductNo," +
                       $"@MainProductName,@Color,@SizeCode,@SizeName,@Weight,@LeftWeight,@RightWeight,@BoxType,@ToolingNo" +
                       $",@PackingBoxType,@CustomeUsePb)", res);

                            if (_insertCount == res.Count)
                            {
                                XtraMessageBox.Show($"Get data from winline Ok.  Rows inserted {_insertCount}/{ res.Count}.", "INFO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                XtraMessageBox.Show($"Get data from winline fail. Rows inserted {_insertCount}/{ res.Count}.", "INFO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        GlobalVariables.MyEvent.RefreshStatus = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Get data from winline exception.");
                XtraMessageBox.Show("Lỗi MainForm Get data from winline: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SplashScreenManager.CloseForm(false);
                GlobalVariables.MyEvent.RefreshStatus = true;
            }
        }

        private void barButtonItemRefreshData_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                if (_masterData == null)
                {
                    _masterData = "Actived";

                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                    SplashScreenManager.Default.SetWaitFormCaption("Vui lòng chờ trong giây lát");
                    SplashScreenManager.Default.SetWaitFormDescription("Loading...");

                    _frmMasterData = new frmMasterData();
                    tabbedView1.AddDocument(_frmMasterData);
                    tabbedView1.ActivateDocument(_frmMasterData);
                }
                else
                {
                    tabbedView1.ActivateDocument(_frmMasterData);
                    GlobalVariables.MyEvent.RefreshStatus = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi MainForm ReFresh masterData exception.");
                XtraMessageBox.Show("Lỗi MainForm: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SplashScreenManager.CloseForm(false);
            }
        }

        private void barButtonItemResetCountMetal_ItemClick_1(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            _resetCounter = true;

            GlobalVariables.RememberInfo.GoodBoxPrinting = 0;
            GlobalVariables.RememberInfo.GoodBoxNoPrinting = 0;
            GlobalVariables.RememberInfo.FailBoxPrinting = 0;
            GlobalVariables.RememberInfo.FailBoxNoPrinting = 0;
            GlobalVariables.RememberInfo.MetalScan = 0;
            GlobalVariables.RememberInfo.NoMetalScan = 0;
            GlobalVariables.RememberInfo.CountMetalScan = 0;

            GlobalVariables.MyEvent.RefreshStatus = true;

            string json = JsonConvert.SerializeObject(GlobalVariables.RememberInfo);
            File.WriteAllText(@"./RememberInfo.json", json);
        }

        private void barButtonItemImport_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Excel File|*.xlsx";
                ofd.Title = "Import Excel File";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                    SplashScreenManager.Default.SetWaitFormCaption("Vui lòng chờ trong giây lát");
                    SplashScreenManager.Default.SetWaitFormDescription("Loading...");
                    List<tblCoreDataCodeItemSizeModel> coreData = new List<tblCoreDataCodeItemSizeModel>();

                    #region Get data from template excel
                    using (Workbook wb = new Workbook())
                    {
                        wb.LoadDocument(ofd.FileName);

                        if (wb.Worksheets.Count > 0)
                        {
                            Worksheet ws = wb.Worksheets[0];

                            //Get ra số hàng và cột có data
                            //index bắt đầu từ 1
                            var _usedRange = ws.GetUsedRange();
                            int _rowUsed = _usedRange.RowCount;
                            int _colUsed = _usedRange.ColumnCount;

                            for (int i = 5; i < _rowUsed + 5; i++)
                            {
                                Row _row = ws.Rows[i];
                                if (!string.IsNullOrEmpty(_row[$"A{i}"].Value.TextValue))
                                {
                                    coreData.Add(new tblCoreDataCodeItemSizeModel()
                                    {
                                        CodeItemSize = _row[$"A{i}"].Value.TextValue,
                                        MainItemName = _row[$"B{i}"].Value.TextValue,
                                        MetalScan = (int)_row[$"C{i}"].Value.NumericValue,
                                        Color = _row[$"D{i}"].Value.TextValue,
                                        Printing = (int)_row[$"E{i}"].Value.NumericValue,
                                        Date = _row[$"F{i}"].Value.DateTimeValue.ToString(),
                                        Size = _row[$"G{i}"].Value.TextValue,
                                        AveWeight1Prs = _row[$"M{i}"].Value.NumericValue,
                                        BoxQtyBx1 = _row[$"O{i}"].Value.NumericValue,
                                        BoxQtyBx2 = _row[$"P{i}"].Value.NumericValue,
                                        BoxQtyBx3 = _row[$"Q{i}"].Value.NumericValue,
                                        BoxQtyBx4 = _row[$"R{i}"].Value.NumericValue,
                                        BoxWeightBx1 = _row[$"S{i}"].Value.NumericValue,
                                        BoxWeightBx2 = _row[$"T{i}"].Value.NumericValue,
                                        BoxWeightBx3 = _row[$"U{i}"].Value.NumericValue,
                                        BoxWeightBx4 = _row[$"V{i}"].Value.NumericValue,
                                        PartitionQty = _row[$"W{i}"].Value.NumericValue,
                                        PlasicBag1Qty = _row[$"X{i}"].Value.NumericValue,
                                        PlasicBag2Qty = _row[$"Y{i}"].Value.NumericValue,
                                        WrapSheetQty = _row[$"Z{i}"].Value.NumericValue,
                                        FoamSheetQty = _row[$"AA{i}"].Value.NumericValue,
                                        PlasicBag1Weight = _row[$"AF{i}"].Value.NumericValue,
                                        PlasicBag2Weight = _row[$"AG{i}"].Value.NumericValue,
                                        WrapSheetWeight = _row[$"AH{i}"].Value.NumericValue,
                                        FoamSheetWeight = _row[$"AI{i}"].Value.NumericValue,
                                        LowerToleranceOfCartonBox = _row[$"AN{i}"].Value.NumericValue,
                                        UpperToleranceOfCartonBox = _row[$"AO{i}"].Value.NumericValue,
                                        LowerToleranceOfPlasticBox = _row[$"AP{i}"].Value.NumericValue,
                                        UpperToleranceOfPlasticBox = _row[$"AQ{i}"].Value.NumericValue,
                                    }); ;
                                }
                            }
                        }
                        else
                        {
                            XtraMessageBox.Show($"File temple is empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    #endregion

                    #region Log DB
                    using (var con = GlobalVariables.GetDbConnection())
                    {
                        con.Execute("truncate table tblCoreDataCodeItemSize");//xóa hết data trong bảng rồi add vào lại

                        var executeResult = con.Execute("INSERT INTO tblCoreDataCodeItemSize (CodeItemSize,MainItemName,MetalScan,Color,Printing,Date,Size,AveWeight1Prs" +
                            ",BoxQtyBx1,BoxQtyBx2,BoxQtyBx3,BoxQtyBx4,BoxWeightBx1,BoxWeightBx2,BoxWeightBx3,BoxWeightBx4," +
                               "PartitionQty,PlasicBag1Qty,PlasicBag2Qty,WrapSheetQty,FoamSheetQty,PartitionWeight,PlasicBag1Weight,PlasicBag2Weight" +
                               ",WrapSheetWeight,FoamSheetWeight,PlasicBoxWeight,LowerToleranceOfCartonBox,UpperToleranceOfCartonBox,LowerToleranceOfPlasticBox,UpperToleranceOfPlasticBox) " +
                           "VALUES (@CodeItemSize,@MainItemName,@MetalScan,@Color,@Printing,@Date,@Size,@AveWeight1Prs,@BoxQtyBx1,@BoxQtyBx2,@BoxQtyBx3,@BoxQtyBx4," +
                                    "@BoxWeightBx1,@BoxWeightBx2,@BoxWeightBx3,@BoxWeightBx4," +
                               "@PartitionQty,@PlasicBag1Qty,@PlasicBag2Qty,@WrapSheetQty,@FoamSheetQty,@PartitionWeight,@PlasicBag1Weight,@PlasicBag2Weight" +
                               ",@WrapSheetWeight,@FoamSheetWeight,@PlasicBoxWeight,@LowerToleranceOfCartonBox,@UpperToleranceOfCartonBox,@LowerToleranceOfPlasticBox,@UpperToleranceOfPlasticBox)"
                           , coreData);

                        //foreach (var item in coreData)
                        //{
                        //    var para = new DynamicParameters();

                        //    para.Add("@CodeItemSize", item.CodeItemSize);
                        //    para.Add("@MainItemName", item.MainItemName);
                        //    para.Add("@MetalScan", item.MetalScan);
                        //    para.Add("@Color", item.Color);
                        //    para.Add("@Printing", item.Printing);
                        //    para.Add("@Date", item.Date);
                        //    para.Add("@Size", item.Size);
                        //    para.Add("@AveWeight1Prs", item.AveWeight1Prs);
                        //    para.Add("@BoxQtyBx1", item.BoxQtyBx1);
                        //    para.Add("@BoxQtyBx2", item.BoxQtyBx2);
                        //    para.Add("@BoxQtyBx3", item.BoxQtyBx3);
                        //    para.Add("@BoxQtyBx4", item.BoxQtyBx4);
                        //    para.Add("@BoxWeightBx1", item.BoxWeightBx1);
                        //    para.Add("@BoxWeightBx2", item.BoxWeightBx2);
                        //    para.Add("@BoxWeightBx3", item.BoxWeightBx3);
                        //    para.Add("@BoxWeightBx4", item.BoxWeightBx4);
                        //    para.Add("@PartitionQty", item.PartitionQty);
                        //    para.Add("@PlasicBag1Qty", item.PlasicBag1Qty);
                        //    para.Add("@PlasicBag2Qty", item.PlasicBag2Qty);
                        //    para.Add("@WrapSheetQty", item.WrapSheetQty);
                        //    para.Add("@FoamSheetQty", item.FoamSheetQty);
                        //    para.Add("@PartitionWeight", item.PartitionWeight);
                        //    para.Add("@PlasicBag1Weight", item.PlasicBag1Weight);
                        //    para.Add("@PlasicBag2Weight", item.PlasicBag2Weight);
                        //    para.Add("@WrapSheetWeight", item.WrapSheetWeight);
                        //    para.Add("@FoamSheetWeight", item.FoamSheetWeight);
                        //    para.Add("@PlasicBoxWeight", item.PlasicBoxWeight);
                        //    para.Add("@LowerToleranceOfCartonBox", item.LowerToleranceOfPlasticBox);
                        //    para.Add("@UpperToleranceOfCartonBox", item.UpperToleranceOfCartonBox);
                        //    para.Add("@LowerToleranceOfPlasticBox", item.LowerToleranceOfPlasticBox);
                        //    para.Add("@UpperToleranceOfPlasticBox", item.UpperToleranceOfPlasticBox);
                        //    con.Execute("sp_tblCoreDataCodeitemSizeInsert", para, commandType: CommandType.StoredProcedure);
                        //}
                    }
                    #endregion

                    GlobalVariables.MyEvent.RefreshStatus = true;
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Update fail.{Environment.NewLine}{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SplashScreenManager.CloseForm(false);
            }
        }

        //update
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

        private void barButtonItemTest_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (GlobalVariables.MyEvent.SensorAfterWeightScan == 1)
            {
                GlobalVariables.MyEvent.SensorAfterWeightScan = 0;
            }
            else
            {
                GlobalVariables.MyEvent.SensorAfterWeightScan = 1;
            }

            Debug.WriteLine($"SensorAfterWeightScan: {GlobalVariables.MyEvent.SensorAfterWeightScan}");
        }

        private void barButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

        }

        private void _barButtonItemUpVersion_ItemClick_1(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

        }

        private async void AutoUpdater_ApplicationExitEvent()
        {
            Text = @"Closing application...";
            await System.Threading.Tasks.Task.Delay(3000);
            Application.Exit();
        }

        #endregion

        public void ReadModbus()
        {
            while (true)
            {
                #region Đọc các giá trị từ PLC Cân
                if (GlobalVariables.IsScale)
                {
                    if (GlobalVariables.ModbusStatus)
                    {
                        //thanh ghi D500 cua PLC Delta DPV14SS2 co dia chi la 4596
                        GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.ReadHoldingRegisters(1, 4596, 7, ref _readHoldingRegisterArr);

                        //GlobalVariables.MyEvent.CountValue = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 0);
                        GlobalVariables.MyEvent.ScaleValue = GlobalVariables.MyDriver.GetShortAt(_readHoldingRegisterArr, 2);
                        GlobalVariables.MyEvent.ScaleValueStable = GlobalVariables.MyDriver.GetShortAt(_readHoldingRegisterArr, 4);
                        GlobalVariables.MyEvent.StableScale = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 6);
                        GlobalVariables.MyEvent.SensorBeforeWeightScan = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 8);
                        GlobalVariables.MyEvent.SensorAfterWeightScan = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 10);
                    }
                    else
                    {
                        _countDisconnectPlc += 1;
                        Debug.WriteLine($"Dem mat ket noi modbus RTU:{_countDisconnectPlc}");
                        if (_countDisconnectPlc >= 3)
                        {
                            _countDisconnectPlc = 0;
                            GlobalVariables.MyDriver.ModbusRTUMaster.NgatKetNoi();

                            GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.KetNoi(GlobalVariables.ComPortScale, 9600, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One);

                            Debug.WriteLine($"Ket noi lai modbus RTU. Result: {GlobalVariables.ModbusStatus}");
                        }
                    }
                }
                #endregion

                #region Đọc các giá trị từ PLC conveyor s7-1200, profinet
                //if (GlobalVariables.ConveyorStatus == "GOOD")
                //{
                //    var resultData = GlobalVariables.MyDriver.S7Ethernet.Client.DocDB(1, 0, 10);

                //    GlobalVariables.ConveyorStatus = resultData.TrangThai;

                //    if (resultData.TrangThai == "GOOD")
                //    {
                //        GlobalVariables.ConveyorStatus = resultData.TrangThai;

                //        _metalScan = resultData.MangGiaTri[0];
                //        _weightPusher = resultData.MangGiaTri[1];
                //        _printPusher = resultData.MangGiaTri[2];
                //        _metalPusher = resultData.MangGiaTri[6];

                //        //vùng nhớ chứa trạng thái của sensor là DB1[3], truoc vị trí metal scan, để tính thời gian quét QR code. 1-On;0-off
                //        GlobalVariables.MyEvent.SensorBeforeMetalScan = resultData.MangGiaTri[3];
                //        //sensor đặt ngay sau máy quét kim loại, báo là thùng hàng đã qua metal scan. 1-On;0-off
                //        GlobalVariables.MyEvent.SensorAfterMetalScan = resultData.MangGiaTri[5];
                //        //vùng nhớ báo kết quả check metal. 0-pass; 1-Fail
                //        GlobalVariables.MyEvent.MetalCheckResult = resultData.MangGiaTri[4];
                //        //vùng nhớ báo tin hiệu sensor ngay vị trí bàn nâng chuyển 3 hướng sau vị trí metal scanner
                //        GlobalVariables.MyEvent.SensorMiddleMetal = resultData.MangGiaTri[7];

                //        //Vùng nhớ báo tín hiệu sensor 2 vị trí sau scannerPrint
                //        GlobalVariables.MyEvent.SensorAfterPrintScannerFG = resultData.MangGiaTri[8];
                //        GlobalVariables.MyEvent.SensorAfterPrintScannerPrinting = resultData.MangGiaTri[9];
                //    }
                //}
                //else
                //{
                //    GlobalVariables.MyDriver.S7Ethernet.Client.NgatKetNoi();

                //    GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.KetNoi(GlobalVariables.IpConveyor);
                //}
                #endregion

                System.Threading.Thread.Sleep(100);
            }
        }

        public void ReadProfinet()
        {
            bool weightPushFlag = false;
            while (true)
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

                    GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.KetNoi(GlobalVariables.IpConveyor);
                }
                #endregion

                System.Threading.Thread.Sleep(100);
            }
        }
    }
}