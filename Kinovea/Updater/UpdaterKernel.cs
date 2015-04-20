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

using Kinovea.Services;
using Kinovea.Updater.Languages;
using Kinovea.Updater.Properties;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: CLSCompliant(true)]

namespace Kinovea.Updater
{
    [ComVisible(true)]
    internal class UpdaterKernel : IKernel
    {
        #region Members

        private readonly ToolStripMenuItem _mnuCheckForUpdates = new ToolStripMenuItem();

        #endregion Members

        #region Menu Event Handlers

        public static void MnuCheckForUpdatesOnClick(object sender, EventArgs e)
        {
            // Stop playing if needed.
            var dp = DelegatesPool.Instance();
            if (dp.StopPlaying != null)
            {
                dp.StopPlaying();
            }

            // Download the update configuration file from the webserver.
            var hiRemote = PreferencesManager.ExperimentalRelease
                ? new HelpIndex("http://www.kinovea.org/setup/updatebeta.xml")
                : new HelpIndex("http://www.kinovea.org/setup/update.xml");

            if (hiRemote.LoadSuccess)
            {
                if (dp.DeactivateKeyboardHandler != null)
                {
                    dp.DeactivateKeyboardHandler();
                }

                // Check if we are up to date.
                //testUpdate = true;
                var currentVersion = new ThreePartsVersion(PreferencesManager.ReleaseVersion);
                if (hiRemote.AppInfos.Version > currentVersion)
                {
                    // We are not up to date, display the full dialog.
                    // The dialog is responsible for displaying the download success msg box.
                    var ud = new UpdateDialog2(hiRemote);
                    ud.ShowDialog();
                    ud.Dispose();
                }
                else
                {
                    // We are up to date, display a simple confirmation box.
                    MessageBox.Show(UpdaterLang.Updater_UpToDate, UpdaterLang.Updater_Title,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                if (dp.ActivateKeyboardHandler != null)
                {
                    dp.ActivateKeyboardHandler();
                }
            }
            else
            {
                // Remote connection failed, we are probably firewalled.
                MessageBox.Show(UpdaterLang.Updater_InternetError, UpdaterLang.Updater_Title, MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
        }

        #endregion Menu Event Handlers

        #region IKernel Implementation

        public void BuildSubTree()
        {
            // No sub modules.
        }

        public void ExtendMenu(ToolStrip menu)
        {
            //Catch Options Menu (5)
            var mnuCatchOptions = new ToolStripMenuItem
            {
                MergeIndex = 5,
                MergeAction = MergeAction.MatchOnly
            };

            // sep
            var mnuSep = new ToolStripSeparator
            {
                MergeIndex = 2,
                MergeAction = MergeAction.Insert
            };

            //Update
            _mnuCheckForUpdates.Image = Resources.software_update;
            _mnuCheckForUpdates.Click += MnuCheckForUpdatesOnClick;

            _mnuCheckForUpdates.MergeIndex = 3;
            _mnuCheckForUpdates.MergeAction = MergeAction.Insert;

            mnuCatchOptions.DropDownItems.AddRange(new ToolStripItem[] { mnuSep, _mnuCheckForUpdates });

            MenuStrip thisMenu;
            using (thisMenu = new MenuStrip())
            {
                thisMenu.Items.AddRange(new ToolStripItem[] { mnuCatchOptions });
                thisMenu.AllowMerge = true;

                ToolStripManager.Merge(thisMenu, menu);
            }

            RefreshUiCulture();
        }

        public void ExtendToolBar(ToolStrip toolbar)
        {
            // Nothing at this level.
            // No sub modules.
        }

        public void ExtendStatusBar(ToolStrip statusbar)
        {
            // Nothing at this level.
            // No sub modules.
        }

        public void ExtendUi()
        {
            // No sub modules.
        }

        public void RefreshUiCulture()
        {
            _mnuCheckForUpdates.Text = UpdaterLang.mnuCheckForUpdates;
        }

        public void CloseSubModules()
        {
            // No sub modules to close.
            // Nothing more to do here.
        }

        #endregion IKernel Implementation
    }
}