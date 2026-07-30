using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    [Table("tblSystemOC")]
    public class tblSystemOC
    {
        [Key]
        public Guid Id { get; set; }
        public string FirstChar { get; set; }
        public string Description { get; set; }
        public bool Actived { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
