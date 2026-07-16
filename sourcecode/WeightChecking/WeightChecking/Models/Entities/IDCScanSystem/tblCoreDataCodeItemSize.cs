using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    [Table("tblCoreDataCodeItemSize")]
    public class tblCoreDataCodeItemSize
    {
        [Key]
        public Guid Id { get; set; }

        public string CodeItemSize { get; set; }

        public string MainItemName { get; set; }

        public int? MetalScan { get; set; }

        public string Color { get; set; }

        public int? Printing { get; set; }

        public DateTime? Date { get; set; }

        public string Size { get; set; }

        public double? AveWeight1Prs { get; set; }

        public double? BoxQtyBx1 { get; set; }

        public double? BoxQtyBx1A { get; set; }

        public double? BoxQtyBx2 { get; set; }

        public double? BoxQtyBx3 { get; set; }

        public double? BoxQtyBx4 { get; set; }

        public double? BoxQtyBx5 { get; set; }

        public double? BoxQtyBx6 { get; set; }

        public double? BoxWeightBx1 { get; set; }

        public double? BoxWeightBx1A { get; set; }

        public double? BoxWeightBx2 { get; set; }

        public double? BoxWeightBx3 { get; set; }

        public double? BoxWeightBx4 { get; set; }

        public double? BoxWeightBx5 { get; set; }

        public double? BoxWeightBx6 { get; set; }

        /// <summary>
        /// Non-HC. QtyOfBox/PartitionQty = qty of partition.
        /// </summary>
        public double? PartitionQty { get; set; }

        public double? PartitionQtyOfBX1A { get; set; }

        public double? PartitionQtyOfBX2 { get; set; }

        public double? PartitionQtyOfBX3 { get; set; }

        public double? PlasticBag1Qty { get; set; }

        public double? PlasticBag2Qty { get; set; }

        public double? WrapSheetQty { get; set; }

        public double? FoamSheetQty { get; set; }

        public double? PartitionWeight { get; set; }

        public double? PlasticBag1Weight { get; set; }

        public double? PlasticBag2Weight { get; set; }

        public double? WrapSheetWeight { get; set; }

        public double? FoamSheetWeight { get; set; }

        public double? PlasticBoxWeight { get; set; }

        public double? LowerToleranceOfCartonBox { get; set; }

        public double? UpperToleranceOfCartonBox { get; set; }

        public double? LowerToleranceOfPlasticBox { get; set; }

        public double? UpperToleranceOfPlasticBox { get; set; }

        public DateTime? CreatedDate { get; set; }

        public bool? IsActived { get; set; }
    }
}
