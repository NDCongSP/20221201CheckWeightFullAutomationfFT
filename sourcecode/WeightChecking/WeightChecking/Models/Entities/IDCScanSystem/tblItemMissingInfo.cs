using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    [Table("tblItemMissingInfo")]
    public class tblItemMissingInfo
    {
        [Key]
        public Guid Id { get; set; }

        public string OcNum { get; set; }

        public string ProductNumber { get; set; }

        public string ProductName { get; set; }

        public DateTime? CreatedDate { get; set; }

        public bool? IsActive { get; set; }

        public string Note { get; set; }

        public string QrCode { get; set; }
    }
}
