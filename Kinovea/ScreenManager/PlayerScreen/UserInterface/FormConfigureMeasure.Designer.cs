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
	partial class FormConfigureMeasure
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
			this.tbMeasure = new TextBox();
			this.lblRealSize = new Label();
			this.cbUnit = new ComboBox();
			this.grpConfig.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnOK
			// 
			this.btnOK.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
			this.btnOK.DialogResult = DialogResult.OK;
			this.btnOK.Location = new Point(71, 115);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new Size(99, 24);
			this.btnOK.TabIndex = 25;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new EventHandler(this.btnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((AnchorStyles)((AnchorStyles.Bottom | AnchorStyles.Right)));
			this.btnCancel.DialogResult = DialogResult.Cancel;
			this.btnCancel.Location = new Point(176, 115);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new Size(99, 24);
			this.btnCancel.TabIndex = 30;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new EventHandler(this.btnCancel_Click);
			// 
			// grpConfig
			// 
			this.grpConfig.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom) 
									| AnchorStyles.Left) 
									| AnchorStyles.Right)));
			this.grpConfig.Controls.Add(this.cbUnit);
			this.grpConfig.Controls.Add(this.tbMeasure);
			this.grpConfig.Controls.Add(this.lblRealSize);
			this.grpConfig.Location = new Point(12, 12);
			this.grpConfig.Name = "grpConfig";
			this.grpConfig.Size = new Size(263, 95);
			this.grpConfig.TabIndex = 29;
			this.grpConfig.TabStop = false;
			this.grpConfig.Text = "Configuration";
			// 
			// tbMeasure
			// 
			this.tbMeasure.AcceptsReturn = true;
			this.tbMeasure.Location = new Point(28, 57);
			this.tbMeasure.MaxLength = 10;
			this.tbMeasure.Name = "tbMeasure";
			this.tbMeasure.Size = new Size(65, 20);
			this.tbMeasure.TabIndex = 24;
			this.tbMeasure.KeyPress += new KeyPressEventHandler(this.tbFPSOriginal_KeyPress);
			// 
			// lblRealSize
			// 
			this.lblRealSize.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) 
									| AnchorStyles.Right)));
			this.lblRealSize.Location = new Point(17, 22);
			this.lblRealSize.Name = "lblRealSize";
			this.lblRealSize.Size = new Size(229, 20);
			this.lblRealSize.TabIndex = 21;
			this.lblRealSize.Text = "dlgConfigureMeasure_lblRealSize";
			this.lblRealSize.TextAlign = ContentAlignment.BottomLeft;
			// 
			// cbUnit
			// 
			this.cbUnit.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cbUnit.FormattingEnabled = true;
			this.cbUnit.Location = new Point(99, 56);
			this.cbUnit.Name = "cbUnit";
			this.cbUnit.Size = new Size(125, 21);
			this.cbUnit.TabIndex = 25;
			// 
			// formConfigureMeasure
			// 
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.BackColor = Color.White;
			this.ClientSize = new Size(285, 147);
			this.Controls.Add(this.grpConfig);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnCancel);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FormConfigureMeasure";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = FormStartPosition.Manual;
			this.Text = "dlgConfigureMeasure_Title";
			this.grpConfig.ResumeLayout(false);
			this.grpConfig.PerformLayout();
			this.ResumeLayout(false);
        }
		private TextBox tbMeasure;
		private Label lblRealSize;
		private ComboBox cbUnit;

        

        private Button btnOK;
        private Button btnCancel;
        private GroupBox grpConfig;
	}
}
