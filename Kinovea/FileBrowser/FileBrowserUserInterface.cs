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

using ExpTreeLib;
using Kinovea.FileBrowser.Commands;
using Kinovea.FileBrowser.Languages;
using Kinovea.FileBrowser.Properties;
using Kinovea.Services;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.FileBrowser
{
    /// <summary>
    ///     The user interface for all explorer like stuff.
    ///     We maintain the synchronization between the shortcut and exptree tab
    ///     when we move between shortcuts. We don't maintain it the other way around.
    /// </summary>
    public partial class FileBrowserUserInterface : UserControl
    {
        #region Members

        private CShItem _mCurrentExptreeItem; // Current item in exptree tab.
        private CShItem _mCurrentShortcutItem; // Current item in shortcuts tab.
        private bool _mBExpanding; // True if the exptree is currently auto expanding. To avoid reentry.
        private bool _mBInitializing = true;
        private readonly PreferencesManager _mPreferencesManager = PreferencesManager.Instance();

        #region Context menu

        private readonly ContextMenuStrip _popMenu = new ContextMenuStrip();
        private readonly ToolStripMenuItem _mnuAddToShortcuts = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuDeleteShortcut = new ToolStripMenuItem();

        #endregion Context menu

        private static readonly string[] MKnownFileTypes =
        {
            ".3gp", ".asf", ".avi", ".dv", ".flv", ".f4v", ".m1v", ".m2p", ".m2t",
            ".m2ts", ".mts", ".m2v", ".m4v", ".mkv", ".mod", ".mov", ".moov", ".mpg", ".mpeg", ".tod", ".mxf",
            ".mp4", ".mpv", ".ogg", ".ogm", ".ogv", ".qt", ".rm", ".swf", ".vob", ".webm", ".wmv",
            ".dpa",
            ".jpg", ".jpeg", ".png", ".bmp", ".gif"
        };

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor & Initialization

        public FileBrowserUserInterface()
        {
            InitializeComponent();
            Dock = DockStyle.Fill;
            btnAddShortcut.Parent = lblFavFolders;
            btnDeleteShortcut.Parent = lblFavFolders;

            // Drag Drop handling.
            lvExplorer.ItemDrag += lv_ItemDrag;
            lvShortcuts.ItemDrag += lv_ItemDrag;
            etExplorer.AllowDrop = false;
            etShortcuts.AllowDrop = false;

            BuildContextMenu();

            // Registers our exposed functions to the DelegatePool.
            var dp = DelegatesPool.Instance();
            dp.RefreshFileExplorer = DoRefreshFileList;

            // Take the list of shortcuts from the prefs and load them.
            ReloadShortcuts();

            // Reload last tab from prefs.
            // We don't reload the splitters here, because we are not at full size yet and they are anchored.
            tabControl.SelectedIndex = (int)_mPreferencesManager.ActiveTab;

            Application.Idle += IdleDetector;
        }

        private void BuildContextMenu()
        {
            // Add an item to shortcuts
            _mnuAddToShortcuts.Image = Resources.folder_add;
            _mnuAddToShortcuts.Click += mnuAddToShortcuts_Click;
            _mnuAddToShortcuts.Visible = false;

            // Delete selected shortcut
            _mnuDeleteShortcut.Image = Resources.folder_delete;
            _mnuDeleteShortcut.Click += mnuDeleteShortcut_Click;
            _mnuDeleteShortcut.Visible = false;

            _popMenu.Items.AddRange(new ToolStripItem[] { _mnuAddToShortcuts, _mnuDeleteShortcut });

            // The context menus will be configured on a per event basis.
            etShortcuts.ContextMenuStrip = _popMenu;
            etExplorer.ContextMenuStrip = _popMenu;
        }

        private void IdleDetector(object sender, EventArgs e)
        {
            // Oh, we are idle. The ScreenManager should be loaded now,
            // and thus will have registered its DisplayThumbnails delegate.

            Log.Debug("Application is idle in FileBrowserUserInterface.");

            // This is a one time only routine.
            Application.Idle -= IdleDetector;
            _mBInitializing = false;

            // Now that we are at full size, we can load splitters from prefs.
            splitExplorerFiles.SplitterDistance = _mPreferencesManager.ExplorerFilesSplitterDistance;
            splitShortcutsFiles.SplitterDistance = _mPreferencesManager.ShortcutsFilesSplitterDistance;

            // Load the initial directory.
            Log.Debug("Load initial directory.");
            DoRefreshFileList(true);
        }

        #endregion Constructor & Initialization

        #region Public interface

        public void DoRefreshFileList(bool bRefreshThumbnails)
        {
            // Called when:
            // - the user changes node in exptree, either explorer or shortcuts
            // - a file modification happens in the thumbnails page. (delete/rename)
            // - a capture is completed.

            Log.Debug("DoRefreshFileList called");

            // We don't update during app start up, because we would most probably
            // end up loading the desktop, and then the saved folder.
            if (!_mBInitializing)
            {
                // Figure out which tab we are on to update the right listview.
                if (tabControl.SelectedIndex == 0)
                {
                    // ExpTree tab.
                    if (_mCurrentExptreeItem != null)
                    {
                        UpdateFileList(_mCurrentExptreeItem, lvExplorer, bRefreshThumbnails);
                    }
                }
                else if (tabControl.SelectedIndex == 1)
                {
                    // Shortcuts tab.
                    if (_mCurrentShortcutItem != null)
                    {
                        UpdateFileList(_mCurrentShortcutItem, lvShortcuts, bRefreshThumbnails);
                    }
                    else if (_mCurrentExptreeItem != null)
                    {
                        // This is the special case where we select a folder on the exptree tab
                        // and then move to the shortcuts tab.
                        // -> reload the hidden list of the exptree tab.
                        // We also force the thumbnail refresh, because in this case it is the only way to update the
                        // filename list held in ScreenManager...
                        UpdateFileList(_mCurrentExptreeItem, lvExplorer, true);
                    }
                }
            }
        }

        public void RefreshUiCulture()
        {
            // ExpTree tab.
            tabPageClassic.Text = FileBrowserLang.tabExplorer;
            lblFolders.Text = FileBrowserLang.lblFolders;
            lblVideoFiles.Text = FileBrowserLang.lblVideoFiles;

            // Shortcut tab.
            tabPageShortcuts.Text = FileBrowserLang.tabShortcuts;
            lblFavFolders.Text = lblFolders.Text;
            lblFavFiles.Text = lblVideoFiles.Text;
            etShortcuts.RootDisplayName = tabPageShortcuts.Text;

            // Menus
            _mnuAddToShortcuts.Text = FileBrowserLang.mnuAddToShortcuts;
            _mnuDeleteShortcut.Text = FileBrowserLang.mnuDeleteShortcut;

            // ToolTips
            ttTabs.SetToolTip(btnAddShortcut, FileBrowserLang.mnuAddShortcut);
            ttTabs.SetToolTip(btnDeleteShortcut, FileBrowserLang.mnuDeleteShortcut);
        }

        public void ReloadShortcuts()
        {
            // Refresh the folder tree with data stored in prefs.
            var shortcuts = new ArrayList();
            var scuts = _mPreferencesManager.ShortcutFolders;

            // Sort by last folder name.
            scuts.Sort();

            foreach (var sf in scuts)
            {
                if (Directory.Exists(sf.Location))
                    shortcuts.Add(sf.Location);
            }

            etShortcuts.SetShortcuts(shortcuts);
            etShortcuts.StartUpDirectory = ExpTree.StartDir.Desktop;
        }

        public void ResetShortcutList()
        {
            lvShortcuts.Clear();
        }

        public void Closing()
        {
            if (_mCurrentExptreeItem != null)
            {
                _mPreferencesManager.LastBrowsedDirectory = _mCurrentExptreeItem.Path;
            }

            _mPreferencesManager.ExplorerFilesSplitterDistance = splitExplorerFiles.SplitterDistance;
            _mPreferencesManager.ShortcutsFilesSplitterDistance = splitShortcutsFiles.SplitterDistance;

            // Flush all prefs not previoulsy flushed.
            _mPreferencesManager.Export();
        }

        #endregion Public interface

        #region Explorer tab

        #region TreeView

        private void etExplorer_ExpTreeNodeSelected(string selPath, CShItem item)
        {
            _mCurrentExptreeItem = item;

            // Update the list view and thumb page.
            if (!_mBExpanding && !_mBInitializing)
            {
                // We don't maintain synchronization with the Shortcuts tab.
                ResetShortcutList();
                UpdateFileList(_mCurrentExptreeItem, lvExplorer, true);
            }
        }

        private void etExplorer_MouseEnter(object sender, EventArgs e)
        {
            // Give focus to enable mouse scroll.
            etExplorer.Focus();
        }

        private void etExplorer_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _mnuDeleteShortcut.Visible = false;

                // User must first select a node to add it to shortcuts.
                if (etExplorer.IsOnSelectedItem(e.Location))
                {
                    if (!_mCurrentExptreeItem.Path.StartsWith("::"))
                    {
                        _mnuAddToShortcuts.Visible = true;
                    }
                    else
                    {
                        // Root node selected. Cannot add.
                        _mnuAddToShortcuts.Visible = false;
                    }
                }
                else
                {
                    _mnuAddToShortcuts.Visible = false;
                }
            }
        }

        #endregion TreeView

        #region ListView

        private void lvExplorer_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            LaunchItemAt(lvExplorer, e);
        }

        private void lvExplorer_MouseEnter(object sender, EventArgs e)
        {
            // Give focus to enable mouse scroll.
            lvExplorer.Focus();
        }

        #endregion ListView

        #endregion Explorer tab

        #region Shortcuts tab

        #region Shortcuts Handling

        private void btnAddShortcut_Click(object sender, EventArgs e)
        {
            AddShortcut();
        }

        private void btnDeleteShortcut_Click(object sender, EventArgs e)
        {
            DeleteSelectedShortcut();
        }

        private void AddShortcut()
        {
            // Launch the OpenFolder common dialog.
            var fbd = new FolderBrowserDialog();

            fbd.ShowNewFolderButton = true;
            fbd.RootFolder = Environment.SpecialFolder.Desktop;

            if (fbd.ShowDialog() == DialogResult.OK && fbd.SelectedPath.Length > 0)
            {
                // Default the friendly name to the folder name.
                var sf = new ShortcutFolder(Path.GetFileName(fbd.SelectedPath), fbd.SelectedPath);
                _mPreferencesManager.ShortcutFolders.Add(sf);
                _mPreferencesManager.Export();
                ReloadShortcuts();
            }
        }

        private void DeleteSelectedShortcut()
        {
            if (_mCurrentShortcutItem != null)
            {
                // Find and delete the shortcut.
                foreach (var sf in _mPreferencesManager.ShortcutFolders)
                {
                    if (sf.Location == _mCurrentShortcutItem.Path)
                    {
                        IUndoableCommand cds = new CommandDeleteShortcut(this, sf);
                        var cm = CommandManager.Instance();
                        cm.LaunchUndoableCommand(cds);
                        break;
                    }
                }
            }
        }

        #endregion Shortcuts Handling

        #region TreeView

        private void etShortcuts_ExpTreeNodeSelected(string selPath, CShItem item)
        {
            // Update the list view and thumb page.
            Log.Debug(string.Format("Shortcut Selected : {0}.", Path.GetFileName(selPath)));
            _mCurrentShortcutItem = item;

            // Initializing happens on the explorer tab. We'll refresh later.
            if (!_mBInitializing)
            {
                // The operation that will trigger the thumbnail refresh MUST only be called at the end.
                // Otherwise the other threads take precedence and the thumbnails are not
                // shown progressively but all at once, when other operations are over.

                // Start by updating hidden explorer tab.
                // Update list and maintain synchronization with the tree.
                UpdateFileList(_mCurrentShortcutItem, lvExplorer, false);

                _mBExpanding = true;
                etExplorer.ExpandANode(_mCurrentShortcutItem);
                _mBExpanding = false;
                _mCurrentExptreeItem = etExplorer.SelectedItem;

                // Finally update the shortcuts tab, and refresh thumbs.
                UpdateFileList(_mCurrentShortcutItem, lvShortcuts, true);
            }
            Log.Debug("Shortcut Selected - Operations done.");
        }

        private void etShortcuts_MouseEnter(object sender, EventArgs e)
        {
            // Give focus to enable mouse scroll.
            etShortcuts.Focus();
        }

        private void etShortcuts_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // User must first select a node to add it to shortcuts.
                // Otherwise we don't have the infos about the item.
                if (_mCurrentExptreeItem != null && etShortcuts.IsOnSelectedItem(e.Location))
                {
                    if (!_mCurrentExptreeItem.Path.StartsWith("::"))
                    {
                        // Do we have it already ?
                        var bIsShortcutAlready = false;
                        foreach (var sf in _mPreferencesManager.ShortcutFolders)
                        {
                            if (_mCurrentShortcutItem.Path == sf.Location)
                            {
                                bIsShortcutAlready = true;
                                break;
                            }
                        }

                        if (bIsShortcutAlready)
                        {
                            // Cannot add, can delete.
                            _mnuAddToShortcuts.Visible = false;
                            _mnuDeleteShortcut.Visible = true;
                        }
                        else
                        {
                            // Can add, cannot delete.
                            _mnuAddToShortcuts.Visible = true;
                            _mnuDeleteShortcut.Visible = false;
                        }
                    }
                    else
                    {
                        _mnuDeleteShortcut.Visible = false;
                        _mnuAddToShortcuts.Visible = false;
                    }
                }
                else
                {
                    _mnuDeleteShortcut.Visible = false;
                    _mnuAddToShortcuts.Visible = false;
                }
            }
        }

        #endregion TreeView

        #region ListView

        private void lvShortcuts_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            LaunchItemAt(lvShortcuts, e);
        }

        private void lvShortcuts_MouseEnter(object sender, EventArgs e)
        {
            // Give focus to enable mouse scroll.
            lvShortcuts.Focus();
        }

        #endregion ListView

        #endregion Shortcuts tab

        #region Common

        private void TabControlSelectedIndexChanged(object sender, EventArgs e)
        {
            // Active tab changed.
            // We don't save to file now as this is not a critical data to loose.
            _mPreferencesManager.ActiveTab = (ActiveFileBrowserTab)tabControl.SelectedIndex;
        }

        private void _tabControl_KeyDown(object sender, KeyEventArgs e)
        {
            // Discard keyboard event as they interfere with player functions
            e.Handled = true;
        }

        private bool IsKnownFileType(string extension)
        {
            // Check if the file is of known extension.
            // All known extensions are kept in a static array.

            // Todo ?: load the known extensions from a file so we can add to them dynamically
            // and the user can add or remove depending on his specific config ?
            var bIsKnown = false;

            foreach (var known in MKnownFileTypes)
            {
                if (extension.Equals(known, StringComparison.OrdinalIgnoreCase))
                {
                    bIsKnown = true;
                    break;
                }
            }

            return bIsKnown;
        }

        private void UpdateFileList(CShItem folder, ListView listView, bool bRefreshThumbnails)
        {
            // Update a file list with the given folder.
            // Triggers an update of the thumbnails pane if requested.

            Log.Debug(string.Format("Updating file list : {0}", listView.Name));

            if (folder != null)
            {
                Cursor = Cursors.WaitCursor;

                listView.BeginUpdate();
                listView.Items.Clear();

                // Each list element will store the CShItem it's referring to in its Tag property.
                // Get all files in the folder, and add them to the list.
                var fileList = folder.GetFiles();
                var fileNames = new List<string>();
                for (var i = 0; i < fileList.Count; i++)
                {
                    var shellItem = (CShItem)fileList[i];

                    if (IsKnownFileType(Path.GetExtension(shellItem.Path)))
                    {
                        var lvi = new ListViewItem(shellItem.DisplayName);

                        lvi.Tag = shellItem;
                        //lvi.ImageIndex = ExpTreeLib.SystemImageListManager.GetIconIndex(ref shellItem, false, false);
                        lvi.ImageIndex = 6;

                        listView.Items.Add(lvi);

                        fileNames.Add(shellItem.Path);
                    }
                }

                listView.EndUpdate();
                Log.Debug("List updated");

                // Even if we don't want to reload the thumbnails, we must ensure that
                // the screen manager backup list is in sync with the actual file list.
                // desync can happen in case of renaming and deleting files.
                // the screenmanager backup list is used at BringBackThumbnail,
                // (i.e. when we close a screen)
                var dp = DelegatesPool.Instance();
                if (dp.DisplayThumbnails != null)
                {
                    Log.Debug("Asking the ScreenManager to refresh the thumbnails.");
                    dp.DisplayThumbnails(fileNames, bRefreshThumbnails);
                }

                Cursor = Cursors.Default;
            }
        }

        private void lv_ItemDrag(object sender, ItemDragEventArgs e)
        {
            // Starting a drag drop.
            var lvi = e.Item as ListViewItem;
            if (lvi != null)
            {
                var item = lvi.Tag as CShItem;
                if (item != null)
                {
                    if (item.IsFileSystem)
                    {
                        DoDragDrop(item.Path, DragDropEffects.All);
                    }
                }
            }
        }

        private void LaunchItemAt(ListView listView, MouseEventArgs e)
        {
            // Launch the video.

            var lvi = listView.GetItemAt(e.X, e.Y);

            if (lvi != null && listView.SelectedItems != null && listView.SelectedItems.Count == 1)
            {
                var item = listView.SelectedItems[0].Tag as CShItem;

                if (item != null)
                {
                    if (item.IsFileSystem)
                    {
                        var dp = DelegatesPool.Instance();
                        if (dp.LoadMovieInScreen != null)
                        {
                            dp.LoadMovieInScreen(item.Path, -1, true);
                        }
                    }
                }
            }
        }

        #endregion Common

        #region Menu Event Handlers

        private void mnuAddToShortcuts_Click(object sender, EventArgs e)
        {
            CShItem itemToAdd;

            if (tabControl.SelectedIndex == (int)ActiveFileBrowserTab.Explorer)
            {
                itemToAdd = _mCurrentExptreeItem;
            }
            else
            {
                itemToAdd = _mCurrentShortcutItem;
            }

            if (itemToAdd != null)
            {
                // Don't add if root node. (Special Folder)
                if (!itemToAdd.Path.StartsWith("::"))
                {
                    var sf = new ShortcutFolder(Path.GetFileName(itemToAdd.Path), itemToAdd.Path);
                    _mPreferencesManager.ShortcutFolders.Add(sf);
                    _mPreferencesManager.Export();
                    ReloadShortcuts();
                }
            }
        }

        private void mnuDeleteShortcut_Click(object sender, EventArgs e)
        {
            DeleteSelectedShortcut();
        }

        #endregion Menu Event Handlers
    }
}