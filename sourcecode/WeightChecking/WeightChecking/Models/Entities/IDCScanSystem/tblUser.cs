using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    [Table("tblUsers")]
    public  class tblUser
    {
        [Key]
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public RolesEnum Role { get; set; }
        public int? Actived { get; set; }
        public DateTime? CreatedBy { get; set; }
        public string Note { get; set; }
        public int Approved { get; set; }
    }
}
