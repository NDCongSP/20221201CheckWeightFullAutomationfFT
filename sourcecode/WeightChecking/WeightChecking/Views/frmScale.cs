using CoreScanner;
using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraExport.Xls;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using DevExpress.XtraRichEdit.Model;
using DevExpress.XtraSplashScreen;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using WeightChecking.StaticClass;

namespace WeightChecking
{
    public partial class frmScale : DevExpress.XtraEditors.XtraForm
    {
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
        private tblScanDataModel _scanDataMetal = new tblScanDataModel();
        private tblScanDataModel _scanDataWeight = new tblScanDataModel();
        private tblScanDataModel _scanDataPrint = new tblScanDataModel();

        private string _idLabel = null;
        private string _plr = null;// kiểu đóng thùng, P-đôi; L/R-left right

        private double _weight = 0, _boxWeight = 0, _accessoriesWeight = 0;

        private bool _approveUpdateActMetalScan = false;

        // Declare CoreScannerClass
        private CCoreScanner _cCoreScannerClass;
        private string _barcodeString1 = null, _barcodeString2 = null, _barcodeString3 = null;//checkMetal--checkWeight--printing
        private bool[] _scannerIsBussy = { false, false, false };

        private SerialPort _serialPort;

        private bool _firstLoad = true;

        private bool _approvePrint = false;// lệnh cho phép in hay không, chỉ khi nào pass cân thì active lên cho in.
        public frmScale()
        {
            InitializeComponent();

            Load += FrmScale_Load;
        }

        private void FrmScale_Load(object sender, EventArgs e)
        {
            //layoutControlGroup3.Visibility = DevExpress.XtraLayout.Utils.LayoutVisibility.Never;

            //BarcodeScanner1Handle(1, "A122644,6812012310-3400-2951,80,8,P,13/14,1900040,1/2|2,417930.2024,,,");
            //BarcodeScanner1Handle(1, "A120078,6814322206-NBA2-E068,24,4,P,11/12,1900050,3/3|2,418004.2024,0,0,14");

            using (var con=GlobalVariables.GetDbConnection())
            {
                AutoPostingHelper.AutoTransfer("", "A123631,6817012201-3265-D228,100,3,P,6/11,1900022,1/4|2,435752.2024,1,0,99", 1185, 2,con);
            }
            

            //_scaleValueStable = 8777;
            //BarcodeScanner2Handle(2, "A10704344,6812012208-2667-E057,45,4,P,6/13,1900082,2/3|2,248212.2023,,,");
            //GlobalVariables.MyEvent.MetalCheckResult = 0;
            //GlobalVariables.MyEvent.SensorAfterMetalScan = 1;

            #region Test get LotNo Brooks
            //using (var connection = GlobalVariables.GetDbConnection())
            //{
            //    var para = new DynamicParameters();
            //    para.Add("ocNo", "DTOTEST002");
            //    para.Add("boxNo", "1/1");

            //    var reader = connection.ExecuteReader("sp_GetLotOfBrooksHC", param: para, commandType: CommandType.StoredProcedure);
            //    DataTable tableResult = new DataTable();
            //    tableResult.Load(reader);

            //    if (tableResult.Rows.Count > 0)
            //    {
            //        _scanDataWeight.LotNo = tableResult.Rows[0]["LotNo"].ToString();
            //    }
            //}
            #endregion

            #region đăng ký sự kiện từ cac PLC
            GlobalVariables.MyEvent.EventHandlerRefreshMasterData += (s, o) =>
            {
                //#region hien thi cac thong so dem
                //if (this.InvokeRequired)
                //{
                //    this.Invoke(new Action(() =>
                //    {
                //        labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString();
                //        labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();
                //        labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();
                //        labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();
                //        labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                //        labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();
                //        labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();
                //        labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();
                //    }));
                //}
                //else
                //{
                //    labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString();
                //    labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();
                //    labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();
                //    labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();
                //    labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                //    labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();
                //    labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();
                //    labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();
                //}
                //#endregion
            };

            //sự kiện lấy số cân hiện tại cảu đầu cân (real time)
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
                Debug.WriteLine($"Event Sensor before metal scan: {o.NewValue}");

                //if (o.NewValue == 0)
                //{
                //    return;
                //}

                //if (!_firstLoad)
                //{
                //    while (_isStartCountTimer == true)
                //    {
                //        Debug.WriteLine($"Event Sensor before metal scan đang chờ: {o.NewValue} |{_isStartCountTimer}");
                //        Thread.Yield();//cho nó qua 1 luồng khác chạy để tránh làm treo luồng hiện tại
                //    }
                //}
                //else
                //{
                //    _firstLoad = false;
                //}

                Debug.WriteLine($"Event Sensor before metal scan đã qua vòng chờ: {o.NewValue} |{_isStartCountTimer}");
                //chạy task đếm thời gian cho việc quét tem, hết thời gian mà chưa nhận đc tín hiệu từ metal scanner
                //thì ghi tín hiêu xuống PLC conveyor để reject với lý do là không đọc đc QR
                if (_isStartCountTimer == false)
                {
                    if (o.NewValue == 0)
                    {
                        _isStartCountTimer = true;
                        _ckQRTask = new Task(() => CheckReadQr((int)(GlobalVariables.TimeCheckQrMetal)));
                        _ckQRTask.Start();
                    }
                    //else if (o.NewValue == 0)
                    //{
                    //    _ckQRTask = new Task(() => CheckReadQr((int)(GlobalVariables.TimeCheckQrMetal)));
                    //    _ckQRTask.Start();
                    //}
                }
            };

            //khi thùng đụng cảm biến out của cân thì reset biến báo bận cho scanner trạm cân quét tiếp
            GlobalVariables.MyEvent.EventHandleSensorAfterWeightScan += (s, o) =>
            {
                if (o.NewValue == 1)
                {
                    _scannerIsBussy[1] = false;
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

            GlobalVariables.MyEvent.EventHandleSensorBeforeWeightScan += (s, o) =>
            {
                Debug.WriteLine($"Event Sensor before weight scan: {o.NewValue}");
                //chạy task đếm thời gian cho việc quét tem, hết thời gian mà chưa nhận đc tín hiệu từ metal scanner
                //thì ghi tín hiêu xuống PLC conveyor để reject với lý do là không đọc đc QR
                if (o.NewValue == 1)
                {
                    _ckQrWeightScanTask = new Task(() => CheckReadQrWeight());
                    _ckQrWeightScanTask.Start();
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

            GlobalVariables.MyEvent.EventHandleSensorAfterMetalScan += (s, o) =>
            {
                GlobalVariables.AutoPostingStatus2 = string.Empty;
                Debug.WriteLine($"Event Sensor After metal scan: {o.NewValue}");
                GlobalVariables.RememberInfo.CountMetalScan += 1;//đếm số thùng đi qua máy metalScan

                if (o.NewValue == 1)
                {
                    using (var connection = GlobalVariables.GetDbConnection())
                    {
                        var para = new DynamicParameters();
                        if (_metalCheckResult == 1)//Check metal fail
                        {
                            GlobalVariables.MyEvent.MetalPusher1 = 1;

                            //log vao bang reject
                            para.Add("_barcodeString", _scanDataMetal.BarcodeString);
                            para.Add("_idLabel", _scanDataMetal.IdLabel);
                            para.Add("_ocNo", _scanDataMetal.OcNo);
                            para.Add("_boxId", _scanDataMetal.BoxNo);
                            para.Add("_productNumber", _scanDataMetal.ProductNumber);
                            para.Add("_productName", _scanDataMetal.ProductName);
                            para.Add("_quantity", _scanDataMetal.Quantity);
                            para.Add("_scannerStation", "Metal");
                            para.Add("_reason", "Dò kim loại lỗi");
                            para.Add("_grossWeight", _scanDataMetal.GrossWeight);
                            para.Add("@_deviationPairs", _scanDataMetal.DeviationPairs);
                            para.Add("@_deviationWeight", _scanDataMetal.Deviation);

                            connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                            //transfer from WH in comming to 964
                            #region Auto Stock In to 1223 if Box come to QC
                            //kiểm tra thùng hàng ko có trong kho production hand ove WH (1185) là cho stock in vao kho QC hand over WH (1223)

                            var res1 = AutoPostingHelper.CheckIn(_scanDataMetal.ProductNumber, _scanDataMetal.BarcodeString, connection);
                            var accept = res1.FirstOrDefault();

                            var para1 = new DynamicParameters();
                            para1.Add("@Message", $"Metal sp_lmpScannerClient_ScanningLabel_CheckIn = {res1.Count}.");
                            para1.Add("@MessageTemplate", $"{_scanDataMetal.BarcodeString}");
                            para1.Add("Level", "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn");
                            connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                            if (accept != null)
                            {
                                para = new DynamicParameters();
                                para.Add("@Message", $"Check metal fail.");
                                para.Add("Level", "Auto post metal.");
                                para.Add("Exception", null);
                                connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                                GlobalVariables.AutoPostingStatus2 = AutoPostingHelper.AutoTransfer(_scanDataMetal.ProductNumber, _scanDataMetal.BarcodeString, Convert.ToInt16(accept.C004), 964, connection);

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    labErrInfoMetal.Text = $"{GlobalVariables.AutoPostingStatus2}";
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
                            var res1 = AutoPostingHelper.CheckIn(_scanDataMetal.ProductNumber, _scanDataMetal.BarcodeString, connection);
                            var accept = res1.FirstOrDefault(x => x.C004 == "964");

                            var para1 = new DynamicParameters();
                            para1.Add("@Message", $"Metal sp_lmpScannerClient_ScanningLabel_CheckIn = {res1.Count}.");
                            para1.Add("@MessageTemplate", $"{_scanDataMetal.BarcodeString}");
                            para1.Add("Level", "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn");
                            connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                            if (accept != null)
                            {
                                para = new DynamicParameters();
                                para.Add("@Message", $"Check metal OK.");
                                para.Add("Level", "Auto post metal.");
                                para.Add("Exception", null);
                                connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                                GlobalVariables.AutoPostingStatus2 = AutoPostingHelper.AutoTransfer(_scanDataMetal.ProductNumber, _scanDataMetal.BarcodeString
                                    , Convert.ToInt16(accept.C004), Convert.ToInt16(accept.C021), connection);

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    labErrInfoMetal.Text = $"{GlobalVariables.AutoPostingStatus2}";
                                });
                            }
                            #endregion
                        }

                        //log gia thông tin check metal vào bảng tblMetalScanResult
                        //MessageBox.Show("vao ghi data check metal");
                        para = new DynamicParameters();
                        para.Add("@_barcodeString", _scanDataMetal.BarcodeString);
                        para.Add("@_productItemCode", _scanDataMetal.ProductNumber);
                        para.Add("@_idLabel", _scanDataMetal.IdLabel);
                        para.Add("@_oc", _scanDataMetal.OcNo);
                        para.Add("@_boxNo", _scanDataMetal.BoxNo);
                        para.Add("@_qty", _scanDataMetal.Quantity);
                        para.Add("@_metalCheckResult", _metalCheckResult);

                        var res = connection.Execute("sp_tblMetalScanResultInsert", para, commandType: CommandType.StoredProcedure);
                    }
                }
            };
            GlobalVariables.MyEvent.EventHandleMetalCheckResult += (s, o) =>
            {
                _metalCheckResult = o.NewValue;
                Debug.WriteLine($"Event Metal check result: {o.NewValue}");
            };
            #endregion

            //khởi tạo scanner
            InitializeScaner();

            //Khởi tạo máy in AnserU2 Smart one
            if (!GlobalVariables.IsTest)
            {
                SerialPortOpen();
                Thread.Sleep(10000);
                SendDynamicString(" ", " ", " ");
            }

            GlobalVariables.AppStatus = "READY";
        }

        private void frmScale_FormClosing(object sender, FormClosingEventArgs e)
        {
            //huy đối tượng máy in
            SerialPortClose();

            if (_ckQRTask != null)
            {
                _ckQRTask.Wait();
                _ckQRTask.Dispose();
            }

            //huy doi tuong can
            //_scaleHelper.StopScale = true;
            //_ckTask.Wait();
            //_ckTask.Dispose();
            //_scaleHelper.Dispose();
            GlobalVariables.ScaleStatus = "Disconnect";
        }

        private void ResetControl()
        {
            GlobalVariables.InvokeIfRequired(this, () =>
            {
                labRealWeight.Text = "0";
                labNetWeight.Text = "0";
                labOcNo.Text = string.Empty;
                labProductCode.Text = string.Empty;
                labProductName.Text = string.Empty;
                labQuantity.Text = "0";
                labColor.Text = string.Empty;
                labSize.Text = string.Empty;
                labAveWeight.Text = "0";
                labLowerTolerance.Text = "0";
                labLowerToleranceWeight.Text = "0";
                labUpperTolerance.Text = "0";
                labUpperToleranceWeight.Text = "0";
                labBoxWeight.Text = "0";
                labAccessoriesWeight.Text = "0";
                labGrossWeight.Text = "0";

                labResult.Text = "Pass/Fail";
                labResult.BackColor = Color.Gray;
                labResult.ForeColor = Color.White;

                labCalculatedPairs.Text = "0";
                labDeviationPairs.Text = "0";
            });
        }

        #region Barcode handle
        private void BarcodeScanner1Handle(int station, string barcodeString)
        {
            GlobalVariables.AutoPostingStatus1 = string.Empty;

            try
            {
                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    labQrMetal.Text = barcodeString;
                    labQrMetal.BackColor = Color.White;
                });

                #region Xử lý data ban đầu theo QR code
                _scanDataMetal.CreatedBy = GlobalVariables.UserLoginInfo.Id;
                _scanDataMetal.Station = GlobalVariables.Station;

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
                            labErrInfoMetal.Text = "OC không đúng định dạng";
                        });

                        //ghi lệnh reject do ko quet đc tem
                        _metalScannerStatus = 1;

                        //log vao bang reject
                        using (var connection = GlobalVariables.GetDbConnection())
                        {
                            var para = new DynamicParameters();
                            para.Add("_barcodeString", _scanDataMetal.BarcodeString);
                            para.Add("_idLabel", _scanDataMetal.IdLabel);
                            para.Add("_ocNo", _scanDataMetal.OcNo);
                            para.Add("_boxId", _scanDataMetal.BoxNo);
                            para.Add("_productNumber", _scanDataMetal.ProductNumber);
                            para.Add("_productName", _scanDataMetal.ProductName);
                            para.Add("_quantity", _scanDataMetal.Quantity);
                            para.Add("_scannerStation", "Identification");
                            para.Add("_reason", "OC không đúng định dạng");
                            para.Add("_grossWeight", _scanDataMetal.GrossWeight);
                            para.Add("@_deviationPairs", _scanDataMetal.DeviationPairs);
                            para.Add("@_deviationWeight", _scanDataMetal.Deviation);

                            connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
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
                            labErrInfoMetal.Text = "OC không đúng định dạng.";
                        });

                        //ghi lệnh reject do ko quet đc tem
                        _metalScannerStatus = 1;

                        //log vao bang reject
                        using (var connection = GlobalVariables.GetDbConnection())
                        {
                            var para = new DynamicParameters();
                            para.Add("_barcodeString", _scanDataMetal.BarcodeString);
                            para.Add("_idLabel", _scanDataMetal.IdLabel);
                            para.Add("_ocNo", _scanDataMetal.OcNo);
                            para.Add("_boxId", _scanDataMetal.BoxNo);
                            para.Add("_productNumber", _scanDataMetal.ProductNumber);
                            para.Add("_productName", _scanDataMetal.ProductName);
                            para.Add("_quantity", _scanDataMetal.Quantity);
                            para.Add("_scannerStation", "Identification");
                            para.Add("_reason", "OC không đúng định dạng");
                            para.Add("_grossWeight", _scanDataMetal.GrossWeight);
                            para.Add("@_deviationPairs", _scanDataMetal.DeviationPairs);
                            para.Add("@_deviationWeight", _scanDataMetal.Deviation);

                            connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
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

                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var para = new DynamicParameters();

                    #region Auto Stock In to 1223 if Box come to QC, update 20240819
                    //kiểm tra thùng hàng ko có trong kho production hand ove WH (1185) là cho stock in vao kho QC hand over WH (1223)
                    var res1 = AutoPostingHelper.CheckIn(_scanDataMetal.ProductNumber, barcodeString, connection);
                    var accept = res1.FirstOrDefault();

                    if (accept == null)
                    {
                        var para1 = new DynamicParameters();
                        para1.Add("@Message", $"Scanner 1 sp_lmpScannerClient_ScanningLabel_CheckIn = {res1.Count}. to 1223.");
                        para1.Add("@MessageTemplate", $"{barcodeString}");
                        para1.Add("Level", "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn");
                        connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                        //nếu tem ko có trong kho nào, hoặc đã có trong kho mà khác kho 4(production) thì stockIn vào kho 1223
                        GlobalVariables.AutoPostingStatus1 = AutoPostingHelper.AutoStockIn(_scanDataMetal.ProductNumber, barcodeString, 1223, connection);
                        Log.Information($"Auto post Scanner 1 | {GlobalVariables.AutoPostingStatus1}");

                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            labErrInfoMetal.Text = GlobalVariables.AutoPostingStatus1;
                        });
                    }
                    //nếu tem nằm trong kho sản xuất, tức là công nhân quên transfer qua kho 1185, vào transfer tự động qua kho 1185
                    else if (accept != null && accept.C004 == "4")
                    {
                        var para1 = new DynamicParameters();
                        para1.Add("@Message", $"Scanner 1 sp_lmpScannerClient_ScanningLabel_CheckIn = {res1.Count}. to 1185.");
                        para1.Add("@MessageTemplate", $"{barcodeString}");
                        para1.Add("Level", "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn");
                        connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                        GlobalVariables.AutoPostingStatus1 = AutoPostingHelper.AutoTransfer(_scanDataMetal.ProductNumber, barcodeString, Convert.ToInt16(accept.C004), 1185, connection);
                        Log.Information($"Auto post Scanner 1 | {GlobalVariables.AutoPostingStatus1}");

                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            labErrInfoMetal.Text = GlobalVariables.AutoPostingStatus1;
                        });
                    }
                    #endregion

                    #region kiểm tra xem oc boxno đã có trong hệ thống hay chưa
                    para.Add("oc", _scanDataMetal.OcNo);
                    para.Add("boxNo", _scanDataMetal.BoxNo);

                    var checkBox = connection.Query<tblScanDataModel>("sp_tblScanDataGetByOcBoxNo", para, commandType: CommandType.StoredProcedure).ToList();

                    if (checkBox != null && checkBox.Count > 0)
                    {
                        //kiểm tra xem tem vừa quét nó là quét lại hay tem mới.  nếu là null là tem mới
                        var box = checkBox.FirstOrDefault(x => x.BarcodeString == _scanDataMetal.BarcodeString);

                        //trường hợp quét tem đã đc in lại tem (tem mới), deactive các tem trước đó đã đi qua băng tải đi để check lại thông tin theo tem mới.
                        if (box == null)
                        {
                            para.Add("active", 0);
                            connection.Execute("sp_tblScanDataUpdateActiveByOcBoxNo", param: para, commandType: CommandType.StoredProcedure);

                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                labQrMetal.BackColor = Color.Yellow;
                            });
                        }
                        else//trường hợp quét lại chính thùng trước đó đã đi qua băng tải
                        {
                            if ((box.Pass == 1 && (box.Status == 2 || GlobalVariables.Station == StationEnum.IDC_1))
                                //|| (item.Pass == 0 && item.ActualDeviationPairs == 0 && item.ApprovedBy != Guid.Empty)
                                || (box.Pass == 0 && box.Status == 2 && box.ActualDeviationPairs == 0)
                                )
                            {
                                #region Auto Stock In to 1223 if Box come to QC, update 20240819
                                //kiểm tra thùng hàng ko có trong kho production hand ove WH (1185) là cho stock in vao kho QC hand over WH (1223)
                                res1 = AutoPostingHelper.CheckIn(_scanDataMetal.ProductNumber, barcodeString, connection);
                                accept = res1.FirstOrDefault();

                                var para1 = new DynamicParameters();
                                para1.Add("@Message", $"Scanner 1 sp_lmpScannerClient_ScanningLabel_CheckIn = {res1.Count}. to 2 .");
                                para1.Add("@MessageTemplate", $"{barcodeString}");
                                para1.Add("Level", "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn");
                                connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                                if (accept != null)
                                {
                                    var whTo = 2;
                                    if (_scanDataMetal.OcNo.Substring(0, 2) == "PR") whTo = 10;

                                    //nếu tem ko có trong kho nào, hoặc đã có trong kho mà khác kho 4(production) thì stockIn vào kho 1223
                                    GlobalVariables.AutoPostingStatus1 = AutoPostingHelper.AutoTransfer(_scanDataMetal.ProductNumber, barcodeString, Convert.ToInt16(accept.C004), whTo, connection);
                                    Log.Information($"Auto post Scanner 1 | {GlobalVariables.AutoPostingStatus1}");

                                    GlobalVariables.InvokeIfRequired(this, () =>
                                    {
                                        labErrInfoMetal.Text = GlobalVariables.AutoPostingStatus1;
                                    });
                                }
                                #endregion

                                Debug.WriteLine($"ProductNumber: {box.ProductNumber} đã kiểm tra OK, không được.");

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    labErrInfoMetal.Text = "Thùng này đã check OK.";
                                });

                                _metalScannerStatus = 1;

                                //log vao bang reject
                                para = null;
                                para = new DynamicParameters();
                                para.Add("_barcodeString", _scanDataMetal.BarcodeString);
                                para.Add("_idLabel", _scanDataMetal.IdLabel);
                                para.Add("_ocNo", _scanDataMetal.OcNo);
                                para.Add("_boxId", _scanDataMetal.BoxNo);
                                para.Add("_productNumber", _scanDataMetal.ProductNumber);
                                para.Add("_productName", _scanDataMetal.ProductName);
                                para.Add("_quantity", _scanDataMetal.Quantity);
                                para.Add("_scannerStation", "Identification");
                                para.Add("_reason", "Thùng này đã check OK.");
                                para.Add("_grossWeight", _scanDataMetal.GrossWeight);
                                para.Add("@_deviationPairs", _scanDataMetal.DeviationPairs);
                                para.Add("@_deviationWeight", _scanDataMetal.Deviation);

                                connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                                return;
                            }
                        }
                    }

                    #region Kiểm tra xem thùng này đã được log vào scanData chưa
                    //para = null;
                    //para = new DynamicParameters();
                    //para.Add("_QrCode", _scanDataMetal.BarcodeString);
                    //var checkInfo = connection.Query<tblScanDataModel>("sp_tblScanDataGetByQrCode", para, commandType: CommandType.StoredProcedure).ToList();
                    //foreach (var item in checkInfo)
                    //{
                    //    if (
                    //        (item.Pass == 1 && (item.Status == 2 || GlobalVariables.Station == StationEnum.IDC_1))
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

                    //        connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                    //        return;
                    //    }
                    //}
                    #endregion
                    #endregion

                    #region process normally 
                    // 2023-07-26:
                    //ghi nhận In commming SSFG
                    DynamicParameters pIncomingIDC = new DynamicParameters();
                    pIncomingIDC.Add("@QRCode", barcodeString);
                    pIncomingIDC.Add("@OCNo", _scanDataMetal.OcNo);
                    pIncomingIDC.Add("@BoxNo", _scanDataMetal.BoxNo);
                    pIncomingIDC.Add("@IdLabel", _scanDataMetal.IdLabel);

                    var resAddIDC = connection.Execute("sp_IncomingIDC_Add", pIncomingIDC, commandType: CommandType.StoredProcedure);
                    if (resAddIDC > 0)
                    {
                        Debug.WriteLine($"Ghi nhận OC {_scanDataMetal.OcNo} hàng vào IDC.");
                        //this?.Invoke((MethodInvoker)delegate { labErrInfoMetal.Text = $"Ghi nhận OC {_scanDataMetal.OcNo} hàng vào IDC."; });
                    }

                    // 2023-07-26:
                    DynamicParameters pr = new DynamicParameters();
                    pr.Add("@ProductNumber", _scanDataMetal.ProductNumber);
                    pr.Add("@SpecialCase", specialCaseMetal);

                    para = new DynamicParameters();
                    para.Add("@ProductNumber", _scanDataMetal.ProductNumber);
                    para.Add("@SpecialCase", specialCaseMetal);

                    var res = connection.Query<ProductInfoModel>("sp_vProductItemInfoGet", para, commandType: CommandType.StoredProcedure).FirstOrDefault();

                    if (res != null)
                    {
                        _scanDataMetal.ProductName = res.ProductName;

                        if (res.AveWeight1Prs > 0)
                        {
                            if (res.MetalScan == 1 && ocFirstCharMetal != "PR")
                            {
                                Debug.WriteLine($"ProductNumber: {res.ProductNumber} có kiểm tra kim loại.");

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    labErrInfoMetal.Text = "Hàng kiểm kim loại.";
                                });

                                _metalScannerStatus = 0;
                                //GlobalVariables.MyEvent.MetalPusher = 0;
                            }
                            else if (res.MetalScan == 0 || (res.MetalScan == 1 && ocFirstCharMetal == "PR"))
                            {
                                Debug.WriteLine($"ProductNumber: {res.ProductNumber} không kiểm tra kim loại.");

                                GlobalVariables.InvokeIfRequired(this, () =>
                                {
                                    labErrInfoMetal.Text = "Hàng không kiểm kim loại.";
                                });

                                // gui data xuong PLC
                                _metalScannerStatus = 2;
                                //GlobalVariables.MyEvent.MetalPusher = 2;
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Item '{_scanDataWeight.ProductNumber}' không có khối lượng/1 đôi. Xin hãy kiểm tra lại thông tin."
                                , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                labErrInfoMetal.Text = "Không có khối lượng đôi. Weight/Prs.";
                            });

                            _metalScannerStatus = 1;//bao reject cho PLC

                            //log vao bang reject
                            para = null;
                            para = new DynamicParameters();
                            para.Add("_barcodeString", _scanDataMetal.BarcodeString);
                            para.Add("_idLabel", _scanDataMetal.IdLabel);
                            para.Add("_ocNo", _scanDataMetal.OcNo);
                            para.Add("_boxId", _scanDataMetal.BoxNo);
                            para.Add("_productNumber", _scanDataMetal.ProductNumber);
                            para.Add("_productName", _scanDataMetal.ProductName);
                            para.Add("_quantity", _scanDataMetal.Quantity);
                            para.Add("_scannerStation", "Identification");
                            para.Add("_reason", "Không có khối lượng đôi. Average Weight/prs.");
                            para.Add("_grossWeight", _scanDataMetal.GrossWeight);
                            para.Add("@_deviationPairs", _scanDataMetal.DeviationPairs);
                            para.Add("@_deviationWeight", _scanDataMetal.Deviation);

                            connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                            para = null;
                            para = new DynamicParameters();
                            para.Add("ProductNumber", _scanDataWeight.ProductNumber);
                            para.Add("ProductName", _scanDataWeight.ProductName);
                            para.Add("OcNum", _scanDataWeight.OcNo);
                            para.Add("Note", "Chưa có data trong file QC.");
                            para.Add("QrCode", _scanDataWeight.BarcodeString);

                            connection.Execute("sp_tblItemMissingInfoInsert", para, commandType: CommandType.StoredProcedure);
                        }
                    }
                    else
                    {
                        #region gui data xuong PLC
                        _metalScannerStatus = 1;
                        //GlobalVariables.MyEvent.MetalPusher = 1;
                        #endregion

                        Debug.WriteLine($"Product number {_scanDataMetal.ProductNumber} không có trong hệ thống. Hãy báo quản lý để lấy lại dữ liệu mới nhất từ Winline về."
                            , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            labErrInfoMetal.Text = "ProductItem chưa có trên hệ thống.";
                        });

                        _metalScannerStatus = 1;//bao reject cho PLC

                        //log vao bang reject
                        para = null;
                        para = new DynamicParameters();
                        para.Add("_barcodeString", _scanDataMetal.BarcodeString);
                        para.Add("_idLabel", _scanDataMetal.IdLabel);
                        para.Add("_ocNo", _scanDataMetal.OcNo);
                        para.Add("_boxId", _scanDataMetal.BoxNo);
                        para.Add("_productNumber", _scanDataMetal.ProductNumber);
                        para.Add("_productName", _scanDataMetal.ProductName);
                        para.Add("_quantity", _scanDataMetal.Quantity);
                        para.Add("_scannerStation", "Identification");
                        para.Add("_reason", "Product item chưa có trong hệ thống. Get data từ WL về lại.");
                        para.Add("_grossWeight", _scanDataMetal.GrossWeight);
                        para.Add("@_deviationPairs", _scanDataMetal.DeviationPairs);
                        para.Add("@_deviationWeight", _scanDataMetal.Deviation);

                        connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                        para = null;
                        para = new DynamicParameters();
                        para.Add("ProductNumber", _scanDataMetal.ProductNumber);
                        para.Add("ProductName", _scanDataMetal.ProductName);
                        para.Add("OcNum", _scanDataMetal.OcNo);
                        para.Add("Note", $"Product item '{_scanDataMetal.ProductNumber}' không có data hệ thống.");
                        para.Add("QrCode", _scanDataMetal.BarcodeString);

                        connection.Execute("sp_tblItemMissingInfoInsert", para, commandType: CommandType.StoredProcedure);
                    }
                    #endregion
                }

            }
            catch (Exception ex)
            {
                //ghi giá trị xuống PLC cân reject
                GlobalVariables.MyEvent.MetalPusher = 1;

                //hien thi mau label
                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    labErrInfoMetal.Text = "System fail.";
                });

                using (var connection = GlobalVariables.GetDbConnection())
                {
                    DynamicParameters para = new DynamicParameters();
                    para.Add("@Message", $"Lỗi check label.Scanner: 1 (Identification)|{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                    para.Add("Level", "LogError");
                    para.Add("Exception", ex.ToString());
                    connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                    //if (station == 2)
                    {
                        para = null;
                        para = new DynamicParameters();
                        para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                        para.Add("_idLabel", _scanDataWeight.IdLabel);
                        para.Add("_ocNo", _scanDataWeight.OcNo);
                        para.Add("_boxId", _scanDataWeight.BoxNo);
                        para.Add("_productNumber", _scanDataWeight.ProductNumber);
                        para.Add("_productName", _scanDataWeight.ProductName);
                        para.Add("_quantity", _scanDataWeight.Quantity);
                        para.Add("_scannerStation", $"Identification");
                        para.Add("_reason", $"System fail. Ex:{ex.ToString()}.");
                        para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                        para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                        para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                        connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
                    }
                }

                Log.Error(ex.ToString(), "Lỗi scale form tại trạm scanner 1 Identification.");
            }
            finally
            {

            }
        }

        private void BarcodeScanner2Handle(int station, string barcodeString)
        {
            GlobalVariables.AutoPostingStatus3 = string.Empty;

            try
            {
                SendDynamicString(" ", " ", " ");
                //reset model để lưu cho thùng mới
                _scanDataWeight = null;
                _scanDataWeight = new tblScanDataModel();
                _approvePrint = false;
                GlobalVariables.IdLabel = string.Empty;

                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    labQrScale.Text = barcodeString;
                });

                #region Xử lý data ban đầu theo QR code
                _scanDataWeight.CreatedBy = GlobalVariables.UserLoginInfo.Id;
                _scanDataWeight.Station = GlobalVariables.Station;

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
                    _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    //Check xem  QR code quét vào có đúng định dạng hay ko

                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstChar);

                    if (resultCheckOc != null)
                    {
                        _scanDataWeight.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            labErrInfoScale.Text = "OC không đúng định dạng.";
                        });

                        //ghi lệnh reject do ko quet đc tem
                        GlobalVariables.MyEvent.WeightPusher = 1;

                        //log vao bang reject
                        using (var connection = GlobalVariables.GetDbConnection())
                        {
                            var para = new DynamicParameters();
                            para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                            para.Add("_idLabel", _scanDataWeight.IdLabel);
                            para.Add("_ocNo", _scanDataWeight.OcNo);
                            para.Add("_boxId", _scanDataWeight.BoxNo);
                            para.Add("_productNumber", _scanDataWeight.ProductNumber);
                            para.Add("_productName", _scanDataWeight.ProductName);
                            para.Add("_quantity", _scanDataWeight.Quantity);
                            para.Add("_scannerStation", "Scale");
                            para.Add("_reason", "OC không đúng định dạng");
                            para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                            para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                            para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                            connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
                        }

                        return;
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
                    _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    //Check xem  QR code quét vào có đúng định dạng hay ko
                    var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstChar);

                    if (resultCheckOc != null)
                    {
                        _scanDataWeight.OcNo = s1[0];
                    }
                    else
                    {
                        Debug.WriteLine("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (labErrInfoScale.InvokeRequired)
                        {
                            labErrInfoScale.Invoke(new Action(() =>
                            {
                                labErrInfoScale.Text = "OC không đúng định dạng.";
                            }));
                        }
                        else labErrInfoScale.Text = "OC không đúng định dạng.";

                        //ghi lệnh reject do ko quet đc tem
                        GlobalVariables.MyEvent.WeightPusher = 1;


                        //log vao bang reject
                        using (var connection = GlobalVariables.GetDbConnection())
                        {
                            var para = new DynamicParameters();
                            para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                            para.Add("_idLabel", _scanDataWeight.IdLabel);
                            para.Add("_ocNo", _scanDataWeight.OcNo);
                            para.Add("_boxId", _scanDataWeight.BoxNo);
                            para.Add("_productNumber", _scanDataWeight.ProductNumber);
                            para.Add("_productName", _scanDataWeight.ProductName);
                            para.Add("_quantity", _scanDataWeight.Quantity);
                            para.Add("_scannerStation", "Scale");
                            para.Add("_reason", "OC không đúng định dạng");
                            para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                            para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                            para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                            connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
                        }
                        return;
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
                while (_stableScale == 0 && GlobalVariables.IsScale)
                {
                    Thread.Yield();//cho nó qua 1 luồng khác chạy để tránh làm treo luồng hiện tại
                }
                //Debug.WriteLine($"da can xong. stable {_stableScale}");

                _scanDataWeight.GrossWeight = GlobalVariables.RealWeight = _scaleValueStable;
                //truy vấn thông tin 
                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var para = new DynamicParameters();

                    #region Kiểm tra xem thùng này đã được log vào scanData chưa
                    //para.Add("QRLabel", _scanData.BarcodeString);
                    //var checkInfo = connection.Query<tblScanDataCheckModel>("sp_tblScanDataCheck", para, commandType: CommandType.StoredProcedure).ToList();

                    para.Add("_QrCode", _scanDataWeight.BarcodeString);
                    var checkInfo = connection.Query<tblScanDataModel>("sp_tblScanDataGetByQrCode", para, commandType: CommandType.StoredProcedure).ToList();
                    foreach (var item in checkInfo)
                    {
                        if (
                            (item.Pass == 1 && (item.Status == 2 || GlobalVariables.Station == StationEnum.IDC_1))
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

                    para = new DynamicParameters();
                    para.Add("@ProductNumber", _scanDataWeight.ProductNumber);
                    para.Add("@SpecialCase", specialCase);

                    //đối với hàng sơn PU, thì trước sơn lấy các giá trị theo printing =0. Sau sơn thì lấy các giá trị theo printing =1
                    var checkOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstChar && ocFirstChar != "PR");
                    if (specialCase)
                    {
                        //after printing
                        if (checkOc != null || (ocFirstChar == "PR" && GlobalVariables.AfterPrinting != 0))
                        {
                            para.Add("@Printing", 1);//sau son
                        }
                        else//before printing
                        {
                            para.Add("@Printing", 0);//truoc son, chi có ở trạm IDC1
                        }
                    }
                    var res = connection.Query<ProductInfoModel>("sp_vProductItemInfoGet", para, commandType: CommandType.StoredProcedure).FirstOrDefault();

                    if (res != null)
                    {
                        _scanDataWeight.ProductName = res.ProductName;
                        _scanDataWeight.Decoration = res.Decoration;
                        _scanDataWeight.MetalScan = res.MetalScan;
                        _scanDataWeight.Brand = res.Brand;
                        _scanDataWeight.AveWeight1Prs = res.AveWeight1Prs;
                        _scanDataWeight.ProductCategory = res.ProductCategory;

                        if (_scanDataWeight.AveWeight1Prs != 0)
                        {
                            #region Fill data from coreData to scanData, tính toán ra NetWeight và GrossWeight
                            //Xét điều kiện để lấy boxWeight. Nếu là hàng đi sơn thì dùng thùng nhựa
                            if ((_scanDataWeight.Decoration == 0 || (_scanDataWeight.Decoration == 1 && checkOc != null)) && checkOc.OcFirstChar != "BF")
                            {
                                _scanDataWeight.Status = 2;//báo trạng thái hàng ko đi sơn, hoặc hàng sơn đã được sơn rồi

                                //lấy tolerance theo thùng giấy
                                lowerToleranceOfBox = res.LowerToleranceOfCartonBox;
                                upperToleranceOfBox = res.UpperToleranceOfCartonBox;

                                if (_scanDataWeight.Quantity <= res.BoxQtyBx4)
                                {
                                    _scanDataWeight.BoxWeight = res.BoxWeightBx4;

                                    if (labBoxType.InvokeRequired)
                                    {
                                        labBoxType.Invoke(new Action(() =>
                                        {
                                            labBoxType.Text = "BX4";
                                        }));
                                    }
                                    else
                                    {
                                        labBoxType.Text = "BX4";
                                    }
                                }
                                else if (_scanDataWeight.Quantity > res.BoxQtyBx4 && _scanDataWeight.Quantity <= res.BoxQtyBx3)
                                {
                                    _scanDataWeight.BoxWeight = res.BoxWeightBx3;

                                    if (labBoxType.InvokeRequired)
                                    {
                                        labBoxType.Invoke(new Action(() =>
                                        {
                                            labBoxType.Text = "BX3";
                                        }));
                                    }
                                    else
                                    {
                                        labBoxType.Text = "BX3";
                                    }
                                }
                                else if (_scanDataWeight.Quantity > res.BoxQtyBx3 && _scanDataWeight.Quantity <= res.BoxQtyBx2)
                                {
                                    _scanDataWeight.BoxWeight = res.BoxWeightBx2;

                                    if (labBoxType.InvokeRequired)
                                    {
                                        labBoxType.Invoke(new Action(() =>
                                        {
                                            labBoxType.Text = "BX2";
                                        }));
                                    }
                                    else
                                    {
                                        labBoxType.Text = "BX2";
                                    }
                                }
                                else if (_scanDataWeight.Quantity > res.BoxQtyBx2 && _scanDataWeight.Quantity <= res.BoxQtyBx1)
                                {
                                    _scanDataWeight.BoxWeight = res.BoxWeightBx1;

                                    if (labBoxType.InvokeRequired)
                                    {
                                        labBoxType.Invoke(new Action(() =>
                                        {
                                            labBoxType.Text = "BX1";
                                        }));
                                    }
                                    else
                                    {
                                        labBoxType.Text = "BX1";
                                    }
                                }
                                else if (_scanDataWeight.Quantity > res.BoxQtyBx1)
                                {
                                    Debug.WriteLine($"Số lượng vượt quá giới hạn thùng BX1 ({res.BoxQtyBx1})", "CẢNH BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                    para = null;
                                    para = new DynamicParameters();
                                    para.Add("ProductNumber", _scanDataWeight.ProductNumber);
                                    para.Add("ProductName", _scanDataWeight.ProductName);
                                    para.Add("OcNum", _scanDataWeight.OcNo);
                                    para.Add("Note", $"Số lượng vượt quá giới hạn thùng BX1 ({res.BoxQtyBx1})");
                                    para.Add("QrCode", _scanDataWeight.BarcodeString);

                                    connection.Execute("sp_tblItemMissingInfoInsert", para, commandType: CommandType.StoredProcedure);

                                    //bật đèn đỏ
                                    GlobalVariables.MyEvent.StatusLightPLC = 1;
                                    //ghi giá trị xuống PLC cân reject
                                    GlobalVariables.MyEvent.WeightPusher = 1;

                                    ResetControl();

                                    if (this.InvokeRequired)
                                    {
                                        this.Invoke(new Action(() =>
                                        {
                                            labResult.Text = "Fail";
                                            labResult.BackColor = Color.Red;
                                            labErrInfoScale.Text = "Quantity box error.";
                                        }));
                                    }
                                    else
                                    {
                                        labResult.Text = "Fail";
                                        labResult.BackColor = Color.Red;
                                        labErrInfoScale.Text = "Quantity box error.";
                                    }

                                    //log vao bang reject
                                    para = null;
                                    para = new DynamicParameters();
                                    para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                                    para.Add("_idLabel", _scanDataWeight.IdLabel);
                                    para.Add("_ocNo", _scanDataWeight.OcNo);
                                    para.Add("_boxId", _scanDataWeight.BoxNo);
                                    para.Add("_productNumber", _scanDataWeight.ProductNumber);
                                    para.Add("_productName", _scanDataWeight.ProductName);
                                    para.Add("_quantity", _scanDataWeight.Quantity);
                                    para.Add("_scannerStation", "Scale");
                                    para.Add("_reason", $"Số lượng vượt quá giới hạn thùng BX1 ({res.BoxQtyBx1})");
                                    para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                                    para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                                    para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                                    connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                                    goto returnLoop;
                                }

                                if (_scanDataWeight.Decoration == 0)
                                {
                                    if (labDeviation.InvokeRequired)
                                    {
                                        labDeviation.Invoke(new Action(() =>
                                        {
                                            labDecoration.BackColor = Color.Gray;
                                        }));
                                    }
                                    else
                                    {
                                        labDecoration.BackColor = Color.Gray;
                                    }
                                }
                                else
                                {
                                    if (labDeviation.InvokeRequired)
                                    {
                                        labDeviation.Invoke(new Action(() =>
                                        {
                                            labDecoration.BackColor = Color.Green;
                                        }));
                                    }
                                    else
                                    {
                                        labDecoration.BackColor = Color.Green;
                                    }
                                }
                            }
                            else //if (_scanDataWeight.Decoration == 1 && _scanDataWeight.OcNo.Contains("PR"))//hàng trước sơn. chỉ có trạm SSFG01 mới nhảy vào đây
                            {
                                //lấy tolerance theo thùng nhựa
                                lowerToleranceOfBox = res.LowerToleranceOfPlasticBox;
                                upperToleranceOfBox = res.UpperToleranceOfPlasticBox;

                                if (GlobalVariables.AfterPrinting == 0 && _scanDataWeight.OcNo.Contains("PR"))
                                {
                                    _scanDataWeight.Status = 1;// báo trạng thái hàng sơn cần đưa đi sơn, trạm SSFG01
                                }
                                else
                                {
                                    _scanDataWeight.Status = 2;// báo trạng thái hàng sơn đã được sơn, trạm SSFG02 và SSFG03(Kerry)
                                }

                                _scanDataWeight.BoxWeight = res.PlasicBoxWeight;

                                if (this.InvokeRequired)
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        labDecoration.BackColor = Color.Gold;
                                        labBoxType.Text = "Plastic";
                                    }));
                                }
                                else
                                {
                                    labDecoration.BackColor = Color.Gold;
                                    labBoxType.Text = "Plastic";
                                }
                            }

                            if (_scanDataWeight.MetalScan == 0)
                            {
                                _approveUpdateActMetalScan = false;

                                if (labMetalScan.InvokeRequired)
                                {
                                    labMetalScan.Invoke(new Action(() =>
                                    {
                                        labMetalScan.BackColor = Color.Gray;
                                    }));
                                }
                                else
                                {
                                    labMetalScan.BackColor = Color.Gray;
                                }
                            }
                            else
                            {
                                GlobalVariables.RememberInfo.MetalScan += 1;

                                _approveUpdateActMetalScan = true;

                                if (labMetalScan.InvokeRequired)
                                {
                                    labMetalScan.Invoke(new Action(() =>
                                    {
                                        labMetalScan.BackColor = Color.Gold;
                                    }));
                                }
                                else
                                {
                                    labMetalScan.BackColor = Color.Gold;
                                }
                            }

                            _scanDataWeight.StdNetWeight = Math.Round(_scanDataWeight.Quantity * _scanDataWeight.AveWeight1Prs, 3);

                            //_scanDataWeight.Tolerance = Math.Round(_scanDataWeight.StdNetWeight * (res.Tolerance / 100), 3);
                            _scanDataWeight.LowerTolerance = Math.Round(_scanDataWeight.StdNetWeight * (lowerToleranceOfBox / 100), 3);
                            _scanDataWeight.UpperTolerance = Math.Round(_scanDataWeight.StdNetWeight * (upperToleranceOfBox / 100), 3);

                            //luu ý các Quantity partition-Plasic-WrapSheet trên DB nó là tính số Prs
                            //sau khi đọc về phải lấy QtyPrs quét trên label / Quantity partition-Plasic-WrapSheet ==> qty * weight ==> Weight package weight
                            double partitionWeight = 0;
                            var p = res.PartitionQty != 0 ? ((double)_scanDataWeight.Quantity / (double)res.PartitionQty) : 0;
                            if (_scanDataWeight.Quantity <= res.BoxQtyBx3 || p < 1)
                            {
                                partitionWeight = 0;
                            }
                            else if (p >= 1)
                            {
                                partitionWeight = Math.Floor(p) * res.PartitionWeight;
                            }
                            //partitionWeight = res.PartitionQty != 0 ? (_scanDataWeight.Quantity / res.PartitionQty) * res.PartitionWeight : 0;
                            var plasicBag1Weight = res.PlasicBag1Qty != 0 ? Math.Ceiling(((double)_scanDataWeight.Quantity / (double)res.PlasicBag1Qty)) * res.PlasicBag1Weight : 0;
                            var plasicBag2Weight = res.PlasicBag2Qty != 0 ? Math.Ceiling(((double)_scanDataWeight.Quantity / (double)res.PlasicBag2Qty)) * res.PlasicBag2Weight : 0;
                            var wrapSheetWeight = res.WrapSheetQty != 0 ? Math.Ceiling(((double)_scanDataWeight.Quantity / (double)res.WrapSheetQty)) * res.WrapSheetWeight : 0;
                            var foamSheetWeight = res.FoamSheetQty != 0 ? Math.Ceiling(((double)_scanDataWeight.Quantity / (double)res.FoamSheetQty)) * res.FoamSheetWeight : 0;

                            _scanDataWeight.PackageWeight = Math.Round(partitionWeight + plasicBag1Weight + plasicBag2Weight + wrapSheetWeight + foamSheetWeight, 3);

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

                            #region hiển thị thông tin
                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                labRealWeight.Text = _scanDataWeight.GrossWeight.ToString();
                                labNetWeight.Text = _scanDataWeight.StdNetWeight.ToString();
                                labOcNo.Text = _scanDataWeight.OcNo;
                                labBoxId.Text = _scanDataWeight.BoxNo;
                                labProductCode.Text = _scanDataWeight.ProductNumber;
                                labProductName.Text = _scanDataWeight.ProductName;
                                labQuantity.Text = _scanDataWeight.Quantity.ToString();
                                labColor.Text = res.Color;
                                labSize.Text = res.SizeName;
                                labAveWeight.Text = _scanDataWeight.AveWeight1Prs.ToString();
                                labLowerTolerance.Text = _scanDataWeight.LowerTolerance.ToString();
                                labUpperTolerance.Text = _scanDataWeight.UpperTolerance.ToString();
                                labBoxWeight.Text = _scanDataWeight.BoxWeight.ToString();
                                labAccessoriesWeight.Text = _scanDataWeight.PackageWeight.ToString();
                                labGrossWeight.Text = _scanDataWeight.StdGrossWeight.ToString();
                            });
                            #endregion

                            #region xử lý so sánh khối lượng cân thực tế với kế hoạch để xử lý
                            _scanDataWeight.NetWeight = Math.Round(_scanDataWeight.GrossWeight - _scanDataWeight.BoxWeight - _scanDataWeight.PackageWeight, 3);
                            _scanDataWeight.Deviation = Math.Round(_scanDataWeight.NetWeight - _scanDataWeight.StdNetWeight, 3);

                            #region tính toán số pairs chênh lệch và hiển thị label
                            //var nwPlus = _scanDataWeight.StdNetWeight + _scanDataWeight.Tolerance;
                            //var nwSub = _scanDataWeight.StdNetWeight - _scanDataWeight.Tolerance;
                            nwPlus = _scanDataWeight.StdNetWeight + _scanDataWeight.UpperTolerance;
                            nwSub = _scanDataWeight.StdNetWeight - _scanDataWeight.LowerTolerance;

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
                                    //connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

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
                                        //Auto transfer tem về kho 2 hoặc 10 ( hàng đi sơn)
                                        var res1 = AutoPostingHelper.CheckIn(_scanDataMetal.ProductNumber, barcodeString, connection);
                                        var accept = res1.FirstOrDefault();

                                        var para1 = new DynamicParameters();
                                        para1.Add("@Message", $"Check Weight sp_lmpScannerClient_ScanningLabel_CheckIn =  {res1.Count}.");
                                        para1.Add("@MessageTemplate", $"{barcodeString}");
                                        para1.Add("Level", "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn");
                                        connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                                        if (accept != null)
                                        {
                                            var whTo = 2;

                                            if (_scanDataWeight.OcNo.Substring(0, 2) == "PR") whTo = 10;

                                            GlobalVariables.AutoPostingStatus3 = AutoPostingHelper.AutoTransfer(_scanDataMetal.ProductNumber, barcodeString
                                                , Convert.ToInt16(accept.C004), whTo, connection);

                                            GlobalVariables.InvokeIfRequired(this, () =>
                                            {
                                                labErrInfoScale.Text = GlobalVariables.AutoPostingStatus3;
                                            });
                                        }

                                        _approvePrint = true;//cho phép in

                                        //bat den xanh 
                                        GlobalVariables.MyEvent.StatusLightPLC = 2;
                                        //hien thi mau label
                                        GlobalVariables.InvokeIfRequired(this, () =>
                                        {
                                            labResult.Text = "Pass";
                                            labResult.BackColor = Color.Green;
                                            labResult.ForeColor = Color.White;
                                            labErrInfoScale.Text = "Khối lượng OK. In tem.";
                                        });

                                        //para = null;
                                        //para = new DynamicParameters();
                                        //para.Add("@Message", $"Hàng Outsole check weight OK vào in tem.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                                        //para.Add("Level", "Log");
                                        //connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                                        if (checkOc != null)//neu khong phai tem OC 'PRT' thì mới in tem
                                        {
                                            //var passMetal = _scanDataWeight.MetalScan == 1 && ocFirstChar != "PR" ? "Passed quality check" : " ";

                                            //20240202 update cho in tất cả các thùng passes quality check
                                            var passMetal = "Passed quality check";
                                            var idLabel = !string.IsNullOrEmpty(_scanDataWeight.IdLabel) ? _scanDataWeight.IdLabel : $"{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}";

                                            SendDynamicString($"{idLabel}  {passMetal}"
                                                               , $"{(_scanDataWeight.GrossWeight / 1000).ToString("#,#0.00")} Kg"
                                                               , _scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
                                                              );

                                            //para = null;
                                            //para = new DynamicParameters();
                                            //para.Add("@Message", $"Hàng Outsole check weight OK đã in tem.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                                            //para.Add("Level", "Log");
                                            //connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);
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


                                            //para = null;
                                            //para = new DynamicParameters();
                                            //para.Add("@Message", $"Hàng Outsole trước sơn check weight OK đã in tem.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                                            //para.Add("Level", "Log");
                                            //connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"Thùng OC đã được quét ghi nhận khối lượng OK rồi, không được phép cân lại." +
                                            $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                        //ghi giá trị xuống PLC cân reject
                                        GlobalVariables.MyEvent.WeightPusher = 1;

                                        //bat den đỏ 
                                        GlobalVariables.MyEvent.StatusLightPLC = 1;
                                        //hien thi mau label

                                        if (this.InvokeRequired)
                                        {
                                            this.Invoke(new Action(() =>
                                            {
                                                labResult.Text = "Fail";
                                                labResult.BackColor = Color.Red;
                                                labResult.ForeColor = Color.White;
                                                labErrInfoScale.Text = "Thùng này đã ghi nhận OK rồi.";
                                            }));
                                        }
                                        else
                                        {
                                            labResult.Text = "Fail";
                                            labResult.BackColor = Color.Red;
                                            labResult.ForeColor = Color.White;
                                            labErrInfoScale.Text = "Thùng này đã ghi nhận OK rồi.";
                                        }

                                        //para = null;
                                        //para = new DynamicParameters();
                                        //para.Add("@Message", $"Hàng Outsole check weight OK nhưng đã ghi nhận dữ liệu - Cho qua mà không in tem.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                                        //para.Add("Level", "Log");
                                        //connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                                        //ResetControl();
                                        goto returnLoop;
                                    }
                                }
                                else//thung fail
                                {
                                    //transfer from WH in comming to 965
                                    //đến đây thì chắc chắn nó đang nằm ở 1185 hoặc 1223
                                    #region Auto transfer 
                                    var res1 = AutoPostingHelper.CheckIn(_scanDataMetal.ProductNumber, barcodeString, connection);
                                    var accept = res1.FirstOrDefault();

                                    var para1 = new DynamicParameters();
                                    para1.Add("@Message", $"Check Weight sp_lmpScannerClient_ScanningLabel_CheckIn =  {res1.Count}.");
                                    para1.Add("@MessageTemplate", $"{barcodeString}");
                                    para1.Add("Level", "Auto Transfer|sp_lmpScannerClient_ScanningLabel_CheckIn");
                                    connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);
                                    if (accept != null)
                                    {
                                        GlobalVariables.AutoPostingStatus3 = AutoPostingHelper.AutoTransfer(_scanDataMetal.ProductNumber, barcodeString, Convert.ToInt16(accept.C004), 965, connection);

                                        GlobalVariables.InvokeIfRequired(this, () =>
                                        {
                                            labErrInfoScale.Text = GlobalVariables.AutoPostingStatus3;
                                        });
                                    }
                                    #endregion

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

                                    if (statusLogData == 0)
                                    {
                                        //hien thi mau label

                                        GlobalVariables.InvokeIfRequired(this, () =>
                                        {
                                            labResult.Text = "Fail";
                                            labResult.BackColor = Color.Red;
                                            labResult.ForeColor = Color.White;
                                            labErrInfoScale.Text = "Khối lượng lỗi.";
                                        });

                                        //log vao bang reject
                                        para = null;
                                        para = new DynamicParameters();
                                        para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                                        para.Add("_idLabel", _scanDataWeight.IdLabel);
                                        para.Add("_ocNo", _scanDataWeight.OcNo);
                                        para.Add("_boxId", _scanDataWeight.BoxNo);
                                        para.Add("_productNumber", _scanDataWeight.ProductNumber);
                                        para.Add("_productName", _scanDataWeight.ProductName);
                                        para.Add("_quantity", _scanDataWeight.Quantity);
                                        para.Add("_scannerStation", "Scale");
                                        para.Add("_reason", "Khối lượng lỗi.");
                                        para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                                        para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                                        para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                                        connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                                        #region Log data
                                        //mỗi thùng chỉ cho log vào tối da là 2 dòng trong scanData, 1 dòng pass và fail (nếu có)
                                        //tính lại tỷ lệ khối lượng số đôi lỗi/ StdGrossWeight của lần scan này để log
                                        //_scanDataWeight.RatioFailWeight = Math.Round((Math.Abs(_scanDataWeight.DeviationPairs) * _scanDataWeight.AveWeight1Prs) / _scanDataWeight.StdGrossWeight, 3);

                                        para = null;
                                        para = new DynamicParameters();
                                        para.Add("@BarcodeString", _scanDataWeight.BarcodeString);
                                        para.Add("@IdLabel", _scanDataWeight.IdLabel);
                                        para.Add("@OcNo", _scanDataWeight.OcNo);
                                        para.Add("@ProductNumber", _scanDataWeight.ProductNumber);
                                        para.Add("@ProductName", _scanDataWeight.ProductName);
                                        para.Add("@Quantity", _scanDataWeight.Quantity);
                                        para.Add("@LinePosNo", _scanDataWeight.LinePosNo);
                                        para.Add("@Unit", _scanDataWeight.Unit);
                                        para.Add("@BoxNo", _scanDataWeight.BoxNo);
                                        para.Add("@CustomerNo", _scanDataWeight.CustomerNo);
                                        para.Add("@Location", _scanDataWeight.Location);
                                        para.Add("@BoxPosNo", _scanDataWeight.BoxPosNo);
                                        para.Add("@Note", _scanDataWeight.Note);
                                        para.Add("@Brand", _scanDataWeight.Brand);
                                        para.Add("@Decoration", _scanDataWeight.Decoration);
                                        para.Add("@MetalScan", _scanDataWeight.MetalScan);
                                        para.Add("@ActualMetalScan", _scanDataWeight.ActualMetalScan);
                                        para.Add("@AveWeight1Prs", _scanDataWeight.AveWeight1Prs);
                                        para.Add("@StdNetWeight", _scanDataWeight.StdNetWeight);
                                        para.Add("@LowerTolerance", _scanDataWeight.LowerTolerance);
                                        para.Add("@UpperTolerance", _scanDataWeight.UpperTolerance);
                                        para.Add("@Boxweight", _scanDataWeight.BoxWeight);
                                        para.Add("@PackageWeight", _scanDataWeight.PackageWeight);
                                        para.Add("@StdGrossWeight", _scanDataWeight.StdGrossWeight);
                                        para.Add("@GrossWeight", _scanDataWeight.GrossWeight);
                                        para.Add("@NetWeight", _scanDataWeight.NetWeight);
                                        para.Add("@Deviation", _scanDataWeight.Deviation);
                                        para.Add("@Pass", _scanDataWeight.Pass);
                                        para.Add("Status", _scanDataWeight.Status);
                                        para.Add("CalculatedPairs", _scanDataWeight.CalculatedPairs);
                                        para.Add("DeviationPairs", _scanDataWeight.DeviationPairs);
                                        para.Add("CreatedBy", _scanDataWeight.CreatedBy);
                                        para.Add("Station", _scanDataWeight.Station);
                                        para.Add("CreatedDate", _scanDataWeight.CreatedDate);
                                        para.Add("ApprovedBy", _scanDataWeight.ApprovedBy);
                                        para.Add("ActualDeviationPairs", _scanDataWeight.ActualDeviationPairs);
                                        para.Add("RatioFailWeight", _scanDataWeight.RatioFailWeight);
                                        para.Add("ProductCategory", _scanDataWeight.ProductCategory);
                                        para.Add("LotNo", _scanDataWeight.LotNo);
                                        //para.Add("Id", ParameterDirection.Output, DbType.Guid);

                                        var insertResult = connection.Execute("sp_tblScanDataInsert", para, commandType: CommandType.StoredProcedure);
                                        #endregion
                                    }
                                    else if (statusLogData == 1)
                                    {
                                        Debug.WriteLine($"Thùng OC đã được quét ghi nhận khối lượng lỗi rồi, không được phép cân lại." +
                                             $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information); ;

                                        //hien thi mau label

                                        GlobalVariables.InvokeIfRequired(this, () =>
                                        {
                                            labResult.Text = "Fail";
                                            labResult.BackColor = Color.Red;
                                            labResult.ForeColor = Color.White;
                                            labErrInfoScale.Text = "Thùng này đã ghi nhận khối lượng lỗi rồi.";
                                        });

                                        //log vao bang reject
                                        para = null;
                                        para = new DynamicParameters();
                                        para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                                        para.Add("_idLabel", _scanDataWeight.IdLabel);
                                        para.Add("_ocNo", _scanDataWeight.OcNo);
                                        para.Add("_boxId", _scanDataWeight.BoxNo);
                                        para.Add("_productNumber", _scanDataWeight.ProductNumber);
                                        para.Add("_productName", _scanDataWeight.ProductName);
                                        para.Add("_quantity", _scanDataWeight.Quantity);
                                        para.Add("_scannerStation", "Scale");
                                        para.Add("_reason", "Thùng này đã ghi nhận khối lượng lỗi rồi.");
                                        para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                                        para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                                        para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                                        connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                                        //transfer from WH in comming to 965
                                        //đã tồn tại 965 thì ko transfer
                                        //...

                                        //ResetControl();
                                        goto returnLoop;
                                    }
                                    else// if (statusLogData == 2)
                                    {
                                        Debug.WriteLine($"Thùng OC đã được quét ghi nhận khối lượng OK rồi, không được phép cân lại." +
                                            $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                        //hien thi mau label
                                        GlobalVariables.InvokeIfRequired(this, () =>
                                        {
                                            labResult.Text = "Fail";
                                            labResult.BackColor = Color.Red;
                                            labResult.ForeColor = Color.White;
                                            labErrInfoScale.Text = "Thùng này đã ghi nhận khối lượng OK rồi.";
                                        });

                                        //ResetControl();
                                        goto returnLoop;
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
                                //connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

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
                                    GlobalVariables.MyEvent.StatusLightPLC = 2;
                                    //hien thi mau label

                                    GlobalVariables.InvokeIfRequired(this, () =>
                                    {
                                        labResult.Text = "HC_Pass";
                                        labResult.BackColor = Color.Green;
                                        labResult.ForeColor = Color.White;
                                        labErrInfoScale.Text = "Hàng heel counter OK. Không kiểm tra khối lượng.";
                                    });

                                    //para = null;
                                    //para = new DynamicParameters();
                                    //para.Add("@Message", $"Hàng HeelCounter check weight OK vào in tem.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    //para.Add("Level", "Log");
                                    //connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                                    //gui lenh in
                                    //var passMetal = _scanDataWeight.MetalScan == 1 && ocFirstChar != "PR" ? "Passed quality check" : " ";
                                    var passMetal = "Passed quality check";
                                    var idLabel = !string.IsNullOrEmpty(_scanDataWeight.IdLabel) ? _scanDataWeight.IdLabel : $"{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}";

                                    #region get LotNo Brooks, printing label
                                    para = null;
                                    para = new DynamicParameters();
                                    para.Add("ocNo", _scanDataWeight.OcNo);
                                    para.Add("boxNo", _scanDataWeight.BoxNo);

                                    var reader = connection.ExecuteReader("sp_GetLotOfBrooksHC", param: para, commandType: CommandType.StoredProcedure);
                                    DataTable tableResult = new DataTable();
                                    tableResult.Load(reader);

                                    if (tableResult.Rows.Count > 0)
                                    {
                                        _scanDataWeight.LotNo = tableResult.Rows[0]["LotNo"].ToString();
                                    }
                                    #endregion

                                    _approvePrint = true;

                                    SendDynamicString($"{idLabel}  {passMetal}"
                                                        , $"{(_scanDataWeight.GrossWeight / 1000).ToString("#,#0.00")} Kg"
                                                        , $"{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")} {_scanDataWeight.LotNo}"
                                                      );

                                    //para = null;
                                    //para = new DynamicParameters();
                                    //para.Add("@Message", $"Hàng HeelCounter check weight OK đã in tem.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    //para.Add("Level", "Log");
                                    //connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);
                                }
                                else
                                {
                                    Debug.WriteLine($"Thùng HC này đã được quét ghi nhận khối lượng OK rồi, không được phép cân lại." +
                                        $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                    //ghi giá trị xuống PLC cân reject
                                    GlobalVariables.MyEvent.WeightPusher = 1;

                                    GlobalVariables.MyEvent.StatusLightPLC = 1;
                                    //hien thi mau label

                                    GlobalVariables.InvokeIfRequired(this, () =>
                                    {
                                        labErrInfoScale.Text = "Thùng heel counter này đã ghi nhận OK rồi.";
                                        labResult.Text = "HC_Fail";
                                        labResult.BackColor = Color.Red;
                                        labResult.ForeColor = Color.White;
                                    });

                                    //para = null;
                                    //para = new DynamicParameters();
                                    //para.Add("@Message", $"Hàng HeelCounter check weight OK đã ghi nhận data - Không in tem.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                                    //para.Add("Level", "Log");
                                    //connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                                    //ResetControl();
                                    goto returnLoop;
                                }
                            }
                            #endregion

                            #endregion

                            #region Log data
                            //mỗi thùng chỉ cho log vào tối da là 2 dòng trong scanData, 1 dòng pass và fail (nếu có)
                            //tính lại tỷ lệ khối lượng số đôi lỗi/ StdGrossWeight của lần scan này để log
                            //_scanDataWeight.RatioFailWeight = Math.Round((Math.Abs(_scanDataWeight.DeviationPairs) * _scanDataWeight.AveWeight1Prs) / _scanDataWeight.StdGrossWeight, 3);

                            //para = null;
                            //para = new DynamicParameters();
                            //para.Add("@BarcodeString", _scanDataWeight.BarcodeString);
                            //para.Add("@IdLabel", _scanDataWeight.IdLabel);
                            //para.Add("@OcNo", _scanDataWeight.OcNo);
                            //para.Add("@ProductNumber", _scanDataWeight.ProductNumber);
                            //para.Add("@ProductName", _scanDataWeight.ProductName);
                            //para.Add("@Quantity", _scanDataWeight.Quantity);
                            //para.Add("@LinePosNo", _scanDataWeight.LinePosNo);
                            //para.Add("@Unit", _scanDataWeight.Unit);
                            //para.Add("@BoxNo", _scanDataWeight.BoxNo);
                            //para.Add("@CustomerNo", _scanDataWeight.CustomerNo);
                            //para.Add("@Location", _scanDataWeight.Location);
                            //para.Add("@BoxPosNo", _scanDataWeight.BoxPosNo);
                            //para.Add("@Note", _scanDataWeight.Note);
                            //para.Add("@Brand", _scanDataWeight.Brand);
                            //para.Add("@Decoration", _scanDataWeight.Decoration);
                            //para.Add("@MetalScan", _scanDataWeight.MetalScan);
                            //para.Add("@ActualMetalScan", _scanDataWeight.ActualMetalScan);
                            //para.Add("@AveWeight1Prs", _scanDataWeight.AveWeight1Prs);
                            //para.Add("@StdNetWeight", _scanDataWeight.StdNetWeight);
                            //para.Add("@LowerTolerance", _scanDataWeight.LowerTolerance);
                            //para.Add("@UpperTolerance", _scanDataWeight.UpperTolerance);
                            //para.Add("@Boxweight", _scanDataWeight.BoxWeight);
                            //para.Add("@PackageWeight", _scanDataWeight.PackageWeight);
                            //para.Add("@StdGrossWeight", _scanDataWeight.StdGrossWeight);
                            //para.Add("@GrossWeight", _scanDataWeight.GrossWeight);
                            //para.Add("@NetWeight", _scanDataWeight.NetWeight);
                            //para.Add("@Deviation", _scanDataWeight.Deviation);
                            //para.Add("@Pass", _scanDataWeight.Pass);
                            //para.Add("Status", _scanDataWeight.Status);
                            //para.Add("CalculatedPairs", _scanDataWeight.CalculatedPairs);
                            //para.Add("DeviationPairs", _scanDataWeight.DeviationPairs);
                            //para.Add("CreatedBy", _scanDataWeight.CreatedBy);
                            //para.Add("Station", _scanDataWeight.Station);
                            //para.Add("CreatedDate", _scanDataWeight.CreatedDate);
                            //para.Add("ApprovedBy", _scanDataWeight.ApprovedBy);
                            //para.Add("ActualDeviationPairs", _scanDataWeight.ActualDeviationPairs);
                            //para.Add("RatioFailWeight", _scanDataWeight.RatioFailWeight);
                            //para.Add("ProductCategory", _scanDataWeight.ProductCategory);
                            //para.Add("LotNo", _scanDataWeight.LotNo);
                            ////para.Add("Id", ParameterDirection.Output, DbType.Guid);

                            //var insertResult = connection.Execute("sp_tblScanDataInsert", para, commandType: CommandType.StoredProcedure);
                            #endregion

                            #region hiển thị thông tin
                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                labRealWeight.Text = _scanDataWeight.GrossWeight.ToString();
                                labNetWeight.Text = _scanDataWeight.StdNetWeight.ToString();
                                labOcNo.Text = _scanDataWeight.OcNo;
                                labBoxId.Text = _scanDataWeight.BoxNo;
                                labProductCode.Text = _scanDataWeight.ProductNumber;
                                labProductName.Text = _scanDataWeight.ProductName;
                                labQuantity.Text = _scanDataWeight.Quantity.ToString();
                                labColor.Text = res.Color;
                                labSize.Text = res.SizeName;
                                labAveWeight.Text = _scanDataWeight.AveWeight1Prs.ToString();
                                labLowerTolerance.Text = _scanDataWeight.LowerTolerance.ToString();
                                labUpperTolerance.Text = _scanDataWeight.UpperTolerance.ToString();
                                //labLowerToleranceWeight.Text = nwSub.ToString("#.###");
                                //labUpperToleranceWeight.Text = nwPlus.ToString("#.###");
                                labBoxWeight.Text = _scanDataWeight.BoxWeight.ToString();
                                labAccessoriesWeight.Text = _scanDataWeight.PackageWeight.ToString();
                                labGrossWeight.Text = _scanDataWeight.StdGrossWeight.ToString();
                            });
                            #endregion
                        }
                        else
                        {
                            Debug.WriteLine($"Item '{_scanDataWeight.ProductNumber}' không có khối lượng/1 đôi. Xin hãy kiểm tra lại thông tin."
                                , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            //bật đèn đỏ
                            GlobalVariables.MyEvent.StatusLightPLC = 1;
                            //ghi giá trị xuống PLC cân reject
                            GlobalVariables.MyEvent.WeightPusher = 1;

                            ResetControl();

                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                labResult.Text = "Fail";
                                labResult.BackColor = Color.Red;
                                labErrInfoScale.Text = "Không có khối lượng đôi. Weight/Prs.";
                            });

                            //log vao bang reject
                            para = null;
                            para = new DynamicParameters();
                            para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                            para.Add("_idLabel", _scanDataWeight.IdLabel);
                            para.Add("_ocNo", _scanDataWeight.OcNo);
                            para.Add("_boxId", _scanDataWeight.BoxNo);
                            para.Add("_productNumber", _scanDataWeight.ProductNumber);
                            para.Add("_productName", _scanDataWeight.ProductName);
                            para.Add("_quantity", _scanDataWeight.Quantity);
                            para.Add("_scannerStation", "Scale");
                            para.Add("_reason", "Không có khối lượng đôi. Average Weight/prs.");
                            para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                            para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                            para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                            connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                            para = null;
                            para = new DynamicParameters();
                            para.Add("ProductNumber", _scanDataWeight.ProductNumber);
                            para.Add("ProductName", _scanDataWeight.ProductName);
                            para.Add("OcNum", _scanDataWeight.OcNo);
                            para.Add("Note", "Chưa có data trong file QC.");
                            para.Add("QrCode", _scanDataWeight.BarcodeString);

                            connection.Execute("sp_tblItemMissingInfoInsert", para, commandType: CommandType.StoredProcedure);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"Product number {_scanDataWeight.ProductNumber} không có trong hệ thống. Báo cho quản lý để update data mới từ winline về."
                            , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                        //bật đèn đỏ
                        GlobalVariables.MyEvent.StatusLightPLC = 1;
                        //ghi giá trị xuống PLC cân reject
                        GlobalVariables.MyEvent.WeightPusher = 1;

                        ResetControl();

                        GlobalVariables.InvokeIfRequired(this, () =>
                        {
                            labResult.Text = "Fail";
                            labResult.BackColor = Color.Red;
                            labErrInfoScale.Text = "ProductItem không có trong hệ thống.";
                        });

                        //log vao bang reject
                        para = null;
                        para = new DynamicParameters();
                        para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                        para.Add("_idLabel", _scanDataWeight.IdLabel);
                        para.Add("_ocNo", _scanDataWeight.OcNo);
                        para.Add("_boxId", _scanDataWeight.BoxNo);
                        para.Add("_productNumber", _scanDataWeight.ProductNumber);
                        para.Add("_productName", _scanDataWeight.ProductName);
                        para.Add("_quantity", _scanDataWeight.Quantity);
                        para.Add("_scannerStation", "Scale");
                        para.Add("_reason", "Product item chưa có trong hệ thống. Get data từ WL về lại.");
                        para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                        para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                        para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                        connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                        para = null;
                        para = new DynamicParameters();
                        para.Add("ProductNumber", _scanDataWeight.ProductNumber);
                        para.Add("ProductName", _scanDataWeight.ProductName);
                        para.Add("OcNum", _scanDataWeight.OcNo);
                        para.Add("Note", $"Product item '{_scanDataWeight.ProductNumber}' không có data hệ thống.");
                        para.Add("QrCode", _scanDataWeight.BarcodeString);

                        connection.Execute("sp_tblItemMissingInfoInsert", para, commandType: CommandType.StoredProcedure);
                    }
                }
            #endregion

            returnLoop:
                #region hien thi cac thong so dem

                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    labCalculatedPairs.Text = _scanDataWeight.CalculatedPairs.ToString();
                    labDeviationPairs.Text = _scanDataWeight.DeviationPairs.ToString();
                    labDeviation.Text = _scanDataWeight.Deviation.ToString();
                    labNetRealWeight.Text = _scanDataWeight.NetWeight.ToString();
                    labLowerToleranceWeight.Text = nwSub.ToString("#.###");
                    labUpperToleranceWeight.Text = nwPlus.ToString("#.###");
                });
                #endregion

                string json = JsonConvert.SerializeObject(GlobalVariables.RememberInfo);
                File.WriteAllText(@"./RememberInfo.json", json);
                //_readQrStatus[1] = false;//trả lại bit này để quét lần sau
            }
            catch (Exception ex)
            {
                //ghi giá trị xuống PLC cân reject
                GlobalVariables.MyEvent.WeightPusher = 1;

                GlobalVariables.MyEvent.StatusLightPLC = 1;
                //hien thi mau label
                GlobalVariables.InvokeIfRequired(this, () =>
                {
                    labResult.Text = "Fail";
                    labResult.BackColor = Color.Red;
                    labResult.ForeColor = Color.White;
                    labErrInfoScale.Text = "System fail.";
                });

                using (var connection = GlobalVariables.GetDbConnection())
                {
                    DynamicParameters para = new DynamicParameters();
                    para.Add("@Message", $"Lỗi check label.Scanner: 2 (Scale)|{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                    para.Add("Level", "LogError");
                    para.Add("Exception", ex.ToString());
                    connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                    para = null;
                    para = new DynamicParameters();
                    para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                    para.Add("_idLabel", _scanDataWeight.IdLabel);
                    para.Add("_ocNo", _scanDataWeight.OcNo);
                    para.Add("_boxId", _scanDataWeight.BoxNo);
                    para.Add("_productNumber", _scanDataWeight.ProductNumber);
                    para.Add("_productName", _scanDataWeight.ProductName);
                    para.Add("_quantity", _scanDataWeight.Quantity);
                    para.Add("_scannerStation", $"Scale");
                    para.Add("_reason", $"System fail. Ex:{ex.ToString()}.");
                    para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                    para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                    para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                    connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
                }
                Log.Error(ex.ToString(), "Lỗi scale form tại trạm scanner 2 scale.");
            }
            finally
            {

            }
        }

        private void BarcodeScanner3Handle(int station, string barcodeString)
        {
            try
            {
                if (labQrPrint.InvokeRequired)
                {
                    labQrPrint.Invoke(new Action(() =>
                    {
                        labQrPrint.Text = barcodeString;
                    }));
                }
                else
                {
                    labQrPrint.Text = barcodeString;
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

                        if (labErrInfoPrint.InvokeRequired)
                        {
                            labErrInfoPrint.Invoke(new Action(() =>
                            {
                                labErrInfoPrint.Text = "OC không đúng định dạng.";
                            }));
                        }
                        else labErrInfoPrint.Text = "OC không đúng định dạng.";

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

                        if (labErrInfoPrint.InvokeRequired)
                        {
                            labErrInfoPrint.Invoke(new Action(() =>
                            {
                                labErrInfoPrint.Text = "OC không đúng định dạng.";
                            }));
                        }
                        else labErrInfoPrint.Text = "OC không đúng định dạng.";

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

                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var para = new DynamicParameters();

                    para = new DynamicParameters();
                    para.Add("@ProductNumber", _scanDataPrint.ProductNumber);
                    para.Add("@SpecialCase", specialCasePrint);

                    var res = connection.Query<ProductInfoModel>("sp_vProductItemInfoGet", para, commandType: CommandType.StoredProcedure).FirstOrDefault();

                    if (res != null)
                    {
                        var resultCheckOc = GlobalVariables.OcUsingList.FirstOrDefault(x => x.OcFirstChar == ocFirstCharPrint && ocFirstCharPrint == "PR");

                        if (resultCheckOc != null)
                        {
                            Debug.WriteLine($"ProductNumber: {res.ProductNumber} là hàng sơn.");

                            if (labQrPrint.InvokeRequired)
                            {
                                labQrPrint.Invoke(new Action(() =>
                                {
                                    labErrInfoPrint.Text = "Hàng Sơn.";
                                }));
                            }
                            else
                            {
                                labErrInfoPrint.Text = "Hàng Sơn.";
                            }

                            GlobalVariables.MyEvent.PrintPusher = 1;

                            // xử lý insert RackStorage cho hàng sơn (nếu là hàng đi sơn thì vào kho 10)
                            //GlobalVariables.AutoPostingStatus = AutoPostingHelper.AutoTransfer(_scanDataPrint.ProductNumber, barcodeString, 1185, 10, connection);

                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                labErrInfoPrint.Text = "Hàng đi sơn.";
                            });
                        }
                        else// không phải hàng sơn thì transfer vào kho 2
                        {
                            GlobalVariables.MyEvent.PrintPusher = 0;

                            // xử lý insert RackStorage cho hàng sơn (nếu là hàng đi sơn thì vào kho 2)

                            //var accept = AutoPostingHelper.CheckIn(_scanDataPrint.ProductNumber, barcodeString, connection).FirstOrDefault();

                            //GlobalVariables.AutoPostingStatus = AutoPostingHelper.AutoTransfer(_scanDataPrint.ProductNumber, barcodeString, 1223, 2, connection);

                            GlobalVariables.InvokeIfRequired(this, () =>
                            {
                                labErrInfoPrint.Text = "Hàng FG.";
                            });
                        }
                    }
                }
                _readQrStatus[2] = false;//trả lại bit này để quét lần sau
            }
            catch (Exception ex)
            {
                //hien thi mau label
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        labErrInfoPrint.Text = "System fail.";
                    }));
                }
                else
                {
                    labErrInfoPrint.Text = "System fail.";
                }

                using (var connection = GlobalVariables.GetDbConnection())
                {
                    DynamicParameters para = new DynamicParameters();
                    para.Add("@Message", $"Lỗi check label.Scanner: 3 (print)|{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}");
                    para.Add("Level", "LogError");
                    para.Add("Exception", ex.ToString());
                    connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                    para = null;
                    para = new DynamicParameters();
                    para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                    para.Add("_idLabel", _scanDataWeight.IdLabel);
                    para.Add("_ocNo", _scanDataWeight.OcNo);
                    para.Add("_boxId", _scanDataWeight.BoxNo);
                    para.Add("_productNumber", _scanDataWeight.ProductNumber);
                    para.Add("_productName", _scanDataWeight.ProductName);
                    para.Add("_quantity", _scanDataWeight.Quantity);
                    para.Add("_scannerStation", $"Print");
                    para.Add("_reason", $"System fail. Ex:{ex.ToString()}.");
                    para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                    para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                    para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                    connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
                }
                Log.Error(ex.ToString(), "Lỗi scale form tại trạm scanner 3 print.");
            }
            finally
            {

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

            //Get ra ID của scanner
            var scannerId = xmlDoc.GetElementsByTagName("scannerID");

            if (scannerId[0].InnerText == GlobalVariables.ScannerIdMetal.ToString())//vị trí check metal. đầu chuyền
            {
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
                    _scanDataMetal = new tblScanDataModel();

                    BarcodeScanner1Handle(1, _barcodeString1);
                }
            }
            else if (scannerId[0].InnerText == GlobalVariables.ScannerIdWeight.ToString())//vị trí check weight. ngay cân
            {
                if (!_scannerIsBussy[1])
                {
                    //bật biến báo bận lên ko cho scan tiếp, chặn trường hợp thùng dán 2 tem.
                    _scannerIsBussy[1] = true;

                    //bật biến báo đọc đc QR code từ label
                    _readQrStatus[1] = true;

                    _barcodeString2 = AsciiToString(xmlDoc.GetElementsByTagName("datalabel")[0].InnerText);

                    //reset model;
                    //_scanDataWeight = null;
                    //_scanDataWeight = new tblScanDataModel();

                    BarcodeScanner2Handle(2, _barcodeString2);

                    ////reset model;
                    //_scanDataWeight = null;
                    //_scanDataWeight = new tblScanDataModel();
                }
            }
            else if (scannerId[0].InnerText == GlobalVariables.ScannerIdPrint.ToString())//vị trí phân loại hàng sơn cuối chuyền
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
                    _scanDataPrint = new tblScanDataModel();

                    BarcodeScanner3Handle(3, _barcodeString3);

                    //reset model;
                    _scanDataPrint = null;
                    _scanDataPrint = new tblScanDataModel();
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

            _serialPort = new System.IO.Ports.SerialPort(GlobalVariables.PrintComPort, 57600, Parity.None, 8, StopBits.One);
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

                    //using (var connection = GlobalVariables.GetDbConnection())
                    //{
                    //    DynamicParameters para = new DynamicParameters();
                    //    para.Add("@Message", GlobalVariables.PrintedResult);
                    //    para.Add("Level", "Printer");

                    //    connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);
                    //}  //Printed event

                    #region Log data
                    //mỗi thùng chỉ cho log vào tối da là 2 dòng trong scanData, 1 dòng pass và fail (nếu có)
                    //tính lại tỷ lệ khối lượng số đôi lỗi/ StdGrossWeight của lần scan này để log
                    //_scanDataWeight.RatioFailWeight = Math.Round((Math.Abs(_scanDataWeight.DeviationPairs) * _scanDataWeight.AveWeight1Prs) / _scanDataWeight.StdGrossWeight, 3);

                    if (_approvePrint)
                    {
                        _approvePrint = false;
                        using (var connection = GlobalVariables.GetDbConnection())
                        {
                            connection.Open();

                            using (var transaction = connection.BeginTransaction())
                            {
                                try
                                {
                                    var para = new DynamicParameters();
                                    para.Add("@BarcodeString", _scanDataWeight.BarcodeString);
                                    para.Add("@IdLabel", _scanDataWeight.IdLabel);
                                    para.Add("@OcNo", _scanDataWeight.OcNo);
                                    para.Add("@ProductNumber", _scanDataWeight.ProductNumber);
                                    para.Add("@ProductName", _scanDataWeight.ProductName);
                                    para.Add("@Quantity", _scanDataWeight.Quantity);
                                    para.Add("@LinePosNo", _scanDataWeight.LinePosNo);
                                    para.Add("@Unit", _scanDataWeight.Unit);
                                    para.Add("@BoxNo", _scanDataWeight.BoxNo);
                                    para.Add("@CustomerNo", _scanDataWeight.CustomerNo);
                                    para.Add("@Location", _scanDataWeight.Location);
                                    para.Add("@BoxPosNo", _scanDataWeight.BoxPosNo);
                                    para.Add("@Note", _scanDataWeight.Note);
                                    para.Add("@Brand", _scanDataWeight.Brand);
                                    para.Add("@Decoration", _scanDataWeight.Decoration);
                                    para.Add("@MetalScan", _scanDataWeight.MetalScan);
                                    para.Add("@ActualMetalScan", _scanDataWeight.ActualMetalScan);
                                    para.Add("@AveWeight1Prs", _scanDataWeight.AveWeight1Prs);
                                    para.Add("@StdNetWeight", _scanDataWeight.StdNetWeight);
                                    para.Add("@LowerTolerance", _scanDataWeight.LowerTolerance);
                                    para.Add("@UpperTolerance", _scanDataWeight.UpperTolerance);
                                    para.Add("@Boxweight", _scanDataWeight.BoxWeight);
                                    para.Add("@PackageWeight", _scanDataWeight.PackageWeight);
                                    para.Add("@StdGrossWeight", _scanDataWeight.StdGrossWeight);
                                    para.Add("@GrossWeight", _scanDataWeight.GrossWeight);
                                    para.Add("@NetWeight", _scanDataWeight.NetWeight);
                                    para.Add("@Deviation", _scanDataWeight.Deviation);
                                    para.Add("@Pass", _scanDataWeight.Pass);
                                    para.Add("Status", _scanDataWeight.Status);
                                    para.Add("CalculatedPairs", _scanDataWeight.CalculatedPairs);
                                    para.Add("DeviationPairs", _scanDataWeight.DeviationPairs);
                                    para.Add("CreatedBy", _scanDataWeight.CreatedBy);
                                    para.Add("Station", _scanDataWeight.Station);
                                    para.Add("CreatedDate", _scanDataWeight.CreatedDate);
                                    para.Add("ApprovedBy", _scanDataWeight.ApprovedBy);
                                    para.Add("ActualDeviationPairs", _scanDataWeight.ActualDeviationPairs);
                                    para.Add("RatioFailWeight", _scanDataWeight.RatioFailWeight);
                                    para.Add("ProductCategory", _scanDataWeight.ProductCategory);
                                    para.Add("LotNo", _scanDataWeight.LotNo);
                                    //para.Add("Id", ParameterDirection.Output, DbType.Guid);

                                    connection.Execute("sp_tblScanDataInsert", para, commandType: CommandType.StoredProcedure, transaction: transaction);

                                    transaction.Commit();
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();

                                    //ghi giá trị xuống PLC cân reject
                                    GlobalVariables.MyEvent.WeightPusher = 1;

                                    //bat den đỏ 
                                    GlobalVariables.MyEvent.StatusLightPLC = 1;

                                    if (this.InvokeRequired)
                                    {
                                        this.Invoke(new Action(() =>
                                        {
                                            labResult.Text = "Fail Printing";
                                            labResult.BackColor = Color.Red;
                                            labResult.ForeColor = Color.White;
                                            labErrInfoScale.Text = "System fail. Lỗi không ghi dữ liệu vào DB được.";
                                        }));
                                    }
                                    else
                                    {
                                        labResult.Text = "Fail Printing";
                                        labResult.BackColor = Color.Red;
                                        labResult.ForeColor = Color.White;
                                        labErrInfoScale.Text = "System fail. Lỗi không ghi dữ liệu vào DB được.";
                                    }

                                    Log.Error(ex, $"Lỗi không insert vào DB được.{ex.ToString()}");
                                }
                                finally
                                {
                                    _scanDataWeight = null;
                                    _scanDataWeight = new tblScanDataModel();
                                }
                            }

                        }
                    }
                    #endregion

                    //reset model;
                    //_scanDataWeight = null;
                    //_scanDataWeight = new tblScanDataModel();
                    //xoa string
                    //SendDynamicString(" ", " ", " ");
                }
                else if (rcvArr[4] == 0x4F)
                {
                    Console.WriteLine($"Gui lenh xuong may in thanh cong!!!");
                    GlobalVariables.PrintResult = $"Gửi lệnh xuống máy in thành công.{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}";
                    //using (var connection = GlobalVariables.GetDbConnection())
                    //{
                    //    DynamicParameters para = new DynamicParameters();
                    //    para.Add("@Message", GlobalVariables.PrintResult);
                    //    para.Add("Level", "Printer");

                    //    connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);
                    //}
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

                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            labResult.Text = "Fail Printing";
                            labResult.BackColor = Color.Red;
                            labResult.ForeColor = Color.White;
                            labErrInfoScale.Text = "IN KHÔNG THÀNH CÔNG.";
                        }));
                    }
                    else
                    {
                        labResult.Text = "Fail Printing";
                        labResult.BackColor = Color.Red;
                        labResult.ForeColor = Color.White;
                        labErrInfoScale.Text = "IN KHÔNG THÀNH CÔNG.";
                    }

                    using (var connection = GlobalVariables.GetDbConnection())
                    {
                        DynamicParameters para = new DynamicParameters();
                        para.Add("@Message", GlobalVariables.PrintResult);
                        para.Add("Level", "Printer");

                        connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);
                    }

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

                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            labResult.Text = "Fail Printing";
                            labResult.BackColor = Color.Red;
                            labResult.ForeColor = Color.White;
                            labErrInfoScale.Text = "IN KHÔNG THÀNH CÔNG.";
                        }));
                    }
                    else
                    {
                        labResult.Text = "Fail Printing";
                        labResult.BackColor = Color.Red;
                        labResult.ForeColor = Color.White;
                        labErrInfoScale.Text = "IN KHÔNG THÀNH CÔNG.";
                    }

                    using (var connection = GlobalVariables.GetDbConnection())
                    {
                        DynamicParameters para = new DynamicParameters();
                        //kiem tra neu co log vao thi xoa di
                        para.Add("_QrCode", _scanDataWeight.BarcodeString);
                        connection.Execute("[sp_tblScanDataGetByQrCodeUpdateDeactive]", param: para, commandType: CommandType.StoredProcedure);

                        para = null;
                        para = new DynamicParameters();
                        para.Add("@Message", GlobalVariables.PrintResult);
                        para.Add("Level", "Printer");

                        connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                        para = null;
                        para = new DynamicParameters();
                        para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                        para.Add("_idLabel", _scanDataWeight.IdLabel);
                        para.Add("_ocNo", _scanDataWeight.OcNo);
                        para.Add("_boxId", _scanDataWeight.BoxNo);
                        para.Add("_productNumber", _scanDataWeight.ProductNumber);
                        para.Add("_productName", _scanDataWeight.ProductName);
                        para.Add("_quantity", _scanDataWeight.Quantity);
                        para.Add("_scannerStation", "Scale");
                        para.Add("_reason", GlobalVariables.PrintResult);
                        para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                        para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                        para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                        connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
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

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        labResult.Text = "EXCEPTION Printing";
                        labResult.BackColor = Color.Red;
                        labResult.ForeColor = Color.White;
                        labErrInfoScale.Text = "EXCEPTION of printing.";
                    }));
                }
                else
                {
                    labResult.Text = "EXCEPTION Printing";
                    labResult.BackColor = Color.Red;
                    labResult.ForeColor = Color.White;
                    labErrInfoScale.Text = "EXCEPTION of printing.";
                }
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

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        labResult.Text = "Fail Printing";
                        labResult.BackColor = Color.Red;
                        labResult.ForeColor = Color.White;
                        labErrInfoScale.Text = "System fail. Lỗi khi đang truyền dữ liệu xuống máy in.";
                    }));
                }
                else
                {
                    labResult.Text = "Fail Printing";
                    labResult.BackColor = Color.Red;
                    labResult.ForeColor = Color.White;
                    labErrInfoScale.Text = "System fail. Lỗi khi đang truyền dữ liệu xuống máy in.";
                }

                using (var connection = GlobalVariables.GetDbConnection())
                {
                    DynamicParameters para = new DynamicParameters();
                    para.Add("@Message", $"Lỗi khi đang truyền dữ liệu xuống máy in:|{_scanDataWeight.IdLabel}|{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}|{_scanDataWeight.GrossWeight}|{_scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")}. Exception:{ex.ToString()}");
                    para.Add("Level", "LogError");
                    para.Add("Exception", ex.ToString());
                    connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                    para = null;
                    para = new DynamicParameters();
                    para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                    para.Add("_idLabel", _scanDataWeight.IdLabel);
                    para.Add("_ocNo", _scanDataWeight.OcNo);
                    para.Add("_boxId", _scanDataWeight.BoxNo);
                    para.Add("_productNumber", _scanDataWeight.ProductNumber);
                    para.Add("_productName", _scanDataWeight.ProductName);
                    para.Add("_quantity", _scanDataWeight.Quantity);
                    para.Add("_scannerStation", "Scale");
                    para.Add("_reason", $"System fail. Lỗi khi đang truyền dữ liệu xuống máy in. Ex:{ex.ToString()}.");
                    para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                    para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                    para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                    connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
                }

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

        private void btnGetDelay_Click(object sender, EventArgs e)
        {
            try
            {
                byte[] GetDelay = new byte[] { 0x2, 0x0, 0x4, 0x0, 0x64, 0x2, 0x0, 0x6A, 0x3 };
                // ID ban tin =2
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

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        labErrInfoMetal.Text = "Không đọc được QR code, Kiểm tra lại tem.";
                        labQrMetal.Text = string.Empty;
                    }));
                }
                else
                {
                    labErrInfoMetal.Text = "Không đọc được QR code, Kiểm tra lại tem.";
                    labQrMetal.Text = string.Empty;
                }

                //hết thời gian đọc QR code mà chưa đọc được
                //gui data xuong PLC báo reject metalPusher
                _metalScannerStatus = 1;

                _scanDataMetal = null;
                _scanDataMetal = new tblScanDataModel();
                //log vao bang reject
                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var para = new DynamicParameters();
                    //para.Add("_barcodeString", _scanDataMetal.BarcodeString);
                    //para.Add("_idLabel", _scanDataMetal.IdLabel);
                    //para.Add("_ocNo", _scanDataMetal.OcNo);
                    //para.Add("_boxId", _scanDataMetal.BoxNo);
                    //para.Add("_productNumber", _scanDataMetal.ProductNumber);
                    //para.Add("_productName", _scanDataMetal.ProductName);
                    //para.Add("_quantity", _scanDataMetal.Quantity);
                    para.Add("_scannerStation", "Identification");
                    para.Add("_reason", "Không đọc được QR code trạm Identification.");
                    //para.Add("_grossWeight", _scanDataMetal.GrossWeight);
                    //para.Add("@_deviationPairs", _scanDataMetal.DeviationPairs);
                    //para.Add("@_deviationWeight", _scanDataMetal.Deviation);

                    connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
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

            while (timeCheck <= GlobalVariables.TimeCheckQrScale)
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

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        labErrInfoScale.Text = "Không đọc được QR code, Kiểm tra lại tem.";
                        labQrScale.Text = string.Empty;
                        labResult.Text = "Fail";
                        labResult.BackColor = Color.Red;
                        labResult.ForeColor = Color.White;
                    }));
                }
                else
                {
                    labErrInfoScale.Text = "Không đọc được QR code, Kiểm tra lại tem.";
                    labQrScale.Text = string.Empty;
                    labResult.Text = "Fail";
                    labResult.BackColor = Color.Red;
                    labResult.ForeColor = Color.White;
                }

                //hết thời gian đọc QR code mà chưa đọc được
                //gui data xuong PLC báo reject metalPusher
                GlobalVariables.MyEvent.WeightPusher = 1;
                //bật đèn đỏ
                GlobalVariables.MyEvent.StatusLightPLC = 1;

                //_scanDataWeight = null;
                //_scanDataWeight = new tblScanDataModel();

                //log vao bang reject
                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var para = new DynamicParameters();
                    //para.Add("_barcodeString", _scanDataWeight.BarcodeString);
                    //para.Add("_idLabel", _scanDataWeight.IdLabel);
                    //para.Add("_ocNo", _scanDataWeight.OcNo);
                    //para.Add("_boxId", _scanDataWeight.BoxNo);
                    //para.Add("_productNumber", _scanDataWeight.ProductNumber);
                    //para.Add("_productName", _scanDataWeight.ProductName);
                    //para.Add("_quantity", _scanDataWeight.Quantity);
                    para.Add("_scannerStation", "Scale");
                    para.Add("_reason", "Không đọc được QR code trạm cân.");
                    //para.Add("_grossWeight", _scanDataWeight.GrossWeight);
                    //para.Add("@_deviationPairs", _scanDataWeight.DeviationPairs);
                    //para.Add("@_deviationWeight", _scanDataWeight.Deviation);

                    connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
                }
            }
            _readQrStatus[1] = false;//xóa biến này cho lần đọc kế tiếp
        }
        #endregion
    }
}