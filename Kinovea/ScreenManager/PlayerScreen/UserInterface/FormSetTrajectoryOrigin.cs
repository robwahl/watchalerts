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
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     FormSetTrajectoryOrigin is a dialog that let the user specify the
    ///     trajectory's coordinate system origin.
    ///     This is then used in spreadsheet export.
    /// </summary>
    public partial class FormSetTrajectoryOrigin : Form
    {
        #region Constructor

        public FormSetTrajectoryOrigin(Bitmap bmpPreview, Metadata parentMetadata)
        {
            // Init data.
            _mParentMetadata = parentMetadata;
            _mBmpPreview = bmpPreview;
            _mPenSelected = new Pen(Color.Red);
            _mPenCurrent = new Pen(Color.Red);
            _mPenCurrent.DashStyle = DashStyle.Dot;

            if (_mParentMetadata.CalibrationHelper.CoordinatesOrigin.X >= 0 &&
                _mParentMetadata.CalibrationHelper.CoordinatesOrigin.Y >= 0)
            {
                _mRealOrigin = _mParentMetadata.CalibrationHelper.CoordinatesOrigin;
            }

            InitializeComponent();

            // Culture
            Text = "   " + ScreenManagerLang.dlgSetTrajectoryOrigin_Title;
            btnOK.Text = ScreenManagerLang.Generic_Apply;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
        }

        #endregion Constructor

        #region Members

        private readonly Bitmap _mBmpPreview;

        //private Track m_Track;
        private Point _mScaledOrigin = new Point(-1, -1); // Selected point in display size coordinates.

        private Point _mRealOrigin = new Point(-1, -1); // Selected point in image size coordinates.
        private float _mFRatio = 1.0f;
        private readonly Pen _mPenCurrent;
        private readonly Pen _mPenSelected;
        private string _mText; // Current mouse coord in the new coordinate system.
        private Point _mCurrentMouse;
        private readonly Metadata _mParentMetadata;

        #endregion Members

        #region Auto Events

        private void formSetTrajectoryOrigin_Load(object sender, EventArgs e)
        {
            RatioStretch();
            UpdateScaledOrigin();
            picPreview.Invalidate();
        }

        private void picPreview_MouseMove(object sender, MouseEventArgs e)
        {
            // Save current mouse position and coordinates in the new system.
            _mCurrentMouse = new Point(e.X, e.Y);

            var iCoordX = (int)(e.X * _mFRatio) - _mRealOrigin.X;
            var iCoordY = _mRealOrigin.Y - (int)(e.Y * _mFRatio);
            var textX = _mParentMetadata.CalibrationHelper.GetLengthText(iCoordX, false, false);
            var textY = _mParentMetadata.CalibrationHelper.GetLengthText(iCoordY, false, false);
            _mText = string.Format("{{{0};{1}}} {2}", textX, textY,
                _mParentMetadata.CalibrationHelper.GetLengthAbbreviation());

            picPreview.Invalidate();
        }

        private void picPreview_Paint(object sender, PaintEventArgs e)
        {
            // Afficher l'image.
            if (_mBmpPreview != null)
            {
                e.Graphics.DrawImage(_mBmpPreview, 0, 0, picPreview.Width, picPreview.Height);

                if (_mScaledOrigin.X >= 0 && _mScaledOrigin.Y >= 0)
                {
                    // Selected Coordinate system.
                    e.Graphics.DrawLine(_mPenSelected, 0, _mScaledOrigin.Y, e.ClipRectangle.Width, _mScaledOrigin.Y);
                    e.Graphics.DrawLine(_mPenSelected, _mScaledOrigin.X, 0, _mScaledOrigin.X, e.ClipRectangle.Height);

                    // Current Mouse system.
                    e.Graphics.DrawLine(_mPenCurrent, 0, _mCurrentMouse.Y, e.ClipRectangle.Width, _mCurrentMouse.Y);
                    e.Graphics.DrawLine(_mPenCurrent, _mCurrentMouse.X, 0, _mCurrentMouse.X, e.ClipRectangle.Height);

                    // Current pos.
                    var fontText = new Font("Arial", 8, FontStyle.Bold);
                    var fontBrush = new SolidBrush(_mPenSelected.Color);
                    e.Graphics.DrawString(_mText, fontText, fontBrush, _mCurrentMouse.X - 67, _mCurrentMouse.Y + 2);
                    fontBrush.Dispose();
                    fontText.Dispose();
                }
            }
        }

        private void pnlPreview_Resize(object sender, EventArgs e)
        {
            RatioStretch();
            UpdateScaledOrigin();
            picPreview.Invalidate();
        }

        #endregion Auto Events

        #region User triggered events

        private void picPreview_MouseClick(object sender, MouseEventArgs e)
        {
            // User selected an origin point.
            _mScaledOrigin = new Point(e.X, e.Y);

            var iLeft = (int)(e.X * _mFRatio);
            var iTop = (int)(e.Y * _mFRatio);
            _mRealOrigin = new Point(iLeft, iTop);

            picPreview.Invalidate();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            _mParentMetadata.CalibrationHelper.CoordinatesOrigin = _mRealOrigin;
        }

        #endregion User triggered events

        #region low level

        private void RatioStretch()
        {
            // Resizes the picture box to maximize the image in the panel.

            // This method directly pasted from FormPreviewVideoFilter.
            // Todo: avoid duplication and factorize the two dialogs ?

            if (_mBmpPreview != null)
            {
                var widthRatio = (float)_mBmpPreview.Width / pnlPreview.Width;
                var heightRatio = (float)_mBmpPreview.Height / pnlPreview.Height;

                //Redimensionner l'image selon la dimension la plus proche de la taille du panel.
                if (widthRatio > heightRatio)
                {
                    picPreview.Width = pnlPreview.Width;
                    picPreview.Height = (int)(_mBmpPreview.Height / widthRatio);
                    _mFRatio = widthRatio;
                }
                else
                {
                    picPreview.Width = (int)(_mBmpPreview.Width / heightRatio);
                    picPreview.Height = pnlPreview.Height;
                    _mFRatio = heightRatio;
                }

                // Centering.
                picPreview.Left = (pnlPreview.Width / 2) - (picPreview.Width / 2);
                picPreview.Top = (pnlPreview.Height / 2) - (picPreview.Height / 2);
            }
        }

        private void UpdateScaledOrigin()
        {
            if (_mRealOrigin.X >= 0 && _mRealOrigin.Y >= 0)
            {
                var iLeft = (int)(_mRealOrigin.X / _mFRatio);
                var iTop = (int)(_mRealOrigin.Y / _mFRatio);
                _mScaledOrigin = new Point(iLeft, iTop);
            }
            else
            {
                _mScaledOrigin = new Point(-1, -1);
            }
        }

        #endregion low level
    }
}