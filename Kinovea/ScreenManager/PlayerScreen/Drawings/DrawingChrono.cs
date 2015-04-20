#region License

/*
Copyright © Joan Charmant 2008-2009.
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
using Kinovea.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace Kinovea.ScreenManager
{
    [XmlType("Chrono")]
    public class DrawingChrono : AbstractDrawing, IDecorable, IKvaSerializable
    {
        public override string ToString()
        {
            return ScreenManagerLang.ToolTip_DrawingToolChrono;
        }

        public override int GetHashCode()
        {
            var iHash = _mMainBackground.GetHashCode();
            iHash ^= TimeStart.GetHashCode();
            iHash ^= TimeStop.GetHashCode();
            iHash ^= TimeVisible.GetHashCode();
            iHash ^= TimeInvisible.GetHashCode();
            iHash ^= CountDown.GetHashCode();
            iHash ^= _mStyleHelper.GetHashCode();
            iHash ^= _mLabel.GetHashCode();
            iHash ^= ShowLabel.GetHashCode();

            return iHash;
        }

        #region Properties

        public DrawingStyle DrawingStyle { get; private set; }

        public override InfosFading InfosFading
        {
            // Fading is not modifiable from outside for chrono.
            get { throw new NotImplementedException("DrawingChrono, The method or operation is not implemented."); }
            set { throw new NotImplementedException("DrawingChrono, The method or operation i not implemented."); }
        }

        public override DrawingCapabilities Caps
        {
            get { return DrawingCapabilities.ConfigureColorSize; }
        }

        public override List<ToolStripMenuItem> ContextMenu
        {
            get { return null; }
        }

        public Metadata ParentMetadata
        {
            get;
            // unused.
            set;
        }

        public long TimeStart { get; }

        public long TimeStop { get; }

        public long TimeVisible { get; }

        public long TimeInvisible { get; }

        public bool CountDown { get; }

        public bool HasTimeStop
        {
            // This is used to know if we can toggle to countdown or not.
            get { return (TimeStop != long.MaxValue); }
        }

        // The following properties are used from the formConfigureChrono.
        public string Label
        {
            get { return _mLabel; }
            set
            {
                _mLabel = value;
                UpdateLabelRectangle();
            }
        }

        public bool ShowLabel { get; }

        #endregion Properties

        #region Members

        // Core

        private string _mTimecode;
        private string _mLabel;

        // Decoration
        private readonly StyleHelper _mStyleHelper = new StyleHelper();

        private readonly InfosFading _mInfosFading;

        private static readonly int MIAllowedFramesOver = 12;
        // Number of frames the chrono stays visible after the 'Hiding' point.

        private readonly RoundedRectangle _mMainBackground = new RoundedRectangle();
        private readonly RoundedRectangle _mLblBackground = new RoundedRectangle();

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructors

        public DrawingChrono(Point p, long start, long averageTimeStampsPerFrame, DrawingStyle preset)
        {
            // Core
            TimeVisible = start;
            TimeStart = long.MaxValue;
            TimeStop = long.MaxValue;
            TimeInvisible = long.MaxValue;
            CountDown = false;
            _mMainBackground.Rectangle = new Rectangle(p, Size.Empty);

            _mTimecode = "error";

            _mStyleHelper.Bicolor = new Bicolor(Color.Black);
            _mStyleHelper.Font = new Font("Arial", 16, FontStyle.Bold);
            if (preset != null)
            {
                DrawingStyle = preset.Clone();
                BindStyle();
            }

            _mLabel = "";
            ShowLabel = true;

            // We use the InfosFading utility to fade the chrono away.
            // The refererence frame will be the frame at which fading start.
            // Must be updated on "Hide" menu.
            _mInfosFading = new InfosFading(TimeInvisible, averageTimeStampsPerFrame);
            _mInfosFading.FadingFrames = MIAllowedFramesOver;
            _mInfosFading.UseDefault = false;
        }

        public DrawingChrono(XmlReader xmlReader, PointF scale, TimeStampMapper remapTimestampCallback)
            : this(Point.Empty, 0, 1, ToolManager.Chrono.StylePreset.Clone())
        {
            ReadXml(xmlReader, scale, remapTimestampCallback);
        }

        #endregion Constructors

        #region AbstractDrawing Implementation

        public override void Draw(Graphics canvas, CoordinateSystem transformer, bool bSelected, long iCurrentTimestamp)
        {
            if (iCurrentTimestamp < TimeVisible)
                return;

            var fOpacityFactor = 1.0;
            if (iCurrentTimestamp > TimeInvisible)
                fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);

            if (fOpacityFactor <= 0)
                return;

            _mTimecode = GetTimecode(iCurrentTimestamp);

            // Update unscaled backround size according to timecode text. Needed for hit testing.
            var f = _mStyleHelper.GetFont(1F);
            var totalSize = canvas.MeasureString(" " + _mTimecode + " ", f);
            var textSize = canvas.MeasureString(_mTimecode, f);
            f.Dispose();
            _mMainBackground.Rectangle = new Rectangle(_mMainBackground.Rectangle.Location,
                new Size((int)totalSize.Width, (int)totalSize.Height));

            using (var brushBack = _mStyleHelper.GetBackgroundBrush((int)(fOpacityFactor * 128)))
            using (var brushText = _mStyleHelper.GetForegroundBrush((int)(fOpacityFactor * 255)))
            using (var fontText = _mStyleHelper.GetFont((float)transformer.Scale))
            {
                var rect = transformer.Transform(_mMainBackground.Rectangle);
                RoundedRectangle.Draw(canvas, rect, brushBack, fontText.Height / 4, false);

                var margin = (int)((totalSize.Width - textSize.Width) / 2);
                var textLocation = new Point(rect.X + margin, rect.Y);
                canvas.DrawString(_mTimecode, fontText, brushText, textLocation);

                if (ShowLabel && _mLabel.Length > 0)
                {
                    using (var fontLabel = _mStyleHelper.GetFont((float)transformer.Scale * 0.5f))
                    {
                        var lblTextSize = canvas.MeasureString(_mLabel, fontLabel);
                        var lblRect = new Rectangle(rect.Location.X, rect.Location.Y - (int)lblTextSize.Height,
                            (int)lblTextSize.Width, (int)lblTextSize.Height);
                        RoundedRectangle.Draw(canvas, lblRect, brushBack, fontLabel.Height / 3, true);
                        canvas.DrawString(_mLabel, fontLabel, brushText, lblRect.Location);
                    }
                }
            }
        }

        public override int HitTest(Point point, long iCurrentTimestamp)
        {
            // Convention: miss = -1, object = 0, handle = n.
            var iHitResult = -1;
            var iMaxHitTimeStamps = TimeInvisible;
            if (iMaxHitTimeStamps != long.MaxValue)
                iMaxHitTimeStamps += (MIAllowedFramesOver * ParentMetadata.AverageTimeStampsPerFrame);

            if (iCurrentTimestamp >= TimeVisible && iCurrentTimestamp <= iMaxHitTimeStamps)
            {
                iHitResult = _mMainBackground.HitTest(point, true);
                if (iHitResult < 0)
                    iHitResult = _mLblBackground.HitTest(point, false);
            }

            return iHitResult;
        }

        public override void MoveHandle(Point point, int iHandleNumber)
        {
            // Invisible handler to change font size.
            var wantedHeight = point.Y - _mMainBackground.Rectangle.Location.Y;
            _mStyleHelper.ForceFontSize(wantedHeight, _mTimecode);
            UpdateLabelRectangle();
        }

        public override void MoveDrawing(int deltaX, int deltaY, Keys modifierKeys)
        {
            _mMainBackground.Move(deltaX, deltaY);
            _mLblBackground.Move(deltaX, deltaY);
        }

        #endregion AbstractDrawing Implementation

        #region KVA Serialization

        public void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("Position",
                string.Format("{0};{1}", _mMainBackground.Rectangle.Location.X, _mMainBackground.Rectangle.Location.Y));

            xmlWriter.WriteStartElement("Values");
            xmlWriter.WriteElementString("Visible", (TimeVisible == long.MaxValue) ? "-1" : TimeVisible.ToString());
            xmlWriter.WriteElementString("StartCounting", (TimeStart == long.MaxValue) ? "-1" : TimeStart.ToString());
            xmlWriter.WriteElementString("StopCounting", (TimeStop == long.MaxValue) ? "-1" : TimeStop.ToString());
            xmlWriter.WriteElementString("Invisible", (TimeInvisible == long.MaxValue) ? "-1" : TimeInvisible.ToString());
            xmlWriter.WriteElementString("Countdown", CountDown ? "true" : "false");

            // Spreadsheet support
            var userDuration = "0";
            if (TimeStart != long.MaxValue && TimeStop != long.MaxValue)
            {
                userDuration = ParentMetadata.TimeStampsToTimecode(TimeStop - TimeStart, TimeCodeFormat.Unknown, false);
            }
            xmlWriter.WriteElementString("UserDuration", userDuration);

            // </values>
            xmlWriter.WriteEndElement();

            // Label
            xmlWriter.WriteStartElement("Label");
            xmlWriter.WriteElementString("Text", _mLabel);
            xmlWriter.WriteElementString("Show", ShowLabel ? "true" : "false");
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("DrawingStyle");
            DrawingStyle.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();
        }

        private void ReadXml(XmlReader xmlReader, PointF scale, TimeStampMapper remapTimestampCallback)
        {
            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "Position":
                        var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                        var location = new Point((int)(p.X * scale.X), (int)(p.Y * scale.Y));
                        _mMainBackground.Rectangle = new Rectangle(location, Size.Empty);
                        break;

                    case "Values":
                        ParseWorkingValues(xmlReader, remapTimestampCallback);
                        break;

                    case "DrawingStyle":
                        DrawingStyle = new DrawingStyle(xmlReader);
                        BindStyle();
                        break;

                    case "Label":
                        ParseLabel(xmlReader);
                        break;

                    default:
                        var unparsed = xmlReader.ReadOuterXml();
                        Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }

            xmlReader.ReadEndElement();
        }

        private void ParseWorkingValues(XmlReader xmlReader, TimeStampMapper remapTimestampCallback)
        {
            if (remapTimestampCallback == null)
            {
                xmlReader.ReadOuterXml();
                return;
            }

            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "Visible":
                        TimeVisible = remapTimestampCallback(xmlReader.ReadElementContentAsLong(), false);
                        break;

                    case "StartCounting":
                        var start = xmlReader.ReadElementContentAsLong();
                        TimeStart = (start == -1) ? long.MaxValue : remapTimestampCallback(start, false);
                        break;

                    case "StopCounting":
                        var stop = xmlReader.ReadElementContentAsLong();
                        TimeStop = (stop == -1) ? long.MaxValue : remapTimestampCallback(stop, false);
                        break;

                    case "Invisible":
                        var hide = xmlReader.ReadElementContentAsLong();
                        TimeInvisible = (hide == -1) ? long.MaxValue : remapTimestampCallback(hide, false);
                        break;

                    case "Countdown":
                        CountDown = XmlHelper.ParseBoolean(xmlReader.ReadElementContentAsString());
                        break;

                    default:
                        var unparsed = xmlReader.ReadOuterXml();
                        Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }

            xmlReader.ReadEndElement();

            // Sanity check values.
            if (TimeVisible < 0) TimeVisible = 0;
            if (TimeStart < 0) TimeStart = 0;
            if (TimeStop < 0) TimeStop = 0;
            if (TimeInvisible < 0) TimeInvisible = 0;

            if (TimeVisible > TimeStart)
            {
                TimeVisible = TimeStart;
            }

            if (TimeStop < TimeStart)
            {
                TimeStop = long.MaxValue;
            }

            if (TimeInvisible < TimeStop)
            {
                TimeInvisible = long.MaxValue;
            }
        }

        private void ParseLabel(XmlReader xmlReader)
        {
            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "Text":
                        _mLabel = xmlReader.ReadElementContentAsString();
                        break;

                    case "Show":
                        ShowLabel = XmlHelper.ParseBoolean(xmlReader.ReadElementContentAsString());
                        break;

                    default:
                        var unparsed = xmlReader.ReadOuterXml();
                        Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }

            xmlReader.ReadEndElement();
        }

        #endregion KVA Serialization

        #region PopMenu commands Implementation that change internal values.

        public void Start(long iCurrentTimestamp)
        {
            TimeStart = iCurrentTimestamp;

            // Reset if crossed.
            if (TimeStart >= TimeStop)
            {
                TimeStop = long.MaxValue;
            }
        }

        public void Stop(long iCurrentTimestamp)
        {
            TimeStop = iCurrentTimestamp;

            // ? if crossed.
            if (TimeStop <= TimeStart)
            {
                TimeStart = TimeStop;
            }

            if (TimeStop > TimeInvisible)
            {
                TimeInvisible = TimeStop;
            }
        }

        public void Hide(long iCurrentTimestamp)
        {
            TimeInvisible = iCurrentTimestamp;

            // Update fading conf.
            _mInfosFading.ReferenceTimestamp = TimeInvisible;

            // Avoid counting when fading.
            if (TimeInvisible < TimeStop)
            {
                TimeStop = TimeInvisible;
                if (TimeStop < TimeStart)
                {
                    TimeStart = TimeStop;
                }
            }
        }

        #endregion PopMenu commands Implementation that change internal values.

        #region Lower level helpers

        private void BindStyle()
        {
            DrawingStyle.Bind(_mStyleHelper, "Bicolor", "color");
            DrawingStyle.Bind(_mStyleHelper, "Font", "font size");
        }

        private void UpdateLabelRectangle()
        {
            using (var f = _mStyleHelper.GetFont(0.5F))
            using (var but = new Button())
            using (var g = but.CreateGraphics())
            {
                var size = g.MeasureString(_mLabel, f);
                _mLblBackground.Rectangle = new Rectangle(_mMainBackground.X,
                    _mMainBackground.Y - _mLblBackground.Rectangle.Height,
                    (int)size.Width + 11,
                    (int)size.Height);
            }
        }

        private string GetTimecode(long iTimestamp)
        {
            long timestamps;

            // compute Text value depending on where we are.
            if (iTimestamp > TimeStart)
            {
                if (iTimestamp <= TimeStop)
                {
                    // After start and before stop.
                    if (CountDown)
                        timestamps = TimeStop - iTimestamp;
                    else
                        timestamps = iTimestamp - TimeStart;
                }
                else
                {
                    // After stop. Keep max value.
                    timestamps = CountDown ? 0 : TimeStop - TimeStart;
                }
            }
            else
            {
                // Before start. Keep min value.
                timestamps = CountDown ? TimeStop - TimeStart : 0;
            }

            return ParentMetadata.TimeStampsToTimecode(timestamps, TimeCodeFormat.Unknown, false);
        }

        #endregion Lower level helpers
    }

    /// <summary>
    ///     Enum used in CommandModifyChrono to know what value we are touching.
    /// </summary>
    public enum ChronoModificationType
    {
        TimeStart,
        TimeStop,
        TimeHide,
        Countdown
    }
}