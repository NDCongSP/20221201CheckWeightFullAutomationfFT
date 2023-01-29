using System;
using System.Collections.Generic;
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
        public int BoxQtyBx1 { get; set; }
        public int BoxQtyBx2 { get; set; }
        public int BoxQtyBx3 { get; set; }
        public int BoxQtyBx4 { get; set; }
        public double BoxWeightBx1 { get; set; }
        public double BoxWeightBx2 { get; set; }
        public double BoxWeightBx3 { get; set; }
        public double BoxWeightBx4 { get; set; }
        public int PartitionQty { get; set; }
        public int PlasicBag1Qty { get; set; }
        public int PlasicBag2Qty { get; set; }
        public int WrapSheetQty { get; set; }
        public int  FoamSheetQty { get; set; }
        public double PartitionWeight { get; set; } = 60;
        public double PlasicBag1Weight { get; set; }
        public double PlasicBag2Weight { get; set; }
        public double WrapSheetWeight { get; set; }
        public double FoamSheetWeight { get; set; }
        public double PlasicBoxWeight { get; set; } = 1210;
        public double Tolerance { get; set; } = 5;
        public double ToleranceAfterPrint { get; set; } = 7;
        public DateTime CreatedDate { get; set; }
        public bool IsActived { get; set; }
    }
}
