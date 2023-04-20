using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public class tblScanDataModel
    {
        [Browsable(false)]
        public Guid Id { get; set; }
        public string BarcodeString { get; set; } = null;
        public string IdLabel { get; set; } = null;
        public string OcNo { get; set; } = null;
        public string ProductNumber { get; set; } = null;
        public string ProductName { get; set; } = null;
        public int Quantity { get; set; } = 0;
        public string LinePosNo { get; set; } = null;
        public string Unit { get; set; } = "Prs";
        public string BoxNo { get; set; } = null;
        public string CustomerNo { get; set; } = null;
        public LocationEnum Location { get; set; }
        public string BoxPosNo { get; set; } = null;
        public string Note { get; set; } = null;
        public string Brand { get; set; } = null;
        public int Decoration { get; set; }
        public int MetalScan { get; set; }
        public int ActualMetalScan { get; set; } = 0;
        public double AveWeight1Prs { get; set; }
        public double StdNetWeight { get; set; }

        //public double Tolerance { get; set; }
        public double LowerTolerance { get; set; }
        public double UpperTolerance { get; set; }

        public double BoxWeight { get; set; }
        public double PackageWeight { get; set; }
        public double StdGrossWeight { get; set; }
        public double GrossWeight { get; set; }
        public double NetWeight { get; set; }
        public double Deviation { get; set; }
        public int Pass { get; set; }
        //Báo trạng thái: 0- thùng fail; 1- chờ đi sơn; 2- Done hàng FG qua kho Kerry.
        //Ở trạm IDC check nêu hàng noPrinting thì set =2. nếu printing set =1.
        //Khi hàng đi sơn về, vào trạm check afterPrinting, quét OK set =2
        public int Status { get; set; }

        public DateTime CreatedDate { get; set; }
        [Browsable(false)]
        public int Actived { get; set; }
        [Browsable(false)]
        public Guid CreatedBy { get; set; }
        public string UserName { get; set; }
        public double CalculatedPairs { get; set; }
        public double DeviationPairs { get; set; } = 0;//thể hiện số pairs bị thiếu.
        public StationEnum Station { get; set; }
        [Browsable(false)]
        public Guid ApprovedBy { get; set; } = Guid.Empty;
        public string ApprovedName { get; set; }
        public double ActualDeviationPairs { get; set; } = 0;

        /// <summary>
        /// Tỉ lệ khối lượng calculate deviation / standar Gross weight.
        /// % = [|CalculateDeviation (prs)| * AveWeight/Prs (g)]/SdtGrossWeight (g).
        /// </summary>
        public double RatioFailWeight
        {
            get { return Math.Round((DeviationPairs * AveWeight1Prs) / StdGrossWeight, 2); }
            set { }
        }

        public int ProductCategory { get; set; }
    }
}
