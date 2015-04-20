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
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: CLSCompliant(true)]

namespace Kinovea.FileBrowser
{
    [ComVisible(true)]
    public class FileBrowserKernel : IKernel
    {
        #region Members

        private readonly FileBrowserUserInterface _mFbui = new FileBrowserUserInterface();

        #endregion Members

        #region IKernel Implementation

        public void BuildSubTree()
        {
            // No sub modules.
        }

        public void ExtendMenu(ToolStrip menu)
        {
            // Nothing at this level.
            // No sub modules.
        }

        public void ExtendToolBar(ToolStrip toolbar)
        {
            if (toolbar == null) throw new ArgumentNullException("toolbar");
            // Nothing at this level.
            // No sub modules.
        }

        public void ExtendStatusBar(ToolStrip statusbar)
        {
            // Nothing at this level.
            // No sub modules.
        }

        public void ExtendUi()
        {
            // No sub modules.
        }

        public void RefreshUiCulture()
        {
            _mFbui.RefreshUiCulture();
        }

        public void CloseSubModules()
        {
            // No sub modules to close.

            // Save last browsed directory
            _mFbui.Closing();
        }

        #endregion IKernel Implementation
    }
}