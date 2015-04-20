#region Licence

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

#endregion Licence

using Kinovea.ScreenManager.Languages;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public class DrawingToolText : AbstractDrawingTool
    {
        #region Constructor

        public DrawingToolText()
        {
            _mDefaultStylePreset.Elements.Add("back color", new StyleElementColor(Color.CornflowerBlue));
            _mDefaultStylePreset.Elements.Add("font size", new StyleElementFontSize(12));

            _mStylePreset = _mDefaultStylePreset.Clone();
        }

        #endregion Constructor

        #region Properties

        public override string DisplayName
        {
            get { return ScreenManagerLang.ToolTip_DrawingToolText; }
        }

        public override Bitmap Icon
        {
            get { return Properties.Drawings.label; }
        }

        public override bool Attached
        {
            get { return true; }
        }

        public override bool KeepTool
        {
            get { return false; }
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
            return new DrawingText(origin, iTimestamp, averageTimeStampsPerFrame, _mStylePreset);
        }

        public override Cursor GetCursor(double fStretchFactor)
        {
            return Cursors.IBeam;
        }

        #endregion Public Methods
    }
}