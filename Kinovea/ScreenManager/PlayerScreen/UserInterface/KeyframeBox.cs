/*
Copyright � Joan Charmant 2008.
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
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     Represent a key image as a thumbnail.
    ///     Holds the key image id in the .Tag prop.
    /// </summary>
    public partial class KeyframeBox : UserControl
    {
        #region Constructor

        public KeyframeBox(Keyframe kf)
        {
            _mKeyframe = kf;

            InitializeComponent();
            //lblTimecode.Visible = true;
            BackColor = Color.Black;
            btnClose.Parent = pbThumbnail;
            btnComment.Parent = pbThumbnail;
        }

        #endregion Constructor

        #region Events

        [Category("Action"), Browsable(true)]
        public event EventHandler CloseThumb;

        [Category("Action"), Browsable(true)]
        public event EventHandler ClickThumb;

        [Category("Action"), Browsable(true)]
        public event EventHandler ClickInfos;

        #endregion Events

        #region Members

        private readonly Keyframe _mKeyframe;
        private bool _mBAutoUpdatingTitle;

        #endregion Members

        #region Public Methods

        public void DisplayAsSelected(bool bSelected)
        {
            BackColor = bSelected ? Color.SteelBlue : Color.Black;
        }

        public void UpdateTitle(string title)
        {
            lblTimecode.Text = title;
            _mBAutoUpdatingTitle = true;
            tbTitle.Text = title;
            _mBAutoUpdatingTitle = false;

            UpdateToolTip();
        }

        #endregion Public Methods

        #region Event Handlers - Mouse Enter / Leave

        private void Controls_MouseEnter(object sender, EventArgs e)
        {
            ShowButtons();
            Focus();
        }

        private void Controls_MouseLeave(object sender, EventArgs e)
        {
            // We hide the close button only if we left the whole control.
            var clientMouse = PointToClient(MousePosition);
            if (!pbThumbnail.ClientRectangle.Contains(clientMouse))
            {
                HideButtons();
                // Fixme: Doesn't prevent typing until another control takes focus.
                StopEditing();
            }
        }

        private void Controls_MouseDoubleClick(object sender, EventArgs e)
        {
            if (ClickInfos != null) ClickInfos(this, e);
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

        private void btnComment_Click(object sender, EventArgs e)
        {
            if (ClickInfos != null) ClickInfos(this, e);
        }

        private void TbTitleTextChanged(object sender, EventArgs e)
        {
            if (!_mBAutoUpdatingTitle)
            {
                _mKeyframe.Title = tbTitle.Text;
                UpdateToolTip();
            }
        }

        private void TbTitleEnter(object sender, EventArgs e)
        {
            DeactivateKeyboardHandler();
        }

        #endregion Event Handlers - Buttons / Text

        #region Private helpers

        private void ShowButtons()
        {
            btnClose.Visible = true;
            btnComment.Visible = true;
        }

        private void HideButtons()
        {
            btnClose.Visible = false;
            btnComment.Visible = false;
        }

        private void DeactivateKeyboardHandler()
        {
            // Mouse enters the info box : deactivate the keyboard handling for the screens
            // so we can use <space>, <return>, etc. here.
            var dp = DelegatesPool.Instance();
            if (dp.DeactivateKeyboardHandler != null)
            {
                dp.DeactivateKeyboardHandler();
            }
        }

        private void ActivateKeyboardHandler()
        {
            // Mouse leave the info box : reactivate the keyboard handling for the screens
            // so we can use <space>, <return>, etc. as player shortcuts
            var dp = DelegatesPool.Instance();
            if (dp.ActivateKeyboardHandler != null)
            {
                dp.ActivateKeyboardHandler();
            }
        }

        private void StopEditing()
        {
            ActivateKeyboardHandler();

            if (tbTitle.Text.Length == 0)
            {
                // We reseted the title. We should now display the timecode.
                _mBAutoUpdatingTitle = true;
                tbTitle.Text = _mKeyframe.Title;
                _mBAutoUpdatingTitle = false;
            }
            UpdateToolTip();
        }

        private void UpdateToolTip()
        {
            if (_mKeyframe.TimeCode != _mKeyframe.Title)
            {
                toolTips.SetToolTip(pbThumbnail, _mKeyframe.Title + "\n" + _mKeyframe.TimeCode);
            }
            else
            {
                toolTips.SetToolTip(pbThumbnail, "");
            }
        }

        #endregion Private helpers
    }
}