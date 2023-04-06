using CoreScanner;
using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WeightChecking
{
    public partial class frmScale : DevExpress.XtraEditors.XtraForm
    {
        private ScaleHelper _scaleHelper;
        private Task _ckTask, _ckQRTask, _ckQrWeightScanTask;//task kiểm tra tại các trạm scanner để check xem có đoc đc QR code ko
        private bool _isStartCountTimer = true;
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

        public frmScale()
        {
            InitializeComponent();

            Load += FrmScale_Load;
        }

        private void FrmScale_Load(object sender, EventArgs e)
        {
            #region hien thi cac thong so dem
            this.Invoke((MethodInvoker)delegate
            {
                labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString();
                labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();
                labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();
                labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();
                labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();
                labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();
                labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();
            });
            #endregion

            #region đăng ký sự kiện từ cac PLC
            GlobalVariables.MyEvent.EventHandlerRefreshMasterData += (s, o) =>
            {
                #region hien thi cac thong so dem

                this.Invoke((MethodInvoker)delegate
                {
                    labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString();
                    labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();
                    labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();
                    labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();
                    labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                    labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();
                    labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();
                    labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();
                });
                #endregion
            };

            //sự kiện lấy số cân hiện tại cảu đầu cân (real time)
            GlobalVariables.MyEvent.EventHandleScaleValue += (s, o) =>
            {
                Debug.WriteLine($"Event Scale value real time: {o.ScaleValue}");
                _scaleValue = o.ScaleValue;

                this.Invoke((MethodInvoker)delegate { labScaleValue.Text = _scaleValue.ToString(); });
            };
            //sự kiến lấy khối lượng cân đã chốt ổn định
            GlobalVariables.MyEvent.EventHandlerScaleValueStable += (s, o) =>
            {
                Debug.WriteLine($"Event Scale value stable: {o.ScaleValue}");

                _scaleValueStable = o.ScaleValue;

                GlobalVariables.RealWeight = _scaleValueStable;

                //this.Invoke((MethodInvoker)delegate { labScaleValue.Text = _scanData.GrossWeight.ToString(); });
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
                //chạy task đếm thời gian cho việc quét tem, hết thời gian mà chưa nhận đc tín hiệu từ metal scanner
                //thì ghi tín hiêu xuống PLC conveyor để reject với lý do là không đọc đc QR
                if (_isStartCountTimer == false)
                {
                    _isStartCountTimer = true;

                    if (o.NewValue == 1)
                    {
                        _ckQRTask = new Task(() => CheckReadQr((int)(GlobalVariables.TimeCheckQrMetal + 2)));
                        _ckQRTask.Start();
                    }
                    else if (o.NewValue == 0)
                    {
                        _ckQRTask = new Task(() => CheckReadQr((int)(GlobalVariables.TimeCheckQrMetal)));
                        _ckQRTask.Start();
                    }
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
                Debug.WriteLine($"Event Sensor After metal scan: {o.NewValue}");
                GlobalVariables.RememberInfo.CountMetalScan += 1;//đếm số thùng đi qua máy metalScan

                if (o.NewValue == 1)
                {
                    if (_metalCheckResult == 1)
                    {
                        GlobalVariables.MyEvent.MetalPusher1 = 1;

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
                            para.Add("_scannerStation", "Metal");
                            para.Add("_reason", "Dò kim loại lỗi");

                            connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
                        }
                    }
                    else
                    {
                        GlobalVariables.MyEvent.MetalPusher1 = 0;
                    }

                    //log gia thông tin check metal vào bảng tblMetalScanResult
                    using (var connection = GlobalVariables.GetDbConnection())
                    {
                        var para = new DynamicParameters();
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

            //_scaleValueStable = 2784;
            //BarcodeHandle(2, "C100028,6817012205-2397-D243,1,2,P,2/2,1900068,1/1|2,22421.2023,,,");
            //GlobalVariables.MyEvent.SensorBeforeWeightScan = 1;
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
            this.Invoke((MethodInvoker)delegate
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

        /// <summary>
        /// xử lý các tác vụ theo barcode theo trạm.
        /// </summary>
        /// <param name="station">vị trí quét barcode. 1-metal; 2-weight; 3-printing.</param>
        /// <param name="barcodeString">barcode.</param>
        private void BarcodeHandle(int station, string barcodeString)
        {
            try
            {
                switch (station)
                {
                    case 1://check metal
                        this.Invoke((MethodInvoker)delegate { labQrMetal.Text = barcodeString; });

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
                                this.Invoke((MethodInvoker)delegate { labErrInfoMetal.Text = "OC không đúng định dạng"; });
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
                                    para.Add("_scannerStation", "Metal");
                                    para.Add("_reason", "OC không đúng định dạng");

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
                                this.Invoke((MethodInvoker)delegate { labErrInfoMetal.Text = "OC không đúng định dạng."; });
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
                                    para.Add("_scannerStation", "Metal");
                                    para.Add("_reason", "OC không đúng định dạng");

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
                            #region Kiểm tra xem thùng này đã được log vào scanData chưa
                            //para.Add("QRLabel", _scanData.BarcodeString);
                            //var checkInfo = connection.Query<tblScanDataCheckModel>("sp_tblScanDataCheck", para, commandType: CommandType.StoredProcedure).ToList();
                            var para = new DynamicParameters();
                            para.Add("_QrCode", _scanDataMetal.BarcodeString);
                            var checkInfo = connection.Query<tblScanDataModel>("sp_tblScanDataGetByQrCode", para, commandType: CommandType.StoredProcedure).ToList();
                            foreach (var item in checkInfo)
                            {
                                if (
                                    (item.Pass == 1 && (item.Status == 2 || GlobalVariables.Station == StationEnum.IDC_1))
                                    //|| (item.Pass == 0 && item.ActualDeviationPairs == 0 && item.ApprovedBy != Guid.Empty)
                                    || (item.Pass == 0 && item.Status == 2 && item.ActualDeviationPairs == 0)
                                    )
                                {
                                    Debug.WriteLine($"ProductNumber: {item.ProductNumber} đã kiểm tra OK, không được.");
                                    this.Invoke((MethodInvoker)delegate { labErrInfoMetal.Text = "Thùng này đã check OK."; });
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
                                    para.Add("_scannerStation", "Metal");
                                    para.Add("_reason", "Thùng này đã check OK.");

                                    connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                                    return;
                                }
                            }
                            #endregion

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
                                        this.Invoke((MethodInvoker)delegate { labErrInfoMetal.Text = "Hàng kiểm kim loại."; });
                                        _metalScannerStatus = 0;
                                        //GlobalVariables.MyEvent.MetalPusher = 0;
                                    }
                                    else if (res.MetalScan == 0 || (res.MetalScan == 1 && ocFirstCharMetal == "PR"))
                                    {
                                        Debug.WriteLine($"ProductNumber: {res.ProductNumber} không kiểm tra kim loại.");
                                        this.Invoke((MethodInvoker)delegate { labErrInfoMetal.Text = "Hàng không kiểm kim loại."; });
                                        // gui data xuong PLC
                                        _metalScannerStatus = 2;
                                        //GlobalVariables.MyEvent.MetalPusher = 2;
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine($"Item '{_scanDataWeight.ProductNumber}' không có khối lượng/1 đôi. Xin hãy kiểm tra lại thông tin."
                                        , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                    this.Invoke((MethodInvoker)delegate
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
                                    para.Add("_scannerStation", "Metal");
                                    para.Add("_reason", "Không có khối lượng đôi. Average Weight/prs.");

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
                                this.Invoke((MethodInvoker)delegate { labErrInfoMetal.Text = "ProductItem chưa có trên hệ thống."; });

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
                                para.Add("_scannerStation", "Metal");
                                para.Add("_reason", "Product item chưa có trong hệ thống. Get data từ WL về lại.");

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
                        }

                        // _readQrStatus[0] = false;//trả lại bit này để quét lần sau
                        break;
                    case 2://trạm cân
                        this.Invoke((MethodInvoker)delegate { labQrScale.Text = barcodeString; });//hiển thị QR lên label

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
                                this.Invoke((MethodInvoker)delegate { labErrInfoScale.Text = "OC không đúng định dạng."; });
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
                                this.Invoke((MethodInvoker)delegate { labErrInfoScale.Text = "OC không đúng định dạng."; });
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

                                            this.Invoke((MethodInvoker)delegate { labBoxType.Text = "BX4"; });
                                        }
                                        else if (_scanDataWeight.Quantity > res.BoxQtyBx4 && _scanDataWeight.Quantity <= res.BoxQtyBx3)
                                        {
                                            _scanDataWeight.BoxWeight = res.BoxWeightBx3;

                                            this.Invoke((MethodInvoker)delegate { labBoxType.Text = "BX3"; });
                                        }
                                        else if (_scanDataWeight.Quantity > res.BoxQtyBx3 && _scanDataWeight.Quantity <= res.BoxQtyBx2)
                                        {
                                            _scanDataWeight.BoxWeight = res.BoxWeightBx2;

                                            this.Invoke((MethodInvoker)delegate { labBoxType.Text = "BX2"; });
                                        }
                                        else if (_scanDataWeight.Quantity > res.BoxQtyBx2 && _scanDataWeight.Quantity <= res.BoxQtyBx1)
                                        {
                                            _scanDataWeight.BoxWeight = res.BoxWeightBx1;

                                            this.Invoke((MethodInvoker)delegate { labBoxType.Text = "BX1"; });
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

                                            connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);


                                            ResetControl();

                                            this.Invoke((MethodInvoker)delegate
                                            {
                                                labResult.Text = "Fail";
                                                labResult.BackColor = Color.Red;
                                                labErrInfoScale.Text = "Quantity box error.";
                                            });

                                            goto returnLoop;
                                        }

                                        if (_scanDataWeight.Decoration == 0)
                                        {
                                            this.Invoke((MethodInvoker)delegate { labDecoration.BackColor = Color.Gray; });
                                        }
                                        else
                                        {
                                            this.Invoke((MethodInvoker)delegate { labDecoration.BackColor = Color.Green; });
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

                                        this.Invoke((MethodInvoker)delegate { labDecoration.BackColor = Color.Gold; });
                                    }

                                    if (_scanDataWeight.MetalScan == 0)
                                    {
                                        _approveUpdateActMetalScan = false;

                                        this.Invoke((MethodInvoker)delegate { labMetalScan.BackColor = Color.Gray; });
                                    }
                                    else
                                    {
                                        GlobalVariables.RememberInfo.MetalScan += 1;

                                        _approveUpdateActMetalScan = true;

                                        this.Invoke((MethodInvoker)delegate { labMetalScan.BackColor = Color.Gold; });
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
                                    this.Invoke((MethodInvoker)delegate
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

                                    //thung hang Pass
                                    if (_scanDataWeight.DeviationPairs == 0)
                                    {
                                        _scanDataWeight.Pass = 1;//báo thùng pass
                                        _scanDataWeight.CreatedDate = GlobalVariables.CreatedDate = DateTime.Now;//lấy thời gian để đồng bộ giữa in tem và log DB Printing
                                                                                                                 //bật tín hiệu để PLC on đèn xanh
                                        GlobalVariables.MyEvent.StatusLightPLC = 2;

                                        if (_scanDataWeight.Decoration == 0)
                                        {
                                            GlobalVariables.RememberInfo.GoodBoxPrinting += 1;
                                        }
                                        else
                                        {
                                            GlobalVariables.RememberInfo.GoodBoxNoPrinting += 1;
                                        }

                                        //hien thi mau label
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            labResult.Text = "Pass";
                                            labResult.BackColor = Color.Green;
                                            labResult.ForeColor = Color.White;
                                            labErrInfoScale.Text = "Khối lượng OK. In tem.";
                                        });

                                        //kiểm tra xem data đã có trên hệ thống hay chưa
                                        if (statusLogData == 0 || statusLogData == 1)
                                        {
                                            //gui lenh in
                                            SendDynamicString((_scanDataWeight.GrossWeight / 1000).ToString("#,#0.00")
                                                , _scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
                                                , !string.IsNullOrEmpty(GlobalVariables.IdLabel) ? GlobalVariables.IdLabel : $"{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}");
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"Thùng này đã được quét ghi nhận khối lượng OK rồi, không được phép cân lại." +
                                                $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            this.Invoke((MethodInvoker)delegate { labErrInfoScale.Text = "Thùng này đã ghi nhận OK rồi."; });
                                            //ghi giá trị xuống PLC cân reject
                                            //GlobalVariables.MyEvent.WeightPusher = 1;

                                            //ResetControl();
                                            goto returnLoop;
                                        }
                                        //GlobalVariables.RealWeight = _scanDataWeight.GrossWeight;
                                        //GlobalVariables.PrintApprove = true;
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

                                        //hien thi mau label
                                        this.Invoke((MethodInvoker)delegate
                                        {
                                            labResult.Text = "Fail";
                                            labResult.BackColor = Color.Red;
                                            labResult.ForeColor = Color.White;
                                            labErrInfoScale.Text = "Khối lượng lỗi.";
                                        });

                                        if (statusLogData == 0)
                                        {
                                            //SendDynamicString(_scanDataWeight.DeviationPairs.ToString("#,#0.00")
                                            //    , _scanDataWeight.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss")
                                            //    , !string.IsNullOrEmpty(GlobalVariables.IdLabel) ? GlobalVariables.IdLabel : $"{_scanDataWeight.OcNo}|{_scanDataWeight.BoxNo}");
                                        }
                                        else if (statusLogData == 1)
                                        {
                                            Debug.WriteLine($"Thùng này đã được quét ghi nhận khối lượng lỗi rồi, không được phép cân lại." +
                                                 $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information); ;
                                            this.Invoke((MethodInvoker)delegate { labErrInfoScale.Text = "Thùng này đã ghi nhận khối lượng lỗi rồi."; });
                                            //ghi giá trị xuống PLC cân reject
                                            GlobalVariables.MyEvent.WeightPusher = 1;

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

                                            connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);

                                            //ResetControl();
                                            goto returnLoop;
                                        }
                                        else// if (statusLogData == 2)
                                        {
                                            Debug.WriteLine($"Thùng này đã được quét ghi nhận khối lượng OK rồi, không được phép cân lại." +
                                                $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                            this.Invoke((MethodInvoker)delegate { labErrInfoScale.Text = "Thùng này đã ghi nhận khối lượng OK rồi."; });
                                            //ghi giá trị xuống PLC cân reject
                                            GlobalVariables.MyEvent.WeightPusher = 1;

                                            //ResetControl();
                                            goto returnLoop;
                                        }
                                    }
                                    #endregion

                                    #region Log data
                                    //mỗi thùng chỉ cho log vào tối da là 2 dòng trong scanData, 1 dòng pass và fail (nếu có)
                                    //tính lại tỷ lệ khối lượng số đôi lỗi/ StdGrossWeight của lần scan này để log
                                    _scanDataWeight.RatioFailWeight = Math.Round((Math.Abs(_scanDataWeight.DeviationPairs) * _scanDataWeight.AveWeight1Prs) / _scanDataWeight.StdGrossWeight, 3);

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
                                    //para.Add("Id", ParameterDirection.Output, DbType.Guid);

                                    var insertResult = connection.Execute("sp_tblScanDataInsert", para, commandType: CommandType.StoredProcedure);

                                    //var id = para.Get<string>("Id");

                                    #endregion

                                    #region hiển thị thông tin
                                    this.Invoke((MethodInvoker)delegate
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


                                    ResetControl();

                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        labResult.Text = "Fail";
                                        labResult.BackColor = Color.Red;
                                        labErrInfoScale.Text = "Không có khối lượng đôi. Weight/Prs.";
                                    });
                                    //bật đèn đỏ
                                    GlobalVariables.MyEvent.StatusLightPLC = 1;
                                    //ghi giá trị xuống PLC cân reject
                                    GlobalVariables.MyEvent.WeightPusher = 1;

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

                                ResetControl();

                                this.Invoke((MethodInvoker)delegate
                                {
                                    labResult.Text = "Fail";
                                    labResult.BackColor = Color.Red;
                                    labErrInfoScale.Text = "ProductItem không có trong hệ thống.";
                                });

                                //bật đèn đỏ
                                GlobalVariables.MyEvent.StatusLightPLC = 1;
                                //ghi giá trị xuống PLC cân reject
                                GlobalVariables.MyEvent.WeightPusher = 1;

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
                        this.Invoke((MethodInvoker)delegate
                        {
                            labCalculatedPairs.Text = _scanDataWeight.CalculatedPairs.ToString();
                            labDeviationPairs.Text = _scanDataWeight.DeviationPairs.ToString();
                            labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString();
                            labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();
                            labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();
                            labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();
                            labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                            labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();
                            labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();
                            labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();
                            labDeviation.Text = _scanDataWeight.Deviation.ToString();
                            labNetRealWeight.Text = _scanDataWeight.NetWeight.ToString();
                            labLowerToleranceWeight.Text = nwSub.ToString("#.###");
                            labUpperToleranceWeight.Text = nwPlus.ToString("#.###");
                        });
                        #endregion

                        string json = JsonConvert.SerializeObject(GlobalVariables.RememberInfo);
                        File.WriteAllText(@"./RememberInfo.json", json);
                        //_readQrStatus[1] = false;//trả lại bit này để quét lần sau
                        break;
                    case 3://trạm phân loại hàng sơn.
                        this.Invoke((MethodInvoker)delegate { labQrPrint.Text = barcodeString; });

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
                                this.Invoke((MethodInvoker)delegate { labErrInfoPrint.Text = "OC không đúng định dạng."; });
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
                                this.Invoke((MethodInvoker)delegate { labErrInfoPrint.Text = "OC không đúng định dạng."; });
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
                                specialCase = true;

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
                                    this.Invoke((MethodInvoker)delegate { labErrInfoPrint.Text = "Hàng sơn."; });

                                    GlobalVariables.MyEvent.PrintPusher = 1;
                                }
                                else
                                {
                                    GlobalVariables.MyEvent.PrintPusher = 0;
                                    this.Invoke((MethodInvoker)delegate { labErrInfoPrint.Text = "Hàng FG."; });
                                }
                            }
                        }
                        _readQrStatus[2] = false;//trả lại bit này để quét lần sau
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, "Lỗi scale form");
            }
            finally
            {

            }
        }

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

                    //this.Invoke((MethodInvoker)delegate { txtDataAscii1.Text = xmlDoc.GetElementsByTagName("datalabel")[0].InnerText; });
                    _barcodeString1 = AsciiToString(xmlDoc.GetElementsByTagName("datalabel")[0].InnerText);

                    //reset model;
                    _scanDataMetal = null;
                    _scanDataMetal = new tblScanDataModel();

                    BarcodeHandle(1, _barcodeString1);
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
                    _scanDataWeight = null;
                    _scanDataWeight = new tblScanDataModel();

                    BarcodeHandle(2, _barcodeString2);
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

                    BarcodeHandle(3, _barcodeString3);
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

                //Printed event
                if (rcvArr[4] == 0x30)
                {
                    Console.WriteLine($"in thanh cong!!!");

                    //xoa string
                    SendDynamicString(" ", " ", " ");
                }
                else if (rcvArr[4] == 0x4F)
                {
                    Console.WriteLine($"Gui lenh xuong may in thanh cong!!!");
                }
                else if (rcvArr[4] == 0x31)
                {
                    Console.WriteLine($"Loi. Error Code: {rcvArr[5]}. Kết nối lại máy in.");
                    // MessageBox.Show($"Send command error: Error code: {rcvArr[5]}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    StopPrint();
                    Thread.Sleep(10000);
                    StartPrint();
                    Thread.Sleep(10000);
                }
                else if (rcvArr[4] == 0x5D)//93 get speed
                {
                    var speedPV = (double)(rcvArr[5] + rcvArr[6] * 0x100 + rcvArr[7] * 0x1000 + rcvArr[8] * 0x10000);
                    speedPV = Math.Round(speedPV / 1000, 2);
                }
                else if (rcvArr[4] == 0x64)//100 get delay
                {
                    var delayPV = (double)(rcvArr[5] + rcvArr[6] * 0x100 + rcvArr[7] * 0x1000 + rcvArr[8] * 0x10000);
                    delayPV = Math.Round(delayPV / 100, 2);
                }

                GlobalVariables.PrintResult = string.Empty;
                foreach (var item in rcvArr)
                {
                    GlobalVariables.PrintResult += item;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Printing data received error: {ex.Message}");
            }
            finally
            {
                System.Threading.Thread.Sleep(500);
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
                Log.Error(ex, $"Start print error: {ex.Message}");

                Thread.Sleep(10000);

                goto loop1;

                Thread.Sleep(10000);
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
                Log.Error(ex, $"Stop print error: {ex.Message}");
            }
            finally
            {
                System.Threading.Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Method truyen noi dung xuong may in.
        /// </summary>
        /// <param name="string1">Khối lượng cân thực tế.</param>
        /// <param name="string2">Thời điểm cân.</param>
        /// <param name="string3">'IdLabel' hoặc là 'OC|BoxNo'.</param>
        private void SendDynamicString(string string1, string string2, string string3)
        {
            int i = 0, j = 0, k = 0;
            int chkSUM = 0;

            byte[] SetDynamicString = new byte[14 + string1.Length + string2.Length + string3.Length];
            SetDynamicString[0] = 0x2;
            SetDynamicString[1] = 0x0;
            SetDynamicString[2] = (byte)(9 + string1.Length + string2.Length + string3.Length);
            SetDynamicString[3] = 0x0;
            SetDynamicString[4] = 0xCA; // Mã lệnh Set dynamic string
            SetDynamicString[5] = 0;
            SetDynamicString[6] = 0;
            SetDynamicString[7] = (byte)(string1.Length); // Chiều dài của string 1
            SetDynamicString[8] = (byte)(string2.Length); // Chiều dài của string 2
            SetDynamicString[9] = (byte)(string3.Length); // Chiều dài của string 3
            SetDynamicString[10] = 0; // Chiều dài của string 4
            SetDynamicString[11] = 0; // Chiều dài của string 5

            //chuyen string sang ASCII
            var string1Arr = string1.ToCharArray();
            var string2Arr = string2.ToCharArray();
            var string3Arr = string3.ToCharArray();

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

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            BarcodeHandle(2, "C100028,6817012205-2397-D243,1,2,P,2/2,1900068,1/1|2,22421.2023,,,");
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
                Log.Error(ex, $"Printing set speed error: {ex.Message}");
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
                Log.Error(ex, $"Printing get speed error: {ex.Message}");
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
                Log.Error(ex, $"Printing get delay error: {ex.Message}");
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
                Log.Error(ex, $"Printing set delay error: {ex.Message}");
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
            double timeCheck = 0;
            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now;

            while (timeCheck <= timeCheckSettings)
            {
                timeCheck = (endTime - startTime).TotalSeconds;
                endTime = DateTime.Now;
                Debug.WriteLine($"Dem thoi gian bao Metal Scanner fail: {timeCheck}");
                Thread.Sleep(100);
            }

            //hết thời gian mà vẫn chưa có tín hiệu từ scanner metal thì ghi tín hiệu xuống PLC conveyor báo reject
            if (!_readQrStatus[0])
            {
                Debug.WriteLine($"Ghi tin hieu bao reject do ko doc dc QR code tram metal");
                this.Invoke((MethodInvoker)delegate
                {
                    labErrInfoMetal.Text = "Không đọc được QR code, Kiểm tra lại tem.";
                    labQrMetal.Text = string.Empty;
                });
                //hết thời gian đọc QR code mà chưa đọc được
                //gui data xuong PLC báo reject metalPusher
                _metalScannerStatus = 1;

                _scanDataMetal = null;
                _scanDataMetal = new tblScanDataModel();

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
                    para.Add("_scannerStation", "Metal");
                    para.Add("_reason", "Không đọc được QR code.");

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
                Debug.WriteLine($"Dem thoi gian bao weight Scanner fail: {timeCheck}");
                Thread.Sleep(100);
            }

            //hết thời gian mà vẫn chưa có tín hiệu từ scanner metal thì ghi tín hiệu xuống PLC conveyor báo reject
            if (!_readQrStatus[1])
            {
                Debug.WriteLine($"Ghi tin hieu bao reject do ko doc dc QR code tram scale");
                this.Invoke((MethodInvoker)delegate
                {
                    labErrInfoScale.Text = "Không đọc được QR code, Kiểm tra lại tem.";
                    labQrScale.Text = string.Empty;
                    labResult.Text = "Fail";
                    labResult.BackColor = Color.Red;
                    labResult.ForeColor = Color.White;
                });
                //hết thời gian đọc QR code mà chưa đọc được
                //gui data xuong PLC báo reject metalPusher
                GlobalVariables.MyEvent.WeightPusher = 1;
                //bật đèn đỏ
                GlobalVariables.MyEvent.StatusLightPLC = 1;

                _scanDataWeight = null;
                _scanDataWeight = new tblScanDataModel();

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
                    para.Add("_reason", "Không đọc được QR code.");

                    connection.Execute("sp_tblScanDataRejectInsert", para, commandType: CommandType.StoredProcedure);
                }
            }
            _readQrStatus[1] = false;//xóa biến này cho lần đọc kế tiếp
        }
        #endregion
    }
}