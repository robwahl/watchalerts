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

using log4net;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     A class to encapsulate the various styling primitive a drawing may need for rendering,
    ///     and provide some utility functions to get a Pen, Brush, Font or Color object according to client opacity or zoom.
    ///     Typical drawing would use just two or three of the primitive for its decoration and leave the others undefined.
    ///     The primitives can be bound to a style element (editable in the UI) through the Bind() method on the
    ///     style element, passing the name of the primitive. The binding will be effective only if types are compatible.
    ///     todo: example.
    /// </summary>
    /// <remarks>
    ///     This class should merge and replace "LineStyle" and "InfoTextDecoration" classes.
    /// </remarks>
    public class StyleHelper
    {
        #region Constructor

        public StyleHelper()
        {
            BindWrite = DoBindWrite;
            BindRead = DoBindRead;
        }

        #endregion Constructor

        #region Exposed function delegates

        public BindWriter BindWrite;
        public BindReader BindRead;

        /// <summary>
        ///     Event raised when the value is changed dynamically through binding.
        ///     This may be useful if the Drawing has several StyleHelper that must be linked somehow.
        ///     An example use is when we change the main color of the track, we need to propagate the change
        ///     to the small label attached (for the Label following mode).
        /// </summary>
        /// <remarks>The event is not raised when the value is changed manually through a property setter</remarks>
        public event EventHandler ValueChanged;

        #endregion Exposed function delegates

        #region Properties

        public Color Color
        {
            get { return _mColor; }
            set { _mColor = value; }
        }

        public int LineSize { get; }

        public LineEnding LineEnding
        {
            get { return _mLineEnding; }
            set { _mLineEnding = value; }
        }

        public Font Font
        {
            get { return _mFont; }
            set
            {
                if (value != null)
                {
                    // We make temp copies of the variables because we call .Dispose() but
                    // it's possible that input value was pointing to the same reference.
                    var fontName = value.Name;
                    var fontStyle = value.Style;
                    var fontSize = value.Size;
                    _mFont.Dispose();
                    _mFont = new Font(fontName, fontSize, fontStyle);
                }
                else
                {
                    _mFont.Dispose();
                    _mFont = null;
                }
            }
        }

        public Bicolor Bicolor
        {
            get { return _mBicolor; }
            set { _mBicolor = value; }
        }

        public TrackShape TrackShape
        {
            get { return _mTrackShape; }
            set { _mTrackShape = value; }
        }

        #endregion Properties

        #region Members

        private Color _mColor;
        private Font _mFont = new Font("Arial", 12, FontStyle.Regular);
        private Bicolor _mBicolor;
        private LineEnding _mLineEnding = LineEnding.None;
        private TrackShape _mTrackShape = TrackShape.Solid;

        // Internal only
        private static readonly int[] MAllowedFontSizes = {8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36};

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Public Methods

        #region Color and LineSize properties

        /// <summary>
        ///     Returns a Pen object suitable to draw a background or color only contour.
        ///     The pen object will only integrate the color property and be of width 1.
        /// </summary>
        /// <param name="iAlpha">Alpha value to multiply the color with</param>
        /// <returns>Pen object initialized with the current value of color and width = 1.0</returns>
        public Pen GetPen(int iAlpha)
        {
            var c = (iAlpha >= 0 && iAlpha <= 255) ? Color.FromArgb(iAlpha, _mColor) : _mColor;

            return NormalPen(new Pen(c, 1.0f));
        }

        public Pen GetPen(double fOpacity)
        {
            return GetPen((int) (fOpacity*255));
        }

        /// <summary>
        ///     Returns a Pen object suitable to draw a line or contour.
        ///     The pen object will integrate the color, line size, line shape, and line endings properties.
        /// </summary>
        /// <param name="iAlpha">Alpha value to multiply the color with</param>
        /// <param name="fStretchFactor">zoom value to multiply the line size with</param>
        /// <returns>Pen object initialized with the current value of color and line size properties</returns>
        public Pen GetPen(int iAlpha, double fStretchFactor)
        {
            var c = (iAlpha >= 0 && iAlpha <= 255) ? Color.FromArgb(iAlpha, _mColor) : _mColor;
            var fPenWidth = (float) (LineSize*fStretchFactor);
            if (fPenWidth < 1) fPenWidth = 1;

            var p = new Pen(c, fPenWidth);
            p.LineJoin = LineJoin.Round;

            // Line endings
            p.StartCap = _mLineEnding.StartCap;
            p.EndCap = _mLineEnding.EndCap;

            // Line shape
            p.DashStyle = _mTrackShape.DashStyle;

            return p;
        }

        public Pen GetPen(double fOpacity, double fStretchFactor)
        {
            return GetPen((int) (fOpacity*255), fStretchFactor);
        }

        /// <summary>
        ///     Returns a Brush object suitable to draw a background or colored area.
        ///     Only use the color property.
        /// </summary>
        /// <param name="iAlpha">Alpha value to multiply the color with</param>
        /// <returns>Brush object initialized with the current value of color property</returns>
        public SolidBrush GetBrush(int iAlpha)
        {
            var c = (iAlpha >= 0 && iAlpha <= 255) ? Color.FromArgb(iAlpha, _mColor) : _mColor;
            return new SolidBrush(c);
        }

        public SolidBrush GetBrush(double fOpacity)
        {
            return GetBrush((int) (fOpacity*255));
        }

        #endregion Color and LineSize properties

        #region Font property

        public Font GetFont(float fStretchFactor)
        {
            var fFontSize = GetRescaledFontSize(fStretchFactor);
            return new Font(_mFont.Name, fFontSize, _mFont.Style);
        }

        public Font GetFontDefaultSize(int fontSize)
        {
            return new Font(_mFont.Name, fontSize, _mFont.Style);
        }

        public void ForceFontSize(int wantedHeight, string text)
        {
            // Compute the optimal font size from a given background rectangle.
            // This is used when the user drag the bottom right corner to resize the text.
            // _wantedHeight is unscaled.
            var but = new Button();
            var g = but.CreateGraphics();

            // We must loop through all allowed font size and compute the output rectangle to find the best match.
            // We only compare with wanted height for simplicity.
            var iSmallestDiff = int.MaxValue;
            var iBestCandidate = MAllowedFontSizes[0];

            foreach (var size in MAllowedFontSizes)
            {
                var testFont = new Font(_mFont.Name, size, _mFont.Style);
                var bgSize = g.MeasureString(text + " ", testFont);
                testFont.Dispose();

                var diff = Math.Abs(wantedHeight - (int) bgSize.Height);

                if (diff < iSmallestDiff)
                {
                    iSmallestDiff = diff;
                    iBestCandidate = size;
                }
            }

            g.Dispose();

            // Push to internal value.
            var fontName = _mFont.Name;
            var fontStyle = _mFont.Style;
            _mFont.Dispose();
            _mFont = new Font(fontName, iBestCandidate, fontStyle);
        }

        #endregion Font property

        #region Bicolor property

        public Color GetForegroundColor(int iAlpha)
        {
            var c = (iAlpha >= 0 && iAlpha <= 255) ? Color.FromArgb(iAlpha, _mBicolor.Foreground) : _mBicolor.Foreground;
            return c;
        }

        public SolidBrush GetForegroundBrush(int iAlpha)
        {
            var c = GetForegroundColor(iAlpha);
            return new SolidBrush(c);
        }

        public Pen GetForegroundPen(int iAlpha)
        {
            var c = GetForegroundColor(iAlpha);
            return NormalPen(new Pen(c, 1.0f));
        }

        public Color GetBackgroundColor(int iAlpha)
        {
            var c = (iAlpha >= 0 && iAlpha <= 255) ? Color.FromArgb(iAlpha, _mBicolor.Background) : _mBicolor.Background;
            return c;
        }

        public SolidBrush GetBackgroundBrush(int iAlpha)
        {
            var c = GetBackgroundColor(iAlpha);
            return new SolidBrush(c);
        }

        public Pen GetBackgroundPen(int iAlpha)
        {
            var c = GetBackgroundColor(iAlpha);
            return NormalPen(new Pen(c, 1.0f));
        }

        #endregion Bicolor property

        public override int GetHashCode()
        {
            var iHash = 0;

            iHash ^= _mColor.GetHashCode();
            iHash ^= LineSize.GetHashCode();
            iHash ^= _mFont.GetHashCode();
            iHash ^= _mBicolor.GetHashCode();
            iHash ^= _mLineEnding.GetHashCode();
            iHash ^= _mTrackShape.GetHashCode();

            return iHash;
        }

        #endregion Public Methods

        #region Private Methods

        private void DoBindWrite(string targetProperty, object value)
        {
            // Check type and import value if compatible with the target prop.
            var imported = false;
            switch (targetProperty)
            {
                case "Color":
                {
                    if (value is Color)
                    {
                        _mColor = (Color) value;
                        imported = true;
                    }
                    break;
                }
                case "LineSize":
                {
                    if (value is int)
                    {
                        LineSize = (int) value;
                        imported = true;
                    }

                    break;
                }
                case "LineEnding":
                {
                    if (value is LineEnding)
                    {
                        _mLineEnding = (LineEnding) value;
                        imported = true;
                    }

                    break;
                }
                case "TrackShape":
                {
                    if (value is TrackShape)
                    {
                        _mTrackShape = (TrackShape) value;
                        imported = true;
                    }

                    break;
                }
                case "Font":
                {
                    if (value is int)
                    {
                        // Recreate the font changing just the size.
                        var fontName = _mFont.Name;
                        var fontStyle = _mFont.Style;
                        _mFont.Dispose();
                        _mFont = new Font(fontName, (int) value, fontStyle);
                        imported = true;
                    }
                    break;
                }
                case "Bicolor":
                {
                    if (value is Color)
                    {
                        _mBicolor.Background = (Color) value;
                        imported = true;
                    }
                    break;
                }
                default:
                {
                    Log.DebugFormat("Unknown target property \"{0}\".", targetProperty);
                    break;
                }
            }

            if (imported)
            {
                if (ValueChanged != null) ValueChanged(null, EventArgs.Empty);
            }
            else
            {
                Log.DebugFormat("Could not import value \"{0}\" to property \"{1}\".", value, targetProperty);
            }
        }

        private object DoBindRead(string sourceProperty, Type targetType)
        {
            // Take the local property and extract something of the required type.
            // This function is used by style elements to stay up to date in case the bound property has been modified externally.
            // The style element might be of an entirely different type than the property.
            var converted = false;
            object result = null;
            switch (sourceProperty)
            {
                case "Color":
                {
                    if (targetType == typeof (Color))
                    {
                        result = _mColor;
                        converted = true;
                    }
                    break;
                }
                case "LineSize":
                {
                    if (targetType == typeof (int))
                    {
                        result = LineSize;
                        converted = true;
                    }
                    break;
                }
                case "LineEnding":
                {
                    if (targetType == typeof (LineEnding))
                    {
                        result = _mLineEnding;
                        converted = true;
                    }
                    break;
                }
                case "TrackShape":
                {
                    if (targetType == typeof (TrackShape))
                    {
                        result = _mTrackShape;
                        converted = true;
                    }
                    break;
                }
                case "Font":
                {
                    if (targetType == typeof (int))
                    {
                        result = (int) _mFont.Size;
                        converted = true;
                    }
                    break;
                }
                case "Bicolor":
                {
                    if (targetType == typeof (Color))
                    {
                        result = _mBicolor.Background;
                        converted = true;
                    }
                    break;
                }
                default:
                {
                    Log.DebugFormat("Unknown source property \"{0}\".", sourceProperty);
                    break;
                }
            }

            if (!converted)
            {
                Log.DebugFormat("Could not convert property \"{0}\" to update value \"{1}\".", sourceProperty,
                    targetType);
            }

            return result;
        }

        private float GetRescaledFontSize(float fStretchFactor)
        {
            // Get the strecthed font size.
            // The final font size returned here may not be part of the allowed font sizes
            // and may exeed the max allowed font size, because it's just for rendering purposes.
            var fFontSize = _mFont.Size*fStretchFactor;
            if (fFontSize < 8) fFontSize = 8;
            return fFontSize;
        }

        private Pen NormalPen(Pen p)
        {
            p.StartCap = LineCap.Round;
            p.EndCap = LineCap.Round;
            p.LineJoin = LineJoin.Round;
            return p;
        }

        #endregion Private Methods
    }

    /// <summary>
    ///     A simple wrapper around two color values.
    ///     When setting the background color, the foreground color is automatically adjusted
    ///     to black or white depending on the luminosity of the background color.
    /// </summary>
    public struct Bicolor
    {
        private Color _mBackground;

        public Bicolor(Color backColor)
        {
            _mBackground = backColor;
            Foreground = backColor.GetBrightness() >= 0.5 ? Color.Black : Color.White;
        }

        public Color Foreground { get; private set; }

        public Color Background
        {
            get { return _mBackground; }
            set
            {
                _mBackground = value;
                Foreground = value.GetBrightness() >= 0.5 ? Color.Black : Color.White;
            }
        }
    }

    /// <summary>
    ///     A simple wrapper around two LineCap values.
    ///     Used to describe arrow endings and possibly other endings.
    /// </summary>
    [TypeConverter(typeof (LineEndingConverter))]
    public struct LineEnding
    {
        public readonly LineCap EndCap;
        public readonly LineCap StartCap;

        public LineEnding(LineCap start, LineCap end)
        {
            StartCap = start;
            EndCap = end;
        }

        #region Static properties

        public static LineEnding None { get; } = private new LineEnding(LineCap.Round, LineCap.Round);

        public static LineEnding StartArrow { get; } = private new LineEnding(LineCap.ArrowAnchor, LineCap.Round);

        public static LineEnding EndArrow { get; } = private new LineEnding(LineCap.Round, LineCap.ArrowAnchor);

        public static LineEnding DoubleArrow { get; } = private new LineEnding(LineCap.ArrowAnchor, LineCap.ArrowAnchor);

        #endregion Static properties
    }

    /// <summary>
    ///     Converter class for LineEnding.
    ///     Support: string.
    /// </summary>
    public class LineEndingConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof (string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof (string))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                var stringValue = value as string;

                if (stringValue.Length == 0)
                    return LineEnding.None;

                var split = stringValue.Split(';');

                if (split.Length != 2)
                    return LineEnding.None;

                var enumConverter = TypeDescriptor.GetConverter(typeof (LineCap));
                var start = (LineCap) enumConverter.ConvertFromString(context, culture, split[0]);
                var end = (LineCap) enumConverter.ConvertFromString(context, culture, split[1]);

                return new LineEnding(start, end);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            if (destinationType == typeof (string))
            {
                var lineEnding = (LineEnding) value;
                var enumConverter = TypeDescriptor.GetConverter(typeof (LineCap));
                var result = string.Format("{0};{1}",
                    enumConverter.ConvertToString(context, culture, lineEnding.StartCap),
                    enumConverter.ConvertToString(context, culture, lineEnding.EndCap));
                return result;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    ///     A simple wrapper around a dash style and the presence of time ticks.
    ///     Used to describe line shape for tracks.
    /// </summary>
    [TypeConverter(typeof (TrackShapeConverter))]
    public struct TrackShape
    {
        public readonly DashStyle DashStyle;
        public readonly bool ShowSteps;

        public TrackShape(DashStyle style, bool steps)
        {
            DashStyle = style;
            ShowSteps = steps;
        }

        #region Static Properties

        public static TrackShape Solid { get; } = private new TrackShape(DashStyle.Solid, false);

        public static TrackShape Dash { get; } = private new TrackShape(DashStyle.Dash, false);

        public static TrackShape SolidSteps { get; } = private new TrackShape(DashStyle.Solid, true);

        public static TrackShape DashSteps { get; } = private new TrackShape(DashStyle.Dash, true);

        #endregion Static Properties
    }

    /// <summary>
    ///     Converter class for TrackShape.
    ///     Support: string.
    /// </summary>
    public class TrackShapeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof (string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof (string))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                var stringValue = value as string;

                if (stringValue.Length == 0)
                    return TrackShape.Solid;

                var split = stringValue.Split(';');

                if (split.Length != 2)
                    return TrackShape.Solid;

                var enumConverter = TypeDescriptor.GetConverter(typeof (DashStyle));
                var dash = (DashStyle) enumConverter.ConvertFromString(context, culture, split[0]);

                var boolConverter = TypeDescriptor.GetConverter(typeof (bool));
                var steps = (bool) boolConverter.ConvertFromString(context, culture, split[1]);

                return new TrackShape(dash, steps);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
            Type destinationType)
        {
            if (destinationType == typeof (string))
            {
                var trackShape = (TrackShape) value;
                var enumConverter = TypeDescriptor.GetConverter(typeof (DashStyle));
                var result = string.Format("{0};{1}",
                    enumConverter.ConvertToString(context, culture, trackShape.DashStyle),
                    trackShape.ShowSteps ? "true" : "false");
                return result;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}