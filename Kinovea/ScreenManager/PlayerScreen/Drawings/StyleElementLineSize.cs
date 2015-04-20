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
    ///     Style element to represent line width.
    ///     Editor: owner drawn combo box.
    ///     Very similar to StyleElementPenSize, just the rendering changes. (lines vs circles)
    /// </summary>
    public class StyleElementLineSize : AbstractStyleElement
    {
        #region Properties

        public override object Value
        {
            get { return _mIPenSize; }
            set
            {
                _mIPenSize = (value is int) ? (int)value : MIDefaultSize;
                RaiseValueChanged();
            }
        }

        public override Bitmap Icon
        {
            get { return Properties.Drawings.linesize; }
        }

        public override string DisplayName
        {
            get { return ScreenManagerLang.Generic_LineSizePicker; }
        }

        public override string XmlName
        {
            get { return "LineSize"; }
        }

        #endregion Properties

        #region Members

        private static readonly int[] MOptions = { 2, 3, 4, 5, 7, 9, 11, 13 };
        private static readonly int MIDefaultSize = 3;
        private int _mIPenSize;

        #endregion Members

        #region Constructor

        public StyleElementLineSize(int _default)
        {
            _mIPenSize = (Array.IndexOf(MOptions, _default) >= 0) ? _default : MIDefaultSize;
        }

        public StyleElementLineSize(XmlReader xmlReader)
        {
            ReadXml(xmlReader);
        }

        #endregion Constructor

        #region Public Methods

        public override Control GetEditor()
        {
            var editor = new ComboBox();
            editor.DropDownStyle = ComboBoxStyle.DropDownList;
            editor.ItemHeight = MOptions[MOptions.Length - 1] + 4;
            editor.DrawMode = DrawMode.OwnerDrawFixed;
            foreach (var i in MOptions) editor.Items.Add(new object());
            editor.SelectedIndex = Array.IndexOf(MOptions, _mIPenSize);
            editor.DrawItem += editor_DrawItem;
            editor.SelectedIndexChanged += editor_SelectedIndexChanged;
            return editor;
        }

        public override AbstractStyleElement Clone()
        {
            AbstractStyleElement clone = new StyleElementLineSize(_mIPenSize);
            clone.Bind(this);
            return clone;
        }

        public override void ReadXml(XmlReader xmlReader)
        {
            xmlReader.ReadStartElement();
            var s = xmlReader.ReadElementContentAsString("Value", "");

            var value = MIDefaultSize;
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
            _mIPenSize = (Array.IndexOf(MOptions, value) >= 0) ? value : MIDefaultSize;

            xmlReader.ReadEndElement();
        }

        public override void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("Value", _mIPenSize.ToString());
        }

        #endregion Public Methods

        #region Private Methods

        private void editor_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index >= 0 && e.Index < MOptions.Length)
            {
                var itemPenSize = MOptions[e.Index];
                var top = (e.Bounds.Height - itemPenSize) / 2;
                e.Graphics.FillRectangle(Brushes.Black, e.Bounds.Left, e.Bounds.Top + top, e.Bounds.Width, itemPenSize);
            }
        }

        private void editor_SelectedIndexChanged(object sender, EventArgs e)
        {
            var index = ((ComboBox)sender).SelectedIndex;
            if (index >= 0 && index < MOptions.Length)
            {
                _mIPenSize = MOptions[index];
                RaiseValueChanged();
            }
        }

        #endregion Private Methods
    }
}