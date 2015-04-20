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
	sealed partial class PreferencePanelDrawings
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
			this.tabSubPages = new TabControl();
			this.tabGeneral = new TabPage();
			this.chkDrawOnPlay = new CheckBox();
			this.tabPersistence = new TabPage();
			this.chkAlwaysVisible = new CheckBox();
			this.lblFading = new Label();
			this.trkFading = new TrackBar();
			this.chkEnablePersistence = new CheckBox();
			this.tabSubPages.SuspendLayout();
			this.tabGeneral.SuspendLayout();
			this.tabPersistence.SuspendLayout();
			((ISupportInitialize)(this.trkFading)).BeginInit();
			this.SuspendLayout();
			// 
			// tabSubPages
			// 
			this.tabSubPages.Controls.Add(this.tabGeneral);
			this.tabSubPages.Controls.Add(this.tabPersistence);
			this.tabSubPages.Dock = DockStyle.Fill;
			this.tabSubPages.Location = new Point(0, 0);
			this.tabSubPages.Name = "tabSubPages";
			this.tabSubPages.SelectedIndex = 0;
			this.tabSubPages.Size = new Size(432, 236);
			this.tabSubPages.TabIndex = 0;
			// 
			// tabGeneral
			// 
			this.tabGeneral.Controls.Add(this.chkDrawOnPlay);
			this.tabGeneral.Location = new Point(4, 22);
			this.tabGeneral.Name = "tabGeneral";
			this.tabGeneral.Padding = new Padding(3);
			this.tabGeneral.Size = new Size(424, 210);
			this.tabGeneral.TabIndex = 0;
			this.tabGeneral.Text = "General";
			this.tabGeneral.UseVisualStyleBackColor = true;
			// 
			// chkDrawOnPlay
			// 
			this.chkDrawOnPlay.AutoSize = true;
			this.chkDrawOnPlay.Location = new Point(17, 27);
			this.chkDrawOnPlay.Name = "chkDrawOnPlay";
			this.chkDrawOnPlay.Size = new Size(202, 17);
			this.chkDrawOnPlay.TabIndex = 52;
			this.chkDrawOnPlay.Text = "Show drawings when video is playing";
			this.chkDrawOnPlay.UseVisualStyleBackColor = true;
			this.chkDrawOnPlay.CheckedChanged += new EventHandler(this.chkDrawOnPlay_CheckedChanged);
			// 
			// tabPersistence
			// 
			this.tabPersistence.Controls.Add(this.chkAlwaysVisible);
			this.tabPersistence.Controls.Add(this.lblFading);
			this.tabPersistence.Controls.Add(this.trkFading);
			this.tabPersistence.Controls.Add(this.chkEnablePersistence);
			this.tabPersistence.Location = new Point(4, 22);
			this.tabPersistence.Name = "tabPersistence";
			this.tabPersistence.Padding = new Padding(3);
			this.tabPersistence.Size = new Size(424, 210);
			this.tabPersistence.TabIndex = 1;
			this.tabPersistence.Text = "Persistence";
			this.tabPersistence.UseVisualStyleBackColor = true;
			// 
			// chkAlwaysVisible
			// 
			this.chkAlwaysVisible.AutoSize = true;
			this.chkAlwaysVisible.Location = new Point(19, 137);
			this.chkAlwaysVisible.Name = "chkAlwaysVisible";
			this.chkAlwaysVisible.Size = new Size(91, 17);
			this.chkAlwaysVisible.TabIndex = 55;
			this.chkAlwaysVisible.Text = "Always visible";
			this.chkAlwaysVisible.UseVisualStyleBackColor = true;
			this.chkAlwaysVisible.CheckedChanged += new EventHandler(this.chkAlwaysVisible_CheckedChanged);
			// 
			// lblFading
			// 
			this.lblFading.Location = new Point(15, 39);
			this.lblFading.Name = "lblFading";
			this.lblFading.Size = new Size(362, 32);
			this.lblFading.TabIndex = 52;
			this.lblFading.Text = "By default, drawings will stay visible for 12 images around the Key Image. kdjfns" +
			"kdjbnsdkjbksdjvbdvkbj";
			this.lblFading.TextAlign = ContentAlignment.BottomLeft;
			// 
			// trkFading
			// 
			this.trkFading.BackColor = Color.White;
			this.trkFading.Location = new Point(19, 77);
			this.trkFading.Maximum = 200;
			this.trkFading.Minimum = 1;
			this.trkFading.Name = "trkFading";
			this.trkFading.Size = new Size(359, 45);
			this.trkFading.TabIndex = 54;
			this.trkFading.TickFrequency = 5;
			this.trkFading.Value = 5;
			this.trkFading.ValueChanged += new EventHandler(this.trkFading_ValueChanged);
			// 
			// chkEnablePersistence
			// 
			this.chkEnablePersistence.AutoSize = true;
			this.chkEnablePersistence.Location = new Point(15, 15);
			this.chkEnablePersistence.Name = "chkEnablePersistence";
			this.chkEnablePersistence.Size = new Size(116, 17);
			this.chkEnablePersistence.TabIndex = 53;
			this.chkEnablePersistence.Text = "Enable persistence";
			this.chkEnablePersistence.UseVisualStyleBackColor = true;
			this.chkEnablePersistence.CheckedChanged += new EventHandler(this.chkFading_CheckedChanged);
			// 
			// PreferencePanelDrawings
			// 
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.Controls.Add(this.tabSubPages);
			this.Name = "PreferencePanelDrawings";
			this.Size = new Size(432, 236);
			this.tabSubPages.ResumeLayout(false);
			this.tabGeneral.ResumeLayout(false);
			this.tabGeneral.PerformLayout();
			this.tabPersistence.ResumeLayout(false);
			this.tabPersistence.PerformLayout();
			((ISupportInitialize)(this.trkFading)).EndInit();
			this.ResumeLayout(false);
		}
		private TabControl tabSubPages;
		private TabPage tabGeneral;
		private TabPage tabPersistence;
		private CheckBox chkEnablePersistence;
		private TrackBar trkFading;
		private Label lblFading;
		private CheckBox chkAlwaysVisible;
		private CheckBox chkDrawOnPlay;
	}
}
