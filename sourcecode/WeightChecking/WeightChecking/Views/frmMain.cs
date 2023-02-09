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

        Timer _timer = new Timer() { Interval = 500 };

        byte[] _readHoldingRegisterArr = { 0, 0 };
        int _countDisconnectPlc = 0;
        private System.Threading.Tasks.Task _tskModbus;

        private bool _resetCounter = false;

        string _stationReport = "All";
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
            else if (GlobalVariables.UserLoginInfo.Role == RolesEnum.Admin)//Admin
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
            }
            #endregion

            #region Ket noi modbus RTU PLC: Scale, Metal scan
            if (GlobalVariables.IsScale)
            {
                GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.KetNoi(GlobalVariables.ComPort, 9600, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One);

                Console.WriteLine($"PLC Status: {GlobalVariables.ModbusStatus}");

                if (GlobalVariables.ModbusStatus)
                {
                    _tskModbus = new System.Threading.Tasks.Task(() => ReadModbus());
                    _tskModbus.Start();
                }
                else
                {
                    MessageBox.Show($"Không thể kết nối được bộ đếm dò kim loại.{Environment.NewLine}Tắt phần mềm, kiểm tra lại kết nối với PLC rồi mở lại phần mềm.",
                                    "CẢNH BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            #endregion

            #region Ket noi conveyor
            GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.KetNoi(GlobalVariables.IpScale);
            Console.WriteLine($"Conveyor Status: {GlobalVariables.ConveyorStatus}");

            if (GlobalVariables.ConveyorStatus == "GOOD")
            {
                GlobalVariables.ConveyorStatus = GlobalVariables.MyDriver.S7Ethernet.Client.GhiDB(1, 0, 3, GlobalVariables.DataWriteDb1);
            }
            else
            {
                MessageBox.Show($"Không thể kết nối được bộ đếm dò kim loại.{Environment.NewLine}Tắt phần mềm, kiểm tra lại kết nối với PLC rồi mở lại phần mềm.",
                                "CẢNH BÁO", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            #endregion

            _barButtonItemExportMissItem.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            this._barEditItemFromDate.EditValue = this._barEditItemToDate.EditValue = DateTime.Now;

            this._barEditItemCombStation.EditValueChanged += _barEditItemCombStation_EditValueChanged;
            this._barEditItemCombStation.EditValue = "All";
            _timer.Enabled = true;
            _timer.Tick += _timer_Tick;
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

                            var reportApproved = new List<ApprovedReportModel>();
                            var resApproved = connection.Query<ApprovedModel>("sp_tblApprovedPrintLableSelect", parametters, commandType: CommandType.StoredProcedure).ToList();

                            var resMissInfo = connection.Query<MissProItemModel>("sp_MissingInfoGets", parametters, commandType: CommandType.StoredProcedure).ToList();

                            using (Workbook wb = new Workbook())
                            {
                                //wb.Worksheets.Remove(wb.Worksheets["Sheet1"]);
                                wb.Worksheets["Sheet1"].Name = "DataScan";
                                wb.Worksheets.Add("ApprovedPrintLable");
                                wb.Worksheets.Add("MissProItems");

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
                                ws.Cells[0, 19].Value = "Tolerance (g)";
                                ws.Cells[0, 20].Value = "BoxWeight (g)";
                                ws.Cells[0, 21].Value = "PackageWeight (g)";
                                ws.Cells[0, 22].Value = "StdGrossWeight (g)";
                                ws.Cells[0, 23].Value = "GrossWeight (g)";
                                ws.Cells[0, 24].Value = "NetWeight (g)";
                                ws.Cells[0, 25].Value = "Deviation (g)";
                                ws.Cells[0, 26].Value = "Pass";
                                ws.Cells[0, 27].Value = "Status";
                                ws.Cells[0, 28].Value = "Calculated (prs)";
                                ws.Cells[0, 29].Value = "DeviationPairs";
                                ws.Cells[0, 30].Value = "CreatedDate";
                                ws.Cells[0, 31].Value = "Station";
                                ws.Cells[0, 32].Value = "UserName";
                                ws.Cells[0, 33].Value = "ApprovedName";
                                ws.Cells[0, 34].Value = "ActualDeviationPairs";

                                CellRange rHeader = ws.Range.FromLTRB(0, 0, 34, 0);//Col-Row;Col-Row. do created new WB nen ko lây theo hàng cot chũ cái đc
                                rHeader.FillColor = Color.Orange;
                                rHeader.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
                                rHeader.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
                                rHeader.Font.Bold = true;

                                foreach (var item in res)
                                {
                                    reportModel.Add(new ScanDataReport1Model()
                                    {
                                        BarcodeString = item.BarcodeString,
                                        IdLable = item.IdLable,
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
                                        Tolerance = item.Tolerance,
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
                                        ActualDeviationPairs = item.ActualDeviationPairs
                                    });
                                }
                                ws.Import(reportModel, 1, 0);

                                ws.Range[$"R2:Z{res.Count}"].NumberFormat = "#,#0.00";
                                ws.Range[$"AC2:AD{res.Count}"].NumberFormat = "#,#0";
                                ws.Range[$"AE2:AE{res.Count}"].NumberFormat = "yyyy/MM/dd HH:mm:ss";

                                ws.Range.FromLTRB(0, 0, 34, res.Count).Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);
                                //ws.FreezeRows(0);
                                //ws.FreezeColumns(3);
                                ws.FreezePanes(0, 3);
                                ws.Columns.AutoFit(0, 34);
                                #endregion

                                #region Approved print lable
                                ws = wb.Worksheets["ApprovedPrintLable"];

                                ws.Cells[0, 0].Value = "User Name";
                                ws.Cells[0, 1].Value = "ID Lable";
                                ws.Cells[0, 2].Value = "OC";
                                ws.Cells[0, 3].Value = "Box No";
                                ws.Cells[0, 4].Value = "Gross Weight";
                                ws.Cells[0, 5].Value = "Station";
                                ws.Cells[0, 6].Value = "Created Date";
                                ws.Cells[0, 7].Value = "QR Label";
                                ws.Cells[0, 8].Value = "Aprrove Type";

                                rHeader = ws.Range.FromLTRB(0, 0, 8, 0);//Col-Row;Col-Row. do created new WB nen ko lây theo hàng cot chũ cái đc
                                rHeader.FillColor = Color.Orange;
                                rHeader.Alignment.Horizontal = SpreadsheetHorizontalAlignment.Center;
                                rHeader.Alignment.Vertical = SpreadsheetVerticalAlignment.Center;
                                rHeader.Font.Bold = true;

                                foreach (var itemApproved in resApproved)
                                {
                                    reportApproved.Add(new ApprovedReportModel()
                                    {
                                        UserName = itemApproved.UserName,
                                        IdLable = itemApproved.IdLable,
                                        OC = itemApproved.OC,
                                        BoxNo = itemApproved.BoxNo,
                                        GrossWeight = itemApproved.GrossWeight,
                                        Station = itemApproved.Station.ToString(),
                                        CreatedDate = itemApproved.CreatedDate,
                                        QRLabel = itemApproved.QRLabel,
                                        ApproveType = itemApproved.ApproveType,
                                    });
                                }
                                ws.Import(reportApproved, 1, 0);

                                //ws.Range[$"Q2:Y{res.Count}"].NumberFormat = "#,#0.00";
                                //ws.Range[$"AB2:AC{res.Count}"].NumberFormat = "#,#0";
                                ws.Range[$"G2:G" +
                                    $"{res.Count}"].NumberFormat = "yyyy/MM/dd HH:mm:ss";

                                ws.Range.FromLTRB(0, 0, 8, reportApproved.Count).Borders.SetAllBorders(Color.Black, BorderLineStyle.Thin);
                                //ws.FreezeRows(0);
                                //ws.FreezeColumns(3);
                                //ws.FreezePanes(0, 3);
                                ws.Columns.AutoFit(0, 8);
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
                string UUrl = "\\\\192.168.1.241\\FramasPublic\\PUBLIC_Able to deleted\\22 IT\\01-UpdateApp\\11-SSFG_IDC\\1.Station1BeforePrint\\Update.xml";
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
            t.Enabled = false;

            barStaticItemStatus.Caption = $"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")} " +
                $"| {GlobalVariables.UserLoginInfo.UserName} | ScaleStatus: {GlobalVariables.ScaleStatus}" +
                $" | ConveyorStatus: {GlobalVariables.ConveyorStatus}" +
                $" | CounterStatus: {GlobalVariables.ModbusStatus}";

            t.Enabled = true;
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
                                        BoxQtyBx1 = (int)_row[$"O{i}"].Value.NumericValue,
                                        BoxQtyBx2 = (int)_row[$"P{i}"].Value.NumericValue,
                                        BoxQtyBx3 = (int)_row[$"Q{i}"].Value.NumericValue,
                                        BoxQtyBx4 = (int)_row[$"R{i}"].Value.NumericValue,
                                        BoxWeightBx1 = _row[$"S{i}"].Value.NumericValue,
                                        BoxWeightBx2 = _row[$"T{i}"].Value.NumericValue,
                                        BoxWeightBx3 = _row[$"U{i}"].Value.NumericValue,
                                        BoxWeightBx4 = _row[$"V{i}"].Value.NumericValue,
                                        PartitionQty = (int)_row[$"W{i}"].Value.NumericValue,
                                        PlasicBag1Qty = (int)_row[$"X{i}"].Value.NumericValue,
                                        PlasicBag2Qty = (int)_row[$"Y{i}"].Value.NumericValue,
                                        WrapSheetQty = (int)_row[$"Z{i}"].Value.NumericValue,
                                        FoamSheetQty = (int)_row[$"AA{i}"].Value.NumericValue,
                                        PlasicBag1Weight = _row[$"AF{i}"].Value.NumericValue,
                                        PlasicBag2Weight = _row[$"AG{i}"].Value.NumericValue,
                                        WrapSheetWeight = _row[$"AH{i}"].Value.NumericValue,
                                        FoamSheetWeight = _row[$"AI{i}"].Value.NumericValue
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
                        foreach (var item in coreData)
                        {
                            var para = new DynamicParameters();

                            var sfdsf = con.Query<tblCoreDataCodeItemSizeModel>($"select * from tblCoreDataCodeItemSize " +
                                $"where CodeItemSize = '{item.CodeItemSize}' and Printing = {item.Printing}").FirstOrDefault();
                            if (sfdsf == null)
                            {
                                para.Add("@CodeItemSize", item.CodeItemSize);
                                para.Add("@MainItemName", item.MainItemName);
                                para.Add("@MetalScan", item.MetalScan);
                                para.Add("@Color", item.Color);
                                para.Add("@Printing", item.Printing);
                                para.Add("@Date", item.Date);
                                para.Add("@Size", item.Size);
                                para.Add("@AveWeight1Prs", item.AveWeight1Prs);
                                para.Add("@BoxQtyBx1", item.BoxQtyBx1);
                                para.Add("@BoxQtyBx2", item.BoxQtyBx2);
                                para.Add("@BoxQtyBx3", item.BoxQtyBx3);
                                para.Add("@BoxQtyBx4", item.BoxQtyBx4);
                                para.Add("@BoxWeightBx1", item.BoxWeightBx1);
                                para.Add("@BoxWeightBx2", item.BoxWeightBx2);
                                para.Add("@BoxWeightBx3", item.BoxWeightBx3);
                                para.Add("@BoxWeightBx4", item.BoxWeightBx4);
                                para.Add("@PartitionQty", item.PartitionQty);
                                para.Add("@PlasicBag1Qty", item.PlasicBag1Qty);
                                para.Add("@PlasicBag2Qty", item.PlasicBag2Qty);
                                para.Add("@WrapSheetQty", item.WrapSheetQty);
                                para.Add("@FoamSheetQty", item.FoamSheetQty);
                                para.Add("@PartitionWeight", item.PartitionWeight);
                                para.Add("@PlasicBag1Weight", item.PlasicBag1Weight);
                                para.Add("@PlasicBag2Weight", item.PlasicBag2Weight);
                                para.Add("@WrapSheetWeight", item.WrapSheetWeight);
                                para.Add("@FoamSheetWeight", item.FoamSheetWeight);
                                para.Add("@PlasicBoxWeight", item.PlasicBoxWeight);
                                para.Add("@Tolerance", item.Tolerance);
                                para.Add("@ToleranceAfterPrint", item.ToleranceAfterPrint);
                                con.Execute("sp_tblCoreDataCodeitemSizeInsert", para, commandType: CommandType.StoredProcedure);
                            }
                            else
                            {
                                para.Add("@CodeItemSize", item.CodeItemSize);
                                para.Add("@MainItemName", item.MainItemName);
                                para.Add("@MetalScan", item.MetalScan);
                                para.Add("@Color", item.Color);
                                para.Add("@Printing", item.Printing);
                                //para.Add("@Date", item.Date);
                                para.Add("@Size", item.Size);
                                para.Add("@AveWeight1Prs", item.AveWeight1Prs);
                                para.Add("@BoxQtyBx1", item.BoxQtyBx1);
                                para.Add("@BoxQtyBx2", item.BoxQtyBx2);
                                para.Add("@BoxQtyBx3", item.BoxQtyBx3);
                                para.Add("@BoxQtyBx4", item.BoxQtyBx4);
                                para.Add("@BoxWeightBx1", item.BoxWeightBx1);
                                para.Add("@BoxWeightBx2", item.BoxWeightBx2);
                                para.Add("@BoxWeightBx3", item.BoxWeightBx3);
                                para.Add("@BoxWeightBx4", item.BoxWeightBx4);
                                para.Add("@PartitionQty", item.PartitionQty);
                                para.Add("@PlasicBag1Qty", item.PlasicBag1Qty);
                                para.Add("@PlasicBag2Qty", item.PlasicBag2Qty);
                                para.Add("@WrapSheetQty", item.WrapSheetQty);
                                para.Add("@FoamSheetQty", item.FoamSheetQty);
                                para.Add("@PartitionWeight", item.PartitionWeight);
                                para.Add("@PlasicBag1Weight", item.PlasicBag1Weight);
                                para.Add("@PlasicBag2Weight", item.PlasicBag2Weight);
                                para.Add("@WrapSheetWeight", item.WrapSheetWeight);
                                para.Add("@FoamSheetWeight", item.FoamSheetWeight);
                                para.Add("@PlasicBoxWeight", item.PlasicBoxWeight);
                                para.Add("@Tolerance", item.Tolerance);
                                para.Add("@ToleranceAfterPrint", item.ToleranceAfterPrint);

                                //if (item.CodeItemSize == "7112321903-*-0018")
                                //{
                                //    var sx = 11;
                                //}

                                con.Execute("sp_tblCoreDataCodeitemSizeUpdate", para, commandType: CommandType.StoredProcedure);
                            }
                        }
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
                #region Đọc các giá trị từ PLC
                if (GlobalVariables.ModbusStatus)
                {
                    if (_resetCounter)
                    {
                        if (GlobalVariables.MyDriver.ModbusRTUMaster.WriteSingleCoil(1, 2, true))
                        {
                            System.Threading.Thread.Sleep(10);
                            if (GlobalVariables.MyDriver.ModbusRTUMaster.WriteSingleCoil(1, 2, false))
                            {
                                _resetCounter = false;
                            }
                        }
                    }
                    //thanh ghi D0 cua PLC Delta DPV14SS2 co dia chi la 4596
                    GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.ReadHoldingRegisters(1, 4596, 1, ref _readHoldingRegisterArr);

                    //GlobalVariables.RememberInfo.CountMetalScan = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 0);
                    ////update gia tri count vao sự kiện để trong frmScal  nó update lên giao diện
                    //GlobalVariables.MyEvent.CountValue = GlobalVariables.RememberInfo.CountMetalScan;

                    GlobalVariables.MyEvent.CountValue = GlobalVariables.MyDriver.GetUshortAt(_readHoldingRegisterArr, 0);
                }
                else
                {
                    _countDisconnectPlc += 1;
                    if (_countDisconnectPlc >= 3)
                    {
                        GlobalVariables.MyDriver.ModbusRTUMaster.NgatKetNoi();

                        GlobalVariables.ModbusStatus = GlobalVariables.MyDriver.ModbusRTUMaster.KetNoi(GlobalVariables.ComPort, 9600, 8, System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One);
                    }
                }
                #endregion

                System.Threading.Thread.Sleep(100);
            }
        }
    }
}