using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WeightChecking
{
    public class MetalScanResultModel
    {
        [Browsable(false)]
        public Guid Id { get; set; }

        public string BarcodeString { get; set; }

        public string IdLabel { get; set; }

        public string Oc { get; set; }

        public string BoxNo { get; set; }

        public double Qty { get; set; }

        public bool MetalCheckResult { get; set; }
        public string ProductItemCode { get; set; }

        public DateTime CreatedDate { get; set; }

        public string CreatedMachine { get; set; }

        public int IsActived { get; set; }
    }
}
