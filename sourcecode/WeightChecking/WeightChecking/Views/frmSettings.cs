using DevExpress.XtraEditors;
using DevExpress.XtraVerticalGrid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using DevExpress.XtraVerticalGrid.Events;
using DevExpress.XtraVerticalGrid.Rows;
using System.Data.Entity.Migrations;


namespace WeightChecking
{
    public partial class frmSettings : DevExpress.XtraEditors.XtraForm
    {
        private ConfigJsonModel _configJson = new ConfigJsonModel();
        private tblConfig _tblConfig = new tblConfig();
        public frmSettings()
        {
            InitializeComponent();

            Load += FrmSettings_Load;
        }

        private void FrmSettings_Load(object sender, EventArgs e)
        {
            using (var db = new ApplicationDbEntities(GlobalVariables.ConnectionString))
            {
                _tblConfig = db.TblConfigs.FirstOrDefault();
                if (_tblConfig != null)
                {
                    if (!string.IsNullOrEmpty(_tblConfig.ConfigJson))
                    {
                        _configJson = Newtonsoft.Json.JsonConvert.DeserializeObject<ConfigJsonModel>(_tblConfig.ConfigJson);
                    }
                }
                else
                {
                    _tblConfig = new tblConfig
                    {
                        Id = Guid.NewGuid(),
                        Location = EnumFactory.framas3,
                        ConfigJson = Newtonsoft.Json.JsonConvert.SerializeObject(_configJson),
                        CreatedBy = Environment.UserName,
                        CreatedDate = DateTime.Now,
                        CreatedMachine = Environment.MachineName
                    };

                    db.TblConfigs.Add(_tblConfig);
                    db.SaveChanges();
                }
            }
            // Gán object config cho property grid
            _propertyGridControlConfig.SelectedObject = _configJson;

            // Bật cho phép sửa giá trị
            _propertyGridControlConfig.OptionsBehavior.Editable = true;

            _btnSave.Click += _btnSave_Click;
        }

        private void _btnSave_Click(object sender, EventArgs e)
        {
            _tblConfig.Location = EnumFactory.framas3;
            _tblConfig.ConfigJson = Newtonsoft.Json.JsonConvert.SerializeObject(_configJson);

            using (var dbContext = new ApplicationDbEntities(GlobalVariables.ConnectionString))
            {
                dbContext.TblConfigs.AddOrUpdate(_tblConfig);
                dbContext.SaveChanges();
            }
        }
    }
}