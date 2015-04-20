/*
Copyright © Joan Charmant 2008.
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

using Kinovea.ScreenManager.Languages;
using System;
using System.IO;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public partial class FormRafaleExport : Form
    {
        public FormRafaleExport(PlayerScreenUserInterface psui, Metadata metadata, string fullPath, long iSelDuration,
            double tsps)
        {
            _mPlayerScreenUserInterface = psui;
            _mMetadata = metadata;
            _mFullPath = fullPath;
            _mISelectionDuration = iSelDuration;
            _mFTimestampsPerSeconds = tsps;
            _mFDurationInSeconds = _mISelectionDuration / _mFTimestampsPerSeconds;
            _mIEstimatedTotal = 0;

            InitializeComponent();
            SetupUiCulture();
            SetupData();
        }

        private void SetupUiCulture()
        {
            // Window
            Text = "   " + ScreenManagerLang.dlgRafaleExport_Title;

            // Group Config
            grpboxConfig.Text = ScreenManagerLang.Generic_Configuration;
            chkBlend.Text = ScreenManagerLang.dlgRafaleExport_LabelBlend;
            chkKeyframesOnly.Text = ScreenManagerLang.dlgRafaleExport_LabelKeyframesOnly;
            if (_mMetadata.Count > 0)
            {
                chkKeyframesOnly.Enabled = true;
            }
            else
            {
                chkKeyframesOnly.Enabled = false;
            }

            // Group Infos
            grpboxInfos.Text = ScreenManagerLang.dlgRafaleExport_GroupInfos;
            lblInfosTotalFrames.Text = ScreenManagerLang.dlgRafaleExport_LabelTotalFrames;
            lblInfosFileSuffix.Text = ScreenManagerLang.dlgRafaleExport_LabelInfoSuffix;
            lblInfosTotalSeconds.Text = string.Format(ScreenManagerLang.dlgRafaleExport_LabelTotalSeconds,
                _mFDurationInSeconds);

            // Buttons
            btnOK.Text = ScreenManagerLang.Generic_Save;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
        }

        private void SetupData()
        {
            // trkInterval values are in milliseconds.
            trkInterval.Minimum = 40;
            trkInterval.Maximum = 8000;
            trkInterval.Value = 1000;
            trkInterval.TickFrequency = 250;
        }

        private void trkInterval_ValueChanged(object sender, EventArgs e)
        {
            freqViewer.Interval = trkInterval.Value;
            UpdateLabels();
        }

        private void chkKeyframesOnly_CheckedChanged(object sender, EventArgs e)
        {
            if (chkKeyframesOnly.Checked)
            {
                trkInterval.Enabled = false;
                chkBlend.Checked = true;
            }
            else
            {
                trkInterval.Enabled = true;
                chkBlend.Checked = true;
            }
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            // Frequency
            var fInterval = (double)trkInterval.Value / 1000;
            lblInfosFrequency.Text = ScreenManagerLang.dlgRafaleExport_LabelFrequencyRoot + " ";
            if (fInterval < 1)
            {
                var iHundredth = (int)(fInterval * 100);
                lblInfosFrequency.Text += string.Format(ScreenManagerLang.dlgRafaleExport_LabelFrequencyHundredth,
                    iHundredth);
            }
            else
            {
                lblInfosFrequency.Text += string.Format(ScreenManagerLang.dlgRafaleExport_LabelFrequencySeconds,
                    fInterval);
            }

            // Number of frames
            double fTotalFrames;
            if (chkKeyframesOnly.Checked)
            {
                fTotalFrames = _mMetadata.Count;
            }
            else
            {
                fTotalFrames = (_mFDurationInSeconds * (1 / fInterval)) + 0.5;
            }
            _mIEstimatedTotal = (int)fTotalFrames;

            lblInfosTotalFrames.Text = string.Format(ScreenManagerLang.dlgRafaleExport_LabelTotalFrames, fTotalFrames);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = ScreenManagerLang.dlgSaveSequenceTitle;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.Filter = ScreenManagerLang.dlgSaveFilter;
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_mFullPath);

            Hide();
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var filePath = saveFileDialog.FileName;
                if (filePath.Length > 0)
                {
                    var iIntervalTimeStamps = (long)(((double)trkInterval.Value / 1000) * _mFTimestampsPerSeconds);

                    // Launch the Progress bar dialog that will trigger the export.
                    // it will call the real function (in PlayerServerUI)
                    var ffe = new FormFramesExport(_mPlayerScreenUserInterface, filePath, iIntervalTimeStamps,
                        chkBlend.Checked, chkKeyframesOnly.Checked, _mIEstimatedTotal);
                    ffe.ShowDialog();
                    ffe.Dispose();
                }
                Close();
            }
            else
            {
                Show();
            }
        }

        //----------------------------------------------------------
        // /!\ The interval slider is in thousandth of seconds. (ms)
        //----------------------------------------------------------

        #region Members

        private readonly PlayerScreenUserInterface _mPlayerScreenUserInterface; // parent
        private readonly Metadata _mMetadata;
        private readonly string _mFullPath;
        private readonly long _mISelectionDuration; // in timestamps.
        private readonly double _mFTimestampsPerSeconds; // ratio
        private readonly double _mFDurationInSeconds;
        private int _mIEstimatedTotal;

        #endregion Members
    }
}