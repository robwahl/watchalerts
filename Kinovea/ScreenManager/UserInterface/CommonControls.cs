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
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public partial class CommonControls : UserControl
    {
        private void CommonControls_Resize(object sender, EventArgs e)
        {
            _btnSnapShot.Location = new Point(trkFrame.Right + 10, btnMerge.Top);
            _btnDualVideo.Location = new Point(_btnSnapShot.Right + 10, btnMerge.Top);
        }

        #region TrkFrame Handlers

        private void trkFrame_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            if (e.Position != _mIOldPosition)
            {
                _mIOldPosition = e.Position;
                if (_mScreenManagerUiContainer != null)
                {
                    _mScreenManagerUiContainer.CommonCtrl_PositionChanged(e.Position);
                }
            }
        }

        #endregion TrkFrame Handlers

        #region Properties

        public IScreenManagerUiContainer ScreenManagerUiContainer
        {
            set { _mScreenManagerUiContainer = value; }
        }

        public bool Playing
        {
            get { return _mBPlaying; }
            set
            {
                _mBPlaying = value;
                RefreshPlayButton();
            }
        }

        public bool SyncMerging
        {
            get { return _mBSyncMerging; }
            set
            {
                _mBSyncMerging = value;
                RefreshMergeTooltip();
            }
        }

        public int SyncOffset
        {
            set
            {
                var iValue = value;
                lblSyncOffset.Text = "SyncOffset : " + iValue;
                lblSyncOffset.Invalidate();
            }
        }

        #endregion Properties

        #region Members

        private bool _mBPlaying;
        private bool _mBSyncMerging;
        private long _mIOldPosition;
        private IScreenManagerUiContainer _mScreenManagerUiContainer;
        private readonly Button _btnSnapShot = new Button();
        private readonly Button _btnDualVideo = new Button();

        #endregion Members

        #region Construction & Culture

        public CommonControls()
        {
            InitializeComponent();
            PostInit();
        }

        private void PostInit()
        {
            BackColor = Color.White;

            _btnSnapShot.BackColor = Color.Transparent;
            _btnSnapShot.BackgroundImageLayout = ImageLayout.None;
            _btnSnapShot.Cursor = Cursors.Hand;
            _btnSnapShot.FlatAppearance.BorderSize = 0;
            _btnSnapShot.FlatAppearance.MouseOverBackColor = Color.Transparent;
            _btnSnapShot.FlatStyle = FlatStyle.Flat;
            _btnSnapShot.Image = Resources.snapsingle_1;
            _btnSnapShot.Location = new Point(trkFrame.Right + 10, btnMerge.Top);
            _btnSnapShot.MinimumSize = new Size(25, 25);
            _btnSnapShot.Name = "btnSnapShot";
            _btnSnapShot.Size = new Size(30, 25);
            _btnSnapShot.UseVisualStyleBackColor = false;
            _btnSnapShot.Click += btnSnapshot_Click;

            _btnDualVideo.BackColor = Color.Transparent;
            _btnDualVideo.BackgroundImageLayout = ImageLayout.None;
            _btnDualVideo.Cursor = Cursors.Hand;
            _btnDualVideo.FlatAppearance.BorderSize = 0;
            _btnDualVideo.FlatAppearance.MouseOverBackColor = Color.Transparent;
            _btnDualVideo.FlatStyle = FlatStyle.Flat;
            _btnDualVideo.Image = Resources.savevideo;
            _btnDualVideo.Location = new Point(_btnSnapShot.Right + 10, _btnSnapShot.Top);
            _btnDualVideo.MinimumSize = new Size(25, 25);
            _btnDualVideo.Name = "btnDualVideo";
            _btnDualVideo.Size = new Size(30, 25);
            _btnDualVideo.UseVisualStyleBackColor = false;
            _btnDualVideo.Click += btnDualVideo_Click;

            Controls.Add(_btnSnapShot);
            Controls.Add(_btnDualVideo);
        }

        public void RefreshUiCulture()
        {
            // Labels
            lblInfo.Text = ScreenManagerLang.lblInfo_Text;

            // ToolTips
            toolTips.SetToolTip(buttonGotoFirst, ScreenManagerLang.buttonGotoFirst_ToolTip);
            toolTips.SetToolTip(buttonGotoLast, ScreenManagerLang.buttonGotoLast_ToolTip);
            toolTips.SetToolTip(buttonGotoNext, ScreenManagerLang.buttonGotoNext_ToolTip);
            toolTips.SetToolTip(buttonGotoPrevious, ScreenManagerLang.buttonGotoPrevious_ToolTip);
            toolTips.SetToolTip(buttonPlay, ScreenManagerLang.buttonPlay_ToolTip);
            toolTips.SetToolTip(btnSwap, ScreenManagerLang.mnuSwapScreens);
            toolTips.SetToolTip(btnSync, ScreenManagerLang.btnSync_ToolTip);
            toolTips.SetToolTip(_btnSnapShot, ScreenManagerLang.ToolTip_SideBySideSnapshot);
            toolTips.SetToolTip(_btnDualVideo, ScreenManagerLang.ToolTip_SideBySideVideo);

            RefreshMergeTooltip();
        }

        #endregion Construction & Culture

        #region Buttons Handlers

        public void buttonGotoFirst_Click(object sender, EventArgs e)
        {
            if (_mScreenManagerUiContainer != null)
            {
                _mScreenManagerUiContainer.CommonCtrl_GotoFirst();
                trkFrame.Position = trkFrame.Minimum;
                PlayStopped();
            }
        }

        public void buttonGotoPrevious_Click(object sender, EventArgs e)
        {
            if (_mScreenManagerUiContainer != null)
            {
                _mScreenManagerUiContainer.CommonCtrl_GotoPrev();
                trkFrame.Position--;
            }
        }

        public void buttonPlay_Click(object sender, EventArgs e)
        {
            if (_mScreenManagerUiContainer != null)
            {
                _mBPlaying = !_mBPlaying;
                RefreshPlayButton();
                _mScreenManagerUiContainer.CommonCtrl_Play();
            }
        }

        public void buttonGotoNext_Click(object sender, EventArgs e)
        {
            if (_mScreenManagerUiContainer != null)
            {
                _mScreenManagerUiContainer.CommonCtrl_GotoNext();
                trkFrame.Position++;
            }
        }

        public void buttonGotoLast_Click(object sender, EventArgs e)
        {
            if (_mScreenManagerUiContainer != null)
            {
                _mScreenManagerUiContainer.CommonCtrl_GotoLast();
                trkFrame.Position = trkFrame.Maximum;
            }
        }

        private void btnSwap_Click(object sender, EventArgs e)
        {
            if (_mScreenManagerUiContainer != null)
            {
                _mScreenManagerUiContainer.CommonCtrl_Swap();
            }
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            if (_mScreenManagerUiContainer != null)
            {
                _mScreenManagerUiContainer.CommonCtrl_Sync();
            }
        }

        private void btnMerge_Click(object sender, EventArgs e)
        {
            if (_mScreenManagerUiContainer != null)
            {
                _mBSyncMerging = !_mBSyncMerging;
                _mScreenManagerUiContainer.CommonCtrl_Merge();
                RefreshMergeTooltip();
            }
        }

        private void btnSnapshot_Click(object sender, EventArgs e)
        {
            if (_mScreenManagerUiContainer != null)
            {
                _mScreenManagerUiContainer.CommonCtrl_Snapshot();
            }
        }

        private void btnDualVideo_Click(object sender, EventArgs e)
        {
            if (_mScreenManagerUiContainer != null)
            {
                _mScreenManagerUiContainer.CommonCtrl_DualVideo();
            }
        }

        #endregion Buttons Handlers

        #region Lower level helpers

        private void RefreshPlayButton()
        {
            if (_mBPlaying)
            {
                buttonPlay.Image = Resources.liqpause6;
            }
            else
            {
                buttonPlay.Image = Resources.liqplay17;
            }
        }

        private void PlayStopped()
        {
            buttonPlay.Image = Resources.liqplay17;
        }

        private void RefreshMergeTooltip()
        {
            if (_mBSyncMerging)
            {
                toolTips.SetToolTip(btnMerge, ScreenManagerLang.ToolTip_CommonCtrl_DisableMerge);
            }
            else
            {
                toolTips.SetToolTip(btnMerge, ScreenManagerLang.ToolTip_CommonCtrl_EnableMerge);
            }
        }

        #endregion Lower level helpers
    }
}