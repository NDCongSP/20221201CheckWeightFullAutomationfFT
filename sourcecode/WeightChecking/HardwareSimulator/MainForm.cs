using System.Windows.Forms;

namespace HardwareSimulator
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            var metal = new ScannerSimulatorControl("Metal Scanner Simulator", "127.0.0.2", 23) { Dock = DockStyle.Fill };
            var scale = new ScannerSimulatorControl("Scale Scanner Simulator", "127.0.0.3", 23) { Dock = DockStyle.Fill };
            var printer = new PrinterSimulatorControl { Dock = DockStyle.Fill };

            tabMetal.Controls.Add(metal);
            tabScale.Controls.Add(scale);
            tabPrinter.Controls.Add(printer);
        }
    }
}
