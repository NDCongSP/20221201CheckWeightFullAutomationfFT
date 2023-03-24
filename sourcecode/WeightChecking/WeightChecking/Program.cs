using DevExpress.LookAndFeel;
using DevExpress.Skins;
using DevExpress.UserSkins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Reflection;
using System.IO;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using DevExpress.XtraSplashScreen;
using AutoUpdaterDotNET;
using System.Threading;
using Dapper;

namespace WeightChecking
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            #region Đọc các thông số cấu hình ban đầu từ settings
            GlobalVariables.ConnectionString = EncodeMD5.DecryptString(Properties.Settings.Default.conString, "ITFramasBDVN");
            GlobalVariables.ConStringWinline = EncodeMD5.DecryptString(Properties.Settings.Default.conStringWL, "ITFramasBDVN");
            GlobalVariables.IpConveyor = Properties.Settings.Default.ipConveyor;
            GlobalVariables.UnitScale = int.TryParse(Properties.Settings.Default.UnitScale, out int value) ? value : 0;
            GlobalVariables.IsScale = Properties.Settings.Default.IsScale;
            GlobalVariables.IsCounter = Properties.Settings.Default.IsCounter;
            GlobalVariables.AfterPrinting = Properties.Settings.Default.AfterPrinting;//0-trước in; 1-sau in
            GlobalVariables.PrintComPort = Properties.Settings.Default.PrintComPort;
            GlobalVariables.ScannerIdMetal = Properties.Settings.Default.ScannerIdMetal;
            GlobalVariables.ScannerIdWeight = Properties.Settings.Default.ScannerIdWeight;
            GlobalVariables.ScannerIdPrint = Properties.Settings.Default.ScannerIdPrint;
            GlobalVariables.TimeCheckQrMetal = Properties.Settings.Default.TimeCheckQrMetal;
            GlobalVariables.TimeCheckQrScale = Properties.Settings.Default.TimeCheckQrScale;            
            GlobalVariables.UpdatePath = Properties.Settings.Default.UpdatePath;

            if (Properties.Settings.Default.Station == 0)
            {
                GlobalVariables.Station = StationEnum.IDC_1;
            }
            else if (Properties.Settings.Default.Station == 1)
            {
                GlobalVariables.Station = StationEnum.IDC_2;
            }
            else if (Properties.Settings.Default.Station == 2)
            {
                GlobalVariables.Station = StationEnum.Kerry_3;
            }

            Console.WriteLine($"Path app: {Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)}");

            GlobalVariables.RememberInfo = JsonConvert.DeserializeObject<RememberInfo>(File.ReadAllText(@"./RememberInfo.json"));

            if (GlobalVariables.RememberInfo.Remember)
            {
                GlobalVariables.RememberInfo.UserName = EncodeMD5.DecryptString(GlobalVariables.RememberInfo.UserName, "ITFramasBDVN");
                GlobalVariables.RememberInfo.Pass = EncodeMD5.DecryptString(GlobalVariables.RememberInfo.Pass, "ITFramasBDVN");
            }

            GlobalVariables.ComPortScale = Properties.Settings.Default.ComPortScale;
            #endregion

            #region Get danh sách tất cả các OC đang sử dụng
            using (var connection = GlobalVariables.GetDbConnectionWinline())
            {
                GlobalVariables.OcUsingList = connection.Query<OcUsingModel>("sp_IdcGetListOcName").ToList();
            }
            #endregion

            #region Đọc DB lấy danh sách specialCase
            using (var connection = GlobalVariables.GetDbConnection())
            {
                GlobalVariables.SpecialCaseList = connection.Query<tblSpecialCaseModel>("sp_tblSpecialCaseGets").ToList();
            }
            #endregion

            //Log các hành động của user thì tự log bằng tay vào bảng tblLog
            //tạo serilog để log Error exception.
            MSSqlServerSinkOptions sinkOption = new MSSqlServerSinkOptions()
            {
                TableName = "tblLog",
                AutoCreateSqlTable = true,
            };
            Log.Logger = new LoggerConfiguration().WriteTo.MSSqlServer(

              connectionString: GlobalVariables.ConnectionString,
              sinkOptions: sinkOption

              ).MinimumLevel.Error().CreateLogger();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool createdNew;

            Mutex m = new Mutex(true, "SSFG", out createdNew);

            if (!createdNew)
            {
                // myApp is already running...
                MessageBox.Show("Ứng dụng đã được mở. Chờ trong giây lát...", "THÔNG BÁO", MessageBoxButtons.OK,
                          MessageBoxIcon.Information);
                return;
            }
            else
            {
                AutoUpdater.RunUpdateAsAdmin = false;
                AutoUpdater.DownloadPath = Environment.CurrentDirectory;
                AutoUpdater.ApplicationExitEvent += AutoUpdater_ApplicationExitEvent;
                AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;
                AutoUpdater.Start(GlobalVariables.UpdatePath);
                Application.Run(new Login());

            }
        }

        private static void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args.IsUpdateAvailable)
            {
                DialogResult dialogResult;
                dialogResult =
                        MessageBox.Show(
                            $@"SSFG App có phiên bản mới {args.CurrentVersion}. Phiên bản đang sử dụng hiện tại  {args.InstalledVersion}. Bạn có muốn cập nhật phần mềm không?", @"Cập nhật phần mềm",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                if (dialogResult.Equals(DialogResult.Yes) || dialogResult.Equals(DialogResult.OK))
                {
                    SplashScreenManager.ShowForm(null, typeof(WaitForm1), true, true, false);
                    SplashScreenManager.Default.SetWaitFormCaption("Vui lòng chờ trong giây lát");
                    SplashScreenManager.Default.SetWaitFormDescription("Updating...");

                    try
                    {
                        if (AutoUpdater.DownloadUpdate(args))
                        {
                            SplashScreenManager.CloseForm(false);
                            Application.Exit();
                            //var prs = Process.GetProcessesByName("ZipExtractor");
                            //if (prs != null)
                            //{
                            //    foreach (var item in prs)
                            //    {
                            //        item.Kill();
                            //    }
                            //}
                        }
                        else
                        {
                            SplashScreenManager.ShowForm(null, typeof(WaitForm1), true, true, false);
                            SplashScreenManager.Default.SetWaitFormCaption("Vui lòng chờ trong giây lát");
                            SplashScreenManager.Default.SetWaitFormDescription("Updating...");
                        }
                    }
                    catch (Exception exception)
                    {
                        SplashScreenManager.CloseForm(false);
                        MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }

            }
            else
            {
                //MessageBox.Show(@"Phiên bản bạn đang sử dụng đã được cập nhật mới nhất.", @"Cập nhật phần mềm",
                //    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private static void AutoUpdater_ApplicationExitEvent()
        {

            Thread.Sleep(5000);
            Application.Exit();
        }
    }
}
