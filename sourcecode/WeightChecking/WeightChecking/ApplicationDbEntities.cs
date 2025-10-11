using DevExpress.XtraSpreadsheet.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public partial class ApplicationDbEntities: DbContext
    {
        public ApplicationDbEntities() : base("name=DBSSFG")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        public virtual DbSet<tblCoreDataCodeItemSizeModel> tblCoreDataCodeItemSizeModels { get; set; }
        public virtual DbSet<tblWinlineProductsInfoModel> tblWinlineProductsInfoModels { get; set; }
    }
}
