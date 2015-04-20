#region License

/*
Copyright © Joan Charmant 2009.
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
using Kinovea.Services;
using System;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     The dialog lets the user configure the fading / persistence option for a given drawing.
    ///     We work with the actual drawing to display the change in real time.
    ///     If the user decide to cancel, there's a "fallback to memo" mechanism.
    /// </summary>
    public partial class FormConfigureFading : Form
    {
        #region Members

        private bool _mBManualClose;

        private readonly PictureBox _mSurfaceScreen; // Used to update the image while configuring.
        private readonly AbstractDrawing _mDrawing; // Instance of the drawing we are modifying.
        private readonly InfosFading _mMemoInfosFading; // Memo to fallback to on cancel.

        #endregion Members

        #region Construction & Initialization

        public FormConfigureFading(AbstractDrawing drawing, PictureBox surfaceScreen)
        {
            _mSurfaceScreen = surfaceScreen;
            _mDrawing = drawing;
            _mMemoInfosFading = drawing.InfosFading.Clone();

            InitializeComponent();
            ConfigureForm();
            LocalizeForm();
        }

        private void ConfigureForm()
        {
            // Display current values.
            var pm = PreferencesManager.Instance();
            trkValue.Maximum = pm.MaxFading;
            trkValue.Value = Math.Min(_mDrawing.InfosFading.FadingFrames, trkValue.Maximum);
            chkDefault.Checked = _mDrawing.InfosFading.UseDefault;
            chkAlwaysVisible.Checked = _mDrawing.InfosFading.AlwaysVisible;
            chkEnable.Checked = _mDrawing.InfosFading.Enabled;
        }

        private void LocalizeForm()
        {
            Text = "   " + ScreenManagerLang.dlgConfigureFading_Title;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
            btnOK.Text = ScreenManagerLang.Generic_Apply;
            grpConfig.Text = ScreenManagerLang.Generic_Configuration;

            chkEnable.Text = ScreenManagerLang.dlgConfigureFading_chkEnable;

            var info = PreferencesManager.Instance().DefaultFading;
            if (info.AlwaysVisible)
            {
                chkDefault.Text = ScreenManagerLang.dlgConfigureFading_chkDefault;
            }
            else
            {
                chkDefault.Text = ScreenManagerLang.dlgConfigureFading_chkDefault +
                                  string.Format("({0})", Math.Min(info.FadingFrames, trkValue.Maximum));
            }

            UpdateValueLabel();
            chkAlwaysVisible.Text = ScreenManagerLang.dlgConfigureFading_chkAlwaysVisible;
        }

        #endregion Construction & Initialization

        #region User choices handlers

        private void chkEnable_CheckedChanged(object sender, EventArgs e)
        {
            _mDrawing.InfosFading.Enabled = chkEnable.Checked;
            EnableDisable();
            _mSurfaceScreen.Invalidate();
        }

        private void chkDefault_CheckedChanged(object sender, EventArgs e)
        {
            _mDrawing.InfosFading.UseDefault = chkDefault.Checked;
            EnableDisable();
            _mSurfaceScreen.Invalidate();
        }

        private void chkAlwaysVisible_CheckedChanged(object sender, EventArgs e)
        {
            _mDrawing.InfosFading.AlwaysVisible = chkAlwaysVisible.Checked;
            EnableDisable();
            _mSurfaceScreen.Invalidate();
        }

        private void trkValue_ValueChanged(object sender, EventArgs e)
        {
            _mDrawing.InfosFading.FadingFrames = trkValue.Value;
            UpdateValueLabel();
            chkAlwaysVisible.Checked = false;
            _mSurfaceScreen.Invalidate();
        }

        private void UpdateValueLabel()
        {
            int val = Math.Min(trkValue.Maximum, _mDrawing.InfosFading.FadingFrames);
            lblValue.Text = string.Format(ScreenManagerLang.dlgConfigureFading_lblValue, val);
        }

        private void EnableDisable()
        {
            if (!chkEnable.Checked)
            {
                chkDefault.Enabled = false;
                lblValue.Enabled = false;
                trkValue.Enabled = false;
                chkAlwaysVisible.Enabled = false;
            }
            else if (chkDefault.Checked)
            {
                chkDefault.Enabled = true;
                lblValue.Enabled = false;
                trkValue.Enabled = false;
                chkAlwaysVisible.Enabled = false;
            }
            else
            {
                // We keep the slider enabled even when the user checked "Always visible".
                // We will automatically uncheck it if he moves the slider.
                chkDefault.Enabled = true;
                lblValue.Enabled = true;
                trkValue.Enabled = true;
                chkAlwaysVisible.Enabled = true;
            }
        }

        #endregion User choices handlers

        #region OK/Cancel Handlers

        private void btnOK_Click(object sender, EventArgs e)
        {
            // Nothing special to do, the drawing has already been updated.
            _mBManualClose = true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // Fall back to memo.
            _mDrawing.InfosFading = _mMemoInfosFading.Clone();
            _mSurfaceScreen.Invalidate();
            _mBManualClose = true;
        }

        private void formConfigureFading_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_mBManualClose)
            {
                _mDrawing.InfosFading = _mMemoInfosFading.Clone();
                _mSurfaceScreen.Invalidate();
            }
        }

        #endregion OK/Cancel Handlers
    }
}