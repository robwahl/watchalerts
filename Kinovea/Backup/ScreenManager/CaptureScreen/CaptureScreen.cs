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


using System;
using System.Drawing;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;

using Kinovea.ScreenManager.Languages;

namespace Kinovea.ScreenManager
{
    public class CaptureScreen : AbstractScreen, ICaptureScreenUIHandler
    {
        #region Properties
        public override Guid UniqueId
        {
            get { return m_UniqueId; }
            set { m_UniqueId = value;}
        }
        public override bool Full
        {
        	get { return m_FrameServer.IsConnected; }	
        }
        public override string FileName
		{
			get 
			{ 
				if(m_FrameServer.IsConnected)
				{
					return m_FrameServer.DeviceName;		
				}
				else
				{
					return ScreenManagerLang.statusEmptyScreen;	
				}
			}
		}
		public override string Status
		{
			get	{ return m_FrameServer.Status;}
		}
        public override UserControl UI
        {
        	get { return m_CaptureScreenUI; }	
        }
		public override string FilePath
		{
			get { return ""; }
		}
		public override bool CapabilityDrawings
		{
			get { return true;}
		}
		public override VideoFiles.AspectRatio AspectRatio
		{
			get { return m_FrameServer.AspectRatio; }
			set { m_FrameServer.AspectRatio = value; }
		}
		public FrameServerCapture FrameServer
		{
			get { return m_FrameServer; }
			set { m_FrameServer = value; }
		}  
        public bool Shared
        {
        	set 
        	{
        		m_FrameServer.Shared = value;
        		m_FrameServer.UpdateMemoryCapacity();
        	}
        }
        public static readonly int HeartBeat = 1000;
        #endregion

        #region Members
        private IScreenHandler m_ScreenHandler; // ScreenManager seen through a limited interface.
        
        private CaptureScreenUserInterface	m_CaptureScreenUI;
		private FrameServerCapture m_FrameServer = new FrameServerCapture();
		private Guid m_UniqueId = System.Guid.NewGuid();
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region Constructor
        public CaptureScreen(IScreenHandler _screenHandler)
        {
        	log.Debug("Constructing a CaptureScreen.");
            m_ScreenHandler = _screenHandler;
            m_CaptureScreenUI = new CaptureScreenUserInterface(m_FrameServer, this);
        }
        #endregion

        #region ICaptureScreenUIHandler implementation
        public void ScreenUI_CloseAsked()
        {
        	m_ScreenHandler.Screen_CloseAsked(this);
        }
        public void ScreenUI_SetAsActiveScreen()
        {
        	m_ScreenHandler.Screen_SetActiveScreen(this);
        }
        public void ScreenUI_UpdateStatusBarAsked()
        {
        	m_ScreenHandler.Screen_UpdateStatusBarAsked(this);
        }
        public void CaptureScreenUI_FileSaved()
        {
        	m_ScreenHandler.Capture_FileSaved(this);
        }
        public void CaptureScreenUI_LoadVideo(string _filepath)
        {
        	m_ScreenHandler.Capture_LoadVideo(this, _filepath);
        }
        #endregion
        
        #region AbstractScreen Implementation
        public override void DisplayAsActiveScreen(bool _bActive)
        {
        	m_CaptureScreenUI.DisplayAsActiveScreen(_bActive);
        }
        public override void refreshUICulture() 
        {
        	m_CaptureScreenUI.RefreshUICulture();
        }
        public override void BeforeClose()
        {
        	m_FrameServer.BeforeClose();
        	m_CaptureScreenUI.BeforeClose();
        }
        public override bool OnKeyPress(Keys _key)
        {
        	return m_CaptureScreenUI.OnKeyPress(_key);
        }
		public override void RefreshImage()
		{
			// Not implemented.
		}
		public override void AddImageDrawing(string _filename, bool _bIsSvg)
        {
			m_CaptureScreenUI.AddImageDrawing(_filename, _bIsSvg);
        }
		public override void AddImageDrawing(Bitmap _bmp)
        {
			// Implemented but currently not used.
        	m_CaptureScreenUI.AddImageDrawing(_bmp);
        }
		public override void FullScreen(bool _bFullScreen)
        {
            m_CaptureScreenUI.FullScreen(_bFullScreen);
        }
        #endregion
    }
}
