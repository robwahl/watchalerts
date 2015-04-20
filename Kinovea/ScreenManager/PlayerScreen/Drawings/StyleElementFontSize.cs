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
using System.Windows.Forms;
using System.Xml;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     Style element to represent the size of font used by the drawing.
    ///     Editor: regular combo box.
    /// </summary>
    public class StyleElementFontSize : AbstractStyleElement
    {
        #region Private Methods

        private void editor_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i;
            var parsed = int.TryParse(((ComboBox)sender).Text, out i);
            _mIFontSize = parsed ? i : MIDefaultFontSize;
            RaiseValueChanged();
            ((ComboBox)sender).Text = _mIFontSize.ToString();
        }

        #endregion Private Methods

        #region Properties

        public override object Value
        {
            get { return _mIFontSize; }
            set
            {
                _mIFontSize = (value is int) ? (int)value : MIDefaultFontSize;
                RaiseValueChanged();
            }
        }

        public override Bitmap Icon
        {
            get { return Properties.Drawings.editortext; }
        }

        public override string DisplayName
        {
            get { return ScreenManagerLang.Generic_FontSizePicker; }
        }

        public override string XmlName
        {
            get { return "FontSize"; }
        }

        #endregion Properties

        #region Members

        private int _mIFontSize;
        private static readonly int MIDefaultFontSize = 10;

        private static readonly string[] MOptions =
        {
            "8", "9", "10", "11", "12", "14", "16", "18", "20", "24", "28",
            "32", "36"
        };

        #endregion Members

        #region Constructor

        public StyleElementFontSize(int _default)
        {
            _mIFontSize = (Array.IndexOf(MOptions, _default.ToString()) >= 0) ? _default : MIDefaultFontSize;
        }

        public StyleElementFontSize(XmlReader xmlReader)
        {
            ReadXml(xmlReader);
        }

        #endregion Constructor

        #region Public Methods

        public override Control GetEditor()
        {
            var editor = new ComboBox();
            editor.DropDownStyle = ComboBoxStyle.DropDownList;
            editor.Items.AddRange(MOptions);
            editor.SelectedIndex = Array.IndexOf(MOptions, _mIFontSize.ToString());
            editor.SelectedIndexChanged += editor_SelectedIndexChanged;
            return editor;
        }

        public override AbstractStyleElement Clone()
        {
            AbstractStyleElement clone = new StyleElementFontSize(_mIFontSize);
            clone.Bind(this);
            return clone;
        }

        public override void ReadXml(XmlReader xmlReader)
        {
            xmlReader.ReadStartElement();
            var s = xmlReader.ReadElementContentAsString("Value", "");

            var value = MIDefaultFontSize;
            try
            {
                var intConverter = TypeDescriptor.GetConverter(typeof(int));
                value = (int)intConverter.ConvertFromString(s);
            }
            catch (Exception)
            {
                // The input XML couldn't be parsed. Keep the default value.
            }

            // Restrict to the actual list of "athorized" values.
            _mIFontSize = (Array.IndexOf(MOptions, value.ToString()) >= 0) ? value : MIDefaultFontSize;

            xmlReader.ReadEndElement();
        }

        public override void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("Value", _mIFontSize.ToString());
        }

        #endregion Public Methods
    }
}