using Dapper;
using DevExpress.XtraEditors;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeightChecking
{
    public partial class frmUpdateTolerance : DevExpress.XtraEditors.XtraForm
    {
        public string ProductNumber { get; set; }
        public string Code_infoSize { get; set; }

        public ProductInfoModel _info = new ProductInfoModel();

        public frmUpdateTolerance()
        {
            InitializeComponent();

            Load += FrmUpdateTolerance_Load;
        }

        private void FrmUpdateTolerance_Load(object sender, EventArgs e)
        {
            var para = new DynamicParameters();
            para.Add("@ProductNumber",_info.ProductNumber);

            using (var connection=GlobalVariables.GetDbConnection())
            {
                _info = connection.Query<ProductInfoModel>("sp_vProductItemInfoGet", para,commandType:CommandType.StoredProcedure).FirstOrDefault();

                if (_info!=null)
                {
                    labProductCode.Text = _info.ProductNumber;
                    labCodeItemSize.Text = _info.CodeItemSize;
                    labProductName.Text = _info.ProductName;
                    labSize.Text = _info.SizeName;
                    txtAveWeight.Text = _info.AveWeight1Prs.ToString();
                    txtBoxQtyBx1.Text = _info.BoxQtyBx1.ToString();
                    txtBoxQtyBx2.Text = _info.BoxQtyBx2.ToString();
                    txtBoxQtyBx3.Text = _info.BoxQtyBx3.ToString();
                    txtBoxQtyBx4.Text = _info.BoxQtyBx4.ToString();
                    txtBoxWeightBx1.Text = _info.BoxWeightBx1.ToString();
                    txtBoxWeightBx2.Text = _info.BoxWeightBx2.ToString();
                    txtBoxWeightBx3.Text = _info.BoxWeightBx3.ToString();
                    txtBoxWeightBx4.Text = _info.BoxWeightBx4.ToString();
                    txtPartitionQty.Text = _info.PartitionQty.ToString();
                    txtPartitionWeight.Text = _info.PartitionWeight.ToString();
                    txtPlasicQty.Text = _info.PlasicBag1Qty.ToString();
                    txtPlasicWeight.Text = _info.PlasicBag1Weight.ToString();
                    txtWrapSheetQty.Text = _info.WrapSheetQty.ToString();
                    txtWrapSheetWeight.Text = _info.WrapSheetWeight.ToString();
                    txtPlasicBoxWeight.Text = _info.PlasicBoxWeight.ToString();
                    txtTolerance.Text = _info.Tolerance.ToString();
                    txtToleranceAfterPrint.Text = _info.ToleranceAfterPrint.ToString();
                }
            }

            #region register events txtChange
            this.txtAveWeight.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.AveWeight1Prs = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtPlasicBoxWeight.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.PlasicBoxWeight = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtBoxQtyBx1.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.BoxQtyBx1 = int.TryParse(t.Text, out int value) ? value : 0;
                }
            };
            this.txtBoxQtyBx2.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.BoxQtyBx2 = int.TryParse(t.Text, out int value) ? value : 0;
                }
            };
            this.txtBoxQtyBx3.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.BoxQtyBx3 = int.TryParse(t.Text, out int value) ? value : 0;
                }
            };
            this.txtBoxQtyBx4.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.BoxQtyBx4 = int.TryParse(t.Text, out int value) ? value : 0;
                }
            };

            this.txtBoxWeightBx1.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.BoxWeightBx1 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtBoxWeightBx2.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.BoxWeightBx2 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtBoxWeightBx3.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.BoxWeightBx3 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtBoxWeightBx4.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.BoxWeightBx4 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtPartitionQty.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.PartitionQty = int.TryParse(t.Text, out int value) ? value : 0;
                }
            };
            this.txtPlasicQty.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.PlasicBag1Qty = int.TryParse(t.Text, out int value) ? value : 0;
                }
            };
            this.txtWrapSheetQty.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.WrapSheetQty = int.TryParse(t.Text, out int value) ? value : 0;
                }
            };

            this.txtPartitionWeight.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.PartitionWeight = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtPlasicWeight.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.PlasicBag1Weight = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtWrapSheetWeight.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.WrapSheetWeight = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtTolerance.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.Tolerance = double.TryParse(t.Text, out double value) ? value : 0;
                }
            }; this.txtToleranceAfterPrint.TextChanged += (s, o) => {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    _info.ToleranceAfterPrint = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            #endregion
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var para = new DynamicParameters();
                    para.Add("@CodeItemSize", _info.CodeItemSize);
                    para.Add("@MainItemName", _info.MainItemName);
                    para.Add("@MetalScan", _info.MetalScan);
                    para.Add("@Color", _info.Color);
                    para.Add("@Printing", _info.Printing);
                    para.Add("@Size", _info.SizeName);
                    //para.Add("@Date", _info.date);
                    para.Add("@AveWeight1Prs", _info.AveWeight1Prs);
                    para.Add("@BoxQtyBx1", _info.BoxQtyBx1);
                    para.Add("@BoxQtyBx2", _info.BoxQtyBx2);
                    para.Add("@BoxQtyBx3", _info.BoxQtyBx3);
                    para.Add("@BoxQtyBx4", _info.BoxQtyBx4);
                    para.Add("@BoxWeightBx1", _info.BoxWeightBx1);
                    para.Add("@BoxWeightBx2", _info.BoxWeightBx2);
                    para.Add("@BoxWeightBx3", _info.BoxWeightBx3);
                    para.Add("@BoxWeightBx4", _info.BoxWeightBx4);
                    para.Add("@PartitionQty", _info.PartitionQty);
                    para.Add("@PlasicBagQty", _info.PlasicBag1Qty);
                    para.Add("@WrapSheetQty", _info.WrapSheetQty);
                    para.Add("@PartitionWeight", _info.PartitionWeight);
                    para.Add("@PlasicBagWeight", _info.PlasicBag1Weight);
                    para.Add("@WrapSheetWeight", _info.WrapSheetWeight);
                    para.Add("@PlasicBoxWeight", _info.PlasicBoxWeight);
                    para.Add("@Tolerance", _info.Tolerance);
                    para.Add("@ToleranceAfterPrint", _info.ToleranceAfterPrint);

                    var res = connection.Execute("sp_tblCoreDataCodeItemSizeUpdate", para, commandType: CommandType.StoredProcedure);

                    XtraMessageBox.Show("Update tolerance successfull.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.Close();
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Update tolerance Fail.{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.Error(ex,"Update tolerance Fail exception.");
            }
            finally
            {
                GlobalVariables.MyEvent.RefreshStatus = true;
            }
        }
    }
}