using Dapper;
using DevExpress.XtraEditors;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BC = BCrypt.Net.BCrypt;

namespace WeightChecking
{
    public partial class Login : DevExpress.XtraEditors.XtraForm
    {
        private Timer _timer = new Timer() { Interval = 100 };
        private bool _saveInfo = false;
        DialogResult dialogResult;
        public Login()
        {
            InitializeComponent();

            Load += Login_Load;

            labStatus.Text = Application.ProductVersion;
        }

        private void Login_Load(object sender, EventArgs e)
        {
            if (GlobalVariables.RememberInfo.Remember)
            {
                this.txtUseName.Text = GlobalVariables.RememberInfo.UserName;
                this.txtPass.Text = GlobalVariables.RememberInfo.Pass;
                this.chkRemember.Checked = GlobalVariables.RememberInfo.Remember;
            }

            this.txtUseName.Focus();
            this.chkRemember.CheckedChanged += (s, o) =>
            {
                CheckEdit ck = (CheckEdit)s;
                GlobalVariables.RememberInfo.Remember = ck.Checked;
            };

            btnSubmit.Click += async (s, o) => await btnSubmit_Click(s, o);

            _timer.Enabled = true;
            _timer.Tick += _timer_Tick;
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            Timer s = (Timer)sender;
            s.Enabled = false;
            labTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            s.Enabled = true;
        }

        private async Task btnSubmit_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtUseName.Text) && !string.IsNullOrEmpty(txtPass.Text))
            {
                using (var connection = GlobalVariables.GetDbConnection())
                {
                    var para = new DynamicParameters();
                    para.Add("@userName", txtUseName.Text);

                    using (var dbContext = new ApplicationDbContextSSFG(GlobalVariables.ConnectionString))
                    {
                        GlobalVariables.UserLoginInfo = await dbContext.TblUsers.FirstOrDefaultAsync(u => u.UserName == txtUseName.Text);
                    }

                    if (GlobalVariables.UserLoginInfo == null)
                    {
                        XtraMessageBox.Show($"User name '{txtUseName.Text}' was not found.", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (!BC.Verify(txtPass.Text, GlobalVariables.UserLoginInfo.Password))
                    {
                        XtraMessageBox.Show("Password is not correct.", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    //log thong tin dang nhap vao rememberInfo
                    if (GlobalVariables.RememberInfo.Remember)
                    {
                        GlobalVariables.RememberInfo.UserName = EncodeMD5.EncryptString(txtUseName.Text, "ITFramasBDVN");
                        GlobalVariables.RememberInfo.Pass = EncodeMD5.EncryptString(txtPass.Text, "ITFramasBDVN");

                        string json = JsonConvert.SerializeObject(GlobalVariables.RememberInfo);

                        File.WriteAllText(@"./RememberInfo.json", json);
                    }
                    else
                    {
                        GlobalVariables.RememberInfo.UserName = null;
                        GlobalVariables.RememberInfo.Pass = null;
                        GlobalVariables.RememberInfo.Remember = false;

                        string json = JsonConvert.SerializeObject(GlobalVariables.RememberInfo);

                        File.WriteAllText(@"./RememberInfo.json", json);
                    }

                    //frmMain nf = new frmMain();
                    //nf.ShowDialog();

                    this.Hide();
                    if (GlobalVariables.UserLoginInfo.Role == RolesEnum.Operator)
                    {
                        var frmMain = new frmScaleNewUI();
                        dialogResult = frmMain.ShowDialog();
                    }
                    else
                    {
                        var frmMain = new frmMain();
                        dialogResult = frmMain.ShowDialog();
                    }

                    //if (dialogResult == DialogResult.OK)
                    {
                        this.Close();
                    }
                }
            }
            else
            {
                XtraMessageBox.Show("Nhập thiếu thông tin, vui lòng nhập lại đầy đủ thông tin.", "CẢNH BÁO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}