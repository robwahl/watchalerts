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
using Kinovea.Services;
using Kinovea.VideoFiles;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     VideoFilterMosaic.
    ///     - Input			: Subset of all images (or all key images (?)).
    ///     - Output		: One image, same size.
    ///     - Operation 	: Combine input images into a single view.
    ///     - Type 			: Called at draw time.
    ///     - Previewable 	: No.
    /// </summary>
    public class VideoFilterMosaic : AbstractVideoFilter
    {
        /// <summary>
        ///     Class to hold the private parameters needed to draw the image.
        /// </summary>
        private class Parameters
        {
            public Parameters(List<Bitmap> frameList, int iFramesToExtract, bool bRightToLeft)
            {
                FrameList = frameList;
                FramesToExtract = iFramesToExtract;
                RightToLeft = bRightToLeft;
            }

            public void ChangeFrameCount(bool bIncrease)
            {
                // Increase the number of frames to take into account for the mosaic.
                var side = (int)Math.Sqrt(FramesToExtract);
                side = bIncrease ? Math.Min(10, side + 1) : Math.Max(2, side - 1);
                FramesToExtract = side * side;
            }

            #region Properties

            public List<Bitmap> FrameList { get; }

            public int FramesToExtract
            { // This value is always side². (4, 9, 16, 25, 49, etc.)
                get;
                private set;
            }

            public bool RightToLeft { get; }

            #endregion Properties
        }

        #region Properties

        public override string Name
        {
            get { return ScreenManagerLang.VideoFilterMosaic_FriendlyName; }
        }

        public override Bitmap Icon
        {
            get { return Resources.mosaic; }
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
            if (Menu.Checked)
            {
                var dfo = new DrawtimeFilterOutput((int)VideoFilterType.Mosaic, false);
                ProcessingOver(dfo);
            }
            else
            {
                var dfo = new DrawtimeFilterOutput((int)VideoFilterType.Mosaic, true);
                dfo.PrivateData = new Parameters(ExtractBitmapList(_mFrameList), 16, false);
                dfo.Draw = Draw;
                dfo.IncreaseZoom = IncreaseZoom;
                dfo.DecreaseZoom = DecreaseZoom;
                ProcessingOver(dfo);
            }
        }

        protected override void Process()
        {
            // Not implemented.
            // This filter process its imput frames at draw time only. See Draw().
        }

        #endregion AbstractVideoFilter Implementation

        #region DrawtimeFilterOutput Implementation

        public static void Draw(Graphics g, Size iNewSize, object privateData)
        {
            //-----------------------------------------------------------------------------------
            // This method will be called by a player screen at draw time.
            // static: the DrawingtimeFilterObject contains all that is needed to use the method.
            // Most notably, the _privateData parameters contains references to the frames
            // to be combined, the zoom level and if the composite is right to left or not.
            //-----------------------------------------------------------------------------------

            var sw = new Stopwatch();
            sw.Start();

            var parameters = privateData as Parameters;

            if (parameters != null)
            {
                // We recompute the image at each draw time.
                // We could have only computed it on first creation and on resize,
                // but in the end it doesn't matter.
                var selectedFrames = GetInputFrames(parameters.FrameList, parameters.FramesToExtract);

                if (selectedFrames != null && selectedFrames.Count > 0)
                {
                    g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                    g.CompositingQuality = CompositingQuality.HighSpeed;
                    g.InterpolationMode = InterpolationMode.Bilinear;
                    g.SmoothingMode = SmoothingMode.HighQuality;

                    //---------------------------------------------------------------------------
                    // We reserve n² placeholders, so we have exactly as many images on width than on height.
                    // Example:
                    // - 32 images as input.
                    // - We get up to next square root : 36. iSide is thus 6.
                    // - This will be 6x6 images (with the last 4 not filled)
                    // - Each image must be scaled down by a factor of 1/6.
                    //---------------------------------------------------------------------------

                    // to test:
                    // 1. Lock all images and get all BitmapData in an array.
                    // 2. Loop on final image pixels, and fill in by interpolating from the source images.
                    // This should get down to a few ms instead of more than 500 ms for HD vids.

                    var iSide = (int)Math.Ceiling(Math.Sqrt(selectedFrames.Count));
                    var iThumbWidth = iNewSize.Width / iSide;
                    var iThumbHeight = iNewSize.Height / iSide;

                    var rSrc = new Rectangle(0, 0, selectedFrames[0].Width, selectedFrames[0].Height);

                    // Configure font for image numbers.
                    var f = new Font("Arial", GetFontSize(iThumbWidth), FontStyle.Bold);

                    for (var i = 0; i < iSide; i++)
                    {
                        for (var j = 0; j < iSide; j++)
                        {
                            var iImageIndex = j * iSide + i;
                            if (iImageIndex < selectedFrames.Count && selectedFrames[iImageIndex] != null)
                            {
                                // compute left coord depending on "RightToLeft" status.
                                int iLeft;
                                if (parameters.RightToLeft)
                                {
                                    iLeft = (iSide - 1 - i) * iThumbWidth;
                                }
                                else
                                {
                                    iLeft = i * iThumbWidth;
                                }

                                var rDst = new Rectangle(iLeft, j * iThumbHeight, iThumbWidth, iThumbHeight);
                                g.DrawImage(selectedFrames[iImageIndex], rDst, rSrc, GraphicsUnit.Pixel);

                                // Draw the image number.
                                DrawImageNumber(g, iImageIndex, rDst, f);
                            }
                        }
                    }

                    f.Dispose();
                }
            }

            sw.Stop();
            Log.Debug(string.Format("Mosaic Draw : {0} ms.", sw.ElapsedMilliseconds));
        }

        public static void IncreaseZoom(object privateData)
        {
            var parameters = privateData as Parameters;
            if (parameters != null)
            {
                parameters.ChangeFrameCount(true);
            }
        }

        public static void DecreaseZoom(object privateData)
        {
            var parameters = privateData as Parameters;
            if (parameters != null)
            {
                parameters.ChangeFrameCount(false);
            }
        }

        #endregion DrawtimeFilterOutput Implementation

        #region Private methods

        private static List<Bitmap> GetInputFrames(List<Bitmap> frameList, int iFramesToExtract)
        {
            // Get the subset of images we will be using for the mosaic.

            var inputFrames = new List<Bitmap>();
            var fExtractStep = (double)frameList.Count / iFramesToExtract;

            var iExtracted = 0;
            for (var i = 0; i < frameList.Count; i++)
            {
                if (i >= iExtracted * fExtractStep)
                {
                    inputFrames.Add(frameList[i]);
                    iExtracted++;
                }
            }

            return inputFrames;
        }

        private List<Bitmap> ExtractBitmapList(List<DecompressedFrame> frameList)
        {
            // Simply create a list of bitmaps from the list of decompressed frames.

            var inputFrames = new List<Bitmap>();
            for (var i = 0; i < frameList.Count; i++)
            {
                inputFrames.Add(frameList[i].BmpImage);
            }

            return inputFrames;
        }

        private static int GetFontSize(int iThumbWidth)
        {
            // Return the font size for the image number based on the thumb width.
            var fontSize = 18;

            if (iThumbWidth >= 200)
            {
                fontSize = 18;
            }
            else if (iThumbWidth >= 150)
            {
                fontSize = 14;
            }
            else
            {
                fontSize = 10;
            }

            return fontSize;
        }

        private static void DrawImageNumber(Graphics canvas, int iImageIndex, Rectangle rDst, Font font)
        {
            var number = string.Format(" {0}", iImageIndex + 1);
            var bgSize = canvas.MeasureString(number, font);
            bgSize = new SizeF(bgSize.Width + 6, bgSize.Height + 2);

            // 1. Draw background.
            var gp = new GraphicsPath();
            gp.StartFigure();
            gp.AddLine(rDst.Left, rDst.Top, rDst.Left + bgSize.Width, rDst.Top);
            gp.AddLine(rDst.Left + bgSize.Width, rDst.Top, rDst.Left + bgSize.Width, rDst.Top + (bgSize.Height / 2));
            gp.AddArc(rDst.Left, rDst.Top, bgSize.Width, bgSize.Height, 0, 90);
            gp.AddLine(rDst.Left + (bgSize.Width / 2), rDst.Top + bgSize.Height, rDst.Left, rDst.Top + bgSize.Height);
            gp.CloseFigure();
            canvas.FillPath(Brushes.Black, gp);

            // 2. Draw image number.
            canvas.DrawString(number, font, Brushes.White, rDst.Location);
        }

        #endregion Private methods
    }
}