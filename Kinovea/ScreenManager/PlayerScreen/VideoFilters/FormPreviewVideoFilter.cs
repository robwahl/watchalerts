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
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     formPreviewVideoFilter is a dialog to let the user decide
    ///     if he really wants to apply a given filter.
    ///     No configuration is needed for the filter but the operation may be destrcutive
    ///     so we better ask him to confirm.
    /// </summary>
    public partial class FormPreviewVideoFilter : Form
    {
        #region Members

        private readonly Bitmap _mBmpPreview;

        #endregion Members

        public FormPreviewVideoFilter(Bitmap bmpPreview, string windowTitle)
        {
            _mBmpPreview = bmpPreview;
            InitializeComponent();

            // Culture
            Text = "   " + windowTitle;
            btnOK.Text = ScreenManagerLang.Generic_Apply;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
        }

        private void formFilterTuner_Load(object sender, EventArgs e)
        {
            RatioStretch();
            picPreview.Invalidate();
        }

        private void picPreview_Paint(object sender, PaintEventArgs e)
        {
            // Afficher l'image.
            if (_mBmpPreview != null)
            {
                e.Graphics.DrawImage(_mBmpPreview, 0, 0, picPreview.Width, picPreview.Height);
            }
        }

        private void RatioStretch()
        {
            // Agrandi la picturebox pour maximisation dans le panel.
            if (_mBmpPreview != null)
            {
                var widthRatio = (float)_mBmpPreview.Width / pnlPreview.Width;
                var heightRatio = (float)_mBmpPreview.Height / pnlPreview.Height;

                //Redimensionner l'image selon la dimension la plus proche de la taille du panel.
                if (widthRatio > heightRatio)
                {
                    picPreview.Width = pnlPreview.Width;
                    picPreview.Height = (int)(_mBmpPreview.Height / widthRatio);
                }
                else
                {
                    picPreview.Width = (int)(_mBmpPreview.Width / heightRatio);
                    picPreview.Height = pnlPreview.Height;
                }

                //recentrer
                picPreview.Left = (pnlPreview.Width / 2) - (picPreview.Width / 2);
                picPreview.Top = (pnlPreview.Height / 2) - (picPreview.Height / 2);
            }
        }
    }
}