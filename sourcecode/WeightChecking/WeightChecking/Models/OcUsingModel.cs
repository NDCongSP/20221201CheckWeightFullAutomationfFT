using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
  public   class OcUsingModel
    {
        public string OcNo { get; set; }//t357.c018
        public string OcFirstChar { get; set; }//căt ra từ OcNo, lấy 2 ký tự đầu để so sánh với QR code
        public string Description { get; set; }//t357.c001
        public string VocherType { get; set; }//t357.c030
    }
}
