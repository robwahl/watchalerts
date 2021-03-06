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
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Globalization;

namespace Kinovea.Services
{
	/// <summary>
	/// A class to encapsulate the user's preferences.
	/// There is really two kind of preferences handled here:
	/// - The filesystem independant preferences (language, flags, etc.)
	/// - The filesystem dependant ones. (file history, shortcuts, etc.)
	/// 
	/// File system independant are stored in preferences.xml
	/// others are handled through .NET settings framework.
	/// TODO: homogenize ?
	/// 
	/// Adding a pref:
	/// - add a member (with its default value) + add the property.
	/// - add import and export in XML Helpers methods.
	/// </summary>
    public class PreferencesManager
    {
        #region Satic Properties
        public static string ReleaseVersion
        {
            // This prop is set as the first instruction of the RootKernel ctor.
            get{ return Properties.Settings.Default.Release; }
            set{ Properties.Settings.Default.Release = value;}
        }
        public static string SettingsFolder
        {
        	// Store settings in user space. 
        	// If it doesn't exist, this folder is created at startup.
        	get{ return m_AppdataFolder;}       
        }
        public static ResourceManager ResourceManager
        {
        	// FIXME: all folders should be accessed through real props.
            get { return Properties.Resources.ResourceManager; }
        }
        public static bool ExperimentalRelease
        {
            // Set in RootKernel ctor. Used to show/hide certain menus.
            get{ return Properties.Settings.Default.ExperimentalRelease; }
            set{ Properties.Settings.Default.ExperimentalRelease = value;}
        }
        public static string DefaultCaptureImageFile = "Capture";
        public static string DefaultCaptureVideoFile = "Capture";
        #endregion
        
        #region Properties (Preferences)
        public int HistoryCount
        {
            get { return m_iFilesToSave; }
            set { m_iFilesToSave = value;}
        }
        public string UICultureName
        {
            get { return m_UICultureName; }
            set 
            { 
            	m_UICultureName = value; 
            }
        }
        public TimeCodeFormat TimeCodeFormat
        {
            get { return m_TimeCodeFormat; }
            set { m_TimeCodeFormat = value; }
        }
        public SpeedUnits SpeedUnit
        {
            get { return m_SpeedUnit; }
            set { m_SpeedUnit = value; }
        }
        public ImageAspectRatio AspectRatio
		{
			get { return m_AspectRatio; }
			set { m_AspectRatio = value; }
		}
        public bool DeinterlaceByDefault
		{
			get { return m_bDeinterlaceByDefault; }
			set { m_bDeinterlaceByDefault = value; }
		}
        public int WorkingZoneSeconds
        {
            get { return m_iWorkingZoneSeconds; }
            set { m_iWorkingZoneSeconds = value; }
        }
        public int WorkingZoneMemory
        {
            get { return m_iWorkingZoneMemory; }
            set { m_iWorkingZoneMemory = value; }
        }
        public InfosFading DefaultFading
        {
            get { return m_DefaultFading; }
            set { m_DefaultFading = value; }
        }
		public int MaxFading
		{
			get { return m_iMaxFading; }
			set { m_iMaxFading = value; }
		}
        public bool DrawOnPlay
        {
            get { return m_bDrawOnPlay; }
            set { m_bDrawOnPlay = value; }
        }
        public bool ExplorerVisible
        {
            get { return m_bIsExplorerVisible; }
            set { m_bIsExplorerVisible = value;}
        }
        public int ExplorerSplitterDistance
        {
        	// Splitter between Explorer and ScreenManager
            get { return m_iExplorerSplitterDistance; }
            set { m_iExplorerSplitterDistance = value; }
        }
		public int ExplorerFilesSplitterDistance
        {
        	// Splitter between folders and files on Explorer tab
            get { return m_iExplorerFilesSplitterDistance; }
            set { m_iExplorerFilesSplitterDistance = value; }
        }
		public ExplorerThumbSizes ExplorerThumbsSize
		{
			// Size category of the thumbnails.
            get { return m_iExplorerThumbsSize; }
            set { m_iExplorerThumbsSize = value; }				
		}
		public int ShortcutsFilesSplitterDistance
        {
        	// Splitter between folders and files on Shortcuts tab
            get { return m_iShortcutsFilesSplitterDistance; }
            set { m_iShortcutsFilesSplitterDistance = value; }
        }
        public List<ShortcutFolder> ShortcutFolders
        {
        	// FIXME.
        	// we want the client of the prop to get a read only access.
        	// here we offer a reference on an internal objetc, he can call .Clear().
        	get{ return m_ShortcutFolders;}
        }
        public string LastBrowsedDirectory 
        {
			get 
			{ 
				return Properties.Settings.Default.BrowserDirectory; 
			}
			set 
			{ 
				Properties.Settings.Default.BrowserDirectory = value;
        		Properties.Settings.Default.Save();
			}
		}
        public ActiveFileBrowserTab ActiveTab 
        {
			get { return m_ActiveFileBrowserTab; }
			set { m_ActiveFileBrowserTab = value; }
		}
		public string CaptureImageDirectory 
        {
        	get { return m_CaptureImageDirectory; }
			set { m_CaptureImageDirectory = value; }
		}
        public string CaptureVideoDirectory 
        {
        	get { return m_CaptureVideoDirectory; }
			set { m_CaptureVideoDirectory = value; }
		}        
		public KinoveaImageFormat CaptureImageFormat
		{
			get { return m_CaptureImageFormat; }
			set { m_CaptureImageFormat = value; }
		}
		public KinoveaVideoFormat CaptureVideoFormat
		{
			get { return m_CaptureVideoFormat; }
			set { m_CaptureVideoFormat = value; }
		}
        public string CaptureImageFile
        {
        	get { return m_CaptureImageFile; }
			set { m_CaptureImageFile = value; }
        }
		public string CaptureVideoFile
        {
        	get { return m_CaptureVideoFile; }
			set { m_CaptureVideoFile = value; }
        }
		public bool CaptureUsePattern
		{
			get { return m_bCaptureUsePattern; }
			set { m_bCaptureUsePattern = value; }
		}
		public string CapturePattern
		{
			get { return m_CapturePattern; }
			set { m_CapturePattern = value; }
		}
		public long CaptureImageCounter
        {
        	get { return m_iCaptureImageCounter; }
            set { m_iCaptureImageCounter = value;}	
        }
		public long CaptureVideoCounter
        {
        	get { return m_iCaptureVideoCounter; }
            set { m_iCaptureVideoCounter = value;}	
        }
		public int CaptureMemoryBuffer
        {
            get { return m_iCaptureMemoryBuffer; }
            set { m_iCaptureMemoryBuffer = value; }
        }
        public List<Color> RecentColors
		{
        	// Fixme: ref to the internal object.
			get { return m_RecentColors; }
		}
        public List<DeviceConfiguration> DeviceConfigurations
        {
        	get { return m_DeviceConfigurations; }	
        }
		public string NetworkCameraUrl
		{
			get { return m_NetworkCameraUrl; }
			set { m_NetworkCameraUrl = value; }
		}      
		public NetworkCameraFormat NetworkCameraFormat
		{
			get { return m_NetworkCameraFormat; }
			set { m_NetworkCameraFormat = value; }
		}
		public List<string> RecentNetworkCameras
        {
        	get{ return m_RecentNetworkCameras;}
        }
		#endregion

        #region Members
        // Preferences
        private List<string> m_HistoryList = new List<string>();
        private int m_iFilesToSave = 5;
        private string m_UICultureName;		// The system's culture.
        private TimeCodeFormat m_TimeCodeFormat = TimeCodeFormat.ClassicTime;
        private SpeedUnits m_SpeedUnit = SpeedUnits.MetersPerSecond;
        private ImageAspectRatio m_AspectRatio = ImageAspectRatio.Auto;
        private bool m_bDeinterlaceByDefault;
        private int m_iWorkingZoneSeconds = 12;
        private int m_iWorkingZoneMemory = 512;
        private InfosFading m_DefaultFading = new InfosFading();
        private int m_iMaxFading = 200;
        private bool m_bDrawOnPlay = true;
        private bool m_bIsExplorerVisible = true;
        private int m_iExplorerSplitterDistance = 250;
        private int m_iExplorerFilesSplitterDistance = 350;
        private int m_iShortcutsFilesSplitterDistance = 350;
        private ExplorerThumbSizes m_iExplorerThumbsSize = ExplorerThumbSizes.Medium; 
        private static string m_AppdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Kinovea\\";
        private List<ShortcutFolder> m_ShortcutFolders = new List<ShortcutFolder>();
        private ActiveFileBrowserTab m_ActiveFileBrowserTab = ActiveFileBrowserTab.Explorer;
		private string m_CaptureImageDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string m_CaptureVideoDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private KinoveaImageFormat m_CaptureImageFormat = KinoveaImageFormat.JPG;
        private KinoveaVideoFormat m_CaptureVideoFormat = KinoveaVideoFormat.MKV;
        private string m_CaptureImageFile = "";
        private string m_CaptureVideoFile = "";
        private bool m_bCaptureUsePattern;
        private string m_CapturePattern = "Cap-%y-%mo-%d - %i";
        private long m_iCaptureImageCounter = 1;
        private long m_iCaptureVideoCounter = 1;
        private int m_iCaptureMemoryBuffer = 768;
        private List<Color> m_RecentColors = new List<Color>();
        private int m_iMaxRecentColors = 12;
        private List<DeviceConfiguration> m_DeviceConfigurations = new List<DeviceConfiguration>();
        private string m_NetworkCameraUrl = "http://localhost:8080/cam_1.jpg";
        private NetworkCameraFormat m_NetworkCameraFormat = NetworkCameraFormat.JPEG;
        private List<string> m_RecentNetworkCameras = new List<string>();
        private int m_iMaxRecentNetworkCameras = 5;
        
        // Helpers members
        private static PreferencesManager m_instance = null;
        
        private ToolStripMenuItem m_HistoryMenu;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion
        
        #region Constructor & Singleton
        public static PreferencesManager Instance()
        {
            if (m_instance == null)
            {
                m_instance = new PreferencesManager();
            }
            return m_instance;
        }
        private PreferencesManager()
        {
            // By default we use the System Language.
            // If it is not supported, it will fall back to English.
            m_UICultureName = Thread.CurrentThread.CurrentUICulture.Name;            
            log.Debug(String.Format("System Culture: [{0}].", m_UICultureName));
            Import();
            GetHistoryAsList();
            
            
        }
        #endregion

        #region Languages
        public CultureInfo GetSupportedCulture()
        {
        	// Returns the culture that is used throgought the UI.
        	CultureInfo ci = new CultureInfo(m_UICultureName);
        	if(LanguageManager.IsSupportedCulture(ci))
        		return ci;
        	else
        		return new CultureInfo("en");
        }
        #endregion
        
        #region Import/Export Public interface
        public void Export()
        {
            log.Debug("Exporting preferences.");
            FlushToDisk(m_AppdataFolder + Properties.Resources.ResourceManager.GetString("PreferencesFile"));
        }
        public void Import()
        {
            log.Debug("Importing preferences.");
            ParseConfigFile(m_AppdataFolder + Properties.Resources.ResourceManager.GetString("PreferencesFile"));
        }
        #endregion

        #region XML Helpers
        private void FlushToDisk(string filePath)
        {
            try
            {
                XmlTextWriter PreferencesWriter = new XmlTextWriter(filePath, null);
                PreferencesWriter.Formatting = Formatting.Indented;
                PreferencesWriter.WriteStartDocument();
                PreferencesWriter.WriteStartElement("KinoveaPreferences");

                // Format version
                PreferencesWriter.WriteStartElement("FormatVersion");
                PreferencesWriter.WriteString("1.2");
                PreferencesWriter.WriteEndElement();

                // Preferences
                PreferencesWriter.WriteElementString("HistoryCount", m_iFilesToSave.ToString());
                PreferencesWriter.WriteElementString("Language", m_UICultureName);
                PreferencesWriter.WriteElementString("TimeCodeFormat", m_TimeCodeFormat.ToString());
                PreferencesWriter.WriteElementString("SpeedUnit", m_SpeedUnit.ToString());
                PreferencesWriter.WriteElementString("ImageAspectRatio", m_AspectRatio.ToString());
                PreferencesWriter.WriteElementString("DeinterlaceByDefault", m_bDeinterlaceByDefault.ToString());
                PreferencesWriter.WriteElementString("WorkingZoneSeconds", m_iWorkingZoneSeconds.ToString());
                PreferencesWriter.WriteElementString("WorkingZoneMemory", m_iWorkingZoneMemory.ToString());

                //m_DefaultFading.ToXml(PreferencesWriter, true);
                PreferencesWriter.WriteElementString("MaxFading", m_iMaxFading.ToString());
                
                PreferencesWriter.WriteElementString("DrawOnPlay", m_bDrawOnPlay.ToString());
                PreferencesWriter.WriteElementString("ExplorerThumbnailsSize", m_iExplorerThumbsSize.ToString());
                PreferencesWriter.WriteElementString("ExplorerVisible", m_bIsExplorerVisible.ToString());
                PreferencesWriter.WriteElementString("ExplorerSplitterDistance", m_iExplorerSplitterDistance.ToString());
                PreferencesWriter.WriteElementString("ActiveFileBrowserTab", m_ActiveFileBrowserTab.ToString());
                PreferencesWriter.WriteElementString("ExplorerFilesSplitterDistance", m_iExplorerFilesSplitterDistance.ToString());
                PreferencesWriter.WriteElementString("ShortcutsFilesSplitterDistance", m_iShortcutsFilesSplitterDistance.ToString());
                
                if(m_ShortcutFolders.Count > 0)
                {
                	PreferencesWriter.WriteStartElement("Shortcuts");
	                foreach(ShortcutFolder sf in m_ShortcutFolders)
	                {
	                	sf.ToXml(PreferencesWriter);
	                }
	                PreferencesWriter.WriteEndElement();
                }
                
                if(m_RecentColors.Count > 0)
                {
                	PreferencesWriter.WriteStartElement("RecentColors");
	                foreach(Color col in m_RecentColors)
	                {
	                	PreferencesWriter.WriteStartElement("Color");
	                	PreferencesWriter.WriteString(col.R.ToString() + ";" + col.G.ToString() + ";" + col.B.ToString());
	                	PreferencesWriter.WriteEndElement();
	                }
	                PreferencesWriter.WriteEndElement();
                }
                
                if(m_CaptureImageDirectory != null && m_CaptureImageDirectory != "") 
                	PreferencesWriter.WriteElementString("CaptureImageDirectory", m_CaptureImageDirectory);
                if(m_CaptureImageDirectory != null && m_CaptureImageFile != "") 
                	PreferencesWriter.WriteElementString("CaptureImageFile", m_CaptureImageFile);
                if(m_CaptureImageDirectory != null && m_CaptureVideoDirectory != "") 
                	PreferencesWriter.WriteElementString("CaptureVideoDirectory", m_CaptureVideoDirectory);
                if(m_CaptureImageDirectory != null && m_CaptureVideoFile != "") 
                	PreferencesWriter.WriteElementString("CaptureVideoFile", m_CaptureVideoFile);
                PreferencesWriter.WriteElementString("CaptureImageFormat", m_CaptureImageFormat.ToString());
                PreferencesWriter.WriteElementString("CaptureVideoFormat", m_CaptureVideoFormat.ToString());
                
                PreferencesWriter.WriteElementString("CaptureUsePattern", m_bCaptureUsePattern.ToString());
                PreferencesWriter.WriteElementString("CapturePattern", m_CapturePattern);
                PreferencesWriter.WriteElementString("CaptureImageCounter", m_iCaptureImageCounter.ToString());
                PreferencesWriter.WriteElementString("CaptureVideoCounter", m_iCaptureVideoCounter.ToString());
                
                PreferencesWriter.WriteElementString("CaptureMemoryBuffer", m_iCaptureMemoryBuffer.ToString());
                
                if(m_DeviceConfigurations.Count > 0)
                {
                	PreferencesWriter.WriteStartElement("DeviceConfigurations");
	                foreach(DeviceConfiguration conf in m_DeviceConfigurations)
	                {
	                	conf.ToXml(PreferencesWriter);
	                }
	                PreferencesWriter.WriteEndElement();
                }
                
                // Network cameras : url, format, list of recent url.
                PreferencesWriter.WriteElementString("NetworkCameraUrl", m_NetworkCameraUrl);
                PreferencesWriter.WriteElementString("NetworkCameraFormat", m_NetworkCameraFormat.ToString());
                if(m_RecentNetworkCameras.Count > 0)
                {
                	PreferencesWriter.WriteStartElement("RecentNetworkCameras");
	                foreach(string url in m_RecentNetworkCameras)
	                {
	                	PreferencesWriter.WriteStartElement("NetworkCamera");
	                	PreferencesWriter.WriteString(url);
	                	PreferencesWriter.WriteEndElement();
	                }
	                PreferencesWriter.WriteEndElement();
                }
                
                PreferencesWriter.WriteEndElement();
                PreferencesWriter.WriteEndDocument();
                PreferencesWriter.Flush();
                PreferencesWriter.Close();
            }
            catch(Exception)
            {
                log.Error("Error happenned while writing preferences.");
            }
        }
        private void ParseConfigFile(string filePath)
        {
            // Fill the local variables with infos found in the XML file.
            XmlReader PreferencesReader = new XmlTextReader(filePath);

            if (PreferencesReader != null)
            {
                try
                {
                    while (PreferencesReader.Read())
                    {
                        if ((PreferencesReader.IsStartElement()) && (PreferencesReader.Name == "KinoveaPreferences"))
                        {
                            while (PreferencesReader.Read())
                            {
                                if (PreferencesReader.IsStartElement())
                                {
                                    switch (PreferencesReader.Name)
                                    {
                                        case "HistoryCount":
                                            m_iFilesToSave = int.Parse(PreferencesReader.ReadString());
                                            break;
                                        case "Language":
                                            m_UICultureName = PreferencesReader.ReadString();
                                            break;
                                        case "TimeCodeFormat":
                                            m_TimeCodeFormat = ParseTimeCodeFormat(PreferencesReader.ReadString());
                                            break;
                                        case "SpeedUnit":
                                            m_SpeedUnit = ParseSpeedUnit(PreferencesReader.ReadString());
                                            break;
                                        case "ImageAspectRatio":
                                            m_AspectRatio = ParseImageAspectRatio(PreferencesReader.ReadString());
                                            break;
                                        case "DeinterlaceByDefault":
                                            m_bDeinterlaceByDefault = bool.Parse(PreferencesReader.ReadString());
                                            break;
                                        case "WorkingZoneSeconds":
                                            m_iWorkingZoneSeconds = int.Parse(PreferencesReader.ReadString());
                                            break;
                                        case "WorkingZoneMemory":
                                            m_iWorkingZoneMemory = int.Parse(PreferencesReader.ReadString());
                                            break;
                                        case "InfosFading":
                                            //m_DefaultFading.ReadXml(PreferencesReader);
                                            break;
                                        case "MaxFading":
                                            m_iMaxFading = int.Parse(PreferencesReader.ReadString());
                                            break;
                                        case "DrawOnPlay":
                                            m_bDrawOnPlay = bool.Parse(PreferencesReader.ReadString());
                                            break;
                                        case "ExplorerThumbnailsSize":
                                            m_iExplorerThumbsSize = (ExplorerThumbSizes)ExplorerThumbSizes.Parse(m_iExplorerThumbsSize.GetType(), PreferencesReader.ReadString());
                                            break;
                                        case "ExplorerVisible":
                                            m_bIsExplorerVisible = bool.Parse(PreferencesReader.ReadString());
                                            break;
                                        case "ExplorerSplitterDistance":
                                            m_iExplorerSplitterDistance = int.Parse(PreferencesReader.ReadString());
                                            break;
										case "ActiveFileBrowserTab":
                                            m_ActiveFileBrowserTab = (ActiveFileBrowserTab)ActiveFileBrowserTab.Parse(m_ActiveFileBrowserTab.GetType(), PreferencesReader.ReadString());
                                            break;
                                        case "ExplorerFilesSplitterDistance":
                                            m_iExplorerFilesSplitterDistance = int.Parse(PreferencesReader.ReadString());
                                            break;
                                        case "ShortcutsFilesSplitterDistance":
                                            m_iShortcutsFilesSplitterDistance = int.Parse(PreferencesReader.ReadString());
                                            break;
                                        case "Shortcuts":
                                            ParseShortcuts(PreferencesReader);
                                            break;
                                        case "RecentColors":
                                            ParseRecentColors(PreferencesReader);
                                            break;
										case "CaptureImageDirectory":
                                            m_CaptureImageDirectory = PreferencesReader.ReadString();
                                            break;
                                        case "CaptureImageFile":
                                            m_CaptureImageFile = PreferencesReader.ReadString();
                                            break;
                                        case "CaptureVideoDirectory":
                                            m_CaptureVideoDirectory = PreferencesReader.ReadString();
                                            break;
                                        case "CaptureVideoFile":
                                            m_CaptureVideoFile = PreferencesReader.ReadString();
                                            break;
                                        case "CaptureImageFormat":
                                            m_CaptureImageFormat = ParseImageFormat(PreferencesReader.ReadString());
                                            break;
                                        case "CaptureVideoFormat":
                                            m_CaptureVideoFormat = ParseVideoFormat(PreferencesReader.ReadString());
                                            break;
                                        case "CaptureUsePattern":
                                            m_bCaptureUsePattern = bool.Parse(PreferencesReader.ReadString());
                                            break;
                                        case "CapturePattern":
                                            m_CapturePattern = PreferencesReader.ReadString();
                                            break;
                                        case "CaptureImageCounter":
                                            m_iCaptureImageCounter = long.Parse(PreferencesReader.ReadString());
                                            break;
										case "CaptureVideoCounter":
                                            m_iCaptureVideoCounter = long.Parse(PreferencesReader.ReadString());
                                            break;
                                        case "CaptureMemoryBuffer":
                                            m_iCaptureMemoryBuffer = int.Parse(PreferencesReader.ReadString());
                                            break;
										case "DeviceConfigurations":
                                            ParseDeviceConfigurations(PreferencesReader);
                                            break;
                                        case "NetworkCameraUrl":
                                            m_NetworkCameraUrl = PreferencesReader.ReadString();
                                            break;
                                        case "NetworkCameraFormat":
                                            m_NetworkCameraFormat = ParseCameraFormat(PreferencesReader.ReadString());
                                            break;
                                        case "RecentNetworkCameras":
                                            ParseRecentCameras(PreferencesReader);
                                            break;
                                        default:
                                            // Preference from a newer file format...
                                            // We don't have a holder variable for it.
                                            break;
                                    }
                                }
                                else if (PreferencesReader.Name == "KinoveaPreferences")
                                {
                                    break;
                                }
                                else
                                {
                                    // Fermeture d'un tag interne.
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    log.Error("Error happenned while parsing preferences. We'll keep the default values.");
                }
                finally
                {
                    PreferencesReader.Close();
                }
            }
        }
        private TimeCodeFormat ParseTimeCodeFormat(string _format)
        {
            TimeCodeFormat tcf;

            // cannot use a switch, a constant value is expected.

            if(_format.Equals(TimeCodeFormat.ClassicTime.ToString()))
            {
                tcf = TimeCodeFormat.ClassicTime;
            }
            else if (_format.Equals(TimeCodeFormat.Frames.ToString()))
            {
                tcf = TimeCodeFormat.Frames;
            }
            else if (_format.Equals(TimeCodeFormat.Milliseconds.ToString()))
            {
                tcf = TimeCodeFormat.Frames;
            }
            else if (_format.Equals(TimeCodeFormat.TenThousandthOfHours.ToString()))
            {
                tcf = TimeCodeFormat.TenThousandthOfHours;
            }
            else if (_format.Equals(TimeCodeFormat.HundredthOfMinutes.ToString()))
            {
                tcf = TimeCodeFormat.HundredthOfMinutes;
            }
            else if (_format.Equals(TimeCodeFormat.Timestamps.ToString()))
            {
                tcf = TimeCodeFormat.Timestamps;
            }
            else if (_format.Equals(TimeCodeFormat.TimeAndFrames.ToString()))
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
        private SpeedUnits ParseSpeedUnit(string _format)
        {
            SpeedUnits su;

            // cannot use a switch, a constant value is expected.

            if(_format.Equals(SpeedUnits.MetersPerSecond.ToString()))
            {
                su = SpeedUnits.MetersPerSecond;
            }
            else if (_format.Equals(SpeedUnits.KilometersPerHour.ToString()))
            {
                su = SpeedUnits.KilometersPerHour;
            }
            else if (_format.Equals(SpeedUnits.FeetPerSecond.ToString()))
            {
                su = SpeedUnits.FeetPerSecond;
            }
            else if (_format.Equals(SpeedUnits.MilesPerHour.ToString()))
            {
                su = SpeedUnits.MilesPerHour;
            }
            else if (_format.Equals(SpeedUnits.Knots.ToString()))
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
        private ImageAspectRatio ParseImageAspectRatio(string _format)
        {
        	ImageAspectRatio iar;

            // cannot use a switch, a constant value is expected.

            if(_format.Equals(ImageAspectRatio.Auto.ToString()))
            {
                iar = ImageAspectRatio.Auto;
            }
            else if (_format.Equals(ImageAspectRatio.Force169.ToString()))
            {
                iar = ImageAspectRatio.Force169;
            }
            else if (_format.Equals(ImageAspectRatio.Force43.ToString()))
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
        private void ParseShortcuts(XmlReader _xmlReader)
        {
        	m_ShortcutFolders.Clear();
        	
        	while (_xmlReader.Read())
            {
                if (_xmlReader.IsStartElement())
                {
                    if (_xmlReader.Name == "Shortcut")
                    {
                    	ShortcutFolder sf = ShortcutFolder.FromXml(_xmlReader);
                    	if(sf != null)
                    	{
                    		m_ShortcutFolders.Add(sf);
                    	}
                    }
                    else
                    {
                        // forward compatibility : ignore new fields. 
                    }
                }
                else if (_xmlReader.Name == "Shortcuts")
                {
                    break;
                }
                else
                {
                    // Fermeture d'un tag interne.
                }
        	}    	
        }
        private void ParseRecentColors(XmlReader _xmlReader)
        {
        	m_RecentColors.Clear();
        	
        	while (_xmlReader.Read())
            {
                if (_xmlReader.IsStartElement())
                {
                    if (_xmlReader.Name == "Color")
                    {
                    	Color col = XmlHelper.ParseColor(_xmlReader.ReadString());
                    	m_RecentColors.Add(col);
                    }
                    else
                    {
                        // forward compatibility : ignore new fields. 
                    }
                }
                else if (_xmlReader.Name == "RecentColors")
                {
                    break;
                }
                else
                {
                    // Fermeture d'un tag interne.
                }
        	}    	
        }
        private void ParseDeviceConfigurations(XmlReader _xmlReader)
        {
        	m_DeviceConfigurations.Clear();
        	
        	while (_xmlReader.Read())
            {
                if (_xmlReader.IsStartElement())
                {
                    if (_xmlReader.Name == "DeviceConfiguration")
                    {
                    	DeviceConfiguration conf = DeviceConfiguration.FromXml(_xmlReader);
                    	if(conf != null)
                    	{
                    		m_DeviceConfigurations.Add(conf);
                    	}
                    }
                    else
                    {
                        // forward compatibility : ignore new fields. 
                    }
                }
                else if (_xmlReader.Name == "DeviceConfigurations")
                {
                    break;
                }
                else
                {
                    // Fermeture d'un tag interne.
                }
        	}    	
        }
        private KinoveaImageFormat ParseImageFormat(string _format)
        {
        	KinoveaImageFormat output = KinoveaImageFormat.JPG;
        	try
        	{
        		output = (KinoveaImageFormat)Enum.Parse(KinoveaImageFormat.JPG.GetType(), _format);
        	}
        	catch(Exception)
        	{
        		log.ErrorFormat("Image format parsing failed. Use default value");
        	}
			
            return output;	
        }
        private KinoveaVideoFormat ParseVideoFormat(string _format)
        {
        	KinoveaVideoFormat output = KinoveaVideoFormat.MKV;
        	try
        	{
        		output = (KinoveaVideoFormat)Enum.Parse(KinoveaVideoFormat.MKV.GetType(), _format);
        	}
        	catch(Exception)
        	{
        		log.ErrorFormat("Video format parsing failed. Use default value");
        	}
			
            return output;
        }
        private NetworkCameraFormat ParseCameraFormat(string _format)
        {
        	NetworkCameraFormat output = NetworkCameraFormat.JPEG;
        	try
        	{
        		output = (NetworkCameraFormat)Enum.Parse(NetworkCameraFormat.JPEG.GetType(), _format);
        	}
        	catch(Exception)
        	{
        		log.ErrorFormat("Network camera format parsing failed. Use default value");
        	}
			
            return output;
        }
        private void ParseRecentCameras(XmlReader _xmlReader)
        {
        	m_RecentNetworkCameras.Clear();
        	
        	while (_xmlReader.Read())
            {
                if (_xmlReader.IsStartElement())
                {
                    if (_xmlReader.Name == "NetworkCamera")
                    {
                    	m_RecentNetworkCameras.Add(_xmlReader.ReadString());
                    }
                    else
                    {
                        // forward compatibility : ignore new fields. 
                    }
                }
                else if (_xmlReader.Name == "RecentNetworkCameras")
                {
                    break;
                }
                else
                {
                    // Fermeture d'un tag interne.
                }
        	}    	
        }
        #endregion

        #region Local files target.
        public void RegisterHistoryMenu(ToolStripMenuItem historyMenu)
        {
            this.m_HistoryMenu = historyMenu;
        }
        public void OrganizeHistoryMenu()
        {
            // History sub menus configuration: Only display non empty entries.
            bool atLeastOne = false;
            for (int i = 0; i < m_HistoryMenu.DropDownItems.Count-2; i++)
            {
                if (!string.IsNullOrEmpty(m_HistoryList[i]) && i < m_iFilesToSave)
                {
                    m_HistoryMenu.DropDownItems[i].Text = Path.GetFileName(m_HistoryList[i]);
                    m_HistoryMenu.DropDownItems[i].Visible = true;
                    atLeastOne = true;
                }
                else
                {
                    m_HistoryMenu.DropDownItems[i].Visible = false;
                }
            }

            // Separator & reset.
            m_HistoryMenu.DropDownItems[m_HistoryMenu.DropDownItems.Count - 2].Visible = atLeastOne;
            m_HistoryMenu.DropDownItems[m_HistoryMenu.DropDownItems.Count - 1].Visible = atLeastOne;
            m_HistoryMenu.Enabled = atLeastOne; 
        }
        private void GetHistoryAsList()
        {
            // Get history in a list to ease read/write operations.
            m_HistoryList.Clear();

            // Remembered files.
            m_HistoryList.Add(Properties.Settings.Default.HistoryVideo1);
            m_HistoryList.Add(Properties.Settings.Default.HistoryVideo2);
            m_HistoryList.Add(Properties.Settings.Default.HistoryVideo3);
            m_HistoryList.Add(Properties.Settings.Default.HistoryVideo4);
            m_HistoryList.Add(Properties.Settings.Default.HistoryVideo5);
            m_HistoryList.Add(Properties.Settings.Default.HistoryVideo6);
            m_HistoryList.Add(Properties.Settings.Default.HistoryVideo7);
            m_HistoryList.Add(Properties.Settings.Default.HistoryVideo8);
            m_HistoryList.Add(Properties.Settings.Default.HistoryVideo9);
            m_HistoryList.Add(Properties.Settings.Default.HistoryVideo10);
            
        }
        private void PutListAsHistory()
        {
            // Items older than max number have already been set to empty string.
            Properties.Settings.Default.HistoryVideo1 = m_HistoryList[0];
            Properties.Settings.Default.HistoryVideo2 = m_HistoryList[1];
            Properties.Settings.Default.HistoryVideo3 = m_HistoryList[2];
            Properties.Settings.Default.HistoryVideo4 = m_HistoryList[3];
            Properties.Settings.Default.HistoryVideo5 = m_HistoryList[4];
            Properties.Settings.Default.HistoryVideo6 = m_HistoryList[5];
            Properties.Settings.Default.HistoryVideo7 = m_HistoryList[6];
            Properties.Settings.Default.HistoryVideo8 = m_HistoryList[7];
            Properties.Settings.Default.HistoryVideo9 = m_HistoryList[8];
            Properties.Settings.Default.HistoryVideo10 = m_HistoryList[9];
            Properties.Settings.Default.Save();
        }
        public void HistoryReset()
        {
            for(int i = 0; i<m_HistoryList.Count;i++) 
                m_HistoryList[i] = "";

            PutListAsHistory();
        }
        public void HistoryAdd( string file)
        {
            int knownIndex = -1;
            
            // Check if we already know it.
            for (int i = 0; i < m_iFilesToSave; i++)
            {
                if (m_HistoryList[i] == file)
                    knownIndex = i;
            }

            if (knownIndex < 0)
            {
                // Shift all entries back one spot to make room for the new one.
                for (int i = m_iFilesToSave - 1; i > 0; i--)
                    m_HistoryList[i] = m_HistoryList[i - 1];
            }
            else
            {
                // Only shift entries that were newer.
                for (int i = knownIndex; i > 0; i--)
                    m_HistoryList[i] = m_HistoryList[i - 1];
            }
            
            m_HistoryList[0] = file;
            PutListAsHistory();
        }
        public string GetFilePathAtIndex(int index)
        {
            return m_HistoryList[index]; 
        }
        #endregion
    
    	#region Misc
    	public void AddRecentColor(Color _color)
    	{
    		// Check if we already have it in the list.
    		int found = -1;
    		for(int i=0; i<m_RecentColors.Count; i++)
    		{
    			if(_color.Equals(m_RecentColors[i]))
    			{
    				found = i;
    				break;
    			}
    		}
    		
    		if(found >= 0)
    		{
    			m_RecentColors.RemoveAt(found);
    		}
    		else if(m_RecentColors.Count == m_iMaxRecentColors)
    		{
    			m_RecentColors.RemoveAt(m_RecentColors.Count - 1);
    		}
    		
    		m_RecentColors.Insert(0, _color);
    		
    		// Maybe the export should be of the caller's responsibility ?
    		Export();
    	}
    	public void UpdateSelectedCapability(string _id, DeviceCapability _cap)
    	{
    		// Check if we already know this device, update it or create it. 
    		bool deviceFound = false;
    		foreach(DeviceConfiguration conf in m_DeviceConfigurations)
    		{
    			if(conf.id == _id)
    			{
    				// Update the device config.
    				deviceFound = true;
    				conf.cap = new DeviceCapability(_cap.FrameSize, _cap.Framerate);
    			}
    		}
    		
    		if(!deviceFound)
    		{
    			// Create the device conf.
    			DeviceConfiguration conf = new DeviceConfiguration();
    			conf.id = _id;
    			conf.cap = new DeviceCapability(_cap.FrameSize, _cap.Framerate);
    			m_DeviceConfigurations.Add(conf);
    		}
    		
    		Export();
    	}
    	public string GetImageFormat()
    	{
    		string format = ".jpg";
    		
    		switch(m_CaptureImageFormat)
    		{
    			case KinoveaImageFormat.PNG:
    				format = ".png";
    				break;
    			case KinoveaImageFormat.BMP:
    				format = ".bmp";
    				break;
    			default :
    				format = ".jpg";
    				break;
    		}
    		return format;
    	}
    	public string GetVideoFormat()
    	{
    		string format = ".mkv";
    		
    		switch(m_CaptureVideoFormat)
    		{
    			case KinoveaVideoFormat.MP4:
    				format = ".mp4";
    				break;
    			case KinoveaVideoFormat.AVI:
    				format = ".avi";
    				break;
    			default :
    				format = ".mkv";
    				break;
    		}
    		return format;
    	}
    	public void AddRecentCamera(string _url)
    	{
    		// Check if we already know about it.
    		int found = -1;
    		for(int i=0; i<m_RecentNetworkCameras.Count; i++)
    		{
    			if(m_RecentNetworkCameras[i] == _url)
    			{
    				found = i;
    				break;
    			}
    		}
    		
    		// Remove it where we found it or remove the oldest one if max reached.
    		if(found >= 0)
    		{
    			m_RecentNetworkCameras.RemoveAt(found);
    		}
    		else if(m_RecentNetworkCameras.Count == m_iMaxRecentNetworkCameras)
    		{
    			m_RecentNetworkCameras.RemoveAt(m_RecentNetworkCameras.Count - 1);
    		}
    		
    		// Insert it on top.
    		m_RecentNetworkCameras.Insert(0, _url);
    		
    		// Export is the responsibility of the caller.
    	}
    	#endregion
    }
    
    #region namespace wide enums
        
    /// <summary>
	/// Timecode formats.
	/// The preferences combo box must keep this order.
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
	/// Standards speed units.
	/// </summary>
	public enum SpeedUnits
	{
		MetersPerSecond,
		KilometersPerHour,
		FeetPerSecond,
		MilesPerHour,
		Knots,
		PixelsPerFrame,			// Native unit. 
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
	/// Last active tab.
	/// Must keep the same ordering as in FileBrowserUI.
	/// </summary>
	public enum ActiveFileBrowserTab
    {
    	Explorer = 0,
    	Shortcuts
    }
	
	/// <summary>
	/// Size of the thumbnails in the explorer.
	/// Sizes are expressed in number of thumbnails that should fit in the width of the explorer.
	/// the actual size of any given thumbnail will change depending on the available space.
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
		JPG,
		PNG,
		BMP
	};
    public enum KinoveaVideoFormat
	{
		MKV,
		MP4,
		AVI
	};
    public enum NetworkCameraFormat
    {
    	JPEG,
    	MJPEG
    };
	#endregion

	public class DeviceConfiguration
	{
		public string id;
		public DeviceCapability cap;
		
		public void ToXml(XmlTextWriter _writer)
		{
			_writer.WriteStartElement("DeviceConfiguration");
			
			_writer.WriteStartElement("Identification");
        	_writer.WriteString(id);
        	_writer.WriteEndElement();
        	
        	_writer.WriteStartElement("Size");
        	_writer.WriteString(cap.FrameSize.Width.ToString() + ";" + cap.FrameSize.Height.ToString());
        	_writer.WriteEndElement();
        	
        	_writer.WriteStartElement("Framerate");
        	_writer.WriteString(cap.Framerate.ToString());
        	_writer.WriteEndElement();
        	
        	_writer.WriteEndElement();
		}
		public static DeviceConfiguration FromXml(XmlReader _xmlReader)
		{
			string id = "";
			Size frameSize = Size.Empty;
			int frameRate = 0;
			
			while (_xmlReader.Read())
            {
                if (_xmlReader.IsStartElement())
                {
                    if (_xmlReader.Name == "Identification")
                    {
                        id = _xmlReader.ReadString();
                    }
                    else if(_xmlReader.Name == "Size")
                    {
                    	Point p = XmlHelper.ParsePoint(_xmlReader.ReadString());
                    	frameSize = new Size(p);
                    }
                    else if(_xmlReader.Name == "Framerate")
                    {
                    	frameRate = int.Parse(_xmlReader.ReadString());
                    }
                }
                else if (_xmlReader.Name == "DeviceConfiguration")
                {
                    break;
                }
                else
                {
                    // Fermeture d'un tag interne.
                }
			}
			
			DeviceConfiguration conf = null;
			if(id.Length > 0)
			{
				conf = new DeviceConfiguration();
				conf.id = id;
				conf.cap = new DeviceCapability(frameSize, frameRate);
			}
			
			return conf;
		}
	}
}
