using DevExpress.XtraEditors;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private ProductInfoModel GetProductItemInfo(string productNumber, int specialCase, int? printing = null)
        {
            using (var db = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
            {
                var winlineQuery = db.TblWinlineProductsInfos.Where(v => v.Actived && v.ProductNumber == productNumber);

                var joined = specialCase == 0
                    ? from v in winlineQuery
                      from c in db.TblCoreDataCodeItemSizes
                          .Where(c => (c.CodeItemSize == v.CodeItemSize || c.CodeItemSize == v.ProductNumber)
                                      && c.Printing == v.Decoration
                                      && c.IsActived == true)
                          .DefaultIfEmpty()
                      select new { v, c }
                    : from v in winlineQuery
                      from c in db.TblCoreDataCodeItemSizes
                          .Where(c => (c.CodeItemSize == v.CodeItemSize || c.CodeItemSize == v.ProductNumber)
                                      && c.Printing == printing
                                      && c.IsActived == true)
                          .DefaultIfEmpty()
                      select new { v, c };

                return joined.Select(x => new ProductInfoModel
                {
                    CodeItemSize = x.c.CodeItemSize,
                    ProductNumber = x.v.ProductNumber,
                    ProductName = x.v.ProductName,
                    ProductCategory = x.v.ProductCategory,
                    Brand = x.v.Brand,
                    Decoration = x.v.Decoration,
                    MetalScan = x.c.MetalScan,
                    Printing = x.c.Printing,
                    MainProductNo = x.v.MainProductNo,
                    MainProductName = x.v.MainProductName,
                    Color = x.v.Color,
                    SizeName = x.v.SizeName,
                    ToolingNo = x.v.ToolingNo,
                    MainItemName = x.c.MainItemName,
                    AveWeight1Prs = x.c.AveWeight1Prs,
                    BoxQtyBx1 = x.c.BoxQtyBx1,
                    BoxQtyBx1A = x.c.BoxQtyBx1A,
                    BoxQtyBx2 = x.c.BoxQtyBx2,
                    BoxQtyBx3 = x.c.BoxQtyBx3,
                    BoxQtyBx4 = x.c.BoxQtyBx4,
                    BoxQtyBx5 = x.c.BoxQtyBx5,
                    BoxQtyBx6 = x.c.BoxQtyBx6,
                    BoxWeightBx1 = x.c.BoxWeightBx1,
                    BoxWeightBx1A = x.c.BoxWeightBx1A,
                    BoxWeightBx2 = x.c.BoxWeightBx2,
                    BoxWeightBx3 = x.c.BoxWeightBx3,
                    BoxWeightBx4 = x.c.BoxWeightBx4,
                    BoxWeightBx5 = x.c.BoxWeightBx5,
                    BoxWeightBx6 = x.c.BoxWeightBx6,
                    PartitionQty = x.c.PartitionQty,
                    PartitionQtyOfBX1A = x.c.PartitionQtyOfBX1A,
                    PartitionQtyOfBX2 = x.c.PartitionQtyOfBX2,
                    PartitionQtyOfBX3 = x.c.PartitionQtyOfBX3,
                    PlasticBag1Qty = x.c.PlasticBag1Qty,
                    PlasticBag2Qty = x.c.PlasticBag2Qty,
                    WrapSheetQty = x.c.WrapSheetQty,
                    FoamSheetQty = x.c.FoamSheetQty,
                    PartitionWeight = x.c.PartitionWeight,
                    PlasticBag1Weight = x.c.PlasticBag1Weight,
                    PlasticBag2Weight = x.c.PlasticBag2Weight,
                    WrapSheetWeight = x.c.WrapSheetWeight,
                    FoamSheetWeight = x.c.FoamSheetWeight,
                    PlasticBoxWeight = x.c.PlasticBoxWeight,
                    LowerToleranceOfCartonBox = x.c.LowerToleranceOfCartonBox,
                    UpperToleranceOfCartonBox = x.c.UpperToleranceOfCartonBox,
                    LowerToleranceOfPlasticBox = x.c.LowerToleranceOfPlasticBox,
                    UpperToleranceOfPlasticBox = x.c.UpperToleranceOfPlasticBox,
                    CreatedDate = x.v.CreatedDate,
                }).FirstOrDefault();
            }
        }

        private void FrmUpdateTolerance_Load(object sender, EventArgs e)
        {
            ItemInfo = GetProductItemInfo(ItemInfo.ProductNumber, 0);

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
                using (var db = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                {
                    if (ItemInfo.CodeItemSize != null)
                    {
                        var entity = db.TblCoreDataCodeItemSizes.FirstOrDefault(x => x.CodeItemSize == ItemInfo.CodeItemSize && x.Printing == ItemInfo.Printing);
                        if (entity != null)
                        {
                            entity.MainItemName = ItemInfo.MainItemName;
                            entity.MetalScan = ItemInfo.MetalScan;
                            entity.Color = ItemInfo.Color;
                            entity.Printing = ItemInfo.Printing;
                            entity.Size = ItemInfo.SizeName;
                            //entity.Date = _info.date;
                            entity.AveWeight1Prs = ItemInfo.AveWeight1Prs;
                            entity.BoxQtyBx1 = ItemInfo.BoxQtyBx1;
                            entity.BoxQtyBx1A = ItemInfo.BoxQtyBx1A;
                            entity.BoxQtyBx2 = ItemInfo.BoxQtyBx2;
                            entity.BoxQtyBx3 = ItemInfo.BoxQtyBx3;
                            entity.BoxQtyBx4 = ItemInfo.BoxQtyBx4;
                            entity.BoxWeightBx1 = ItemInfo.BoxWeightBx1;
                            entity.BoxWeightBx1A = ItemInfo.BoxWeightBx1A;
                            entity.BoxWeightBx2 = ItemInfo.BoxWeightBx2;
                            entity.BoxWeightBx3 = ItemInfo.BoxWeightBx3;
                            entity.BoxWeightBx4 = ItemInfo.BoxWeightBx4;
                            entity.PartitionQty = ItemInfo.PartitionQty;
                            entity.PartitionQtyOfBX1A = ItemInfo.PartitionQtyOfBX1A;
                            entity.PartitionQtyOfBX2 = ItemInfo.PartitionQtyOfBX2;
                            entity.PartitionQtyOfBX3 = ItemInfo.PartitionQtyOfBX3;
                            entity.PlasticBag1Qty = ItemInfo.PlasticBag1Qty;
                            entity.PlasticBag2Qty = ItemInfo.PlasticBag2Qty;
                            entity.WrapSheetQty = ItemInfo.WrapSheetQty;
                            entity.FoamSheetQty = ItemInfo.FoamSheetQty;
                            entity.PartitionWeight = ItemInfo.PartitionWeight;
                            entity.PlasticBag1Weight = ItemInfo.PlasticBag1Weight;
                            entity.PlasticBag2Weight = ItemInfo.PlasticBag2Weight;
                            entity.WrapSheetWeight = ItemInfo.WrapSheetWeight;
                            entity.FoamSheetWeight = ItemInfo.FoamSheetWeight;
                            entity.PlasticBoxWeight = ItemInfo.PlasticBoxWeight;
                            entity.LowerToleranceOfCartonBox = ItemInfo.LowerToleranceOfCartonBox;
                            entity.UpperToleranceOfCartonBox = ItemInfo.UpperToleranceOfCartonBox;
                            entity.LowerToleranceOfPlasticBox = ItemInfo.LowerToleranceOfPlasticBox;
                            entity.UpperToleranceOfPlasticBox = ItemInfo.UpperToleranceOfPlasticBox;

                            db.SaveChanges();
                        }
                    }
                    else//chua co coreData
                    {
                        var productItemArr = ItemInfo.ProductNumber.Split('-');
                        ItemInfo.CodeItemSize = $"{productItemArr[0]}-*-{productItemArr[2]}";

                        #region Update lại decoration trong bảng tblItemWinline
                        var winlineEntity = db.TblWinlineProductsInfos.FirstOrDefault(x => x.ProductNumber == ItemInfo.ProductNumber);
                        if (winlineEntity != null)
                        {
                            winlineEntity.CodeItemSize = ItemInfo.CodeItemSize;
                            db.SaveChanges();
                        }
                        #endregion

                        #region Insert vao bang tblCoreDataCodeItemSize. 2 dong Printing = 0 --- printing =1
                        var newEntityPrinting0 = new tblCoreDataCodeItemSize
                        {
                            Id = Guid.NewGuid(),
                            CodeItemSize = ItemInfo.CodeItemSize,
                            MainItemName = ItemInfo.MainItemName,
                            MetalScan = ItemInfo.MetalScan,
                            Color = ItemInfo.Color,
                            Printing = 0,
                            Date = DateTime.Now.Date,
                            Size = string.Empty,
                            AveWeight1Prs = ItemInfo.AveWeight1Prs,
                            BoxQtyBx1 = ItemInfo.BoxQtyBx1,
                            BoxQtyBx2 = ItemInfo.BoxQtyBx2,
                            BoxQtyBx3 = ItemInfo.BoxQtyBx3,
                            BoxQtyBx4 = ItemInfo.BoxQtyBx4,
                            BoxWeightBx1 = ItemInfo.BoxWeightBx1,
                            BoxWeightBx2 = ItemInfo.BoxWeightBx2,
                            BoxWeightBx3 = ItemInfo.BoxWeightBx3,
                            BoxWeightBx4 = ItemInfo.BoxWeightBx4,
                            PartitionQty = ItemInfo.PartitionQty,
                            PlasticBag1Qty = ItemInfo.PlasticBag1Qty,
                            PlasticBag2Qty = ItemInfo.PlasticBag2Qty,
                            WrapSheetQty = ItemInfo.WrapSheetQty,
                            FoamSheetQty = ItemInfo.FoamSheetQty,
                            PartitionWeight = ItemInfo.PartitionWeight,
                            PlasticBag1Weight = ItemInfo.PlasticBag1Weight,
                            PlasticBag2Weight = ItemInfo.PlasticBag2Weight,
                            WrapSheetWeight = ItemInfo.WrapSheetWeight,
                            FoamSheetWeight = ItemInfo.FoamSheetWeight,
                            PlasticBoxWeight = ItemInfo.PlasticBoxWeight,
                            LowerToleranceOfCartonBox = ItemInfo.LowerToleranceOfPlasticBox,
                            UpperToleranceOfCartonBox = ItemInfo.UpperToleranceOfCartonBox,
                            LowerToleranceOfPlasticBox = ItemInfo.LowerToleranceOfPlasticBox,
                            UpperToleranceOfPlasticBox = ItemInfo.UpperToleranceOfPlasticBox,
                        };
                        db.TblCoreDataCodeItemSizes.Add(newEntityPrinting0);

                        var newEntityPrinting1 = new tblCoreDataCodeItemSize
                        {
                            Id = Guid.NewGuid(),
                            CodeItemSize = ItemInfo.CodeItemSize,
                            MainItemName = ItemInfo.MainItemName,
                            MetalScan = ItemInfo.MetalScan,
                            Color = ItemInfo.Color,
                            Printing = 1,
                            Date = DateTime.Now.Date,
                            Size = string.Empty,
                            AveWeight1Prs = ItemInfo.AveWeight1Prs,
                            BoxQtyBx1 = ItemInfo.BoxQtyBx1,
                            BoxQtyBx2 = ItemInfo.BoxQtyBx2,
                            BoxQtyBx3 = ItemInfo.BoxQtyBx3,
                            BoxQtyBx4 = ItemInfo.BoxQtyBx4,
                            BoxWeightBx1 = ItemInfo.BoxWeightBx1,
                            BoxWeightBx2 = ItemInfo.BoxWeightBx2,
                            BoxWeightBx3 = ItemInfo.BoxWeightBx3,
                            BoxWeightBx4 = ItemInfo.BoxWeightBx4,
                            PartitionQty = ItemInfo.PartitionQty,
                            PlasticBag1Qty = ItemInfo.PlasticBag1Qty,
                            PlasticBag2Qty = ItemInfo.PlasticBag2Qty,
                            WrapSheetQty = ItemInfo.WrapSheetQty,
                            FoamSheetQty = ItemInfo.FoamSheetQty,
                            PartitionWeight = ItemInfo.PartitionWeight,
                            PlasticBag1Weight = ItemInfo.PlasticBag1Weight,
                            PlasticBag2Weight = ItemInfo.PlasticBag2Weight,
                            WrapSheetWeight = ItemInfo.WrapSheetWeight,
                            FoamSheetWeight = ItemInfo.FoamSheetWeight,
                            PlasticBoxWeight = ItemInfo.PlasticBoxWeight,
                            LowerToleranceOfCartonBox = ItemInfo.LowerToleranceOfPlasticBox,
                            UpperToleranceOfCartonBox = ItemInfo.UpperToleranceOfCartonBox,
                            LowerToleranceOfPlasticBox = ItemInfo.LowerToleranceOfPlasticBox,
                            UpperToleranceOfPlasticBox = ItemInfo.UpperToleranceOfPlasticBox,
                        };
                        db.TblCoreDataCodeItemSizes.Add(newEntityPrinting1);

                        db.SaveChanges();
                        #endregion
                    }

                    XtraMessageBox.Show("Update tolerance successfull.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    this.Close();
                }
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