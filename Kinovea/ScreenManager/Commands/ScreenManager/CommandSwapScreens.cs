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
    public class CommandSwapScreens : IUndoableCommand
    {
        private readonly ScreenManagerKernel _mScreenManagerKernel;

        #region constructor

        public CommandSwapScreens(ScreenManagerKernel smk)
        {
            _mScreenManagerKernel = smk;
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.CommandSwapScreens_FriendlyName; }
        }

        public void Execute()
        {
            // We keep the list ordered. [0] = left.
            var temp = _mScreenManagerKernel.ScreenList[0];
            _mScreenManagerKernel.ScreenList[0] = _mScreenManagerKernel.ScreenList[1];
            _mScreenManagerKernel.ScreenList[1] = temp;

            // Show new disposition.
            var smui = _mScreenManagerKernel.Ui as ScreenManagerUserInterface;
            if (smui != null)
            {
                smui.splitScreens.Panel1.Controls.Clear();
                smui.splitScreens.Panel2.Controls.Clear();

                smui.splitScreens.Panel1.Controls.Add(_mScreenManagerKernel.ScreenList[0].Ui);
                smui.splitScreens.Panel2.Controls.Add(_mScreenManagerKernel.ScreenList[1].Ui);
            }

            // the following lines are placed here so they also get called at unexecute.
            _mScreenManagerKernel.OrganizeMenus();
            _mScreenManagerKernel.UpdateStatusBar();
            _mScreenManagerKernel.SwapSync();
            _mScreenManagerKernel.SetSyncPoint(true);
        }

        public void Unexecute()
        {
            Execute();
        }
    }
}