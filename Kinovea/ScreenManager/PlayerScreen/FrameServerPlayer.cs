#region License

/*
Copyright © Joan Charmant 2009.
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
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     FrameServerPlayer encapsulate the video file, meta data and everything
    ///     needed to render the frame and access file functions.
    ///     PlayerScreenUserInterface is the View, FrameServerPlayer is the Model.
    /// </summary>
    public class FrameServerPlayer : AbstractFrameServer
    {
        #region Properties

        public VideoFile VideoFile { get; set; } = private new VideoFile();

        public Metadata Metadata { get; set; }

        public CoordinateSystem CoordinateSystem
        {
            get { return Metadata.CoordinateSystem; }
        }

        public bool Loaded
        {
            get
            {
                var loaded = false;
                if (VideoFile != null && VideoFile.Loaded)
                {
                    loaded = true;
                }
                return loaded;
            }
        }

        #endregion Properties

        #region Members

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Saving process (globals because the bgWorker is split in several methods)
        private FormProgressBar _mFormProgressBar;

        private long _mISaveStart;
        private long _mISaveEnd;
        private string _mSaveFile;
        private Metadata _mSaveMetadata;
        private double _mFSaveFramesInterval;
        private bool _mBSaveFlushDrawings;
        private bool _mBSaveKeyframesOnly;
        private bool _mBSavePausedVideo;
        private DelegateGetOutputBitmap _mSaveDelegateOutputBitmap;
        private SaveResult _mSaveResult;

        #endregion Members

        #region Public

        public LoadResult Load(string filePath)
        {
            // Set global settings.
            var pm = PreferencesManager.Instance();
            VideoFile.SetDefaultSettings((int) pm.AspectRatio, pm.DeinterlaceByDefault);
            return VideoFile.Load(filePath);
        }

        public void Unload()
        {
            // Prepare the FrameServer for a new video by resetting everything.
            VideoFile.Unload();
            if (Metadata != null) Metadata.Reset();
        }

        public void SetupMetadata()
        {
            // Setup Metadata global infos in case we want to flush it to a file (or mux).

            if (Metadata != null)
            {
                var imageSize = new Size(VideoFile.Infos.iDecodingWidth, VideoFile.Infos.iDecodingHeight);

                Metadata.ImageSize = imageSize;
                Metadata.AverageTimeStampsPerFrame = VideoFile.Infos.iAverageTimeStampsPerFrame;
                Metadata.CalibrationHelper.FramesPerSeconds = VideoFile.Infos.fFps;
                Metadata.FirstTimeStamp = VideoFile.Infos.iFirstTimeStamp;

                Log.Debug("Setup metadata.");
            }
        }

        public override void Draw(Graphics canvas)
        {
            // Draw the current image on canvas according to conf.
            // This is called back from screen paint method.
        }

        public void Save(double fPlaybackFrameInterval, double fSlowmotionPercentage, long iSelStart, long iSelEnd,
            DelegateGetOutputBitmap delegateOutputBitmap)
        {
            // Let the user select what he wants to save exactly.
            // Note: _iSelStart, _iSelEnd, _Metadata, should ultimately be taken from local members.

            var fve = new FormVideoExport(VideoFile.FilePath, Metadata, fSlowmotionPercentage);

            if (fve.Spawn() == DialogResult.OK)
            {
                if (fve.SaveAnalysis)
                {
                    // Save analysis.
                    Metadata.ToXmlFile(fve.Filename);
                }
                else
                {
                    DoSave(fve.Filename,
                        fve.MuxDrawings ? Metadata : null,
                        fve.UseSlowMotion ? fPlaybackFrameInterval : VideoFile.Infos.fFrameInterval,
                        iSelStart,
                        iSelEnd,
                        fve.BlendDrawings,
                        false,
                        false,
                        delegateOutputBitmap);
                }
            }

            // Release configuration form.
            fve.Dispose();
        }

        public void SaveDiaporama(long iSelStart, long iSelEnd, DelegateGetOutputBitmap delegateOutputBitmap, bool diapo)
        {
            // Let the user configure the diaporama export.

            var fde = new FormDiapoExport(diapo);
            if (fde.ShowDialog() == DialogResult.OK)
            {
                DoSave(fde.Filename,
                    null,
                    fde.FrameInterval,
                    iSelStart,
                    iSelEnd,
                    true,
                    fde.PausedVideo ? false : true,
                    fde.PausedVideo,
                    delegateOutputBitmap);
            }

            // Release configuration form.
            fde.Dispose();
        }

        public void AfterSave()
        {
            // Ask the Explorer tree to refresh itself, (but not the thumbnails pane.)
            var dp = DelegatesPool.Instance();
            if (dp.RefreshFileExplorer != null)
            {
                dp.RefreshFileExplorer(false);
            }
        }

        #endregion Public

        #region Saving processing

        private void DoSave(string filePath, Metadata metadata, double fPlaybackFrameInterval, long iSelStart,
            long iSelEnd, bool bFlushDrawings, bool bKeyframesOnly, bool bPausedVideo,
            DelegateGetOutputBitmap delegateOutputBitmap)
        {
            // Save video.
            // We use a bgWorker and a Progress Bar.

            // Memorize the parameters, they will be used later in bgWorkerSave_DoWork.
            // Note: _iSelStart, _iSelEnd, _Metadata, should ultimately be taken from the local members.
            _mISaveStart = iSelStart;
            _mISaveEnd = iSelEnd;
            _mSaveMetadata = metadata;
            _mSaveFile = filePath;
            _mFSaveFramesInterval = fPlaybackFrameInterval;
            _mBSaveFlushDrawings = bFlushDrawings;
            _mBSaveKeyframesOnly = bKeyframesOnly;
            _mBSavePausedVideo = bPausedVideo;
            _mSaveDelegateOutputBitmap = delegateOutputBitmap;

            // Instanciate and configure the bgWorker.
            var bgWorkerSave = new BackgroundWorker();
            bgWorkerSave.WorkerReportsProgress = true;
            bgWorkerSave.WorkerSupportsCancellation = true;
            bgWorkerSave.DoWork += bgWorkerSave_DoWork;
            bgWorkerSave.ProgressChanged += bgWorkerSave_ProgressChanged;
            bgWorkerSave.RunWorkerCompleted += bgWorkerSave_RunWorkerCompleted;

            // Attach the bgWorker to the VideoFile object so it can report progress.
            VideoFile.BgWorker = bgWorkerSave;

            // Create the progress bar and launch the worker.
            _mFormProgressBar = new FormProgressBar(true);
            _mFormProgressBar.Cancel = Cancel_Asked;
            bgWorkerSave.RunWorkerAsync();
            _mFormProgressBar.ShowDialog();
        }

        private void bgWorkerSave_DoWork(object sender, DoWorkEventArgs e)
        {
            // This is executed in Worker Thread space. (Do not call any UI methods)

            var metadata = "";
            if (_mSaveMetadata != null)
            {
                // Get the metadata as XML string.
                // If frame duplication is going to occur (when saving in slow motion at less than 8fps)
                // We have to store this in the xml output to be able to match frames with timestamps later.
                var iDuplicateFactor = (int) Math.Ceiling(_mFSaveFramesInterval/125.0);
                metadata = _mSaveMetadata.ToXmlString(iDuplicateFactor);
            }

            try
            {
                _mSaveResult = VideoFile.Save(_mSaveFile,
                    _mFSaveFramesInterval,
                    _mISaveStart,
                    _mISaveEnd,
                    metadata,
                    _mBSaveFlushDrawings,
                    _mBSaveKeyframesOnly,
                    _mBSavePausedVideo,
                    _mSaveDelegateOutputBitmap);
                if (_mSaveMetadata != null)
                {
                    _mSaveMetadata.CleanupHash();
                }
            }
            catch (Exception exp)
            {
                _mSaveResult = SaveResult.UnknownError;
                Log.Error("Unknown error while saving video.");
                Log.Error(exp.StackTrace);
            }

            e.Result = 0;
        }

        private void bgWorkerSave_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // This method should be called back from the VideoFile when a frame has been processed.
            // call snippet : m_BackgroundWorker.ReportProgress(iCurrentValue, iMaximum);

            var iValue = e.ProgressPercentage;
            var iMaximum = (int) e.UserState;

            if (iValue > iMaximum)
            {
                iValue = iMaximum;
            }

            _mFormProgressBar.Update(iValue, iMaximum, true);
        }

        private void bgWorkerSave_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _mFormProgressBar.Close();
            _mFormProgressBar.Dispose();

            if (_mSaveResult != SaveResult.Success)
            {
                ReportError(_mSaveResult);
            }
            else
            {
                AfterSave();
            }
        }

        private void ReportError(SaveResult err)
        {
            switch (err)
            {
                case SaveResult.Cancelled:
                    // No error message if the user cancelled herself.
                    break;

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
                case SaveResult.ReadingError:
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

        private void Cancel_Asked(object sender, EventArgs e)
        {
            // This will simply set BgWorker.CancellationPending to true,
            // which we check periodically in VideoFile.ExtractToMemory method.
            // This will also end the bgWorker immediately,
            // maybe before we check for the cancellation in the other thread.
            VideoFile.BgWorker.CancelAsync();

            // m_FormProgressBar.Dispose();
        }

        #endregion Saving processing
    }
}