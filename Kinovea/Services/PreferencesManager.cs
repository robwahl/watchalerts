#region Licence

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

#endregion Licence

using Kinovea.Services.Properties;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace Kinovea.Services
{
    /// <summary>
    ///     A class to encapsulate the user's preferences.
    ///     There is really two kind of preferences handled here:
    ///     - The filesystem independant preferences (language, flags, etc.)
    ///     - The filesystem dependant ones. (file history, shortcuts, etc.)
    ///     File system independant are stored in preferences.xml
    ///     others are handled through .NET settings framework.
    ///     TODO: homogenize ?
    ///     Adding a pref:
    ///     - add a member (with its default value) + add the property.
    ///     - add import and export in XML Helpers methods.
    /// </summary>
    public class PreferencesManager
    {
        #region Languages

        public CultureInfo GetSupportedCulture()
        {
            // Returns the culture that is used throgought the UI.
            var ci = new CultureInfo(UiCultureName);
            if (LanguageManager.IsSupportedCulture(ci))
                return ci;
            return new CultureInfo("en");
        }

        #endregion Languages

        #region Satic Properties

        public static string ReleaseVersion
        {
            // This prop is set as the first instruction of the RootKernel ctor.
            get { return Settings.Default.Release; }
            set { Settings.Default.Release = value; }
        }

        public static string SettingsFolder
        {
            // Store settings in user space.
            // If it doesn't exist, this folder is created at startup.
            get { return MAppdataFolder; }
        }

        public static ResourceManager ResourceManager
        {
            // FIXME: all folders should be accessed through real props.
            get { return Resources.ResourceManager; }
        }

        public static bool ExperimentalRelease
        {
            // Set in RootKernel ctor. Used to show/hide certain menus.
            get { return Settings.Default.ExperimentalRelease; }
            set { Settings.Default.ExperimentalRelease = value; }
        }

        public static string DefaultCaptureImageFile = "Capture";
        public static string DefaultCaptureVideoFile = "Capture";

        #endregion Satic Properties

        #region Properties (Preferences)

        public int HistoryCount
        {
            get { return _mIFilesToSave; }
            set { _mIFilesToSave = value; }
        }

        public string UiCultureName { get; set; }

        public TimeCodeFormat TimeCodeFormat
        {
            get { return _mTimeCodeFormat; }
            set { _mTimeCodeFormat = value; }
        }

        public SpeedUnits SpeedUnit
        {
            get { return _mSpeedUnit; }
            set { _mSpeedUnit = value; }
        }

        public ImageAspectRatio AspectRatio
        {
            get { return _mAspectRatio; }
            set { _mAspectRatio = value; }
        }

        public bool DeinterlaceByDefault { get; set; }

        public int WorkingZoneSeconds
        {
            get { return _mIWorkingZoneSeconds; }
            set { _mIWorkingZoneSeconds = value; }
        }

        public int WorkingZoneMemory
        {
            get { return _mIWorkingZoneMemory; }
            set { _mIWorkingZoneMemory = value; }
        }

        public InfosFading DefaultFading
        {
            get { return _mDefaultFading; }
            set { _mDefaultFading = value; }
        }

        public int MaxFading
        {
            get { return _mIMaxFading; }
            set { _mIMaxFading = value; }
        }

        public bool DrawOnPlay
        {
            get { return _mBDrawOnPlay; }
            set { _mBDrawOnPlay = value; }
        }

        public bool ExplorerVisible
        {
            get { return _mBIsExplorerVisible; }
            set { _mBIsExplorerVisible = value; }
        }

        public int ExplorerSplitterDistance
        {
            // Splitter between Explorer and ScreenManager
            get { return _mIExplorerSplitterDistance; }
            set { _mIExplorerSplitterDistance = value; }
        }

        public int ExplorerFilesSplitterDistance
        {
            // Splitter between folders and files on Explorer tab
            get { return _mIExplorerFilesSplitterDistance; }
            set { _mIExplorerFilesSplitterDistance = value; }
        }

        public ExplorerThumbSizes ExplorerThumbsSize
        {
            // Size category of the thumbnails.
            get { return _mIExplorerThumbsSize; }
            set { _mIExplorerThumbsSize = value; }
        }

        public int ShortcutsFilesSplitterDistance
        {
            // Splitter between folders and files on Shortcuts tab
            get { return _mIShortcutsFilesSplitterDistance; }
            set { _mIShortcutsFilesSplitterDistance = value; }
        }

        public List<ShortcutFolder> ShortcutFolders
        {
            // FIXME.
            // we want the client of the prop to get a read only access.
            // here we offer a reference on an internal objetc, he can call .Clear().
            get { return MShortcutFolders; }
        }

        public string LastBrowsedDirectory
        {
            get { return Settings.Default.BrowserDirectory; }
            set
            {
                Settings.Default.BrowserDirectory = value;
                Settings.Default.Save();
            }
        }

        public ActiveFileBrowserTab ActiveTab
        {
            get { return _mActiveFileBrowserTab; }
            set { _mActiveFileBrowserTab = value; }
        }

        public string CaptureImageDirectory
        {
            get { return _mCaptureImageDirectory; }
            set { _mCaptureImageDirectory = value; }
        }

        public string CaptureVideoDirectory
        {
            get { return _mCaptureVideoDirectory; }
            set { _mCaptureVideoDirectory = value; }
        }

        public KinoveaImageFormat CaptureImageFormat
        {
            get { return _mCaptureImageFormat; }
            set { _mCaptureImageFormat = value; }
        }

        public KinoveaVideoFormat CaptureVideoFormat
        {
            get { return _mCaptureVideoFormat; }
            set { _mCaptureVideoFormat = value; }
        }

        public string CaptureImageFile
        {
            get { return _mCaptureImageFile; }
            set { _mCaptureImageFile = value; }
        }

        public string CaptureVideoFile
        {
            get { return _mCaptureVideoFile; }
            set { _mCaptureVideoFile = value; }
        }

        public bool CaptureUsePattern { get; set; }

        public string CapturePattern
        {
            get { return _mCapturePattern; }
            set { _mCapturePattern = value; }
        }

        public long CaptureImageCounter
        {
            get { return _mICaptureImageCounter; }
            set { _mICaptureImageCounter = value; }
        }

        public long CaptureVideoCounter
        {
            get { return _mICaptureVideoCounter; }
            set { _mICaptureVideoCounter = value; }
        }

        public int CaptureMemoryBuffer
        {
            get { return _mICaptureMemoryBuffer; }
            set { _mICaptureMemoryBuffer = value; }
        }

        public List<Color> RecentColors
        {
            // Fixme: ref to the internal object.
            get { return _mRecentColors; }
        }

        public List<DeviceConfiguration> DeviceConfigurations
        {
            get { return _mDeviceConfigurations; }
        }

        public string NetworkCameraUrl
        {
            get { return _mNetworkCameraUrl; }
            set { _mNetworkCameraUrl = value; }
        }

        public NetworkCameraFormat NetworkCameraFormat
        {
            get { return _mNetworkCameraFormat; }
            set { _mNetworkCameraFormat = value; }
        }

        public List<string> RecentNetworkCameras
        {
            get { return MRecentNetworkCameras; }
        }

        #endregion Properties (Preferences)

        #region Members

        // Preferences
        private readonly List<string> _mHistoryList = new List<string>();

        private int _mIFilesToSave = 5;
        private TimeCodeFormat _mTimeCodeFormat = TimeCodeFormat.ClassicTime;
        private SpeedUnits _mSpeedUnit = SpeedUnits.MetersPerSecond;
        private ImageAspectRatio _mAspectRatio = ImageAspectRatio.Auto;
        private int _mIWorkingZoneSeconds = 12;
        private int _mIWorkingZoneMemory = 512;
        private InfosFading _mDefaultFading = new InfosFading();
        private int _mIMaxFading = 200;
        private bool _mBDrawOnPlay = true;
        private bool _mBIsExplorerVisible = true;
        private int _mIExplorerSplitterDistance = 250;
        private int _mIExplorerFilesSplitterDistance = 350;
        private int _mIShortcutsFilesSplitterDistance = 350;
        private ExplorerThumbSizes _mIExplorerThumbsSize = ExplorerThumbSizes.Medium;

        private static readonly string MAppdataFolder =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Kinovea\\";

        public readonly List<ShortcutFolder> MShortcutFolders = new List<ShortcutFolder>();
        private ActiveFileBrowserTab _mActiveFileBrowserTab = ActiveFileBrowserTab.Explorer;
        private string _mCaptureImageDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string _mCaptureVideoDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private KinoveaImageFormat _mCaptureImageFormat = KinoveaImageFormat.Jpg;
        private KinoveaVideoFormat _mCaptureVideoFormat = KinoveaVideoFormat.Mkv;
        private string _mCaptureImageFile = "";
        private string _mCaptureVideoFile = "";
        private string _mCapturePattern = "Cap-%y-%mo-%d - %i";
        private long _mICaptureImageCounter = 1;
        private long _mICaptureVideoCounter = 1;
        private int _mICaptureMemoryBuffer = 768;
        private readonly List<Color> _mRecentColors = new List<Color>();
        private readonly int _mIMaxRecentColors = 12;
        private readonly List<DeviceConfiguration> _mDeviceConfigurations = new List<DeviceConfiguration>();
        private string _mNetworkCameraUrl = "http://localhost:8080/cam_1.jpg";
        private NetworkCameraFormat _mNetworkCameraFormat = NetworkCameraFormat.Jpeg;
        public readonly List<string> MRecentNetworkCameras = new List<string>();
        private readonly int _mIMaxRecentNetworkCameras = 5;

        // Helpers members
        private static PreferencesManager _mInstance;

        private ToolStripMenuItem _mHistoryMenu;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor & Singleton

        public static PreferencesManager Instance()
        {
            return _mInstance ?? (_mInstance = new PreferencesManager());
        }

        private PreferencesManager()
        {
            // By default we use the System Language.
            // If it is not supported, it will fall back to English.
            UiCultureName = Thread.CurrentThread.CurrentUICulture.Name;
            Log.Debug(string.Format("System Culture: [{0}].", UiCultureName));
            Import();
            GetHistoryAsList();
        }

        #endregion Constructor & Singleton

        #region Import/Export Public interface

        public void Export()
        {
            Log.Debug("Exporting preferences.");
            FlushToDisk(MAppdataFolder + Resources.ResourceManager.GetString("PreferencesFile"));
        }

        public void Import()
        {
            Log.Debug("Importing preferences.");
            ParseConfigFile(MAppdataFolder + Resources.ResourceManager.GetString("PreferencesFile"));
        }

        #endregion Import/Export Public interface

        #region XML Helpers

        private void FlushToDisk(string filePath)
        {
            try
            {
                var preferencesWriter = new XmlTextWriter(filePath, null) { Formatting = Formatting.Indented };
                preferencesWriter.WriteStartDocument();
                preferencesWriter.WriteStartElement("KinoveaPreferences");

                // Format version
                preferencesWriter.WriteStartElement("FormatVersion");
                preferencesWriter.WriteString("1.2");
                preferencesWriter.WriteEndElement();

                // Preferences
                preferencesWriter.WriteElementString("HistoryCount", _mIFilesToSave.ToString());
                preferencesWriter.WriteElementString("Language", UiCultureName);
                preferencesWriter.WriteElementString("TimeCodeFormat", _mTimeCodeFormat.ToString());
                preferencesWriter.WriteElementString("SpeedUnit", _mSpeedUnit.ToString());
                preferencesWriter.WriteElementString("ImageAspectRatio", _mAspectRatio.ToString());
                preferencesWriter.WriteElementString("DeinterlaceByDefault", DeinterlaceByDefault.ToString());
                preferencesWriter.WriteElementString("WorkingZoneSeconds", _mIWorkingZoneSeconds.ToString());
                preferencesWriter.WriteElementString("WorkingZoneMemory", _mIWorkingZoneMemory.ToString());

                //m_DefaultFading.ToXml(PreferencesWriter, true);
                preferencesWriter.WriteElementString("MaxFading", _mIMaxFading.ToString());

                preferencesWriter.WriteElementString("DrawOnPlay", _mBDrawOnPlay.ToString());
                preferencesWriter.WriteElementString("ExplorerThumbnailsSize", _mIExplorerThumbsSize.ToString());
                preferencesWriter.WriteElementString("ExplorerVisible", _mBIsExplorerVisible.ToString());
                preferencesWriter.WriteElementString("ExplorerSplitterDistance", _mIExplorerSplitterDistance.ToString());
                preferencesWriter.WriteElementString("ActiveFileBrowserTab", _mActiveFileBrowserTab.ToString());
                preferencesWriter.WriteElementString("ExplorerFilesSplitterDistance",
                    _mIExplorerFilesSplitterDistance.ToString());
                preferencesWriter.WriteElementString("ShortcutsFilesSplitterDistance",
                    _mIShortcutsFilesSplitterDistance.ToString());

                if (MShortcutFolders.Count > 0)
                {
                    preferencesWriter.WriteStartElement("Shortcuts");
                    foreach (var sf in MShortcutFolders)
                    {
                        sf.ToXml(preferencesWriter);
                    }
                    preferencesWriter.WriteEndElement();
                }

                if (_mRecentColors.Count > 0)
                {
                    preferencesWriter.WriteStartElement("RecentColors");
                    foreach (var col in _mRecentColors)
                    {
                        preferencesWriter.WriteStartElement("Color");
                        preferencesWriter.WriteString(col.R + ";" + col.G + ";" + col.B);
                        preferencesWriter.WriteEndElement();
                    }
                    preferencesWriter.WriteEndElement();
                }

                if (!string.IsNullOrEmpty(_mCaptureImageDirectory))
                    preferencesWriter.WriteElementString("CaptureImageDirectory", _mCaptureImageDirectory);
                if (_mCaptureImageDirectory != null && _mCaptureImageFile != "")
                    preferencesWriter.WriteElementString("CaptureImageFile", _mCaptureImageFile);
                if (_mCaptureImageDirectory != null && _mCaptureVideoDirectory != "")
                    preferencesWriter.WriteElementString("CaptureVideoDirectory", _mCaptureVideoDirectory);
                if (_mCaptureImageDirectory != null && _mCaptureVideoFile != "")
                    preferencesWriter.WriteElementString("CaptureVideoFile", _mCaptureVideoFile);
                preferencesWriter.WriteElementString("CaptureImageFormat", _mCaptureImageFormat.ToString());
                preferencesWriter.WriteElementString("CaptureVideoFormat", _mCaptureVideoFormat.ToString());

                preferencesWriter.WriteElementString("CaptureUsePattern", CaptureUsePattern.ToString());
                preferencesWriter.WriteElementString("CapturePattern", _mCapturePattern);
                preferencesWriter.WriteElementString("CaptureImageCounter", _mICaptureImageCounter.ToString());
                preferencesWriter.WriteElementString("CaptureVideoCounter", _mICaptureVideoCounter.ToString());

                preferencesWriter.WriteElementString("CaptureMemoryBuffer", _mICaptureMemoryBuffer.ToString());

                if (_mDeviceConfigurations.Count > 0)
                {
                    preferencesWriter.WriteStartElement("DeviceConfigurations");
                    foreach (var conf in _mDeviceConfigurations)
                    {
                        conf.ToXml(preferencesWriter);
                    }
                    preferencesWriter.WriteEndElement();
                }

                // Network cameras : url, format, list of recent url.
                preferencesWriter.WriteElementString("NetworkCameraUrl", _mNetworkCameraUrl);
                preferencesWriter.WriteElementString("NetworkCameraFormat", _mNetworkCameraFormat.ToString());
                if (MRecentNetworkCameras.Count > 0)
                {
                    preferencesWriter.WriteStartElement("RecentNetworkCameras");
                    foreach (var url in MRecentNetworkCameras)
                    {
                        preferencesWriter.WriteStartElement("NetworkCamera");
                        preferencesWriter.WriteString(url);
                        preferencesWriter.WriteEndElement();
                    }
                    preferencesWriter.WriteEndElement();
                }

                preferencesWriter.WriteEndElement();
                preferencesWriter.WriteEndDocument();
                preferencesWriter.Flush();
                preferencesWriter.Close();
            }
            catch (Exception)
            {
                Log.Error("Error happenned while writing preferences.");
            }
        }

        private void ParseConfigFile(string filePath)
        {
            // Fill the local variables with infos found in the XML file.
            XmlReader preferencesReader = new XmlTextReader(filePath);

            try
            {
                while (preferencesReader.Read())
                {
                    if ((preferencesReader.IsStartElement()) && (preferencesReader.Name == "KinoveaPreferences"))
                    {
                        while (preferencesReader.Read())
                        {
                            if (preferencesReader.IsStartElement())
                            {
                                switch (preferencesReader.Name)
                                {
                                    case "HistoryCount":
                                        _mIFilesToSave = int.Parse(preferencesReader.ReadString());
                                        break;

                                    case "Language":
                                        UiCultureName = preferencesReader.ReadString();
                                        break;

                                    case "TimeCodeFormat":
                                        _mTimeCodeFormat = ParseTimeCodeFormat(preferencesReader.ReadString());
                                        break;

                                    case "SpeedUnit":
                                        _mSpeedUnit = ParseSpeedUnit(preferencesReader.ReadString());
                                        break;

                                    case "ImageAspectRatio":
                                        _mAspectRatio = ParseImageAspectRatio(preferencesReader.ReadString());
                                        break;

                                    case "DeinterlaceByDefault":
                                        DeinterlaceByDefault = bool.Parse(preferencesReader.ReadString());
                                        break;

                                    case "WorkingZoneSeconds":
                                        _mIWorkingZoneSeconds = int.Parse(preferencesReader.ReadString());
                                        break;

                                    case "WorkingZoneMemory":
                                        _mIWorkingZoneMemory = int.Parse(preferencesReader.ReadString());
                                        break;

                                    case "InfosFading":
                                        //m_DefaultFading.ReadXml(PreferencesReader);
                                        break;

                                    case "MaxFading":
                                        _mIMaxFading = int.Parse(preferencesReader.ReadString());
                                        break;

                                    case "DrawOnPlay":
                                        _mBDrawOnPlay = bool.Parse(preferencesReader.ReadString());
                                        break;

                                    case "ExplorerThumbnailsSize":
                                        _mIExplorerThumbsSize =
                                            (ExplorerThumbSizes)
                                                Enum.Parse(_mIExplorerThumbsSize.GetType(),
                                                    preferencesReader.ReadString());
                                        break;

                                    case "ExplorerVisible":
                                        _mBIsExplorerVisible = bool.Parse(preferencesReader.ReadString());
                                        break;

                                    case "ExplorerSplitterDistance":
                                        _mIExplorerSplitterDistance = int.Parse(preferencesReader.ReadString());
                                        break;

                                    case "ActiveFileBrowserTab":
                                        _mActiveFileBrowserTab =
                                            (ActiveFileBrowserTab)
                                                Enum.Parse(_mActiveFileBrowserTab.GetType(),
                                                    preferencesReader.ReadString());
                                        break;

                                    case "ExplorerFilesSplitterDistance":
                                        _mIExplorerFilesSplitterDistance = int.Parse(preferencesReader.ReadString());
                                        break;

                                    case "ShortcutsFilesSplitterDistance":
                                        _mIShortcutsFilesSplitterDistance = int.Parse(preferencesReader.ReadString());
                                        break;

                                    case "Shortcuts":
                                        ParseShortcuts(preferencesReader);
                                        break;

                                    case "RecentColors":
                                        ParseRecentColors(preferencesReader);
                                        break;

                                    case "CaptureImageDirectory":
                                        _mCaptureImageDirectory = preferencesReader.ReadString();
                                        break;

                                    case "CaptureImageFile":
                                        _mCaptureImageFile = preferencesReader.ReadString();
                                        break;

                                    case "CaptureVideoDirectory":
                                        _mCaptureVideoDirectory = preferencesReader.ReadString();
                                        break;

                                    case "CaptureVideoFile":
                                        _mCaptureVideoFile = preferencesReader.ReadString();
                                        break;

                                    case "CaptureImageFormat":
                                        _mCaptureImageFormat = ParseImageFormat(preferencesReader.ReadString());
                                        break;

                                    case "CaptureVideoFormat":
                                        _mCaptureVideoFormat = ParseVideoFormat(preferencesReader.ReadString());
                                        break;

                                    case "CaptureUsePattern":
                                        CaptureUsePattern = bool.Parse(preferencesReader.ReadString());
                                        break;

                                    case "CapturePattern":
                                        _mCapturePattern = preferencesReader.ReadString();
                                        break;

                                    case "CaptureImageCounter":
                                        _mICaptureImageCounter = long.Parse(preferencesReader.ReadString());
                                        break;

                                    case "CaptureVideoCounter":
                                        _mICaptureVideoCounter = long.Parse(preferencesReader.ReadString());
                                        break;

                                    case "CaptureMemoryBuffer":
                                        _mICaptureMemoryBuffer = int.Parse(preferencesReader.ReadString());
                                        break;

                                    case "DeviceConfigurations":
                                        ParseDeviceConfigurations(preferencesReader);
                                        break;

                                    case "NetworkCameraUrl":
                                        _mNetworkCameraUrl = preferencesReader.ReadString();
                                        break;

                                    case "NetworkCameraFormat":
                                        _mNetworkCameraFormat = ParseCameraFormat(preferencesReader.ReadString());
                                        break;

                                    case "RecentNetworkCameras":
                                        ParseRecentCameras(preferencesReader);
                                        break;
                                    // Preference from a newer file format...
                                    // We don't have a holder variable for it.
                                }
                            }
                            else if (preferencesReader.Name == "KinoveaPreferences")
                            {
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                Log.Error("Error happenned while parsing preferences. We'll keep the default values.");
            }
            finally
            {
                preferencesReader.Close();
            }
        }

        private TimeCodeFormat ParseTimeCodeFormat(string format)
        {
            TimeCodeFormat tcf;

            // cannot use a switch, a constant value is expected.

            if (format.Equals(TimeCodeFormat.ClassicTime.ToString()))
            {
                tcf = TimeCodeFormat.ClassicTime;
            }
            else if (format.Equals(TimeCodeFormat.Frames.ToString()))
            {
                tcf = TimeCodeFormat.Frames;
            }
            else if (format.Equals(TimeCodeFormat.Milliseconds.ToString()))
            {
                tcf = TimeCodeFormat.Frames;
            }
            else if (format.Equals(TimeCodeFormat.TenThousandthOfHours.ToString()))
            {
                tcf = TimeCodeFormat.TenThousandthOfHours;
            }
            else if (format.Equals(TimeCodeFormat.HundredthOfMinutes.ToString()))
            {
                tcf = TimeCodeFormat.HundredthOfMinutes;
            }
            else if (format.Equals(TimeCodeFormat.Timestamps.ToString()))
            {
                tcf = TimeCodeFormat.Timestamps;
            }
            else if (format.Equals(TimeCodeFormat.TimeAndFrames.ToString()))
            {
                tcf = TimeCodeFormat.TimeAndFrames;
            }
            else
            {
                // Unkown format. May be a Preferences file from a newer version.
                // We'll stick to default.
                tcf = TimeCodeFormat.ClassicTime;
            }

            return tcf;
        }

        private SpeedUnits ParseSpeedUnit(string format)
        {
            SpeedUnits su;

            // cannot use a switch, a constant value is expected.

            if (format.Equals(SpeedUnits.MetersPerSecond.ToString()))
            {
                su = SpeedUnits.MetersPerSecond;
            }
            else if (format.Equals(SpeedUnits.KilometersPerHour.ToString()))
            {
                su = SpeedUnits.KilometersPerHour;
            }
            else if (format.Equals(SpeedUnits.FeetPerSecond.ToString()))
            {
                su = SpeedUnits.FeetPerSecond;
            }
            else if (format.Equals(SpeedUnits.MilesPerHour.ToString()))
            {
                su = SpeedUnits.MilesPerHour;
            }
            else if (format.Equals(SpeedUnits.Knots.ToString()))
            {
                su = SpeedUnits.Knots;
            }
            else
            {
                // Unkown format. May be a Preferences file from a newer version.
                // We'll stick to default.
                su = SpeedUnits.MetersPerSecond;
            }

            return su;
        }

        private ImageAspectRatio ParseImageAspectRatio(string format)
        {
            ImageAspectRatio iar;

            // cannot use a switch, a constant value is expected.

            if (format.Equals(ImageAspectRatio.Auto.ToString()))
            {
                iar = ImageAspectRatio.Auto;
            }
            else if (format.Equals(ImageAspectRatio.Force169.ToString()))
            {
                iar = ImageAspectRatio.Force169;
            }
            else if (format.Equals(ImageAspectRatio.Force43.ToString()))
            {
                iar = ImageAspectRatio.Force43;
            }
            else
            {
                // Unkown format. May be a Preferences file from a newer version.
                // We'll stick to default.
                iar = ImageAspectRatio.Auto;
            }

            return iar;
        }

        private void ParseShortcuts(XmlReader xmlReader)
        {
            MShortcutFolders.Clear();

            while (xmlReader.Read())
            {
                if (xmlReader.IsStartElement())
                {
                    if (xmlReader.Name == "Shortcut")
                    {
                        var sf = ShortcutFolder.FromXml(xmlReader);
                        if (sf != null)
                        {
                            MShortcutFolders.Add(sf);
                        }
                    }
                }
                else if (xmlReader.Name == "Shortcuts")
                {
                    break;
                }
            }
        }

        private void ParseRecentColors(XmlReader xmlReader)
        {
            _mRecentColors.Clear();

            while (xmlReader.Read())
            {
                if (xmlReader.IsStartElement())
                {
                    if (xmlReader.Name == "Color")
                    {
                        var col = XmlHelper.ParseColor(xmlReader.ReadString());
                        _mRecentColors.Add(col);
                    }
                }
                else if (xmlReader.Name == "RecentColors")
                {
                    break;
                }
            }
        }

        private void ParseDeviceConfigurations(XmlReader xmlReader)
        {
            _mDeviceConfigurations.Clear();

            while (xmlReader.Read())
            {
                if (xmlReader.IsStartElement())
                {
                    if (xmlReader.Name == "DeviceConfiguration")
                    {
                        var conf = DeviceConfiguration.FromXml(xmlReader);
                        if (conf != null)
                        {
                            _mDeviceConfigurations.Add(conf);
                        }
                    }
                }
                else if (xmlReader.Name == "DeviceConfigurations")
                {
                    break;
                }
            }
        }

        private KinoveaImageFormat ParseImageFormat(string format)
        {
            var output = KinoveaImageFormat.Jpg;
            try
            {
                output = (KinoveaImageFormat)Enum.Parse(KinoveaImageFormat.Jpg.GetType(), format);
            }
            catch (Exception)
            {
                Log.ErrorFormat("Image format parsing failed. Use default value");
            }

            return output;
        }

        private KinoveaVideoFormat ParseVideoFormat(string format)
        {
            var output = KinoveaVideoFormat.Mkv;
            try
            {
                output = (KinoveaVideoFormat)Enum.Parse(KinoveaVideoFormat.Mkv.GetType(), format);
            }
            catch (Exception)
            {
                Log.ErrorFormat("Video format parsing failed. Use default value");
            }

            return output;
        }

        private NetworkCameraFormat ParseCameraFormat(string format)
        {
            var output = NetworkCameraFormat.Jpeg;
            try
            {
                output = (NetworkCameraFormat)Enum.Parse(NetworkCameraFormat.Jpeg.GetType(), format);
            }
            catch (Exception)
            {
                Log.ErrorFormat("Network camera format parsing failed. Use default value");
            }

            return output;
        }

        private void ParseRecentCameras(XmlReader xmlReader)
        {
            MRecentNetworkCameras.Clear();

            while (xmlReader.Read())
            {
                if (xmlReader.IsStartElement())
                {
                    if (xmlReader.Name == "NetworkCamera")
                    {
                        MRecentNetworkCameras.Add(xmlReader.ReadString());
                    }
                }
                else if (xmlReader.Name == "RecentNetworkCameras")
                {
                    break;
                }
            }
        }

        #endregion XML Helpers

        #region Local files target.

        public void RegisterHistoryMenu(ToolStripMenuItem historyMenu)
        {
            _mHistoryMenu = historyMenu;
        }

        public void OrganizeHistoryMenu()
        {
            // History sub menus configuration: Only display non empty entries.
            var atLeastOne = false;
            for (var i = 0; i < _mHistoryMenu.DropDownItems.Count - 2; i++)
            {
                if (!string.IsNullOrEmpty(_mHistoryList[i]) && i < _mIFilesToSave)
                {
                    _mHistoryMenu.DropDownItems[i].Text = Path.GetFileName(_mHistoryList[i]);
                    _mHistoryMenu.DropDownItems[i].Visible = true;
                    atLeastOne = true;
                }
                else
                {
                    _mHistoryMenu.DropDownItems[i].Visible = false;
                }
            }

            // Separator & reset.
            _mHistoryMenu.DropDownItems[_mHistoryMenu.DropDownItems.Count - 2].Visible = atLeastOne;
            _mHistoryMenu.DropDownItems[_mHistoryMenu.DropDownItems.Count - 1].Visible = atLeastOne;
            _mHistoryMenu.Enabled = atLeastOne;
        }

        private void GetHistoryAsList()
        {
            // Get history in a list to ease read/write operations.
            _mHistoryList.Clear();

            // Remembered files.
            _mHistoryList.Add(Settings.Default.HistoryVideo1);
            _mHistoryList.Add(Settings.Default.HistoryVideo2);
            _mHistoryList.Add(Settings.Default.HistoryVideo3);
            _mHistoryList.Add(Settings.Default.HistoryVideo4);
            _mHistoryList.Add(Settings.Default.HistoryVideo5);
            _mHistoryList.Add(Settings.Default.HistoryVideo6);
            _mHistoryList.Add(Settings.Default.HistoryVideo7);
            _mHistoryList.Add(Settings.Default.HistoryVideo8);
            _mHistoryList.Add(Settings.Default.HistoryVideo9);
            _mHistoryList.Add(Settings.Default.HistoryVideo10);
        }

        private void PutListAsHistory()
        {
            // Items older than max number have already been set to empty string.
            Settings.Default.HistoryVideo1 = _mHistoryList[0];
            Settings.Default.HistoryVideo2 = _mHistoryList[1];
            Settings.Default.HistoryVideo3 = _mHistoryList[2];
            Settings.Default.HistoryVideo4 = _mHistoryList[3];
            Settings.Default.HistoryVideo5 = _mHistoryList[4];
            Settings.Default.HistoryVideo6 = _mHistoryList[5];
            Settings.Default.HistoryVideo7 = _mHistoryList[6];
            Settings.Default.HistoryVideo8 = _mHistoryList[7];
            Settings.Default.HistoryVideo9 = _mHistoryList[8];
            Settings.Default.HistoryVideo10 = _mHistoryList[9];
            Settings.Default.Save();
        }

        public void HistoryReset()
        {
            for (var i = 0; i < _mHistoryList.Count; i++)
                _mHistoryList[i] = "";

            PutListAsHistory();
        }

        public void HistoryAdd(string file)
        {
            var knownIndex = -1;

            // Check if we already know it.
            for (var i = 0; i < _mIFilesToSave; i++)
            {
                if (_mHistoryList[i] == file)
                    knownIndex = i;
            }

            if (knownIndex < 0)
            {
                // Shift all entries back one spot to make room for the new one.
                for (var i = _mIFilesToSave - 1; i > 0; i--)
                    _mHistoryList[i] = _mHistoryList[i - 1];
            }
            else
            {
                // Only shift entries that were newer.
                for (var i = knownIndex; i > 0; i--)
                    _mHistoryList[i] = _mHistoryList[i - 1];
            }

            _mHistoryList[0] = file;
            PutListAsHistory();
        }

        public string GetFilePathAtIndex(int index)
        {
            return _mHistoryList[index];
        }

        #endregion Local files target.

        #region Misc

        public void AddRecentColor(Color color)
        {
            // Check if we already have it in the list.
            var found = -1;
            for (var i = 0; i < _mRecentColors.Count; i++)
            {
                if (color.Equals(_mRecentColors[i]))
                {
                    found = i;
                    break;
                }
            }

            if (found >= 0)
            {
                _mRecentColors.RemoveAt(found);
            }
            else if (_mRecentColors.Count == _mIMaxRecentColors)
            {
                _mRecentColors.RemoveAt(_mRecentColors.Count - 1);
            }

            _mRecentColors.Insert(0, color);

            // Maybe the export should be of the caller's responsibility ?
            Export();
        }

        public void UpdateSelectedCapability(string id, DeviceCapability cap)
        {
            // Check if we already know this device, update it or create it.
            var deviceFound = false;
            foreach (var conf in _mDeviceConfigurations)
            {
                if (conf.Id == id)
                {
                    // Update the device config.
                    deviceFound = true;
                    conf.Cap = new DeviceCapability(cap.FrameSize, cap.Framerate);
                }
            }

            if (!deviceFound)
            {
                // Create the device conf.
                var conf = new DeviceConfiguration
                {
                    Id = id,
                    Cap = new DeviceCapability(cap.FrameSize, cap.Framerate)
                };
                _mDeviceConfigurations.Add(conf);
            }

            Export();
        }

        public string GetImageFormat()
        {
            string format;

            switch (_mCaptureImageFormat)
            {
                case KinoveaImageFormat.Png:
                    format = ".png";
                    break;

                case KinoveaImageFormat.Bmp:
                    format = ".bmp";
                    break;

                default:
                    format = ".jpg";
                    break;
            }
            return format;
        }

        public string GetVideoFormat()
        {
            string format;

            switch (_mCaptureVideoFormat)
            {
                case KinoveaVideoFormat.Mp4:
                    format = ".mp4";
                    break;

                case KinoveaVideoFormat.Avi:
                    format = ".avi";
                    break;

                default:
                    format = ".mkv";
                    break;
            }
            return format;
        }

        public void AddRecentCamera(string url)
        {
            // Check if we already know about it.
            var found = -1;
            for (var i = 0; i < MRecentNetworkCameras.Count; i++)
            {
                if (MRecentNetworkCameras[i] == url)
                {
                    found = i;
                    break;
                }
            }

            // Remove it where we found it or remove the oldest one if max reached.
            if (found >= 0)
            {
                MRecentNetworkCameras.RemoveAt(found);
            }
            else if (MRecentNetworkCameras.Count == _mIMaxRecentNetworkCameras)
            {
                MRecentNetworkCameras.RemoveAt(MRecentNetworkCameras.Count - 1);
            }

            // Insert it on top.
            MRecentNetworkCameras.Insert(0, url);

            // Export is the responsibility of the caller.
        }

        #endregion Misc
    }

    #region namespace wide enums

    /// <summary>
    ///     Timecode formats.
    ///     The preferences combo box must keep this order.
    /// </summary>
    public enum TimeCodeFormat
    {
        ClassicTime,
        Frames,
        Milliseconds,
        TenThousandthOfHours,
        HundredthOfMinutes,
        TimeAndFrames,
        Timestamps,
        Unknown,
        NumberOfTimeCodeFormats
    }

    /// <summary>
    ///     Standards speed units.
    /// </summary>
    public enum SpeedUnits
    {
        MetersPerSecond,
        KilometersPerHour,
        FeetPerSecond,
        MilesPerHour,
        Knots,
        PixelsPerFrame // Native unit.
    }

    public enum TimeCodeType
    {
        Number,
        String,
        Time
    }

    public enum ImageAspectRatio
    {
        // Note: this enum also exists in VideoFiles namespace.
        Auto,

        Force43,
        Force169
    }

    /// <summary>
    ///     Last active tab.
    ///     Must keep the same ordering as in FileBrowserUI.
    /// </summary>
    public enum ActiveFileBrowserTab
    {
        Explorer = 0,
        Shortcuts
    }

    /// <summary>
    ///     Size of the thumbnails in the explorer.
    ///     Sizes are expressed in number of thumbnails that should fit in the width of the explorer.
    ///     the actual size of any given thumbnail will change depending on the available space.
    /// </summary>
    public enum ExplorerThumbSizes
    {
        ExtraLarge = 4,
        Large = 5,
        Medium = 7,
        Small = 10,
        ExtraSmall = 14
    };

    // Named with Kinovea to avoid conflict with System.Drawing.Imaging.
    public enum KinoveaImageFormat
    {
        Jpg,
        Png,
        Bmp
    };

    public enum KinoveaVideoFormat
    {
        Mkv,
        Mp4,
        Avi
    };

    public enum NetworkCameraFormat
    {
        Jpeg,
        Mjpeg
    };

    #endregion namespace wide enums

    public class DeviceConfiguration
    {
        public DeviceCapability Cap;
        public string Id;

        public void ToXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("DeviceConfiguration");

            writer.WriteStartElement("Identification");
            writer.WriteString(Id);
            writer.WriteEndElement();

            writer.WriteStartElement("Size");
            writer.WriteString(Cap.FrameSize.Width + ";" + Cap.FrameSize.Height);
            writer.WriteEndElement();

            writer.WriteStartElement("Framerate");
            writer.WriteString(Cap.Framerate.ToString());
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        public static DeviceConfiguration FromXml(XmlReader xmlReader)
        {
            var id = "";
            var frameSize = Size.Empty;
            var frameRate = 0;

            while (xmlReader.Read())
            {
                if (xmlReader.IsStartElement())
                {
                    if (xmlReader.Name == "Identification")
                    {
                        id = xmlReader.ReadString();
                    }
                    else if (xmlReader.Name == "Size")
                    {
                        var p = XmlHelper.ParsePoint(xmlReader.ReadString());
                        frameSize = new Size(p);
                    }
                    else if (xmlReader.Name == "Framerate")
                    {
                        frameRate = int.Parse(xmlReader.ReadString());
                    }
                }
                else if (xmlReader.Name == "DeviceConfiguration")
                {
                    break;
                }
            }

            DeviceConfiguration conf = null;
            if (id.Length > 0)
            {
                conf = new DeviceConfiguration
                {
                    Id = id,
                    Cap = new DeviceCapability(frameSize, frameRate)
                };
            }

            return conf;
        }
    }
}