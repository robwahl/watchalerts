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
    public class DrawingToolLine2D : AbstractDrawingTool
    {
        #region Constructor

        public DrawingToolLine2D()
        {
            _mDefaultStylePreset.Elements.Add("color", new StyleElementColor(Color.LightGreen));
            _mDefaultStylePreset.Elements.Add("line size", new StyleElementLineSize(2));
            _mDefaultStylePreset.Elements.Add("arrows", new StyleElementLineEnding(LineEnding.None));
            _mStylePreset = _mDefaultStylePreset.Clone();
        }

        #endregion Constructor

        #region Properties

        public override string DisplayName
        {
            get { return ScreenManagerLang.ToolTip_DrawingToolLine2D; }
        }

        public override Bitmap Icon
        {
            get { return Properties.Drawings.line; }
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

        /// <summary>
        ///     This static property is used to keep the same setting for new lines.
        ///     Once we activate the measure, new lines will be created with the setting on, and vice versa.
        /// </summary>
        public static bool ShowMeasure;

        #endregion Properties

        #region Members

        private readonly DrawingStyle _mDefaultStylePreset = new DrawingStyle();
        private DrawingStyle _mStylePreset;

        #endregion Members

        #region Public Methods

        public override AbstractDrawing GetNewDrawing(Point origin, long iTimestamp, long averageTimeStampsPerFrame)
        {
            return new DrawingLine2D(origin, new Point(origin.X + 10, origin.Y), iTimestamp, averageTimeStampsPerFrame,
                _mStylePreset);
        }

        public override Cursor GetCursor(double fStretchFactor)
        {
            return Cursors.Cross;
        }

        #endregion Public Methods
    }
}