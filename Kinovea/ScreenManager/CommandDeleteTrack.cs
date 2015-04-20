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

namespace Kinovea.ScreenManager
{
    public class CommandDeleteTrack : IUndoableCommand
    {
        private readonly Metadata _mMetadata;
        private readonly PlayerScreenUserInterface _mPsui;
        private readonly Track _mTrack;

        #region constructor

        public CommandDeleteTrack(PlayerScreenUserInterface psui, Metadata metadata)
        {
            _mPsui = psui;
            _mMetadata = metadata;
            _mTrack = _mMetadata.ExtraDrawings[_mMetadata.SelectedExtraDrawing] as Track;
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.mnuDeleteTrajectory; }
        }

        public void Execute()
        {
            if (_mTrack != null)
            {
                _mMetadata.ExtraDrawings.Remove(_mTrack);
                _mPsui.pbSurfaceScreen.Invalidate();
            }
        }

        public void Unexecute()
        {
            // Recreate the drawing.
            if (_mTrack != null)
            {
                _mMetadata.ExtraDrawings.Add(_mTrack);
                _mPsui.pbSurfaceScreen.Invalidate();
            }
        }
    }
}