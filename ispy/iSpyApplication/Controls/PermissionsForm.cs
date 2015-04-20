﻿using System;
using System.Linq;
using System.Windows.Forms;

namespace iSpyApplication.Controls
{
    public partial class PermissionsForm : UserControl
    {
        private configurationGroup _group;

        public PermissionsForm()
        {
            InitializeComponent();
            label3.Text = LocRm.GetString("Password");
        }

        public void Init(configurationGroup group)
        {
            _group = group;
            txtPassword.Text = EncDec.DecryptData(group.password, MainForm.Conf.EncryptCode);
            if (_group.name == "Admin")
            {
                //force all features for admin user
                _group.featureset = 1;
                fpFeatures.Enabled = false;
            }
            var i = 1;
            var feats = Enum.GetValues(typeof(Enums.Features));
            foreach (var f in feats)
            {
                var cb = new CheckBox { Text = f.ToString(), Tag = f, AutoSize = true };
                if ((Convert.ToInt32(f) & group.featureset) == i)
                    cb.Checked = true;
                fpFeatures.Controls.Add(cb);
                i = i * 2;
            }
        }

        public bool Save()
        {
            if (EncDec.DecryptData(_group.password, MainForm.Conf.EncryptCode) != txtPassword.Text)
            {
                var p = new Prompt(LocRm.GetString("ConfirmPassword") + ": " + _group.name, "", true);
                if (p.ShowDialog(this) == DialogResult.OK)
                {
                    var v = p.Val;
                    if (v != txtPassword.Text)
                    {
                        MessageBox.Show(this, LocRm.GetString("PasswordMismatch"));
                        p.Dispose();
                        return false;
                    }
                }
                p.Dispose();
                _group.password = EncDec.EncryptData(txtPassword.Text, MainForm.Conf.EncryptCode);
            }

            var tot = (from CheckBox c in fpFeatures.Controls where c.Checked select (int)c.Tag).Sum();
            _group.featureset = tot;
            return true;
        }

        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e)
        {
        }

        private void fpFeatures_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}