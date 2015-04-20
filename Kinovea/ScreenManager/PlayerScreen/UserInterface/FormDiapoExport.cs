#region License

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

#endregion License

using Kinovea.ScreenManager.Languages;
using System;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     This dialog let the user configure a diaporama of the key images.
    ///     A diaporama here is a movie where each key image is seen for a lenghty period of time.
    ///     An other option is provided, for the creation of a more classic movie where each
    ///     key image is paused for a lengthy period of time. (but non key images are included in the video)
    ///     The dialog is only used to configure the interval time and file name.
    /// </summary>
    public partial class FormDiapoExport : Form
    {
        #region Members

        private readonly bool _mBDiaporama;

        #endregion Members

        #region OK / Cancel handler

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Hide/Close logic:
            // We start by hiding the current dialog.
            // If the user cancels on the file choosing dialog, we show back ourselves.

            Hide();

            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = ScreenManagerLang.dlgSaveVideoTitle;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.Filter = ScreenManagerLang.dlgSaveVideoFilterAlone;
            saveFileDialog.FilterIndex = 1;

            var result = DialogResult.Cancel;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var filePath = saveFileDialog.FileName;
                if (filePath.Length > 0)
                {
                    // Commit output props.
                    Filename = filePath;
                    FrameInterval = trkInterval.Value;

                    DialogResult = DialogResult.OK;
                    result = DialogResult.OK;
                }
            }

            if (result == DialogResult.OK)
            {
                Close();
            }
            else
            {
                Show();
            }
        }

        #endregion OK / Cancel handler

        #region Properties

        public string Filename { get; private set; }

        public double FrameInterval { get; private set; }

        public bool PausedVideo
        {
            get { return radioSavePausedVideo.Checked; }
        }

        #endregion Properties

        #region Construction and initialization

        public FormDiapoExport(bool diapo)
        {
            _mBDiaporama = diapo;

            InitializeComponent();

            SetupUiCulture();
            SetupData();
        }

        private void SetupUiCulture()
        {
            Text = "   " + ScreenManagerLang.CommandSaveMovie_FriendlyName;

            groupSaveMethod.Text = ScreenManagerLang.dlgDiapoExport_GroupDiapoType;
            radioSaveSlideshow.Text = ScreenManagerLang.dlgDiapoExport_RadioSlideshow;
            radioSavePausedVideo.Text = ScreenManagerLang.dlgDiapoExport_RadioPausedVideo;

            grpboxConfig.Text = ScreenManagerLang.Generic_Configuration;
            btnOK.Text = ScreenManagerLang.Generic_Save;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
        }

        private void SetupData()
        {
            // trkInterval values are in milliseconds.
            trkInterval.Minimum = 40;
            trkInterval.Maximum = 4000;
            trkInterval.Value = 2000;
            trkInterval.TickFrequency = 250;

            // default option
            if (_mBDiaporama)
            {
                radioSaveSlideshow.Checked = true;
            }
            else
            {
                radioSavePausedVideo.Checked = true;
            }
        }

        #endregion Construction and initialization

        #region Choice handler

        private void trkInterval_ValueChanged(object sender, EventArgs e)
        {
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            // Frequency
            var fInterval = (double)trkInterval.Value / 1000;
            if (fInterval < 1)
            {
                var iHundredth = (int)(fInterval * 100);
                lblInfosFrequency.Text = string.Format(ScreenManagerLang.dlgDiapoExport_LabelFrequencyHundredth,
                    iHundredth);
            }
            else
            {
                lblInfosFrequency.Text = string.Format(ScreenManagerLang.dlgDiapoExport_LabelFrequencySeconds, fInterval);
            }
        }

        #endregion Choice handler
    }
}