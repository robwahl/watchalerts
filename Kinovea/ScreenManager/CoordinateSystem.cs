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

using log4net;
using System.Drawing;
using System.Reflection;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     Helper class to encapsulate calculations about the coordinate system for drawings,
    ///     to compensate the differences between the original image and the displayed one.
    ///     Note : This is not the coordinate system that the user can adjust for distance calculations.
    ///     Includes :
    ///     - stretching, image may be stretched or squeezed relative to the original.
    ///     - zooming, the actual view may be a sub window of the original image.
    ///     - rotating. (todo).
    ///     - mirroring. (todo, currently handled elsewhere).
    ///     The class will keep track of the current changes in the coordinate system relatively to the
    ///     original image size and provide conversion routines.
    ///     All drawings coordinates are kept in the system of the original image.
    ///     For actually drawing them on screen we ask the transformation.
    ///     The image ratio is never altered. Skew is not supported.
    /// </summary>
    public class CoordinateSystem
    {
        #region Properties

        public double Scale
        {
            get { return Stretch*Zoom; }
        }

        public double Stretch { get; set; } = 1.0f;

        public double Zoom { get; set; } = 1.0f;

        public bool Zooming
        {
            get { return Zoom > 1.0f; }
        }

        public Point Location
        {
            get { return ZoomWindow.Location; }
        }

        public Rectangle ZoomWindow { get; private set; }

        public bool FreeMove { get; set; }

        public CoordinateSystem Identity
        {
            // Return a barebone system with no stretch and no zoom, based on current image size. Used for saving.
            get { return new CoordinateSystem(_mOriginalSize); }
        }

        #endregion Properties

        #region Members

        private Size _mOriginalSize; // Decoding size of the image
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor

        public CoordinateSystem()
            : this(new Size(1, 1))
        {
        }

        public CoordinateSystem(Size size)
        {
            SetOriginalSize(size);
        }

        #endregion Constructor

        #region System manipulation

        public void SetOriginalSize(Size size)
        {
            _mOriginalSize = size;
        }

        public void Reset()
        {
            Stretch = 1.0f;
            Zoom = 1.0f;
            ZoomWindow = Rectangle.Empty;
        }

        public void ReinitZoom()
        {
            Zoom = 1.0f;
            ZoomWindow = new Rectangle(0, 0, _mOriginalSize.Width, _mOriginalSize.Height);
        }

        public void RelocateZoomWindow()
        {
            RelocateZoomWindow(new Point(ZoomWindow.Left + (ZoomWindow.Width/2), ZoomWindow.Top + (ZoomWindow.Height/2)));
        }

        public void RelocateZoomWindow(Point center)
        {
            // Recreate the zoom window coordinates, given a new zoom factor, keeping the window center.
            // This used when increasing and decreasing the zoom factor,
            // to automatically adjust the viewing window.

            var iNewWidth = (int) (_mOriginalSize.Width/Zoom);
            var iNewHeight = (int) (_mOriginalSize.Height/Zoom);

            var iNewLeft = center.X - (iNewWidth/2);
            var iNewTop = center.Y - (iNewHeight/2);

            if (!FreeMove)
            {
                if (iNewLeft < 0) iNewLeft = 0;
                if (iNewLeft + iNewWidth >= _mOriginalSize.Width) iNewLeft = _mOriginalSize.Width - iNewWidth;

                if (iNewTop < 0) iNewTop = 0;
                if (iNewTop + iNewHeight >= _mOriginalSize.Height) iNewTop = _mOriginalSize.Height - iNewHeight;
            }

            ZoomWindow = new Rectangle(iNewLeft, iNewTop, iNewWidth, iNewHeight);
        }

        public void MoveZoomWindow(double fDeltaX, double fDeltaY)
        {
            // Move the zoom window keeping the same zoom factor.

            // Tentative new coords.
            var iNewLeft = (int) (ZoomWindow.Left - fDeltaX);
            var iNewTop = (int) (ZoomWindow.Top - fDeltaY);

            // Restraint the tentative coords at image borders.
            if (!FreeMove)
            {
                if (iNewLeft < 0) iNewLeft = 0;
                if (iNewTop < 0) iNewTop = 0;

                if (iNewLeft + ZoomWindow.Width >= _mOriginalSize.Width)
                    iNewLeft = _mOriginalSize.Width - ZoomWindow.Width;

                if (iNewTop + ZoomWindow.Height >= _mOriginalSize.Height)
                    iNewTop = _mOriginalSize.Height - ZoomWindow.Height;
            }

            // Reposition.
            ZoomWindow = new Rectangle(iNewLeft, iNewTop, ZoomWindow.Width, ZoomWindow.Height);
        }

        #endregion System manipulation

        #region Transformations

        public Point Untransform(Point point)
        {
            // in: screen coordinates
            // out: image coordinates.
            // Image may have been stretched, zoomed and moved.

            // 1. Unstretch coords -> As if stretch factor was 1.0f.
            var fUnstretchedX = point.X/Stretch;
            var fUnstretchedY = point.Y/Stretch;

            // 2. Unzoom coords -> As if zoom factor was 1.0f.
            // Unmoved is m_DirectZoomWindow.Left * m_fDirectZoomFactor.
            // Unzoomed is Unmoved / m_fDirectZoomFactor.
            var fUnzoomedX = ZoomWindow.Left + (fUnstretchedX/Zoom);
            var fUnzoomedY = ZoomWindow.Top + (fUnstretchedY/Zoom);

            return new Point((int) fUnzoomedX, (int) fUnzoomedY);
        }

        /// <summary>
        ///     Transform a point from image system to screen system. Handles scale, zoom and translate.
        /// </summary>
        /// <param name="point">The point in image coordinate system</param>
        /// <returns>The point in screen coordinate system</returns>
        public Point Transform(Point point)
        {
            // Zoom and translate
            var fZoomedX = (point.X - ZoomWindow.Left)*Zoom;
            var fZoomedY = (point.Y - ZoomWindow.Top)*Zoom;

            // Scale
            var fStretchedX = fZoomedX*Stretch;
            var fStretchedY = fZoomedY*Stretch;

            return new Point((int) fStretchedX, (int) fStretchedY);
        }

        /// <summary>
        ///     Transform a length in the image coordinate system to its equivalent in screen coordinate system.
        ///     Only uses scale and zoom.
        /// </summary>
        /// <param name="length">The length value to transform</param>
        /// <returns>The length value in screen coordinate system</returns>
        public int Transform(int length)
        {
            return (int) (length*Stretch*Zoom);
        }

        /// <summary>
        ///     Transform a size from the image coordinate system to its equivalent in screen coordinate system.
        ///     Only uses stretch and zoom.
        /// </summary>
        /// <param name="size">The Size value to transform</param>
        /// <returns>The size value in screen coordinate system</returns>
        public Size Transform(Size size)
        {
            return new Size(Transform(size.Width), Transform(size.Height));
        }

        /// <summary>
        ///     Transform a rectangle from the image coordinate system to its equivalent in screen coordinate system.
        ///     Uses stretch, zoom and translate.
        /// </summary>
        /// <param name="rect">The rectangle value to transform</param>
        /// <returns>The rectangle value in screen coordinate system</returns>
        public Rectangle Transform(Rectangle rect)
        {
            return new Rectangle(Transform(rect.Location), Transform(rect.Size));
        }

        public Quadrilateral Transform(Quadrilateral quad)
        {
            return new Quadrilateral
            {
                A = Transform(quad.A),
                B = Transform(quad.B),
                C = Transform(quad.C),
                D = Transform(quad.D)
            };
        }

        #endregion Transformations
    }
}