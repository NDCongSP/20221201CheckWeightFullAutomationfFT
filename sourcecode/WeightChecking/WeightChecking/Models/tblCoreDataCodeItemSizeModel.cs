using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public class tblCoreDataCodeItemSizeModel
    {
        public Guid Id { get; set; }
        public string CodeItemSize { get; set; }
        public string MainItemName { get; set; }
        public int MetalScan { get; set; } //1 co; 0 ko
        public string Color { get; set; } = null;
        public int Printing { get; set; }//1 co; 0 ko
        public string Date { get; set; }
        public string Size { get; set; }
        public double AveWeight1Prs { get; set; }
        public double BoxQtyBx1 { get; set; }
        public double BoxQtyBx2 { get; set; }
        public double BoxQtyBx3 { get; set; }
        public double BoxQtyBx4 { get; set; }
        public double BoxWeightBx1 { get; set; }
        public double BoxWeightBx2 { get; set; }
        public double BoxWeightBx3 { get; set; }
        public double BoxWeightBx4 { get; set; }
        public double PartitionQty { get; set; }
        [DisplayName("PlasticBag1Qty")]
        public double PlasicBag1Qty { get; set; }
        [DisplayName("PlasticBag2Qty")]
        public double PlasicBag2Qty { get; set; }
        public double WrapSheetQty { get; set; }
        public double FoamSheetQty { get; set; }
        public double PartitionWeight { get; set; } = 60;
        [DisplayName("PlasticBag1Weight")]
        public double PlasicBag1Weight { get; set; }
        [DisplayName("PlasticBag2Weight")]
        public double PlasicBag2Weight { get; set; }
        public double WrapSheetWeight { get; set; }
        public double FoamSheetWeight { get; set; }
        [DisplayName("PlasticBoxWeight")]
        public double PlasicBoxWeight { get; set; } = 1210;
        public double LowerToleranceOfCartonBox { get; set; } = 0;
        public double UpperToleranceOfCartonBox { get; set; } = 0;
        public double LowerToleranceOfPlasticBox { get; set; } = 0;
        public double UpperToleranceOfPlasticBox { get; set; } = 0;
        public DateTime CreatedDate { get; set; }
        [Browsable(false)]
        public bool IsActived { get; set; }
    }
}
