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

using log4net;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     Represents the styling elements of a drawing or drawing tool preset.
    ///     Host a list of style elements needed to decorate the drawing.
    /// </summary>
    public class DrawingStyle
    {
        #region Properties

        public Dictionary<string, AbstractStyleElement> Elements { get; } =

            new Dictionary<string, AbstractStyleElement>();

        #endregion Properties

        #region Members

        private readonly Dictionary<string, AbstractStyleElement> _mMemo =
            new Dictionary<string, AbstractStyleElement>();

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor

        public DrawingStyle()
        {
        }

        public DrawingStyle(XmlReader xmlReader)
        {
            ReadXml(xmlReader);
        }

        #endregion Constructor

        #region Public Methods

        public DrawingStyle Clone()
        {
            var clone = new DrawingStyle();
            foreach (var element in Elements)
            {
                clone.Elements.Add(element.Key, element.Value.Clone());
            }
            return clone;
        }

        public void ReadXml(XmlReader xmlReader)
        {
            Elements.Clear();

            xmlReader.ReadStartElement(); // <ToolPreset Key="ToolName"> or <DrawingStyle>
            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                AbstractStyleElement styleElement = null;
                var key = xmlReader.GetAttribute("Key");

                switch (xmlReader.Name)
                {
                    case "Color":
                        styleElement = new StyleElementColor(xmlReader);
                        break;

                    case "FontSize":
                        styleElement = new StyleElementFontSize(xmlReader);
                        break;

                    case "PenSize":
                        styleElement = new StyleElementPenSize(xmlReader);
                        break;

                    case "LineSize":
                        styleElement = new StyleElementLineSize(xmlReader);
                        break;

                    case "Arrows":
                        styleElement = new StyleElementLineEnding(xmlReader);
                        break;

                    case "TrackShape":
                        styleElement = new StyleElementTrackShape(xmlReader);
                        break;

                    default:
                        Log.ErrorFormat("Could not import style element \"{0}\"", xmlReader.Name);
                        Log.ErrorFormat("Content was: {0}", xmlReader.ReadOuterXml());
                        break;
                }

                if (styleElement != null)
                {
                    Elements.Add(key, styleElement);
                }
            }

            xmlReader.ReadEndElement();
        }

        public void WriteXml(XmlWriter xmlWriter)
        {
            foreach (var element in Elements)
            {
                xmlWriter.WriteStartElement(element.Value.XmlName);
                xmlWriter.WriteAttributeString("Key", element.Key);
                element.Value.WriteXml(xmlWriter);
                xmlWriter.WriteEndElement();
            }
        }

        /// <summary>
        ///     Binds a property in the style helper to an editable style element.
        ///     Once bound, each time the element is edited in the UI, the property is updated,
        ///     so the actual drawing automatically changes its style.
        ///     Style elements and properties need not be of the same type. The style helper knows how to
        ///     map a FontSize element to its own Font property for example.
        /// </summary>
        /// <param name="target">The drawing's style helper object</param>
        /// <param name="targetProperty">The name of the property in the style helper that needs automatic update</param>
        /// <param name="source">The style element that will push its change to the property</param>
        public void Bind(StyleHelper target, string targetProperty, string source)
        {
            AbstractStyleElement elem;
            var found = Elements.TryGetValue(source, out elem);
            if (found && elem != null)
            {
                elem.Bind(target, targetProperty);
            }
            else
            {
                Log.ErrorFormat("The element \"{0}\" was not found.", source);
            }
        }

        public void RaiseValueChanged()
        {
            foreach (var element in Elements)
            {
                element.Value.RaiseValueChanged();
            }
        }

        public void ReadValue()
        {
            foreach (var element in Elements)
            {
                element.Value.ReadValue();
            }
        }

        public void Memorize()
        {
            _mMemo.Clear();
            foreach (var element in Elements)
            {
                _mMemo.Add(element.Key, element.Value.Clone());
            }
        }

        public void Memorize(DrawingStyle memo)
        {
            // This is used when the whole DrawingStyle has been recreated and we want it to
            // remember its state before the recreation.
            // Used for style presets to carry the memo after XML load.
            _mMemo.Clear();
            foreach (var element in memo.Elements)
            {
                _mMemo.Add(element.Key, element.Value.Clone());
            }
        }

        public void Revert()
        {
            Elements.Clear();
            foreach (var element in _mMemo)
            {
                Elements.Add(element.Key, element.Value.Clone());
            }
        }

        public void Dump()
        {
            foreach (var element in Elements)
            {
                Log.DebugFormat("{0}: {1}", element.Key, element.Value);
            }

            foreach (var element in _mMemo)
            {
                Log.DebugFormat("Memo: {0}: {1}", element.Key, element.Value);
            }
        }

        #endregion Public Methods
    }
}