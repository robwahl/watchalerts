#region License

/*
Copyright © Joan Charmant 2008-2009.
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
    ///     FormProgressBar is a simple form to display a progress bar.
    ///     The progress is computed outside and communicated through Update() method.
    ///     See AbstractVideoFilter for usage sample.
    /// </summary>
    public partial class FormProgressBar : Form
    {
        #region Callbacks

        public EventHandler Cancel;

        #endregion Callbacks

        #region Constructor

        public FormProgressBar(bool isCancellable)
        {
            InitializeComponent();
            Application.Idle += IdleDetector;
            btnCancel.Visible = isCancellable;

            // Culture
            Text = "   " + ScreenManagerLang.FormProgressBar_Title;
            labelInfos.Text = ScreenManagerLang.FormFileSave_Infos + " 0 / ~?";
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
        }

        #endregion Constructor

        #region Members

        private bool _mIsIdle;
        private bool _mBIsCancelling;

        #endregion Members

        #region Methods

        private void IdleDetector(object sender, EventArgs e)
        {
            _mIsIdle = true;
        }

        public void Update(int iValue, int iMaximum, bool bAsPercentage)
        {
            if (_mIsIdle && !_mBIsCancelling)
            {
                _mIsIdle = false;

                progressBar.Maximum = iMaximum;
                progressBar.Value = iValue > 0 ? iValue : 0;

                if (bAsPercentage)
                {
                    labelInfos.Text = ScreenManagerLang.FormFileSave_Infos + " " + (iValue * 100) / iMaximum + "%";
                }
                else
                {
                    labelInfos.Text = ScreenManagerLang.FormFileSave_Infos + " " + iValue + " / ~" + iMaximum;
                }
            }
        }

        #endregion Methods

        #region Events

        private void formProgressBar_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Idle -= IdleDetector;
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            // User clicked on cancel, trigger the callback that will cancel the ongoing operation.
            btnCancel.Enabled = false;
            _mBIsCancelling = true;
            if (Cancel != null) Cancel(this, EventArgs.Empty);
        }

        #endregion Events
    }
}