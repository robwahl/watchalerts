#region License
/*
Copyright © Joan Charmant 2009.
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
	partial class FormSetTrajectoryOrigin
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
			this.pnlPreview = new Panel();
			this.picPreview = new PictureBox();
			this.pnlPreview.SuspendLayout();
			((ISupportInitialize)(this.picPreview)).BeginInit();
			this.SuspendLayout();
			// 
			// btnOK
			// 
			this.btnOK.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
			this.btnOK.DialogResult = DialogResult.OK;
			this.btnOK.Location = new Point(414, 470);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new Size(99, 24);
			this.btnOK.TabIndex = 17;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new EventHandler(this.btnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
			this.btnCancel.DialogResult = DialogResult.Cancel;
			this.btnCancel.Location = new Point(519, 470);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new Size(99, 24);
			this.btnCancel.TabIndex = 18;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// pnlPreview
			// 
			this.pnlPreview.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) 
									| AnchorStyles.Left) 
									| AnchorStyles.Right)));
			this.pnlPreview.BackColor = Color.Black;
			this.pnlPreview.Controls.Add(this.picPreview);
			this.pnlPreview.Cursor = Cursors.Cross;
			this.pnlPreview.Location = new Point(12, 15);
			this.pnlPreview.Name = "pnlPreview";
			this.pnlPreview.Size = new Size(603, 443);
			this.pnlPreview.TabIndex = 19;
			this.pnlPreview.Resize += new EventHandler(this.pnlPreview_Resize);
			// 
			// picPreview
			// 
			this.picPreview.Cursor = Cursors.Cross;
			this.picPreview.Location = new Point(166, 116);
			this.picPreview.Name = "picPreview";
			this.picPreview.Size = new Size(250, 193);
			this.picPreview.SizeMode = PictureBoxSizeMode.StretchImage;
			this.picPreview.TabIndex = 0;
			this.picPreview.TabStop = false;
			this.picPreview.MouseMove += new MouseEventHandler(this.picPreview_MouseMove);
			this.picPreview.MouseClick += new MouseEventHandler(this.picPreview_MouseClick);
			this.picPreview.Paint += new PaintEventHandler(this.picPreview_Paint);
			// 
			// formSetTrajectoryOrigin
			// 
			this.AcceptButton = this.btnOK;
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new Size(630, 506);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.pnlPreview);
			this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new Size(400, 350);
			this.Name = "FormSetTrajectoryOrigin";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.Text = "dlgSetTrajectoryOrigin_Title";
			this.Load += new EventHandler(this.formSetTrajectoryOrigin_Load);
			this.pnlPreview.ResumeLayout(false);
			((ISupportInitialize)(this.picPreview)).EndInit();
			this.ResumeLayout(false);
		}
		private PictureBox picPreview;
		private Panel pnlPreview;
		private Button btnCancel;
		private Button btnOK;
	}
}
