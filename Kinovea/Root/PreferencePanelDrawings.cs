#region License

/*
Copyright © Joan Charmant 2011.
joan.charmant@gmail.com

This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.
*/

#endregion License

using Kinovea.Root.Languages;
using Kinovea.Root.Properties;
using Kinovea.Root.UserInterface.PreferencePanels;
using Kinovea.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.Root
{
    /// <summary>
    ///     Description of PreferencePanelDrawings.
    /// </summary>
    public sealed partial class PreferencePanelDrawings : UserControl, IPreferencePanel
    {
        public void CommitChanges()
        {
            _mPrefManager.DrawOnPlay = _mBDrawOnPlay;
            _mPrefManager.DefaultFading.FromInfosFading(_mDefaultFading);
        }

        private void EnableDisableFadingOptions()
        {
            trkFading.Enabled = chkEnablePersistence.Checked;
            lblFading.Enabled = chkEnablePersistence.Checked;
            chkAlwaysVisible.Enabled = chkEnablePersistence.Checked;
        }

        #region IPreferencePanel properties

        public string Description { get; private set; }

        public Bitmap Icon { get; private set; }

        #endregion IPreferencePanel properties

        #region Members

        private InfosFading _mDefaultFading;
        private bool _mBDrawOnPlay;

        private readonly PreferencesManager _mPrefManager;

        #endregion Members

        #region Construction & Initialization

        public PreferencePanelDrawings()
        {
            InitializeComponent();
            BackColor = Color.White;

            _mPrefManager = PreferencesManager.Instance();

            Description = RootLang.dlgPreferences_btnDrawings;
            Icon = Resources.drawings;

            ImportPreferences();
            InitPage();
        }

        private void ImportPreferences()
        {
            _mBDrawOnPlay = _mPrefManager.DrawOnPlay;
            _mDefaultFading = new InfosFading(0, 0);
        }

        private void InitPage()
        {
            tabGeneral.Text = RootLang.dlgPreferences_ButtonGeneral;
            chkDrawOnPlay.Text = RootLang.dlgPreferences_chkDrawOnPlay;

            tabPersistence.Text = RootLang.dlgPreferences_grpPersistence;
            chkEnablePersistence.Text = RootLang.dlgPreferences_chkEnablePersistence;
            chkAlwaysVisible.Text = RootLang.dlgPreferences_chkAlwaysVisible;

            chkDrawOnPlay.Checked = _mBDrawOnPlay;
            chkEnablePersistence.Checked = _mDefaultFading.Enabled;
            trkFading.Maximum = _mPrefManager.MaxFading;
            trkFading.Value = Math.Min(_mDefaultFading.FadingFrames, trkFading.Maximum);
            chkAlwaysVisible.Checked = _mDefaultFading.AlwaysVisible;
            EnableDisableFadingOptions();
            lblFading.Text = string.Format(RootLang.dlgPreferences_lblFading, trkFading.Value);
        }

        #endregion Construction & Initialization

        #region Handlers

        #region General

        private void chkDrawOnPlay_CheckedChanged(object sender, EventArgs e)
        {
            _mBDrawOnPlay = chkDrawOnPlay.Checked;
        }

        #endregion General

        #region Persistence

        private void chkFading_CheckedChanged(object sender, EventArgs e)
        {
            _mDefaultFading.Enabled = chkEnablePersistence.Checked;
            EnableDisableFadingOptions();
        }

        private void trkFading_ValueChanged(object sender, EventArgs e)
        {
            lblFading.Text = string.Format(RootLang.dlgPreferences_lblFading, trkFading.Value);
            _mDefaultFading.FadingFrames = trkFading.Value;
            chkAlwaysVisible.Checked = false;
        }

        private void chkAlwaysVisible_CheckedChanged(object sender, EventArgs e)
        {
            _mDefaultFading.AlwaysVisible = chkAlwaysVisible.Checked;
        }

        #endregion Persistence

        #endregion Handlers
    }
}