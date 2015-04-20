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
    ///     Style element to represent line endings (for arrows).
    ///     Editor: owner drawn combo box.
    /// </summary>
    public class StyleElementLineEnding : AbstractStyleElement
    {
        #region Properties

        public override object Value
        {
            get { return _mLineEnding; }
            set
            {
                _mLineEnding = (value is LineEnding) ? (LineEnding)value : LineEnding.None;
                RaiseValueChanged();
            }
        }

        public override Bitmap Icon
        {
            get { return Properties.Drawings.arrows; }
        }

        public override string DisplayName
        {
            get { return ScreenManagerLang.Generic_ArrowPicker; }
        }

        public override string XmlName
        {
            get { return "Arrows"; }
        }

        #endregion Properties

        #region Members

        private LineEnding _mLineEnding;
        private static readonly int MILineWidth = 6;

        private static readonly LineEnding[] MOptions =
        {
            LineEnding.None, LineEnding.StartArrow, LineEnding.EndArrow,
            LineEnding.DoubleArrow
        };

        #endregion Members

        #region Constructor

        public StyleElementLineEnding(LineEnding _default)
        {
            _mLineEnding = (Array.IndexOf(MOptions, _default) >= 0) ? _default : LineEnding.None;
        }

        public StyleElementLineEnding(XmlReader xmlReader)
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
            editor.SelectedIndex = Array.IndexOf(MOptions, _mLineEnding);
            editor.DrawItem += editor_DrawItem;
            editor.SelectedIndexChanged += editor_SelectedIndexChanged;
            return editor;
        }

        public override AbstractStyleElement Clone()
        {
            AbstractStyleElement clone = new StyleElementLineEnding(_mLineEnding);
            clone.Bind(this);
            return clone;
        }

        public override void ReadXml(XmlReader xmlReader)
        {
            xmlReader.ReadStartElement();
            var s = xmlReader.ReadElementContentAsString("Value", "");

            var value = LineEnding.None;
            try
            {
                var lineEndingConverter = TypeDescriptor.GetConverter(typeof(LineEnding));
                value = (LineEnding)lineEndingConverter.ConvertFromString(s);
            }
            catch (Exception)
            {
                // The input XML couldn't be parsed. Keep the default value.
            }

            // Restrict to the actual list of "athorized" values.
            _mLineEnding = (Array.IndexOf(MOptions, value) >= 0) ? value : LineEnding.None;

            xmlReader.ReadEndElement();
        }

        public override void WriteXml(XmlWriter xmlWriter)
        {
            var converter = TypeDescriptor.GetConverter(_mLineEnding);
            var s = converter.ConvertToString(_mLineEnding);
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
                p.StartCap = MOptions[e.Index].StartCap;
                p.EndCap = MOptions[e.Index].EndCap;

                var top = e.Bounds.Height / 2;

                e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Top + top, e.Bounds.Left + e.Bounds.Width,
                    e.Bounds.Top + top);
                p.Dispose();
            }
        }

        private void editor_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = ((ComboBox)sender).SelectedIndex;
            if (index >= 0 && index < MOptions.Length)
            {
                _mLineEnding = MOptions[index];
                RaiseValueChanged();
            }
        }

        #endregion Private Methods
    }
}