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
    public class CommandAddDrawing : IUndoableCommand
    {
        private readonly Action _mDoInvalidate;
        private readonly Action _mDoUndrawn;
        private readonly AbstractDrawing _mDrawing;
        private readonly long _mIFramePosition;
        private readonly int _mITotalDrawings;
        private readonly Metadata _mMetadata;

        #region constructor

        public CommandAddDrawing(Action invalidate, Action undrawn, Metadata metadata, long iFramePosition)
        {
            _mDoInvalidate = invalidate;
            _mDoUndrawn = undrawn;

            _mIFramePosition = iFramePosition;
            _mMetadata = metadata;

            var iIndex = GetKeyframeIndex();
            if (iIndex >= 0)
            {
                _mITotalDrawings = _mMetadata[iIndex].Drawings.Count;
                _mDrawing = _mMetadata[iIndex].Drawings[0];
            }
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.CommandAddDrawing_FriendlyName + " (" + _mDrawing + ")"; }
        }

        /// <summary>
        ///     Command execution.
        /// </summary>
        public void Execute()
        {
            // We need to differenciate between two cases :
            // First execution : Work has already been done in the PlayerScreen (interactively).
            // Redo : We need to bring back the drawing to life.

            var iIndex = GetKeyframeIndex();
            if (iIndex >= 0)
            {
                if (_mMetadata[iIndex].Drawings.Count == _mITotalDrawings)
                {
                    // first exec.
                    // Nothing to do.
                }
                else if (_mMetadata[iIndex].Drawings.Count == _mITotalDrawings - 1)
                {
                    //Redo.
                    _mMetadata[iIndex].Drawings.Insert(0, _mDrawing);
                    _mDoInvalidate();
                }
            }
        }

        public void Unexecute()
        {
            // Delete the last drawing on Keyframe.

            // 1. Look for the keyframe
            var iIndex = GetKeyframeIndex();
            if (iIndex >= 0)
            {
                if (_mMetadata[iIndex].Drawings.Count > 0)
                {
                    _mMetadata[iIndex].Drawings.RemoveAt(0);
                    _mDoUndrawn();
                    _mDoInvalidate();
                }
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