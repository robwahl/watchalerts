//----------------------------------------------------------------
//
// Originally written by John Batte
// Modifications, API changes and cleanups by Phil Crosby
// http://codeproject.com/cs/library/downloader.asp
//
// This file is under the BSD Licence.
//
//-----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace Kinovea.Updater
{
    /// <summary>
    ///     Downloads and resumes files from HTTP, FTP, and File (file://) URLS
    /// </summary>
    public class FileDownloader
    {
        // Block size to download is by default 1K.
        private const int DownloadBlockSize = 1024;

        // Determines whether the user has canceled or not.
        private bool _canceled;

        /// <summary>
        ///     This is the name of the file we get back from the server when we
        ///     try to download the provided url. It will only contain a non-null
        ///     string when we've successfully contacted the server and it has started
        ///     sending us a file.
        /// </summary>
        public string DownloadingTo { get; private set; }

        /// <summary>
        ///     Proxy to be used for http and ftp requests.
        /// </summary>
        public IWebProxy Proxy { get; set; }

        public void Cancel()
        {
            _canceled = true;
        }

        /// <summary>
        ///     Progress update
        /// </summary>
        public event DownloadProgressHandler ProgressChanged;

        /// <summary>
        ///     Fired when progress reaches 100%.
        /// </summary>
        public event EventHandler DownloadComplete;

        private void OnDownloadComplete()
        {
            if (DownloadComplete != null)
                DownloadComplete(this, new EventArgs());
        }

        /// <summary>
        ///     Begin downloading the file at the specified url, and save it to the current folder.
        /// </summary>
        public void Download(string url)
        {
            Download(url, "");
        }

        /// <summary>
        ///     Begin downloading the file at the specified url, and save it to the given folder.
        /// </summary>
        public void Download(string url, string destFolder)
        {
            DownloadData data = null;
            _canceled = false;

            try
            {
                // get download details
                data = DownloadData.Create(url, destFolder, Proxy);
                // Find out the name of the file that the web server gave us.
                var destFileName = Path.GetFileName(data.Response.ResponseUri.ToString());

                // The place we're downloading to (not from) must not be a URI,
                // because Path and File don't handle them...
                destFolder = destFolder.Replace("file:///", "").Replace("file://", "");
                DownloadingTo = Path.Combine(destFolder, destFileName);

                // Create the file on disk here, so even if we don't receive any data of the file
                // it's still on disk. This allows us to download 0-byte files.
                if (!File.Exists(DownloadingTo))
                {
                    var fs = File.Create(DownloadingTo);
                    fs.Close();
                }

                // create the download buffer
                var buffer = new byte[DownloadBlockSize];

                int readCount;

                // update how many bytes have already been read
                var totalDownloaded = data.StartPoint;

                var gotCanceled = false;

                // boucle de téléchargement
                while ((readCount = data.DownloadStream.Read(buffer, 0, DownloadBlockSize)) > 0)
                {
                    // break on cancel
                    if (_canceled)
                    {
                        gotCanceled = true;
                        data.Close();
                        break;
                    }

                    // update total bytes read
                    totalDownloaded += readCount;

                    // save block to end of file
                    SaveToFile(buffer, readCount, DownloadingTo);

                    // send progress info
                    if (data.IsProgressKnown)
                        RaiseProgressChanged(totalDownloaded, data.FileSize);

                    // break on cancel
                    if (_canceled)
                    {
                        gotCanceled = true;
                        data.Close();
                        break;
                    }
                }

                if (!gotCanceled)
                    OnDownloadComplete();
            }
            catch (UriFormatException e)
            {
                throw new ArgumentException(
                    string.Format("Could not parse the URL \"{0}\" - it's either malformed or is an unknown protocol.",
                        url), e);
            }
            finally
            {
                if (data != null)
                    data.Close();
            }
        }

        /// <summary>
        ///     Download a file from a list or URLs. If downloading from one of the URLs fails,
        ///     another URL is tried.
        /// </summary>
        public void Download(List<string> urlList)
        {
            Download(urlList, "");
        }

        /// <summary>
        ///     Download a file from a list or URLs. If downloading from one of the URLs fails,
        ///     another URL is tried.
        /// </summary>
        public void Download(List<string> urlList, string destFolder)
        {
            // validate input
            if (urlList == null)
                throw new ArgumentException("Url list not specified.");

            if (urlList.Count == 0)
                throw new ArgumentException("Url list empty.");

            // try each url in the list.
            // if one succeeds, we are done.
            // if any fail, move to the next.
            Exception ex = null;
            foreach (var s in urlList)
            {
                ex = null;
                try
                {
                    Download(s, destFolder);
                }
                catch (Exception e)
                {
                    ex = e;
                }
                // If we got through that without an exception, we found a good url
                if (ex == null)
                    break;
            }
            if (ex != null)
                throw ex;
        }

        /// <summary>
        ///     Asynchronously download a file from the url.
        /// </summary>
        public void AsyncDownload(string url)
        {
            ThreadPool.QueueUserWorkItem(
                WaitCallbackMethod, new[] { url, "" });
        }

        /// <summary>
        ///     Asynchronously download a file from the url to the destination folder.
        /// </summary>
        public void AsyncDownload(string url, string destFolder)
        {
            ThreadPool.QueueUserWorkItem(
                WaitCallbackMethod, new[] { url, destFolder });
        }

        /// <summary>
        ///     Asynchronously download a file from a list or URLs. If downloading from one of the URLs fails,
        ///     another URL is tried.
        /// </summary>
        public void AsyncDownload(List<string> urlList, string destFolder)
        {
            ThreadPool.QueueUserWorkItem(
                WaitCallbackMethod, new object[] { urlList, destFolder });
        }

        /// <summary>
        ///     Asynchronously download a file from a list or URLs. If downloading from one of the URLs fails,
        ///     another URL is tried.
        /// </summary>
        public void AsyncDownload(List<string> urlList)
        {
            ThreadPool.QueueUserWorkItem(
                WaitCallbackMethod, new object[] { urlList, "" });
        }

        /// <summary>
        ///     A WaitCallback used by the AsyncDownload methods.
        /// </summary>
        private void WaitCallbackMethod(object data)
        {
            // Can either be a string array of two strings (url and dest folder),
            // or an object array containing a list<string> and a dest folder
            var strings1 = data as string[];
            if (strings1 != null)
            {
                var strings = strings1;
                Download(strings[0], strings[1]);
            }
            else
            {
                var list = data as object[];
                if (list != null)
                {
                    var urlList = list[0] as List<string>;
                    var destFolder = list[1] as string;
                    Download(urlList, destFolder);
                }
            }
        }

        private void SaveToFile(byte[] buffer, int count, string fileName)
        {
            FileStream f = null;

            try
            {
                f = File.Open(fileName, FileMode.Append, FileAccess.Write);
                f.Write(buffer, 0, count);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(
                    string.Format("Error trying to save file \"{0}\": {1}", fileName, e.Message), e);
            }
            finally
            {
                if (f != null)
                    f.Close();
            }
        }

        private void RaiseProgressChanged(long current, long target)
        {
            if (ProgressChanged != null)
                ProgressChanged(this, new DownloadEventArgs(target, current));
        }
    }

    /// <summary>
    ///     Constains the connection to the file server and other statistics about a file
    ///     that's downloading.
    /// </summary>
    internal class DownloadData
    {
        private IWebProxy _proxy;
        private Stream _stream;

        // Used by the factory method
        private DownloadData()
        {
        }

        public static DownloadData Create(string url, string destFolder)
        {
            return Create(url, destFolder, null);
        }

        public static DownloadData Create(string url, string destFolder, IWebProxy proxy)
        {
            // This is what we will return
            var downloadData = new DownloadData { _proxy = proxy };

            var urlSize = downloadData.GetFileSize(url);
            downloadData.FileSize = urlSize;

            var req = downloadData.GetRequest(url);
            try
            {
                downloadData.Response = req.GetResponse();
            }
            catch (Exception e)
            {
                throw new ArgumentException(string.Format(
                    "Error downloading \"{0}\": {1}", url, e.Message), e);
            }

            // Check to make sure the response isn't an error. If it is this method
            // will throw exceptions.
            ValidateResponse(downloadData.Response, url);

            // Take the name of the file given to use from the web server.
            var fileName = Path.GetFileName(downloadData.Response.ResponseUri.ToString());

            var downloadTo = Path.Combine(destFolder, fileName);

            // If we don't know how big the file is supposed to be,
            // we can't resume, so delete what we already have if something is on disk already.
            if (!downloadData.IsProgressKnown && File.Exists(downloadTo))
                File.Delete(downloadTo);

            if (downloadData.IsProgressKnown && File.Exists(downloadTo))
            {
                // We only support resuming on http requests
                if (!(downloadData.Response is HttpWebResponse))
                {
                    File.Delete(downloadTo);
                }
                else
                {
                    // Try and start where the file on disk left off
                    downloadData.StartPoint = new FileInfo(downloadTo).Length;

                    // If we have a file that's bigger than what is online, then something
                    // strange happened. Delete it and start again.
                    if (downloadData.StartPoint > urlSize)
                        File.Delete(downloadTo);
                    else if (downloadData.StartPoint < urlSize)
                    {
                        // Try and resume by creating a new request with a new start position
                        downloadData.Response.Close();
                        req = downloadData.GetRequest(url);
                        ((HttpWebRequest)req).AddRange((int)downloadData.StartPoint);
                        downloadData.Response = req.GetResponse();

                        if (((HttpWebResponse)downloadData.Response).StatusCode != HttpStatusCode.PartialContent)
                        {
                            // They didn't support our resume request.
                            File.Delete(downloadTo);
                            downloadData.StartPoint = 0;
                        }
                    }
                }
            }
            return downloadData;
        }

        /// <summary>
        ///     Checks whether a WebResponse is an error.
        /// </summary>
        private static void ValidateResponse(WebResponse response, string url)
        {
            var webResponse = response as HttpWebResponse;
            if (webResponse != null)
            {
                using (var httpResponse = webResponse)
                {
                    if (httpResponse.ContentType.Contains("text/html") ||
                        httpResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new ArgumentException(
                            string.Format("Could not download \"{0}\" - a web page was returned from the web server.",
                                url));
                    }
                }
            }
            else
            {
                var ftpWebResponse = response as FtpWebResponse;
                if (ftpWebResponse != null)
                {
                    var ftpResponse = ftpWebResponse;
                    if (ftpResponse.StatusCode == FtpStatusCode.ConnectionClosed)
                        throw new ArgumentException(
                            string.Format("Could not download \"{0}\" - FTP server closed the connection.", url));
                }
            }
            // FileWebResponse doesn't have a status code to check.
        }

        /// <summary>
        ///     Checks the file size of a remote file. If size is -1, then the file size
        ///     could not be determined.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private long GetFileSize(string url)
        {
            WebResponse response = null;
            long size = -1;
            try
            {
                response = GetRequest(url).GetResponse();
                size = response.ContentLength;
            }
            catch
            {
                // Erreur 404 for exemple... What to do ?
            }
            finally
            {
                if (response != null)
                    response.Close();
            }

            return size;
        }

        private WebRequest GetRequest(string url)
        {
            //WebProxy proxy = WebProxy.GetDefaultProxy();
            var request = WebRequest.Create(url);
            if (request is HttpWebRequest)
            {
                request.Credentials = CredentialCache.DefaultCredentials;
                if (request.Proxy != null) request.Proxy.GetProxy(new Uri("http://www.google.com"));
            }

            if (_proxy != null)
            {
                request.Proxy = _proxy;
            }

            return request;
        }

        public void Close()
        {
            Response.Close();
        }

        #region Properties

        public WebResponse Response { get; set; }

        public Stream DownloadStream
        {
            get
            {
                if (StartPoint == FileSize)
                    return Stream.Null;
                return _stream ?? (_stream = Response.GetResponseStream());
            }
        }

        public long FileSize { get; private set; }

        public long StartPoint { get; private set; }

        public bool IsProgressKnown
        {
            get
            {
                // If the size of the remote url is -1, that means we
                // couldn't determine it, and so we don't know
                // progress information.
                return FileSize > -1;
            }
        }

        #endregion Properties
    }

    /// <summary>
    ///     Progress of a downloading file.
    /// </summary>
    public class DownloadEventArgs : EventArgs
    {
        private readonly string _downloadState;
        private readonly int _percentDone;

        public DownloadEventArgs(long totalFileSize, long currentFileSize)
        {
            TotalFileSize = totalFileSize;
            CurrentFileSize = currentFileSize;

            _percentDone = (int)((((double)currentFileSize) / totalFileSize) * 100);
        }

        public DownloadEventArgs(string state)
        {
            _downloadState = state;
        }

        public DownloadEventArgs(int percentDone, string state)
        {
            _percentDone = percentDone;
            _downloadState = state;
        }

        public long TotalFileSize { get; set; }

        public long CurrentFileSize { get; set; }

        public int PercentDone
        {
            get { return _percentDone; }
        }

        public string DownloadState
        {
            get { return _downloadState; }
        }
    }

    public delegate void DownloadProgressHandler(object sender, DownloadEventArgs e);
}