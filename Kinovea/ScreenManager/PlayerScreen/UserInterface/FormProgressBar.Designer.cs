#region License
/*
Copyright © Joan Charmant 2008-2009.
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
	partial class FormProgressBar
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
			this.progressBar = new ProgressBar();
			this.labelInfos = new Label();
			this.btnCancel = new Button();
			this.SuspendLayout();
			// 
			// progressBar
			// 
			this.progressBar.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) 
									| AnchorStyles.Right)));
			this.progressBar.Location = new Point(20, 12);
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new Size(335, 22);
			this.progressBar.Step = 1;
			this.progressBar.TabIndex = 4;
			// 
			// labelInfos
			// 
			this.labelInfos.AutoSize = true;
			this.labelInfos.Location = new Point(17, 47);
			this.labelInfos.Name = "labelInfos";
			this.labelInfos.Size = new Size(36, 13);
			this.labelInfos.TabIndex = 5;
			this.labelInfos.Text = "[Infos]";
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = DialogResult.Cancel;
			this.btnCancel.Location = new Point(270, 42);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new Size(85, 22);
			this.btnCancel.TabIndex = 7;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new EventHandler(this.ButtonCancel_Click);
			// 
			// formProgressBar
			// 
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.AutoSize = true;
			this.ClientSize = new Size(369, 76);
			this.ControlBox = false;
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.labelInfos);
			this.Controls.Add(this.progressBar);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormProgressBar";
			this.Opacity = 0.9;
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.Text = "[formProgressBar_Title]";
			this.FormClosing += new FormClosingEventHandler(this.formProgressBar_FormClosing);
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private Button btnCancel;
		public Label labelInfos;
		public ProgressBar progressBar;
	}
}
