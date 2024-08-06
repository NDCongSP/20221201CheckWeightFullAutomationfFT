using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScannerCam
{
    public partial class Form1 : Form
    {

        ScaleHelper _scale = new ScaleHelper();
        public Form1()
        {
            InitializeComponent();

            Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _scale.ValueChanged += _scale_ValueChanged;
            Task.Run(() =>
            {
                _scale.CheckConnect();
                // _scale.StartReadScale();
            });


        }

        private void _scale_ValueChanged(object sender, ScaleValueChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"QR code: {e.Value}");

            if (_labQRCode.InvokeRequired)
            {
                _labQRCode.Invoke(new Action(() =>
                {
                    _labQRCode.Text = e.Value;
                }));
            }
            else _labQRCode.Text = e.Value;
        }
    }
}
