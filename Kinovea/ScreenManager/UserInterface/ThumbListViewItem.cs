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
using Kinovea.ScreenManager.Properties;
using Kinovea.Services;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     Thumbnail control.
    /// </summary>
    public partial class ThumbListViewItem : UserControl
    {
        #region Events

        [Category("Action"), Browsable(true)]
        public event EventHandler LaunchVideo;

        [Category("Action"), Browsable(true)]
        public event EventHandler VideoSelected;

        [Category("Action"), Browsable(true)]
        public event EventHandler<EditingEventArgs> FileNameEditing;

        #endregion Events

        #region Properties

        public string FileName
        {
            get { return _mFileName; }
            set
            {
                _mFileName = value;
                lblFileName.Text = Path.GetFileNameWithoutExtension(_mFileName);
                if (ToolTipHandler != null)
                {
                    ToolTipHandler.SetToolTip(picBox, Path.GetFileNameWithoutExtension(_mFileName));
                }
            }
        }

        public ToolTip ToolTipHandler { get; set; }

        public bool ErrorImage { get; set; }

        public List<Bitmap> Thumbnails
        {
            get { return _mBitmaps; } // unused.
            set
            {
                _mBitmaps = value;
                if (_mBitmaps != null)
                {
                    if (_mBitmaps.Count > 0)
                    {
                        _mICurrentThumbnailIndex = 0;
                        _mCurrentThumbnail = _mBitmaps[_mICurrentThumbnailIndex];
                    }
                }

                SetSize(Width);
            }
        }

        public Bitmap Thumbnail
        {
            get { return _mCurrentThumbnail; } // unused.
            set
            {
                _mCurrentThumbnail = value;
                SetSize(Width);
            }
        }

        public string Duration { get;
// unused.
            set; } = "0:00:00";

        public Size ImageSize
        {
            set { _mImageSize = string.Format("{0}×{1}", value.Width, value.Height); }
        }

        public bool IsImage { get; set; }

        public bool HasKva { get; set; }

        #endregion Properties

        #region Members

        private string _mFileName;
        private bool _mBIsSelected;
        private List<Bitmap> _mBitmaps;
        private Bitmap _mCurrentThumbnail;
        private string _mImageSize = "";
        private string _mImageText;
        private int _mICurrentThumbnailIndex;
        private bool _mHovering;
        private readonly Bitmap _bmpKvaAnalysis = Resources.bullet_white;
        private readonly Timer _tmrThumbs = new Timer();

        #region Context menu

        private readonly ContextMenuStrip _popMenu = new ContextMenuStrip();
        private readonly ToolStripMenuItem _mnuLaunch = new ToolStripMenuItem();
        private readonly ToolStripSeparator _mnuSep = new ToolStripSeparator();
        private readonly ToolStripMenuItem _mnuRename = new ToolStripMenuItem();
        private readonly ToolStripMenuItem _mnuDelete = new ToolStripMenuItem();

        #endregion Context menu

        private bool _mBEditMode;

        private static readonly int MIFilenameMaxCharacters = 18;
        private static readonly int MITimerInterval = 700;
        private static readonly Pen MPenSelected = new Pen(Color.DodgerBlue, 2);
        private static readonly Pen MPenUnselected = new Pen(Color.Silver, 2);
        private static readonly Pen MPenShadow = new Pen(Color.Lavender, 2);
        private static readonly Font MFontDuration = new Font("Arial", 8, FontStyle.Bold);

        private static readonly SolidBrush MBrushQuickPreviewActive =
            new SolidBrush(Color.FromArgb(128, Color.SteelBlue));

        private static readonly SolidBrush MBrushQuickPreviewInactive =
            new SolidBrush(Color.FromArgb(128, Color.LightSteelBlue));

        private static readonly SolidBrush MBrushDuration = new SolidBrush(Color.FromArgb(150, Color.Black));
        private readonly Pen _mPenDuration = new Pen(MBrushDuration);

        #endregion Members

        #region Construction & initialization

        public ThumbListViewItem()
        {
            InitializeComponent();
            BackColor = Color.White;
            picBox.BackgroundImage = null;

            // Setup timer
            _tmrThumbs.Interval = MITimerInterval;
            _tmrThumbs.Tick += tmrThumbs_Tick;
            _mICurrentThumbnailIndex = 0;

            _mPenDuration.StartCap = LineCap.Round;
            _mPenDuration.Width = 14;

            // Make the editbox follow the same layout pattern than the label.
            // except that its minimal height is depending on font.
            tbFileName.Left = lblFileName.Left;
            tbFileName.Width = lblFileName.Width;
            tbFileName.Top = Height - tbFileName.Height;
            tbFileName.Anchor = lblFileName.Anchor;

            BuildContextMenus();
            RefreshUiCulture();
        }

        private void BuildContextMenus()
        {
            _mnuLaunch.Image = Resources.film_go;
            _mnuLaunch.Click += mnuLaunch_Click;
            _mnuRename.Image = Resources.rename;
            _mnuRename.Click += mnuRename_Click;
            _mnuDelete.Image = Resources.delete;
            _mnuDelete.Click += mnuDelete_Click;
            _popMenu.Items.AddRange(new ToolStripItem[] {_mnuLaunch, _mnuSep, _mnuRename, _mnuDelete});
            ContextMenuStrip = _popMenu;
        }

        #endregion Construction & initialization

        #region Public interface

        public void SetSize(int iWidth)
        {
            // Called at init step and on resize..

            // Width changed due to screen resize or thumbview mode change.
            Width = iWidth;
            Height = ((Width*3)/4) + 15;

            // picBox is ratio strecthed.
            if (_mCurrentThumbnail != null)
            {
                var iDoubleMargin = 6;

                var fWidthRatio = (float) _mCurrentThumbnail.Width/(Width - iDoubleMargin);
                var fHeightRatio = (float) _mCurrentThumbnail.Height/(Height - 15 - iDoubleMargin);
                if (fWidthRatio > fHeightRatio)
                {
                    picBox.Width = Width - iDoubleMargin;
                    picBox.Height = (int) (_mCurrentThumbnail.Height/fWidthRatio);
                }
                else
                {
                    picBox.Width = (int) (_mCurrentThumbnail.Width/fHeightRatio);
                    picBox.Height = Height - 15 - iDoubleMargin;
                }

                // Center back.
                picBox.Left = 3 + (Width - iDoubleMargin - picBox.Width)/2;
                picBox.Top = 3 + (Height - iDoubleMargin - 15 - picBox.Height)/2;
            }
            else
            {
                picBox.Height = (picBox.Width*3)/4;
            }

            // File name may have to be hidden if not enough room.
            lblFileName.Visible = (Width >= 110);

            picBox.Invalidate();
        }

        public void DisplayAsError()
        {
            // Called only at init step.
            picBox.BackColor = Color.WhiteSmoke;
            lblFileName.ForeColor = Color.Silver;
            picBox.BackgroundImage = Resources.missing3;
            picBox.BackgroundImageLayout = ImageLayout.Center;
            picBox.Cursor = Cursors.No;
            ErrorImage = true;
            _mnuLaunch.Visible = false;
            _mnuSep.Visible = false;
        }

        public void SetUnselected()
        {
            // This method does NOT trigger an event to notify the container.
            _mBIsSelected = false;
            picBox.Invalidate();
        }

        public void SetSelected()
        {
            // This method triggers an event to notify the container.
            if (!_mBIsSelected)
            {
                _mBIsSelected = true;
                picBox.Invalidate();

                // Report change in selection
                if (VideoSelected != null)
                {
                    VideoSelected(this, EventArgs.Empty);
                }
            }
        }

        public void CancelEditMode()
        {
            // Called from the container when we click nowhere.
            // Do not call QuitEditMode here, as we may be entering as a result of that.
            if (_mBEditMode)
            {
                _mBEditMode = false;
                ToggleEditMode();
            }
        }

        public void RefreshUiCulture()
        {
            _mnuLaunch.Text = ScreenManagerLang.mnuThumbnailPlay;
            _mnuRename.Text = ScreenManagerLang.mnuThumbnailRename;
            _mnuDelete.Text = ScreenManagerLang.mnuThumbnailDelete;

            // The # char is just a placeholder for a space,
            // Because MeasureString doesn't support trailing spaces.
            // (see PicBoxPaint)
            _mImageText = string.Format("{0}#", ScreenManagerLang.Generic_Image);

            picBox.Invalidate();
        }

        #endregion Public interface

        #region UI Event Handlers

        private void ThumbListViewItem_DoubleClick(object sender, EventArgs e)
        {
            // this event handler is actually shared by all controls
            if (LaunchVideo != null)
            {
                Cursor = Cursors.WaitCursor;
                LaunchVideo(this, EventArgs.Empty);
                Cursor = Cursors.Default;
            }
        }

        private void ThumbListViewItem_Click(object sender, EventArgs e)
        {
            // this event handler is actually shared by all controls.
            // (except for lblFilename)
            if (!ErrorImage)
            {
                SetSelected();
            }
        }

        private void LblFileNameClick(object sender, EventArgs e)
        {
            if (!ErrorImage)
            {
                if (!_mBIsSelected)
                {
                    SetSelected();
                }
                else
                {
                    StartRenaming();
                }
            }
        }

        private void lblFileName_TextChanged(object sender, EventArgs e)
        {
            // Re check if we need to elid it.
            if (lblFileName.Text.Length > MIFilenameMaxCharacters)
            {
                lblFileName.Text = lblFileName.Text.Substring(0, MIFilenameMaxCharacters) + "...";
            }
        }

        private void PicBoxPaint(object sender, PaintEventArgs e)
        {
            // Draw picture, border and duration.
            if (!ErrorImage && _mCurrentThumbnail != null)
            {
                // configure for speed. These are thumbnails anyway.
                e.Graphics.PixelOffsetMode = PixelOffsetMode.None; //PixelOffsetMode.HighSpeed;
                e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                e.Graphics.SmoothingMode = SmoothingMode.HighSpeed;

                // Draw picture. We always draw to the whole container.
                // it is the picBox that is ratio stretched, see SetSize().
                e.Graphics.DrawImage(_mCurrentThumbnail, 0, 0, picBox.Width, picBox.Height);

                // Draw border.
                var p = _mBIsSelected ? MPenSelected : MPenUnselected;
                e.Graphics.DrawRectangle(p, 1, 1, picBox.Width - 2, picBox.Height - 2);
                e.Graphics.DrawRectangle(Pens.White, 2, 2, picBox.Width - 5, picBox.Height - 5);

                // Draw quick preview rectangles.
                if (_mHovering && _mBitmaps != null && _mBitmaps.Count > 1)
                {
                    var rectWidth = picBox.Width/_mBitmaps.Count;
                    var rectHeight = 20;
                    for (var i = 0; i < _mBitmaps.Count; i++)
                    {
                        if (i == _mICurrentThumbnailIndex)
                        {
                            e.Graphics.FillRectangle(MBrushQuickPreviewActive, rectWidth*i, picBox.Height - 20,
                                rectWidth, rectHeight);
                        }
                        else
                        {
                            e.Graphics.FillRectangle(MBrushQuickPreviewInactive, rectWidth*i, picBox.Height - 20,
                                rectWidth, rectHeight);
                        }
                    }
                }

                // Draw duration text in the corner + background.
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (IsImage)
                {
                    // MeasureString doesn't support trailing spaces.
                    // We used # as placeholders, remove them just before drawing.
                    var bgSize = e.Graphics.MeasureString(_mImageText, MFontDuration);
                    e.Graphics.DrawLine(_mPenDuration, picBox.Width - bgSize.Width - 1, 12, (float) picBox.Width - 4, 12);
                    e.Graphics.DrawString(_mImageText.Replace('#', ' '), MFontDuration, Brushes.White,
                        picBox.Width - bgSize.Width - 3, 5);
                }
                else
                {
                    var bgSize = e.Graphics.MeasureString(Duration, MFontDuration);
                    e.Graphics.DrawLine(_mPenDuration, picBox.Width - bgSize.Width - 1, 12, (float) picBox.Width - 4, 12);
                    e.Graphics.DrawString(Duration, MFontDuration, Brushes.White, picBox.Width - bgSize.Width - 3, 5);
                }

                // Draw image size
                var bgSize2 = e.Graphics.MeasureString(_mImageSize, MFontDuration);
                var sizeTop = 29;
                e.Graphics.DrawLine(_mPenDuration, picBox.Width - bgSize2.Width - 1, sizeTop, (float) picBox.Width - 4,
                    sizeTop);
                e.Graphics.DrawString(_mImageSize, MFontDuration, Brushes.White, picBox.Width - bgSize2.Width - 3,
                    sizeTop - 7);

                // Draw KVA file indicator
                if (HasKva)
                {
                    e.Graphics.DrawLine(_mPenDuration, (float) picBox.Width - 20, 45, (float) picBox.Width - 4, 45);
                    e.Graphics.DrawImage(_bmpKvaAnalysis, picBox.Width - 25, 38);
                }
            }
        }

        private void PicBoxMouseMove(object sender, MouseEventArgs e)
        {
            if (!ErrorImage && _mBitmaps != null)
            {
                if (_mBitmaps.Count > 0)
                {
                    if (e.Y > picBox.Height - 20)
                    {
                        _tmrThumbs.Stop();
                        var index = e.X/(picBox.Width/_mBitmaps.Count);
                        _mICurrentThumbnailIndex = Math.Max(Math.Min(index, _mBitmaps.Count - 1), 0);
                        _mCurrentThumbnail = _mBitmaps[_mICurrentThumbnailIndex];
                        picBox.Invalidate();
                    }
                    else
                    {
                        _tmrThumbs.Start();
                    }
                }
            }
        }

        private void ThumbListViewItemPaint(object sender, PaintEventArgs e)
        {
            // Draw the shadow
            e.Graphics.DrawLine(MPenShadow, picBox.Left + picBox.Width + 1, picBox.Top + MPenShadow.Width,
                picBox.Left + picBox.Width + 1, picBox.Top + picBox.Height + MPenShadow.Width);
            e.Graphics.DrawLine(MPenShadow, picBox.Left + MPenShadow.Width, picBox.Top + picBox.Height + 1,
                picBox.Left + MPenShadow.Width + picBox.Width, picBox.Top + picBox.Height + 1);
        }

        private void tmrThumbs_Tick(object sender, EventArgs e)
        {
            // This event occur when the user has been staying for a while on the same thumbnail.
            // Loop between all stored images.
            if (!ErrorImage && _mBitmaps != null)
            {
                if (_mBitmaps.Count > 1)
                {
                    // Change the thumbnail displayed.
                    _mICurrentThumbnailIndex++;
                    if (_mICurrentThumbnailIndex >= _mBitmaps.Count)
                    {
                        _mICurrentThumbnailIndex = 0;
                    }

                    _mCurrentThumbnail = _mBitmaps[_mICurrentThumbnailIndex];
                    picBox.Invalidate();
                }
            }
        }

        private void PicBoxMouseEnter(object sender, EventArgs e)
        {
            _mHovering = true;

            if (!ErrorImage && _mBitmaps != null)
            {
                if (_mBitmaps.Count > 1)
                {
                    // Instantly change image
                    _mICurrentThumbnailIndex = 1;
                    _mCurrentThumbnail = _mBitmaps[_mICurrentThumbnailIndex];
                    picBox.Invalidate();

                    // Then start timer to slideshow.
                    _tmrThumbs.Start();
                }
            }
        }

        private void PicBoxMouseLeave(object sender, EventArgs e)
        {
            _mHovering = false;

            if (!ErrorImage && _mBitmaps != null)
            {
                _tmrThumbs.Stop();
                if (_mBitmaps.Count > 0)
                {
                    _mICurrentThumbnailIndex = 0;
                    _mCurrentThumbnail = _mBitmaps[_mICurrentThumbnailIndex];
                    picBox.Invalidate();
                }
            }
        }

        private void TbFileNameKeyPress(object sender, KeyPressEventArgs e)
        {
            // editing a file name.

            if (e.KeyChar == 13) // Carriage Return.
            {
                var newFileName = Path.GetDirectoryName(_mFileName) + "\\" + tbFileName.Text;

                // Prevent overwriting.
                if (File.Exists(_mFileName) && !File.Exists(newFileName) && newFileName.Length > 5)
                {
                    // Try to change the filename
                    try
                    {
                        File.Move(_mFileName, newFileName);

                        // If renaming went fine, consolidate the file name.
                        if (!File.Exists(_mFileName))
                        {
                            FileName = newFileName;
                        }

                        // Ask the Explorer tree to refresh itself...
                        // But not the thumbnails pane.
                        var dp = DelegatesPool.Instance();
                        if (dp.RefreshFileExplorer != null)
                        {
                            dp.RefreshFileExplorer(false);
                        }
                    }
                    catch (ArgumentException)
                    {
                        // contains only white space, or contains invalid characters as defined in InvalidPathChars.
                        // -> Silently fail.
                        // TODO:Display error dialog box.
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // The caller does not have the required permission.
                    }
                    catch (Exception)
                    {
                        // Log error.
                    }
                }
                QuitEditMode();

                // Set this thumb as selected.
                SetSelected();
            }
            else if (e.KeyChar == 27) // Escape.
            {
                QuitEditMode();
            }
        }

        #endregion UI Event Handlers

        #region Menu Event Handlers

        private void mnuRename_Click(object sender, EventArgs e)
        {
            StartRenaming();
        }

        private void mnuDelete_Click(object sender, EventArgs e)
        {
            // Use the built-in dialogs to confirm (or not).
            // Delete is done through moving to recycle bin.
            try
            {
                FileSystem.DeleteFile(_mFileName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
            }
            catch (OperationCanceledException)
            {
                // User cancelled confirmation box.
            }

            // Other possible error case: the file couldn't be deleted because it's still in use.

            // If file was effectively moved to trash, reload the folder.
            if (!File.Exists(_mFileName))
            {
                // Ask the Explorer tree to refresh itself...
                // This will in turn refresh the thumbnails pane.
                var dp = DelegatesPool.Instance();
                if (dp.RefreshFileExplorer != null)
                {
                    dp.RefreshFileExplorer(true);
                }
            }
        }

        private void mnuLaunch_Click(object sender, EventArgs e)
        {
            if (LaunchVideo != null)
            {
                Cursor = Cursors.WaitCursor;
                LaunchVideo(this, EventArgs.Empty);
                Cursor = Cursors.Default;
            }
        }

        #endregion Menu Event Handlers

        #region Edit mode

        public void StartRenaming()
        {
            // Switch to edit mode.
            if (FileNameEditing != null)
            {
                FileNameEditing(this, new EditingEventArgs(true));

                _mBEditMode = true;
                ToggleEditMode();
            }
        }

        private void QuitEditMode()
        {
            // Quit edit mode.
            if (FileNameEditing != null)
            {
                FileNameEditing(this, new EditingEventArgs(false));
            }

            _mBEditMode = false;
            ToggleEditMode();
        }

        private void ToggleEditMode()
        {
            // the global variable m_bEditMode should already have been set
            // Now let's configure the display depending on its value.
            if (_mBEditMode)
            {
                // The layout is configured at construction time.
                tbFileName.Text = Path.GetFileName(_mFileName);
                tbFileName.SelectAll(); // Only works for tab ?
                tbFileName.Visible = true;
                tbFileName.Focus();
            }
            else
            {
                tbFileName.Visible = false;
            }
        }

        #endregion Edit mode
    }

    #region EventArgs classe used here

    /// <summary>
    ///     A (very) simple event args class to encapsulate the state of the editing.
    /// </summary>
    public class EditingEventArgs : EventArgs
    {
        public readonly bool Editing;

        public EditingEventArgs(bool bEditing)
        {
            Editing = bEditing;
        }
    }

    #endregion EventArgs classe used here
}