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
using Kinovea.VideoFiles;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     A control that let the user explore a folder.
    ///     A folder is loaded asynchronically through a background worker.
    ///     We hold a list of several bgWorkers. (List ThumbListLoader)
    ///     When we start loading thumbs, we try the first bgWorker,
    ///     if it's currently used, we cancel it and spawn a new one.
    ///     we use this new one to handle the new thumbs.
    ///     This allows us to effectively cancel the display of a folder
    ///     without preventing the load of the new folder.
    ///     (Fixes bugs for when we change directories fastly)
    ///     Each thumbnail will be presented in a ThumbListViewItem.
    ///     We first position and initialize the ThumbListViewItem,
    ///     and then load them with the video data. (thumbnail from ffmpeg)
    ///     through the help of the ThumbListLoader who is responsible for this.
    /// </summary>
    public partial class ThumbListView : UserControl
    {
        #region Construction & initialization

        public ThumbListView()
        {
            Log.Debug("Constructing ThumbListView");

            InitializeComponent();

            RefreshUiCulture();

            _mICurrentSize = (int)_mPreferencesManager.ExplorerThumbsSize;
            DeselectAllSizingButtons();
            SelectSizingButton();
        }

        #endregion Construction & initialization

        #region Events

        [Category("Action"), Browsable(true)]
        public event EventHandler Closing;

        #endregion Events

        public void SetScreenManagerUiContainer(IScreenManagerUiContainer value)
        {
            _mScreenManagerUiContainer = value;
        }

        public void RefreshUiCulture()
        {
            btnHideThumbView.Text = ScreenManagerLang.btnHideThumbView;

            // Refresh all thumbnails.
            for (var i = 0; i < splitResizeBar.Panel2.Controls.Count; i++)
            {
                var tlvi = splitResizeBar.Panel2.Controls[i] as ThumbListViewItem;
                if (tlvi != null)
                {
                    tlvi.RefreshUiCulture();
                }
            }
        }

        #region Keyboard Handling

        public bool OnKeyPress(Keys keycode)
        {
            // Method called from the Screen Manager's PreFilterMessage.
            var bWasHandled = false;
            if (splitResizeBar.Panel2.Controls.Count > 0 && !_mBEditMode)
            {
                // Note that ESC key to cancel editing is handled directly in
                // each thumbnail item.
                switch (keycode)
                {
                    case Keys.Left:
                        {
                            if (_mSelectedVideo == null)
                            {
                                ((ThumbListViewItem)splitResizeBar.Panel2.Controls[0]).SetSelected();
                            }
                            else
                            {
                                var index = (int)_mSelectedVideo.Tag;
                                var iRow = index / _mICurrentSize;
                                var iCol = index - (iRow * _mICurrentSize);

                                if (iCol > 0)
                                {
                                    ((ThumbListViewItem)splitResizeBar.Panel2.Controls[index - 1]).SetSelected();
                                }
                            }
                            break;
                        }
                    case Keys.Right:
                        {
                            if (_mSelectedVideo == null)
                            {
                                ((ThumbListViewItem)splitResizeBar.Panel2.Controls[0]).SetSelected();
                            }
                            else
                            {
                                var index = (int)_mSelectedVideo.Tag;
                                var iRow = index / _mICurrentSize;
                                var iCol = index - (iRow * _mICurrentSize);

                                if (iCol < _mICurrentSize - 1 && index + 1 < splitResizeBar.Panel2.Controls.Count)
                                {
                                    ((ThumbListViewItem)splitResizeBar.Panel2.Controls[index + 1]).SetSelected();
                                }
                            }
                            break;
                        }
                    case Keys.Up:
                        {
                            if (_mSelectedVideo == null)
                            {
                                ((ThumbListViewItem)splitResizeBar.Panel2.Controls[0]).SetSelected();
                            }
                            else
                            {
                                var index = (int)_mSelectedVideo.Tag;
                                var iRow = index / _mICurrentSize;
                                var iCol = index - (iRow * _mICurrentSize);

                                if (iRow > 0)
                                {
                                    ((ThumbListViewItem)splitResizeBar.Panel2.Controls[index - _mICurrentSize]).SetSelected
                                        ();
                                }
                            }
                            splitResizeBar.Panel2.ScrollControlIntoView(_mSelectedVideo);
                            break;
                        }
                    case Keys.Down:
                        {
                            if (_mSelectedVideo == null)
                            {
                                ((ThumbListViewItem)splitResizeBar.Panel2.Controls[0]).SetSelected();
                            }
                            else
                            {
                                var index = (int)_mSelectedVideo.Tag;
                                var iRow = index / _mICurrentSize;
                                var iCol = index - (iRow * _mICurrentSize);

                                if ((iRow < splitResizeBar.Panel2.Controls.Count / _mICurrentSize) &&
                                    index + _mICurrentSize < splitResizeBar.Panel2.Controls.Count)
                                {
                                    ((ThumbListViewItem)splitResizeBar.Panel2.Controls[index + _mICurrentSize]).SetSelected
                                        ();
                                }
                            }
                            splitResizeBar.Panel2.ScrollControlIntoView(_mSelectedVideo);
                            break;
                        }
                    case Keys.Return:
                        {
                            if (_mSelectedVideo != null)
                            {
                                if (!_mSelectedVideo.ErrorImage)
                                {
                                    _mScreenManagerUiContainer.DropLoadMovie(_mSelectedVideo.FileName, -1);
                                }
                            }
                            break;
                        }
                    case Keys.Add:
                        {
                            if ((ModifierKeys & Keys.Control) == Keys.Control)
                            {
                                UpSizeThumbs();
                            }
                            break;
                        }
                    case Keys.Subtract:
                        {
                            if ((ModifierKeys & Keys.Control) == Keys.Control)
                            {
                                DownSizeThumbs();
                            }
                            break;
                        }
                    case Keys.F2:
                        {
                            if (_mSelectedVideo != null)
                            {
                                if (!_mSelectedVideo.ErrorImage)
                                    _mSelectedVideo.StartRenaming();
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
            return bWasHandled;
        }

        #endregion Keyboard Handling

        private void Panel2MouseDown(object sender, MouseEventArgs e)
        {
            // Clicked nowhere.

            // 1. Deselect all videos.
            if (_mSelectedVideo != null)
            {
                _mSelectedVideo.SetUnselected();
                _mSelectedVideo = null;
            }

            // 2. Toggle off edit mode.
            CancelEditMode();
        }

        private void CancelEditMode()
        {
            _mBEditMode = false;

            // Browse all thumbs and make sure they are all in normal mode.
            for (var i = 0; i < splitResizeBar.Panel2.Controls.Count; i++)
            {
                var tlvi = splitResizeBar.Panel2.Controls[i] as ThumbListViewItem;
                if (tlvi != null)
                {
                    tlvi.CancelEditMode();
                }
            }
        }

        private void Panel2MouseEnter(object sender, EventArgs e)
        {
            // Give focus to enbale mouse scroll
            if (!_mBEditMode)
            {
                splitResizeBar.Panel2.Focus();
            }
        }

        private void Panel2Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawLine(MGradientPen, 33, 0, 350, 0);
        }

        #region Members

        private readonly VideoFile _mVideoFile = new VideoFile();

        private readonly int _mILeftMargin = 30;

        private readonly int _mIRightMargin = 20;
        // Allow for potential scrollbar. This value doesn't include the last pic spacing.

        private readonly int _mITopMargin = 5;
        private int _mIHorzSpacing = 20; // Right placed and respected even for the last column.
        private int _mIVertSpacing = 20;
        private int _mICurrentSize = (int)ExplorerThumbSizes.Large;

        private static readonly Brush MGradientBrush = new LinearGradientBrush(new Point(33, 0), new Point(350, 0),
            Color.LightSteelBlue, Color.White);

        private static readonly Pen MGradientPen = new Pen(MGradientBrush);

        private readonly List<ThumbListLoader> _mLoaders = new List<ThumbListLoader>();
        private ThumbListViewItem _mSelectedVideo;

        private bool _mBEditMode;
        private IScreenManagerUiContainer _mScreenManagerUiContainer;
        private readonly PreferencesManager _mPreferencesManager = PreferencesManager.Instance();
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region RAM Monitoring

        /*private void TraceRamUsage(int id)
        {
            float iCurrentRam = m_RamCounter.NextValue();
            if (id >= 0)
            {
                Console.WriteLine("id:{0}, RAM: {1}", id.ToString(), m_fLastRamValue - iCurrentRam);
            }
            m_fLastRamValue = iCurrentRam;
        }
        private void InitRamCounter()
        {
            m_RamCounter = new PerformanceCounter("Memory", "Available KBytes");
            m_fLastRamValue = m_RamCounter.NextValue();
            Console.WriteLine("Initial state, Available RAM: {0}", m_fLastRamValue);
        }*/

        #endregion RAM Monitoring

        #region Organize and Display

        public void DisplayThumbnails(List<string> fileNames)
        {
            // Remove loaders that completed loading or cancellation.
            CleanupLoaders();
            Log.Debug(string.Format("New set of files asked, currently having {0} loaders", _mLoaders.Count));

            // Cancel remaining ones.
            foreach (var loader in _mLoaders)
            {
                loader.Cancel();
            }

            if (fileNames.Count > 0)
            {
                // Reset display for new files.
                SetupPlaceHolders(fileNames);

                // Create the new loader and launch it.
                var tll = new ThumbListLoader(fileNames, splitResizeBar.Panel2, _mVideoFile);
                _mLoaders.Add(tll);
                tll.Launch();
            }
        }

        public void CleanupThumbnails()
        {
            // Remove the controls and deallocate any ressources used.
            for (var iCtrl = splitResizeBar.Panel2.Controls.Count - 1; iCtrl >= 0; iCtrl--)
            {
                var tlvi = splitResizeBar.Panel2.Controls[iCtrl] as ThumbListViewItem;
                if (tlvi != null)
                {
                    var bmp = tlvi.picBox.BackgroundImage;
                    splitResizeBar.Panel2.Controls.RemoveAt(iCtrl);

                    if (!tlvi.ErrorImage)
                    {
                        if (bmp != null)
                            bmp.Dispose();
                    }
                }
            }

            _mSelectedVideo = null;
        }

        private void CleanupLoaders()
        {
            // Remove loaders that completed loading or cancellation.
            for (var i = _mLoaders.Count - 1; i >= 0; i--)
            {
                if (_mLoaders[i].Unused)
                {
                    _mLoaders.RemoveAt(i);
                }
            }
        }

        private void SetupPlaceHolders(List<string> fileNames)
        {
            //-----------------------------------------------------------
            // Creates a list of thumb boxes to hold this folder's thumbs
            // They will be turned visible only when
            // they are loaded with their respective thumbnail.
            //-----------------------------------------------------------

            Log.Debug("Organizing placeholders.");

            CleanupThumbnails();

            if (fileNames.Count > 0)
            {
                ToggleButtonsVisibility(true);

                var iColumnWidth = (splitResizeBar.Panel2.Width - _mILeftMargin - _mIRightMargin) / _mICurrentSize;

                _mIHorzSpacing = iColumnWidth / 20;
                _mIVertSpacing = _mIHorzSpacing;

                for (var i = 0; i < fileNames.Count; i++)
                {
                    var tlvi = new ThumbListViewItem();

                    tlvi.FileName = fileNames[i];
                    tlvi.Tag = i;
                    tlvi.ToolTipHandler = toolTip1;
                    tlvi.SetSize(iColumnWidth - _mIHorzSpacing);
                    tlvi.Location = new Point(0, 0);
                    tlvi.LaunchVideo += ThumbListViewItem_LaunchVideo;
                    tlvi.VideoSelected += ThumbListViewItem_VideoSelected;
                    tlvi.FileNameEditing += ThumbListViewItem_FileNameEditing;

                    // Organize
                    var iRow = i / _mICurrentSize;
                    var iCol = i - (iRow * _mICurrentSize);
                    tlvi.Location = new Point(_mILeftMargin + (iCol * (tlvi.Size.Width + _mIHorzSpacing)),
                        _mITopMargin + (iRow * (tlvi.Size.Height + _mIVertSpacing)));

                    tlvi.Visible = false;
                    splitResizeBar.Panel2.Controls.Add(tlvi);
                }
            }
            else
            {
                ToggleButtonsVisibility(false);
            }

            Log.Debug("Placeholders organized.");
        }

        private void ToggleButtonsVisibility(bool bVisible)
        {
            btnHideThumbView.Visible = bVisible;
            btnExtraSmall.Visible = bVisible;
            btnSmall.Visible = bVisible;
            btnMedium.Visible = bVisible;
            btnLarge.Visible = bVisible;
            btnExtraLarge.Visible = bVisible;
        }

        private void OrganizeThumbnailsByColumns(int iTotalCols)
        {
            // Resize and Organize thumbs to match a given number of columns
            if (splitResizeBar.Panel2.Controls.Count > 0 && !IsLoading())
            {
                Log.Debug("Reorganizing thumbnails.");

                var iColumnWidth = (splitResizeBar.Panel2.Width - _mILeftMargin - _mIRightMargin) / iTotalCols;
                _mIHorzSpacing = iColumnWidth / 20;
                _mIVertSpacing = _mIHorzSpacing;

                // Scroll up before relocating controls.
                splitResizeBar.Panel2.ScrollControlIntoView(splitResizeBar.Panel2.Controls[0]);

                splitResizeBar.Panel2.SuspendLayout();

                for (var i = 0; i < splitResizeBar.Panel2.Controls.Count; i++)
                {
                    var tlvi = splitResizeBar.Panel2.Controls[i] as ThumbListViewItem;
                    if (tlvi != null)
                    {
                        var iRow = i / iTotalCols;
                        var iCol = i - (iRow * iTotalCols);

                        tlvi.SetSize(iColumnWidth - _mIHorzSpacing);

                        var loc = new Point();
                        loc.X = _mILeftMargin + (iCol * (tlvi.Size.Width + _mIHorzSpacing));
                        loc.Y = _mITopMargin + (iRow * (tlvi.Size.Height + _mIVertSpacing));
                        tlvi.Location = loc;
                    }
                }

                splitResizeBar.Panel2.ResumeLayout();

                Log.Debug("Thumbnails reorganized.");
            }
        }

        private void UpSizeThumbs()
        {
            DeselectAllSizingButtons();

            switch (_mICurrentSize)
            {
                case (int)ExplorerThumbSizes.ExtraSmall:
                    _mICurrentSize = (int)ExplorerThumbSizes.Small;
                    btnSmall.BackColor = Color.LightSteelBlue;
                    break;

                case (int)ExplorerThumbSizes.Small:
                    _mICurrentSize = (int)ExplorerThumbSizes.Medium;
                    btnMedium.BackColor = Color.LightSteelBlue;
                    break;

                case (int)ExplorerThumbSizes.Medium:
                    _mICurrentSize = (int)ExplorerThumbSizes.Large;
                    btnLarge.BackColor = Color.LightSteelBlue;
                    break;

                case (int)ExplorerThumbSizes.Large:
                default:
                    _mICurrentSize = (int)ExplorerThumbSizes.ExtraLarge;
                    btnExtraLarge.BackColor = Color.LightSteelBlue;
                    break;
            }

            OrganizeThumbnailsByColumns(_mICurrentSize);
            splitResizeBar.Panel2.Invalidate();
        }

        private void DownSizeThumbs()
        {
            DeselectAllSizingButtons();

            switch (_mICurrentSize)
            {
                case (int)ExplorerThumbSizes.Small:
                    _mICurrentSize = (int)ExplorerThumbSizes.ExtraSmall;
                    btnExtraSmall.BackColor = Color.LightSteelBlue;
                    break;

                case (int)ExplorerThumbSizes.Medium:
                    _mICurrentSize = (int)ExplorerThumbSizes.Small;
                    btnSmall.BackColor = Color.LightSteelBlue;
                    break;

                case (int)ExplorerThumbSizes.Large:
                    _mICurrentSize = (int)ExplorerThumbSizes.Medium;
                    btnMedium.BackColor = Color.LightSteelBlue;
                    break;

                case (int)ExplorerThumbSizes.ExtraLarge:
                    _mICurrentSize = (int)ExplorerThumbSizes.Large;
                    btnLarge.BackColor = Color.LightSteelBlue;
                    break;

                case (int)ExplorerThumbSizes.ExtraSmall:
                default:
                    _mICurrentSize = (int)ExplorerThumbSizes.ExtraSmall;
                    btnExtraSmall.BackColor = Color.LightSteelBlue;
                    break;
            }

            OrganizeThumbnailsByColumns(_mICurrentSize);
            splitResizeBar.Panel2.Invalidate();
        }

        private void splitResizeBar_Panel2_Resize(object sender, EventArgs e)
        {
            if (Visible)
            {
                OrganizeThumbnailsByColumns(_mICurrentSize);
            }
        }

        private bool IsLoading()
        {
            var bLoading = false;
            foreach (var loader in _mLoaders)
            {
                if (!loader.Unused)
                {
                    bLoading = true;
                    break;
                }
            }
            return bLoading;
        }

        #endregion Organize and Display

        #region Thumbnails items events handlers

        private void ThumbListViewItem_LaunchVideo(object sender, EventArgs e)
        {
            CancelEditMode();
            var tlvi = sender as ThumbListViewItem;

            if (tlvi != null && !tlvi.ErrorImage)
            {
                _mScreenManagerUiContainer.DropLoadMovie(tlvi.FileName, -1);
            }
        }

        private void ThumbListViewItem_VideoSelected(object sender, EventArgs e)
        {
            CancelEditMode();
            var tlvi = sender as ThumbListViewItem;

            if (tlvi != null)
            {
                if (_mSelectedVideo != null && _mSelectedVideo != tlvi)
                {
                    _mSelectedVideo.SetUnselected();
                }

                _mSelectedVideo = tlvi;
            }
        }

        private void ThumbListViewItem_FileNameEditing(object sender, EditingEventArgs e)
        {
            // Make sure the keyboard handling doesn't interfere
            // if one thumbnail is in edit mode.
            // There should only be one thumbnail in edit mode at a time.
            _mBEditMode = e.Editing;
        }

        #endregion Thumbnails items events handlers

        #region Sizing Buttons

        private void DeselectAllSizingButtons()
        {
            btnExtraSmall.BackColor = Color.SteelBlue;
            btnSmall.BackColor = Color.SteelBlue;
            btnMedium.BackColor = Color.SteelBlue;
            btnLarge.BackColor = Color.SteelBlue;
            btnExtraLarge.BackColor = Color.SteelBlue;
        }

        private void SelectSizingButton()
        {
            switch (_mICurrentSize)
            {
                case (int)ExplorerThumbSizes.Small:
                    btnSmall.BackColor = Color.LightSteelBlue;
                    break;

                case (int)ExplorerThumbSizes.Medium:
                    btnMedium.BackColor = Color.LightSteelBlue;
                    break;

                case (int)ExplorerThumbSizes.Large:
                    btnLarge.BackColor = Color.LightSteelBlue;
                    break;

                case (int)ExplorerThumbSizes.ExtraLarge:
                    btnExtraLarge.BackColor = Color.LightSteelBlue;
                    break;

                case (int)ExplorerThumbSizes.ExtraSmall:
                    btnExtraSmall.BackColor = Color.LightSteelBlue;
                    break;

                default:
                    break;
            }
        }

        private void btnExtraSmall_Click(object sender, EventArgs e)
        {
            _mICurrentSize = 14;
            OrganizeThumbnailsByColumns(_mICurrentSize);
            DeselectAllSizingButtons();
            btnExtraSmall.BackColor = Color.LightSteelBlue;
            splitResizeBar.Panel2.Invalidate();
            SavePrefs();
        }

        private void btnSmall_Click(object sender, EventArgs e)
        {
            _mICurrentSize = 10;
            OrganizeThumbnailsByColumns(_mICurrentSize);
            DeselectAllSizingButtons();
            btnSmall.BackColor = Color.LightSteelBlue;
            splitResizeBar.Panel2.Invalidate();
            SavePrefs();
        }

        private void btnMedium_Click(object sender, EventArgs e)
        {
            _mICurrentSize = 7;
            OrganizeThumbnailsByColumns(_mICurrentSize);
            DeselectAllSizingButtons();
            btnMedium.BackColor = Color.LightSteelBlue;
            splitResizeBar.Panel2.Invalidate();
            SavePrefs();
        }

        private void btnLarge_Click(object sender, EventArgs e)
        {
            _mICurrentSize = 5;
            OrganizeThumbnailsByColumns(_mICurrentSize);
            DeselectAllSizingButtons();
            btnLarge.BackColor = Color.LightSteelBlue;
            splitResizeBar.Panel2.Invalidate();
            SavePrefs();
        }

        private void btnExtraLarge_Click(object sender, EventArgs e)
        {
            _mICurrentSize = 4;
            OrganizeThumbnailsByColumns(_mICurrentSize);
            DeselectAllSizingButtons();
            btnExtraLarge.BackColor = Color.LightSteelBlue;
            splitResizeBar.Panel2.Invalidate();
            SavePrefs();
        }

        #endregion Sizing Buttons

        #region Closing

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (Closing != null) Closing(this, EventArgs.Empty);
        }

        private void btnShowThumbView_Click(object sender, EventArgs e)
        {
            CleanupThumbnails();
            if (Closing != null) Closing(this, EventArgs.Empty);
        }

        private void SavePrefs()
        {
            _mPreferencesManager.ExplorerThumbsSize = (ExplorerThumbSizes)_mICurrentSize;
            _mPreferencesManager.Export();
        }

        #endregion Closing
    }
}