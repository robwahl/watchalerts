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

using Kinovea.ScreenManager.Languages;
using Kinovea.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace Kinovea.ScreenManager
{
    [XmlType("Line")]
    public class DrawingLine2D : AbstractDrawing, IKvaSerializable, IDecorable, IInitializable
    {
        #region IInitializable implementation

        public void ContinueSetup(Point point)
        {
            MoveHandle(point, 2);
        }

        #endregion IInitializable implementation

        public override string ToString()
        {
            return ScreenManagerLang.ToolTip_DrawingToolLine2D;
        }

        public override int GetHashCode()
        {
            // Combine all relevant fields with XOR to get the Hash.
            var iHash = MStartPoint.GetHashCode();
            iHash ^= MEndPoint.GetHashCode();
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
            get
            {
                // Rebuild the menu to get the localized text.
                var contextMenu = new List<ToolStripMenuItem>();

                _mnuShowMeasure.Text = ScreenManagerLang.mnuShowMeasure;
                _mnuShowMeasure.Checked = ShowMeasure;
                _mnuSealMeasure.Text = ScreenManagerLang.mnuSealMeasure;

                contextMenu.Add(_mnuShowMeasure);
                contextMenu.Add(_mnuSealMeasure);

                return contextMenu;
            }
        }

        public Metadata ParentMetadata
        {
            // get => unused.
            set { _mParentMetadata = value; }
        }

        public bool ShowMeasure { get; set; }

        #endregion Properties

        #region Members

        // Core
        public Point MStartPoint; // Public because also used for the Active Screen Bordering...

        public Point MEndPoint; // Idem.

        // Decoration
        private readonly StyleHelper _mStyleHelper = new StyleHelper();

        private readonly KeyframeLabel _mLabelMeasure;
        private Metadata _mParentMetadata;
        private InfosFading _mInfosFading;

        // Context menu
        private readonly ToolStripMenuItem _mnuShowMeasure = new ToolStripMenuItem();

        private readonly ToolStripMenuItem _mnuSealMeasure = new ToolStripMenuItem();

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructors

        public DrawingLine2D(Point start, Point end, long iTimestamp, long iAverageTimeStampsPerFrame,
            DrawingStyle preset)
        {
            MStartPoint = start;
            MEndPoint = end;
            _mLabelMeasure = new KeyframeLabel(GetMiddlePoint(), Color.Black);

            // Decoration
            _mStyleHelper.Color = Color.DarkSlateGray;
            _mStyleHelper.LineSize = 1;
            if (preset != null)
            {
                DrawingStyle = preset.Clone();
                BindStyle();
            }

            // Fading
            _mInfosFading = new InfosFading(iTimestamp, iAverageTimeStampsPerFrame);

            // Context menu
            _mnuShowMeasure.Click += mnuShowMeasure_Click;
            _mnuShowMeasure.Image = Properties.Drawings.measure;
            _mnuSealMeasure.Click += mnuSealMeasure_Click;
            _mnuSealMeasure.Image = Properties.Drawings.linecalibrate;
        }

        public DrawingLine2D(XmlReader xmlReader, PointF scale, Metadata parent)
            : this(Point.Empty, Point.Empty, 0, 0, ToolManager.Line.StylePreset.Clone())
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

            var start = transformer.Transform(MStartPoint);
            var end = transformer.Transform(MEndPoint);

            using (var penEdges = _mStyleHelper.GetPen((int)(fOpacityFactor * 255), transformer.Scale))
            {
                canvas.DrawLine(penEdges, start, end);

                // Handlers
                penEdges.Width = bSelected ? 2 : 1;
                if (_mStyleHelper.LineEnding.StartCap != LineCap.ArrowAnchor)
                    canvas.DrawEllipse(penEdges, start.Box(3));

                if (_mStyleHelper.LineEnding.EndCap != LineCap.ArrowAnchor)
                    canvas.DrawEllipse(penEdges, end.Box(3));
            }

            if (ShowMeasure)
            {
                // Text of the measure. (The helpers knows the unit)
                _mLabelMeasure.SetText(_mParentMetadata.CalibrationHelper.GetLengthText(MStartPoint, MEndPoint));
                _mLabelMeasure.Draw(canvas, transformer, fOpacityFactor);
            }
        }

        public override void MoveHandle(Point point, int iHandleNumber)
        {
            switch (iHandleNumber)
            {
                case 1:
                    MStartPoint = point;
                    _mLabelMeasure.SetAttach(GetMiddlePoint(), true);
                    break;

                case 2:
                    MEndPoint = point;
                    _mLabelMeasure.SetAttach(GetMiddlePoint(), true);
                    break;

                case 3:
                    // Move the center of the mini label to the mouse coord.
                    _mLabelMeasure.SetLabel(point);
                    break;
            }
        }

        public override void MoveDrawing(int deltaX, int deltaY, Keys modifierKeys)
        {
            MStartPoint.X += deltaX;
            MStartPoint.Y += deltaY;

            MEndPoint.X += deltaX;
            MEndPoint.Y += deltaY;

            _mLabelMeasure.SetAttach(GetMiddlePoint(), true);
        }

        public override int HitTest(Point point, long iCurrentTimestamp)
        {
            var iHitResult = -1;
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor > 0)
            {
                if (ShowMeasure && _mLabelMeasure.HitTest(point))
                    iHitResult = 3;
                else if (MStartPoint.Box(6).Contains(point))
                    iHitResult = 1;
                else if (MEndPoint.Box(6).Contains(point))
                    iHitResult = 2;
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
                    case "Start":
                        {
                            var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                            MStartPoint = new Point((int)(p.X * scale.X), (int)(p.Y * scale.Y));
                            break;
                        }
                    case "End":
                        {
                            var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                            MEndPoint = new Point((int)(p.X * scale.X), (int)(p.Y * scale.Y));
                            break;
                        }
                    case "DrawingStyle":
                        DrawingStyle = new DrawingStyle(xmlReader);
                        BindStyle();
                        break;

                    case "InfosFading":
                        _mInfosFading.ReadXml(xmlReader);
                        break;

                    case "MeasureVisible":
                        ShowMeasure = XmlHelper.ParseBoolean(xmlReader.ReadElementContentAsString());
                        break;

                    default:
                        var unparsed = xmlReader.ReadOuterXml();
                        Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }

            xmlReader.ReadEndElement();

            _mLabelMeasure.SetAttach(GetMiddlePoint(), true);
        }

        public void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("Start", string.Format("{0};{1}", MStartPoint.X, MStartPoint.Y));
            xmlWriter.WriteElementString("End", string.Format("{0};{1}", MEndPoint.X, MEndPoint.Y));
            xmlWriter.WriteElementString("MeasureVisible", ShowMeasure ? "true" : "false");

            xmlWriter.WriteStartElement("DrawingStyle");
            DrawingStyle.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("InfosFading");
            _mInfosFading.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();

            if (ShowMeasure)
            {
                // Spreadsheet support.
                xmlWriter.WriteStartElement("Measure");

                var len = _mParentMetadata.CalibrationHelper.GetLengthInUserUnit(MStartPoint, MEndPoint);
                var value = string.Format("{0:0.00}", len);
                var valueInvariant = string.Format(CultureInfo.InvariantCulture, "{0:0.00}", len);

                xmlWriter.WriteAttributeString("UserLength", value);
                xmlWriter.WriteAttributeString("UserLengthInvariant", valueInvariant);
                xmlWriter.WriteAttributeString("UserUnitLength",
                    _mParentMetadata.CalibrationHelper.GetLengthAbbreviation());

                xmlWriter.WriteEndElement();
            }
        }

        #endregion KVA Serialization

        #region Context menu

        private void mnuShowMeasure_Click(object sender, EventArgs e)
        {
            // Enable / disable the display of the measure for this line.
            ShowMeasure = !ShowMeasure;

            // Use this setting as the default value for new lines.
            DrawingToolLine2D.ShowMeasure = ShowMeasure;

            CallInvalidateFromMenu(sender);
        }

        private void mnuSealMeasure_Click(object sender, EventArgs e)
        {
            // display a dialog that let the user specify how many real-world-units long is this line.

            if (MStartPoint.X != MEndPoint.X || MStartPoint.Y != MEndPoint.Y)
            {
                if (!ShowMeasure)
                    ShowMeasure = true;

                DrawingToolLine2D.ShowMeasure = true;

                var dp = DelegatesPool.Instance();
                if (dp.DeactivateKeyboardHandler != null)
                    dp.DeactivateKeyboardHandler();

                var fcm = new FormConfigureMeasure(_mParentMetadata, this);
                ScreenManagerKernel.LocateForm(fcm);
                fcm.ShowDialog();
                fcm.Dispose();

                // Update traj for distance and speed after calibration.
                _mParentMetadata.UpdateTrajectoriesForKeyframes();

                CallInvalidateFromMenu(sender);

                if (dp.ActivateKeyboardHandler != null)
                    dp.ActivateKeyboardHandler();
            }
        }

        #endregion Context menu

        #region Lower level helpers

        private void BindStyle()
        {
            DrawingStyle.Bind(_mStyleHelper, "Color", "color");
            DrawingStyle.Bind(_mStyleHelper, "LineSize", "line size");
            DrawingStyle.Bind(_mStyleHelper, "LineEnding", "arrows");
        }

        private bool IsPointInObject(Point point)
        {
            // Create path which contains wide line for easy mouse selection
            var areaPath = new GraphicsPath();

            if (MStartPoint == MEndPoint)
                areaPath.AddLine(MStartPoint.X, MStartPoint.Y, MStartPoint.X + 2, MStartPoint.Y + 2);
            else
                areaPath.AddLine(MStartPoint, MEndPoint);

            var areaPen = new Pen(Color.Black, 7);
            areaPath.Widen(areaPen);
            areaPen.Dispose();
            var areaRegion = new Region(areaPath);
            return areaRegion.IsVisible(point);
        }

        private Point GetMiddlePoint()
        {
            return new Point((MStartPoint.X + MEndPoint.X) / 2, (MStartPoint.Y + MEndPoint.Y) / 2);
        }

        #endregion Lower level helpers
    }
}