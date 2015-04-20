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

namespace Kinovea.ScreenManager
{
	partial class FormConfigureOpacity
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
			this.grpConfig = new GroupBox();
			this.lblValue = new Label();
			this.trkValue = new TrackBar();
			this.btnOK = new Button();
			this.btnCancel = new Button();
			this.grpConfig.SuspendLayout();
			((ISupportInitialize)(this.trkValue)).BeginInit();
			this.SuspendLayout();
			// 
			// grpConfig
			// 
			this.grpConfig.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) 
									| AnchorStyles.Left) 
									| AnchorStyles.Right)));
			this.grpConfig.Controls.Add(this.lblValue);
			this.grpConfig.Controls.Add(this.trkValue);
			this.grpConfig.Location = new Point(11, 11);
			this.grpConfig.Name = "grpConfig";
			this.grpConfig.Size = new Size(208, 85);
			this.grpConfig.TabIndex = 32;
			this.grpConfig.TabStop = false;
			this.grpConfig.Text = "Configuration";
			// 
			// lblValue
			// 
			this.lblValue.Location = new Point(18, 16);
			this.lblValue.Name = "lblValue";
			this.lblValue.Size = new Size(174, 13);
			this.lblValue.TabIndex = 4;
			this.lblValue.Text = "100%";
			this.lblValue.TextAlign = ContentAlignment.MiddleRight;
			// 
			// trkValue
			// 
			this.trkValue.Location = new Point(6, 32);
			this.trkValue.Maximum = 100;
			this.trkValue.Minimum = 1;
			this.trkValue.Name = "trkValue";
			this.trkValue.Size = new Size(196, 45);
			this.trkValue.TabIndex = 15;
			this.trkValue.TickFrequency = 4;
			this.trkValue.Value = 100;
			this.trkValue.ValueChanged += new EventHandler(this.trkValue_ValueChanged);
			// 
			// btnOK
			// 
			this.btnOK.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
			this.btnOK.DialogResult = DialogResult.OK;
			this.btnOK.Location = new Point(13, 111);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new Size(99, 24);
			this.btnOK.TabIndex = 31;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new EventHandler(this.btnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
			this.btnCancel.DialogResult = DialogResult.Cancel;
			this.btnCancel.Location = new Point(118, 111);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new Size(99, 24);
			this.btnCancel.TabIndex = 33;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new EventHandler(this.btnCancel_Click);
			// 
			// formConfigureOpacity
			// 
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.BackColor = Color.White;
			this.ClientSize = new Size(230, 146);
			this.Controls.Add(this.grpConfig);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnCancel);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormConfigureOpacity";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = FormStartPosition.Manual;
			this.Text = "  Configure Opacity";
			this.FormClosing += new FormClosingEventHandler(this.formConfigureOpacity_FormClosing);
			this.grpConfig.ResumeLayout(false);
			this.grpConfig.PerformLayout();
			((ISupportInitialize)(this.trkValue)).EndInit();
			this.ResumeLayout(false);
		}
		private Button btnCancel;
		private Button btnOK;
		private TrackBar trkValue;
		private Label lblValue;
		private GroupBox grpConfig;
	}
}