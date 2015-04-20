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

using Kinovea.ScreenManager.Languages;
using Kinovea.ScreenManager.Properties;
using Kinovea.VideoFiles;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     VideoFilterReverse.
    ///     - Input			: All images.
    ///     - Output		: All images, same size.
    ///     - Operation 	: revert the order of the images.
    ///     - Type 			: Work on all frames at once.
    ///     - Previewable 	: No.
    /// </summary>
    public class VideoFilterReverse : AbstractVideoFilter
    {
        #region Properties

        public override string Name
        {
            get { return ScreenManagerLang.VideoFilterReverse_FriendlyName; }
        }

        public override Bitmap Icon
        {
            get { return Resources.revert; }
        }

        public override List<DecompressedFrame> FrameList
        {
            set { _mFrameList = value; }
        }

        public override bool Experimental
        {
            get { return false; }
        }

        #endregion Properties

        #region Members

        private List<DecompressedFrame> _mFrameList;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region AbstractVideoFilter Implementation

        public override void Menu_OnClick(object sender, EventArgs e)
        {
            // Direct call to Process because we don't need progress bar support.
            Process();
        }

        protected override void Process()
        {
            var mTempFrameList = new List<DecompressedFrame>();

            for (var i = _mFrameList.Count - 1; i >= 0; i--)
            {
                var iCurrent = _mFrameList.Count - 1 - i;

                var df = new DecompressedFrame();
                df.BmpImage = _mFrameList[i].BmpImage;
                df.iTimeStamp = _mFrameList[iCurrent].iTimeStamp;

                mTempFrameList.Add(df);
            }

            for (var i = 0; i < _mFrameList.Count; i++)
            {
                _mFrameList[i] = mTempFrameList[i];
            }

            ProcessingOver();
        }

        #endregion AbstractVideoFilter Implementation
    }
}