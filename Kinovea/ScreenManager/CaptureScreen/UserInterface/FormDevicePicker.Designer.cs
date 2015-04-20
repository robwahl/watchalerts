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
using Kinovea.ScreenManager.Properties;

namespace Kinovea.ScreenManager
{
	partial class FormDevicePicker
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
			this.btnApply = new Button();
			this.btnCancel = new Button();
			this.gpCurrentDevice = new GroupBox();
			this.cmbUrl = new ComboBox();
			this.lblStreamType = new Label();
			this.cmbStreamType = new ComboBox();
			this.lblUrl = new Label();
			this.btnDeviceProperties = new Button();
			this.lblNoConf = new Label();
			this.btnCamcorder = new Button();
			this.cmbCapabilities = new ComboBox();
			this.lblConfig = new Label();
			this.lblCurrentlySelected = new Label();
			this.gpOtherDevices = new GroupBox();
			this.cmbOtherDevices = new ComboBox();
			this.gpCurrentDevice.SuspendLayout();
			this.gpOtherDevices.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnApply
			// 
			this.btnApply.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
			this.btnApply.DialogResult = DialogResult.OK;
			this.btnApply.Location = new Point(115, 261);
			this.btnApply.Name = "btnApply";
			this.btnApply.Size = new Size(99, 24);
			this.btnApply.TabIndex = 76;
			this.btnApply.Text = "Apply";
			this.btnApply.UseVisualStyleBackColor = true;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
			this.btnCancel.DialogResult = DialogResult.Cancel;
			this.btnCancel.Location = new Point(220, 261);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new Size(99, 24);
			this.btnCancel.TabIndex = 77;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// gpCurrentDevice
			// 
			this.gpCurrentDevice.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) 
									| AnchorStyles.Right)));
			this.gpCurrentDevice.Controls.Add(this.cmbUrl);
			this.gpCurrentDevice.Controls.Add(this.lblStreamType);
			this.gpCurrentDevice.Controls.Add(this.cmbStreamType);
			this.gpCurrentDevice.Controls.Add(this.lblUrl);
			this.gpCurrentDevice.Controls.Add(this.btnDeviceProperties);
			this.gpCurrentDevice.Controls.Add(this.lblNoConf);
			this.gpCurrentDevice.Controls.Add(this.btnCamcorder);
			this.gpCurrentDevice.Controls.Add(this.cmbCapabilities);
			this.gpCurrentDevice.Controls.Add(this.lblConfig);
			this.gpCurrentDevice.Controls.Add(this.lblCurrentlySelected);
			this.gpCurrentDevice.Location = new Point(12, 12);
			this.gpCurrentDevice.Name = "gpCurrentDevice";
			this.gpCurrentDevice.Size = new Size(307, 153);
			this.gpCurrentDevice.TabIndex = 78;
			this.gpCurrentDevice.TabStop = false;
			this.gpCurrentDevice.Text = "Current device";
			// 
			// cmbUrl
			// 
			this.cmbUrl.FormattingEnabled = true;
			this.cmbUrl.Location = new Point(48, 95);
			this.cmbUrl.Name = "cmbUrl";
			this.cmbUrl.Size = new Size(43, 21);
			this.cmbUrl.TabIndex = 15;
			// 
			// lblStreamType
			// 
			this.lblStreamType.AutoSize = true;
			this.lblStreamType.Location = new Point(16, 113);
			this.lblStreamType.Name = "lblStreamType";
			this.lblStreamType.Size = new Size(17, 13);
			this.lblStreamType.TabIndex = 14;
			this.lblStreamType.Text = "T:";
			this.lblStreamType.Visible = false;
			// 
			// cmbStreamType
			// 
			this.cmbStreamType.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cmbStreamType.FormattingEnabled = true;
			this.cmbStreamType.Location = new Point(39, 113);
			this.cmbStreamType.Name = "cmbStreamType";
			this.cmbStreamType.Size = new Size(70, 21);
			this.cmbStreamType.TabIndex = 13;
			// 
			// lblUrl
			// 
			this.lblUrl.AutoSize = true;
			this.lblUrl.Location = new Point(16, 98);
			this.lblUrl.Name = "lblUrl";
			this.lblUrl.Size = new Size(26, 13);
			this.lblUrl.TabIndex = 11;
			this.lblUrl.Text = "Url :";
			this.lblUrl.Visible = false;
			// 
			// btnDeviceProperties
			// 
			this.btnDeviceProperties.Location = new Point(115, 113);
			this.btnDeviceProperties.Name = "btnDeviceProperties";
			this.btnDeviceProperties.Size = new Size(173, 24);
			this.btnDeviceProperties.TabIndex = 10;
			this.btnDeviceProperties.Text = "Device Properties";
			this.btnDeviceProperties.UseVisualStyleBackColor = true;
			this.btnDeviceProperties.Click += new EventHandler(this.btnDeviceProperties_Click);
			// 
			// lblNoConf
			// 
			this.lblNoConf.BackColor = Color.Transparent;
			this.lblNoConf.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.lblNoConf.ForeColor = Color.Gray;
			this.lblNoConf.Location = new Point(115, 51);
			this.lblNoConf.Name = "lblNoConf";
			this.lblNoConf.Size = new Size(187, 23);
			this.lblNoConf.TabIndex = 9;
			this.lblNoConf.Text = "No other option available";
			this.lblNoConf.TextAlign = ContentAlignment.MiddleLeft;
			// 
			// btnCamcorder
			// 
			this.btnCamcorder.FlatAppearance.BorderSize = 0;
			this.btnCamcorder.FlatStyle = FlatStyle.Flat;
			this.btnCamcorder.Image = Resources.camera_selected;
			this.btnCamcorder.Location = new Point(16, 25);
			this.btnCamcorder.Name = "btnCamcorder";
			this.btnCamcorder.Size = new Size(30, 30);
			this.btnCamcorder.TabIndex = 8;
			this.btnCamcorder.UseVisualStyleBackColor = true;
			// 
			// cmbCapabilities
			// 
			this.cmbCapabilities.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cmbCapabilities.FormattingEnabled = true;
			this.cmbCapabilities.Location = new Point(115, 77);
			this.cmbCapabilities.Name = "cmbCapabilities";
			this.cmbCapabilities.Size = new Size(173, 21);
			this.cmbCapabilities.TabIndex = 5;
			// 
			// lblConfig
			// 
			this.lblConfig.BackColor = Color.Transparent;
			this.lblConfig.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.lblConfig.ForeColor = Color.Black;
			this.lblConfig.Location = new Point(16, 75);
			this.lblConfig.Name = "lblConfig";
			this.lblConfig.Size = new Size(93, 23);
			this.lblConfig.TabIndex = 4;
			this.lblConfig.Text = "Configuration";
			this.lblConfig.TextAlign = ContentAlignment.MiddleLeft;
			// 
			// lblCurrentlySelected
			// 
			this.lblCurrentlySelected.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
			this.lblCurrentlySelected.Location = new Point(77, 32);
			this.lblCurrentlySelected.Name = "lblCurrentlySelected";
			this.lblCurrentlySelected.Size = new Size(217, 20);
			this.lblCurrentlySelected.TabIndex = 0;
			this.lblCurrentlySelected.Text = "My Device";
			// 
			// gpOtherDevices
			// 
			this.gpOtherDevices.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) 
									| AnchorStyles.Right)));
			this.gpOtherDevices.Controls.Add(this.cmbOtherDevices);
			this.gpOtherDevices.Location = new Point(12, 171);
			this.gpOtherDevices.Name = "gpOtherDevices";
			this.gpOtherDevices.Size = new Size(307, 79);
			this.gpOtherDevices.TabIndex = 79;
			this.gpOtherDevices.TabStop = false;
			this.gpOtherDevices.Text = "Select another device";
			// 
			// cmbOtherDevices
			// 
			this.cmbOtherDevices.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cmbOtherDevices.FormattingEnabled = true;
			this.cmbOtherDevices.Location = new Point(16, 37);
			this.cmbOtherDevices.Name = "cmbOtherDevices";
			this.cmbOtherDevices.Size = new Size(272, 21);
			this.cmbOtherDevices.TabIndex = 7;
			this.cmbOtherDevices.SelectedIndexChanged += new EventHandler(this.cmbOtherDevices_SelectedIndexChanged);
			// 
			// formDevicePicker
			// 
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.BackColor = Color.White;
			this.ClientSize = new Size(331, 297);
			this.Controls.Add(this.gpOtherDevices);
			this.Controls.Add(this.gpCurrentDevice);
			this.Controls.Add(this.btnApply);
			this.Controls.Add(this.btnCancel);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormDevicePicker";
			this.ShowInTaskbar = false;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.Text = "FormDevicePicker";
			this.gpCurrentDevice.ResumeLayout(false);
			this.gpCurrentDevice.PerformLayout();
			this.gpOtherDevices.ResumeLayout(false);
			this.ResumeLayout(false);
		}
		private ComboBox cmbUrl;
		private ComboBox cmbStreamType;
		private Label lblStreamType;
		private Label lblUrl;
		private Button btnDeviceProperties;
		private Label lblNoConf;
		private ComboBox cmbOtherDevices;
		private Button btnCamcorder;
		private GroupBox gpOtherDevices;
		private Label lblConfig;
		private ComboBox cmbCapabilities;
		private GroupBox gpCurrentDevice;
		private Label lblCurrentlySelected;
		private Button btnCancel;
		private Button btnApply;
	}
}
