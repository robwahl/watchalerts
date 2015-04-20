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

using Kinovea.Services;
using System;
using System.Windows.Forms;

namespace Kinovea.Root
{
    public partial class SupervisorUserInterface : UserControl
    {
        #region Properties

        public bool IsExplorerCollapsed { get; private set; }

        #endregion Properties

        #region Members

        private int _mIOldSplitterDistance;
        private readonly RootKernel _rootKernel;
        private bool _isOpening;
        private readonly PreferencesManager _mPrefManager;
        private bool _mBInitialized;

        #endregion Members

        #region Construction Destruction

        public SupervisorUserInterface(RootKernel rootKernel)
        {
            _rootKernel = rootKernel;
            InitializeComponent();
            _mBInitialized = false;

            // Get Explorer values from settings.
            _mPrefManager = PreferencesManager.Instance();
            _mIOldSplitterDistance = _mPrefManager.ExplorerSplitterDistance;

            // Services offered here
            var dp = DelegatesPool.Instance();
            dp.OpenVideoFile = DoOpenVideoFile;
        }

        private void SupervisorUserInterface_Load(object sender, EventArgs e)
        {
            if (CommandLineArgumentManager.Instance().HideExplorer || !_mPrefManager.ExplorerVisible)
            {
                CollapseExplorer();
            }
            else
            {
                ExpandExplorer(true);
            }
            _mBInitialized = true;
        }

        #endregion Construction Destruction

        #region Event Handlers

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            // Finished moving the splitter.

            splitWorkSpace.Panel1.Refresh();

            if (_mBInitialized)
            {
                _mPrefManager.ExplorerSplitterDistance = splitWorkSpace.SplitterDistance;
                _mPrefManager.ExplorerVisible = true;
                _mPrefManager.Export();
            }
        }

        public void DoOpenVideoFile()
        {
            // Open a video.
            if ((_rootKernel.ScreenManager.ScreenList.Count == 0) && (!_isOpening))
            {
                _isOpening = true;

                var filePath = _rootKernel.LaunchOpenFileDialog();
                if (filePath.Length > 0)
                {
                    var dp = DelegatesPool.Instance();
                    if (dp.LoadMovieInScreen != null)
                    {
                        dp.LoadMovieInScreen(filePath, -1, true);
                    }
                }

                _isOpening = false;
            }
        }

        private void buttonCloseExplo_Click(object sender, EventArgs e)
        {
            CollapseExplorer();
        }

        private void _splitWorkSpace_DoubleClick(object sender, EventArgs e)
        {
            if (IsExplorerCollapsed)
            {
                ExpandExplorer(true);
            }
            else
            {
                CollapseExplorer();
            }
        }

        private void _splitWorkSpace_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsExplorerCollapsed && splitWorkSpace.SplitterDistance > 30)
            {
                ExpandExplorer(false);
            }
        }

        private void splitWorkSpace_Panel1_Click(object sender, EventArgs e)
        {
            // Clic sur l'explorer
            if (IsExplorerCollapsed)
            {
                ExpandExplorer(true);
            }
        }

        #endregion Event Handlers

        #region Lower level methods

        public void CollapseExplorer()
        {
            splitWorkSpace.Panel2.SuspendLayout();
            splitWorkSpace.Panel1.SuspendLayout();

            if (_mBInitialized)
            {
                _mIOldSplitterDistance = splitWorkSpace.SplitterDistance;
            }
            else
            {
                _mIOldSplitterDistance = _mPrefManager.ExplorerSplitterDistance;
            }
            IsExplorerCollapsed = true;
            foreach (Control ctrl in splitWorkSpace.Panel1.Controls)
            {
                ctrl.Visible = false;
            }
            splitWorkSpace.SplitterDistance = 4;
            splitWorkSpace.SplitterWidth = 1;
            splitWorkSpace.BorderStyle = BorderStyle.None;
            _rootKernel.MnuToggleFileExplorer.Checked = false;

            splitWorkSpace.Panel1.ResumeLayout();
            splitWorkSpace.Panel2.ResumeLayout();

            _mPrefManager.ExplorerSplitterDistance = _mIOldSplitterDistance;
            _mPrefManager.ExplorerVisible = false;
            _mPrefManager.Export();
        }

        public void ExpandExplorer(bool resetSplitter)
        {
            if (_mIOldSplitterDistance != -1)
            {
                splitWorkSpace.Panel2.SuspendLayout();
                splitWorkSpace.Panel1.SuspendLayout();

                IsExplorerCollapsed = false;
                foreach (Control ctrl in splitWorkSpace.Panel1.Controls)
                {
                    ctrl.Visible = true;
                }
                if (resetSplitter)
                {
                    splitWorkSpace.SplitterDistance = _mIOldSplitterDistance;
                }
                splitWorkSpace.SplitterWidth = 4;
                splitWorkSpace.BorderStyle = BorderStyle.FixedSingle;
                _rootKernel.MnuToggleFileExplorer.Checked = true;

                splitWorkSpace.Panel1.ResumeLayout();
                splitWorkSpace.Panel2.ResumeLayout();

                _mPrefManager.ExplorerSplitterDistance = splitWorkSpace.SplitterDistance;
                _mPrefManager.ExplorerVisible = true;
                _mPrefManager.Export();
            }
        }

        #endregion Lower level methods
    }
}