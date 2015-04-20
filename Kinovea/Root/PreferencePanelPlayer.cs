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
    ///     PreferencePanelPlayer.
    /// </summary>
    public partial class PreferencePanelPlayer : UserControl, IPreferencePanel
    {
        public void CommitChanges()
        {
            _mPrefManager.DeinterlaceByDefault = _mBDeinterlaceByDefault;
            _mPrefManager.WorkingZoneSeconds = _mIWorkingZoneSeconds;
            _mPrefManager.WorkingZoneMemory = _mIWorkingZoneMemory;
        }

        #region IPreferencePanel properties

        public string Description { get; private set; }

        public Bitmap Icon { get; private set; }

        #endregion IPreferencePanel properties

        #region Members

        private bool _mBDeinterlaceByDefault;
        private int _mIWorkingZoneSeconds;
        private int _mIWorkingZoneMemory;

        private readonly PreferencesManager _mPrefManager;

        #endregion Members

        #region Construction & Initialization

        public PreferencePanelPlayer()
        {
            InitializeComponent();
            BackColor = Color.White;

            _mPrefManager = PreferencesManager.Instance();

            Description = RootLang.dlgPreferences_ButtonPlayAnalyze;
            Icon = Resources.video;

            ImportPreferences();
            InitPage();
        }

        private void ImportPreferences()
        {
            _mBDeinterlaceByDefault = _mPrefManager.DeinterlaceByDefault;
            _mIWorkingZoneSeconds = _mPrefManager.WorkingZoneSeconds;
            _mIWorkingZoneMemory = _mPrefManager.WorkingZoneMemory;
        }

        private void InitPage()
        {
            chkDeinterlace.Text = RootLang.dlgPreferences_DeinterlaceByDefault;
            grpSwitchToAnalysis.Text = RootLang.dlgPreferences_GroupAnalysisMode;
            lblWorkingZoneLogic.Text = RootLang.dlgPreferences_LabelLogic;

            // Fill in initial values.
            chkDeinterlace.Checked = _mBDeinterlaceByDefault;
            trkWorkingZoneSeconds.Value = _mIWorkingZoneSeconds;
            lblWorkingZoneSeconds.Text = string.Format(RootLang.dlgPreferences_LabelWorkingZoneSeconds,
                trkWorkingZoneSeconds.Value);
            trkWorkingZoneMemory.Value = _mIWorkingZoneMemory;
            lblWorkingZoneMemory.Text = string.Format(RootLang.dlgPreferences_LabelWorkingZoneMemory,
                trkWorkingZoneMemory.Value);
        }

        #endregion Construction & Initialization

        #region Handlers

        private void ChkDeinterlaceCheckedChanged(object sender, EventArgs e)
        {
            _mBDeinterlaceByDefault = chkDeinterlace.Checked;
        }

        private void trkWorkingZoneSeconds_ValueChanged(object sender, EventArgs e)
        {
            lblWorkingZoneSeconds.Text = string.Format(RootLang.dlgPreferences_LabelWorkingZoneSeconds,
                trkWorkingZoneSeconds.Value);
            _mIWorkingZoneSeconds = trkWorkingZoneSeconds.Value;
        }

        private void trkWorkingZoneMemory_ValueChanged(object sender, EventArgs e)
        {
            lblWorkingZoneMemory.Text = string.Format(RootLang.dlgPreferences_LabelWorkingZoneMemory,
                trkWorkingZoneMemory.Value);
            _mIWorkingZoneMemory = trkWorkingZoneMemory.Value;
        }

        #endregion Handlers
    }
}