using Dapper;
using DevExpress.XtraEditors;
using DevExpress.XtraSplashScreen;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeightChecking
{
    public partial class frmReports : Form
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Station { get; set; } = "All";

        public frmReports()
        {
            InitializeComponent();
            Load += FrmReports_Load;
        }

        private void FrmReports_Load(object sender, EventArgs e)
        {
            GlobalVariables.MyEvent.EventHandlerRefreshReport += (s, o) =>
            {
                RefreshData();
            };

            RefreshData();
        }

        void RefreshData()
        {
            try
            {
                SplashScreenManager.ShowForm(this, typeof(WaitForm1), true, true, false);
                SplashScreenManager.Default.SetWaitFormCaption("Vui lòng chờ trong giây lát");
                SplashScreenManager.Default.SetWaitFormDescription("Loading...");

                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var parametters = new DynamicParameters();
                    parametters.Add("FromDate", FromDate);
                    parametters.Add("ToDate", ToDate);
                    parametters.Add("Station", Station);

                    #region Scan Data
                    var res = connection.Query<tblScanDataModel>("sp_tblScanDataGets", parametters, commandType: CommandType.StoredProcedure).ToList();

                    if (grcReports.InvokeRequired)
                    {
                        grcReports.Invoke(new Action(() =>
                        {
                            grcReports.DataSource = res;
                        }));
                    }
                    else
                    {
                        grcReports.DataSource = res;
                    }

                    grvReports.Columns["CreatedDate"].DisplayFormat.FormatString = "YYYY-MM-dd HH:mm:ss";
                    grvReports.Columns["ProductNumber"].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Left;
                    grvReports.Columns["IdLabel"].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Left;
                    grvReports.Columns["OcNo"].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Left;
                    grvReports.Columns["BoxNo"].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Left;

                    grvReports.Columns["Station"].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Right;
                    grvReports.Columns["ApprovedName"].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Right;
                    grvReports.Columns["ActualDeviationPairs"].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Right;
                    grvReports.Columns["DeviationPairs"].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Right;
                    grvReports.Columns["CalculatedPairs"].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Right;
                    grvReports.Columns["CreatedDate"].Fixed = DevExpress.XtraGrid.Columns.FixedStyle.Right;
                    #endregion

                    #region Approved Print
                    var resApproved = connection.Query<ApprovedModel>("sp_tblApprovedPrintLableSelect", parametters, commandType: CommandType.StoredProcedure).ToList();

                    if (grcApprove.InvokeRequired)
                    {
                        grcApprove.Invoke(new Action(() =>
                        {
                            grcApprove.DataSource = resApproved;
                        }));
                    }
                    else
                    {
                        grcApprove.DataSource = resApproved;
                    }

                    grvApprove.Columns["CreatedDate"].DisplayFormat.FormatString = "YYYY-MM-dd HH:mm:ss";
                    #endregion

                    #region Missing infomation
                    var resMissInfo = connection.Query<MissProItemModel>("sp_MissingInfoGets", parametters, commandType: CommandType.StoredProcedure).ToList();

                    if (grcMissInfo.InvokeRequired)
                    {
                        grcMissInfo.Invoke(new Action(() =>
                        {
                            grcMissInfo.DataSource = resMissInfo;
                        }));
                    }
                    else
                    {
                        grcMissInfo.DataSource = resMissInfo;
                    }

                    grvMissInfo.Columns["CreatedDate"].DisplayFormat.FormatString = "YYYY-MM-dd HH:mm:ss";
                    #endregion

                    #region Scan data reject
                    var resScanDataReject = connection.Query<ScanDataRejectModel>("sp_tblScanDataRejectSelectFromTo", parametters, commandType: CommandType.StoredProcedure).ToList();

                    this.Invoke((MethodInvoker)delegate
                    {
                        grcReject.DataSource = resScanDataReject;
                        grvReject.Columns["CreatedDate"].DisplayFormat.FormatString = "YYYY-MM-dd HH:mm:ss";
                    });
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Lỗi Report exception.");
                XtraMessageBox.Show("Lỗi Report: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SplashScreenManager.CloseForm(false);
            }
        }
    }
}
