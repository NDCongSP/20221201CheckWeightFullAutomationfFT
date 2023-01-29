using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public class RememberInfo
    {
        public string UserName { get; set; }
        public string Pass { get; set; }
        public bool Remember { get; set; } = false;
        public int GoodBoxNoPrinting { get; set; } = 0;
        public int GoodBoxPrinting { get; set; } = 0;
        public int FailBoxNoPrinting { get; set; } = 0;
        public int FailBoxPrinting { get; set; } = 0;
        public int CountMetalScan { get; set; }//đếm số thùng chạy qua máy quét kim loại
        public int MetalScan { get; set; }
        public int NoMetalScan { get; set; }
    }
}
