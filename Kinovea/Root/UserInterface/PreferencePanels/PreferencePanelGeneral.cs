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
using Kinovea.ScreenManager;
using Kinovea.Services;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.Root
{
    /// <summary>
    ///     PreferencePanelGeneral.
    /// </summary>
    public partial class PreferencePanelGeneral : UserControl, IPreferencePanel
    {
        public void CommitChanges()
        {
            _mPrefManager.UiCultureName = _mUiCultureName;
            _mPrefManager.HistoryCount = _mIFilesToSave;
            _mPrefManager.TimeCodeFormat = _mTimeCodeFormat;
            _mPrefManager.AspectRatio = _mImageAspectRatio;
            _mPrefManager.SpeedUnit = _mSpeedUnit;
        }

        #region IPreferencePanel properties

        public string Description { get; private set; }

        public Bitmap Icon { get; private set; }

        #endregion IPreferencePanel properties

        #region Members

        private string _mUiCultureName;
        private int _mIFilesToSave;
        private TimeCodeFormat _mTimeCodeFormat;
        private ImageAspectRatio _mImageAspectRatio;
        private SpeedUnits _mSpeedUnit;

        private readonly PreferencesManager _mPrefManager;

        #endregion Members

        #region Construction & Initialization

        public PreferencePanelGeneral()
        {
            InitializeComponent();
            BackColor = Color.White;

            _mPrefManager = PreferencesManager.Instance();

            Description = RootLang.dlgPreferences_ButtonGeneral;
            Icon = Resources.pref_general;

            ImportPreferences();
            InitPage();
        }

        private void ImportPreferences()
        {
            var ci = _mPrefManager.GetSupportedCulture();
            _mUiCultureName = ci.IsNeutralCulture ? ci.Name : ci.Parent.Name;
            _mIFilesToSave = _mPrefManager.HistoryCount;
            _mTimeCodeFormat = _mPrefManager.TimeCodeFormat;
            _mImageAspectRatio = _mPrefManager.AspectRatio;
            _mSpeedUnit = _mPrefManager.SpeedUnit;
        }

        private void InitPage()
        {
            // Localize and fill possible values

            lblLanguage.Text = RootLang.dlgPreferences_LabelLanguages;
            cmbLanguage.Items.Clear();
            foreach (var lang in LanguageManager.Languages)
            {
                cmbLanguage.Items.Add(new LanguageIdentifier(lang.Key, lang.Value));
            }

            lblHistoryCount.Text = RootLang.dlgPreferences_LabelHistoryCount;

            // Combo TimeCodeFormats (MUST be filled in the order of the enum, see PreferencesManager.TimeCodeFormat)
            lblTimeMarkersFormat.Text = RootLang.dlgPreferences_LabelTimeFormat + " :";
            cmbTimeCodeFormat.Items.Add(RootLang.TimeCodeFormat_Classic);
            cmbTimeCodeFormat.Items.Add(RootLang.TimeCodeFormat_Frames);
            cmbTimeCodeFormat.Items.Add(RootLang.TimeCodeFormat_Milliseconds);
            cmbTimeCodeFormat.Items.Add(RootLang.TimeCodeFormat_TenThousandthOfHours);
            cmbTimeCodeFormat.Items.Add(RootLang.TimeCodeFormat_HundredthOfMinutes);
            cmbTimeCodeFormat.Items.Add(RootLang.TimeCodeFormat_TimeAndFrames);
            //cmbTimeCodeFormat.Items.Add(RootLang.TimeCodeFormat_Timestamps);	// Debug purposes.

            // Combo Speed units (MUST be filled in the order of the enum)
            lblSpeedUnit.Text = RootLang.dlgPreferences_LabelSpeedUnit;
            cmbSpeedUnit.Items.Add(string.Format(RootLang.dlgPreferences_Speed_MetersPerSecond,
                CalibrationHelper.GetSpeedAbbreviationFromUnit(SpeedUnits.MetersPerSecond)));
            cmbSpeedUnit.Items.Add(string.Format(RootLang.dlgPreferences_Speed_KilometersPerHour,
                CalibrationHelper.GetSpeedAbbreviationFromUnit(SpeedUnits.KilometersPerHour)));
            cmbSpeedUnit.Items.Add(string.Format(RootLang.dlgPreferences_Speed_FeetPerSecond,
                CalibrationHelper.GetSpeedAbbreviationFromUnit(SpeedUnits.FeetPerSecond)));
            cmbSpeedUnit.Items.Add(string.Format(RootLang.dlgPreferences_Speed_MilesPerHour,
                CalibrationHelper.GetSpeedAbbreviationFromUnit(SpeedUnits.MilesPerHour)));
            //cmbSpeedUnit.Items.Add(RootLang.dlgPreferences_Speed_Knots);		// Is this useful at all ?

            // Combo Image Aspect Ratios (MUST be filled in the order of the enum)
            lblImageFormat.Text = RootLang.dlgPreferences_LabelImageFormat;
            cmbImageFormats.Items.Add(RootLang.dlgPreferences_FormatAuto);
            cmbImageFormats.Items.Add(RootLang.dlgPreferences_Format43);
            cmbImageFormats.Items.Add(RootLang.dlgPreferences_Format169);

            // Fill current values
            SelectCurrentLanguage();
            cmbHistoryCount.SelectedIndex = _mIFilesToSave;
            SelectCurrentTimecodeFormat();
            SelectCurrentSpeedUnit();
            SelectCurrentImageFormat();
        }

        private void SelectCurrentLanguage()
        {
            var found = false;
            for (var i = 0; i < cmbLanguage.Items.Count; i++)
            {
                var li = (LanguageIdentifier)cmbLanguage.Items[i];

                if (li.Culture.Equals(_mUiCultureName))
                {
                    // Matching
                    cmbLanguage.SelectedIndex = i;
                    found = true;
                }
            }
            if (!found)
            {
                // The supported language is not in the combo box. (error).
                cmbLanguage.SelectedIndex = 0;
            }
        }

        private void SelectCurrentTimecodeFormat()
        {
            // the combo box items have been filled in the order of the enum.
            if ((int)_mTimeCodeFormat < cmbTimeCodeFormat.Items.Count)
            {
                cmbTimeCodeFormat.SelectedIndex = (int)_mTimeCodeFormat;
            }
            else
            {
                cmbTimeCodeFormat.SelectedIndex = 0;
            }
        }

        private void SelectCurrentSpeedUnit()
        {
            // the combo box items have been filled in the order of the enum.
            if ((int)_mSpeedUnit < cmbSpeedUnit.Items.Count)
            {
                cmbSpeedUnit.SelectedIndex = (int)_mSpeedUnit;
            }
            else
            {
                cmbSpeedUnit.SelectedIndex = 0;
            }
        }

        private void SelectCurrentImageFormat()
        {
            // the combo box items have been filled in the order of the enum.
            if ((int)_mImageAspectRatio < cmbImageFormats.Items.Count)
            {
                cmbImageFormats.SelectedIndex = (int)_mImageAspectRatio;
            }
            else
            {
                cmbImageFormats.SelectedIndex = 0;
            }
        }

        #endregion Construction & Initialization

        #region Handlers

        private void cmbLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            _mUiCultureName = ((LanguageIdentifier)cmbLanguage.Items[cmbLanguage.SelectedIndex]).Culture;
        }

        private void cmbHistoryCount_SelectedIndexChanged(object sender, EventArgs e)
        {
            _mIFilesToSave = cmbHistoryCount.SelectedIndex;
        }

        private void cmbTimeCodeFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            // the combo box items have been filled in the order of the enum.
            _mTimeCodeFormat = (TimeCodeFormat)cmbTimeCodeFormat.SelectedIndex;
        }

        private void cmbImageAspectRatio_SelectedIndexChanged(object sender, EventArgs e)
        {
            // the combo box items have been filled in the order of the enum.
            _mImageAspectRatio = (ImageAspectRatio)cmbImageFormats.SelectedIndex;
        }

        private void cmbSpeedUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            // the combo box items have been filled in the order of the enum.
            _mSpeedUnit = (SpeedUnits)cmbSpeedUnit.SelectedIndex;
        }

        #endregion Handlers
    }
}