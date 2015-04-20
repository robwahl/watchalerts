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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     ColorPicker. Let the user choose a color.
    ///     This color picker is heavily inspired by the Color Picker from Greenshot.
    ///     The code for generating the palette is taken from Greenshot with almost no modifications.
    ///     http://greenshot.sourceforge.net/
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        #region Properties

        public Color PickedColor { get; private set; }

        #endregion Properties

        #region Events

        [Category("Action"), Browsable(true)]
        public event EventHandler ColorPicked;

        #endregion Events

        public void DisplayRecentColors(List<Color> recentColors)
        {
            if (recentColors.Count > 0)
            {
                var lblRecent = new Label();
                lblRecent.AutoSize = true;

                lblRecent.Text = ScreenManagerLang.RecentlyUsedColors;
                lblRecent.Top = Margin.Top + (11 * MIButtonSize) + 30;
                Controls.Add(lblRecent);

                var recentButtons = new List<Button>();
                var x = 0;
                var y = lblRecent.Bottom + 5;
                for (var i = 0; i < recentColors.Count; i++)
                {
                    var b = createColorButton(recentColors[i], x, y, MIButtonSize, MIButtonSize);
                    recentButtons.Add(b);
                    x += MIButtonSize;
                }

                Controls.AddRange(recentButtons.ToArray());
            }
        }

        #region Members

        private readonly List<Button> _mColorButtons = new List<Button>();
        private static readonly int MIButtonSize = 15;

        #endregion Members

        #region Construction and Initialization

        public ColorPicker()
        {
            SuspendLayout();
            InitializeComponent();
            GeneratePalette(0, 0, MIButtonSize, MIButtonSize);
            ResumeLayout();
        }

        private void GeneratePalette(int left, int top, int buttonWidth, int buttonHeight)
        {
            var shades = 11;

            CreateColorButtonColumn(255, 0, 0, left, top, buttonWidth, buttonHeight, shades);

            left += buttonWidth;
            CreateColorButtonColumn(255, 255 / 2, 0, left, top, buttonWidth, buttonHeight, shades);

            left += buttonWidth;
            CreateColorButtonColumn(255, 255, 0, left, top, buttonWidth, buttonHeight, shades);

            left += buttonWidth;
            CreateColorButtonColumn(255 / 2, 255, 0, left, top, buttonWidth, buttonHeight, shades);

            left += buttonWidth;
            CreateColorButtonColumn(0, 255, 0, left, top, buttonWidth, buttonHeight, shades);

            left += buttonWidth;
            CreateColorButtonColumn(0, 255, 255 / 2, left, top, buttonWidth, buttonHeight, shades);

            left += buttonWidth;
            CreateColorButtonColumn(0, 255, 255, left, top, buttonWidth, buttonHeight, shades);

            left += buttonWidth;
            CreateColorButtonColumn(0, 255 / 2, 255, left, top, buttonWidth, buttonHeight, shades);

            left += buttonWidth;
            CreateColorButtonColumn(0, 0, 255, left, top, buttonWidth, buttonHeight, shades);

            left += buttonWidth;
            CreateColorButtonColumn(255 / 2, 0, 255, left, top, buttonWidth, buttonHeight, shades);

            left += buttonWidth;
            CreateColorButtonColumn(255, 0, 255, left, top, buttonWidth, buttonHeight, shades);

            left += buttonWidth;
            CreateColorButtonColumn(255, 0, 255 / 2, left, top, buttonWidth, buttonHeight, shades);

            // Grayscale column.
            left += buttonWidth + 5;
            CreateColorButtonColumn(255 / 2, 255 / 2, 255 / 2, left, top, buttonWidth, buttonHeight, shades);

            Controls.AddRange(_mColorButtons.ToArray());
        }

        private void CreateColorButtonColumn(int red, int green, int blue, int x, int y, int w, int h, int shades)
        {
            var shadedColorsNum = (shades - 1) / 2;

            for (var i = 0; i <= shadedColorsNum; i++)
            {
                _mColorButtons.Add(createColorButton(red * i / shadedColorsNum, green * i / shadedColorsNum,
                    blue * i / shadedColorsNum, x, y + i * h, w, h));

                if (i > 0)
                {
                    _mColorButtons.Add(createColorButton(red + (255 - red) * i / shadedColorsNum,
                        green + (255 - green) * i / shadedColorsNum, blue + (255 - blue) * i / shadedColorsNum, x,
                        y + (i + shadedColorsNum) * h, w, h));
                }
            }
        }

        private Button createColorButton(int red, int green, int blue, int x, int y, int w, int h)
        {
            return createColorButton(Color.FromArgb(255, red, green, blue), x, y, w, h);
        }

        private Button createColorButton(Color color, int x, int y, int w, int h)
        {
            var b = new Button();

            b.BackColor = color;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = color;
            b.FlatAppearance.BorderColor = Color.FromArgb(color.A, 255 - color.R, 255 - color.G, 255 - color.B);
            b.FlatStyle = FlatStyle.Flat;
            b.Location = new Point(x, y);
            b.Size = new Size(w, h);
            b.TabStop = false;

            b.Click += colorButton_Click;
            b.MouseEnter += colorButton_MouseEnter;
            b.MouseLeave += colorButton_MouseLeave;

            return b;
        }

        #endregion Construction and Initialization

        #region event handlers

        private void colorButton_Click(object sender, EventArgs e)
        {
            var b = (Button)sender;
            PickedColor = b.BackColor;

            // Raise event.
            if (ColorPicked != null)
            {
                ColorPicked(this, e);
            }
        }

        private void colorButton_MouseEnter(object sender, EventArgs e)
        {
            var b = (Button)sender;
            b.FlatAppearance.BorderSize = 1;
        }

        private void colorButton_MouseLeave(object sender, EventArgs e)
        {
            var b = (Button)sender;
            b.FlatAppearance.BorderSize = 0;
        }

        #endregion event handlers
    }
}