using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraReports.UI;
using PLCPiProject;

namespace WeightChecking
{
    public static class GlobalVariables
    {
        public static string ConnectionString { get; set; }
        public static string ConStringWinline { get; set; }

        public static IDbConnection GetDbConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public static IDbConnection GetDbConnectionWinline()
        {
            return new SqlConnection(ConStringWinline);
        }
        //chứa các thông tin cần lưu lại để khi mở phần mềm lên thì sẽ đọc lên để tiếp tục làm việc.
        public static RememberInfo RememberInfo { get; set; } = new RememberInfo();

        public static CustomEvents MyEvent = new CustomEvents();

        //biến cấu hình cân
        public static string IpConveyor = "";//ip cau PLC S7-1200 kết n
        public static string PortScale = "23";
        public static string ScaleStatus = "Disconnected";
        public static int ScaleDelay = 300;
        public static int UnitScale { get; set; } = 0;

        public static tblUsers UserLoginInfo { get; set; } = new tblUsers();

        public static PLCPi MyDriver = new PLCPi();
        public static byte[] ReadHoldingArr { get; set; }
        public static bool ModbusStatus { get; set; }
        public static string ComPortScale { get; set; }//com kết nối PLC Delta ngay bàn cân, để đọc khố lượng cân và điều khiển đèn tháp

        public static bool IsScale { get; set; } = false;
        public static bool IsCounter { get; set; } = false;
        public static StationEnum Station { get; set; }

        //bao can o tram truoc son hay sau son. 0-truoc; 1-sau
        public static int AfterPrinting { get; set; } = 0;

        public static string PrintComPort { get; set; }
        public static int ScannerIdMetal { get; set; } = 1;
        public static int ScannerIdWeight { get; set; } = 2;
        public static int ScannerIdPrint { get; set; } = 3;

        public static string ConveyorStatus { get; set; } = "Bad";
        public static string PrintConnectionStatus { get; set; } = "Bad";
        public static byte[] DataWriteDb1 { get; set; } = new byte[] { 0, 0, 0, 0 };//biến dùng để chứa các giá trị ghi xuống PLC để điều khiển pusher
        //byte[0]-Metal; byte[1]-Scale; byte[2]-print

        public static List<OcUsingModel> OcUsingList { get; set; } = new List<OcUsingModel>();//get ra danh sách tất cả các OcNo đang sử dụng
        public static double TimeCheckQrMetal { get; set; }//Đơn vị (s), thời gian quy định thùng chạy từ cảm biến trước metal scanner đến vị trí scanner,n
                                                           //nếu qua thời gian này mà scanner chưa có tín hiệu thì báo lỗi không đọc được QR code, ghi lệnh xuống PLC conveyor reject 
        public static double TimeCheckQrScale { get; set; }        
        public static bool AutoMan { get; set; } = true;//biến chọn chế độ hoạt động là tự động hoàn toàn hay là bằng tay. True-Auto; False-Man
        public static List<tblSpecialCaseModel> SpecialCaseList { get; set; } = new List<tblSpecialCaseModel>();

        public static string PrintResult { get; set; } = "";//ket qua tra ve khi thuc hien in
        public static string  UpdatePath { get; set; }
        public static string AppStatus { get; set; } = "DANG KHỞI ĐỘNG...";
        public static bool IsTest { get; set; } = false;//biến báo đang ở chế độ test hay chạy chính

        #region Printing
        // Print the file.
        public static void Printing(string content, string idLabel, bool pass, string createdDate)
        {
            //content of the QR code "OC283225,6112012227-2094-2651,28,13,P,1/56,160506,1/1|1,30.2022"
            if (pass)
            {
                var rptRe = new rptLabel();
                //rptRe.DataSource = ds;

                rptRe.Parameters["Weight"].Value = content;
                rptRe.Parameters["IdLabel"].Value = idLabel;
                rptRe.Parameters["CreatedDate"].Value = createdDate;

                rptRe.CreateDocument();
                ReportPrintTool printToolCrush = new ReportPrintTool(rptRe);
                printToolCrush.Print();
            }
            else
            {
                var rptRe = new rptLabelFail();
                //rptRe.DataSource = ds;

                rptRe.Parameters["DeviationPrs"].Value = content;
                rptRe.Parameters["IdLabel"].Value = idLabel;
                rptRe.Parameters["CreatedDate"].Value = createdDate;

                rptRe.CreateDocument();
                ReportPrintTool printToolCrush = new ReportPrintTool(rptRe);
                printToolCrush.Print();
            }
        }

        public static double RealWeight { get; set; } = 0;
        public static string IdLabel { get; set; } = null;
        public static string OcNo { get; set; } = null;
        public static string BoxNo { get; set; } = null;
        public static bool PrintApprove { get; set; } = false;
        public static DateTime CreatedDate { get; set; }//dung de chua thoi gian tạo, để đồng bộ giữa in tem và log DB. dung trong Confirm in tem
        #endregion
    }
}
