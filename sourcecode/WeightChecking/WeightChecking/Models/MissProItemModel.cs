using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public class MissProItemModel
    {
        public string OcNum { get; set; }
        public string ProductNumber { get; set; }
        public string ProductName { get; set; }
        public string QRCode { get; set; }
        public string Note { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
