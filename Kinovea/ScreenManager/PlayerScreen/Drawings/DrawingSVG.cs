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

using Kinovea.Services;
using log4net;
using SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi;
using SharpVectors.Dom.Svg;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public class DrawingSvg : AbstractDrawing
    {
        #region Constructor

        public DrawingSvg(int iWidth, int iHeight, long iTimestamp, long iAverageTimeStampsPerFrame, string filename)
        {
            // Init and import an SVG.
            _mRenderer.BackColor = Color.Transparent;

            // Rendering window. The width and height will be updated later.
            _mSvgWindow = new SvgWindow(100, 100, _mRenderer);

            // FIXME: some files have external DTD that will be attempted to be loaded.
            // See files created from Amaya for example.
            _mSvgWindow.Src = filename;
            _mBLoaded = true;

            if (_mSvgWindow.Document.RootElement.Width.BaseVal.UnitType == SvgLengthType.Percentage)
            {
                _mBSizeInPercentage = true;
                _mIOriginalWidth =
                    (int)
                        (_mSvgWindow.Document.RootElement.ViewBox.BaseVal.Width *
                         (_mSvgWindow.Document.RootElement.Width.BaseVal.Value / 100));
                _mIOriginalHeight =
                    (int)
                        (_mSvgWindow.Document.RootElement.ViewBox.BaseVal.Height *
                         (_mSvgWindow.Document.RootElement.Height.BaseVal.Value / 100));
            }
            else
            {
                _mBSizeInPercentage = false;
                _mIOriginalWidth = (int)_mSvgWindow.Document.RootElement.Width.BaseVal.Value;
                _mIOriginalHeight = (int)_mSvgWindow.Document.RootElement.Height.BaseVal.Value;
            }

            // Set the initial scale so that the drawing is some part of the image height, to make sure it fits well.
            _mFInitialScale = (float)((iHeight * 0.75) / _mIOriginalHeight);
            _mIOriginalWidth = (int)(_mIOriginalWidth * _mFInitialScale);
            _mIOriginalHeight = (int)(_mIOriginalHeight * _mFInitialScale);

            _mBoundingBox.Rectangle = new Rectangle((iWidth - _mIOriginalWidth) / 2, (iHeight - _mIOriginalHeight) / 2,
                _mIOriginalWidth, _mIOriginalHeight);

            // Render on first draw call.
            _mBFinishedResizing = true;

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

        #endregion Constructor

        public override string ToString()
        {
            // Return the name of the tool used to draw this drawing.
            return "SVG Drawing";
        }

        public override int GetHashCode()
        {
            // Should not trigger meta data changes.
            return 0;
        }

        public void ResizeFinished()
        {
            // While the user was resizing the drawing or the image, we didn't update / render the SVG image.
            // Now that he is done, we can stop using the low quality interpolation and resort to SVG scalability.

            // However we do not know the final scale until we get back in Draw(),
            // So we just switch a flag on and we'll call the rendering from there.
            _mBFinishedResizing = true;
        }

        #region Lower level helpers

        private void RenderAtNewScale(Size size, double fScreenScaling)
        {
            // Depending on the complexity of the SVG, this can be a costly operation.
            // We should only do that when mouse move is over,
            // and use the interpolated version during the change.

            // Compute the final drawing sizes,
            // taking both the drawing transformation and the image scaling into account.
            _mFDrawingScale = _mBoundingBox.Rectangle.Width / (float)_mIOriginalWidth;
            _mFDrawingRenderingScale = (float)(fScreenScaling * _mFDrawingScale * _mFInitialScale);

            if (_mSvgRendered == null || _mFDrawingRenderingScale != _mSvgWindow.Document.RootElement.CurrentScale)
            {
                // In the case of percentage, CurrentScale is always 100%. But since there is a cache for the transformation matrix,
                // we need to set it anyway to clear the cache.
                _mSvgWindow.Document.RootElement.CurrentScale = _mBSizeInPercentage ? 1.0f : _mFDrawingRenderingScale;

                _mSvgWindow.InnerWidth = size.Width;
                _mSvgWindow.InnerHeight = size.Height;

                _mSvgRendered = _mRenderer.Render(_mSvgWindow.Document as SvgDocument);

                Log.Debug(
                    string.Format(
                        "Rendering SVG ({0};{1}), Initial scaling to fit video: {2:0.00}. User scaling: {3:0.00}. Video image scaling: {4:0.00}, Final transformation: {5:0.00}.",
                        _mIOriginalWidth, _mIOriginalHeight, _mFInitialScale, _mFDrawingScale, fScreenScaling,
                        _mFDrawingRenderingScale));
            }
        }

        #endregion Lower level helpers

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

        // SVG
        private readonly GdiRenderer _mRenderer = new GdiRenderer();

        private readonly SvgWindow _mSvgWindow;
        private readonly bool _mBLoaded;
        private Bitmap _mSvgRendered;

        // Position
        // The drawing scale is used to keep track of the user transform on the drawing, outside of the image transform context.
        // Drawing original dimensions are used to compute the drawing scale.
        private float _mFDrawingScale = 1.0f;

        // The current scale of the drawing if it were rendered on the original sized image.

        private readonly float _mFInitialScale = 1.0f;
        // The scale we apply upon loading to make sure the image fits the screen.

        private float _mFDrawingRenderingScale = 1.0f;
        // The scale of the drawing taking drawing transform AND image transform into account.

        private readonly int _mIOriginalWidth; // After initial scaling.
        private readonly int _mIOriginalHeight;
        private readonly BoundingBox _mBoundingBox = new BoundingBox();
        private readonly bool _mBSizeInPercentage; // A property of some SVG files.
        private bool _mBFinishedResizing;

        // Decoration
        private InfosFading _mInfosFading;

        private readonly ColorMatrix _mFadingColorMatrix = new ColorMatrix();
        private readonly ImageAttributes _mFadingImgAttr = new ImageAttributes();
        private readonly Pen _mPenBoundingBox;
        private readonly SolidBrush _mBrushBoundingBox;

        // Instru
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region AbstractDrawing Implementation

        public override void Draw(Graphics canvas, CoordinateSystem transformer, bool bSelected, long iCurrentTimestamp)
        {
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor <= 0 || !_mBLoaded)
                return;

            var rect = transformer.Transform(_mBoundingBox.Rectangle);

            if (_mBFinishedResizing)
            {
                _mBFinishedResizing = false;
                RenderAtNewScale(rect.Size, transformer.Scale);
            }

            if (_mSvgRendered != null)
            {
                _mFadingColorMatrix.Matrix33 = (float)fOpacityFactor;
                _mFadingImgAttr.SetColorMatrix(_mFadingColorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                canvas.DrawImage(_mSvgRendered, rect, 0, 0, _mSvgRendered.Width, _mSvgRendered.Height,
                    GraphicsUnit.Pixel, _mFadingImgAttr);

                if (bSelected)
                    _mBoundingBox.Draw(canvas, rect, _mPenBoundingBox, _mBrushBoundingBox, 4);
            }
        }

        public override int HitTest(Point point, long iCurrentTimestamp)
        {
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