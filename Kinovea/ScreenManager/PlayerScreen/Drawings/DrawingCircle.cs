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

using Kinovea.ScreenManager.Languages;
using Kinovea.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace Kinovea.ScreenManager
{
    [XmlType("Circle")]
    public class DrawingCircle : AbstractDrawing, IKvaSerializable, IDecorable, IInitializable
    {
        #region IInitializable implementation

        public void ContinueSetup(Point point)
        {
            MoveHandle(point, 1);
        }

        #endregion IInitializable implementation

        public override string ToString()
        {
            return ScreenManagerLang.ToolTip_DrawingToolCircle;
        }

        public override int GetHashCode()
        {
            var iHash = _mCenter.GetHashCode();
            iHash ^= _mIRadius.GetHashCode();
            iHash ^= _mStyleHelper.GetHashCode();
            return iHash;
        }

        #region Properties

        public DrawingStyle DrawingStyle { get; private set; }

        public override InfosFading InfosFading
        {
            get { return _mInfosFading; }
            set { _mInfosFading = value; }
        }

        public override DrawingCapabilities Caps
        {
            get { return DrawingCapabilities.ConfigureColorSize | DrawingCapabilities.Fading; }
        }

        public override List<ToolStripMenuItem> ContextMenu
        {
            get { return null; }
        }

        #endregion Properties

        #region Members

        // Core
        private Point _mCenter;

        private int _mIRadius;
        private bool _mBSelected;

        // Decoration
        private readonly StyleHelper _mStyleHelper = new StyleHelper();

        private InfosFading _mInfosFading;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor

        public DrawingCircle(Point center, int radius, long iTimestamp, long iAverageTimeStampsPerFrame,
            DrawingStyle preset)
        {
            _mCenter = center;
            _mIRadius = Math.Min(radius, 10);
            _mInfosFading = new InfosFading(iTimestamp, iAverageTimeStampsPerFrame);

            _mStyleHelper.Color = Color.Empty;
            _mStyleHelper.LineSize = 1;
            if (preset != null)
            {
                DrawingStyle = preset.Clone();
                BindStyle();
            }
        }

        public DrawingCircle(XmlReader xmlReader, PointF scale, Metadata parent)
            : this(Point.Empty, 0, 0, 0, ToolManager.Circle.StylePreset.Clone())
        {
            ReadXml(xmlReader, scale);
        }

        #endregion Constructor

        #region AbstractDrawing Implementation

        public override void Draw(Graphics canvas, CoordinateSystem transformer, bool bSelected, long iCurrentTimestamp)
        {
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor <= 0)
                return;

            var alpha = (int)(fOpacityFactor * 255);
            _mBSelected = bSelected;

            using (var p = _mStyleHelper.GetPen(alpha, transformer.Scale))
            {
                var boundingBox = transformer.Transform(_mCenter.Box(_mIRadius));
                canvas.DrawEllipse(p, boundingBox);

                if (bSelected)
                {
                    // Handler: arc in lower right quadrant.
                    p.Color = p.Color.Invert();
                    canvas.DrawArc(p, boundingBox, 25, 40);
                }
            }
        }

        public override void MoveHandle(Point point, int iHandleNumber)
        {
            // User is dragging the outline of the circle, figure out the new radius at this point.
            var shiftX = Math.Abs(point.X - _mCenter.X);
            var shiftY = Math.Abs(point.Y - _mCenter.Y);
            _mIRadius = (int)Math.Sqrt((shiftX * shiftX) + (shiftY * shiftY));
            if (_mIRadius < 10)
                _mIRadius = 10;
        }

        public override void MoveDrawing(int deltaX, int deltaY, Keys modifierKeys)
        {
            _mCenter.X += deltaX;
            _mCenter.Y += deltaY;
        }

        public override int HitTest(Point point, long iCurrentTimestamp)
        {
            // Convention: miss = -1, object = 0, handle = n.
            var iHitResult = -1;
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor > 0)
            {
                if (_mBSelected && IsPointOnHandler(point))
                    iHitResult = 1;
                else if (IsPointInObject(point))
                    iHitResult = 0;
            }
            return iHitResult;
        }

        #endregion AbstractDrawing Implementation

        #region KVA Serialization

        private void ReadXml(XmlReader xmlReader, PointF scale)
        {
            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "Origin":
                        var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                        _mCenter = new Point((int)(p.X * scale.X), (int)(p.Y * scale.Y));
                        break;

                    case "Radius":
                        var radius = xmlReader.ReadElementContentAsInt();
                        _mIRadius = (int)((double)radius * scale.X);
                        break;

                    case "DrawingStyle":
                        DrawingStyle = new DrawingStyle(xmlReader);
                        BindStyle();
                        break;

                    case "InfosFading":
                        _mInfosFading.ReadXml(xmlReader);
                        break;

                    default:
                        var unparsed = xmlReader.ReadOuterXml();
                        Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }

            xmlReader.ReadEndElement();
        }

        public void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("Origin", string.Format("{0};{1}", _mCenter.X, _mCenter.Y));
            xmlWriter.WriteElementString("Radius", _mIRadius.ToString());

            xmlWriter.WriteStartElement("DrawingStyle");
            DrawingStyle.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("InfosFading");
            _mInfosFading.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();
        }

        #endregion KVA Serialization

        #region Lower level helpers

        private void BindStyle()
        {
            DrawingStyle.Bind(_mStyleHelper, "Color", "color");
            DrawingStyle.Bind(_mStyleHelper, "LineSize", "pen size");
        }

        private bool IsPointInObject(Point point)
        {
            var bIsPointInObject = false;
            var areaPath = new GraphicsPath();
            areaPath.AddEllipse(_mCenter.Box(_mIRadius + 10));
            bIsPointInObject = new Region(areaPath).IsVisible(point);
            return bIsPointInObject;
        }

        private bool IsPointOnHandler(Point point)
        {
            var bIsPointOnHandler = false;
            if (_mIRadius > 0)
            {
                var areaPath = new GraphicsPath();
                areaPath.AddArc(_mCenter.Box(_mIRadius + 5), 25, 40);

                var areaPen = new Pen(Color.Black, _mStyleHelper.LineSize + 10);
                areaPath.Widen(areaPen);
                areaPen.Dispose();
                bIsPointOnHandler = new Region(areaPath).IsVisible(point);
            }

            return bIsPointOnHandler;
        }

        #endregion Lower level helpers
    }
}