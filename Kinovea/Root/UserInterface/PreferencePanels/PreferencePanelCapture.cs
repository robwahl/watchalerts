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
using Kinovea.ScreenManager;
using Kinovea.Services;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Kinovea.Root.UserInterface.PreferencePanels
{
    /// <summary>
    ///     PreferencePanelCapture.
    /// </summary>
    internal sealed partial class PreferencePanelCapture : UserControl, IPreferencePanel
    {
        public void CommitChanges()
        {
            _mPrefManager.CaptureImageDirectory = _mImageDirectory;
            _mPrefManager.CaptureVideoDirectory = _mVideoDirectory;
            _mPrefManager.CaptureImageFormat = _mImageFormat;
            _mPrefManager.CaptureVideoFormat = _mVideoFormat;

            _mPrefManager.CaptureUsePattern = _mBUsePattern;
            _mPrefManager.CapturePattern = _mPattern;
            if (_mBResetCounter)
            {
                _mPrefManager.CaptureImageCounter = 1;
                _mPrefManager.CaptureVideoCounter = 1;
            }

            _mPrefManager.CaptureMemoryBuffer = _mIMemoryBuffer;
        }

        #region IPreferencePanel properties

        public string Description { get; private set; }

        public Bitmap Icon { get; private set; }

        #endregion IPreferencePanel properties

        #region Members

        private string _mImageDirectory;
        private string _mVideoDirectory;
        private KinoveaImageFormat _mImageFormat;
        private KinoveaVideoFormat _mVideoFormat;
        private bool _mBUsePattern;
        private string _mPattern;
        private bool _mBResetCounter;
        private long _mICounter;
        private int _mIMemoryBuffer;

        private readonly PreferencesManager _mPrefManager;
        private readonly FilenameHelper _mFilenameHelper = new FilenameHelper();

        #endregion Members

        #region Construction & Initialization

        public PreferencePanelCapture()
        {
            InitializeComponent();
            BackColor = Color.White;

            _mPrefManager = PreferencesManager.Instance();

            Description = RootLang.dlgPreferences_btnCapture;
            Icon = Resources.pref_capture;

            // Use the tag property of labels to store the actual marker.
            lblYear.Tag = "%y";
            lblMonth.Tag = "%mo";
            lblDay.Tag = "%d";
            lblHour.Tag = "%h";
            lblMinute.Tag = "%mi";
            lblSecond.Tag = "%s";
            lblCounter.Tag = "%i";

            ImportPreferences();
            InitPage();
        }

        private void ImportPreferences()
        {
            _mImageDirectory = _mPrefManager.CaptureImageDirectory;
            _mVideoDirectory = _mPrefManager.CaptureVideoDirectory;
            _mImageFormat = _mPrefManager.CaptureImageFormat;
            _mVideoFormat = _mPrefManager.CaptureVideoFormat;
            _mBUsePattern = _mPrefManager.CaptureUsePattern;
            _mPattern = _mPrefManager.CapturePattern;
            _mICounter = _mPrefManager.CaptureImageCounter; // Use the image counter for sample.
            _mIMemoryBuffer = _mPrefManager.CaptureMemoryBuffer;
        }

        private void InitPage()
        {
            // General tab
            tabGeneral.Text = RootLang.dlgPreferences_ButtonGeneral;
            lblImageDirectory.Text = RootLang.dlgPreferences_Capture_lblImageDirectory;
            lblVideoDirectory.Text = RootLang.dlgPreferences_Capture_lblVideoDirectory;
            tbImageDirectory.Text = _mImageDirectory;
            tbVideoDirectory.Text = _mVideoDirectory;

            lblImageFormat.Text = RootLang.dlgPreferences_Capture_lblImageFormat;
            cmbImageFormat.Items.Add("JPG");
            cmbImageFormat.Items.Add("PNG");
            cmbImageFormat.Items.Add("BMP");
            cmbImageFormat.SelectedIndex = ((int)_mImageFormat < cmbImageFormat.Items.Count) ? (int)_mImageFormat : 0;

            lblVideoFormat.Text = RootLang.dlgPreferences_Capture_lblVideoFormat;
            cmbVideoFormat.Items.Add("MKV");
            cmbVideoFormat.Items.Add("MP4");
            cmbVideoFormat.Items.Add("AVI");
            cmbVideoFormat.SelectedIndex = ((int)_mVideoFormat < cmbVideoFormat.Items.Count) ? (int)_mVideoFormat : 0;

            // Naming tab
            tabNaming.Text = RootLang.dlgPreferences_Capture_tabNaming;
            rbFreeText.Text = RootLang.dlgPreferences_Capture_rbFreeText;
            rbPattern.Text = RootLang.dlgPreferences_Capture_rbPattern;
            lblYear.Text = RootLang.dlgPreferences_Capture_lblYear;
            lblMonth.Text = RootLang.dlgPreferences_Capture_lblMonth;
            lblDay.Text = RootLang.dlgPreferences_Capture_lblDay;
            lblHour.Text = RootLang.dlgPreferences_Capture_lblHour;
            lblMinute.Text = RootLang.dlgPreferences_Capture_lblMinute;
            lblSecond.Text = RootLang.dlgPreferences_Capture_lblSecond;
            lblCounter.Text = RootLang.dlgPreferences_Capture_lblCounter;
            btnResetCounter.Text = RootLang.dlgPreferences_Capture_btnResetCounter;

            tbPattern.Text = _mPattern;
            UpdateSample();

            rbPattern.Checked = _mBUsePattern;
            rbFreeText.Checked = !_mBUsePattern;

            // Memory tab
            tabMemory.Text = RootLang.dlgPreferences_Capture_tabMemory;
            trkMemoryBuffer.Value = _mIMemoryBuffer;
            UpdateMemoryLabel();
        }

        #endregion Construction & Initialization

        #region Handlers

        #region Tab general

        private void btnBrowseImageLocation_Click(object sender, EventArgs e)
        {
            // Select the image snapshot folder.
            SelectSavingDirectory(tbImageDirectory);
        }

        private void btnBrowseVideoLocation_Click(object sender, EventArgs e)
        {
            // Select the video capture folder.
            SelectSavingDirectory(tbVideoDirectory);
        }

        private void SelectSavingDirectory(TextBox tb)
        {
            var folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = ""; // TODO.
            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;

            if (Directory.Exists(tb.Text))
            {
                folderBrowserDialog.SelectedPath = tb.Text;
            }

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                tb.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void tbImageDirectory_TextChanged(object sender, EventArgs e)
        {
            if (!_mFilenameHelper.ValidateFilename(tbImageDirectory.Text, true))
            {
                ScreenManagerKernel.AlertInvalidFileName();
            }
            else
            {
                _mImageDirectory = tbImageDirectory.Text;
            }
        }

        private void tbVideoDirectory_TextChanged(object sender, EventArgs e)
        {
            if (!_mFilenameHelper.ValidateFilename(tbVideoDirectory.Text, true))
            {
                ScreenManagerKernel.AlertInvalidFileName();
            }
            else
            {
                _mVideoDirectory = tbVideoDirectory.Text;
            }
        }

        private void cmbImageFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            _mImageFormat = (KinoveaImageFormat)cmbImageFormat.SelectedIndex;
        }

        private void cmbVideoFormat_SelectedIndexChanged(object sender, EventArgs e)
        {
            _mVideoFormat = (KinoveaVideoFormat)cmbVideoFormat.SelectedIndex;
        }

        #endregion Tab general

        #region Tab naming

        private void tbPattern_TextChanged(object sender, EventArgs e)
        {
            if (_mFilenameHelper.ValidateFilename(tbPattern.Text, true))
            {
                UpdateSample();
            }
            else
            {
                ScreenManagerKernel.AlertInvalidFileName();
            }
        }

        private void btnMarker_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                var selStart = tbPattern.SelectionStart;
                tbPattern.Text = tbPattern.Text.Insert(selStart, btn.Text);
                tbPattern.SelectionStart = selStart + btn.Text.Length;
            }
        }

        private void lblMarker_Click(object sender, EventArgs e)
        {
            var lbl = sender as Label;
            if (lbl != null)
            {
                var macro = lbl.Tag as string;
                if (macro != null)
                {
                    var selStart = tbPattern.SelectionStart;
                    tbPattern.Text = tbPattern.Text.Insert(selStart, macro);
                    tbPattern.SelectionStart = selStart + macro.Length;
                }
            }
        }

        private void btnResetCounter_Click(object sender, EventArgs e)
        {
            _mBResetCounter = true;
            _mICounter = 1;
            UpdateSample();
        }

        private void radio_CheckedChanged(object sender, EventArgs e)
        {
            _mBUsePattern = rbPattern.Checked;
            EnableDisablePattern(_mBUsePattern);
        }

        #endregion Tab naming

        #region Tab Memory

        private void trkMemoryBuffer_ValueChanged(object sender, EventArgs e)
        {
            _mIMemoryBuffer = trkMemoryBuffer.Value;
            UpdateMemoryLabel();
        }

        #endregion Tab Memory

        #endregion Handlers

        #region Private methods

        private void UpdateSample()
        {
            var sample = _mFilenameHelper.ConvertPattern(tbPattern.Text, _mICounter);
            lblSample.Text = sample;
            _mPattern = tbPattern.Text;
        }

        private void EnableDisablePattern(bool bEnable)
        {
            tbPattern.Enabled = bEnable;
            lblSample.Enabled = bEnable;
            btnYear.Enabled = bEnable;
            btnMonth.Enabled = bEnable;
            btnDay.Enabled = bEnable;
            btnHour.Enabled = bEnable;
            btnMinute.Enabled = bEnable;
            btnSecond.Enabled = bEnable;
            btnIncrement.Enabled = bEnable;
            btnResetCounter.Enabled = bEnable;
            lblYear.Enabled = bEnable;
            lblMonth.Enabled = bEnable;
            lblDay.Enabled = bEnable;
            lblHour.Enabled = bEnable;
            lblMinute.Enabled = bEnable;
            lblSecond.Enabled = bEnable;
            lblCounter.Enabled = bEnable;
        }

        private void UpdateMemoryLabel()
        {
            lblMemoryBuffer.Text = string.Format(RootLang.dlgPreferences_Capture_lblMemoryBuffer, trkMemoryBuffer.Value);
        }

        #endregion Private methods
    }
}