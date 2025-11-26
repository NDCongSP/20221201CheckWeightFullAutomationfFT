using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    [Table("tblMetalScanResult")]
    public class tblMetalScanResult
    {
        [Key]
        public Guid Id { get; set; }

        public string BarcodeString { get; set; }

        public string ProductItemCode { get; set; }

        public string IdLabel { get; set; }

        public string Oc { get; set; }

        public string BoxNo { get; set; }

        public double? Qty { get; set; }

        public bool MetalCheckResult { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string CreatedMachine { get; set; }

        public int? IsActived { get; set; }
    }
}
