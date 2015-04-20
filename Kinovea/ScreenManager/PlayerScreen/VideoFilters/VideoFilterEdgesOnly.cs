#region License

/*
Copyright © Joan Charmant 2009.
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

#endregion License

using AForge.Imaging.Filters;
using Kinovea.ScreenManager.Languages;
using Kinovea.VideoFiles;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     VideoFilterEdgesOnly.
    ///     - Input			: All images.
    ///     - Output		: All images, same size.
    ///     - Operation 	: Turn video to black with only edges highlighted in white.
    ///     - Type 			: Work on each frame separately.
    ///     - Previewable 	: Yes.
    /// </summary>
    public class VideoFilterEdgesOnly : AbstractVideoFilter
    {
        #region Properties

        public override string Name
        {
            get { return ScreenManagerLang.VideoFilterEdgesOnly_FriendlyName; }
        }

        public override Bitmap Icon
        {
            get { return null; }
        }

        public override List<DecompressedFrame> FrameList
        {
            set { _mFrameList = value; }
        }

        public override bool Experimental
        {
            get { return true; }
        }

        #endregion Properties

        #region Members

        private List<DecompressedFrame> _mFrameList;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region AbstractVideoFilter Implementation

        public override void Menu_OnClick(object sender, EventArgs e)
        {
            // 1. Display preview dialog box.
            var preview = GetPreviewImage();
            var fpvf = new FormPreviewVideoFilter(preview, Name);
            if (fpvf.ShowDialog() == DialogResult.OK)
            {
                // 2. Process filter.
                StartProcessing();

                // Enregistrement de l'état courant du screen pour le undo.
                // m_MemoPlayerScreen = m_PlayerScreen.GetMemo();
            }
            fpvf.Dispose();
            preview.Dispose();
        }

        protected override void Process()
        {
            // Method called back from AbstractVideoFilter after a call to StartProcessing().
            // Use StartProcessing() to get progress bar and threading support.

            for (var i = 0; i < _mFrameList.Count; i++)
            {
                _mFrameList[i].BmpImage = ProcessSingleImage(_mFrameList[i].BmpImage);
                MBackgroundWorker.ReportProgress(i, _mFrameList.Count);
            }
        }

        #endregion AbstractVideoFilter Implementation

        #region Private methods

        private Bitmap GetPreviewImage()
        {
            // Deep clone an image then pass it to the filter.
            var bmp = CloneTo24Bpp(_mFrameList[(_mFrameList.Count - 1) / 2].BmpImage);
            return ProcessSingleImage(bmp);
        }

        private Bitmap ProcessSingleImage(Bitmap src)
        {
            // Apply filter.
            var img = (src.PixelFormat == PixelFormat.Format24bppRgb) ? src : CloneTo24Bpp(src);
            var tmp = new DifferenceEdgeDetector().Apply(Grayscale.CommonAlgorithms.BT709.Apply(img));
            src.Dispose();
            if (src.PixelFormat != PixelFormat.Format24bppRgb)
            {
                img.Dispose();
            }

            // Back to 24bpp.
            var tmp2 = new GrayscaleToRGB().Apply(tmp);
            tmp.Dispose();

            return tmp2;
        }

        #endregion Private methods
    }
}