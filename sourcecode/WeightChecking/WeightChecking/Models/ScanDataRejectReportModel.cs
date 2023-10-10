using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public class ScanDataRejectReportModel
    {
        public string BarcodeString { get; set; }
        public string IdLabel { get; set; }
        public string OcNo { get; set; }
        public string BoxId { get; set; }
        public string ProductNumber { get; set; }
        public string ScannerStation { get; set; }
        public string Reason { get; set; }
        public double Quantity { get; set; }
        public double GrossWeight { get; set; }
        public double DeviationPairs { get; set; }
        public double DeviationWeight { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ProductName { get; set; }
    }
}
