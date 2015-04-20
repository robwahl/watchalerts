#region License

/*
Copyright © Joan Charmant 2009.
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

#endregion License

using Kinovea.ScreenManager.Languages;
using Kinovea.Services;

namespace Kinovea.ScreenManager
{
    public class CommandAddCaptureScreen : IUndoableCommand
    {
        #region Members

        private readonly ScreenManagerKernel _mScreenManagerKernel;

        #endregion Members

        #region constructor

        public CommandAddCaptureScreen(ScreenManagerKernel smk, bool bStoreState)
        {
            _mScreenManagerKernel = smk;
            if (bStoreState)
            {
                _mScreenManagerKernel.StoreCurrentState();
            }
        }

        #endregion constructor

        #region Properties

        public string FriendlyName
        {
            get { return ScreenManagerLang.CommandAddCaptureScreen_FriendlyName; }
        }

        #endregion Properties

        public void Execute()
        {
            var screen = new CaptureScreen(_mScreenManagerKernel);
            if (_mScreenManagerKernel.ScreenList.Count > 1) screen.Shared = true;
            screen.RefreshUiCulture();
            _mScreenManagerKernel.ScreenList.Add(screen);
        }

        public void Unexecute()
        {
            _mScreenManagerKernel.RecallState();
        }
    }
}