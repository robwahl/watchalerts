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
using Kinovea.ScreenManager.Properties;
using Kinovea.Services;
using Microsoft.VisualBasic.FileIO;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     Represent a recently saved video as a thumbnail.
    ///     (saved during the live of a Capture Screen)
    ///     The box display a thumbnail of the video and allows the user to change
    ///     the file name of the video and to launch it in a PlayerScreen.
    /// </summary>
    public partial class CapturedVideoBox : UserControl
    {
        #region Properties

        public string FilePath
        {
            get { return _mCapturedVideo.Filepath; }
        }

        #endregion Properties

        public void RefreshUiCulture()
        {
            ReloadMenusCulture();
        }

        #region Events

        [Category("Action"), Browsable(true)]
        public event EventHandler CloseThumb;

        [Category("Action"), Browsable(true)]
        public event EventHandler ClickThumb;

        [Category("Action"), Browsable(true)]
        public event EventHandler LaunchVideo;

        #endregion Events

        #region Members

        private readonly CapturedVideo _mCapturedVideo;

        #region Context menu

        private readonly ContextMenuStrip _popMenu = new ContextMenuStrip();
        private readonly ToolStripMenuItem _mnuLoadVideo = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuHide = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuDelete = new ToolStripMenuItem();

        #endregion Context menu

        #endregion Members

        #region Construction & initialization

        public CapturedVideoBox(CapturedVideo cv)
        {
            _mCapturedVideo = cv;
            InitializeComponent();

            btnClose.Parent = pbThumbnail;
            tbTitle.Text = Path.GetFileName(_mCapturedVideo.Filepath);

            BuildContextMenus();
            ReloadMenusCulture();
        }

        private void BuildContextMenus()
        {
            _mnuLoadVideo.Click += mnuLoadVideo_Click;
            _mnuLoadVideo.Image = Resources.film_go;
            _mnuHide.Click += mnuHide_Click;
            _mnuHide.Image = Resources.hide;
            _mnuDelete.Click += mnuDelete_Click;
            _mnuDelete.Image = Resources.delete;
            _popMenu.Items.AddRange(new ToolStripItem[] { _mnuLoadVideo, new ToolStripSeparator(), _mnuHide, _mnuDelete });
            ContextMenuStrip = _popMenu;
        }

        #endregion Construction & initialization

        #region Event Handlers - Mouse Enter / Leave

        private void pbThumbnail_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                DoDragDrop(_mCapturedVideo.Filepath, DragDropEffects.Copy);
            }
        }

        private void Controls_MouseEnter(object sender, EventArgs e)
        {
            ShowButtons();
        }

        private void Controls_MouseLeave(object sender, EventArgs e)
        {
            // We hide the close button only if we left the whole control.
            var clientMouse = PointToClient(MousePosition);
            if (!pbThumbnail.ClientRectangle.Contains(clientMouse))
            {
                HideButtons();
            }
        }

        #endregion Event Handlers - Mouse Enter / Leave

        #region Event Handlers - Buttons / Text

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (CloseThumb != null) CloseThumb(this, e);
        }

        private void pbThumbnail_Click(object sender, EventArgs e)
        {
            if (ClickThumb != null) ClickThumb(this, e);
        }

        private void pbThumbnail_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (LaunchVideo != null) LaunchVideo(this, e);
        }

        #endregion Event Handlers - Buttons / Text

        #region Event Handlers - Menu

        private void mnuLoadVideo_Click(object sender, EventArgs e)
        {
            if (LaunchVideo != null) LaunchVideo(this, e);
        }

        private void mnuHide_Click(object sender, EventArgs e)
        {
            if (CloseThumb != null) CloseThumb(this, e);
        }

        private void mnuDelete_Click(object sender, EventArgs e)
        {
            // Use the built-in dialogs to confirm (or not).
            // Delete is done through moving to recycle bin.
            if (File.Exists(_mCapturedVideo.Filepath))
            {
                try
                {
                    FileSystem.DeleteFile(_mCapturedVideo.Filepath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                }
                catch (OperationCanceledException)
                {
                    // User cancelled confirmation box.
                }

                // Other possible error case: the file couldn't be deleted because it's still in use.

                // If file was effectively moved to trash, hide the thumb and reload the folder.
                if (!File.Exists(_mCapturedVideo.Filepath))
                {
                    if (CloseThumb != null) CloseThumb(this, e);

                    // Ask the Explorer tree to refresh itself...
                    // This will in turn refresh the thumbnails pane.
                    var dp = DelegatesPool.Instance();
                    if (dp.RefreshFileExplorer != null)
                    {
                        dp.RefreshFileExplorer(true);
                    }
                }
            }
        }

        #endregion Event Handlers - Menu

        #region Private helpers

        private void ReloadMenusCulture()
        {
            // Reload the text for each menu.
            // this is done at construction time and at RefreshUICulture time.
            _mnuLoadVideo.Text = ScreenManagerLang.mnuThumbnailPlay;
            _mnuHide.Text = ScreenManagerLang.mnuGridsHide;
            _mnuDelete.Text = ScreenManagerLang.mnuThumbnailDelete;
        }

        private void ShowButtons()
        {
            btnClose.Visible = true;
        }

        private void HideButtons()
        {
            btnClose.Visible = false;
        }

        #endregion Private helpers
    }
}