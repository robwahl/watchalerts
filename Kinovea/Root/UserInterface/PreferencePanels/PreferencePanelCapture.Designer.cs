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
using Kinovea.Root.Properties;

namespace Kinovea.Root.UserInterface.PreferencePanels
{
    internal sealed partial class PreferencePanelCapture
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
			this.btnBrowseVideo = new Button();
			this.btnBrowseImage = new Button();
			this.tbVideoDirectory = new TextBox();
			this.tbImageDirectory = new TextBox();
			this.cmbImageFormat = new ComboBox();
			this.cmbVideoFormat = new ComboBox();
			this.lblVideoFormat = new Label();
			this.lblImageFormat = new Label();
			this.lblVideoDirectory = new Label();
			this.lblImageDirectory = new Label();
			this.tabNaming = new TabPage();
			this.btnResetCounter = new Button();
			this.lblCounter = new Label();
			this.btnIncrement = new Button();
			this.lblSecond = new Label();
			this.lblMinute = new Label();
			this.lblHour = new Label();
			this.btnHour = new Button();
			this.btnSecond = new Button();
			this.btnMinute = new Button();
			this.lblDay = new Label();
			this.lblMonth = new Label();
			this.lblYear = new Label();
			this.btnYear = new Button();
			this.btnDay = new Button();
			this.btnMonth = new Button();
			this.button1 = new Button();
			this.lblSample = new Label();
			this.tbPattern = new TextBox();
			this.rbPattern = new RadioButton();
			this.rbFreeText = new RadioButton();
			this.tabMemory = new TabPage();
			this.lblMemoryBuffer = new Label();
			this.trkMemoryBuffer = new TrackBar();
			this.tabSubPages.SuspendLayout();
			this.tabGeneral.SuspendLayout();
			this.tabNaming.SuspendLayout();
			this.tabMemory.SuspendLayout();
			((ISupportInitialize)(this.trkMemoryBuffer)).BeginInit();
			this.SuspendLayout();
			// 
			// tabSubPages
			// 
			this.tabSubPages.Controls.Add(this.tabGeneral);
			this.tabSubPages.Controls.Add(this.tabNaming);
			this.tabSubPages.Controls.Add(this.tabMemory);
			this.tabSubPages.Dock = DockStyle.Fill;
			this.tabSubPages.Location = new Point(0, 0);
			this.tabSubPages.Name = "tabSubPages";
			this.tabSubPages.SelectedIndex = 0;
			this.tabSubPages.Size = new Size(432, 236);
			this.tabSubPages.TabIndex = 0;
			// 
			// tabGeneral
			// 
			this.tabGeneral.Controls.Add(this.btnBrowseVideo);
			this.tabGeneral.Controls.Add(this.btnBrowseImage);
			this.tabGeneral.Controls.Add(this.tbVideoDirectory);
			this.tabGeneral.Controls.Add(this.tbImageDirectory);
			this.tabGeneral.Controls.Add(this.cmbImageFormat);
			this.tabGeneral.Controls.Add(this.cmbVideoFormat);
			this.tabGeneral.Controls.Add(this.lblVideoFormat);
			this.tabGeneral.Controls.Add(this.lblImageFormat);
			this.tabGeneral.Controls.Add(this.lblVideoDirectory);
			this.tabGeneral.Controls.Add(this.lblImageDirectory);
			this.tabGeneral.Location = new Point(4, 22);
			this.tabGeneral.Name = "tabGeneral";
			this.tabGeneral.Padding = new Padding(3);
			this.tabGeneral.Size = new Size(424, 210);
			this.tabGeneral.TabIndex = 0;
			this.tabGeneral.Text = "General";
			this.tabGeneral.UseVisualStyleBackColor = true;
			// 
			// btnBrowseVideo
			// 
			this.btnBrowseVideo.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
			this.btnBrowseVideo.BackgroundImageLayout = ImageLayout.None;
			this.btnBrowseVideo.Cursor = Cursors.Hand;
			this.btnBrowseVideo.FlatAppearance.BorderSize = 0;
			this.btnBrowseVideo.FlatAppearance.MouseOverBackColor = Color.WhiteSmoke;
			this.btnBrowseVideo.FlatStyle = FlatStyle.Flat;
			this.btnBrowseVideo.Image = Resources.folder;
			this.btnBrowseVideo.Location = new Point(375, 54);
			this.btnBrowseVideo.MinimumSize = new Size(25, 25);
			this.btnBrowseVideo.Name = "btnBrowseVideo";
			this.btnBrowseVideo.Size = new Size(30, 25);
			this.btnBrowseVideo.TabIndex = 37;
			this.btnBrowseVideo.Tag = "";
			this.btnBrowseVideo.UseVisualStyleBackColor = true;
			this.btnBrowseVideo.Click += new EventHandler(this.btnBrowseVideoLocation_Click);
			// 
			// btnBrowseImage
			// 
			this.btnBrowseImage.Anchor = ((AnchorStyles)((AnchorStyles.Top | AnchorStyles.Right)));
			this.btnBrowseImage.BackgroundImageLayout = ImageLayout.None;
			this.btnBrowseImage.Cursor = Cursors.Hand;
			this.btnBrowseImage.FlatAppearance.BorderSize = 0;
			this.btnBrowseImage.FlatAppearance.MouseOverBackColor = Color.WhiteSmoke;
			this.btnBrowseImage.FlatStyle = FlatStyle.Flat;
			this.btnBrowseImage.Image = Resources.folder;
			this.btnBrowseImage.Location = new Point(375, 25);
			this.btnBrowseImage.MinimumSize = new Size(25, 25);
			this.btnBrowseImage.Name = "btnBrowseImage";
			this.btnBrowseImage.Size = new Size(30, 25);
			this.btnBrowseImage.TabIndex = 36;
			this.btnBrowseImage.Tag = "";
			this.btnBrowseImage.UseVisualStyleBackColor = true;
			this.btnBrowseImage.Click += new EventHandler(this.btnBrowseImageLocation_Click);
			// 
			// tbVideoDirectory
			// 
			this.tbVideoDirectory.Location = new Point(171, 59);
			this.tbVideoDirectory.Name = "tbVideoDirectory";
			this.tbVideoDirectory.Size = new Size(198, 20);
			this.tbVideoDirectory.TabIndex = 7;
			this.tbVideoDirectory.TextChanged += new EventHandler(this.tbVideoDirectory_TextChanged);
			// 
			// tbImageDirectory
			// 
			this.tbImageDirectory.Location = new Point(171, 30);
			this.tbImageDirectory.Name = "tbImageDirectory";
			this.tbImageDirectory.Size = new Size(198, 20);
			this.tbImageDirectory.TabIndex = 6;
			this.tbImageDirectory.TextChanged += new EventHandler(this.tbImageDirectory_TextChanged);
			// 
			// cmbImageFormat
			// 
			this.cmbImageFormat.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cmbImageFormat.FormattingEnabled = true;
			this.cmbImageFormat.Location = new Point(171, 105);
			this.cmbImageFormat.Name = "cmbImageFormat";
			this.cmbImageFormat.Size = new Size(52, 21);
			this.cmbImageFormat.TabIndex = 5;
			this.cmbImageFormat.SelectedIndexChanged += new EventHandler(this.cmbImageFormat_SelectedIndexChanged);
			// 
			// cmbVideoFormat
			// 
			this.cmbVideoFormat.DropDownStyle = ComboBoxStyle.DropDownList;
			this.cmbVideoFormat.FormattingEnabled = true;
			this.cmbVideoFormat.Location = new Point(171, 137);
			this.cmbVideoFormat.Name = "cmbVideoFormat";
			this.cmbVideoFormat.Size = new Size(52, 21);
			this.cmbVideoFormat.TabIndex = 4;
			this.cmbVideoFormat.SelectedIndexChanged += new EventHandler(this.cmbVideoFormat_SelectedIndexChanged);
			// 
			// lblVideoFormat
			// 
			this.lblVideoFormat.Location = new Point(16, 140);
			this.lblVideoFormat.Name = "lblVideoFormat";
			this.lblVideoFormat.Size = new Size(149, 18);
			this.lblVideoFormat.TabIndex = 3;
			this.lblVideoFormat.Text = "Video format :";
			// 
			// lblImageFormat
			// 
			this.lblImageFormat.Location = new Point(16, 108);
			this.lblImageFormat.Name = "lblImageFormat";
			this.lblImageFormat.Size = new Size(149, 18);
			this.lblImageFormat.TabIndex = 2;
			this.lblImageFormat.Text = "Image format :";
			// 
			// lblVideoDirectory
			// 
			this.lblVideoDirectory.Location = new Point(16, 62);
			this.lblVideoDirectory.Name = "lblVideoDirectory";
			this.lblVideoDirectory.Size = new Size(149, 17);
			this.lblVideoDirectory.TabIndex = 1;
			this.lblVideoDirectory.Text = "Video directory :";
			// 
			// lblImageDirectory
			// 
			this.lblImageDirectory.Location = new Point(16, 33);
			this.lblImageDirectory.Name = "lblImageDirectory";
			this.lblImageDirectory.Size = new Size(149, 17);
			this.lblImageDirectory.TabIndex = 0;
			this.lblImageDirectory.Text = "Image directory :";
			// 
			// tabNaming
			// 
			this.tabNaming.Controls.Add(this.btnResetCounter);
			this.tabNaming.Controls.Add(this.lblCounter);
			this.tabNaming.Controls.Add(this.btnIncrement);
			this.tabNaming.Controls.Add(this.lblSecond);
			this.tabNaming.Controls.Add(this.lblMinute);
			this.tabNaming.Controls.Add(this.lblHour);
			this.tabNaming.Controls.Add(this.btnHour);
			this.tabNaming.Controls.Add(this.btnSecond);
			this.tabNaming.Controls.Add(this.btnMinute);
			this.tabNaming.Controls.Add(this.lblDay);
			this.tabNaming.Controls.Add(this.lblMonth);
			this.tabNaming.Controls.Add(this.lblYear);
			this.tabNaming.Controls.Add(this.btnYear);
			this.tabNaming.Controls.Add(this.btnDay);
			this.tabNaming.Controls.Add(this.btnMonth);
			this.tabNaming.Controls.Add(this.button1);
			this.tabNaming.Controls.Add(this.lblSample);
			this.tabNaming.Controls.Add(this.tbPattern);
			this.tabNaming.Controls.Add(this.rbPattern);
			this.tabNaming.Controls.Add(this.rbFreeText);
			this.tabNaming.Location = new Point(4, 22);
			this.tabNaming.Name = "tabNaming";
			this.tabNaming.Padding = new Padding(3);
			this.tabNaming.Size = new Size(424, 210);
			this.tabNaming.TabIndex = 1;
			this.tabNaming.Text = "File naming";
			this.tabNaming.UseVisualStyleBackColor = true;
			// 
			// btnResetCounter
			// 
			this.btnResetCounter.BackColor = Color.Transparent;
			this.btnResetCounter.Cursor = Cursors.Hand;
			this.btnResetCounter.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.btnResetCounter.Location = new Point(266, 168);
			this.btnResetCounter.Name = "btnResetCounter";
			this.btnResetCounter.Size = new Size(142, 25);
			this.btnResetCounter.TabIndex = 21;
			this.btnResetCounter.Text = "Reset counters";
			this.btnResetCounter.UseVisualStyleBackColor = false;
			this.btnResetCounter.Click += new EventHandler(this.btnResetCounter_Click);
			// 
			// lblCounter
			// 
			this.lblCounter.AutoSize = true;
			this.lblCounter.Cursor = Cursors.Hand;
			this.lblCounter.Location = new Point(312, 119);
			this.lblCounter.Name = "lblCounter";
			this.lblCounter.Size = new Size(44, 13);
			this.lblCounter.TabIndex = 20;
			this.lblCounter.Text = "Counter";
			this.lblCounter.Click += new EventHandler(this.lblMarker_Click);
			// 
			// btnIncrement
			// 
			this.btnIncrement.BackColor = Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
			this.btnIncrement.Cursor = Cursors.Hand;
			this.btnIncrement.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.btnIncrement.Location = new Point(266, 113);
			this.btnIncrement.Name = "btnIncrement";
			this.btnIncrement.Size = new Size(40, 25);
			this.btnIncrement.TabIndex = 19;
			this.btnIncrement.Text = "%i";
			this.btnIncrement.TextAlign = ContentAlignment.TopCenter;
			this.btnIncrement.UseVisualStyleBackColor = false;
			this.btnIncrement.Click += new EventHandler(this.btnMarker_Click);
			// 
			// lblSecond
			// 
			this.lblSecond.AutoSize = true;
			this.lblSecond.Cursor = Cursors.Hand;
			this.lblSecond.Location = new Point(198, 174);
			this.lblSecond.Name = "lblSecond";
			this.lblSecond.Size = new Size(44, 13);
			this.lblSecond.TabIndex = 18;
			this.lblSecond.Text = "Second";
			this.lblSecond.Click += new EventHandler(this.lblMarker_Click);
			// 
			// lblMinute
			// 
			this.lblMinute.AutoSize = true;
			this.lblMinute.Cursor = Cursors.Hand;
			this.lblMinute.Location = new Point(198, 146);
			this.lblMinute.Name = "lblMinute";
			this.lblMinute.Size = new Size(39, 13);
			this.lblMinute.TabIndex = 17;
			this.lblMinute.Text = "Minute";
			this.lblMinute.Click += new EventHandler(this.lblMarker_Click);
			// 
			// lblHour
			// 
			this.lblHour.AutoSize = true;
			this.lblHour.Cursor = Cursors.Hand;
			this.lblHour.Location = new Point(198, 119);
			this.lblHour.Name = "lblHour";
			this.lblHour.Size = new Size(30, 13);
			this.lblHour.TabIndex = 16;
			this.lblHour.Text = "Hour";
			this.lblHour.Click += new EventHandler(this.lblMarker_Click);
			// 
			// btnHour
			// 
			this.btnHour.BackColor = Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
			this.btnHour.Cursor = Cursors.Hand;
			this.btnHour.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.btnHour.Location = new Point(152, 113);
			this.btnHour.Name = "btnHour";
			this.btnHour.Size = new Size(40, 25);
			this.btnHour.TabIndex = 15;
			this.btnHour.Text = "%h";
			this.btnHour.TextAlign = ContentAlignment.TopCenter;
			this.btnHour.UseVisualStyleBackColor = false;
			this.btnHour.Click += new EventHandler(this.btnMarker_Click);
			// 
			// btnSecond
			// 
			this.btnSecond.BackColor = Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(255)))));
			this.btnSecond.Cursor = Cursors.Hand;
			this.btnSecond.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.btnSecond.Location = new Point(152, 168);
			this.btnSecond.Name = "btnSecond";
			this.btnSecond.Size = new Size(40, 25);
			this.btnSecond.TabIndex = 14;
			this.btnSecond.Text = "%s";
			this.btnSecond.TextAlign = ContentAlignment.TopCenter;
			this.btnSecond.UseVisualStyleBackColor = false;
			this.btnSecond.Click += new EventHandler(this.btnMarker_Click);
			// 
			// btnMinute
			// 
			this.btnMinute.BackColor = Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.btnMinute.Cursor = Cursors.Hand;
			this.btnMinute.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.btnMinute.Location = new Point(152, 140);
			this.btnMinute.Name = "btnMinute";
			this.btnMinute.Size = new Size(40, 25);
			this.btnMinute.TabIndex = 13;
			this.btnMinute.Text = "%mi";
			this.btnMinute.UseVisualStyleBackColor = false;
			this.btnMinute.Click += new EventHandler(this.btnMarker_Click);
			// 
			// lblDay
			// 
			this.lblDay.AutoSize = true;
			this.lblDay.Cursor = Cursors.Hand;
			this.lblDay.Location = new Point(83, 174);
			this.lblDay.Name = "lblDay";
			this.lblDay.Size = new Size(26, 13);
			this.lblDay.TabIndex = 12;
			this.lblDay.Text = "Day";
			this.lblDay.Click += new EventHandler(this.lblMarker_Click);
			// 
			// lblMonth
			// 
			this.lblMonth.AutoSize = true;
			this.lblMonth.Cursor = Cursors.Hand;
			this.lblMonth.Location = new Point(83, 146);
			this.lblMonth.Name = "lblMonth";
			this.lblMonth.Size = new Size(37, 13);
			this.lblMonth.TabIndex = 11;
			this.lblMonth.Text = "Month";
			this.lblMonth.Click += new EventHandler(this.lblMarker_Click);
			// 
			// lblYear
			// 
			this.lblYear.AutoSize = true;
			this.lblYear.Cursor = Cursors.Hand;
			this.lblYear.Location = new Point(83, 119);
			this.lblYear.Name = "lblYear";
			this.lblYear.Size = new Size(29, 13);
			this.lblYear.TabIndex = 10;
			this.lblYear.Text = "Year";
			this.lblYear.Click += new EventHandler(this.lblMarker_Click);
			// 
			// btnYear
			// 
			this.btnYear.BackColor = Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(192)))), ((int)(((byte)(192)))));
			this.btnYear.Cursor = Cursors.Hand;
			this.btnYear.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.btnYear.Location = new Point(37, 113);
			this.btnYear.Name = "btnYear";
			this.btnYear.Size = new Size(40, 25);
			this.btnYear.TabIndex = 9;
			this.btnYear.Text = "%y";
			this.btnYear.TextAlign = ContentAlignment.TopCenter;
			this.btnYear.UseVisualStyleBackColor = false;
			this.btnYear.Click += new EventHandler(this.btnMarker_Click);
			// 
			// btnDay
			// 
			this.btnDay.BackColor = Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
			this.btnDay.Cursor = Cursors.Hand;
			this.btnDay.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.btnDay.Location = new Point(37, 168);
			this.btnDay.Name = "btnDay";
			this.btnDay.Size = new Size(40, 25);
			this.btnDay.TabIndex = 8;
			this.btnDay.Text = "%d";
			this.btnDay.TextAlign = ContentAlignment.TopCenter;
			this.btnDay.UseVisualStyleBackColor = false;
			this.btnDay.Click += new EventHandler(this.btnMarker_Click);
			// 
			// btnMonth
			// 
			this.btnMonth.BackColor = Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(224)))), ((int)(((byte)(192)))));
			this.btnMonth.Cursor = Cursors.Hand;
			this.btnMonth.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.btnMonth.Location = new Point(37, 140);
			this.btnMonth.Name = "btnMonth";
			this.btnMonth.Size = new Size(40, 25);
			this.btnMonth.TabIndex = 6;
			this.btnMonth.Text = "%mo";
			this.btnMonth.UseVisualStyleBackColor = false;
			this.btnMonth.Click += new EventHandler(this.btnMarker_Click);
			// 
			// button1
			// 
			this.button1.BackColor = Color.Transparent;
			this.button1.BackgroundImage = Resources.bullet_go;
			this.button1.BackgroundImageLayout = ImageLayout.Center;
			this.button1.FlatAppearance.BorderSize = 0;
			this.button1.FlatStyle = FlatStyle.Flat;
			this.button1.Location = new Point(210, 71);
			this.button1.Name = "button1";
			this.button1.Size = new Size(20, 20);
			this.button1.TabIndex = 5;
			this.button1.UseVisualStyleBackColor = false;
			// 
			// lblSample
			// 
			this.lblSample.BackColor = Color.WhiteSmoke;
			this.lblSample.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.lblSample.Location = new Point(236, 71);
			this.lblSample.Name = "lblSample";
			this.lblSample.Size = new Size(172, 21);
			this.lblSample.TabIndex = 4;
			this.lblSample.Text = "[computed value]";
			this.lblSample.TextAlign = ContentAlignment.MiddleLeft;
			// 
			// tbPattern
			// 
			this.tbPattern.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
			this.tbPattern.Location = new Point(51, 71);
			this.tbPattern.Name = "tbPattern";
			this.tbPattern.Size = new Size(153, 20);
			this.tbPattern.TabIndex = 2;
			this.tbPattern.Text = "Cap-%y-%mo-%d - %i";
			this.tbPattern.TextChanged += new EventHandler(this.tbPattern_TextChanged);
			// 
			// rbPattern
			// 
			this.rbPattern.Location = new Point(20, 43);
			this.rbPattern.Name = "rbPattern";
			this.rbPattern.Size = new Size(286, 22);
			this.rbPattern.TabIndex = 1;
			this.rbPattern.TabStop = true;
			this.rbPattern.Text = "Naming pattern";
			this.rbPattern.UseVisualStyleBackColor = true;
			this.rbPattern.CheckedChanged += new EventHandler(this.radio_CheckedChanged);
			// 
			// rbFreeText
			// 
			this.rbFreeText.Location = new Point(20, 14);
			this.rbFreeText.Name = "rbFreeText";
			this.rbFreeText.Size = new Size(286, 20);
			this.rbFreeText.TabIndex = 0;
			this.rbFreeText.TabStop = true;
			this.rbFreeText.Text = "Free text with automatic counter";
			this.rbFreeText.UseVisualStyleBackColor = true;
			this.rbFreeText.CheckedChanged += new EventHandler(this.radio_CheckedChanged);
			// 
			// tabMemory
			// 
			this.tabMemory.Controls.Add(this.lblMemoryBuffer);
			this.tabMemory.Controls.Add(this.trkMemoryBuffer);
			this.tabMemory.Location = new Point(4, 22);
			this.tabMemory.Name = "tabMemory";
			this.tabMemory.Size = new Size(424, 210);
			this.tabMemory.TabIndex = 2;
			this.tabMemory.Text = "Memory";
			this.tabMemory.UseVisualStyleBackColor = true;
			// 
			// lblMemoryBuffer
			// 
			this.lblMemoryBuffer.AutoSize = true;
			this.lblMemoryBuffer.Location = new Point(15, 30);
			this.lblMemoryBuffer.Name = "lblMemoryBuffer";
			this.lblMemoryBuffer.Size = new Size(221, 13);
			this.lblMemoryBuffer.TabIndex = 36;
			this.lblMemoryBuffer.Text = "Memory allocated for capture buffers : {0} MB";
			// 
			// trkMemoryBuffer
			// 
			this.trkMemoryBuffer.BackColor = Color.White;
			this.trkMemoryBuffer.Location = new Point(15, 55);
			this.trkMemoryBuffer.Maximum = 1024;
			this.trkMemoryBuffer.Minimum = 16;
			this.trkMemoryBuffer.Name = "trkMemoryBuffer";
			this.trkMemoryBuffer.Size = new Size(386, 45);
			this.trkMemoryBuffer.TabIndex = 38;
			this.trkMemoryBuffer.TickFrequency = 50;
			this.trkMemoryBuffer.Value = 16;
			this.trkMemoryBuffer.ValueChanged += new EventHandler(this.trkMemoryBuffer_ValueChanged);
			// 
			// PreferencePanelCapture
			// 
			this.AutoScaleDimensions = new SizeF(6F, 13F);
			this.AutoScaleMode = AutoScaleMode.Font;
			this.Controls.Add(this.tabSubPages);
			this.Name = "PreferencePanelCapture";
			this.Size = new Size(432, 236);
			this.tabSubPages.ResumeLayout(false);
			this.tabGeneral.ResumeLayout(false);
			this.tabGeneral.PerformLayout();
			this.tabNaming.ResumeLayout(false);
			this.tabNaming.PerformLayout();
			this.tabMemory.ResumeLayout(false);
			this.tabMemory.PerformLayout();
			((ISupportInitialize)(this.trkMemoryBuffer)).EndInit();
			this.ResumeLayout(false);
		}
		private Label lblMemoryBuffer;
		private TrackBar trkMemoryBuffer;
		private TabPage tabMemory;
		private Label lblVideoFormat;
		private Label lblImageFormat;
		private Label lblVideoDirectory;
		private Label lblImageDirectory;
		private Button btnBrowseImage;
		private Button btnBrowseVideo;
		private TextBox tbImageDirectory;
		private TextBox tbVideoDirectory;
		private ComboBox cmbImageFormat;
		private ComboBox cmbVideoFormat;
		private Label lblCounter;
		private Label lblSecond;
		private Label lblMinute;
		private Label lblHour;
		private Label lblDay;
		private Label lblMonth;
		private Label lblYear;
		private RadioButton rbPattern;
		private RadioButton rbFreeText;
		private Button btnResetCounter;
		private Button btnIncrement;
		private Button btnHour;
		private Button btnSecond;
		private Button btnMinute;
		private Button btnDay;
		private Button btnMonth;
		private Button btnYear;
		private Label lblSample;
		private TextBox tbPattern;
		private Button button1;
		private TabControl tabSubPages;
		private TabPage tabGeneral;
		private TabPage tabNaming;
	}
}
