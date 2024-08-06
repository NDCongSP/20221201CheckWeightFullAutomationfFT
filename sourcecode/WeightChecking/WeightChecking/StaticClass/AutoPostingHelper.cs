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
        public static string AutoTransfer(string productNumber, string barcodeString, int fromWH, int toWH, IDbConnection connection)
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

            (int Accept, string Message) = connection.Query<(int Accept, string Message)>("DOGE_WH.dbo.sp_lmpScannerClient_ScanningLabel_CheckLabel", para, commandType: CommandType.StoredProcedure).FirstOrDefault();
            // thùng hàng có trong kho -> có thể transfer
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

        public static string AutoStockIn(string productNumber, string barcodeString, int toWH, IDbConnection connection)
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

            (int Accept, string Message) = connection.Query<(int Accept, string Message)>("DOGE_WH.dbo.sp_lmpScannerClient_ScanningLabel_CheckLabel", para, commandType: CommandType.StoredProcedure).FirstOrDefault();
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

                var resInsertTransferRackStorage = connection.Execute("DOGE_WH.dbo.sp_lmpScannerClient_ScannedLabel_Insert", para, commandType: CommandType.StoredProcedure);
                if (resInsertTransferRackStorage > 0)
                {
                    Debug.WriteLine($"ProductNumber: {productNumber} đã nhập kho.");
                    return $"ProductNumber: {productNumber} đã nhập kho.";
                }
                else
                {
                    Debug.WriteLine($"ProductNumber: {productNumber} nhập kho thất bại.");
                    return $"ProductNumber: {productNumber} nhập kho thất bại.";
                }

            }
            else
            {
                Debug.WriteLine($"Thông tin không hợp lệ: {Message}");
                return $"Thông tin không hợp lệ: {Message}";

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
        public static int CheckIn(string productNumber, string barcodeString, int WH, IDbConnection connection)
        {
            // xử lý insert RackStorage 
            var arr = barcodeString.Split('|');
            var arr1 = arr[0].Split(',');

            // check nếu QRCode hiện tại có nằm trong kho
            DynamicParameters para = new DynamicParameters();
            para.Add("@oc", arr1[0]);
            para.Add("@boxId", arr1[5]);
            para.Add("@unit", arr1[4]);
            para.Add("@wh", WH);

            var res = connection.ExecuteScalar<int>("DOGE_WH.dbo.sp_lmpScannerClient_ScanningLabel_CheckIn", para, commandType: CommandType.StoredProcedure);
            // thùng hàng có trong kho -> có thể transfer
            return res;
        }
    }
}
