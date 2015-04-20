#region License

/*
Copyright © Joan Charmant 2011.
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

using Kinovea.VideoFiles;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Threading;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     VideoRecorder - Saves images to a file using the producer-consumer paradigm.
    ///     The class hosts a queue of frames to save, and process them as fast as possible in its own thread.
    ///     Once all frames in the queue are processed, it just waits for the producer to add some more or signal stop.
    ///     The producer push frames in the queue and set the signal.
    ///     The frames pushed have to be deep copies since we don't know when we will be able to handle them.
    ///     Ref: http://www.albahari.com/threading/part2.aspx#_Signaling_with_Event_Wait_Handles
    /// </summary>
    public class VideoRecorder : IDisposable
    {
        #region Constructor

        public VideoRecorder()
        {
            _mWorkerThread = new Thread(Work);
        }

        #endregion Constructor

        #region Private Methods

        private void Work()
        {
            Thread.CurrentThread.Name = "Record";

            while (!Cancelling)
            {
                Bitmap bmp = null;
                lock (_mLocker)
                {
                    if (_mFrameQueue.Count > 0)
                    {
                        bmp = _mFrameQueue.Dequeue();
                        if (bmp == null)
                        {
                            Log.Debug("Recording thread finished.");
                            return;
                        }
                    }
                }

                if (bmp != null)
                {
                    var res = _mVideoFileWriter.SaveFrame(bmp);

                    if (res != SaveResult.Success)
                    {
                        // Start cancellation procedure
                        // The producer should test for .Cancelling and stop queuing items at this point.
                        // We don't try to save the outstanding frames, but the video file should be valid.
                        Log.Error("Error while saving frame to file.");
                        Cancelling = true;
                        CancelReason = res;

                        bmp.Dispose();

                        lock (_mLocker)
                        {
                            while (_mFrameQueue.Count > 0)
                            {
                                var outstanding = _mFrameQueue.Dequeue();
                                if (outstanding != null)
                                {
                                    outstanding.Dispose();
                                }
                            }

                            if (Initialized)
                            {
                                try
                                {
                                    _mVideoFileWriter.CloseSavingContext(true);
                                }
                                catch (Exception exp)
                                {
                                    Log.Error(exp.Message);
                                    Log.Error(exp.StackTrace);
                                }
                            }
                        }

                        _mWaitHandle.Close();
                        return;
                    }
                    if (!_mBCaptureThumbSet)
                    {
                        CaptureThumb = bmp;
                        _mBCaptureThumbSet = true;
                    }
                    else
                    {
                        bmp.Dispose();
                    }
                }
                else
                {
                    _mWaitHandle.WaitOne();
                }
            }
        }

        #endregion Private Methods

        #region Properties

        public bool Initialized { get; private set; }

        public Bitmap CaptureThumb { get; private set; }

        public bool Cancelling { get; private set; }

        public SaveResult CancelReason { get; private set; } = SaveResult.UnknownError;

        public bool Full
        {
            get { return _mFrameQueue.Count > MICapacity; }
        }

        #endregion Properties

        #region Members

        private readonly EventWaitHandle _mWaitHandle = new AutoResetEvent(false);
        private readonly Thread _mWorkerThread;
        private readonly object _mLocker = new object();
        private readonly Queue<Bitmap> _mFrameQueue = new Queue<Bitmap>();
        private readonly VideoFileWriter _mVideoFileWriter = new VideoFileWriter();
        private bool _mBCaptureThumbSet;
        private static readonly int MICapacity = 5;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Public Methods

        public SaveResult Initialize(string filepath, double interval, Size frameSize)
        {
            // Open the recording context and start the recording thread.
            // The thread will then wait for the first frame to drop in.

            // FIXME: The FileWriter will currently only use the original size due to some problems.
            // Most notably, DV video passed into 16:9 (720x405) crashes swscale().
            // TODO: Check if this is due to non even height.
            var iv = new InfosVideo();
            iv.iWidth = frameSize.Width;
            iv.iHeight = frameSize.Height;

            var result = _mVideoFileWriter.OpenSavingContext(filepath, iv, interval, false);

            if (result == SaveResult.Success)
            {
                Initialized = true;
                _mBCaptureThumbSet = false;
                _mWorkerThread.Start();
            }
            else
            {
                try
                {
                    _mVideoFileWriter.CloseSavingContext(false);
                }
                catch (Exception exp)
                {
                    // Saving context couldn't be opened properly. Depending on failure we might also fail at trying to close it again.
                    Log.Error(exp.Message);
                    Log.Error(exp.StackTrace);
                }
            }

            return result;
        }

        public void Dispose()
        {
            if (!Cancelling)
            {
                EnqueueFrame(null); // Signal the consumer to exit.
                _mWorkerThread.Join(); // Wait for the consumer's thread to finish.
                _mWaitHandle.Close(); // Release any OS resources.

                if (Initialized)
                    _mVideoFileWriter.CloseSavingContext(true);
            }
        }

        public void EnqueueFrame(Bitmap frame)
        {
            if (!Cancelling)
            {
                lock (_mLocker)
                {
                    // TODO: prevent overflowing the queue.
                    _mFrameQueue.Enqueue(frame);
                }

                _mWaitHandle.Set();
            }
        }

        #endregion Public Methods
    }
}