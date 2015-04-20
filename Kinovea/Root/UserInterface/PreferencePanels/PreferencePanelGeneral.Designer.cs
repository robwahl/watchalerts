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

namespace Kinovea.Root
{
	partial class PreferencePanelGeneral
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the control.
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
			this.cmbHistoryCount = new ComboBox();
			this.lblLanguage = new Label();
			this.lblHistoryCount = new Label();
			this.cmbLanguage = new ComboBox();
			this.cmbTimeCodeFormat = new ComboBox();
			this.lblTimeMarkersFormat = new Label();
			this.cmbSpeedUnit = new ComboBox();
			this.lblSpeedUnit = new Label();
			this.cmbImageFormats = new ComboBox();
			this.lblImageFormat = new Label();
			this.SuspendLayout();
			// 
			// cmbHistoryCount
			// 
			this.cmbHistoryCount.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cmbHistoryCount.FormattingEnabled = true;
			this.cmbHistoryCount.Items.AddRange(new object[] {
									"0",
									"1",
									"2",
									"3",
									"4",
									"5",
									"6",
									"7",
									"8",
									"9",
									"10"});
			this.cmbHistoryCount.Location = new Point(369, 60);
			this.cmbHistoryCount.Name = "cmbHistoryCount";
			this.cmbHistoryCount.Size = new Size(36, 21);
			this.cmbHistoryCount.TabIndex = 13;
			this.cmbHistoryCount.SelectedIndexChanged += new EventHandler(this.cmbHistoryCount_SelectedIndexChanged);
			// 
			// lblLanguage
			// 
			this.lblLanguage.AutoSize = true;
			this.lblLanguage.Location = new Point(28, 24);
			this.lblLanguage.Name = "lblLanguage";
			this.lblLanguage.Size = new Size(61, 13);
			this.lblLanguage.TabIndex = 12;
			this.lblLanguage.Text = "Language :";
			// 
			// lblHistoryCount
			// 
			this.lblHistoryCount.AutoSize = true;
			this.lblHistoryCount.Location = new Point(28, 63);
			this.lblHistoryCount.Name = "lblHistoryCount";
			this.lblHistoryCount.Size = new Size(160, 13);
			this.lblHistoryCount.TabIndex = 14;
			this.lblHistoryCount.Text = "Number of files in recent history :";
			// 
			// cmbLanguage
			// 
			this.cmbLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cmbLanguage.FormattingEnabled = true;
			this.cmbLanguage.Items.AddRange(new object[] {
									"English",
									"Français"});
			this.cmbLanguage.Location = new Point(301, 24);
			this.cmbLanguage.Name = "cmbLanguage";
			this.cmbLanguage.Size = new Size(104, 21);
			this.cmbLanguage.TabIndex = 11;
			this.cmbLanguage.SelectedIndexChanged += new EventHandler(this.cmbLanguage_SelectedIndexChanged);
			// 
			// cmbTimeCodeFormat
			// 
			this.cmbTimeCodeFormat.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cmbTimeCodeFormat.Location = new Point(221, 98);
			this.cmbTimeCodeFormat.Name = "cmbTimeCodeFormat";
			this.cmbTimeCodeFormat.Size = new Size(184, 21);
			this.cmbTimeCodeFormat.TabIndex = 17;
			this.cmbTimeCodeFormat.SelectedIndexChanged += new EventHandler(this.cmbTimeCodeFormat_SelectedIndexChanged);
			// 
			// lblTimeMarkersFormat
			// 
			this.lblTimeMarkersFormat.AutoSize = true;
			this.lblTimeMarkersFormat.Location = new Point(28, 101);
			this.lblTimeMarkersFormat.Name = "lblTimeMarkersFormat";
			this.lblTimeMarkersFormat.Size = new Size(108, 13);
			this.lblTimeMarkersFormat.TabIndex = 16;
			this.lblTimeMarkersFormat.Text = "Time markers format :";
			// 
			// cmbSpeedUnit
			// 
			this.cmbSpeedUnit.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cmbSpeedUnit.Location = new Point(221, 178);
			this.cmbSpeedUnit.Name = "cmbSpeedUnit";
			this.cmbSpeedUnit.Size = new Size(184, 21);
			this.cmbSpeedUnit.TabIndex = 29;
			this.cmbSpeedUnit.SelectedIndexChanged += new EventHandler(this.cmbSpeedUnit_SelectedIndexChanged);
			// 
			// lblSpeedUnit
			// 
			this.lblSpeedUnit.AutoSize = true;
			this.lblSpeedUnit.Location = new Point(28, 183);
			this.lblSpeedUnit.Name = "lblSpeedUnit";
			this.lblSpeedUnit.Size = new Size(123, 13);
			this.lblSpeedUnit.TabIndex = 28;
			this.lblSpeedUnit.Text = "Preferred unit for speed :";
			// 
			// cmbImageFormats
			// 
			this.cmbImageFormats.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cmbImageFormats.Location = new Point(221, 139);
			this.cmbImageFormats.Name = "cmbImageFormats";
			this.cmbImageFormats.Size = new Size(183, 21);
			this.cmbImageFormats.TabIndex = 27;
			this.cmbImageFormats.SelectedIndexChanged += new EventHandler(this.cmbImageAspectRatio_SelectedIndexChanged);
			// 
			// lblImageFormat
			// 
			this.lblImageFormat.AutoSize = true;
			this.lblImageFormat.Location = new Point(28, 143);
			this.lblImageFormat.Name = "lblImageFormat";
			this.lblImageFormat.Size = new Size(110, 13);
			this.lblImageFormat.TabIndex = 26;
			this.lblImageFormat.Text = "Default image format :";
			// 
			// PreferencePanelGeneral
			// 
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.BackColor = Color.Gainsboro;
			this.Controls.Add(this.cmbSpeedUnit);
			this.Controls.Add(this.lblSpeedUnit);
			this.Controls.Add(this.cmbImageFormats);
			this.Controls.Add(this.lblImageFormat);
			this.Controls.Add(this.cmbTimeCodeFormat);
			this.Controls.Add(this.lblTimeMarkersFormat);
			this.Controls.Add(this.cmbHistoryCount);
			this.Controls.Add(this.lblLanguage);
			this.Controls.Add(this.lblHistoryCount);
			this.Controls.Add(this.cmbLanguage);
			this.Name = "PreferencePanelGeneral";
			this.Size = new Size(432, 236);
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private Label lblImageFormat;
		private ComboBox cmbImageFormats;
		private Label lblSpeedUnit;
		private ComboBox cmbSpeedUnit;
		private Label lblTimeMarkersFormat;
		private ComboBox cmbTimeCodeFormat;
		private ComboBox cmbLanguage;
		private Label lblHistoryCount;
		private Label lblLanguage;
		private ComboBox cmbHistoryCount;
	}
}
