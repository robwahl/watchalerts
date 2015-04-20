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

using Kinovea.Services;
using log4net;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;
using Image = AForge.Imaging.Image;

namespace Kinovea.ScreenManager
{
    public class DrawingBitmap : AbstractDrawing
    {
        public override string ToString()
        {
            // Return the name of the tool used to draw this drawing.
            return "Bitmap Drawing";
        }

        public override int GetHashCode()
        {
            // Should not trigger meta data changes.
            return 0;
        }

        #region Properties

        public override InfosFading InfosFading
        {
            get { return _mInfosFading; }
            set { _mInfosFading = value; }
        }

        public override DrawingCapabilities Caps
        {
            get { return DrawingCapabilities.Opacity; }
        }

        public override List<ToolStripMenuItem> ContextMenu
        {
            get { return null; }
        }

        #endregion Properties

        #region Members

        private readonly Bitmap _mBitmap;
        private readonly BoundingBox _mBoundingBox = new BoundingBox();
        private float _mFInitialScale = 1.0f; // The scale we apply upon loading to make sure the image fits the screen.
        private int _mIOriginalWidth;
        private int _mIOriginalHeight;

        // Decoration
        private InfosFading _mInfosFading;

        private readonly ColorMatrix _mFadingColorMatrix = new ColorMatrix();
        private readonly ImageAttributes _mFadingImgAttr = new ImageAttributes();
        private Pen _mPenBoundingBox;
        private SolidBrush _mBrushBoundingBox;

        // Instrumentation
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructors

        public DrawingBitmap(int iWidth, int iHeight, long iTimestamp, long iAverageTimeStampsPerFrame, string filename)
        {
            _mBitmap = new Bitmap(filename);

            if (_mBitmap != null)
            {
                Initialize(iWidth, iHeight, iTimestamp, iAverageTimeStampsPerFrame);
            }
        }

        public DrawingBitmap(int iWidth, int iHeight, long iTimestamp, long iAverageTimeStampsPerFrame, Bitmap bmp)
        {
            _mBitmap = Image.Clone(bmp);

            if (_mBitmap != null)
            {
                Initialize(iWidth, iHeight, iTimestamp, iAverageTimeStampsPerFrame);
            }
        }

        private void Initialize(int iWidth, int iHeight, long iTimestamp, long iAverageTimeStampsPerFrame)
        {
            _mIOriginalWidth = _mBitmap.Width;
            _mIOriginalHeight = _mBitmap.Height;

            // Set the initial scale so that the drawing is some part of the image height, to make sure it fits well.
            // For bitmap drawing, we only do this if no upsizing is involved.
            _mFInitialScale = (float)((iHeight * 0.75) / _mIOriginalHeight);
            if (_mFInitialScale < 1.0)
            {
                _mIOriginalWidth = (int)(_mIOriginalWidth * _mFInitialScale);
                _mIOriginalHeight = (int)(_mIOriginalHeight * _mFInitialScale);
            }

            _mBoundingBox.Rectangle = new Rectangle((iWidth - _mIOriginalWidth) / 2, (iHeight - _mIOriginalHeight) / 2,
                _mIOriginalWidth, _mIOriginalHeight);

            // Fading
            _mInfosFading = new InfosFading(iTimestamp, iAverageTimeStampsPerFrame);
            _mInfosFading.UseDefault = false;
            _mInfosFading.AlwaysVisible = true;

            // This is used to set the opacity factor.
            _mFadingColorMatrix.Matrix00 = 1.0f;
            _mFadingColorMatrix.Matrix11 = 1.0f;
            _mFadingColorMatrix.Matrix22 = 1.0f;
            _mFadingColorMatrix.Matrix33 = 1.0f; // Change alpha value here for fading. (i.e: 0.5f).
            _mFadingColorMatrix.Matrix44 = 1.0f;
            _mFadingImgAttr.SetColorMatrix(_mFadingColorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            var pm = PreferencesManager.Instance();
            _mPenBoundingBox = new Pen(Color.White, 1);
            _mPenBoundingBox.DashStyle = DashStyle.Dash;
            _mBrushBoundingBox = new SolidBrush(_mPenBoundingBox.Color);
        }

        #endregion Constructors

        #region AbstractDrawing Implementation

        public override void Draw(Graphics canvas, CoordinateSystem transformer, bool bSelected, long iCurrentTimestamp)
        {
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor <= 0)
                return;

            var rect = transformer.Transform(_mBoundingBox.Rectangle);

            if (_mBitmap != null)
            {
                _mFadingColorMatrix.Matrix33 = (float)fOpacityFactor;
                _mFadingImgAttr.SetColorMatrix(_mFadingColorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                canvas.DrawImage(_mBitmap, rect, 0, 0, _mBitmap.Width, _mBitmap.Height, GraphicsUnit.Pixel,
                    _mFadingImgAttr);

                if (bSelected)
                {
                    _mBoundingBox.Draw(canvas, rect, _mPenBoundingBox, _mBrushBoundingBox, 4);
                }
            }
        }

        public override int HitTest(Point point, long iCurrentTimestamp)
        {
            // Convention: miss = -1, object = 0, handle = n.
            var iHitResult = -1;
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor > 0)
                iHitResult = _mBoundingBox.HitTest(point);

            return iHitResult;
        }

        public override void MoveHandle(Point point, int iHandleNumber)
        {
            _mBoundingBox.MoveHandle(point, iHandleNumber, new Size(_mIOriginalWidth, _mIOriginalHeight));
        }

        public override void MoveDrawing(int deltaX, int deltaY, Keys modifierKeys)
        {
            _mBoundingBox.Move(deltaX, deltaY);
        }

        #endregion AbstractDrawing Implementation
    }
}