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

using log4net;
using System.Drawing;
using System.Reflection;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     CapturedVideo represent a recently captured file.
    ///     It keeps the thumbnail, and path...
    ///     It is used to display the recently captured videos as launchable thumbs.
    /// </summary>
    public class CapturedVideo
    {
        #region Members

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        public CapturedVideo(string filepath, Bitmap image)
        {
            Filepath = filepath;
            if (image != null)
            {
                Thumbnail = new Bitmap(image, 100, 75);
            }
            else
            {
                Thumbnail = new Bitmap(100, 75);
                Log.Error("Cannot create captured video thumbnail.");
            }
        }

        #region Properties

        public Bitmap Thumbnail { get; }

        public string Filepath { get; }

        #endregion Properties
    }
}