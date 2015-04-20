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
    [XmlType("Plane")]
    public class DrawingPlane : AbstractDrawing, IDecorable, IKvaSerializable
    {
        public void SetLocations(Size imageSize, double fStretchFactor, Point directZoomTopLeft)
        {
            // Initialize corners positions
            if (!_mBInitialized)
            {
                _mBInitialized = true;

                var horzTenth = (int)(((double)imageSize.Width) / 10);
                var vertTenth = (int)(((double)imageSize.Height) / 10);

                if (_mBSupport3D)
                {
                    // Initialize with a faked perspective.
                    _mCorners.A = new Point(3 * horzTenth, 4 * vertTenth);
                    _mCorners.B = new Point(7 * horzTenth, 4 * vertTenth);
                    _mCorners.C = new Point(9 * horzTenth, 8 * vertTenth);
                    _mCorners.D = new Point(1 * horzTenth, 8 * vertTenth);
                }
                else
                {
                    // initialize with a rectangle.
                    _mCorners.A = new Point(2 * horzTenth, 2 * vertTenth);
                    _mCorners.B = new Point(8 * horzTenth, 2 * vertTenth);
                    _mCorners.C = new Point(8 * horzTenth, 8 * vertTenth);
                    _mCorners.D = new Point(2 * horzTenth, 8 * vertTenth);
                }
            }
            RedefineHomography();
            _mFShift = 0.0F;
        }

        public void Reset()
        {
            // Used on metadata over load.
            Divisions = MIDefaultDivisions;
            _mFShift = 0.0F;
            _mBValidPlane = true;
            _mBInitialized = false;
            _mCorners = Quadrilateral.UnitRectangle;
        }

        #region Properties

        public DrawingStyle DrawingStyle { get; private set; }

        public override InfosFading InfosFading
        {
            get { return _mInfosFading; }
            set { _mInfosFading = value; }
        }

        public override List<ToolStripMenuItem> ContextMenu
        {
            get { return null; }
        }

        public override DrawingCapabilities Caps
        {
            get { return DrawingCapabilities.ConfigureColor | DrawingCapabilities.Fading; }
        }

        public int Divisions { get; set; }

        #endregion Properties

        #region Members

        private Quadrilateral _mCorners = Quadrilateral.UnitRectangle;
        private Quadrilateral _mRefPlane = Quadrilateral.UnitRectangle;

        private bool _mBSupport3D;

        private InfosFading _mInfosFading;
        private readonly StyleHelper _mStyleHelper = new StyleHelper();
        private Pen _mPenEdges = Pens.White;

        private bool _mBInitialized;
        private bool _mBValidPlane = true;
        private float _mFShift; // used only for expand/retract, to stay relative to the original mapping.

        private const int MIMinimumDivisions = 2;
        private const int MIDefaultDivisions = 8;
        private const int MIMaximumDivisions = 20;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor

        public DrawingPlane(int divisions, bool support3D, long iTimestamp, long iAverageTimeStampsPerFrame,
            DrawingStyle preset)
        {
            Divisions = divisions == 0 ? MIDefaultDivisions : divisions;
            _mBSupport3D = support3D;

            // Decoration
            _mStyleHelper.Color = Color.Empty;
            if (preset != null)
            {
                DrawingStyle = preset.Clone();
                BindStyle();
            }

            _mInfosFading = new InfosFading(iTimestamp, iAverageTimeStampsPerFrame);
            _mInfosFading.UseDefault = false;
            _mInfosFading.AlwaysVisible = true;

            RedefineHomography();
        }

        public DrawingPlane(XmlReader xmlReader, PointF scale, Metadata parent)
            : this(MIDefaultDivisions, false, 0, 0, ToolManager.Grid.StylePreset.Clone())
        {
            ReadXml(xmlReader, scale);
        }

        #endregion Constructor

        #region AbstractDrawing implementation

        public override void Draw(Graphics canvas, CoordinateSystem transformer, bool bSelected, long iCurrentTimestamp)
        {
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor <= 0)
                return;

            var quad = transformer.Transform(_mCorners);

            using (_mPenEdges = _mStyleHelper.GetPen(fOpacityFactor, 1.0))
            using (var br = _mStyleHelper.GetBrush(fOpacityFactor))
            {
                // Handlers
                foreach (Point p in quad)
                    canvas.FillEllipse(br, p.Box(4));

                // Grid
                if (_mBValidPlane)
                {
                    var homography = GetHomographyMatrix(quad.ToArray());

                    // Rows
                    for (var iRow = 0; iRow <= Divisions; iRow++)
                    {
                        var v = (float)iRow / Divisions;
                        var h1 = ProjectiveMapping(new PointF(0, v), homography);
                        var h2 = ProjectiveMapping(new PointF(1, v), homography);
                        canvas.DrawLine(_mPenEdges, h1, h2);
                    }

                    // Columns
                    for (var iCol = 0; iCol <= Divisions; iCol++)
                    {
                        var h = (float)iCol / Divisions;
                        var h1 = ProjectiveMapping(new PointF(h, 0), homography);
                        var h2 = ProjectiveMapping(new PointF(h, 1), homography);
                        canvas.DrawLine(_mPenEdges, h1, h2);
                    }
                }
                else
                {
                    // Non convex quadrilateral: only draw the borders
                    canvas.DrawLine(_mPenEdges, quad.A, quad.B);
                    canvas.DrawLine(_mPenEdges, quad.B, quad.C);
                    canvas.DrawLine(_mPenEdges, quad.C, quad.D);
                    canvas.DrawLine(_mPenEdges, quad.D, quad.A);
                }
            }
        }

        public override int HitTest(Point point, long iCurrentTimestamp)
        {
            var iHitResult = -1;
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);

            if (fOpacityFactor > 0)
            {
                for (var i = 0; i < 4; i++)
                {
                    if (_mCorners[i].Box(6).Contains(point))
                        iHitResult = i + 1;
                }

                if (iHitResult == -1 && _mCorners.Contains(point))
                    iHitResult = 0;
            }

            return iHitResult;
        }

        public override void MoveDrawing(int deltaX, int deltaY, Keys modifierKeys)
        {
            if ((modifierKeys & Keys.Alt) == Keys.Alt)
            {
                // Just change the number of divisions.
                Divisions = Divisions + ((deltaX - deltaY) / 4);
                Divisions = Math.Min(Math.Max(Divisions, MIMinimumDivisions), MIMaximumDivisions);
            }
            else if ((modifierKeys & Keys.Control) == Keys.Control)
            {
                // Expand the grid while staying on the same plane.
                var offset = deltaX;

                if (_mBSupport3D)
                {
                    if (_mBValidPlane)
                    {
                        // find new corners by growing the current homography.
                        var homography = GetHomographyMatrix(_mRefPlane.ToArray());
                        var fShift = _mFShift + ((float)(deltaX - deltaY) / 200);

                        var shiftedCorners = new PointF[4];
                        shiftedCorners[0] = ProjectiveMapping(new PointF(-fShift, -fShift), homography);
                        shiftedCorners[1] = ProjectiveMapping(new PointF(1 + fShift, -fShift), homography);
                        shiftedCorners[2] = ProjectiveMapping(new PointF(1 + fShift, 1 + fShift), homography);
                        shiftedCorners[3] = ProjectiveMapping(new PointF(-fShift, 1 + fShift), homography);

                        try
                        {
                            var expanded = new Quadrilateral
                            {
                                A = new Point((int)shiftedCorners[0].X, (int)shiftedCorners[0].Y),
                                B = new Point((int)shiftedCorners[1].X, (int)shiftedCorners[1].Y),
                                C = new Point((int)shiftedCorners[2].X, (int)shiftedCorners[2].Y),
                                D = new Point((int)shiftedCorners[3].X, (int)shiftedCorners[3].Y)
                            };

                            _mFShift = fShift;
                            _mCorners = expanded.Clone();
                        }
                        catch (OverflowException)
                        {
                            Log.Debug("Overflow during grid expansion");
                        }
                    }
                }
                else
                {
                    var fGrowFactor = 1 + ((float)offset / 100); // for offset [-10;+10] => Growth [0.9;1.1]
                    var width = _mCorners.B.X - _mCorners.A.X;
                    var height = _mCorners.D.Y - _mCorners.A.Y;

                    var fNewWidth = fGrowFactor * width;
                    var fNewHeight = fGrowFactor * height;

                    var shiftx = (int)((fNewWidth - width) / 2);
                    var shifty = (int)((fNewHeight - height) / 2);

                    _mCorners.Expand(shiftx, shifty);
                }
            }
            else
            {
                _mCorners.Translate(deltaX, deltaY);
                RedefineHomography();
                _mFShift = 0F;
            }
        }

        public override void MoveHandle(Point point, int iHandleNumber)
        {
            _mCorners[iHandleNumber - 1] = point;

            if (_mBSupport3D)
                _mBValidPlane = _mCorners.IsConvex;
            else
                _mCorners.MakeRectangle(iHandleNumber - 1);

            RedefineHomography();
            _mFShift = 0F;
        }

        #endregion AbstractDrawing implementation

        #region KVA Serialization

        private void ReadXml(XmlReader xmlReader, PointF scale)
        {
            xmlReader.ReadStartElement();

            Reset();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "PointUpperLeft":
                        {
                            var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                            _mCorners.A = new Point((int)(p.X * scale.X), (int)(p.Y * scale.Y));
                            break;
                        }
                    case "PointUpperRight":
                        {
                            var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                            _mCorners.B = new Point((int)(p.X * scale.X), (int)(p.Y * scale.Y));
                            break;
                        }
                    case "PointLowerRight":
                        {
                            var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                            _mCorners.C = new Point((int)(p.X * scale.X), (int)(p.Y * scale.Y));
                            break;
                        }
                    case "PointLowerLeft":
                        {
                            var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                            _mCorners.D = new Point((int)(p.X * scale.X), (int)(p.Y * scale.Y));
                            break;
                        }
                    case "Divisions":
                        Divisions = xmlReader.ReadElementContentAsInt();
                        break;

                    case "Perspective":
                        _mBSupport3D = XmlHelper.ParseBoolean(xmlReader.ReadElementContentAsString());
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

            // Sanity check for rectangular constraint.
            if (!_mBSupport3D && !_mCorners.IsRectangle)
                _mBSupport3D = true;

            RedefineHomography();
        }

        public void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("PointUpperLeft", string.Format("{0};{1}", _mCorners.A.X, _mCorners.A.Y));
            xmlWriter.WriteElementString("PointUpperRight", string.Format("{0};{1}", _mCorners.B.X, _mCorners.B.Y));
            xmlWriter.WriteElementString("PointLowerRight", string.Format("{0};{1}", _mCorners.C.X, _mCorners.C.Y));
            xmlWriter.WriteElementString("PointLowerLeft", string.Format("{0};{1}", _mCorners.D.X, _mCorners.D.Y));

            xmlWriter.WriteElementString("Divisions", Divisions.ToString());
            xmlWriter.WriteElementString("Perspective", _mBSupport3D ? "true" : "false");

            xmlWriter.WriteStartElement("DrawingStyle");
            DrawingStyle.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("InfosFading");
            _mInfosFading.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();
        }

        #endregion KVA Serialization

        #region Private methods

        private void BindStyle()
        {
            DrawingStyle.Bind(_mStyleHelper, "Color", "color");
        }

        private void RedefineHomography()
        {
            _mRefPlane = _mCorners.Clone();
        }

        private float[] GetHomographyMatrix(Point[] sourceCoords)
        {
            var homography = new float[18];

            float sx = (sourceCoords[0].X - sourceCoords[1].X) + (sourceCoords[2].X - sourceCoords[3].X);
            float sy = (sourceCoords[0].Y - sourceCoords[1].Y) + (sourceCoords[2].Y - sourceCoords[3].Y);
            float dx1 = sourceCoords[1].X - sourceCoords[2].X;
            float dx2 = sourceCoords[3].X - sourceCoords[2].X;
            float dy1 = sourceCoords[1].Y - sourceCoords[2].Y;
            float dy2 = sourceCoords[3].Y - sourceCoords[2].Y;

            var z = (dx1 * dy2) - (dy1 * dx2);
            var g = ((sx * dy2) - (sy * dx2)) / z;
            var h = ((sy * dx1) - (sx * dy1)) / z;

            // Transformation matrix. From the square to the quadrilateral.
            var a = homography[0] = sourceCoords[1].X - sourceCoords[0].X + g * sourceCoords[1].X;
            var b = homography[1] = sourceCoords[3].X - sourceCoords[0].X + h * sourceCoords[3].X;
            var c = homography[2] = sourceCoords[0].X;
            var d = homography[3] = sourceCoords[1].Y - sourceCoords[0].Y + g * sourceCoords[1].Y;
            var e = homography[4] = sourceCoords[3].Y - sourceCoords[0].Y + h * sourceCoords[3].Y;
            var f = homography[5] = sourceCoords[0].Y;
            homography[6] = g;
            homography[7] = h;
            homography[8] = 1;

            // Inverse Transformation Matrix. From the quadrilateral to our square.
            homography[9] = e - f * h;
            homography[10] = c * h - b;
            homography[11] = b * f - c * e;
            homography[12] = f * g - d;
            homography[13] = a - c * g;
            homography[14] = c * d - a * f;
            homography[15] = d * h - e * g;
            homography[16] = b * g - a * h;
            homography[17] = a * e - b * d;

            return homography;
        }

        private PointF ProjectiveMapping(PointF sourcePoint, float[] homography)
        {
            double x = (homography[0] * sourcePoint.X + homography[1] * sourcePoint.Y + homography[2]) /
                       (homography[6] * sourcePoint.X + homography[7] * sourcePoint.Y + 1);
            double y = (homography[3] * sourcePoint.X + homography[4] * sourcePoint.Y + homography[5]) /
                       (homography[6] * sourcePoint.X + homography[7] * sourcePoint.Y + 1);

            return new PointF((float)x, (float)y);
        }

        private PointF InverseProjectiveMapping(PointF sourcePoint, float[] homography)
        {
            double x = (homography[9] * sourcePoint.X + homography[10] * sourcePoint.Y + homography[11]) /
                       (homography[15] * sourcePoint.X + homography[16] * sourcePoint.Y + 1);
            double y = (homography[12] * sourcePoint.X + homography[13] * sourcePoint.Y + homography[14]) /
                       (homography[15] * sourcePoint.X + homography[16] * sourcePoint.Y + 1);
            double z = (homography[18] * sourcePoint.X + homography[16] * sourcePoint.Y + homography[17]) /
                       (homography[15] * sourcePoint.X + homography[16] * sourcePoint.Y + 1);

            return new PointF((float)x / (float)z, (float)y / (float)z);
        }

        #endregion Private methods
    }
}