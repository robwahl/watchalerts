/*
Copyright � Joan Charmant 2008.
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
    /// <summary>
    ///     This command is used to translate the screen list in actual screen panels.
    ///     We generally land here after a command modified the screen list.
    ///     We parse the list and make sure the panels are conform, by adding or removing them.
    /// </summary>
    public class CommandShowScreens : ICommand
    {
        private readonly ScreenManagerKernel _screenManagerKernel;

        #region constructor

        public CommandShowScreens(ScreenManagerKernel smk)
        {
            _screenManagerKernel = smk;
        }

        #endregion constructor

        public string FriendlyName
        {
            get { return ScreenManagerLang.CommandShowScreen_FriendlyName; }
        }

        public void Execute()
        {
            // - parse the current screen list and fill panels with screens.
            // - hide unused panels if necessary.

            var smui = _screenManagerKernel.Ui as ScreenManagerUserInterface;
            if (smui != null)
            {
                // Empty the screen panels.
                smui.splitScreens.Panel1.Controls.Clear();
                smui.splitScreens.Panel2.Controls.Clear();
                smui.CloseThumbnails();

                if (_screenManagerKernel.ScreenList.Count == 1)
                {
                    smui.pnlScreens.Visible = true;
                    smui.AllowDrop = false;

                    // left screen enabled.
                    smui.splitScreens.Panel1Collapsed = false;
                    smui.splitScreens.Panel1.AllowDrop = true;
                    smui.splitScreens.Panel1.Controls.Add(_screenManagerKernel.ScreenList[0].Ui);

                    // right screen disabled
                    smui.splitScreens.Panel2Collapsed = true;
                    smui.splitScreens.Panel2.AllowDrop = false;
                }
                else if (_screenManagerKernel.ScreenList.Count == 2)
                {
                    smui.pnlScreens.Visible = true;
                    smui.AllowDrop = false;

                    // left screen
                    smui.splitScreens.Panel1Collapsed = false;
                    smui.splitScreens.Panel1.AllowDrop = true;
                    smui.splitScreens.Panel1.Controls.Add(_screenManagerKernel.ScreenList[0].Ui);

                    // right screen
                    smui.splitScreens.Panel2Collapsed = false;
                    smui.splitScreens.Panel2.AllowDrop = true;
                    smui.splitScreens.Panel2.Controls.Add(_screenManagerKernel.ScreenList[1].Ui);
                }
                else if (_screenManagerKernel.ScreenList.Count == 0)
                {
                    smui.pnlScreens.Visible = false;
                    smui.AllowDrop = true;

                    smui.splitScreens.Panel1.AllowDrop = false;
                    smui.splitScreens.Panel2.AllowDrop = false;

                    smui.BringBackThumbnails();
                }
            }

            // Update status bar.
            _screenManagerKernel.UpdateStatusBar();
        }
    }
}