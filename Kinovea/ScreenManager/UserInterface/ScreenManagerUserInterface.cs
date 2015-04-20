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
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public partial class ScreenManagerUserInterface : UserControl
    {
        public ThumbListView MThumbsViewer = new ThumbListView();

        public ScreenManagerUserInterface(IScreenManagerUiContainer screenManagerUiContainer)
        {
            Log.Debug("Constructing ScreenManagerUserInterface.");

            _mScreenManagerUiContainer = screenManagerUiContainer;

            InitializeComponent();
            ComCtrls.ScreenManagerUiContainer = _mScreenManagerUiContainer;
            MThumbsViewer.SetScreenManagerUiContainer(_mScreenManagerUiContainer);

            BackColor = Color.White;
            Dock = DockStyle.Fill;

            MThumbsViewer.Top = 0;
            MThumbsViewer.Left = 0;
            MThumbsViewer.Width = Width;
            MThumbsViewer.Height = Height - pbLogo.Height - 10;
            MThumbsViewer.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            MThumbsViewer.Closing += ThumbsViewer_Closing;
            Controls.Add(MThumbsViewer);

            MDelegateUpdateTrkFrame = UpdateTrkFrame;

            // Registers our exposed functions to the DelegatePool.
            var dp = DelegatesPool.Instance();
            dp.DisplayThumbnails = DoDisplayThumbnails;

            // Thumbs are enabled by default.
            MThumbsViewer.Visible = true;
            _mBThumbnailsWereVisible = true;
            MThumbsViewer.BringToFront();

            pnlScreens.BringToFront();
            pnlScreens.Dock = DockStyle.Fill;

            Application.Idle += IdleDetector;
        }

        private void IdleDetector(object sender, EventArgs e)
        {
            Log.Debug("Application is idle in ScreenManagerUserInterface.");

            // This is a one time only routine.
            Application.Idle -= IdleDetector;

            // Launch file.
            var filePath = CommandLineArgumentManager.Instance().InputFile;
            if (filePath != null && File.Exists(filePath))
            {
                _mScreenManagerUiContainer.DropLoadMovie(filePath, -1);
            }
        }

        private void pnlScreens_Resize(object sender, EventArgs e)
        {
            // Reposition Common Controls panel so it doesn't take
            // more space than necessary.
            splitScreensPanel.SplitterDistance = pnlScreens.Height - 50;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            // Hide Common Controls Panel
            IUndoableCommand ctcc = new CommandToggleCommonControls(splitScreensPanel);
            var cm = CommandManager.Instance();
            cm.LaunchUndoableCommand(ctcc);
        }

        private void ScreenManagerUserInterface_DoubleClick(object sender, EventArgs e)
        {
            var dp = DelegatesPool.Instance();
            if (dp.OpenVideoFile != null)
            {
                dp.OpenVideoFile();
            }
        }

        private void btnShowThumbView_Click(object sender, EventArgs e)
        {
            MThumbsViewer.Visible = true;
            Cursor = Cursors.WaitCursor;
            MThumbsViewer.DisplayThumbnails(_mFolderFileNames);
            Cursor = Cursors.Default;
        }

        private void ThumbsViewer_Closing(object sender, EventArgs e)
        {
            MThumbsViewer.Visible = false;
            _mBThumbnailsWereVisible = false;
        }

        private void DoDisplayThumbnails(List<string> fileNames, bool bRefreshNow)
        {
            // Keep track of the files, in case we need to bring them back
            // after closing a screen.
            _mFolderFileNames = fileNames;

            if (bRefreshNow)
            {
                if (fileNames.Count > 0)
                {
                    MThumbsViewer.Height = Height - 20; // margin for cosmetic
                    btnShowThumbView.Visible = true;

                    // We keep the Kinovea logo until there is at least 1 thumbnail to show.
                    // After that we never display it again.
                    pbLogo.Visible = false;
                }
                else
                {
                    // If no thumbs are to be displayed, enable the drag & drop and double click on background.
                    MThumbsViewer.Height = 1;
                    btnShowThumbView.Visible = false;

                    // TODO: info message.
                    //"No files to display in this folder."
                }

                if (MThumbsViewer.Visible)
                {
                    Cursor = Cursors.WaitCursor;
                    MThumbsViewer.DisplayThumbnails(fileNames);
                    Cursor = Cursors.Default;
                }
                else if (_mBThumbnailsWereVisible)
                {
                    // Thumbnail pane was hidden to show player screen
                    // Then we changed folder and we don't have anything to show.
                    // Let's clean older thumbnails now.
                    MThumbsViewer.CleanupThumbnails();
                }
            }
        }

        public void CloseThumbnails()
        {
            // This happens when the Thumbnail view is closed by another component
            // (e.g: When we need to show screens)
            Log.Debug("Closing thumbnails to display screen.");
            if (MThumbsViewer.Visible)
            {
                _mBThumbnailsWereVisible = true;
            }

            MThumbsViewer.Visible = false;
        }

        public void BringBackThumbnails()
        {
            if (_mBThumbnailsWereVisible)
            {
                MThumbsViewer.Visible = true;
                Cursor = Cursors.WaitCursor;
                MThumbsViewer.DisplayThumbnails(_mFolderFileNames);
                Cursor = Cursors.Default;
            }
        }

        #region Delegates

        public delegate void DelegateUpdateTrkFrame(int iFrame);

        public DelegateUpdateTrkFrame MDelegateUpdateTrkFrame;

        #endregion Delegates

        #region Members

        private List<string> _mFolderFileNames = new List<string>();
        private bool _mBThumbnailsWereVisible;
        private readonly IScreenManagerUiContainer _mScreenManagerUiContainer;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region public, called from Kernel

        public void RefreshUiCulture()
        {
            ComCtrls.RefreshUiCulture();
            btnShowThumbView.Text = ScreenManagerLang.btnShowThumbView;
            MThumbsViewer.RefreshUiCulture();
        }

        public void DisplaySyncLag(int iOffset)
        {
            ComCtrls.SyncOffset = iOffset;
        }

        public void UpdateSyncPosition(int iPosition)
        {
            ComCtrls.trkFrame.UpdateSyncPointMarker(iPosition);
            ComCtrls.trkFrame.Invalidate();
        }

        public void SetupTrkFrame(int iMinimum, int iMaximum, int iPosition)
        {
            ComCtrls.trkFrame.Minimum = iMinimum;
            ComCtrls.trkFrame.Maximum = iMaximum;
            ComCtrls.trkFrame.Position = iPosition;
        }

        public void UpdateTrkFrame(int iPosition)
        {
            ComCtrls.trkFrame.Position = iPosition;
        }

        public void OrganizeMenuProxy(Delegate method)
        {
            method.DynamicInvoke();
        }

        public void DisplayAsPaused()
        {
            ComCtrls.Playing = false;
        }

        #endregion public, called from Kernel

        #region DragDrop

        private void ScreenManagerUserInterface_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = _mScreenManagerUiContainer.GetDragDropEffects(-1);
        }

        private void ScreenManagerUserInterface_DragDrop(object sender, DragEventArgs e)
        {
            CommitDrop(e, -1);
        }

        private void splitScreens_Panel1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = _mScreenManagerUiContainer.GetDragDropEffects(0);
        }

        private void splitScreens_Panel1_DragDrop(object sender, DragEventArgs e)
        {
            CommitDrop(e, 1);
        }

        private void splitScreens_Panel2_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = _mScreenManagerUiContainer.GetDragDropEffects(1);
        }

        private void splitScreens_Panel2_DragDrop(object sender, DragEventArgs e)
        {
            CommitDrop(e, 2);
        }

        private void CommitDrop(DragEventArgs e, int iScreen)
        {
            //-----------------------------------------------------------
            // An object has been dropped.
            // Support drag & drop from the FileExplorer module (listview)
            // or from the Windows Explorer.
            // Not between screens.
            //-----------------------------------------------------------
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                // String. Coming from the file explorer.
                var filePath = (string)e.Data.GetData(DataFormats.StringFormat);
                _mScreenManagerUiContainer.DropLoadMovie(filePath, iScreen);
            }
            else if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // File. Coming from Windows Explorer.
                var fileArray = (Array)e.Data.GetData(DataFormats.FileDrop);

                if (fileArray != null)
                {
                    //----------------------------------------------------------------
                    // Extract string from first array element
                    // (ignore all files except first if number of files are dropped).
                    //----------------------------------------------------------------
                    var filePath = fileArray.GetValue(0).ToString();
                    _mScreenManagerUiContainer.DropLoadMovie(filePath, iScreen);
                }
            }
        }

        #endregion DragDrop
    }
}