using DevExpress.XtraSpreadsheet.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking
{
    public partial class ApplicationDbContextSSFG : DbContext
    {
        public ApplicationDbContextSSFG(string connectionString) : base(connectionString)
        {
        }
        public string GetConnectionString()
        {
            return this.Database.Connection.ConnectionString;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

            modelBuilder.Ignore<CheckLabelResult>();

            base.OnModelCreating(modelBuilder);
        }


        // Không cần DbSet<CheckLabelResult>, nhưng nếu muốn dễ gọi:
        public DbSet<CheckLabelResult> CheckLabelResults { get; set; }
        public virtual DbSet<tblConfig> TblConfigs { get; set; }

        public virtual DbSet<tblApprovedPrintLabel> TblApprovedPrintLabels { get; set; }

        public virtual DbSet<tblIncomingIDC> TblIncomingIDCs { get; set; }
        public virtual DbSet<tblCoreDataCodeItemSize> TblCoreDataCodeItemSizes { get; set; }
        public virtual DbSet<tblItemMissingInfo> TblItemMissingInfos { get; set; }
        public virtual DbSet<tblLog> TblLogs { get; set; }
        public virtual DbSet<tblMetalScanResult> TblMetalScanResults { get; set; }
        public virtual DbSet<tblScanData> TblScanDatas { get; set; }
        public virtual DbSet<tblScanDataReject> TblScanDataRejects { get; set; }
        public virtual DbSet<tblUser> TblUsers { get; set; }
        public virtual DbSet<tblWinlineProductsInfo> TblWinlineProductsInfos { get; set; }

        public virtual DbSet<tblSpecialCase> TblSpecialCases { get; set; }
    }
}
