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

using Kinovea.ScreenManager.Languages;
using Kinovea.Services;
using Kinovea.VideoFiles;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     FrameServerCapture encapsulates all the metadata and configuration for managing frames in a capture screen.
    ///     This is the object that maintains the interface with file level operations done by VideoFile class.
    /// </summary>
    public class FrameServerCapture : AbstractFrameServer, IFrameGrabberContainer
    {
        #region Constructor

        public FrameServerCapture()
        {
            _mFrameGrabber = new FrameGrabberAForge(this, _mFrameBuffer);
            _mAspectRatio = (AspectRatio) ((int) PreferencesManager.Instance().AspectRatio);

            var forceHandleCreation = _mDummyControl.Handle; // Needed to show that the main thread "owns" this Control.
            MEventFrameGrabbed = FrameGrabbed_Invoked;
        }

        #endregion Constructor

        #region Properties

        public string Status
        {
            get
            {
                if (_mFrameGrabber.IsConnected)
                {
                    var bufferFill = string.Format(ScreenManagerLang.statusBufferFill, _mFrameBuffer.FillPercentage);
                    var status = string.Format("{0} - {1} ({2})",
                        _mFrameGrabber.DeviceName,
                        _mFrameGrabber.SelectedCapability,
                        bufferFill);
                    return status;
                }
                return ScreenManagerLang.statusEmptyScreen;
            }
        }

        // Capture device.
        public bool IsConnected
        {
            get { return _mFrameGrabber.IsConnected; }
        }

        public bool IsGrabbing
        {
            get { return _mFrameGrabber.IsGrabbing; }
        }

        public Size ImageSize { get; private set; } = private new Size(720, 576);

        public bool IsRecording { get; private set; }

        public string DeviceName
        {
            get { return _mFrameGrabber.DeviceName; }
        }

        public bool Shared
        {
            set { _mBShared = value; }
        }

        // Image, Drawings and other screens overlays.
        public Metadata Metadata { get; set; }

        public Magnifier Magnifier { get; set; } = private new Magnifier();

        public CoordinateSystem CoordinateSystem { get; } = private new CoordinateSystem();

        // Saving to disk.
        public List<CapturedVideo> RecentlyCapturedVideos { get; } = new List<CapturedVideo>();

        public string CurrentCaptureFilePath { get; set; }

        public AspectRatio AspectRatio
        {
            get { return _mAspectRatio; }
            set { SetAspectRatio(value, _mFrameGrabber.FrameSize); }
        }

        #endregion Properties

        #region Members

        private IFrameServerCaptureContainer _mContainer;
            // CaptureScreenUserInterface seen through a limited interface.

        // Threading
        private readonly Control _mDummyControl = new Control();

        private readonly object _mLocker = new object();

        private event EventHandler MEventFrameGrabbed;

        // Grabbing frames
        private readonly AbstractFrameGrabber _mFrameGrabber;

        private readonly FrameBuffer _mFrameBuffer = new FrameBuffer();
        private Bitmap _mImageToDisplay;

        //private Bitmap m_PreviousImageDisplayed;

        private AspectRatio _mAspectRatio = AspectRatio.AutoDetect;
        private int _mIFrameIndex; // The "age" we pull from, in the circular buffer.
        private int _mICurrentBufferFill;

        // Image, drawings and other screens overlays.
        private bool _mBPainting; // 'true' between paint requests.

        // Saving to disk

        private VideoRecorder _mVideoRecorder;

        // Captured video thumbnails.

        // General
        private Stopwatch _mStopwatch = new Stopwatch();

        private bool _mBShared;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Implementation of IFrameGrabberContainer

        public void Connected()
        {
            Log.Debug("Screen connected.");
            StartGrabbing();
        }

        public void FrameGrabbed()
        {
            // We are still in the grabbing thread.
            // We must return as fast as possible to avoid slowing down the grabbing.
            // We use a Control object to merge back into the main thread, we'll do the work there.
            _mDummyControl.BeginInvoke(MEventFrameGrabbed);
        }

        public void SetImageSize(Size size)
        {
            // This method is still in the grabbing thread.
            // (NO UI calls, must use BeginInvoke).
            if (size != Size.Empty)
            {
                SetAspectRatio(_mAspectRatio, size);
                Log.Debug(string.Format("Image size specified. {0}", ImageSize));
            }
            else
            {
                _mImageToDisplay = null;
                ImageSize = new Size(720, 576);
                _mFrameBuffer.UpdateFrameSize(ImageSize);
            }
        }

        #endregion Implementation of IFrameGrabberContainer

        #region Public methods

        public void SetContainer(IFrameServerCaptureContainer container)
        {
            _mContainer = container;
        }

        public void PromptDeviceSelector()
        {
            _mFrameGrabber.PromptDeviceSelector();
        }

        public void NegociateDevice()
        {
            _mFrameGrabber.NegociateDevice();
        }

        public void HeartBeat()
        {
            // Heartbeat called regularly by the UI to ensure the grabber is still alive.

            // This runs on the UI thread and is not accurate.
            // Do not use it for measures needing accuracy, like framerate estimation.

            _mFrameGrabber.CheckDeviceConnection();
        }

        public void StartGrabbing()
        {
            _mFrameGrabber.StartGrabbing();
            _mContainer.DisplayAsGrabbing(true);
        }

        public void PauseGrabbing()
        {
            _mFrameGrabber.PauseGrabbing();
            _mContainer.DisplayAsGrabbing(false);
        }

        public void BeforeClose()
        {
            if (IsRecording)
            {
                StopRecording();
            }
            _mFrameGrabber.BeforeClose();
        }

        public override void Draw(Graphics canvas)
        {
            // Draw the current image on canvas according to conf.
            // This is called back from UI paint method.
            if (_mFrameGrabber.IsConnected)
            {
                if (_mImageToDisplay != null)
                {
                    try
                    {
                        var outputSize = new Size((int) canvas.ClipBounds.Width, (int) canvas.ClipBounds.Height);
                        FlushOnGraphics(_mImageToDisplay, canvas, outputSize);
                    }
                    catch (Exception exp)
                    {
                        Log.Error("Error while painting image.");
                        Log.Error(exp.Message);
                        Log.Error(exp.StackTrace);
                    }
                }
            }

            _mBPainting = false;
        }

        public Bitmap GetFlushedImage()
        {
            // Returns a standalone image with all drawings flushed.
            // This can be used by snapshot or movie saving.
            // We don't use the screen size, but the original video size (differs from PlayerScreen.)
            // This always represents the image that is drawn on screen, not the last image grabbed by the device.
            var output = new Bitmap(ImageSize.Width, ImageSize.Height, PixelFormat.Format24bppRgb);

            try
            {
                if (_mImageToDisplay != null)
                {
                    output.SetResolution(_mImageToDisplay.HorizontalResolution, _mImageToDisplay.VerticalResolution);
                    FlushOnGraphics(_mImageToDisplay, Graphics.FromImage(output), output.Size);
                }
            }
            catch (Exception)
            {
                Log.ErrorFormat("Exception while trying to get flushed image. Returning blank image.");
            }

            return output;
        }

        public bool StartRecording(string filepath)
        {
            var bRecordingStarted = false;
            Log.Debug("Start recording images to file.");

            // Restart capturing if needed.
            if (!_mFrameGrabber.IsGrabbing)
            {
                _mFrameGrabber.StartGrabbing();
            }

            // Prepare the recorder
            _mVideoRecorder = new VideoRecorder();
            var interval = (_mFrameGrabber.FramesInterval > 0) ? _mFrameGrabber.FramesInterval : 40;
            var result = _mVideoRecorder.Initialize(filepath, interval, _mFrameGrabber.FrameSize);

            if (result == SaveResult.Success)
            {
                // The frames will be pushed to the file upon receiving the FrameGrabbed event.
                IsRecording = true;
                bRecordingStarted = true;
            }
            else
            {
                IsRecording = false;
                DisplayError(result);
            }

            return bRecordingStarted;
        }

        public void StopRecording()
        {
            IsRecording = false;
            Log.Debug("Stop recording images to file.");

            if (_mVideoRecorder != null)
            {
                // Add a VideofileBox with a thumbnail of this video.
                if (_mVideoRecorder.CaptureThumb != null)
                {
                    var cv = new CapturedVideo(CurrentCaptureFilePath, _mVideoRecorder.CaptureThumb);
                    RecentlyCapturedVideos.Add(cv);
                    _mVideoRecorder.CaptureThumb.Dispose();
                    _mContainer.DoUpdateCapturedVideos();
                }

                // Terminate the recording thread and release resources. This will treat any outstanding frames in the queue.
                _mVideoRecorder.Dispose();
            }

            Thread.CurrentThread.Priority = ThreadPriority.Normal;

            // Ask the Explorer tree to refresh itself, (but not the thumbnails pane.)
            var dp = DelegatesPool.Instance();
            if (dp.RefreshFileExplorer != null)
            {
                dp.RefreshFileExplorer(false);
            }
        }

        public int DelayChanged(int percentage)
        {
            // Set the new delay, and give back the value in seconds.
            // The value given back is just an integer, not a double, because we don't have that much precision.
            // The frame rate is roughly estimated from frame received by seconds,
            // and there is a latency inherent to the camcorder that we can't know.
            _mIFrameIndex = (int) ((_mFrameBuffer.Capacity/100.0)*percentage);

            var interval = (_mFrameGrabber.FramesInterval > 0) ? _mFrameGrabber.FramesInterval : 40.0;
            var delay = (int) ((_mIFrameIndex*interval)/1000);

            // Re-adjust frame for the special case of no delay at all.
            // (it's not always easy to drag all the way left to the real 0 spot).
            if (delay < 1)
                _mIFrameIndex = 0;

            // Explicitely call the refresh if we are not currently grabbing.
            if (!_mFrameGrabber.IsGrabbing)
            {
                _mImageToDisplay = _mFrameBuffer.ReadAt(_mIFrameIndex);
                if (!_mBPainting)
                {
                    _mBPainting = true;
                    _mContainer.DoInvalidate();
                }
            }

            return delay;
        }

        public void UpdateMemoryCapacity()
        {
            _mFrameBuffer.UpdateMemoryCapacity(_mBShared);
        }

        #endregion Public methods

        #region Final image creation

        private void FlushOnGraphics(Bitmap image, Graphics canvas, Size outputSize)
        {
            // Configure canvas.
            canvas.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            canvas.CompositingQuality = CompositingQuality.HighSpeed;
            canvas.InterpolationMode = InterpolationMode.Bilinear;
            canvas.SmoothingMode = SmoothingMode.None;

            // Draw image.
            Rectangle rDst;
            rDst = new Rectangle(0, 0, outputSize.Width, outputSize.Height);

            RectangleF rSrc;
            if (CoordinateSystem.Zooming)
            {
                rSrc = CoordinateSystem.ZoomWindow;
            }
            else
            {
                rSrc = new Rectangle(0, 0, image.Width, image.Height);
            }

            canvas.DrawImage(image, rDst, rSrc, GraphicsUnit.Pixel);

            FlushDrawingsOnGraphics(canvas);

            // .Magnifier
            // TODO: handle miroring.
            if (Magnifier.Mode != MagnifierMode.NotVisible)
            {
                Magnifier.Draw(image, canvas, 1.0, false);
            }
        }

        private void FlushDrawingsOnGraphics(Graphics canvas)
        {
            // Commit drawings on image.
            canvas.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (AbstractDrawing ad in Metadata.ExtraDrawings)
            {
                ad.Draw(canvas, CoordinateSystem, false, 0);
            }

            // In capture mode, all drawings are gathered in a virtual key image at m_Metadata[0].
            // Draw all drawings in reverse order to get first object on the top of Z-order.
            for (var i = Metadata[0].Drawings.Count - 1; i >= 0; i--)
            {
                var bSelected = (i == Metadata.SelectedDrawing);
                Metadata[0].Drawings[i].Draw(canvas, CoordinateSystem, bSelected, 0);
            }
        }

        #endregion Final image creation

        #region Error messages

        public void AlertCannotConnect()
        {
            // Couldn't find device. Signal to user.
            MessageBox.Show(
                ScreenManagerLang.Error_Capture_CannotConnect_Text.Replace("\\n", "\n"),
                ScreenManagerLang.Error_Capture_CannotConnect_Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);
        }

        public void AlertConnectionLost()
        {
            // Device stopped sending frames.
            _mContainer.AlertDisconnected();
        }

        private void DisplayError(SaveResult result)
        {
            switch (result)
            {
                case SaveResult.FileHeaderNotWritten:
                case SaveResult.FileNotOpened:
                    DisplayErrorMessage(ScreenManagerLang.Error_SaveMovie_FileError);
                    break;

                case SaveResult.EncoderNotFound:
                case SaveResult.EncoderNotOpened:
                case SaveResult.EncoderParametersNotAllocated:
                case SaveResult.EncoderParametersNotSet:
                case SaveResult.InputFrameNotAllocated:
                case SaveResult.MuxerNotFound:
                case SaveResult.MuxerParametersNotAllocated:
                case SaveResult.MuxerParametersNotSet:
                case SaveResult.VideoStreamNotCreated:
                case SaveResult.UnknownError:
                default:
                    DisplayErrorMessage(ScreenManagerLang.Error_SaveMovie_LowLevelError);
                    break;
            }
        }

        private void DisplayErrorMessage(string err)
        {
            MessageBox.Show(
                err.Replace("\\n", "\n"),
                ScreenManagerLang.Error_SaveMovie_Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);
        }

        #endregion Error messages

        #region Private Methods

        private void SetAspectRatio(AspectRatio aspectRatio, Size size)
        {
            _mAspectRatio = aspectRatio;

            if (_mFrameGrabber.IsConnected)
            {
                int newHeight;

                switch (aspectRatio)
                {
                    case AspectRatio.AutoDetect:
                    default:
                        newHeight = size.Height;
                        break;

                    case AspectRatio.Force43:
                        newHeight = (size.Width/4)*3;
                        break;

                    case AspectRatio.Force169:
                        newHeight = (size.Width/16)*9;
                        break;
                }

                ImageSize = new Size(size.Width, newHeight);
                CoordinateSystem.SetOriginalSize(ImageSize);
                _mContainer.DoInitDecodingSize();
                Metadata.ImageSize = ImageSize;
                _mFrameBuffer.UpdateFrameSize(ImageSize);
            }
        }

        private void FrameGrabbed_Invoked(object sender, EventArgs e)
        {
            // We are back in the Main thread.

            // Get the raw frame we will be displaying/saving.
            _mImageToDisplay = _mFrameBuffer.ReadAt(_mIFrameIndex);

            if (IsRecording && _mVideoRecorder != null && _mVideoRecorder.Initialized)
            {
                // The recorder runs in its own thread.
                // We need to make a full copy of the frame because m_ImageToDisplay may change before we actually save it.
                // TODO: drop mechanism in case the frame queue grows too big.
                if (!_mVideoRecorder.Cancelling)
                {
                    var bmp = GetFlushedImage();
                    _mVideoRecorder.EnqueueFrame(bmp);
                }
                else
                {
                    StopRecording();
                    _mContainer.DisplayAsRecording(false);
                    DisplayError(_mVideoRecorder.CancelReason);
                }
            }

            // Ask a refresh.
            if (!_mBPainting)
            {
                _mBPainting = true;
                _mContainer.DoInvalidate();
            }

            // Update status bar if needed.
            if (_mICurrentBufferFill != _mFrameBuffer.FillPercentage)
            {
                _mICurrentBufferFill = _mFrameBuffer.FillPercentage;
                _mContainer.DoUpdateStatusBar();
            }
        }

        #endregion Private Methods
    }
}