#region License

/*
Copyright © Joan Charmant 2008-2009.
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

using System;
using System.IO;
using System.Xml;

namespace Kinovea.Services
{
    /// <summary>
    ///     A class to encapsulate one item of the shortcut folders.
    /// </summary>
    public class ShortcutFolder : IComparable
    {
        public ShortcutFolder(string friendlyName, string location)
        {
            FriendlyName = friendlyName;
            Location = location;
        }

        #region IComparable Implementation

        public int CompareTo(object obj)
        {
            var sf = obj as ShortcutFolder;
            if (sf != null)
            {
                var path2 = Path.GetFileName(sf.Location);
                {
                    var path1 = Path.GetFileName(Location);
                    return string.Compare(path1, path2, StringComparison.Ordinal);
                }
            }
            throw new ArgumentException("Impossible comparison");
        }

        #endregion IComparable Implementation

        public override string ToString()
        {
            return FriendlyName;
        }

        public void ToXml(XmlTextWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("Shortcut");

            xmlWriter.WriteStartElement("FriendlyName");
            xmlWriter.WriteString(FriendlyName);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("Location");
            xmlWriter.WriteString(Location);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteEndElement();
        }

        public static ShortcutFolder FromXml(XmlReader xmlReader)
        {
            // When we land in this method we MUST already be at the "Shortcut" node.

            var friendlyName = "";
            var location = "";

            while (xmlReader.Read())
            {
                if (xmlReader.IsStartElement())
                {
                    if (xmlReader.Name == "FriendlyName")
                    {
                        friendlyName = xmlReader.ReadString();
                    }
                    else if (xmlReader.Name == "Location")
                    {
                        location = xmlReader.ReadString();
                    }
                }
                else if (xmlReader.Name == "Shortcut")
                {
                    break;
                }
            }

            ShortcutFolder sf = null;
            if (location.Length > 0)
            {
                sf = new ShortcutFolder(friendlyName, location);
            }

            return sf;
        }

        #region Properties

        public string Location { get; set; }

        public string FriendlyName { get; set; }

        #endregion Properties
    }
}