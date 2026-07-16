using System.Windows.Forms;

namespace CognexScannerTester
{
    public partial class MainForm : Form
    {
        private readonly ScannerConnectionControl _metal;
        private readonly ScannerConnectionControl _scale;

        public MainForm()
        {
            InitializeComponent();

            _metal = new ScannerConnectionControl("Metal Scanner", "192.168.80.5", 23) { Dock = DockStyle.Fill };
            _scale = new ScannerConnectionControl("Scale Scanner", "192.168.80.4", 23) { Dock = DockStyle.Fill };

            tabMetal.Controls.Add(_metal);
            tabScale.Controls.Add(_scale);

            FormClosing += MainForm_FormClosing;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _metal.Disconnect();
            _scale.Disconnect();
        }
    }
}
