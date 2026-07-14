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

        public ProductInfoModel ItemInfo = new ProductInfoModel();

        public frmUpdateTolerance()
        {
            InitializeComponent();

            Load += FrmUpdateTolerance_Load;
        }

        private void FrmUpdateTolerance_Load(object sender, EventArgs e)
        {
            var para = new DynamicParameters();
            para.Add("@ProductNumber", ItemInfo.ProductNumber);
            para.Add("@SpecialCase", 0);

            using (var connection = GlobalVariables.GetDbConnection())
            {
                ItemInfo = connection.Query<ProductInfoModel>("sp_vProductItemInfoGet", para, commandType: CommandType.StoredProcedure).FirstOrDefault();

                if (ItemInfo != null)
                {
                    labProductCode.Text = ItemInfo.ProductNumber;
                    labCodeItemSize.Text = ItemInfo.CodeItemSize;
                    labProductName.Text = ItemInfo.ProductName;
                    labSize.Text = ItemInfo.SizeName;
                    txtAveWeight.Text = ItemInfo.AveWeight1Prs.ToString();
                    txtBoxQtyBx1.Text = ItemInfo.BoxQtyBx1.ToString();
                    txtBoxQtyBx1A.Text = ItemInfo.BoxQtyBx1A.ToString();
                    txtBoxQtyBx2.Text = ItemInfo.BoxQtyBx2.ToString();
                    txtBoxQtyBx3.Text = ItemInfo.BoxQtyBx3.ToString();
                    txtBoxQtyBx4.Text = ItemInfo.BoxQtyBx4.ToString();
                    txtBoxWeightBx1.Text = ItemInfo.BoxWeightBx1.ToString();
                    txtBoxWeightBx1A.Text = ItemInfo.BoxWeightBx1A.ToString();
                    txtBoxWeightBx2.Text = ItemInfo.BoxWeightBx2.ToString();
                    txtBoxWeightBx3.Text = ItemInfo.BoxWeightBx3.ToString();
                    txtBoxWeightBx4.Text = ItemInfo.BoxWeightBx4.ToString();
                    txtPartitionQty.Text = ItemInfo.PartitionQty.ToString();
                    txtPartitionQtyBx1A.Text = ItemInfo.PartitionQtyOfBX1A.ToString();
                    txtPartitionQtyBx2.Text = ItemInfo.PartitionQtyOfBX2.ToString();
                    txtPartitionQtyBx3.Text = ItemInfo.PartitionQtyOfBX3.ToString();
                    txtPartitionWeight.Text = ItemInfo.PartitionWeight.ToString();
                    txtPlasicBag1Qty.Text = ItemInfo.PlasticBag1Qty.ToString();
                    txtPlasicBag1Weight.Text = ItemInfo.PlasticBag1Weight.ToString();
                    txtWrapSheetQty.Text = ItemInfo.WrapSheetQty.ToString();
                    txtWrapSheetWeight.Text = ItemInfo.WrapSheetWeight.ToString();
                    txtPlasicBoxWeight.Text = ItemInfo.PlasticBoxWeight.ToString();
                    txtLowerToleranceCarton.Text = ItemInfo.LowerToleranceOfCartonBox.ToString();
                    txtUpperToleranceCarton.Text = ItemInfo.UpperToleranceOfCartonBox.ToString();
                    txtLowerTolerancePlastic.Text = ItemInfo.LowerToleranceOfPlasticBox.ToString();
                    txtUpperTolerancePlastic.Text = ItemInfo.UpperToleranceOfPlasticBox.ToString();
                    txtPlasicBag2Qty.Text = ItemInfo.PlasticBag2Qty.ToString();
                    txtPlasicBag2Weight.Text = ItemInfo.PlasticBag2Weight.ToString();
                    txtFoarmSheetQty.Text = ItemInfo.FoamSheetQty.ToString();
                    txtFoarmSheetWeight.Text = ItemInfo.FoamSheetWeight.ToString();

                    _ = ItemInfo.Decoration == 0 ? ckDecorarion.Checked = false : ckDecorarion.Checked = true;
                    _ = ItemInfo.MetalScan == 0 ? ckMetalScan.Checked = false : ckMetalScan.Checked = true;
                }
            }

            #region register events txtChange
            this.txtAveWeight.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.AveWeight1Prs = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtPlasicBoxWeight.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.PlasticBoxWeight = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtBoxQtyBx1.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.BoxQtyBx1 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtBoxQtyBx1A.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.BoxQtyBx1A = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtBoxQtyBx2.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.BoxQtyBx2 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtBoxQtyBx3.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.BoxQtyBx3 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtBoxQtyBx4.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.BoxQtyBx4 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtBoxWeightBx1.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.BoxWeightBx1 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtBoxWeightBx1A.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.BoxWeightBx1A = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtBoxWeightBx2.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.BoxWeightBx2 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtBoxWeightBx3.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.BoxWeightBx3 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtBoxWeightBx4.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.BoxWeightBx4 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtPartitionQty.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.PartitionQty = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtPartitionQtyBx1A.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.PartitionQtyOfBX1A = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtPartitionQtyBx2.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.PartitionQtyOfBX2 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtPartitionQtyBx3.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.PartitionQtyOfBX3 = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtPlasicBag1Qty.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.PlasticBag1Qty = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtWrapSheetQty.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.WrapSheetQty = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtPartitionWeight.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.PartitionWeight = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtPlasicBag1Weight.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.PlasticBag1Weight = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtWrapSheetWeight.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.WrapSheetWeight = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtLowerToleranceCarton.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.LowerToleranceOfCartonBox = double.TryParse(t.Text, out double value) ? value : 0;
                }
            }; this.txtUpperToleranceCarton.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.UpperToleranceOfCartonBox = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtLowerTolerancePlastic.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.LowerToleranceOfPlasticBox = double.TryParse(t.Text, out double value) ? value : 0;
                }
            }; this.txtUpperTolerancePlastic.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.UpperToleranceOfPlasticBox = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtPlasicBag2Qty.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.PlasticBag2Qty = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtPlasicBag2Weight.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.PlasticBag2Weight = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            this.txtFoarmSheetQty.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.FoamSheetQty = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };
            this.txtFoarmSheetWeight.TextChanged += (s, o) =>
            {
                TextEdit t = (TextEdit)s;
                if (!string.IsNullOrEmpty(t.Text))
                {
                    ItemInfo.FoamSheetWeight = double.TryParse(t.Text, out double value) ? value : 0;
                }
            };

            //this.ckDecorarion.CheckedChanged += (s, o) =>
            //{
            //    CheckEdit c = (CheckEdit)s;
            //    _ = c.Checked ? ItemInfo.Decoration = ItemInfo.Printing = 1 : ItemInfo.Decoration = ItemInfo.Printing = 0;
            //};

            //this.ckMetalScan.CheckedChanged += (s, o) =>
            //{
            //    CheckEdit c = (CheckEdit)s;
            //    _ = c.Checked ? ItemInfo.MetalScan = 1 : ItemInfo.MetalScan = 0;
            //};
            #endregion
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var para = new DynamicParameters();
                    if (ItemInfo.CodeItemSize != null)
                    {
                        para.Add("@CodeItemSize", ItemInfo.CodeItemSize);
                        para.Add("@MainItemName", ItemInfo.MainItemName);
                        para.Add("@MetalScan", ItemInfo.MetalScan);
                        para.Add("@Color", ItemInfo.Color);
                        para.Add("@Printing", ItemInfo.Printing);
                        para.Add("@Size", ItemInfo.SizeName);
                        //para.Add("@Date", _info.date);
                        para.Add("@AveWeight1Prs", ItemInfo.AveWeight1Prs);
                        para.Add("@BoxQtyBx1", ItemInfo.BoxQtyBx1);
                        para.Add("@BoxQtyBx1A", ItemInfo.BoxQtyBx1A);
                        para.Add("@BoxQtyBx2", ItemInfo.BoxQtyBx2);
                        para.Add("@BoxQtyBx3", ItemInfo.BoxQtyBx3);
                        para.Add("@BoxQtyBx4", ItemInfo.BoxQtyBx4);
                        para.Add("@BoxWeightBx1", ItemInfo.BoxWeightBx1);
                        para.Add("@BoxWeightBx1A", ItemInfo.BoxWeightBx1A);
                        para.Add("@BoxWeightBx2", ItemInfo.BoxWeightBx2);
                        para.Add("@BoxWeightBx3", ItemInfo.BoxWeightBx3);
                        para.Add("@BoxWeightBx4", ItemInfo.BoxWeightBx4);
                        para.Add("@PartitionQty", ItemInfo.PartitionQty);
                        para.Add("@PartitionQtyBX1A", ItemInfo.PartitionQtyOfBX1A);
                        para.Add("@PartitionQtyBX2", ItemInfo.PartitionQtyOfBX2);
                        para.Add("@PartitionQtyBX3", ItemInfo.PartitionQtyOfBX3);
                        para.Add("@PlasticBag1Qty", ItemInfo.PlasticBag1Qty);
                        para.Add("@PlasticBag2Qty", ItemInfo.PlasticBag2Qty);
                        para.Add("@WrapSheetQty", ItemInfo.WrapSheetQty);
                        para.Add("@FoamSheetQty", ItemInfo.FoamSheetQty);
                        para.Add("@PartitionWeight", ItemInfo.PartitionWeight);
                        para.Add("@PlasticBag1Weight", ItemInfo.PlasticBag1Weight);
                        para.Add("@PlasticBag2Weight", ItemInfo.PlasticBag2Weight);
                        para.Add("@WrapSheetWeight", ItemInfo.WrapSheetWeight);
                        para.Add("@FoamSheetWeight", ItemInfo.FoamSheetWeight);
                        para.Add("@PlasticBoxWeight", ItemInfo.PlasticBoxWeight);
                        para.Add("@LowerToleranceOfCartonBox", ItemInfo.LowerToleranceOfCartonBox);
                        para.Add("@UpperToleranceOfCartonBox", ItemInfo.UpperToleranceOfCartonBox);
                        para.Add("@LowerToleranceOfPlasticBox", ItemInfo.LowerToleranceOfPlasticBox);
                        para.Add("@UpperToleranceOfPlasticBox", ItemInfo.UpperToleranceOfPlasticBox);

                        var res = connection.Execute("sp_tblCoreDataCodeItemSizeUpdate", para, commandType: CommandType.StoredProcedure);
                    }
                    else//chua co coreData
                    {
                        var productItemArr = ItemInfo.ProductNumber.Split('-');
                        ItemInfo.CodeItemSize = $"{productItemArr[0]}-*-{productItemArr[2]}";

                        #region Update lại decoration trong bảng tblItemWinline
                        para = null;
                        para = new DynamicParameters();
                        para.Add("ProductItemCode", ItemInfo.ProductNumber);
                        para.Add("CodeItemSize", ItemInfo.CodeItemSize);

                        int res = connection.Execute("sp_tblWinlineProductsInfoUpdateCodeItemSize", para, commandType: CommandType.StoredProcedure);
                        #endregion

                        #region Insert vao bang sp_tblCoreDataCodeitemSizeInsert. 2 dong Printing = --- printing =1
                        para = null;
                        para = new DynamicParameters();
                        para.Add("@CodeItemSize", ItemInfo.CodeItemSize);
                        para.Add("@MainItemName", ItemInfo.MainItemName);
                        para.Add("@MetalScan", ItemInfo.MetalScan);
                        para.Add("@Color", ItemInfo.Color);
                        para.Add("@Printing", 0);
                        para.Add("@Date", DateTime.Now.Date);
                        para.Add("@Size", string.Empty);
                        para.Add("@AveWeight1Prs", ItemInfo.AveWeight1Prs);
                        para.Add("@BoxQtyBx1", ItemInfo.BoxQtyBx1);
                        para.Add("@BoxQtyBx2", ItemInfo.BoxQtyBx2);
                        para.Add("@BoxQtyBx3", ItemInfo.BoxQtyBx3);
                        para.Add("@BoxQtyBx4", ItemInfo.BoxQtyBx4);
                        para.Add("@BoxWeightBx1", ItemInfo.BoxWeightBx1);
                        para.Add("@BoxWeightBx2", ItemInfo.BoxWeightBx2);
                        para.Add("@BoxWeightBx3", ItemInfo.BoxWeightBx3);
                        para.Add("@BoxWeightBx4", ItemInfo.BoxWeightBx4);
                        para.Add("@PartitionQty", ItemInfo.PartitionQty);
                        para.Add("@PlasticBag1Qty", ItemInfo.PlasticBag1Qty);
                        para.Add("@PlasticBag2Qty", ItemInfo.PlasticBag2Qty);
                        para.Add("@WrapSheetQty", ItemInfo.WrapSheetQty);
                        para.Add("@FoamSheetQty", ItemInfo.FoamSheetQty);
                        para.Add("@PartitionWeight", ItemInfo.PartitionWeight);
                        para.Add("@PlasticBag1Weight", ItemInfo.PlasticBag1Weight);
                        para.Add("@PlasticBag2Weight", ItemInfo.PlasticBag2Weight);
                        para.Add("@WrapSheetWeight", ItemInfo.WrapSheetWeight);
                        para.Add("@FoamSheetWeight", ItemInfo.FoamSheetWeight);
                        para.Add("@PlasticBoxWeight", ItemInfo.PlasticBoxWeight);
                        para.Add("@LowerToleranceOfCartonBox", ItemInfo.LowerToleranceOfPlasticBox);
                        para.Add("@UpperToleranceOfCartonBox", ItemInfo.UpperToleranceOfCartonBox);
                        para.Add("@LowerToleranceOfPlasticBox", ItemInfo.LowerToleranceOfPlasticBox);
                        para.Add("@UpperToleranceOfPlasticBox", ItemInfo.UpperToleranceOfPlasticBox);
                        connection.Execute("sp_tblCoreDataCodeitemSizeInsert", para, commandType: CommandType.StoredProcedure);

                        para = null;
                        para = new DynamicParameters();
                        para.Add("@CodeItemSize", ItemInfo.CodeItemSize);
                        para.Add("@MainItemName", ItemInfo.MainItemName);
                        para.Add("@MetalScan", ItemInfo.MetalScan);
                        para.Add("@Color", ItemInfo.Color);
                        para.Add("@Printing", 1);
                        para.Add("@Date", DateTime.Now.Date);
                        para.Add("@Size", string.Empty);
                        para.Add("@AveWeight1Prs", ItemInfo.AveWeight1Prs);
                        para.Add("@BoxQtyBx1", ItemInfo.BoxQtyBx1);
                        para.Add("@BoxQtyBx2", ItemInfo.BoxQtyBx2);
                        para.Add("@BoxQtyBx3", ItemInfo.BoxQtyBx3);
                        para.Add("@BoxQtyBx4", ItemInfo.BoxQtyBx4);
                        para.Add("@BoxWeightBx1", ItemInfo.BoxWeightBx1);
                        para.Add("@BoxWeightBx2", ItemInfo.BoxWeightBx2);
                        para.Add("@BoxWeightBx3", ItemInfo.BoxWeightBx3);
                        para.Add("@BoxWeightBx4", ItemInfo.BoxWeightBx4);
                        para.Add("@PartitionQty", ItemInfo.PartitionQty);
                        para.Add("@PlasticBag1Qty", ItemInfo.PlasticBag1Qty);
                        para.Add("@PlasticBag2Qty", ItemInfo.PlasticBag2Qty);
                        para.Add("@WrapSheetQty", ItemInfo.WrapSheetQty);
                        para.Add("@FoamSheetQty", ItemInfo.FoamSheetQty);
                        para.Add("@PartitionWeight", ItemInfo.PartitionWeight);
                        para.Add("@PlasticBag1Weight", ItemInfo.PlasticBag1Weight);
                        para.Add("@PlasticBag2Weight", ItemInfo.PlasticBag2Weight);
                        para.Add("@WrapSheetWeight", ItemInfo.WrapSheetWeight);
                        para.Add("@FoamSheetWeight", ItemInfo.FoamSheetWeight);
                        para.Add("@PlasticBoxWeight", ItemInfo.PlasticBoxWeight);
                        para.Add("@LowerToleranceOfCartonBox", ItemInfo.LowerToleranceOfPlasticBox);
                        para.Add("@UpperToleranceOfCartonBox", ItemInfo.UpperToleranceOfCartonBox);
                        para.Add("@LowerToleranceOfPlasticBox", ItemInfo.LowerToleranceOfPlasticBox);
                        para.Add("@UpperToleranceOfPlasticBox", ItemInfo.UpperToleranceOfPlasticBox);
                        connection.Execute("sp_tblCoreDataCodeitemSizeInsert", para, commandType: CommandType.StoredProcedure);
                        #endregion
                    }

                    XtraMessageBox.Show("Update tolerance successfull.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.Close();
                }

                //using var dbContext = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString);
                //var mesoinfo = dbContext.Database.SqlQuery<MesoInfoModel>($"sp_GetMesoInfo").AsEnumerable().FirstOrDefault();

                //if (ItemInfo.CodeItemSize != null)
                //{

                //}
                //else
                //{

                //}
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Update tolerance Fail.{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log.Error(ex, "Update tolerance Fail exception.");
            }
            finally
            {
                GlobalVariables.MyEvent.RefreshStatus = true;
            }
        }
    }
}