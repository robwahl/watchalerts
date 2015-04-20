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

using Kinovea.ScreenManager.Languages;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public class DrawingToolCross2D : AbstractDrawingTool
    {
        #region Constructor

        public DrawingToolCross2D()
        {
            _mDefaultStylePreset.Elements.Add("back color", new StyleElementColor(Color.CornflowerBlue));
            _mStylePreset = _mDefaultStylePreset.Clone();
        }

        #endregion Constructor

        #region Properties

        public override string DisplayName
        {
            get { return ScreenManagerLang.ToolTip_DrawingToolCross2D; }
        }

        public override Bitmap Icon
        {
            get { return Properties.Drawings.crossmark; }
        }

        public override bool Attached
        {
            get { return true; }
        }

        public override bool KeepTool
        {
            get { return true; }
        }

        public override bool KeepToolFrameChanged
        {
            get { return false; }
        }

        public override DrawingStyle StylePreset
        {
            get { return _mStylePreset; }
            set { _mStylePreset = value; }
        }

        public override DrawingStyle DefaultStylePreset
        {
            get { return _mDefaultStylePreset; }
        }

        /// <summary>
        ///     This static property is used to keep the same setting for new cross markers.
        ///     Once we activate the display of coords, new markers will be created with the setting on, and vice versa.
        /// </summary>
        public static bool ShowCoordinates;

        #endregion Properties

        #region Private Methods

        private readonly DrawingStyle _mDefaultStylePreset = new DrawingStyle();
        private DrawingStyle _mStylePreset;

        #endregion Private Methods

        #region Public Methods

        public override AbstractDrawing GetNewDrawing(Point origin, long iTimestamp, long averageTimeStampsPerFrame)
        {
            return new DrawingCross2D(origin, iTimestamp, averageTimeStampsPerFrame, _mStylePreset);
        }

        public override Cursor GetCursor(double fStretchFactor)
        {
            // Draw custom cursor: cross inside a semi transparent circle (same as drawing).
            var c = (Color)_mStylePreset.Elements["back color"].Value;
            var p = new Pen(c, 1);
            var b = new Bitmap(9, 9);
            var g = Graphics.FromImage(b);

            // Center point is {4,4}
            g.DrawLine(p, 1, 4, 7, 4);
            g.DrawLine(p, 4, 1, 4, 7);

            var tempBrush = new SolidBrush(Color.FromArgb(32, c));
            g.FillEllipse(tempBrush, 0, 0, 8, 8);
            tempBrush.Dispose();
            p.Dispose();

            return new Cursor(b.GetHicon());
        }

        #endregion Public Methods
    }
}