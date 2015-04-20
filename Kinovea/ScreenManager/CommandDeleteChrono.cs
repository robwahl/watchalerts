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
    public class CommandDeleteChrono : IUndoableCommand
    {
        private readonly DrawingChrono _mChrono;
        private readonly Metadata _mMetadata;
        private readonly PlayerScreenUserInterface _mPsui;

        #region constructor

        public CommandDeleteChrono(PlayerScreenUserInterface psui, Metadata metadata)
        {
            _mPsui = psui;
            _mMetadata = metadata;
            _mChrono = _mMetadata.ExtraDrawings[_mMetadata.SelectedExtraDrawing] as DrawingChrono;
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.mnuChronoDelete; }
        }

        public void Execute()
        {
            if (_mChrono != null)
            {
                _mMetadata.ExtraDrawings.Remove(_mChrono);
                _mPsui.pbSurfaceScreen.Invalidate();
            }
        }

        public void Unexecute()
        {
            // Recreate the drawing.
            if (_mChrono != null)
            {
                _mMetadata.ExtraDrawings.Add(_mChrono);
                _mPsui.pbSurfaceScreen.Invalidate();
            }
        }
    }
}