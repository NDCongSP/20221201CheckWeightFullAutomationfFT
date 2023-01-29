using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public class WinlineDataModel
    {
        public string CodeItemSize { get; set; }
        public string ProductNumber { get; set; }
        public string ProductName { get; set; }
        public int ProductCategory { get; set; } //=1 là heelCounter
        public string Brand { get; set; }
        public double? Decoration { get; set; }
        public string MainProductNo { get; set; }
        public string MainProductName { get; set; }
        public string Color { get; set; }
        public int SizeCode { get; set; }
        public string SizeName { get; set; }
        public double? Weight { get; set; }
        public double? LeftWeight { get; set; }
        public double? RightWeight { get; set; }
        public string BoxType { get; set; }
        public string ToolingNo { get; set; }
        public string PackingBoxType { get; set; }
        public string CustomeUsePb { get; set; }
    }
}
