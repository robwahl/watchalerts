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

using Kinovea.ScreenManager.Languages;
using Kinovea.Services;
using System;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     The dialog lets the user configure the opacity option for image type drawing (SVG or Bitmap).
    ///     We work with the actual drawing to display the change in real time.
    ///     If the user decide to cancel, there's a "fallback to memo" mechanism.
    /// </summary>
    public partial class FormConfigureOpacity : Form
    {
        #region Members

        private bool _mBManualClose;

        private readonly PictureBox _mSurfaceScreen; // Used to update the image while configuring.
        private readonly AbstractDrawing _mDrawing; // Instance of the drawing we are modifying.
        private readonly InfosFading _mMemoInfosFading; // Memo to fallback to on cancel.

        #endregion Members

        #region Construction & Initialization

        public FormConfigureOpacity(AbstractDrawing drawing, PictureBox surfaceScreen)
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
            trkValue.Value = (int)Math.Ceiling(_mDrawing.InfosFading.MasterFactor * 100);
        }

        private void LocalizeForm()
        {
            Text = "   " + ScreenManagerLang.dlgConfigureOpacity_Title;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
            btnOK.Text = ScreenManagerLang.Generic_Apply;
            grpConfig.Text = ScreenManagerLang.Generic_Configuration;

            UpdateValueLabel();
        }

        #endregion Construction & Initialization

        #region User choices handlers

        private void trkValue_ValueChanged(object sender, EventArgs e)
        {
            _mDrawing.InfosFading.MasterFactor = (float)trkValue.Value / 100;
            UpdateValueLabel();
            _mSurfaceScreen.Invalidate();
        }

        private void UpdateValueLabel()
        {
            lblValue.Text = string.Format(ScreenManagerLang.dlgConfigureOpacity_lblValue,
                _mDrawing.InfosFading.MasterFactor * 100);
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

        private void formConfigureOpacity_FormClosing(object sender, FormClosingEventArgs e)
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