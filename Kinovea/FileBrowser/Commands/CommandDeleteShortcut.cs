#region License

/*
Copyright © Joan Charmant 2011.
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

using Kinovea.FileBrowser.Languages;
using Kinovea.Services;

namespace Kinovea.FileBrowser.Commands
{
    public class CommandDeleteShortcut : IUndoableCommand
    {
        #region Constructor

        public CommandDeleteShortcut(FileBrowserUserInterface fbUi, ShortcutFolder shortcut)
        {
            MFbUi = fbUi;
            MShortcut = shortcut;
        }

        #endregion Constructor

        public string FriendlyName
        {
            get { return FileBrowserLang.mnuDeleteShortcut; }
        }

        public void Execute()
        {
            var prefManager = PreferencesManager.Instance();

            // Parse the list and remove any match.
            for (var i = prefManager.ShortcutFolders.Count - 1; i >= 0; i--)
            {
                if (prefManager.ShortcutFolders[i].Location == MShortcut.Location)
                {
                    prefManager.ShortcutFolders.RemoveAt(i);
                }
            }

            prefManager.Export();

            // Refresh the list.
            MFbUi.ReloadShortcuts();
        }

        public void Unexecute()
        {
            // Add the shortcut back to the list (if it hasn't been added again in the meantime).
            var prefManager = PreferencesManager.Instance();

            var bIsShortcutAlready = false;
            foreach (var sf in prefManager.ShortcutFolders)
            {
                if (sf.Location == MShortcut.Location)
                {
                    bIsShortcutAlready = true;
                    break;
                }
            }

            if (!bIsShortcutAlready)
            {
                prefManager.ShortcutFolders.Add(MShortcut);
                prefManager.Export();

                // Refresh the list.
                MFbUi.ReloadShortcuts();
            }
        }

        #region Members

        public readonly FileBrowserUserInterface MFbUi;
        public readonly ShortcutFolder MShortcut;

        #endregion Members
    }
}