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
using System.Collections.Generic;

namespace Kinovea.ScreenManager
{
    public class CommandDeleteEndOfTrack : IUndoableCommand
    {
        private readonly long _mITimeStamp;
        private readonly Metadata _mMetadata;
        private readonly PlayerScreenUserInterface _mPsui;
        private readonly Track _mTrack;
        public List<AbstractTrackPoint> MPositions;

        #region constructor

        public CommandDeleteEndOfTrack(PlayerScreenUserInterface psui, Metadata metadata, long iTimeStamp)
        {
            _mPsui = psui;
            _mMetadata = metadata;
            _mTrack = _mMetadata.ExtraDrawings[_mMetadata.SelectedExtraDrawing] as Track;
            _mITimeStamp = iTimeStamp;
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.mnuDeleteEndOfTrajectory; }
        }

        public void Execute()
        {
            // We store the old end-of-track values only here (and not in the ctor)
            // because some points may be moved between the undo and
            // the redo and we'll want to keep teir values.
            if (_mTrack != null)
            {
                MPositions = _mTrack.GetEndOfTrack(_mITimeStamp);
                _mTrack.ChopTrajectory(_mITimeStamp);
                _mPsui.pbSurfaceScreen.Invalidate();
            }
        }

        public void Unexecute()
        {
            // Revival of the discarded points.
            if (MPositions != null && _mTrack != null)
            {
                _mTrack.AppendPoints(_mITimeStamp, MPositions);
            }
            _mPsui.pbSurfaceScreen.Invalidate();
        }
    }
}