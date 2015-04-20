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

using AForge.Imaging;
using Kinovea.ScreenManager.Properties;
using Kinovea.VideoFiles;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     VideoFilterSandbox.
    ///     This filter is for testing purposes only.
    ///     It may be used to test a particular code or experiment.
    ///     It should never be available to the end-user.
    /// </summary>
    public class VideoFilterSandbox : AbstractVideoFilter
    {
        #region Properties

        public override string Name
        {
            get { return "Sandbox"; }
        }

        public override Bitmap Icon
        {
            get { return Resources.controller; }
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
            StartProcessing();
        }

        protected override void Process()
        {
            // Method called back from AbstractVideoFilter after a call to StartProcessing().

            //TestFindSurfFeatures();
            TestCreateYtSlices();
            //TestFrameInterpolation();
        }

        #endregion AbstractVideoFilter Implementation

        #region YTSlices

        private void TestCreateYtSlices()
        {
            // Create a number of YT-Slice images.
            // I call YT-Slice the image created over time by a specific column of pixel at X coordinate in the video.
            // A bit like a finish-line image, we get to see what happened at this X during the video.
            // Currently used in experimentations on frame interpolation for slow motion.

            var testDirectory =
                @"C:\Documents and Settings\Administrateur\Mes documents\Dev  Prog\Videa\Video Testing\YT images\test output\";

            // Clean up output folder.
            var outFiles = Directory.GetFiles(testDirectory, "*.bmp");
            foreach (var f in outFiles)
            {
                File.Delete(f);
            }

            // Get column of each image and output it in the resulting image.
            var imgHeight = _mFrameList[0].BmpImage.Height;
            var imgWidth = _mFrameList[0].BmpImage.Width;
            var iTotalImages = _mFrameList.Count;
            iTotalImages = Math.Min(100, _mFrameList.Count);
            for (var iCurrentX = 0; iCurrentX < imgWidth; iCurrentX++)
            {
                CreateYtSlice(iCurrentX, iTotalImages, imgHeight, imgWidth, testDirectory);
                MBackgroundWorker.ReportProgress(iCurrentX, imgWidth);
            }

            // Switch lists.
            /*for(int i=0;i<iTotalImages;i++)
            {
                if(i<imgWidth)
                {
                    m_FrameList[i].BmpImage.Dispose();
                    m_FrameList[i].BmpImage = m_TempImageList[i];
                }
                else
                {
                    // Black out image.
                }
            }*/
        }

        private Bitmap CreateYtSlice(int iCurrentX, int iTotalImages, int imgHeight, int imgWidth, string testDirectory)
        {
            // Create the lateral image.
            // Gather the same column in all images and paint it on a new image.

            /*
            //1. Mode same 3D space-time block.
            // We try to keep the same 3D space-time block.
            Bitmap ytImage = new Bitmap(imgWidth, imgHeight, PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(ytImage);

            int iScope = Math.Min(iTotalImages, imgWidth);
            for(int i=0;i<iScope;i++)
            {
                //Rectangle rSrc = new Rectangle(iXCoord, 0, 1, imgHeight);
                //Rectangle rDst = new Rectangle(i, 0, 1, imgHeight);
                Rectangle rSrc = new Rectangle(iCurrentX, 0, 1, imgHeight);
                Rectangle rDst = new Rectangle(i, 0, 1, imgHeight);
                g.DrawImage(m_FrameList[i].BmpImage, rDst, rSrc, GraphicsUnit.Pixel);
            }*/

            // 2. Mode full output.
            var ytImage = new Bitmap(iTotalImages, imgHeight, PixelFormat.Format24bppRgb);
            var g = Graphics.FromImage(ytImage);

            // loop on all t.
            var iScope = iTotalImages;
            for (var i = 0; i < iScope; i++)
            {
                //Rectangle rSrc = new Rectangle(iXCoord, 0, 1, imgHeight);
                //Rectangle rDst = new Rectangle(i, 0, 1, imgHeight);
                var rSrc = new Rectangle(iCurrentX, 0, 1, imgHeight);
                var rDst = new Rectangle(i, 0, 1, imgHeight);
                g.DrawImage(_mFrameList[i].BmpImage, rDst, rSrc, GraphicsUnit.Pixel);
            }

            ytImage.Save(testDirectory + string.Format("test-X{0:000}", iCurrentX) + ".bmp");

            return ytImage;
        }

        #endregion YTSlices

        #region Frame Interpolation

        private void TestFrameInterpolation()
        {
            var imgStart = 60;
            var range = 10;

            var interpolatedList = new List<Bitmap>();
            for (var i = imgStart; i < imgStart + range; i++)
            {
                var bmp = ElaVertInterpolation(_mFrameList[i].BmpImage, _mFrameList[i + 1].BmpImage, 5);
                interpolatedList.Add(bmp);
                MBackgroundWorker.ReportProgress(i - imgStart, range);
            }

            // Interleave.
            var interleavedList = new List<Bitmap>();
            for (var i = 0; i < _mFrameList.Count; i++)
            {
                interleavedList.Add(_mFrameList[i].BmpImage);
                if (i >= imgStart && i < imgStart + range)
                {
                    interleavedList.Add(interpolatedList[i - imgStart]);
                }
            }

            // Reconstruct original list.
            var totalImages = _mFrameList.Count;
            for (var i = 0; i < _mFrameList.Count; i++)
            {
                _mFrameList[i].BmpImage = interleavedList[i];
            }

            // Dispose the extra images.
            for (var i = _mFrameList.Count; i < interleavedList.Count; i++)
            {
                interleavedList[i].Dispose();
            }
        }

        private static unsafe Bitmap ElaVertInterpolation(Bitmap _src1, Bitmap _src2, int aperture)
        {
            //----------------------------------------------------
            // Performs ELA between two adjacent (in time) images.
            // Works only on columns.
            //----------------------------------------------------

            var src1 = _src1;
            var src2 = _src2;
            var dst = new Bitmap(src1.Width, src1.Height, src1.PixelFormat);

            // Lock images.
            var src1Data = src1.LockBits(new Rectangle(0, 0, src1.Width, src1.Height), ImageLockMode.ReadOnly,
                src1.PixelFormat);
            var src2Data = src2.LockBits(new Rectangle(0, 0, src1.Width, src1.Height), ImageLockMode.ReadOnly,
                src1.PixelFormat);
            var dstData = dst.LockBits(new Rectangle(0, 0, src1.Width, src1.Height), ImageLockMode.ReadWrite,
                src1.PixelFormat);

            // Get unmanaged images.
            var src1Unmanaged = new UnmanagedImage(src1Data);
            var src2Unmanaged = new UnmanagedImage(src2Data);
            var dstUnmanaged = new UnmanagedImage(dstData);

            // Dimensions.
            var width = src1Unmanaged.Width;
            var height = src1Unmanaged.Height;
            var stride = src1Unmanaged.Stride;
            //int dstStride = dstUnmanaged.Stride;

            var pSrc1 = (byte*)src1Unmanaged.ImageData.ToPointer();
            var pSrc2 = (byte*)src2Unmanaged.ImageData.ToPointer();
            var pDst = (byte*)dstUnmanaged.ImageData.ToPointer();

            //--
            // for each line
            for (var y = 0; y < height; y++)
            {
                // for each pixel
                for (var x = 0; x < width; x++)
                {
                    var pos = (y * (stride)) + (x * 3);

                    // Find minimum difference of lines in the aperture window.
                    var minDiff = 255 * 3;
                    var top = y - (aperture / 2);
                    var minRow = top + (aperture / 2);

                    for (var row = top; row < top + aperture; row++)
                    {
                        var row2 = (top + aperture - 1) - (row - top);

                        if (((row < 0) || (row >= height)) || ((row2 < 0) || (row2 >= height)) || (x + 1 >= width))
                        {
                        }
                        else
                        {
                            var pos1 = (row * stride) + x * 3;
                            var pos2 = (row2 * stride) + x * 3;

                            var b1 = pSrc1[pos1];
                            var b2 = pSrc2[pos2];
                            var g1 = pSrc1[pos1 + 1];
                            var g2 = pSrc2[pos2 + 1];
                            var r1 = pSrc1[pos1 + 2];
                            var r2 = pSrc2[pos2 + 2];

                            var diff = Math.Abs(b1 - b2);
                            diff += Math.Abs(g1 - g2);
                            diff += Math.Abs(r1 - r2);

                            if (minDiff > diff)
                            {
                                minDiff = diff;
                                minRow = row;
                            }
                        }
                    }

                    // minPos has the best pos.
                    var minPos1 = (minRow * stride) + x * 3;
                    var minPos2 = (((top + aperture - 1) - (minRow - top)) * stride) + x * 3;

                    pDst[pos] = (byte)((pSrc1[minPos1] + pSrc2[minPos2]) / 2);
                    pDst[pos + 1] = (byte)((pSrc1[minPos1 + 1] + pSrc2[minPos2 + 1]) / 2);
                    pDst[pos + 2] = (byte)((pSrc1[minPos1 + 2] + pSrc2[minPos2 + 2]) / 2);
                }
            }
            //--

            src1.UnlockBits(src1Data);
            src2.UnlockBits(src2Data);
            dst.UnlockBits(dstData);

            return dst;
        }

        #endregion Frame Interpolation
    }
}