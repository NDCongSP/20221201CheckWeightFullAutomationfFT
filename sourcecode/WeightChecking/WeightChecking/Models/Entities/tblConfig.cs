using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WeightChecking
{
    [Table("tblConfig")]
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
        [Description("Connection string to database SSFG.")]
        public string ConStringSSFG { get; set; } = "ed3YbBgz3fF3bb/osXwqYNhSCfyKvNWvXPUgvQLnsbz+Nd6Vj/7Bp8rlvWbE/zlBsqiGfzls0FozxdSpLEGyIYk/FpLk0PUEB0owITx4e7QWtAs6hhG9O20ffz+nroHV3b//mZ91MP5kAywrsmMsnt2//5mfdTD+ZFycX6i+aqjOePbB/5XtS54TqvGoMQ6uZs5JoyLjxtFaOXCkfgRc3OCVgB+kkBmL";

        [Description("Connection string to database DOGE_WH.")]
        public string ConStringWL { get; set; } = "ed3YbBgz3fF3bb/osXwqYNhSCfyKvNWvXPUgvQLnsbz+Nd6Vj/7Bp8rlvWbE/zlBsqiGfzls0FozxdSpLEGyIYk/FpLk0PUEB0owITx4e7QWtAs6hhG9O20ffz+nroHV3b//mZ91MP5kAywrsmMsnt2//5mfdTD+ZFycX6i+aqjOePbB/5XtS54TqvGoMQ6uZs5JoyLjxtFaOXCkfgRc3OCVgB+kkBmL";

        [Description("The IP address of PLC conveyor.")]
        public string IpConveyor { get; set; } = "192.168.80.3";

        public double UnitScale { get; set; } = 1000;

        [Description("The COM port of scale.")]
        public string ComPortScale { get; set; } = "COM2";

        [Description("The path folder to update version for application.")]
        public string UpdatePath { get; set; } = "\\\\10.40.10.9\\Public$\\98_Public_Share\\99_Shared\\05_IT\\01_Update\\21- IDCScaleSystem\\update.xml";

        public bool IsCounter { get; set; } = false;

        public StationEnum Station { get; set; } = StationEnum.IDC_1;

        public int AfterPrinting { get; set; } = 0;

        [Description("The COM port of printer.")]
        public string ComPortPrinter { get; set; } = "COM4";

        public int ScannerIdMetal { get; set; } = 2;
        public int ScannerIdWeight { get; set; } = 3;
        public int ScannerIdPrint { get; set; } = 1;

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

        [Description("Enable to scale.")]
        public bool IsScale { get; set; } = true;
    }
}
