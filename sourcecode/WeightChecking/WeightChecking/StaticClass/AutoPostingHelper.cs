using Dapper;
using DevExpress.Spreadsheet;
using DevExpress.XtraRichEdit.Model;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeightChecking.StaticClass
{
    public class AutoPostingHelper
    {
        public static string AutoTransfer(bool isEnable, string productNumber, string barcodeString, int fromWH, int toWH
            , ApplicationDbContextSSFG dbContext, DateTime scantime)
        {
            if (!isEnable) return null;

            try
            {
                // xử lý insert RackStorage 

                // check nếu QRCode hiện tại có nằm trong kho
                //DynamicParameters para = new DynamicParameters();
                //para.Add("@qr", barcodeString);
                //para.Add("@userId", "idc_autoposting");
                //para.Add("@mode", "TRANSFER");
                //// hàng đến từ kho (FFT)
                //para.Add("@whFrom", fromWH);
                //// sẽ vào kho (FFT)
                //para.Add("@whTo", toWH);
                //para.Add("@lock", 0);
                //para.Add("@inputQuantity", null);

                var logNl = new tblLog()
                {
                    Message = $"Transfer from {fromWH} to {toWH}.",
                    MessageTemplate = $"{barcodeString}",
                    Level = "Auto transfer|Before|sp_lmpScannerClient_ScanningLabel_CheckLabel",
                    Exception = null,
                    TimeStamp = DateTime.Now
                };
                dbContext.TblLogs.Add(logNl);

                //(int Accept, string Message) = dbContext.Query<(int Accept, string Message)>("DOGE_WH.dbo.sp_lmpScannerClient_ScanningLabel_CheckLabel", para, commandType: CommandType.StoredProcedure).FirstOrDefault();
                var validateLabel = dbContext.Database
                    .SqlQuery<CheckLabelResult>(
                        "DOGE_WH.dbo.sp_lmpScannerClient_ScanningLabel_CheckLabel @qr = {0}, @userId = {1}, @mode = {2}, @whFrom = {3}, @whTo ={4}, @lock = {5}, @inputQuantity = {6}"
                        , barcodeString, "idc_autoposting", "TRANSFER", fromWH, toWH, 0, null
                    )
                    .FirstOrDefault();
                // thùng hàng có trong kho -> có thể transfer

                logNl = new tblLog()
                {
                    Message = $"Transfer from {fromWH} to {toWH}. Result: Accept = {validateLabel?.Accept};Message = {validateLabel?.Message}",
                    MessageTemplate = $"{barcodeString}",
                    Level = "Auto transfer|After|sp_lmpScannerClient_ScanningLabel_CheckLabel",
                    Exception = null,
                    TimeStamp = DateTime.Now
                };
                dbContext.TblLogs.Add(logNl);
                dbContext.SaveChanges();

                if (validateLabel?.Accept > 0)
                {
                    string machineName = Environment.MachineName;

                    logNl = new tblLog()
                    {
                        Message = $"Transfer from {fromWH} to {toWH}.",
                        MessageTemplate = $"{barcodeString}",
                        Level = "Auto transfer|Before|sp_lmpScannerClient_ScannedLabel_Insert",
                        Exception = null,
                        TimeStamp = DateTime.Now
                    };
                    dbContext.TblLogs.Add(logNl);

                    //var resInsertTransferRackStorage = dbContext.Execute("DOGE_WH.dbo.sp_lmpScannerClient_ScannedLabel_Insert", para, commandType: CommandType.StoredProcedure);

                    // 1. Tạo danh sách tham số (SqlParameter)
                    var parameters = new object[]
                    {
                        new SqlParameter("@qr", barcodeString ?? (object)DBNull.Value),
                        new SqlParameter("@userId", machineName ?? (object)DBNull.Value),
                        new SqlParameter("@mode", "TRANSFER"),
                        new SqlParameter("@whFrom", fromWH),
                        new SqlParameter("@whTo", toWH),
                        new SqlParameter("@deviceId", machineName),
                        new SqlParameter("@scanTime", scantime), // Đảm bảo scantime không null, nếu có thể null hãy check như trên
                        new SqlParameter("@ipAdd", ""),
                        new SqlParameter("@postingText", ""),
                        new SqlParameter("@inputQuantity", DBNull.Value), // Truyền DBNull cho giá trị null
                        new SqlParameter("@id", DBNull.Value)
                    };

                    // 2. Viết câu lệnh SQL gọi Stored Procedure
                    string sqlCommand = "EXEC DOGE_WH.dbo.sp_lmpScannerClient_ScannedLabel_Insert @qr, @userId, @mode, @whFrom, @whTo, @deviceId, @scanTime, @ipAdd, @postingText, @inputQuantity, @id";

                    // 3. Thực thi
                    // Kết quả trả về là số dòng bị ảnh hưởng (int)
                    var resInsertTransferRackStorage = dbContext.Database.ExecuteSqlCommand(sqlCommand, parameters);

                    logNl = new tblLog()
                    {
                        Message = $"Transfer from {fromWH} to {toWH}. Result: {resInsertTransferRackStorage}",
                        MessageTemplate = $"{barcodeString}",
                        Level = "Auto transfer|After|sp_lmpScannerClient_ScannedLabel_Insert",
                        Exception = null,
                        TimeStamp = DateTime.Now
                    };
                    dbContext.TblLogs.Add(logNl);
                    dbContext.SaveChanges();

                    if (resInsertTransferRackStorage > 0)
                    {
                        Debug.WriteLine($"ProductNumber: {productNumber} updated warehouse.");
                        return $"OK. Transfer from {fromWH} to {toWH}";
                    }
                    else
                    {
                        Debug.WriteLine($"ProductNumber: {productNumber} failed to update warehouse.");
                        return $"Fail. Transfer from {fromWH} to {toWH}";
                    }

                }
                else
                {
                    Debug.WriteLine($"Invalid information: {validateLabel?.Message}");
                    return $"Fail. Transfer from {fromWH} to {toWH} - {validateLabel?.Message}";

                }
            }
            catch (Exception ex)
            {
                var logNl = new tblLog()
                {
                    Message = $"Transfer from {fromWH} to {toWH}.",
                    MessageTemplate = $"{barcodeString}",
                    Level = "Auto transfer|ERROR|sp_lmpScannerClient_ScannedLabel_Insert",
                    Exception = null,
                    TimeStamp = DateTime.Now
                };
                dbContext.TblLogs.Add(logNl);
                dbContext.SaveChanges();
                return null;
            }
        }

        public static string AutoStockIn(bool isEnable, string productNumber, string barcodeString, int toWH, IDbConnection connection)
        {
            if (!isEnable) return null;

            try
            {
                // xử lý insert RackStorage 

                // check nếu QRCode hiện tại có nằm trong kho
                DynamicParameters para = new DynamicParameters();
                para.Add("@qr", barcodeString);
                para.Add("@userId", "idc_autoposting"); //user sử dụng cho việc auto posting
                para.Add("@mode", "ADD");
                // hàng đến từ kho (FFT)
                para.Add("@whFrom", "");
                // sẽ vào kho (FFT)
                para.Add("@whTo", toWH);
                para.Add("@lock", 0);
                para.Add("@inputQuantity", null);

                var para1 = new DynamicParameters();
                para1.Add("@Message", $"Stock in to {toWH}.");
                para1.Add("@MessageTemplate", $"{barcodeString}");
                para1.Add("Level", "Auto stock in|Before|sp_lmpScannerClient_ScanningLabel_CheckLabel");
                para1.Add("Exception", null);
                connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                (int Accept, string Message) = connection.Query<(int Accept, string Message)>("DOGE_WH.dbo.sp_lmpScannerClient_ScanningLabel_CheckLabel", para, commandType: CommandType.StoredProcedure).FirstOrDefault();

                para = new DynamicParameters();
                para.Add("@Message", $"Stock in to {toWH}. Result: Accept = {Accept};Message = {Message}");
                para.Add("@MessageTemplate", $"{barcodeString}");
                para.Add("Level", "Auto stock in|After|sp_lmpScannerClient_ScanningLabel_CheckLabel");
                para.Add("Exception", null);
                connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);

                // thùng hàng có trong kho -> có thể transfer
                if (Accept > 0)
                {
                    string machineName = System.Environment.MachineName;

                    para = new DynamicParameters();
                    para.Add("@qr", barcodeString);
                    para.Add("@userId", machineName);
                    para.Add("@mode", "ADD");
                    // hàng đến từ kho (FFT)
                    para.Add("@whFrom", "");
                    // sẽ vào kho (FFT)
                    para.Add("@whTo", toWH);
                    para.Add("@deviceId", machineName);
                    para.Add("@scanTime", DateTime.Now);
                    para.Add("@ipAdd", "");
                    para.Add("@postingText", "");
                    para.Add("@inputQuantity", null);
                    para.Add("@id", null);

                    para1 = new DynamicParameters();
                    para1.Add("@Message", $"Stock in to {toWH}.");
                    para1.Add("@MessageTemplate", $"{barcodeString}");
                    para1.Add("Level", "Auto stock in|Before|sp_lmpScannerClient_ScannedLabel_Insert");
                    para1.Add("Exception", null);
                    connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                    var resInsertTransferRackStorage = connection.Execute("DOGE_WH.dbo.sp_lmpScannerClient_ScannedLabel_Insert", para, commandType: CommandType.StoredProcedure);

                    para1 = new DynamicParameters();
                    para1.Add("@Message", $"Stock in to {toWH}. Result: {resInsertTransferRackStorage}");
                    para1.Add("@MessageTemplate", $"{barcodeString}");
                    para1.Add("Level", "Auto stock in|After|sp_lmpScannerClient_ScannedLabel_Insert");
                    para1.Add("Exception", null);
                    connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                    if (resInsertTransferRackStorage > 0)
                    {
                        Debug.WriteLine($"ProductNumber: {productNumber} updated warehouse.");
                        return $"OK. Stock in to {toWH}.";
                    }
                    else
                    {
                        Debug.WriteLine($"ProductNumber: {productNumber} failed to update warehouse.");
                        return $"Fail. Stock in to {toWH}";
                    }

                }
                else
                {
                    Debug.WriteLine($"Invalid information: {Message}");
                    return $"Fail. Stock in to {toWH} - {Message}";

                }
            }
            catch (Exception ex)
            {

                var para = new DynamicParameters();
                para.Add("@Message", $"Stock in to {toWH}.");
                para.Add("@MessageTemplate", $"{barcodeString}");
                para.Add("Level", "Auto stock in|ERROR|sp_lmpScannerClient_ScannedLabel_Insert");
                para.Add("Exception", ex.ToString());
                connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);
                return null;
            }
        }

        public static string AutoStockOut(bool isEnable, string productNumber, string barcodeString, int fromWH, IDbConnection connection)
        {
            if (!isEnable) return null;

            // xử lý insert RackStorage 

            // check nếu QRCode hiện tại có nằm trong kho
            DynamicParameters para = new DynamicParameters();
            para.Add("@qr", barcodeString);
            para.Add("@userId", "idc_autoposting");
            para.Add("@mode", "REMOVE");
            // hàng đến từ kho (FFT)
            para.Add("@whFrom", fromWH);
            // sẽ vào kho (FFT)
            para.Add("@whTo", "");
            para.Add("@lock", 0);
            para.Add("@inputQuantity", null);

            (int Accept, string Message) = connection.Query<(int Accept, string Message)>("DOGE_WH.dbo.sp_lmpScannerClient_ScanningLabel_CheckLabel", para, commandType: CommandType.StoredProcedure).FirstOrDefault();
            // thùng hàng có trong kho -> có thể transfer
            if (Accept > 0)
            {
                string machineName = System.Environment.MachineName;

                para = new DynamicParameters();
                para.Add("@qr", barcodeString);
                para.Add("@userId", machineName);
                para.Add("@mode", "REMOVE");
                // hàng đến từ kho (FFT)
                para.Add("@whFrom", fromWH);
                // sẽ vào kho (FFT)
                para.Add("@whTo", "");
                para.Add("@deviceId", machineName);
                para.Add("@scanTime", DateTime.Now);
                para.Add("@ipAdd", "");
                para.Add("@postingText", "");
                para.Add("@inputQuantity", null);
                para.Add("@id", null);

                var resInsertTransferRackStorage = connection.Execute("DOGE_WH.dbo.sp_lmpScannerClient_ScannedLabel_Insert", para, commandType: CommandType.StoredProcedure);
                if (resInsertTransferRackStorage > 0)
                {
                    Debug.WriteLine($"ProductNumber: {productNumber} updated warehouse.");
                    return $"ProductNumber: {productNumber} updated warehouse.";
                }
                else
                {
                    Debug.WriteLine($"ProductNumber: {productNumber} failed to update warehouse.");
                    return $"ProductNumber: {productNumber} failed to update warehouse.";
                }

            }
            else
            {
                Debug.WriteLine($"Invalid information: {Message}");
                return $"Invalid information: {Message}";

            }
        }

        /// <summary>
        /// Kiểm tra xem hàng có trong kho hay không.
        /// Accept > 0 -> hàng có trong kho.
        /// </summary>
        /// <param name="productNumber"></param>
        /// <param name="barcodeString"></param>
        /// <param name="toWH"></param>
        /// <param name="dbContext"></param>
        /// <returns>(int,String)</returns>
        public static List<FT050Model> CheckIn(bool isEnable, string productNumber, string barcodeString, ApplicationDbContextSSFG dbContext)
        {
            if (!isEnable) return null;

            // xử lý insert RackStorage 
            var arr = barcodeString.Split('|');
            var arr1 = arr[0].Split(',');

            // check nếu QRCode hiện tại có nằm trong kho
            var res = dbContext.Database.SqlQuery<FT050Model>("DOGE_WH.dbo.sp_lmpScannerClient_ScanningLabel_CheckIn"
                        , arr[0], arr[5], arr[4])
                .ToList();
            // thùng hàng có trong kho -> có thể transfer
            return res;
        }
    }
}
