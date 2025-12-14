using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    [Table("tblApprovedPrintLabel")]
    public class tblApprovedPrintLabel
    {
        [Key]
        public Guid? Id { get; set; }

        public Guid? QrCode { get; set; }

        public string IdLabel { get; set; }

        public string OC { get; set; }

        public string BoxNo { get; set; }

        public double? GrossWeight { get; set; }

        public int? Station { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string CreatedMachine { get; set; }

        public string QRLabel { get; set; }

        public string ApproveType { get; set; }

        public double? CalculatorDeviationPairs { get; set; }

        public double? ActualDeviationPairs { get; set; }
    }
}
