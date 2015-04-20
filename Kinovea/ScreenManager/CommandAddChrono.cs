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
    public class CommandAddChrono : IUndoableCommand
    {
        private readonly DrawingChrono _mChrono;
        private readonly Action _mDoInvalidate;
        private readonly Action _mDoUndrawn;
        private readonly Metadata _mMetadata;

        #region constructor

        public CommandAddChrono(Action invalidate, Action undrawn, Metadata metadata)
        {
            _mDoInvalidate = invalidate;
            _mDoUndrawn = undrawn;
            _mMetadata = metadata;
            _mChrono = _mMetadata.ExtraDrawings[_mMetadata.SelectedExtraDrawing] as DrawingChrono;
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.CommandAddChrono_FriendlyName; }
        }

        public void Execute()
        {
            // In the case of the first execution, the Chrono has already been added to the extra drawings list.
            if (_mChrono != null)
            {
                if (_mMetadata.ExtraDrawings.IndexOf(_mChrono) == -1)
                {
                    _mMetadata.AddChrono(_mChrono);
                    _mDoInvalidate();
                }
            }
        }

        public void Unexecute()
        {
            // Delete this chrono.
            if (_mChrono != null)
            {
                _mMetadata.ExtraDrawings.Remove(_mChrono);
                _mDoUndrawn();
                _mDoInvalidate();
            }
        }
    }
}