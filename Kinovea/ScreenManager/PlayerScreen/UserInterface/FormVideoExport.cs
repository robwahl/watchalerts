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
    /// <summary>
    ///     A dialog to help the user on what to save and how.
    /// </summary>
    public partial class FormVideoExport : Form
    {
        #region Public methods

        public DialogResult Spawn()
        {
            // We use this method instead of directly calling ShowDialog()
            // in order to catch for the special case where the user has no choice.
            if (!_mMetadata.HasData && _mFSlowmotionPercentage == 100)
            {
                // Directly ask for a filename.
                return DoSaveVideo();
            }
            return ShowDialog();
        }

        #endregion Public methods

        #region Properties

        public bool SaveAnalysis { get; private set; }

        public bool BlendDrawings { get; private set; }

        public bool MuxDrawings { get; private set; }

        public string Filename { get; private set; }

        public bool UseSlowMotion { get; private set; }

        #endregion Properties

        #region Members

        //private PlayerScreen m_PlayerScreen;

        private readonly string _mOriginalFilename;
        private readonly double _mFSlowmotionPercentage;
        private readonly Metadata _mMetadata;

        #endregion Members

        #region constructor and initialisation

        public FormVideoExport(string originalFilename, Metadata metadata, double fSlowmotionPercentage)
        {
            _mFSlowmotionPercentage = fSlowmotionPercentage;
            _mMetadata = metadata;
            _mOriginalFilename = originalFilename;

            InitializeComponent();

            if (_mFSlowmotionPercentage == 100)
            {
                groupOptions.Visible = false;
                Height -= groupOptions.Height;
            }

            InitCulture();
        }

        private void InitCulture()
        {
            Text = "   " + ScreenManagerLang.dlgSaveAnalysisOrVideo_Title;
            groupSaveMethod.Text = ScreenManagerLang.dlgSaveAnalysisOrVideo_GroupSaveMethod;
            radioSaveMuxed.Text = ScreenManagerLang.dlgSaveAnalysisOrVideo_RadioMuxed;
            tbSaveMuxed.Lines = ScreenManagerLang.dlgSaveAnalysisOrVideo_HintMuxed.Split('#');
            radioSaveBlended.Text = ScreenManagerLang.dlgSaveAnalysisOrVideo_RadioBlended;
            tbSaveBlended.Lines = ScreenManagerLang.dlgSaveAnalysisOrVideo_HintBlended.Split('#');
            radioSaveAnalysis.Text = ScreenManagerLang.dlgSaveAnalysisOrVideo_RadioAnalysis;
            tbSaveAnalysis.Lines = ScreenManagerLang.dlgSaveAnalysisOrVideo_HintAnalysis.Split('#');

            groupOptions.Text = ScreenManagerLang.dlgSaveAnalysisOrVideo_GroupOptions;
            checkSlowMotion.Text = ScreenManagerLang.dlgSaveAnalysisOrVideo_CheckSlow;
            checkSlowMotion.Text = checkSlowMotion.Text + _mFSlowmotionPercentage + "%).";

            btnOK.Text = ScreenManagerLang.Generic_Save;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;

            EnableDisableOptions();

            // default option
            radioSaveMuxed.Checked = true;
        }

        #endregion constructor and initialisation

        #region event handlers

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Hide/Close logic:
            // We start by hiding the current dialog.
            // If the user cancels on the file choosing dialog, we show back ourselves.

            Hide();
            DialogResult dr;

            if (radioSaveAnalysis.Checked)
            {
                dr = DoSaveAnalysis();
            }
            else
            {
                dr = DoSaveVideo();
            }

            if (dr == DialogResult.OK)
            {
                Close();
            }
            else
            {
                //If cancelled, we display the wizard again.
                Show();
            }
        }

        private void BtnSaveAnalysisClick(object sender, EventArgs e)
        {
            UncheckAllOptions();
            radioSaveAnalysis.Checked = true;
        }

        private void BtnSaveMuxedClick(object sender, EventArgs e)
        {
            UncheckAllOptions();
            radioSaveMuxed.Checked = true;
        }

        private void BtnSaveBothClick(object sender, EventArgs e)
        {
            UncheckAllOptions();
            radioSaveBlended.Checked = true;
        }

        private void radio_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableOptions();
        }

        #endregion event handlers

        #region lower levels helpers

        private void EnableDisableOptions()
        {
            radioSaveMuxed.Enabled = true;
            radioSaveBlended.Enabled = true;
            radioSaveAnalysis.Enabled = true;
            checkSlowMotion.Enabled = radioSaveAnalysis.Checked ? false : (_mFSlowmotionPercentage != 100);
        }

        private void UncheckAllOptions()
        {
            radioSaveAnalysis.Checked = false;
            radioSaveMuxed.Checked = false;
            radioSaveBlended.Checked = false;
        }

        private DialogResult DoSaveVideo()
        {
            //--------------------------------------------------------------------------
            // Save Video file. (Either Alone or along with the Analysis muxed into it.)
            //--------------------------------------------------------------------------
            var result = DialogResult.Cancel;
            string filePath = null;

            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = ScreenManagerLang.dlgSaveVideoTitle;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.FilterIndex = 1;

            if (radioSaveMuxed.Checked)
            {
                saveFileDialog.Filter = ScreenManagerLang.dlgSaveVideoFilterMuxed;
            }
            else
            {
                saveFileDialog.Filter = ScreenManagerLang.dlgSaveVideoFilterAlone;
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = saveFileDialog.FileName;
                if (filePath.Length > 0)
                {
                    // Commit to output props.
                    SaveAnalysis = false;
                    Filename = filePath;
                    BlendDrawings = radioSaveBlended.Checked;
                    MuxDrawings = radioSaveMuxed.Checked;
                    UseSlowMotion = checkSlowMotion.Checked;

                    DialogResult = DialogResult.OK;
                    result = DialogResult.OK;
                }
            }
            return result;
        }

        private DialogResult DoSaveAnalysis()
        {
            // Analysis only.
            var result = DialogResult.Cancel;
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = ScreenManagerLang.dlgSaveAnalysisTitle;

            // Goto this video directory and suggest filename for saving.
            saveFileDialog.InitialDirectory = Path.GetDirectoryName(_mOriginalFilename);
            saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_mOriginalFilename);
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.Filter = ScreenManagerLang.dlgSaveAnalysisFilter;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var filePath = saveFileDialog.FileName;
                if (filePath.Length > 0)
                {
                    if (!filePath.ToLower().EndsWith(".kva") && !filePath.ToLower().EndsWith(".xml"))
                    {
                        filePath = filePath + ".kva";
                    }

                    // Commit output props
                    Filename = filePath;
                    SaveAnalysis = true;
                    DialogResult = DialogResult.OK;
                    result = DialogResult.OK;
                }
            }

            return result;
        }

        #endregion lower levels helpers
    }
}