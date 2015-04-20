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

using Kinovea.VideoFiles;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public abstract class AbstractScreen
    {
        public abstract Guid UniqueId { get; set; }

        public abstract bool Full { get; }

        public abstract UserControl Ui { get; }

        public abstract string FileName { get; }

        public abstract string Status { get; }

        public abstract string FilePath { get; }

        public abstract bool CapabilityDrawings { get; }

        public abstract AspectRatio AspectRatio { get; set; }

        public abstract void DisplayAsActiveScreen(bool bActive);

        public abstract void RefreshUiCulture();

        public abstract void BeforeClose();

        public abstract bool OnKeyPress(Keys key);

        public abstract void RefreshImage();

        public abstract void AddImageDrawing(string filename, bool bIsSvg);

        public abstract void AddImageDrawing(Bitmap bmp);

        public abstract void FullScreen(bool bFullScreen);
    }
}