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
using Kinovea.VideoFiles;
using log4net;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

[assembly: CLSCompliant(false)]

namespace Kinovea.ScreenManager
{
    public class PlayerScreen : AbstractScreen, IPlayerScreenUiHandler
    {
        #region Constructor

        public PlayerScreen(IScreenHandler screenHandler)
        {
            Log.Debug("Constructing a PlayerScreen.");
            _mScreenHandler = screenHandler;
            _mUniqueId = Guid.NewGuid();
            MPlayerScreenUi = new PlayerScreenUserInterface(FrameServer, this);
        }

        #endregion Constructor

        #region Properties

        public override bool Full
        {
            get { return FrameServer.VideoFile.Loaded; }
        }

        public override UserControl Ui
        {
            get { return MPlayerScreenUi; }
        }

        public override Guid UniqueId
        {
            get { return _mUniqueId; }
            set { _mUniqueId = value; }
        }

        public override string FileName
        {
            get
            {
                if (FrameServer.VideoFile.Loaded)
                {
                    return Path.GetFileName(FrameServer.VideoFile.FilePath);
                }
                return ScreenManagerLang.statusEmptyScreen;
            }
        }

        public override string Status
        {
            get { return FileName; }
        }

        public override string FilePath
        {
            get { return FrameServer.VideoFile.FilePath; }
        }

        public override bool CapabilityDrawings
        {
            get { return true; }
        }

        public override AspectRatio AspectRatio
        {
            get { return FrameServer.VideoFile.Infos.eAspectRatio; }
            set
            {
                FrameServer.VideoFile.ChangeAspectRatio(value);

                if (FrameServer.VideoFile.Selection.iAnalysisMode == 1)
                {
                    MPlayerScreenUi.ImportSelectionToMemory(true);
                }

                MPlayerScreenUi.UpdateImageSize();
                RefreshImage();
            }
        }

        public FrameServerPlayer FrameServer { get; set; } = private new FrameServerPlayer();

        public bool IsPlaying
        {
            get
            {
                if (!FrameServer.VideoFile.Loaded)
                {
                    return false;
                }
                return (MPlayerScreenUi.IsCurrentlyPlaying);
            }
        }

        public bool IsSingleFrame
        {
            get
            {
                if (!FrameServer.VideoFile.Loaded)
                {
                    return false;
                }
                return (FrameServer.VideoFile.Infos.iDurationTimeStamps == 1);
            }
        }

        public bool IsInAnalysisMode
        {
            get
            {
                if (!FrameServer.VideoFile.Loaded)
                {
                    return false;
                }
                return (FrameServer.VideoFile.Selection.iAnalysisMode == 1);
            }
        }

        public int CurrentFrame
        {
            get
            {
                // Get the approximate frame we should be on.
                // Only as accurate as the framerate is stable regarding to the timebase.

                // Timestamp (relative to selection start).
                var iCurrentTimestamp = MPlayerScreenUi.SyncCurrentPosition;
                return (int) (iCurrentTimestamp/FrameServer.VideoFile.Infos.iAverageTimeStampsPerFrame);
            }
        }

        public int LastFrame
        {
            get
            {
                if (FrameServer.VideoFile.Selection.iAnalysisMode == 1)
                {
                    return FrameServer.VideoFile.Selection.iDurationFrame - 1;
                }
                var iDurationTimestamp = MPlayerScreenUi.SelectionDuration;
                return (int) (iDurationTimestamp/FrameServer.VideoFile.Infos.iAverageTimeStampsPerFrame) - 1;
            }
        }

        public double FrameInterval
        {
            get
            {
                // Returns the playback interval between frames in Milliseconds, taking slow motion slider into account.
                if (FrameServer.VideoFile.Loaded && FrameServer.VideoFile.Infos.fFrameInterval > 0)
                {
                    return MPlayerScreenUi.FrameInterval;
                }
                return 40;
            }
        }

        public double RealtimePercentage
        {
            get { return MPlayerScreenUi.RealtimePercentage; }
            set { MPlayerScreenUi.RealtimePercentage = value; }
        }

        public bool Synched
        {
            //get { return m_PlayerScreenUI.m_bSynched; }
            set { MPlayerScreenUi.Synched = value; }
        }

        public long SyncPosition
        {
            // Reference timestamp for synchronization, expressed in local timebase.
            get { return MPlayerScreenUi.SyncPosition; }
            set { MPlayerScreenUi.SyncPosition = value; }
        }

        public long Position
        {
            // Used to feed SyncPosition.
            get
            {
                return FrameServer.VideoFile.Selection.iCurrentTimeStamp - FrameServer.VideoFile.Infos.iFirstTimeStamp;
            }
        }

        public bool SyncMerge
        {
            set
            {
                MPlayerScreenUi.SyncMerge = value;
                RefreshImage();
            }
        }

        public bool DualSaveInProgress
        {
            set { MPlayerScreenUi.DualSaveInProgress = value; }
        }

        // Pseudo Filters (Impacts rendering)
        public bool Deinterlaced
        {
            get { return FrameServer.VideoFile.Infos.bDeinterlaced; }
            set
            {
                FrameServer.VideoFile.Infos.bDeinterlaced = value;

                // If there was a selection it must be imported again.
                // (This means we'll loose color adjustments.)
                if (FrameServer.VideoFile.Selection.iAnalysisMode == 1)
                {
                    MPlayerScreenUi.ImportSelectionToMemory(true);
                }

                RefreshImage();
            }
        }

        public bool Mirrored
        {
            get { return FrameServer.Metadata.Mirrored; }
            set
            {
                FrameServer.Metadata.Mirrored = value;
                RefreshImage();
            }
        }

        public int DrawtimeFilterType
        {
            get { return MPlayerScreenUi.DrawtimeFilterType; }
        }

        #endregion Properties

        #region members

        public PlayerScreenUserInterface MPlayerScreenUi;

        private readonly IScreenHandler _mScreenHandler;
        private Guid _mUniqueId;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion members

        #region IPlayerScreenUIHandler (and IScreenUIHandler) implementation

        public void ScreenUI_CloseAsked()
        {
            _mScreenHandler.Screen_CloseAsked(this);
        }

        public void ScreenUI_SetAsActiveScreen()
        {
            _mScreenHandler.Screen_SetActiveScreen(this);
        }

        public void ScreenUI_UpdateStatusBarAsked()
        {
            _mScreenHandler.Screen_UpdateStatusBarAsked(this);
        }

        public void PlayerScreenUI_SpeedChanged(bool bIntervalOnly)
        {
            // Used for synchronisation handling.
            _mScreenHandler.Player_SpeedChanged(this, bIntervalOnly);
        }

        public void PlayerScreenUI_PauseAsked()
        {
            _mScreenHandler.Player_PauseAsked(this);
        }

        public void PlayerScreenUI_SelectionChanged(bool bInitialization)
        {
            // Used for synchronisation handling.
            _mScreenHandler.Player_SelectionChanged(this, bInitialization);
        }

        public void PlayerScreenUI_ImageChanged(Bitmap image)
        {
            _mScreenHandler.Player_ImageChanged(this, image);
        }

        public void PlayerScreenUI_SendImage(Bitmap image)
        {
            _mScreenHandler.Player_SendImage(this, image);
        }

        public void PlayerScreenUI_Reset()
        {
            _mScreenHandler.Player_Reset(this);
        }

        #endregion IPlayerScreenUIHandler (and IScreenUIHandler) implementation

        #region AbstractScreen Implementation

        public override void DisplayAsActiveScreen(bool bActive)
        {
            MPlayerScreenUi.DisplayAsActiveScreen(bActive);
        }

        public override void BeforeClose()
        {
            // Called by the ScreenManager when this screen is about to be closed.
            // Note: We shouldn't call ResetToEmptyState here because we will want
            // the close screen routine to detect if there is something left in the
            // metadata and alerts the user.
            MPlayerScreenUi.StopPlaying();
        }

        public override void RefreshUiCulture()
        {
            MPlayerScreenUi.RefreshUiCulture();
        }

        public override bool OnKeyPress(Keys key)
        {
            return MPlayerScreenUi.OnKeyPress(key);
        }

        public override void RefreshImage()
        {
            MPlayerScreenUi.RefreshImage();
        }

        public override void AddImageDrawing(string filename, bool bIsSvg)
        {
            MPlayerScreenUi.AddImageDrawing(filename, bIsSvg);
        }

        public override void AddImageDrawing(Bitmap bmp)
        {
            MPlayerScreenUi.AddImageDrawing(bmp);
        }

        public override void FullScreen(bool bFullScreen)
        {
            MPlayerScreenUi.FullScreen(bFullScreen);
        }

        #endregion AbstractScreen Implementation

        #region Other public methods called from the ScreenManager

        public void StopPlaying()
        {
            MPlayerScreenUi.StopPlaying();
        }

        public void GotoNextFrame(bool bAllowUiUpdate)
        {
            MPlayerScreenUi.SyncSetCurrentFrame(-1, bAllowUiUpdate);
        }

        public void GotoFrame(int frame, bool bAllowUiUpdate)
        {
            MPlayerScreenUi.SyncSetCurrentFrame(frame, bAllowUiUpdate);
        }

        public void ResetSelectionImages(MemoPlayerScreen memo)
        {
            MPlayerScreenUi.ResetSelectionImages(memo);
        }

        public MemoPlayerScreen GetMemo()
        {
            return MPlayerScreenUi.GetMemo();
        }

        public void SetDrawingtimeFilterOutput(DrawtimeFilterOutput dfo)
        {
            // A video filter just finished and is passing us its output object.
            // It is used as a communication channel between the filter and the player.
            MPlayerScreenUi.SetDrawingtimeFilterOutput(dfo);
        }

        public void SetSyncMergeImage(Bitmap syncMergeImage, bool bUpdateUi)
        {
            MPlayerScreenUi.SetSyncMergeImage(syncMergeImage, bUpdateUi);
        }

        public void Save()
        {
            MPlayerScreenUi.Save();
        }

        public void ConfigureHighSpeedCamera()
        {
            MPlayerScreenUi.DisplayConfigureSpeedBox(true);
        }

        public long GetOutputBitmap(Graphics canvas, Bitmap sourceImage, long iTimestamp, bool bFlushDrawings,
            bool bKeyframesOnly)
        {
            return MPlayerScreenUi.GetOutputBitmap(canvas, sourceImage, iTimestamp, bFlushDrawings, bKeyframesOnly);
        }

        public Bitmap GetFlushedImage()
        {
            return MPlayerScreenUi.GetFlushedImage();
        }

        #endregion Other public methods called from the ScreenManager
    }
}