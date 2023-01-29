﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public class ApprovedModel
    {
        public Guid Id { get; set; }
        public Guid QrCode { get; set; }
        public string UserName { get; set; }
        public string IdLable { get; set; }
        public string OC { get; set; }
        public string BoxNo { get; set; }
        public double GrossWeight { get; set; }
        public StationEnum Station { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedMachine { get; set; }
        public string QRLabel { get; set; }
        public string ApproveType { get; set; }
    }
}
