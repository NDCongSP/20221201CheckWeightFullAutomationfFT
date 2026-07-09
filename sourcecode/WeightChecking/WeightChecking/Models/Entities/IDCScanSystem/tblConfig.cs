using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeightChecking
{
    [Table("tblConfig_test")]
    public partial class tblConfig
    {
        [Key]
        public Guid Id { get; set; }

        public EnumFactory Location { get; set; } = EnumFactory.framas3;

        /// <summary>
        /// Chính là ConfigJsonModel.
        /// </summary>
        public string ConfigJson { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public string CreatedMachine { get; set; } = string.Empty;
    }

    public class ConfigJsonModel
    {
        [Description("Connection string to database DOGE_WH.")]
        public string ConStringWL { get; set; } = "ed3YbBgz3fF3bb/osXwqYHrw1jNOFfKc3JbTTBiu8d5CSX39iCbf3AbWoi/gdplGgQ4S1F0eqejL96hGqkSHiNF/aYcu0biohTIIX7i0D7GVBt2fqKx5q9lUQL3OPGXcNPbJq6KBYuZUOhOab0v96CKvWGtQSc7IV5bg4V+JSmYW7U+gN2mFCqA5Mo58v0O9uV+4I0lTv9bglYAfpJAZiw==";

        [Description("The IP address of PLC conveyor.")]
        public string IpConveyor { get; set; } = "192.168.80.3";

        public double UnitScale { get; set; } = 1000;

        [Description("The path folder to update version for application.")]
        public string UpdatePath { get; set; } = "\\\\10.40.10.9\\Public$\\98_Public_Share\\99_Shared\\05_IT\\01_Update\\21- IDCScaleSystem\\update.xml";

        public bool IsCounter { get; set; } = false;

        public EnumStation Station { get; set; } = EnumStation.Identification;

        public int AfterPrinting { get; set; } = 0;

        [Description("The COM port of printer (legacy, replaced by TCP).")]
        public string ComPortPrinter { get; set; } = "COM4";

        [Description("The IP address of Anser U2 printer (TCP connection).")]
        public string IpPrinter { get; set; } = "192.168.4.70";

        [Description("The TCP port of Anser U2 printer.")]
        public int PortPrinter { get; set; } = 4001;

        /// <summary>
        /// THời gian đếm từ khi cảm biến S1 trên conveyor kích hoạt, sau khoảng thời gian này mà không nhận được tín hiêu từ scanner trả về thì reject.
        /// Unit: second.
        /// </summary>
        [Description("Unir: second.")]
        public double TimerCheckQrMetal { get; set; } = 4;


        /// <summary>
        /// THời gian đếm từ khi cảm biến in trên băng tải cân kích hoạt, sau khoảng thời gian này mà không nhận được tín hiêu từ scanner trả về thì reject.
        /// Unit: second.
        /// </summary>
        [Description("Unir: second.")]
        public double TimerCheckQrScale { get; set; } = 3;

        public bool IsTest { get; set; } = false;

        public string ConStringTest { get; set; } = "ed3YbBgz3fF3bb/osXwqYNhSCfyKvNWvXPUgvQLnsbz+Nd6Vj/7Bp3JpDzU1SquQf9r/UjvioXIcrdYeKJIBZxfUIaWnODCfVyoZD4vtKfTY2bzuwo+x9IWaUwNDM8sXy0vvVfwBcTaxNaqOPWxs7wJ2VL5tVKN6NXz6gB+a8K0JokfzGUFtGBP4RKiPCgVqqhImSXhPA27fwcNR/ExwoYestgqmQS8+";

        [Description("The IP address of matrix scanner weighing.")]
        public string IpCognexCamScale { get; set; } = "192.168.80.4";

        [Description("The IP address of matrix scanner at the metal (Identification) station. Fallback default - confirm real IP with the production line before deploying.")]
        public string IpCognexCamMetal { get; set; } = "192.168.80.5";

        [Description("Enable to scale.")]
        public bool IsScale { get; set; } = true;

        /// <summary>
        /// Thời gian đếm ngược để reset UI dau khi thực hiện xong.
        /// đơn vị (s).
        /// </summary>
        public int ResetUiInterval { get; set; } = 10;

        /// <summary>
        /// Thời gian chờ cân ổn định trước khi lấy giá trị cân.
        /// Tính theo công thức: DelayTimer2 x 100ms. 10 x 100ms = 1s.
        /// </summary>
        [Description("Delay time to set the scale stable. value x 100(ms)")]
        public ushort DelayTimer2 { get; set; } = 5;

        /// <summary>
        /// thời gian chạy băng tải để đưa thùng vào đúng vị trí chính giữa băng tải cân.
        /// Tính theo công thức: DelayTimer1 x 100ms. 25 x 100ms = 2.5s.
        /// </summary>
        [Description("The delay time to run conveyor for moving the box to center the scale. value x 100(ms)")]

        public ushort DelayTimer1 { get; set; } = 9;

        /// <summary>
        /// Tính theo công thức: DelayTimer3 x 100ms. 10 x 100ms = 1s.
        /// Reset đèn tháp sau khi in xong.
        /// </summary>
        [Description("The delay time (in milliseconds) to print label after passed the weight checking. value x 100(ms)")]
        public ushort DelayTimer3 { get; set; } = 20;

        /// <summary>
        /// Tính theo công thức: DelayTimer4 x 100ms. 50 x 100ms = 5s.
        /// Reset đèn tháp sau khi in xong.
        /// </summary>
        [Description("Delay time to reset D506 (light town). value x 100(ms)\"")]
        public ushort DelayTimer4 { get; set; } = 50;

        /// <summary>
        /// Tính theo công thức: DelayTimer5 x 100ms. 20 x 100ms = 5s.
        /// Thời gian chờ để reset cờ báo bận Y3.
        /// </summary>
        [Description("Delay time to reset Y3 (busy flag). value x 100(ms)\"")]
        public ushort DelayTimer5 { get; set; } = 20;

        /// <summary>
        /// cho phép tự động post dữ liệu sau khi cân xong lên hệ thống Winline.
        /// </summary>
        [Description("Allow to auto post data to Winline system after weighing.")]
        public bool FlagAutoPost { get; set; } = false;
    }
}
