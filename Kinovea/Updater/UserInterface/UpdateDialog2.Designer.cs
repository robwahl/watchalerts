#region License
/*
Copyright © Joan Charmant 2010.
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
using Kinovea.Updater.Properties;

namespace Kinovea.Updater
{
	partial class UpdateDialog2
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
			this.btnSoftware = new Button();
			this.labelInfos = new Label();
			this.lblNewVersion = new Label();
			this.lblNewVersionFileSize = new Label();
			this.lblChangeLog = new Label();
			this.rtbxChangeLog = new RichTextBox();
			this.lnkKinovea = new LinkLabel();
			this.btnDownload = new Button();
		    this.btnCancel = new Button {DialogResult = DialogResult.Cancel};
		    this.toolTip1 = new ToolTip(this.components);
		    this.folderBrowserDialog = new FolderBrowserDialog();
		    this.bgwkrDownloader = new BackgroundWorker();
		    this.progressDownload = new ProgressBar();
		    this.SuspendLayout();
		    // 
		    // btnSoftware
		    // 
		    this.btnSoftware.FlatAppearance.BorderSize = 0;
		    this.btnSoftware.FlatAppearance.MouseDownBackColor = Color.White;
		    this.btnSoftware.FlatAppearance.MouseOverBackColor = Color.White;
		    this.btnSoftware.FlatStyle = FlatStyle.Flat;
		    this.btnSoftware.Image = Resources.Install;
		    this.btnSoftware.Location = new Point(12, 12);
		    this.btnSoftware.Name = "btnSoftware";
		    this.btnSoftware.Size = new Size(80, 74);
		    this.btnSoftware.TabIndex = 14;
		    this.btnSoftware.UseVisualStyleBackColor = true;
		    // 
		    // labelInfos
		    // 
		    this.labelInfos.AutoSize = true;
		    this.labelInfos.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold, GraphicsUnit.Point, ((byte) (0)));
		    this.labelInfos.Location = new Point(109, 41);
		    this.labelInfos.Name = "labelInfos";
		    this.labelInfos.Size = new Size(197, 16);
		    this.labelInfos.TabIndex = 15;
		    this.labelInfos.Text = "A new version is available !";
		    // 
		    // lblNewVersion
		    // 
		    this.lblNewVersion.AutoSize = true;
		    this.lblNewVersion.Location = new Point(12, 99);
		    this.lblNewVersion.Name = "lblNewVersion";
		    this.lblNewVersion.Size = new Size(188, 13);
		    this.lblNewVersion.TabIndex = 16;
		    this.lblNewVersion.Text = "New Version : 0.6.4 - ( Current : 0.6.2 )";
		    // 
		    // lblNewVersionFileSize
		    // 
		    this.lblNewVersionFileSize.Anchor = ((AnchorStyles) ((AnchorStyles.Top | AnchorStyles.Right)));
		    this.lblNewVersionFileSize.Location = new Point(298, 99);
		    this.lblNewVersionFileSize.Name = "lblNewVersionFileSize";
		    this.lblNewVersionFileSize.Size = new Size(178, 13);
		    this.lblNewVersionFileSize.TabIndex = 17;
		    this.lblNewVersionFileSize.Text = "File Size : 5.4 MB";
		    this.lblNewVersionFileSize.TextAlign = ContentAlignment.MiddleRight;
		    // 
		    // lblChangeLog
		    // 
		    this.lblChangeLog.AutoSize = true;
		    this.lblChangeLog.Location = new Point(12, 126);
		    this.lblChangeLog.Name = "lblChangeLog";
		    this.lblChangeLog.Size = new Size(71, 13);
		    this.lblChangeLog.TabIndex = 18;
		    this.lblChangeLog.Text = "Change Log :";
		    // 
		    // rtbxChangeLog
		    // 
		    this.rtbxChangeLog.Anchor = ((AnchorStyles) ((((AnchorStyles.Top | AnchorStyles.Bottom)
		                                                   | AnchorStyles.Left)
		                                                  | AnchorStyles.Right)));
		    this.rtbxChangeLog.BackColor = Color.Gainsboro;
		    this.rtbxChangeLog.BorderStyle = BorderStyle.None;
		    this.rtbxChangeLog.Font = new Font("Arial", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte) (0)));
		    this.rtbxChangeLog.Location = new Point(12, 154);
		    this.rtbxChangeLog.Name = "rtbxChangeLog";
		    this.rtbxChangeLog.Size = new Size(464, 208);
		    this.rtbxChangeLog.TabIndex = 19;
		    this.rtbxChangeLog.Text = "The quick brown fox jumps over the lazy dog";
		    // 
		    // lnkKinovea
		    // 
		    this.lnkKinovea.ActiveLinkColor = Color.GreenYellow;
		    this.lnkKinovea.Anchor = ((AnchorStyles) ((AnchorStyles.Bottom | AnchorStyles.Left)));
		    this.lnkKinovea.AutoSize = true;
		    this.lnkKinovea.Font = new Font("Microsoft Sans Serif", 6.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte) (0)));
		    this.lnkKinovea.LinkBehavior = LinkBehavior.HoverUnderline;
		    this.lnkKinovea.LinkColor = Color.DarkGreen;
		    this.lnkKinovea.Location = new Point(12, 375);
		    this.lnkKinovea.Name = "lnkKinovea";
		    this.lnkKinovea.Size = new Size(81, 12);
		    this.lnkKinovea.TabIndex = 20;
		    this.lnkKinovea.TabStop = true;
		    this.lnkKinovea.Text = "www.kinovea.org";
		    this.lnkKinovea.VisitedLinkColor = Color.DarkGreen;
		    // 
		    // btnDownload
		    // 
		    this.btnDownload.Anchor = ((AnchorStyles) ((AnchorStyles.Bottom | AnchorStyles.Right)));
		    this.btnDownload.Location = new Point(271, 368);
		    this.btnDownload.Name = "btnDownload";
		    this.btnDownload.Size = new Size(99, 24);
		    this.btnDownload.TabIndex = 41;
		    this.btnDownload.Text = "Download";
		    this.btnDownload.UseVisualStyleBackColor = true;
		    this.btnDownload.Click += new EventHandler(this.btnDownload_Click);
		    // 
		    // btnCancel
		    // 
		    this.btnCancel.Anchor = ((AnchorStyles) ((AnchorStyles.Bottom | AnchorStyles.Right)));
		    this.btnCancel.Location = new Point(377, 368);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new Size(99, 24);
			this.btnCancel.TabIndex = 42;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new EventHandler(this.btnCancel_Click);
			// 
			// progressDownload
			// 
			this.progressDownload.Location = new Point(312, 41);
			this.progressDownload.Name = "progressDownload";
			this.progressDownload.Size = new Size(164, 26);
			this.progressDownload.TabIndex = 43;
			this.progressDownload.Visible = false;
			// 
			// UpdateDialog2
			// 
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.BackColor = Color.White;
			this.ClientSize = new Size(488, 404);
			this.Controls.Add(this.progressDownload);
			this.Controls.Add(this.btnDownload);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.lnkKinovea);
			this.Controls.Add(this.rtbxChangeLog);
			this.Controls.Add(this.lblChangeLog);
			this.Controls.Add(this.lblNewVersionFileSize);
			this.Controls.Add(this.lblNewVersion);
			this.Controls.Add(this.labelInfos);
			this.Controls.Add(this.btnSoftware);
			this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
			this.Name = "UpdateDialog2";
			this.StartPosition = FormStartPosition.CenterScreen;
			this.Text = "UpdateDialog2";
			this.ResumeLayout(false);
			this.PerformLayout();
		}
		private ProgressBar progressDownload;
		private BackgroundWorker bgwkrDownloader;
		private FolderBrowserDialog folderBrowserDialog;
		private ToolTip toolTip1;
		private Button btnCancel;
		private Button btnDownload;
		private LinkLabel lnkKinovea;
		private RichTextBox rtbxChangeLog;
		private Label lblChangeLog;
		private Label lblNewVersionFileSize;
		private Label lblNewVersion;
		private Label labelInfos;
		private Button btnSoftware;
	}
}
