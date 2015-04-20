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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     The dialog lets the user configure a track instance.
    ///     Some of the logic is the same as for formConfigureDrawing.
    ///     Specifically, we work and update the actual instance in real time.
    ///     If the user finally decide to cancel there's a "fallback to memo" mechanism.
    /// </summary>
    public partial class FormConfigureTrajectoryDisplay : Form
    {
        #region Construction

        public FormConfigureTrajectoryDisplay(Track track, Action invalidate)
        {
            InitializeComponent();
            _mInvalidate = invalidate;
            _mTrack = track;
            _mTrack.DrawingStyle.ReadValue();

            // Save the current state in case of cancel.
            _mTrack.MemorizeState();
            _mTrack.DrawingStyle.Memorize();

            InitExtraDataCombo();
            SetupStyleControls();
            SetCurrentOptions();
            InitCulture();
        }

        #endregion Construction

        #region Members

        private bool _mBManualClose;
        private readonly Action _mInvalidate;
        private readonly Track _mTrack;
        private readonly List<AbstractStyleElement> _mElements = new List<AbstractStyleElement>();

        #endregion Members

        #region Init

        private void InitExtraDataCombo()
        {
            // Combo must be filled in the order of the enum.
            cmbExtraData.Items.Add(ScreenManagerLang.dlgConfigureTrajectory_ExtraData_None);
            cmbExtraData.Items.Add(ScreenManagerLang.dlgConfigureTrajectory_ExtraData_TotalDistance);
            cmbExtraData.Items.Add(ScreenManagerLang.dlgConfigureTrajectory_ExtraData_Speed);
        }

        private void SetupStyleControls()
        {
            // Dynamic loading of track styles but only semi dynamic UI (restricted to 3) for simplicity.
            // Styles should be Color, LineSize and TrackShape.
            foreach (var pair in _mTrack.DrawingStyle.Elements)
            {
                _mElements.Add(pair.Value);
            }

            if (_mElements.Count == 3)
            {
                var editorsLeft = 200;
                var lastEditorBottom = 10;
                var editorSize = new Size(60, 20);

                foreach (var styleElement in _mElements)
                {
                    styleElement.ValueChanged += element_ValueChanged;

                    var btn = new Button();
                    btn.Image = styleElement.Icon;
                    btn.Size = new Size(20, 20);
                    btn.Location = new Point(10, lastEditorBottom + 15);
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.BackColor = Color.Transparent;

                    var lbl = new Label();
                    lbl.Text = styleElement.DisplayName;
                    lbl.AutoSize = true;
                    lbl.Location = new Point(btn.Right + 10, lastEditorBottom + 20);

                    var miniEditor = styleElement.GetEditor();
                    miniEditor.Size = editorSize;
                    miniEditor.Location = new Point(editorsLeft, btn.Top);

                    lastEditorBottom = miniEditor.Bottom;

                    grpAppearance.Controls.Add(btn);
                    grpAppearance.Controls.Add(lbl);
                    grpAppearance.Controls.Add(miniEditor);
                }
            }
        }

        private void SetCurrentOptions()
        {
            // Current configuration.

            // General
            switch (_mTrack.View)
            {
                case TrackView.Focus:
                    radioFocus.Checked = true;
                    break;

                case TrackView.Label:
                    radioLabel.Checked = true;
                    break;

                case TrackView.Complete:
                default:
                    radioComplete.Checked = true;
                    break;
            }
            tbLabel.Text = _mTrack.Label;
            cmbExtraData.SelectedIndex = (int)_mTrack.ExtraData;
        }

        private void InitCulture()
        {
            Text = "   " + ScreenManagerLang.dlgConfigureTrajectory_Title;

            grpConfig.Text = ScreenManagerLang.Generic_Configuration;
            radioComplete.Text = ScreenManagerLang.dlgConfigureTrajectory_RadioComplete;
            radioFocus.Text = ScreenManagerLang.dlgConfigureTrajectory_RadioFocus;
            radioLabel.Text = ScreenManagerLang.dlgConfigureTrajectory_RadioLabel;
            lblLabel.Text = ScreenManagerLang.dlgConfigureChrono_Label;
            lblExtra.Text = ScreenManagerLang.dlgConfigureTrajectory_LabelExtraData;
            grpAppearance.Text = ScreenManagerLang.Generic_Appearance;
            btnOK.Text = ScreenManagerLang.Generic_Apply;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
        }

        #endregion Init

        #region Event handlers

        private void btnComplete_Click(object sender, EventArgs e)
        {
            radioComplete.Checked = true;
        }

        private void btnFocus_Click(object sender, EventArgs e)
        {
            radioFocus.Checked = true;
        }

        private void btnLabel_Click(object sender, EventArgs e)
        {
            radioLabel.Checked = true;
        }

        private void RadioViews_CheckedChanged(object sender, EventArgs e)
        {
            if (radioComplete.Checked)
            {
                _mTrack.View = TrackView.Complete;
            }
            else if (radioFocus.Checked)
            {
                _mTrack.View = TrackView.Focus;
            }
            else
            {
                _mTrack.View = TrackView.Label;
            }

            if (_mInvalidate != null) _mInvalidate();
        }

        private void tbLabel_TextChanged(object sender, EventArgs e)
        {
            _mTrack.Label = tbLabel.Text;
            if (_mInvalidate != null) _mInvalidate();
        }

        private void CmbExtraData_SelectedIndexChanged(object sender, EventArgs e)
        {
            _mTrack.ExtraData = (TrackExtraData)cmbExtraData.SelectedIndex;
            if (_mInvalidate != null) _mInvalidate();
        }

        private void element_ValueChanged(object sender, EventArgs e)
        {
            if (_mInvalidate != null) _mInvalidate();
        }

        #endregion Event handlers

        #region OK/Cancel/Closing

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_mBManualClose)
            {
                UnhookEvents();
                Revert();
            }
        }

        private void UnhookEvents()
        {
            // Unhook style event handlers
            foreach (var element in _mElements)
            {
                element.ValueChanged -= element_ValueChanged;
            }
        }

        private void Revert()
        {
            // Revert to memo and re-update data.
            _mTrack.DrawingStyle.Revert();
            _mTrack.DrawingStyle.RaiseValueChanged();
            _mTrack.RecallState();
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

        #endregion OK/Cancel/Closing
    }
}