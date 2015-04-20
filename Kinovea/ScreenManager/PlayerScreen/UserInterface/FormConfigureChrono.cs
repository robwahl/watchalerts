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
using log4net;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     The dialog lets the user configure a chronometer instance.
    ///     Some of the logic is the same as for formConfigureDrawing.
    ///     Specifically, we work and update the actual instance in real time.
    ///     If the user finally decide to cancel there's a "fallback to memo" mechanism.
    /// </summary>
    public partial class FormConfigureChrono : Form
    {
        #region Construction

        public FormConfigureChrono(DrawingChrono chrono, Action invalidate)
        {
            InitializeComponent();
            _mInvalidate = invalidate;
            _mChrono = chrono;
            _mChrono.DrawingStyle.ReadValue();
            _mChrono.DrawingStyle.Memorize();
            _mMemoLabel = _mChrono.Label;
            _mBMemoShowLabel = _mChrono.ShowLabel;

            SetupForm();
            LocalizeForm();

            tbLabel.Text = _mChrono.Label;
            chkShowLabel.Checked = _mChrono.ShowLabel;
        }

        #endregion Construction

        #region Members

        private bool _mBManualClose;
        private readonly Action _mInvalidate;
        private readonly DrawingChrono _mChrono;
        private readonly string _mMemoLabel;
        private readonly bool _mBMemoShowLabel;
        private AbstractStyleElement _mFirstElement;
        private AbstractStyleElement _mSecondElement;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Private Methods

        private void SetupForm()
        {
            foreach (var styleElement in _mChrono.DrawingStyle.Elements)
            {
                if (_mFirstElement == null)
                {
                    _mFirstElement = styleElement.Value;
                }
                else if (_mSecondElement == null)
                {
                    _mSecondElement = styleElement.Value;
                }
                else
                {
                    Log.DebugFormat("Discarding style element: \"{0}\". (Only 2 style elements supported).",
                        styleElement.Key);
                }
            }

            // Configure editor line for each element.
            // The style element is responsible for updating the internal value and the editor appearance.
            // The element internal value might also be bound to a style helper property so that the underlying drawing will get updated.
            if (_mFirstElement != null)
            {
                lblFirstElement.Text = _mFirstElement.DisplayName;
                _mFirstElement.ValueChanged += element_ValueChanged;

                var editorsLeft = 150; // works in High DPI ?

                var firstEditor = _mFirstElement.GetEditor();
                firstEditor.Size = new Size(50, 20);
                firstEditor.Location = new Point(editorsLeft, lblFirstElement.Top - 3);
                grpConfig.Controls.Add(firstEditor);

                if (_mSecondElement != null)
                {
                    lblSecondElement.Text = _mSecondElement.DisplayName;
                    _mSecondElement.ValueChanged += element_ValueChanged;

                    var secondEditor = _mSecondElement.GetEditor();
                    secondEditor.Size = new Size(50, 20);
                    secondEditor.Location = new Point(editorsLeft, lblSecondElement.Top - 3);
                    grpConfig.Controls.Add(secondEditor);
                }
                else
                {
                    lblSecondElement.Visible = false;
                }
            }
            else
            {
                lblFirstElement.Visible = false;
            }
        }

        private void LocalizeForm()
        {
            Text = "   " + ScreenManagerLang.dlgConfigureChrono_Title;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
            btnOK.Text = ScreenManagerLang.Generic_Apply;
            grpConfig.Text = ScreenManagerLang.Generic_Configuration;

            lblLabel.Text = ScreenManagerLang.dlgConfigureChrono_Label;
            chkShowLabel.Text = ScreenManagerLang.dlgConfigureChrono_chkShowLabel;
        }

        private void element_ValueChanged(object sender, EventArgs e)
        {
            if (_mInvalidate != null) _mInvalidate();
        }

        private void tbLabel_TextChanged(object sender, EventArgs e)
        {
            _mChrono.Label = tbLabel.Text;
            if (_mInvalidate != null) _mInvalidate();
        }

        private void chkShowLabel_CheckedChanged(object sender, EventArgs e)
        {
            _mChrono.ShowLabel = chkShowLabel.Checked;
            if (_mInvalidate != null) _mInvalidate();
        }

        #endregion Private Methods

        #region Closing

        private void UnhookEvents()
        {
            // Unhook event handlers
            if (_mFirstElement != null)
            {
                _mFirstElement.ValueChanged -= element_ValueChanged;
            }

            if (_mSecondElement != null)
            {
                _mSecondElement.ValueChanged -= element_ValueChanged;
            }
        }

        private void Revert()
        {
            // Revert to memo and re-update data.
            _mChrono.DrawingStyle.Revert();
            _mChrono.DrawingStyle.RaiseValueChanged();
            _mChrono.Label = _mMemoLabel;
            _mChrono.ShowLabel = _mBMemoShowLabel;

            if (_mInvalidate != null) _mInvalidate();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            UnhookEvents();
            _mBManualClose = true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            UnhookEvents();
            Revert();
            _mBManualClose = true;
        }

        private void formConfigureChrono_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_mBManualClose)
            {
                UnhookEvents();
                Revert();
            }
        }

        #endregion Closing
    }
}