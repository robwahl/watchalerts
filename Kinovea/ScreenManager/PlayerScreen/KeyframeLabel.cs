#region License

/*
Copyright © Joan Charmant 2008-2011.
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
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     A class to encapsulate a mini label.
    ///     Mainly used for Keyframe labels / line measure / track speed.
    ///     The object is comprised of an attach point and the mini label itself.
    ///     The label can be moved relatively to the attach point from the container drawing tool.
    ///     The mini label position is expressed in absolute coordinates. (previously was relative to the attach).
    ///     The text to display is actually reset just before we need to draw it.
    /// </summary>
    public class KeyframeLabel
    {
        #region Properties

        public long Timestamp { get; set; }

        public int AttachIndex { get; set; }

        public Color BackColor
        {
            get { return _mStyleHelper.Bicolor.Background; }
            set { _mStyleHelper.Bicolor = new Bicolor(value); }
        }

        #endregion Properties

        #region Members

        private string _mText = "Label";
        private readonly RoundedRectangle _mBackground = new RoundedRectangle();
        private Point _mAttachLocation; // The point we are attached to (image coordinates).
        private readonly StyleHelper _mStyleHelper = new StyleHelper();
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Construction

        public KeyframeLabel()
            : this(Point.Empty, Color.Black)
        {
        }

        public KeyframeLabel(Point attachPoint, Color color)
        {
            _mAttachLocation = attachPoint;
            _mBackground.Rectangle = new Rectangle(attachPoint.Translate(-20, -50), Size.Empty);
            _mStyleHelper.Font = new Font("Arial", 8, FontStyle.Bold);
            _mStyleHelper.Bicolor = new Bicolor(Color.FromArgb(160, color));
        }

        public KeyframeLabel(XmlReader xmlReader, PointF scale)
            : this(Point.Empty, Color.Black)
        {
            ReadXml(xmlReader, scale);
        }

        #endregion Construction

        #region Public methods

        public bool HitTest(Point point)
        {
            return (_mBackground.HitTest(point, false) > -1);
        }

        public override int GetHashCode()
        {
            var iHash = 0;
            iHash ^= _mBackground.Rectangle.Location.GetHashCode();
            iHash ^= _mStyleHelper.GetHashCode();
            return iHash;
        }

        public void Draw(Graphics canvas, CoordinateSystem transformer, double fOpacityFactor)
        {
            using (var fillBrush = _mStyleHelper.GetBackgroundBrush((int)(fOpacityFactor * 255)))
            using (var p = _mStyleHelper.GetBackgroundPen((int)(fOpacityFactor * 64)))
            using (var f = _mStyleHelper.GetFont((float)transformer.Scale))
            using (var fontBrush = _mStyleHelper.GetForegroundBrush((int)(fOpacityFactor * 255)))
            {
                // Small dot and connector.
                var attch = transformer.Transform(_mAttachLocation);
                var center = transformer.Transform(_mBackground.Center);
                canvas.FillEllipse(fillBrush, attch.Box(2));
                canvas.DrawLine(p, attch, center);

                // Background and text.
                var textSize = canvas.MeasureString(_mText, f);
                var location = transformer.Transform(_mBackground.Rectangle.Location);
                var size = new Size((int)textSize.Width, (int)textSize.Height);
                var rect = new Rectangle(location, size);
                RoundedRectangle.Draw(canvas, rect, fillBrush, f.Height / 4, false);
                canvas.DrawString(_mText, f, fontBrush, rect.Location);
            }
        }

        public void SetAttach(Point point, bool moveLabel)
        {
            var dx = point.X - _mAttachLocation.X;
            var dy = point.Y - _mAttachLocation.Y;

            _mAttachLocation = point;

            if (moveLabel)
                _mBackground.Move(dx, dy);
        }

        public void SetLabel(Point point)
        {
            _mBackground.CenterOn(point);
        }

        public void MoveLabel(int dx, int dy)
        {
            _mBackground.Move(dx, dy);
        }

        public void SetText(string text)
        {
            _mText = text;

            using (var but = new Button())
            using (var g = but.CreateGraphics())
            using (var f = _mStyleHelper.GetFont(1F))
            {
                var textSize = g.MeasureString(_mText, f);
                _mBackground.Rectangle = new Rectangle(_mBackground.Rectangle.Location,
                    new Size((int)textSize.Width, (int)textSize.Height));
            }
        }

        public void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("SpacePosition", string.Format("{0};{1}", _mBackground.X, _mBackground.Y));
            xmlWriter.WriteElementString("TimePosition", Timestamp.ToString());
        }

        public void ReadXml(XmlReader xmlReader, PointF scale)
        {
            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "SpacePosition":
                        var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                        var location = new Point((int)(scale.X * p.X), (int)(scale.Y * p.Y));
                        _mBackground.Rectangle = new Rectangle(location, Size.Empty);
                        break;

                    case "TimePosition":
                        Timestamp = xmlReader.ReadElementContentAsLong();
                        break;

                    default:
                        var unparsed = xmlReader.ReadOuterXml();
                        Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }

            xmlReader.ReadEndElement();
        }

        #endregion Public methods
    }
}