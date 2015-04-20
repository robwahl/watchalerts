#region License

/*
Copyright © Joan Charmant 2011.
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

using Kinovea.Services;
using log4net;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     FlienameHelper computes the next file name.
    /// </summary>
    public class FilenameHelper
    {
        // The goal of this class is to compute the next file name for snapshot and recording feature on capture screen.
        // For "free text with increment" type of naming (default) :
        // We try to make it look like "it just works" for the user.
        // The compromise :
        // - We try to increment taking both screens into account.
        // - User should always be able to modify the text manually if he wants to.
        // hence: We do not try to update both screens simultaneously with the same number.
        // Each screen tracks his own file name.
        //
        // When using pattern, both screen will use the same pattern and they will be updated after each save.

        #region Members

        private readonly PreferencesManager _mPrefManager = PreferencesManager.Instance();
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Public Methods

        public string InitImage()
        {
            var next = "";
            if (_mPrefManager.CaptureUsePattern)
            {
                next = ConvertPattern(_mPrefManager.CapturePattern, _mPrefManager.CaptureImageCounter);
            }
            else if (_mPrefManager.CaptureImageFile == "")
            {
                next = PreferencesManager.DefaultCaptureImageFile;
                Log.DebugFormat("We never saved a file before, return the default file name : {0}", next);
            }
            else
            {
                next = Next(_mPrefManager.CaptureImageFile);
            }

            return next;
        }

        public string InitVideo()
        {
            var next = "";
            if (_mPrefManager.CaptureUsePattern)
            {
                next = ConvertPattern(_mPrefManager.CapturePattern, _mPrefManager.CaptureVideoCounter);
            }
            else if (_mPrefManager.CaptureVideoFile == "")
            {
                next = PreferencesManager.DefaultCaptureVideoFile;
                Log.DebugFormat("We never saved a file before, return the default file name : {0}", next);
            }
            else
            {
                next = Next(_mPrefManager.CaptureVideoFile);
            }

            return next;
        }

        public string Next(string current)
        {
            //---------------------------------------------------------------------
            // Increments an existing file name.
            // DO NOT use this function when using naming pattern, always use InitImage/InitVideo.
            // This function is oblivious to image/video.
            // if the existing name has a number in it, we increment this number.
            // if not, we create a suffix.
            // We do not care about extension here, it will be appended afterwards.
            //---------------------------------------------------------------------

            var next = "";
            if (_mPrefManager.CaptureUsePattern)
            {
                throw new NotImplementedException("Not implemented when using pattern. Use InitImage or InitVideo");
            }
            if (!string.IsNullOrEmpty(current))
            {
                // Find all numbers in the name, if any.
                var r = new Regex(@"\d+");
                var mc = r.Matches(current);

                if (mc.Count > 0)
                {
                    // Number(s) found. Increment the last one.
                    // TODO: handle leading zeroes in the original (001 -> 002).
                    var m = mc[mc.Count - 1];
                    var number = int.Parse(m.Value);
                    number++;

                    // Replace the number in the original.
                    next = r.Replace(current, number.ToString(), 1, m.Index);
                }
                else
                {
                    // No number found, add suffix.
                    next = string.Format("{0} - 2", Path.GetFileNameWithoutExtension(current));
                }

                Log.DebugFormat("Current file name : {0}, next file name : {1}", current, next);
            }

            return next;
        }

        public bool ValidateFilename(string filename, bool allowEmpty)
        {
            // Validate filename chars.
            var bIsValid = false;

            if (filename.Length == 0 && allowEmpty)
            {
                // special case for when the user is currently typing.
                bIsValid = true;
            }
            else
            {
                try
                {
                    new FileInfo(filename);
                    bIsValid = true;
                }
                catch (ArgumentException)
                {
                    // filename is empty, only white spaces or contains invalid chars.
                    Log.ErrorFormat("Capture filename has invalid characters. Proposed file was: {0}", filename);
                }
                catch (NotSupportedException)
                {
                    // filename contains a colon in the middle of the string.
                    Log.ErrorFormat("Capture filename has a colon in the middle. Proposed file was: {0}", filename);
                }
            }

            return bIsValid;
        }

        public string ConvertPattern(string input, long iAutoIncrement)
        {
            // Convert pattern into file name.
            // Codes : %y, %mo, %d, %h, %mi, %s, %i.
            var output = "";

            if (!string.IsNullOrEmpty(input))
            {
                var sb = new StringBuilder(input);

                // Date and time.
                var dt = DateTime.Now;
                sb.Replace("%y", string.Format("{0:0000}", dt.Year));
                sb.Replace("%mo", string.Format("{0:00}", dt.Month));
                sb.Replace("%d", string.Format("{0:00}", dt.Day));
                sb.Replace("%h", string.Format("{0:00}", dt.Hour));
                sb.Replace("%mi", string.Format("{0:00}", dt.Minute));
                sb.Replace("%s", string.Format("{0:00}", dt.Second));

                // auto-increment
                sb.Replace("%i", string.Format("{0}", iAutoIncrement));

                output = sb.ToString();
            }

            return output;
        }

        public void AutoIncrement(bool image)
        {
            // Autoincrement (only if needed and only the corresponding type).
            if (_mPrefManager.CapturePattern.Contains("%i"))
            {
                if (image)
                {
                    _mPrefManager.CaptureImageCounter++;
                }
                else
                {
                    _mPrefManager.CaptureVideoCounter++;
                }
            }
        }

        #endregion Public Methods
    }
}