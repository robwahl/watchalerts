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
    public class DrawingToolPencil : AbstractDrawingTool
    {
        #region Constructor

        public DrawingToolPencil()
        {
            _mDefaultStylePreset.Elements.Add("color", new StyleElementColor(Color.SeaGreen));
            _mDefaultStylePreset.Elements.Add("pen size", new StyleElementPenSize(9));
            _mStylePreset = _mDefaultStylePreset.Clone();
        }

        #endregion Constructor

        #region Properties

        public override string DisplayName
        {
            get { return ScreenManagerLang.ToolTip_DrawingToolPencil; }
        }

        public override Bitmap Icon
        {
            get { return Properties.Drawings.pencil; }
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
            get { return true; }
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

        #endregion Properties

        #region Members

        private readonly DrawingStyle _mDefaultStylePreset = new DrawingStyle();
        private DrawingStyle _mStylePreset;

        #endregion Members

        #region Public Methods

        public override AbstractDrawing GetNewDrawing(Point origin, long iTimestamp, long averageTimeStampsPerFrame)
        {
            return new DrawingPencil(origin, new Point(origin.X + 1, origin.Y), iTimestamp, averageTimeStampsPerFrame,
                _mStylePreset);
        }

        public override Cursor GetCursor(double fStretchFactor)
        {
            // Draw custom cursor: Colored and sized circle.
            var c = (Color)_mStylePreset.Elements["color"].Value;
            var size = (int)(fStretchFactor * (int)_mStylePreset.Elements["pen size"].Value);
            var p = new Pen(c, 1);
            var b = new Bitmap(size + 2, size + 2);
            var g = Graphics.FromImage(b);
            g.DrawEllipse(p, 1, 1, size, size);
            p.Dispose();
            return new Cursor(b.GetHicon());
        }

        #endregion Public Methods
    }
}