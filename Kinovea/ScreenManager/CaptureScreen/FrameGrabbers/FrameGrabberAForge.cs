#region License

/*
Copyright © Joan Charmant 2010.
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

using AForge.Video;
using AForge.Video.DirectShow;
using Kinovea.ScreenManager.Languages;
using Kinovea.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     FrameGrabberAForge - a FrameGrabber using DirectShow via AForge library.
    ///     We define 3 type of sources:
    ///     - capture sources. (Directshow devices)
    ///     - network source. Built-in source to represent a network camera.
    ///     - empty source. Built-in source when no capture sources have been found.
    /// </summary>
    public class FrameGrabberAForge : AbstractFrameGrabber
    {
        #region Constructor

        public FrameGrabberAForge(IFrameGrabberContainer parent, FrameBuffer buffer)
        {
            _mContainer = parent;
            _mFrameBuffer = buffer;
        }

        #endregion Constructor

        #region Properties

        public override bool IsConnected
        {
            get { return _mBIsConnected; }
        }

        public override bool IsGrabbing
        {
            get { return _mBIsGrabbing; }
        }

        public override string DeviceName
        {
            get { return _mCurrentVideoDevice.Name; }
        }

        public override double FramesInterval
        {
            get { return _mFramesInterval; }
        }

        public override Size FrameSize
        {
            // This may not be used because the user may want to bypass and force an aspect ratio.
            // In this case, only the FrameServerCapture is aware of the final image size.
            get { return _mFrameSize; }
        }

        public override DeviceCapability SelectedCapability
        {
            get { return _mCurrentVideoDevice.SelectedCapability; }
        }

        #endregion Properties

        #region Members

        private IVideoSource _mVideoSource;
        private DeviceDescriptor _mCurrentVideoDevice;
        private readonly IFrameGrabberContainer _mContainer; // FrameServerCapture seen through a limited interface.
        private readonly FrameBuffer _mFrameBuffer;
        private bool _mBIsConnected;
        private bool _mBIsGrabbing;
        private bool _mBSizeKnown;
        private bool _mBSizeChanged;
        private double _mFramesInterval = -1;
        private Size _mFrameSize;
        private int _mIConnectionsAttempts;
        private int _mIGrabbedSinceLastCheck;
        private int _mIConnectionsWithoutFrames;
        private readonly PreferencesManager _mPrefsManager = PreferencesManager.Instance();
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region AbstractFrameGrabber implementation

        public override void PromptDeviceSelector()
        {
            var dp = DelegatesPool.Instance();
            if (dp.DeactivateKeyboardHandler != null)
            {
                dp.DeactivateKeyboardHandler();
            }

            var reconnected = false;

            // Ask the user which device he wants to use or which size/framerate.
            var fdp = new FormDevicePicker(ListDevices(), _mCurrentVideoDevice, DisplayDevicePropertyPage);

            if (fdp.ShowDialog() == DialogResult.OK)
            {
                var dev = fdp.SelectedDevice;

                if (dev == null || dev.Empty)
                {
                    Log.DebugFormat("Selected device is null or empty.");
                    if (_mCurrentVideoDevice != null)
                    {
                        // From something to empty.
                        Disconnect();
                    }
                }
                else if (dev.Network)
                {
                    if (_mCurrentVideoDevice == null || !_mCurrentVideoDevice.Network)
                    {
                        // From empty or non-network to network.
                        Log.DebugFormat("Selected network camera - connect with default parameters");
                        reconnected = ConnectToDevice(dev);
                    }
                    else
                    {
                        // From network to network.
                        Log.DebugFormat("Network camera - parameters changed - connect with new parameters");
                        // Parameters were set on the dialog. We don't care if the parameters were actually changed.
                        var netDevice = new DeviceDescriptor(ScreenManagerLang.Capture_NetworkCamera, fdp.SelectedUrl,
                            fdp.SelectedFormat);
                        reconnected = ConnectToDevice(netDevice);
                    }
                }
                else
                {
                    if (_mCurrentVideoDevice == null || _mCurrentVideoDevice.Network ||
                        dev.Identification != _mCurrentVideoDevice.Identification)
                    {
                        // From network or different capture device to capture device.
                        Log.DebugFormat("Selected capture device");
                        reconnected = ConnectToDevice(dev);
                    }
                    else
                    {
                        // From same capture device - caps changed.
                        var cap = fdp.SelectedCapability;
                        if (cap != null && !cap.Equals(_mCurrentVideoDevice.SelectedCapability))
                        {
                            Log.DebugFormat("Capture device, capability changed.");

                            _mCurrentVideoDevice.SelectedCapability = cap;
                            _mPrefsManager.UpdateSelectedCapability(_mCurrentVideoDevice.Identification, cap);

                            if (_mBIsGrabbing)
                            {
                                _mVideoSource.Stop();
                            }

                            ((VideoCaptureDevice)_mVideoSource).DesiredFrameSize = cap.FrameSize;
                            ((VideoCaptureDevice)_mVideoSource).DesiredFrameRate = cap.Framerate;

                            _mFrameSize = cap.FrameSize;
                            _mFramesInterval = 1000 / (double)cap.Framerate;

                            Log.Debug(string.Format("New capability: {0}", cap));

                            _mBSizeChanged = true;

                            if (_mBIsGrabbing)
                            {
                                _mVideoSource.Start();
                            }
                        }
                    }
                }

                if (reconnected)
                {
                    _mContainer.Connected();
                }
            }

            fdp.Dispose();

            if (dp.ActivateKeyboardHandler != null)
            {
                dp.ActivateKeyboardHandler();
            }
        }

        public override void NegociateDevice()
        {
            if (!_mBIsConnected)
            {
                Log.Debug("Trying to connect to a Capture source.");

                _mIConnectionsAttempts++;

                // TODO: Detect if a device is already in use
                // (by an other app or even just by the other screen).
                var devices = ListDevices();
                if (devices.Count > 0)
                {
                    var bSuccess = false;

                    // Check if we were already connected to a device.
                    // In that case we try to reconnect to the same one instead of defaulting to the first of the list.
                    // Unless that was the empty device placeholder. (This might happen if we start with 0 device and plug one afterwards)
                    // The network placeholder device is not subject to the disconnection/reconnection mechanism either.
                    if (_mCurrentVideoDevice != null && !_mCurrentVideoDevice.Empty && !_mCurrentVideoDevice.Network)
                    {
                        // Look for the device we were previously connected to.
                        var bFoundCurrentDevice = false;
                        for (var i = 0; i < devices.Count - 1; i++)
                        {
                            if (devices[i].Identification == _mCurrentVideoDevice.Identification)
                            {
                                bFoundCurrentDevice = true;
                                Log.DebugFormat("Trying to reconnect to {0}.", _mCurrentVideoDevice.Name);
                                bSuccess = ConnectToDevice(devices[i]);
                            }
                        }

                        // If not found, default to the first one anyway.
                        if (!bFoundCurrentDevice && !devices[0].Empty)
                        {
                            Log.DebugFormat(
                                "Current device not found (has been physically unplugged) - connect to first one ({0})",
                                devices[0].Name);
                            bSuccess = ConnectToDevice(devices[0]);
                        }
                    }
                    else
                    {
                        // We were not connected to any device, or we were connected to a special device.
                        // Connect to the first one (Capture device).
                        if (!devices[0].Empty)
                        {
                            Log.DebugFormat("First attempt, default to first device ({0})", devices[0].Name);
                            bSuccess = ConnectToDevice(devices[0]);
                        }
                    }

                    if (bSuccess)
                    {
                        _mContainer.Connected();
                    }
                }

                _mIGrabbedSinceLastCheck = 0;

                if (_mBIsConnected)
                {
                    _mIConnectionsWithoutFrames++;
                }

                if (!_mBIsConnected && _mIConnectionsAttempts == 2)
                {
                    _mContainer.AlertCannotConnect();
                }
            }
        }

        public override void CheckDeviceConnection()
        {
            //--------------------------------------------------------------------------------------------------
            //
            // Automatic reconnection mechanism.
            //
            // Issue: We are not notified when the source disconnects, we need to figure it out ourselves.
            //
            // Mechanism : count the number of frames we received since last check. (heartbeat = 1 second).
            // If we are supposed to do grabbing and we received nothing, we are in one of two conditions:
            // 1. The device has been disconnected.
            // 2. We are in PAUSE state.
            //
            // The problem is that even in condition 1, we may still succeed in reconnecting for a few attempts.
            // If we detect that we CONSTANTLY succeed in reconnecting but there are still no frames coming,
            // we are probably in condition 2. Thus we'll stop trying to disconnect/reconnect, and just wait
            // for the source to start sending frames again.
            //
            // On first connection and after a size change, we keep waiting for frames without disconnecting until
            // we receive the first one. Otherwise we would constantly trigger the mechanism as the newly connected
            // device doesn't start to stream before the next heartbeat.
            //
            // Note:
            // This prevents working with very slow capturing devices (when fps < heartbeat).
            // Can't check if we are not currently grabbing.
            //--------------------------------------------------------------------------------------------------

            // bHasJustConnected : prevent triggerring the mechanism if we just changed device or conf.
            // bStayConnected : do not trigger either if we are on network device.
            // m_iConnectionsWithoutFrames : we allow for a few attempts at reconnection without a single frame grabbed.
            var bHasJustConnected = !_mBSizeKnown || _mBSizeChanged;
            var bStayConnected = _mCurrentVideoDevice.Empty || _mCurrentVideoDevice.Network || bHasJustConnected;

            if (!bStayConnected && _mIConnectionsWithoutFrames < 2)
            {
                if (_mBIsGrabbing && _mIGrabbedSinceLastCheck == 0)
                {
                    Log.DebugFormat("{0} has been disconnected.", _mCurrentVideoDevice.Name);

                    // Close properly.
                    _mVideoSource.SignalToStop();
                    _mVideoSource.WaitForStop();
                    _mBIsGrabbing = false;
                    _mBIsConnected = false;
                    _mContainer.AlertConnectionLost();

                    // Set connection attempts so we don't show the initial error message.
                    _mIConnectionsAttempts = 2;
                }
            }

            _mIGrabbedSinceLastCheck = 0;
        }

        public override void StartGrabbing()
        {
            if (_mBIsConnected && _mVideoSource != null)
            {
                if (!_mBIsGrabbing)
                {
                    _mBSizeKnown = false;
                    _mVideoSource.Start();
                }

                _mBIsGrabbing = true;
                Log.DebugFormat("Starting to grab frames from {0}.", _mCurrentVideoDevice.Name);
            }
        }

        public override void PauseGrabbing()
        {
            if (_mVideoSource != null)
            {
                Log.Debug("Pausing frame grabbing.");

                if (_mBIsConnected && _mBIsGrabbing)
                {
                    _mVideoSource.Stop();
                }

                _mBIsGrabbing = false;
                _mIGrabbedSinceLastCheck = 0;
            }
        }

        public override void BeforeClose()
        {
            Disconnect();
        }

        public void DisplayDevicePropertyPage(IntPtr windowHandle)
        {
            var device = _mVideoSource as VideoCaptureDevice;
            if (device != null)
            {
                try
                {
                    device.DisplayPropertyPage(windowHandle);
                }
                catch (Exception)
                {
                    Log.ErrorFormat("Error when trying to display device property page.");
                }
            }
        }

        #endregion AbstractFrameGrabber implementation

        #region Private methods

        private List<DeviceDescriptor> ListDevices()
        {
            // List all the devices currently connected (+ special entries).
            var devices = new List<DeviceDescriptor>();

            // Capture devices
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo fi in videoDevices)
            {
                devices.Add(new DeviceDescriptor(fi.Name, fi.MonikerString));
            }

            if (devices.Count == 0)
            {
                // Special entry if no Directshow camera found.
                // We add this one so the network camera doesn't get connected by default.
                devices.Add(new DeviceDescriptor(ScreenManagerLang.Capture_CameraNotFound));
            }

            // Special entry for network cameras.
            devices.Add(new DeviceDescriptor(ScreenManagerLang.Capture_NetworkCamera, _mPrefsManager.NetworkCameraUrl,
                _mPrefsManager.NetworkCameraFormat));

            return devices;
        }

        private bool ConnectToDevice(DeviceDescriptor device)
        {
            Log.DebugFormat("Connecting to {0}", device.Name);

            Disconnect();
            var created = false;
            if (device.Network)
            {
                // Network Camera. Connect to last used url.
                // The user will have to open the dialog again if parameters have changed or aren't good.

                // Parse URL for inline username:password.
                var login = "";
                var pass = "";

                var networkCameraUrl = new Uri(device.NetworkCameraUrl);
                if (!string.IsNullOrEmpty(networkCameraUrl.UserInfo))
                {
                    var split = networkCameraUrl.UserInfo.Split(':');
                    if (split.Length == 2)
                    {
                        login = split[0];
                        pass = split[1];
                    }
                }

                if (device.NetworkCameraFormat == NetworkCameraFormat.Jpeg)
                {
                    var source = new JPEGStream(device.NetworkCameraUrl);
                    if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(pass))
                    {
                        source.Login = login;
                        source.Password = pass;
                    }

                    _mVideoSource = source;
                }
                else
                {
                    var source = new MJPEGStream(device.NetworkCameraUrl);
                    if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(pass))
                    {
                        source.Login = login;
                        source.Password = pass;
                    }

                    _mVideoSource = source;
                }
                _mPrefsManager.NetworkCameraFormat = device.NetworkCameraFormat;
                _mPrefsManager.NetworkCameraUrl = device.NetworkCameraUrl;
                _mPrefsManager.Export();

                created = true;
            }
            else
            {
                _mVideoSource = new VideoCaptureDevice(device.Identification);
                var captureDevice = _mVideoSource as VideoCaptureDevice;
                if (captureDevice != null)
                {
                    if ((captureDevice.VideoCapabilities != null) && (captureDevice.VideoCapabilities.Length > 0))
                    {
                        // Import the capabilities of the device.
                        foreach (var vc in captureDevice.VideoCapabilities)
                        {
                            var dc = new DeviceCapability(vc.FrameSize, vc.FrameRate);
                            device.Capabilities.Add(dc);

                            Log.Debug(string.Format("Device Capability. {0}", dc));
                        }

                        DeviceCapability selectedCapability = null;

                        // Check if we already know this device and have a preferred configuration.
                        foreach (var conf in _mPrefsManager.DeviceConfigurations)
                        {
                            if (conf.Id == device.Identification)
                            {
                                // Try to find the previously selected capability.
                                selectedCapability = device.GetCapabilityFromSpecs(conf.Cap);
                                if (selectedCapability != null)
                                    Log.Debug(string.Format("Picking capability from preferences: {0}",
                                        selectedCapability));
                            }
                        }

                        if (selectedCapability == null)
                        {
                            // Pick the one with max frame size.
                            selectedCapability = device.GetBestSizeCapability();
                            Log.Debug(string.Format("Picking a default capability (best size): {0}", selectedCapability));
                            _mPrefsManager.UpdateSelectedCapability(device.Identification, selectedCapability);
                        }

                        device.SelectedCapability = selectedCapability;
                        captureDevice.DesiredFrameSize = selectedCapability.FrameSize;
                        captureDevice.DesiredFrameRate = selectedCapability.Framerate;
                        _mFrameSize = selectedCapability.FrameSize;
                        _mFramesInterval = 1000 / (double)selectedCapability.Framerate;
                    }
                    else
                    {
                        captureDevice.DesiredFrameRate = 0;
                    }

                    created = true;
                }
            }

            if (created)
            {
                _mCurrentVideoDevice = device;
                _mVideoSource.NewFrame += VideoDevice_NewFrame;
                _mVideoSource.VideoSourceError += VideoDevice_VideoSourceError;
                _mBIsConnected = true;
            }
            else
            {
                Log.Error("Couldn't create the capture device.");
            }

            return created;
        }

        private void Disconnect()
        {
            // The screen is about to be closed, release resources.

            if (_mBIsConnected && _mVideoSource != null)
            {
                Log.DebugFormat("disconnecting from {0}", _mCurrentVideoDevice.Name);

                // Reset
                _mBIsGrabbing = false;
                _mBSizeKnown = false;
                _mIConnectionsAttempts = 0;
                _mContainer.SetImageSize(Size.Empty);

                _mVideoSource.Stop();
                _mVideoSource.NewFrame -= VideoDevice_NewFrame;
                _mVideoSource.VideoSourceError -= VideoDevice_VideoSourceError;

                _mFrameBuffer.Clear();
                _mBIsConnected = false;
            }
        }

        private void VideoDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // A new frame has been grabbed, push it to the buffer and notifies the frame server.
            if (!_mBSizeKnown || _mBSizeChanged)
            {
                Thread.CurrentThread.Name = "Grab";

                _mBSizeKnown = true;
                _mBSizeChanged = false;

                var sz = eventArgs.Frame.Size;

                _mContainer.SetImageSize(sz);
                Log.DebugFormat("First frame or size changed. Device infos : {0}. Received frame : {1}", _mFrameSize, sz);

                // Update the "official" size (used for saving context.)
                _mFrameSize = sz;

                if (_mCurrentVideoDevice.Network)
                {
                    // This source is now officially working. Save the parameters to prefs.
                    _mPrefsManager.NetworkCameraUrl = _mCurrentVideoDevice.NetworkCameraUrl;
                    _mPrefsManager.NetworkCameraFormat = _mCurrentVideoDevice.NetworkCameraFormat;
                    _mPrefsManager.AddRecentCamera(_mCurrentVideoDevice.NetworkCameraUrl);
                    _mPrefsManager.Export();
                }
            }

            _mIConnectionsWithoutFrames = 0;
            _mIGrabbedSinceLastCheck++;
            _mFrameBuffer.Write(eventArgs.Frame);
            _mContainer.FrameGrabbed();
        }

        private void VideoDevice_VideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs)
        {
            Log.ErrorFormat("Capture error: {1}", _mCurrentVideoDevice.Name, eventArgs.Description);
        }

        #endregion Private methods
    }
}