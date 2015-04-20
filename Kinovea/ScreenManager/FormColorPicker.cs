#region License

/*
Copyright © Joan Charmant 2010.
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
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     FormColorPicker. Let the user choose a color.
    ///     The color picker control itself is based on the one from Greenshot.
    /// </summary>
    public partial class FormColorPicker : Form
    {
        #region Construction and Initialization

        public FormColorPicker()
        {
            SuspendLayout();
            InitializeComponent();
            _mColorPicker.Top = 5;
            _mColorPicker.Left = 5;
            _mColorPicker.ColorPicked += colorPicker_ColorPicked;

            Controls.Add(_mColorPicker);
            Text = "   " + ScreenManagerLang.dlgColorPicker_Title;
            ResumeLayout();

            // Recent colors.
            _mRecentColors = PreferencesManager.Instance().RecentColors;

            _mColorPicker.DisplayRecentColors(_mRecentColors);
            Height = _mColorPicker.Bottom + 20;
        }

        #endregion Construction and Initialization

        #region Properties

        public Color PickedColor { get; private set; }

        #endregion Properties

        #region event handlers

        private void colorPicker_ColorPicked(object sender, EventArgs e)
        {
            PickedColor = _mColorPicker.PickedColor;
            PreferencesManager.Instance().AddRecentColor(_mColorPicker.PickedColor);
            DialogResult = DialogResult.OK;
            Close();
        }

        #endregion event handlers

        #region Members

        private readonly ColorPicker _mColorPicker = new ColorPicker();
        private readonly List<Color> _mRecentColors;

        #endregion Members
    }
}