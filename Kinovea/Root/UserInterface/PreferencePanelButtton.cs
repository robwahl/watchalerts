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

using Kinovea.Root.UserInterface.PreferencePanels;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.Root
{
    /// <summary>
    ///     PreferencePanelButtton.
    ///     A simple "image + label" control to be used for preferences pages.
    /// </summary>
    public partial class PreferencePanelButtton : UserControl
    {
        #region Construction

        public PreferencePanelButtton(IPreferencePanel preferencePanel)
        {
            InitializeComponent();
            PreferencePanel = preferencePanel;
        }

        #endregion Construction

        #region Properties

        public IPreferencePanel PreferencePanel { get; private set; }

        #endregion Properties

        #region Public Methods

        public void SetSelected(bool bSelected)
        {
            _mBSelected = bSelected;
            BackColor = bSelected ? Color.LightSteelBlue : Color.White;
        }

        #endregion Public Methods

        #region Members

        private bool _mBSelected;
        private static readonly Font MFontLabel = new Font("Arial", 8, FontStyle.Regular);

        #endregion Members

        #region Private Methods

        private void preferencePanelButtton_Paint(object sender, PaintEventArgs e)
        {
            if (PreferencePanel.Icon != null)
            {
                var iconStart = new Point((Width - PreferencePanel.Icon.Width) / 2, 10);
                e.Graphics.DrawImage(PreferencePanel.Icon, iconStart);
            }

            var textSize = e.Graphics.MeasureString(PreferencePanel.Description, MFontLabel);
            var textStart = new PointF((Width - textSize.Width) / 2, 50.0F);
            e.Graphics.DrawString(PreferencePanel.Description, MFontLabel, Brushes.Black, textStart);
        }

        private void PreferencePanelButttonMouseEnter(object sender, EventArgs e)
        {
            if (!_mBSelected)
            {
                BackColor = Color.FromArgb(224, 232, 246);
            }
        }

        private void PreferencePanelButttonMouseLeave(object sender, EventArgs e)
        {
            if (!_mBSelected)
            {
                BackColor = Color.White;
            }
        }

        #endregion Private Methods
    }
}