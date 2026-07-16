using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public class ProductInfoModel
    {
        public string? CodeItemSize { get; set; } = string.Empty;
        public string? ProductNumber { get; set; } = string.Empty;
        public string? ProductName { get; set; } = string.Empty;
        public int? ProductCategory { get; set; } = 0;
        public string? Brand { get; set; } = string.Empty;
        public int? Decoration { get; set; } = 0;
        public int? MetalScan { get; set; } = 0;
        public int? Printing { get; set; } = 0;//1 co; 0 ko
        public string? MainProductNo { get; set; } = string.Empty;
        public string? MainProductName { get; set; } = string.Empty;
        public string? Color { get; set; } = string.Empty;
        public string? SizeName { get; set; } = string.Empty;
        public string? ToolingNo { get; set; } = string.Empty;
        public string? MainItemName { get; set; } = string.Empty;
        public double? AveWeight1Prs { get; set; } = 0;
        public double? BoxQtyBx1 { get; set; } = 0;
        public double? BoxQtyBx1A { get; set; } = 0;
        public double? BoxQtyBx2 { get; set; } = 0;
        public double? BoxQtyBx3 { get; set; } = 0;
        public double? BoxQtyBx4 { get; set; } = 0;
        public double? BoxQtyBx5 { get; set; } = 0;
        public double? BoxQtyBx6 { get; set; } = 0;
        public double? BoxWeightBx1 { get; set; } = 0;
        public double? BoxWeightBx1A { get; set; } = 0;
        public double? BoxWeightBx2 { get; set; } = 0;
        public double? BoxWeightBx3 { get; set; } = 0;
        public double? BoxWeightBx4 { get; set; } = 0;
        public double? BoxWeightBx5 { get; set; } = 0;
        public double? BoxWeightBx6 { get; set; } = 0;
        public double? PartitionQty { get; set; } = 0;
        public double? PartitionQtyOfBX1A { get; set; } = 0;
        public double? PartitionQtyOfBX2 { get; set; } = 0;
        public double? PartitionQtyOfBX3 { get; set; } = 0;
        public double? PlasticBag1Qty { get; set; } = 0;
        public double? PlasticBag2Qty { get; set; } = 0;
        public double? WrapSheetQty { get; set; } = 0;
        public double? FoamSheetQty { get; set; } = 0;
        public double? PartitionWeight { get; set; } = 0;
        public double? PlasticBag1Weight { get; set; } = 0;
        public double? PlasticBag2Weight { get; set; } = 0;
        public double? WrapSheetWeight { get; set; } = 0;
        public double? FoamSheetWeight { get; set; } = 0;
        public double? PlasticBoxWeight { get; set; } = 0;
        [DisplayName("LowerToleranceOfTotalCartonBox")]
        public double? LowerToleranceOfCartonBox { get; set; } = 0;
        [DisplayName("UpperToleranceOfTotalCartonBox")]
        public double? UpperToleranceOfCartonBox { get; set; } = 0;
        [DisplayName("LowerToleranceOfTotalPlasticBox")]
        public double? LowerToleranceOfPlasticBox { get; set; } = 0;
        [DisplayName("UpperToleranceOfTotalPlasticBox")]
        public double? UpperToleranceOfPlasticBox { get; set; } = 0;
        public DateTime? CreatedDate { get; set; }//Thời gian item được get từ WL về
    }
}