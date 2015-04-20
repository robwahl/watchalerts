#region License

/*
Copyright © Joan Charmant 2010.
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

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using Image = AForge.Imaging.Image;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     TrackerBlock2 uses Template Matching through Normalized cross correlation to perform tracking.
    ///     It uses TrackPointBlock to describe a tracked point.
    ///     Working:
    ///     To find the point in image I:
    ///     - use the template found in image I-1.
    ///     - save the template in point at image I.
    ///     - no need to save the relative search window as points are saved in absolute coords.
    /// </summary>
    public class TrackerBlock2 : AbstractTracker
    {
        #region Constructor

        public TrackerBlock2(int imgWidth, int imgHeight)
        {
            _mFSimilarityTreshold = 0.50f;

            // If simi is better than this, we keep the same template, to avoid the template update drift.

            // When using CCORR : 0.90 or 0.95.
            // When using CCOEFF : 0.80
            _mFTemplateUpdateSimilarityThreshold = 0.80f;

            //int blockFactor = 15;	// Bigger template.
            var blockFactor = 20; // Smaller template can improve tracking by focusing on the object instead of Bg.
            var blockWidth = imgWidth / blockFactor;
            var blockHeight = imgHeight / blockFactor;

            if (blockWidth < 20)
            {
                blockWidth = 20;
            }

            if (blockHeight < 20)
            {
                blockHeight = 20;
            }

            _mBlockSize = new Size(blockWidth, blockHeight);

            var searchFactor = 4.0f;
            _mSearchWindowSize = new Size((int)(blockWidth * searchFactor), (int)(blockHeight * searchFactor));

            Log.Debug(
                string.Format(
                    "Template matching: Image:{0}x{1}, Template:{2}, Search Window:{3}, Similarity thr.:{4}, Tpl update thr.:{5}",
                    imgWidth, imgHeight, _mBlockSize, _mSearchWindowSize, _mFSimilarityTreshold,
                    _mFTemplateUpdateSimilarityThreshold));
        }

        #endregion Constructor

        #region Members

        // Options - initialize in the constructor.
        private readonly float _mFSimilarityTreshold; // Discard candidate block with lower similarity.

        private Size _mBlockSize = new Size(20, 20); // Size of block to be matched.
        private Size _mSearchWindowSize = new Size(100, 100); // Size of window of candidates.

        private readonly float _mFTemplateUpdateSimilarityThreshold = 1.0f;
        // Only update the template if that dissimilar.

        // Monitoring, debugging.
        private static readonly bool MBMonitoring = false;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region AbstractTracker Implementation

        public override bool Track(List<AbstractTrackPoint> previousPoints, Bitmap currentImage, long t,
            out AbstractTrackPoint currentPoint)
        {
            //---------------------------------------------------------------------
            // The input informations we have at hand are:
            // - The current bitmap we have to find the point into.
            // - The coordinates of all the previous points tracked.
            // - Previous tracking infos, stored in the TrackPoints tracked so far.
            //---------------------------------------------------------------------

            var lastTrackPoint = (TrackPointBlock)previousPoints[previousPoints.Count - 1];
            var lastPoint = lastTrackPoint.Point;

            var bMatched = false;
            currentPoint = null;

            if (lastTrackPoint.Template != null && currentImage != null)
            {
                // Center search zone around last point.
                var searchCenter = lastPoint;
                var searchZone = new Rectangle(searchCenter.X - (_mSearchWindowSize.Width / 2),
                    searchCenter.Y - (_mSearchWindowSize.Height / 2),
                    _mSearchWindowSize.Width,
                    _mSearchWindowSize.Height);

                searchZone.Intersect(new Rectangle(0, 0, currentImage.Width, currentImage.Height));

                double fBestScore = 0;
                var bestCandidate = new Point(-1, -1);

                //Image<Bgr, Byte> cvTemplate = new Image<Bgr, Byte>(lastTrackPoint.Template);
                //Image<Bgr, Byte> cvImage = new Image<Bgr, Byte>(_CurrentImage);

                var img = currentImage;
                var tpl = lastTrackPoint.Template;

                var imageData = img.LockBits(new Rectangle(0, 0, img.Width, img.Height), ImageLockMode.ReadOnly,
                    img.PixelFormat);
                var templateData = tpl.LockBits(new Rectangle(0, 0, tpl.Width, tpl.Height), ImageLockMode.ReadOnly,
                    tpl.PixelFormat);

                var cvImage = new Image<Bgra, byte>(imageData.Width, imageData.Height, imageData.Stride, imageData.Scan0);
                var cvTemplate = new Image<Bgra, byte>(templateData.Width, templateData.Height, templateData.Stride,
                    templateData.Scan0);

                cvImage.ROI = searchZone;

                var resWidth = searchZone.Width - lastTrackPoint.Template.Width + 1;
                var resHeight = searchZone.Height - lastTrackPoint.Template.Height + 1;

                var similarityMap = new Image<Gray, float>(resWidth, resHeight);

                //CvInvoke.cvMatchTemplate(cvImage.Ptr, cvTemplate.Ptr, similarityMap.Ptr, TM_TYPE.CV_TM_SQDIFF_NORMED);
                //CvInvoke.cvMatchTemplate(cvImage.Ptr, cvTemplate.Ptr, similarityMap.Ptr, TM_TYPE.CV_TM_CCORR_NORMED);
                CvInvoke.cvMatchTemplate(cvImage.Ptr, cvTemplate.Ptr, similarityMap.Ptr, TM_TYPE.CV_TM_CCOEFF_NORMED);

                img.UnlockBits(imageData);
                tpl.UnlockBits(templateData);

                // Find max
                var p1 = new Point(0, 0);
                var p2 = new Point(0, 0);
                double fMin = 0;
                double fMax = 0;

                CvInvoke.cvMinMaxLoc(similarityMap.Ptr, ref fMin, ref fMax, ref p1, ref p2, IntPtr.Zero);

                if (fMax > _mFSimilarityTreshold)
                {
                    bestCandidate = new Point(searchZone.Left + p2.X + tpl.Width / 2, searchZone.Top + p2.Y + tpl.Height / 2);
                    fBestScore = fMax;
                }

                #region Monitoring

                if (MBMonitoring)
                {
                    // Save the similarity map to file.
                    var mapNormalized = new Image<Gray, byte>(similarityMap.Width, similarityMap.Height);
                    CvInvoke.cvNormalize(similarityMap.Ptr, mapNormalized.Ptr, 0, 255, NORM_TYPE.CV_MINMAX, IntPtr.Zero);

                    var bmpMap = mapNormalized.ToBitmap();

                    var tplDirectory =
                        @"C:\Documents and Settings\Administrateur\Mes documents\Dev  Prog\Videa\Video Testing\Tracking\Template Update";
                    bmpMap.Save(tplDirectory +
                                string.Format(@"\simiMap-{0:000}-{1:0.00}.bmp", previousPoints.Count, fBestScore));
                }

                #endregion Monitoring

                // Result of the matching.
                if (bestCandidate.X != -1 && bestCandidate.Y != -1)
                {
                    // Save template in the point.
                    currentPoint = CreateTrackPoint(false, bestCandidate.X, bestCandidate.Y, fBestScore, t, img,
                        previousPoints);
                    ((TrackPointBlock)currentPoint).Similarity = fBestScore;

                    bMatched = true;
                }
                else
                {
                    // No match. Create the point at the center of the search window (whatever that might be).
                    currentPoint = CreateTrackPoint(false, searchCenter.X, searchCenter.Y, 0.0f, t, img, previousPoints);
                    Log.Debug("Track failed. No block over the similarity treshold in the search window.");
                }
            }
            else
            {
                // No image. (error case ?)
                // Create the point at the last point location.
                currentPoint = CreateTrackPoint(false, lastTrackPoint.X, lastTrackPoint.Y, 0.0f, t, currentImage,
                    previousPoints);
                Log.Debug("Track failed. No input image, or last point doesn't have any cached block image.");
            }

            return bMatched;
        }

        public override AbstractTrackPoint CreateTrackPoint(bool bManual, int x, int y, double fSimilarity, long t,
            Bitmap currentImage, List<AbstractTrackPoint> previousPoints)
        {
            // Creates a TrackPoint from the input image at the given coordinates.
            // Stores algorithm internal data in the point, to help next match.
            // _t is in relative timestamps from the first point.

            // Copy the template from the image into its own Bitmap.

            var tpl = new Bitmap(_mBlockSize.Width, _mBlockSize.Height, PixelFormat.Format32bppPArgb);

            var bUpdateWithCurrentImage = true;

            if (!bManual && previousPoints.Count > 0 && fSimilarity > _mFTemplateUpdateSimilarityThreshold)
            {
                // Do not update the template if it's not that different.
                var prevBlock = previousPoints[previousPoints.Count - 1] as TrackPointBlock;
                if (prevBlock != null && prevBlock.Template != null)
                {
                    tpl = Image.Clone(prevBlock.Template);
                    bUpdateWithCurrentImage = false;
                }
            }

            if (bUpdateWithCurrentImage)
            {
                var imageData = currentImage.LockBits(new Rectangle(0, 0, currentImage.Width, currentImage.Height),
                    ImageLockMode.ReadOnly, currentImage.PixelFormat);
                var templateData = tpl.LockBits(new Rectangle(0, 0, tpl.Width, tpl.Height), ImageLockMode.ReadWrite,
                    tpl.PixelFormat);

                var pixelSize = 4;

                var tplStride = templateData.Stride;
                var templateWidthInBytes = _mBlockSize.Width * pixelSize;
                var tplOffset = tplStride - templateWidthInBytes;

                var imgStride = imageData.Stride;
                var imageWidthInBytes = currentImage.Width * pixelSize;
                var imgOffset = imgStride - (currentImage.Width * pixelSize) + imageWidthInBytes - templateWidthInBytes;

                var startY = y - (_mBlockSize.Height / 2);
                var startX = x - (_mBlockSize.Width / 2);

                if (startX < 0)
                    startX = 0;

                if (startY < 0)
                    startY = 0;

                unsafe
                {
                    var pTpl = (byte*)templateData.Scan0.ToPointer();
                    var pImg = (byte*)imageData.Scan0.ToPointer() + (imgStride * startY) + (pixelSize * startX);

                    for (var row = 0; row < _mBlockSize.Height; row++)
                    {
                        if (startY + row > imageData.Height - 1)
                        {
                            break;
                        }

                        for (var col = 0; col < templateWidthInBytes; col++, pTpl++, pImg++)
                        {
                            if (startX * pixelSize + col < imageWidthInBytes)
                            {
                                *pTpl = *pImg;
                            }
                        }

                        pTpl += tplOffset;
                        pImg += imgOffset;
                    }
                }

                currentImage.UnlockBits(imageData);
                tpl.UnlockBits(templateData);
            }

            #region Monitoring

            if (MBMonitoring && bUpdateWithCurrentImage)
            {
                // Save current template to file, to visually monitor the drift.
                var tplDirectory =
                    @"C:\Documents and Settings\Administrateur\Mes documents\Dev  Prog\Videa\Video Testing\Tracking\Template Update";
                if (previousPoints.Count <= 1)
                {
                    // Clean up folder.
                    var tplFiles = Directory.GetFiles(tplDirectory, "*.bmp");
                    foreach (var f in tplFiles)
                    {
                        File.Delete(f);
                    }
                }
                var iFileName = string.Format("{0}\\tpl-{1:000}.bmp", tplDirectory, previousPoints.Count);
                tpl.Save(iFileName);
            }

            #endregion Monitoring

            var tpb = new TrackPointBlock(x, y, t, tpl);
            tpb.IsReferenceBlock = bManual;
            tpb.Similarity = bManual ? 1.0f : fSimilarity;

            return tpb;
        }

        public override AbstractTrackPoint CreateOrphanTrackPoint(int x, int y, long t)
        {
            // This creates a bare bone TrackPoint.
            // This is used only in the case of importing from xml.
            // The TrackPoint can't be used as-is to track the next one because it's missing the algo internal data (block).
            // We'll need to reconstruct it when we have the corresponding image.
            return new TrackPointBlock(x, y, t);
        }

        public override void Draw(Graphics canvas, Point point, CoordinateSystem transformer, Color color,
            double fOpacityFactor)
        {
            // Draw the search and template boxes around the point.
            var p = transformer.Transform(point);
            using (var pen = new Pen(Color.FromArgb((int)(fOpacityFactor * 192), color)))
            {
                canvas.DrawRectangle(pen, p.Box(transformer.Transform(_mSearchWindowSize)));
                canvas.DrawRectangle(pen, p.Box(transformer.Transform(_mBlockSize)));
            }
        }

        public override Rectangle GetEditRectangle(Point position)
        {
            return position.Box(_mSearchWindowSize);
        }

        #endregion AbstractTracker Implementation
    }
}