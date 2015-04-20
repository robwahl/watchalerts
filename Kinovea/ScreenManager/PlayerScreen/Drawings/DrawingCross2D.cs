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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace Kinovea.ScreenManager
{
    [XmlType("CrossMark")]
    public class DrawingCross2D : AbstractDrawing, IKvaSerializable, IDecorable
    {
        public override string ToString()
        {
            return ScreenManagerLang.ToolTip_DrawingToolCross2D;
        }

        public override int GetHashCode()
        {
            var iHash = _mCenter.GetHashCode();
            iHash ^= _mStyleHelper.GetHashCode();
            return iHash;
        }

        #region Context menu

        private void mnuShowCoordinates_Click(object sender, EventArgs e)
        {
            // Enable / disable the display of the coordinates for this cross marker.
            ShowCoordinates = !ShowCoordinates;

            // Use this setting as the default value for new lines.
            DrawingToolCross2D.ShowCoordinates = ShowCoordinates;

            CallInvalidateFromMenu(sender);
        }

        #endregion Context menu

        #region Lower level helpers

        private void BindStyle()
        {
            DrawingStyle.Bind(_mStyleHelper, "Color", "back color");
        }

        #endregion Lower level helpers

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

                _mnuShowCoordinates.Text = ScreenManagerLang.mnuShowCoordinates;
                _mnuShowCoordinates.Checked = ShowCoordinates;

                contextMenu.Add(_mnuShowCoordinates);

                return contextMenu;
            }
        }

        public bool ShowCoordinates { get; set; }

        public Metadata ParentMetadata
        {
            // get => unused.
            set { _mParentMetadata = value; }
        }

        // Next 2 props are accessed from Track creation.
        public Point Center
        {
            get { return _mCenter; }
        }

        public Color PenColor
        {
            get { return _mStyleHelper.Color; }
        }

        #endregion Properties

        #region Members

        // Core
        private Point _mCenter;

        private readonly KeyframeLabel _mLabelCoordinates;

        // Decoration
        private readonly StyleHelper _mStyleHelper = new StyleHelper();

        private InfosFading _mInfosFading;

        // Context menu
        private readonly ToolStripMenuItem _mnuShowCoordinates = new ToolStripMenuItem();

        private Metadata _mParentMetadata;
        private const int MIDefaultBackgroundAlpha = 64;
        private const int MIDefaultRadius = 3;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructors

        public DrawingCross2D(Point center, long iTimestamp, long iAverageTimeStampsPerFrame, DrawingStyle preset)
        {
            _mCenter = center;
            _mLabelCoordinates = new KeyframeLabel(_mCenter, Color.Black);

            // Decoration & binding with editors
            _mStyleHelper.Color = Color.CornflowerBlue;
            if (preset != null)
            {
                DrawingStyle = preset.Clone();
                BindStyle();
            }

            _mInfosFading = new InfosFading(iTimestamp, iAverageTimeStampsPerFrame);

            // Context menu
            _mnuShowCoordinates.Click += mnuShowCoordinates_Click;
            _mnuShowCoordinates.Image = Properties.Drawings.measure;
        }

        public DrawingCross2D(XmlReader xmlReader, PointF scale, Metadata parent)
            : this(Point.Empty, 0, 0, ToolManager.CrossMark.StylePreset.Clone())
        {
            ReadXml(xmlReader, scale);
            _mParentMetadata = parent;
        }

        #endregion Constructors

        #region AbstractDrawing Implementation

        public override void Draw(Graphics canvas, CoordinateSystem transformer, bool bSelected, long iCurrentTimestamp)
        {
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor <= 0)
                return;

            var iAlpha = (int)(fOpacityFactor * 255);
            var c = transformer.Transform(_mCenter);

            using (var p = _mStyleHelper.GetPen(iAlpha))
            using (var b = _mStyleHelper.GetBrush((int)(fOpacityFactor * MIDefaultBackgroundAlpha)))
            {
                canvas.DrawLine(p, c.X - MIDefaultRadius, c.Y, c.X + MIDefaultRadius, c.Y);
                canvas.DrawLine(p, c.X, c.Y - MIDefaultRadius, c.X, c.Y + MIDefaultRadius);
                canvas.FillEllipse(b, c.Box(MIDefaultRadius + 1));
            }

            if (ShowCoordinates)
            {
                _mLabelCoordinates.SetText(_mParentMetadata.CalibrationHelper.GetPointText(_mCenter, true));
                _mLabelCoordinates.Draw(canvas, transformer, fOpacityFactor);
            }
        }

        public override void MoveHandle(Point point, int iHandleNumber)
        {
            if (iHandleNumber == 1)
                _mLabelCoordinates.SetLabel(point);
        }

        public override void MoveDrawing(int deltaX, int deltaY, Keys modifierKeys)
        {
            _mCenter.X += deltaX;
            _mCenter.Y += deltaY;
            _mLabelCoordinates.SetAttach(_mCenter, true);
        }

        public override int HitTest(Point point, long iCurrentTimestamp)
        {
            // Convention: miss = -1, object = 0, handle = n.
            var iHitResult = -1;
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor > 0)
            {
                if (ShowCoordinates && _mLabelCoordinates.HitTest(point))
                    iHitResult = 1;
                else if (_mCenter.Box(MIDefaultRadius + 10).Contains(point))
                    iHitResult = 0;
            }

            return iHitResult;
        }

        #endregion AbstractDrawing Implementation

        #region Serialization

        private void ReadXml(XmlReader xmlReader, PointF scale)
        {
            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "CenterPoint":
                        var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                        _mCenter = new Point((int)(scale.X * p.X), (int)(scale.Y * p.Y));
                        break;

                    case "CoordinatesVisible":
                        ShowCoordinates = XmlHelper.ParseBoolean(xmlReader.ReadElementContentAsString());
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
            _mLabelCoordinates.SetAttach(_mCenter, true);
        }

        public void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("CenterPoint", string.Format("{0};{1}", _mCenter.X, _mCenter.Y));
            xmlWriter.WriteElementString("CoordinatesVisible", ShowCoordinates ? "true" : "false");

            xmlWriter.WriteStartElement("DrawingStyle");
            DrawingStyle.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("InfosFading");
            _mInfosFading.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();

            if (ShowCoordinates)
            {
                // Spreadsheet support.
                xmlWriter.WriteStartElement("Coordinates");

                var coords = _mParentMetadata.CalibrationHelper.GetPointInUserUnit(_mCenter);
                xmlWriter.WriteAttributeString("UserX", string.Format("{0:0.00}", coords.X));
                xmlWriter.WriteAttributeString("UserXInvariant",
                    string.Format(CultureInfo.InvariantCulture, "{0:0.00}", coords.X));
                xmlWriter.WriteAttributeString("UserY", string.Format("{0:0.00}", coords.Y));
                xmlWriter.WriteAttributeString("UserYInvariant",
                    string.Format(CultureInfo.InvariantCulture, "{0:0.00}", coords.Y));
                xmlWriter.WriteAttributeString("UserUnitLength",
                    _mParentMetadata.CalibrationHelper.GetLengthAbbreviation());

                xmlWriter.WriteEndElement();
            }
        }

        #endregion Serialization
    }
}