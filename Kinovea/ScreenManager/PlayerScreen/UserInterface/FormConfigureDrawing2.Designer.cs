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
	partial class FormConfigureDrawing2
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
			this.btnOK = new Button();
			this.btnCancel = new Button();
			this.grpConfig = new GroupBox();
			this.SuspendLayout();
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = DialogResult.OK;
			this.btnOK.Location = new Point(44, 120);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new Size(99, 24);
			this.btnOK.TabIndex = 31;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new EventHandler(this.BtnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = DialogResult.Cancel;
			this.btnCancel.Location = new Point(149, 120);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new Size(99, 24);
			this.btnCancel.TabIndex = 32;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new EventHandler(this.BtnCancel_Click);
			// 
			// grpConfig
			// 
			this.grpConfig.Location = new Point(12, 12);
			this.grpConfig.Name = "grpConfig";
			this.grpConfig.Padding = new Padding(3, 3, 20, 3);
			this.grpConfig.Size = new Size(236, 97);
			this.grpConfig.TabIndex = 33;
			this.grpConfig.TabStop = false;
			this.grpConfig.Text = "Configuration";
			// 
			// FormConfigureDrawing2
			// 
			this.AcceptButton = this.btnOK;
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new Size(260, 156);
			this.Controls.Add(this.grpConfig);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnCancel);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormConfigureDrawing2";
			this.StartPosition = FormStartPosition.Manual;
			this.Text = "FormConfigureDrawing2";
			this.FormClosing += new FormClosingEventHandler(this.Form_FormClosing);
			this.ResumeLayout(false);
		}
		private GroupBox grpConfig;
		private Button btnCancel;
		private Button btnOK;
	}
}
