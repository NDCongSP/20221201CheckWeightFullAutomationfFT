using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    [Table("tblIncomingIDC")]
    public class tblIncomingIDC
    {
        public string QRCode { get; set; }

        public string OCNo { get; set; }

        public string BoxNo { get; set; }

        public string IdLabel { get; set; }

        public int? Actived { get; set; }

        public string CreatedBy { get; set; }

        [Key]
        public DateTime? CreatedDate { get; set; }
    }
}
