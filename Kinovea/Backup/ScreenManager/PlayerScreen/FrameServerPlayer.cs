﻿#region License
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
#endregion
using Kinovea.Services;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Kinovea.ScreenManager.Languages;
using Kinovea.VideoFiles;

namespace Kinovea.ScreenManager
{
	/// <summary>
	/// FrameServerPlayer encapsulate the video file, meta data and everything 
	/// needed to render the frame and access file functions.
	/// PlayerScreenUserInterface is the View, FrameServerPlayer is the Model.
	/// </summary>
	public class FrameServerPlayer : AbstractFrameServer
	{
		#region Properties
		public VideoFile VideoFile
		{
			get { return m_VideoFile; }
			set { m_VideoFile = value; }
		}
		public Metadata Metadata
		{
			get { return m_Metadata; }
			set { m_Metadata = value; }
		}
		public CoordinateSystem CoordinateSystem
		{
			get { return m_Metadata.CoordinateSystem; }
		}
		public bool Loaded
		{
			get 
			{ 
				bool loaded = false;
				if(m_VideoFile != null && m_VideoFile.Loaded)
				{
					loaded = true;
				}
				return loaded;
			}
		}
		#endregion
		
		#region Members
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private VideoFile m_VideoFile = new VideoFile();
		private Metadata m_Metadata;
		
		// Saving process (globals because the bgWorker is split in several methods)
		private formProgressBar m_FormProgressBar;
		private long m_iSaveStart;
		private long m_iSaveEnd;
        private string m_SaveFile;
        private Metadata m_SaveMetadata;
        private double m_fSaveFramesInterval;
        private bool m_bSaveFlushDrawings;
        private bool m_bSaveKeyframesOnly;
        private bool m_bSavePausedVideo;
        private DelegateGetOutputBitmap m_SaveDelegateOutputBitmap;
        private SaveResult m_SaveResult;
		#endregion

		#region Constructor
		public FrameServerPlayer()
		{
		}
		#endregion
		
		#region Public
		public LoadResult Load(string _FilePath)
		{
			// Set global settings.
			PreferencesManager pm = PreferencesManager.Instance();
			m_VideoFile.SetDefaultSettings((int)pm.AspectRatio, pm.DeinterlaceByDefault);
			return m_VideoFile.Load(_FilePath);
		}
		public void Unload()
		{
			// Prepare the FrameServer for a new video by resetting everything.
			m_VideoFile.Unload();
			if(m_Metadata != null) m_Metadata.Reset();
		}
		public void SetupMetadata()
		{
			// Setup Metadata global infos in case we want to flush it to a file (or mux).
			
			if(m_Metadata != null)
			{
				Size imageSize = new Size(m_VideoFile.Infos.iDecodingWidth, m_VideoFile.Infos.iDecodingHeight);
						
				m_Metadata.ImageSize = imageSize;
				m_Metadata.AverageTimeStampsPerFrame = m_VideoFile.Infos.iAverageTimeStampsPerFrame;
				m_Metadata.CalibrationHelper.FramesPerSeconds = m_VideoFile.Infos.fFps;
				m_Metadata.FirstTimeStamp = m_VideoFile.Infos.iFirstTimeStamp;
				
				log.Debug("Setup metadata.");
			}
		}
		public override void Draw(Graphics _canvas)
		{
			// Draw the current image on canvas according to conf.
			// This is called back from screen paint method.
		}
		public void Save(double _fPlaybackFrameInterval, double _fSlowmotionPercentage, Int64 _iSelStart, Int64 _iSelEnd, DelegateGetOutputBitmap _DelegateOutputBitmap)	
		{
			// Let the user select what he wants to save exactly.
			// Note: _iSelStart, _iSelEnd, _Metadata, should ultimately be taken from local members.
			
			formVideoExport fve = new formVideoExport(m_VideoFile.FilePath, m_Metadata, _fSlowmotionPercentage);
            
			if(fve.Spawn() == DialogResult.OK)
            {
            	if(fve.SaveAnalysis)
            	{
            		// Save analysis.
            		m_Metadata.ToXmlFile(fve.Filename);
            	}
            	else
            	{
            		DoSave(fve.Filename, 
    						fve.MuxDrawings ? m_Metadata : null,
    						fve.UseSlowMotion ? _fPlaybackFrameInterval : m_VideoFile.Infos.fFrameInterval,
    						_iSelStart,
    						_iSelEnd,
    						fve.BlendDrawings,
    						false,
    						false,
    						_DelegateOutputBitmap);
            	}
            }
			
			// Release configuration form.
            fve.Dispose();
		}
		public void SaveDiaporama(Int64 _iSelStart, Int64 _iSelEnd, DelegateGetOutputBitmap _DelegateOutputBitmap, bool _diapo)
		{
			// Let the user configure the diaporama export.
			
			formDiapoExport fde = new formDiapoExport(_diapo);
			if(fde.ShowDialog() == DialogResult.OK)
			{
				DoSave(fde.Filename, 
				       	null, 
				       	fde.FrameInterval, 
				       	_iSelStart, 
				       	_iSelEnd, 
				       	true, 
				       	fde.PausedVideo ? false : true,
				       	fde.PausedVideo,
				       	_DelegateOutputBitmap);
			}
			
			// Release configuration form.
			fde.Dispose();
		}
		public void AfterSave()
		{
			// Ask the Explorer tree to refresh itself, (but not the thumbnails pane.)
            DelegatesPool dp = DelegatesPool.Instance();
            if (dp.RefreshFileExplorer != null)
            {
                dp.RefreshFileExplorer(false);
            }
		}
		#endregion
		
		#region Saving processing
		private void DoSave(String _FilePath, Metadata _Metadata, double _fPlaybackFrameInterval, Int64 _iSelStart, Int64 _iSelEnd, bool _bFlushDrawings, bool _bKeyframesOnly, bool _bPausedVideo, DelegateGetOutputBitmap _DelegateOutputBitmap)
        {
			// Save video.
    		// We use a bgWorker and a Progress Bar.
    		
			// Memorize the parameters, they will be used later in bgWorkerSave_DoWork.
			// Note: _iSelStart, _iSelEnd, _Metadata, should ultimately be taken from the local members.
			m_iSaveStart = _iSelStart;
            m_iSaveEnd = _iSelEnd;
            m_SaveMetadata = _Metadata;
            m_SaveFile = _FilePath;
            m_fSaveFramesInterval = _fPlaybackFrameInterval;
            m_bSaveFlushDrawings = _bFlushDrawings;
            m_bSaveKeyframesOnly = _bKeyframesOnly;
            m_bSavePausedVideo = _bPausedVideo;
            m_SaveDelegateOutputBitmap = _DelegateOutputBitmap;
            
            // Instanciate and configure the bgWorker.
            BackgroundWorker bgWorkerSave = new BackgroundWorker();
            bgWorkerSave.WorkerReportsProgress = true;
        	bgWorkerSave.WorkerSupportsCancellation = true;
            bgWorkerSave.DoWork += new DoWorkEventHandler(bgWorkerSave_DoWork);
        	bgWorkerSave.ProgressChanged += new ProgressChangedEventHandler(bgWorkerSave_ProgressChanged);
            bgWorkerSave.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorkerSave_RunWorkerCompleted);

            // Attach the bgWorker to the VideoFile object so it can report progress.
            m_VideoFile.BgWorker = bgWorkerSave;
            
            // Create the progress bar and launch the worker.
            m_FormProgressBar = new formProgressBar(true);
            m_FormProgressBar.Cancel = Cancel_Asked;
        	bgWorkerSave.RunWorkerAsync();
        	m_FormProgressBar.ShowDialog();
		}
		private void bgWorkerSave_DoWork(object sender, DoWorkEventArgs e)
        {
        	// This is executed in Worker Thread space. (Do not call any UI methods)
        	
        	string metadata = "";
        	if(m_SaveMetadata != null)
        	{
        		// Get the metadata as XML string.
        		// If frame duplication is going to occur (when saving in slow motion at less than 8fps)
        		// We have to store this in the xml output to be able to match frames with timestamps later.
        		int iDuplicateFactor = (int)Math.Ceiling(m_fSaveFramesInterval / 125.0);
        		metadata = m_SaveMetadata.ToXmlString(iDuplicateFactor);
        	}
        	
        	try
        	{
        		m_SaveResult = m_VideoFile.Save(	m_SaveFile, 
	        	                                	m_fSaveFramesInterval, 
	        	                                	m_iSaveStart, 
	        	                                	m_iSaveEnd, 
	        	                                	metadata, 
	        	                                	m_bSaveFlushDrawings, 
	        	                                	m_bSaveKeyframesOnly,
	        	                                	m_bSavePausedVideo,
	        	                                	m_SaveDelegateOutputBitmap);
        		if(m_SaveMetadata != null)
        		{
        			m_SaveMetadata.CleanupHash();
        		}
        	}
        	catch (Exception exp)
			{
        		m_SaveResult = SaveResult.UnknownError;
				log.Error("Unknown error while saving video.");
				log.Error(exp.StackTrace);
			}
        	
        	e.Result = 0;
        }
		private void bgWorkerSave_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        	// This method should be called back from the VideoFile when a frame has been processed.
        	// call snippet : m_BackgroundWorker.ReportProgress(iCurrentValue, iMaximum);
        	
        	int iValue = (int)e.ProgressPercentage;
        	int iMaximum = (int)e.UserState;
            
            if (iValue > iMaximum) { iValue = iMaximum; }
        	
            m_FormProgressBar.Update(iValue, iMaximum, true);
        }
		private void bgWorkerSave_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        	m_FormProgressBar.Close();
        	m_FormProgressBar.Dispose();
        	
        	if(m_SaveResult != SaveResult.Success)
            {
            	ReportError(m_SaveResult);
            }
        	else
        	{
        		AfterSave();
        	}
        }
		private void ReportError(SaveResult _err)
        {
        	switch(_err)
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
		private void DisplayErrorMessage(string _err)
        {
        	MessageBox.Show(
        		_err.Replace("\\n", "\n"),
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
	        m_VideoFile.BgWorker.CancelAsync();
	        
	        // m_FormProgressBar.Dispose();
		}
		#endregion
	}
}
