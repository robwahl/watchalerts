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

using Kinovea.Services;
using Kinovea.VideoFiles;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public class ThumbListLoader
    {
        #region Constructor

        public ThumbListLoader(List<string> fileNames, SplitterPanel panel, VideoFile playerServer)
        {
            _mFileNames = fileNames;
            _mPanel = panel;
            _mVideoFile = playerServer;

            _mITotalFilesToLoad = fileNames.Count;
            _mInfosThumbnailQueue = new List<InfosThumbnail>();

            _mBgThumbsLoader = new BackgroundWorker();
            _mBgThumbsLoader.WorkerReportsProgress = true;
            _mBgThumbsLoader.WorkerSupportsCancellation = true;
            _mBgThumbsLoader.DoWork += bgThumbsLoader_DoWork;
            _mBgThumbsLoader.RunWorkerCompleted += bgThumbsLoader_RunWorkerCompleted;
            _mBgThumbsLoader.ProgressChanged += bgThumbsLoader_ProgressChanged;
        }

        #endregion Constructor

        #region Properties

        public bool Unused { get; set; } = true;

        #endregion Properties

        #region Members

        private readonly List<string> _mFileNames;

        private readonly BackgroundWorker _mBgThumbsLoader;
        private bool _mBIsIdle;
        private readonly List<InfosThumbnail> _mInfosThumbnailQueue;
        private int _mILastFilled = -1;
        private readonly SplitterPanel _mPanel;
        private readonly VideoFile _mVideoFile;
        private readonly int _mITotalFilesToLoad; // only for debug info
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Public interface

        public void Launch()
        {
            Log.Debug(string.Format("ThumbListLoader preparing to load {0} files.", _mITotalFilesToLoad));

            Unused = false;
            Application.Idle += IdleDetector;
            if (!_mBgThumbsLoader.IsBusy)
            {
                _mBgThumbsLoader.RunWorkerAsync(_mFileNames);
            }
        }

        public void Cancel()
        {
            _mBgThumbsLoader.CancelAsync();
        }

        #endregion Public interface

        #region Background Thread Work and display.

        private void IdleDetector(object sender, EventArgs e)
        {
            // Used to know when it is safe to update the ui.
            _mBIsIdle = true;
        }

        private void bgThumbsLoader_DoWork(object sender, DoWorkEventArgs e)
        {
            //-------------------------------------------------------------
            // /!\ This is WORKER THREAD space. Do not update UI.
            //-------------------------------------------------------------
            Thread.CurrentThread.Name = string.Format("Thumbnail Loader ({0})", Thread.CurrentThread.ManagedThreadId);
            var fileNames = (List<string>) e.Argument;
            _mInfosThumbnailQueue.Clear();
            _mILastFilled = -1;

            e.Cancel = false;

            for (var i = 0; i < fileNames.Count; i++)
            {
                if (!_mBgThumbsLoader.CancellationPending)
                {
                    try
                    {
                        var it = _mVideoFile.GetThumbnail(fileNames[i], 200, 5);
                        _mInfosThumbnailQueue.Insert(0, it);
                    }
                    catch (Exception)
                    {
                        _mInfosThumbnailQueue.Insert(0, null);
                    }
                    _mBgThumbsLoader.ReportProgress(i, null);
                }
                else
                {
                    Log.Debug("bgThumbsLoader_DoWork - cancelling");
                    e.Cancel = true;
                    break;
                }
            }
            e.Result = 0;
        }

        private void bgThumbsLoader_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //-------------------------------------------------
            // This is not Worker Thread space. Update UI here.
            //-------------------------------------------------
            //Console.WriteLine("[ProgressChanged] - queue:{0}, total:{1}", m_BitmapQueue.Count, m_iTotalFilesToLoad);

            if (_mBIsIdle && !_mBgThumbsLoader.CancellationPending && (_mILastFilled + 1 < _mPanel.Controls.Count))
            {
                _mBIsIdle = false;

                // Copy the queue, because it is still being filled by the bg worker.
                var tmpQueue = new List<InfosThumbnail>();
                foreach (var it in _mInfosThumbnailQueue)
                {
                    tmpQueue.Add(it);
                }

                // bg worker can start re fueling now, if a bitmap was queued during the copy, don't clear it.
                _mInfosThumbnailQueue.RemoveRange(_mInfosThumbnailQueue.Count - tmpQueue.Count, tmpQueue.Count);

                PopulateControls(tmpQueue);
            }
        }

        private void bgThumbsLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //---------------------------------------------------------------------------
            // Check if the queue has been completely flushed.
            // (in case the last thumbs loaded couldn't be added because we weren't idle.
            //---------------------------------------------------------------------------
            if (_mInfosThumbnailQueue.Count > 0 && !e.Cancelled)
            {
                PopulateControls(_mInfosThumbnailQueue);
            }

            Application.Idle -= IdleDetector;
            Unused = true;
        }

        private void PopulateControls(List<InfosThumbnail> infosThumbQueue)
        {
            // Unqueue bitmaps and populate the controls
            for (var i = infosThumbQueue.Count - 1; i >= 0; i--)
            {
                // Double check.
                if (_mILastFilled + 1 < _mPanel.Controls.Count)
                {
                    _mILastFilled++;
                    var tlvi = _mPanel.Controls[_mILastFilled] as ThumbListViewItem;
                    if (tlvi != null)
                    {
                        if (infosThumbQueue[i] != null)
                        {
                            if (infosThumbQueue[i].Thumbnails.Count > 0)
                            {
                                tlvi.Thumbnails = infosThumbQueue[i].Thumbnails;
                                if (infosThumbQueue[i].IsImage)
                                {
                                    tlvi.IsImage = true;
                                    tlvi.Duration = "0";
                                }
                                else
                                {
                                    tlvi.Duration =
                                        TimeHelper.MillisecondsToTimecode(infosThumbQueue[i].iDurationMilliseconds,
                                            false, true);
                                }

                                tlvi.ImageSize = (Size) infosThumbQueue[i].imageSize;
                                tlvi.HasKva = infosThumbQueue[i].HasKva;
                            }
                            else
                            {
                                tlvi.DisplayAsError();
                            }
                        }
                        else
                        {
                            tlvi.DisplayAsError();
                        }

                        // Issue: We computed the .top coord of the thumb when the panel wasn't moving.
                        // If we are scrolling, the .top of the panel is moving,
                        // so the thumbnails will be activated at the wrong spot.
                        if (_mPanel.AutoScrollPosition.Y != 0)
                        {
                            tlvi.Top = tlvi.Top + _mPanel.AutoScrollPosition.Y;
                        }

                        _mPanel.Controls[_mILastFilled].Visible = true;
                    }

                    infosThumbQueue.RemoveAt(i);
                }
            }
            _mPanel.Invalidate();
        }

        #endregion Background Thread Work and display.
    }
}