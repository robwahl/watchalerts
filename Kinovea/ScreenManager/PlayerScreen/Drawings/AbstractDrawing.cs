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

using Kinovea.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager.PlayerScreen.Drawings
{
    /// <summary>
    ///     Describes a generic drawing.
    ///     All drawings must implement rendering and manipulation methods.
    /// </summary>
    public abstract class AbstractDrawing
    {
        #region Concrete methods

        public static void CallInvalidateFromMenu(object sender)
        {
            // The screen invalidate hook was injected inside menus during popMenu attach.
            // This avoids having an injection hanging in DrawingTool.
            var tsmi = sender as ToolStripMenuItem;
            if (tsmi != null)
            {
                var screenInvalidate = tsmi.Tag as Action;
                if (screenInvalidate != null) screenInvalidate();
            }
        }

        #endregion Concrete methods

        #region Properties

        /// <summary>
        ///     Gets or set the fading object for this drawing.
        ///     This is used in opacity calculation for Persistence.
        /// </summary>
        public abstract InfosFading InfosFading { get; set; }

        /// <summary>
        ///     Get the capabilities of this drawing for the generic part of context menu.
        /// </summary>
        public abstract DrawingCapabilities Caps { get; }

        /// <summary>
        ///     Gets the list of extra context menu specific to this drawing.
        /// </summary>
        public abstract List<ToolStripMenuItem> ContextMenu { get; }

        #endregion Properties

        #region Abstract Methods

        /// <summary>
        ///     Draw this drawing on the provided canvas.
        /// </summary>
        /// <param name="canvas">The GDI+ surface on which to draw</param>
        /// <param name="transformer">A helper object providing coordinate systems transformation</param>
        /// <param name="bSelected">Whether the drawing is currently selected</param>
        /// <param name="iCurrentTimestamp">The current time position in the video</param>
        public abstract void Draw(Graphics canvas, CoordinateSystem transformer, bool bSelected, long iCurrentTimestamp);

        /// <summary>
        ///     Evaluates if a particular point is inside the drawing, on a handler, or completely outside the drawing.
        /// </summary>
        /// <param name="point">The coordinates at original image scale of the point to evaluate</param>
        /// <param name="iCurrentTimestamp">The current time position in the video</param>
        /// <returns>
        ///     -1 : missed. 0 : The drawing as a whole has been hit. n (with n>0) : The id of a manipulation handle that has
        ///     been hit
        /// </returns>
        public abstract int HitTest(Point point, long iCurrentTimestamp);

        /// <summary>
        ///     Move the specified handle to its new location.
        /// </summary>
        /// <param name="point">The new location of the handle, in original image scale coordinates</param>
        /// <param name="iHandleNumber">The handle identifier</param>
        public abstract void MoveHandle(Point point, int iHandleNumber);

        /// <summary>
        ///     Move the drawing as a whole.
        /// </summary>
        /// <param name="deltaX">Change in x coordinates</param>
        /// <param name="deltaY">Change in y coordinates</param>
        /// <param name="modifierKeys">Modifiers key pressed while moving the drawing</param>
        public abstract void MoveDrawing(int deltaX, int deltaY, Keys modifierKeys);

        #endregion Abstract Methods
    }

    /// <summary>
    ///     The various capabilities of a drawing, used to support dynamically adding generic menus.
    /// </summary>
    [Flags]
    public enum DrawingCapabilities
    {
        None = 0,
        ConfigureColor = 1,
        ConfigureColorSize = 2,
        Fading = 4,
        Opacity = 8
    }
}