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

using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public class Magnifier
    {
        private Hit HitTest(Point point)
        {
            // Hit Result:
            // -1: miss, 0: on source window, 1: on magnification window, 1+: on source resizer.

            var res = Hit.None;

            var srcRectangle = new Rectangle(_mISrcCustomLeft, _mISrcCustomTop, _mISrcCustomWidth, _mISrcCustomHeight);
            var magRectangle = new Rectangle(_mIMagLeft, _mIMagTop, _mIMagWidth, _mIMagHeight);

            // We widen the size of handlers rectangle for easier selection.
            var widen = 6;

            if (new Rectangle(_mISrcCustomLeft - widen, _mISrcCustomTop - widen, widen * 2, widen * 2).Contains(point))
            {
                res = Hit.TopLeftResizer;
            }
            else if (
                new Rectangle(_mISrcCustomLeft - widen, _mISrcCustomTop + _mISrcCustomHeight - widen, widen * 2,
                    widen * 2).Contains(point))
            {
                res = Hit.BottomLeftResizer;
            }
            else if (
                new Rectangle(_mISrcCustomLeft + _mISrcCustomWidth - widen, _mISrcCustomTop - widen, widen * 2,
                    widen * 2).Contains(point))
            {
                res = Hit.TopRightResizer;
            }
            else if (
                new Rectangle(_mISrcCustomLeft + _mISrcCustomWidth - widen,
                    _mISrcCustomTop + _mISrcCustomHeight - widen, widen * 2, widen * 2).Contains(point))
            {
                res = Hit.BottomRightResizer;
            }
            else if (srcRectangle.Contains(point))
            {
                res = Hit.SourceWindow;
            }
            else if (magRectangle.Contains(point))
            {
                res = Hit.MagnifyWindow;
            }

            return res;
        }

        private enum Hit
        {
            None,
            SourceWindow,
            MagnifyWindow,
            TopLeftResizer,
            TopRightResizer,
            BottomLeftResizer,
            BottomRightResizer
        }

        #region Properties

        public double ZoomFactor
        {
            get { return _mFZoomFactor; }
            set
            {
                _mFZoomFactor = value;
                _mIMagWidth = (int)(_mISrcCustomWidth * _mFZoomFactor);
                _mIMagHeight = (int)(_mISrcCustomHeight * _mFZoomFactor);
            }
        }

        public int MouseX = 0;
        public int MouseY = 0;
        public MagnifierMode Mode = MagnifierMode.NotVisible;

        public Point MagnifiedCenter
        {
            get { return new Point(_mImgTopLeft.X + _mIImgWidth / 2, _mImgTopLeft.Y + _mIImgHeight / 2); }
        }

        #endregion Properties

        #region Members

        private double _mFStretchFactor = double.MaxValue;

        private Size _mIImageSize = new Size(1, 1);

        // TODO : turn everything into Rectangles.

        // Precomputed values (computed only when image stretch factor changes)
        private int _mISrcWidth;

        private int _mISrcHeight;

        private int _mIMagLeft = 10;
        private int _mIMagTop = 10;
        private int _mIMagWidth;
        private int _mIMagHeight;

        private Point _mImgTopLeft = new Point(0, 0); // Location of source zone in source image system.
        private int _mIImgWidth; // size of the source zone in the original image
        private int _mIImgHeight;

        // Default coeffs
        private static readonly double _mFDefaultWindowFactor = 0.20; // Size of the source zone relative to image size.

        private static double _mFZoomFactor = 1.75;

        // Indirect mode values
        private int _mISrcCustomLeft;

        private int _mISrcCustomTop;
        private int _mISrcCustomWidth;
        private int _mISrcCustomHeight;
        private Point _mLastPoint = new Point(0, 0);
        private Hit _mMovingObject = Hit.None;

        #endregion Members

        #region Public Interface

        public void Draw(Bitmap bitmap, Graphics canvas, double fStretchFactor, bool bMirrored)
        {
            _mIImageSize = new Size(bitmap.Width, bitmap.Height);

            if (fStretchFactor != _mFStretchFactor)
            {
                _mLastPoint = new Point((int)(_mLastPoint.X * fStretchFactor), (int)(_mLastPoint.Y * fStretchFactor));

                if (_mFStretchFactor != double.MaxValue)
                {
                    // Scale to new stretch factor.

                    var fRescaleFactor = fStretchFactor / _mFStretchFactor;

                    _mISrcCustomLeft = (int)(_mISrcCustomLeft * fRescaleFactor);
                    _mISrcCustomTop = (int)(_mISrcCustomTop * fRescaleFactor);
                    _mISrcCustomWidth = (int)(_mISrcCustomWidth * fRescaleFactor);
                    _mISrcCustomHeight = (int)(_mISrcCustomHeight * fRescaleFactor);

                    _mIMagLeft = (int)(_mIMagLeft * fRescaleFactor);
                    _mIMagTop = (int)(_mIMagTop * fRescaleFactor);
                    _mIMagWidth = (int)(_mIMagWidth * fRescaleFactor);
                    _mIMagHeight = (int)(_mIMagHeight * fRescaleFactor);
                }
                else
                {
                    // Initializations.

                    _mISrcWidth = (int)(bitmap.Width * fStretchFactor * _mFDefaultWindowFactor);
                    _mISrcHeight = (int)(bitmap.Height * fStretchFactor * _mFDefaultWindowFactor);

                    _mIMagLeft = 10;
                    _mIMagTop = 10;

                    _mIMagWidth = (int)(_mISrcWidth * _mFZoomFactor);
                    _mIMagHeight = (int)(_mISrcHeight * _mFZoomFactor);

                    _mIImgWidth = (int)(_mISrcWidth / fStretchFactor);
                    _mIImgHeight = (int)(_mISrcHeight / fStretchFactor);
                }

                _mFStretchFactor = fStretchFactor;
            }

            var iImgLeft = 0;
            var iImgTop = 0;

            if (Mode == MagnifierMode.Direct)
            {
                iImgLeft = (int)(MouseX / fStretchFactor) - (_mIImgWidth / 2);
                iImgTop = (int)(MouseY / fStretchFactor) - (_mIImgHeight / 2);
                _mImgTopLeft = new Point(iImgLeft, iImgTop);

                canvas.DrawRectangle(Pens.White, MouseX - _mISrcWidth / 2, MouseY - _mISrcHeight / 2, _mISrcWidth,
                    _mISrcHeight);
            }
            else if (Mode == MagnifierMode.Indirect)
            {
                iImgLeft = (int)(_mISrcCustomLeft / fStretchFactor);
                iImgTop = (int)(_mISrcCustomTop / fStretchFactor);

                canvas.DrawRectangle(Pens.LightGray, _mISrcCustomLeft, _mISrcCustomTop, _mISrcCustomWidth,
                    _mISrcCustomHeight);

                // Handlers
                canvas.DrawLine(Pens.LightGray, _mISrcCustomLeft - 2, _mISrcCustomTop - 2, _mISrcCustomLeft + 2,
                    _mISrcCustomTop - 2);
                canvas.DrawLine(Pens.LightGray, _mISrcCustomLeft - 2, _mISrcCustomTop - 2, _mISrcCustomLeft - 2,
                    _mISrcCustomTop + 2);

                canvas.DrawLine(Pens.LightGray, _mISrcCustomLeft - 2, _mISrcCustomTop + _mISrcCustomHeight + 2,
                    _mISrcCustomLeft + 2, _mISrcCustomTop + _mISrcCustomHeight + 2);
                canvas.DrawLine(Pens.LightGray, _mISrcCustomLeft - 2, _mISrcCustomTop + _mISrcCustomHeight + 2,
                    _mISrcCustomLeft - 2, _mISrcCustomTop + _mISrcCustomHeight - 2);

                canvas.DrawLine(Pens.LightGray, _mISrcCustomLeft + _mISrcCustomWidth + 2, _mISrcCustomTop - 2,
                    _mISrcCustomLeft + _mISrcCustomWidth - 2, _mISrcCustomTop - 2);
                canvas.DrawLine(Pens.LightGray, _mISrcCustomLeft + _mISrcCustomWidth + 2, _mISrcCustomTop - 2,
                    _mISrcCustomLeft + _mISrcCustomWidth + 2, _mISrcCustomTop + 2);

                canvas.DrawLine(Pens.LightGray, _mISrcCustomLeft + _mISrcCustomWidth + 2,
                    _mISrcCustomTop + _mISrcCustomHeight + 2, _mISrcCustomLeft + _mISrcCustomWidth - 2,
                    _mISrcCustomTop + _mISrcCustomHeight + 2);
                canvas.DrawLine(Pens.LightGray, _mISrcCustomLeft + _mISrcCustomWidth + 2,
                    _mISrcCustomTop + _mISrcCustomHeight + 2, _mISrcCustomLeft + _mISrcCustomWidth + 2,
                    _mISrcCustomTop + _mISrcCustomHeight - 2);
            }

            // Image Window.
            _mImgTopLeft = new Point(iImgLeft, iImgTop);
            Rectangle rDst;
            Rectangle rSrc;
            if (bMirrored)
            {
                // If mirrored, the destination spot is reversed (negative width),
                // and the source spot is reversed relatively to the edge of the image.
                rDst = new Rectangle(_mIMagLeft + _mIMagWidth, _mIMagTop, -_mIMagWidth, _mIMagHeight);
                rSrc = new Rectangle(_mIImageSize.Width - (iImgLeft + _mIImgWidth), iImgTop, _mIImgWidth, _mIImgHeight);
            }
            else
            {
                rDst = new Rectangle(_mIMagLeft, _mIMagTop, _mIMagWidth, _mIMagHeight);
                rSrc = new Rectangle(iImgLeft, iImgTop, _mIImgWidth, _mIImgHeight);
            }

            canvas.DrawImage(bitmap, rDst, rSrc, GraphicsUnit.Pixel);
            canvas.DrawRectangle(Pens.White, _mIMagLeft, _mIMagTop, _mIMagWidth, _mIMagHeight);
        }

        public void OnMouseUp(MouseEventArgs e)
        {
            if (Mode == MagnifierMode.Direct)
            {
                Mode = MagnifierMode.Indirect;

                // Fix current values.
                _mISrcCustomLeft = MouseX - _mISrcWidth / 2;
                _mISrcCustomTop = MouseY - _mISrcHeight / 2;
                _mISrcCustomWidth = _mISrcWidth;
                _mISrcCustomHeight = _mISrcHeight;
            }
        }

        public bool OnMouseMove(MouseEventArgs e)
        {
            if (Mode == MagnifierMode.Indirect)
            {
                var deltaX = e.X - _mLastPoint.X;
                var deltaY = e.Y - _mLastPoint.Y;

                _mLastPoint.X = e.X;
                _mLastPoint.Y = e.Y;

                switch (_mMovingObject)
                {
                    case Hit.SourceWindow:
                        if ((_mISrcCustomLeft + deltaX > 0) &&
                            (_mISrcCustomLeft + _mISrcCustomWidth + deltaX + 1 < _mIImageSize.Width * _mFStretchFactor))
                        {
                            _mISrcCustomLeft += deltaX;
                        }
                        if ((_mISrcCustomTop + deltaY > 0) &&
                            (_mISrcCustomTop + _mISrcCustomHeight + deltaY + 1 < _mIImageSize.Height * _mFStretchFactor))
                        {
                            _mISrcCustomTop += deltaY;
                        }
                        break;

                    case Hit.MagnifyWindow:
                        if ((_mIMagLeft + deltaX > 0) &&
                            (_mIMagLeft + _mIMagWidth + deltaX + 1 < _mIImageSize.Width * _mFStretchFactor))
                        {
                            _mIMagLeft += deltaX;
                        }
                        if ((_mIMagTop + deltaY > 0) &&
                            (_mIMagTop + _mIMagHeight + deltaY + 1 < _mIImageSize.Height * _mFStretchFactor))
                        {
                            _mIMagTop += deltaY;
                        }
                        break;

                    case Hit.TopLeftResizer:
                        if ((_mISrcCustomLeft + deltaX > 0) &&
                            (_mISrcCustomTop + deltaY > 0) &&
                            (_mISrcCustomLeft + _mISrcCustomWidth + deltaX + 1 < _mIImageSize.Width * _mFStretchFactor) &&
                            (_mISrcCustomTop + _mISrcCustomHeight + deltaY + 1 < _mIImageSize.Height * _mFStretchFactor))
                        {
                            _mISrcCustomLeft += deltaX;
                            _mISrcCustomTop += deltaY;
                            _mISrcCustomWidth -= deltaX;
                            _mISrcCustomHeight -= deltaY;

                            if (_mISrcCustomWidth < 10) _mISrcCustomWidth = 10;
                            if (_mISrcCustomHeight < 10) _mISrcCustomHeight = 10;

                            _mIMagWidth = (int)(_mISrcCustomWidth * _mFZoomFactor);
                            _mIMagHeight = (int)(_mISrcCustomHeight * _mFZoomFactor);
                            _mIImgWidth = (int)(_mISrcCustomWidth / _mFStretchFactor);
                            _mIImgHeight = (int)(_mISrcCustomHeight / _mFStretchFactor);
                        }
                        break;

                    case Hit.BottomLeftResizer:
                        if ((_mISrcCustomLeft + deltaX > 0) &&
                            (_mISrcCustomTop + deltaY > 0) &&
                            (_mISrcCustomLeft + _mISrcCustomWidth + deltaX + 1 < _mIImageSize.Width * _mFStretchFactor) &&
                            (_mISrcCustomTop + _mISrcCustomHeight + deltaY + 1 < _mIImageSize.Height * _mFStretchFactor))
                        {
                            _mISrcCustomLeft += deltaX;
                            _mISrcCustomWidth -= deltaX;
                            _mISrcCustomHeight += deltaY;

                            if (_mISrcCustomWidth < 10) _mISrcCustomWidth = 10;
                            if (_mISrcCustomHeight < 10) _mISrcCustomHeight = 10;

                            _mIMagWidth = (int)(_mISrcCustomWidth * _mFZoomFactor);
                            _mIMagHeight = (int)(_mISrcCustomHeight * _mFZoomFactor);
                            _mIImgWidth = (int)(_mISrcCustomWidth / _mFStretchFactor);
                            _mIImgHeight = (int)(_mISrcCustomHeight / _mFStretchFactor);
                        }
                        break;

                    case Hit.TopRightResizer:
                        if ((_mISrcCustomLeft + deltaX > 0) &&
                            (_mISrcCustomTop + deltaY > 0) &&
                            (_mISrcCustomLeft + _mISrcCustomWidth + deltaX + 1 < _mIImageSize.Width * _mFStretchFactor) &&
                            (_mISrcCustomTop + _mISrcCustomHeight + deltaY + 1 < _mIImageSize.Height * _mFStretchFactor))
                        {
                            _mISrcCustomTop += deltaY;
                            _mISrcCustomWidth += deltaX;
                            _mISrcCustomHeight -= deltaY;

                            if (_mISrcCustomWidth < 10) _mISrcCustomWidth = 10;
                            if (_mISrcCustomHeight < 10) _mISrcCustomHeight = 10;

                            _mIMagWidth = (int)(_mISrcCustomWidth * _mFZoomFactor);
                            _mIMagHeight = (int)(_mISrcCustomHeight * _mFZoomFactor);
                            _mIImgWidth = (int)(_mISrcCustomWidth / _mFStretchFactor);
                            _mIImgHeight = (int)(_mISrcCustomHeight / _mFStretchFactor);
                        }
                        break;

                    case Hit.BottomRightResizer:
                        if ((_mISrcCustomLeft + deltaX > 0) &&
                            (_mISrcCustomTop + deltaY > 0) &&
                            (_mISrcCustomLeft + _mISrcCustomWidth + deltaX + 1 < _mIImageSize.Width * _mFStretchFactor) &&
                            (_mISrcCustomTop + _mISrcCustomHeight + deltaY + 1 < _mIImageSize.Height * _mFStretchFactor))
                        {
                            _mISrcCustomWidth += deltaX;
                            _mISrcCustomHeight += deltaY;

                            if (_mISrcCustomWidth < 10) _mISrcCustomWidth = 10;
                            if (_mISrcCustomHeight < 10) _mISrcCustomHeight = 10;

                            _mIMagWidth = (int)(_mISrcCustomWidth * _mFZoomFactor);
                            _mIMagHeight = (int)(_mISrcCustomHeight * _mFZoomFactor);
                            _mIImgWidth = (int)(_mISrcCustomWidth / _mFStretchFactor);
                            _mIImgHeight = (int)(_mISrcCustomHeight / _mFStretchFactor);
                        }
                        break;

                    default:
                        break;
                }
            }

            return (_mMovingObject != Hit.None);
        }

        public bool OnMouseDown(MouseEventArgs e)
        {
            //----------------------------------------------------------------
            // Return true if we actually hit any of the magnifier elements
            // (mag window, resizers, etc...)
            // In case of the first switch to indirect mode, will return true.
            //----------------------------------------------------------------
            if (Mode == MagnifierMode.Indirect)
            {
                // initialize position.
                _mLastPoint.X = e.X;
                _mLastPoint.Y = e.Y;

                // initialize what we are moving.
                _mMovingObject = HitTest(new Point(e.X, e.Y));
            }

            return (_mMovingObject != Hit.None);
        }

        public bool IsOnObject(MouseEventArgs e)
        {
            return (HitTest(new Point(e.X, e.Y)) != Hit.None);
        }

        public void ResetData()
        {
            _mLastPoint.X = 0;
            _mLastPoint.Y = 0;

            _mIImageSize = new Size(1, 1);
            _mImgTopLeft = new Point(0, 0);
        }

        #endregion Public Interface
    }

    public enum MagnifierMode
    {
        NotVisible,
        Direct, // When the mouse move makes the magnifier move.
        Indirect // When the user has to click to change the boundaries of the magnifier.
    }
}