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

using Kinovea.Services;
using Kinovea.Updater.Languages;
using log4net;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.Updater
{
    /// <summary>
    ///     This is the simplified update dialog.
    ///     It just handles the update of the software itself.
    ///     We only come here if there is an actual update available.
    /// </summary>
    public partial class UpdateDialog2 : Form
    {
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Hide();
            Log.Info("Download cancelled.");

            // Cancel the ongoing download if any.
            // Todo: remove the partially downloaded file?
            if (_mBDownloadStarted && _mDownloader != null)
            {
                _mBDownloadStarted = false;
                _mDownloader.Cancel();
            }

            Close();
        }

        #region Delegate

        public delegate void CallbackUpdateProgressBar(int percentDone);

        public delegate void CallbackDownloadComplete(int result);

        private readonly CallbackUpdateProgressBar _mCallbackUpdateProgressBar;
        private readonly CallbackDownloadComplete _mCallbackDownloadComplete;

        #endregion Delegate

        #region Members

        private readonly HelpIndex _mHiRemote;
        private readonly ThreePartsVersion _mCurrentVersion;
        private readonly FileDownloader _mDownloader = new FileDownloader();
        private bool _mBDownloadStarted;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructors & initialisation

        public UpdateDialog2(HelpIndex hiRemote)
        {
            _mHiRemote = hiRemote;
            _mCurrentVersion = new ThreePartsVersion(PreferencesManager.ReleaseVersion);

            InitializeComponent();

            _mDownloader.DownloadComplete += downloader_DownloadedComplete;
            _mDownloader.ProgressChanged += downloader_ProgressChanged;

            _mCallbackUpdateProgressBar = UpdateProgressBar;
            _mCallbackDownloadComplete = DownloadComplete;

            InitDialog();
        }

        private void InitDialog()
        {
            Text = "   " + UpdaterLang.Updater_Title;

            btnCancel.Text = UpdaterLang.Updater_Quit;
            btnDownload.Text = UpdaterLang.Updater_Download;
            labelInfos.Text = UpdaterLang.Updater_Behind;

            lblNewVersion.Text = string.Format("{0}: {1} - ({2} {3}).",
                UpdaterLang.Updater_NewVersion,
                _mHiRemote.AppInfos.Version,
                UpdaterLang.Updater_CurrentVersion,
                _mCurrentVersion);

            lblNewVersionFileSize.Text = string.Format("{0} {1:0.00} {2}",
                UpdaterLang.Updater_FileSize,
                (double)_mHiRemote.AppInfos.FileSizeInBytes / (1024 * 1024),
                UpdaterLang.Updater_MegaBytes);

            lblChangeLog.Text = UpdaterLang.Updater_LblChangeLog;

            rtbxChangeLog.Clear();
            if (_mHiRemote.AppInfos.ChangelogLocation.Length > 0)
            {
                var request = (HttpWebRequest)WebRequest.Create(_mHiRemote.AppInfos.ChangelogLocation);
                var response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK && response.ContentLength > 0)
                {
                    TextReader reader = new StreamReader(response.GetResponseStream());

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        rtbxChangeLog.AppendText("\n");
                        rtbxChangeLog.AppendText(line);
                    }
                }
            }

            // website link.
            lnkKinovea.Links.Clear();
            var lnkTarget = "http://www.kinovea.org";
            lnkKinovea.Links.Add(0, lnkKinovea.Text.Length, lnkTarget);
            toolTip1.SetToolTip(lnkKinovea, lnkTarget);
        }

        #endregion Constructors & initialisation

        #region Download

        private void btnDownload_Click(object sender, EventArgs e)
        {
            // Get a destination folder.
            folderBrowserDialog.Description = UpdaterLang.Updater_BrowseFolderDescription;
            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                // Start download.
                labelInfos.Visible = false;
                btnDownload.Visible = false;
                progressDownload.Visible = true;
                progressDownload.Left = 109;
                progressDownload.Width = 367;
                _mBDownloadStarted = true;
                Log.Info("Starting the download of a new version.");
                _mDownloader.AsyncDownload(_mHiRemote.AppInfos.FileLocation, folderBrowserDialog.SelectedPath);
            }
        }

        private void downloader_ProgressChanged(object sender, DownloadEventArgs e)
        {
            // (In WorkerThread space)
            if (_mBDownloadStarted)
            {
                BeginInvoke(_mCallbackUpdateProgressBar, e.PercentDone);
            }
        }

        private void downloader_DownloadedComplete(object sender, EventArgs e)
        {
            // (In WorkerThread space)
            if (_mCallbackDownloadComplete != null)
            {
                BeginInvoke(_mCallbackDownloadComplete, 0);
            }
        }

        private void UpdateProgressBar(int iPercentDone)
        {
            // In UI thread space.
            progressDownload.Value = iPercentDone;
        }

        private void DownloadComplete(int iResult)
        {
            // In UI thread space.
            Hide();
            Log.Info("Download of the new version complete.");
            _mBDownloadStarted = false;
            _mDownloader.DownloadComplete -= downloader_DownloadedComplete;
            DialogResult = DialogResult.OK;

            MessageBox.Show(UpdaterLang.Updater_mboxDownloadSuccess_Description.Replace("\\n", "\n"),
                UpdaterLang.Updater_Title,
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            Close();
        }

        #endregion Download
    }
}