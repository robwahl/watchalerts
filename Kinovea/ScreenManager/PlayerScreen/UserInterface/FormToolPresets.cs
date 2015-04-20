#region License

/*
Copyright © Joan Charmant 2011.
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
using Kinovea.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     The dialog lets the user configure the whole list of tool presets.
    ///     Modifications done on the current presets, reload from file to revert.
    ///     Replaces FormColorProfile.
    /// </summary>
    public partial class FormToolPresets : Form
    {
        #region Members

        private bool _mBManualClose;
        private readonly List<AbstractStyleElement> _mElements = new List<AbstractStyleElement>();
        private int _mIEditorsLeft;
        private readonly AbstractDrawingTool _mPreselect;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor

        public FormToolPresets()
            : this(null)
        {
        }

        public FormToolPresets(AbstractDrawingTool preselect)
        {
            _mPreselect = preselect;
            InitializeComponent();
            LocalizeForm();
            LoadPresets(true);
        }

        #endregion Constructor

        #region Private Methods

        private void LocalizeForm()
        {
            // Window & Controls
            Text = "   " + ScreenManagerLang.dlgColorProfile_Title;
            btnCancel.Text = ScreenManagerLang.Generic_Cancel;
            btnApply.Text = ScreenManagerLang.Generic_Apply;

            // ToolTips
            toolTips.SetToolTip(btnLoadProfile, ScreenManagerLang.dlgColorProfile_ToolTip_LoadProfile);
            toolTips.SetToolTip(btnSaveProfile, ScreenManagerLang.dlgColorProfile_ToolTip_SaveProfile);
            toolTips.SetToolTip(btnDefault, ScreenManagerLang.dlgColorProfile_ToolTip_DefaultProfile);
        }

        private void LoadPresets(bool memorize)
        {
            // Load the list
            lstPresets.Items.Clear();
            var preselected = -1;
            foreach (AbstractDrawingTool tool in ToolManager.Tools.Values)
            {
                if (tool.StylePreset != null && tool.StylePreset.Elements.Count > 0)
                {
                    lstPresets.Items.Add(tool);
                    if (memorize)
                        tool.StylePreset.Memorize();
                    if (tool == _mPreselect) preselected = lstPresets.Items.Count - 1;
                }
            }

            if (lstPresets.Items.Count > 0)
            {
                lstPresets.SelectedIndex = preselected >= 0 ? preselected : 0;
            }
        }

        private void LoadPreset(AbstractDrawingTool preset)
        {
            // Load a single preset
            // The layout is dynamic. The groupbox and the whole form is resized when needed on a "GrowOnly" basis.

            // Tool title and icon
            btnToolIcon.BackColor = Color.Transparent;
            btnToolIcon.Image = preset.Icon;
            lblToolName.Text = preset.DisplayName;

            // Clean up
            _mElements.Clear();
            grpConfig.Controls.Clear();
            var helper = grpConfig.CreateGraphics();

            var editorSize = new Size(60, 20);

            // Initialize the horizontal layout with a minimal value,
            // it will be fixed later if some of the entries have long text.
            var minimalWidth = btnApply.Width + btnCancel.Width + 10;
            //m_iEditorsLeft = Math.Max(minimalWidth - 20 - editorSize.Width, m_iEditorsLeft);
            _mIEditorsLeft = minimalWidth - 20 - editorSize.Width;

            var mimimalHeight = grpConfig.Height;
            var lastEditorBottom = 10;

            foreach (KeyValuePair<string, AbstractStyleElement> pair in preset.StylePreset.Elements)
            {
                var styleElement = pair.Value;
                _mElements.Add(styleElement);

                //styleElement.ValueChanged += element_ValueChanged;

                var btn = new Button();
                btn.Image = styleElement.Icon;
                btn.Size = new Size(20, 20);
                btn.Location = new Point(10, lastEditorBottom + 15);
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = Color.Transparent;

                var lbl = new Label();
                lbl.Text = styleElement.DisplayName;
                lbl.AutoSize = true;
                lbl.Location = new Point(btn.Right + 10, lastEditorBottom + 20);

                var labelSize = helper.MeasureString(lbl.Text, lbl.Font);

                if (lbl.Left + labelSize.Width + 25 > _mIEditorsLeft)
                {
                    // dynamic horizontal layout for high dpi and verbose languages.
                    _mIEditorsLeft = (int)(lbl.Left + labelSize.Width + 25);
                }

                var miniEditor = styleElement.GetEditor();
                miniEditor.Size = editorSize;
                miniEditor.Location = new Point(_mIEditorsLeft, btn.Top);

                lastEditorBottom = miniEditor.Bottom;

                grpConfig.Controls.Add(btn);
                grpConfig.Controls.Add(lbl);
                grpConfig.Controls.Add(miniEditor);
            }

            helper.Dispose();

            // Recheck all mini editors for the left positionning.
            foreach (Control c in grpConfig.Controls)
            {
                if (!(c is Label) && !(c is Button))
                {
                    if (c.Left < _mIEditorsLeft) c.Left = _mIEditorsLeft;
                }
            }

            //grpConfig.Height = Math.Max(lastEditorBottom + 20, grpConfig.Height);
            //grpConfig.Width = Math.Max(m_iEditorsLeft + editorSize.Width + 20, grpConfig.Width);
            grpConfig.Height = Math.Max(lastEditorBottom + 20, 110);
            grpConfig.Width = _mIEditorsLeft + editorSize.Width + 20;
            lstPresets.Height = grpConfig.Bottom - lstPresets.Top;

            btnApply.Top = grpConfig.Bottom + 10;
            btnApply.Left = grpConfig.Right - (btnCancel.Width + 10 + btnApply.Width);
            btnCancel.Top = btnApply.Top;
            btnCancel.Left = btnApply.Right + 10;

            var borderLeft = Width - ClientRectangle.Width;
            Width = borderLeft + btnCancel.Right + 10;

            var borderTop = Height - ClientRectangle.Height;
            Height = borderTop + btnApply.Bottom + 10;
        }

        private void LstPresetsSelectedIndexChanged(object sender, EventArgs e)
        {
            AbstractDrawingTool preset = lstPresets.SelectedItem as AbstractDrawingTool;
            if (preset != null)
            {
                LoadPreset(preset);
            }
        }

        private void BtnDefaultClick(object sender, EventArgs e)
        {
            // Reset all tools to their default preset.
            foreach (AbstractDrawingTool tool in ToolManager.Tools.Values)
            {
                if (tool.StylePreset != null && tool.StylePreset.Elements.Count > 0)
                {
                    DrawingStyle memo = tool.StylePreset.Clone();
                    tool.ResetToDefaultStyle();
                    tool.StylePreset.Memorize(memo);
                }
            }

            LoadPresets(false);
        }

        private void BtnLoadProfileClick(object sender, EventArgs e)
        {
            // load file to working copy of the profile
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = ScreenManagerLang.dlgColorProfile_ToolTip_LoadProfile;
            openFileDialog.Filter = ScreenManagerLang.dlgColorProfile_FileFilter;
            openFileDialog.FilterIndex = 1;
            openFileDialog.InitialDirectory = PreferencesManager.ResourceManager.GetString("ColorProfilesFolder");

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var filePath = openFileDialog.FileName;
                if (filePath.Length > 0)
                {
                    ToolManager.LoadPresets(filePath);
                    LoadPresets(false);
                }
            }
        }

        private void BtnSaveProfileClick(object sender, EventArgs e)
        {
            // Save current working copy to file

            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = ScreenManagerLang.dlgColorProfile_ToolTip_SaveProfile;
            saveFileDialog.Filter = ScreenManagerLang.dlgColorProfile_FileFilter;
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.InitialDirectory = Application.StartupPath + "\\" +
                                              PreferencesManager.ResourceManager.GetString("ColorProfilesFolder");

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                var filePath = saveFileDialog.FileName;
                if (filePath.Length > 0)
                {
                    ToolManager.SavePresets(filePath);
                }
            }
        }

        #region Form closing

        private void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_mBManualClose)
            {
                Revert();
            }
        }

        private void Revert()
        {
            // Revert to memos
            foreach (AbstractDrawingTool tool in ToolManager.Tools.Values)
            {
                if (tool.StylePreset != null && tool.StylePreset.Elements.Count > 0)
                {
                    tool.StylePreset.Revert();
                }
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Revert();
            _mBManualClose = true;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            ToolManager.SavePresets();
            _mBManualClose = true;
        }

        #endregion Form closing

        #endregion Private Methods
    }
}