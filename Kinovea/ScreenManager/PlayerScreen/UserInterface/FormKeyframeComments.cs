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
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public partial class FormKeyframeComments : Form
    {
        #region Contructors

        public FormKeyframeComments(PlayerScreenUserInterface psui)
        {
            InitializeComponent();
            RefreshUiCulture();
            UserActivated = false;
            _mPsui = psui;
        }

        #endregion Contructors

        // This is an info box common to all keyframes.
        // It can be activated or deactivated by the user.
        // When activated, it only display itself if we are stopped on a keyframe.
        // The content is then updated with keyframe content.

        #region Properties

        public bool UserActivated { get; set; }

        #endregion Properties

        #region Members

        private Keyframe _mKeyframe;
        private readonly PlayerScreenUserInterface _mPsui;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Public Interface

        public void UpdateContent(Keyframe keyframe)
        {
            // We keep only one window, and the keyframe it displays is swaped.

            if (_mKeyframe != keyframe)
            {
                SaveInfos();
                _mKeyframe = keyframe;
                LoadInfos();
            }
        }

        public void CommitChanges()
        {
            SaveInfos();
        }

        public void RefreshUiCulture()
        {
            Text = "   " + ScreenManagerLang.dlgKeyframeComment_Title;
            toolTips.SetToolTip(btnBold, ScreenManagerLang.ToolTip_RichText_Bold);
            toolTips.SetToolTip(btnItalic, ScreenManagerLang.ToolTip_RichText_Italic);
            toolTips.SetToolTip(btnUnderline, ScreenManagerLang.ToolTip_RichText_Underline);
            toolTips.SetToolTip(btnStrike, ScreenManagerLang.ToolTip_RichText_Strikeout);
            toolTips.SetToolTip(btnForeColor, ScreenManagerLang.ToolTip_RichText_ForeColor);
            toolTips.SetToolTip(btnBackColor, ScreenManagerLang.ToolTip_RichText_BackColor);
        }

        #endregion Public Interface

        #region Form event handlers

        private void formKeyframeComments_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // If the user close the mini window we only hide it.
                e.Cancel = true;
                UserActivated = false;
                SaveInfos();
                ActivateKeyboardHandler();
            }

            Visible = false;
        }

        private void formKeyframeComments_MouseEnter(object sender, EventArgs e)
        {
            DeactivateKeyboardHandler();
        }

        private void formKeyframeComments_MouseLeave(object sender, EventArgs e)
        {
            CheckMouseLeave();
        }

        #endregion Form event handlers

        #region Styling event handlers

        private void btnBold_Click(object sender, EventArgs e)
        {
            var style = GetSelectionStyle();
            style = rtbComment.SelectionFont.Bold ? style - (int)FontStyle.Bold : style + (int)FontStyle.Bold;
            rtbComment.SelectionFont = new Font(rtbComment.SelectionFont.FontFamily, rtbComment.SelectionFont.Size,
                (FontStyle)style);
        }

        private void btnItalic_Click(object sender, EventArgs e)
        {
            var style = GetSelectionStyle();
            style = rtbComment.SelectionFont.Italic ? style - (int)FontStyle.Italic : style + (int)FontStyle.Italic;
            rtbComment.SelectionFont = new Font(rtbComment.SelectionFont.FontFamily, rtbComment.SelectionFont.Size,
                (FontStyle)style);
        }

        private void btnUnderline_Click(object sender, EventArgs e)
        {
            var style = GetSelectionStyle();
            style = rtbComment.SelectionFont.Underline
                ? style - (int)FontStyle.Underline
                : style + (int)FontStyle.Underline;
            rtbComment.SelectionFont = new Font(rtbComment.SelectionFont.FontFamily, rtbComment.SelectionFont.Size,
                (FontStyle)style);
        }

        private void btnStrike_Click(object sender, EventArgs e)
        {
            var style = GetSelectionStyle();
            style = rtbComment.SelectionFont.Strikeout
                ? style - (int)FontStyle.Strikeout
                : style + (int)FontStyle.Strikeout;
            rtbComment.SelectionFont = new Font(rtbComment.SelectionFont.FontFamily, rtbComment.SelectionFont.Size,
                (FontStyle)style);
        }

        private void btnForeColor_Click(object sender, EventArgs e)
        {
            var picker = new FormColorPicker();
            ScreenManagerKernel.LocateForm(picker);
            if (picker.ShowDialog() == DialogResult.OK)
            {
                rtbComment.SelectionColor = picker.PickedColor;
            }
            picker.Dispose();
        }

        private void btnBackColor_Click(object sender, EventArgs e)
        {
            var picker = new FormColorPicker();
            ScreenManagerKernel.LocateForm(picker);
            if (picker.ShowDialog() == DialogResult.OK)
            {
                rtbComment.SelectionBackColor = picker.PickedColor;
            }
            picker.Dispose();
        }

        #endregion Styling event handlers

        #region Lower level helpers

        private void CheckMouseLeave()
        {
            // We really leave only if we left the whole control.
            // we have to do this because placing the mouse over the text boxes will raise a
            // formKeyframeComments_MouseLeave event...
            if (!Bounds.Contains(MousePosition))
            {
                ActivateKeyboardHandler();
            }
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
            // so we can use <space>, <return>, etc. as player shortcuts.
            // This is sometimes strange. You put the mouse away to start typing,
            // and the first carriage return triggers the playback leaving the key image.

            var dp = DelegatesPool.Instance();
            if (dp.ActivateKeyboardHandler != null)
            {
                dp.ActivateKeyboardHandler();
            }
        }

        private void LoadInfos()
        {
            // Update
            txtTitle.Text = _mKeyframe.Title;
            rtbComment.Clear();
            rtbComment.Rtf = _mKeyframe.CommentRtf;
        }

        private void SaveInfos()
        {
            // Commit changes to the keyframe
            // This must not be called at each info modification otherwise the update routine breaks...

            Log.Debug("Saving comment and title");
            if (_mKeyframe != null)
            {
                _mKeyframe.CommentRtf = rtbComment.Rtf;

                if (_mKeyframe.Title != txtTitle.Text)
                {
                    _mKeyframe.Title = txtTitle.Text;
                    _mPsui.OnKeyframesTitleChanged();
                }
            }
        }

        private int GetSelectionStyle()
        {
            // Combine all the styles into an int, to have generic toggles methods.
            var bold = rtbComment.SelectionFont.Bold ? (int)FontStyle.Bold : 0;
            var italic = rtbComment.SelectionFont.Italic ? (int)FontStyle.Italic : 0;
            var underline = rtbComment.SelectionFont.Underline ? (int)FontStyle.Underline : 0;
            var strikeout = rtbComment.SelectionFont.Strikeout ? (int)FontStyle.Strikeout : 0;

            return bold + italic + underline + strikeout;
        }

        #endregion Lower level helpers
    }
}