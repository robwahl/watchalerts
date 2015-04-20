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
using Kinovea.ScreenManager.Properties;
using Kinovea.Services;
using Kinovea.VideoFiles;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public class ScreenManagerKernel : IKernel, IScreenHandler, IScreenManagerUiContainer, IMessageFilter
    {
        #region IMessageFilter Implementation

        public bool PreFilterMessage(ref Message m)
        {
            //----------------------------------------------------------------------------
            // Main keyboard handler.
            //
            // We must be careful with performances with this function.
            // As it will intercept every WM_XXX Windows message,
            // incuding WM_PAINT, WM_MOUSEMOVE, etc. from each control.
            //
            // If the function interfere with other parts of the application (because it
            // handles Return, Space, etc.) Use the DeactivateKeyboardHandler and
            // ActivateKeyboardHandler delegates from the delegate pool, to temporarily
            // bypass this handler.
            //----------------------------------------------------------------------------

            var bWasHandled = false;
            var smui = Ui as ScreenManagerUserInterface;

            if (_mBAllowKeyboardHandler && smui != null)
            {
                var bCommonControlsVisible = !smui.splitScreensPanel.Panel2Collapsed;
                var bThumbnailsViewerVisible = smui.MThumbsViewer.Visible;

                if ((m.Msg == WmKeydown) &&
                    ((ScreenList.Count > 0 && MActiveScreen != null) || (bThumbnailsViewerVisible)))
                {
                    var keyCode = (Keys)(int)m.WParam & Keys.KeyCode;

                    switch (keyCode)
                    {
                        case Keys.Delete:
                        case Keys.Add:
                        case Keys.Subtract:
                        case Keys.F2:
                        case Keys.F7:
                            {
                                //------------------------------------------------
                                // These keystrokes impact only the active screen.
                                //------------------------------------------------
                                if (!bThumbnailsViewerVisible)
                                {
                                    bWasHandled = MActiveScreen.OnKeyPress(keyCode);
                                }
                                else
                                {
                                    bWasHandled = smui.MThumbsViewer.OnKeyPress(keyCode);
                                }
                                break;
                            }
                        case Keys.Escape:
                        case Keys.F6:
                            {
                                //---------------------------------------------------
                                // These keystrokes impact each screen independently.
                                //---------------------------------------------------
                                if (!bThumbnailsViewerVisible)
                                {
                                    foreach (var abScreen in ScreenList)
                                    {
                                        bWasHandled = abScreen.OnKeyPress(keyCode);
                                    }
                                }
                                else
                                {
                                    bWasHandled = smui.MThumbsViewer.OnKeyPress(keyCode);
                                }
                                break;
                            }
                        case Keys.Down:
                        case Keys.Up:
                            {
                                //-----------------------------------------------------------------------
                                // These keystrokes impact only one screen, because it will automatically
                                // trigger the same change in the other screen.
                                //------------------------------------------------------------------------
                                if (!bThumbnailsViewerVisible)
                                {
                                    if (ScreenList.Count > 0)
                                    {
                                        bWasHandled = ScreenList[0].OnKeyPress(keyCode);
                                    }
                                }
                                else
                                {
                                    bWasHandled = smui.MThumbsViewer.OnKeyPress(keyCode);
                                }
                                break;
                            }
                        case Keys.Space:
                        case Keys.Return:
                        case Keys.Left:
                        case Keys.Right:
                        case Keys.End:
                        case Keys.Home:
                            {
                                //---------------------------------------------------
                                // These keystrokes impact both screens as a whole.
                                //---------------------------------------------------
                                if (!bThumbnailsViewerVisible)
                                {
                                    if (ScreenList.Count == 2)
                                    {
                                        if (bCommonControlsVisible)
                                        {
                                            bWasHandled = OnKeyPress(keyCode);
                                        }
                                        else
                                        {
                                            bWasHandled = MActiveScreen.OnKeyPress(keyCode);
                                        }
                                    }
                                    else if (ScreenList.Count == 1)
                                    {
                                        bWasHandled = ScreenList[0].OnKeyPress(keyCode);
                                    }
                                }
                                else
                                {
                                    bWasHandled = smui.MThumbsViewer.OnKeyPress(keyCode);
                                }
                                break;
                            }
                        //-------------------------------------------------
                        // All the remaining keystrokes impact both screen,
                        // even if the common controls aren't visible.
                        //-------------------------------------------------
                        case Keys.Tab:
                            {
                                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                                {
                                    // Change active screen.
                                    if (!bThumbnailsViewerVisible)
                                    {
                                        if (ScreenList.Count == 2)
                                        {
                                            ActivateOtherScreen();
                                            bWasHandled = true;
                                        }
                                    }
                                    else
                                    {
                                        bWasHandled = smui.MThumbsViewer.OnKeyPress(keyCode);
                                    }
                                }
                                break;
                            }
                        case Keys.F8:
                            {
                                // Go to sync frame.
                                if (!bThumbnailsViewerVisible)
                                {
                                    if (_mBSynching)
                                    {
                                        if (_mISyncLag > 0)
                                        {
                                            _mICurrentFrame = _mIRightSyncFrame;
                                        }
                                        else
                                        {
                                            _mICurrentFrame = _mILeftSyncFrame;
                                        }

                                        // Update
                                        OnCommonPositionChanged(_mICurrentFrame, true);
                                        smui.UpdateTrkFrame(_mICurrentFrame);
                                        bWasHandled = true;
                                    }
                                }
                                else
                                {
                                    bWasHandled = smui.MThumbsViewer.OnKeyPress(keyCode);
                                }
                                break;
                            }
                        case Keys.F9:
                            {
                                //---------------------------------------
                                // Fonctions associées :
                                // Resynchroniser après déplacement individuel
                                //---------------------------------------
                                if (!bThumbnailsViewerVisible)
                                {
                                    if (_mBSynching)
                                    {
                                        SyncCatch();
                                        bWasHandled = true;
                                    }
                                }
                                else
                                {
                                    bWasHandled = smui.MThumbsViewer.OnKeyPress(keyCode);
                                }
                                break;
                            }
                        default:
                            break;
                    }
                }
            }

            return bWasHandled;
        }

        #endregion IMessageFilter Implementation

        private enum SyncStep
        {
            Initial,
            StartingWait,
            BothPlaying,
            EndingWait
        }

        #region Properties

        public UserControl Ui { get; set; }

        public ResourceManager ResManager
        {
            get
            {
                return new ResourceManager("Kinovea.ScreenManager.Languages.ScreenManagerLang",
                    Assembly.GetExecutingAssembly());
            }
        }

        public bool CancelLastCommand
        {
            get;
            // Unused.
            set;
        }

        #endregion Properties

        #region Members

        //List of screens ( 0..n )
        public List<AbstractScreen> ScreenList = new List<AbstractScreen>();

        public AbstractScreen MActiveScreen;
        private bool _mBCanShowCommonControls;

        // Dual saving
        private string _mDualSaveFileName;

        private bool _mBDualSaveCancelled;
        private bool _mBDualSaveInProgress;
        private readonly VideoFileWriter _mVideoFileWriter = new VideoFileWriter();
        private BackgroundWorker _mBgWorkerDualSave;
        private FormProgressBar _mDualSaveProgressBar;

        // Video Filters
        private AbstractVideoFilter[] _mVideoFilters;

        private bool _mBHasSvgFiles;
        private readonly string _mSvgPath;
        private readonly FileSystemWatcher _mSvgFilesWatcher = new FileSystemWatcher();
        private readonly MethodInvoker _mSvgFilesChangedInvoker;
        private bool _mBuildingSvgMenu;

        #region Menus

        private readonly ToolStripMenuItem _mnuCloseFile = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuCloseFile2 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuSave = new ToolStripMenuItem();

        private readonly ToolStripMenuItem _mnuExportSpreadsheet = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuExportOdf = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuExportMsxml = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuExportXhtml = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuExportText = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuLoadAnalysis = new ToolStripMenuItem();

        private readonly ToolStripMenuItem _mnuOnePlayer = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuTwoPlayers = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuOneCapture = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuTwoCaptures = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuTwoMixed = new ToolStripMenuItem();

        private readonly ToolStripMenuItem _mnuSwapScreens = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuToggleCommonCtrls = new ToolStripMenuItem();

        private readonly ToolStripMenuItem _mnuDeinterlace = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuFormat = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuFormatAuto = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuFormatForce43 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuFormatForce169 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMirror = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuSvgTools = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuImportImage = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuCoordinateAxis = new ToolStripMenuItem();

        private readonly ToolStripMenuItem _mnuHighspeedCamera = new ToolStripMenuItem();

        #endregion Menus

        #region Toolbar

        private readonly ToolStripButton _toolHome = new ToolStripButton();
        private readonly ToolStripButton _toolSave = new ToolStripButton();
        private readonly ToolStripButton _toolOnePlayer = new ToolStripButton();
        private readonly ToolStripButton _toolTwoPlayers = new ToolStripButton();
        private readonly ToolStripButton _toolOneCapture = new ToolStripButton();
        private readonly ToolStripButton _toolTwoCaptures = new ToolStripButton();
        private readonly ToolStripButton _toolTwoMixed = new ToolStripButton();

        #endregion Toolbar

        #region Synchronization

        private bool _mBSynching;
        private bool _mBSyncMerging; // true if blending each other videos.
        private int _mISyncLag; // Sync Lag in Frames, for static sync.
        private int _mISyncLagMilliseconds; // Sync lag in Milliseconds, for dynamic sync.
        private bool _mBDynamicSynching; // replace the common timer.

        // Static Sync Positions
        private int _mICurrentFrame; // Current frame in trkFrame...

        private int _mILeftSyncFrame; // Sync reference in the left video
        private int _mIRightSyncFrame; // Sync reference in the right video
        private int _mIMaxFrame; // Max du trkFrame

        // Dynamic Sync Flags.
        private bool _mBRightIsStarting; // true when the video is between [0] and [1] frames.

        private bool _mBLeftIsStarting;
        private bool _mBLeftIsCatchingUp; // CatchingUp is when the video is the only one left running,
        private bool _mBRightIsCatchingUp; // heading towards end, the other video is waiting the lag.

        #endregion Synchronization

        private bool _mBAllowKeyboardHandler;

        private readonly List<ScreenManagerState> _mStoredStates = new List<ScreenManagerState>();
        private const int WmKeydown = 0x100;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor & initialization

        public ScreenManagerKernel()
        {
            Log.Debug("Module Construction : ScreenManager.");

            _mBAllowKeyboardHandler = true;

            Ui = new ScreenManagerUserInterface(this);

            InitializeVideoFilters();

            // Registers our exposed functions to the DelegatePool.
            var dp = DelegatesPool.Instance();

            dp.LoadMovieInScreen = DoLoadMovieInScreen;
            dp.StopPlaying = DoStopPlaying;
            dp.DeactivateKeyboardHandler = DoDeactivateKeyboardHandler;
            dp.ActivateKeyboardHandler = DoActivateKeyboardHandler;
            dp.VideoProcessingDone = DoVideoProcessingDone;

            // Watch for changes in the guides directory.
            _mSvgPath = Path.GetDirectoryName(Application.ExecutablePath) + "\\guides\\";
            _mSvgFilesWatcher.Path = _mSvgPath;
            _mSvgFilesWatcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName |
                                             NotifyFilters.LastWrite;
            _mSvgFilesWatcher.Filter = "*.svg";
            _mSvgFilesWatcher.IncludeSubdirectories = true;
            _mSvgFilesWatcher.Changed += OnSvgFilesChanged;
            _mSvgFilesWatcher.Created += OnSvgFilesChanged;
            _mSvgFilesWatcher.Deleted += OnSvgFilesChanged;
            _mSvgFilesWatcher.Renamed += OnSvgFilesChanged;

            _mSvgFilesChangedInvoker = DoSvgFilesChanged;

            _mSvgFilesWatcher.EnableRaisingEvents = true;
        }

        private void InitializeVideoFilters()
        {
            // Creates Video Filters
            _mVideoFilters = new AbstractVideoFilter[(int)VideoFilterType.NumberOfVideoFilters];

            _mVideoFilters[(int)VideoFilterType.AutoLevels] = new VideoFilterAutoLevels();
            _mVideoFilters[(int)VideoFilterType.AutoContrast] = new VideoFilterContrast();
            _mVideoFilters[(int)VideoFilterType.Sharpen] = new VideoFilterSharpen();
            _mVideoFilters[(int)VideoFilterType.EdgesOnly] = new VideoFilterEdgesOnly();
            _mVideoFilters[(int)VideoFilterType.Mosaic] = new VideoFilterMosaic();
            _mVideoFilters[(int)VideoFilterType.Reverse] = new VideoFilterReverse();
            _mVideoFilters[(int)VideoFilterType.Sandbox] = new VideoFilterSandbox();
        }

        public void PrepareScreen()
        {
            // Prepare a screen to hold the command line argument file.
            IUndoableCommand caps = new CommandAddPlayerScreen(this, true);
            CommandManager.Instance().LaunchUndoableCommand(caps);

            ICommand css = new CommandShowScreens(this);
            CommandManager.LaunchCommand(css);

            OrganizeCommonControls();
            OrganizeMenus();
        }

        public void Prepare()
        {
            Application.AddMessageFilter(this);
        }

        #endregion Constructor & initialization

        #region IKernel Implementation

        public void BuildSubTree()
        {
            // No sub modules.
        }

        public void ExtendMenu(ToolStrip menu)
        {
            #region File

            var mnuCatchFile = new ToolStripMenuItem();
            mnuCatchFile.MergeIndex = 0; // (File)
            mnuCatchFile.MergeAction = MergeAction.MatchOnly;

            _mnuCloseFile.Image = Resources.film_close3;
            _mnuCloseFile.Enabled = false;
            _mnuCloseFile.Click += MnuCloseFileOnClick;
            _mnuCloseFile.MergeIndex = 2;
            _mnuCloseFile.MergeAction = MergeAction.Insert;

            _mnuCloseFile2.Image = Resources.film_close3;
            _mnuCloseFile2.Enabled = false;
            _mnuCloseFile2.Visible = false;
            _mnuCloseFile2.Click += MnuCloseFile2OnClick;
            _mnuCloseFile2.MergeIndex = 3;
            _mnuCloseFile2.MergeAction = MergeAction.Insert;

            _mnuSave.Image = Resources.filesave;
            _mnuSave.Click += MnuSaveOnClick;
            _mnuSave.ShortcutKeys = Keys.Control | Keys.S;
            _mnuSave.MergeIndex = 5;
            _mnuSave.MergeAction = MergeAction.Insert;

            _mnuExportSpreadsheet.Image = Resources.table;
            _mnuExportSpreadsheet.MergeIndex = 6;
            _mnuExportSpreadsheet.MergeAction = MergeAction.Insert;
            _mnuExportOdf.Image = Resources.file_ods;
            _mnuExportOdf.Click += mnuExportODF_OnClick;
            _mnuExportMsxml.Image = Resources.file_xls;
            _mnuExportMsxml.Click += mnuExportMSXML_OnClick;
            _mnuExportXhtml.Image = Resources.file_html;
            _mnuExportXhtml.Click += mnuExportXHTML_OnClick;
            _mnuExportText.Image = Resources.file_txt;
            _mnuExportText.Click += mnuExportTEXT_OnClick;

            _mnuExportSpreadsheet.DropDownItems.AddRange(new ToolStripItem[] { _mnuExportOdf, _mnuExportMsxml, _mnuExportXhtml, _mnuExportText });

            // Load Analysis
            _mnuLoadAnalysis.Image = Resources.file_kva2;
            _mnuLoadAnalysis.Click += MnuLoadAnalysisOnClick;
            _mnuLoadAnalysis.MergeIndex = 7;
            _mnuLoadAnalysis.MergeAction = MergeAction.Insert;

            ToolStripItem[] subFile = { _mnuCloseFile, _mnuCloseFile2, _mnuSave, _mnuExportSpreadsheet, _mnuLoadAnalysis };
            mnuCatchFile.DropDownItems.AddRange(subFile);

            #endregion File

            #region View

            var mnuCatchScreens = new ToolStripMenuItem();
            mnuCatchScreens.MergeIndex = 2; // (Screens)
            mnuCatchScreens.MergeAction = MergeAction.MatchOnly;

            _mnuOnePlayer.Image = Resources.television;
            _mnuOnePlayer.Click += MnuOnePlayerOnClick;
            _mnuOnePlayer.MergeAction = MergeAction.Append;
            _mnuTwoPlayers.Image = Resources.dualplayback;
            _mnuTwoPlayers.Click += MnuTwoPlayersOnClick;
            _mnuTwoPlayers.MergeAction = MergeAction.Append;
            _mnuOneCapture.Image = Resources.camera_video;
            _mnuOneCapture.Click += MnuOneCaptureOnClick;
            _mnuOneCapture.MergeAction = MergeAction.Append;
            _mnuTwoCaptures.Image = Resources.dualcapture2;
            _mnuTwoCaptures.Click += MnuTwoCapturesOnClick;
            _mnuTwoCaptures.MergeAction = MergeAction.Append;
            _mnuTwoMixed.Image = Resources.dualmixed3;
            _mnuTwoMixed.Click += MnuTwoMixedOnClick;
            _mnuTwoMixed.MergeAction = MergeAction.Append;

            _mnuSwapScreens.Image = Resources.arrow_swap;
            _mnuSwapScreens.Enabled = false;
            _mnuSwapScreens.Click += MnuSwapScreensOnClick;
            _mnuSwapScreens.MergeAction = MergeAction.Append;

            _mnuToggleCommonCtrls.Image = Resources.common_controls;
            _mnuToggleCommonCtrls.Enabled = false;
            _mnuToggleCommonCtrls.ShortcutKeys = Keys.F5;
            _mnuToggleCommonCtrls.Click += MnuToggleCommonCtrlsOnClick;
            _mnuToggleCommonCtrls.MergeAction = MergeAction.Append;

            ToolStripItem[] subScreens =
            {
                _mnuOnePlayer,
                _mnuTwoPlayers,
                new ToolStripSeparator(),
                _mnuOneCapture,
                _mnuTwoCaptures,
                new ToolStripSeparator(),
                _mnuTwoMixed,
                new ToolStripSeparator(),
                _mnuSwapScreens,
                _mnuToggleCommonCtrls
            };
            mnuCatchScreens.DropDownItems.AddRange(subScreens);

            #endregion View

            #region Image

            var mnuCatchImage = new ToolStripMenuItem();
            mnuCatchImage.MergeIndex = 3; // (Image)
            mnuCatchImage.MergeAction = MergeAction.MatchOnly;

            _mnuDeinterlace.Image = Resources.deinterlace;
            _mnuDeinterlace.Checked = false;
            _mnuDeinterlace.ShortcutKeys = Keys.Control | Keys.D;
            _mnuDeinterlace.Click += MnuDeinterlaceOnClick;
            _mnuDeinterlace.MergeAction = MergeAction.Append;

            _mnuFormatAuto.Checked = true;
            _mnuFormatAuto.Click += MnuFormatAutoOnClick;
            _mnuFormatAuto.MergeAction = MergeAction.Append;
            _mnuFormatForce43.Image = Resources.format43;
            _mnuFormatForce43.Click += MnuFormatForce43OnClick;
            _mnuFormatForce43.MergeAction = MergeAction.Append;
            _mnuFormatForce169.Image = Resources.format169;
            _mnuFormatForce169.Click += MnuFormatForce169OnClick;
            _mnuFormatForce169.MergeAction = MergeAction.Append;
            _mnuFormat.Image = Resources.shape_formats;
            _mnuFormat.MergeAction = MergeAction.Append;
            _mnuFormat.DropDownItems.AddRange(new ToolStripItem[] { _mnuFormatAuto, new ToolStripSeparator(), _mnuFormatForce43, _mnuFormatForce169 });

            _mnuMirror.Image = Resources.shape_mirror;
            _mnuMirror.Checked = false;
            _mnuMirror.ShortcutKeys = Keys.Control | Keys.M;
            _mnuMirror.Click += MnuMirrorOnClick;
            _mnuMirror.MergeAction = MergeAction.Append;

            BuildSvgMenu();

            _mnuCoordinateAxis.Image = Resources.coordinate_axis;
            _mnuCoordinateAxis.Click += mnuCoordinateAxis_OnClick;
            _mnuCoordinateAxis.MergeAction = MergeAction.Append;

            ConfigureVideoFilterMenus(null);

            mnuCatchImage.DropDownItems.AddRange(new ToolStripItem[]
            {
                _mnuDeinterlace,
                _mnuFormat,
                _mnuMirror,
                new ToolStripSeparator(),
                _mVideoFilters[(int) VideoFilterType.AutoLevels].Menu,
                _mVideoFilters[(int) VideoFilterType.AutoContrast].Menu,
                _mVideoFilters[(int) VideoFilterType.Sharpen].Menu,
                new ToolStripSeparator(),
                _mnuSvgTools,
                _mnuCoordinateAxis
            });

            #endregion Image

            #region Motion

            var mnuCatchMotion = new ToolStripMenuItem();
            mnuCatchMotion.MergeIndex = 4;
            mnuCatchMotion.MergeAction = MergeAction.MatchOnly;

            _mnuHighspeedCamera.Image = Resources.camera_speed;
            _mnuHighspeedCamera.Click += mnuHighspeedCamera_OnClick;
            _mnuHighspeedCamera.MergeAction = MergeAction.Append;

            mnuCatchMotion.DropDownItems.AddRange(new ToolStripItem[]
            {
                _mnuHighspeedCamera,
                new ToolStripSeparator(),
                _mVideoFilters[(int) VideoFilterType.Mosaic].Menu,
                _mVideoFilters[(int) VideoFilterType.Reverse].Menu,
                _mVideoFilters[(int) VideoFilterType.Sandbox].Menu
            });

            #endregion Motion

            var thisMenu = new MenuStrip();
            thisMenu.Items.AddRange(new ToolStripItem[] { mnuCatchFile, mnuCatchScreens, mnuCatchImage, mnuCatchMotion });
            thisMenu.AllowMerge = true;

            ToolStripManager.Merge(thisMenu, menu);

            RefreshCultureMenu();
        }

        public void ExtendToolBar(ToolStrip toolbar)
        {
            // Save
            _toolSave.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _toolSave.Image = Resources.filesave;
            _toolSave.Click += MnuSaveOnClick;

            // Workspace presets.

            _toolHome.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _toolHome.Image = Resources.home3;
            _toolHome.Click += mnuHome_OnClick;

            _toolOnePlayer.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _toolOnePlayer.Image = Resources.television;
            _toolOnePlayer.Click += MnuOnePlayerOnClick;

            _toolTwoPlayers.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _toolTwoPlayers.Image = Resources.dualplayback;
            _toolTwoPlayers.Click += MnuTwoPlayersOnClick;

            _toolOneCapture.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _toolOneCapture.Image = Resources.camera_video;
            _toolOneCapture.Click += MnuOneCaptureOnClick;

            _toolTwoCaptures.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _toolTwoCaptures.Image = Resources.dualcapture2;
            _toolTwoCaptures.Click += MnuTwoCapturesOnClick;

            _toolTwoMixed.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _toolTwoMixed.Image = Resources.dualmixed3;
            _toolTwoMixed.Click += MnuTwoMixedOnClick;

            var ts = new ToolStrip(_toolSave, new ToolStripSeparator(), _toolHome, new ToolStripSeparator(),
                _toolOnePlayer, _toolTwoPlayers, new ToolStripSeparator(), _toolOneCapture, _toolTwoCaptures,
                new ToolStripSeparator(), _toolTwoMixed);

            ToolStripManager.Merge(ts, toolbar);
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
            RefreshCultureMenu();
            OrganizeMenus();
            RefreshCultureToolbar();
            UpdateStatusBar();

            ((ScreenManagerUserInterface)Ui).RefreshUiCulture();

            foreach (var screen in ScreenList)
                screen.RefreshUiCulture();

            ((ScreenManagerUserInterface)Ui).DisplaySyncLag(_mISyncLag);
        }

        public void CloseSubModules()
        {
            foreach (var screen in ScreenList)
                screen.BeforeClose();
        }

        #endregion IKernel Implementation

        #region IScreenHandler Implementation

        public void Screen_SetActiveScreen(AbstractScreen activeScreen)
        {
            //-------------------------------------------------------------
            // /!\ Calls in OrganizeMenu which is a bit heavy on the UI.
            // Screen_SetActiveScreen should only be called when necessary.
            //-------------------------------------------------------------

            if (MActiveScreen != activeScreen)
            {
                MActiveScreen = activeScreen;

                if (ScreenList.Count > 1)
                {
                    MActiveScreen.DisplayAsActiveScreen(true);

                    // Make other screens inactive.
                    foreach (var screen in ScreenList)
                    {
                        if (screen != activeScreen)
                        {
                            screen.DisplayAsActiveScreen(false);
                        }
                    }
                }
            }

            OrganizeMenus();
        }

        public void Screen_CloseAsked(AbstractScreen senderScreen)
        {
            // If the screen is in Drawtime filter (e.g: Mosaic), we just go back to normal play.
            if (senderScreen is PlayerScreen)
            {
                int iDrawtimeFilterType = ((PlayerScreen)senderScreen).DrawtimeFilterType;
                if (iDrawtimeFilterType > -1)
                {
                    // We need to make sure this is the active screen for the DrawingFilter menu action to be properly routed.
                    Screen_SetActiveScreen(senderScreen);
                    _mVideoFilters[iDrawtimeFilterType].Menu_OnClick(this, EventArgs.Empty);
                    return;
                }
            }

            senderScreen.BeforeClose();

            // Reorganise screens.
            // We leverage the fact that screens are always well ordered relative to menus.
            if (ScreenList.Count > 0 && senderScreen == ScreenList[0])
            {
                MnuCloseFileOnClick(null, EventArgs.Empty);
            }
            else
            {
                MnuCloseFile2OnClick(null, EventArgs.Empty);
            }

            UpdateCaptureBuffers();
            PrepareSync(false);
        }

        public void Screen_UpdateStatusBarAsked(AbstractScreen senderScreen)
        {
            UpdateStatusBar();
        }

        public void Player_SpeedChanged(PlayerScreen screen, bool bInitialisation)
        {
            if (_mBSynching)
            {
                Log.Debug("Speed percentage of one video changed. Force same percentage on the other.");
                if (ScreenList.Count == 2)
                {
                    var otherScreen = 1;
                    if (screen == ScreenList[1])
                    {
                        otherScreen = 0;
                    }

                    ((PlayerScreen)ScreenList[otherScreen]).RealtimePercentage = screen.RealtimePercentage;

                    SetSyncPoint(true);
                }
            }
        }

        public void Player_PauseAsked(PlayerScreen screen)
        {
            // An individual player asks for a global pause.
            if (_mBSynching && ((ScreenManagerUserInterface)Ui).ComCtrls.Playing)
            {
                ((ScreenManagerUserInterface)Ui).ComCtrls.Playing = false;
                CommonCtrl_Play();
            }
        }

        public void Player_SelectionChanged(PlayerScreen screen, bool bInitialization)
        {
            PrepareSync(bInitialization);
        }

        public void Player_ImageChanged(PlayerScreen _screen, Bitmap image)
        {
            if (_mBSynching)
            {
                // Transfer the image to the other screen.
                if (_mBSyncMerging)
                {
                    foreach (var screen in ScreenList)
                    {
                        if (screen != _screen && screen is PlayerScreen)
                        {
                            // The image has been cloned and transformed in the caller screen.
                            ((PlayerScreen)screen).SetSyncMergeImage(image, !_mBDualSaveInProgress);
                        }
                    }
                }

                // Dynamic sync.
                if (_mBDynamicSynching)
                {
                    DynamicSync();
                }
            }
        }

        public void Player_SendImage(PlayerScreen screen, Bitmap image)
        {
            // An image was sent from a screen to be added as an observational reference in the other screen.
            for (var i = 0; i < ScreenList.Count; i++)
            {
                if (ScreenList[i] != screen && ScreenList[i] is PlayerScreen)
                {
                    // The image has been cloned and transformed in the caller screen.
                    ScreenList[i].AddImageDrawing(image);
                }
            }
        }

        public void Player_Reset(PlayerScreen screen)
        {
            // A screen was reset. (ex: a video was reloded in place).
            // We need to also reset all the sync states.
            PrepareSync(true);
        }

        public void Capture_FileSaved(CaptureScreen screen)
        {
            // A file was saved in one screen, we need to update the text on the other.
            for (var i = 0; i < ScreenList.Count; i++)
            {
                if (ScreenList[i] != screen && ScreenList[i] is CaptureScreen)
                {
                    ScreenList[i].RefreshUiCulture();
                }
            }
        }

        public void Capture_LoadVideo(CaptureScreen screen, string filepath)
        {
            // Launch a video in the other screen.

            if (ScreenList.Count == 1)
            {
                // Create the screen if necessary.
                // The buffer of the capture screen will be reset during the operation.
                DoLoadMovieInScreen(filepath, -1, true);
            }
            else if (ScreenList.Count == 2)
            {
                // Identify the other screen.
                AbstractScreen otherScreen = null;
                var iOtherScreenIndex = 0;
                for (var i = 0; i < ScreenList.Count; i++)
                {
                    if (ScreenList[i] != screen)
                    {
                        otherScreen = ScreenList[i];
                        iOtherScreenIndex = i + 1;
                    }
                }

                if (otherScreen is CaptureScreen)
                {
                    // Unload capture screen to play the video ?
                }
                else if (otherScreen is PlayerScreen)
                {
                    // Replace the video.
                    DoLoadMovieInScreen(filepath, iOtherScreenIndex, true);
                }
            }
        }

        #endregion IScreenHandler Implementation

        #region ICommonControlsHandler Implementation

        public void DropLoadMovie(string filePath, int iScreen)
        {
            // End of drag and drop between FileManager and ScreenManager
            DoLoadMovieInScreen(filePath, iScreen, true);
        }

        public DragDropEffects GetDragDropEffects(int screen)
        {
            var effects = DragDropEffects.All;

            // If the screen we are dragging over is a capture screen, we can't drop.
            if (screen >= 0 && ScreenList.Count >= screen && ScreenList[screen] is CaptureScreen)
            {
                effects = DragDropEffects.None;
            }

            return effects;
        }

        public void CommonCtrl_GotoFirst()
        {
            DoStopPlaying();

            if (_mBSynching)
            {
                _mICurrentFrame = 0;
                OnCommonPositionChanged(_mICurrentFrame, true);
                ((ScreenManagerUserInterface)Ui).UpdateTrkFrame(_mICurrentFrame);
            }
            else
            {
                // Ask global GotoFirst.
                foreach (var screen in ScreenList)
                {
                    if (screen is PlayerScreen)
                    {
                        ((PlayerScreen)screen).MPlayerScreenUi.buttonGotoFirst_Click(null, EventArgs.Empty);
                    }
                }
            }
        }

        public void CommonCtrl_GotoPrev()
        {
            DoStopPlaying();

            if (_mBSynching)
            {
                if (_mICurrentFrame > 0)
                {
                    _mICurrentFrame--;
                    OnCommonPositionChanged(_mICurrentFrame, true);
                    ((ScreenManagerUserInterface)Ui).UpdateTrkFrame(_mICurrentFrame);
                }
            }
            else
            {
                // Ask global GotoPrev.
                foreach (var screen in ScreenList)
                {
                    if (screen.GetType().FullName.Equals("Kinovea.ScreenManager.PlayerScreen"))
                    {
                        ((PlayerScreen)screen).MPlayerScreenUi.buttonGotoPrevious_Click(null, EventArgs.Empty);
                    }
                }
            }
        }

        public void CommonCtrl_GotoNext()
        {
            DoStopPlaying();

            if (_mBSynching)
            {
                if (_mICurrentFrame < _mIMaxFrame)
                {
                    _mICurrentFrame++;
                    OnCommonPositionChanged(-1, true);
                    ((ScreenManagerUserInterface)Ui).UpdateTrkFrame(_mICurrentFrame);
                }
            }
            else
            {
                // Ask global GotoNext.
                foreach (var screen in ScreenList)
                {
                    if (screen.GetType().FullName.Equals("Kinovea.ScreenManager.PlayerScreen"))
                    {
                        ((PlayerScreen)screen).MPlayerScreenUi.buttonGotoNext_Click(null, EventArgs.Empty);
                    }
                }
            }
        }

        public void CommonCtrl_GotoLast()
        {
            DoStopPlaying();

            if (_mBSynching)
            {
                _mICurrentFrame = _mIMaxFrame;
                OnCommonPositionChanged(_mICurrentFrame, true);
                ((ScreenManagerUserInterface)Ui).UpdateTrkFrame(_mICurrentFrame);
            }
            else
            {
                // Demander un GotoLast à tout le monde
                foreach (var screen in ScreenList)
                {
                    if (screen is PlayerScreen)
                    {
                        ((PlayerScreen)screen).MPlayerScreenUi.buttonGotoLast_Click(null, EventArgs.Empty);
                    }
                }
            }
        }

        public void CommonCtrl_Play()
        {
            var bPlaying = ((ScreenManagerUserInterface)Ui).ComCtrls.Playing;
            if (_mBSynching)
            {
                if (bPlaying)
                {
                    // On play, simply launch the dynamic sync.
                    // It will handle which video can start right away.
                    StartDynamicSync();
                }
                else
                {
                    StopDynamicSync();
                    _mBLeftIsStarting = false;
                    _mBRightIsStarting = false;
                }
            }

            // On stop, propagate the call to screens.
            if (!bPlaying)
            {
                if (ScreenList[0] is PlayerScreen)
                    EnsurePause(0);

                if (ScreenList[1] is PlayerScreen)
                    EnsurePause(1);
            }
        }

        public void CommonCtrl_Swap()
        {
            MnuSwapScreensOnClick(null, EventArgs.Empty);
        }

        public void CommonCtrl_Sync()
        {
            if (_mBSynching && ScreenList.Count == 2)
            {
                // Mise à jour : m_iLeftSyncFrame, m_iRightSyncFrame, m_iSyncLag, m_iCurrentFrame. m_iMaxFrame.
                Log.Debug("Sync point change.");
                SetSyncPoint(false);
                SetSyncLimits();

                // Mise à jour du trkFrame.
                ((ScreenManagerUserInterface)Ui).SetupTrkFrame(0, _mIMaxFrame, _mICurrentFrame);

                // Mise à jour des Players.
                OnCommonPositionChanged(_mICurrentFrame, true);

                // debug
                ((ScreenManagerUserInterface)Ui).DisplaySyncLag(_mISyncLag);
            }
        }

        public void CommonCtrl_Merge()
        {
            if (_mBSynching && ScreenList.Count == 2)
            {
                _mBSyncMerging = ((ScreenManagerUserInterface)Ui).ComCtrls.SyncMerging;
                Log.Debug(string.Format("SyncMerge videos is now {0}", _mBSyncMerging));

                // This will also do a full refresh, and triggers Player_ImageChanged().
                ((PlayerScreen)ScreenList[0]).SyncMerge = _mBSyncMerging;
                ((PlayerScreen)ScreenList[1]).SyncMerge = _mBSyncMerging;
            }
        }

        public void CommonCtrl_PositionChanged(long iPosition)
        {
            // Manual static sync.
            if (_mBSynching)
            {
                StopDynamicSync();

                EnsurePause(0);
                EnsurePause(1);

                ((ScreenManagerUserInterface)Ui).DisplayAsPaused();

                _mICurrentFrame = (int)iPosition;
                OnCommonPositionChanged(_mICurrentFrame, true);
            }
        }

        public void CommonCtrl_Snapshot()
        {
            // Retrieve current images and create a composite out of them.
            if (_mBSynching && ScreenList.Count == 2)
            {
                PlayerScreen ps1 = ScreenList[0] as PlayerScreen;
                PlayerScreen ps2 = ScreenList[1] as PlayerScreen;
                if (ps1 != null && ps2 != null)
                {
                    DoStopPlaying();

                    // get a copy of the images with drawings flushed on.
                    Bitmap leftImage = ps1.GetFlushedImage();
                    Bitmap rightImage = ps2.GetFlushedImage();
                    var composite = ImageHelper.GetSideBySideComposite(leftImage, rightImage, false, true);

                    // Configure Save dialog.
                    var dlgSave = new SaveFileDialog();
                    dlgSave.Title = ScreenManagerLang.Generic_SaveImage;
                    dlgSave.RestoreDirectory = true;
                    dlgSave.Filter = ScreenManagerLang.dlgSaveFilter;
                    dlgSave.FilterIndex = 1;
                    dlgSave.FileName = string.Format("{0} - {1}", Path.GetFileNameWithoutExtension(ps1.FilePath),
                        Path.GetFileNameWithoutExtension(ps2.FilePath));

                    // Launch the dialog and save image.
                    if (dlgSave.ShowDialog() == DialogResult.OK)
                    {
                        ImageHelper.Save(dlgSave.FileName, composite);
                    }

                    composite.Dispose();
                    leftImage.Dispose();
                    rightImage.Dispose();

                    var dp = DelegatesPool.Instance();
                    if (dp.RefreshFileExplorer != null) dp.RefreshFileExplorer(false);
                }
            }
        }

        public void CommonCtrl_DualVideo()
        {
            // Create and save a composite video with side by side synchronized images.
            // If merge is active, just save one video.

            if (_mBSynching && ScreenList.Count == 2)
            {
                PlayerScreen ps1 = ScreenList[0] as PlayerScreen;
                PlayerScreen ps2 = ScreenList[1] as PlayerScreen;
                if (ps1 != null && ps2 != null)
                {
                    DoStopPlaying();

                    // Get file name from user.
                    var dlgSave = new SaveFileDialog();
                    dlgSave.Title = ScreenManagerLang.dlgSaveVideoTitle;
                    dlgSave.RestoreDirectory = true;
                    dlgSave.Filter = ScreenManagerLang.dlgSaveVideoFilterAlone;
                    dlgSave.FilterIndex = 1;
                    dlgSave.FileName = string.Format("{0} - {1}", Path.GetFileNameWithoutExtension(ps1.FilePath),
                        Path.GetFileNameWithoutExtension(ps2.FilePath));

                    if (dlgSave.ShowDialog() == DialogResult.OK)
                    {
                        var iCurrentFrame = _mICurrentFrame;
                        _mBDualSaveCancelled = false;
                        _mDualSaveFileName = dlgSave.FileName;

                        // Instanciate and configure the bgWorker.
                        _mBgWorkerDualSave = new BackgroundWorker();
                        _mBgWorkerDualSave.WorkerReportsProgress = true;
                        _mBgWorkerDualSave.WorkerSupportsCancellation = true;
                        _mBgWorkerDualSave.DoWork += bgWorkerDualSave_DoWork;
                        _mBgWorkerDualSave.ProgressChanged += bgWorkerDualSave_ProgressChanged;
                        _mBgWorkerDualSave.RunWorkerCompleted += bgWorkerDualSave_RunWorkerCompleted;

                        // Make sure none of the screen will try to update itself.
                        // Otherwise it will cause access to the other screen image (in case of merge), which can cause a crash.
                        _mBDualSaveInProgress = true;
                        ps1.DualSaveInProgress = true;
                        ps2.DualSaveInProgress = true;

                        // Create the progress bar and launch the worker.
                        _mDualSaveProgressBar = new FormProgressBar(true);
                        _mDualSaveProgressBar.Cancel = dualSave_CancelAsked;
                        _mBgWorkerDualSave.RunWorkerAsync();
                        _mDualSaveProgressBar.ShowDialog();

                        // If cancelled, delete temporary file.
                        if (_mBDualSaveCancelled)
                        {
                            DeleteTemporaryFile(_mDualSaveFileName);
                        }

                        // Reset to where we were.
                        _mBDualSaveInProgress = false;
                        ps1.DualSaveInProgress = false;
                        ps2.DualSaveInProgress = false;
                        _mICurrentFrame = iCurrentFrame;
                        OnCommonPositionChanged(_mICurrentFrame, true);
                    }
                }
            }
        }

        #endregion ICommonControlsHandler Implementation

        #region Public Methods

        public void UpdateStatusBar()
        {
            //------------------------------------------------------------------
            // Function called on RefreshUiCulture, CommandShowScreen...
            // and calling upper module (supervisor).
            //------------------------------------------------------------------

            var statusString = "";

            switch (ScreenList.Count)
            {
                case 1:
                    statusString = ScreenList[0].Status;
                    break;

                case 2:
                    statusString = ScreenList[0].Status + " | " + ScreenList[1].Status;
                    break;

                default:
                    break;
            }

            var dp = DelegatesPool.Instance();
            if (dp.UpdateStatusBar != null)
            {
                dp.UpdateStatusBar(statusString);
            }
        }

        public void OrganizeCommonControls()
        {
            _mBCanShowCommonControls = false;

            switch (ScreenList.Count)
            {
                case 0:
                case 1:
                default:
                    ((ScreenManagerUserInterface)Ui).splitScreensPanel.Panel2Collapsed = true;
                    break;

                case 2:
                    if (ScreenList[0] is PlayerScreen && ScreenList[1] is PlayerScreen)
                    {
                        ((ScreenManagerUserInterface)Ui).splitScreensPanel.Panel2Collapsed = false;
                        _mBCanShowCommonControls = true;
                    }
                    else
                    {
                        ((ScreenManagerUserInterface)Ui).splitScreensPanel.Panel2Collapsed = true;
                    }
                    break;
            }
        }

        public void UpdateCaptureBuffers()
        {
            // The screen list has changed and involve capture screens.
            // Update their shared state to trigger a memory buffer reset.
            var shared = ScreenList.Count == 2;
            foreach (var screen in ScreenList)
            {
                var capScreen = screen as CaptureScreen;
                if (capScreen != null)
                {
                    capScreen.Shared = shared;
                }
            }
        }

        public void FullScreen(bool bFullScreen)
        {
            // Propagate the new mode to screens.
            foreach (var screen in ScreenList)
            {
                screen.FullScreen(bFullScreen);
            }
        }

        public static void AlertInvalidFileName()
        {
            var msgTitle = ScreenManagerLang.Error_Capture_InvalidFile_Title;
            var msgText = ScreenManagerLang.Error_Capture_InvalidFile_Text.Replace("\\n", "\n");

            MessageBox.Show(msgText, msgTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public static void LocateForm(Form form)
        {
            // A helper function to locate the dialog box right under the mouse, or center of screen.
            if (Cursor.Position.X + (form.Width / 2) >= SystemInformation.PrimaryMonitorSize.Width ||
                Cursor.Position.Y + form.Height >= SystemInformation.PrimaryMonitorSize.Height)
            {
                form.StartPosition = FormStartPosition.CenterScreen;
            }
            else
            {
                form.Location = new Point(Cursor.Position.X - (form.Width / 2), Cursor.Position.Y - 20);
            }
        }

        #endregion Public Methods

        #region Menu organization

        public void OrganizeMenus()
        {
            DoOrganizeMenu();
        }

        private void BuildSvgMenu()
        {
            _mnuSvgTools.Image = Resources.images;
            _mnuSvgTools.MergeAction = MergeAction.Append;
            _mnuImportImage.Image = Resources.image;
            _mnuImportImage.Click += mnuImportImage_OnClick;
            _mnuImportImage.MergeAction = MergeAction.Append;
            AddImportImageMenu(_mnuSvgTools);

            AddSvgSubMenus(_mSvgPath, _mnuSvgTools);
        }

        private void AddImportImageMenu(ToolStripMenuItem menu)
        {
            menu.DropDownItems.Add(_mnuImportImage);
            menu.DropDownItems.Add(new ToolStripSeparator());
        }

        private void AddSvgSubMenus(string dir, ToolStripMenuItem menu)
        {
            // This is a recursive function that browses a directory and its sub directories,
            // each directory is made into a menu tree, each svg file is added as a menu leaf.
            _mBuildingSvgMenu = true;

            if (Directory.Exists(dir))
            {
                // Loop sub directories.
                var subDirs = Directory.GetDirectories(dir);
                foreach (var subDir in subDirs)
                {
                    // Create a menu
                    var mnuSubDir = new ToolStripMenuItem();
                    mnuSubDir.Text = Path.GetFileName(subDir);
                    mnuSubDir.Image = Resources.folder;
                    mnuSubDir.MergeAction = MergeAction.Append;

                    // Build sub tree.
                    AddSvgSubMenus(subDir, mnuSubDir);

                    // Add to parent if non-empty.
                    if (mnuSubDir.HasDropDownItems)
                    {
                        menu.DropDownItems.Add(mnuSubDir);
                    }
                }

                // Then loop files within the sub directory.
                foreach (var file in Directory.GetFiles(dir))
                {
                    if (Path.GetExtension(file).ToLower().Equals(".svg"))
                    {
                        _mBHasSvgFiles = true;

                        // Create a menu.
                        var mnuSvgDrawing = new ToolStripMenuItem();
                        mnuSvgDrawing.Text = Path.GetFileNameWithoutExtension(file);
                        mnuSvgDrawing.Tag = file;
                        mnuSvgDrawing.Image = Resources.vector;
                        mnuSvgDrawing.Click += mnuSVGDrawing_OnClick;
                        mnuSvgDrawing.MergeAction = MergeAction.Append;

                        // Add to parent.
                        menu.DropDownItems.Add(mnuSvgDrawing);
                    }
                }
            }

            _mBuildingSvgMenu = false;
        }

        private void DoOrganizeMenu()
        {
            // Enable / disable menus depending on state of active screen
            // and global screen configuration.

            #region Menus depending only on the state of the active screen

            var bActiveScreenIsEmpty = false;
            if (MActiveScreen != null && ScreenList.Count > 0)
            {
                if (!MActiveScreen.Full)
                {
                    bActiveScreenIsEmpty = true;
                }
                else if (MActiveScreen is PlayerScreen)
                {
                    PlayerScreen player = MActiveScreen as PlayerScreen;

                    // 1. Video is loaded : save-able and analysis is loadable.

                    // File
                    _mnuSave.Enabled = true;
                    _toolSave.Enabled = true;
                    _mnuExportSpreadsheet.Enabled = player.FrameServer.Metadata.HasData;
                    _mnuExportOdf.Enabled = player.FrameServer.Metadata.HasData;
                    _mnuExportMsxml.Enabled = player.FrameServer.Metadata.HasData;
                    _mnuExportXhtml.Enabled = player.FrameServer.Metadata.HasData;
                    _mnuExportText.Enabled = player.FrameServer.Metadata.HasTrack();
                    _mnuLoadAnalysis.Enabled = true;

                    // Image
                    _mnuDeinterlace.Enabled = true;
                    _mnuMirror.Enabled = true;
                    _mnuSvgTools.Enabled = _mBHasSvgFiles;
                    _mnuCoordinateAxis.Enabled = true;

                    _mnuDeinterlace.Checked = player.Deinterlaced;
                    _mnuMirror.Checked = player.Mirrored;

                    if (!player.IsSingleFrame)
                    {
                        ConfigureImageFormatMenus(player);
                    }
                    else
                    {
                        // Prevent usage of format menu for image files
                        ConfigureImageFormatMenus(null);
                    }

                    // Motion
                    _mnuHighspeedCamera.Enabled = true;
                    ConfigureVideoFilterMenus(player);
                }
                else if (MActiveScreen is CaptureScreen)
                {
                    var cs = MActiveScreen as CaptureScreen;

                    // File
                    _mnuSave.Enabled = false;
                    _toolSave.Enabled = false;
                    _mnuExportSpreadsheet.Enabled = false;
                    _mnuExportOdf.Enabled = false;
                    _mnuExportMsxml.Enabled = false;
                    _mnuExportXhtml.Enabled = false;
                    _mnuExportText.Enabled = false;
                    _mnuLoadAnalysis.Enabled = false;

                    // Image
                    _mnuDeinterlace.Enabled = false;
                    _mnuMirror.Enabled = false;
                    _mnuSvgTools.Enabled = _mBHasSvgFiles;
                    _mnuCoordinateAxis.Enabled = false;

                    _mnuDeinterlace.Checked = false;
                    _mnuMirror.Checked = false;

                    ConfigureImageFormatMenus(cs);

                    // Motion
                    _mnuHighspeedCamera.Enabled = false;
                    ConfigureVideoFilterMenus(null);
                }
                else
                {
                    // KO ?
                    bActiveScreenIsEmpty = true;
                }
            }
            else
            {
                // No active screen. ( = no screens)
                bActiveScreenIsEmpty = true;
            }

            if (bActiveScreenIsEmpty)
            {
                // File
                _mnuSave.Enabled = false;
                _toolSave.Enabled = false;
                _mnuLoadAnalysis.Enabled = false;
                _mnuExportSpreadsheet.Enabled = false;
                _mnuExportOdf.Enabled = false;
                _mnuExportMsxml.Enabled = false;
                _mnuExportXhtml.Enabled = false;
                _mnuExportText.Enabled = false;

                // Image
                _mnuDeinterlace.Enabled = false;
                _mnuMirror.Enabled = false;
                _mnuSvgTools.Enabled = false;
                _mnuCoordinateAxis.Enabled = false;
                _mnuDeinterlace.Checked = false;
                _mnuMirror.Checked = false;

                ConfigureImageFormatMenus(null);

                // Motion
                _mnuHighspeedCamera.Enabled = false;
                ConfigureVideoFilterMenus(null);
            }

            #endregion Menus depending only on the state of the active screen

            #region Menus depending on the specifc screen configuration

            // File
            _mnuCloseFile.Visible = false;
            _mnuCloseFile.Enabled = false;
            _mnuCloseFile2.Visible = false;
            _mnuCloseFile2.Enabled = false;
            var strClosingText = ScreenManagerLang.Generic_Close;

            var bAllScreensEmpty = false;
            switch (ScreenList.Count)
            {
                case 0:

                    // No screens at all.
                    _mnuSwapScreens.Enabled = false;
                    _mnuToggleCommonCtrls.Enabled = false;
                    bAllScreensEmpty = true;
                    break;

                case 1:

                    // Only one screen
                    _mnuSwapScreens.Enabled = false;
                    _mnuToggleCommonCtrls.Enabled = false;

                    if (!ScreenList[0].Full)
                    {
                        bAllScreensEmpty = true;
                    }
                    else if (ScreenList[0] is PlayerScreen)
                    {
                        // Only screen is an full PlayerScreen.
                        _mnuCloseFile.Text = strClosingText;
                        _mnuCloseFile.Enabled = true;
                        _mnuCloseFile.Visible = true;

                        _mnuCloseFile2.Visible = false;
                        _mnuCloseFile2.Enabled = false;
                    }
                    else if (ScreenList[0] is CaptureScreen)
                    {
                        bAllScreensEmpty = true;
                    }
                    break;

                case 2:

                    // Two screens
                    _mnuSwapScreens.Enabled = true;
                    _mnuToggleCommonCtrls.Enabled = _mBCanShowCommonControls;

                    // Left Screen
                    if (ScreenList[0] is PlayerScreen)
                    {
                        if (ScreenList[0].Full)
                        {
                            bAllScreensEmpty = false;

                            var strCompleteClosingText = strClosingText + " - " +
                                                         ((PlayerScreen)ScreenList[0]).FileName;
                            _mnuCloseFile.Text = strCompleteClosingText;
                            _mnuCloseFile.Enabled = true;
                            _mnuCloseFile.Visible = true;
                        }
                        else
                        {
                            // Left screen is an empty PlayerScreen.
                            // Global emptiness might be changed below.
                            bAllScreensEmpty = true;
                        }
                    }
                    else if (ScreenList[0] is CaptureScreen)
                    {
                        // Global emptiness might be changed below.
                        bAllScreensEmpty = true;
                    }

                    // Right Screen.
                    if (ScreenList[1] is PlayerScreen)
                    {
                        if (ScreenList[1].Full)
                        {
                            bAllScreensEmpty = false;

                            var strCompleteClosingText = strClosingText + " - " +
                                                         ((PlayerScreen)ScreenList[1]).FileName;
                            _mnuCloseFile2.Text = strCompleteClosingText;
                            _mnuCloseFile2.Enabled = true;
                            _mnuCloseFile2.Visible = true;
                        }
                    }
                    else if (ScreenList[1] is CaptureScreen)
                    {
                        // Ecran de droite en capture.
                        // Si l'écran de gauche était également vide, bEmpty reste à true.
                        // Si l'écran de gauche était plein, bEmpty reste à false.
                    }
                    break;

                default:
                    // KO.
                    _mnuSwapScreens.Enabled = false;
                    _mnuToggleCommonCtrls.Enabled = false;
                    bAllScreensEmpty = true;
                    break;
            }

            if (bAllScreensEmpty)
            {
                // No screens at all, or all screens empty => 1 menu visible but disabled.

                _mnuCloseFile.Text = strClosingText;
                _mnuCloseFile.Visible = true;
                _mnuCloseFile.Enabled = false;

                _mnuCloseFile2.Visible = false;
            }

            #endregion Menus depending on the specifc screen configuration
        }

        private void ConfigureVideoFilterMenus(PlayerScreen player)
        {
            // determines if any given video filter menu should be
            // visible, enabled, checked...

            // 1. Visibility
            // Experimental menus are only visible if we are on experimental release.
            foreach (var vf in _mVideoFilters)
            {
                if (vf.Menu != null)
                    vf.Menu.Visible = vf.Experimental ? PreferencesManager.ExperimentalRelease : true;
            }

            // Secret menu. Set to true during developpement.
            _mVideoFilters[(int)VideoFilterType.Sandbox].Menu.Visible = false;

            // 2. Enabled, checked
            if (player != null)
            {
                // Video filters can only be enabled when Analysis mode.
                foreach (var vf in _mVideoFilters)
                    vf.Menu.Enabled = player.IsInAnalysisMode;

                if (player.IsInAnalysisMode)
                {
                    // Fixme: Why is this here ?
                    List<DecompressedFrame> frameList = player.FrameServer.VideoFile.FrameList;
                    foreach (var vf in _mVideoFilters)
                    {
                        vf.FrameList = frameList;
                    }
                }

                foreach (var vf in _mVideoFilters)
                    vf.Menu.Checked = false;

                if (player.DrawtimeFilterType > -1)
                    _mVideoFilters[player.DrawtimeFilterType].Menu.Checked = true;
            }
            else
            {
                foreach (var vf in _mVideoFilters)
                {
                    vf.Menu.Enabled = false;
                    vf.Menu.Checked = false;
                }
            }
        }

        private void ConfigureImageFormatMenus(AbstractScreen screen)
        {
            // Set the enable and check prop of the image formats menu according of current screen state.

            if (screen != null)
            {
                _mnuFormat.Enabled = true;
                _mnuFormatAuto.Enabled = true;
                _mnuFormatForce43.Enabled = true;
                _mnuFormatForce169.Enabled = true;

                // Reset all checks before setting the right one.
                _mnuFormatAuto.Checked = false;
                _mnuFormatForce43.Checked = false;
                _mnuFormatForce169.Checked = false;

                switch (screen.AspectRatio)
                {
                    case AspectRatio.Force43:
                        _mnuFormatForce43.Checked = true;
                        break;

                    case AspectRatio.Force169:
                        _mnuFormatForce169.Checked = true;
                        break;

                    case AspectRatio.AutoDetect:
                    default:
                        _mnuFormatAuto.Checked = true;
                        break;
                }
            }
            else
            {
                _mnuFormat.Enabled = false;
                _mnuFormatAuto.Enabled = false;
                _mnuFormatForce43.Enabled = false;
                _mnuFormatForce169.Enabled = false;
                _mnuFormatAuto.Checked = false;
                _mnuFormatForce43.Checked = false;
                _mnuFormatForce169.Checked = false;
            }
        }

        private void OnSvgFilesChanged(object source, FileSystemEventArgs e)
        {
            // We are in the file watcher thread. NO direct UI Calls from here.
            Log.Debug(string.Format("Action recorded in the guides directory: {0}", e.ChangeType));
            if (!_mBuildingSvgMenu)
            {
                _mBuildingSvgMenu = true;
                ((ScreenManagerUserInterface)Ui).BeginInvoke(_mSvgFilesChangedInvoker);
            }
        }

        public void DoSvgFilesChanged()
        {
            _mnuSvgTools.DropDownItems.Clear();
            AddImportImageMenu(_mnuSvgTools);
            AddSvgSubMenus(_mSvgPath, _mnuSvgTools);
        }

        #endregion Menu organization

        #region Culture

        private void RefreshCultureToolbar()
        {
            _toolSave.ToolTipText = ScreenManagerLang.mnuSave;
            _toolHome.ToolTipText = ScreenManagerLang.mnuHome;
            _toolOnePlayer.ToolTipText = ScreenManagerLang.mnuOnePlayer;
            _toolTwoPlayers.ToolTipText = ScreenManagerLang.mnuTwoPlayers;
            _toolOneCapture.ToolTipText = ScreenManagerLang.mnuOneCapture;
            _toolTwoCaptures.ToolTipText = ScreenManagerLang.mnuTwoCaptures;
            _toolTwoMixed.ToolTipText = ScreenManagerLang.mnuTwoMixed;
        }

        private void RefreshCultureMenu()
        {
            _mnuCloseFile.Text = ScreenManagerLang.Generic_Close;
            _mnuCloseFile2.Text = ScreenManagerLang.Generic_Close;
            _mnuSave.Text = ScreenManagerLang.mnuSave;
            _mnuExportSpreadsheet.Text = ScreenManagerLang.mnuExportSpreadsheet;
            _mnuExportOdf.Text = ScreenManagerLang.mnuExportODF;
            _mnuExportMsxml.Text = ScreenManagerLang.mnuExportMSXML;
            _mnuExportXhtml.Text = ScreenManagerLang.mnuExportXHTML;
            _mnuExportText.Text = ScreenManagerLang.mnuExportTEXT;
            _mnuLoadAnalysis.Text = ScreenManagerLang.mnuLoadAnalysis;

            _mnuOnePlayer.Text = ScreenManagerLang.mnuOnePlayer;
            _mnuTwoPlayers.Text = ScreenManagerLang.mnuTwoPlayers;
            _mnuOneCapture.Text = ScreenManagerLang.mnuOneCapture;
            _mnuTwoCaptures.Text = ScreenManagerLang.mnuTwoCaptures;
            _mnuTwoMixed.Text = ScreenManagerLang.mnuTwoMixed;
            _mnuSwapScreens.Text = ScreenManagerLang.mnuSwapScreens;
            _mnuToggleCommonCtrls.Text = ScreenManagerLang.mnuToggleCommonCtrls;

            _mnuDeinterlace.Text = ScreenManagerLang.mnuDeinterlace;
            _mnuFormatAuto.Text = ScreenManagerLang.mnuFormatAuto;
            _mnuFormatForce43.Text = ScreenManagerLang.mnuFormatForce43;
            _mnuFormatForce169.Text = ScreenManagerLang.mnuFormatForce169;
            _mnuFormat.Text = ScreenManagerLang.mnuFormat;
            _mnuMirror.Text = ScreenManagerLang.mnuMirror;
            _mnuCoordinateAxis.Text = ScreenManagerLang.dlgConfigureTrajectory_SetOrigin;

            _mnuSvgTools.Text = ScreenManagerLang.mnuSVGTools;
            _mnuImportImage.Text = ScreenManagerLang.mnuImportImage;

            foreach (var avf in _mVideoFilters)
                avf.Menu.Text = avf.Name;

            _mnuHighspeedCamera.Text = ScreenManagerLang.mnuSetCaptureSpeed;
        }

        #endregion Culture

        #region Side by side saving

        private void bgWorkerDualSave_DoWork(object sender, DoWorkEventArgs e)
        {
            // This is executed in Worker Thread space. (Do not call any UI methods)

            // For each position: get both images, compute the composite, save it to the file.
            // If blending is activated, only get the image from left screen, since it already contains both images.
            Log.Debug("Saving side by side video.");

            if (_mBSynching && ScreenList.Count == 2)
            {
                PlayerScreen ps1 = ScreenList[0] as PlayerScreen;
                PlayerScreen ps2 = ScreenList[1] as PlayerScreen;
                if (ps1 != null && ps2 != null)
                {
                    // Todo: get frame interval from one of the videos.

                    // Get first frame outside the loop, to be able to set video size.
                    _mICurrentFrame = 0;
                    OnCommonPositionChanged(_mICurrentFrame, false);

                    Bitmap img1 = ps1.GetFlushedImage();
                    Bitmap img2 = null;
                    Bitmap composite;
                    if (!_mBSyncMerging)
                    {
                        img2 = ps2.GetFlushedImage();
                        composite = ImageHelper.GetSideBySideComposite(img1, img2, true, true);
                    }
                    else
                    {
                        composite = img1;
                    }

                    Log.Debug(string.Format("Composite size: {0}.", composite.Size));

                    // Configure a fake InfoVideo to setup image size.
                    var iv = new InfosVideo();
                    iv.iWidth = composite.Width;
                    iv.iHeight = composite.Height;

                    var result = _mVideoFileWriter.OpenSavingContext(_mDualSaveFileName, iv, -1, false);

                    if (result == SaveResult.Success)
                    {
                        _mVideoFileWriter.SaveFrame(composite);

                        img1.Dispose();
                        if (!_mBSyncMerging)
                        {
                            img2.Dispose();
                            composite.Dispose();
                        }

                        _mBgWorkerDualSave.ReportProgress(1, _mIMaxFrame);

                        // Loop all remaining frames in static sync mode, but without refreshing the UI.
                        while (_mICurrentFrame < _mIMaxFrame && !_mBDualSaveCancelled)
                        {
                            _mICurrentFrame++;

                            if (_mBgWorkerDualSave.CancellationPending)
                            {
                                e.Result = 1;
                                _mBDualSaveCancelled = true;
                                break;
                            }
                            // Move both playheads and get the composite image.
                            OnCommonPositionChanged(-1, false);
                            img1 = ps1.GetFlushedImage();
                            if (!_mBSyncMerging)
                            {
                                img2 = ps2.GetFlushedImage();
                                composite = ImageHelper.GetSideBySideComposite(img1, img2, true, true);
                            }
                            else
                            {
                                composite = img1;
                            }

                            // Save to file.
                            _mVideoFileWriter.SaveFrame(composite);

                            // Clean up and report progress.
                            img1.Dispose();
                            if (!_mBSyncMerging)
                            {
                                img2.Dispose();
                                composite.Dispose();
                            }

                            _mBgWorkerDualSave.ReportProgress(_mICurrentFrame + 1, _mIMaxFrame);
                        }

                        if (!_mBDualSaveCancelled)
                        {
                            e.Result = 0;
                        }
                    }
                    else
                    {
                        // Saving context couldn't be opened.
                        e.Result = 2;
                    }
                }
            }
        }

        private void bgWorkerDualSave_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // call snippet : m_BackgroundWorker.ReportProgress(iCurrentValue, iMaximum);
            if (!_mBgWorkerDualSave.CancellationPending)
            {
                var iValue = e.ProgressPercentage;
                var iMaximum = (int)e.UserState;
                if (iValue > iMaximum)
                {
                    iValue = iMaximum;
                }

                _mDualSaveProgressBar.Update(iValue, iMaximum, true);
            }
        }

        private void bgWorkerDualSave_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _mDualSaveProgressBar.Close();
            _mDualSaveProgressBar.Dispose();

            if (!_mBDualSaveCancelled && (int)e.Result != 1)
            {
                _mVideoFileWriter.CloseSavingContext((int)e.Result == 0);
            }

            var dp = DelegatesPool.Instance();
            if (dp.RefreshFileExplorer != null) dp.RefreshFileExplorer(false);
        }

        private void dualSave_CancelAsked(object sender, EventArgs e)
        {
            // This will simply set BgWorker.CancellationPending to true,
            // which we check periodically in the saving loop.
            // This will also end the bgWorker immediately,
            // maybe before we check for the cancellation in the other thread.
            _mVideoFileWriter.CloseSavingContext(false);
            _mBDualSaveCancelled = true;
            _mBgWorkerDualSave.CancelAsync();
        }

        private void DeleteTemporaryFile(string filename)
        {
            Log.Debug("Side by side video saving cancelled. Deleting temporary file.");
            if (File.Exists(filename))
            {
                try
                {
                    File.Delete(filename);
                }
                catch (Exception exp)
                {
                    Log.Error("Error while deleting temporary file.");
                    Log.Error(exp.Message);
                    Log.Error(exp.StackTrace);
                }
            }
        }

        #endregion Side by side saving

        #region Menus events handlers

        #region File

        private void MnuCloseFileOnClick(object sender, EventArgs e)
        {
            // In this event handler, we always close the first ([0]) screen.
            RemoveScreen(0, true);

            ICommand css = new CommandShowScreens(this);
            CommandManager.LaunchCommand(css);

            OrganizeCommonControls();
            OrganizeMenus();
        }

        private void MnuCloseFile2OnClick(object sender, EventArgs e)
        {
            // In this event handler, we always close the first ([1]) screen.
            RemoveScreen(1, true);

            ICommand css = new CommandShowScreens(this);
            CommandManager.LaunchCommand(css);

            OrganizeCommonControls();
            OrganizeMenus();
        }

        public void MnuSaveOnClick(object sender, EventArgs e)
        {
            //---------------------------------------------------------------------------
            // Launch the dialog box where the user can choose to save the video,
            // the metadata or both.
            // Public because accessed from the closing command when we realize there are
            // unsaved modified data.
            //---------------------------------------------------------------------------

            PlayerScreen ps = MActiveScreen as PlayerScreen;
            if (ps != null)
            {
                DoStopPlaying();
                DoDeactivateKeyboardHandler();

                ps.Save();

                DoActivateKeyboardHandler();
            }
        }

        private void MnuLoadAnalysisOnClick(object sender, EventArgs e)
        {
            if (MActiveScreen != null)
            {
                if (MActiveScreen is PlayerScreen)
                {
                    LoadAnalysis();
                }
            }
        }

        private void LoadAnalysis()
        {
            DoStopPlaying();

            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = ScreenManagerLang.dlgLoadAnalysis_Title;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Filter = ScreenManagerLang.dlgLoadAnalysis_Filter;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var filePath = openFileDialog.FileName;
                if (filePath.Length > 0)
                {
                    ((PlayerScreen)MActiveScreen).FrameServer.Metadata.Load(filePath, true);
                    ((PlayerScreen)MActiveScreen).MPlayerScreenUi.PostImportMetadata();
                }
            }
        }

        private void mnuExportODF_OnClick(object sender, EventArgs e)
        {
            ExportSpreadsheet(MetadataExportFormat.Odf);
        }

        private void mnuExportMSXML_OnClick(object sender, EventArgs e)
        {
            ExportSpreadsheet(MetadataExportFormat.Msxml);
        }

        private void mnuExportXHTML_OnClick(object sender, EventArgs e)
        {
            ExportSpreadsheet(MetadataExportFormat.Xhtml);
        }

        private void mnuExportTEXT_OnClick(object sender, EventArgs e)
        {
            ExportSpreadsheet(MetadataExportFormat.Text);
        }

        private void ExportSpreadsheet(MetadataExportFormat format)
        {
            PlayerScreen player = MActiveScreen as PlayerScreen;
            if (player != null)
            {
                if (player.FrameServer.Metadata.HasData)
                {
                    DoStopPlaying();

                    var saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Title = ScreenManagerLang.dlgExportSpreadsheet_Title;
                    saveFileDialog.RestoreDirectory = true;
                    saveFileDialog.Filter = ScreenManagerLang.dlgExportSpreadsheet_Filter;

                    saveFileDialog.FilterIndex = ((int)format) + 1;

                    saveFileDialog.FileName = Path.GetFileNameWithoutExtension(player.FrameServer.Metadata.FullPath);

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        var filePath = saveFileDialog.FileName;
                        if (filePath.Length > 0)
                        {
                            player.FrameServer.Metadata.Export(filePath, format);
                        }
                    }
                }
            }
        }

        #endregion File

        #region View

        private void mnuHome_OnClick(object sender, EventArgs e)
        {
            // Remove all screens.
            if (ScreenList.Count > 0)
            {
                if (RemoveScreen(0, true))
                {
                    _mBSynching = false;

                    if (ScreenList.Count > 0)
                    {
                        // Second screen is now in [0] spot.
                        RemoveScreen(0, true);
                    }
                }

                // Display the new list.
                var cm = CommandManager.Instance();
                ICommand css = new CommandShowScreens(this);
                CommandManager.LaunchCommand(css);

                OrganizeCommonControls();
                OrganizeMenus();
            }
        }

        private void MnuOnePlayerOnClick(object sender, EventArgs e)
        {
            //------------------------------------------------------------
            // - Reorganize the list so it conforms to the asked combination.
            // - Display the new list.
            //
            // Here : One player screen.
            //------------------------------------------------------------

            _mBSynching = false;
            var cm = CommandManager.Instance();

            switch (ScreenList.Count)
            {
                case 0:
                    {
                        // Currently : 0 screens. -> add a player.
                        IUndoableCommand caps = new CommandAddPlayerScreen(this, true);
                        cm.LaunchUndoableCommand(caps);
                        break;
                    }
                case 1:
                    {
                        if (ScreenList[0] is CaptureScreen)
                        {
                            // Currently : 1 capture. -> remove and add a player.
                            IUndoableCommand crs = new CommandRemoveScreen(this, 0, true);
                            cm.LaunchUndoableCommand(crs);
                            IUndoableCommand caps = new CommandAddPlayerScreen(this, true);
                            cm.LaunchUndoableCommand(caps);
                        }
                        break;
                    }
                case 2:
                    {
                        // We need to decide which screen(s) to remove.
                        // Possible cases :
                        // [capture][capture] -> remove both and add player.
                        // [capture][player] -> remove capture.
                        // [player][capture] -> remove capture.
                        // [player][player] -> depends on emptiness.

                        if (ScreenList[0] is CaptureScreen && ScreenList[1] is CaptureScreen)
                        {
                            // [capture][capture] -> remove both and add player.
                            IUndoableCommand crs = new CommandRemoveScreen(this, 0, true);
                            cm.LaunchUndoableCommand(crs);
                            IUndoableCommand crs2 = new CommandRemoveScreen(this, 0, true);
                            cm.LaunchUndoableCommand(crs2);
                            IUndoableCommand caps = new CommandAddPlayerScreen(this, true);
                            cm.LaunchUndoableCommand(caps);
                        }
                        else if (ScreenList[0] is CaptureScreen && ScreenList[1] is PlayerScreen)
                        {
                            // [capture][player] -> remove capture.
                            IUndoableCommand crs = new CommandRemoveScreen(this, 0, true);
                            cm.LaunchUndoableCommand(crs);
                        }
                        else if (ScreenList[0] is PlayerScreen && ScreenList[1] is CaptureScreen)
                        {
                            // [player][capture] -> remove capture.
                            IUndoableCommand crs = new CommandRemoveScreen(this, 1, true);
                            cm.LaunchUndoableCommand(crs);
                        }
                        else
                        {
                            //---------------------------------------------
                            // [player][player] -> depends on emptiness :
                            //
                            // [empty][full] -> remove empty.
                            // [full][full] -> remove second one (right).
                            // [full][empty] -> remove empty (right).
                            // [empty][empty] -> remove second one (right).
                            //---------------------------------------------

                            if (!ScreenList[0].Full && ScreenList[1].Full)
                            {
                                RemoveScreen(0, true);
                            }
                            else
                            {
                                RemoveScreen(1, true);
                            }
                        }
                        break;
                    }
                default:
                    break;
            }

            // Display the new list.
            ICommand css = new CommandShowScreens(this);
            CommandManager.LaunchCommand(css);

            OrganizeCommonControls();
            OrganizeMenus();
        }

        private void MnuTwoPlayersOnClick(object sender, EventArgs e)
        {
            //------------------------------------------------------------
            // - Reorganize the list so it conforms to the asked combination.
            // - Display the new list.
            //
            // Here : Two player screens.
            //------------------------------------------------------------
            _mBSynching = false;
            var cm = CommandManager.Instance();

            switch (ScreenList.Count)
            {
                case 0:
                    {
                        // Currently : 0 screens. -> add two players.
                        // We use two different commands to keep the undo history working.
                        IUndoableCommand caps1 = new CommandAddPlayerScreen(this, true);
                        cm.LaunchUndoableCommand(caps1);
                        IUndoableCommand caps2 = new CommandAddPlayerScreen(this, true);
                        cm.LaunchUndoableCommand(caps2);
                        break;
                    }
                case 1:
                    {
                        if (ScreenList[0] is CaptureScreen)
                        {
                            // Currently : 1 capture. -> remove and add 2 players.
                            IUndoableCommand crs = new CommandRemoveScreen(this, 0, true);
                            cm.LaunchUndoableCommand(crs);
                            IUndoableCommand caps1 = new CommandAddPlayerScreen(this, true);
                            cm.LaunchUndoableCommand(caps1);
                            IUndoableCommand caps2 = new CommandAddPlayerScreen(this, true);
                            cm.LaunchUndoableCommand(caps2);
                        }
                        else
                        {
                            // Currently : 1 player. -> add another.
                            IUndoableCommand caps = new CommandAddPlayerScreen(this, true);
                            cm.LaunchUndoableCommand(caps);
                        }
                        break;
                    }
                case 2:
                    {
                        // We need to decide which screen(s) to remove.
                        // Possible cases :
                        // [capture][capture] -> remove both and add two players.
                        // [capture][player] -> remove capture and add player.
                        // [player][capture] -> remove capture and add player.
                        // [player][player] -> do nothing.

                        if (ScreenList[0] is CaptureScreen && ScreenList[1] is CaptureScreen)
                        {
                            // [capture][capture] -> remove both and add two players.
                            IUndoableCommand crs = new CommandRemoveScreen(this, 0, true);
                            cm.LaunchUndoableCommand(crs);
                            IUndoableCommand crs2 = new CommandRemoveScreen(this, 0, true);
                            cm.LaunchUndoableCommand(crs2);
                            IUndoableCommand caps1 = new CommandAddPlayerScreen(this, true);
                            cm.LaunchUndoableCommand(caps1);
                            IUndoableCommand caps2 = new CommandAddPlayerScreen(this, true);
                            cm.LaunchUndoableCommand(caps2);
                        }
                        else if (ScreenList[0] is CaptureScreen && ScreenList[1] is PlayerScreen)
                        {
                            // [capture][player] -> remove capture and add player.
                            IUndoableCommand crs = new CommandRemoveScreen(this, 0, true);
                            cm.LaunchUndoableCommand(crs);
                            IUndoableCommand caps = new CommandAddPlayerScreen(this, true);
                            cm.LaunchUndoableCommand(caps);
                        }
                        else if (ScreenList[0] is PlayerScreen && ScreenList[1] is CaptureScreen)
                        {
                            // [player][capture] -> remove capture and add player.
                            IUndoableCommand crs = new CommandRemoveScreen(this, 1, true);
                            cm.LaunchUndoableCommand(crs);
                            IUndoableCommand caps = new CommandAddPlayerScreen(this, true);
                            cm.LaunchUndoableCommand(caps);
                        }

                        break;
                    }
                default:
                    break;
            }

            // Display the new list.
            ICommand css = new CommandShowScreens(this);
            CommandManager.LaunchCommand(css);

            OrganizeCommonControls();
            OrganizeMenus();
        }

        private void MnuOneCaptureOnClick(object sender, EventArgs e)
        {
            //------------------------------------------------------------
            // - Reorganize the list so it conforms to the asked combination.
            // - Display the new list.
            //
            // Here : One capture screens.
            //------------------------------------------------------------
            _mBSynching = false;
            var cm = CommandManager.Instance();

            switch (ScreenList.Count)
            {
                case 0:
                    {
                        // Currently : 0 screens. -> add a capture.
                        IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                        cm.LaunchUndoableCommand(cacs);
                        break;
                    }
                case 1:
                    {
                        if (ScreenList[0] is PlayerScreen)
                        {
                            // Currently : 1 player. -> remove and add a capture.
                            if (RemoveScreen(0, true))
                            {
                                IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                                cm.LaunchUndoableCommand(cacs);
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        // We need to decide which screen(s) to remove.
                        // Possible cases :
                        // [capture][capture] -> depends on emptiness.
                        // [capture][player] -> remove player.
                        // [player][capture] -> remove player.
                        // [player][player] -> remove both and add capture.

                        if (ScreenList[0] is CaptureScreen && ScreenList[1] is CaptureScreen)
                        {
                            //---------------------------------------------
                            // [capture][capture] -> depends on emptiness.
                            //
                            // [empty][full] -> remove empty.
                            // [full][full] -> remove second one (right).
                            // [full][empty] -> remove empty (right).
                            // [empty][empty] -> remove second one (right).
                            //---------------------------------------------

                            if (!ScreenList[0].Full && ScreenList[1].Full)
                            {
                                RemoveScreen(0, true);
                            }
                            else
                            {
                                RemoveScreen(1, true);
                            }
                        }
                        else if (ScreenList[0] is CaptureScreen && ScreenList[1] is PlayerScreen)
                        {
                            // [capture][player] -> remove player.
                            RemoveScreen(1, true);
                        }
                        else if (ScreenList[0] is PlayerScreen && ScreenList[1] is CaptureScreen)
                        {
                            // [player][capture] -> remove player.
                            RemoveScreen(0, true);
                        }
                        else
                        {
                            // remove both and add one capture.
                            if (RemoveScreen(0, true))
                            {
                                // remaining player has moved in [0] spot.
                                if (RemoveScreen(0, true))
                                {
                                    IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                                    cm.LaunchUndoableCommand(cacs);
                                }
                            }
                        }
                        break;
                    }
                default:
                    break;
            }

            // Display the new list.
            ICommand css = new CommandShowScreens(this);
            CommandManager.LaunchCommand(css);

            UpdateCaptureBuffers();
            OrganizeCommonControls();
            OrganizeMenus();
        }

        private void MnuTwoCapturesOnClick(object sender, EventArgs e)
        {
            //------------------------------------------------------------
            // - Reorganize the list so it conforms to the asked combination.
            // - Display the new list.
            //
            // Here : Two capture screens.
            //------------------------------------------------------------
            _mBSynching = false;
            var cm = CommandManager.Instance();

            switch (ScreenList.Count)
            {
                case 0:
                    {
                        // Currently : 0 screens. -> add two capture.
                        // We use two different commands to keep the undo history working.
                        IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                        cm.LaunchUndoableCommand(cacs);
                        IUndoableCommand cacs2 = new CommandAddCaptureScreen(this, true);
                        cm.LaunchUndoableCommand(cacs2);
                        break;
                    }
                case 1:
                    {
                        if (ScreenList[0] is CaptureScreen)
                        {
                            // Currently : 1 capture. -> add another.
                            IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                            cm.LaunchUndoableCommand(cacs);
                        }
                        else
                        {
                            // Currently : 1 player. -> remove and add 2 capture.
                            if (RemoveScreen(0, true))
                            {
                                IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                                cm.LaunchUndoableCommand(cacs);
                                IUndoableCommand cacs2 = new CommandAddCaptureScreen(this, true);
                                cm.LaunchUndoableCommand(cacs2);
                            }
                        }
                        break;
                    }
                case 2:
                    {
                        // We need to decide which screen(s) to remove.
                        // Possible cases :
                        // [capture][capture] -> do nothing.
                        // [capture][player] -> remove player and add capture.
                        // [player][capture] -> remove player and add capture.
                        // [player][player] -> remove both and add 2 capture.

                        if (ScreenList[0] is CaptureScreen && ScreenList[1] is CaptureScreen)
                        {
                            // [capture][capture] -> do nothing.
                        }
                        else if (ScreenList[0] is CaptureScreen && ScreenList[1] is PlayerScreen)
                        {
                            // [capture][player] -> remove player and add capture.
                            if (RemoveScreen(1, true))
                            {
                                IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                                cm.LaunchUndoableCommand(cacs);
                            }
                        }
                        else if (ScreenList[0] is PlayerScreen && ScreenList[1] is CaptureScreen)
                        {
                            // [player][capture] -> remove player and add capture.
                            if (RemoveScreen(0, true))
                            {
                                IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                                cm.LaunchUndoableCommand(cacs);
                            }
                        }
                        else
                        {
                            // [player][player] -> remove both and add 2 capture.
                            if (RemoveScreen(0, true))
                            {
                                // remaining player has moved in [0] spot.
                                if (RemoveScreen(0, true))
                                {
                                    IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                                    cm.LaunchUndoableCommand(cacs);
                                    IUndoableCommand cacs2 = new CommandAddCaptureScreen(this, true);
                                    cm.LaunchUndoableCommand(cacs2);
                                }
                            }
                        }

                        break;
                    }
                default:
                    break;
            }

            // Display the new list.
            ICommand css = new CommandShowScreens(this);
            CommandManager.LaunchCommand(css);

            UpdateCaptureBuffers();
            OrganizeCommonControls();
            OrganizeMenus();
        }

        private void MnuTwoMixedOnClick(object sender, EventArgs e)
        {
            //------------------------------------------------------------
            // - Reorganize the list so it conforms to the asked combination.
            // - Display the new list.
            //
            // Here : Mixed screen. The workspace preset is : [capture][player]
            //------------------------------------------------------------
            _mBSynching = false;
            var cm = CommandManager.Instance();

            switch (ScreenList.Count)
            {
                case 0:
                    {
                        // Currently : 0 screens. -> add a capture and a player.
                        IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                        cm.LaunchUndoableCommand(cacs);
                        IUndoableCommand caps = new CommandAddPlayerScreen(this, true);
                        cm.LaunchUndoableCommand(caps);
                        break;
                    }
                case 1:
                    {
                        if (ScreenList[0] is CaptureScreen)
                        {
                            // Currently : 1 capture. -> add a player.
                            IUndoableCommand caps = new CommandAddPlayerScreen(this, true);
                            cm.LaunchUndoableCommand(caps);
                        }
                        else
                        {
                            // Currently : 1 player. -> add a capture.
                            IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                            cm.LaunchUndoableCommand(cacs);
                        }
                        break;
                    }
                case 2:
                    {
                        // We need to decide which screen(s) to remove/replace.

                        if (ScreenList[0] is CaptureScreen && ScreenList[1] is CaptureScreen)
                        {
                            // [capture][capture] -> remove right and add player.
                            IUndoableCommand crs = new CommandRemoveScreen(this, 1, true);
                            cm.LaunchUndoableCommand(crs);
                            IUndoableCommand caps1 = new CommandAddPlayerScreen(this, true);
                            cm.LaunchUndoableCommand(caps1);
                        }
                        else if (ScreenList[0] is CaptureScreen && ScreenList[1] is PlayerScreen)
                        {
                            // [capture][player] -> do nothing.
                        }
                        else if (ScreenList[0] is PlayerScreen && ScreenList[1] is CaptureScreen)
                        {
                            // [player][capture] -> do nothing.
                        }
                        else
                        {
                            // [player][player] -> remove right and add capture.
                            if (RemoveScreen(1, true))
                            {
                                IUndoableCommand cacs = new CommandAddCaptureScreen(this, true);
                                cm.LaunchUndoableCommand(cacs);
                            }
                        }

                        break;
                    }
                default:
                    break;
            }

            // Display the new list.
            ICommand css = new CommandShowScreens(this);
            CommandManager.LaunchCommand(css);

            UpdateCaptureBuffers();
            OrganizeCommonControls();
            OrganizeMenus();
        }

        private void MnuSwapScreensOnClick(object sender, EventArgs e)
        {
            if (ScreenList.Count == 2)
            {
                IUndoableCommand command = new CommandSwapScreens(this);
                var cm = CommandManager.Instance();
                cm.LaunchUndoableCommand(command);
            }
        }

        private void MnuToggleCommonCtrlsOnClick(object sender, EventArgs e)
        {
            IUndoableCommand ctcc = new CommandToggleCommonControls(((ScreenManagerUserInterface)Ui).splitScreensPanel);
            var cm = CommandManager.Instance();
            cm.LaunchUndoableCommand(ctcc);
        }

        #endregion View

        #region Image

        private void MnuDeinterlaceOnClick(object sender, EventArgs e)
        {
            PlayerScreen player = MActiveScreen as PlayerScreen;
            if (player != null)
            {
                _mnuDeinterlace.Checked = !_mnuDeinterlace.Checked;
                player.Deinterlaced = _mnuDeinterlace.Checked;
            }
        }

        private void MnuFormatAutoOnClick(object sender, EventArgs e)
        {
            ChangeAspectRatio(AspectRatio.AutoDetect);
        }

        private void MnuFormatForce43OnClick(object sender, EventArgs e)
        {
            ChangeAspectRatio(AspectRatio.Force43);
        }

        private void MnuFormatForce169OnClick(object sender, EventArgs e)
        {
            ChangeAspectRatio(AspectRatio.Force169);
        }

        private void ChangeAspectRatio(AspectRatio aspectRatio)
        {
            if (MActiveScreen != null)
            {
                if (MActiveScreen.AspectRatio != aspectRatio)
                {
                    MActiveScreen.AspectRatio = aspectRatio;
                }

                _mnuFormatForce43.Checked = aspectRatio == AspectRatio.Force43;
                _mnuFormatForce169.Checked = aspectRatio == AspectRatio.Force169;
                _mnuFormatAuto.Checked = aspectRatio == AspectRatio.AutoDetect;
            }
        }

        private void MnuMirrorOnClick(object sender, EventArgs e)
        {
            PlayerScreen player = MActiveScreen as PlayerScreen;
            if (player != null)
            {
                _mnuMirror.Checked = !_mnuMirror.Checked;
                player.Mirrored = _mnuMirror.Checked;
            }
        }

        private void mnuImportImage_OnClick(object sender, EventArgs e)
        {
            // Display file open dialog and launch the drawing.
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = ScreenManagerLang.dlgImportReference_Title;
            openFileDialog.Filter = ScreenManagerLang.dlgImportReference_Filter;
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (openFileDialog.FileName.Length > 0 && MActiveScreen != null && MActiveScreen.CapabilityDrawings)
                {
                    LoadDrawing(openFileDialog.FileName, Path.GetExtension(openFileDialog.FileName).ToLower() == ".svg");
                }
            }
        }

        private void mnuSVGDrawing_OnClick(object sender, EventArgs e)
        {
            // One of the dynamically added SVG tools menu has been clicked.

            // Add a drawing of the right type to the active screen.
            var menu = sender as ToolStripMenuItem;
            if (menu != null)
            {
                var svgFile = menu.Tag as string;
                LoadDrawing(svgFile, true);
            }
        }

        private void LoadDrawing(string filePath, bool _bIsSvg)
        {
            if (filePath != null && filePath.Length > 0 && MActiveScreen != null && MActiveScreen.CapabilityDrawings)
            {
                MActiveScreen.AddImageDrawing(filePath, _bIsSvg);
            }
        }

        private void mnuCoordinateAxis_OnClick(object sender, EventArgs e)
        {
            PlayerScreen ps = MActiveScreen as PlayerScreen;
            if (ps != null)
            {
                var fsto = new FormSetTrajectoryOrigin(ps.FrameServer.VideoFile.CurrentImage, ps.FrameServer.Metadata);
                fsto.StartPosition = FormStartPosition.CenterScreen;
                fsto.ShowDialog();
                fsto.Dispose();
                ps.RefreshImage();
            }
        }

        #endregion Image

        #region Motion

        private void mnuHighspeedCamera_OnClick(object sender, EventArgs e)
        {
            PlayerScreen ps = MActiveScreen as PlayerScreen;
            if (ps != null)
            {
                ps.ConfigureHighSpeedCamera();
            }
        }

        #endregion Motion

        #endregion Menus events handlers

        #region Services

        public void DoLoadMovieInScreen(string filePath, int iForceScreen, bool bStoreState)
        {
            if (File.Exists(filePath))
            {
                IUndoableCommand clmis = new CommandLoadMovieInScreen(this, filePath, iForceScreen, bStoreState);
                var cm = CommandManager.Instance();
                cm.LaunchUndoableCommand(clmis);

                // No need to call PrepareSync here because it will be called when the working zone is set anyway.
            }
        }

        public void DoStopPlaying()
        {
            // Called from Supervisor, when user launch open dialog box.

            // 1. Stop each screen.
            foreach (var screen in ScreenList)
            {
                if (screen is PlayerScreen)
                {
                    ((PlayerScreen)screen).StopPlaying();
                }
            }

            // 2. Stop the common timer.
            StopDynamicSync();
            ((ScreenManagerUserInterface)Ui).DisplayAsPaused();
        }

        public void DoDeactivateKeyboardHandler()
        {
            _mBAllowKeyboardHandler = false;
        }

        public void DoActivateKeyboardHandler()
        {
            _mBAllowKeyboardHandler = true;
        }

        public void DoVideoProcessingDone(DrawtimeFilterOutput dfo)
        {
            // Disable draw time filter in player.
            if (dfo != null)
            {
                _mVideoFilters[dfo.VideoFilterType].Menu.Checked = dfo.Active;

                PlayerScreen player = MActiveScreen as PlayerScreen;
                if (player != null)
                {
                    player.SetDrawingtimeFilterOutput(dfo);
                }
            }

            MActiveScreen.RefreshImage();
        }

        #endregion Services

        #region Keyboard Handling

        private bool OnKeyPress(Keys keycode)
        {
            //---------------------------------------------------------
            // Here are grouped the handling of the keystrokes that are
            // screen manager's responsibility.
            // And only when the common controls are actually visible.
            //---------------------------------------------------------
            var bWasHandled = false;
            var smui = Ui as ScreenManagerUserInterface;

            if (smui != null)
            {
                switch (keycode)
                {
                    case Keys.Space:
                    case Keys.Return:
                        {
                            smui.ComCtrls.buttonPlay_Click(null, EventArgs.Empty);
                            bWasHandled = true;
                            break;
                        }
                    case Keys.Left:
                        {
                            smui.ComCtrls.buttonGotoPrevious_Click(null, EventArgs.Empty);
                            bWasHandled = true;
                            break;
                        }
                    case Keys.Right:
                        {
                            smui.ComCtrls.buttonGotoNext_Click(null, EventArgs.Empty);
                            bWasHandled = true;
                            break;
                        }
                    case Keys.End:
                        {
                            smui.ComCtrls.buttonGotoLast_Click(null, EventArgs.Empty);
                            bWasHandled = true;
                            break;
                        }
                    case Keys.Home:
                        {
                            smui.ComCtrls.buttonGotoFirst_Click(null, EventArgs.Empty);
                            bWasHandled = true;
                            break;
                        }
                    default:
                        break;
                }
            }
            return bWasHandled;
        }

        private void ActivateOtherScreen()
        {
            if (ScreenList.Count == 2)
            {
                if (MActiveScreen == ScreenList[0])
                {
                    Screen_SetActiveScreen(ScreenList[1]);
                }
                else
                {
                    Screen_SetActiveScreen(ScreenList[0]);
                }
            }
        }

        #endregion Keyboard Handling

        #region Synchronisation

        private void PrepareSync(bool bInitialization)
        {
            // Called each time the screen list change
            // or when a screen changed selection.

            // We don't care which video was updated.
            // Set sync mode and reset sync.
            _mBSynching = false;

            if ((ScreenList.Count == 2))
            {
                if ((ScreenList[0] is PlayerScreen) && (ScreenList[1] is PlayerScreen))
                {
                    if (((PlayerScreen)ScreenList[0]).Full && ((PlayerScreen)ScreenList[1]).Full)
                    {
                        _mBSynching = true;
                        ((PlayerScreen)ScreenList[0]).Synched = true;
                        ((PlayerScreen)ScreenList[1]).Synched = true;

                        if (bInitialization)
                        {
                            Log.Debug("PrepareSync() - Initialization (reset of sync point).");
                            // Static Sync
                            _mIRightSyncFrame = 0;
                            _mILeftSyncFrame = 0;
                            _mISyncLag = 0;
                            _mICurrentFrame = 0;

                            ((PlayerScreen)ScreenList[0]).SyncPosition = 0;
                            ((PlayerScreen)ScreenList[1]).SyncPosition = 0;
                            ((ScreenManagerUserInterface)Ui).UpdateSyncPosition(_mICurrentFrame);

                            // Dynamic Sync
                            ResetDynamicSyncFlags();

                            // Sync Merging
                            ((PlayerScreen)ScreenList[0]).SyncMerge = false;
                            ((PlayerScreen)ScreenList[1]).SyncMerge = false;
                            ((ScreenManagerUserInterface)Ui).ComCtrls.SyncMerging = false;
                        }

                        // Mise à jour trkFrame
                        SetSyncLimits();
                        ((ScreenManagerUserInterface)Ui).SetupTrkFrame(0, _mIMaxFrame, _mICurrentFrame);

                        // Mise à jour Players
                        OnCommonPositionChanged(_mICurrentFrame, true);

                        // debug
                        ((ScreenManagerUserInterface)Ui).DisplaySyncLag(_mISyncLag);
                    }
                    else
                    {
                        // Not all screens are loaded with videos.
                        ((PlayerScreen)ScreenList[0]).Synched = false;
                        ((PlayerScreen)ScreenList[1]).Synched = false;
                    }
                }
            }
            else
            {
                // Only one screen, or not all screens are PlayerScreens.
                switch (ScreenList.Count)
                {
                    case 1:
                        if (ScreenList[0] is PlayerScreen)
                        {
                            ((PlayerScreen)ScreenList[0]).Synched = false;
                        }
                        break;

                    case 2:
                        if (ScreenList[0] is PlayerScreen)
                        {
                            ((PlayerScreen)ScreenList[0]).Synched = false;
                        }
                        if (ScreenList[1] is PlayerScreen)
                        {
                            ((PlayerScreen)ScreenList[1]).Synched = false;
                        }
                        break;

                    default:
                        break;
                }
            }

            if (!_mBSynching)
            {
                StopDynamicSync();
                ((ScreenManagerUserInterface)Ui).DisplayAsPaused();
            }
        }

        public void SetSyncPoint(bool bIntervalOnly)
        {
            //--------------------------------------------------------------------------------------------------
            // Registers the current position of each video as its sync frame. (Optional)
            // Computes the lag in common timestamps between positions.
            // Computes the lag in milliseconds between positions. (using current framerate of each video)
            // Update current common position.
            // (public only because accessed from the Swap command.)
            //--------------------------------------------------------------------------------------------------

            //---------------------------------------------------------------------------
            // Par défaut les deux vidéos sont synchronisées sur {0}.
            // Le paramètre de synchro se lit comme suit :
            // {+2} : La vidéo de droite à 2 frames d'avance sur celle de gauche.
            // {-4} : La vidéo de droite à 4 frames de retard.
            //
            // Si le décalage est positif, la vidéo de droite doit partir en premier.
            // La pause de terminaison dépend à la fois du paramètre de synchro et
            // des durées (en frames) respectives des deux vidéos.
            //
            // Si _bIntervalOnly == true, on ne veut pas changer les frames de référence
            // (Généralement après une modification du framerate de l'une des vidéos ou swap)
            //----------------------------------------------------------------------------
            if (_mBSynching && ScreenList.Count == 2)
            {
                // Registers current positions.
                if (!bIntervalOnly)
                {
                    // For timing label only
                    ((PlayerScreen)ScreenList[0]).SyncPosition = ((PlayerScreen)ScreenList[0]).Position;
                    ((PlayerScreen)ScreenList[1]).SyncPosition = ((PlayerScreen)ScreenList[1]).Position;

                    _mILeftSyncFrame = ((PlayerScreen)ScreenList[0]).CurrentFrame;
                    _mIRightSyncFrame = ((PlayerScreen)ScreenList[1]).CurrentFrame;

                    Log.Debug(string.Format("New Sync Points:[{0}][{1}], Sync lag:{2}", _mILeftSyncFrame,
                        _mIRightSyncFrame, _mIRightSyncFrame - _mILeftSyncFrame));
                }

                // Sync Lag is expressed in frames.
                _mISyncLag = _mIRightSyncFrame - _mILeftSyncFrame;

                // We need to recompute the lag in milliseconds because it can change even when
                // the references positions don't change. For exemple when varying framerate (speed).
                var iLeftSyncMilliseconds = ((PlayerScreen)ScreenList[0]).FrameInterval * _mILeftSyncFrame;
                var iRightSyncMilliseconds = ((PlayerScreen)ScreenList[1]).FrameInterval * _mIRightSyncFrame;
                _mISyncLagMilliseconds = iRightSyncMilliseconds - iLeftSyncMilliseconds;

                // Update common position (sign of m_iSyncLag might have changed.)
                if (_mISyncLag > 0)
                {
                    _mICurrentFrame = _mIRightSyncFrame;
                }
                else
                {
                    _mICurrentFrame = _mILeftSyncFrame;
                }

                ((ScreenManagerUserInterface)Ui).UpdateSyncPosition(_mICurrentFrame);
                ((ScreenManagerUserInterface)Ui).DisplaySyncLag(_mISyncLag);
            }
        }

        private void SetSyncLimits()
        {
            //--------------------------------------------------------------------------------
            // Computes the real max of the trkFrame, considering the lag and original sizes.
            // Updates trkFrame bounds, expressed in *Frames*.
            // impact : m_iMaxFrame.
            //---------------------------------------------------------------------------------
            Log.Debug("SetSyncLimits() called.");
            int iLeftMaxFrame = ((PlayerScreen)ScreenList[0]).LastFrame;
            int iRightMaxFrame = ((PlayerScreen)ScreenList[1]).LastFrame;

            if (_mISyncLag > 0)
            {
                // Lag is positive. Right video starts first and its duration stay the same as original.
                // Left video has to wait for an ammount of time.

                // Get Lag in number of frames of left video.
                //int iSyncLagFrames = ((PlayerScreen)screenList[0]).NormalizedToFrame(m_iSyncLag);

                // Check if lag is still valid. (?)
                if (_mISyncLag > iRightMaxFrame)
                {
                    _mISyncLag = 0;
                }

                iLeftMaxFrame += _mISyncLag;
            }
            else
            {
                // Lag is negative. Left video starts first and its duration stay the same as original.
                // Right video has to wait for an ammount of time.

                // Get Lag in frames of right video
                //int iSyncLagFrames = ((PlayerScreen)screenList[1]).NormalizedToFrame(m_iSyncLag);

                // Check if lag is still valid.(?)
                if (-_mISyncLag > iLeftMaxFrame)
                {
                    _mISyncLag = 0;
                }
                iRightMaxFrame += (-_mISyncLag);
            }

            _mIMaxFrame = Math.Max(iLeftMaxFrame, iRightMaxFrame);

            //Console.WriteLine("m_iSyncLag:{0}, m_iSyncLagMilliseconds:{1}, MaxFrames:{2}", m_iSyncLag, m_iSyncLagMilliseconds, m_iMaxFrame);
        }

        private void OnCommonPositionChanged(int iFrame, bool bAllowUiUpdate)
        {
            //------------------------------------------------------------------------------
            // This is where the "static sync" is done.
            // Updates each video to reflect current common position.
            // Used to handle GotoNext, GotoPrev, trkFrame, etc.
            //
            // note: m_iSyncLag and _iFrame are expressed in frames.
            //------------------------------------------------------------------------------

            //log.Debug(String.Format("Static Sync, common position changed to {0}",_iFrame));

            // Get corresponding position in each video, in frames
            var iLeftFrame = 0;
            var iRightFrame = 0;

            if (iFrame >= 0)
            {
                if (_mISyncLag > 0)
                {
                    // Right video must go ahead.

                    iRightFrame = iFrame;
                    iLeftFrame = iFrame - _mISyncLag;
                    if (iLeftFrame < 0)
                    {
                        iLeftFrame = 0;
                    }
                }
                else
                {
                    // Left video must go ahead.

                    iLeftFrame = iFrame;
                    iRightFrame = iFrame - (-_mISyncLag);
                    if (iRightFrame < 0)
                    {
                        iRightFrame = 0;
                    }
                }

                // Force positions.
                ((PlayerScreen)ScreenList[0]).GotoFrame(iLeftFrame, bAllowUiUpdate);
                ((PlayerScreen)ScreenList[1]).GotoFrame(iRightFrame, bAllowUiUpdate);
            }
            else
            {
                // Special case for ++.
                if (_mISyncLag > 0)
                {
                    // Right video must go ahead.
                    ((PlayerScreen)ScreenList[1]).GotoNextFrame(bAllowUiUpdate);

                    if (_mICurrentFrame > _mISyncLag)
                    {
                        ((PlayerScreen)ScreenList[0]).GotoNextFrame(bAllowUiUpdate);
                    }
                }
                else
                {
                    // Left video must go ahead.
                    ((PlayerScreen)ScreenList[0]).GotoNextFrame(bAllowUiUpdate);

                    if (_mICurrentFrame > -_mISyncLag)
                    {
                        ((PlayerScreen)ScreenList[1]).GotoNextFrame(bAllowUiUpdate);
                    }
                }
            }
        }

        public void SwapSync()
        {
            if (_mBSynching && ScreenList.Count == 2)
            {
                var iTemp = _mILeftSyncFrame;
                _mILeftSyncFrame = _mIRightSyncFrame;
                _mIRightSyncFrame = iTemp;

                // Reset dynamic sync flags
                ResetDynamicSyncFlags();
            }
        }

        private void StartDynamicSync()
        {
            _mBDynamicSynching = true;
            DynamicSync();
        }

        private void StopDynamicSync()
        {
            _mBDynamicSynching = false;
        }

        private void DynamicSync()
        {
            // This is where the dynamic sync is done.
            // Get each video positions in common timebase and milliseconds.
            // Figure if a restart or pause is needed, considering current positions.

            // When the user press the common play button, we just propagate the play to the screens.
            // The common timer is just set to try to be notified of each frame change.
            // It is not used to provoke frame change itself.
            // We just start and stop the players timers when we detect one of the video has reached the end,
            // to prevent it from auto restarting.

            //-----------------------------------------------------------------------------
            // /!\ Following paragraph is obsolete when using Direct call to dynamic sync.
            // This function is executed in the WORKER THREAD.
            // nothing called from here should ultimately call in the UI thread.
            //
            // Except when using BeginInvoke.
            // But we can't use BeginInvoke here, because it's only available for Controls.
            // Calling the BeginInvoke of the PlayerScreenUI is useless because it's not the same
            // UI thread as the one used to create the menus that we will update upon SetAsActiveScreen
            //
            //-----------------------------------------------------------------------------

            // Glossary:
            // XIsStarting 		: currently on [0] but a Play was asked.
            // XIsCatchingUp 	: video is between [0] and the point where both video will be running.

            if (_mBSynching && ScreenList.Count == 2)
            {
                // Function called by timer event handler, asynchronously on each tick.

                // L'ensemble de la supervision est réalisée en TimeStamps.
                // Seul les décision de lancer / arrêter sont établies par rapport
                // au temps auquel on est.

                int iLeftPosition = ((PlayerScreen)ScreenList[0]).CurrentFrame;
                int iRightPosition = ((PlayerScreen)ScreenList[1]).CurrentFrame;
                var iLeftMilliseconds = iLeftPosition * ((PlayerScreen)ScreenList[0]).FrameInterval;
                var iRightMilliseconds = iRightPosition * ((PlayerScreen)ScreenList[1]).FrameInterval;

                //-----------------------------------------------------------------------
                // Dans cette fonction, on part du principe que les deux vidéos tournent.
                // Et on fait des 'Ensure Pause' quand nécessaire.
                // On évite les Ensure Play' car l'utilisateur a pu
                // manuellement pauser une vidéo.
                //-----------------------------------------------------------------------

                #region [i][0]

                if (iLeftPosition > 0 && iRightPosition == 0)
                {
                    EnsurePlay(0);

                    // Etat 4. [i][0]
                    _mBLeftIsStarting = false;

                    if (_mISyncLag == 0)
                    {
                        //-----------------------------------------------------
                        // La vidéo de droite
                        // - vient de boucler et on doit attendre l'autre
                        // - est en train de repartir.
                        //-----------------------------------------------------
                        if (!_mBRightIsStarting)
                        {
                            // Stop pour bouclage
                            EnsurePause(1);
                        }

                        _mICurrentFrame = iLeftPosition;
                    }
                    else if (_mISyncLagMilliseconds > 0)
                    {
                        // La vidéo de droite est sur 0 et doit partir en premier.
                        // Si elle n'est pas en train de repartir, c'est qu'on
                        // doit attendre que la vidéo de gauche ait finit son tour.
                        if (!_mBRightIsStarting)
                        {
                            EnsurePause(1);
                            _mICurrentFrame = iLeftPosition + _mISyncLag;
                        }
                        else
                        {
                            _mICurrentFrame = iLeftPosition;
                        }
                    }
                    else if (_mISyncLagMilliseconds < 0)
                    {
                        // La vidéo de droite est sur 0, en train de prendre son retard.
                        // On la relance si celle de gauche a fait son décalage.

                        // Attention, ne pas relancer si celle de gauche est en fait en train de terminer son tour
                        if (!_mBLeftIsCatchingUp && !_mBRightIsStarting)
                        {
                            EnsurePause(1);
                            _mICurrentFrame = iLeftPosition;
                        }
                        else if (iLeftMilliseconds > (-_mISyncLagMilliseconds) - 24)
                        {
                            // La vidéo de gauche est sur le point de franchir le sync point.
                            // les 24 ms supplémentaires sont pour tenir compte de l'inertie qu'à généralement
                            // la vidéo qui est partie en premier...
                            EnsurePlay(1);
                            _mBRightIsStarting = true;
                            _mBLeftIsCatchingUp = false;
                            _mICurrentFrame = iLeftPosition;
                        }
                        else
                        {
                            // La vidéo de gauche n'a pas encore fait son décalage.
                            // On ne force pas sa lecture. (Pause manuelle possible).
                            _mBLeftIsCatchingUp = true;
                            _mICurrentFrame = iLeftPosition;
                        }
                    }
                }

                #endregion [i][0]

                #region [0][0]

                else if (iLeftPosition == 0 && iRightPosition == 0)
                {
                    // Etat 1. [0][0]
                    _mICurrentFrame = 0;

                    // Les deux vidéos viennent de boucler ou sont en train de repartir.
                    if (_mISyncLag == 0)
                    {
                        //---------------------
                        // Redemmarrage commun.
                        //---------------------
                        if (!_mBLeftIsStarting && !_mBRightIsStarting)
                        {
                            EnsurePlay(0);
                            EnsurePlay(1);

                            _mBRightIsStarting = true;
                            _mBLeftIsStarting = true;
                        }
                    }
                    else if (_mISyncLagMilliseconds > 0)
                    {
                        // Redemarrage uniquement de la vidéo de droite,
                        // qui doit faire son décalage

                        EnsurePause(0);
                        EnsurePlay(1);
                        _mBRightIsStarting = true;
                        _mBRightIsCatchingUp = true;
                    }
                    else if (_mISyncLagMilliseconds < 0)
                    {
                        // Redemarrage uniquement de la vidéo de gauche,
                        // qui doit faire son décalage

                        EnsurePlay(0);
                        EnsurePause(1);
                        _mBLeftIsStarting = true;
                        _mBLeftIsCatchingUp = true;
                    }
                }

                #endregion [0][0]

                #region [0][i]

                else if (iLeftPosition == 0 && iRightPosition > 0)
                {
                    // Etat [0][i]
                    EnsurePlay(1);

                    _mBRightIsStarting = false;

                    if (_mISyncLag == 0)
                    {
                        _mICurrentFrame = iRightPosition;

                        //--------------------------------------------------------------------
                        // Configuration possible : la vidéo de gauche vient de boucler.
                        // On la stoppe en attendant le redemmarrage commun.
                        //--------------------------------------------------------------------
                        if (!_mBLeftIsStarting)
                        {
                            EnsurePause(0);
                        }
                    }
                    else if (_mISyncLagMilliseconds > 0)
                    {
                        // La vidéo de gauche est sur 0, en train de prendre son retard.
                        // On la relance si celle de droite a fait son décalage.

                        // Attention ne pas relancer si la vidéo de droite est en train de finir son tour
                        if (!_mBRightIsCatchingUp && !_mBLeftIsStarting)
                        {
                            // La vidéo de droite est en train de finir son tour tandisque celle de gauche a déjà bouclé.
                            EnsurePause(0);
                            _mICurrentFrame = iRightPosition;
                        }
                        else if (iRightMilliseconds > _mISyncLagMilliseconds - 24)
                        {
                            // La vidéo de droite est sur le point de franchir le sync point.
                            // les 24 ms supplémentaires sont pour tenir compte de l'inertie qu'à généralement
                            // la vidéo qui est partie en premier...
                            EnsurePlay(0);
                            _mBLeftIsStarting = true;
                            _mBRightIsCatchingUp = false;
                            _mICurrentFrame = iRightPosition;
                        }
                        else
                        {
                            // La vidéo de droite n'a pas encore fait son décalage.
                            // On ne force pas sa lecture. (Pause manuelle possible).
                            _mBRightIsCatchingUp = true;
                            _mICurrentFrame = iRightPosition;
                        }
                    }
                    else if (_mISyncLagMilliseconds < 0)
                    {
                        // La vidéo de gauche est sur 0 et doit partir en premier.
                        // Si elle n'est pas en train de repartir, c'est qu'on
                        // doit attendre que la vidéo de droite ait finit son tour.
                        if (!_mBLeftIsStarting)
                        {
                            EnsurePause(0);
                            _mICurrentFrame = iRightPosition + _mISyncLag;
                        }
                        else
                        {
                            // Rare, les deux première frames de chaque vidéo n'arrivent pas en même temps
                            _mICurrentFrame = iRightPosition;
                        }
                    }
                }

                #endregion [0][i]

                #region [i][i]

                else
                {
                    // Etat [i][i]
                    EnsurePlay(0);
                    EnsurePlay(1);

                    _mBLeftIsStarting = false;
                    _mBRightIsStarting = false;

                    _mICurrentFrame = Math.Max(iLeftPosition, iRightPosition);
                }

                #endregion [i][i]

                // Update position for trkFrame.
                object[] parameters = { _mICurrentFrame };
                ((ScreenManagerUserInterface)Ui).BeginInvoke(
                    ((ScreenManagerUserInterface)Ui).MDelegateUpdateTrkFrame, parameters);

                //log.Debug(String.Format("Tick:[{0}][{1}], Starting:[{2}][{3}], Catching up:[{4}][{5}]", iLeftPosition, iRightPosition, m_bLeftIsStarting, m_bRightIsStarting, m_bLeftIsCatchingUp, m_bRightIsCatchingUp));
            }
            else
            {
                // This can happen when a screen is closed on the fly while synching.
                StopDynamicSync();
                _mBSynching = false;
                ((ScreenManagerUserInterface)Ui).DisplayAsPaused();
            }
        }

        private void EnsurePause(int iScreen)
        {
            //log.Debug(String.Format("Ensuring pause of screen [{0}]", _iScreen));
            if (iScreen < ScreenList.Count)
            {
                if (((PlayerScreen)ScreenList[iScreen]).IsPlaying)
                {
                    ((PlayerScreen)ScreenList[iScreen]).MPlayerScreenUi.OnButtonPlay();
                }
            }
            else
            {
                _mBSynching = false;
                ((ScreenManagerUserInterface)Ui).DisplayAsPaused();
            }
        }

        private void EnsurePlay(int iScreen)
        {
            //log.Debug(String.Format("Ensuring play of screen [{0}]", _iScreen));
            if (iScreen < ScreenList.Count)
            {
                if (!((PlayerScreen)ScreenList[iScreen]).IsPlaying)
                {
                    ((PlayerScreen)ScreenList[iScreen]).MPlayerScreenUi.OnButtonPlay();
                }
            }
            else
            {
                _mBSynching = false;
                ((ScreenManagerUserInterface)Ui).DisplayAsPaused();
            }
        }

        private void ResetDynamicSyncFlags()
        {
            _mBRightIsStarting = false;
            _mBLeftIsStarting = false;
            _mBLeftIsCatchingUp = false;
            _mBRightIsCatchingUp = false;
        }

        private void SyncCatch()
        {
            // We sync back the videos.
            // Used when one video has been moved individually.
            Log.Debug("SyncCatch() called.");
            int iLeftFrame = ((PlayerScreen)ScreenList[0]).CurrentFrame;
            int iRightFrame = ((PlayerScreen)ScreenList[1]).CurrentFrame;

            if (_mISyncLag > 0)
            {
                // Right video goes ahead.
                if (iLeftFrame + _mISyncLag == _mICurrentFrame || (_mICurrentFrame < _mISyncLag && iLeftFrame == 0))
                {
                    // Left video wasn't moved, we'll move it according to right video.
                    _mICurrentFrame = iRightFrame;
                }
                else if (iRightFrame == _mICurrentFrame)
                {
                    // Right video wasn't moved, we'll move it according to left video.
                    _mICurrentFrame = iLeftFrame + _mISyncLag;
                }
                else
                {
                    // Both videos were moved.
                    _mICurrentFrame = iLeftFrame + _mISyncLag;
                }
            }
            else
            {
                // Left video goes ahead.
                if (iRightFrame - _mISyncLag == _mICurrentFrame || (_mICurrentFrame < -_mISyncLag && iRightFrame == 0))
                {
                    // Right video wasn't moved, we'll move it according to left video.
                    _mICurrentFrame = iLeftFrame;
                }
                else if (iLeftFrame == _mICurrentFrame)
                {
                    // Left video wasn't moved, we'll move it according to right video.
                    _mICurrentFrame = iRightFrame - _mISyncLag;
                }
                else
                {
                    // Both videos were moved.
                    _mICurrentFrame = iLeftFrame;
                }
            }

            OnCommonPositionChanged(_mICurrentFrame, true);
            ((ScreenManagerUserInterface)Ui).UpdateTrkFrame(_mICurrentFrame);
        }

        #endregion Synchronisation

        #region Screens State Recalling

        public void StoreCurrentState()
        {
            //------------------------------------------------------------------------------
            // Before we start anything messy, let's store the current state of the ViewPort
            // So we can reinstate it later in case the user change his mind.
            //-------------------------------------------------------------------------------
            _mStoredStates.Add(GetCurrentState());
        }

        public ScreenManagerState GetCurrentState()
        {
            var mState = new ScreenManagerState();

            foreach (var screen in ScreenList)
            {
                var state = new ScreenState();

                state.UniqueId = screen.UniqueId;

                if (screen is PlayerScreen)
                {
                    state.Loaded = screen.Full;
                    state.FilePath = ((PlayerScreen)screen).FilePath;

                    if (state.Loaded)
                    {
                        state.MetadataString = ((PlayerScreen)screen).FrameServer.Metadata.ToXmlString(1);
                    }
                    else
                    {
                        state.MetadataString = "";
                    }
                }
                else
                {
                    state.Loaded = false;
                    state.FilePath = "";
                    state.MetadataString = "";
                }
                mState.ScreenList.Add(state);
            }

            return mState;
        }

        public void RecallState()
        {
            //-------------------------------------------------
            // Reconfigure the ViewPort to match the old state.
            // Reload the right movie with its meta data.
            //-------------------------------------------------

            if (_mStoredStates.Count > 0)
            {
                var iLastState = _mStoredStates.Count - 1;
                var cm = CommandManager.Instance();
                ICommand css = new CommandShowScreens(this);

                var currentState = GetCurrentState();

                switch (currentState.ScreenList.Count)
                {
                    case 0:
                        //-----------------------------
                        // Il y a actuellement 0 écran.
                        //-----------------------------
                        switch (_mStoredStates[iLastState].ScreenList.Count)
                        {
                            case 0:
                                // Il n'y en avait aucun : Ne rien faire.
                                break;

                            case 1:
                                {
                                    // Il y en avait un : Ajouter l'écran.
                                    ReinstateScreen(_mStoredStates[iLastState].ScreenList[0], 0, currentState);
                                    CommandManager.LaunchCommand(css);
                                    break;
                                }
                            case 2:
                                {
                                    // Ajouter les deux écrans, on ne se préoccupe pas trop de l'ordre
                                    ReinstateScreen(_mStoredStates[iLastState].ScreenList[0], 0, currentState);
                                    ReinstateScreen(_mStoredStates[iLastState].ScreenList[1], 1, currentState);
                                    CommandManager.LaunchCommand(css);
                                    break;
                                }
                            default:
                                break;
                        }
                        break;

                    case 1:
                        //-----------------------------
                        // Il y a actuellement 1 écran.
                        //-----------------------------
                        switch (_mStoredStates[iLastState].ScreenList.Count)
                        {
                            case 0:
                                {
                                    // Il n'y en avait aucun : Supprimer l'écran.
                                    RemoveScreen(0, false);
                                    CommandManager.LaunchCommand(css);
                                    break;
                                }
                            case 1:
                                {
                                    // Il y en avait un : Remplacer si besoin.
                                    ReinstateScreen(_mStoredStates[iLastState].ScreenList[0], 0, currentState);
                                    CommandManager.LaunchCommand(css);
                                    break;
                                }
                            case 2:
                                {
                                    // Il y avait deux écran : Comparer chaque ancien écran avec le restant.
                                    var iMatchingScreen = -1;
                                    var i = 0;
                                    while ((iMatchingScreen == -1) && (i < _mStoredStates[iLastState].ScreenList.Count))
                                    {
                                        if (_mStoredStates[iLastState].ScreenList[i].UniqueId ==
                                            currentState.ScreenList[0].UniqueId)
                                        {
                                            iMatchingScreen = i;
                                        }
                                        else
                                        {
                                            i++;
                                        }
                                    }

                                    switch (iMatchingScreen)
                                    {
                                        case -1:
                                            {
                                                // No matching screen found
                                                ReinstateScreen(_mStoredStates[iLastState].ScreenList[0], 0, currentState);
                                                ReinstateScreen(_mStoredStates[iLastState].ScreenList[1], 1, currentState);
                                                break;
                                            }
                                        case 0:
                                            {
                                                // the old 0 is the new 0, the old 1 doesn't exist yet.
                                                ReinstateScreen(_mStoredStates[iLastState].ScreenList[1], 1, currentState);
                                                break;
                                            }
                                        case 1:
                                            {
                                                // the old 1 is the new 0, the old 0 doesn't exist yet.
                                                ReinstateScreen(_mStoredStates[iLastState].ScreenList[0], 1, currentState);
                                                break;
                                            }
                                        default:
                                            break;
                                    }
                                    CommandManager.LaunchCommand(css);
                                    break;
                                }
                            default:
                                break;
                        }
                        break;

                    case 2:
                        // Il y a actuellement deux écrans.
                        switch (_mStoredStates[iLastState].ScreenList.Count)
                        {
                            case 0:
                                {
                                    // Il n'yen avait aucun : supprimer les deux.
                                    RemoveScreen(1, false);
                                    RemoveScreen(0, false);
                                    CommandManager.LaunchCommand(css);
                                    break;
                                }
                            case 1:
                                {
                                    // Il y en avait un : le rechercher parmi les nouveaux.
                                    var iMatchingScreen = -1;
                                    var i = 0;
                                    while ((iMatchingScreen == -1) && (i < currentState.ScreenList.Count))
                                    {
                                        if (_mStoredStates[iLastState].ScreenList[0].UniqueId ==
                                            currentState.ScreenList[i].UniqueId)
                                        {
                                            iMatchingScreen = i;
                                        }

                                        i++;
                                    }

                                    switch (iMatchingScreen)
                                    {
                                        case -1:
                                            // L'ancien écran n'a pas été retrouvé.
                                            // On supprime tout et on le rajoute.
                                            RemoveScreen(1, false);
                                            ReinstateScreen(_mStoredStates[iLastState].ScreenList[0], 0, currentState);
                                            break;

                                        case 0:
                                            // L'ancien écran a été retrouvé dans l'écran [0]
                                            // On supprime le second.
                                            RemoveScreen(1, false);
                                            break;

                                        case 1:
                                            // L'ancien écran a été retrouvé dans l'écran [1]
                                            // On supprime le premier.
                                            RemoveScreen(0, false);
                                            break;

                                        default:
                                            break;
                                    }
                                    CommandManager.LaunchCommand(css);
                                    break;
                                }
                            case 2:
                                {
                                    // Il y avait deux écrans également : Rechercher chacun parmi les nouveaux.
                                    var iMatchingScreen = new int[2];
                                    iMatchingScreen[0] = -1;
                                    iMatchingScreen[1] = -1;
                                    var i = 0;
                                    while (i < currentState.ScreenList.Count)
                                    {
                                        if (_mStoredStates[iLastState].ScreenList[0].UniqueId ==
                                            currentState.ScreenList[i].UniqueId)
                                        {
                                            iMatchingScreen[0] = i;
                                        }
                                        else if (_mStoredStates[iLastState].ScreenList[1].UniqueId ==
                                                 currentState.ScreenList[i].UniqueId)
                                        {
                                            iMatchingScreen[1] = i;
                                        }

                                        i++;
                                    }

                                    switch (iMatchingScreen[0])
                                    {
                                        case -1:
                                            {
                                                // => L'ancien écran [0] n'a pas été retrouvé.
                                                switch (iMatchingScreen[1])
                                                {
                                                    case -1:
                                                        {
                                                            // Aucun écran n'a été retrouvé.
                                                            ReinstateScreen(_mStoredStates[iLastState].ScreenList[0], 0,
                                                                currentState);
                                                            ReinstateScreen(_mStoredStates[iLastState].ScreenList[1], 1,
                                                                currentState);
                                                            break;
                                                        }
                                                    case 0:
                                                        {
                                                            // Ecran 0 non retrouvé, écran 1 retrouvé dans le 0.
                                                            // Remplacer l'écran 1 par l'ancien 0.
                                                            ReinstateScreen(_mStoredStates[iLastState].ScreenList[0], 1,
                                                                currentState);
                                                            break;
                                                        }
                                                    case 1:
                                                        {
                                                            // Ecran 0 non retrouvé, écran 1 retrouvé dans le 1.
                                                            // Remplacer l'écran 0.
                                                            ReinstateScreen(_mStoredStates[iLastState].ScreenList[0], 0,
                                                                currentState);
                                                            break;
                                                        }
                                                    default:
                                                        break;
                                                }
                                                break;
                                            }
                                        case 0:
                                            {
                                                // L'ancien écran [0] a été retrouvé dans l'écran [0]
                                                switch (iMatchingScreen[1])
                                                {
                                                    case -1:
                                                        {
                                                            // Ecran 0 retrouvé dans le [0], écran 1 non retrouvé.
                                                            ReinstateScreen(_mStoredStates[iLastState].ScreenList[1], 1,
                                                                currentState);
                                                            break;
                                                        }
                                                    case 0:
                                                        {
                                                            // Ecran 0 retrouvé dans le [0], écran 1 retrouvé dans le [0].
                                                            // Impossible.
                                                            break;
                                                        }
                                                    case 1:
                                                        {
                                                            // Ecran 0 retrouvé dans le [0], écran 1 retrouvé dans le [1].
                                                            // rien à faire.
                                                            break;
                                                        }
                                                    default:
                                                        break;
                                                }
                                                break;
                                            }
                                        case 1:
                                            {
                                                // L'ancien écran [0] a été retrouvé dans l'écran [1]
                                                switch (iMatchingScreen[1])
                                                {
                                                    case -1:
                                                        {
                                                            // Ecran 0 retrouvé dans le [1], écran 1 non retrouvé.
                                                            ReinstateScreen(_mStoredStates[iLastState].ScreenList[1], 0,
                                                                currentState);
                                                            break;
                                                        }
                                                    case 0:
                                                        {
                                                            // Ecran 0 retrouvé dans le [1], écran 1 retrouvé dans le [0].
                                                            // rien à faire (?)
                                                            break;
                                                        }
                                                    case 1:
                                                        {
                                                            // Ecran 0 retrouvé dans le [1], écran 1 retrouvé dans le [1].
                                                            // Impossible
                                                            break;
                                                        }
                                                    default:
                                                        break;
                                                }
                                                break;
                                            }
                                        default:
                                            break;
                                    }
                                    CommandManager.LaunchCommand(css);
                                    break;
                                }
                            default:
                                break;
                        }
                        break;

                    default:
                        break;
                }

                // Once we have made such a recall, the Redo menu must be disabled...
                cm.BlockRedo();

                UpdateCaptureBuffers();

                // Mettre à jour menus et Status bar
                UpdateStatusBar();
                OrganizeCommonControls();
                OrganizeMenus();

                _mStoredStates.RemoveAt(iLastState);
            }
        }

        private void ReinstateScreen(ScreenState oldScreen, int iNewPosition, ScreenManagerState currentState)
        {
            var cm = CommandManager.Instance();

            if (iNewPosition > currentState.ScreenList.Count - 1)
            {
                // We need a new screen.
                ICommand caps = new CommandAddPlayerScreen(this, false);
                CommandManager.LaunchCommand(caps);

                if (oldScreen.Loaded)
                {
                    ReloadScreen(oldScreen, iNewPosition + 1);
                }
            }
            else
            {
                if (oldScreen.Loaded)
                {
                    ReloadScreen(oldScreen, iNewPosition + 1);
                }
                else if (currentState.ScreenList[iNewPosition].Loaded)
                {
                    // L'ancien n'est pas chargé mais le nouveau l'est.
                    // => unload movie.
                    RemoveScreen(iNewPosition, false);

                    ICommand caps = new CommandAddPlayerScreen(this, false);
                    CommandManager.LaunchCommand(caps);
                }
            }
        }

        private bool RemoveScreen(int iPosition, bool bStoreState)
        {
            ICommand crs = new CommandRemoveScreen(this, iPosition, bStoreState);
            CommandManager.LaunchCommand(crs);

            var cancelled = CancelLastCommand;
            if (cancelled)
            {
                var cm = CommandManager.Instance();
                cm.UnstackLastCommand();
                CancelLastCommand = false;
            }

            return !cancelled;
        }

        private void ReloadScreen(ScreenState oldScreen, int iNewPosition)
        {
            if (File.Exists(oldScreen.FilePath))
            {
                // We instantiate and launch it like a simple command (not undoable).
                ICommand clmis = new CommandLoadMovieInScreen(this, oldScreen.FilePath, iNewPosition, false);
                CommandManager.LaunchCommand(clmis);

                // Check that everything went well
                // Potential problem : the video was deleted between do and undo.
                // _iNewPosition should always point to a valid position here.
                if (ScreenList[iNewPosition - 1].Full)
                {
                    PlayerScreen ps = MActiveScreen as PlayerScreen;
                    if (ps != null)
                    {
                        ps.FrameServer.Metadata.Load(oldScreen.MetadataString, false);
                        ps.MPlayerScreenUi.PostImportMetadata();
                    }
                }
            }
        }

        #endregion Screens State Recalling
    }
}