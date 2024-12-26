using Dapper;
using DevExpress.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeightChecking.StaticClass
{
    public class AutoPostingHelper
    {
        public static string AutoTransfer(string productNumber, string barcodeString, int fromWH, int toWH, IDbConnection connection, DateTime scantime)
        {
            try
            {
                // xử lý insert RackStorage 

                // check nếu QRCode hiện tại có nằm trong kho
                DynamicParameters para = new DynamicParameters();
                para.Add("@qr", barcodeString);
                para.Add("@userId", "idc_autoposting");
                para.Add("@mode", "TRANSFER");
                // hàng đến từ kho (FFT)
                para.Add("@whFrom", fromWH);
                // sẽ vào kho (FFT)
                para.Add("@whTo", toWH);
                para.Add("@lock", 0);
                para.Add("@inputQuantity", null);

                var para1 = new DynamicParameters();
                para1.Add("@Message", $"Transfer from {fromWH} to {toWH}.");
                para1.Add("@MessageTemplate", $"{barcodeString}");
                para1.Add("Level", "Auto transfer|Before|sp_lmpScannerClient_ScanningLabel_CheckLabel");
                para1.Add("Exception", null);
                connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                (int Accept, string Message) = connection.Query<(int Accept, string Message)>("DOGE_WH.dbo.sp_lmpScannerClient_ScanningLabel_CheckLabel", para, commandType: CommandType.StoredProcedure).FirstOrDefault();
                // thùng hàng có trong kho -> có thể transfer

                para1 = new DynamicParameters();
                para1.Add("@Message", $"Transfer from {fromWH} to {toWH}. Result: Accept = {Accept};Message = {Message}");
                para1.Add("@MessageTemplate", $"{barcodeString}");
                para1.Add("Level", "Auto transfer|After|sp_lmpScannerClient_ScanningLabel_CheckLabel");
                para1.Add("Exception", null);
                connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                if (Accept > 0)
                {
                    string machineName = System.Environment.MachineName;

                    para = new DynamicParameters();
                    para.Add("@qr", barcodeString);
                    para.Add("@userId", machineName);
                    para.Add("@mode", "TRANSFER");
                    // hàng đến từ kho (FFT)
                    para.Add("@whFrom", fromWH);
                    // sẽ vào kho (FFT)
                    para.Add("@whTo", toWH);
                    para.Add("@deviceId", machineName);
                    para.Add("@scanTime", scantime);
                    para.Add("@ipAdd", "");
                    para.Add("@postingText", "");
                    para.Add("@inputQuantity", null);
                    para.Add("@id", null);

                    para1 = new DynamicParameters();
                    para1.Add("@Message", $"Transfer from {fromWH} to {toWH}.");
                    para1.Add("@MessageTemplate", $"{barcodeString}");
                    para1.Add("Level", "Auto transfer|Before|sp_lmpScannerClient_ScannedLabel_Insert");
                    para1.Add("Exception", null);
                    connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                    var resInsertTransferRackStorage = connection.Execute("DOGE_WH.dbo.sp_lmpScannerClient_ScannedLabel_Insert", para, commandType: CommandType.StoredProcedure);

                    para1 = new DynamicParameters();
                    para1.Add("@Message", $"Transfer from {fromWH} to {toWH}. Result: {resInsertTransferRackStorage}");
                    para1.Add("@MessageTemplate", $"{barcodeString}");
                    para1.Add("Level", "Auto transfer|After|sp_lmpScannerClient_ScannedLabel_Insert");
                    para1.Add("Exception", null);
                    connection.Execute("sp_tblLog_Insert", param: para1, commandType: CommandType.StoredProcedure);

                    if (resInsertTransferRackStorage > 0)
                    {
                        Debug.WriteLine($"ProductNumber: {productNumber} đã cập nhật kho.");
                        return $"OK. Transfer from {fromWH} to {toWH}";
                    }
                    else
                    {
                        Debug.WriteLine($"ProductNumber: {productNumber} cập nhật kho thất bại.");
                        return $"Fail. Transfer from {fromWH} to {toWH}";
                    }

                }
                else
                {
                    Debug.WriteLine($"Thông tin không hợp lệ: {Message}");
                    return $"Fail. Transfer from {fromWH} to {toWH} - {Message}";

                }
            }
            catch (Exception ex)
            {

                var para = new DynamicParameters();
                para.Add("@Message", $"Transfer from {fromWH} to {toWH}.");
                para.Add("@MessageTemplate", $"{barcodeString}");
                para.Add("Level", "Auto transfer|ERROR|sp_lmpScannerClient_ScannedLabel_Insert");
                para.Add("Exception", ex.ToString());
                connection.Execute("sp_tblLog_Insert", param: para, commandType: CommandType.StoredProcedure);
                return null;
            }
        }

        public static string AutoStockIn(string productNumber, string barcodeString, int toWH, IDbConnection connection)
        {
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
                        Debug.WriteLine($"ProductNumber: {productNumber} đã cập nhật kho.");
                        return $"OK. Stock in to {toWH}.";
                    }
                    else
                    {
                        Debug.WriteLine($"ProductNumber: {productNumber} cập nhật kho thất bại.");
                        return $"Fail. Stock in to {toWH}";
                    }

                }
                else
                {
                    Debug.WriteLine($"Thông tin không hợp lệ: {Message}");
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

        public static string AutoStockOut(string productNumber, string barcodeString, int fromWH, IDbConnection connection)
        {
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
                    Debug.WriteLine($"ProductNumber: {productNumber} đã cập nhật kho.");
                    return $"ProductNumber: {productNumber} đã cập nhật kho.";
                }
                else
                {
                    Debug.WriteLine($"ProductNumber: {productNumber} cập nhật kho thất bại.");
                    return $"ProductNumber: {productNumber} cập nhật kho thất bại.";
                }

            }
            else
            {
                Debug.WriteLine($"Thông tin không hợp lệ: {Message}");
                return $"Thông tin không hợp lệ: {Message}";

            }
        }

        /// <summary>
        /// Kiểm tra xem hàng có trong kho hay không.
        /// Accept > 0 -> hàng có trong kho.
        /// </summary>
        /// <param name="productNumber"></param>
        /// <param name="barcodeString"></param>
        /// <param name="toWH"></param>
        /// <param name="connection"></param>
        /// <returns>(int,String)</returns>
        public static List<FT050Model> CheckIn(string productNumber, string barcodeString, IDbConnection connection)
        {
            // xử lý insert RackStorage 
            var arr = barcodeString.Split('|');
            var arr1 = arr[0].Split(',');

            // check nếu QRCode hiện tại có nằm trong kho
            DynamicParameters para = new DynamicParameters();
            para.Add("@oc", arr1[0]);
            para.Add("@boxId", arr1[5]);
            para.Add("@unit", arr1[4]);

            var res = connection.Query<FT050Model>("DOGE_WH.dbo.sp_lmpScannerClient_ScanningLabel_CheckIn", para, commandType: CommandType.StoredProcedure).ToList();
            // thùng hàng có trong kho -> có thể transfer
            return res;
        }
    }
}
