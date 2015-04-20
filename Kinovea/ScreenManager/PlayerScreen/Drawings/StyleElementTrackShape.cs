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

using Kinovea.ScreenManager.Languages;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Xml;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     Style element to represent track line shape.
    ///     Editor: owner drawn combo box.
    /// </summary>
    public class StyleElementTrackShape : AbstractStyleElement
    {
        #region Properties

        public override object Value
        {
            get { return _mTrackShape; }
            set
            {
                _mTrackShape = (value is TrackShape) ? (TrackShape)value : TrackShape.Solid;
                RaiseValueChanged();
            }
        }

        public override Bitmap Icon
        {
            get { return Properties.Drawings.trackshape; }
        }

        public override string DisplayName
        {
            get { return ScreenManagerLang.Generic_TrackShapePicker; }
        }

        public override string XmlName
        {
            get { return "TrackShape"; }
        }

        #endregion Properties

        #region Members

        private TrackShape _mTrackShape;
        private static readonly int MILineWidth = 3;

        private static readonly TrackShape[] MOptions =
        {
            TrackShape.Solid, TrackShape.Dash, TrackShape.SolidSteps,
            TrackShape.DashSteps
        };

        #endregion Members

        #region Constructor

        public StyleElementTrackShape(TrackShape _default)
        {
            _mTrackShape = (Array.IndexOf(MOptions, _default) >= 0) ? _default : TrackShape.Solid;
        }

        public StyleElementTrackShape(XmlReader xmlReader)
        {
            ReadXml(xmlReader);
        }

        #endregion Constructor

        #region Public Methods

        public override Control GetEditor()
        {
            var editor = new ComboBox();
            editor.DropDownStyle = ComboBoxStyle.DropDownList;
            editor.ItemHeight = 15;
            editor.DrawMode = DrawMode.OwnerDrawFixed;
            for (var i = 0; i < MOptions.Length; i++) editor.Items.Add(new object());
            editor.SelectedIndex = Array.IndexOf(MOptions, _mTrackShape);
            editor.DrawItem += editor_DrawItem;
            editor.SelectedIndexChanged += editor_SelectedIndexChanged;
            return editor;
        }

        public override AbstractStyleElement Clone()
        {
            AbstractStyleElement clone = new StyleElementTrackShape(_mTrackShape);
            clone.Bind(this);
            return clone;
        }

        public override void ReadXml(XmlReader xmlReader)
        {
            xmlReader.ReadStartElement();
            var s = xmlReader.ReadElementContentAsString("Value", "");

            var value = TrackShape.Solid;
            try
            {
                var trackShapeConverter = TypeDescriptor.GetConverter(typeof(TrackShape));
                value = (TrackShape)trackShapeConverter.ConvertFromString(s);
            }
            catch (Exception)
            {
                // The input XML couldn't be parsed. Keep the default value.
            }

            // Restrict to the actual list of "athorized" values.
            _mTrackShape = (Array.IndexOf(MOptions, value) >= 0) ? value : TrackShape.Solid;

            xmlReader.ReadEndElement();
        }

        public override void WriteXml(XmlWriter xmlWriter)
        {
            var converter = TypeDescriptor.GetConverter(_mTrackShape);
            var s = converter.ConvertToString(_mTrackShape);
            xmlWriter.WriteElementString("Value", s);
        }

        #endregion Public Methods

        #region Private Methods

        private void editor_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= 0 && e.Index < MOptions.Length)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var p = new Pen(Color.Black, MILineWidth);
                p.DashStyle = MOptions[e.Index].DashStyle;

                var top = e.Bounds.Height / 2;

                e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Top + top, e.Bounds.Left + e.Bounds.Width,
                    e.Bounds.Top + top);

                if (MOptions[e.Index].ShowSteps)
                {
                    var stepPen = new Pen(Color.Black, 2);
                    var margin = (int)(MILineWidth * 1.5);
                    var diameter = margin * 2;
                    var left = e.Bounds.Width / 2;
                    e.Graphics.DrawEllipse(stepPen, e.Bounds.Left + left - margin, e.Bounds.Top + top - margin, diameter,
                        diameter);
                    stepPen.Dispose();
                }

                p.Dispose();
            }
        }

        private void editor_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = ((ComboBox)sender).SelectedIndex;
            if (index >= 0 && index < MOptions.Length)
            {
                _mTrackShape = MOptions[index];
                RaiseValueChanged();
            }
        }

        #endregion Private Methods
    }
}