/*
Copyright © Joan Charmant 2008.
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

using log4net;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.Root.UserInterface
{
    public partial class KinoveaMainWindow : Form
    {
        public void ToggleFullScreen()
        {
            // TODO: Does this work for multiple monitors ?

            SuspendLayout();

            FullScreen = !FullScreen;

            if (FullScreen)
            {
                _mMemoBounds = Bounds;
                _mMemoWindowState = WindowState;

                // Go full screen. We switch to normal state first, otherwise it doesn't work each time.
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Normal;
                var screen = Screen.FromControl(this);
                Bounds = screen.Bounds;

                menuStrip.Visible = false;
                toolStrip.Visible = false;
                statusStrip.Visible = false;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = _mMemoWindowState;
                Bounds = _mMemoBounds;

                menuStrip.Visible = true;
                toolStrip.Visible = true;
                statusStrip.Visible = true;
            }

            ResumeLayout();
        }

        #region Event Handlers

        private void UserInterface_FormClosing(object sender, FormClosingEventArgs e)
        {
            _mRootKernel.CloseSubModules();
        }

        #endregion Event Handlers

        #region Properties

        public SupervisorUserInterface SupervisorControl { get; set; }

        public bool FullScreen { get; private set; }

        protected override CreateParams CreateParams
        {
            // Fix flickering of controls during resize.
            // Ref. http://social.msdn.microsoft.com/forums/en-US/winforms/thread/aaed00ce-4bc9-424e-8c05-c30213171c2c/
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        #endregion Properties

        #region Members

        private readonly RootKernel _mRootKernel;
        private Rectangle _mMemoBounds;
        private FormWindowState _mMemoWindowState;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor

        public KinoveaMainWindow(RootKernel rootKernel)
        {
            Log.Debug("Create main UI window.");

            _mRootKernel = rootKernel;
            InitializeComponent();

            Text = " Kinovea";
            SupervisorControl = new SupervisorUserInterface(_mRootKernel);
            Controls.Add(SupervisorControl);
            SupervisorControl.Dock = DockStyle.Fill;
            SupervisorControl.BringToFront();
        }

        [Localizable(false)]
        public override sealed string Text
        {
            get { return base.Text; }
            set { base.Text = value; }
        }

        #endregion Constructor
    }
}