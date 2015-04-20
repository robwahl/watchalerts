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
	partial class PreferencePanelPlayer
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
			this.chkDeinterlace = new CheckBox();
			this.grpSwitchToAnalysis = new GroupBox();
			this.lblWorkingZoneLogic = new Label();
			this.trkWorkingZoneSeconds = new TrackBar();
			this.lblWorkingZoneSeconds = new Label();
			this.trkWorkingZoneMemory = new TrackBar();
			this.lblWorkingZoneMemory = new Label();
			this.grpSwitchToAnalysis.SuspendLayout();
			((ISupportInitialize)(this.trkWorkingZoneSeconds)).BeginInit();
			((ISupportInitialize)(this.trkWorkingZoneMemory)).BeginInit();
			this.SuspendLayout();
			// 
			// chkDeinterlace
			// 
			this.chkDeinterlace.Location = new Point(13, 199);
			this.chkDeinterlace.Name = "chkDeinterlace";
			this.chkDeinterlace.Size = new Size(369, 20);
			this.chkDeinterlace.TabIndex = 23;
			this.chkDeinterlace.Text = "dlgPreferences_DeinterlaceByDefault";
			this.chkDeinterlace.UseVisualStyleBackColor = true;
			this.chkDeinterlace.CheckedChanged += new EventHandler(this.ChkDeinterlaceCheckedChanged);
			// 
			// grpSwitchToAnalysis
			// 
			this.grpSwitchToAnalysis.Controls.Add(this.lblWorkingZoneLogic);
			this.grpSwitchToAnalysis.Controls.Add(this.trkWorkingZoneSeconds);
			this.grpSwitchToAnalysis.Controls.Add(this.lblWorkingZoneSeconds);
			this.grpSwitchToAnalysis.Controls.Add(this.trkWorkingZoneMemory);
			this.grpSwitchToAnalysis.Controls.Add(this.lblWorkingZoneMemory);
			this.grpSwitchToAnalysis.Location = new Point(13, 20);
			this.grpSwitchToAnalysis.Name = "grpSwitchToAnalysis";
			this.grpSwitchToAnalysis.Size = new Size(405, 163);
			this.grpSwitchToAnalysis.TabIndex = 26;
			this.grpSwitchToAnalysis.TabStop = false;
			this.grpSwitchToAnalysis.Text = "Switch to Analysis Mode";
			// 
			// lblWorkingZoneLogic
			// 
			this.lblWorkingZoneLogic.AutoSize = true;
			this.lblWorkingZoneLogic.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
			this.lblWorkingZoneLogic.Location = new Point(6, 72);
			this.lblWorkingZoneLogic.Name = "lblWorkingZoneLogic";
			this.lblWorkingZoneLogic.Size = new Size(29, 13);
			this.lblWorkingZoneLogic.TabIndex = 37;
			this.lblWorkingZoneLogic.Text = "And";
			// 
			// trkWorkingZoneSeconds
			// 
			this.trkWorkingZoneSeconds.Location = new Point(9, 40);
			this.trkWorkingZoneSeconds.Maximum = 30;
			this.trkWorkingZoneSeconds.Minimum = 1;
			this.trkWorkingZoneSeconds.Name = "trkWorkingZoneSeconds";
			this.trkWorkingZoneSeconds.Size = new Size(386, 45);
			this.trkWorkingZoneSeconds.TabIndex = 38;
			this.trkWorkingZoneSeconds.Value = 12;
			this.trkWorkingZoneSeconds.ValueChanged += new EventHandler(this.trkWorkingZoneSeconds_ValueChanged);
			// 
			// lblWorkingZoneSeconds
			// 
		    Label workingZoneSeconds = this.lblWorkingZoneSeconds;
		    if (workingZoneSeconds != null)
		    {
		        workingZoneSeconds.AutoSize = true;
		        workingZoneSeconds.Location = new Point(14, 23);
		        workingZoneSeconds.Name = "lblWorkingZoneSeconds";
		        workingZoneSeconds.Size = new Size(191, 13);
		        workingZoneSeconds.TabIndex = 36;
		        workingZoneSeconds.Text = "Working Zone is less than 12 seconds.";
		    }
		    // 
			// trkWorkingZoneMemory
			// 
			this.trkWorkingZoneMemory.Location = new Point(10, 110);
			this.trkWorkingZoneMemory.Maximum = 1024;
			this.trkWorkingZoneMemory.Minimum = 16;
			this.trkWorkingZoneMemory.Name = "trkWorkingZoneMemory";
			this.trkWorkingZoneMemory.Size = new Size(390, 45);
			this.trkWorkingZoneMemory.TabIndex = 35;
			this.trkWorkingZoneMemory.TickFrequency = 50;
			this.trkWorkingZoneMemory.Value = 512;
			this.trkWorkingZoneMemory.ValueChanged += new EventHandler(this.trkWorkingZoneMemory_ValueChanged);
			// 
			// lblWorkingZoneMemory
			// 
			this.lblWorkingZoneMemory.AutoSize = true;
			this.lblWorkingZoneMemory.Location = new Point(14, 92);
			this.lblWorkingZoneMemory.Name = "lblWorkingZoneMemory";
			this.lblWorkingZoneMemory.Size = new Size(257, 13);
			this.lblWorkingZoneMemory.TabIndex = 17;
			this.lblWorkingZoneMemory.Text = "Working Zone will take less than 512 Mib of Memory.";
			// 
			// PreferencePanelPlayer
			// 
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.BackColor = Color.Green;
			this.Controls.Add(this.grpSwitchToAnalysis);
			this.Controls.Add(this.chkDeinterlace);
			this.Name = "PreferencePanelPlayer";
			this.Size = new Size(432, 236);
			this.grpSwitchToAnalysis.ResumeLayout(false);
			this.grpSwitchToAnalysis.PerformLayout();
			((ISupportInitialize)(this.trkWorkingZoneSeconds)).EndInit();
			((ISupportInitialize)(this.trkWorkingZoneMemory)).EndInit();
			this.ResumeLayout(false);
		}
		private Label lblWorkingZoneSeconds;
		private TrackBar trkWorkingZoneSeconds;
		private Label lblWorkingZoneLogic;
		private Label lblWorkingZoneMemory;
		private TrackBar trkWorkingZoneMemory;
		private GroupBox grpSwitchToAnalysis;
		private CheckBox chkDeinterlace;
	}
}
