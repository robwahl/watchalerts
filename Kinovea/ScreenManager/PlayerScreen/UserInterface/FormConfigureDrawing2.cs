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
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     Dialog that let the user change the style of a specific drawing.
    ///     Works on the original and revert to a copy in case of cancel.
    /// </summary>
    public partial class FormConfigureDrawing2 : Form
    {
        #region Constructor

        public FormConfigureDrawing2(DrawingStyle style, Action invalidate)
        {
            _mStyle = style;
            _mStyle.ReadValue();
            _mStyle.Memorize();
            _mInvalidate = invalidate;
            InitializeComponent();
            LocalizeForm();
            SetupForm();
        }

        #endregion Constructor

        #region Members

        private readonly DrawingStyle _mStyle;
        private readonly Action _mInvalidate;
        private bool _mBManualClose;
        private readonly List<AbstractStyleElement> _mElements = new List<AbstractStyleElement>();
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Private Methods

        private void LocalizeForm()
        {
            Text = "   " + ScreenManagerLang.dlgConfigureDrawing_Title;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
            btnOK.Text = ScreenManagerLang.Generic_Apply;
            grpConfig.Text = ScreenManagerLang.Generic_Configuration;
        }

        private void SetupForm()
        {
            // Dynamic layout:
            // Any number of mini editor lines. (must scale vertically)
            // High dpi vs normal dpi (scales vertically and horizontally)
            // Verbose languages (scales horizontally)

            // Clean up
            grpConfig.Controls.Clear();
            var helper = grpConfig.CreateGraphics();

            var editorSize = new Size(60, 20);
            // Initialize the horizontal layout with a minimal value,
            // it will be fixed later if some of the entries have long text.
            var minimalWidth = btnOK.Width + btnCancel.Width + 10;
            var editorsLeft = minimalWidth - 20 - editorSize.Width;

            var lastEditorBottom = 10;

            foreach (var pair in _mStyle.Elements)
            {
                var styleElement = pair.Value;
                _mElements.Add(styleElement);

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

                var labelSize = helper.MeasureString(lbl.Text, lbl.Font);

                if (lbl.Left + labelSize.Width + 25 > editorsLeft)
                {
                    // dynamic horizontal layout for high dpi and verbose languages.
                    editorsLeft = (int)(lbl.Left + labelSize.Width + 25);
                }

                var miniEditor = styleElement.GetEditor();
                miniEditor.Size = editorSize;
                miniEditor.Location = new Point(editorsLeft, btn.Top);

                lastEditorBottom = miniEditor.Bottom;

                grpConfig.Controls.Add(btn);
                grpConfig.Controls.Add(lbl);
                grpConfig.Controls.Add(miniEditor);
            }

            helper.Dispose();

            // Recheck all mini editors for the left positionning.
            foreach (Control c in grpConfig.Controls)
            {
                if (!(c is Label) && !(c is Button))
                {
                    if (c.Left < editorsLeft) c.Left = editorsLeft;
                }
            }

            grpConfig.Height = lastEditorBottom + 20;
            grpConfig.Width = editorsLeft + editorSize.Width + 20;

            btnOK.Top = grpConfig.Bottom + 10;
            btnOK.Left = grpConfig.Right - (btnCancel.Width + 10 + btnOK.Width);
            btnCancel.Top = btnOK.Top;
            btnCancel.Left = btnOK.Right + 10;

            var borderLeft = Width - ClientRectangle.Width;
            Width = borderLeft + btnCancel.Right + 10;

            var borderTop = Height - ClientRectangle.Height;
            Height = borderTop + btnOK.Bottom + 10;
        }

        private void element_ValueChanged(object sender, EventArgs e)
        {
            if (_mInvalidate != null) _mInvalidate();
        }

        #region Closing

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
            // Unhook event handlers
            foreach (var element in _mElements)
            {
                element.ValueChanged -= element_ValueChanged;
            }
        }

        private void Revert()
        {
            // Revert to memo and re-update data.
            _mStyle.Revert();
            _mStyle.RaiseValueChanged();

            // Update main UI.
            if (_mInvalidate != null) _mInvalidate();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            UnhookEvents();
            Revert();
            _mBManualClose = true;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            UnhookEvents();
            _mBManualClose = true;
        }

        #endregion Closing

        #endregion Private Methods
    }
}