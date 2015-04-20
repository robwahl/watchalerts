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

using Kinovea.Services;
using log4net;
using System;
using System.Drawing;
using System.Reflection;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     CalibrationHelper encapsulates informations used for pixels to real world calculations.
    ///     The user can specify the real distance of a Line drawing and a coordinate system.
    ///     We also keep the length units and the preferred unit for speeds.
    /// </summary>
    public class CalibrationHelper
    {
        #region Members

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor

        public CalibrationHelper()
        {
            var prefManager = PreferencesManager.Instance();
            CurrentSpeedUnit = prefManager.SpeedUnit;
        }

        #endregion Constructor

        #region Properties

        public LengthUnits CurrentLengthUnit { get; set; } = LengthUnits.Pixels;

        public double PixelToUnit { get; set; } = 1.0;

        public SpeedUnits CurrentSpeedUnit { get; set; } = SpeedUnits.PixelsPerFrame;

        public bool IsOriginSet
        {
            get { return (CoordinatesOrigin.X >= 0 && CoordinatesOrigin.Y >= 0); }
        }

        public Point CoordinatesOrigin { get; set; } = private new Point(-1, -1);

        public double FramesPerSeconds { // This is the frames per second as in real action reference.
            // It takes high-speed camera into account, may be different than the video framerate.
            get; set; } = 25;

        #endregion Properties

        #region Public Methods

        public static string GetLengthAbbreviationFromUnit(LengthUnits unit)
        {
            var abbreviation = "";
            switch (unit)
            {
                case LengthUnits.Centimeters:
                    abbreviation = "cm";
                    break;

                case LengthUnits.Meters:
                    abbreviation = "m";
                    break;

                case LengthUnits.Inches:
                    abbreviation = "in";
                    break;

                case LengthUnits.Feet:
                    abbreviation = "ft";
                    break;

                case LengthUnits.Yards:
                    abbreviation = "yd";
                    break;

                case LengthUnits.Pixels:
                default:
                    abbreviation = "px";
                    break;
            }

            return abbreviation;
        }

        public string GetLengthAbbreviation()
        {
            return GetLengthAbbreviationFromUnit(CurrentLengthUnit);
        }

        public string GetLengthText(double pixelLength)
        {
            // Return the length in the user unit, with the abbreviation.
            var lengthText = "";

            // Use 2 digits precision except for pixels.
            if (CurrentLengthUnit == LengthUnits.Pixels)
            {
                lengthText = string.Format("{0:0} {1}", pixelLength, GetLengthAbbreviationFromUnit(CurrentLengthUnit));
            }
            else
            {
                lengthText = string.Format("{0:0.00} {1}", GetLengthInUserUnit(pixelLength),
                    GetLengthAbbreviationFromUnit(CurrentLengthUnit));
            }

            return lengthText;
        }

        public string GetLengthText(Point p1, Point p2)
        {
            // Return the length in the user unit, with the abbreviation.
            var lengthText = "";

            if (p1.X == p2.X && p1.Y == p2.Y)
            {
                lengthText = "0" + " " + GetLengthAbbreviationFromUnit(CurrentLengthUnit);
            }
            else
            {
                // Use 2 digits precision except for pixels.
                if (CurrentLengthUnit == LengthUnits.Pixels)
                {
                    lengthText = string.Format("{0:0} {1}", GetLengthInUserUnit(p1, p2),
                        GetLengthAbbreviationFromUnit(CurrentLengthUnit));
                }
                else
                {
                    lengthText = string.Format("{0:0.00} {1}", GetLengthInUserUnit(p1, p2),
                        GetLengthAbbreviationFromUnit(CurrentLengthUnit));
                }
            }

            return lengthText;
        }

        public string GetLengthText(double fPixelLength, bool bAbbreviation, bool bPrecise)
        {
            // Return length as a string.
            var lengthText = "";
            if (bAbbreviation)
            {
                if (CurrentLengthUnit == LengthUnits.Pixels || !bPrecise)
                {
                    lengthText = string.Format("{0:0} {1}", GetLengthInUserUnit(fPixelLength),
                        GetLengthAbbreviationFromUnit(CurrentLengthUnit));
                }
                else
                {
                    lengthText = string.Format("{0:0.00} {1}", GetLengthInUserUnit(fPixelLength),
                        GetLengthAbbreviationFromUnit(CurrentLengthUnit));
                }
            }
            else
            {
                if (CurrentLengthUnit == LengthUnits.Pixels || !bPrecise)
                {
                    lengthText = string.Format("{0:0}", GetLengthInUserUnit(fPixelLength));
                }
                else
                {
                    lengthText = string.Format("{0:0.00}", GetLengthInUserUnit(fPixelLength));
                }
            }

            return lengthText;
        }

        public double GetLengthInUserUnit(Point p1, Point p2)
        {
            // Return the length in the user unit.
            double fUnitLength = 0;

            if (p1.X != p2.X || p1.Y != p2.Y)
            {
                var fPixelLength = PixelDistance(p1, p2);

                fUnitLength = GetLengthInUserUnit(fPixelLength);
            }

            return fUnitLength;
        }

        public double GetLengthInUserUnit(double fPixelLength)
        {
            // Return the length in the user unit.
            return fPixelLength*PixelToUnit;
        }

        public PointF GetPointInUserUnit(Point p)
        {
            var fX = GetLengthInUserUnit(p.X - CoordinatesOrigin.X);
            var fY = GetLengthInUserUnit(CoordinatesOrigin.Y - p.Y);
            return new PointF((float) fX, (float) fY);
        }

        public string GetPointText(Point p, bool bAbbreviation)
        {
            var fX = GetLengthInUserUnit(p.X - CoordinatesOrigin.X);
            var fY = GetLengthInUserUnit(CoordinatesOrigin.Y - p.Y);

            string pointText;
            if (CurrentLengthUnit == LengthUnits.Pixels)
            {
                pointText = string.Format("{{{0:0};{1:0}}}", fX, fY);
            }
            else
            {
                pointText = string.Format("{{{0:0.00};{1:0.00}}}", fX, fY);
            }

            if (bAbbreviation)
            {
                pointText = pointText + " " + GetLengthAbbreviation();
            }

            return pointText;
        }

        public static string GetSpeedAbbreviationFromUnit(SpeedUnits unit)
        {
            var abbreviation = "";
            switch (unit)
            {
                case SpeedUnits.FeetPerSecond:
                    abbreviation = "ft/s";
                    break;

                case SpeedUnits.MetersPerSecond:
                    abbreviation = "m/s";
                    break;

                case SpeedUnits.KilometersPerHour:
                    abbreviation = "km/h";
                    break;

                case SpeedUnits.MilesPerHour:
                    abbreviation = "mph";
                    break;

                case SpeedUnits.Knots:
                    abbreviation = "kn";
                    break;

                case SpeedUnits.PixelsPerFrame:
                default:
                    abbreviation = "px/f";
                    break;
            }

            return abbreviation;
        }

        public string GetSpeedText(Point p1, Point p2, int frames)
        {
            // Return the speed in user units, with the abbreviation.

            var speedText = "";

            if ((p1.X == p2.X && p1.Y == p2.Y) || frames == 0)
            {
                speedText = "0" + " " + GetSpeedAbbreviationFromUnit(CurrentSpeedUnit);
            }
            else
            {
                var unitToUse = CurrentSpeedUnit;

                if (CurrentLengthUnit == LengthUnits.Pixels && CurrentSpeedUnit != SpeedUnits.PixelsPerFrame)
                {
                    // The user may have configured a preferred speed unit that we can't use because no
                    // calibration has been done on the video. In this case we use the px/f speed unit,
                    // but we don't change the user's preference.

                    unitToUse = SpeedUnits.PixelsPerFrame;
                }

                // Use 2 digits precision except for pixels.
                if (unitToUse == SpeedUnits.PixelsPerFrame)
                {
                    speedText = string.Format("{0:0} {1}", GetSpeedInUserUnit(p1, p2, frames, unitToUse),
                        GetSpeedAbbreviationFromUnit(unitToUse));
                }
                else
                {
                    speedText = string.Format("{0:0.00} {1}", GetSpeedInUserUnit(p1, p2, frames, unitToUse),
                        GetSpeedAbbreviationFromUnit(unitToUse));
                }
            }

            return speedText;
        }

        public static double PixelDistance(Point p1, Point p2)
        {
            // General utility method to return distance between pixels.
            return Math.Sqrt(((p1.X - p2.X)*(p1.X - p2.X)) + ((p1.Y - p2.Y)*(p1.Y - p2.Y)));
        }

        public static double PixelDistance(PointF p1, PointF p2)
        {
            // General utility method to return distance between pixels, subpixel accuracy.
            return Math.Sqrt(((p1.X - p2.X)*(p1.X - p2.X)) + ((p1.Y - p2.Y)*(p1.Y - p2.Y)));
        }

        #endregion Public Methods

        #region Private methods

        private double GetSpeedInUserUnit(Point p1, Point p2, int frames, SpeedUnits speedUnit)
        {
            // Return the speed in the current user unit.
            double fUnitSpeed = 0;

            if (p1.X != p2.X || p1.Y != p2.Y)
            {
                // Compute the length in pixels and send to converter.
                var fPixelLength = Math.Sqrt(((p1.X - p2.X)*(p1.X - p2.X)) + ((p1.Y - p2.Y)*(p1.Y - p2.Y)));
                fUnitSpeed = GetSpeedInUserUnit(fPixelLength, frames, speedUnit);
            }

            return fUnitSpeed;
        }

        private double GetSpeedInUserUnit(double fPixelLength, int frames, SpeedUnits speedUnit)
        {
            // Return the speed in the current user unit.

            // 1. Convert length from pixels to known distance.
            // (depends on user calibration from a line drawing)
            var fUnitLength = GetLengthInUserUnit(fPixelLength);

            // 2. Convert between length user units (length to speed)
            // (depends only on standards conversion ratio)
            var fUnitLength2 = ConvertLengthForSpeedUnit(fUnitLength, CurrentLengthUnit, speedUnit);

            // 3. Convert to speed unit.
            // (depends on video frame rate)
            var fUnitSpeed = ConvertToSpeedUnit(fUnitLength2, frames, speedUnit);

            Log.Debug(
                string.Format(
                    "Pixel conversion for speed. Input:{0:0.00} px for {1} frames. length1: {2:0.00} {3}, length2:{4:0.00} {5}, speed:{6:0.00} {7}",
                    fPixelLength, frames,
                    fUnitLength, GetLengthAbbreviation(),
                    fUnitLength2, GetSpeedAbbreviationFromUnit(speedUnit),
                    fUnitSpeed, GetSpeedAbbreviationFromUnit(speedUnit)));

            return fUnitSpeed;
        }

        #endregion Private methods

        #region Converters

        private double ConvertLengthForSpeedUnit(double fLength, LengthUnits lengthUnit, SpeedUnits speedUnits)
        {
            // Convert from one length unit to another.
            // For example: user calibrated the screen using centimeters and wants a speed in km/h.
            // We get a distance in centimeters, we convert it to kilometers.

            // http://en.wikipedia.org/wiki/Conversion_of_units
            // 1 inch 			= 0.0254 m.
            // 1 foot			= 0.3048 m.
            // 1 yard 			= 0.9144 m.
            // 1 mile 			= 1 609.344 m.
            // 1 nautical mile 	= 1 852 m.

            double fLength2 = 0;

            switch (lengthUnit)
            {
                case LengthUnits.Centimeters:
                    switch (speedUnits)
                    {
                        case SpeedUnits.FeetPerSecond:
                            //  Centimeters to feet.
                            fLength2 = fLength/30.48;
                            break;

                        case SpeedUnits.MetersPerSecond:
                            //  Centimeters to meters.
                            fLength2 = fLength/100;
                            break;

                        case SpeedUnits.KilometersPerHour:
                            // Centimeters to kilometers.
                            fLength2 = fLength/100000;
                            break;

                        case SpeedUnits.MilesPerHour:
                            // Centimeters to miles
                            fLength2 = fLength/160934.4;
                            break;

                        case SpeedUnits.Knots:
                            // Centimeters to nautical miles
                            fLength2 = fLength/185200;
                            break;

                        case SpeedUnits.PixelsPerFrame:
                        default:
                            // Centimeters to Pixels. (?)
                            // User has calibrated the image but now wants the speed in px/f.
                            fLength2 = fLength/PixelToUnit;
                            break;
                    }
                    break;

                case LengthUnits.Meters:
                    switch (speedUnits)
                    {
                        case SpeedUnits.FeetPerSecond:
                            // Meters to feet.
                            fLength2 = fLength/0.3048;
                            break;

                        case SpeedUnits.MetersPerSecond:
                            // Meters to meters.
                            fLength2 = fLength;
                            break;

                        case SpeedUnits.KilometersPerHour:
                            // Meters to kilometers.
                            fLength2 = fLength/1000;
                            break;

                        case SpeedUnits.MilesPerHour:
                            // Meters to miles.
                            fLength2 = fLength/1609.344;
                            break;

                        case SpeedUnits.Knots:
                            // Meters to nautical miles.
                            fLength2 = fLength/1852;
                            break;

                        case SpeedUnits.PixelsPerFrame:
                        default:
                            // Meters to Pixels. (revert)
                            fLength2 = fLength/PixelToUnit;
                            break;
                    }
                    break;

                case LengthUnits.Inches:
                    switch (speedUnits)
                    {
                        case SpeedUnits.FeetPerSecond:
                            // Inches to feet.
                            fLength2 = fLength/12;
                            break;

                        case SpeedUnits.MetersPerSecond:
                            // Inches to meters.
                            fLength2 = fLength/39.3700787;
                            break;

                        case SpeedUnits.KilometersPerHour:
                            // Inches to kilometers.
                            fLength2 = fLength/39370.0787;
                            break;

                        case SpeedUnits.MilesPerHour:
                            // Inches to miles.
                            fLength2 = fLength/63360;
                            break;

                        case SpeedUnits.Knots:
                            // Inches to nautical miles.
                            fLength2 = fLength/72913.3858;
                            break;

                        case SpeedUnits.PixelsPerFrame:
                        default:
                            // Inches to Pixels. (revert)
                            fLength2 = fLength/PixelToUnit;
                            break;
                    }
                    break;

                case LengthUnits.Feet:
                    switch (speedUnits)
                    {
                        case SpeedUnits.FeetPerSecond:
                            // Feet to feet.
                            fLength2 = fLength;
                            break;

                        case SpeedUnits.MetersPerSecond:
                            // Feet to meters.
                            fLength2 = fLength/3.2808399;
                            break;

                        case SpeedUnits.KilometersPerHour:
                            // Feet to kilometers.
                            fLength2 = fLength/3280.8399;
                            break;

                        case SpeedUnits.MilesPerHour:
                            // Feet to miles.
                            fLength2 = fLength/5280;
                            break;

                        case SpeedUnits.Knots:
                            // Feet to nautical miles.
                            fLength2 = fLength/6076.11549;
                            break;

                        case SpeedUnits.PixelsPerFrame:
                        default:
                            // Feet to Pixels. (revert)
                            fLength2 = fLength/PixelToUnit;
                            break;
                    }
                    break;

                case LengthUnits.Yards:
                    switch (speedUnits)
                    {
                        case SpeedUnits.FeetPerSecond:
                            // Yards to feet.
                            fLength2 = fLength*3;
                            break;

                        case SpeedUnits.MetersPerSecond:
                            // Yards to meters.
                            fLength2 = fLength/1.0936133;
                            break;

                        case SpeedUnits.KilometersPerHour:
                            // Yards to kilometers.
                            fLength2 = fLength/1093.6133;
                            break;

                        case SpeedUnits.MilesPerHour:
                            // Yards to miles.
                            fLength2 = fLength/1760;
                            break;

                        case SpeedUnits.Knots:
                            // Yards to nautical miles.
                            fLength2 = fLength/2025.37183;
                            break;

                        case SpeedUnits.PixelsPerFrame:
                        default:
                            // Yards to Pixels. (revert)
                            fLength2 = fLength/PixelToUnit;
                            break;
                    }
                    break;

                case LengthUnits.Pixels:
                default:
                    // If input length is in pixel, this means the image is not calibrated.
                    // Unless the target speed unit is pixel per frame, we can't compute the speed.
                    if (speedUnits != SpeedUnits.PixelsPerFrame)
                    {
                        fLength2 = 0;
                        Log.Error(
                            "Can't compute speed : image is not calibrated and speed is required in real world units.");
                    }
                    else
                    {
                        fLength2 = fLength;
                    }
                    break;
            }

            return fLength2;
        }

        private double ConvertToSpeedUnit(double fRawSpeed, int frames, SpeedUnits speedUnit)
        {
            // We now have the right length unit, but for the total time between the frames. (e.g: km/x frames).
            // Convert this to real world speed.
            // (depends on video frame rate).

            double fPerUserUnit = 0;

            // 1. per seconds
            var fPerSecond = (fRawSpeed/frames)*FramesPerSeconds;

            // 2. To required speed
            switch (speedUnit)
            {
                case SpeedUnits.FeetPerSecond:
                case SpeedUnits.MetersPerSecond:
                    // To seconds.
                    fPerUserUnit = fPerSecond;
                    break;

                case SpeedUnits.KilometersPerHour:
                case SpeedUnits.MilesPerHour:
                case SpeedUnits.Knots:
                    // To hours.
                    fPerUserUnit = fPerSecond*3600;
                    break;

                case SpeedUnits.PixelsPerFrame:
                default:
                    // To frames.
                    fPerUserUnit = fRawSpeed/frames;
                    break;
            }

            return fPerUserUnit;
        }

        #endregion Converters
    }
}