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

using Kinovea.ScreenManager.Languages;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public class DrawingToolCircle : AbstractDrawingTool
    {
        public DrawingToolCircle()
        {
            _mDefaultStylePreset.Elements.Add("color", new StyleElementColor(Color.CadetBlue));
            _mDefaultStylePreset.Elements.Add("pen size", new StyleElementPenSize(3));
            _mStylePreset = _mDefaultStylePreset.Clone();
        }

        #region Properties

        public override string DisplayName
        {
            get { return ScreenManagerLang.ToolTip_DrawingToolCircle; }
        }

        public override Bitmap Icon
        {
            get { return Properties.Drawings.circle; }
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

        #endregion Properties

        #region Members

        private readonly DrawingStyle _mDefaultStylePreset = new DrawingStyle();
        private DrawingStyle _mStylePreset;

        #endregion Members

        #region Public Methods

        public override AbstractDrawing GetNewDrawing(Point origin, long iTimestamp, long averageTimeStampsPerFrame)
        {
            return new DrawingCircle(origin, 25, iTimestamp, averageTimeStampsPerFrame, _mStylePreset);
        }

        public override Cursor GetCursor(double fStretchFactor)
        {
            return Cursors.Cross;
        }

        #endregion Public Methods
    }
}