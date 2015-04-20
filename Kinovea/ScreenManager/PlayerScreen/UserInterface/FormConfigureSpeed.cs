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
using System;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     This dialog let the user specify the original speed of the camera used.
    ///     This is used when the camera was filming at say, 1000 fps,
    ///     and the resulting movie created at 24 fps.
    ///     The result of this dialog is only a change in the way we compute the times.
    ///     The value stored in the PlayerScreen UI is not updated in real time.
    /// </summary>
    public partial class FormConfigureSpeed : Form
    {
        #region Properties

        public double SlowFactor
        {
            get
            {
                if (_mFRealWorldFps < 1)
                {
                    // Fall back to original.
                    return _mFSlowFactor;
                }
                return _mFRealWorldFps / _mFVideoFps;
            }
        }

        #endregion Properties

        #region Members

        private readonly double _mFVideoFps; // This is the fps read in the video. (ex: 24 fps)
        private double _mFRealWorldFps; // The current fps modified value (ex: 1000 fps).
        private double _mFSlowFactor; // The current slow factor. (if we already used the dialog)

        #endregion Members

        #region Construction & Initialization

        public FormConfigureSpeed(double fFps, double fSlowFactor)
        {
            _mFSlowFactor = fSlowFactor;
            _mFVideoFps = fFps;
            _mFRealWorldFps = _mFVideoFps * _mFSlowFactor;

            InitializeComponent();
            LocalizeForm();
        }

        private void LocalizeForm()
        {
            Text = "   " + ScreenManagerLang.dlgConfigureSpeed_Title;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
            btnOK.Text = ScreenManagerLang.Generic_Apply;
            grpConfig.Text = ScreenManagerLang.Generic_Configuration;
            lblFPSCaptureTime.Text = ScreenManagerLang.dlgConfigureSpeed_lblFPSCaptureTime.Replace("\\n", "\n");
            toolTips.SetToolTip(btnReset, ScreenManagerLang.dlgConfigureSpeed_ToolTip_Reset);

            // Update text box with current value. (Will update computed values too)
            tbFPSRealWorld.Text = string.Format("{0:0.00}", _mFRealWorldFps);
        }

        #endregion Construction & Initialization

        #region User choices handlers

        private void UpdateValues()
        {
            lblFPSDisplayTime.Text = string.Format(ScreenManagerLang.dlgConfigureSpeed_lblFPSDisplayTime, _mFVideoFps);
            var timesSlower = (int)(_mFRealWorldFps / _mFVideoFps);
            lblSlowFactor.Visible = timesSlower > 1;
            lblSlowFactor.Text = string.Format(ScreenManagerLang.dlgConfigureSpeed_lblSlowFactor, timesSlower);
        }

        private void tbFPSRealWorld_TextChanged(object sender, EventArgs e)
        {
            try
            {
                // FIXME: check how this play with culture variations on decimal separator.
                _mFRealWorldFps = double.Parse(tbFPSRealWorld.Text);
                if (_mFRealWorldFps > 2000)
                {
                    tbFPSRealWorld.Text = "2000";
                }
                else if (_mFRealWorldFps < 1)
                {
                    _mFRealWorldFps = _mFVideoFps;
                }
            }
            catch
            {
                // Failed : do nothing.
            }

            UpdateValues();
        }

        private void tbFPSRealWorld_KeyPress(object sender, KeyPressEventArgs e)
        {
            // We only accept numbers, points and coma in there.
            var key = e.KeyChar;
            if (((key < '0') || (key > '9')) && (key != ',') && (key != '.') && (key != '\b'))
            {
                e.Handled = true;
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            // Fall back To original.
            tbFPSRealWorld.Text = string.Format("{0:0.00}", _mFVideoFps);

            // Force proper reset of values, as the text may lack full precision.
            _mFRealWorldFps = _mFVideoFps;
            _mFSlowFactor = 1.0;
        }

        #endregion User choices handlers
    }
}