#region License

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

#endregion License

using Kinovea.ScreenManager.Languages;
using Kinovea.Services;
using log4net;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace Kinovea.ScreenManager
{
    [XmlType("Pencil")]
    public class DrawingPencil : AbstractDrawing, IKvaSerializable, IDecorable, IInitializable
    {
        #region IInitializable implementation

        public void ContinueSetup(Point point)
        {
            AddPoint(point);
        }

        #endregion IInitializable implementation

        public override string ToString()
        {
            return ScreenManagerLang.ToolTip_DrawingToolPencil;
        }

        public override int GetHashCode()
        {
            var iHashCode = 0;
            foreach (var p in _mPointList)
                iHashCode ^= p.GetHashCode();

            iHashCode ^= _mStyleHelper.GetHashCode();

            return iHashCode;
        }

        public void AddPoint(Point coordinates)
        {
            _mPointList.Add(coordinates);
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

        private List<Point> _mPointList = new List<Point>();
        private readonly StyleHelper _mStyleHelper = new StyleHelper();
        private InfosFading _mInfosFading;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructors

        public DrawingPencil(Point origin, Point second, long iTimestamp, long averageTimeStampsPerFrame,
            DrawingStyle preset)
        {
            _mPointList.Add(origin);
            _mPointList.Add(second);
            _mInfosFading = new InfosFading(iTimestamp, averageTimeStampsPerFrame);

            _mStyleHelper.Color = Color.Black;
            _mStyleHelper.LineSize = 1;
            if (preset != null)
            {
                DrawingStyle = preset.Clone();
                BindStyle();
            }
        }

        public DrawingPencil(XmlReader xmlReader, PointF scale, Metadata parent)
            : this(Point.Empty, Point.Empty, 0, 0, ToolManager.Pencil.StylePreset.Clone())
        {
            ReadXml(xmlReader, scale);
        }

        #endregion Constructors

        #region AbstractDrawing Implementation

        public override void Draw(Graphics canvas, CoordinateSystem transformer, bool bSelected, long iCurrentTimestamp)
        {
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor <= 0)
                return;

            using (var penLine = _mStyleHelper.GetPen(fOpacityFactor, transformer.Scale))
            {
                var points = _mPointList.Select(p => transformer.Transform(p)).ToArray();
                canvas.DrawCurve(penLine, points, 0.5f);
            }
        }

        public override void MoveHandle(Point point, int iHandleNumber)
        {
        }

        public override void MoveDrawing(int deltaX, int deltaY, Keys modifierKeys)
        {
            _mPointList = _mPointList.Select(p => p.Translate(deltaX, deltaY)).ToList();
        }

        public override int HitTest(Point point, long iCurrentTimestamp)
        {
            var iHitResult = -1;
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor > 0 && IsPointInObject(point))
                iHitResult = 0;

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
                    case "PointList":
                        ParsePointList(xmlReader, scale);
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

        private void ParsePointList(XmlReader xmlReader, PointF scale)
        {
            _mPointList.Clear();

            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                if (xmlReader.Name == "Point")
                {
                    var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                    var adapted = new Point((int)(p.X * scale.X), (int)(p.Y * scale.Y));
                    _mPointList.Add(adapted);
                }
                else
                {
                    var unparsed = xmlReader.ReadOuterXml();
                    Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                }
            }

            xmlReader.ReadEndElement();
        }

        public void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("PointList");
            xmlWriter.WriteAttributeString("Count", _mPointList.Count.ToString());
            foreach (var p in _mPointList)
                xmlWriter.WriteElementString("Point", string.Format("{0};{1}", p.X, p.Y));

            xmlWriter.WriteEndElement();

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
            // Create path which contains wide line for easy mouse selection
            var areaPath = new GraphicsPath();
            areaPath.AddCurve(_mPointList.ToArray(), 0.5f);

            var bounds = areaPath.GetBounds();
            if (!bounds.IsEmpty)
            {
                var areaPen = new Pen(Color.Black, _mStyleHelper.LineSize + 7);
                areaPen.StartCap = LineCap.Round;
                areaPen.EndCap = LineCap.Round;
                areaPen.LineJoin = LineJoin.Round;
                areaPath.Widen(areaPen);
                areaPen.Dispose();
                var areaRegion = new Region(areaPath);
                return areaRegion.IsVisible(point);
            }
            return false;
        }

        #endregion Lower level helpers
    }
}