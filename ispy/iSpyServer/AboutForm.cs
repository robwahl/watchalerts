using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace iSpyServer
{
    /// <summary>
    /// Summary description for AboutForm.
    /// </summary>
    public class AboutForm : Form
    {
        private Label _lblCopyright;
        private PictureBox _pictureBox1;
        private LinkLabel _linkLabel2;
        private Label _label1;
        private Label _lblVersion;
        private Button _btnOk;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private readonly Container _components = null;

        public AboutForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            RenderResources();
            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }

        private void RenderResources()
        {
            _lblVersion.Text = string.Format("iSpyServer v{0}", Application.ProductVersion);
            Text = LocRM.GetString("AboutiSpy");
            _label1.Text = LocRM.GetString("HomePage");
            _lblCopyright.Text = @"Copyright " + DateTime.Now.Year;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_components != null)
                {
                    _components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._lblCopyright = new System.Windows.Forms.Label();
            this._pictureBox1 = new System.Windows.Forms.PictureBox();
            this._btnOk = new System.Windows.Forms.Button();
            this._linkLabel2 = new System.Windows.Forms.LinkLabel();
            this._label1 = new System.Windows.Forms.Label();
            this._lblVersion = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this._pictureBox1)).BeginInit();
            this.SuspendLayout();
            //
            // lblCopyright
            //
            this._lblCopyright.Location = new System.Drawing.Point(147, 28);
            this._lblCopyright.Name = "_lblCopyright";
            this._lblCopyright.Size = new System.Drawing.Size(215, 16);
            this._lblCopyright.TabIndex = 13;
            this._lblCopyright.Text = "Copyright © 2011 iSpyConnect.com";
            this._lblCopyright.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // pictureBox1
            //
            this._pictureBox1.Image = global::iSpyServer.Properties.Resources.ispy;
            this._pictureBox1.Location = new System.Drawing.Point(12, 12);
            this._pictureBox1.Name = "_pictureBox1";
            this._pictureBox1.Size = new System.Drawing.Size(129, 124);
            this._pictureBox1.TabIndex = 17;
            this._pictureBox1.TabStop = false;
            //
            // btnOK
            //
            this._btnOk.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnOk.Location = new System.Drawing.Point(298, 107);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(75, 23);
            this._btnOk.TabIndex = 19;
            this._btnOk.Text = "OK";
            this._btnOk.UseVisualStyleBackColor = true;
            this._btnOk.Click += new System.EventHandler(this.btnOK_Click);
            //
            // linkLabel2
            //
            this._linkLabel2.AutoSize = true;
            this._linkLabel2.Location = new System.Drawing.Point(179, 72);
            this._linkLabel2.Name = "_linkLabel2";
            this._linkLabel2.Size = new System.Drawing.Size(145, 13);
            this._linkLabel2.TabIndex = 20;
            this._linkLabel2.TabStop = true;
            this._linkLabel2.Text = "http://www.ispyconnect.com";
            this._linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);
            //
            // label1
            //
            this._label1.AutoSize = true;
            this._label1.Location = new System.Drawing.Point(147, 48);
            this._label1.Name = "_label1";
            this._label1.Size = new System.Drawing.Size(65, 13);
            this._label1.TabIndex = 19;
            this._label1.Text = "Homepage: ";
            //
            // lblVersion
            //
            this._lblVersion.AutoSize = true;
            this._lblVersion.Location = new System.Drawing.Point(147, 12);
            this._lblVersion.Name = "_lblVersion";
            this._lblVersion.Size = new System.Drawing.Size(42, 13);
            this._lblVersion.TabIndex = 18;
            this._lblVersion.Text = "Version";
            //
            // AboutForm
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(392, 150);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._linkLabel2);
            this.Controls.Add(this._pictureBox1);
            this.Controls.Add(this._label1);
            this.Controls.Add(this._lblCopyright);
            this.Controls.Add(this._lblVersion);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.Load += new System.EventHandler(this.AboutForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this._pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion Windows Form Designer generated code

        private void AboutForm_Load(object sender, EventArgs e)
        {
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.StartBrowser("http://www.ispyconnect.com/");
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}