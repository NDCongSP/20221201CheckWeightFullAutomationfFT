using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    [Table ("tblSpecialCase")]
    public class tblSpecialCase
    {
        [Key]
        [Browsable(false)]
        public Guid Id { get; set; }
        public string MainItem { get; set; }
        public DateTime CreatedDate { get; set; }
        [Browsable(false)]
        public string CreatedBy { get; set; }
        [Browsable(false)]
        public int IsActived { get; set; }
    }
}
