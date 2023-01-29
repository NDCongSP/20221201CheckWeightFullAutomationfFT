using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public class tblScanDataCheckModel
    {
        public int Pass { get; set; }
        public Guid ApprovedBy { get; set; }
        public int ActualDeviationPairs { get; set; } = 0;
        public int Status { get; set; } = 0;
        public int Station { get; set; } = 0;
    }
}
