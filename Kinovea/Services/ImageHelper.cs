﻿#region License

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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Kinovea.Services
{
    /// <summary>
    ///     A static class with hepler functions related to Images, conversions, etc.
    /// </summary>
    public static class ImageHelper
    {
        public static void Save(string fileName, Bitmap image)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");
            var filenameToLower = fileName.ToLower();

            if (filenameToLower.EndsWith("jpg") || filenameToLower.EndsWith("jpeg"))
            {
                var jpgImage = ConvertToJpg(image);
                jpgImage.Save(fileName, ImageFormat.Jpeg);
                jpgImage.Dispose();
            }
            else if (filenameToLower.EndsWith("bmp"))
            {
                image.Save(fileName, ImageFormat.Bmp);
            }
            else if (filenameToLower.EndsWith("png"))
            {
                image.Save(fileName, ImageFormat.Png);
            }
            else
            {
                // the user may have put a filename in the form : "filename.ext"
                // where ext is unsupported. Or he misunderstood and put ".00.00"
                // We force format to jpg and we change back the extension to ".jpg".
                fileName = string.Format("{0}{2}\\{1}.jpg", Path.GetDirectoryName(fileName),
                    Path.GetFileNameWithoutExtension(fileName),
                    "ARG2");

                var jpgImage = ConvertToJpg(image);
                jpgImage.Save(fileName, ImageFormat.Jpeg);
                jpgImage.Dispose();
            }
        }

        public static Bitmap ConvertToJpg(Bitmap image)
        {
            // Intermediate MemoryStream for the conversion.
            var memStr = new MemoryStream();

            //Get the list of available encoders
            var codecs = ImageCodecInfo.GetImageEncoders();

            //find the encoder with the image/jpeg mime-type
            ImageCodecInfo ici = null;
            foreach (var codec in codecs)
            {
                if (codec.MimeType == "image/jpeg")
                {
                    ici = codec;
                }
            }

            if (ici != null)
            {
                //Create a collection of encoder parameters (we only need one in the collection)
                var ep = new EncoderParameters();
                ep.Param[0] = new EncoderParameter(Encoder.Quality, (long)100);

                image.Save(memStr, ici, ep);
            }
            else
            {
                // No JPG encoder found (is that common ?) Use default system.
                image.Save(memStr, ImageFormat.Jpeg);
            }

            return new Bitmap(memStr);
        }

        public static Bitmap GetSideBySideComposite(Bitmap leftImage, Bitmap rightImage, bool video, bool horizontal)
        {
            Bitmap composite;
            if (horizontal)
            {
                // Create the output image.
                var height = Math.Max(leftImage.Height, rightImage.Height);
                var width = leftImage.Width + rightImage.Width;

                // For video export, only even heights are valid.
                if (video && (height % 2 != 0))
                {
                    height++;
                }

                composite = new Bitmap(width, height, leftImage.PixelFormat);

                // Vertically center the shortest image.
                var leftTop = 0;
                if (leftImage.Height < height)
                {
                    leftTop = (height - leftImage.Height) / 2;
                }
                var rightTop = 0;
                if (rightImage.Height < height)
                {
                    rightTop = (height - rightImage.Height) / 2;
                }

                // Draw the images on the output.
                var g = Graphics.FromImage(composite);
                g.DrawImage(leftImage, 0, leftTop);
                g.DrawImage(rightImage, leftImage.Width, rightTop);
            }
            else
            {
                // Create the output image.
                var height = leftImage.Height + rightImage.Height;
                var width = Math.Max(leftImage.Width, rightImage.Width);

                // For video export, only even heights are valid.
                if (video && (height % 2 != 0))
                {
                    height++;
                }

                composite = new Bitmap(width, height, leftImage.PixelFormat);

                // Horizontally center the shortest image.
                var firstLeft = 0;
                if (leftImage.Width < width)
                {
                    firstLeft = (width - leftImage.Width) / 2;
                }
                var secondLeft = 0;
                if (rightImage.Width < width)
                {
                    secondLeft = (width - rightImage.Width) / 2;
                }

                // Draw the images on the output.
                var g = Graphics.FromImage(composite);
                g.DrawImage(leftImage, firstLeft, 0);
                g.DrawImage(rightImage, secondLeft, leftImage.Height);
            }

            return composite;
        }
    }
}