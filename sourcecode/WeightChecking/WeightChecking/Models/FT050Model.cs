using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    /// <summary>
    /// Class kiểm tra tem đang ở kho nào.
    /// </summary>
    public class FT050Model
    {
        public Guid Id { get; set; }
        /// <summary>
        /// OC.
        /// </summary>
        public string C000 { get; set; }
        /// <summary>
        /// BoxId.
        /// </summary>

        public string C001 { get; set; }

        public int C002 { get; set; }

        public int C003 { get; set; }
        /// <summary>
        /// Warehouse hiện tại của OC-BoxId.
        /// </summary>

        public string C004 { get; set; }

        public Guid C005 { get; set; }

        public DateTime C006 { get; set; }

        public string C007 { get; set; }

        public string C008 { get; set; }

        public string C009 { get; set; }

        public string C010 { get; set; }
        /// <summary>
        /// QR code.
        /// </summary>

        public string C011 { get; set; }

        public string C012 { get; set; }

        public double C013 { get; set; }
        /// <summary>
        /// Unit.
        /// </summary>

        public string C014 { get; set; }

        public string C015 { get; set; }

        public string C016 { get; set; }

        public string C017 { get; set; }

        public string C018 { get; set; }

        public int C019 { get; set; }

        public int C020 { get; set; }

        /// <summary>
        /// Last WH.
        /// </summary>
        public string C021 { get; set; }

        public int mesoyear { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; }

        public string CreatedMachine { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime ModifiedDate { get; set; }

        public string ModifiedMachine { get; set; }

        public Guid TransactionId { get; set; }

        public bool Actived { get; set; }
    }
}
