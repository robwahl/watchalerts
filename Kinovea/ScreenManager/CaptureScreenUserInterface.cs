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
using log4net;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

#endregion Using directives

namespace Kinovea.ScreenManager
{
    public partial class CaptureScreenUserInterface : UserControl, IFrameServerCaptureContainer
    {
        #region Constructor

        public CaptureScreenUserInterface(FrameServerCapture frameServer, ICaptureScreenUiHandler screenUiHandler)
        {
            Log.Debug("Constructing the CaptureScreen user interface.");
            _mScreenUiHandler = screenUiHandler;
            _mFrameServer = frameServer;
            _mFrameServer.SetContainer(this);
            _mFrameServer.Metadata = new Metadata(null, null);

            // Initialize UI.
            InitializeComponent();
            UpdateDelayLabel();
            AddExtraControls();
            Dock = DockStyle.Fill;
            ShowHideResizers(false);
            InitializeDrawingTools();
            InitializeMetadata();
            BuildContextMenus();
            tmrCaptureDeviceDetector.Interval = CaptureScreen.HeartBeat;
            _mBDocked = true;

            InitializeCaptureFiles();
            _mMessageToaster = new MessageToaster(pbSurfaceScreen);

            // Delegates
            _mInitDecodingSize = InitDecodingSize_Invoked;

            _mDeselectionTimer.Interval = 3000;
            _mDeselectionTimer.Tick += DeselectionTimer_OnTick;

            TryToConnect();
            tmrCaptureDeviceDetector.Start();
        }

        #endregion Constructor

        #region Internal delegates for async methods

        private delegate void InitDecodingSize();

        #endregion Internal delegates for async methods

        #region Members

        private readonly ICaptureScreenUiHandler _mScreenUiHandler; // CaptureScreen seen trough a limited interface.
        private readonly FrameServerCapture _mFrameServer;

        // General
        private readonly PreferencesManager _mPrefManager = PreferencesManager.Instance();

        private bool _mBTryingToConnect;
        private int _mIDelay;

        // Image
        private bool _mBStretchModeOn; // This is just a toggle to know what to do on double click.

        private bool _mBShowImageBorder;
        private static readonly Pen MPenImageBorder = Pens.SteelBlue;

        // Keyframes, Drawings, etc.
        private DrawingToolPointer _mActiveTool;

        private DrawingToolPointer _mPointerTool;
        private bool _mBDocked = true;
        private bool _mBTextEdit;

        // Other
        private readonly Timer _mDeselectionTimer = new Timer();

        private readonly MessageToaster _mMessageToaster;
        private string _mLastSavedImage;
        private string _mLastSavedVideo;
        private readonly FilenameHelper _mFilenameHelper = new FilenameHelper();

        #region Context Menus

        private readonly ContextMenuStrip _popMenu = new ContextMenuStrip();
        private readonly ToolStripMenuItem _mnuCamSettings = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuSavePic = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuCloseScreen = new ToolStripMenuItem();

        private readonly ContextMenuStrip _popMenuDrawings = new ContextMenuStrip();
        private readonly ToolStripMenuItem _mnuConfigureDrawing = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuConfigureOpacity = new ToolStripMenuItem();
        private readonly ToolStripSeparator _mnuSepDrawing = new ToolStripSeparator();
        private readonly ToolStripSeparator _mnuSepDrawing2 = new ToolStripSeparator();
        private readonly ToolStripMenuItem _mnuDeleteDrawing = new ToolStripMenuItem();

        private readonly ContextMenuStrip _popMenuMagnifier = new ContextMenuStrip();
        private readonly ToolStripMenuItem _mnuMagnifier150 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifier175 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifier200 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifier225 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifier250 = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifierDirect = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuMagnifierQuit = new ToolStripMenuItem();

        #endregion Context Menus

        private ToolStripButton _mBtnToolPresets;

        private readonly SpeedSlider _sldrDelay = new SpeedSlider();

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region IFrameServerCaptureContainer implementation

        public void DoInvalidate()
        {
            pbSurfaceScreen.Invalidate();
        }

        public void DoInitDecodingSize()
        {
            BeginInvoke(_mInitDecodingSize);
        }

        private void InitDecodingSize_Invoked()
        {
            _mPointerTool.SetImageSize(_mFrameServer.ImageSize);
            _mFrameServer.CoordinateSystem.Stretch = 1;
            _mBStretchModeOn = false;

            PanelCenter_Resize(null, EventArgs.Empty);

            // As a matter of fact we pass here at the first received frame.
            ShowHideResizers(true);
            UpdateFilenameLabel();
        }

        public void DisplayAsGrabbing(bool bIsGrabbing)
        {
            if (bIsGrabbing)
            {
                pbSurfaceScreen.Visible = true;
                btnGrab.Image = Resources.capturepause5;
            }
            else
            {
                btnGrab.Image = Resources.capturegrab5;
            }
        }

        public void DisplayAsRecording(bool bIsRecording)
        {
            if (bIsRecording)
            {
                btnRecord.Image = Resources.control_recstop;
                toolTips.SetToolTip(btnRecord, ScreenManagerLang.ToolTip_RecordStop);
                ToastStartRecord();
            }
            else
            {
                btnRecord.Image = Resources.control_rec;
                toolTips.SetToolTip(btnRecord, ScreenManagerLang.ToolTip_RecordStart);
                ToastStopRecord();
            }
        }

        public void AlertDisconnected()
        {
            ToastDisconnect();
            pbSurfaceScreen.Invalidate();
        }

        public void DoUpdateCapturedVideos()
        {
            // Update the list of Captured Videos.
            // Similar to OrganizeKeyframe in PlayerScreen.

            pnlThumbnails.Controls.Clear();

            if (_mFrameServer.RecentlyCapturedVideos.Count > 0)
            {
                var iPixelsOffset = 0;
                var iPixelsSpacing = 20;

                for (var i = _mFrameServer.RecentlyCapturedVideos.Count - 1; i >= 0; i--)
                {
                    var box = new CapturedVideoBox(_mFrameServer.RecentlyCapturedVideos[i]);
                    SetupDefaultThumbBox(box);

                    // Finish the setup
                    box.Left = iPixelsOffset + iPixelsSpacing;
                    box.pbThumbnail.Image = _mFrameServer.RecentlyCapturedVideos[i].Thumbnail;
                    box.CloseThumb += CapturedVideoBox_Close;
                    box.LaunchVideo += CapturedVideoBox_LaunchVideo;

                    iPixelsOffset += (iPixelsSpacing + box.Width);
                    pnlThumbnails.Controls.Add(box);
                }

                DockKeyframePanel(false);
                pnlThumbnails.Refresh();
            }
            else
            {
                DockKeyframePanel(true);
            }
        }

        public void DoUpdateStatusBar()
        {
            _mScreenUiHandler.ScreenUI_UpdateStatusBarAsked();
        }

        #endregion IFrameServerCaptureContainer implementation

        #region Public Methods

        public void DisplayAsActiveScreen(bool bActive)
        {
            // Called from ScreenManager.
            ShowBorder(bActive);
        }

        public void RefreshUiCulture()
        {
            // Labels
            lblImageFile.Text = ScreenManagerLang.Capture_NextImage;
            lblVideoFile.Text = ScreenManagerLang.Capture_NextVideo;
            var maxRight = Math.Max(lblImageFile.Right, lblVideoFile.Right);
            tbImageFilename.Left = maxRight + 5;
            tbVideoFilename.Left = maxRight + 5;
            UpdateDelayLabel();

            ReloadTooltipsCulture();
            ReloadMenusCulture();
            ReloadCapturedVideosCulture();

            // Update the file naming.
            // By doing this we fix the naming for prefs change in free text (FT), in pattern, switch from FT to pattern,
            // switch from pattern to FT, no change in pattern.
            // but we loose any changes that have been done between the last saving and now. (no pref change in FT)
            InitializeCaptureFiles();

            // Refresh image to update grids colors, etc.
            pbSurfaceScreen.Invalidate();

            _mFrameServer.UpdateMemoryCapacity();
        }

        public bool OnKeyPress(Keys keycode)
        {
            var bWasHandled = false;

            if (tbImageFilename.Focused || tbVideoFilename.Focused)
            {
                return false;
            }

            // Method called from the Screen Manager's PreFilterMessage.
            switch (keycode)
            {
                case Keys.Space:
                case Keys.Return:
                    {
                        if ((ModifierKeys & Keys.Control) == Keys.Control)
                        {
                            btnRecord_Click(null, EventArgs.Empty);
                        }
                        else
                        {
                            OnButtonGrab();
                        }
                        bWasHandled = true;
                        break;
                    }
                case Keys.Escape:
                    {
                        if (_mFrameServer.IsRecording)
                        {
                            btnRecord_Click(null, EventArgs.Empty);
                        }
                        DisablePlayAndDraw();
                        pbSurfaceScreen.Invalidate();
                        bWasHandled = true;
                        break;
                    }
                case Keys.Left:
                case Keys.Right:
                    {
                        sldrDelay_KeyDown(null, new KeyEventArgs(keycode));
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
                case Keys.Delete:
                    {
                        // Remove selected Drawing
                        // Note: Should only work if the Drawing is currently being moved...
                        DeleteSelectedDrawing();
                        bWasHandled = true;
                        break;
                    }
                default:
                    break;
            }

            return bWasHandled;
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
            if (_mFrameServer.IsConnected && File.Exists(filename))
            {
                _mFrameServer.Metadata.AllDrawingTextToNormalMode();

                try
                {
                    if (bIsSvg)
                    {
                        var dsvg = new DrawingSvg(_mFrameServer.ImageSize.Width,
                            _mFrameServer.ImageSize.Height,
                            0,
                            _mFrameServer.Metadata.AverageTimeStampsPerFrame,
                            filename);

                        _mFrameServer.Metadata[0].AddDrawing(dsvg);
                    }
                    else
                    {
                        var dbmp = new DrawingBitmap(_mFrameServer.ImageSize.Width,
                            _mFrameServer.ImageSize.Height,
                            0,
                            _mFrameServer.Metadata.AverageTimeStampsPerFrame,
                            filename);

                        _mFrameServer.Metadata[0].AddDrawing(dbmp);
                    }

                    _mFrameServer.Metadata.SelectedDrawingFrame = 0;
                    _mFrameServer.Metadata.SelectedDrawing = 0;
                }
                catch
                {
                    // An error occurred during the creation.
                    // example : external DTD an no network or invalid svg file.
                    Log.Error("An error occurred during the creation of an SVG drawing.");
                }

                pbSurfaceScreen.Invalidate();
            }
        }

        public void AddImageDrawing(Bitmap bmp)
        {
            // Add an image drawing from a bitmap.
            // Mimick all the actions that are normally taken when we select a drawing tool and click on the image.
            if (_mFrameServer.IsConnected)
            {
                var dbmp = new DrawingBitmap(_mFrameServer.ImageSize.Width,
                    _mFrameServer.ImageSize.Height,
                    0,
                    _mFrameServer.Metadata.AverageTimeStampsPerFrame,
                    bmp);

                _mFrameServer.Metadata[0].AddDrawing(dbmp);

                _mFrameServer.Metadata.SelectedDrawingFrame = 0;
                _mFrameServer.Metadata.SelectedDrawing = 0;
            }
        }

        public void BeforeClose()
        {
            // This screen is about to be closed.
            tmrCaptureDeviceDetector.Stop();
            tmrCaptureDeviceDetector.Dispose();
            _mPrefManager.Export();
        }

        #endregion Public Methods

        #region Various Inits & Setups

        private void CaptureScreenUserInterface_Load(object sender, EventArgs e)
        {
            _mScreenUiHandler.ScreenUI_SetAsActiveScreen();
        }

        private void AddExtraControls()
        {
            // Add additional controls to the screen. This is needed due to some issue in SharpDevelop with custom controls.
            //(This method is hopefully temporary).
            panelVideoControls.Controls.Add(_sldrDelay);

            //sldrDelay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            _sldrDelay.BackColor = Color.White;
            //sldrDelay.Enabled = false;
            _sldrDelay.LargeChange = 1;
            _sldrDelay.Location = new Point(lblDelay.Left + lblDelay.Width + 10, lblDelay.Top + 2);
            _sldrDelay.Maximum = 100;
            _sldrDelay.Minimum = 0;
            _sldrDelay.MinimumSize = new Size(20, 10);
            _sldrDelay.Name = "sldrSpeed";
            _sldrDelay.Size = new Size(150, 10);
            _sldrDelay.SmallChange = 1;
            _sldrDelay.StickyValue = -100;
            _sldrDelay.StickyMark = true;
            _sldrDelay.Value = 0;
            _sldrDelay.ValueChanged += sldrDelay_ValueChanged;
            _sldrDelay.KeyDown += sldrDelay_KeyDown;
        }

        private void InitializeDrawingTools()
        {
            _mPointerTool = new DrawingToolPointer();
            _mActiveTool = _mPointerTool;

            // Tools buttons.
            EventHandler handler = drawingTool_Click;

            AddToolButton(_mPointerTool, handler);
            stripDrawingTools.Items.Add(new ToolStripSeparator());
            AddToolButton(ToolManager.Label, handler);
            AddToolButton(ToolManager.Pencil, handler);
            AddToolButton(ToolManager.Line, handler);
            AddToolButton(ToolManager.Circle, handler);
            AddToolButton(ToolManager.CrossMark, handler);
            AddToolButton(ToolManager.Angle, handler);
            AddToolButton(ToolManager.Plane, handler);
            AddToolButton(ToolManager.Magnifier, btnMagnifier_Click);

            // Tool presets
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

        private void AddToolButton(DrawingToolPointer tool, EventHandler handler)
        {
            var btn = CreateToolButton();
            btn.Image = tool.Icon;
            btn.Tag = tool;
            btn.Click += handler;
            btn.ToolTipText = tool.DisplayName;
            stripDrawingTools.Items.Add(btn);
        }

        private void InitializeMetadata()
        {
            // In capture, there is always a single keyframe.
            // All drawings are considered motion guides.
            var kf = new Keyframe(_mFrameServer.Metadata);
            kf.Position = 0;
            _mFrameServer.Metadata.Add(kf);

            // Check if there is a startup kva.
            // For capture, the kva will only work if the drawings are on a frame at position 0.
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Kinovea\\";
            var startupFile = folder + "\\capture.kva";
            if (File.Exists(startupFile))
            {
                _mFrameServer.Metadata.Load(startupFile, true);
            }

            // Strip extra keyframes, as there can only be one for capture.
            if (_mFrameServer.Metadata.Count > 1)
            {
                _mFrameServer.Metadata.Keyframes.RemoveRange(1, _mFrameServer.Metadata.Keyframes.Count - 1);
            }
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
            _mnuCamSettings.Click += btnCamSettings_Click;
            _mnuCamSettings.Image = Resources.camera_video;
            _mnuSavePic.Click += btnSnapShot_Click;
            _mnuSavePic.Image = Resources.picture_save;
            _mnuCloseScreen.Click += btnClose_Click;
            _mnuCloseScreen.Image = Resources.capture_close2;
            _popMenu.Items.AddRange(new ToolStripItem[] { _mnuCamSettings, _mnuSavePic, new ToolStripSeparator(), _mnuCloseScreen });

            // 2. Drawings context menu (Configure, Delete, Track this)
            _mnuConfigureDrawing.Click += mnuConfigureDrawing_Click;
            _mnuConfigureDrawing.Image = Properties.Drawings.configure;
            _mnuConfigureOpacity.Click += mnuConfigureOpacity_Click;
            _mnuConfigureOpacity.Image = Properties.Drawings.persistence;
            _mnuDeleteDrawing.Click += mnuDeleteDrawing_Click;
            _mnuDeleteDrawing.Image = Properties.Drawings.delete;

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

        private void InitializeCaptureFiles()
        {
            // Get the last values used and move forward (or use default if first time).
            _mLastSavedImage = _mFilenameHelper.InitImage();
            _mLastSavedVideo = _mFilenameHelper.InitVideo();
            tbImageFilename.Text = _mLastSavedImage;
            tbVideoFilename.Text = _mLastSavedVideo;
            tbImageFilename.Enabled = !_mPrefManager.CaptureUsePattern;
            tbVideoFilename.Enabled = !_mPrefManager.CaptureUsePattern;
        }

        private void UpdateFilenameLabel()
        {
            lblFileName.Text = _mFrameServer.DeviceName;
        }

        #endregion Various Inits & Setups

        #region Misc Events

        private void btnClose_Click(object sender, EventArgs e)
        {
            // Propagate to PlayerScreen which will report to ScreenManager.
            Cursor = Cursors.WaitCursor;
            _mScreenUiHandler.ScreenUI_CloseAsked();
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
            pbSurfaceScreen.Invalidate();
        }

        #endregion Misc Events

        #region Misc private helpers

        private void OnPoke()
        {
            //------------------------------------------------------------------------------
            // This function is a hub event handler for all button press, mouse clicks, etc.
            // Signal itself as the active screen to the ScreenManager
            //---------------------------------------------------------------------

            _mScreenUiHandler.ScreenUI_SetAsActiveScreen();

            _mFrameServer.Metadata.AllDrawingTextToNormalMode();
            _mActiveTool = _mActiveTool.KeepToolFrameChanged ? _mActiveTool : _mPointerTool;
            if (_mActiveTool == _mPointerTool)
            {
                SetCursor(_mPointerTool.GetCursor(-1));
            }

            if (_mFrameServer.RecentlyCapturedVideos.Count < 1)
            {
                DockKeyframePanel(true);
            }
        }

        private void DoDrawingUndrawn()
        {
            //--------------------------------------------------------
            // this function is called after we undo a drawing action.
            // Called from CommandAddDrawing.Unexecute() through a delegate.
            //--------------------------------------------------------
            _mActiveTool = _mActiveTool.KeepToolFrameChanged ? _mActiveTool : _mPointerTool;
            if (_mActiveTool == _mPointerTool)
            {
                SetCursor(_mPointerTool.GetCursor(-1));
            }
        }

        private void ShowBorder(bool bShow)
        {
            _mBShowImageBorder = bShow;
            pbSurfaceScreen.Invalidate();
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
            _mActiveTool = _mPointerTool;
            SetCursor(_mPointerTool.GetCursor(0));
            DisableMagnifier();
            UnzoomDirectZoom();
        }

        #endregion Misc private helpers

        #region Video Controls

        private void btnGrab_Click(object sender, EventArgs e)
        {
            if (_mFrameServer.IsConnected)
            {
                OnPoke();
                OnButtonGrab();
            }
            else
            {
                _mFrameServer.PauseGrabbing();
            }
        }

        private void OnButtonGrab()
        {
            if (_mFrameServer.IsConnected)
            {
                if (_mFrameServer.IsGrabbing)
                {
                    _mFrameServer.PauseGrabbing();
                    ToastPause();
                }
                else
                {
                    _mFrameServer.StartGrabbing();
                }
            }
        }

        public void Common_MouseWheel(object sender, MouseEventArgs e)
        {
            // MouseWheel was recorded on one of the controls.
            if (_mFrameServer.IsConnected)
            {
                var iScrollOffset = e.Delta * SystemInformation.MouseWheelScrollLines / 120;

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
            }
        }

        private void sldrDelay_ValueChanged(object sender, EventArgs e)
        {
            // sldrDelay value always goes [0..100].
            _mIDelay = _mFrameServer.DelayChanged(_sldrDelay.Value);
            if (!_mFrameServer.IsGrabbing)
            {
                pbSurfaceScreen.Invalidate();
            }
            UpdateDelayLabel();
        }

        private void sldrDelay_KeyDown(object sender, KeyEventArgs e)
        {
            // Increase/Decrease delay on LEFT/RIGHT Arrows.
            if (_mFrameServer.IsConnected)
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

                if (e.KeyCode == Keys.Left)
                {
                    _sldrDelay.Value = jumpFactor * ((_sldrDelay.Value - 1) / jumpFactor);
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Right)
                {
                    _sldrDelay.Value = jumpFactor * ((_sldrDelay.Value / jumpFactor) + 1);
                    e.Handled = true;
                }

                _mIDelay = _mFrameServer.DelayChanged(_sldrDelay.Value);
                if (!_mFrameServer.IsGrabbing)
                {
                    pbSurfaceScreen.Invalidate();
                }
                UpdateDelayLabel();
            }
        }

        private void UpdateDelayLabel()
        {
            lblDelay.Text = string.Format(ScreenManagerLang.lblDelay_Text, _mIDelay);
        }

        #endregion Video Controls

        #region Auto Stretch & Manual Resize

        private void StretchSqueezeSurface()
        {
            if (_mFrameServer.IsConnected)
            {
                // Check if the image was loaded squeezed.
                // (happen when screen control isn't being fully expanded at video load time.)
                if (pbSurfaceScreen.Height < panelCenter.Height && _mFrameServer.CoordinateSystem.Stretch < 1.0)
                {
                    _mFrameServer.CoordinateSystem.Stretch = 1.0;
                }

                var imgSize = _mFrameServer.ImageSize;

                //---------------------------------------------------------------
                // Check if the stretch factor is not going to outsize the panel.
                // If so, force maximized, unless screen is smaller than video.
                //---------------------------------------------------------------
                var iTargetHeight = (int)(imgSize.Height * _mFrameServer.CoordinateSystem.Stretch);
                var iTargetWidth = (int)(imgSize.Width * _mFrameServer.CoordinateSystem.Stretch);

                if (iTargetHeight > panelCenter.Height || iTargetWidth > panelCenter.Width)
                {
                    if (_mFrameServer.CoordinateSystem.Stretch > 1.0)
                    {
                        _mBStretchModeOn = true;
                    }
                }

                if ((_mBStretchModeOn) || (imgSize.Width > panelCenter.Width) || (imgSize.Height > panelCenter.Height))
                {
                    //-------------------------------------------------------------------------------
                    // Maximiser :
                    // Redimensionner l'image selon la dimension la plus proche de la taille du panel.
                    //-------------------------------------------------------------------------------
                    var widthRatio = (float)imgSize.Width / panelCenter.Width;
                    var heightRatio = (float)imgSize.Height / panelCenter.Height;

                    if (widthRatio > heightRatio)
                    {
                        pbSurfaceScreen.Width = panelCenter.Width;
                        pbSurfaceScreen.Height = (int)(imgSize.Height / widthRatio);

                        _mFrameServer.CoordinateSystem.Stretch = (1 / widthRatio);
                    }
                    else
                    {
                        pbSurfaceScreen.Width = (int)(imgSize.Width / heightRatio);
                        pbSurfaceScreen.Height = panelCenter.Height;

                        _mFrameServer.CoordinateSystem.Stretch = (1 / heightRatio);
                    }
                }
                else
                {
                    pbSurfaceScreen.Width = (int)(imgSize.Width * _mFrameServer.CoordinateSystem.Stretch);
                    pbSurfaceScreen.Height = (int)(imgSize.Height * _mFrameServer.CoordinateSystem.Stretch);
                }

                // Center
                pbSurfaceScreen.Left = (panelCenter.Width / 2) - (pbSurfaceScreen.Width / 2);
                pbSurfaceScreen.Top = (panelCenter.Height / 2) - (pbSurfaceScreen.Height / 2);
                ReplaceResizers();
            }
        }

        private void ReplaceResizers()
        {
            ImageResizerSE.Left = pbSurfaceScreen.Left + pbSurfaceScreen.Width - (ImageResizerSE.Width / 2);
            ImageResizerSE.Top = pbSurfaceScreen.Top + pbSurfaceScreen.Height - (ImageResizerSE.Height / 2);

            ImageResizerSW.Left = pbSurfaceScreen.Left - (ImageResizerSW.Width / 2);
            ImageResizerSW.Top = pbSurfaceScreen.Top + pbSurfaceScreen.Height - (ImageResizerSW.Height / 2);

            ImageResizerNE.Left = pbSurfaceScreen.Left + pbSurfaceScreen.Width - (ImageResizerNE.Width / 2);
            ImageResizerNE.Top = pbSurfaceScreen.Top - (ImageResizerNE.Height / 2);

            ImageResizerNW.Left = pbSurfaceScreen.Left - (ImageResizerNW.Width / 2);
            ImageResizerNW.Top = pbSurfaceScreen.Top - (ImageResizerNW.Height / 2);
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
            pbSurfaceScreen.Invalidate();
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
            if (iTargetHeight > _mFrameServer.ImageSize.Height &&
                iTargetHeight < panelCenter.Height &&
                iTargetWidth > _mFrameServer.ImageSize.Width &&
                iTargetWidth < panelCenter.Width)
            {
                var fHeightFactor = ((iTargetHeight) / (double)_mFrameServer.ImageSize.Height);
                var fWidthFactor = ((iTargetWidth) / (double)_mFrameServer.ImageSize.Width);

                _mFrameServer.CoordinateSystem.Stretch = (fWidthFactor + fHeightFactor) / 2;
                _mBStretchModeOn = false;
                StretchSqueezeSurface();
                pbSurfaceScreen.Invalidate();
            }
        }

        private void Resizers_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ToggleStretchMode();
        }

        private void Resizers_MouseUp(object sender, MouseEventArgs e)
        {
            _mFrameServer.Metadata.ResizeFinished();
            pbSurfaceScreen.Invalidate();
        }

        #endregion Auto Stretch & Manual Resize

        #region Culture

        private void ReloadMenusCulture()
        {
            // Reload the text for each menu.
            // this is done at construction time and at RefreshUICulture time.

            // 1. Default context menu.
            _mnuCamSettings.Text = ScreenManagerLang.ToolTip_DevicePicker;
            _mnuSavePic.Text = ScreenManagerLang.Generic_SaveImage;
            _mnuCloseScreen.Text = ScreenManagerLang.mnuCloseScreen;

            // 2. Drawings context menu.
            _mnuConfigureDrawing.Text = ScreenManagerLang.mnuConfigureDrawing_ColorSize;
            _mnuConfigureOpacity.Text = ScreenManagerLang.Generic_Opacity;
            _mnuDeleteDrawing.Text = ScreenManagerLang.mnuDeleteDrawing;

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
            toolTips.SetToolTip(btnGrab, ScreenManagerLang.ToolTip_Play);
            toolTips.SetToolTip(btnCamSnap, ScreenManagerLang.Generic_SaveImage);
            toolTips.SetToolTip(btnCamSettings, ScreenManagerLang.ToolTip_DevicePicker);
            toolTips.SetToolTip(btnRecord,
                _mFrameServer.IsRecording ? ScreenManagerLang.ToolTip_RecordStop : ScreenManagerLang.ToolTip_RecordStart);

            // Drawing tools
            foreach (ToolStripItem tsi in stripDrawingTools.Items)
            {
                if (tsi is ToolStripButton)
                {
                    var tool = tsi.Tag as AbstractScreen;
                    if (tool != null)
                    {
                        tsi.ToolTipText = tool.DisplayName;
                    }
                }
            }

            _mBtnToolPresets.ToolTipText = ScreenManagerLang.ToolTip_ColorProfile;
        }

        private void ReloadCapturedVideosCulture()
        {
            foreach (Control c in pnlThumbnails.Controls)
            {
                var cvb = c as CapturedVideoBox;
                if (cvb != null)
                {
                    cvb.RefreshUiCulture();
                }
            }
        }

        #endregion Culture

        #region SurfaceScreen Events

        private void SurfaceScreen_MouseDown(object sender, MouseEventArgs e)
        {
            if (_mFrameServer.IsConnected)
            {
                _mDeselectionTimer.Stop();

                if (e.Button == MouseButtons.Left)
                {
                    if (_mFrameServer.IsConnected)
                    {
                        if ((_mActiveTool == _mPointerTool) &&
                            (_mFrameServer.Magnifier.Mode != MagnifierMode.NotVisible) &&
                            (_mFrameServer.Magnifier.IsOnObject(e)))
                        {
                            _mFrameServer.Magnifier.OnMouseDown(e);
                        }
                        else
                        {
                            //-------------------------------------
                            // Action begins:
                            // Move or set magnifier
                            // Move or set Drawing
                            //-------------------------------------

                            var descaledMouse = _mFrameServer.CoordinateSystem.Untransform(e.Location);

                            // 1. Pass all DrawingText to normal mode
                            _mFrameServer.Metadata.AllDrawingTextToNormalMode();

                            if (_mActiveTool == _mPointerTool)
                            {
                                // 1. Manipulating an object or Magnifier
                                var bMovingMagnifier = false;
                                var bDrawingHit = false;

                                // Show the grabbing hand cursor.
                                SetCursor(_mPointerTool.GetCursor(1));

                                if (_mFrameServer.Magnifier.Mode == MagnifierMode.Indirect)
                                {
                                    bMovingMagnifier = _mFrameServer.Magnifier.OnMouseDown(e);
                                }

                                if (!bMovingMagnifier)
                                {
                                    // Magnifier wasn't hit or is not in use,
                                    // try drawings (including chronos and other extra drawings)
                                    bDrawingHit = _mPointerTool.OnMouseDown(_mFrameServer.Metadata, 0, descaledMouse, 0,
                                        _mPrefManager.DefaultFading.Enabled);
                                }
                            }
                            else
                            {
                                //-----------------------
                                // Creating a new Drawing
                                //-----------------------
                                if (_mActiveTool != ToolManager.Label)
                                {
                                    // Add an instance of a drawing from the active tool to the current keyframe.
                                    // The drawing is initialized with the current mouse coordinates.
                                    AbstractDrawing ad = _mActiveTool.GetNewDrawing(descaledMouse, 0, 1);

                                    _mFrameServer.Metadata[0].AddDrawing(ad);
                                    _mFrameServer.Metadata.SelectedDrawingFrame = 0;
                                    _mFrameServer.Metadata.SelectedDrawing = 0;

                                    // Post creation hacks.
                                    if (ad is DrawingLine2D)
                                    {
                                        ((DrawingLine2D)ad).ParentMetadata = _mFrameServer.Metadata;
                                        ((DrawingLine2D)ad).ShowMeasure = DrawingToolLine2D.ShowMeasure;
                                    }
                                    else if (ad is DrawingCross2D)
                                    {
                                        ((DrawingCross2D)ad).ParentMetadata = _mFrameServer.Metadata;
                                        ((DrawingCross2D)ad).ShowCoordinates = DrawingToolCross2D.ShowCoordinates;
                                    }
                                    else if (ad is DrawingPlane)
                                    {
                                        ((DrawingPlane)ad).SetLocations(_mFrameServer.ImageSize, 1.0, new Point(0, 0));
                                    }
                                }
                                else
                                {
                                    // We are using the Text Tool. This is a special case because
                                    // if we are on an existing Textbox, we just go into edit mode
                                    // otherwise, we add and setup a new textbox.
                                    var bEdit = false;
                                    foreach (AbstractDrawing ad in _mFrameServer.Metadata[0].Drawings)
                                    {
                                        if (ad is DrawingText)
                                        {
                                            int hitRes = ad.HitTest(descaledMouse, 0);
                                            if (hitRes >= 0)
                                            {
                                                bEdit = true;
                                                ((DrawingText)ad).SetEditMode(true, _mFrameServer.CoordinateSystem);
                                            }
                                        }
                                    }

                                    // If we are not on an existing textbox : create new DrawingText.
                                    if (!bEdit)
                                    {
                                        _mFrameServer.Metadata[0].AddDrawing(_mActiveTool.GetNewDrawing(descaledMouse, 0,
                                            1));
                                        _mFrameServer.Metadata.SelectedDrawingFrame = 0;
                                        _mFrameServer.Metadata.SelectedDrawing = 0;

                                        var dt = (DrawingText)_mFrameServer.Metadata[0].Drawings[0];

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
                }
                else if (e.Button == MouseButtons.Right)
                {
                    // Show the right Pop Menu depending on context.
                    // (Drawing, Magnifier, Nothing)

                    var descaledMouse = _mFrameServer.CoordinateSystem.Untransform(e.Location);

                    if (_mFrameServer.IsConnected)
                    {
                        _mFrameServer.Metadata.UnselectAll();

                        if (_mFrameServer.Metadata.IsOnDrawing(0, descaledMouse, 0))
                        {
                            // Rebuild the context menu according to the capabilities of the drawing we are on.

                            AbstractDrawing ad =
                                _mFrameServer.Metadata.Keyframes[_mFrameServer.Metadata.SelectedDrawingFrame].Drawings[
                                    _mFrameServer.Metadata.SelectedDrawing];
                            if (ad != null)
                            {
                                _popMenuDrawings.Items.Clear();

                                // Generic context menu from drawing capabilities.
                                if ((ad.Caps & DrawingCapabilities.ConfigureColor) == DrawingCapabilities.ConfigureColor)
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
                                        _popMenuDrawings.Items.Add(tsmi);
                                    }
                                }

                                if (hasExtraMenu)
                                    _popMenuDrawings.Items.Add(_mnuSepDrawing2);

                                // Generic delete
                                _popMenuDrawings.Items.Add(_mnuDeleteDrawing);

                                // Set this menu as the context menu.
                                panelCenter.ContextMenuStrip = _popMenuDrawings;
                            }
                        }
                        else if (_mFrameServer.Magnifier.Mode == MagnifierMode.Indirect &&
                                 _mFrameServer.Magnifier.IsOnObject(e))
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
                            // No drawing touched and no tool selected
                            panelCenter.ContextMenuStrip = _popMenu;
                        }
                    }
                }

                pbSurfaceScreen.Invalidate();
            }
        }

        private void SurfaceScreen_MouseMove(object sender, MouseEventArgs e)
        {
            // We must keep the same Z order.
            // 1:Magnifier, 2:Drawings, 3:Chronos/Tracks
            // When creating a drawing, the active tool will stay on this drawing until its setup is over.
            // After the drawing is created, we either fall back to Pointer tool or stay on the same tool.

            if (_mFrameServer.IsConnected)
            {
                if (e.Button == MouseButtons.None && _mFrameServer.Magnifier.Mode == MagnifierMode.Direct)
                {
                    _mFrameServer.Magnifier.MouseX = e.X;
                    _mFrameServer.Magnifier.MouseY = e.Y;
                    pbSurfaceScreen.Invalidate();
                }
                else if (e.Button == MouseButtons.Left)
                {
                    if (_mActiveTool != _mPointerTool)
                    {
                        // Currently setting the second point of a Drawing.
                        var initializableDrawing = _mFrameServer.Metadata[0].Drawings[0] as IInitializable;
                        if (initializableDrawing != null)
                        {
                            initializableDrawing.ContinueSetup(
                                _mFrameServer.CoordinateSystem.Untransform(new Point(e.X, e.Y)));
                        }
                    }
                    else
                    {
                        var bMovingMagnifier = false;
                        if (_mFrameServer.Magnifier.Mode == MagnifierMode.Indirect)
                        {
                            bMovingMagnifier = _mFrameServer.Magnifier.OnMouseMove(e);
                        }

                        if (!bMovingMagnifier && _mActiveTool == _mPointerTool)
                        {
                            // Moving an object.

                            var descaledMouse = _mFrameServer.CoordinateSystem.Untransform(e.Location);

                            // Magnifier is not being moved or is invisible, try drawings through pointer tool.
                            var bMovingObject = _mPointerTool.OnMouseMove(_mFrameServer.Metadata, descaledMouse,
                                _mFrameServer.CoordinateSystem.Location, ModifierKeys);

                            if (!bMovingObject && _mFrameServer.CoordinateSystem.Zooming)
                            {
                                // User is not moving anything and we are zooming : move the zoom window.

                                // Get mouse deltas (descaled=in image coords).
                                double fDeltaX = _mPointerTool.MouseDelta.X;
                                double fDeltaY = _mPointerTool.MouseDelta.Y;

                                _mFrameServer.CoordinateSystem.MoveZoomWindow(fDeltaX, fDeltaY);
                            }
                        }
                    }
                }

                if (!_mFrameServer.IsGrabbing)
                {
                    pbSurfaceScreen.Invalidate();
                }
            }
        }

        private void SurfaceScreen_MouseUp(object sender, MouseEventArgs e)
        {
            // End of an action.
            // Depending on the active tool we have various things to do.

            if (_mFrameServer.IsConnected && e.Button == MouseButtons.Left)
            {
                if (_mActiveTool == _mPointerTool)
                {
                    OnPoke();
                }

                _mFrameServer.Magnifier.OnMouseUp(e);

                // Memorize the action we just finished to enable undo.
                if (_mActiveTool != _mPointerTool)
                {
                    // Record the adding unless we are editing a text box.
                    if (!_mBTextEdit)
                    {
                        IUndoableCommand cad = new CommandAddDrawing(DoInvalidate, DoDrawingUndrawn,
                            _mFrameServer.Metadata, _mFrameServer.Metadata[0].Position);
                        var cm = CommandManager.Instance();
                        cm.LaunchUndoableCommand(cad);
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

                // Unselect drawings.
                //m_FrameServer.Metadata.SelectedDrawingFrame = -1;
                //m_FrameServer.Metadata.SelectedDrawing = -1;

                if (_mFrameServer.Metadata.SelectedDrawingFrame != -1 && _mFrameServer.Metadata.SelectedDrawing != -1)
                {
                    _mDeselectionTimer.Start();
                }

                pbSurfaceScreen.Invalidate();
            }
        }

        private void SurfaceScreen_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (_mFrameServer.IsConnected && e.Button == MouseButtons.Left && _mActiveTool == _mPointerTool)
            {
                OnPoke();

                var descaledMouse = _mFrameServer.CoordinateSystem.Untransform(e.Location);
                _mFrameServer.Metadata.AllDrawingTextToNormalMode();
                _mFrameServer.Metadata.UnselectAll();

                //------------------------------------------------------------------------------------
                // - If on text, switch to edit mode.
                // - If on other drawing, launch the configuration dialog.
                // - Otherwise -> Maximize/Reduce image.
                //------------------------------------------------------------------------------------
                if (_mFrameServer.Metadata.IsOnDrawing(0, descaledMouse, 0))
                {
                    AbstractDrawing ad =
                        _mFrameServer.Metadata.Keyframes[0].Drawings[_mFrameServer.Metadata.SelectedDrawing];
                    if (ad is DrawingText)
                    {
                        ((DrawingText)ad).SetEditMode(true, _mFrameServer.CoordinateSystem);
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
                else
                {
                    ToggleStretchMode();
                }
            }
        }

        private void SurfaceScreen_Paint(object sender, PaintEventArgs e)
        {
            // Draw the image.
            _mFrameServer.Draw(e.Graphics);

            if (_mMessageToaster.Enabled)
            {
                _mMessageToaster.Draw(e.Graphics);
            }

            // Draw selection Border if needed.
            if (_mBShowImageBorder)
            {
                DrawImageBorder(e.Graphics);
            }
        }

        private void SurfaceScreen_MouseEnter(object sender, EventArgs e)
        {
            // Set focus to surfacescreen to enable mouse scroll

            // But only if there is no Text edition going on.
            var bEditing = false;

            foreach (AbstractDrawing ad in _mFrameServer.Metadata[0].Drawings)
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

            if (!bEditing)
            {
                pbSurfaceScreen.Focus();
            }
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
            pbSurfaceScreen.Invalidate();
        }

        private void PanelCenter_MouseDown(object sender, MouseEventArgs e)
        {
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

        private void SetupDefaultThumbBox(UserControl box)
        {
            box.Top = 10;
            box.Cursor = Cursors.Hand;
        }

        public void OnKeyframesTitleChanged()
        {
            // Called when title changed.
            pbSurfaceScreen.Invalidate();
        }

        private void pnlThumbnails_DoubleClick(object sender, EventArgs e)
        {
            OnPoke();
        }

        private void CapturedVideoBox_LaunchVideo(object sender, EventArgs e)
        {
            var box = sender as CapturedVideoBox;
            if (box != null)
            {
                _mScreenUiHandler.CaptureScreenUI_LoadVideo(box.FilePath);
            }
        }

        private void CapturedVideoBox_Close(object sender, EventArgs e)
        {
            var box = sender as CapturedVideoBox;
            if (box != null)
            {
                for (var i = 0; i < _mFrameServer.RecentlyCapturedVideos.Count; i++)
                {
                    if (_mFrameServer.RecentlyCapturedVideos[i].Filepath == box.FilePath)
                    {
                        _mFrameServer.RecentlyCapturedVideos.RemoveAt(i);
                    }
                }

                DoUpdateCapturedVideos();
            }
        }

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
                btnDockBottom.Visible = _mFrameServer.RecentlyCapturedVideos.Count > 0;
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

        #endregion Docking Undocking

        #endregion Keyframes Panel

        #region Drawings Toolbar Events

        private void drawingTool_Click(object sender, EventArgs e)
        {
            // User clicked on a drawing tool button. A reference to the tool is stored in .Tag
            // Set this tool as the active tool (waiting for the actual use) and set the cursor accordingly.

            // Deactivate magnifier if not commited.
            if (_mFrameServer.Magnifier.Mode == MagnifierMode.Direct)
            {
                DisableMagnifier();
            }

            OnPoke();

            var tool = ((ToolStripItem)sender).Tag as AbstractScreen;
            if (tool != null)
            {
                _mActiveTool = tool;
            }
            else
            {
                _mActiveTool = _mPointerTool;
            }

            UpdateCursor();
            pbSurfaceScreen.Invalidate();
        }

        private void btnMagnifier_Click(object sender, EventArgs e)
        {
            if (_mFrameServer.IsConnected)
            {
                _mActiveTool = _mPointerTool;

                if (_mFrameServer.Magnifier.Mode == MagnifierMode.NotVisible)
                {
                    UnzoomDirectZoom();
                    _mFrameServer.Magnifier.Mode = MagnifierMode.Direct;
                    //btnMagnifier.BackgroundImage = Resources.magnifierActive2;
                    SetCursor(Cursors.Cross);
                }
                else if (_mFrameServer.Magnifier.Mode == MagnifierMode.Direct)
                {
                    // Revert to no magnification.
                    UnzoomDirectZoom();
                    _mFrameServer.Magnifier.Mode = MagnifierMode.NotVisible;
                    //btnMagnifier.BackgroundImage = Resources.magnifier2;
                    SetCursor(_mPointerTool.GetCursor(0));
                    pbSurfaceScreen.Invalidate();
                }
                else
                {
                    DisableMagnifier();
                    pbSurfaceScreen.Invalidate();
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

        #region Drawings Menus

        private void mnuConfigureDrawing_Click(object sender, EventArgs e)
        {
            if (_mFrameServer.Metadata.SelectedDrawing >= 0)
            {
                var decorableDrawing =
                    _mFrameServer.Metadata[0].Drawings[_mFrameServer.Metadata.SelectedDrawing] as IDecorable;
                if (decorableDrawing != null && decorableDrawing.DrawingStyle != null &&
                    decorableDrawing.DrawingStyle.Elements.Count > 0)
                {
                    var fcd = new FormConfigureDrawing2(decorableDrawing.DrawingStyle, DoInvalidate);
                    ScreenManagerKernel.LocateForm(fcd);
                    fcd.ShowDialog();
                    fcd.Dispose();
                    DoInvalidate();
                    ContextMenuStrip = _popMenu;
                }
            }
        }

        private void mnuConfigureOpacity_Click(object sender, EventArgs e)
        {
            if (_mFrameServer.Metadata.SelectedDrawing >= 0)
            {
                var fco =
                    new FormConfigureOpacity(
                        _mFrameServer.Metadata[0].Drawings[_mFrameServer.Metadata.SelectedDrawing], pbSurfaceScreen);
                ScreenManagerKernel.LocateForm(fco);
                fco.ShowDialog();
                fco.Dispose();
                pbSurfaceScreen.Invalidate();
            }
        }

        private void mnuDeleteDrawing_Click(object sender, EventArgs e)
        {
            DeleteSelectedDrawing();
            ContextMenuStrip = _popMenu;
        }

        private void DeleteSelectedDrawing()
        {
            if (_mFrameServer.Metadata.SelectedDrawing >= 0)
            {
                IUndoableCommand cdd = new CommandDeleteDrawing(DoInvalidate, _mFrameServer.Metadata,
                    _mFrameServer.Metadata[0].Position, _mFrameServer.Metadata.SelectedDrawing);
                var cm = CommandManager.Instance();
                cm.LaunchUndoableCommand(cdd);
                pbSurfaceScreen.Invalidate();
                ContextMenuStrip = _popMenu;
            }
        }

        #endregion Drawings Menus

        #region Magnifier Menus

        private void mnuMagnifierQuit_Click(object sender, EventArgs e)
        {
            DisableMagnifier();
            pbSurfaceScreen.Invalidate();
        }

        private void mnuMagnifierDirect_Click(object sender, EventArgs e)
        {
            // Use position and magnification to Direct Zoom.
            // Go to direct zoom, at magnifier zoom factor, centered on same point as magnifier.
            _mFrameServer.CoordinateSystem.Zoom = _mFrameServer.Magnifier.ZoomFactor;
            _mFrameServer.CoordinateSystem.RelocateZoomWindow(_mFrameServer.Magnifier.MagnifiedCenter);
            DisableMagnifier();
            _mFrameServer.Metadata.ResizeFinished();
            pbSurfaceScreen.Invalidate();
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
            _mFrameServer.Magnifier.ZoomFactor = fValue;
            UncheckMagnifierMenus();
            menu.Checked = true;
            pbSurfaceScreen.Invalidate();
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
            _mFrameServer.Magnifier.Mode = MagnifierMode.NotVisible;
            //btnMagnifier.BackgroundImage = Drawings.magnifier;
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
            if (_mFrameServer.Magnifier.Mode != MagnifierMode.NotVisible)
            {
                DisableMagnifier();
            }

            // Max zoom : 600%
            if (_mFrameServer.CoordinateSystem.Zoom < 6.0f)
            {
                _mFrameServer.CoordinateSystem.Zoom += 0.20f;
                RelocateDirectZoom();
                _mFrameServer.Metadata.ResizeFinished();
                ToastZoom();
            }

            pbSurfaceScreen.Invalidate();
        }

        private void DecreaseDirectZoom()
        {
            if (_mFrameServer.CoordinateSystem.Zoom > 1.2f)
            {
                _mFrameServer.CoordinateSystem.Zoom -= 0.20f;
            }
            else
            {
                _mFrameServer.CoordinateSystem.Zoom = 1.0f;
            }

            RelocateDirectZoom();
            _mFrameServer.Metadata.ResizeFinished();
            ToastZoom();
            pbSurfaceScreen.Invalidate();
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
            var percentage = (int)(_mFrameServer.CoordinateSystem.Zoom * 100);
            _mMessageToaster.Show(string.Format(ScreenManagerLang.Toast_Zoom, percentage));
        }

        private void ToastPause()
        {
            _mMessageToaster.SetDuration(750);
            _mMessageToaster.Show(ScreenManagerLang.Toast_Pause);
        }

        private void ToastDisconnect()
        {
            _mMessageToaster.SetDuration(1500);
            _mMessageToaster.Show(ScreenManagerLang.Toast_Disconnected);
        }

        private void ToastStartRecord()
        {
            _mMessageToaster.SetDuration(1000);
            _mMessageToaster.Show(ScreenManagerLang.Toast_StartRecord);
        }

        private void ToastStopRecord()
        {
            _mMessageToaster.SetDuration(750);
            _mMessageToaster.Show(ScreenManagerLang.Toast_StopRecord);
        }

        private void ToastImageSaved()
        {
            _mMessageToaster.SetDuration(750);
            _mMessageToaster.Show(ScreenManagerLang.Toast_ImageSaved);
        }

        #endregion Toasts

        #region Export video and frames

        private void tbImageFilename_TextChanged(object sender, EventArgs e)
        {
            if (!_mFilenameHelper.ValidateFilename(tbImageFilename.Text, true))
            {
                ScreenManagerKernel.AlertInvalidFileName();
            }
        }

        private void tbVideoFilename_TextChanged(object sender, EventArgs e)
        {
            if (!_mFilenameHelper.ValidateFilename(tbVideoFilename.Text, true))
            {
                ScreenManagerKernel.AlertInvalidFileName();
            }
        }

        private void btnSnapShot_Click(object sender, EventArgs e)
        {
            // Export the current frame.
            if (_mFrameServer.IsConnected)
            {
                if (!_mFilenameHelper.ValidateFilename(tbImageFilename.Text, false))
                {
                    ScreenManagerKernel.AlertInvalidFileName();
                }
                else if (Directory.Exists(_mPrefManager.CaptureImageDirectory))
                {
                    // In the meantime the other screen could have make a snapshot too,
                    // which would have updated the last saved file name in the global prefs.
                    // However we keep using the name of the last file saved in this specific screen to keep them independant.
                    // for ex. the user might be saving to "Front - 4" on the left, and to "Side - 7" on the right.
                    // This doesn't apply if we are using a pattern though.
                    var filename = _mPrefManager.CaptureUsePattern ? _mFilenameHelper.InitImage() : tbImageFilename.Text;
                    var filepath = _mPrefManager.CaptureImageDirectory + "\\" + filename +
                                   _mPrefManager.GetImageFormat();

                    // Check if file already exists.
                    if (OverwriteOrCreateFile(filepath))
                    {
                        var outputImage = _mFrameServer.GetFlushedImage();

                        ImageHelper.Save(filepath, outputImage);
                        outputImage.Dispose();

                        if (_mPrefManager.CaptureUsePattern)
                        {
                            _mFilenameHelper.AutoIncrement(true);
                            _mScreenUiHandler.CaptureScreenUI_FileSaved();
                        }

                        // Keep track of the last successful save.
                        // Each screen must keep its own independant history.
                        _mLastSavedImage = filename;
                        _mPrefManager.CaptureImageFile = filename;
                        _mPrefManager.Export();

                        // Update the filename for the next snapshot.
                        tbImageFilename.Text = _mPrefManager.CaptureUsePattern
                            ? _mFilenameHelper.InitImage()
                            : _mFilenameHelper.Next(_mLastSavedImage);

                        ToastImageSaved();
                    }
                }
            }
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            if (_mFrameServer.IsConnected)
            {
                if (_mFrameServer.IsRecording)
                {
                    _mFrameServer.StopRecording();
                    btnCamSettings.Enabled = true;
                    EnableVideoFileEdit(true);

                    // Keep track of the last successful save.
                    _mPrefManager.CaptureVideoFile = _mLastSavedVideo;
                    _mPrefManager.Export();

                    // update file name.
                    tbVideoFilename.Text = _mPrefManager.CaptureUsePattern
                        ? _mFilenameHelper.InitVideo()
                        : _mFilenameHelper.Next(_mLastSavedVideo);

                    DisplayAsRecording(false);
                }
                else
                {
                    // Start exporting frames to a video.

                    // Check that the destination folder exists.
                    if (!_mFilenameHelper.ValidateFilename(tbVideoFilename.Text, false))
                    {
                        ScreenManagerKernel.AlertInvalidFileName();
                    }
                    else if (Directory.Exists(_mPrefManager.CaptureVideoDirectory))
                    {
                        var filename = _mPrefManager.CaptureUsePattern
                            ? _mFilenameHelper.InitVideo()
                            : tbVideoFilename.Text;
                        var filepath = _mPrefManager.CaptureVideoDirectory + "\\" + filename +
                                       _mPrefManager.GetVideoFormat();

                        // Check if file already exists.
                        if (OverwriteOrCreateFile(filepath))
                        {
                            if (_mPrefManager.CaptureUsePattern)
                            {
                                _mFilenameHelper.AutoIncrement(false);
                                _mScreenUiHandler.CaptureScreenUI_FileSaved();
                            }

                            btnCamSettings.Enabled = false;
                            _mLastSavedVideo = filename;
                            _mFrameServer.CurrentCaptureFilePath = filepath;
                            var bRecordingStarted = _mFrameServer.StartRecording(filepath);
                            if (bRecordingStarted)
                            {
                                // Record will force grabbing if needed.
                                btnGrab.Image = Resources.capturepause5;
                                EnableVideoFileEdit(false);
                                DisplayAsRecording(true);
                            }
                        }
                    }
                }

                OnPoke();
            }
        }

        private bool OverwriteOrCreateFile(string filepath)
        {
            // Check if the specified video file exists, and asks the user if he wants to overwrite.
            var bOverwriteOrCreate = true;
            if (File.Exists(filepath))
            {
                var msgTitle = ScreenManagerLang.Error_Capture_FileExists_Title;
                var msgText = string.Format(ScreenManagerLang.Error_Capture_FileExists_Text, filepath)
                    .Replace("\\n", "\n");

                var dr = MessageBox.Show(msgText, msgTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (dr != DialogResult.Yes)
                {
                    bOverwriteOrCreate = false;
                }
            }

            return bOverwriteOrCreate;
        }

        private void EnableVideoFileEdit(bool bEnable)
        {
            lblVideoFile.Enabled = bEnable;
            tbVideoFilename.Enabled = bEnable && !_mPrefManager.CaptureUsePattern;
            btnSaveVideoLocation.Enabled = bEnable;
        }

        private void TextBoxes_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                tb.SelectAll();
            }
        }

        #endregion Export video and frames

        #region Device management

        private void btnCamSettings_Click(object sender, EventArgs e)
        {
            if (!_mFrameServer.IsRecording)
            {
                _mFrameServer.PromptDeviceSelector();
            }
        }

        private void tmrCaptureDeviceDetector_Tick(object sender, EventArgs e)
        {
            if (!_mFrameServer.IsConnected)
            {
                TryToConnect();
            }
            else
            {
                CheckDeviceConnection();
            }
        }

        private void TryToConnect()
        {
            // Try to connect to a device.
            // Prevent reentry.
            if (!_mBTryingToConnect)
            {
                _mBTryingToConnect = true;
                _mFrameServer.NegociateDevice();
                _mBTryingToConnect = false;
            }
        }

        private void CheckDeviceConnection()
        {
            // Ensure we stay connected.
            if (!_mBTryingToConnect)
            {
                _mBTryingToConnect = true;
                _mFrameServer.HeartBeat();
                _mBTryingToConnect = false;
            }
        }

        #endregion Device management
    }
}