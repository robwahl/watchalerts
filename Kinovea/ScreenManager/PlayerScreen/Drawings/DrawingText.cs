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
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace Kinovea.ScreenManager
{
    [XmlType("Label")]
    public class DrawingText : AbstractDrawing, IKvaSerializable, IDecorable
    {
        public override string ToString()
        {
            return ScreenManagerLang.ToolTip_DrawingToolText;
        }

        public override int GetHashCode()
        {
            var iHash = _mText.GetHashCode();
            iHash ^= _mBackground.Rectangle.Location.GetHashCode();
            iHash ^= _mStyleHelper.GetHashCode();
            return iHash;
        }

        public void SetEditMode(bool bEdit, CoordinateSystem transformer)
        {
            EditMode = bEdit;

            if (_mCoordinateSystem == null)
                _mCoordinateSystem = transformer;

            // Activate or deactivate the ScreenManager Keyboard Handler,
            // so we can use <space>, <return>, etc.
            var dp = DelegatesPool.Instance();
            if (EditMode)
            {
                if (dp.DeactivateKeyboardHandler != null)
                    dp.DeactivateKeyboardHandler();

                RelocateEditbox(); // This is needed because the container top-left corner may have changed
            }
            else
            {
                if (dp.ActivateKeyboardHandler != null)
                    dp.ActivateKeyboardHandler();
            }

            EditBox.Visible = EditMode;
        }

        public void RelocateEditbox()
        {
            if (_mCoordinateSystem != null)
            {
                var rect = _mCoordinateSystem.Transform(_mBackground.Rectangle);
                EditBox.Location = rect.Location.Translate(ContainerScreen.Left, ContainerScreen.Top);
            }
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

        public TextBox EditBox { get; set; }

        public PictureBox ContainerScreen { get; set; }

        public bool EditMode { get; private set; }

        #endregion Properties

        #region Members

        private string _mText;
        private readonly StyleHelper _mStyleHelper = new StyleHelper();
        private InfosFading _mInfosFading;
        private CoordinateSystem _mCoordinateSystem;

        private readonly RoundedRectangle _mBackground = new RoundedRectangle();

        private const int MIDefaultFontSize = 16; // will also be used for the text box.
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructors

        public DrawingText(Point p, long iTimestamp, long iAverageTimeStampsPerFrame, DrawingStyle preset)
        {
            _mText = " ";
            _mBackground.Rectangle = new Rectangle(p, Size.Empty);

            // Decoration & binding with editors
            _mStyleHelper.Bicolor = new Bicolor(Color.Black);
            _mStyleHelper.Font = new Font("Arial", MIDefaultFontSize, FontStyle.Bold);
            if (preset != null)
            {
                DrawingStyle = preset.Clone();
                BindStyle();
            }

            _mInfosFading = new InfosFading(iTimestamp, iAverageTimeStampsPerFrame);
            EditMode = false;

            EditBox = new TextBox
            {
                Visible = false,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Multiline = true,
                Text = _mText,
                Font = _mStyleHelper.GetFontDefaultSize(MIDefaultFontSize)
            };

            EditBox.TextChanged += TextBox_TextChanged;

            UpdateLabelRectangle();
        }

        public DrawingText(XmlReader xmlReader, PointF scale, Metadata parent)
            : this(Point.Empty, 0, 0, ToolManager.Label.StylePreset.Clone())
        {
            ReadXml(xmlReader, scale);
        }

        #endregion Constructors

        #region AbstractDrawing Implementation

        public override void Draw(Graphics canvas, CoordinateSystem transformer, bool bSelected, long iCurrentTimestamp)
        {
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor <= 0 || EditMode)
                return;

            using (var brushBack = _mStyleHelper.GetBackgroundBrush((int)(fOpacityFactor * 128)))
            using (var brushText = _mStyleHelper.GetForegroundBrush((int)(fOpacityFactor * 255)))
            using (var fontText = _mStyleHelper.GetFont((float)transformer.Scale))
            {
                var rect = transformer.Transform(_mBackground.Rectangle);
                RoundedRectangle.Draw(canvas, rect, brushBack, fontText.Height / 4, false);
                canvas.DrawString(_mText, fontText, brushText, rect.Location);
            }
        }

        public override int HitTest(Point point, long iCurrentTimestamp)
        {
            var iHitResult = -1;
            var fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            if (fOpacityFactor > 0)
            {
                iHitResult = _mBackground.HitTest(point, true);
            }

            return iHitResult;
        }

        public override void MoveHandle(Point point, int iHandleNumber)
        {
            // Invisible handler to change font size.
            var wantedHeight = point.Y - _mBackground.Rectangle.Location.Y;
            _mStyleHelper.ForceFontSize(wantedHeight, _mText);
            UpdateLabelRectangle();
        }

        public override void MoveDrawing(int deltaX, int deltaY, Keys modifierKeys)
        {
            _mBackground.Move(deltaX, deltaY);
            RelocateEditbox();
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
                    case "Text":
                        _mText = xmlReader.ReadElementContentAsString();
                        break;

                    case "Position":
                        var p = XmlHelper.ParsePoint(xmlReader.ReadElementContentAsString());
                        var location = new Point((int)(p.X * scale.X), (int)(p.Y * scale.Y));
                        _mBackground.Rectangle = new Rectangle(location, Size.Empty);
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
            xmlWriter.WriteElementString("Text", _mText);
            xmlWriter.WriteElementString("Position",
                string.Format("{0};{1}", _mBackground.Rectangle.X, _mBackground.Rectangle.Y));

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
            DrawingStyle.Bind(_mStyleHelper, "Bicolor", "back color");
            DrawingStyle.Bind(_mStyleHelper, "Font", "font size");
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            _mText = EditBox.Text;
            UpdateLabelRectangle();
        }

        private void UpdateLabelRectangle()
        {
            // Text changed or font size changed.
            using (var but = new Button())
            using (var g = but.CreateGraphics())
            using (var f = _mStyleHelper.GetFont(1F))
            {
                var textSize = g.MeasureString(_mText, f);
                _mBackground.Rectangle = new Rectangle(_mBackground.Rectangle.Location,
                    new Size((int)textSize.Width, (int)textSize.Height));

                // Also update the edit box size. (Use a fixed font though).
                // The extra space is to account for blank new lines.
                var boxSize = g.MeasureString(_mText + " ", EditBox.Font);
                EditBox.Size = new Size((int)boxSize.Width + 10, (int)boxSize.Height);
            }
        }

        #endregion Lower level helpers
    }
}