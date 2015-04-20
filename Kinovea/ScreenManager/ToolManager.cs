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

using Kinovea.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     Host of the list of tools.
    /// </summary>
    public static class ToolManager
    {
        public static void SavePresets()
        {
            var folder = PreferencesManager.SettingsFolder +
                         PreferencesManager.ResourceManager.GetString("ColorProfilesFolder");
            SavePresets(folder + "\\current.xml");
        }

        public static void SavePresets(string file)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.CloseOutput = true;

            using (var w = XmlWriter.Create(file, settings))
            {
                w.WriteStartElement("KinoveaColorProfile");
                w.WriteElementString("FormatVersion", "3.0");
                foreach (KeyValuePair<string, AbstractDrawingTool> tool in Tools)
                {
                    DrawingStyle preset = tool.Value.StylePreset;
                    if (preset != null && preset.Elements.Count > 0)
                    {
                        w.WriteStartElement("ToolPreset");
                        w.WriteAttributeString("Key", tool.Key);
                        preset.WriteXml(w);
                        w.WriteEndElement();
                    }
                }

                w.WriteEndElement();
            }
        }

        public static void LoadPresets()
        {
            var folder = PreferencesManager.SettingsFolder +
                         PreferencesManager.ResourceManager.GetString("ColorProfilesFolder");
            LoadPresets(folder + "\\current.xml");
        }

        public static void LoadPresets(string file)
        {
            if (!File.Exists(file))
                return;

            var settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;
            settings.CloseInput = true;

            using (var r = XmlReader.Create(file, settings))
            {
                try
                {
                    r.MoveToContent();
                    r.ReadStartElement();
                    var version = r.ReadElementContentAsString("FormatVersion", "");
                    if (version == "3.0")
                    {
                        while (r.NodeType == XmlNodeType.Element && r.Name == "ToolPreset")
                        {
                            var key = r.GetAttribute("Key");
                            var preset = new DrawingStyle(r);

                            // Find the tool with this key and replace its preset style with the one we just read.
                            AbstractDrawingTool tool;
                            var found = Tools.TryGetValue(key, out tool);
                            if (found)
                            {
                                // Carry on the memo so we can still do cancel and retrieve the old values.
                                DrawingStyle memo = tool.StylePreset.Clone();
                                tool.StylePreset = preset;
                                tool.StylePreset.Memorize(memo);
                            }
                            else
                            {
                                Log.ErrorFormat("The tool \"{0}\" was not found. Preset not imported.", key);
                            }
                        }
                    }
                    else
                    {
                        Log.ErrorFormat("Unsupported format ({0}) for tool presets", version);
                    }
                }
                catch (Exception)
                {
                    Log.Error("An error happened during the parsing of the tool presets file");
                }
            }
        }

        #region Private Methods

        private static void Initialize()
        {
            _mTools = new Dictionary<string, AbstractDrawingTool>();

            // The core drawing tools are hard wired.
            // Maybe in the future we can have a plug-in system with .dll containing extensions tools.
            // Note that the pointer "tool" is not listed, as each screen must have its own.
            _mTools.Add("Angle", new DrawingToolAngle2D());
            _mTools.Add("Chrono", new DrawingToolChrono());
            _mTools.Add("Circle", new DrawingToolCircle());
            _mTools.Add("CrossMark", new DrawingToolCross2D());
            _mTools.Add("Line", new DrawingToolLine2D());
            _mTools.Add("Pencil", new DrawingToolPencil());
            _mTools.Add("Label", new DrawingToolText());
            _mTools.Add("Grid", new DrawingToolGrid());
            _mTools.Add("Plane", new DrawingToolPlane());
            _mTools.Add("Magnifier", new DrawingToolMagnifier());

            LoadPresets();
        }

        #endregion Private Methods

        #region Properties

        /// <summary>
        ///     Returns the cached list of tools.
        /// </summary>
        public static Dictionary<string, AbstractDrawingTool> Tools
        {
            get
            {
                if (ReferenceEquals(_mTools, null))
                {
                    Initialize();
                }

                return _mTools;
            }
        }

        // Maybe we could find a way to generate this list of properties automatically.
        // A custom tool in the vein of the ResXFileCodeGenerator that would take an XML file in,
        // and creates a set of accessor properties.
        public static DrawingToolPointer Angle
        {
            get { return (DrawingToolAngle2D)Tools["Angle"]; }
        }

        public static DrawingToolChrono Chrono
        {
            get { return (DrawingToolChrono)Tools["Chrono"]; }
        }

        public static DrawingToolPointer Circle
        {
            get { return (DrawingToolCircle)Tools["Circle"]; }
        }

        public static DrawingToolPointer CrossMark
        {
            get { return (DrawingToolCross2D)Tools["CrossMark"]; }
        }

        public static DrawingToolPointer Line
        {
            get { return (DrawingToolLine2D)Tools["Line"]; }
        }

        public static DrawingToolPointer Pencil
        {
            get { return (DrawingToolPencil)Tools["Pencil"]; }
        }

        public static DrawingToolPointer Label
        {
            get { return (DrawingToolText)Tools["Label"]; }
        }

        public static DrawingToolGrid Grid
        {
            get { return (DrawingToolGrid)Tools["Grid"]; }
        }

        public static DrawingToolPointer Plane
        {
            get { return (DrawingToolPlane)Tools["Plane"]; }
        }

        public static DrawingToolPointer Magnifier
        {
            get { return (DrawingToolMagnifier)Tools["Magnifier"]; }
        }

        #endregion Properties

        #region Members

        private static Dictionary<string, AbstractDrawingTool> _mTools;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members
    }
}