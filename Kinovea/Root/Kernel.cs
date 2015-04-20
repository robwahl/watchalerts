#region Licence

/*
Copyright © Joan Charmant 2008-2009.
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

#endregion Licence

using Kinovea.FileBrowser;
using Kinovea.Root.Commands;
using Kinovea.Root.Languages;
using Kinovea.Root.Properties;
using Kinovea.Root.UserInterface;
using Kinovea.ScreenManager;
using Kinovea.Services;
using Kinovea.Updater;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Kinovea.Root
{
    public class RootKernel : IKernel
    {
        public RootKernel()
        {
            // Store Kinovea's version from the assembly.
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            PreferencesManager.ReleaseVersion = string.Format("{0}.{1}.{2}", v.Major, v.Minor, v.Build);

            // Set type of release (Experimental vs Production)
            PreferencesManager.ExperimentalRelease = true;

            // Display some system infos in the log.
            Log.Info(string.Format("Kinovea version : {0}, ({1})", PreferencesManager.ReleaseVersion,
                PreferencesManager.ExperimentalRelease ? "Experimental" : "Production"));
            Log.Info(".NET Framework Version : " + Environment.Version);
            Log.Info("OS Version : " + Environment.OSVersion);
            Log.Info("Primary Screen : " + SystemInformation.PrimaryMonitorSize);
            Log.Info("Virtual Screen : " + SystemInformation.VirtualScreen);

            // Since it is the very first call, it will both instanciate and import.
            // Previous calls were done on static prioperties, no instanciation.
            PreferencesManager.Instance();

            // Initialise command line parser and get the arguments.
            var am = CommandLineArgumentManager.Instance();
            am.InitializeCommandLineParser();
            var args = Environment.GetCommandLineArgs();
            am.ParseArguments(args);

            BuildSubTree();
            _mainWindow = new KinoveaMainWindow(this);

            Log.Debug("Plug sub modules at UI extension points (Menus, ToolBars, StatusBAr, Windows).");
            ExtendMenu(_mainWindow.menuStrip);
            ExtendToolBar(_mainWindow.toolStrip);
            ExtendStatusBar(_mainWindow.statusStrip);
            ExtendUi();

            Log.Debug("Register global services offered at Root level.");
            var dp = DelegatesPool.Instance();
            dp.UpdateStatusBar = DoUpdateStatusBar;
            dp.MakeTopMost = DoMakeTopMost;
        }

        public void BuildSubTree()
        {
            Log.Debug("Building the modules tree.");
            _mFileBrowser = new FileBrowserKernel();
            _mUpdater = new UpdaterKernel();
            ScreenManager = new ScreenManagerKernel();
            Log.Debug("Modules tree built.");
        }

        public void ExtendMenu(ToolStrip menu)
        {
            if (menu == null) throw new ArgumentNullException("menu");
            menu.AllowMerge = true;
            GetModuleMenus(menu);
            GetSubModulesMenus(menu);
        }

        public void ExtendToolBar(ToolStrip toolbar)
        {
            toolbar.AllowMerge = true;
            GetModuleToolBar(toolbar);
            GetSubModulesToolBars(toolbar);
            toolbar.Visible = true;
        }

        public void ExtendStatusBar(ToolStrip statusbar)
        {
            if (statusbar != null)
            {
                // This level
                _mStatusLabel = new ToolStripStatusLabel();
                statusbar.Items.AddRange(new ToolStripItem[] { _mStatusLabel });
            }
        }

        public void ExtendUi()
        {
            // Sub Modules
            _mFileBrowser.ExtendUi();
            _mUpdater.ExtendUi();
            ScreenManager.ExtendUi();

            // Integrate the sub modules UI into the main kernel UI.
            _mainWindow.SupervisorControl.splitWorkSpace.Panel1.Controls.Add(_mFileBrowser.UI);
            _mainWindow.SupervisorControl.splitWorkSpace.Panel2.Controls.Add(ScreenManager.Ui);

            _mainWindow.SupervisorControl.buttonCloseExplo.BringToFront();
        }

        public void RefreshUiCulture()
        {
            Log.Debug("RefreshUICulture - Reload localized strings for the whole tree.");
            RefreshCultureMenu();
            CheckLanguageMenu();
            CheckTimecodeMenu();

            var pm = PreferencesManager.Instance();
            pm.OrganizeHistoryMenu();

            var cm = CommandManager.Instance();
            cm.UpdateMenus();

            _mToolOpenFile.ToolTipText = RootLang.mnuOpenFile;

            // Sub Modules
            _mFileBrowser.RefreshUiCulture();
            _mUpdater.RefreshUiCulture();
            ScreenManager.RefreshUiCulture();

            Log.Debug("RefreshUICulture - Whole tree culture reloaded.");
        }

        public void CloseSubModules()
        {
            Log.Debug("Root is closing. Call close on all sub modules.");
            _mFileBrowser.CloseSubModules();
            _mUpdater.CloseSubModules();
            ScreenManager.CloseSubModules();
        }

        #region Properties

        public ScreenManagerKernel ScreenManager { get; private set; }

        public ToolStripMenuItem MnuToggleFileExplorer
        {
            get { return _mnuToggleFileExplorer; }
            set { _mnuToggleFileExplorer = value; }
        }

        public ToolStripMenuItem MnuView
        {
            get { return _mnuView; }
        }

        #endregion Properties

        #region Members

        private readonly KinoveaMainWindow _mainWindow;
        private FileBrowserKernel _mFileBrowser;
        private UpdaterKernel _mUpdater;

        #region Menus

        private readonly ToolStripMenuItem _mnuFile = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuOpenFile = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuHistory = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuHistoryReset = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuQuit = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuEdit = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuUndo = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuRedo = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuView = new ToolStripMenuItem();
        private ToolStripMenuItem _mnuToggleFileExplorer = new ToolStripMenuItem();
        public ToolStripMenuItem MnuFullScreen = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuImage = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMotion = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuOptions = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuLanguages = new ToolStripMenuItem();

        private readonly Dictionary<string, ToolStripMenuItem> _mLanguageMenus =
            new Dictionary<string, ToolStripMenuItem>();

        private readonly ToolStripMenuItem _mnuPreferences = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuTimecode = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuTimecodeClassic = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuTimecodeFrames = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuTimecodeMilliseconds = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuTimecodeTimeAndFrames = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuHelp = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuHelpContents = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuTutorialVideos = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuApplicationFolder = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuAbout = new ToolStripMenuItem();

        #endregion Menus

        private readonly ToolStripButton _mToolOpenFile = new ToolStripButton();
        private ToolStripStatusLabel _mStatusLabel = new ToolStripStatusLabel();

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Prepare & Launch

        public void Prepare()
        {
            // Prepare the right strings before we open the curtains.
            Log.Debug("Setting current ui culture.");
            Thread.CurrentThread.CurrentUICulture = PreferencesManager.Instance().GetSupportedCulture();
            RefreshUiCulture();
            CheckLanguageMenu();
            CheckTimecodeMenu();

            ScreenManager.Prepare();
            LogInitialConfiguration();
            if (CommandLineArgumentManager.Instance().InputFile != null)
            {
                ScreenManager.PrepareScreen();
            }
        }

        public void Launch()
        {
            Log.Debug("Calling Application.Run() to boot up the UI.");
            Application.Run(_mainWindow);
        }

        #endregion Prepare & Launch

        #region Public methods and Services

        public string LaunchOpenFileDialog()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = RootLang.dlgOpenFile_Title,
                RestoreDirectory = true,
                Filter = RootLang.dlgOpenFile_Filter,
                FilterIndex = 1
            };
            var filePath = "";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
            }
            return filePath;
        }

        public void DoUpdateStatusBar(string status)
        {
            _mStatusLabel.Text = status;
        }

        public void DoMakeTopMost(Form form)
        {
            form.Owner = _mainWindow;
        }

        #endregion Public methods and Services

        #region Extension point helpers

        private void GetModuleMenus(ToolStrip menu)
        {
            // Affectation of .Text property happens in RefreshCultureMenu

            #region File

            _mnuFile.MergeAction = MergeAction.Append;
            _mnuOpenFile.Image = Resources.folder;
            _mnuOpenFile.ShortcutKeys = Keys.Control | Keys.O;
            _mnuOpenFile.Click += MnuOpenFileOnClick;
            _mnuHistory.Image = Resources.time;

            var maxHistory = 10;
            for (var i = 0; i < maxHistory; i++)
            {
                var mnu = new ToolStripMenuItem
                {
                    MergeAction = MergeAction.Append,
                    Visible = false,
                    Tag = i
                };
                mnu.Click += mnuHistoryVideo_OnClick;
                _mnuHistory.DropDownItems.Add(mnu);
            }

            var mnuSepHistory = new ToolStripSeparator { Visible = false };
            _mnuHistory.DropDownItems.Add(mnuSepHistory);

            _mnuHistoryReset.Image = Resources.bin_empty;
            _mnuHistoryReset.MergeAction = MergeAction.Append;
            _mnuHistoryReset.Visible = false;
            _mnuHistoryReset.Click += MnuHistoryResetOnClick;
            _mnuHistory.DropDownItems.Add(_mnuHistoryReset);

            var pm = PreferencesManager.Instance();
            pm.RegisterHistoryMenu(_mnuHistory);

            _mnuQuit.Image = Resources.quit;
            _mnuQuit.Click += MenuQuitOnClick;

            _mnuFile.DropDownItems.AddRange(new ToolStripItem[]
            {
                _mnuOpenFile,
                _mnuHistory,
                new ToolStripSeparator(),
                // -> Here will be plugged the other file menus (save, export)
                new ToolStripSeparator(),
                _mnuQuit
            });

            #endregion File

            #region Edit

            _mnuEdit.MergeAction = MergeAction.Append;
            _mnuUndo.Tag = RootLang.ResourceManager;
            _mnuUndo.Image = Resources.arrow_undo;
            _mnuUndo.ShortcutKeys = Keys.Control | Keys.Z;
            _mnuUndo.Click += MenuUndoOnClick;
            _mnuUndo.Enabled = false;
            _mnuRedo.Tag = RootLang.ResourceManager;
            _mnuRedo.Image = Resources.arrow_redo;
            _mnuRedo.ShortcutKeys = Keys.Control | Keys.Y;
            _mnuRedo.Click += MenuRedoOnClick;
            _mnuRedo.Enabled = false;

            var cm = CommandManager.Instance();
            cm.RegisterUndoMenu(_mnuUndo);
            cm.RegisterRedoMenu(_mnuRedo);

            _mnuEdit.DropDownItems.AddRange(new ToolStripItem[] { _mnuUndo, _mnuRedo });

            #endregion Edit

            #region View

            _mnuToggleFileExplorer.Image = Resources.explorer;
            _mnuToggleFileExplorer.Checked = true;
            _mnuToggleFileExplorer.CheckState = CheckState.Checked;
            _mnuToggleFileExplorer.ShortcutKeys = Keys.F4;
            _mnuToggleFileExplorer.Click += MnuToggleFileExplorerOnClick;
            MnuFullScreen.Image = Resources.fullscreen;
            MnuFullScreen.ShortcutKeys = Keys.F11;
            MnuFullScreen.Click += MnuFullScreenOnClick;

            _mnuView.DropDownItems.AddRange(new ToolStripItem[] { _mnuToggleFileExplorer, MnuFullScreen, new ToolStripSeparator() });

            #endregion View

            #region Options

            _mnuLanguages.Image = Resources.international;
            foreach (var lang in LanguageManager.Languages)
            {
                var mnuLang = new ToolStripMenuItem(lang.Value) { Tag = lang.Key };
                mnuLang.Click += mnuLanguage_OnClick;
                _mLanguageMenus.Add(lang.Key, mnuLang);
                _mnuLanguages.DropDownItems.Add(mnuLang);
            }

            _mnuPreferences.Image = Resources.wrench;
            _mnuPreferences.Click += MnuPreferencesOnClick;
            _mnuTimecode.Image = Resources.time_edit;

            _mnuTimecodeClassic.Click += mnuTimecodeClassic_OnClick;
            _mnuTimecodeFrames.Click += mnuTimecodeFrames_OnClick;
            _mnuTimecodeMilliseconds.Click += mnuTimecodeMilliseconds_OnClick;
            _mnuTimecodeTimeAndFrames.Click += mnuTimecodeTimeAndFrames_OnClick;

            _mnuTimecode.DropDownItems.AddRange(new ToolStripItem[] { _mnuTimecodeClassic, _mnuTimecodeFrames, _mnuTimecodeMilliseconds, _mnuTimecodeTimeAndFrames });

            _mnuOptions.DropDownItems.AddRange(new ToolStripItem[]
            {
                _mnuLanguages,
                _mnuTimecode,
                new ToolStripSeparator(),
                _mnuPreferences
            });

            #endregion Options

            #region Help

            _mnuHelpContents.Image = Resources.book_open;
            _mnuHelpContents.ShortcutKeys = Keys.F1;
            _mnuHelpContents.Click += mnuHelpContents_OnClick;
            _mnuTutorialVideos.Image = Resources.film;
            _mnuTutorialVideos.Click += mnuTutorialVideos_OnClick;
            _mnuApplicationFolder.Image = Resources.bug;
            _mnuApplicationFolder.Click += mnuApplicationFolder_OnClick;
            _mnuAbout.Image = Resources.information;
            _mnuAbout.Click += mnuAbout_OnClick;

            _mnuHelp.DropDownItems.AddRange(new ToolStripItem[]
            {
                _mnuHelpContents,
                _mnuTutorialVideos,
                new ToolStripSeparator(),
                _mnuApplicationFolder,
                new ToolStripSeparator(),
                _mnuAbout
            });

            #endregion Help

            // Top level merge.
            var thisMenuStrip = new MenuStrip();
            thisMenuStrip.Items.AddRange(new ToolStripItem[] { _mnuFile, _mnuEdit, _mnuView, _mnuImage, _mnuMotion, _mnuOptions, _mnuHelp });
            thisMenuStrip.AllowMerge = true;

            ToolStripManager.Merge(thisMenuStrip, menu);

            // We need to affect the Text properties before merging with submenus.
            RefreshCultureMenu();
        }

        private void GetSubModulesMenus(ToolStrip menu)
        {
            _mFileBrowser.ExtendMenu(menu);
            _mUpdater.ExtendMenu(menu);
            ScreenManager.ExtendMenu(menu);
        }

        private void GetModuleToolBar(ToolStrip toolbar)
        {
            // Open.
            _mToolOpenFile.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _mToolOpenFile.Image = Resources.folder;
            _mToolOpenFile.ToolTipText = RootLang.mnuOpenFile;
            _mToolOpenFile.Click += MnuOpenFileOnClick;

            toolbar.Items.Add(_mToolOpenFile);
        }

        private void GetSubModulesToolBars(ToolStrip toolbar)
        {
            _mFileBrowser.ExtendToolBar(toolbar);
            _mUpdater.ExtendToolBar(toolbar);
            ScreenManager.ExtendToolBar(toolbar);
        }

        private void RefreshCultureMenu()
        {
            // Get the appropriate text (RootLang knows the current Culture)
            _mnuFile.Text = RootLang.mnuFile;
            _mnuOpenFile.Text = RootLang.mnuOpenFile;
            _mnuHistory.Text = RootLang.mnuHistory;
            _mnuHistoryReset.Text = RootLang.mnuHistoryReset;
            _mnuQuit.Text = RootLang.Generic_Quit;

            _mnuEdit.Text = RootLang.mnuEdit;
            _mnuUndo.Text = RootLang.mnuUndo;
            _mnuRedo.Text = RootLang.mnuRedo;

            _mnuView.Text = RootLang.mnuScreens;
            _mnuToggleFileExplorer.Text = RootLang.mnuToggleFileExplorer;
            MnuFullScreen.Text = RootLang.mnuFullScreen;

            _mnuImage.Text = RootLang.mnuImage;
            _mnuMotion.Text = RootLang.mnuMotion;

            _mnuOptions.Text = RootLang.mnuOptions;
            _mnuLanguages.Text = RootLang.mnuLanguages;
            _mnuPreferences.Text = RootLang.mnuPreferences;
            _mnuTimecode.Text = RootLang.dlgPreferences_LabelTimeFormat;
            _mnuTimecodeClassic.Text = RootLang.TimeCodeFormat_Classic;
            _mnuTimecodeFrames.Text = RootLang.TimeCodeFormat_Frames;
            _mnuTimecodeMilliseconds.Text = RootLang.TimeCodeFormat_Milliseconds;
            _mnuTimecodeTimeAndFrames.Text = RootLang.TimeCodeFormat_TimeAndFrames;

            _mnuHelp.Text = RootLang.mnuHelp;
            _mnuHelpContents.Text = RootLang.mnuHelpContents;
            _mnuTutorialVideos.Text = RootLang.mnuTutorialVideos;
            _mnuApplicationFolder.Text = RootLang.mnuApplicationFolder;
            _mnuAbout.Text = RootLang.mnuAbout;
            _mnuHelp.Text = RootLang.mnuHelp;
        }

        #endregion Extension point helpers

        #region Menus Event Handlers

        #region File

        private void MnuOpenFileOnClick(object sender, EventArgs e)
        {
            var dp = DelegatesPool.Instance();
            if (dp.StopPlaying != null)
            {
                dp.StopPlaying();
            }

            var filePath = LaunchOpenFileDialog();
            if (filePath.Length > 0)
            {
                OpenFileFromPath(filePath);
            }
        }

        private void mnuHistoryVideo_OnClick(object sender, EventArgs e)
        {
            var mnu = sender as ToolStripMenuItem;
            if (mnu != null)
            {
                var pm = PreferencesManager.Instance();
                if (mnu.Tag is int)
                    OpenFileFromPath(pm.GetFilePathAtIndex((int)mnu.Tag));
            }
        }

        private void MnuHistoryResetOnClick(object sender, EventArgs e)
        {
            var pm = PreferencesManager.Instance();
            pm.HistoryReset();
            pm.OrganizeHistoryMenu();
        }

        private void MenuQuitOnClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion File

        #region Edit

        private void MenuUndoOnClick(object sender, EventArgs e)
        {
            var cm = CommandManager.Instance();
            cm.Undo();
        }

        private void MenuRedoOnClick(object sender, EventArgs e)
        {
            var cm = CommandManager.Instance();
            cm.Redo();
        }

        #endregion Edit

        #region View

        private void MnuToggleFileExplorerOnClick(object sender, EventArgs e)
        {
            if (_mainWindow.SupervisorControl.IsExplorerCollapsed)
            {
                _mainWindow.SupervisorControl.ExpandExplorer(true);
            }
            else
            {
                _mainWindow.SupervisorControl.CollapseExplorer();
            }
        }

        private void MnuFullScreenOnClick(object sender, EventArgs e)
        {
            _mainWindow.ToggleFullScreen();

            if (_mainWindow.FullScreen)
            {
                _mainWindow.SupervisorControl.CollapseExplorer();
            }
            else
            {
                _mainWindow.SupervisorControl.ExpandExplorer(true);
            }

            // Propagates the call to screens.
            ScreenManager.FullScreen(_mainWindow.FullScreen);
        }

        #endregion View

        #region Options

        private void mnuLanguage_OnClick(object sender, EventArgs e)
        {
            var menu = sender as ToolStripMenuItem;
            if (menu != null)
            {
                var s = menu.Tag as string;
                if (s != null)
                {
                    SwitchCulture(s);
                }
            }
        }

        private void SwitchCulture(string name)
        {
            IUndoableCommand command = new CommandSwitchUiCulture(this, Thread.CurrentThread, new CultureInfo(name),
                Thread.CurrentThread.CurrentUICulture);
            var cm = CommandManager.Instance();
            cm.LaunchUndoableCommand(command);
        }

        private void CheckLanguageMenu()
        {
            foreach (var mnuLang in _mLanguageMenus.Values)
                mnuLang.Checked = false;

            var ci = PreferencesManager.Instance().GetSupportedCulture();
            var cultureName = ci.IsNeutralCulture ? ci.Name : ci.Parent.Name;

            try
            {
                _mLanguageMenus[cultureName].Checked = true;
            }
            catch (KeyNotFoundException)
            {
                _mLanguageMenus["en"].Checked = true;
            }
        }

        private void MnuPreferencesOnClick(object sender, EventArgs e)
        {
            var dp = DelegatesPool.Instance();
            if (dp.StopPlaying != null && dp.DeactivateKeyboardHandler != null)
            {
                dp.StopPlaying();
                dp.DeactivateKeyboardHandler();
            }

            var fp = new FormPreferences2(-1);
            fp.ShowDialog();
            fp.Dispose();

            if (dp.ActivateKeyboardHandler != null)
            {
                dp.ActivateKeyboardHandler();
            }

            // Refresh Preferences
            var pm = PreferencesManager.Instance();
            Log.Debug("Setting current ui culture.");
            Thread.CurrentThread.CurrentUICulture = pm.GetSupportedCulture();
            RefreshUiCulture();
        }

        private void CheckTimecodeMenu()
        {
            _mnuTimecodeClassic.Checked = false;
            _mnuTimecodeFrames.Checked = false;
            _mnuTimecodeMilliseconds.Checked = false;
            _mnuTimecodeTimeAndFrames.Checked = false;

            var tf = PreferencesManager.Instance().TimeCodeFormat;

            switch (tf)
            {
                case TimeCodeFormat.ClassicTime:
                    _mnuTimecodeClassic.Checked = true;
                    break;

                case TimeCodeFormat.Frames:
                    _mnuTimecodeFrames.Checked = true;
                    break;

                case TimeCodeFormat.Milliseconds:
                    _mnuTimecodeMilliseconds.Checked = true;
                    break;

                case TimeCodeFormat.TimeAndFrames:
                    _mnuTimecodeTimeAndFrames.Checked = true;
                    break;
            }
        }

        private void mnuTimecodeClassic_OnClick(object sender, EventArgs e)
        {
            SwitchTimecode(TimeCodeFormat.ClassicTime);
        }

        private void mnuTimecodeFrames_OnClick(object sender, EventArgs e)
        {
            SwitchTimecode(TimeCodeFormat.Frames);
        }

        private void mnuTimecodeMilliseconds_OnClick(object sender, EventArgs e)
        {
            SwitchTimecode(TimeCodeFormat.Milliseconds);
        }

        private void mnuTimecodeTimeAndFrames_OnClick(object sender, EventArgs e)
        {
            SwitchTimecode(TimeCodeFormat.TimeAndFrames);
        }

        private void SwitchTimecode(TimeCodeFormat timecode)
        {
            // Todo: turn this into a command ?
            var pm = PreferencesManager.Instance();
            pm.TimeCodeFormat = timecode;
            RefreshUiCulture();
            pm.Export();
        }

        #endregion Options

        #region Help

        private void mnuHelpContents_OnClick(object sender, EventArgs e)
        {
            // Launch Help file from current UI language.
            var resourceUri = GetLocalizedHelpResource(true);
            if (!string.IsNullOrEmpty(resourceUri) && File.Exists(resourceUri))
            {
                Help.ShowHelp(_mainWindow, resourceUri);
            }
            else
            {
                Log.Error(string.Format("Cannot find the manual. ({0}).", resourceUri));
            }
        }

        private void mnuTutorialVideos_OnClick(object sender, EventArgs e)
        {
            // Launch help video from current UI language.
            var resourceUri = GetLocalizedHelpResource(false);
            if (!string.IsNullOrEmpty(resourceUri) && File.Exists(resourceUri))
            {
                IUndoableCommand clmis = new CommandLoadMovieInScreen(ScreenManager, resourceUri, -1, true);
                var cm = CommandManager.Instance();
                cm.LaunchUndoableCommand(clmis);
            }
            else
            {
                Log.Error(string.Format("Cannot find the video tutorial file. ({0}).", resourceUri));
                MessageBox.Show(ScreenManager.ResManager.GetString("LoadMovie_FileNotOpened"),
                    ScreenManager.ResManager.GetString("LoadMovie_Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
        }

        private void mnuApplicationFolder_OnClick(object sender, EventArgs e)
        {
            // Launch Windows Explorer on App folder.
            Process.Start("explorer.exe",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Kinovea\\");
        }

        private void mnuAbout_OnClick(object sender, EventArgs e)
        {
            var fa = new FormAbout();
            fa.ShowDialog();
            fa.Dispose();
        }

        #endregion Help

        #endregion Menus Event Handlers

        #region Lower Level Methods

        private void OpenFileFromPath(string filePath)
        {
            if (File.Exists(filePath))
            {
                //--------------------------------------------------------------------------
                // CommandLoadMovieInScreen est une commande du ScreenManager.
                // elle gère la création du screen si besoin, et demande
                // si on veut charger surplace ou dans un nouveau en fonction de l'existant.
                //--------------------------------------------------------------------------
                IUndoableCommand clmis = new CommandLoadMovieInScreen(ScreenManager, filePath, -1, true);
                var cm = CommandManager.Instance();
                cm.LaunchUndoableCommand(clmis);

                //-------------------------------------------------------------
                // Get the video ready to play (normalement inutile ici, car on
                // l'a déjà fait dans le LoadMovieInScreen.
                //-------------------------------------------------------------
                ICommand css = new CommandShowScreens(ScreenManager);
                CommandManager.LaunchCommand(css);
            }
            else
            {
                MessageBox.Show(ScreenManager.ResManager.GetString("LoadMovie_FileNotOpened"),
                    ScreenManager.ResManager.GetString("LoadMovie_Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
            }
        }

        private void LogInitialConfiguration()
        {
            var am = CommandLineArgumentManager.Instance();

            Log.Debug("Initial configuration:");
            Log.Debug("InputFile : " + am.InputFile);
            Log.Debug("SpeedPercentage : " + am.SpeedPercentage);
            Log.Debug("StretchImage : " + am.StretchImage);
            Log.Debug("HideExplorer : " + am.HideExplorer);
        }

        private string GetLocalizedHelpResource(bool manual)
        {
            // Find the local file path of a help resource (manual or help video) according to what is saved in the help index.

            var resourceUri = "";

            // Load the help file system.
            var hiLocal =
                new HelpIndex(Application.StartupPath + "\\" +
                              PreferencesManager.ResourceManager.GetString("URILocalHelpIndex"));

            if (hiLocal.LoadSuccess)
            {
                // Loop into the file to find the required resource in the matching locale, or fallback to english.
                var englishUri = "";
                var bLocaleFound = false;
                var bEnglishFound = false;
                var i = 0;

                var ci = PreferencesManager.Instance().GetSupportedCulture();
                var neutral = ci.IsNeutralCulture ? ci.Name : ci.Parent.Name;

                // Look for a matching locale, or English.
                var iTotalResource = manual ? hiLocal.UserGuides.Count : hiLocal.HelpVideos.Count;
                while (i < iTotalResource)
                {
                    var hi = manual ? hiLocal.UserGuides[i] : hiLocal.HelpVideos[i];

                    if (hi.Language == neutral)
                    {
                        bLocaleFound = true;
                        resourceUri = hi.FileLocation;
                        break;
                    }

                    if (hi.Language == "en")
                    {
                        bEnglishFound = true;
                        englishUri = hi.FileLocation;
                    }

                    i++;
                }

                if (!bLocaleFound && bEnglishFound)
                {
                    resourceUri = englishUri;
                }
            }
            else
            {
                Log.Error("Cannot find the xml help index.");
            }

            return resourceUri;
        }

        #endregion Lower Level Methods
    }
}