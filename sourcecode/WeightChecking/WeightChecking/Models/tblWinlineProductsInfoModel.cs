using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
  public  class tblWinlineProductsInfoModel
    {
        public Guid Id { get; set; }
        public string Code_infoSize { get; set; }
        public string ProductNunmber { get; set; }
        public int ProductCategory { get; set; }//=1 la heel counter
        public string ProductName { get; set; }
        public string Brand { get; set; }
        public int Decoration { get; set; }//1 co; 0 Khong
        public string MainProductNo { get; set; }
        public string MainProductName { get; set; }
        public string Color { get; set; }
        public int SizeCode { get; set; }
        public string SizeName { get; set; }
        public double Weight { get; set; }
        public double LeftWeight { get; set; }
        public double RightWeight { get; set; }
        public string BoxType { get; set; }
        public string ToolingNo { get; set; }
        public string PackingBoxType { get; set; }
        public string CustomeUsePb { get; set; }
        public bool Actived { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedMachine { get; set; }
    }
}
