﻿using System.ComponentModel;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    partial class ThumbListViewItem
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
        	this.lblFileName = new System.Windows.Forms.Label();
        	this.picBox = new System.Windows.Forms.PictureBox();
        	this.tbFileName = new System.Windows.Forms.TextBox();
        	((System.ComponentModel.ISupportInitialize)(this.picBox)).BeginInit();
        	this.SuspendLayout();
        	// 
        	// lblFileName
        	// 
        	this.lblFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
        	        	        	| System.Windows.Forms.AnchorStyles.Right)));
        	this.lblFileName.BackColor = System.Drawing.Color.Transparent;
        	this.lblFileName.Cursor = System.Windows.Forms.Cursors.Hand;
        	this.lblFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        	this.lblFileName.ForeColor = System.Drawing.Color.Black;
        	this.lblFileName.Location = new System.Drawing.Point(0, 180);
        	this.lblFileName.Name = "lblFileName";
        	this.lblFileName.Size = new System.Drawing.Size(240, 15);
        	this.lblFileName.TabIndex = 1;
        	this.lblFileName.Text = "FileName";
        	this.lblFileName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        	this.lblFileName.TextChanged += new System.EventHandler(this.lblFileName_TextChanged);
        	this.lblFileName.DoubleClick += new System.EventHandler(this.ThumbListViewItem_DoubleClick);
        	this.lblFileName.Click += new System.EventHandler(this.LblFileNameClick);
        	// 
        	// picBox
        	// 
        	this.picBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
        	        	        	| System.Windows.Forms.AnchorStyles.Right)));
        	this.picBox.BackColor = System.Drawing.Color.WhiteSmoke;
        	this.picBox.BackgroundImage = global::Kinovea.ScreenManager.Properties.Resources.missing3;
        	this.picBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
        	this.picBox.Cursor = System.Windows.Forms.Cursors.Hand;
        	this.picBox.Location = new System.Drawing.Point(3, 3);
        	this.picBox.Name = "picBox";
        	this.picBox.Size = new System.Drawing.Size(234, 174);
        	this.picBox.TabIndex = 0;
        	this.picBox.TabStop = false;
        	this.picBox.DoubleClick += new System.EventHandler(this.ThumbListViewItem_DoubleClick);
        	this.picBox.MouseLeave += new System.EventHandler(this.PicBoxMouseLeave);
        	this.picBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PicBoxMouseMove);
        	this.picBox.Click += new System.EventHandler(this.ThumbListViewItem_Click);
        	this.picBox.Paint += new System.Windows.Forms.PaintEventHandler(this.PicBoxPaint);
        	this.picBox.MouseEnter += new System.EventHandler(this.PicBoxMouseEnter);
        	// 
        	// tbFileName
        	// 
        	this.tbFileName.Anchor = System.Windows.Forms.AnchorStyles.None;
        	this.tbFileName.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        	this.tbFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
        	this.tbFileName.Location = new System.Drawing.Point(166, 168);
        	this.tbFileName.MaxLength = 255;
        	this.tbFileName.Name = "tbFileName";
        	this.tbFileName.Size = new System.Drawing.Size(58, 18);
        	this.tbFileName.TabIndex = 3;
        	this.tbFileName.Visible = false;
        	this.tbFileName.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.TbFileNameKeyPress);
        	// 
        	// ThumbListViewItem
        	// 
        	this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        	this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        	this.BackColor = System.Drawing.Color.WhiteSmoke;
        	this.Controls.Add(this.tbFileName);
        	this.Controls.Add(this.lblFileName);
        	this.Controls.Add(this.picBox);
        	this.Name = "ThumbListViewItem";
        	this.Size = new System.Drawing.Size(240, 195);
        	this.DoubleClick += new System.EventHandler(this.ThumbListViewItem_DoubleClick);
        	this.Paint += new System.Windows.Forms.PaintEventHandler(this.ThumbListViewItemPaint);
        	this.Click += new System.EventHandler(this.ThumbListViewItem_Click);
        	((System.ComponentModel.ISupportInitialize)(this.picBox)).EndInit();
        	this.ResumeLayout(false);
        	this.PerformLayout();
        }
        public System.Windows.Forms.TextBox tbFileName;

        #endregion

        public PictureBox picBox;
        public Label lblFileName;
    }
}
