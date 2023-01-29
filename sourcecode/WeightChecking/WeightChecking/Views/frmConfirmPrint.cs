using Dapper;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BC = BCrypt.Net.BCrypt;

namespace WeightChecking
{
    public partial class frmConfirmPrint : Form
    {
        public ConfirmPrintModel ConfirmPrintInfo = new ConfirmPrintModel();
        string _code = null;
        Timer _timer = new Timer();
        int _checkCount = 0;//đếm số lần scan QR code. ban đầu vào scan QR code label, sau đó scan QR code approve. rồi mới cho in lại tem
        tblScanDataModel _scanData = new tblScanDataModel();
        double _scaleValue = 0;
        Guid _qrApproved;
        int _actualDeviation = 0;

        public frmConfirmPrint()
        {
            InitializeComponent();
            Load += FrmConfirmPrint_Load;
            btnConfirm.Click += BtnConfirm_Click;
            txtQrCode.KeyDown += TxtQrCode_KeyDown;
            btnConfirm.Visible = false;
        }

        private void TxtQrCode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    TextEdit _sen = sender as TextEdit;

                    _actualDeviation = short.TryParse(txtActualDeviation.Text, out short value) ? value : 0;

                    if (_checkCount == 0)
                    {
                        #region xử lý barcode lấy ra các giá trị theo code
                        _scanData.BarcodeString = _sen.Text;
                        if (_scanData.BarcodeString.Contains("|"))
                        {
                            var s = _sen.Text.Split('|');
                            var s1 = s[0].Split(',');
                            _scanData.OcNo = s1[0];
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
                            _scanData.OcNo = s1[0];
                            _scanData.ProductNumber = s1[1];

                            _scanData.Quantity = Convert.ToInt32(s1[2]);
                            _scanData.LinePosNo = s1[3];
                            _scanData.BoxNo = s1[5];
                        }
                        #endregion

                        #region show info labels
                        if (labProductCode.InvokeRequired)
                        {
                            labProductCode.Invoke(new Action(() =>
                            {
                                labProductCode.Text = _scanData.ProductNumber;
                            }));
                        }
                        else
                        {
                            labProductCode.Text = _scanData.ProductNumber;
                        }

                        if (labIdLabel.InvokeRequired)
                        {
                            labIdLabel.Invoke(new Action(() =>
                            {
                                labIdLabel.Text = _scanData.IdLabel;
                            }));
                        }
                        else
                        {
                            labIdLabel.Text = _scanData.IdLabel;
                        }

                        if (labOcNo.InvokeRequired)
                        {
                            labOcNo.Invoke(new Action(() =>
                            {
                                labOcNo.Text = _scanData.OcNo;
                            }));
                        }
                        else
                        {
                            labOcNo.Text = _scanData.OcNo;
                        }

                        if (labBoxNo.InvokeRequired)
                        {
                            labBoxNo.Invoke(new Action(() =>
                            {
                                labBoxNo.Text = _scanData.BoxNo;
                            }));
                        }
                        else
                        {
                            labBoxNo.Text = _scanData.BoxNo;
                        }

                        if (labQty.InvokeRequired)
                        {
                            labQty.Invoke(new Action(() =>
                            {
                                labQty.Text = _scanData.Quantity.ToString();
                            }));
                        }
                        else
                        {
                            labQty.Text = _scanData.Quantity.ToString();
                        }
                        #endregion

                        if (labQrCode.InvokeRequired)
                        {
                            labQrCode.Invoke(new Action(() =>
                            {
                                labQrCode.Text = "Scan QR Code Approve:";
                            }));
                        }
                        else
                        {
                            labQrCode.Text = "Scan QR Code Approve:";
                        }
                        _checkCount = 1;
                    }
                    else if (_checkCount == 1)
                    {
                        _qrApproved = Guid.TryParse(_sen.Text, out Guid valueD) ? valueD : Guid.Empty;

                        CheckingInfoUpdate();

                        _checkCount = 0;
                    }

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
                catch (Exception ex)
                {
                    MessageBox.Show("Quét sai mã QR. Mời quét lại.","CẢNH BÁO",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            CheckingInfoUpdate();
        }

        private void CheckingInfoUpdate()
        {
            try
            {
                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var para = new DynamicParameters();
                    para.Add("Id", _qrApproved);

                    var res = connection.Query<tblUsers>("sp_tblUserGet", para, commandType: CommandType.StoredProcedure).FirstOrDefault();
                    if (res != null)
                    {
                        if (res.Approved == 1)
                        {
                            //var scandataDetail = new tblScanDataModel();
                            string approveType = null;

                            #region Get thông tin của thùng trong bảng ScanData
                            para = null;
                            para = new DynamicParameters();
                            para.Add("QRLabel", _scanData.BarcodeString);

                            var scandataDetail = connection.Query<tblScanDataModel>("sp_tblScanDataGetForApprovedPrint", para, commandType: CommandType.StoredProcedure).FirstOrDefault();
                            #endregion

                            if (scandataDetail != null)
                            {
                                //trường hợp xác định là false alarm, cho in lại tem và cập nhật ApprovedBy vào bảng tblScanData
                                if (scandataDetail.ApprovedBy == Guid.Empty && scandataDetail.Pass == 0 && _actualDeviation == 0)
                                {
                                    var dialogResult = MessageBox.Show($"Bạn có chắc chắn xác nhận thùng với thông tin sau:" +
                                         $"{Environment.NewLine}{scandataDetail.IdLabel}|{scandataDetail.OcNo}|{scandataDetail.BoxNo}{Environment.NewLine}" +
                                         $" là cảnh báo sai và in lại tem?", "CẢNH BÁO", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                    if (dialogResult == DialogResult.Yes)
                                    {
                                        GlobalVariables.Printing(_scaleValue.ToString("#,#0.00")
                                                  , !string.IsNullOrEmpty(_scanData.IdLabel) ? _scanData.IdLabel : $"{_scanData.OcNo}|{_scanData.BoxNo}", true
                                                  , scandataDetail.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"));

                                        para = null;
                                        para = new DynamicParameters();
                                        para.Add("Id", scandataDetail.Id);
                                        para.Add("ApproveBy", _qrApproved);
                                        para.Add("ActualDeviationPairs", _actualDeviation);
                                        para.Add("GrossWeight", GlobalVariables.RealWeight);

                                        connection.Execute("sp_tblScanDataUpdateApproveBy", para, commandType: CommandType.StoredProcedure);

                                        approveType = "False alarm";
                                    }

                                }
                                //trường hợp này là true alarm, nhưng vào chỉnh sửa lại chênh lêch thực tế (actual deviation)
                                else if (scandataDetail.ApprovedBy == Guid.Empty && scandataDetail.Pass == 0 && _actualDeviation != 0)
                                {
                                    var dialogResult = MessageBox.Show($"Bạn có chắc chắn xác nhận cập nhật số lượng chênh lệch thực tế cho thùng với thông tin sau:" +
                                         $"{Environment.NewLine}{scandataDetail.IdLabel}|{scandataDetail.OcNo}|{scandataDetail.BoxNo}.{Environment.NewLine}" +
                                         $"Số lượng lệch thực tế là: {_actualDeviation}?", "CẢNH BÁO", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                    if (dialogResult == DialogResult.Yes)
                                    {
                                        //GlobalVariables.Printing(_scaleValue.ToString("#,#0.00")
                                        //          , !string.IsNullOrEmpty(_scanData.IdLabel) ? _scanData.IdLabel : $"{_scanData.OcNo}|{_scanData.BoxNo}", true
                                        //          , scandataDetail.CreatedDate.ToString("yyyy/MM/dd HH:mm:ss"));

                                        para = null;
                                        para = new DynamicParameters();
                                        para.Add("Id", scandataDetail.Id);
                                        para.Add("ApproveBy", _qrApproved);
                                        para.Add("ActualDeviationPairs", _actualDeviation);
                                        para.Add("GrossWeight", GlobalVariables.RealWeight);

                                        connection.Execute("sp_tblScanDataUpdateApproveBy", para, commandType: CommandType.StoredProcedure);

                                        approveType = "Actual deviation";
                                    }
                                }
                                //trường hợp in lại tem. chỉ cho những thằng Pass, hoặc false alarm mới được phép in lại
                                //else if (
                                //            (scandataDetail.ApprovedBy != Guid.Empty && scandataDetail.Pass == 0 && scandataDetail.ActualDeviationPairs == 0) 
                                //            || scandataDetail.Pass == 1
                                //        )//reprint
                                //{
                                //    var dialogResult = MessageBox.Show($"Bạn có chắc chắn xác nhận in lại tem cho thùng với thông tin sau:" +
                                //           $"{Environment.NewLine}{scandataDetail.IdLabel}|{scandataDetail.OcNo}|{scandataDetail.BoxNo}?"
                                //           , "CẢNH BÁO", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                                //    if (dialogResult == DialogResult.Yes)
                                //    {
                                //        GlobalVariables.Printing(scandataDetail.GrossWeight.ToString("#,#0.00")
                                //              , !string.IsNullOrEmpty(_scanData.IdLabel) ? _scanData.IdLabel : $"{_scanData.OcNo}|{_scanData.BoxNo}", true
                                //              , scandataDetail.CreatedDate.ToString("yyyy/MM/dd HH:mm:ss"));

                                //        approveType = "Reprint";
                                //    }
                                //}

                                #region Log
                                para = null;
                                para = new DynamicParameters();
                                para.Add("QrCode", _qrApproved);
                                para.Add("IdLabel", _scanData.IdLabel);
                                para.Add("OC", _scanData.OcNo);
                                para.Add("BoxNo", _scanData.BoxNo);
                                para.Add("GrossWeight", _scaleValue.ToString("#,#0.00"));
                                para.Add("Station", GlobalVariables.Station);
                                para.Add("QRLabel", _scanData.BarcodeString);
                                para.Add("ApproveType", approveType);

                                connection.Execute("sp_tblApprovedPrintLabelInsert", para, commandType: CommandType.StoredProcedure);
                                #endregion
                            }
                        }
                        else
                        {
                            MessageBox.Show("Bạn không có quyền thực hiện chức năng này", "THÔNG BÁO", MessageBoxButtons.OK
                                , MessageBoxIcon.Warning);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy thông tin.", "THÔNG BÁO", MessageBoxButtons.OK
                            , MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Close();
            }
        }

        private void FrmConfirmPrint_Load(object sender, EventArgs e)
        {
            //labIdLabel.Text = GlobalVariables.IdLabel;
            //labOcNo.Text = GlobalVariables.OcNo;
            //labBoxNo.Text = GlobalVariables.BoxNo;
            //labWeight.Text = (GlobalVariables.RealWeight / 1000).ToString("#,#0.00");

            txtQrCode.Focus();

            _timer.Interval = 200;
            _timer.Tick += _timer_Tick;
            _timer.Enabled = true;
            _timer.Start();
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            Timer t = (Timer)sender;
            t.Enabled = false;

            _scaleValue = GlobalVariables.RealWeight / 1000;

            if (labWeight.InvokeRequired)
            {
                labWeight.Text = _scaleValue.ToString("#,#0.00");
            }
            else
            {
                labWeight.Text = _scaleValue.ToString("#,#0.00");
            }

            t.Enabled = true;
        }
    }

    public class ConfirmPrintModel
    {
        public string IdLabel { get; set; } = null;
        public string OcNo { get; set; } = null;
        public string BoxNo { get; set; } = null;
        public double Weight { get; set; } = 0;
        public string CreatedDate { get; set; }
    }
}
