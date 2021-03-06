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
    [XmlType("Angle")]
    public class DrawingAngle2D : AbstractDrawing, IKvaSerializable, IDecorable, IInitializable
    {
        #region IInitializable implementation

        public void ContinueSetup(Point point)
        {
            MoveHandle(point, 2);
        }

        #endregion IInitializable implementation

        public override string ToString()
        {
            return ScreenManagerLang.ToolTip_DrawingToolAngle2D;
        }

        public override int GetHashCode()
        {
            var iHash = _mPointO.GetHashCode();
            iHash ^= _mPointA.GetHashCode();
            iHash ^= _mPointB.GetHashCode();
            iHash ^= _mStyleHelper.GetHashCode();
            return iHash;
        }

        #region Specific context menu

        private void mnuInvertAngle_Click(object sender, EventArgs e)
        {
            var temp = _mPointA;
            _mPointA = _mPointB;
            _mPointB = temp;
            ComputeValues();
            CallInvalidateFromMenu(sender);
        }

        #endregion Specific context menu

        #region Properties

        public DrawingStyle DrawingStyle { get; private set; }

        public override InfosFading InfosFading
        {
            get { return _mInfosFading; }
            set { _mInfosFading = value; }
        }

        public override DrawingCapabilities Caps
        {
            get { return DrawingCapabilities.ConfigureColor | DrawingCapabilities.Fading; }
        }

        public override List<ToolStripMenuItem> ContextMenu
        {
            get
            {
                // Rebuild the menu to get the localized text.
                var contextMenu = new List<ToolStripMenuItem>();

                _mMnuInvertAngle.Text = ScreenManagerLang.mnuInvertAngle;
                contextMenu.Add(_mMnuInvertAngle);

                return contextMenu;
            }
        }

        #endregion Properties

        #region Members

        // Core
        private Point _mPointO;

        private Point _mPointA;
        private Point _mPointB;

        // Precomputed
        private Rectangle _mBoundingBox;

        private float _mFStartAngle;
        private float _mFSweepAngle;
        private Point _mTextShift;

        // Decoration

        private readonly StyleHelper _mStyleHelper = new StyleHelper();
        private InfosFading _mInfosFading;

        // Context menu
        private readonly ToolStripMenuItem _mMnuInvertAngle = new ToolStripMenuItem();

        // Constants
        private const int MIDefaultBackgroundAlpha = 92;

        private const int MILabelDistance = 40;
        private const double MRadToDegrees = 180 / Math.PI;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor

        public DrawingAngle2D(Point o, Point a, Point b, long iTimestamp, long iAverageTimeStampsPerFrame,
            DrawingStyle stylePreset)
        {
            // Core
            _mPointO = o;
            _mPointA = a;
            _mPointB = b;
            ComputeValues();

            // Decoration and binding to mini editors.
            _mStyleHelper.Bicolor = new Bicolor(Color.Empty);
            _mStyleHelper.Font = new Font("Arial", 12, FontStyle.Bold);
            if (stylePreset != null)
            {
                DrawingStyle = stylePreset.Clone();
                BindStyle();
            }

            // Fading
            _mInfosFading = new InfosFading(iTimestamp, iAverageTimeStampsPerFrame);

            // Context menu
            _mMnuInvertAngle.Click += mnuInvertAngle_Click;
            _mMnuInvertAngle.Image = Properties.Drawings.angleinvert;
        }

        public DrawingAngle2D(XmlReader xmlReader, PointF scale, Metadata parent)
            : this(Point.Empty, Point.Empty, Point.Empty, 0, 0, ToolManager.Angle.StylePreset.Clone())
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

            var pointO = transformer.Transform(_mPointO);
            var pointA = transformer.Transform(_mPointA);
            var pointB = transformer.Transform(_mPointB);
            var boundingBox = transformer.Transform(_mBoundingBox);

            using (var penEdges = _mStyleHelper.GetBackgroundPen((int)(fOpacityFactor * 255)))
            using (var brushEdges = _mStyleHelper.GetBackgroundBrush((int)(fOpacityFactor * 255)))
            using (var brushFill = _mStyleHelper.GetBackgroundBrush((int)(fOpacityFactor * MIDefaultBackgroundAlpha)))
            {
                // Disk section
                canvas.FillPie(brushFill, boundingBox, _mFStartAngle, _mFSweepAngle);
                canvas.DrawPie(penEdges, boundingBox, _mFStartAngle, _mFSweepAngle);

                // Edges
                canvas.DrawLine(penEdges, pointO, pointA);
                canvas.DrawLine(penEdges, pointO, pointB);

                // Handlers
                canvas.DrawEllipse(penEdges, pointO.Box(3));
                canvas.FillEllipse(brushEdges, pointA.Box(3));
                canvas.FillEllipse(brushEdges, pointB.Box(3));

                var fontBrush = _mStyleHelper.GetForegroundBrush((int)(fOpacityFactor * 255));
                var angle = (int)Math.Floor(-_mFSweepAngle);
                var label = angle + "°";
                var tempFont = _mStyleHelper.GetFont((float)transformer.Scale);
                var labelSize = canvas.MeasureString(label, tempFont);

                // Background
                var shiftx = (float)(transformer.Scale * _mTextShift.X);
                var shifty = (float)(transformer.Scale * _mTextShift.Y);
                var textOrigin = new PointF(shiftx + pointO.X - labelSize.Width / 2,
                    shifty + pointO.Y - labelSize.Height / 2);
                var backRectangle = new RectangleF(textOrigin, labelSize);
                RoundedRectangle.Draw(canvas, backRectangle, brushFill, tempFont.Height / 4, false);

                // Text
                canvas.DrawString(label, tempFont, fontBrush, backRectangle.Location);

                tempFont.Dispose();
                fontBrush.Dispose();
            }
        }

        public override int HitTest(Point point, long iCurrentTimestamp)
        {
            // Convention: miss = -1, object = 0, handle = n.
            var iHitResult = -1;
            if (_mInfosFading.GetOpacityFactor(iCurrentTimestamp) > 0)
            {
                if (_mPointO.Box(10).Contains(point))
                    iHitResult = 1;
                else if (_mPointA.Box(10).Contains(point))
                    iHitResult = 2;
                else if (_mPointB.Box(10).Contains(point))
                    iHitResult = 3;
                else if (IsPointInObject(point))
                    iHitResult = 0;
            }

            return iHitResult;
        }

        public override void MoveHandle(Point point, int iHandleNumber)
        {
            switch (iHandleNumber)
            {
                case 1:
                    _mPointO = point;
                    break;

                case 2:
                    _mPointA = point;
                    break;

                case 3:
                    _mPointB = point;
                    break;

                default:
                    break;
            }

            ComputeValues();
        }

        public override void MoveDrawing(int deltaX, int deltaY, Keys modifierKeys)
        {
            _mPointO.X += deltaX;
            _mPointO.Y += deltaY;

            _mPointA.X += deltaX;
            _mPointA.Y += deltaY;

            _mPointB.X += deltaX;
            _mPointB.Y += deltaY;

            ComputeValues();
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
                    case "PointO":
                        _mPointO = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                        break;

                    case "PointA":
                        _mPointA = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                        break;

                    case "PointB":
                        _mPointB = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
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

            _mPointO = new Point((int)(_mPointO.X * scale.X), (int)(_mPointO.Y * scale.Y));
            _mPointA = new Point((int)(_mPointA.X * scale.X), (int)(_mPointA.Y * scale.Y));
            _mPointB = new Point((int)(_mPointB.X * scale.X), (int)(_mPointB.Y * scale.Y));

            ComputeValues();
        }

        public void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("PointO", string.Format("{0};{1}", _mPointO.X, _mPointO.Y));
            xmlWriter.WriteElementString("PointA", string.Format("{0};{1}", _mPointA.X, _mPointA.Y));
            xmlWriter.WriteElementString("PointB", string.Format("{0};{1}", _mPointB.X, _mPointB.Y));

            xmlWriter.WriteStartElement("DrawingStyle");
            DrawingStyle.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("InfosFading");
            _mInfosFading.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();

            // Spreadsheet support.
            xmlWriter.WriteStartElement("Measure");
            var angle = (int)Math.Floor(-_mFSweepAngle);
            xmlWriter.WriteAttributeString("UserAngle", angle.ToString());
            xmlWriter.WriteEndElement();
        }

        #endregion KVA Serialization

        #region Lower level helpers

        private void BindStyle()
        {
            DrawingStyle.Bind(_mStyleHelper, "Bicolor", "line color");
        }

        private void ComputeValues()
        {
            FixIfNull();
            ComputeAngles();
            ComputeBoundingBox();
            ComputeTextPosition();
        }

        private void FixIfNull()
        {
            if (_mPointA == _mPointO)
                _mPointA = new Point(_mPointO.X + 50, _mPointO.Y);

            if (_mPointB == _mPointO)
                _mPointB = new Point(_mPointO.X, _mPointO.Y - 50);
        }

        private void ComputeAngles()
        {
            var fOaRadians = Math.Atan((_mPointA.Y - _mPointO.Y) / (double)(_mPointA.X - _mPointO.X));
            var fObRadians = Math.Atan((_mPointB.Y - _mPointO.Y) / (double)(_mPointB.X - _mPointO.X));

            var fOaDegrees = fOaRadians * MRadToDegrees;
            if (_mPointA.X < _mPointO.X)
                fOaDegrees -= 180;

            var fObDegrees = fObRadians * MRadToDegrees;
            if (_mPointB.X < _mPointO.X)
                fObDegrees -= 180;

            _mFStartAngle = (float)fOaDegrees;
            _mFSweepAngle = (float)(fObDegrees - fOaDegrees);
            if (fObDegrees > fOaDegrees)
                _mFSweepAngle -= 360;
        }

        private void ComputeBoundingBox()
        {
            // Smallest segment gets to be the radius of the box.
            var oaLength =
                Math.Sqrt(((_mPointA.X - _mPointO.X) * (_mPointA.X - _mPointO.X)) +
                          ((_mPointA.Y - _mPointO.Y) * (_mPointA.Y - _mPointO.Y)));
            var obLength =
                Math.Sqrt(((_mPointB.X - _mPointO.X) * (_mPointB.X - _mPointO.X)) +
                          ((_mPointB.Y - _mPointO.Y) * (_mPointB.Y - _mPointO.Y)));
            var radius = (int)Math.Min(oaLength, obLength);
            if (radius > 20) radius -= 10;

            _mBoundingBox = new Rectangle(_mPointO.X - radius, _mPointO.Y - radius, radius * 2, radius * 2);
        }

        private void ComputeTextPosition()
        {
            var iBissect = _mFStartAngle + (_mFSweepAngle / 2);
            if (iBissect < 0)
                iBissect += 360;

            var fRadiansBissect = (Math.PI / 180) * iBissect;
            var fAdjacent = (int)(Math.Cos(fRadiansBissect) * MILabelDistance);
            var iOpposed = (int)(Math.Sin(fRadiansBissect) * MILabelDistance);

            _mTextShift = new Point(fAdjacent, iOpposed);
        }

        private bool IsPointInObject(Point point)
        {
            var bIsPointInObject = false;
            if (_mBoundingBox != Rectangle.Empty)
            {
                using (var gp = new GraphicsPath())
                {
                    gp.AddPie(_mBoundingBox, _mFStartAngle, _mFSweepAngle);
                    using (var r = new Region(gp))
                    {
                        bIsPointInObject = r.IsVisible(point);
                    }
                }
            }

            return bIsPointInObject;
        }

        #endregion Lower level helpers
    }
}