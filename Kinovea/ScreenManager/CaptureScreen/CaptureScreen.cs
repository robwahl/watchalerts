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
using Kinovea.VideoFiles;
using log4net;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public class CaptureScreen : AbstractScreen, ICaptureScreenUiHandler
    {
        #region Constructor

        public CaptureScreen(IScreenHandler screenHandler)
        {
            Log.Debug("Constructing a CaptureScreen.");
            _mScreenHandler = screenHandler;
            _mCaptureScreenUi = new CaptureScreenUserInterface(FrameServer, this);
        }

        #endregion Constructor

        #region Properties

        public override Guid UniqueId { get; set; } = Guid.NewGuid();

        public override bool Full
        {
            get { return FrameServer.IsConnected; }
        }

        public override string FileName
        {
            get
            {
                if (FrameServer.IsConnected)
                {
                    return FrameServer.DeviceName;
                }
                return ScreenManagerLang.statusEmptyScreen;
            }
        }

        public override string Status
        {
            get { return FrameServer.Status; }
        }

        public override UserControl Ui
        {
            get { return _mCaptureScreenUi; }
        }

        public override string FilePath
        {
            get { return ""; }
        }

        public override bool CapabilityDrawings
        {
            get { return true; }
        }

        public override AspectRatio AspectRatio
        {
            get { return FrameServer.AspectRatio; }
            set { FrameServer.AspectRatio = value; }
        }

        public FrameServerCapture FrameServer { get; set; } = private new FrameServerCapture();

        public bool Shared
        {
            set
            {
                FrameServer.Shared = value;
                FrameServer.UpdateMemoryCapacity();
            }
        }

        public static readonly int HeartBeat = 1000;

        #endregion Properties

        #region Members

        private readonly IScreenHandler _mScreenHandler; // ScreenManager seen through a limited interface.

        private readonly CaptureScreenUserInterface _mCaptureScreenUi;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region ICaptureScreenUIHandler implementation

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

        public void CaptureScreenUI_FileSaved()
        {
            _mScreenHandler.Capture_FileSaved(this);
        }

        public void CaptureScreenUI_LoadVideo(string filepath)
        {
            _mScreenHandler.Capture_LoadVideo(this, filepath);
        }

        #endregion ICaptureScreenUIHandler implementation

        #region AbstractScreen Implementation

        public override void DisplayAsActiveScreen(bool bActive)
        {
            _mCaptureScreenUi.DisplayAsActiveScreen(bActive);
        }

        public override void RefreshUiCulture()
        {
            _mCaptureScreenUi.RefreshUiCulture();
        }

        public override void BeforeClose()
        {
            FrameServer.BeforeClose();
            _mCaptureScreenUi.BeforeClose();
        }

        public override bool OnKeyPress(Keys key)
        {
            return _mCaptureScreenUi.OnKeyPress(key);
        }

        public override void RefreshImage()
        {
            // Not implemented.
        }

        public override void AddImageDrawing(string filename, bool bIsSvg)
        {
            _mCaptureScreenUi.AddImageDrawing(filename, bIsSvg);
        }

        public override void AddImageDrawing(Bitmap bmp)
        {
            // Implemented but currently not used.
            _mCaptureScreenUi.AddImageDrawing(bmp);
        }

        public override void FullScreen(bool bFullScreen)
        {
            _mCaptureScreenUi.FullScreen(bFullScreen);
        }

        #endregion AbstractScreen Implementation
    }
}