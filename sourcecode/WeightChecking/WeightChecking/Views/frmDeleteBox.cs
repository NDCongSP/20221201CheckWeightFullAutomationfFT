using Dapper;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WeightChecking
{
    public partial class frmDeleteBox : DevExpress.XtraEditors.XtraForm
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string IdLabel { get; set; } = string.Empty;
        public string Oc { get; set; } = string.Empty;
        public string BoxId { get; set; } = string.Empty;
        public string PassFail { get; set; } = "0";

        public frmDeleteBox()
        {
            InitializeComponent();
            Load += FrmDeleteBox_Load;
        }

        private void FrmDeleteBox_Load(object sender, EventArgs e)
        {
            _btnDelete.Click += _btnDelete_Click;

            _txtIdLabel.Text = IdLabel;
            _txtOc.Text = Oc;
            _txtBoxId.Text = BoxId;
            bool v = PassFail == "0" ? _ckPass.Checked = false : _ckPass.Checked = true;

            _ckPass.CheckedChanged += (s, o) =>
            {
                if (_ckPass.Checked)
                {
                    PassFail = "1";
                }
                else
                {
                    PassFail = "0";
                }
            };

            _txtIdLabel.TextChanged += (s, o) => { IdLabel = _txtIdLabel.Text; };
            _txtOc.TextChanged += (s, o) => { Oc = _txtOc.Text; };
            _txtBoxId.TextChanged += (s, o) => { BoxId = _txtBoxId.Text; };
        }

        private void _btnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show($"Are you sure delete this box?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var para = new DynamicParameters();
                    if (IdLabel != string.Empty)
                    {
                        para.Add("IdLabel", IdLabel);
                    }
                    else
                    {
                        para.Add("Oc", Oc);
                        para.Add("BoxId", BoxId);
                    }
                    para.Add("IsPass", PassFail);

                    var reader = connection.ExecuteReader("sp_tblScanDataGetForDeleteBox", param: para, commandType: CommandType.StoredProcedure);

                    DataTable boxInfo = new DataTable();
                    boxInfo.Load(reader);

                    if (boxInfo != null && boxInfo.Rows.Count > 0)
                    {
                        para = null;
                        para = new DynamicParameters();
                        para.Add("Id", boxInfo.Rows[0]["Id"]);

                        var id = boxInfo.Rows[0]["Id"];

                        var resultExec = connection.Execute("sp_deleteBox", param: para, commandType: CommandType.StoredProcedure);

                        if (resultExec > 0)
                        {
                            MessageBox.Show("Successfull", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Fail.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        MessageBox.Show("The box could not be found in the data.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}