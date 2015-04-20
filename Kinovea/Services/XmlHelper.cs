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

using log4net;
using System;
using System.Drawing;
using System.Reflection;

namespace Kinovea.Services
{
    public static class XmlHelper
    {
        // Note: the built-in TypeConverters are crashing on some machines for unknown reason. (TypeDescriptor.GetConverter(typeof(Point)))
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static Point ParsePoint(string sPoint)
        {
            var point = Point.Empty;
            try
            {
                var a = sPoint.Split(';');
                point = new Point(int.Parse(a[0]), int.Parse(a[1]));
            }
            catch (Exception)
            {
                Log.Error(string.Format("An error happened while parsing Point value. ({0}).", sPoint));
            }

            return point;
        }

        public static Color ParseColor(string sColor)
        {
            var output = Color.Black;

            try
            {
                var a = sColor.Split(';');
                if (a.Length == 3)
                {
                    output = Color.FromArgb(255, byte.Parse(a[0]), byte.Parse(a[1]), byte.Parse(a[2]));
                }
                else if (a.Length == 4)
                {
                    output = Color.FromArgb(byte.Parse(a[0]), byte.Parse(a[1]), byte.Parse(a[2]), byte.Parse(a[3]));
                }
            }
            catch (Exception)
            {
                Log.Error(string.Format("An error happened while parsing color value. ({0}).", sColor));
            }

            return output;
        }

        public static bool ParseBoolean(string str)
        {
            // This function helps fix the discrepancy between:
            // - Boolean.ToString() which returns "False" or "True",
            // - ReadElementContentAsBoolean() which only accepts "false", "true", "1" or "0" as per XML spec and throws an exception otherwise.
            return (str != "false" && str != "False" && str != "0");
        }
    }
}