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
using Kinovea.Services;
using System;

namespace Kinovea.ScreenManager
{
    public class CommandDeleteDrawing : IUndoableCommand
    {
        private readonly Action _mDoScreenInvalidate;
        private readonly AbstractDrawing _mDrawing;
        private readonly int _mIDrawingIndex;
        private readonly long _mIFramePosition;
        private readonly Metadata _mMetadata;

        #region constructor

        public CommandDeleteDrawing(Action invalidate, Metadata metadata, long iFramePosition, int iDrawingIndex)
        {
            _mDoScreenInvalidate = invalidate;
            _mIFramePosition = iFramePosition;
            _mMetadata = metadata;
            _mIDrawingIndex = iDrawingIndex;

            var iIndex = GetKeyframeIndex();
            if (iIndex >= 0)
            {
                _mDrawing = _mMetadata[iIndex].Drawings[_mIDrawingIndex];
            }
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.CommandDeleteDrawing_FriendlyName + " (" + _mDrawing + ")"; }
        }

        /// <summary>
        ///     Execution de la commande
        /// </summary>
        public void Execute()
        {
            // It should work because all add/delete actions modify the undo stack.
            // When we come back here for a redo, we should be in the exact same state
            // as the first time.
            // Even if drawings were added in between, we can't come back here
            // before all those new drawings have been unstacked from the m_CommandStack stack.

            var iIndex = GetKeyframeIndex();
            if (iIndex >= 0)
            {
                _mMetadata[iIndex].Drawings.RemoveAt(_mIDrawingIndex);
                _mMetadata.SelectedDrawing = -1;
                _mMetadata.SelectedDrawingFrame = -1;
                _mDoScreenInvalidate();
            }
        }

        public void Unexecute()
        {
            // Recreate the drawing.

            // 1. Look for the keyframe
            var iIndex = GetKeyframeIndex();
            if (iIndex >= 0)
            {
                // We must insert exactly where we deleted, otherwise the drawing table gets messed up.
                // We must still be able to undo any Add action that where performed before.
                _mMetadata[iIndex].Drawings.Insert(_mIDrawingIndex, _mDrawing);
                _mDoScreenInvalidate();
            }
        }

        private int GetKeyframeIndex()
        {
            var iIndex = -1;
            for (var i = 0; i < _mMetadata.Count; i++)
            {
                if (_mMetadata[i].Position == _mIFramePosition)
                {
                    iIndex = i;
                }
            }

            return iIndex;
        }
    }
}