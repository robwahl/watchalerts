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
#endregion

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Kinovea.ScreenManager.Properties;

namespace Kinovea.ScreenManager
{
	partial class FormToolPresets
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new Container();
			this.btnDefault = new Button();
			this.btnSaveProfile = new Button();
			this.btnLoadProfile = new Button();
			this.btnApply = new Button();
			this.btnCancel = new Button();
			this.lstPresets = new ListBox();
			this.btnToolIcon = new Button();
			this.lblToolName = new Label();
			this.grpConfig = new GroupBox();
			this.lblFirstElement = new Label();
			this.btnFirstElement = new Button();
			this.toolTips = new ToolTip(this.components);
			this.grpConfig.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnDefault
			// 
			this.btnDefault.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
			this.btnDefault.FlatAppearance.BorderSize = 0;
			this.btnDefault.FlatAppearance.MouseDownBackColor = Color.White;
			this.btnDefault.FlatAppearance.MouseOverBackColor = Color.White;
			this.btnDefault.FlatStyle = FlatStyle.Flat;
			this.btnDefault.Image = Resources.bin_empty;
			this.btnDefault.Location = new Point(265, 12);
			this.btnDefault.Name = "btnDefault";
			this.btnDefault.Size = new Size(25, 25);
			this.btnDefault.TabIndex = 18;
			this.btnDefault.TextAlign = ContentAlignment.TopCenter;
			this.btnDefault.UseVisualStyleBackColor = true;
			this.btnDefault.Click += new EventHandler(this.BtnDefaultClick);
			// 
			// btnSaveProfile
			// 
			this.btnSaveProfile.FlatAppearance.BorderSize = 0;
			this.btnSaveProfile.FlatAppearance.MouseDownBackColor = Color.White;
			this.btnSaveProfile.FlatAppearance.MouseOverBackColor = Color.White;
			this.btnSaveProfile.FlatStyle = FlatStyle.Flat;
			this.btnSaveProfile.Image = Resources.filesave;
			this.btnSaveProfile.Location = new Point(45, 12);
			this.btnSaveProfile.Name = "btnSaveProfile";
			this.btnSaveProfile.Size = new Size(25, 25);
			this.btnSaveProfile.TabIndex = 17;
			this.btnSaveProfile.TextAlign = ContentAlignment.TopCenter;
			this.btnSaveProfile.UseVisualStyleBackColor = true;
			this.btnSaveProfile.Click += new EventHandler(this.BtnSaveProfileClick);
			// 
			// btnLoadProfile
			// 
			this.btnLoadProfile.FlatAppearance.BorderSize = 0;
			this.btnLoadProfile.FlatAppearance.MouseDownBackColor = Color.White;
			this.btnLoadProfile.FlatAppearance.MouseOverBackColor = Color.White;
			this.btnLoadProfile.FlatStyle = FlatStyle.Flat;
			this.btnLoadProfile.Image = Resources.folder_new;
			this.btnLoadProfile.Location = new Point(14, 12);
			this.btnLoadProfile.Name = "btnLoadProfile";
			this.btnLoadProfile.Size = new Size(25, 25);
			this.btnLoadProfile.TabIndex = 16;
			this.btnLoadProfile.TextAlign = ContentAlignment.TopCenter;
			this.btnLoadProfile.UseVisualStyleBackColor = true;
			this.btnLoadProfile.Click += new EventHandler(this.BtnLoadProfileClick);
			// 
			// btnApply
			// 
			this.btnApply.DialogResult = DialogResult.OK;
			this.btnApply.Location = new Point(87, 204);
			this.btnApply.Name = "btnApply";
			this.btnApply.Size = new Size(99, 24);
			this.btnApply.TabIndex = 76;
			this.btnApply.Text = "Apply";
			this.btnApply.UseVisualStyleBackColor = true;
			this.btnApply.Click += new EventHandler(this.BtnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = DialogResult.Cancel;
			this.btnCancel.Location = new Point(192, 204);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new Size(99, 24);
			this.btnCancel.TabIndex = 77;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new EventHandler(this.BtnCancel_Click);
			// 
			// lstPresets
			// 
			this.lstPresets.FormattingEnabled = true;
			this.lstPresets.Location = new Point(12, 48);
			this.lstPresets.Name = "lstPresets";
			this.lstPresets.Size = new Size(114, 147);
			this.lstPresets.TabIndex = 79;
			this.lstPresets.SelectedIndexChanged += new EventHandler(this.LstPresetsSelectedIndexChanged);
			// 
			// btnToolIcon
			// 
			this.btnToolIcon.BackColor = Color.Black;
			this.btnToolIcon.BackgroundImageLayout = ImageLayout.Center;
			this.btnToolIcon.FlatAppearance.BorderSize = 0;
			this.btnToolIcon.FlatAppearance.MouseDownBackColor = Color.Transparent;
			this.btnToolIcon.FlatAppearance.MouseOverBackColor = Color.WhiteSmoke;
			this.btnToolIcon.FlatStyle = FlatStyle.Flat;
			this.btnToolIcon.ForeColor = Color.Black;
			this.btnToolIcon.Location = new Point(140, 50);
			this.btnToolIcon.Name = "btnToolIcon";
			this.btnToolIcon.Size = new Size(25, 25);
			this.btnToolIcon.TabIndex = 32;
			this.btnToolIcon.TabStop = false;
			this.btnToolIcon.UseVisualStyleBackColor = false;
			// 
			// lblToolName
			// 
			this.lblToolName.AutoSize = true;
			this.lblToolName.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.lblToolName.Location = new Point(175, 55);
			this.lblToolName.Name = "lblToolName";
			this.lblToolName.Size = new Size(73, 16);
			this.lblToolName.TabIndex = 80;
			this.lblToolName.Text = "Tool name";
			// 
			// grpConfig
			// 
			this.grpConfig.Controls.Add(this.lblFirstElement);
			this.grpConfig.Controls.Add(this.btnFirstElement);
			this.grpConfig.Location = new Point(132, 85);
			this.grpConfig.Name = "grpConfig";
			this.grpConfig.Size = new Size(159, 110);
			this.grpConfig.TabIndex = 81;
			this.grpConfig.TabStop = false;
			// 
			// lblFirstElement
			// 
			this.lblFirstElement.AutoSize = true;
			this.lblFirstElement.Location = new Point(38, 29);
			this.lblFirstElement.Name = "lblFirstElement";
			this.lblFirstElement.Size = new Size(73, 13);
			this.lblFirstElement.TabIndex = 1;
			this.lblFirstElement.Text = "First Element :";
			// 
			// btnFirstElement
			// 
			this.btnFirstElement.BackColor = Color.Black;
			this.btnFirstElement.FlatAppearance.BorderSize = 0;
			this.btnFirstElement.FlatStyle = FlatStyle.Flat;
			this.btnFirstElement.Location = new Point(11, 25);
			this.btnFirstElement.Name = "btnFirstElement";
			this.btnFirstElement.Size = new Size(21, 20);
			this.btnFirstElement.TabIndex = 0;
			this.btnFirstElement.UseVisualStyleBackColor = false;
			// 
			// FormToolPresets
			// 
			this.AcceptButton = this.btnApply;
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.BackColor = Color.White;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new Size(302, 240);
			this.Controls.Add(this.grpConfig);
			this.Controls.Add(this.lblToolName);
			this.Controls.Add(this.btnToolIcon);
			this.Controls.Add(this.lstPresets);
			this.Controls.Add(this.btnApply);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnDefault);
			this.Controls.Add(this.btnSaveProfile);
			this.Controls.Add(this.btnLoadProfile);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.Name = "FormToolPresets";
			this.StartPosition = FormStartPosition.Manual;
			this.Text = "FormToolPresets";
			this.FormClosing += new FormClosingEventHandler(this.Form_FormClosing);
			this.grpConfig.ResumeLayout(false);
			this.grpConfig.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private ToolTip toolTips;
		private GroupBox grpConfig;
		private Button btnFirstElement;
		private Label lblFirstElement;
		private Button btnDefault;
		private Label lblToolName;
		private Button btnToolIcon;
		private ListBox lstPresets;
		private Button btnCancel;
		private Button btnApply;
		private Button btnLoadProfile;
		private Button btnSaveProfile;
	}
}
