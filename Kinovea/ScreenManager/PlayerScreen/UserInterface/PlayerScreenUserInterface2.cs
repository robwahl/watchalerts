#region License

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

#endregion License

#region Using directives

using Kinovea.ScreenManager.Languages;
using Kinovea.ScreenManager.Properties;
using Kinovea.Services;
using Kinovea.VideoFiles;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Image = AForge.Imaging.Image;

#endregion Using directives

namespace Kinovea.ScreenManager
{
    public partial class PlayerScreenUserInterface : UserControl
    {
        #region Constructor

        public PlayerScreenUserInterface(FrameServerPlayer frameServer, IPlayerScreenUiHandler playerScreenUiHandler)
        {
            Log.Debug("Constructing the PlayerScreen user interface.");

            _mPlayerScreenUiHandler = playerScreenUiHandler;
            _mFrameServer = frameServer;
            _mFrameServer.Metadata = new Metadata(TimeStampsToTimecode, OnShowClosestFrame);

            InitializeComponent();
            BuildContextMenus();
            InitializeDrawingTools();
            SyncSetAlpha(0.5f);
            _mMessageToaster = new MessageToaster(pbSurfaceScreen);

            var clam = CommandLineArgumentManager.Instance();
            if (!clam.SpeedConsumed)
            {
                sldrSpeed.Value = clam.SpeedPercentage;
                clam.SpeedConsumed = true;
            }

            // Most members and controls should be initialized with the right value.
            // So we don't need to do an extra ResetData here.

            // Controls that renders differently between run time and design time.
            Dock = DockStyle.Fill;
            ShowHideResizers(false);
            SetupPrimarySelectionPanel();
            SetupKeyframeCommentsHub();
            pnlThumbnails.Controls.Clear();
            DockKeyframePanel(true);

            // Internal delegates
            _mTimerEventHandler = MultimediaTimer_Tick;
            _mPlayLoop = PlayLoop_Invoked;

            _mDeselectionTimer.Interval = 3000;
            _mDeselectionTimer.Tick += DeselectionTimer_OnTick;

            EnableDisableActions(false);
            //SetupDebugPanel();
        }

        #endregion Constructor

        #region Importing selection to memory

        public void ImportSelectionToMemory(bool bForceReload)
        {
            //-------------------------------------------------------------------------------------
            // Switch the current selection to memory if possible.
            // Called at video load after first frame load, recalling a screen memo on undo,
            // and when the user manually modifies the selection.
            // At this point the selection sentinels (m_iSelStart and m_iSelEnd) must be good.
            // They would have been positionned from file data or from trkSelection pixel mapping.
            // The internal data of the trkSelection should also already have been updated.
            //
            // After the selection is imported, we may have different values than before
            // regarding selection sentinels because:
            // - the video ending timestamp may have been misadvertised in the file,
            // - the timestamps may not be linear so the mapping with the trkSelection isn't perfect.
            // We check and fix these discrepancies.
            //
            // Public because accessed from PlayerServer.Deinterlace property
            //-------------------------------------------------------------------------------------
            if (_mFrameServer.VideoFile.Loaded)
            {
                if (_mFrameServer.VideoFile.CanExtractToMemory(_mISelStart, _mISelEnd, _mPrefManager.WorkingZoneSeconds,
                    _mPrefManager.WorkingZoneMemory))
                {
                    StopPlaying();
                    _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();

                    var ffi = new FormFramesImport(_mFrameServer.VideoFile, _mISelStart, _mISelEnd, bForceReload);
                    ffi.ShowDialog();

                    if (_mFrameServer.VideoFile.Selection.iAnalysisMode == 0)
                    {
                        // It didn't work. (Operation canceled, or failed).
                        Log.Debug("Extract to memory canceled or failed, reload first frame.");
                        _mIFramesToDecode = 1;
                        ShowNextFrame(_mISelStart, true);
                        UpdateNavigationCursor();
                    }

                    ffi.Dispose();
                }
                else if (_mFrameServer.VideoFile.Selection.iAnalysisMode == 1)
                {
                    // Exiting Analysis mode.
                    // TODO - free memory for images now ?
                    _mFrameServer.VideoFile.Selection.iAnalysisMode = 0;
                }

                // Here, we may have changed mode.
                if (_mFrameServer.VideoFile.Selection.iAnalysisMode == 1)
                {
                    // We now have solid facts. Update all variables with them.
                    _mISelStart = _mFrameServer.VideoFile.GetTimeStamp(0);
                    _mISelEnd =
                        _mFrameServer.VideoFile.GetTimeStamp(_mFrameServer.VideoFile.Selection.iDurationFrame - 1);
                    var fAverageTimeStampsPerFrame = _mFrameServer.VideoFile.Infos.fAverageTimeStampsPerSeconds/
                                                     _mFrameServer.VideoFile.Infos.fFps;
                    SelectionDuration = (long) (_mISelEnd - _mISelStart + fAverageTimeStampsPerFrame);

                    if (trkSelection.SelStart != _mISelStart) trkSelection.SelStart = _mISelStart;
                    if (trkSelection.SelEnd != _mISelEnd) trkSelection.SelEnd = _mISelEnd;

                    // Remap frame tracker with solid data.
                    trkFrame.Remap(_mISelStart, _mISelEnd);
                    trkFrame.ReportOnMouseMove = true;

                    // Display first frame.
                    _mIFramesToDecode = 1;
                    ShowNextFrame(_mISelStart, true);
                    UpdateNavigationCursor();
                }
                else
                {
                    /*
                    m_iSelStart = trkSelection.SelStart;
                    // Hack : If we changed the trkSelection.SelEnd before the trkSelection.SelStart
                    // (As we do when we first load the video), the selstart will not take into account
                    // a possible shift of unreadable first frames.
                    // We make the ad-hoc modif here.
                    if (m_iSelStart < m_iStartingPosition) m_iSelStart = m_iStartingPosition;

                    m_iSelEnd = trkSelection.SelEnd;
                     */

                    var fAverageTimeStampsPerFrame = _mFrameServer.VideoFile.Infos.fAverageTimeStampsPerSeconds/
                                                     _mFrameServer.VideoFile.Infos.fFps;
                    SelectionDuration = (long) (_mISelEnd - _mISelStart + fAverageTimeStampsPerFrame);

                    // Remap frame tracker.
                    trkFrame.Remap(_mISelStart, _mISelEnd);
                    trkFrame.ReportOnMouseMove = false;
                }

                UpdateSelectionLabels();
                OnPoke();

                _mPlayerScreenUiHandler.PlayerScreenUI_SelectionChanged(true);
                if (_mBShowInfos)
                {
                    UpdateDebugInfos();
                }
            }
        }

        #endregion Importing selection to memory

        #region Enums

        private enum PlayingMode
        {
            Once,
            Loop,
            Bounce
        }

        #endregion Enums

        #region Imports Win32

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern uint timeSetEvent(int msDelay, int msResolution, TimerEventHandler handler,
            ref int userCtx, int eventType);

        [DllImport("winmm.dll", SetLastError = true)]
        private static extern uint timeKillEvent(uint timerEventId);

        private const int TimePeriodic = 0x01;
        private const int TimeKillSynchronous = 0x0100;

        #endregion Imports Win32

        #region Internal delegates for async methods

        private delegate void TimerEventHandler(uint id, uint msg, ref int userCtx, int rsv1, int rsv2);

        private delegate void PlayLoop();

        private readonly TimerEventHandler _mTimerEventHandler;
        private readonly PlayLoop _mPlayLoop;

        #endregion Internal delegates for async methods

        #region Properties

        public bool IsCurrentlyPlaying { get; private set; }

        public int DrawtimeFilterType
        {
            get
            {
                if (_mBDrawtimeFiltered)
                {
                    return _mDrawingFilterOutput.VideoFilterType;
                }
                return -1;
            }
        }

        public double FrameInterval
        {
            get { return (_mFrameServer.VideoFile.Infos.fFrameInterval/(_mFSlowmotionPercentage/100)); }
        }

        public double RealtimePercentage
        {
            // RealtimePercentage expresses the speed percentage relative to real time action.
            // It takes high speed camera into account.
            get { return _mFSlowmotionPercentage/_mFHighSpeedFactor; }
            set
            {
                // This happens only in the context of synching
                // when the other video changed its speed percentage (user or forced).
                // We must NOT trigger the event here, or it will impact the other screen in an infinite loop.
                // Compute back the slow motion percentage relative to the playback framerate.
                var fPlaybackPercentage = value*_mFHighSpeedFactor;
                if (fPlaybackPercentage > 200) fPlaybackPercentage = 200;
                sldrSpeed.Value = (int) fPlaybackPercentage;

                // If the other screen is in high speed context, we honor the decimal value.
                // (When it will be changed from this screen's slider, it will be an integer value).
                _mFSlowmotionPercentage = fPlaybackPercentage > 0 ? fPlaybackPercentage : 1;

                // Reset timer with new value.
                if (IsCurrentlyPlaying)
                {
                    StopMultimediaTimer();
                    StartMultimediaTimer(GetPlaybackFrameInterval());
                }

                UpdateSpeedLabel();
            }
        }

        public bool Synched
        {
            //get { return m_bSynched; }
            set
            {
                _mBSynched = value;

                if (!_mBSynched)
                {
                    _mISyncPosition = 0;
                    trkFrame.UpdateSyncPointMarker(_mISyncPosition);
                    UpdateCurrentPositionLabel();

                    _mBSyncMerge = false;
                    if (_mSyncMergeImage != null)
                        _mSyncMergeImage.Dispose();
                }

                buttonPlayingMode.Enabled = !_mBSynched;
            }
        }

        public long SelectionDuration { // The duration of the selection in ts.
            get; private set; } = 100;

        public long SyncPosition
        {
            // The absolute ts of the sync point for this video.
            get { return _mISyncPosition; }
            set
            {
                _mISyncPosition = value;
                trkFrame.UpdateSyncPointMarker(_mISyncPosition);
                UpdateCurrentPositionLabel();
            }
        }

        public long SyncCurrentPosition
        {
            // The current ts, relative to the selection.
            get { return _mICurrentPosition - _mISelStart; }
        }

        public bool SyncMerge
        {
            // Idicates whether we should draw the other screen image on top of this one.
            get { return _mBSyncMerge; }
            set
            {
                _mBSyncMerge = value;

                _mFrameServer.CoordinateSystem.FreeMove = _mBSyncMerge;

                if (!_mBSyncMerge && _mSyncMergeImage != null)
                {
                    _mSyncMergeImage.Dispose();
                }

                DoInvalidate();
            }
        }

        public bool DualSaveInProgress
        {
            set { _mDualSaveInProgress = value; }
        }

        #endregion Properties

        #region Members

        private readonly IPlayerScreenUiHandler _mPlayerScreenUiHandler;
        private readonly FrameServerPlayer _mFrameServer;

        // General
        private readonly PreferencesManager _mPrefManager = PreferencesManager.Instance();

        // Playback current state

        private int _mIFramesToDecode = 1;
        private bool _mBSeekToStart;
        private uint _mIdMultimediaTimer;
        private PlayingMode _mEPlayingMode = PlayingMode.Loop;
        private int _mIDroppedFrames; // For debug purposes only.
        private int _mIDecodedFrames;

        private double _mFSlowmotionPercentage = 100.0f;
            // Always between 1 and 200 : this specific value is not impacted by high speed cameras.

        private bool _mBIsIdle = true;

        // Synchronisation
        private bool _mBSynched;

        private long _mISyncPosition;
        private bool _mBSyncMerge;
        private Bitmap _mSyncMergeImage;
        private readonly ColorMatrix _mSyncMergeMatrix = new ColorMatrix();
        private readonly ImageAttributes _mSyncMergeImgAttr = new ImageAttributes();
        private bool _mDualSaveInProgress;

        // Image
        private bool _mBStretchModeOn;

        private bool _mBShowImageBorder;
        private static readonly Pen MPenImageBorder = Pens.SteelBlue;

        // Selection (All values in TimeStamps)
        // trkSelection.minimum and maximum are also in absolute timestamps.
        private long _mITotalDuration = 100;

        private long _mISelStart; // Valeur absolue, par défaut égale à m_iStartingPosition. (pas 0)
        private long _mISelEnd = 99; // Value absolue
        private long _mICurrentPosition; // Valeur absolue dans l'ensemble des timestamps.
        private long _mIStartingPosition; // Valeur absolue correspond au timestamp de la première frame.
        private bool _mBHandlersLocked;

        // Keyframes, Drawings, etc.
        private int _mIActiveKeyFrameIndex = -1; // The index of the keyframe we are on, or -1 if not a KF.

        private AbstractDrawingTool _mActiveTool;
        private DrawingToolPointer _mPointerTool;

        private FormKeyframeComments _mKeyframeCommentsHub;
        private bool _mBDocked = true;
        private bool _mBTextEdit;
        private Point _mDescaledMouse;

        // Video Filters Management
        private bool _mBDrawtimeFiltered;

        private DrawtimeFilterOutput _mDrawingFilterOutput;

        // Others
        private double _mFHighSpeedFactor = 1.0f; // When capture fps is different from Playing fps.

        private readonly Timer _mDeselectionTimer = new Timer();
        private readonly MessageToaster _mMessageToaster;

        #region Context Menus

        private readonly ContextMenuStrip _popMenu = new ContextMenuStrip();
        private readonly ToolStripMenuItem _mnuDirectTrack = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuPlayPause = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuSetCaptureSpeed = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuSavePic = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuSendPic = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuCloseScreen = new ToolStripMenuItem();

        private readonly ContextMenuStrip _popMenuDrawings = new ContextMenuStrip();
        private readonly ToolStripMenuItem _mnuConfigureDrawing = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuConfigureFading = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuConfigureOpacity = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuTrackTrajectory = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuGotoKeyframe = new ToolStripMenuItem();
        private readonly ToolStripSeparator _mnuSepDrawing = new ToolStripSeparator();
        private readonly ToolStripSeparator _mnuSepDrawing2 = new ToolStripSeparator();
        private readonly ToolStripMenuItem _mnuDeleteDrawing = new ToolStripMenuItem();

        private readonly ContextMenuStrip _popMenuTrack = new ContextMenuStrip();
        private readonly ToolStripMenuItem _mnuRestartTracking = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuStopTracking = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuDeleteTrajectory = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuDeleteEndOfTrajectory = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuConfigureTrajectory = new ToolStripMenuItem();

        private readonly ContextMenuStrip _popMenuChrono = new ContextMenuStrip();
        private readonly ToolStripMenuItem _mnuChronoStart = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuChronoStop = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuChronoHide = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuChronoCountdown = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuChronoDelete = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuChronoConfigure = new ToolStripMenuItem();

        private readonly ContextMenuStrip _popMenuMagnifier = new ContextMenuStrip();
        private readonly ToolStripMenuItem _mnuMagnifier150 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifier175 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifier200 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifier225 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifier250 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifierDirect = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifierQuit = new ToolStripMenuItem();

        #endregion Context Menus

        private ToolStripButton _mBtnAddKeyFrame;
        private ToolStripButton _mBtnShowComments;
        private ToolStripButton _mBtnToolPresets;

        // Debug
        private bool _mBShowInfos;

        private Stopwatch _mStopwatch = new Stopwatch();
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Public Methods

        public void ResetToEmptyState()
        {
            // Called when we load a new video over an already loaded screen.
            // also recalled if the video loaded but the first frame cannot be displayed.

            Log.Debug("Reset screen to empty state.");

            // 1. Reset all data.
            _mFrameServer.Unload();
            ResetData();

            // 2. Reset all interface.
            ShowHideResizers(false);
            SetupPrimarySelectionPanel();
            pnlThumbnails.Controls.Clear();
            DockKeyframePanel(true);
            UpdateFramesMarkers();
            trkFrame.UpdateSyncPointMarker(_mISyncPosition);
            EnableDisableAllPlayingControls(true);
            EnableDisableDrawingTools(true);
            EnableDisableSnapshot(true);
            buttonPlay.Image = Resources.liqplay17;
            sldrSpeed.Value = 100;
            sldrSpeed.Enabled = false;
            lblFileName.Text = "";
            _mKeyframeCommentsHub.Hide();
            UpdatePlayingModeButton();

            _mPlayerScreenUiHandler.PlayerScreenUI_Reset();
        }

        public void EnableDisableActions(bool bEnable)
        {
            // Called back after a load error.
            // Prevent any actions.
            if (!bEnable)
                DisablePlayAndDraw();

            EnableDisableSnapshot(bEnable);
            EnableDisableDrawingTools(bEnable);

            if (bEnable && _mFrameServer.Loaded && _mFrameServer.VideoFile.Infos.iDurationTimeStamps == 1)
            {
                // If we are in the special case of a one-frame video, disable playback controls.
                EnableDisableAllPlayingControls(false);
            }
            else
            {
                EnableDisableAllPlayingControls(bEnable);
            }
        }

        public int PostLoadProcess()
        {
            //---------------------------------------------------------------------------
            // Configure the interface according to he video and try to read first frame.
            // Called from CommandLoadMovie when VideoFile.Load() is successful.
            //---------------------------------------------------------------------------

            var iPostLoadResult = 0;

            // By default the filename of metadata will be the one of the video.
            _mFrameServer.Metadata.FullPath = _mFrameServer.VideoFile.FilePath;

            // Try to get MetaData from file.
            DemuxMetadata();

            // Try to display first frame.
            var readFrameResult = ShowNextFrame(-1, true);
            UpdateNavigationCursor();

            if (readFrameResult != ReadResult.Success)
            {
                iPostLoadResult = -1;
                _mFrameServer.Unload();
                Log.Error("First frame couldn't be loaded - aborting");
            }
            else
            {
                Log.Debug(string.Format("Timestamp after loading first frame : {0}", _mICurrentPosition));

                if (_mICurrentPosition < 0)
                {
                    // First frame loaded but inconsistency. (Seen with some AVCHD)
                    Log.Error(string.Format("First frame loaded but negative timestamp ({0}) - aborting",
                        _mICurrentPosition));
                    iPostLoadResult = -2;
                    _mFrameServer.Unload();
                }
                else
                {
                    //---------------------------------------------------------------------------------------
                    // First frame loaded finely.
                    //
                    // We will now update the internal data of the screen ui and
                    // set up the various child controls (like the timelines).
                    // Call order matters.
                    // Some bugs come from variations between what the file infos advertised
                    // and the reality.
                    // We fix what we can with the help of data read from the first frame or
                    // from the analysis mode switch if successful.
                    //---------------------------------------------------------------------------------------

                    iPostLoadResult = 0;
                    DoInvalidate();

                    //--------------------------------------------------------
                    // 1. Internal data : timestamps. Controls : trkSelection.
                    //
                    // - Set tentatives timestamps from infos read in the file and first frame load.
                    // - Try to switch to analysis mode.
                    // - Update the tentative timestamps with more accurate data gotten from analysis mode.
                    //--------------------------------------------------------

                    //-----------------------------------------------------------------------------
                    // [2008-04-26] Time stamp non 0 :Assez courant en fait.
                    // La première frame peut avoir un timestamp à 1 au lieu de 0 selon l'encodeur.
                    // Sans que cela soit répercuté sur iFirstTimeStamp...
                    // On fixe à la main.
                    //-----------------------------------------------------------------------------
                    _mFrameServer.VideoFile.Infos.iFirstTimeStamp = _mICurrentPosition;
                    _mIStartingPosition = _mICurrentPosition;
                    _mITotalDuration = _mFrameServer.VideoFile.Infos.iDurationTimeStamps;

                    var fAverageTimeStampsPerFrame = _mFrameServer.VideoFile.Infos.fAverageTimeStampsPerSeconds/
                                                     _mFrameServer.VideoFile.Infos.fFps;
                    _mISelStart = _mIStartingPosition;
                    _mISelEnd = (long) (_mITotalDuration + _mIStartingPosition - fAverageTimeStampsPerFrame);
                    SelectionDuration = _mITotalDuration;

                    // TODO Remove following call
                    //trkSelection.UpdateInternalState(m_iSelStart, m_iSelEnd, m_iSelStart, m_iSelEnd, m_iSelStart);

                    // Switch to analysis mode if possible.
                    // This will update the selection sentinels (m_iSelStart, m_iSelEnd) with more robust data.
                    ImportSelectionToMemory(false);

                    _mICurrentPosition = _mISelStart;
                    _mFrameServer.VideoFile.Infos.iFirstTimeStamp = _mICurrentPosition;
                    _mIStartingPosition = _mICurrentPosition;
                    _mITotalDuration = SelectionDuration;

                    // Update the control.
                    // FIXME - already done in ImportSelectionToMemory ?
                    SetupPrimarySelectionPanel();

                    //---------------------------------------------------
                    // 2. Other various infos.
                    //---------------------------------------------------
                    _mIDecodedFrames = 1;
                    _mIDroppedFrames = 0;
                    _mBSeekToStart = false;

                    _mFrameServer.SetupMetadata();
                    _mPointerTool.SetImageSize(_mFrameServer.Metadata.ImageSize);

                    UpdateFilenameLabel();
                    sldrSpeed.Enabled = true;

                    //---------------------------------------------------
                    // 3. Screen position and size.
                    //---------------------------------------------------
                    _mFrameServer.CoordinateSystem.SetOriginalSize(_mFrameServer.Metadata.ImageSize);
                    _mFrameServer.CoordinateSystem.ReinitZoom();
                    SetUpForNewMovie();
                    _mKeyframeCommentsHub.UserActivated = false;

                    //------------------------------------------------------------
                    // 4. If metadata demux failed,
                    // check if there is an brother analysis file in the directory
                    //------------------------------------------------------------
                    if (!_mFrameServer.Metadata.HasData)
                    {
                        LookForLinkedAnalysis();
                    }

                    // Check if there is a startup kva
                    var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Kinovea\\";
                    var startupFile = folder + "\\playback.kva";
                    if (File.Exists(startupFile))
                    {
                        _mFrameServer.Metadata.Load(startupFile, true);
                    }

                    // Do the post import whether the data come from external file or included .
                    if (_mFrameServer.Metadata.HasData)
                    {
                        _mFrameServer.Metadata.CleanupHash();
                        PostImportMetadata();
                    }

                    UpdateFramesMarkers();

                    // Debug
                    if (_mBShowInfos)
                    {
                        UpdateDebugInfos();
                    }
                }
            }

            return iPostLoadResult;
        }

        public void PostImportMetadata()
        {
            //----------------------------------------------------------
            // Analysis file or stream was imported into metadata.
            // Now we need to load each frames and do some scaling.
            //
            // Public because accessed from :
            // 	ScreenManager upon loading standalone analysis.
            //----------------------------------------------------------

            // TODO - progress bar ?

            var iOutOfRange = -1;
            var iCurrentKeyframe = -1;

            foreach (var kf in _mFrameServer.Metadata.Keyframes)
            {
                iCurrentKeyframe++;

                if (kf.Position <
                    (_mFrameServer.VideoFile.Infos.iFirstTimeStamp + _mFrameServer.VideoFile.Infos.iDurationTimeStamps))
                {
                    // Goto frame.
                    _mIFramesToDecode = 1;
                    ShowNextFrame(kf.Position, true);
                    UpdateNavigationCursor();
                    UpdateCurrentPositionLabel();
                    trkSelection.SelPos = trkFrame.Position;

                    // Readjust and complete the Keyframe
                    kf.Position = _mICurrentPosition;
                    kf.ImportImage(_mFrameServer.VideoFile.CurrentImage);
                    kf.GenerateDisabledThumbnail();

                    // EditBoxes
                    foreach (AbstractDrawing ad in kf.Drawings)
                    {
                        if (ad is DrawingText)
                        {
                            ((DrawingText) ad).ContainerScreen = pbSurfaceScreen;
                            panelCenter.Controls.Add(((DrawingText) ad).EditBox);
                            ((DrawingText) ad).EditBox.BringToFront();
                        }
                    }
                }
                else
                {
                    // TODO - Alert box to inform that some images couldn't be matched.
                    if (iOutOfRange < 0)
                    {
                        iOutOfRange = iCurrentKeyframe;
                    }
                }
            }

            if (iOutOfRange != -1)
            {
                // Some keyframes were out of range. remove them.
                _mFrameServer.Metadata.Keyframes.RemoveRange(iOutOfRange,
                    _mFrameServer.Metadata.Keyframes.Count - iOutOfRange);
            }

            UpdateFilenameLabel();
            OrganizeKeyframes();
            if (_mFrameServer.Metadata.Count > 0)
            {
                DockKeyframePanel(false);
            }

            // Goto selection start and refresh.
            _mIFramesToDecode = 1;
            ShowNextFrame(_mISelStart, true);
            UpdateNavigationCursor();
            ActivateKeyframe(_mICurrentPosition);

            _mFrameServer.SetupMetadata();
            _mPointerTool.SetImageSize(_mFrameServer.Metadata.ImageSize);

            DoInvalidate();
        }

        public void DisplayAsActiveScreen(bool bActive)
        {
            // Called from ScreenManager.
            ShowBorder(bActive);
        }

        public void StopPlaying()
        {
            StopPlaying(true);
        }

        public void SyncSetCurrentFrame(long iFrame, bool bAllowUiUpdate)
        {
            // Called during static sync.
            // Common position changed, we get a new frame to jump to.
            // target frame may be over the total.

            if (_mFrameServer.VideoFile.Loaded)
            {
                _mIFramesToDecode = 1;

                if (iFrame == -1)
                {
                    // Special case for +1 frame.
                    if (_mICurrentPosition < _mISelEnd)
                    {
                        ShowNextFrame(-1, bAllowUiUpdate);
                    }
                }
                else
                {
                    _mICurrentPosition = iFrame*_mFrameServer.VideoFile.Infos.iAverageTimeStampsPerFrame;
                    _mICurrentPosition += _mISelStart;

                    if (_mICurrentPosition > _mISelEnd) _mICurrentPosition = _mISelEnd;

                    ShowNextFrame(_mICurrentPosition, bAllowUiUpdate);
                }

                if (bAllowUiUpdate)
                {
                    UpdateNavigationCursor();
                    UpdateCurrentPositionLabel();
                    ActivateKeyframe(_mICurrentPosition);
                    trkSelection.SelPos = trkFrame.Position;

                    if (_mBShowInfos)
                    {
                        UpdateDebugInfos();
                    }
                }
            }
        }

        public void RefreshImage()
        {
            // For cases where surfaceScreen.Invalidate() is not enough.
            if (_mFrameServer.VideoFile.Loaded)
            {
                ShowNextFrame(_mICurrentPosition, true);
            }
        }

        public void RefreshUiCulture()
        {
            // Labels
            lblSelStartSelection.AutoSize = true;
            lblSelDuration.AutoSize = true;

            lblWorkingZone.Text = ScreenManagerLang.lblWorkingZone_Text;
            UpdateSpeedLabel();
            UpdateSelectionLabels();
            UpdateCurrentPositionLabel();

            lblSpeedTuner.Left = lblTimeCode.Left + lblTimeCode.Width + 8;
            sldrSpeed.Left = lblSpeedTuner.Left + lblSpeedTuner.Width + 8;

            ReloadTooltipsCulture();
            ReloadMenusCulture();
            _mKeyframeCommentsHub.RefreshUiCulture();

            // Because this method is called when we change the general preferences,
            // we can use it to update data too.

            // Keyframes positions.
            if (_mFrameServer.Metadata.Count > 0)
            {
                EnableDisableKeyframes();
            }

            _mFrameServer.Metadata.CalibrationHelper.CurrentSpeedUnit = _mPrefManager.SpeedUnit;
            _mFrameServer.Metadata.UpdateTrajectoriesForKeyframes();

            // Refresh image to update timecode in chronos, grids colors, default fading, etc.
            DoInvalidate();
        }

        public void SetDrawingtimeFilterOutput(DrawtimeFilterOutput dfo)
        {
            // A video filter just finished and is passing us its output object.
            // It is used as a communication channel between the filter and the player.
            // Depending on the filter type, we may need to switch to a special mode,
            // keep track of old pre-filter parameters,
            // delegate the draw to the filter, etc...

            if (dfo.Active)
            {
                _mBDrawtimeFiltered = true;
                _mDrawingFilterOutput = dfo;

                // Disable playing and drawing.
                DisablePlayAndDraw();

                // Disable all player controls
                EnableDisableAllPlayingControls(false);
                EnableDisableDrawingTools(false);

                // TODO: memorize current state (keyframe docked) and recall it when quiting filtered mode.
                DockKeyframePanel(true);
                _mBStretchModeOn = true;
                StretchSqueezeSurface();
            }
            else
            {
                _mBDrawtimeFiltered = false;
                _mDrawingFilterOutput = null;

                EnableDisableAllPlayingControls(true);
                EnableDisableDrawingTools(true);

                // TODO:recall saved state.
            }
        }

        public void SetSyncMergeImage(Bitmap syncMergeImage, bool bUpdateUi)
        {
            //if(m_SyncMergeImage != null)
            //	m_SyncMergeImage.Dispose();

            _mSyncMergeImage = syncMergeImage;

            if (bUpdateUi)
            {
                // Ask for a repaint. We don't wait for the next frame to be drawn
                // because the user may be manually moving the other video.
                DoInvalidate();
            }
        }

        public bool OnKeyPress(Keys keycode)
        {
            var bWasHandled = false;

            // Disabled completely if no video.
            if (_mFrameServer.VideoFile.Loaded)
            {
                // Method called from the Screen Manager's PreFilterMessage.
                switch (keycode)
                {
                    case Keys.Space:
                    case Keys.Return:
                    {
                        OnButtonPlay();
                        bWasHandled = true;
                        break;
                    }
                    case Keys.Escape:
                    {
                        DisablePlayAndDraw();
                        DoInvalidate();
                        bWasHandled = true;
                        break;
                    }
                    case Keys.Left:
                    {
                        if ((ModifierKeys & Keys.Control) == Keys.Control)
                        {
                            // Previous keyframe
                            GotoPreviousKeyframe();
                        }
                        else
                        {
                            if (((ModifierKeys & Keys.Shift) == Keys.Shift) && _mICurrentPosition <= _mISelStart)
                            {
                                // Shift + Left on first = loop backward.
                                buttonGotoLast_Click(null, EventArgs.Empty);
                            }
                            else
                            {
                                // Previous frame
                                buttonGotoPrevious_Click(null, EventArgs.Empty);
                            }
                        }
                        bWasHandled = true;
                        break;
                    }
                    case Keys.Right:
                    {
                        if ((ModifierKeys & Keys.Control) == Keys.Control)
                        {
                            // Next keyframe
                            GotoNextKeyframe();
                        }
                        else
                        {
                            // Next frame
                            buttonGotoNext_Click(null, EventArgs.Empty);
                        }
                        bWasHandled = true;
                        break;
                    }
                    case Keys.Add:
                    {
                        IncreaseDirectZoom();
                        bWasHandled = true;
                        break;
                    }
                    case Keys.Subtract:
                    {
                        // Decrease Zoom.
                        DecreaseDirectZoom();
                        bWasHandled = true;
                        break;
                    }
                    case Keys.F6:
                    {
                        AddKeyframe();
                        bWasHandled = true;
                        break;
                    }
                    case Keys.F7:
                    {
                        // Unused.
                        break;
                    }
                    case Keys.Delete:
                    {
                        if ((ModifierKeys & Keys.Control) == Keys.Control)
                        {
                            // Remove Keyframe
                            if (_mIActiveKeyFrameIndex >= 0)
                            {
                                RemoveKeyframe(_mIActiveKeyFrameIndex);
                            }
                        }
                        else
                        {
                            // Remove selected Drawing
                            // Note: Should only work if the Drawing is currently being moved...
                            DeleteSelectedDrawing();
                        }
                        bWasHandled = true;
                        break;
                    }
                    case Keys.End:
                    {
                        buttonGotoLast_Click(null, EventArgs.Empty);
                        bWasHandled = true;
                        break;
                    }
                    case Keys.Home:
                    {
                        buttonGotoFirst_Click(null, EventArgs.Empty);
                        bWasHandled = true;
                        break;
                    }
                    case Keys.Down:
                    case Keys.Up:
                    {
                        sldrSpeed_KeyDown(null, new KeyEventArgs(keycode));
                        bWasHandled = true;
                        break;
                    }
                    default:
                        break;
                }
            }

            return bWasHandled;
        }

        public void UpdateImageSize()
        {
            var imageSize = new Size(_mFrameServer.VideoFile.Infos.iDecodingWidth,
                _mFrameServer.VideoFile.Infos.iDecodingHeight);

            _mFrameServer.Metadata.ImageSize = imageSize;

            _mPointerTool.SetImageSize(_mFrameServer.Metadata.ImageSize);

            _mFrameServer.CoordinateSystem.SetOriginalSize(_mFrameServer.Metadata.ImageSize);
            _mFrameServer.CoordinateSystem.ReinitZoom();

            StretchSqueezeSurface();
        }

        public void FullScreen(bool bFullScreen)
        {
            if (bFullScreen && !_mBStretchModeOn)
            {
                _mBStretchModeOn = true;
                StretchSqueezeSurface();
                _mFrameServer.Metadata.ResizeFinished();
                DoInvalidate();
            }
        }

        public void AddImageDrawing(string filename, bool bIsSvg)
        {
            // Add an image drawing from a file.
            // Mimick all the actions that are normally taken when we select a drawing tool and click on the image.
            if (_mFrameServer.VideoFile != null && _mFrameServer.VideoFile.Loaded)
            {
                BeforeAddImageDrawing();

                if (File.Exists(filename))
                {
                    try
                    {
                        if (bIsSvg)
                        {
                            var dsvg = new DrawingSvg(_mFrameServer.Metadata.ImageSize.Width,
                                _mFrameServer.Metadata.ImageSize.Height,
                                _mICurrentPosition,
                                _mFrameServer.Metadata.AverageTimeStampsPerFrame,
                                filename);

                            _mFrameServer.Metadata[_mIActiveKeyFrameIndex].AddDrawing(dsvg);
                        }
                        else
                        {
                            var dbmp = new DrawingBitmap(_mFrameServer.Metadata.ImageSize.Width,
                                _mFrameServer.Metadata.ImageSize.Height,
                                _mICurrentPosition,
                                _mFrameServer.Metadata.AverageTimeStampsPerFrame,
                                filename);

                            _mFrameServer.Metadata[_mIActiveKeyFrameIndex].AddDrawing(dbmp);
                        }
                    }
                    catch
                    {
                        // An error occurred during the creation.
                        // example : external DTD an no network or invalid svg file.
                        // TODO: inform the user.
                    }
                }

                AfterAddImageDrawing();
            }
        }

        public void AddImageDrawing(Bitmap bmp)
        {
            // Add an image drawing from a bitmap.
            // Mimick all the actions that are normally taken when we select a drawing tool and click on the image.
            // TODO: use an actual DrawingTool class for this!?
            if (_mFrameServer.VideoFile != null && _mFrameServer.VideoFile.Loaded)
            {
                BeforeAddImageDrawing();

                var dbmp = new DrawingBitmap(_mFrameServer.Metadata.ImageSize.Width,
                    _mFrameServer.Metadata.ImageSize.Height,
                    _mICurrentPosition,
                    _mFrameServer.Metadata.AverageTimeStampsPerFrame,
                    bmp);

                _mFrameServer.Metadata[_mIActiveKeyFrameIndex].AddDrawing(dbmp);

                AfterAddImageDrawing();
            }
        }

        private void BeforeAddImageDrawing()
        {
            if (IsCurrentlyPlaying)
            {
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();
                ActivateKeyframe(_mICurrentPosition);
            }

            PrepareKeyframesDock();
            _mFrameServer.Metadata.AllDrawingTextToNormalMode();
            _mFrameServer.Metadata.SelectedExtraDrawing = -1;

            // Add a KeyFrame here if it doesn't exist.
            AddKeyframe();
        }

        private void AfterAddImageDrawing()
        {
            _mFrameServer.Metadata.SelectedDrawingFrame = -1;
            _mFrameServer.Metadata.SelectedDrawing = -1;

            _mActiveTool = _mPointerTool;
            SetCursor(_mPointerTool.GetCursor(0));

            DoInvalidate();
        }

        #endregion Public Methods

        #region Various Inits & Setups

        private void InitializeDrawingTools()
        {
            _mPointerTool = new DrawingToolPointer();
            _mActiveTool = _mPointerTool;

            stripDrawingTools.Left = 3;

            // Special button: Add key image
            _mBtnAddKeyFrame = CreateToolButton();
            _mBtnAddKeyFrame.Image = Resources.page_white_go;
            _mBtnAddKeyFrame.Click += btnAddKeyframe_Click;
            _mBtnAddKeyFrame.ToolTipText = ScreenManagerLang.ToolTip_AddKeyframe;
            stripDrawingTools.Items.Add(_mBtnAddKeyFrame);

            // Pointer tool button
            AddToolButton(_mPointerTool, drawingTool_Click);
            stripDrawingTools.Items.Add(new ToolStripSeparator());

            // Special button: Key image comments
            _mBtnShowComments = CreateToolButton();
            _mBtnShowComments.Image = Resources.comments2;
            _mBtnShowComments.Click += btnShowComments_Click;
            _mBtnShowComments.ToolTipText = ScreenManagerLang.ToolTip_ShowComments;
            stripDrawingTools.Items.Add(_mBtnShowComments);

            // All other tools
            AddToolButton(ToolManager.Label, drawingTool_Click);
            AddToolButton(ToolManager.Pencil, drawingTool_Click);
            AddToolButton(ToolManager.Line, drawingTool_Click);
            AddToolButton(ToolManager.Circle, drawingTool_Click);
            AddToolButton(ToolManager.CrossMark, drawingTool_Click);
            AddToolButton(ToolManager.Angle, drawingTool_Click);
            AddToolButton(ToolManager.Chrono, drawingTool_Click);
            AddToolButton(ToolManager.Plane, drawingTool_Click);
            AddToolButton(ToolManager.Magnifier, btnMagnifier_Click);

            // Special button: Tool presets
            _mBtnToolPresets = CreateToolButton();
            _mBtnToolPresets.Image = Resources.SwatchIcon3;
            _mBtnToolPresets.Click += btnColorProfile_Click;
            _mBtnToolPresets.ToolTipText = ScreenManagerLang.ToolTip_ColorProfile;
            stripDrawingTools.Items.Add(_mBtnToolPresets);
        }

        private ToolStripButton CreateToolButton()
        {
            var btn = new ToolStripButton();
            btn.AutoSize = false;
            btn.DisplayStyle = ToolStripItemDisplayStyle.Image;
            btn.ImageScaling = ToolStripItemImageScaling.None;
            btn.Size = new Size(25, 25);
            btn.AutoToolTip = false;
            return btn;
        }

        private void AddToolButton(AbstractDrawingTool tool, EventHandler handler)
        {
            var btn = CreateToolButton();
            btn.Image = tool.Icon;
            btn.Tag = tool;
            btn.Click += handler;
            btn.ToolTipText = tool.DisplayName;
            stripDrawingTools.Items.Add(btn);
        }

        private void ResetData()
        {
            _mIFramesToDecode = 1;

            _mFSlowmotionPercentage = 100.0;
            _mBDrawtimeFiltered = false;
            IsCurrentlyPlaying = false;
            _mBSeekToStart = false;
            _mEPlayingMode = PlayingMode.Loop;
            _mBStretchModeOn = false;
            _mFrameServer.CoordinateSystem.Reset();

            // Sync
            _mBSynched = false;
            _mISyncPosition = 0;
            _mBSyncMerge = false;
            if (_mSyncMergeImage != null)
                _mSyncMergeImage.Dispose();

            _mBShowImageBorder = false;

            SetupPrimarySelectionData(); // Should not be necessary when every data is coming from m_FrameServer.

            _mBHandlersLocked = false;

            _mIActiveKeyFrameIndex = -1;
            _mActiveTool = _mPointerTool;

            _mBDocked = true;
            _mBTextEdit = false;
            DrawingToolLine2D.ShowMeasure = false;
            DrawingToolCross2D.ShowCoordinates = false;

            _mBDrawtimeFiltered = false;

            _mFHighSpeedFactor = 1.0f;
            UpdateSpeedLabel();
        }

        private void DemuxMetadata()
        {
            // Try to find metadata muxed inside the file and load it.

            var metadata = _mFrameServer.VideoFile.ReadMetadata();

            if (metadata != null)
            {
                _mFrameServer.Metadata = new Metadata(metadata,
                    _mFrameServer.VideoFile.Infos.iDecodingWidth,
                    _mFrameServer.VideoFile.Infos.iDecodingHeight,
                    _mFrameServer.VideoFile.Infos.iAverageTimeStampsPerFrame,
                    _mFrameServer.VideoFile.FilePath,
                    TimeStampsToTimecode,
                    OnShowClosestFrame);

                UpdateFramesMarkers();
                OrganizeKeyframes();
            }
        }

        private void SetupPrimarySelectionData()
        {
            // Setup data
            if (_mFrameServer.VideoFile.Loaded)
            {
                var fAverageTimeStampsPerFrame = _mFrameServer.VideoFile.Infos.fAverageTimeStampsPerSeconds/
                                                 _mFrameServer.VideoFile.Infos.fFps;
                _mISelStart = _mIStartingPosition;
                _mISelEnd = (long) (_mITotalDuration + _mIStartingPosition - fAverageTimeStampsPerFrame);
                SelectionDuration = _mITotalDuration;
            }
            else
            {
                _mISelStart = 0;
                _mISelEnd = 99;
                SelectionDuration = 100;
                _mITotalDuration = 100;

                _mICurrentPosition = 0;
                _mIStartingPosition = 0;
            }
        }

        private void SetupPrimarySelectionPanel()
        {
            // Setup controls & labels.
            // Update internal state only, doesn't trigger the events.
            trkSelection.UpdateInternalState(_mISelStart, _mISelEnd, _mISelStart, _mISelEnd, _mISelStart);
            UpdateSelectionLabels();
        }

        private void SetUpForNewMovie()
        {
            // Problem: The screensurface hasn't got its final size...
            // So it doesn't make much sense to call it here...
            ShowHideResizers(true);
            StretchSqueezeSurface();

            // Since it hadn't its final size, we don't really know if the pic is too large...
            _mBStretchModeOn = false;
            OnPoke();
        }

        private void SetupKeyframeCommentsHub()
        {
            var dp = DelegatesPool.Instance();
            if (dp.MakeTopMost != null)
            {
                _mKeyframeCommentsHub = new FormKeyframeComments(this);
                dp.MakeTopMost(_mKeyframeCommentsHub);
            }
        }

        private void LookForLinkedAnalysis()
        {
            // Look for an Anlaysis with the same file name in the same directory.

            // Complete path of hypothetical Analysis.
            var kvaFile = Path.GetDirectoryName(_mFrameServer.VideoFile.FilePath);
            kvaFile = kvaFile + "\\" + Path.GetFileNameWithoutExtension(_mFrameServer.VideoFile.FilePath) + ".kva";

            if (File.Exists(kvaFile))
            {
                _mFrameServer.Metadata.Load(kvaFile, true);
            }
        }

        private void UpdateFilenameLabel()
        {
            lblFileName.Text = Path.GetFileName(_mFrameServer.VideoFile.FilePath);
        }

        private void ShowHideResizers(bool bShow)
        {
            ImageResizerNE.Visible = bShow;
            ImageResizerNW.Visible = bShow;
            ImageResizerSE.Visible = bShow;
            ImageResizerSW.Visible = bShow;
        }

        private void BuildContextMenus()
        {
            // Attach the event handlers and build the menus.

            // 1. Default context menu.
            _mnuDirectTrack.Click += mnuDirectTrack_Click;
            _mnuDirectTrack.Image = Properties.Drawings.track;
            _mnuPlayPause.Click += buttonPlay_Click;
            _mnuSetCaptureSpeed.Click += mnuSetCaptureSpeed_Click;
            _mnuSetCaptureSpeed.Image = Resources.camera_speed;
            _mnuSavePic.Click += btnSnapShot_Click;
            _mnuSavePic.Image = Resources.picture_save;
            _mnuSendPic.Click += mnuSendPic_Click;
            _mnuSendPic.Image = Resources.image;
            _mnuCloseScreen.Click += btnClose_Click;
            _mnuCloseScreen.Image = Resources.film_close3;
            _popMenu.Items.AddRange(new ToolStripItem[]
            {_mnuDirectTrack, _mnuSetCaptureSpeed, _mnuSavePic, _mnuSendPic, new ToolStripSeparator(), _mnuCloseScreen});

            // 2. Drawings context menu (Configure, Delete, Track this)
            _mnuConfigureDrawing.Click += mnuConfigureDrawing_Click;
            _mnuConfigureDrawing.Image = Properties.Drawings.configure;
            _mnuConfigureFading.Click += mnuConfigureFading_Click;
            _mnuConfigureFading.Image = Properties.Drawings.persistence;
            _mnuConfigureOpacity.Click += mnuConfigureOpacity_Click;
            _mnuConfigureOpacity.Image = Properties.Drawings.persistence;
            _mnuTrackTrajectory.Click += mnuTrackTrajectory_Click;
            _mnuTrackTrajectory.Image = Properties.Drawings.track;
            _mnuGotoKeyframe.Click += mnuGotoKeyframe_Click;
            _mnuGotoKeyframe.Image = Resources.page_white_go;
            _mnuDeleteDrawing.Click += mnuDeleteDrawing_Click;
            _mnuDeleteDrawing.Image = Properties.Drawings.delete;

            // 3. Tracking pop menu (Restart, Stop tracking)
            _mnuStopTracking.Click += mnuStopTracking_Click;
            _mnuStopTracking.Visible = false;
            _mnuStopTracking.Image = Properties.Drawings.trackstop;
            _mnuRestartTracking.Click += mnuRestartTracking_Click;
            _mnuRestartTracking.Visible = false;
            _mnuRestartTracking.Image = Properties.Drawings.trackingplay;
            _mnuDeleteTrajectory.Click += mnuDeleteTrajectory_Click;
            _mnuDeleteTrajectory.Image = Properties.Drawings.delete;
            _mnuDeleteEndOfTrajectory.Click += mnuDeleteEndOfTrajectory_Click;
            //mnuDeleteEndOfTrajectory.Image = Properties.Resources.track_trim2;
            _mnuConfigureTrajectory.Click += mnuConfigureTrajectory_Click;
            _mnuConfigureTrajectory.Image = Properties.Drawings.configure;
            _popMenuTrack.Items.AddRange(new ToolStripItem[]
            {
                _mnuConfigureTrajectory, new ToolStripSeparator(), _mnuStopTracking, _mnuRestartTracking,
                new ToolStripSeparator(), _mnuDeleteEndOfTrajectory, _mnuDeleteTrajectory
            });

            // 4. Chrono pop menu (Start, Stop, Hide, etc.)
            _mnuChronoConfigure.Click += mnuChronoConfigure_Click;
            _mnuChronoConfigure.Image = Properties.Drawings.configure;
            _mnuChronoStart.Click += mnuChronoStart_Click;
            _mnuChronoStart.Image = Properties.Drawings.chronostart;
            _mnuChronoStop.Click += mnuChronoStop_Click;
            _mnuChronoStop.Image = Properties.Drawings.chronostop;
            _mnuChronoCountdown.Click += mnuChronoCountdown_Click;
            _mnuChronoCountdown.Checked = false;
            _mnuChronoCountdown.Enabled = false;
            _mnuChronoHide.Click += mnuChronoHide_Click;
            _mnuChronoHide.Image = Properties.Drawings.hide;
            _mnuChronoDelete.Click += mnuChronoDelete_Click;
            _mnuChronoDelete.Image = Properties.Drawings.delete;
            _popMenuChrono.Items.AddRange(new ToolStripItem[]
            {
                _mnuChronoConfigure, new ToolStripSeparator(), _mnuChronoStart, _mnuChronoStop, _mnuChronoCountdown,
                new ToolStripSeparator(), _mnuChronoHide, _mnuChronoDelete
            });

            // 5. Magnifier
            _mnuMagnifier150.Click += mnuMagnifier150_Click;
            _mnuMagnifier175.Click += mnuMagnifier175_Click;
            _mnuMagnifier175.Checked = true;
            _mnuMagnifier200.Click += mnuMagnifier200_Click;
            _mnuMagnifier225.Click += mnuMagnifier225_Click;
            _mnuMagnifier250.Click += mnuMagnifier250_Click;
            _mnuMagnifierDirect.Click += mnuMagnifierDirect_Click;
            _mnuMagnifierDirect.Image = Resources.arrow_out;
            _mnuMagnifierQuit.Click += mnuMagnifierQuit_Click;
            _mnuMagnifierQuit.Image = Resources.hide;
            _popMenuMagnifier.Items.AddRange(new ToolStripItem[]
            {
                _mnuMagnifier150, _mnuMagnifier175, _mnuMagnifier200, _mnuMagnifier225, _mnuMagnifier250,
                new ToolStripSeparator(), _mnuMagnifierDirect, _mnuMagnifierQuit
            });

            // The right context menu and its content will be choosen upon MouseDown.
            panelCenter.ContextMenuStrip = _popMenu;

            // Load texts
            ReloadMenusCulture();
        }

        private void SetupDebugPanel()
        {
            _mBShowInfos = true;
            panelDebug.Left = 0;
            panelDebug.Width = 180;
            panelDebug.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            panelDebug.BackColor = Color.Black;
        }

        #endregion Various Inits & Setups

        #region Misc Events

        private void btnClose_Click(object sender, EventArgs e)
        {
            // If we currently are in DrawTime filter, we just close this and return to normal playback.
            // Propagate to PlayerScreen which will report to ScreenManager.
            _mPlayerScreenUiHandler.ScreenUI_CloseAsked();
        }

        private void PanelVideoControls_MouseEnter(object sender, EventArgs e)
        {
            // Set focus to enable mouse scroll
            panelVideoControls.Focus();
        }

        #endregion Misc Events

        #region Misc private helpers

        private void OnPoke()
        {
            //------------------------------------------------------------------------------
            // This function is a hub event handler for all button press, mouse clicks, etc.
            // Signal itself as the active screen to the ScreenManager
            //---------------------------------------------------------------------

            _mPlayerScreenUiHandler.ScreenUI_SetAsActiveScreen();

            // 1. Ensure no DrawingText is in edit mode.
            _mFrameServer.Metadata.AllDrawingTextToNormalMode();

            _mActiveTool = _mActiveTool.KeepToolFrameChanged ? _mActiveTool : _mPointerTool;
            if (_mActiveTool == _mPointerTool)
            {
                SetCursor(_mPointerTool.GetCursor(-1));
            }

            // 3. Dock Keyf panel if nothing to see.
            if (_mFrameServer.Metadata.Count < 1)
            {
                DockKeyframePanel(true);
            }
        }

        private string TimeStampsToTimecode(long _iTimeStamp, TimeCodeFormat timeCodeFormat, bool bSynched)
        {
            //-------------------------
            // Input    : TimeStamp (might be a duration. If starting ts isn't 0, it should already be shifted.)
            // Output   : time in a specific format
            //-------------------------

            TimeCodeFormat tcf;
            if (timeCodeFormat == TimeCodeFormat.Unknown)
            {
                tcf = _mPrefManager.TimeCodeFormat;
            }
            else
            {
                tcf = timeCodeFormat;
            }

            long iTimeStamp;
            if (bSynched)
            {
                iTimeStamp = _iTimeStamp - _mISyncPosition;
            }
            else
            {
                iTimeStamp = _iTimeStamp;
            }

            // timestamp to milliseconds. (Needed for most formats)
            double fSeconds;

            if (_mFrameServer.VideoFile.Loaded)
                fSeconds = iTimeStamp/_mFrameServer.VideoFile.Infos.fAverageTimeStampsPerSeconds;
            else
                fSeconds = 0;

            // m_fSlowFactor is different from 1.0f only when user specify that the capture fps
            // was different than the playing fps. We readjust time.
            var fMilliseconds = (fSeconds*1000)/_mFHighSpeedFactor;

            // If there are more than 100 frames per seconds, we display milliseconds.
            // This can happen when the user manually tune the input fps.
            var bShowThousandth = (_mFHighSpeedFactor*_mFrameServer.VideoFile.Infos.fFps >= 100);

            string outputTimeCode;
            switch (tcf)
            {
                case TimeCodeFormat.ClassicTime:
                    outputTimeCode = TimeHelper.MillisecondsToTimecode(fMilliseconds, bShowThousandth, true);
                    break;

                case TimeCodeFormat.Frames:
                    if (_mFrameServer.VideoFile.Infos.iAverageTimeStampsPerFrame != 0)
                    {
                        outputTimeCode = string.Format("{0}",
                            (int) ((double) iTimeStamp/_mFrameServer.VideoFile.Infos.iAverageTimeStampsPerFrame) + 1);
                    }
                    else
                    {
                        outputTimeCode = "0";
                    }
                    break;

                case TimeCodeFormat.Milliseconds:
                    outputTimeCode = string.Format("{0}", (int) Math.Round(fMilliseconds));
                    break;

                case TimeCodeFormat.TenThousandthOfHours:
                    // 1 Ten Thousandth of Hour = 360 ms.
                    var fTth = fMilliseconds/360.0;
                    outputTimeCode = string.Format("{0}:{1:00}", (int) fTth, Math.Floor((fTth - (int) fTth)*100));
                    break;

                case TimeCodeFormat.HundredthOfMinutes:
                    // 1 Hundredth of minute = 600 ms.
                    var fCtm = fMilliseconds/600.0;
                    outputTimeCode = string.Format("{0}:{1:00}", (int) fCtm, Math.Floor((fCtm - (int) fCtm)*100));
                    break;

                case TimeCodeFormat.TimeAndFrames:
                    var timeString = TimeHelper.MillisecondsToTimecode(fMilliseconds, bShowThousandth, true);
                    string frameString;
                    if (_mFrameServer.VideoFile.Infos.iAverageTimeStampsPerFrame != 0)
                    {
                        frameString = string.Format("{0}",
                            (int) ((double) iTimeStamp/_mFrameServer.VideoFile.Infos.iAverageTimeStampsPerFrame) + 1);
                    }
                    else
                    {
                        frameString = "0";
                    }
                    outputTimeCode = string.Format("{0} ({1})", timeString, frameString);
                    break;

                case TimeCodeFormat.Timestamps:
                    outputTimeCode = string.Format("{0}", (int) iTimeStamp);
                    break;

                default:
                    outputTimeCode = TimeHelper.MillisecondsToTimecode(fMilliseconds, bShowThousandth, true);
                    break;
            }

            return outputTimeCode;
        }

        private void DoDrawingUndrawn()
        {
            //--------------------------------------------------------
            // this function is called after we undo a drawing action.
            // Called from CommandAddDrawing.Unexecute().
            //--------------------------------------------------------
            _mActiveTool = _mActiveTool.KeepToolFrameChanged ? _mActiveTool : _mPointerTool;
            if (_mActiveTool == _mPointerTool)
            {
                SetCursor(_mPointerTool.GetCursor(0));
            }
        }

        private void UpdateFramesMarkers()
        {
            // Updates the markers coordinates and redraw the trkFrame.
            trkFrame.UpdateMarkers(_mFrameServer.Metadata);
        }

        private void ShowBorder(bool bShow)
        {
            _mBShowImageBorder = bShow;
            DoInvalidate();
        }

        private void DrawImageBorder(Graphics canvas)
        {
            // Draw the border around the screen to mark it as selected.
            // Called back from main drawing routine.
            canvas.DrawRectangle(MPenImageBorder, 0, 0, pbSurfaceScreen.Width - MPenImageBorder.Width,
                pbSurfaceScreen.Height - MPenImageBorder.Width);
        }

        private void DisablePlayAndDraw()
        {
            StopPlaying();
            _mActiveTool = _mPointerTool;
            SetCursor(_mPointerTool.GetCursor(0));
            DisableMagnifier();
            UnzoomDirectZoom();
            _mFrameServer.Metadata.StopAllTracking();
        }

        #endregion Misc private helpers

        #region Debug Helpers

        private void UpdateDebugInfos()
        {
            panelDebug.Visible = true;

            dbgDurationTimeStamps.Text = string.Format("TotalDuration (ts): {0:0}", _mITotalDuration);
            dbgFFps.Text = string.Format("Fps Avg (f): {0:0.00}", _mFrameServer.VideoFile.Infos.fFps);
            dbgSelectionStart.Text = string.Format("SelStart (ts): {0:0}", _mISelStart);
            dbgSelectionEnd.Text = string.Format("SelEnd (ts): {0:0}", _mISelEnd);
            dbgSelectionDuration.Text = string.Format("SelDuration (ts): {0:0}", SelectionDuration);
            dbgCurrentPositionAbs.Text = string.Format("CurrentPosition (abs, ts): {0:0}", _mICurrentPosition);
            dbgCurrentPositionRel.Text = string.Format("CurrentPosition (rel, ts): {0:0}",
                _mICurrentPosition - _mISelStart);
            dbgStartOffset.Text = string.Format("StartOffset (ts): {0:0}", _mFrameServer.VideoFile.Infos.iFirstTimeStamp);
            dbgDrops.Text = string.Format("Drops (f): {0:0}", _mIDroppedFrames);

            dbgCurrentFrame.Text = string.Format("CurrentFrame (f): {0}",
                _mFrameServer.VideoFile.Selection.iCurrentFrame);
            dbgDurationFrames.Text = string.Format("Duration (f) : {0}",
                _mFrameServer.VideoFile.Selection.iDurationFrame);

            panelDebug.Invalidate();
        }

        private void panelDebug_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            UpdateDebugInfos();
            Log.Debug("");
            Log.Debug("Timestamps Full Dump");
            Log.Debug("--------------------");
            var fAverageTimeStampsPerFrame = _mFrameServer.VideoFile.Infos.fAverageTimeStampsPerSeconds/
                                             _mFrameServer.VideoFile.Infos.fFps;
            Log.Debug("Average ts per frame     : " + fAverageTimeStampsPerFrame);
            Log.Debug("");
            Log.Debug("m_iStartingPosition      : " + _mIStartingPosition);
            Log.Debug("m_iTotalDuration         : " + _mITotalDuration);
            Log.Debug("m_iCurrentPosition       : " + _mICurrentPosition);
            Log.Debug("");
            Log.Debug("m_iSelStart              : " + _mISelStart);
            Log.Debug("m_iSelEnd                : " + _mISelEnd);
            Log.Debug("m_iSelDuration           : " + SelectionDuration);
            Log.Debug("");
            Log.Debug("trkSelection.Minimum     : " + trkSelection.Minimum);
            Log.Debug("trkSelection.Maximum     : " + trkSelection.Maximum);
            Log.Debug("trkSelection.SelStart    : " + trkSelection.SelStart);
            Log.Debug("trkSelection.SelEnd      : " + trkSelection.SelEnd);
            Log.Debug("trkSelection.SelPos      : " + trkSelection.SelPos);
            Log.Debug("");
            Log.Debug("trkFrame.Minimum         : " + trkFrame.Minimum);
            Log.Debug("trkFrame.Maximum         : " + trkFrame.Maximum);
            Log.Debug("trkFrame.Position        : " + trkFrame.Position);
        }

        #endregion Debug Helpers

        #region Video Controls

        #region Playback Controls

        public void buttonGotoFirst_Click(object sender, EventArgs e)
        {
            // Jump to start.
            if (_mFrameServer.VideoFile.Loaded)
            {
                OnPoke();
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();

                _mIFramesToDecode = 1;
                ShowNextFrame(_mISelStart, true);

                UpdateNavigationCursor();
                ActivateKeyframe(_mICurrentPosition);

                if (_mBShowInfos)
                {
                    UpdateDebugInfos();
                }
            }
        }

        public void buttonGotoPrevious_Click(object sender, EventArgs e)
        {
            if (_mFrameServer.VideoFile.Loaded)
            {
                OnPoke();
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();

                //---------------------------------------------------------------------------
                // Si on est en dehors de la zone primaire, ou qu'on va en sortir,
                // se replacer au début de celle-ci.
                //---------------------------------------------------------------------------
                if ((_mICurrentPosition <= _mISelStart) || (_mICurrentPosition > _mISelEnd))
                {
                    _mIFramesToDecode = 1;
                    ShowNextFrame(_mISelStart, true);
                }
                else
                {
                    var oldPos = _mICurrentPosition;
                    _mIFramesToDecode = -1;
                    ShowNextFrame(-1, true);

                    // If it didn't work, try going back two frames to unstuck the situation.
                    // Todo: check if we're going to endup outside the working zone ?
                    if (_mICurrentPosition == oldPos)
                    {
                        Log.Debug("Seeking to previous frame did not work. Moving backward 2 frames.");
                        _mIFramesToDecode = -2;
                        ShowNextFrame(-1, true);
                    }

                    // Reset to normal.
                    _mIFramesToDecode = 1;
                }

                UpdateNavigationCursor();
                ActivateKeyframe(_mICurrentPosition);

                if (_mBShowInfos)
                {
                    UpdateDebugInfos();
                }
            }
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            //----------------------------------------------------------------------------
            // L'appui sur le bouton play ne fait qu'activer ou désactiver le Timer
            // La lecture est ensuite automatique et c'est dans la fonction du Timer
            // que l'on gère la NextFrame à afficher en fonction du ralentit,
            // du mode de bouclage etc...
            //----------------------------------------------------------------------------
            if (_mFrameServer.VideoFile.Loaded)
            {
                OnPoke();
                OnButtonPlay();
            }
        }

        public void buttonGotoNext_Click(object sender, EventArgs e)
        {
            if (_mFrameServer.VideoFile.Loaded)
            {
                OnPoke();
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();
                _mIFramesToDecode = 1;

                // If we are outside the primary zone or going to get out, seek to start.
                // We also only do the seek if we are after the m_iStartingPosition,
                // Sometimes, the second frame will have a time stamp inferior to the first,
                // which sort of breaks our sentinels.
                if (((_mICurrentPosition < _mISelStart) || (_mICurrentPosition >= _mISelEnd)) &&
                    (_mICurrentPosition >= _mIStartingPosition))
                {
                    ShowNextFrame(_mISelStart, true);
                }
                else
                {
                    ShowNextFrame(-1, true);
                }

                UpdateNavigationCursor();
                ActivateKeyframe(_mICurrentPosition);
                if (_mBShowInfos)
                {
                    UpdateDebugInfos();
                }
            }
        }

        public void buttonGotoLast_Click(object sender, EventArgs e)
        {
            if (_mFrameServer.VideoFile.Loaded)
            {
                OnPoke();
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();

                _mIFramesToDecode = 1;
                ShowNextFrame(_mISelEnd, true);

                UpdateNavigationCursor();
                ActivateKeyframe(_mICurrentPosition);
                if (_mBShowInfos)
                {
                    UpdateDebugInfos();
                }
            }
        }

        public void OnButtonPlay()
        {
            //--------------------------------------------------------------
            // This function is accessed from ScreenManager.
            // Eventually from a worker thread. (no SetAsActiveScreen here).
            //--------------------------------------------------------------
            if (_mFrameServer.VideoFile.Loaded)
            {
                if (IsCurrentlyPlaying)
                {
                    // Go into Pause mode.
                    StopPlaying();
                    _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();
                    buttonPlay.Image = Resources.liqplay17;
                    IsCurrentlyPlaying = false;
                    ActivateKeyframe(_mICurrentPosition);
                    ToastPause();
                }
                else
                {
                    // Go into Play mode
                    buttonPlay.Image = Resources.liqpause6;
                    Application.Idle += IdleDetector;
                    StartMultimediaTimer(GetPlaybackFrameInterval());
                    IsCurrentlyPlaying = true;
                }
            }
        }

        public void Common_MouseWheel(object sender, MouseEventArgs e)
        {
            // MouseWheel was recorded on one of the controls.
            var iScrollOffset = e.Delta*SystemInformation.MouseWheelScrollLines/120;

            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (iScrollOffset > 0)
                {
                    IncreaseDirectZoom();
                }
                else
                {
                    DecreaseDirectZoom();
                }
            }
            else
            {
                if (iScrollOffset > 0)
                {
                    if (_mBDrawtimeFiltered)
                    {
                        IncreaseDirectZoom();
                    }
                    else
                    {
                        buttonGotoNext_Click(null, EventArgs.Empty);
                    }
                }
                else
                {
                    if (_mBDrawtimeFiltered)
                    {
                        DecreaseDirectZoom();
                    }
                    else if (((ModifierKeys & Keys.Shift) == Keys.Shift) && _mICurrentPosition <= _mISelStart)
                    {
                        // Shift + Left on first = loop backward.
                        buttonGotoLast_Click(null, EventArgs.Empty);
                    }
                    else
                    {
                        buttonGotoPrevious_Click(null, EventArgs.Empty);
                    }
                }
            }
        }

        #endregion Playback Controls

        #region Working Zone Selection

        private void trkSelection_SelectionChanging(object sender, EventArgs e)
        {
            if (_mFrameServer.VideoFile.Loaded)
            {
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();

                // Update selection timestamps and labels.
                UpdateSelectionDataFromControl();
                UpdateSelectionLabels();

                // Update the frame tracker internal timestamps (including position if needed).
                trkFrame.Remap(_mISelStart, _mISelEnd);

                if (_mBShowInfos)
                {
                    UpdateDebugInfos();
                }
            }
        }

        private void trkSelection_SelectionChanged(object sender, EventArgs e)
        {
            // Actual update.
            if (_mFrameServer.VideoFile.Loaded)
            {
                UpdateSelectionDataFromControl();
                ImportSelectionToMemory(false);

                AfterSelectionChanged();
            }
        }

        private void trkSelection_TargetAcquired(object sender, EventArgs e)
        {
            // User clicked inside selection: Jump to position.
            if (_mFrameServer.VideoFile.Loaded)
            {
                OnPoke();
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();
                _mIFramesToDecode = 1;

                //ShowNextFrame(trkSelection.SelTarget, true);
                //m_iCurrentPosition = trkSelection.SelTarget + trkSelection.Minimum;
                ShowNextFrame(trkSelection.SelPos, true);
                _mICurrentPosition = trkSelection.SelPos + trkSelection.Minimum;

                UpdateNavigationCursor();
                ActivateKeyframe(_mICurrentPosition);
                if (_mBShowInfos)
                {
                    UpdateDebugInfos();
                }
            }
        }

        private void btn_HandlersLock_Click(object sender, EventArgs e)
        {
            // Lock the selection handlers.
            if (_mFrameServer.VideoFile.Loaded)
            {
                _mBHandlersLocked = !_mBHandlersLocked;
                trkSelection.SelLocked = _mBHandlersLocked;

                // Update UI accordingly.
                if (_mBHandlersLocked)
                {
                    btn_HandlersLock.Image = Resources.primselec_locked3;
                    toolTips.SetToolTip(btn_HandlersLock, ScreenManagerLang.LockSelectionUnlock);
                }
                else
                {
                    btn_HandlersLock.Image = Resources.primselec_unlocked3;
                    toolTips.SetToolTip(btn_HandlersLock, ScreenManagerLang.LockSelectionLock);
                }
            }
        }

        private void btnSetHandlerLeft_Click(object sender, EventArgs e)
        {
            // Set the left handler of the selection at the current frame.
            if (_mFrameServer.VideoFile.Loaded && !_mBHandlersLocked)
            {
                trkSelection.SelStart = _mICurrentPosition;
                UpdateSelectionDataFromControl();
                UpdateSelectionLabels();
                trkFrame.Remap(_mISelStart, _mISelEnd);
                ImportSelectionToMemory(false);

                AfterSelectionChanged();
            }
        }

        private void btnSetHandlerRight_Click(object sender, EventArgs e)
        {
            // Set the right handler of the selection at the current frame.
            if (_mFrameServer.VideoFile.Loaded && !_mBHandlersLocked)
            {
                trkSelection.SelEnd = _mICurrentPosition;
                UpdateSelectionDataFromControl();
                UpdateSelectionLabels();
                trkFrame.Remap(_mISelStart, _mISelEnd);
                ImportSelectionToMemory(false);

                AfterSelectionChanged();
            }
        }

        private void btnHandlersReset_Click(object sender, EventArgs e)
        {
            // Reset both selection sentinels to their max values.
            if (_mFrameServer.VideoFile.Loaded && !_mBHandlersLocked)
            {
                trkSelection.Reset();
                UpdateSelectionDataFromControl();

                // We need to force the reloading of all frames.
                ImportSelectionToMemory(true);

                AfterSelectionChanged();
            }
        }

        private void UpdateFramePrimarySelection()
        {
            //--------------------------------------------------------------
            // Update the visible image to reflect the new selection.
            // Cheks that the previous current frame is still within selection,
            // jumps to closest sentinel otherwise.
            //--------------------------------------------------------------

            if (_mFrameServer.VideoFile.Selection.iAnalysisMode == 1)
            {
                // In analysis mode, we always refresh the current frame.
                ShowNextFrame(_mFrameServer.VideoFile.Selection.iCurrentFrame, true);
            }
            else
            {
                if ((_mICurrentPosition >= _mISelStart) && (_mICurrentPosition <= _mISelEnd))
                {
                    // Nothing more to do.
                }
                else
                {
                    _mIFramesToDecode = 1;

                    // Currently visible frame is not in selection, force refresh.
                    if (_mICurrentPosition < _mISelStart)
                    {
                        // Was before start: goto start.
                        ShowNextFrame(_mISelStart, true);
                    }
                    else
                    {
                        // Was after end: goto end.
                        ShowNextFrame(_mISelEnd, true);
                    }
                }
            }

            UpdateNavigationCursor();
            if (_mBShowInfos) UpdateDebugInfos();
        }

        private void UpdateSelectionLabels()
        {
            lblSelStartSelection.Text = ScreenManagerLang.lblSelStartSelection_Text + " : " +
                                        TimeStampsToTimecode(_mISelStart - _mIStartingPosition,
                                            _mPrefManager.TimeCodeFormat, false);
            lblSelDuration.Text = ScreenManagerLang.lblSelDuration_Text + " : " +
                                  TimeStampsToTimecode(SelectionDuration, _mPrefManager.TimeCodeFormat, false);
        }

        private void UpdateSelectionDataFromControl()
        {
            // Update WorkingZone data according to control.
            if ((_mISelStart != trkSelection.SelStart) || (_mISelEnd != trkSelection.SelEnd))
            {
                _mISelStart = trkSelection.SelStart;
                _mISelEnd = trkSelection.SelEnd;
                var fAverageTimeStampsPerFrame = _mFrameServer.VideoFile.Infos.fAverageTimeStampsPerSeconds/
                                                 _mFrameServer.VideoFile.Infos.fFps;
                SelectionDuration = _mISelEnd - _mISelStart + (long) fAverageTimeStampsPerFrame;
            }
        }

        private void AfterSelectionChanged()
        {
            // Update everything as if we moved the handlers manually.
            _mFrameServer.Metadata.SelectionStart = _mISelStart;

            UpdateFramesMarkers();

            OnPoke();
            _mPlayerScreenUiHandler.PlayerScreenUI_SelectionChanged(false);

            // Update current image and keyframe  status.
            UpdateFramePrimarySelection();
            EnableDisableKeyframes();
            ActivateKeyframe(_mICurrentPosition);
        }

        #endregion Working Zone Selection

        #region Frame Tracker

        private void trkFrame_PositionChanging(object sender, PositionChangedEventArgs e)
        {
            // This one should only fire during analysis mode.
            if (_mFrameServer.VideoFile.Loaded)
            {
                // Update image but do not touch cursor, as the user is manipulating it.
                // If the position needs to be adjusted to an actual timestamp, it'll be done later.
                UpdateFrameCurrentPosition(false);
                UpdateCurrentPositionLabel();

                ActivateKeyframe(_mICurrentPosition);
            }
        }

        private void trkFrame_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            if (_mFrameServer.VideoFile.Loaded)
            {
                OnPoke();
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();

                // Update image and cursor.
                UpdateFrameCurrentPosition(true);
                UpdateCurrentPositionLabel();
                ActivateKeyframe(_mICurrentPosition);

                // Update WorkingZone hairline.
                trkSelection.SelPos = trkFrame.Position;
            }
        }

        private void UpdateFrameCurrentPosition(bool bUpdateNavCursor)
        {
            // Displays the image corresponding to the current position within working zone.
            // Trigerred by user (or first load). i.e: cursor moved, show frame.
            if (_mFrameServer.VideoFile.Selection.iAnalysisMode == 0)
            {
                // We may have to decode a few images so show hourglass.
                Cursor = Cursors.WaitCursor;
            }

            _mICurrentPosition = trkFrame.Position;
            _mIFramesToDecode = 1;
            ShowNextFrame(_mICurrentPosition, true);

            if (bUpdateNavCursor)
            {
                // This may readjust the cursor in case the mouse wasn't on a valid timestamp value.
                UpdateNavigationCursor();
            }
            if (_mBShowInfos)
            {
                UpdateDebugInfos();
            }

            if (_mFrameServer.VideoFile.Selection.iAnalysisMode == 0)
            {
                Cursor = Cursors.Default;
            }
        }

        private void UpdateCurrentPositionLabel()
        {
            // Position is relative to working zone.
            var timecode = TimeStampsToTimecode(_mICurrentPosition - _mISelStart, _mPrefManager.TimeCodeFormat,
                _mBSynched);
            lblTimeCode.Text = ScreenManagerLang.lblTimeCode_Text + " : " + timecode;
            lblTimeCode.Invalidate();
        }

        private void UpdateNavigationCursor()
        {
            // Update cursor position after Resize, ShowNextFrame, Working Zone change.
            trkFrame.Position = _mICurrentPosition;
            trkSelection.SelPos = trkFrame.Position;
            UpdateCurrentPositionLabel();
        }

        #endregion Frame Tracker

        #region Speed Slider

        private void sldrSpeed_ValueChanged(object sender, EventArgs e)
        {
            _mFSlowmotionPercentage = sldrSpeed.Value > 0 ? sldrSpeed.Value : 1;

            if (_mFrameServer.VideoFile.Loaded)
            {
                // Reset timer with new value.
                if (IsCurrentlyPlaying)
                {
                    StopMultimediaTimer();
                    StartMultimediaTimer(GetPlaybackFrameInterval());
                }

                // Impacts synchro.
                _mPlayerScreenUiHandler.PlayerScreenUI_SpeedChanged(true);
            }

            UpdateSpeedLabel();
        }

        private void sldrSpeed_KeyDown(object sender, KeyEventArgs e)
        {
            // Increase/Decrease speed on UP/DOWN Arrows.
            if (_mFrameServer.VideoFile.Loaded)
            {
                var jumpFactor = 25;
                if ((ModifierKeys & Keys.Control) == Keys.Control)
                {
                    jumpFactor = 1;
                }
                else if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    jumpFactor = 10;
                }

                if (e.KeyCode == Keys.Down)
                {
                    sldrSpeed.ForceValue(jumpFactor*((sldrSpeed.Value - 1)/jumpFactor));
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Up)
                {
                    sldrSpeed.ForceValue(jumpFactor*((sldrSpeed.Value/jumpFactor) + 1));
                    e.Handled = true;
                }
            }
        }

        private void lblSpeedTuner_DoubleClick(object sender, EventArgs e)
        {
            // Double click on the speed label : Back to 100%
            sldrSpeed.ForceValue(sldrSpeed.StickyValue);
        }

        private void UpdateSpeedLabel()
        {
            if (_mFHighSpeedFactor != 1.0)
            {
                var fRealtimePercentage = _mFSlowmotionPercentage/_mFHighSpeedFactor;
                lblSpeedTuner.Text = string.Format("{0} {1:0.00}%", ScreenManagerLang.lblSpeedTuner_Text,
                    fRealtimePercentage);
            }
            else
            {
                if (_mFSlowmotionPercentage%1 == 0)
                {
                    lblSpeedTuner.Text = ScreenManagerLang.lblSpeedTuner_Text + " " + _mFSlowmotionPercentage + "%";
                }
                else
                {
                    // Special case when the speed percentage is coming from the other screen and is not an integer.
                    lblSpeedTuner.Text = string.Format("{0} {1:0.00}%", ScreenManagerLang.lblSpeedTuner_Text,
                        _mFSlowmotionPercentage);
                }
            }
        }

        #endregion Speed Slider

        #region Loop Mode

        private void buttonPlayingMode_Click(object sender, EventArgs e)
        {
            // Playback mode ('Once' or 'Loop').
            if (_mFrameServer.VideoFile.Loaded)
            {
                OnPoke();

                if (_mEPlayingMode == PlayingMode.Once)
                {
                    _mEPlayingMode = PlayingMode.Loop;
                }
                else if (_mEPlayingMode == PlayingMode.Loop)
                {
                    _mEPlayingMode = PlayingMode.Once;
                }

                UpdatePlayingModeButton();
            }
        }

        private void UpdatePlayingModeButton()
        {
            if (_mEPlayingMode == PlayingMode.Once)
            {
                buttonPlayingMode.Image = Resources.playmodeonce;
                toolTips.SetToolTip(buttonPlayingMode, ScreenManagerLang.ToolTip_PlayingMode_Once);
            }
            else if (_mEPlayingMode == PlayingMode.Loop)
            {
                buttonPlayingMode.Image = Resources.playmodeloop;
                toolTips.SetToolTip(buttonPlayingMode, ScreenManagerLang.ToolTip_PlayingMode_Loop);
            }
        }

        #endregion Loop Mode

        #endregion Video Controls

        #region Auto Stretch & Manual Resize

        private void StretchSqueezeSurface()
        {
            if (_mFrameServer.Loaded)
            {
                // Check if the image was loaded squeezed.
                // (happen when screen control isn't being fully expanded at video load time.)
                if (pbSurfaceScreen.Height < panelCenter.Height && _mFrameServer.CoordinateSystem.Stretch < 1.0)
                {
                    _mFrameServer.CoordinateSystem.Stretch = 1.0;
                }

                //---------------------------------------------------------------
                // Check if the stretch factor is not going to outsize the panel.
                // If so, force maximized, unless screen is smaller than video.
                //---------------------------------------------------------------
                var iTargetHeight =
                    (int) (_mFrameServer.VideoFile.Infos.iDecodingHeight*_mFrameServer.CoordinateSystem.Stretch);
                var iTargetWidth =
                    (int) (_mFrameServer.VideoFile.Infos.iDecodingWidth*_mFrameServer.CoordinateSystem.Stretch);

                if (iTargetHeight > panelCenter.Height || iTargetWidth > panelCenter.Width)
                {
                    if (_mFrameServer.CoordinateSystem.Stretch > 1.0)
                    {
                        _mBStretchModeOn = true;
                    }
                }

                if ((_mBStretchModeOn) || (_mFrameServer.VideoFile.Infos.iDecodingWidth > panelCenter.Width) ||
                    (_mFrameServer.VideoFile.Infos.iDecodingHeight > panelCenter.Height))
                {
                    //-------------------------------------------------------------------------------
                    // Maximiser :
                    //Redimensionner l'image selon la dimension la plus proche de la taille du panel.
                    //-------------------------------------------------------------------------------
                    var widthRatio = (float) _mFrameServer.VideoFile.Infos.iDecodingWidth/panelCenter.Width;
                    var heightRatio = (float) _mFrameServer.VideoFile.Infos.iDecodingHeight/panelCenter.Height;

                    if (widthRatio > heightRatio)
                    {
                        pbSurfaceScreen.Width = panelCenter.Width;
                        pbSurfaceScreen.Height = (int) (_mFrameServer.VideoFile.Infos.iDecodingHeight/widthRatio);

                        _mFrameServer.CoordinateSystem.Stretch = (1/widthRatio);
                    }
                    else
                    {
                        pbSurfaceScreen.Width = (int) (_mFrameServer.VideoFile.Infos.iDecodingWidth/heightRatio);
                        pbSurfaceScreen.Height = panelCenter.Height;

                        _mFrameServer.CoordinateSystem.Stretch = (1/heightRatio);
                    }
                }
                else
                {
                    pbSurfaceScreen.Width =
                        (int) (_mFrameServer.VideoFile.Infos.iDecodingWidth*_mFrameServer.CoordinateSystem.Stretch);
                    pbSurfaceScreen.Height =
                        (int) (_mFrameServer.VideoFile.Infos.iDecodingHeight*_mFrameServer.CoordinateSystem.Stretch);
                }

                // Center
                pbSurfaceScreen.Left = (panelCenter.Width/2) - (pbSurfaceScreen.Width/2);
                pbSurfaceScreen.Top = (panelCenter.Height/2) - (pbSurfaceScreen.Height/2);
                ReplaceResizers();
            }
        }

        private void ReplaceResizers()
        {
            ImageResizerSE.Left = pbSurfaceScreen.Left + pbSurfaceScreen.Width - (ImageResizerSE.Width/2);
            ImageResizerSE.Top = pbSurfaceScreen.Top + pbSurfaceScreen.Height - (ImageResizerSE.Height/2);

            ImageResizerSW.Left = pbSurfaceScreen.Left - (ImageResizerSW.Width/2);
            ImageResizerSW.Top = pbSurfaceScreen.Top + pbSurfaceScreen.Height - (ImageResizerSW.Height/2);

            ImageResizerNE.Left = pbSurfaceScreen.Left + pbSurfaceScreen.Width - (ImageResizerNE.Width/2);
            ImageResizerNE.Top = pbSurfaceScreen.Top - (ImageResizerNE.Height/2);

            ImageResizerNW.Left = pbSurfaceScreen.Left - (ImageResizerNW.Width/2);
            ImageResizerNW.Top = pbSurfaceScreen.Top - (ImageResizerNW.Height/2);
        }

        private void ToggleStretchMode()
        {
            if (!_mBStretchModeOn)
            {
                _mBStretchModeOn = true;
            }
            else
            {
                // Ne pas repasser en stretch mode à false si on est plus petit que l'image
                if (_mFrameServer.CoordinateSystem.Stretch >= 1)
                {
                    _mFrameServer.CoordinateSystem.Stretch = 1;
                    _mBStretchModeOn = false;
                }
            }
            StretchSqueezeSurface();
            _mFrameServer.Metadata.ResizeFinished();
            DoInvalidate();
        }

        private void ImageResizerSE_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var iTargetHeight = (ImageResizerSE.Top - pbSurfaceScreen.Top + e.Y);
                var iTargetWidth = (ImageResizerSE.Left - pbSurfaceScreen.Left + e.X);
                ResizeImage(iTargetWidth, iTargetHeight);
            }
        }

        private void ImageResizerSW_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var iTargetHeight = (ImageResizerSW.Top - pbSurfaceScreen.Top + e.Y);
                var iTargetWidth = pbSurfaceScreen.Width + (pbSurfaceScreen.Left - (ImageResizerSW.Left + e.X));
                ResizeImage(iTargetWidth, iTargetHeight);
            }
        }

        private void ImageResizerNW_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var iTargetHeight = pbSurfaceScreen.Height + (pbSurfaceScreen.Top - (ImageResizerNW.Top + e.Y));
                var iTargetWidth = pbSurfaceScreen.Width + (pbSurfaceScreen.Left - (ImageResizerNW.Left + e.X));
                ResizeImage(iTargetWidth, iTargetHeight);
            }
        }

        private void ImageResizerNE_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var iTargetHeight = pbSurfaceScreen.Height + (pbSurfaceScreen.Top - (ImageResizerNE.Top + e.Y));
                var iTargetWidth = (ImageResizerNE.Left - pbSurfaceScreen.Left + e.X);
                ResizeImage(iTargetWidth, iTargetHeight);
            }
        }

        private void ResizeImage(int iTargetWidth, int iTargetHeight)
        {
            //-------------------------------------------------------------------
            // Resize at the following condition:
            // Bigger than original image size, smaller than panel size.
            //-------------------------------------------------------------------
            if (iTargetHeight > _mFrameServer.VideoFile.Infos.iDecodingHeight &&
                iTargetHeight < panelCenter.Height &&
                iTargetWidth > _mFrameServer.VideoFile.Infos.iDecodingWidth &&
                iTargetWidth < panelCenter.Width)
            {
                var fHeightFactor = ((iTargetHeight)/(double) _mFrameServer.VideoFile.Infos.iDecodingHeight);
                var fWidthFactor = ((iTargetWidth)/(double) _mFrameServer.VideoFile.Infos.iDecodingWidth);

                _mFrameServer.CoordinateSystem.Stretch = (fWidthFactor + fHeightFactor)/2;
                _mBStretchModeOn = false;
                StretchSqueezeSurface();
                DoInvalidate();
            }
        }

        private void Resizers_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ToggleStretchMode();
        }

        private void Resizers_MouseUp(object sender, MouseEventArgs e)
        {
            _mFrameServer.Metadata.ResizeFinished();
            DoInvalidate();
        }

        #endregion Auto Stretch & Manual Resize

        #region Timers & Playloop

        private void StartMultimediaTimer(double interval)
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

            var myData = 0; // dummy data
            _mIdMultimediaTimer = timeSetEvent((int) interval, // Délai en ms.
                (int) interval, // Resolution en ms.
                _mTimerEventHandler, // event handler du tick.
                ref myData, // ?
                TimePeriodic | TimeKillSynchronous); // Type d'event (1=periodic)

            Log.Debug("PlayerScreen multimedia timer started.");

            // Deactivate all keyframes during playing.
            ActivateKeyframe(-1);
        }

        private void StopMultimediaTimer()
        {
            if (_mIdMultimediaTimer != 0)
            {
                timeKillEvent(_mIdMultimediaTimer);
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                Log.Debug("PlayerScreen multimedia timer stopped.");
            }
        }

        private void MultimediaTimer_Tick(uint id, uint msg, ref int userCtx, int rsv1, int rsv2)
        {
            // We comes here more often than we should, by bunches.
            if (_mFrameServer.VideoFile.Loaded)
            {
                BeginInvoke(_mPlayLoop);
            }
        }

        private void PlayLoop_Invoked()
        {
            //--------------------------------------------------------------
            // Function called by main timer event handler, on each tick.
            // Asynchronously if needed.
            //--------------------------------------------------------------

            //-----------------------------------------------------------------------------
            // Attention, comme la fonction est assez longue et qu'elle met à jour l'UI,
            // Il y a un risque de UI unresponsive si les BeginInvokes sont trop fréquents.
            // tout le temps sera passé ici, et on ne pourra plus répondre aux évents
            //
            // Solution : n'effectuer le traitement long que si la form est idle.
            // ca va dropper des frames, mais on pourra toujours utiliser l'appli.
            // Par contre on doit quand même mettre à jour NextFrame.
            //-----------------------------------------------------------------------------

            /*m_Stopwatch.Stop();
            log.Debug(String.Format("Back in Playloop. Elapsed: {0} ms.", m_Stopwatch.ElapsedMilliseconds));

            m_Stopwatch.Reset();
            m_Stopwatch.Start();*/

            var bStopAtEnd = false;
            //----------------------------------------------------------------------------
            // En prévision de l'appel à ShowNextFrame, on vérifie qu'on ne va pas sortir.
            // Si c'est le cas, on stoppe la lecture pour rewind.
            // m_iFramesToDecode est toujours strictement positif. (Car on est en Play)
            //----------------------------------------------------------------------------
            var iTargetPosition = _mICurrentPosition +
                                  (_mIFramesToDecode*_mFrameServer.VideoFile.Infos.iAverageTimeStampsPerFrame);

            if ((iTargetPosition > _mISelEnd) || (iTargetPosition >= (_mIStartingPosition + _mITotalDuration)))
            {
                Log.Debug("End of video reached");

                // We have reached the end of the video.
                if (_mEPlayingMode == PlayingMode.Once)
                {
                    StopPlaying();
                    bStopAtEnd = true;
                }
                else
                {
                    if (_mBSynched)
                    {
                        // Go into Pause mode.
                        StopPlaying();
                    }
                    else
                    {
                        // If auto rewind, only stops timer,
                        // playback will restart automatically if everything is ok.
                        StopMultimediaTimer();
                    }
                    _mBSeekToStart = true;
                }

                //Close Tracks
                _mFrameServer.Metadata.StopAllTracking();
            }

            //-----------------------------------------
            // Moving playhead and rendering mechanics.
            //-----------------------------------------
            if (_mBIsIdle || _mBSeekToStart || bStopAtEnd)
            {
                if (_mBSeekToStart)
                {
                    // Rewind to begining.
                    if (ShowNextFrame(_mISelStart, true) == 0)
                    {
                        if (_mBSynched)
                        {
                            //log.Debug("Stopping on frame [0] after auto rewind.");
                            // Stop on frame [0]. Will be restarted by the dynamic sync when needed.
                        }
                        else
                        {
                            // Auto restart timer if everything went fine.
                            StartMultimediaTimer(GetPlaybackFrameInterval());
                        }
                    }
                    else
                    {
                        Log.Debug("Error while decoding first frame after auto rewind.");
                        StopPlaying();
                    }
                    _mBSeekToStart = false;
                }
                else if (bStopAtEnd)
                {
                    // Nothing to do. Playback was stopped on last frame.
                }
                else
                {
                    // Not rewinding and not stopped on last frame.
                    // display next frame(s) in queue.
                    ShowNextFrame(-1, true);
                }

                UpdateNavigationCursor();

                if (_mBShowInfos)
                {
                    UpdateDebugInfos();
                }

                // Empty frame queue.
                _mIFramesToDecode = 1;
            }
            else
            {
                //-------------------------------------------------------------------------------
                // Not Idle.
                // Cannot decode frame now.
                // Enqueue frames.
                //
                // Side effect: queue will always stabilize right under the treshold.
                //-------------------------------------------------------------------------------

                // If we a re currently tracking a point, do not try to keep with the framerate.

                var bTracking = false;
                foreach (AbstractDrawing ad in _mFrameServer.Metadata.ExtraDrawings)
                {
                    var t = ad as Track;
                    if (t != null && t.Status == TrackStatus.Edit)
                    {
                        bTracking = true;
                        break;
                    }
                }

                if (!bTracking)
                {
                    _mIFramesToDecode++;
                    _mIDroppedFrames++;
                    //log.Debug(String.Format("Dropping. Total :{0} frames.", m_iDroppedFrames));
                }

                //-------------------------------------------------------------------------------
                // Mécanisme de sécurité.
                //
                // Si le nb de drops augmente alors que la vitesse de défilement n'a pas été touchée
                // On à atteint le seuil de non retour :
                // Les images prennent plus de temps à décoder/afficher que l'intervalle du timer.
                // -> Diminuer automatiquement la vitesse.
                //-------------------------------------------------------------------------------
                if (_mIFramesToDecode > 6)
                {
                    _mIFramesToDecode = 0;
                    if (sldrSpeed.Value >= sldrSpeed.Minimum + sldrSpeed.LargeChange)
                    {
                        sldrSpeed.ForceValue(sldrSpeed.Value - sldrSpeed.LargeChange);
                    }
                }
            }

            //m_Stopwatch.Stop();
            //log.Debug(String.Format("Exiting Playloop. Took: {0} ms.", m_Stopwatch.ElapsedMilliseconds));
        }

        private void IdleDetector(object sender, EventArgs e)
        {
            //log.Debug("back to idle");
            _mBIsIdle = true;
        }

        private ReadResult ShowNextFrame(long iSeekTarget, bool bAllowUiUpdate)
        {
            //---------------------------------------------------------------------------
            // Demande au PlayerServer de remplir la bmp avec la prochaine frame requise.
            // 2 paramètres, dépendant du contexte.
            //
            // Si _iSeekTarget = -1, utilise m_iFramesToDecode.
            // Sinon, utilise directement _iSeekTarget.
            // m_iFramesToDecode peut être négatif quand on recule.
            //---------------------------------------------------------------------------
            _mBIsIdle = false;

            //m_Stopwatch.Reset();
            //m_Stopwatch.Start();

            var res = _mFrameServer.VideoFile.ReadFrame(iSeekTarget, _mIFramesToDecode);

            if (res == ReadResult.Success)
            {
                _mIDecodedFrames++;
                _mICurrentPosition = _mFrameServer.VideoFile.Selection.iCurrentTimeStamp;

                // Compute or stop tracking
                if (_mFrameServer.Metadata.HasTrack())
                {
                    if (iSeekTarget >= 0 || _mIFramesToDecode > 1)
                    {
                        // Tracking works frame to frame.
                        // If playhead jumped several frames at once or moved back, we force-stop tracking.
                        _mFrameServer.Metadata.StopAllTracking();
                    }
                    else
                    {
                        foreach (AbstractDrawing ad in _mFrameServer.Metadata.ExtraDrawings)
                        {
                            var t = ad as Track;
                            if (t != null && t.Status == TrackStatus.Edit)
                            {
                                t.TrackCurrentPosition(_mICurrentPosition, _mFrameServer.VideoFile.CurrentImage);
                            }
                        }
                    }
                    UpdateFramesMarkers();
                }

                // Display image
                if (bAllowUiUpdate) DoInvalidate();

                // Report image for synchro and merge.
                ReportForSyncMerge();
            }
            else
            {
                switch (res)
                {
                    case ReadResult.MovieNotLoaded:
                    {
                        // This will be a silent error.
                        break;
                    }
                    case ReadResult.MemoryNotAllocated:
                    {
                        // SHOW_NEXT_FRAME_ALLOC_ERROR
                        StopPlaying(bAllowUiUpdate);

                        // This will be a silent error.
                        // It is very low level and seem to always come in pair with another error
                        // for which we'll show the dialog.
                        break;
                    }
                    case ReadResult.FrameNotRead:
                    {
                        //------------------------------------------------------------------------------------
                        // SHOW_NEXT_FRAME_READFRAME_ERROR
                        // Frame bloquante ou fin de fichier.
                        // On fait une demande de jump jusqu'à la fin de la selection.
                        // Au prochain tick du timer, on prendra la décision d'arrêter la vidéo
                        // ou pas en fonction du PlayingMode. (et on se replacera en début de selection)
                        //
                        // Possibilité que cette même frame ne soit plus bloquante lors des passages suivants.
                        //------------------------------------------------------------------------------------
                        _mICurrentPosition = _mISelEnd;
                        if (bAllowUiUpdate)
                        {
                            trkSelection.SelPos = _mICurrentPosition;
                            DoInvalidate();
                        }
                        //Close Tracks
                        _mFrameServer.Metadata.StopAllTracking();

                        break;
                    }
                    case ReadResult.ImageNotConverted:
                    {
                        //-------------------------------------
                        // SHOW_NEXT_FRAME_IMAGE_CONVERT_ERROR
                        // La Bitmap n'a pas pu être créé à partir des octets
                        // (format d'image non standard.)
                        //-------------------------------------
                        StopPlaying(bAllowUiUpdate);
                        break;
                    }
                    default:
                    {
                        //------------------------------------------------
                        // Erreur imprévue (donc grave) :
                        // on reverse le compteur et on arrète la lecture.
                        //------------------------------------------------
                        StopPlaying(bAllowUiUpdate);

                        break;
                    }
                }
            }

            //m_Stopwatch.Stop();
            //log.Debug(String.Format("ShowNextFrame: {0} ms.", m_Stopwatch.ElapsedMilliseconds));

            return res;
        }

        private void StopPlaying(bool bAllowUiUpdate)
        {
            if (_mFrameServer.VideoFile.Loaded)
            {
                if (IsCurrentlyPlaying)
                {
                    StopMultimediaTimer();
                    IsCurrentlyPlaying = false;
                    Application.Idle -= IdleDetector;
                    _mIFramesToDecode = 0;

                    if (bAllowUiUpdate)
                    {
                        buttonPlay.Image = Resources.liqplay17;
                        DoInvalidate();
                    }
                }
            }
        }

        private void mnuSetCaptureSpeed_Click(object sender, EventArgs e)
        {
            DisplayConfigureSpeedBox(false);
        }

        private void lblTimeCode_DoubleClick(object sender, EventArgs e)
        {
            // Same as mnuSetCaptureSpeed_Click but different location.
            DisplayConfigureSpeedBox(true);
        }

        public void DisplayConfigureSpeedBox(bool center)
        {
            //--------------------------------------------------------------------
            // Display the dialog box that let the user specify the capture speed.
            // Used to adpat time for high speed cameras.
            //--------------------------------------------------------------------
            if (_mFrameServer.VideoFile.Loaded)
            {
                var dp = DelegatesPool.Instance();
                if (dp.DeactivateKeyboardHandler != null)
                {
                    dp.DeactivateKeyboardHandler();
                }

                var fcs = new FormConfigureSpeed(_mFrameServer.VideoFile.Infos.fFps, _mFHighSpeedFactor);
                if (center)
                {
                    fcs.StartPosition = FormStartPosition.CenterScreen;
                }
                else
                {
                    fcs.StartPosition = FormStartPosition.Manual;
                    ScreenManagerKernel.LocateForm(fcs);
                }

                if (fcs.ShowDialog() == DialogResult.OK)
                {
                    _mFHighSpeedFactor = fcs.SlowFactor;
                }

                fcs.Dispose();

                if (dp.ActivateKeyboardHandler != null)
                {
                    dp.ActivateKeyboardHandler();
                }

                // Update times.
                UpdateSelectionLabels();
                UpdateCurrentPositionLabel();
                UpdateSpeedLabel();
                _mPlayerScreenUiHandler.PlayerScreenUI_SpeedChanged(true);
                _mFrameServer.Metadata.CalibrationHelper.FramesPerSeconds = _mFrameServer.VideoFile.Infos.fFps*
                                                                            _mFHighSpeedFactor;
                DoInvalidate();
            }
        }

        private double GetPlaybackFrameInterval()
        {
            // Returns the playback interval between frames in Milliseconds, taking slow motion slider into account.
            // m_iSlowmotionPercentage must be > 0.
            if (_mFrameServer.VideoFile.Loaded && _mFrameServer.VideoFile.Infos.fFrameInterval > 0 &&
                _mFSlowmotionPercentage > 0)
            {
                return (_mFrameServer.VideoFile.Infos.fFrameInterval/(_mFSlowmotionPercentage/100.0));
            }
            return 40;
        }

        private void DeselectionTimer_OnTick(object sender, EventArgs e)
        {
            // Deselect the currently selected drawing.
            // This is used for drawings that must show extra stuff for being transformed, but we
            // don't want to show the extra stuff all the time for clarity.

            _mFrameServer.Metadata.SelectedDrawingFrame = -1;
            _mFrameServer.Metadata.SelectedDrawing = -1;
            Log.Debug("Deselection timer fired.");
            _mDeselectionTimer.Stop();
            DoInvalidate();
        }

        #endregion Timers & Playloop

        #region Culture

        private void ReloadMenusCulture()
        {
            // Reload the text for each menu.
            // this is done at construction time and at RefreshUICulture time.

            // 1. Default context menu.
            _mnuDirectTrack.Text = ScreenManagerLang.mnuTrackTrajectory;
            _mnuPlayPause.Text = ScreenManagerLang.mnuPlayPause;
            _mnuSetCaptureSpeed.Text = ScreenManagerLang.mnuSetCaptureSpeed;
            _mnuSavePic.Text = ScreenManagerLang.Generic_SaveImage;
            _mnuSendPic.Text = ScreenManagerLang.mnuSendPic;
            _mnuCloseScreen.Text = ScreenManagerLang.mnuCloseScreen;

            // 2. Drawings context menu.
            _mnuConfigureDrawing.Text = ScreenManagerLang.mnuConfigureDrawing_ColorSize;
            _mnuConfigureFading.Text = ScreenManagerLang.mnuConfigureFading;
            _mnuConfigureOpacity.Text = ScreenManagerLang.Generic_Opacity;
            _mnuTrackTrajectory.Text = ScreenManagerLang.mnuTrackTrajectory;
            _mnuGotoKeyframe.Text = ScreenManagerLang.mnuGotoKeyframe;
            _mnuDeleteDrawing.Text = ScreenManagerLang.mnuDeleteDrawing;

            // 3. Tracking pop menu (Restart, Stop tracking)
            _mnuStopTracking.Text = ScreenManagerLang.mnuStopTracking;
            _mnuRestartTracking.Text = ScreenManagerLang.mnuRestartTracking;
            _mnuDeleteTrajectory.Text = ScreenManagerLang.mnuDeleteTrajectory;
            _mnuDeleteEndOfTrajectory.Text = ScreenManagerLang.mnuDeleteEndOfTrajectory;
            _mnuConfigureTrajectory.Text = ScreenManagerLang.Generic_Configuration;

            // 4. Chrono pop menu (Start, Stop, Hide, etc.)
            _mnuChronoConfigure.Text = ScreenManagerLang.Generic_Configuration;
            _mnuChronoStart.Text = ScreenManagerLang.mnuChronoStart;
            _mnuChronoStop.Text = ScreenManagerLang.mnuChronoStop;
            _mnuChronoHide.Text = ScreenManagerLang.mnuChronoHide;
            _mnuChronoCountdown.Text = ScreenManagerLang.mnuChronoCountdown;
            _mnuChronoDelete.Text = ScreenManagerLang.mnuChronoDelete;

            // 5. Magnifier
            _mnuMagnifier150.Text = ScreenManagerLang.mnuMagnifier150;
            _mnuMagnifier175.Text = ScreenManagerLang.mnuMagnifier175;
            _mnuMagnifier200.Text = ScreenManagerLang.mnuMagnifier200;
            _mnuMagnifier225.Text = ScreenManagerLang.mnuMagnifier225;
            _mnuMagnifier250.Text = ScreenManagerLang.mnuMagnifier250;
            _mnuMagnifierDirect.Text = ScreenManagerLang.mnuMagnifierDirect;
            _mnuMagnifierQuit.Text = ScreenManagerLang.mnuMagnifierQuit;
        }

        private void ReloadTooltipsCulture()
        {
            // Video controls
            toolTips.SetToolTip(buttonPlay, ScreenManagerLang.ToolTip_Play);
            toolTips.SetToolTip(buttonGotoPrevious, ScreenManagerLang.ToolTip_Back);
            toolTips.SetToolTip(buttonGotoNext, ScreenManagerLang.ToolTip_Next);
            toolTips.SetToolTip(buttonGotoFirst, ScreenManagerLang.ToolTip_First);
            toolTips.SetToolTip(buttonGotoLast, ScreenManagerLang.ToolTip_Last);
            if (_mEPlayingMode == PlayingMode.Once)
            {
                toolTips.SetToolTip(buttonPlayingMode, ScreenManagerLang.ToolTip_PlayingMode_Once);
            }
            else
            {
                toolTips.SetToolTip(buttonPlayingMode, ScreenManagerLang.ToolTip_PlayingMode_Loop);
            }

            // Export buttons
            toolTips.SetToolTip(btnSnapShot, ScreenManagerLang.Generic_SaveImage);
            toolTips.SetToolTip(btnRafale, ScreenManagerLang.ToolTip_Rafale);
            toolTips.SetToolTip(btnDiaporama, ScreenManagerLang.ToolTip_SaveDiaporama);
            toolTips.SetToolTip(btnSaveVideo, ScreenManagerLang.dlgSaveVideoTitle);
            toolTips.SetToolTip(btnPausedVideo, ScreenManagerLang.ToolTip_SavePausedVideo);

            // Working zone and sliders.
            if (_mBHandlersLocked)
            {
                toolTips.SetToolTip(btn_HandlersLock, ScreenManagerLang.LockSelectionUnlock);
            }
            else
            {
                toolTips.SetToolTip(btn_HandlersLock, ScreenManagerLang.LockSelectionLock);
            }
            toolTips.SetToolTip(btnSetHandlerLeft, ScreenManagerLang.ToolTip_SetHandlerLeft);
            toolTips.SetToolTip(btnSetHandlerRight, ScreenManagerLang.ToolTip_SetHandlerRight);
            toolTips.SetToolTip(btnHandlersReset, ScreenManagerLang.ToolTip_ResetWorkingZone);
            trkSelection.ToolTip = ScreenManagerLang.ToolTip_trkSelection;
            sldrSpeed.ToolTip = ScreenManagerLang.ToolTip_sldrSpeed;

            // Drawing tools
            foreach (ToolStripItem tsi in stripDrawingTools.Items)
            {
                if (tsi is ToolStripButton)
                {
                    AbstractDrawingTool tool = tsi.Tag as AbstractDrawingTool;
                    if (tool != null)
                    {
                        tsi.ToolTipText = tool.DisplayName;
                    }
                }
            }

            _mBtnAddKeyFrame.ToolTipText = ScreenManagerLang.ToolTip_AddKeyframe;
            _mBtnShowComments.ToolTipText = ScreenManagerLang.ToolTip_ShowComments;
            _mBtnToolPresets.ToolTipText = ScreenManagerLang.ToolTip_ColorProfile;
        }

        #endregion Culture

        #region SurfaceScreen Events

        private void SurfaceScreen_MouseDown(object sender, MouseEventArgs e)
        {
            if (_mFrameServer.VideoFile != null)
            {
                if (_mFrameServer.VideoFile.Loaded)
                {
                    _mDeselectionTimer.Stop();

                    if (e.Button == MouseButtons.Left)
                    {
                        // Magnifier can be moved even when the video is playing.
                        var bWasPlaying = false;

                        if (IsCurrentlyPlaying)
                        {
                            if ((_mActiveTool == _mPointerTool) &&
                                (_mFrameServer.Metadata.Magnifier.Mode != MagnifierMode.NotVisible) &&
                                (_mFrameServer.Metadata.Magnifier.IsOnObject(e)))
                            {
                                _mFrameServer.Metadata.Magnifier.OnMouseDown(e);
                            }
                            else
                            {
                                // MouseDown while playing: Halt the video.
                                StopPlaying();
                                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();
                                ActivateKeyframe(_mICurrentPosition);
                                bWasPlaying = true;
                                ToastPause();
                            }
                        }

                        if (!IsCurrentlyPlaying && !_mBDrawtimeFiltered)
                        {
                            //-------------------------------------
                            // Action begins:
                            // Move or set magnifier
                            // Move or set Drawing
                            // Move or set Chrono / Track
                            //-------------------------------------

                            _mDescaledMouse = _mFrameServer.CoordinateSystem.Untransform(e.Location);

                            // 1. Pass all DrawingText to normal mode
                            _mFrameServer.Metadata.AllDrawingTextToNormalMode();

                            if (_mActiveTool == _mPointerTool)
                            {
                                // 1. Manipulating an object or Magnifier
                                var bMovingMagnifier = false;
                                var bDrawingHit = false;

                                // Show the grabbing hand cursor.
                                SetCursor(_mPointerTool.GetCursor(1));

                                if (_mFrameServer.Metadata.Magnifier.Mode == MagnifierMode.Indirect)
                                {
                                    bMovingMagnifier = _mFrameServer.Metadata.Magnifier.OnMouseDown(e);
                                }

                                if (!bMovingMagnifier)
                                {
                                    // Magnifier wasn't hit or is not in use,
                                    // try drawings (including chronos, grids, etc.)
                                    bDrawingHit = _mPointerTool.OnMouseDown(_mFrameServer.Metadata,
                                        _mIActiveKeyFrameIndex, _mDescaledMouse, _mICurrentPosition,
                                        _mPrefManager.DefaultFading.Enabled);
                                }

                                if (!bDrawingHit && !bWasPlaying)
                                {
                                    // MouseDown in arbitrary location and we were halted already.

                                    // We cannot restart the video here because this MouseDown may actually be the start
                                    // of a double click. (expand screen)
                                }
                            }
                            else if (_mActiveTool == ToolManager.Chrono)
                            {
                                // Add a Chrono.
                                var chrono =
                                    (DrawingChrono)
                                        _mActiveTool.GetNewDrawing(_mDescaledMouse, _mICurrentPosition,
                                            _mFrameServer.Metadata.AverageTimeStampsPerFrame);
                                _mFrameServer.Metadata.AddChrono(chrono);
                                _mActiveTool = _mPointerTool;
                            }
                            else
                            {
                                //-----------------------
                                // Creating a new Drawing
                                //-----------------------
                                _mFrameServer.Metadata.SelectedExtraDrawing = -1;

                                // Add a KeyFrame here if it doesn't exist.
                                AddKeyframe();

                                if (_mActiveTool != ToolManager.Label)
                                {
                                    // Add an instance of a drawing from the active tool to the current keyframe.
                                    // The drawing is initialized with the current mouse coordinates.
                                    AbstractDrawing ad = _mActiveTool.GetNewDrawing(_mDescaledMouse, _mICurrentPosition,
                                        _mFrameServer.Metadata.AverageTimeStampsPerFrame);

                                    _mFrameServer.Metadata[_mIActiveKeyFrameIndex].AddDrawing(ad);
                                    _mFrameServer.Metadata.SelectedDrawingFrame = _mIActiveKeyFrameIndex;
                                    _mFrameServer.Metadata.SelectedDrawing = 0;

                                    // Post creation hacks.
                                    if (ad is DrawingLine2D)
                                    {
                                        ((DrawingLine2D) ad).ParentMetadata = _mFrameServer.Metadata;
                                        ((DrawingLine2D) ad).ShowMeasure = DrawingToolLine2D.ShowMeasure;
                                    }
                                    else if (ad is DrawingCross2D)
                                    {
                                        ((DrawingCross2D) ad).ParentMetadata = _mFrameServer.Metadata;
                                        ((DrawingCross2D) ad).ShowCoordinates = DrawingToolCross2D.ShowCoordinates;
                                    }
                                    else if (ad is DrawingPlane)
                                    {
                                        ((DrawingPlane) ad).SetLocations(_mFrameServer.Metadata.ImageSize, 1.0,
                                            new Point(0, 0));
                                    }
                                }
                                else
                                {
                                    // We are using the Text Tool. This is a special case because
                                    // if we are on an existing Textbox, we just go into edit mode
                                    // otherwise, we add and setup a new textbox.
                                    var bEdit = false;
                                    foreach (
                                        AbstractDrawing ad in _mFrameServer.Metadata[_mIActiveKeyFrameIndex].Drawings)
                                    {
                                        if (ad is DrawingText)
                                        {
                                            int hitRes = ad.HitTest(_mDescaledMouse, _mICurrentPosition);
                                            if (hitRes >= 0)
                                            {
                                                bEdit = true;
                                                ((DrawingText) ad).SetEditMode(true, _mFrameServer.CoordinateSystem);
                                            }
                                        }
                                    }

                                    // If we are not on an existing textbox : create new DrawingText.
                                    if (!bEdit)
                                    {
                                        _mFrameServer.Metadata[_mIActiveKeyFrameIndex].AddDrawing(
                                            _mActiveTool.GetNewDrawing(_mDescaledMouse, _mICurrentPosition,
                                                _mFrameServer.Metadata.AverageTimeStampsPerFrame));
                                        _mFrameServer.Metadata.SelectedDrawingFrame = _mIActiveKeyFrameIndex;
                                        _mFrameServer.Metadata.SelectedDrawing = 0;

                                        var dt =
                                            (DrawingText) _mFrameServer.Metadata[_mIActiveKeyFrameIndex].Drawings[0];

                                        dt.ContainerScreen = pbSurfaceScreen;
                                        dt.SetEditMode(true, _mFrameServer.CoordinateSystem);
                                        panelCenter.Controls.Add(dt.EditBox);
                                        dt.EditBox.BringToFront();
                                        dt.EditBox.Focus();
                                    }
                                }
                            }
                        }
                    }
                    else if (e.Button == MouseButtons.Right)
                    {
                        // Show the right Pop Menu depending on context.
                        // (Drawing, Trajectory, Chronometer, Magnifier, Nothing)

                        _mDescaledMouse = _mFrameServer.CoordinateSystem.Untransform(e.Location);

                        if (!IsCurrentlyPlaying)
                        {
                            _mFrameServer.Metadata.UnselectAll();
                            AbstractDrawing hitDrawing = null;

                            if (_mBDrawtimeFiltered)
                            {
                                _mnuDirectTrack.Visible = false;
                                _mnuSendPic.Visible = false;
                                panelCenter.ContextMenuStrip = _popMenu;
                            }
                            else if (_mFrameServer.Metadata.IsOnDrawing(_mIActiveKeyFrameIndex, _mDescaledMouse,
                                _mICurrentPosition))
                            {
                                // Rebuild the context menu according to the capabilities of the drawing we are on.

                                AbstractDrawing ad =
                                    _mFrameServer.Metadata.Keyframes[_mFrameServer.Metadata.SelectedDrawingFrame]
                                        .Drawings[_mFrameServer.Metadata.SelectedDrawing];
                                if (ad != null)
                                {
                                    _popMenuDrawings.Items.Clear();

                                    // Generic context menu from drawing capabilities.
                                    if ((ad.Caps & DrawingCapabilities.ConfigureColor) ==
                                        DrawingCapabilities.ConfigureColor)
                                    {
                                        _mnuConfigureDrawing.Text = ScreenManagerLang.mnuConfigureDrawing_Color;
                                        _popMenuDrawings.Items.Add(_mnuConfigureDrawing);
                                    }

                                    if ((ad.Caps & DrawingCapabilities.ConfigureColorSize) ==
                                        DrawingCapabilities.ConfigureColorSize)
                                    {
                                        _mnuConfigureDrawing.Text = ScreenManagerLang.mnuConfigureDrawing_ColorSize;
                                        _popMenuDrawings.Items.Add(_mnuConfigureDrawing);
                                    }

                                    if (_mPrefManager.DefaultFading.Enabled &&
                                        ((ad.Caps & DrawingCapabilities.Fading) == DrawingCapabilities.Fading))
                                    {
                                        _popMenuDrawings.Items.Add(_mnuConfigureFading);
                                    }

                                    if ((ad.Caps & DrawingCapabilities.Opacity) == DrawingCapabilities.Opacity)
                                    {
                                        _popMenuDrawings.Items.Add(_mnuConfigureOpacity);
                                    }

                                    _popMenuDrawings.Items.Add(_mnuSepDrawing);

                                    // Specific menus. Hosted by the drawing itself.
                                    var hasExtraMenu = (ad.ContextMenu != null && ad.ContextMenu.Count > 0);
                                    if (hasExtraMenu)
                                    {
                                        foreach (ToolStripMenuItem tsmi in ad.ContextMenu)
                                        {
                                            tsmi.Tag = (Action) DoInvalidate;
                                                // Inject dependency on this screen's invalidate method.
                                            _popMenuDrawings.Items.Add(tsmi);
                                        }
                                    }

                                    var gotoVisible = (_mPrefManager.DefaultFading.Enabled &&
                                                       (ad.InfosFading.ReferenceTimestamp != _mICurrentPosition));
                                    if (gotoVisible)
                                        _popMenuDrawings.Items.Add(_mnuGotoKeyframe);

                                    if (hasExtraMenu || gotoVisible)
                                        _popMenuDrawings.Items.Add(_mnuSepDrawing2);

                                    // Generic delete
                                    _popMenuDrawings.Items.Add(_mnuDeleteDrawing);

                                    // Set this menu as the context menu.
                                    panelCenter.ContextMenuStrip = _popMenuDrawings;
                                }
                            }
                            else if (
                                (hitDrawing =
                                    _mFrameServer.Metadata.IsOnExtraDrawing(_mDescaledMouse, _mICurrentPosition)) !=
                                null)
                            {
                                // Unlike attached drawings, each extra drawing type has its own context menu for now.

                                if (hitDrawing is DrawingChrono)
                                {
                                    // Toggle to countdown is active only if we have a stop time.
                                    _mnuChronoCountdown.Enabled = ((DrawingChrono) hitDrawing).HasTimeStop;
                                    _mnuChronoCountdown.Checked = ((DrawingChrono) hitDrawing).CountDown;
                                    panelCenter.ContextMenuStrip = _popMenuChrono;
                                }
                                else if (hitDrawing is Track)
                                {
                                    if (((Track) hitDrawing).Status == TrackStatus.Edit)
                                    {
                                        _mnuStopTracking.Visible = true;
                                        _mnuRestartTracking.Visible = false;
                                    }
                                    else
                                    {
                                        _mnuStopTracking.Visible = false;
                                        _mnuRestartTracking.Visible = true;
                                    }

                                    panelCenter.ContextMenuStrip = _popMenuTrack;
                                }
                            }
                            else if (_mFrameServer.Metadata.Magnifier.Mode == MagnifierMode.Indirect &&
                                     _mFrameServer.Metadata.Magnifier.IsOnObject(e))
                            {
                                panelCenter.ContextMenuStrip = _popMenuMagnifier;
                            }
                            else if (_mActiveTool != _mPointerTool)
                            {
                                // Launch FormToolPreset.
                                var ftp = new FormToolPresets(_mActiveTool);
                                ScreenManagerKernel.LocateForm(ftp);
                                ftp.ShowDialog();
                                ftp.Dispose();
                                UpdateCursor();
                            }
                            else
                            {
                                // No drawing touched and no tool selected, but not currently playing. Default menu.
                                _mnuDirectTrack.Visible = true;
                                _mnuSendPic.Visible = _mBSynched;
                                panelCenter.ContextMenuStrip = _popMenu;
                            }
                        }
                        else
                        {
                            // Currently playing.
                            _mnuDirectTrack.Visible = false;
                            _mnuSendPic.Visible = false;
                            panelCenter.ContextMenuStrip = _popMenu;
                        }
                    }

                    DoInvalidate();
                }
            }
        }

        private void SurfaceScreen_MouseMove(object sender, MouseEventArgs e)
        {
            // We must keep the same Z order.
            // 1:Magnifier, 2:Drawings, 3:Chronos/Tracks
            // When creating a drawing, the active tool will stay on this drawing until its setup is over.
            // After the drawing is created, we either fall back to Pointer tool or stay on the same tool.

            if (_mFrameServer.VideoFile != null && _mFrameServer.VideoFile.Loaded)
            {
                if (e.Button == MouseButtons.None && _mFrameServer.Metadata.Magnifier.Mode == MagnifierMode.Direct)
                {
                    _mFrameServer.Metadata.Magnifier.MouseX = e.X;
                    _mFrameServer.Metadata.Magnifier.MouseY = e.Y;

                    if (!IsCurrentlyPlaying)
                    {
                        DoInvalidate();
                    }
                }
                else if (e.Button == MouseButtons.Left)
                {
                    if (_mActiveTool != _mPointerTool)
                    {
                        // Tools that are not IInitializable should reset to Pointer tool after creation.
                        if (_mIActiveKeyFrameIndex >= 0 && !IsCurrentlyPlaying)
                        {
                            // Currently setting the second point of a Drawing.
                            var initializableDrawing =
                                _mFrameServer.Metadata[_mIActiveKeyFrameIndex].Drawings[0] as IInitializable;
                            if (initializableDrawing != null)
                            {
                                initializableDrawing.ContinueSetup(
                                    _mFrameServer.CoordinateSystem.Untransform(new Point(e.X, e.Y)));
                            }
                        }
                    }
                    else
                    {
                        var bMovingMagnifier = false;
                        if (_mFrameServer.Metadata.Magnifier.Mode == MagnifierMode.Indirect)
                        {
                            bMovingMagnifier = _mFrameServer.Metadata.Magnifier.OnMouseMove(e);
                        }

                        if (!bMovingMagnifier && _mActiveTool == _mPointerTool)
                        {
                            if (!IsCurrentlyPlaying)
                            {
                                _mDescaledMouse = _mFrameServer.CoordinateSystem.Untransform(e.Location);

                                // Magnifier is not being moved or is invisible, try drawings through pointer tool.
                                // (including chronos, tracks and grids)
                                var bMovingObject = _mPointerTool.OnMouseMove(_mFrameServer.Metadata, _mDescaledMouse,
                                    _mFrameServer.CoordinateSystem.Location, ModifierKeys);

                                if (!bMovingObject)
                                {
                                    // User is not moving anything: move the whole image.
                                    // This may not have any effect if we try to move outside the original size and not in "free move" mode.

                                    // Get mouse deltas (descaled=in image coords).
                                    double fDeltaX = _mPointerTool.MouseDelta.X;
                                    double fDeltaY = _mPointerTool.MouseDelta.Y;

                                    if (_mFrameServer.Metadata.Mirrored)
                                    {
                                        fDeltaX = -fDeltaX;
                                    }

                                    _mFrameServer.CoordinateSystem.MoveZoomWindow(fDeltaX, fDeltaY);
                                }
                            }
                        }
                    }

                    if (!IsCurrentlyPlaying)
                    {
                        DoInvalidate();
                    }
                }
            }
        }

        private void SurfaceScreen_MouseUp(object sender, MouseEventArgs e)
        {
            // End of an action.
            // Depending on the active tool we have various things to do.

            if (_mFrameServer.VideoFile != null && _mFrameServer.VideoFile.Loaded && e.Button == MouseButtons.Left)
            {
                if (_mActiveTool == _mPointerTool)
                {
                    OnPoke();

                    // Update tracks with current image and pos.
                    _mFrameServer.Metadata.UpdateTrackPoint(_mFrameServer.VideoFile.CurrentImage);

                    // Report for synchro and merge to update image in the other screen.
                    ReportForSyncMerge();
                }

                _mFrameServer.Metadata.Magnifier.OnMouseUp(e);

                // Memorize the action we just finished to enable undo.
                if (_mActiveTool == ToolManager.Chrono)
                {
                    IUndoableCommand cac = new CommandAddChrono(DoInvalidate, DoDrawingUndrawn, _mFrameServer.Metadata);
                    var cm = CommandManager.Instance();
                    cm.LaunchUndoableCommand(cac);
                }
                else if (_mActiveTool != _mPointerTool && _mIActiveKeyFrameIndex >= 0)
                {
                    // Record the adding unless we are editing a text box.
                    if (!_mBTextEdit)
                    {
                        IUndoableCommand cad = new CommandAddDrawing(DoInvalidate, DoDrawingUndrawn,
                            _mFrameServer.Metadata, _mFrameServer.Metadata[_mIActiveKeyFrameIndex].Position);
                        var cm = CommandManager.Instance();
                        cm.LaunchUndoableCommand(cad);

                        // Deselect the drawing we just added.
                        _mFrameServer.Metadata.SelectedDrawingFrame = -1;
                        _mFrameServer.Metadata.SelectedDrawing = -1;
                    }
                    else
                    {
                        _mBTextEdit = false;
                    }
                }

                // The fact that we stay on this tool or fall back to pointer tool, depends on the tool.
                _mActiveTool = _mActiveTool.KeepTool ? _mActiveTool : _mPointerTool;

                if (_mActiveTool == _mPointerTool)
                {
                    SetCursor(_mPointerTool.GetCursor(0));
                    _mPointerTool.OnMouseUp();

                    // If we were resizing an SVG drawing, trigger a render.
                    // TODO: this is currently triggered on every mouse up, not only on resize !
                    var selectedFrame = _mFrameServer.Metadata.SelectedDrawingFrame;
                    var selectedDrawing = _mFrameServer.Metadata.SelectedDrawing;
                    if (selectedFrame != -1 && selectedDrawing != -1)
                    {
                        var d = _mFrameServer.Metadata.Keyframes[selectedFrame].Drawings[selectedDrawing] as DrawingSvg;
                        if (d != null)
                        {
                            d.ResizeFinished();
                        }
                    }
                }

                if (_mFrameServer.Metadata.SelectedDrawingFrame != -1 && _mFrameServer.Metadata.SelectedDrawing != -1)
                {
                    _mDeselectionTimer.Start();
                }

                DoInvalidate();
            }
        }

        private void SurfaceScreen_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_mFrameServer.VideoFile != null &&
                _mFrameServer.VideoFile.Loaded &&
                e.Button == MouseButtons.Left &&
                _mActiveTool == _mPointerTool)
            {
                OnPoke();

                _mDescaledMouse = _mFrameServer.CoordinateSystem.Untransform(e.Location);
                _mFrameServer.Metadata.AllDrawingTextToNormalMode();
                _mFrameServer.Metadata.UnselectAll();

                AbstractDrawing hitDrawing = null;

                //------------------------------------------------------------------------------------
                // - If on text, switch to edit mode.
                // - If on other drawing, launch the configuration dialog.
                // - Otherwise -> Maximize/Reduce image.
                //------------------------------------------------------------------------------------
                if (_mBDrawtimeFiltered)
                {
                    ToggleStretchMode();
                }
                else if (_mFrameServer.Metadata.IsOnDrawing(_mIActiveKeyFrameIndex, _mDescaledMouse, _mICurrentPosition))
                {
                    // Double click on a drawing:
                    // turn text tool into edit mode, launch config for others, SVG don't have a config.
                    AbstractDrawing ad =
                        _mFrameServer.Metadata.Keyframes[_mFrameServer.Metadata.SelectedDrawingFrame].Drawings[
                            _mFrameServer.Metadata.SelectedDrawing];
                    if (ad is DrawingText)
                    {
                        ((DrawingText) ad).SetEditMode(true, _mFrameServer.CoordinateSystem);
                        _mActiveTool = ToolManager.Label;
                        _mBTextEdit = true;
                    }
                    else if (ad is DrawingSvg || ad is DrawingBitmap)
                    {
                        mnuConfigureOpacity_Click(null, EventArgs.Empty);
                    }
                    else
                    {
                        mnuConfigureDrawing_Click(null, EventArgs.Empty);
                    }
                }
                else if ((hitDrawing = _mFrameServer.Metadata.IsOnExtraDrawing(_mDescaledMouse, _mICurrentPosition)) !=
                         null)
                {
                    if (hitDrawing is DrawingChrono)
                    {
                        mnuChronoConfigure_Click(null, EventArgs.Empty);
                    }
                    else if (hitDrawing is Track)
                    {
                        mnuConfigureTrajectory_Click(null, EventArgs.Empty);
                    }
                }
                else
                {
                    ToggleStretchMode();
                }
            }
        }

        private void SurfaceScreen_Paint(object sender, PaintEventArgs e)
        {
            //-------------------------------------------------------------------
            // We always draw at full SurfaceScreen size.
            // It is the SurfaceScreen itself that is resized if needed.
            //-------------------------------------------------------------------
            if (_mFrameServer.VideoFile != null && _mFrameServer.VideoFile.Loaded && !_mDualSaveInProgress)
            {
                if (_mBDrawtimeFiltered && _mDrawingFilterOutput.Draw != null)
                {
                    _mDrawingFilterOutput.Draw(e.Graphics, pbSurfaceScreen.Size, _mDrawingFilterOutput.PrivateData);
                }
                else if (_mFrameServer.VideoFile.CurrentImage != null)
                {
                    try
                    {
                        //m_Stopwatch.Reset();
                        //m_Stopwatch.Start();

                        // If we are on a keyframe, see if it has any drawing.
                        var iKeyFrameIndex = -1;
                        if (_mIActiveKeyFrameIndex >= 0)
                        {
                            if (_mFrameServer.Metadata[_mIActiveKeyFrameIndex].Drawings.Count > 0)
                            {
                                iKeyFrameIndex = _mIActiveKeyFrameIndex;
                            }
                        }

                        FlushOnGraphics(_mFrameServer.VideoFile.CurrentImage, e.Graphics, pbSurfaceScreen.Size,
                            iKeyFrameIndex, _mICurrentPosition);

                        if (_mMessageToaster.Enabled)
                        {
                            _mMessageToaster.Draw(e.Graphics);
                        }

                        //m_Stopwatch.Stop();
                        //log.Debug(String.Format("Paint: {0} ms.", m_Stopwatch.ElapsedMilliseconds));
                    }
                    catch (InvalidOperationException)
                    {
                        Log.Error("Error while painting image. Object is currently in use elsewhere... ATI Drivers ?");
                    }
                    catch (Exception exp)
                    {
                        Log.Error("Error while painting image.");
                        Log.Error(exp.Message);
                        Log.Error(exp.StackTrace);
                    }
                    finally
                    {
                        // Nothing more to do.
                    }
                }
                else
                {
                    Log.Debug("Painting screen - no image to display.");
                }

                // Draw Selection Border if needed.
                if (_mBShowImageBorder)
                {
                    DrawImageBorder(e.Graphics);
                }
            }
        }

        private void SurfaceScreen_MouseEnter(object sender, EventArgs e)
        {
            // Set focus to surfacescreen to enable mouse scroll

            // But only if there is no Text edition going on.
            var bEditing = false;
            if (_mFrameServer.Metadata.Count > _mIActiveKeyFrameIndex && _mIActiveKeyFrameIndex >= 0)
            {
                foreach (AbstractDrawing ad in _mFrameServer.Metadata[_mIActiveKeyFrameIndex].Drawings)
                {
                    var dt = ad as DrawingText;
                    if (dt != null)
                    {
                        if (dt.EditMode)
                        {
                            bEditing = true;
                            break;
                        }
                    }
                }
            }

            if (!bEditing)
            {
                pbSurfaceScreen.Focus();
            }
        }

        private void FlushOnGraphics(Bitmap sourceImage, Graphics g, Size iNewSize, int iKeyFrameIndex, long iPosition)
        {
            // This function is used both by the main rendering loop and by image export functions.

            // Notes on performances:
            // - The global performance depends on the size of the *source* image. Not destination.
            //   (rendering 1 pixel from an HD source will still be slow)
            // - Using a matrix transform instead of the buit in interpolation doesn't seem to do much.
            // - InterpolationMode has a sensible effect. but can look ugly for lowest values.
            // - Using unmanaged BitBlt or StretchBlt doesn't seem to do much... (!?)
            // - the scaling and interpolation better be done directly from ffmpeg. (cut on memory usage too)
            // - furthermore ffmpeg has a mode called 'FastBilinear' that seems more promising.

            // 1. Image
            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.InterpolationMode = InterpolationMode.Bilinear;
            g.SmoothingMode = SmoothingMode.None;

            // TODO - matrix transform.
            // - Rotate 90°/-90°
            // - Mirror

            Rectangle rDst;
            if (_mFrameServer.Metadata.Mirrored)
            {
                rDst = new Rectangle(iNewSize.Width, 0, -iNewSize.Width, iNewSize.Height);
            }
            else
            {
                rDst = new Rectangle(0, 0, iNewSize.Width, iNewSize.Height);
            }

            g.DrawImage(sourceImage, rDst, _mFrameServer.CoordinateSystem.ZoomWindow, GraphicsUnit.Pixel);

            // Testing Key images overlay.
            // Creates a ghost image of the last keyframe superposed with the current image.
            // We can only do it in analysis mode to get the key image bitmap.
            /*if(m_FrameServer.VideoFile.Selection.iAnalysisMode == 1 && m_FrameServer.Metadata.Keyframes.Count > 0)
            {
                // Look for the closest key image before.
                int iImageMerge = -1 ;
                long iBestDistance = long.MaxValue;
                for(int i=0; i<m_FrameServer.Metadata.Keyframes.Count;i++)
                {
                    long iDistance = _iPosition - m_FrameServer.Metadata.Keyframes[i].Position;
                    if(iDistance >=0 && iDistance < iBestDistance)
                    {
                        iBestDistance = iDistance;
                        iImageMerge = i;
                    }
                }

                // Merge images.
                int iFrameIndex = (int)m_FrameServer.VideoFile.GetFrameNumber(m_FrameServer.Metadata.Keyframes[iImageMerge].Position);
                Bitmap mergeImage = m_FrameServer.VideoFile.FrameList[iFrameIndex].BmpImage;
                g.DrawImage(mergeImage, rDst, 0, 0, _sourceImage.Width, _sourceImage.Height, GraphicsUnit.Pixel, m_SyncMergeImgAttr);
            }*/

            // .Sync superposition.
            if (_mBSynched && _mBSyncMerge && _mSyncMergeImage != null)
            {
                // The mirroring, if any, will have been done already and applied to the sync image.
                // (because to draw the other image, we take account its own mirroring option,
                // not the option of the original video in this screen.)
                var rSyncDst = new Rectangle(0, 0, iNewSize.Width, iNewSize.Height);
                g.DrawImage(_mSyncMergeImage, rSyncDst, 0, 0, _mSyncMergeImage.Width, _mSyncMergeImage.Height,
                    GraphicsUnit.Pixel, _mSyncMergeImgAttr);
            }

            if ((IsCurrentlyPlaying && _mPrefManager.DrawOnPlay) || !IsCurrentlyPlaying)
            {
                FlushDrawingsOnGraphics(g, _mFrameServer.CoordinateSystem, iKeyFrameIndex, iPosition,
                    _mFrameServer.CoordinateSystem.Stretch, _mFrameServer.CoordinateSystem.Zoom,
                    _mFrameServer.CoordinateSystem.Location);
                FlushMagnifierOnGraphics(sourceImage, g);
            }
        }

        private void FlushDrawingsOnGraphics(Graphics canvas, CoordinateSystem transformer, int iKeyFrameIndex,
            long iPosition, double fStretchFactor, double fDirectZoomFactor, Point directZoomTopLeft)
        {
            // Prepare for drawings
            canvas.SmoothingMode = SmoothingMode.AntiAlias;
            canvas.TextRenderingHint = TextRenderingHint.AntiAlias;

            // 1. Extra (non attached to any key image).
            for (var i = 0; i < _mFrameServer.Metadata.ExtraDrawings.Count; i++)
            {
                var selected = (i == _mFrameServer.Metadata.SelectedExtraDrawing);
                _mFrameServer.Metadata.ExtraDrawings[i].Draw(canvas, transformer, selected, iPosition);
            }

            // 2. Drawings attached to key images.
            if (_mPrefManager.DefaultFading.Enabled)
            {
                // If fading is on, we ask all drawings to draw themselves with their respective
                // fading factor for this position.

                var zOrder = _mFrameServer.Metadata.GetKeyframesZOrder(iPosition);

                // Draw in reverse keyframes z order so the closest next keyframe gets drawn on top (last).
                for (var ikf = zOrder.Length - 1; ikf >= 0; ikf--)
                {
                    var kf = _mFrameServer.Metadata.Keyframes[zOrder[ikf]];
                    for (var idr = kf.Drawings.Count - 1; idr >= 0; idr--)
                    {
                        var bSelected = (zOrder[ikf] == _mFrameServer.Metadata.SelectedDrawingFrame &&
                                         idr == _mFrameServer.Metadata.SelectedDrawing);
                        kf.Drawings[idr].Draw(canvas, transformer, bSelected, iPosition);
                    }
                }
            }
            else if (iKeyFrameIndex >= 0)
            {
                // if fading is off, only draw the current keyframe.
                // Draw all drawings in reverse order to get first object on the top of Z-order.
                for (var i = _mFrameServer.Metadata[iKeyFrameIndex].Drawings.Count - 1; i >= 0; i--)
                {
                    var bSelected = (iKeyFrameIndex == _mFrameServer.Metadata.SelectedDrawingFrame &&
                                     i == _mFrameServer.Metadata.SelectedDrawing);
                    _mFrameServer.Metadata[iKeyFrameIndex].Drawings[i].Draw(canvas, transformer, bSelected, iPosition);
                }
            }
        }

        private void FlushMagnifierOnGraphics(Bitmap sourceImage, Graphics g)
        {
            // Note: the Graphics object must not be the one extracted from the image itself.
            // If needed, clone the image.
            if (sourceImage != null && _mFrameServer.Metadata.Magnifier.Mode != MagnifierMode.NotVisible)
            {
                _mFrameServer.Metadata.Magnifier.Draw(sourceImage, g, _mFrameServer.CoordinateSystem.Stretch,
                    _mFrameServer.Metadata.Mirrored);
            }
        }

        private void DoInvalidate()
        {
            // This function should be the single point where we call for rendering.
            // Here we can decide to render directly on the surface or go through the Windows message pump.
            pbSurfaceScreen.Invalidate();
        }

        #endregion SurfaceScreen Events

        #region PanelCenter Events

        private void PanelCenter_MouseEnter(object sender, EventArgs e)
        {
            // Give focus to enable mouse scroll.
            panelCenter.Focus();
        }

        private void PanelCenter_MouseClick(object sender, MouseEventArgs e)
        {
            OnPoke();
        }

        private void PanelCenter_Resize(object sender, EventArgs e)
        {
            StretchSqueezeSurface();
            DoInvalidate();
        }

        private void PanelCenter_MouseDown(object sender, MouseEventArgs e)
        {
            _mnuDirectTrack.Visible = false;
            _mnuSendPic.Visible = _mBSynched;
            panelCenter.ContextMenuStrip = _popMenu;
        }

        #endregion PanelCenter Events

        #region Keyframes Panel

        private void pnlThumbnails_MouseEnter(object sender, EventArgs e)
        {
            // Give focus to disable keyframe box editing.
            pnlThumbnails.Focus();
        }

        private void splitKeyframes_Resize(object sender, EventArgs e)
        {
            // Redo the dock/undock if needed to be at the right place.
            // (Could be handled by layout ?)
            DockKeyframePanel(_mBDocked);
        }

        private void btnAddKeyframe_Click(object sender, EventArgs e)
        {
            if (_mFrameServer.VideoFile.Loaded)
            {
                AddKeyframe();

                // Set as active screen is done afterwards, so the export as pdf menu is activated
                // even if we had no keyframes yet.
                OnPoke();
            }
        }

        private void OrganizeKeyframes()
        {
            // Should only be called when adding/removing a Thumbnail

            pnlThumbnails.Controls.Clear();

            if (_mFrameServer.Metadata.Count > 0)
            {
                var iKeyframeIndex = 0;
                var iPixelsOffset = 0;
                var iPixelsSpacing = 20;

                foreach (var kf in _mFrameServer.Metadata.Keyframes)
                {
                    var box = new KeyframeBox(kf);
                    SetupDefaultThumbBox(box);

                    // Finish the setup
                    box.Left = iPixelsOffset + iPixelsSpacing;

                    box.UpdateTitle(kf.Title);
                    box.Tag = iKeyframeIndex;
                    box.pbThumbnail.SizeMode = PictureBoxSizeMode.StretchImage;

                    box.CloseThumb += ThumbBoxClose;
                    box.ClickThumb += ThumbBoxClick;
                    box.ClickInfos += ThumbBoxInfosClick;

                    // TODO - Titre de la Keyframe en ToolTip.
                    iPixelsOffset += (iPixelsSpacing + box.Width);

                    pnlThumbnails.Controls.Add(box);

                    iKeyframeIndex++;
                }

                EnableDisableKeyframes();
                pnlThumbnails.Refresh();
            }
            else
            {
                DockKeyframePanel(true);
                _mIActiveKeyFrameIndex = -1;
            }

            UpdateFramesMarkers();
            DoInvalidate(); // Because of trajectories with keyframes labels.
        }

        private void SetupDefaultThumbBox(UserControl box)
        {
            box.Top = 10;
            box.Cursor = Cursors.Hand;
        }

        private void ActivateKeyframe(long iPosition)
        {
            ActivateKeyframe(iPosition, true);
        }

        private void ActivateKeyframe(long iPosition, bool bAllowUiUpdate)
        {
            //--------------------------------------------------------------
            // Black border every keyframe, unless it is at the given position.
            // This method might be called with -1 to force complete blackout.
            //--------------------------------------------------------------

            // This method is called on each frame during frametracker browsing
            // keep it fast or fix the strategy.

            _mIActiveKeyFrameIndex = -1;

            // We leverage the fact that pnlThumbnail is exclusively populated with thumboxes.
            for (var i = 0; i < pnlThumbnails.Controls.Count; i++)
            {
                if (_mFrameServer.Metadata[i].Position == iPosition)
                {
                    _mIActiveKeyFrameIndex = i;
                    if (bAllowUiUpdate)
                        ((KeyframeBox) pnlThumbnails.Controls[i]).DisplayAsSelected(true);

                    // Make sure the thumbnail is always in the visible area by auto scrolling.
                    if (bAllowUiUpdate) pnlThumbnails.ScrollControlIntoView(pnlThumbnails.Controls[i]);
                }
                else
                {
                    if (bAllowUiUpdate)
                        ((KeyframeBox) pnlThumbnails.Controls[i]).DisplayAsSelected(false);
                }
            }

            if (bAllowUiUpdate && _mKeyframeCommentsHub.UserActivated && _mIActiveKeyFrameIndex >= 0)
            {
                _mKeyframeCommentsHub.UpdateContent(_mFrameServer.Metadata[_mIActiveKeyFrameIndex]);
                _mKeyframeCommentsHub.Visible = true;
            }
            else
            {
                if (_mKeyframeCommentsHub.Visible)
                    _mKeyframeCommentsHub.CommitChanges();

                _mKeyframeCommentsHub.Visible = false;
            }
        }

        private void EnableDisableKeyframes()
        {
            // public : called from formKeyFrameComments. (fixme ?)

            // Enable Keyframes that are within Working Zone, Disable others.

            // We leverage the fact that pnlThumbnail is exclusively populated with thumboxes.
            for (var i = 0; i < pnlThumbnails.Controls.Count; i++)
            {
                var tb = pnlThumbnails.Controls[i] as KeyframeBox;
                if (tb != null)
                {
                    _mFrameServer.Metadata[i].TimeCode =
                        TimeStampsToTimecode(_mFrameServer.Metadata[i].Position - _mISelStart,
                            _mPrefManager.TimeCodeFormat, false);

                    // Enable thumbs that are within Working Zone, grey out others.
                    if (_mFrameServer.Metadata[i].Position >= _mISelStart &&
                        _mFrameServer.Metadata[i].Position <= _mISelEnd)
                    {
                        _mFrameServer.Metadata[i].Disabled = false;

                        tb.Enabled = true;
                        tb.pbThumbnail.Image = _mFrameServer.Metadata[i].Thumbnail;
                    }
                    else
                    {
                        _mFrameServer.Metadata[i].Disabled = true;

                        tb.Enabled = false;
                        tb.pbThumbnail.Image = _mFrameServer.Metadata[i].DisabledThumbnail;
                    }

                    tb.UpdateTitle(_mFrameServer.Metadata[i].Title);
                }
            }
        }

        public void OnKeyframesTitleChanged()
        {
            // Called when title changed.

            // Update trajectories.
            _mFrameServer.Metadata.UpdateTrajectoriesForKeyframes();

            // Update thumb boxes.
            EnableDisableKeyframes();

            DoInvalidate();
        }

        private void GotoNextKeyframe()
        {
            if (_mFrameServer.Metadata.Count > 1)
            {
                var iNextKeyframe = -1;
                for (var i = 0; i < _mFrameServer.Metadata.Count; i++)
                {
                    if (_mICurrentPosition < _mFrameServer.Metadata[i].Position)
                    {
                        iNextKeyframe = i;
                        break;
                    }
                }

                if (iNextKeyframe >= 0 && _mFrameServer.Metadata[iNextKeyframe].Position <= _mISelEnd)
                {
                    ThumbBoxClick(pnlThumbnails.Controls[iNextKeyframe], EventArgs.Empty);
                }
            }
        }

        private void GotoPreviousKeyframe()
        {
            if (_mFrameServer.Metadata.Count > 0)
            {
                var iPrevKeyframe = -1;
                for (var i = _mFrameServer.Metadata.Count - 1; i >= 0; i--)
                {
                    if (_mICurrentPosition > _mFrameServer.Metadata[i].Position)
                    {
                        iPrevKeyframe = i;
                        break;
                    }
                }

                if (iPrevKeyframe >= 0 && _mFrameServer.Metadata[iPrevKeyframe].Position >= _mISelStart)
                {
                    ThumbBoxClick(pnlThumbnails.Controls[iPrevKeyframe], EventArgs.Empty);
                }
            }
        }

        private void AddKeyframe()
        {
            var i = 0;
            // Check if it's not already registered.
            var bAlreadyKeyFrame = false;
            for (i = 0; i < _mFrameServer.Metadata.Count; i++)
            {
                if (_mFrameServer.Metadata[i].Position == _mICurrentPosition)
                {
                    bAlreadyKeyFrame = true;
                    _mIActiveKeyFrameIndex = i;
                }
            }

            // Add it to the list.
            if (!bAlreadyKeyFrame)
            {
                IUndoableCommand cak = new CommandAddKeyframe(this, _mFrameServer.Metadata, _mICurrentPosition);
                var cm = CommandManager.Instance();
                cm.LaunchUndoableCommand(cak);

                // If it is the very first key frame, we raise the KF panel.
                // Otherwise we keep whatever choice the user made.
                if (_mFrameServer.Metadata.Count == 1)
                {
                    DockKeyframePanel(false);
                }
            }
        }

        public void OnAddKeyframe(long iPosition)
        {
            // Public because called from CommandAddKeyframe.Execute()
            // Title becomes the current timecode. (relative to sel start or sel minimum ?)

            var kf = new Keyframe(iPosition,
                TimeStampsToTimecode(iPosition - _mISelStart, _mPrefManager.TimeCodeFormat, _mBSynched),
                _mFrameServer.VideoFile.CurrentImage, _mFrameServer.Metadata);

            if (iPosition != _mICurrentPosition)
            {
                // Move to the required Keyframe.
                // Should only happen when Undoing a DeleteKeyframe.
                _mIFramesToDecode = 1;
                ShowNextFrame(iPosition, true);
                UpdateNavigationCursor();
                trkSelection.SelPos = trkFrame.Position;

                // Readjust and complete the Keyframe
                kf.ImportImage(_mFrameServer.VideoFile.CurrentImage);
            }

            _mFrameServer.Metadata.Add(kf);

            // Keep the list sorted
            _mFrameServer.Metadata.Sort();
            _mFrameServer.Metadata.UpdateTrajectoriesForKeyframes();

            // Refresh Keyframes preview.
            OrganizeKeyframes();

            // B&W conversion can be lengthly. We do it after showing the result.
            kf.GenerateDisabledThumbnail();

            if (!IsCurrentlyPlaying)
            {
                ActivateKeyframe(_mICurrentPosition);
            }
        }

        private void RemoveKeyframe(int iKeyframeIndex)
        {
            IUndoableCommand cdk = new CommandDeleteKeyframe(this, _mFrameServer.Metadata,
                _mFrameServer.Metadata[iKeyframeIndex].Position);
            var cm = CommandManager.Instance();
            cm.LaunchUndoableCommand(cdk);

            //OnRemoveKeyframe(_iKeyframeIndex);
        }

        public void OnRemoveKeyframe(int iKeyframeIndex)
        {
            if (iKeyframeIndex == _mIActiveKeyFrameIndex)
            {
                // Removing active frame
                _mIActiveKeyFrameIndex = -1;
            }
            else if (iKeyframeIndex < _mIActiveKeyFrameIndex)
            {
                if (_mIActiveKeyFrameIndex > 0)
                {
                    // Active keyframe index shift
                    _mIActiveKeyFrameIndex--;
                }
            }

            _mFrameServer.Metadata.RemoveAt(iKeyframeIndex);
            _mFrameServer.Metadata.UpdateTrajectoriesForKeyframes();
            OrganizeKeyframes();
            DoInvalidate();
        }

        public void UpdateKeyframes()
        {
            // Primary selection has been image-adjusted,
            // some keyframes may have been impacted.

            var bAtLeastOne = false;

            foreach (var kf in _mFrameServer.Metadata.Keyframes)
            {
                if (kf.Position >= _mISelStart && kf.Position <= _mISelEnd)
                {
                    kf.ImportImage(
                        _mFrameServer.VideoFile.FrameList[(int) _mFrameServer.VideoFile.GetFrameNumber(kf.Position)]
                            .BmpImage);
                    kf.GenerateDisabledThumbnail();
                    bAtLeastOne = true;
                }
            }

            if (bAtLeastOne)
                OrganizeKeyframes();
        }

        private void pnlThumbnails_DoubleClick(object sender, EventArgs e)
        {
            if (_mFrameServer.VideoFile.Loaded)
            {
                // On double click in the thumbs panel : Add a keyframe at current pos.
                AddKeyframe();
                OnPoke();
            }
        }

        #region ThumbBox event Handlers

        private void ThumbBoxClose(object sender, EventArgs e)
        {
            RemoveKeyframe((int) ((KeyframeBox) sender).Tag);

            // Set as active screen is done after in case we don't have any keyframes left.
            OnPoke();
        }

        private void ThumbBoxClick(object sender, EventArgs e)
        {
            // Move to the right spot.
            OnPoke();
            StopPlaying();
            _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();

            var iTargetPosition = _mFrameServer.Metadata[(int) ((KeyframeBox) sender).Tag].Position;

            trkSelection.SelPos = iTargetPosition;
            _mIFramesToDecode = 1;

            ShowNextFrame(iTargetPosition, true);
            _mICurrentPosition = iTargetPosition;

            UpdateNavigationCursor();
            if (_mBShowInfos)
            {
                UpdateDebugInfos();
            }

            // On active sur la position réelle, au cas où on ne soit pas sur la frame demandée.
            // par ex, si la kf cliquée est hors zone
            ActivateKeyframe(_mICurrentPosition);
        }

        private void ThumbBoxInfosClick(object sender, EventArgs e)
        {
            ThumbBoxClick(sender, e);
            _mKeyframeCommentsHub.UserActivated = true;
            ActivateKeyframe(_mICurrentPosition);
        }

        #endregion ThumbBox event Handlers

        #region Docking Undocking

        private void btnDockBottom_Click(object sender, EventArgs e)
        {
            DockKeyframePanel(!_mBDocked);
        }

        private void splitKeyframes_Panel2_DoubleClick(object sender, EventArgs e)
        {
            DockKeyframePanel(!_mBDocked);
        }

        private void DockKeyframePanel(bool bDock)
        {
            if (bDock)
            {
                // hide the keyframes, change image.
                splitKeyframes.SplitterDistance = splitKeyframes.Height - 25;
                btnDockBottom.BackgroundImage = Resources.undock16x16;
                btnDockBottom.Visible = _mFrameServer.Metadata.Count > 0;
            }
            else
            {
                // show the keyframes, change image.
                splitKeyframes.SplitterDistance = splitKeyframes.Height - 140;
                btnDockBottom.BackgroundImage = Resources.dock16x16;
                btnDockBottom.Visible = true;
            }

            _mBDocked = bDock;
        }

        private void PrepareKeyframesDock()
        {
            // If there's no keyframe, and we will be using a tool,
            // the keyframes dock should be raised.
            // This way we don't surprise the user when he click the screen and the image moves around.
            // (especially problematic when using the Pencil.

            // this is only done for the very first keyframe.
            if (_mFrameServer.Metadata.Count < 1)
            {
                DockKeyframePanel(false);
            }
        }

        #endregion Docking Undocking

        #endregion Keyframes Panel

        #region Drawings Toolbar Events

        private void drawingTool_Click(object sender, EventArgs e)
        {
            // User clicked on a drawing tool button. A reference to the tool is stored in .Tag
            // Set this tool as the active tool (waiting for the actual use) and set the cursor accordingly.

            // Deactivate magnifier if not commited.
            if (_mFrameServer.Metadata.Magnifier.Mode == MagnifierMode.Direct)
            {
                DisableMagnifier();
            }

            OnPoke();

            AbstractDrawingTool tool = ((ToolStripItem) sender).Tag as AbstractDrawingTool;
            if (tool != null)
            {
                _mActiveTool = tool;
            }
            else
            {
                _mActiveTool = _mPointerTool;
            }

            UpdateCursor();

            // Ensure there's a key image at this position, unless the tool creates unattached drawings.
            if (_mActiveTool == _mPointerTool && _mFrameServer.Metadata.Count < 1)
            {
                DockKeyframePanel(true);
            }
            else if (_mActiveTool.Attached)
            {
                PrepareKeyframesDock();
            }

            pbSurfaceScreen.Invalidate();
        }

        private void btnMagnifier_Click(object sender, EventArgs e)
        {
            if (_mFrameServer.VideoFile.Loaded)
            {
                _mActiveTool = _mPointerTool;

                if (_mFrameServer.Metadata.Magnifier.Mode == MagnifierMode.NotVisible)
                {
                    UnzoomDirectZoom();
                    _mFrameServer.Metadata.Magnifier.Mode = MagnifierMode.Direct;
                    //btnMagnifier.Image = Drawings.magnifieractive;
                    SetCursor(Cursors.Cross);
                }
                else if (_mFrameServer.Metadata.Magnifier.Mode == MagnifierMode.Direct)
                {
                    // Revert to no magnification.
                    UnzoomDirectZoom();
                    _mFrameServer.Metadata.Magnifier.Mode = MagnifierMode.NotVisible;
                    //btnMagnifier.Image = Drawings.magnifier;
                    SetCursor(_mPointerTool.GetCursor(0));
                    DoInvalidate();
                }
                else
                {
                    DisableMagnifier();
                    DoInvalidate();
                }
            }
        }

        private void btnShowComments_Click(object sender, EventArgs e)
        {
            OnPoke();

            if (_mFrameServer.VideoFile.Loaded)
            {
                // If the video is currently playing, the comments are not visible.
                // We stop the video and show them.
                var bWasPlaying = IsCurrentlyPlaying;
                if (IsCurrentlyPlaying)
                {
                    StopPlaying();
                    _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();
                    ActivateKeyframe(_mICurrentPosition);
                }

                if (_mIActiveKeyFrameIndex < 0 || !_mKeyframeCommentsHub.UserActivated || bWasPlaying)
                {
                    // As of now, Keyframes infobox should display when we are on a keyframe
                    _mKeyframeCommentsHub.UserActivated = true;

                    if (_mIActiveKeyFrameIndex < 0)
                    {
                        // We are not on a keyframe but user asked to show the infos...
                        // did he want to create a keyframe here and put some infos,
                        // or did he only want to activate the infobox for next keyframes ?
                        //
                        // Since he clicked on the DrawingTools bar, we will act as if it was a Drawing,
                        // and add a keyframe here in case there isn't already one.
                        AddKeyframe();
                    }

                    _mKeyframeCommentsHub.UpdateContent(_mFrameServer.Metadata[_mIActiveKeyFrameIndex]);
                    _mKeyframeCommentsHub.Visible = true;
                }
                else
                {
                    _mKeyframeCommentsHub.UserActivated = false;
                    _mKeyframeCommentsHub.CommitChanges();
                    _mKeyframeCommentsHub.Visible = false;
                }
            }
        }

        private void btnColorProfile_Click(object sender, EventArgs e)
        {
            OnPoke();

            // Load, save or modify current profile.
            var ftp = new FormToolPresets();
            ScreenManagerKernel.LocateForm(ftp);
            ftp.ShowDialog();
            ftp.Dispose();

            UpdateCursor();
            DoInvalidate();
        }

        private void UpdateCursor()
        {
            if (_mActiveTool == _mPointerTool)
            {
                SetCursor(_mPointerTool.GetCursor(0));
            }
            else
            {
                SetCursor(_mActiveTool.GetCursor(_mFrameServer.CoordinateSystem.Stretch));
            }
        }

        private void SetCursor(Cursor cur)
        {
            pbSurfaceScreen.Cursor = cur;
        }

        #endregion Drawings Toolbar Events

        #region Context Menus Events

        #region Main

        private void mnuDirectTrack_Click(object sender, EventArgs e)
        {
            // Track the point. No Cross2D was selected.
            // m_DescaledMouse would have been set during the MouseDown event.
            var trk = new Track(_mDescaledMouse, _mICurrentPosition, _mFrameServer.VideoFile.CurrentImage,
                _mFrameServer.VideoFile.CurrentImage.Size);
            _mFrameServer.Metadata.AddTrack(trk, OnShowClosestFrame, Color.CornflowerBlue);
                // todo: get from track tool.

            // Return to the pointer tool.
            _mActiveTool = _mPointerTool;
            SetCursor(_mPointerTool.GetCursor(0));

            DoInvalidate();
        }

        private void mnuSendPic_Click(object sender, EventArgs e)
        {
            // Send the current image to the other screen for conversion into an observational reference.
            if (_mBSynched && _mFrameServer.VideoFile.CurrentImage != null)
            {
                var img = CloneTransformedImage();
                _mPlayerScreenUiHandler.PlayerScreenUI_SendImage(img);
            }
        }

        #endregion Main

        #region Drawings Menus

        private void mnuConfigureDrawing_Click(object sender, EventArgs e)
        {
            // Generic menu for all drawings with the Color or ColorSize capability.
            if (_mFrameServer.Metadata.SelectedDrawingFrame >= 0 &&
                _mFrameServer.Metadata.SelectedDrawing >= 0 &&
                _mFrameServer.Metadata.Count > _mFrameServer.Metadata.SelectedDrawingFrame)
            {
                var kf = _mFrameServer.Metadata[_mFrameServer.Metadata.SelectedDrawingFrame];
                if (kf.Drawings.Count > _mFrameServer.Metadata.SelectedDrawing)
                {
                    var decorableDrawing = kf.Drawings[_mFrameServer.Metadata.SelectedDrawing] as IDecorable;
                    if (decorableDrawing != null && decorableDrawing.DrawingStyle != null &&
                        decorableDrawing.DrawingStyle.Elements.Count > 0)
                    {
                        var fcd = new FormConfigureDrawing2(decorableDrawing.DrawingStyle, DoInvalidate);
                        ScreenManagerKernel.LocateForm(fcd);
                        fcd.ShowDialog();
                        fcd.Dispose();
                        DoInvalidate();
                    }
                }
            }
        }

        private void mnuConfigureFading_Click(object sender, EventArgs e)
        {
            // Generic menu for all drawings with the Fading capability.
            if (_mFrameServer.Metadata.SelectedDrawingFrame >= 0 && _mFrameServer.Metadata.SelectedDrawing >= 0)
            {
                var fcf =
                    new FormConfigureFading(
                        _mFrameServer.Metadata[_mFrameServer.Metadata.SelectedDrawingFrame].Drawings[
                            _mFrameServer.Metadata.SelectedDrawing], pbSurfaceScreen);
                ScreenManagerKernel.LocateForm(fcf);
                fcf.ShowDialog();
                fcf.Dispose();
                DoInvalidate();
            }
        }

        private void mnuConfigureOpacity_Click(object sender, EventArgs e)
        {
            // Generic menu for all drawings with the Opacity capability.
            if (_mFrameServer.Metadata.SelectedDrawingFrame >= 0 && _mFrameServer.Metadata.SelectedDrawing >= 0)
            {
                var fco =
                    new FormConfigureOpacity(
                        _mFrameServer.Metadata[_mFrameServer.Metadata.SelectedDrawingFrame].Drawings[
                            _mFrameServer.Metadata.SelectedDrawing], pbSurfaceScreen);
                ScreenManagerKernel.LocateForm(fco);
                fco.ShowDialog();
                fco.Dispose();
                DoInvalidate();
            }
        }

        private void mnuGotoKeyframe_Click(object sender, EventArgs e)
        {
            // Generic menu for all drawings when we are not on their attachement key frame.
            if (_mFrameServer.Metadata.SelectedDrawingFrame >= 0 && _mFrameServer.Metadata.SelectedDrawing >= 0)
            {
                long iPosition =
                    _mFrameServer.Metadata[_mFrameServer.Metadata.SelectedDrawingFrame].Drawings[
                        _mFrameServer.Metadata.SelectedDrawing].InfosFading.ReferenceTimestamp;

                _mIFramesToDecode = 1;
                ShowNextFrame(iPosition, true);
                UpdateNavigationCursor();
                trkSelection.SelPos = trkFrame.Position;
                ActivateKeyframe(_mICurrentPosition);
            }
        }

        private void mnuDeleteDrawing_Click(object sender, EventArgs e)
        {
            // Generic menu for all attached drawings.
            DeleteSelectedDrawing();
        }

        private void DeleteSelectedDrawing()
        {
            if (_mFrameServer.Metadata.SelectedDrawingFrame >= 0 && _mFrameServer.Metadata.SelectedDrawing >= 0)
            {
                IUndoableCommand cdd = new CommandDeleteDrawing(DoInvalidate, _mFrameServer.Metadata,
                    _mFrameServer.Metadata[_mFrameServer.Metadata.SelectedDrawingFrame].Position,
                    _mFrameServer.Metadata.SelectedDrawing);
                var cm = CommandManager.Instance();
                cm.LaunchUndoableCommand(cdd);
                DoInvalidate();
            }
        }

        private void mnuTrackTrajectory_Click(object sender, EventArgs e)
        {
            //---------------------------------------
            // Turn a Cross2D into a Track.
            // Cross2D was selected upon Right Click.
            //---------------------------------------

            // We force the user to be on the right frame.
            if (_mIActiveKeyFrameIndex >= 0 && _mIActiveKeyFrameIndex == _mFrameServer.Metadata.SelectedDrawingFrame)
            {
                var iSelectedDrawing = _mFrameServer.Metadata.SelectedDrawing;

                if (iSelectedDrawing >= 0)
                {
                    // TODO - link to CommandAddTrajectory.
                    // Add track on this point.
                    var dc = _mFrameServer.Metadata[_mIActiveKeyFrameIndex].Drawings[iSelectedDrawing] as DrawingCross2D;
                    if (dc != null)
                    {
                        var trk = new Track(dc.Center, _mICurrentPosition, _mFrameServer.VideoFile.CurrentImage,
                            _mFrameServer.VideoFile.CurrentImage.Size);
                        _mFrameServer.Metadata.AddTrack(trk, OnShowClosestFrame, dc.PenColor);

                        // Suppress the point as a Drawing (?)
                        _mFrameServer.Metadata[_mIActiveKeyFrameIndex].Drawings.RemoveAt(iSelectedDrawing);
                        _mFrameServer.Metadata.SelectedDrawingFrame = -1;
                        _mFrameServer.Metadata.SelectedDrawing = -1;

                        // Return to the pointer tool.
                        _mActiveTool = _mPointerTool;
                        SetCursor(_mPointerTool.GetCursor(0));
                    }
                }
            }
            DoInvalidate();
        }

        #endregion Drawings Menus

        #region Tracking Menus

        private void mnuStopTracking_Click(object sender, EventArgs e)
        {
            var trk = _mFrameServer.Metadata.ExtraDrawings[_mFrameServer.Metadata.SelectedExtraDrawing] as Track;
            if (trk != null)
            {
                trk.StopTracking();
            }
            DoInvalidate();
        }

        private void mnuDeleteEndOfTrajectory_Click(object sender, EventArgs e)
        {
            IUndoableCommand cdeot = new CommandDeleteEndOfTrack(this, _mFrameServer.Metadata, _mICurrentPosition);
            var cm = CommandManager.Instance();
            cm.LaunchUndoableCommand(cdeot);

            DoInvalidate();
            UpdateFramesMarkers();
        }

        private void mnuRestartTracking_Click(object sender, EventArgs e)
        {
            var trk = _mFrameServer.Metadata.ExtraDrawings[_mFrameServer.Metadata.SelectedExtraDrawing] as Track;
            if (trk != null)
            {
                trk.RestartTracking();
            }
            DoInvalidate();
        }

        private void mnuDeleteTrajectory_Click(object sender, EventArgs e)
        {
            IUndoableCommand cdc = new CommandDeleteTrack(this, _mFrameServer.Metadata);
            var cm = CommandManager.Instance();
            cm.LaunchUndoableCommand(cdc);

            UpdateFramesMarkers();

            // Trigger a refresh of the export to spreadsheet menu,
            // in case we don't have any more trajectory left to export.
            OnPoke();
        }

        private void mnuConfigureTrajectory_Click(object sender, EventArgs e)
        {
            var trk = _mFrameServer.Metadata.ExtraDrawings[_mFrameServer.Metadata.SelectedExtraDrawing] as Track;
            if (trk != null)
            {
                // Change this trajectory display.
                var dp = DelegatesPool.Instance();
                if (dp.DeactivateKeyboardHandler != null)
                {
                    dp.DeactivateKeyboardHandler();
                }

                var fctd = new FormConfigureTrajectoryDisplay(trk, DoInvalidate);
                fctd.StartPosition = FormStartPosition.CenterScreen;
                fctd.ShowDialog();
                fctd.Dispose();

                if (dp.ActivateKeyboardHandler != null)
                {
                    dp.ActivateKeyboardHandler();
                }
            }
        }

        private void OnShowClosestFrame(Point mouse, long iBeginTimestamp, List<AbstractTrackPoint> positions,
            int iPixelTotalDistance, bool b2DOnly)
        {
            //--------------------------------------------------------------------------
            // This is where the interactivity of the trajectory is done.
            // The user has draged or clicked the trajectory, we find the closest point
            // and we update to the corresponding frame.
            //--------------------------------------------------------------------------

            // Compute the 3D distance (x,y,t) of each point in the path.
            // unscaled coordinates.

            var minDistance = double.MaxValue;
            var iClosestPoint = 0;

            if (b2DOnly)
            {
                // Check the closest location on screen.
                for (var i = 0; i < positions.Count; i++)
                {
                    var dist = Math.Sqrt(((mouse.X - positions[i].X)*(mouse.X - positions[i].X))
                                         + ((mouse.Y - positions[i].Y)*(mouse.Y - positions[i].Y)));

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        iClosestPoint = i;
                    }
                }
            }
            else
            {
                // Check closest location on screen, but giving priority to the one also close in time.
                // = distance in 3D.
                // Distance on t is not in the same unit as distance on x and y.
                // So first step is to normalize t.

                // _iPixelTotalDistance should be the flat distance (distance from topleft to bottomright)
                // not the added distances of each segments, otherwise it will be biased towards time.

                var timeTotalDistance = positions[positions.Count - 1].T - positions[0].T;
                var scaleFactor = timeTotalDistance/(double) iPixelTotalDistance;

                for (var i = 0; i < positions.Count; i++)
                {
                    double fTimeDistance = _mICurrentPosition - iBeginTimestamp - positions[i].T;

                    var dist = Math.Sqrt(((mouse.X - positions[i].X)*(mouse.X - positions[i].X))
                                         + ((mouse.Y - positions[i].Y)*(mouse.Y - positions[i].Y))
                                         + ((long) (fTimeDistance/scaleFactor)*(long) (fTimeDistance/scaleFactor)));

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        iClosestPoint = i;
                    }
                }
            }

            // move to corresponding timestamp.
            _mIFramesToDecode = 1;
            ShowNextFrame(positions[iClosestPoint].T + iBeginTimestamp, true);
            UpdateNavigationCursor();
            trkSelection.SelPos = trkFrame.Position;
        }

        #endregion Tracking Menus

        #region Chronometers Menus

        private void mnuChronoStart_Click(object sender, EventArgs e)
        {
            IUndoableCommand cmc = new CommandModifyChrono(this, _mFrameServer.Metadata,
                ChronoModificationType.TimeStart, _mICurrentPosition);
            var cm = CommandManager.Instance();
            cm.LaunchUndoableCommand(cmc);
        }

        private void mnuChronoStop_Click(object sender, EventArgs e)
        {
            IUndoableCommand cmc = new CommandModifyChrono(this, _mFrameServer.Metadata, ChronoModificationType.TimeStop,
                _mICurrentPosition);
            var cm = CommandManager.Instance();
            cm.LaunchUndoableCommand(cmc);
            UpdateFramesMarkers();
        }

        private void mnuChronoHide_Click(object sender, EventArgs e)
        {
            IUndoableCommand cmc = new CommandModifyChrono(this, _mFrameServer.Metadata, ChronoModificationType.TimeHide,
                _mICurrentPosition);
            var cm = CommandManager.Instance();
            cm.LaunchUndoableCommand(cmc);
        }

        private void mnuChronoCountdown_Click(object sender, EventArgs e)
        {
            // This menu should only be accessible if we have a "Stop" value.
            _mnuChronoCountdown.Checked = !_mnuChronoCountdown.Checked;

            IUndoableCommand cmc = new CommandModifyChrono(this, _mFrameServer.Metadata,
                ChronoModificationType.Countdown, _mnuChronoCountdown.Checked ? 1 : 0);
            var cm = CommandManager.Instance();
            cm.LaunchUndoableCommand(cmc);

            DoInvalidate();
        }

        private void mnuChronoDelete_Click(object sender, EventArgs e)
        {
            IUndoableCommand cdc = new CommandDeleteChrono(this, _mFrameServer.Metadata);
            var cm = CommandManager.Instance();
            cm.LaunchUndoableCommand(cdc);

            UpdateFramesMarkers();
        }

        private void mnuChronoConfigure_Click(object sender, EventArgs e)
        {
            var dc = _mFrameServer.Metadata.ExtraDrawings[_mFrameServer.Metadata.SelectedExtraDrawing] as DrawingChrono;
            if (dc != null)
            {
                var dp = DelegatesPool.Instance();
                if (dp.DeactivateKeyboardHandler != null)
                {
                    dp.DeactivateKeyboardHandler();
                }

                // Change this chrono display.
                var fcc = new FormConfigureChrono(dc, DoInvalidate);
                ScreenManagerKernel.LocateForm(fcc);
                fcc.ShowDialog();
                fcc.Dispose();
                DoInvalidate();

                if (dp.ActivateKeyboardHandler != null)
                {
                    dp.ActivateKeyboardHandler();
                }
            }
        }

        #endregion Chronometers Menus

        #region Magnifier Menus

        private void mnuMagnifierQuit_Click(object sender, EventArgs e)
        {
            DisableMagnifier();
            DoInvalidate();
        }

        private void mnuMagnifierDirect_Click(object sender, EventArgs e)
        {
            // Use position and magnification to Direct Zoom.
            // Go to direct zoom, at magnifier zoom factor, centered on same point as magnifier.
            _mFrameServer.CoordinateSystem.Zoom = _mFrameServer.Metadata.Magnifier.ZoomFactor;
            _mFrameServer.CoordinateSystem.RelocateZoomWindow(_mFrameServer.Metadata.Magnifier.MagnifiedCenter);
            DisableMagnifier();
            _mFrameServer.Metadata.ResizeFinished();
            ToastZoom();
            DoInvalidate();
        }

        private void mnuMagnifier150_Click(object sender, EventArgs e)
        {
            SetMagnifier(_mnuMagnifier150, 1.5);
        }

        private void mnuMagnifier175_Click(object sender, EventArgs e)
        {
            SetMagnifier(_mnuMagnifier175, 1.75);
        }

        private void mnuMagnifier200_Click(object sender, EventArgs e)
        {
            SetMagnifier(_mnuMagnifier200, 2.0);
        }

        private void mnuMagnifier225_Click(object sender, EventArgs e)
        {
            SetMagnifier(_mnuMagnifier225, 2.25);
        }

        private void mnuMagnifier250_Click(object sender, EventArgs e)
        {
            SetMagnifier(_mnuMagnifier250, 2.5);
        }

        private void SetMagnifier(ToolStripMenuItem menu, double fValue)
        {
            _mFrameServer.Metadata.Magnifier.ZoomFactor = fValue;
            UncheckMagnifierMenus();
            menu.Checked = true;
            DoInvalidate();
        }

        private void UncheckMagnifierMenus()
        {
            _mnuMagnifier150.Checked = false;
            _mnuMagnifier175.Checked = false;
            _mnuMagnifier200.Checked = false;
            _mnuMagnifier225.Checked = false;
            _mnuMagnifier250.Checked = false;
        }

        private void DisableMagnifier()
        {
            // Revert to no magnification.
            _mFrameServer.Metadata.Magnifier.Mode = MagnifierMode.NotVisible;
            //btnMagnifier.Image = Drawings.magnifier;
            SetCursor(_mPointerTool.GetCursor(0));
        }

        #endregion Magnifier Menus

        #endregion Context Menus Events

        #region DirectZoom

        private void UnzoomDirectZoom()
        {
            _mFrameServer.CoordinateSystem.ReinitZoom();
            _mPointerTool.SetZoomLocation(_mFrameServer.CoordinateSystem.Location);
            _mFrameServer.Metadata.ResizeFinished();
        }

        private void IncreaseDirectZoom()
        {
            if (_mFrameServer.Metadata.Magnifier.Mode != MagnifierMode.NotVisible)
            {
                DisableMagnifier();
            }

            if (_mBDrawtimeFiltered && _mDrawingFilterOutput.IncreaseZoom != null)
            {
                _mDrawingFilterOutput.IncreaseZoom(_mDrawingFilterOutput.PrivateData);
            }
            else
            {
                // Max zoom : 600%
                if (_mFrameServer.CoordinateSystem.Zoom < 6.0f)
                {
                    _mFrameServer.CoordinateSystem.Zoom += 0.10f;
                    RelocateDirectZoom();
                    _mFrameServer.Metadata.ResizeFinished();
                    ToastZoom();
                    ReportForSyncMerge();
                }
            }

            DoInvalidate();
        }

        private void DecreaseDirectZoom()
        {
            if (_mBDrawtimeFiltered && _mDrawingFilterOutput.DecreaseZoom != null)
            {
                _mDrawingFilterOutput.DecreaseZoom(_mDrawingFilterOutput.PrivateData);
            }
            else if (_mFrameServer.CoordinateSystem.Zooming)
            {
                if (_mFrameServer.CoordinateSystem.Zoom > 1.1f)
                {
                    _mFrameServer.CoordinateSystem.Zoom -= 0.10f;
                }
                else
                {
                    _mFrameServer.CoordinateSystem.Zoom = 1.0f;
                }

                RelocateDirectZoom();
                _mFrameServer.Metadata.ResizeFinished();
                ToastZoom();
                ReportForSyncMerge();
            }

            DoInvalidate();
        }

        private void RelocateDirectZoom()
        {
            _mFrameServer.CoordinateSystem.RelocateZoomWindow();
            _mPointerTool.SetZoomLocation(_mFrameServer.CoordinateSystem.Location);
        }

        #endregion DirectZoom

        #region Toasts

        private void ToastZoom()
        {
            _mMessageToaster.SetDuration(750);
            var percentage = (int) (_mFrameServer.CoordinateSystem.Zoom*100);
            _mMessageToaster.Show(string.Format(ScreenManagerLang.Toast_Zoom, percentage));
        }

        private void ToastPause()
        {
            _mMessageToaster.SetDuration(750);
            _mMessageToaster.Show(ScreenManagerLang.Toast_Pause);
        }

        #endregion Toasts

        #region Synchronisation specifics

        private void SyncSetAlpha(float alpha)
        {
            _mSyncMergeMatrix.Matrix00 = 1.0f;
            _mSyncMergeMatrix.Matrix11 = 1.0f;
            _mSyncMergeMatrix.Matrix22 = 1.0f;
            _mSyncMergeMatrix.Matrix33 = alpha;
            _mSyncMergeMatrix.Matrix44 = 1.0f;
            _mSyncMergeImgAttr.SetColorMatrix(_mSyncMergeMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        }

        private void ReportForSyncMerge()
        {
            // We have to re-apply the transformations here, because when drawing in this screen we draw directly on the canvas.
            // (there is no intermediate image that we could reuse here, this might be a future optimization).
            // We need to clone it anyway, so we might aswell do the transform.
            if (_mBSynched && _mFrameServer.VideoFile.CurrentImage != null)
            {
                var img = CloneTransformedImage();
                _mPlayerScreenUiHandler.PlayerScreenUI_ImageChanged(img);
            }
        }

        private Bitmap CloneTransformedImage()
        {
            var imgSize = new Size(_mFrameServer.VideoFile.CurrentImage.Size.Width,
                _mFrameServer.VideoFile.CurrentImage.Size.Height);
            var img = new Bitmap(imgSize.Width, imgSize.Height);
            var g = Graphics.FromImage(img);

            Rectangle rDst;
            if (_mFrameServer.Metadata.Mirrored)
            {
                rDst = new Rectangle(imgSize.Width, 0, -imgSize.Width, imgSize.Height);
            }
            else
            {
                rDst = new Rectangle(0, 0, imgSize.Width, imgSize.Height);
            }

            g.DrawImage(_mFrameServer.VideoFile.CurrentImage, rDst, _mFrameServer.CoordinateSystem.ZoomWindow,
                GraphicsUnit.Pixel);
            return img;
        }

        #endregion Synchronisation specifics

        #region VideoFilters Management

        private void EnableDisableAllPlayingControls(bool bEnable)
        {
            // Disable playback controls and some other controls for the case
            // of a one-frame rendering. (mosaic, single image)

            btnSetHandlerLeft.Enabled = bEnable;
            btnSetHandlerRight.Enabled = bEnable;
            btnHandlersReset.Enabled = bEnable;
            btn_HandlersLock.Enabled = bEnable;

            buttonGotoFirst.Enabled = bEnable;
            buttonGotoLast.Enabled = bEnable;
            buttonGotoNext.Enabled = bEnable;
            buttonGotoPrevious.Enabled = bEnable;
            buttonPlay.Enabled = bEnable;
            buttonPlayingMode.Enabled = bEnable;

            lblSpeedTuner.Enabled = bEnable;
            trkFrame.EnableDisable(bEnable);
            trkSelection.EnableDisable(bEnable);
            sldrSpeed.EnableDisable(bEnable);
            trkFrame.Enabled = bEnable;
            trkSelection.Enabled = bEnable;
            sldrSpeed.Enabled = bEnable;

            btnRafale.Enabled = bEnable;
            btnSaveVideo.Enabled = bEnable;
            btnDiaporama.Enabled = bEnable;
            btnPausedVideo.Enabled = bEnable;

            _mnuPlayPause.Visible = bEnable;
            _mnuDirectTrack.Visible = bEnable;
        }

        private void EnableDisableSnapshot(bool bEnable)
        {
            btnSnapShot.Enabled = bEnable;
        }

        private void EnableDisableDrawingTools(bool bEnable)
        {
            foreach (ToolStripItem tsi in stripDrawingTools.Items)
            {
                tsi.Enabled = bEnable;
            }
        }

        #endregion VideoFilters Management

        #region Export video and frames

        private void btnSnapShot_Click(object sender, EventArgs e)
        {
            // Export the current frame.
            if ((_mFrameServer.VideoFile.Loaded) && (_mFrameServer.VideoFile.CurrentImage != null))
            {
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();
                try
                {
                    var dlgSave = new SaveFileDialog();
                    dlgSave.Title = ScreenManagerLang.Generic_SaveImage;
                    dlgSave.RestoreDirectory = true;
                    dlgSave.Filter = ScreenManagerLang.dlgSaveFilter;
                    dlgSave.FilterIndex = 1;

                    if (_mBDrawtimeFiltered && _mDrawingFilterOutput != null)
                    {
                        dlgSave.FileName = Path.GetFileNameWithoutExtension(_mFrameServer.VideoFile.FilePath);
                    }
                    else
                    {
                        dlgSave.FileName = BuildFilename(_mFrameServer.VideoFile.FilePath, _mICurrentPosition,
                            _mPrefManager.TimeCodeFormat);
                    }

                    if (dlgSave.ShowDialog() == DialogResult.OK)
                    {
                        // 1. Reconstruct the extension.
                        // If the user let "file.00.00" as a filename, the extension is not appended automatically.
                        var strImgNameLower = dlgSave.FileName.ToLower();
                        string strImgName;
                        if (strImgNameLower.EndsWith("jpg") || strImgNameLower.EndsWith("jpeg") ||
                            strImgNameLower.EndsWith("bmp") || strImgNameLower.EndsWith("png"))
                        {
                            // Ok, the user added the extension himself or he did not use the preformatting.
                            strImgName = dlgSave.FileName;
                        }
                        else
                        {
                            // Get the extension
                            string extension;
                            switch (dlgSave.FilterIndex)
                            {
                                case 1:
                                    extension = ".jpg";
                                    break;

                                case 2:
                                    extension = ".png";
                                    break;

                                case 3:
                                    extension = ".bmp";
                                    break;

                                default:
                                    extension = ".jpg";
                                    break;
                            }
                            strImgName = dlgSave.FileName + extension;
                        }

                        //2. Get image and save it to the file.
                        var outputImage = GetFlushedImage();
                        ImageHelper.Save(strImgName, outputImage);
                        outputImage.Dispose();
                        _mFrameServer.AfterSave();
                    }
                }
                catch (Exception exp)
                {
                    Log.Error(exp.StackTrace);
                }
            }
        }

        private void btnRafale_Click(object sender, EventArgs e)
        {
            //---------------------------------------------------------------------------------
            // Workflow:
            // 1. formRafaleExport  : configure the export, calls:
            // 2. FileSaveDialog    : choose the file name, then:
            // 3. formFrameExport   : Progress bar holder and updater, calls:
            // 4. SaveImageSequence (below) to perform the real work. (saving the pics)
            //---------------------------------------------------------------------------------

            if ((_mFrameServer.VideoFile.Loaded) && (_mFrameServer.VideoFile.CurrentImage != null))
            {
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();

                var dp = DelegatesPool.Instance();
                if (dp.DeactivateKeyboardHandler != null)
                {
                    dp.DeactivateKeyboardHandler();
                }

                // Launch sequence saving configuration dialog
                var fre = new FormRafaleExport(this,
                    _mFrameServer.Metadata,
                    _mFrameServer.VideoFile.FilePath,
                    SelectionDuration,
                    _mFrameServer.VideoFile.Infos.fAverageTimeStampsPerSeconds);
                fre.ShowDialog();
                fre.Dispose();
                _mFrameServer.AfterSave();

                if (dp.ActivateKeyboardHandler != null)
                {
                    dp.ActivateKeyboardHandler();
                }
            }
        }

        public void SaveImageSequence(BackgroundWorker bgWorker, string filePath, long iIntervalTimeStamps,
            bool bBlendDrawings, bool bKeyframesOnly, int iEstimatedTotal)
        {
            //---------------------------------------------------------------
            // Save image sequence.
            // (Method called back from the FormRafaleExport dialog box)
            //
            // We start at the first frame and use the interval in timestamps.
            // We append the timecode between the filename and the extension.
            //---------------------------------------------------------------

            //-------------------------------------------------------------
            // /!\ Cette fonction s'execute dans l'espace du WORKER THREAD.
            // Les fonctions appelées d'ici ne doivent pas toucher l'UI.
            // Les appels ici sont synchrones mais on peut remonter de
            // l'information par bgWorker_ProgressChanged().
            //-------------------------------------------------------------
            if (bKeyframesOnly)
            {
                var iCurrent = 0;
                var iTotal = _mFrameServer.Metadata.Keyframes.Count;
                foreach (var kf in _mFrameServer.Metadata.Keyframes)
                {
                    if (kf.Position >= _mISelStart && kf.Position <= _mISelEnd)
                    {
                        // Build the file name
                        var fileName = Path.GetDirectoryName(filePath) + "\\" +
                                       BuildFilename(filePath, kf.Position, _mPrefManager.TimeCodeFormat) +
                                       Path.GetExtension(filePath);

                        // Get the image
                        var iNewSize = new Size((int) (kf.FullFrame.Width*_mFrameServer.CoordinateSystem.Stretch),
                            (int) (kf.FullFrame.Height*_mFrameServer.CoordinateSystem.Stretch));
                        var outputImage = new Bitmap(iNewSize.Width, iNewSize.Height, PixelFormat.Format24bppRgb);
                        outputImage.SetResolution(kf.FullFrame.HorizontalResolution, kf.FullFrame.VerticalResolution);
                        var g = Graphics.FromImage(outputImage);

                        if (bBlendDrawings)
                        {
                            FlushOnGraphics(kf.FullFrame, g, iNewSize, iCurrent, kf.Position);
                        }
                        else
                        {
                            // image only.
                            g.DrawImage(kf.FullFrame, 0, 0, iNewSize.Width, iNewSize.Height);
                        }

                        // Save the file
                        ImageHelper.Save(fileName, outputImage);
                        outputImage.Dispose();
                    }

                    // Report to Progress Bar
                    iCurrent++;
                    bgWorker.ReportProgress(iCurrent, iTotal);
                }
            }
            else
            {
                // We are in the worker thread space.
                // We'll move the playhead and check for rafale period.

                _mIFramesToDecode = 1;
                ShowNextFrame(_mISelStart, false);

                var done = false;
                var iCurrent = 0;
                do
                {
                    ActivateKeyframe(_mICurrentPosition, false);

                    // Build the file name
                    var fileName = Path.GetDirectoryName(filePath) + "\\" +
                                   BuildFilename(filePath, _mICurrentPosition, _mPrefManager.TimeCodeFormat) +
                                   Path.GetExtension(filePath);

                    var iNewSize =
                        new Size(
                            (int) (_mFrameServer.VideoFile.CurrentImage.Width*_mFrameServer.CoordinateSystem.Stretch),
                            (int) (_mFrameServer.VideoFile.CurrentImage.Height*_mFrameServer.CoordinateSystem.Stretch));
                    var outputImage = new Bitmap(iNewSize.Width, iNewSize.Height, PixelFormat.Format24bppRgb);
                    outputImage.SetResolution(_mFrameServer.VideoFile.CurrentImage.HorizontalResolution,
                        _mFrameServer.VideoFile.CurrentImage.VerticalResolution);
                    var g = Graphics.FromImage(outputImage);

                    if (bBlendDrawings)
                    {
                        var iKeyFrameIndex = -1;
                        if (_mIActiveKeyFrameIndex >= 0 &&
                            _mFrameServer.Metadata[_mIActiveKeyFrameIndex].Drawings.Count > 0)
                        {
                            iKeyFrameIndex = _mIActiveKeyFrameIndex;
                        }

                        FlushOnGraphics(_mFrameServer.VideoFile.CurrentImage, g, iNewSize, iKeyFrameIndex,
                            _mICurrentPosition);
                    }
                    else
                    {
                        // image only.
                        g.DrawImage(_mFrameServer.VideoFile.CurrentImage, 0, 0, iNewSize.Width, iNewSize.Height);
                    }

                    // Save the file
                    ImageHelper.Save(fileName, outputImage);
                    outputImage.Dispose();

                    // Report to Progress Bar
                    iCurrent++;
                    bgWorker.ReportProgress(iCurrent, iEstimatedTotal);

                    // Go to next timestamp.
                    if (_mICurrentPosition + iIntervalTimeStamps < _mISelEnd)
                    {
                        _mIFramesToDecode = 1;
                        ShowNextFrame(_mICurrentPosition + iIntervalTimeStamps, false);
                    }
                    else
                    {
                        done = true;
                    }
                } while (!done);

                // Replace at selection start.
                _mIFramesToDecode = 1;
                ShowNextFrame(_mISelStart, false);
                ActivateKeyframe(_mICurrentPosition, false);
            }

            DoInvalidate();
        }

        private void btnVideo_Click(object sender, EventArgs e)
        {
            if (_mFrameServer.Loaded)
            {
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();
                var dp = DelegatesPool.Instance();
                if (dp.DeactivateKeyboardHandler != null)
                {
                    dp.DeactivateKeyboardHandler();
                }

                Save();

                if (dp.ActivateKeyboardHandler != null)
                {
                    dp.ActivateKeyboardHandler();
                }
            }
        }

        private void btnDiaporama_Click(object sender, EventArgs e)
        {
            var bDiapo = sender == btnDiaporama;

            if (_mFrameServer.Metadata.Keyframes.Count < 1)
            {
                if (bDiapo)
                {
                    MessageBox.Show(ScreenManagerLang.Error_SaveDiaporama_NoKeyframes.Replace("\\n", "\n"),
                        ScreenManagerLang.Error_SaveDiaporama,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
                }
                else
                {
                    MessageBox.Show(ScreenManagerLang.Error_SavePausedVideo_NoKeyframes.Replace("\\n", "\n"),
                        ScreenManagerLang.Error_SavePausedVideo,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
                }
            }
            else if ((_mFrameServer.VideoFile.Loaded) && (_mFrameServer.VideoFile.CurrentImage != null))
            {
                StopPlaying();
                _mPlayerScreenUiHandler.PlayerScreenUI_PauseAsked();

                var dp = DelegatesPool.Instance();
                if (dp.DeactivateKeyboardHandler != null)
                {
                    dp.DeactivateKeyboardHandler();
                }

                _mFrameServer.SaveDiaporama(_mISelStart, _mISelEnd, GetOutputBitmap, bDiapo);

                if (dp.ActivateKeyboardHandler != null)
                {
                    dp.ActivateKeyboardHandler();
                }
            }
        }

        public void Save()
        {
            // Todo:
            // Eventually, this call should be done directly by PlayerScreen, without passing through the UI.
            // This will be possible when m_FrameServer.Metadata, m_iSelStart, m_iSelEnd are encapsulated in m_FrameServer
            // and when PlaybackFrameInterval, m_iSlowmotionPercentage, GetOutputBitmap are available publically.

            _mFrameServer.Save(GetPlaybackFrameInterval(),
                _mFSlowmotionPercentage,
                _mISelStart,
                _mISelEnd,
                GetOutputBitmap);
        }

        public long GetOutputBitmap(Graphics canvas, Bitmap sourceImage, long iTimestamp, bool bFlushDrawings,
            bool bKeyframesOnly)
        {
            // Used by the VideoFile for SaveMovie.
            // The image to save was already retrieved (from stream or analysis array)
            // This image is already drawn on _canvas.
            // Here we we flush the drawings on it if needed.
            // We return the distance to the closest key image.
            // This can then be used by the caller.

            // 1. Look for the closest key image.
            var iClosestKeyImageDistance = long.MaxValue;
            var iKeyFrameIndex = -1;
            for (var i = 0; i < _mFrameServer.Metadata.Keyframes.Count; i++)
            {
                var iDistance = Math.Abs(iTimestamp - _mFrameServer.Metadata.Keyframes[i].Position);
                if (iDistance < iClosestKeyImageDistance)
                {
                    iClosestKeyImageDistance = iDistance;
                    iKeyFrameIndex = i;
                }
            }

            // 2. Invalidate the distance if we wanted only key images, and we are not on one.
            // Or if there is no key image at all.
            if ((bKeyframesOnly && iClosestKeyImageDistance != 0) || (iClosestKeyImageDistance == long.MaxValue))
            {
                iClosestKeyImageDistance = -1;
            }

            // 3. Flush drawings if needed.
            if (bFlushDrawings)
            {
                Bitmap rawImage = null;

                if (_mFrameServer.Metadata.Magnifier.Mode != MagnifierMode.NotVisible)
                {
                    // For the magnifier, we must clone the image since the graphics object has been
                    // extracted from the image itself (painting fails if we reuse the uncloned image).
                    // And we must clone it before the drawings are flushed on it.
                    rawImage = Image.Clone(sourceImage);
                }

                var temp = _mFrameServer.CoordinateSystem.Identity;

                if (bKeyframesOnly)
                {
                    if (iClosestKeyImageDistance == 0)
                    {
                        FlushDrawingsOnGraphics(canvas, temp, iKeyFrameIndex, iTimestamp, 1.0f, 1.0f, new Point(0, 0));
                        FlushMagnifierOnGraphics(rawImage, canvas);
                    }
                }
                else
                {
                    if (iClosestKeyImageDistance == 0)
                    {
                        FlushDrawingsOnGraphics(canvas, temp, iKeyFrameIndex, iTimestamp, 1.0f, 1.0f, new Point(0, 0));
                    }
                    else
                    {
                        FlushDrawingsOnGraphics(canvas, temp, -1, iTimestamp, 1.0f, 1.0f, new Point(0, 0));
                    }

                    FlushMagnifierOnGraphics(rawImage, canvas);
                }
            }

            return iClosestKeyImageDistance;
        }

        public Bitmap GetFlushedImage()
        {
            // Returns an image with all drawings flushed, including
            // grids, chronos, magnifier, etc.
            // image should be at same strech factor than the one visible on screen.
            var iNewSize =
                new Size((int) (_mFrameServer.VideoFile.CurrentImage.Width*_mFrameServer.CoordinateSystem.Stretch),
                    (int) (_mFrameServer.VideoFile.CurrentImage.Height*_mFrameServer.CoordinateSystem.Stretch));
            var output = new Bitmap(iNewSize.Width, iNewSize.Height, PixelFormat.Format24bppRgb);
            output.SetResolution(_mFrameServer.VideoFile.CurrentImage.HorizontalResolution,
                _mFrameServer.VideoFile.CurrentImage.VerticalResolution);

            if (_mBDrawtimeFiltered && _mDrawingFilterOutput.Draw != null)
            {
                _mDrawingFilterOutput.Draw(Graphics.FromImage(output), iNewSize, _mDrawingFilterOutput.PrivateData);
            }
            else
            {
                var iKeyFrameIndex = -1;
                if (_mIActiveKeyFrameIndex >= 0 && _mFrameServer.Metadata[_mIActiveKeyFrameIndex].Drawings.Count > 0)
                {
                    iKeyFrameIndex = _mIActiveKeyFrameIndex;
                }

                FlushOnGraphics(_mFrameServer.VideoFile.CurrentImage, Graphics.FromImage(output), iNewSize,
                    iKeyFrameIndex, _mICurrentPosition);
            }

            return output;
        }

        private string BuildFilename(string filePath, long position, TimeCodeFormat timeCodeFormat)
        {
            //-------------------------------------------------------
            // Build a file name, including extension
            // inserting the current timecode in the given file name.
            //-------------------------------------------------------

            TimeCodeFormat tcf;
            if (timeCodeFormat == TimeCodeFormat.TimeAndFrames)
                tcf = TimeCodeFormat.ClassicTime;
            else
                tcf = timeCodeFormat;

            // Timecode string (Not relative to sync position)
            var suffix = TimeStampsToTimecode(position - _mISelStart, tcf, false);
            var maxSuffix = TimeStampsToTimecode(_mISelEnd - _mISelStart, tcf, false);

            switch (tcf)
            {
                case TimeCodeFormat.Frames:
                case TimeCodeFormat.Milliseconds:
                case TimeCodeFormat.TenThousandthOfHours:
                case TimeCodeFormat.HundredthOfMinutes:

                    var iZerosToPad = maxSuffix.Length - suffix.Length;
                    for (var i = 0; i < iZerosToPad; i++)
                    {
                        // Add a leading zero.
                        suffix = suffix.Insert(0, "0");
                    }
                    break;

                default:
                    break;
            }

            // Reconstruct filename
            return Path.GetFileNameWithoutExtension(filePath) + "-" + suffix.Replace(':', '.');
        }

        #endregion Export video and frames

        #region Memo & Reset

        public MemoPlayerScreen GetMemo()
        {
            return new MemoPlayerScreen(_mISelStart, _mISelEnd);
        }

        public void ResetSelectionImages(MemoPlayerScreen memo)
        {
            // This is typically called when undoing image adjustments.
            // We do not actually undo the adjustment because we don't have the original data anymore.
            // We emulate it by reloading the selection.

            // Memorize the current selection boundaries.
            var mps = new MemoPlayerScreen(_mISelStart, _mISelEnd);

            // Reset the selection to whatever it was when we did the image adjustment.
            _mISelStart = memo.SelStart;
            _mISelEnd = memo.SelEnd;

            // Undo all adjustments made on this portion.
            ImportSelectionToMemory(true);
            UpdateKeyframes();

            // Reset to the current selection.
            _mISelStart = mps.SelStart;
            _mISelEnd = mps.SelEnd;
        }

        #endregion Memo & Reset
    }
}