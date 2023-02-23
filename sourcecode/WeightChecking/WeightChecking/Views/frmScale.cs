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
        private Task _ckTask, _ckQRTask;

        private int _stableScale = 0;
        private double _scaleValue = 0;

        private tblScanDataModel _scanData = new tblScanDataModel();

        private string _idLabel = null;
        private string _plr = null;// kiểu đóng thùng, P-đôi; L/R-left right

        private double _weight = 0, _boxWeight = 0, _accessoriesWeight = 0;

        private bool _approveUpdateActMetalScan = false;

        // Declare CoreScannerClass
        private CCoreScanner _cCoreScannerClass;
        private string _barcodeString1 = null, _barcodeString2 = null, _barcodeString3 = null;//checkMetal--checkWeight--printing

        private SerialPort _serialPort;

        public frmScale()
        {
            InitializeComponent();

            Load += FrmScale_Load;
        }

        private void FrmScale_Load(object sender, EventArgs e)
        {
            #region Register events Scale value change
            //if (GlobalVariables.IsScale)
            //{
            //    _scaleHelper = new ScaleHelper()
            //    {
            //        Ip = GlobalVariables.IpScale,
            //        Port = Convert.ToInt32(GlobalVariables.PortScale),
            //        ScaleDelay = GlobalVariables.ScaleDelay,
            //        StopScale = false
            //    };

            //    _scaleHelper.StatusChanged += (s, o) =>
            //    {
            //        GlobalVariables.ScaleStatus = o.StatusConnection;
            //        Console.WriteLine($"Scale {o}");
            //    };

            //    _ckTask = new Task(() => _scaleHelper.CheckConnect());
            //    _ckTask.Start();

            //    _scaleHelper.ValueChanged += (s, o) =>
            //    {
            //        try
            //        {
            //            var w = o.Value * GlobalVariables.UnitScale;
            //            GlobalVariables.RealWeight = w;
            //            if (w.ToString().Length >= 4 || w == 0)
            //            {
            //                if (labRealWeight.InvokeRequired)
            //                {
            //                    labRealWeight.Invoke(new Action(() =>
            //                    {
            //                        labScaleValue.Text = w.ToString();
            //                    }));
            //                }
            //                else
            //                {
            //                    labScaleValue.Text = w.ToString();
            //                }
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            Log.Error(ex, "Scale event error.");
            //        }
            //    };
            //    _scaleHelper.ScaleValue = 1;// 5.545;//tac động để đọc cân lần đầu tiên
            //}
            #endregion

            #region hien thi cac thong so dem
            if (labGoodBox.InvokeRequired)
            {
                labGoodBox.Invoke(new Action(() => { labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString(); }));
            }
            else labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString();

            if (labGoodNoPrint.InvokeRequired)
            {
                labGoodNoPrint.Invoke(new Action(() =>
                {
                    labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();
                }));
            }
            else labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();

            if (labGoodPrint.InvokeRequired)
            {
                labGoodPrint.Invoke(new Action(() =>
                {
                    labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();
                }));
            }
            else labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();

            if (labFailBox.InvokeRequired)
            {
                labFailBox.Invoke(new Action(() =>
                {
                    labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting
                    + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();
                }));
            }
            else labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();

            if (labFailNoPrint.InvokeRequired)
            {
                labFailNoPrint.Invoke(new Action(() =>
                {
                    labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                }));
            }
            else labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
            if (labFailPrint.InvokeRequired)
            {
                labFailPrint.Invoke(new Action(() =>
                {
                    labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();
                }));
            }
            else labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();

            if (labMetalScanBox.InvokeRequired)
            {
                labMetalScanBox.Invoke(new Action(() =>
                {
                    labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();
                }));
            }
            else labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();

            if (labMetalScanCount.InvokeRequired)
            {
                labMetalScanCount.Invoke(new Action(() =>
                {
                    labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();
                }));
            }
            else labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();

            #endregion

            #region đăng ký sự kiện từ cac PLC
            GlobalVariables.MyEvent.EventHandlerCount += (s, o) =>
            {
                #region check Actual metal scan
                if (GlobalVariables.Station == StationEnum.IDC_1 && _approveUpdateActMetalScan
                && o.CountValue != GlobalVariables.RememberInfo.CountMetalScan && o.CountValue != 0)
                {
                    _scanData.ActualMetalScan = 1;

                    #region update actualMetalScan vào thùng vừa được cân
                    var para = new DynamicParameters();
                    para.Add("QrCode", _scanData.BarcodeString);
                    para.Add("ActualMetalScan", _scanData.ActualMetalScan);

                    using (var con = GlobalVariables.GetDbConnection())
                    {
                        con.Execute("sp_tblScanDataUpdateActualMetalScan", para, commandType: CommandType.StoredProcedure);
                    }
                    #endregion

                    _approveUpdateActMetalScan = false;
                }
                _scanData.ActualMetalScan = 0;
                #endregion

                GlobalVariables.RememberInfo.CountMetalScan = o.CountValue;

                if (labMetalScanCount.InvokeRequired)
                {
                    labMetalScanCount.Invoke(new Action(() =>
                    {
                        labMetalScanCount.Text = o.CountValue.ToString();
                    }));
                }
                else
                {
                    labMetalScanCount.Text = o.CountValue.ToString();
                }
            };

            GlobalVariables.MyEvent.EventHandlerRefreshMasterData += (s, o) =>
            {
                #region hien thi cac thong so dem
                if (labGoodBox.InvokeRequired)
                {
                    labGoodBox.Invoke(new Action(() => { labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString(); }));
                }
                else labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString();

                if (labGoodNoPrint.InvokeRequired)
                {
                    labGoodNoPrint.Invoke(new Action(() =>
                    {
                        labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();
                    }));
                }
                else labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();

                if (labGoodPrint.InvokeRequired)
                {
                    labGoodPrint.Invoke(new Action(() =>
                    {
                        labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();
                    }));
                }
                else labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();

                if (labFailBox.InvokeRequired)
                {
                    labFailBox.Invoke(new Action(() =>
                    {
                        labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting
                        + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();
                    }));
                }
                else labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();

                if (labFailNoPrint.InvokeRequired)
                {
                    labFailNoPrint.Invoke(new Action(() =>
                    {
                        labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                    }));
                }
                else labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                if (labFailPrint.InvokeRequired)
                {
                    labFailPrint.Invoke(new Action(() =>
                    {
                        labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();
                    }));
                }
                else labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();

                if (labMetalScanBox.InvokeRequired)
                {
                    labMetalScanBox.Invoke(new Action(() =>
                    {
                        labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();
                    }));
                }
                else labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();

                if (labMetalScanCount.InvokeRequired)
                {
                    labMetalScanCount.Invoke(new Action(() =>
                    {
                        labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();
                    }));
                }
                else labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();

                #endregion
            };

            //sự kiện báo cân đã ổn định, chốt số cân.
            GlobalVariables.MyEvent.EventHandlerStableScale += (s, o) =>
            {
                _stableScale = o.NewValue;

                _scanData.GrossWeight = _scaleValue;
            };

            //sự kiến lấy khối lượng cân
            GlobalVariables.MyEvent.EventHandlerScale += (s, o) =>
            {
                _scaleValue = o.ScaleValue;
            };
            #endregion

            //khởi tạo scanner
            InitializeScaner();

            //Khởi tạo máy in AnserU2 Smart one
            SerialPortOpen();

            //this.txtQrCode.Focus();
            //this.txtQrCode.KeyDown += TxtQrCode_KeyDown;
        }

        private void TxtQrCode_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                _scanData.GrossWeight = double.TryParse(labScaleValue.Text, out double value) ? value : 0;
                GlobalVariables.RealWeight = _scanData.GrossWeight;
                _scanData.CreatedBy = GlobalVariables.UserLoginInfo.Id;
                _scanData.Station = GlobalVariables.Station;

                bool specialCase = false;//dùng có các trường hợp hàng PU, trên WL decpration là 0, nhưng QC phân ra printing 0-1. beforePrinting thì get theo
                                         //printing=0; afterPrinting thì get theo printing=1. 6112012228

                //biến dùng để check xem thùng đó có trong bảng scanData hay chưa.
                int statusLogData = 0;//0-chưa có;1-đã có dòng fail;2-đã có dòng pass;3-đã có cả fail và pass
                bool isFail = false;
                bool isPass = false;

                if (e.KeyCode == Keys.Enter)
                {
                    TextEdit _sen = sender as TextEdit;
                    Console.WriteLine(_sen.Text);

                    #region xử lý barcode lấy ra các giá trị theo code
                    _scanData.BarcodeString = _sen.Text;

                    if (_scanData.BarcodeString.Contains("|"))
                    {
                        var s = _sen.Text.Split('|');
                        var s1 = s[0].Split(',');
                        _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                        var ocFirstChar = s1[0].Substring(0, 2);
                        if (ocFirstChar == "OS" || ocFirstChar == "CS" || ocFirstChar == "OC" || ocFirstChar == "RE" || ocFirstChar == "LA" ||
                            ocFirstChar == "CL" || ocFirstChar == "PB" || ocFirstChar == "OL" || ocFirstChar == "SZ" || ocFirstChar == "OP"
                            || ocFirstChar == "PR"
                            )
                        {
                            _scanData.OcNo = s1[0];
                        }
                        else
                        {
                            MessageBox.Show("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            #region reset txtQrcode để quét mã tiếp
                            if (_sen.InvokeRequired)
                            {
                                _sen?.Invoke(new Action(() =>
                                {
                                    _sen.Text = null;
                                }));
                            }
                            else
                            {
                                _sen.Text = null;
                            }
                            _sen.Focus();
                            #endregion

                            return;
                        }

                        _scanData.ProductNumber = s1[1];

                        _scanData.Quantity = Convert.ToInt32(s1[2]);
                        _scanData.LinePosNo = s1[3];
                        _scanData.BoxNo = s1[5];
                        _scanData.CustomerNo = s1[6];
                        _scanData.BoxPosNo = s1[7];

                        if (s[1].Contains(","))
                        {
                            var s2 = s[1].Split(',');

                            GlobalVariables.IdLabel = s2[1];
                            _scanData.IdLabel = GlobalVariables.IdLabel;

                            if (s2[0] == "1")
                            {
                                _scanData.Location = LocationEnum.fVN;
                            }
                            else if (s2[0] == "2")
                            {
                                _scanData.Location = LocationEnum.fFT;
                            }
                            else if (s2[0] == "3")
                            {
                                _scanData.Location = LocationEnum.fKV;
                            }
                        }
                        else
                        {
                            if (s[1] == "1")
                            {
                                _scanData.Location = LocationEnum.fVN;
                            }
                            else if (s[1] == "2")
                            {
                                _scanData.Location = LocationEnum.fFT;
                            }
                            else if (s[1] == "3")
                            {
                                _scanData.Location = LocationEnum.fKV;
                            }
                        }
                    }
                    else
                    {
                        var s1 = _scanData.BarcodeString.Split(',');
                        _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                        var ocFirstChar = s1[0].Substring(0, 2);
                        if (ocFirstChar == "OS" || ocFirstChar == "CS" || ocFirstChar == "OC" || ocFirstChar == "RE" || ocFirstChar == "LA" ||
                            ocFirstChar == "CL" || ocFirstChar == "PB" || ocFirstChar == "OL" || ocFirstChar == "SZ" || ocFirstChar == "OP"
                            || ocFirstChar == "PR"
                            )
                        {
                            _scanData.OcNo = s1[0];
                        }
                        else
                        {
                            MessageBox.Show("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);

                            #region reset txtQrcode để quét mã tiếp
                            if (_sen.InvokeRequired)
                            {
                                _sen?.Invoke(new Action(() =>
                                {
                                    _sen.Text = null;
                                }));
                            }
                            else
                            {
                                _sen.Text = null;
                            }
                            _sen.Focus();
                            #endregion

                            return;
                        }

                        //_scanData.OcNo = s1[0];
                        _scanData.ProductNumber = s1[1];

                        _scanData.Quantity = Convert.ToInt32(s1[2]);
                        _scanData.LinePosNo = s1[3];
                        _scanData.BoxNo = s1[5];
                    }

                    //Special case
                    if (_scanData.ProductNumber.Contains("6112012228-"))
                    {
                        specialCase = true;
                    }

                    GlobalVariables.OcNo = _scanData.OcNo;
                    GlobalVariables.BoxNo = _scanData.BoxNo;
                    #endregion

                    #region truy vấn data và xử lý
                    //truy vấn thông tin 
                    using (var connection = GlobalVariables.GetDbConnection())
                    {
                        var para = new DynamicParameters();

                        #region Kiểm tra xem thùng này đã được log vào scanData chưa
                        para.Add("QRLabel", _scanData.BarcodeString);

                        var checkInfo = connection.Query<tblScanDataCheckModel>("sp_tblScanDataCheck", para, commandType: CommandType.StoredProcedure).ToList();
                        var countRow = checkInfo.Count;
                        foreach (var item in checkInfo)
                        {

                            if (item.Pass == 1 || (item.Pass == 0 && item.ActualDeviationPairs == 0 && item.ApprovedBy != Guid.Empty))
                            {
                                if (!_scanData.OcNo.Contains("PR"))
                                {
                                    isPass = true;
                                }
                                else if (_scanData.OcNo.Contains("PR") && GlobalVariables.AfterPrinting == 0 && item.Status == 1)
                                {
                                    isPass = true;
                                }
                                else if (_scanData.OcNo.Contains("PR") && GlobalVariables.AfterPrinting == 1 && item.Status == 2)
                                {
                                    isPass = true;
                                }
                            }
                            else if (item.Pass == 0)// && item.ActualDeviationPairs != 0 && item.ApprovedBy != Guid.Empty)
                            {
                                if (!_scanData.OcNo.Contains("PR"))
                                {
                                    isFail = true;
                                }
                                else if (_scanData.OcNo.Contains("PR") && GlobalVariables.AfterPrinting == 0 && item.Station == 0)
                                {
                                    isFail = true;
                                }
                                else if (_scanData.OcNo.Contains("PR") && GlobalVariables.AfterPrinting == 1 && item.Station != 0)
                                {
                                    isFail = true;
                                }
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
                        para.Add("@ProductNumber", _scanData.ProductNumber);
                        para.Add("@SpecialCase", specialCase);

                        if (specialCase)
                        {
                            //after printing
                            if (
                                _scanData.OcNo.Contains("OS")
                                || _scanData.OcNo.Contains("CS")
                                || _scanData.OcNo.Contains("OC")
                                || _scanData.OcNo.Contains("RE")
                                || _scanData.OcNo.Contains("LA")
                                || _scanData.OcNo.Contains("CL")
                                || _scanData.OcNo.Contains("PBF")
                                || _scanData.OcNo.Contains("OL")
                                || _scanData.OcNo.Contains("SZ")
                                || _scanData.OcNo.Contains("OP")
                                || (_scanData.OcNo.Contains("PR") && GlobalVariables.AfterPrinting != 0)
                                )
                            {
                                para.Add("@Printing", 1);//0 or 1, tùy theo hàng trước sơn hay sau sơn
                            }
                            //before printing
                            else if (_scanData.OcNo.Contains("PRT") && GlobalVariables.AfterPrinting == 0)
                            {
                                para.Add("@Printing", 0);//0 or 1, tùy theo hàng trước sơn hay sau sơn
                            }
                        }

                        var res = connection.Query<ProductInfoModel>("sp_vProductItemInfoGet", para, commandType: CommandType.StoredProcedure).FirstOrDefault();

                        if (res != null)
                        {
                            _scanData.ProductName = res.ProductName;
                            _scanData.Decoration = res.Decoration;
                            _scanData.MetalScan = res.MetalScan;
                            _scanData.Brand = res.Brand;
                            _scanData.AveWeight1Prs = res.AveWeight1Prs;

                            if (_scanData.AveWeight1Prs != 0)
                            {
                                #region Fill data from coreData to scanData, tính toán ra NetWeight và GrossWeight
                                //Xét điều kiện để lấy boxWeight. Nếu là hàng đi sơn thì dùng thùng nhựa
                                if (_scanData.Decoration == 0
                                    || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("OS"))
                                    || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("CS"))
                                    || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("OC"))
                                    || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("RE"))
                                    || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("LA"))
                                    || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("CL"))
                                    || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("PBF"))
                                    || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("OL"))
                                    || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("SZ"))
                                    || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("OP"))
                                    )
                                {
                                    _scanData.Status = 2;//báo trạng thái hàng ko đi sơn, hoặc hàng sơn đã được sơn rồi

                                    if (_scanData.Quantity <= res.BoxQtyBx4)
                                    {
                                        _scanData.BoxWeight = res.BoxWeightBx4;

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
                                    else if (_scanData.Quantity > res.BoxQtyBx4 && _scanData.Quantity <= res.BoxQtyBx3)
                                    {
                                        _scanData.BoxWeight = res.BoxWeightBx3;

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
                                    else if (_scanData.Quantity > res.BoxQtyBx3 && _scanData.Quantity <= res.BoxQtyBx2)
                                    {
                                        _scanData.BoxWeight = res.BoxWeightBx2;

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
                                    else if (_scanData.Quantity > res.BoxQtyBx2 && _scanData.Quantity <= res.BoxQtyBx1)
                                    {
                                        _scanData.BoxWeight = res.BoxWeightBx1;

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
                                    else if (_scanData.Quantity > res.BoxQtyBx1)
                                    {
                                        MessageBox.Show($"Số lượng vượt quá giới hạn thùng BX1 ({res.BoxQtyBx1})", "CẢNH BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                        para = null;
                                        para = new DynamicParameters();
                                        para.Add("ProductNumber", _scanData.ProductNumber);
                                        para.Add("ProductName", _scanData.ProductName);
                                        para.Add("OcNum", _scanData.OcNo);
                                        para.Add("Note", $"Số lượng vượt quá giới hạn thùng BX1 ({res.BoxQtyBx1})");
                                        para.Add("QrCode", _scanData.BarcodeString);

                                        connection.Execute("sp_tblItemMissingInfoInsert", para, commandType: CommandType.StoredProcedure);

                                        ResetControl();
                                        goto returnLoop;
                                    }

                                    if (_scanData.Decoration == 0)
                                    {
                                        if (labDecoration.InvokeRequired)
                                        {
                                            labDecoration.Invoke(new Action(() =>
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
                                        if (labDecoration.InvokeRequired)
                                        {
                                            labDecoration.Invoke(new Action(() =>
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
                                else if (_scanData.Decoration == 1 && _scanData.OcNo.Contains("PR"))//hàng trước sơn. chỉ có trạm SSFG01 mới nhảy vào đây
                                {
                                    if (GlobalVariables.AfterPrinting == 0)
                                    {
                                        _scanData.Status = 1;// báo trạng thái hàng sơn cần đưa đi sơn, trạm SSFG01
                                    }
                                    else
                                    {
                                        _scanData.Status = 2;// báo trạng thái hàng sơn đã được sơn, trạm SSFG02 và SSFG03(Kerry)
                                    }

                                    _scanData.BoxWeight = res.PlasicBoxWeight;

                                    if (labDecoration.InvokeRequired)
                                    {
                                        labDecoration.Invoke(new Action(() =>
                                        {
                                            labDecoration.BackColor = Color.Gold;
                                        }));
                                    }
                                    else
                                    {
                                        labDecoration.BackColor = Color.Gold;
                                    }
                                }

                                if (_scanData.MetalScan == 0)
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

                                _scanData.StdNetWeight = Math.Round(_scanData.Quantity * _scanData.AveWeight1Prs, 3);
                                _scanData.Tolerance = Math.Round(_scanData.StdNetWeight * (res.Tolerance / 100), 3);

                                //luu ý các Quantity partition-Plasic-WrapSheet trên DB nó là tính số Prs
                                //sau khi đọc về phải lấy QtyPrs quét trên label / Quantity partition-Plasic-WrapSheet ==> qty * weight ==> Weight package weight
                                double partitionWeight = 0;
                                var p = res.PartitionQty != 0 ? ((double)_scanData.Quantity / (double)res.PartitionQty) : 0;
                                if (_scanData.Quantity <= res.BoxQtyBx3 || p < 1)
                                {
                                    partitionWeight = 0;
                                }
                                else if (p >= 1)
                                {
                                    partitionWeight = Math.Floor(p) * res.PartitionWeight;
                                }
                                //partitionWeight = res.PartitionQty != 0 ? (_scanData.Quantity / res.PartitionQty) * res.PartitionWeight : 0;
                                var plasicBag1Weight = res.PlasicBag1Qty != 0 ? Math.Ceiling(((double)_scanData.Quantity / (double)res.PlasicBag1Qty)) * res.PlasicBag1Weight : 0;
                                var plasicBag2Weight = res.PlasicBag2Qty != 0 ? Math.Ceiling(((double)_scanData.Quantity / (double)res.PlasicBag2Qty)) * res.PlasicBag2Weight : 0;
                                var wrapSheetWeight = res.WrapSheetQty != 0 ? Math.Ceiling(((double)_scanData.Quantity / (double)res.WrapSheetQty)) * res.WrapSheetWeight : 0;
                                var foamSheetWeight = res.FoamSheetQty != 0 ? Math.Ceiling(((double)_scanData.Quantity / (double)res.FoamSheetQty)) * res.FoamSheetWeight : 0;

                                _scanData.PackageWeight = Math.Round(partitionWeight + plasicBag1Weight + plasicBag2Weight + wrapSheetWeight + foamSheetWeight, 3);

                                _scanData.StdGrossWeight = Math.Round(_scanData.StdNetWeight + _scanData.PackageWeight + _scanData.BoxWeight, 3);

                                #region tinh toán standardWeight theo Pair/Left/Right. lưu ý để sau này có áp dụng thì làm
                                //if (_plr == "P")
                                //{
                                //    _scanData.GrossdWeight = res.Weight * res.QtyPerbag + res.BagWeight;
                                //}
                                //else if (_plr == "L")
                                //{
                                //    if (res.LeftWeight == 0)
                                //    {
                                //        _scanData.StandardWeight = res.Weight * res.QtyPerbag + res.BagWeight;
                                //    }
                                //    else
                                //    {
                                //        _scanData.StandardWeight = res.LeftWeight * res.QtyPerbag + res.BagWeight;
                                //    }
                                //}
                                //else if (_plr == "R")
                                //{
                                //    if (res.RightWeight == 0)
                                //    {
                                //        _scanData.StandardWeight = res.Weight * res.QtyPerbag + res.BagWeight;
                                //    }
                                //    else
                                //    {
                                //        _scanData.StandardWeight = res.RightWeight * res.QtyPerbag + res.BagWeight;
                                //    }
                                //}
                                #endregion

                                #endregion

                                #region hiển thị thông tin
                                if (labRealWeight.InvokeRequired)
                                {
                                    labRealWeight.Invoke(new Action(() =>
                                    {
                                        labRealWeight.Text = _scanData.GrossWeight.ToString();
                                    }));
                                }
                                else labRealWeight.Text = _scanData.GrossWeight.ToString();

                                if (labNetWeight.InvokeRequired)
                                {
                                    labNetWeight.Invoke(new Action(() =>
                                    {
                                        labNetWeight.Text = _scanData.StdNetWeight.ToString();
                                    }));
                                }
                                else labNetWeight.Text = _scanData.StdNetWeight.ToString();

                                if (labOcNo.InvokeRequired)
                                {
                                    labOcNo.Invoke(new Action(() => { labOcNo.Text = _scanData.OcNo; }));
                                }
                                else labOcNo.Text = _scanData.OcNo;

                                if (labProductCode.InvokeRequired)
                                {
                                    labProductCode.Invoke(new Action(() => { labProductCode.Text = _scanData.ProductNumber; }));
                                }
                                else labProductCode.Text = _scanData.ProductNumber;

                                if (labProductName.InvokeRequired)
                                {
                                    labProductName.Invoke(new Action(() => { labProductName.Text = _scanData.ProductName; }));
                                }
                                else labProductName.Text = _scanData.ProductName;

                                if (labQuantity.InvokeRequired)
                                {
                                    labQuantity.Invoke(new Action(() => { labQuantity.Text = _scanData.Quantity.ToString(); }));
                                }
                                else labQuantity.Text = _scanData.Quantity.ToString();

                                if (labColor.InvokeRequired)
                                {
                                    labColor.Invoke(new Action(() => { labColor.Text = res.Color; }));
                                }
                                else labColor.Text = res.Color;

                                if (labSize.InvokeRequired)
                                {
                                    labSize.Invoke(new Action(() => { labSize.Text = res.SizeName; }));
                                }
                                else labSize.Text = res.SizeName;

                                if (labAveWeight.InvokeRequired)
                                {
                                    labAveWeight.Invoke(new Action(() => { labAveWeight.Text = _scanData.AveWeight1Prs.ToString(); }));
                                }
                                else labAveWeight.Text = _scanData.AveWeight1Prs.ToString();

                                if (labToloren.InvokeRequired)
                                {
                                    labToloren.Invoke(new Action(() => { labToloren.Text = _scanData.Tolerance.ToString(); }));
                                }
                                else labToloren.Text = _scanData.Tolerance.ToString();

                                if (labBoxWeight.InvokeRequired)
                                {
                                    labBoxWeight.Invoke(new Action(() => { labBoxWeight.Text = _scanData.BoxWeight.ToString(); }));
                                }
                                else labBoxWeight.Text = _scanData.BoxWeight.ToString();

                                if (labAccessoriesWeight.InvokeRequired)
                                {
                                    labAccessoriesWeight.Invoke(new Action(() => { labAccessoriesWeight.Text = _scanData.PackageWeight.ToString(); }));
                                }
                                else labAccessoriesWeight.Text = _scanData.PackageWeight.ToString();

                                if (labGrossWeight.InvokeRequired)
                                {
                                    labGrossWeight.Invoke(new Action(() => { labGrossWeight.Text = _scanData.StdGrossWeight.ToString(); }));
                                }
                                else labGrossWeight.Text = _scanData.StdGrossWeight.ToString();
                                #endregion

                                #region xử lý so sánh khối lượng cân thực tế với kế hoạch để xử lý
                                _scanData.NetWeight = Math.Round(_scanData.GrossWeight - _scanData.BoxWeight - _scanData.PackageWeight, 3);
                                _scanData.Deviation = Math.Round(_scanData.NetWeight - _scanData.StdNetWeight, 3);

                                #region tính toán số pairs chênh lệch và hiển thị label
                                var nwPlus = _scanData.StdNetWeight + _scanData.Tolerance;
                                var nwSub = _scanData.StdNetWeight - _scanData.Tolerance;

                                if (((_scanData.NetWeight > nwPlus) && (_scanData.NetWeight - nwPlus < _scanData.AveWeight1Prs / 2))
                                || ((_scanData.NetWeight < nwSub) && (nwSub - _scanData.NetWeight < _scanData.AveWeight1Prs / 2))
                                )
                                {
                                    _scanData.CalculatedPairs = _scanData.Quantity;
                                }
                                else if (_scanData.NetWeight > nwPlus)//roundDown
                                {
                                    _scanData.CalculatedPairs = (int)(_scanData.Quantity + Math.Floor((_scanData.NetWeight - nwPlus) / _scanData.AveWeight1Prs));
                                }
                                else if (_scanData.NetWeight < nwSub)//RoundUp
                                {
                                    _scanData.CalculatedPairs = (int)(_scanData.Quantity - Math.Ceiling((nwSub - _scanData.NetWeight) / _scanData.AveWeight1Prs));
                                }
                                else
                                {
                                    _scanData.CalculatedPairs = _scanData.Quantity;
                                }

                                _scanData.DeviationPairs = _scanData.CalculatedPairs - _scanData.Quantity;

                                if (labCalculatedPairs.InvokeRequired)
                                {
                                    labCalculatedPairs.Invoke(new Action(() =>
                                    {
                                        labCalculatedPairs.Text = _scanData.CalculatedPairs.ToString();
                                    }));
                                }
                                else
                                {
                                    labCalculatedPairs.Text = _scanData.CalculatedPairs.ToString();
                                }

                                if (labDeviationPairs.InvokeRequired)
                                {
                                    labDeviationPairs.Invoke(new Action(() =>
                                    {
                                        labDeviationPairs.Text = _scanData.DeviationPairs.ToString();
                                    }));
                                }
                                else
                                {
                                    labDeviationPairs.Text = _scanData.DeviationPairs.ToString();
                                }
                                #endregion

                                //thung hang Pass
                                if (_scanData.DeviationPairs == 0)
                                {
                                    if (_scanData.Decoration == 0)
                                    {
                                        GlobalVariables.RememberInfo.GoodBoxPrinting += 1;
                                        //_scanData.Status = 1;
                                    }
                                    else
                                    {
                                        GlobalVariables.RememberInfo.GoodBoxNoPrinting += 1;
                                        //_scanData.Status = 2;
                                    }

                                    #region hien thi mau label
                                    if (labResult.InvokeRequired)
                                    {
                                        labResult.Invoke(new Action(() =>
                                        {
                                            labResult.Text = "Pass";
                                            labResult.BackColor = Color.Green;
                                            labResult.ForeColor = Color.White;
                                        }));
                                    }
                                    else
                                    {
                                        labResult.Text = "Pass";
                                        labResult.BackColor = Color.Green;
                                        labResult.ForeColor = Color.White;
                                    }
                                    #endregion

                                    //kiểm tra xem data đã có trên hệ thống hay chưa
                                    if (statusLogData == 0 || statusLogData == 1)
                                    {
                                        _scanData.Pass = 1;
                                        _scanData.CreatedDate = GlobalVariables.CreatedDate = DateTime.Now;//lấy thời gian để đồng bộ giữa in tem và log DB
                                                                                                           //Printing
                                        GlobalVariables.Printing((_scanData.GrossWeight / 1000).ToString("#,#0.00")
                                                    , !string.IsNullOrEmpty(GlobalVariables.IdLabel) ? GlobalVariables.IdLabel : $"{_scanData.OcNo}|{_scanData.BoxNo}", true
                                                     , _scanData.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
                                    }
                                    else
                                    {
                                        MessageBox.Show($"Thùng này đã được quét ghi nhận khối lượng OK rồi, không được phép cân lại." +
                                            $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information); ;

                                        ResetControl();
                                        goto returnLoop;
                                    }
                                    //GlobalVariables.RealWeight = _scanData.GrossWeight;
                                    //GlobalVariables.PrintApprove = true;
                                }
                                else//thung fail
                                {
                                    _scanData.Pass = 0;
                                    _scanData.Status = 0;

                                    GlobalVariables.PrintApprove = false;
                                    if (_scanData.Decoration == 1)
                                    {
                                        GlobalVariables.RememberInfo.FailBoxPrinting += 1;
                                    }
                                    else
                                    {
                                        GlobalVariables.RememberInfo.FailBoxNoPrinting += 1;
                                    }

                                    #region hien thi mau label
                                    if (labResult.InvokeRequired)
                                    {
                                        labResult.Invoke(new Action(() =>
                                        {
                                            labResult.Text = "Fail";
                                            labResult.BackColor = Color.Red;
                                            labResult.ForeColor = Color.White;
                                        }));
                                    }
                                    else
                                    {
                                        labResult.Text = "Fail";
                                        labResult.BackColor = Color.Red;
                                        labResult.ForeColor = Color.White;
                                    }
                                    #endregion

                                    if (statusLogData == 0)
                                    {
                                        _scanData.CreatedDate = GlobalVariables.CreatedDate = DateTime.Now;//lấy thời gian để đồng bộ giữa in tem và log DB

                                        GlobalVariables.Printing(_scanData.DeviationPairs.ToString()
                                                    , !string.IsNullOrEmpty(GlobalVariables.IdLabel) ? GlobalVariables.IdLabel : $"{_scanData.OcNo}|{_scanData.BoxNo}", false
                                                    , _scanData.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
                                    }
                                    else
                                    {
                                        MessageBox.Show($"Thùng này đã được quét ghi nhận khối lượng lỗi rồi, không được phép cân lại." +
                                            $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information); ;

                                        ResetControl();
                                        goto returnLoop;
                                    }
                                }
                                #endregion

                                #region Log data
                                //mỗi thùng chỉ cho log vào tối da là 2 dòng trong scanData, 1 dòng pass và fail (nếu có)

                                #region Log scanData
                                para = null;
                                para = new DynamicParameters();
                                para.Add("@BarcodeString", _scanData.BarcodeString);
                                para.Add("@IdLable", _scanData.IdLabel);
                                para.Add("@OcNo", _scanData.OcNo);
                                para.Add("@ProductNumber", _scanData.ProductNumber);
                                para.Add("@ProductName", _scanData.ProductName);
                                para.Add("@Quantity", _scanData.Quantity);
                                para.Add("@LinePosNo", _scanData.LinePosNo);
                                para.Add("@Unit", _scanData.Unit);
                                para.Add("@BoxNo", _scanData.BoxNo);
                                para.Add("@CustomerNo", _scanData.CustomerNo);
                                para.Add("@Location", _scanData.Location);
                                para.Add("@BoxPosNo", _scanData.BoxPosNo);
                                para.Add("@Note", _scanData.Note);
                                para.Add("@Brand", _scanData.Brand);
                                para.Add("@Decoration", _scanData.Decoration);
                                para.Add("@MetalScan", _scanData.MetalScan);
                                para.Add("@ActualMetalScan", _scanData.ActualMetalScan);
                                para.Add("@AveWeight1Prs", _scanData.AveWeight1Prs);
                                para.Add("@StdNetWeight", _scanData.StdNetWeight);
                                para.Add("@Tolerance", _scanData.Tolerance);
                                para.Add("@Boxweight", _scanData.BoxWeight);
                                para.Add("@PackageWeight", _scanData.PackageWeight);
                                para.Add("@StdGrossWeight", _scanData.StdGrossWeight);
                                para.Add("@GrossWeight", _scanData.GrossWeight);
                                para.Add("@NetWeight", _scanData.NetWeight);
                                para.Add("@Deviation", _scanData.Deviation);
                                para.Add("@Pass", _scanData.Pass);
                                para.Add("Status", _scanData.Status);
                                para.Add("CalculatedPairs", _scanData.CalculatedPairs);
                                para.Add("DeviationPairs", _scanData.DeviationPairs);
                                para.Add("CreatedBy", _scanData.CreatedBy);
                                para.Add("Station", _scanData.Station);
                                para.Add("CreatedDate", _scanData.CreatedDate);
                                para.Add("ApprovedBy", _scanData.ApprovedBy);
                                para.Add("ActualDeviationPairs", _scanData.ActualDeviationPairs);
                                //para.Add("Id", ParameterDirection.Output, DbType.Guid);

                                var insertResult = connection.Execute("sp_tblScanDataInsert", para, commandType: CommandType.StoredProcedure);

                                //var id = para.Get<string>("Id");
                                #endregion

                                #endregion

                                #region hien thi cac thong so dem
                                if (labGoodBox.InvokeRequired)
                                {
                                    labGoodBox.Invoke(new Action(() => { labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString(); }));
                                }
                                else labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString();

                                if (labGoodNoPrint.InvokeRequired)
                                {
                                    labGoodNoPrint.Invoke(new Action(() =>
                                    {
                                        labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();
                                    }));
                                }
                                else labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();

                                if (labGoodPrint.InvokeRequired)
                                {
                                    labGoodPrint.Invoke(new Action(() =>
                                    {
                                        labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();
                                    }));
                                }
                                else labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();

                                if (labFailBox.InvokeRequired)
                                {
                                    labFailBox.Invoke(new Action(() =>
                                    {
                                        labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting
                                        + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();
                                    }));
                                }
                                else labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();

                                if (labFailNoPrint.InvokeRequired)
                                {
                                    labFailNoPrint.Invoke(new Action(() =>
                                    {
                                        labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                                    }));
                                }
                                else labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                                if (labFailPrint.InvokeRequired)
                                {
                                    labFailPrint.Invoke(new Action(() =>
                                    {
                                        labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();
                                    }));
                                }
                                else labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();

                                if (labMetalScanBox.InvokeRequired)
                                {
                                    labMetalScanBox.Invoke(new Action(() =>
                                    {
                                        labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();
                                    }));
                                }
                                else labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();

                                if (labMetalScanCount.InvokeRequired)
                                {
                                    labMetalScanCount.Invoke(new Action(() =>
                                    {
                                        labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();
                                    }));
                                }
                                else labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();

                                if (labDeviation.InvokeRequired)
                                {
                                    labDeviation.Invoke(new Action(() =>
                                    {
                                        labDeviation.Text = _scanData.Deviation.ToString();
                                    }));
                                }
                                else labDeviation.Text = _scanData.Deviation.ToString();

                                if (labNetRealWeight.InvokeRequired)
                                {
                                    labNetRealWeight.Invoke(new Action(() =>
                                    {
                                        labNetRealWeight.Text = _scanData.NetWeight.ToString();
                                    }));
                                }
                                else labNetRealWeight.Text = _scanData.NetWeight.ToString();
                                #endregion

                                string json = JsonConvert.SerializeObject(GlobalVariables.RememberInfo);
                                File.WriteAllText(@"./RememberInfo.json", json);
                            }
                            else
                            {
                                XtraMessageBox.Show($"Item '{_scanData.ProductNumber}' không có khối lượng/1 đôi. Xin hãy kiểm tra lại thông tin."
                                    , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                ResetControl();

                                para = null;
                                para = new DynamicParameters();
                                para.Add("ProductNumber", _scanData.ProductNumber);
                                para.Add("ProductName", _scanData.ProductName);
                                para.Add("OcNum", _scanData.OcNo);
                                para.Add("Note", "Chưa có data trong file QC.");
                                para.Add("QrCode", _scanData.BarcodeString);

                                connection.Execute("sp_tblItemMissingInfoInsert", para, commandType: CommandType.StoredProcedure);
                            }
                        }
                        else
                        {
                            XtraMessageBox.Show($"Product number {_scanData.ProductNumber} không có trong hệ thống. Xin hãy kiểm tra lại thông tin."
                                , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            ResetControl();

                            para = null;
                            para = new DynamicParameters();
                            para.Add("ProductNumber", _scanData.ProductNumber);
                            para.Add("ProductName", _scanData.ProductName);
                            para.Add("OcNum", _scanData.OcNo);
                            para.Add("Note", $"Product item '{_scanData.ProductNumber}' không có data hệ thống.");
                            para.Add("QrCode", _scanData.BarcodeString);

                            connection.Execute("sp_tblItemMissingInfoInsert", para, commandType: CommandType.StoredProcedure);
                        }
                    }
                #endregion

                returnLoop:
                    #region reset txtQrcode để quét mã tiếp
                    if (_sen.InvokeRequired)
                    {
                        _sen?.Invoke(new Action(() =>
                        {
                            _sen.Text = null;
                        }));
                    }
                    else
                    {
                        _sen.Text = null;
                    }
                    _sen.Focus();
                    #endregion
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

        private void frmScale_FormClosing(object sender, FormClosingEventArgs e)
        {
            //huy đối tượng máy in
            SerialPortClose();

            //huy doi tuong can
            _scaleHelper.StopScale = true;
            _ckTask.Wait();
            _ckTask.Dispose();
            _scaleHelper.Dispose();
            GlobalVariables.ScaleStatus = "Disconnect";
        }

        private void ResetControl()
        {
            #region hiển thị thông tin
            if (labRealWeight.InvokeRequired)
            {
                labRealWeight.Invoke(new Action(() =>
                {
                    labRealWeight.Text = "0";
                }));
            }
            else labRealWeight.Text = "0";

            if (labNetWeight.InvokeRequired)
            {
                labNetWeight.Invoke(new Action(() =>
                {
                    labNetWeight.Text = "0";
                }));
            }
            else labNetWeight.Text = "0";

            if (labOcNo.InvokeRequired)
            {
                labOcNo.Invoke(new Action(() => { labOcNo.Text = string.Empty; }));
            }
            else labOcNo.Text = string.Empty;

            if (labProductCode.InvokeRequired)
            {
                labProductCode.Invoke(new Action(() => { labProductCode.Text = string.Empty; }));
            }
            else labProductCode.Text = string.Empty;

            if (labProductName.InvokeRequired)
            {
                labProductName.Invoke(new Action(() => { labProductName.Text = string.Empty; }));
            }
            else labProductName.Text = string.Empty;

            if (labQuantity.InvokeRequired)
            {
                labQuantity.Invoke(new Action(() => { labQuantity.Text = "0"; }));
            }
            else labQuantity.Text = "0";

            if (labColor.InvokeRequired)
            {
                labColor.Invoke(new Action(() => { labColor.Text = string.Empty; }));
            }
            else labColor.Text = string.Empty;

            if (labSize.InvokeRequired)
            {
                labSize.Invoke(new Action(() => { labSize.Text = string.Empty; }));
            }
            else labSize.Text = string.Empty;

            if (labAveWeight.InvokeRequired)
            {
                labAveWeight.Invoke(new Action(() => { labAveWeight.Text = "0"; }));
            }
            else labAveWeight.Text = "0";

            if (labToloren.InvokeRequired)
            {
                labToloren.Invoke(new Action(() => { labToloren.Text = "0"; }));
            }
            else labToloren.Text = "0";

            if (labBoxWeight.InvokeRequired)
            {
                labBoxWeight.Invoke(new Action(() => { labBoxWeight.Text = "0"; }));
            }
            else labBoxWeight.Text = "0";

            if (labAccessoriesWeight.InvokeRequired)
            {
                labAccessoriesWeight.Invoke(new Action(() => { labAccessoriesWeight.Text = "0"; }));
            }
            else labAccessoriesWeight.Text = "0";

            if (labGrossWeight.InvokeRequired)
            {
                labGrossWeight.Invoke(new Action(() => { labGrossWeight.Text = "0"; }));
            }
            else labGrossWeight.Text = "0";

            if (labResult.InvokeRequired)
            {
                labResult.Invoke(new Action(() =>
                {
                    labResult.Text = "Pass/Fail";
                    labResult.BackColor = Color.Gray;
                    labResult.ForeColor = Color.White;
                }));
            }
            else
            {
                labResult.Text = "Pass/Fail";
                labResult.BackColor = Color.Gray;
                labResult.ForeColor = Color.White;
            }

            if (labCalculatedPairs.InvokeRequired)
            {
                labCalculatedPairs.Invoke(new Action(() =>
                {
                    labCalculatedPairs.Text = "0";
                }));
            }
            else
            {
                labCalculatedPairs.Text = "0";
            }

            if (labDeviationPairs.InvokeRequired)
            {
                labDeviationPairs.Invoke(new Action(() =>
                {
                    labDeviationPairs.Text = "0";
                }));
            }
            else
            {
                labDeviationPairs.Text = "0";
            }
            #endregion
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
                _scanData.CreatedBy = GlobalVariables.UserLoginInfo.Id;
                _scanData.Station = GlobalVariables.Station;

                bool specialCase = false;//dùng có các trường hợp hàng PU, trên WL decpration là 0, nhưng QC phân ra printing 0-1. beforePrinting thì get theo
                                         //printing=0; afterPrinting thì get theo printing=1. 6112012228

                //biến dùng để check xem thùng đó có trong bảng scanData hay chưa.
                int statusLogData = 0;//0-chưa có;1-đã có dòng fail;2-đã có dòng pass;3-đã có cả fail và pass
                bool isFail = false;
                bool isPass = false;


                #region xử lý barcode lấy ra các giá trị theo code
                _scanData.BarcodeString = barcodeString;

                if (_scanData.BarcodeString.Contains("|"))
                {
                    var s = barcodeString.Split('|');
                    var s1 = s[0].Split(',');
                    _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    var ocFirstChar = s1[0].Substring(0, 2);
                    if (ocFirstChar == "OS" || ocFirstChar == "CS" || ocFirstChar == "OC" || ocFirstChar == "RE" || ocFirstChar == "LA" ||
                        ocFirstChar == "CL" || ocFirstChar == "PB" || ocFirstChar == "OL" || ocFirstChar == "SZ" || ocFirstChar == "OP"
                        || ocFirstChar == "PR"
                        )
                    {
                        _scanData.OcNo = s1[0];
                    }
                    else
                    {
                        MessageBox.Show("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return;
                    }

                    _scanData.ProductNumber = s1[1];

                    _scanData.Quantity = Convert.ToInt32(s1[2]);
                    _scanData.LinePosNo = s1[3];
                    _scanData.BoxNo = s1[5];
                    _scanData.CustomerNo = s1[6];
                    _scanData.BoxPosNo = s1[7];

                    if (s[1].Contains(","))
                    {
                        var s2 = s[1].Split(',');

                        GlobalVariables.IdLabel = s2[1];
                        _scanData.IdLabel = GlobalVariables.IdLabel;

                        if (s2[0] == "1")
                        {
                            _scanData.Location = LocationEnum.fVN;
                        }
                        else if (s2[0] == "2")
                        {
                            _scanData.Location = LocationEnum.fFT;
                        }
                        else if (s2[0] == "3")
                        {
                            _scanData.Location = LocationEnum.fKV;
                        }
                    }
                    else
                    {
                        if (s[1] == "1")
                        {
                            _scanData.Location = LocationEnum.fVN;
                        }
                        else if (s[1] == "2")
                        {
                            _scanData.Location = LocationEnum.fFT;
                        }
                        else if (s[1] == "3")
                        {
                            _scanData.Location = LocationEnum.fKV;
                        }
                    }
                }
                else
                {
                    var s1 = _scanData.BarcodeString.Split(',');
                    _plr = s1[4];//get Thung này đóng theo đôi (P) hay L/R

                    var ocFirstChar = s1[0].Substring(0, 2);
                    if (ocFirstChar == "OS" || ocFirstChar == "CS" || ocFirstChar == "OC" || ocFirstChar == "RE" || ocFirstChar == "LA" ||
                        ocFirstChar == "CL" || ocFirstChar == "PB" || ocFirstChar == "OL" || ocFirstChar == "SZ" || ocFirstChar == "OP"
                        || ocFirstChar == "PR"
                        )
                    {
                        _scanData.OcNo = s1[0];
                    }
                    else
                    {
                        MessageBox.Show("QR code bị sai, xóa đi rồi scan lại", "LỖI", MessageBoxButtons.OK, MessageBoxIcon.Error);

                        return;
                    }

                    //_scanData.OcNo = s1[0];
                    _scanData.ProductNumber = s1[1];

                    _scanData.Quantity = Convert.ToInt32(s1[2]);
                    _scanData.LinePosNo = s1[3];
                    _scanData.BoxNo = s1[5];
                }

                //Special case
                if (_scanData.ProductNumber.Contains("6112012228-"))
                {
                    specialCase = true;
                }

                GlobalVariables.OcNo = _scanData.OcNo;
                GlobalVariables.BoxNo = _scanData.BoxNo;
                #endregion

                switch (station)
                {
                    case 1://check metal
                        using (var connection = GlobalVariables.GetDbConnection())
                        {
                            var para = new DynamicParameters();

                            para = new DynamicParameters();
                            para.Add("@ProductNumber", _scanData.ProductNumber);
                            para.Add("@SpecialCase", specialCase);

                            var res = connection.Query<ProductInfoModel>("sp_vProductItemInfoGet", para, commandType: CommandType.StoredProcedure).FirstOrDefault();

                            if (res != null)
                            {
                                if (res.MetalScan == 1)
                                {
                                    Console.WriteLine($"ProductNumber: {res.ProductNumber} có kiểm tra kim loại.");
                                    #region gui data xuong PLC
                                    GlobalVariables.MyEvent.MetalPusher = 0;
                                    #endregion
                                }
                                else if (res.MetalScan == 0)
                                {
                                    Console.WriteLine($"ProductNumber: {res.ProductNumber} không kiểm tra kim loại.");
                                    #region gui data xuong PLC
                                    GlobalVariables.MyEvent.MetalPusher = 2;
                                    #endregion
                                }
                            }
                            else
                            {
                                #region gui data xuong PLC
                                GlobalVariables.MyEvent.MetalPusher = 1;
                                #endregion

                                XtraMessageBox.Show($"Product number {_scanData.ProductNumber} không có trong hệ thống. Xin hãy kiểm tra lại thông tin."
                                    , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                ResetControl();

                                para = null;
                                para = new DynamicParameters();
                                para.Add("ProductNumber", _scanData.ProductNumber);
                                para.Add("ProductName", _scanData.ProductName);
                                para.Add("OcNum", _scanData.OcNo);
                                para.Add("Note", $"Product item '{_scanData.ProductNumber}' không có data hệ thống.");
                                para.Add("QrCode", _scanData.BarcodeString);

                                connection.Execute("sp_tblItemMissingInfoInsert", para, commandType: CommandType.StoredProcedure);
                            }
                        }

                        break;
                    case 2:
                        #region truy vấn data và xử lý
                        //lấy thông tin khối lượng cân sau khi cân đã báo stable
                        while(_stableScale==0)
                        _scanData.GrossWeight = GlobalVariables.RealWeight = _scaleValue;

                        //truy vấn thông tin 
                        using (var connection = GlobalVariables.GetDbConnection())
                        {
                            var para = new DynamicParameters();

                            #region Kiểm tra xem thùng này đã được log vào scanData chưa
                            para.Add("QRLabel", _scanData.BarcodeString);

                            var checkInfo = connection.Query<tblScanDataCheckModel>("sp_tblScanDataCheck", para, commandType: CommandType.StoredProcedure).ToList();
                            var countRow = checkInfo.Count;
                            foreach (var item in checkInfo)
                            {

                                if (item.Pass == 1 || (item.Pass == 0 && item.ActualDeviationPairs == 0 && item.ApprovedBy != Guid.Empty))
                                {
                                    if (!_scanData.OcNo.Contains("PR"))
                                    {
                                        isPass = true;
                                    }
                                    else if (_scanData.OcNo.Contains("PR") && GlobalVariables.AfterPrinting == 0 && item.Status == 1)
                                    {
                                        isPass = true;
                                    }
                                    else if (_scanData.OcNo.Contains("PR") && GlobalVariables.AfterPrinting == 1 && item.Status == 2)
                                    {
                                        isPass = true;
                                    }
                                }
                                else if (item.Pass == 0)// && item.ActualDeviationPairs != 0 && item.ApprovedBy != Guid.Empty)
                                {
                                    if (!_scanData.OcNo.Contains("PR"))
                                    {
                                        isFail = true;
                                    }
                                    else if (_scanData.OcNo.Contains("PR") && GlobalVariables.AfterPrinting == 0 && item.Station == 0)
                                    {
                                        isFail = true;
                                    }
                                    else if (_scanData.OcNo.Contains("PR") && GlobalVariables.AfterPrinting == 1 && item.Station != 0)
                                    {
                                        isFail = true;
                                    }
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
                            para.Add("@ProductNumber", _scanData.ProductNumber);
                            para.Add("@SpecialCase", specialCase);

                            if (specialCase)
                            {
                                //after printing
                                if (
                                    _scanData.OcNo.Contains("OS")
                                    || _scanData.OcNo.Contains("CS")
                                    || _scanData.OcNo.Contains("OC")
                                    || _scanData.OcNo.Contains("RE")
                                    || _scanData.OcNo.Contains("LA")
                                    || _scanData.OcNo.Contains("CL")
                                    || _scanData.OcNo.Contains("PBF")
                                    || _scanData.OcNo.Contains("OL")
                                    || _scanData.OcNo.Contains("SZ")
                                    || _scanData.OcNo.Contains("OP")
                                    || (_scanData.OcNo.Contains("PR") && GlobalVariables.AfterPrinting != 0)
                                    )
                                {
                                    para.Add("@Printing", 1);//0 or 1, tùy theo hàng trước sơn hay sau sơn
                                }
                                //before printing
                                else if (_scanData.OcNo.Contains("PRT") && GlobalVariables.AfterPrinting == 0)
                                {
                                    para.Add("@Printing", 0);//0 or 1, tùy theo hàng trước sơn hay sau sơn
                                }
                            }

                            var res = connection.Query<ProductInfoModel>("sp_vProductItemInfoGet", para, commandType: CommandType.StoredProcedure).FirstOrDefault();

                            if (res != null)
                            {
                                _scanData.ProductName = res.ProductName;
                                _scanData.Decoration = res.Decoration;
                                _scanData.MetalScan = res.MetalScan;
                                _scanData.Brand = res.Brand;
                                _scanData.AveWeight1Prs = res.AveWeight1Prs;

                                if (_scanData.AveWeight1Prs != 0)
                                {
                                    #region Fill data from coreData to scanData, tính toán ra NetWeight và GrossWeight
                                    //Xét điều kiện để lấy boxWeight. Nếu là hàng đi sơn thì dùng thùng nhựa
                                    if (_scanData.Decoration == 0
                                        || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("OS"))
                                        || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("CS"))
                                        || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("OC"))
                                        || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("RE"))
                                        || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("LA"))
                                        || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("CL"))
                                        || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("PBF"))
                                        || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("OL"))
                                        || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("SZ"))
                                        || (_scanData.Decoration == 1 && _scanData.OcNo.Contains("OP"))
                                        )
                                    {
                                        _scanData.Status = 2;//báo trạng thái hàng ko đi sơn, hoặc hàng sơn đã được sơn rồi

                                        if (_scanData.Quantity <= res.BoxQtyBx4)
                                        {
                                            _scanData.BoxWeight = res.BoxWeightBx4;

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
                                        else if (_scanData.Quantity > res.BoxQtyBx4 && _scanData.Quantity <= res.BoxQtyBx3)
                                        {
                                            _scanData.BoxWeight = res.BoxWeightBx3;

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
                                        else if (_scanData.Quantity > res.BoxQtyBx3 && _scanData.Quantity <= res.BoxQtyBx2)
                                        {
                                            _scanData.BoxWeight = res.BoxWeightBx2;

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
                                        else if (_scanData.Quantity > res.BoxQtyBx2 && _scanData.Quantity <= res.BoxQtyBx1)
                                        {
                                            _scanData.BoxWeight = res.BoxWeightBx1;

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
                                        else if (_scanData.Quantity > res.BoxQtyBx1)
                                        {
                                            MessageBox.Show($"Số lượng vượt quá giới hạn thùng BX1 ({res.BoxQtyBx1})", "CẢNH BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                            para = null;
                                            para = new DynamicParameters();
                                            para.Add("ProductNumber", _scanData.ProductNumber);
                                            para.Add("ProductName", _scanData.ProductName);
                                            para.Add("OcNum", _scanData.OcNo);
                                            para.Add("Note", $"Số lượng vượt quá giới hạn thùng BX1 ({res.BoxQtyBx1})");
                                            para.Add("QrCode", _scanData.BarcodeString);

                                            connection.Execute("sp_tblItemMissingInfoInsert", para, commandType: CommandType.StoredProcedure);

                                            ResetControl();
                                            goto returnLoop;
                                        }

                                        if (_scanData.Decoration == 0)
                                        {
                                            if (labDecoration.InvokeRequired)
                                            {
                                                labDecoration.Invoke(new Action(() =>
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
                                            if (labDecoration.InvokeRequired)
                                            {
                                                labDecoration.Invoke(new Action(() =>
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
                                    else if (_scanData.Decoration == 1 && _scanData.OcNo.Contains("PR"))//hàng trước sơn. chỉ có trạm SSFG01 mới nhảy vào đây
                                    {
                                        if (GlobalVariables.AfterPrinting == 0)
                                        {
                                            _scanData.Status = 1;// báo trạng thái hàng sơn cần đưa đi sơn, trạm SSFG01
                                        }
                                        else
                                        {
                                            _scanData.Status = 2;// báo trạng thái hàng sơn đã được sơn, trạm SSFG02 và SSFG03(Kerry)
                                        }

                                        _scanData.BoxWeight = res.PlasicBoxWeight;

                                        if (labDecoration.InvokeRequired)
                                        {
                                            labDecoration.Invoke(new Action(() =>
                                            {
                                                labDecoration.BackColor = Color.Gold;
                                            }));
                                        }
                                        else
                                        {
                                            labDecoration.BackColor = Color.Gold;
                                        }
                                    }

                                    if (_scanData.MetalScan == 0)
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

                                    _scanData.StdNetWeight = Math.Round(_scanData.Quantity * _scanData.AveWeight1Prs, 3);
                                    _scanData.Tolerance = Math.Round(_scanData.StdNetWeight * (res.Tolerance / 100), 3);

                                    //luu ý các Quantity partition-Plasic-WrapSheet trên DB nó là tính số Prs
                                    //sau khi đọc về phải lấy QtyPrs quét trên label / Quantity partition-Plasic-WrapSheet ==> qty * weight ==> Weight package weight
                                    double partitionWeight = 0;
                                    var p = res.PartitionQty != 0 ? ((double)_scanData.Quantity / (double)res.PartitionQty) : 0;
                                    if (_scanData.Quantity <= res.BoxQtyBx3 || p < 1)
                                    {
                                        partitionWeight = 0;
                                    }
                                    else if (p >= 1)
                                    {
                                        partitionWeight = Math.Floor(p) * res.PartitionWeight;
                                    }
                                    //partitionWeight = res.PartitionQty != 0 ? (_scanData.Quantity / res.PartitionQty) * res.PartitionWeight : 0;
                                    var plasicBag1Weight = res.PlasicBag1Qty != 0 ? Math.Ceiling(((double)_scanData.Quantity / (double)res.PlasicBag1Qty)) * res.PlasicBag1Weight : 0;
                                    var plasicBag2Weight = res.PlasicBag2Qty != 0 ? Math.Ceiling(((double)_scanData.Quantity / (double)res.PlasicBag2Qty)) * res.PlasicBag2Weight : 0;
                                    var wrapSheetWeight = res.WrapSheetQty != 0 ? Math.Ceiling(((double)_scanData.Quantity / (double)res.WrapSheetQty)) * res.WrapSheetWeight : 0;
                                    var foamSheetWeight = res.FoamSheetQty != 0 ? Math.Ceiling(((double)_scanData.Quantity / (double)res.FoamSheetQty)) * res.FoamSheetWeight : 0;

                                    _scanData.PackageWeight = Math.Round(partitionWeight + plasicBag1Weight + plasicBag2Weight + wrapSheetWeight + foamSheetWeight, 3);

                                    _scanData.StdGrossWeight = Math.Round(_scanData.StdNetWeight + _scanData.PackageWeight + _scanData.BoxWeight, 3);

                                    #region tinh toán standardWeight theo Pair/Left/Right. lưu ý để sau này có áp dụng thì làm
                                    //if (_plr == "P")
                                    //{
                                    //    _scanData.GrossdWeight = res.Weight * res.QtyPerbag + res.BagWeight;
                                    //}
                                    //else if (_plr == "L")
                                    //{
                                    //    if (res.LeftWeight == 0)
                                    //    {
                                    //        _scanData.StandardWeight = res.Weight * res.QtyPerbag + res.BagWeight;
                                    //    }
                                    //    else
                                    //    {
                                    //        _scanData.StandardWeight = res.LeftWeight * res.QtyPerbag + res.BagWeight;
                                    //    }
                                    //}
                                    //else if (_plr == "R")
                                    //{
                                    //    if (res.RightWeight == 0)
                                    //    {
                                    //        _scanData.StandardWeight = res.Weight * res.QtyPerbag + res.BagWeight;
                                    //    }
                                    //    else
                                    //    {
                                    //        _scanData.StandardWeight = res.RightWeight * res.QtyPerbag + res.BagWeight;
                                    //    }
                                    //}
                                    #endregion

                                    #endregion

                                    #region hiển thị thông tin
                                    if (labRealWeight.InvokeRequired)
                                    {
                                        labRealWeight.Invoke(new Action(() =>
                                        {
                                            labRealWeight.Text = _scanData.GrossWeight.ToString();
                                        }));
                                    }
                                    else labRealWeight.Text = _scanData.GrossWeight.ToString();

                                    if (labNetWeight.InvokeRequired)
                                    {
                                        labNetWeight.Invoke(new Action(() =>
                                        {
                                            labNetWeight.Text = _scanData.StdNetWeight.ToString();
                                        }));
                                    }
                                    else labNetWeight.Text = _scanData.StdNetWeight.ToString();

                                    if (labOcNo.InvokeRequired)
                                    {
                                        labOcNo.Invoke(new Action(() => { labOcNo.Text = _scanData.OcNo; }));
                                    }
                                    else labOcNo.Text = _scanData.OcNo;

                                    if (labProductCode.InvokeRequired)
                                    {
                                        labProductCode.Invoke(new Action(() => { labProductCode.Text = _scanData.ProductNumber; }));
                                    }
                                    else labProductCode.Text = _scanData.ProductNumber;

                                    if (labProductName.InvokeRequired)
                                    {
                                        labProductName.Invoke(new Action(() => { labProductName.Text = _scanData.ProductName; }));
                                    }
                                    else labProductName.Text = _scanData.ProductName;

                                    if (labQuantity.InvokeRequired)
                                    {
                                        labQuantity.Invoke(new Action(() => { labQuantity.Text = _scanData.Quantity.ToString(); }));
                                    }
                                    else labQuantity.Text = _scanData.Quantity.ToString();

                                    if (labColor.InvokeRequired)
                                    {
                                        labColor.Invoke(new Action(() => { labColor.Text = res.Color; }));
                                    }
                                    else labColor.Text = res.Color;

                                    if (labSize.InvokeRequired)
                                    {
                                        labSize.Invoke(new Action(() => { labSize.Text = res.SizeName; }));
                                    }
                                    else labSize.Text = res.SizeName;

                                    if (labAveWeight.InvokeRequired)
                                    {
                                        labAveWeight.Invoke(new Action(() => { labAveWeight.Text = _scanData.AveWeight1Prs.ToString(); }));
                                    }
                                    else labAveWeight.Text = _scanData.AveWeight1Prs.ToString();

                                    if (labToloren.InvokeRequired)
                                    {
                                        labToloren.Invoke(new Action(() => { labToloren.Text = _scanData.Tolerance.ToString(); }));
                                    }
                                    else labToloren.Text = _scanData.Tolerance.ToString();

                                    if (labBoxWeight.InvokeRequired)
                                    {
                                        labBoxWeight.Invoke(new Action(() => { labBoxWeight.Text = _scanData.BoxWeight.ToString(); }));
                                    }
                                    else labBoxWeight.Text = _scanData.BoxWeight.ToString();

                                    if (labAccessoriesWeight.InvokeRequired)
                                    {
                                        labAccessoriesWeight.Invoke(new Action(() => { labAccessoriesWeight.Text = _scanData.PackageWeight.ToString(); }));
                                    }
                                    else labAccessoriesWeight.Text = _scanData.PackageWeight.ToString();

                                    if (labGrossWeight.InvokeRequired)
                                    {
                                        labGrossWeight.Invoke(new Action(() => { labGrossWeight.Text = _scanData.StdGrossWeight.ToString(); }));
                                    }
                                    else labGrossWeight.Text = _scanData.StdGrossWeight.ToString();
                                    #endregion

                                    #region xử lý so sánh khối lượng cân thực tế với kế hoạch để xử lý
                                    _scanData.NetWeight = Math.Round(_scanData.GrossWeight - _scanData.BoxWeight - _scanData.PackageWeight, 3);
                                    _scanData.Deviation = Math.Round(_scanData.NetWeight - _scanData.StdNetWeight, 3);

                                    #region tính toán số pairs chênh lệch và hiển thị label
                                    var nwPlus = _scanData.StdNetWeight + _scanData.Tolerance;
                                    var nwSub = _scanData.StdNetWeight - _scanData.Tolerance;

                                    if (((_scanData.NetWeight > nwPlus) && (_scanData.NetWeight - nwPlus < _scanData.AveWeight1Prs / 2))
                                    || ((_scanData.NetWeight < nwSub) && (nwSub - _scanData.NetWeight < _scanData.AveWeight1Prs / 2))
                                    )
                                    {
                                        _scanData.CalculatedPairs = _scanData.Quantity;
                                    }
                                    else if (_scanData.NetWeight > nwPlus)//roundDown
                                    {
                                        _scanData.CalculatedPairs = (int)(_scanData.Quantity + Math.Floor((_scanData.NetWeight - nwPlus) / _scanData.AveWeight1Prs));
                                    }
                                    else if (_scanData.NetWeight < nwSub)//RoundUp
                                    {
                                        _scanData.CalculatedPairs = (int)(_scanData.Quantity - Math.Ceiling((nwSub - _scanData.NetWeight) / _scanData.AveWeight1Prs));
                                    }
                                    else
                                    {
                                        _scanData.CalculatedPairs = _scanData.Quantity;
                                    }

                                    _scanData.DeviationPairs = _scanData.CalculatedPairs - _scanData.Quantity;

                                    if (labCalculatedPairs.InvokeRequired)
                                    {
                                        labCalculatedPairs.Invoke(new Action(() =>
                                        {
                                            labCalculatedPairs.Text = _scanData.CalculatedPairs.ToString();
                                        }));
                                    }
                                    else
                                    {
                                        labCalculatedPairs.Text = _scanData.CalculatedPairs.ToString();
                                    }

                                    if (labDeviationPairs.InvokeRequired)
                                    {
                                        labDeviationPairs.Invoke(new Action(() =>
                                        {
                                            labDeviationPairs.Text = _scanData.DeviationPairs.ToString();
                                        }));
                                    }
                                    else
                                    {
                                        labDeviationPairs.Text = _scanData.DeviationPairs.ToString();
                                    }
                                    #endregion

                                    //thung hang Pass
                                    if (_scanData.DeviationPairs == 0)
                                    {
                                        //bật tín hiệu để PLC on đèn xanh
                                        GlobalVariables.MyEvent.StatusLightPLC = true;

                                        if (_scanData.Decoration == 0)
                                        {
                                            GlobalVariables.RememberInfo.GoodBoxPrinting += 1;
                                            //_scanData.Status = 1;
                                        }
                                        else
                                        {
                                            GlobalVariables.RememberInfo.GoodBoxNoPrinting += 1;
                                            //_scanData.Status = 2;
                                        }

                                        #region hien thi mau label
                                        if (labResult.InvokeRequired)
                                        {
                                            labResult.Invoke(new Action(() =>
                                            {
                                                labResult.Text = "Pass";
                                                labResult.BackColor = Color.Green;
                                                labResult.ForeColor = Color.White;
                                            }));
                                        }
                                        else
                                        {
                                            labResult.Text = "Pass";
                                            labResult.BackColor = Color.Green;
                                            labResult.ForeColor = Color.White;
                                        }
                                        #endregion

                                        //kiểm tra xem data đã có trên hệ thống hay chưa
                                        if (statusLogData == 0 || statusLogData == 1)
                                        {
                                            _scanData.Pass = 1;
                                            _scanData.CreatedDate = GlobalVariables.CreatedDate = DateTime.Now;//lấy thời gian để đồng bộ giữa in tem và log DB
                                                                                                               //Printing
                                            GlobalVariables.Printing((_scanData.GrossWeight / 1000).ToString("#,#0.00")
                                                        , !string.IsNullOrEmpty(GlobalVariables.IdLabel) ? GlobalVariables.IdLabel : $"{_scanData.OcNo}|{_scanData.BoxNo}", true
                                                         , _scanData.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));

                                            //báo trạng thái cho pusher là thùng cân OK
                                            GlobalVariables.MyEvent.WeightPusher = 0;
                                        }
                                        else
                                        {
                                            MessageBox.Show($"Thùng này đã được quét ghi nhận khối lượng OK rồi, không được phép cân lại." +
                                                $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information); ;

                                            ResetControl();
                                            goto returnLoop;
                                        }
                                        //GlobalVariables.RealWeight = _scanData.GrossWeight;
                                        //GlobalVariables.PrintApprove = true;
                                    }
                                    else//thung fail
                                    {
                                        //bật tín hiệu để PLC on đèn xanh
                                        GlobalVariables.MyEvent.StatusLightPLC = false;
                                        //ghi tín hiệu xuống PLC conveyor điều khiển pusher reject. //báo trạng thái cho pusher là thùng cân OK
                                        GlobalVariables.MyEvent.WeightPusher = 1;

                                        _scanData.Pass = 0;
                                        _scanData.Status = 0;

                                        GlobalVariables.PrintApprove = false;
                                        if (_scanData.Decoration == 1)
                                        {
                                            GlobalVariables.RememberInfo.FailBoxPrinting += 1;
                                        }
                                        else
                                        {
                                            GlobalVariables.RememberInfo.FailBoxNoPrinting += 1;
                                        }

                                        #region hien thi mau label
                                        if (labResult.InvokeRequired)
                                        {
                                            labResult.Invoke(new Action(() =>
                                            {
                                                labResult.Text = "Fail";
                                                labResult.BackColor = Color.Red;
                                                labResult.ForeColor = Color.White;
                                            }));
                                        }
                                        else
                                        {
                                            labResult.Text = "Fail";
                                            labResult.BackColor = Color.Red;
                                            labResult.ForeColor = Color.White;
                                        }
                                        #endregion

                                        if (statusLogData == 0)
                                        {
                                            _scanData.CreatedDate = GlobalVariables.CreatedDate = DateTime.Now;//lấy thời gian để đồng bộ giữa in tem và log DB

                                            GlobalVariables.Printing(_scanData.DeviationPairs.ToString()
                                                        , !string.IsNullOrEmpty(GlobalVariables.IdLabel) ? GlobalVariables.IdLabel : $"{_scanData.OcNo}|{_scanData.BoxNo}", false
                                                        , _scanData.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));
                                        }
                                        else
                                        {
                                            MessageBox.Show($"Thùng này đã được quét ghi nhận khối lượng lỗi rồi, không được phép cân lại." +
                                                $"{Environment.NewLine}Quét thùng khác.", "THÔNG BÁO", MessageBoxButtons.OK, MessageBoxIcon.Information); ;

                                            ResetControl();
                                            goto returnLoop;
                                        }
                                    }
                                    #endregion

                                    #region Log data
                                    //mỗi thùng chỉ cho log vào tối da là 2 dòng trong scanData, 1 dòng pass và fail (nếu có)

                                    #region Log scanData
                                    para = null;
                                    para = new DynamicParameters();
                                    para.Add("@BarcodeString", _scanData.BarcodeString);
                                    para.Add("@IdLable", _scanData.IdLabel);
                                    para.Add("@OcNo", _scanData.OcNo);
                                    para.Add("@ProductNumber", _scanData.ProductNumber);
                                    para.Add("@ProductName", _scanData.ProductName);
                                    para.Add("@Quantity", _scanData.Quantity);
                                    para.Add("@LinePosNo", _scanData.LinePosNo);
                                    para.Add("@Unit", _scanData.Unit);
                                    para.Add("@BoxNo", _scanData.BoxNo);
                                    para.Add("@CustomerNo", _scanData.CustomerNo);
                                    para.Add("@Location", _scanData.Location);
                                    para.Add("@BoxPosNo", _scanData.BoxPosNo);
                                    para.Add("@Note", _scanData.Note);
                                    para.Add("@Brand", _scanData.Brand);
                                    para.Add("@Decoration", _scanData.Decoration);
                                    para.Add("@MetalScan", _scanData.MetalScan);
                                    para.Add("@ActualMetalScan", _scanData.ActualMetalScan);
                                    para.Add("@AveWeight1Prs", _scanData.AveWeight1Prs);
                                    para.Add("@StdNetWeight", _scanData.StdNetWeight);
                                    para.Add("@Tolerance", _scanData.Tolerance);
                                    para.Add("@Boxweight", _scanData.BoxWeight);
                                    para.Add("@PackageWeight", _scanData.PackageWeight);
                                    para.Add("@StdGrossWeight", _scanData.StdGrossWeight);
                                    para.Add("@GrossWeight", _scanData.GrossWeight);
                                    para.Add("@NetWeight", _scanData.NetWeight);
                                    para.Add("@Deviation", _scanData.Deviation);
                                    para.Add("@Pass", _scanData.Pass);
                                    para.Add("Status", _scanData.Status);
                                    para.Add("CalculatedPairs", _scanData.CalculatedPairs);
                                    para.Add("DeviationPairs", _scanData.DeviationPairs);
                                    para.Add("CreatedBy", _scanData.CreatedBy);
                                    para.Add("Station", _scanData.Station);
                                    para.Add("CreatedDate", _scanData.CreatedDate);
                                    para.Add("ApprovedBy", _scanData.ApprovedBy);
                                    para.Add("ActualDeviationPairs", _scanData.ActualDeviationPairs);
                                    //para.Add("Id", ParameterDirection.Output, DbType.Guid);

                                    var insertResult = connection.Execute("sp_tblScanDataInsert", para, commandType: CommandType.StoredProcedure);

                                    //var id = para.Get<string>("Id");
                                    #endregion

                                    #endregion

                                    #region hien thi cac thong so dem
                                    if (labGoodBox.InvokeRequired)
                                    {
                                        labGoodBox.Invoke(new Action(() => { labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString(); }));
                                    }
                                    else labGoodBox.Text = (GlobalVariables.RememberInfo.GoodBoxNoPrinting + GlobalVariables.RememberInfo.GoodBoxPrinting).ToString();

                                    if (labGoodNoPrint.InvokeRequired)
                                    {
                                        labGoodNoPrint.Invoke(new Action(() =>
                                        {
                                            labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();
                                        }));
                                    }
                                    else labGoodNoPrint.Text = GlobalVariables.RememberInfo.GoodBoxNoPrinting.ToString();

                                    if (labGoodPrint.InvokeRequired)
                                    {
                                        labGoodPrint.Invoke(new Action(() =>
                                        {
                                            labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();
                                        }));
                                    }
                                    else labGoodPrint.Text = GlobalVariables.RememberInfo.GoodBoxPrinting.ToString();

                                    if (labFailBox.InvokeRequired)
                                    {
                                        labFailBox.Invoke(new Action(() =>
                                        {
                                            labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting
                                            + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();
                                        }));
                                    }
                                    else labFailBox.Text = (GlobalVariables.RememberInfo.FailBoxNoPrinting + GlobalVariables.RememberInfo.FailBoxPrinting).ToString();

                                    if (labFailNoPrint.InvokeRequired)
                                    {
                                        labFailNoPrint.Invoke(new Action(() =>
                                        {
                                            labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                                        }));
                                    }
                                    else labFailNoPrint.Text = GlobalVariables.RememberInfo.FailBoxNoPrinting.ToString();
                                    if (labFailPrint.InvokeRequired)
                                    {
                                        labFailPrint.Invoke(new Action(() =>
                                        {
                                            labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();
                                        }));
                                    }
                                    else labFailPrint.Text = GlobalVariables.RememberInfo.FailBoxPrinting.ToString();

                                    if (labMetalScanBox.InvokeRequired)
                                    {
                                        labMetalScanBox.Invoke(new Action(() =>
                                        {
                                            labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();
                                        }));
                                    }
                                    else labMetalScanBox.Text = GlobalVariables.RememberInfo.MetalScan.ToString();

                                    if (labMetalScanCount.InvokeRequired)
                                    {
                                        labMetalScanCount.Invoke(new Action(() =>
                                        {
                                            labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();
                                        }));
                                    }
                                    else labMetalScanCount.Text = GlobalVariables.RememberInfo.CountMetalScan.ToString();

                                    if (labDeviation.InvokeRequired)
                                    {
                                        labDeviation.Invoke(new Action(() =>
                                        {
                                            labDeviation.Text = _scanData.Deviation.ToString();
                                        }));
                                    }
                                    else labDeviation.Text = _scanData.Deviation.ToString();

                                    if (labNetRealWeight.InvokeRequired)
                                    {
                                        labNetRealWeight.Invoke(new Action(() =>
                                        {
                                            labNetRealWeight.Text = _scanData.NetWeight.ToString();
                                        }));
                                    }
                                    else labNetRealWeight.Text = _scanData.NetWeight.ToString();
                                    #endregion

                                    string json = JsonConvert.SerializeObject(GlobalVariables.RememberInfo);
                                    File.WriteAllText(@"./RememberInfo.json", json);
                                }
                                else
                                {
                                    XtraMessageBox.Show($"Item '{_scanData.ProductNumber}' không có khối lượng/1 đôi. Xin hãy kiểm tra lại thông tin."
                                        , "CẢNH BÁO.", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                                    ResetControl();

                                    para = null;
                                    para = new DynamicParameters();
                                    para.Add("ProductNumber", _scanData.ProductNumber);
                                    para.Add("ProductName", _scanData.ProductName);
                                    para.Add("OcNum", _scanData.OcNo);
                                    para.Add("Note", "Chưa có data trong file QC.");
                                    para.Add("QrCode", _scanData.BarcodeString);

                                    connection.Execute("sp_tblItemMissingInfoInsert", para, commandType: CommandType.StoredProcedure);
                                }
                            }
                        }
                    #endregion
                    returnLoop:
                        break;
                    case 3:
                        using (var connection = GlobalVariables.GetDbConnection())
                        {
                            var para = new DynamicParameters();

                            para = new DynamicParameters();
                            para.Add("@ProductNumber", _scanData.ProductNumber);
                            para.Add("@SpecialCase", specialCase);

                            var res = connection.Query<ProductInfoModel>("sp_vProductItemInfoGet", para, commandType: CommandType.StoredProcedure).FirstOrDefault();

                            if (res != null)
                            {
                                if (res.Printing == 1)
                                {
                                    Console.WriteLine($"ProductNumber: {res.ProductNumber} là hàng sơn.");
                                    #region gui data xuong PLC
                                    GlobalVariables.MyEvent.PrintPusher = 1;
                                    #endregion
                                }
                                else if (res.Printing == 0)
                                {
                                    Console.WriteLine($"ProductNumber: {res.ProductNumber} không phải hàng sơn.");
                                    #region gui data xuong PLC
                                    GlobalVariables.MyEvent.PrintPusher = 0;
                                    #endregion
                                }
                            }
                        }
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
                //this.Invoke((MethodInvoker)delegate { txtDataAscii1.Text = xmlDoc.GetElementsByTagName("datalabel")[0].InnerText; });
                _barcodeString1 = AsciiToString(xmlDoc.GetElementsByTagName("datalabel")[0].InnerText);

                BarcodeHandle(1, _barcodeString1);
            }
            else if (scannerId[0].InnerText == GlobalVariables.ScannerIdWeight.ToString())//vị trí check weight. ngay cân
            {
                _barcodeString2 = AsciiToString(xmlDoc.GetElementsByTagName("datalabel")[0].InnerText);
                BarcodeHandle(2, _barcodeString2);
            }
            else if (scannerId[0].InnerText == GlobalVariables.ScannerIdPrint.ToString())//vị trí phân loại hàng sơn cuối chuyền
            {
                _barcodeString3 = AsciiToString(xmlDoc.GetElementsByTagName("datalabel")[0].InnerText);
                BarcodeHandle(3, _barcodeString3);
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
                    SendDynamicString(" ", " ", "");
                }
                else if (rcvArr[4] == 0x4F)
                {
                    Console.WriteLine($"Gui lenh xuong may in thanh cong!!!");
                }
                else if (rcvArr[4] == 0x31)
                {
                    Console.WriteLine($"Loi. Error Code: {rcvArr[5]}");
                    MessageBox.Show($"Send command error: Error code: {rcvArr[5]}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    StopPrint();
                    Thread.Sleep(1000);
                    StartPrint();
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

                //this.Invoke((MethodInvoker)delegate {
                //    labStatus.Text = string.Empty;
                //    labStatus.Text += $"{item} ";
                //});
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
        /// <param name="idLabel">Id label hoặc là OC.</param>
        /// <param name="weight">Khối lượng cân thực tế.</param>
        /// <param name="date">Thời điểm cân.</param>
        private void SendDynamicString(string idLabel, string weight, string date)
        {
            int i = 0, j = 0, k = 0;
            int chkSUM = 0;

            byte[] SetDynamicString = new byte[14 + idLabel.Length + weight.Length + date.Length];
            SetDynamicString[0] = 0x2;
            SetDynamicString[1] = 0x0;
            SetDynamicString[2] = (byte)(9 + idLabel.Length + weight.Length + date.Length);
            SetDynamicString[3] = 0x0;
            SetDynamicString[4] = 0xCA; // Mã lệnh Set dynamic string
            SetDynamicString[5] = 0;
            SetDynamicString[6] = 0;
            SetDynamicString[7] = (byte)(idLabel.Length); // Chiều dài của string 1
            SetDynamicString[8] = (byte)(weight.Length); // Chiều dài của string 2
            SetDynamicString[9] = (byte)(date.Length); // Chiều dài của string 3
            SetDynamicString[10] = 0; // Chiều dài của string 4
            SetDynamicString[11] = 0; // Chiều dài của string 5

            //chuyen string sang ASCII
            var idLabelArr = idLabel.ToCharArray();
            var weighArr = weight.ToCharArray();
            var dateArr = date.ToCharArray();

            byte[] idLabelAscii = Encoding.ASCII.GetBytes(idLabelArr);
            byte[] weightAscii = Encoding.ASCII.GetBytes(weighArr);
            byte[] dateAscii = Encoding.ASCII.GetBytes(dateArr);

            for (i = 0; i <= idLabelAscii.Length - 1; i++)
            {
                SetDynamicString[12 + i] = idLabelAscii[i];// Nội dung của string 1
            }

            for (j = 0; j <= weightAscii.Length - 1; j++)
            {
                SetDynamicString[12 + i + j] = weightAscii[j];// Nội dung của string 1
            }

            for (j = 0; j <= dateAscii.Length - 1; j++)
            {
                SetDynamicString[12 + i + j + k] = dateAscii[k];// Nội dung của string 1
            }

            // Tính check SUM
            for (var c = 1; c <= i + j + k + 12; c++)
                chkSUM = chkSUM + SetDynamicString[c];
            chkSUM = chkSUM & 0xFF;
            SetDynamicString[i + j + k + 12] = System.Convert.ToByte(chkSUM); // Gán byte checksum vào arr
            SetDynamicString[i + j + k + 12 + 1] = 0x3;

            _serialPort.Write(SetDynamicString, 0, SetDynamicString.Length);
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
    }
}