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

using Kinovea.Services.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;

namespace Kinovea.Services
{
    public class ApplicationInfos
    {
        public string ChangelogLocation;
        public string FileLocation;
        public int FileSizeInBytes;
        public ThreePartsVersion Version;
    }

    public class LangGroup
    {
        public List<HelpItem> Items;
        public List<string> ItemTypes;
        public string Lang;
    }
    
    [ComVisible(visibility: true)]
    public class HelpIndex
    {
        #region Init

        private void Init()
        {
            AppInfos = new ApplicationInfos();
            UserGuides = new List<HelpItem>();
            HelpVideos = new List<HelpItem>();
            LoadSuccess = true;
        }

        #endregion Init

        #region Members

        public ApplicationInfos AppInfos;
        public List<HelpItem> UserGuides;
        public List<HelpItem> HelpVideos;
        public bool LoadSuccess;

        private readonly XmlTextReader _mXmlReader;

        #endregion Members

        #region Construction

        public HelpIndex()
        {
            // Used for writing the conf to file.
            Init();
        }

        public HelpIndex(string filePath)
        {
            // Used to read conf from file.
            Init();

            // If we can't
            try
            {
                _mXmlReader = new XmlTextReader(filePath);
                ParseConfigFile();
            }
            catch (Exception)
            {
                LoadSuccess = false;
            }
        }

        #endregion Construction

        #region Parsing

        private void ParseConfigFile()
        {
            //-----------------------------------------------------------
            // Fill the local variables with infos found in the XML file.
            //-----------------------------------------------------------
            if (_mXmlReader != null)
            {
                try
                {
                    while (_mXmlReader.Read())
                    {
                        if ((_mXmlReader.IsStartElement()) && (_mXmlReader.Name == "kinovea"))
                        {
                            while (_mXmlReader.Read())
                            {
                                if (_mXmlReader.IsStartElement())
                                {
                                    if (_mXmlReader.Name == "software")
                                    {
                                        AppInfos = ParseAppInfos();
                                    }

                                    if (_mXmlReader.Name == "lang")
                                    {
                                        ParseHelpItems();
                                    }
                                }
                                else if (_mXmlReader.Name == "kinovea")
                                {
                                    break;
                                }
                            }
                        }
                    }
                    LoadSuccess = true;
                }
                catch (Exception)
                {
                    // Une erreur est survenue pendant le parsing.
                    LoadSuccess = false;
                }
                finally
                {
                    _mXmlReader.Close();
                }
            }
        }

        private ApplicationInfos ParseAppInfos()
        {
            var ai = new ApplicationInfos
            {
                Version = new ThreePartsVersion(_mXmlReader.GetAttribute("release"))
            };

            while (_mXmlReader.Read())
            {
                if (_mXmlReader.IsStartElement())
                {
                    if (_mXmlReader.Name == "filesize")
                    {
                        ai.FileSizeInBytes = int.Parse(_mXmlReader.ReadString());
                    }

                    if (_mXmlReader.Name == "location")
                    {
                        ai.FileLocation = _mXmlReader.ReadString();
                    }

                    if (_mXmlReader.Name == "changelog")
                    {
                        ai.ChangelogLocation = _mXmlReader.ReadString();
                    }
                }
                else if (_mXmlReader.Name == "software")
                {
                    break;
                }
            }

            return ai;
        }

        private void ParseHelpItems()
        {
            var lang = _mXmlReader.GetAttribute("id");

            while (_mXmlReader.Read())
            {
                if (_mXmlReader.IsStartElement())
                {
                    if (_mXmlReader.Name == "manual")
                    {
                        var hi = ParseHelpItem(lang, _mXmlReader.Name);
                        UserGuides.Add(hi);
                    }

                    if (_mXmlReader.Name == "video")
                    {
                        var hi = ParseHelpItem(lang, _mXmlReader.Name);
                        HelpVideos.Add(hi);
                    }
                }
                else if (_mXmlReader.Name == "lang")
                {
                    break;
                }
            }
        }

        private HelpItem ParseHelpItem(string lang, string tag)
        {
            var hi = new HelpItem();

            if (_mXmlReader != null)
            {
                hi.Identification = int.Parse(_mXmlReader.GetAttribute("id"));
                hi.Revision = int.Parse(_mXmlReader.GetAttribute("revision"));
                hi.Language = lang;

                while (_mXmlReader.Read())
                {
                    if (_mXmlReader.IsStartElement())
                    {
                        if (_mXmlReader.Name == "title")
                        {
                            hi.LocalizedTitle = _mXmlReader.ReadString();
                        }
                        if (_mXmlReader.Name == "filesize")
                        {
                            hi.FileSizeInBytes = int.Parse(_mXmlReader.ReadString());
                        }
                        if (_mXmlReader.Name == "location")
                        {
                            hi.FileLocation = _mXmlReader.ReadString();
                        }
                        if (_mXmlReader.Name == "comment")
                        {
                            hi.Comment = _mXmlReader.ReadString();
                        }
                    }
                    else if (_mXmlReader.Name == tag)
                    {
                        break;
                    }
                }
            }

            return hi;
        }

        #endregion Parsing

        #region Update

        public void UpdateIndex(HelpItem helpItem, int listId)
        {
            //-----------------------------------------------------
            // Vérifier s'il existe déjà, mettre à jour ou ajouter.
            //-----------------------------------------------------

            // 1. Choix de la liste.
            List<HelpItem> hiList;
            string szDownloadFolder;
            if (listId == 0)
            {
                hiList = UserGuides;
                szDownloadFolder = Application.StartupPath + "\\" + Resources.ManualsFolder;
            }
            else
            {
                hiList = HelpVideos;
                szDownloadFolder = Application.StartupPath + "\\" + Resources.HelpVideosFolder;
            }

            // 2. Recherche de l'Item.
            var found = false;
            var i = 0;
            while (!found && i < hiList.Count)
            {
                if (helpItem.Identification == hiList[i].Identification && helpItem.Language == hiList[i].Language)
                {
                    found = true;
                    // Mise à jour.
                    UpdateHelpItem(hiList[i], helpItem, szDownloadFolder);
                }
                else
                {
                    i++;
                }
            }

            if (!found)
            {
                // Ajout.
                var hiNew = new HelpItem();
                UpdateHelpItem(hiNew, helpItem, szDownloadFolder);
                hiList.Add(hiNew);
            }
        }

        private void UpdateHelpItem(HelpItem hiLocalCopy, HelpItem hiUpdatedCopy, string szFolder)
        {
            // rempli plus tard dynamiquement : _hiLocalCopy.Description
            hiLocalCopy.FileLocation = szFolder + "\\" + Path.GetFileName(hiUpdatedCopy.FileLocation);
            hiLocalCopy.FileSizeInBytes = hiUpdatedCopy.FileSizeInBytes;
            hiLocalCopy.Identification = hiUpdatedCopy.Identification;
            hiLocalCopy.Language = hiUpdatedCopy.Language;
            hiLocalCopy.LocalizedTitle = hiUpdatedCopy.LocalizedTitle;
            hiLocalCopy.Revision = hiUpdatedCopy.Revision;
            hiLocalCopy.Comment = hiUpdatedCopy.Comment;
        }

        public void WriteToDisk()
        {
            try
            {
                var localHelpIndexWriter =
                    new XmlTextWriter(Application.StartupPath + "\\" + Resources.URILocalHelpIndex, null)
                    {
                        Formatting = Formatting.Indented
                    };
                localHelpIndexWriter.WriteStartDocument();

                localHelpIndexWriter.WriteStartElement("kinovea");
                localHelpIndexWriter.WriteStartElement("software");
                localHelpIndexWriter.WriteAttributeString("release", AppInfos.Version.ToString());
                localHelpIndexWriter.WriteString(" "); // placeholder necessary due to the parser algo.
                localHelpIndexWriter.WriteEndElement();

                // On retrie les items par langues.
                var langList = new List<LangGroup>();
                SortByLang(langList, UserGuides, "manual");
                SortByLang(langList, HelpVideos, "video");

                // Ajouter les groupes de langues
                foreach (var lg in langList)
                {
                    localHelpIndexWriter.WriteStartElement("lang");
                    localHelpIndexWriter.WriteAttributeString("id", lg.Lang);
                    for (var i = 0; i < lg.Items.Count; i++)
                    {
                        localHelpIndexWriter.WriteStartElement(lg.ItemTypes[i]);
                        localHelpIndexWriter.WriteAttributeString("id", lg.Items[i].Identification.ToString());
                        localHelpIndexWriter.WriteAttributeString("revision", lg.Items[i].Revision.ToString());

                        localHelpIndexWriter.WriteElementString("title", lg.Items[i].LocalizedTitle);
                        localHelpIndexWriter.WriteElementString("filesize", lg.Items[i].FileSizeInBytes.ToString());
                        localHelpIndexWriter.WriteElementString("location", lg.Items[i].FileLocation);
                        localHelpIndexWriter.WriteElementString("comment", lg.Items[i].Comment);

                        localHelpIndexWriter.WriteEndElement();
                    }
                    localHelpIndexWriter.WriteEndElement();
                }
                localHelpIndexWriter.WriteEndElement();
                localHelpIndexWriter.WriteEndDocument();
                localHelpIndexWriter.Flush();
                localHelpIndexWriter.Close();
            }
            catch (Exception)
            {
                // Possible cause: doesn't have rights to write.
            }
        }

        private void SortByLang(List<LangGroup> sortedList, List<HelpItem> inputList, string szItemType)
        {
            foreach (var item in inputList)
            {
                // Vérifier si la langue est connue
                var iLangIndex = -1;
                for (var i = 0; i < sortedList.Count; i++)
                {
                    if (item.Language == sortedList[i].Lang)
                    {
                        iLangIndex = i;
                    }
                }

                if (iLangIndex == -1)
                {
                    // ajouter l'item dans une nouvelle langue.
                    var lg = new LangGroup
                    {
                        Lang = item.Language,
                        Items = new List<HelpItem> { item },
                        ItemTypes = new List<string> { szItemType }
                    };
                    sortedList.Add(lg);
                }
                else
                {
                    // ajouter l'item dans sa langue.
                    sortedList[iLangIndex].Items.Add(item);
                    sortedList[iLangIndex].ItemTypes.Add(szItemType);
                }
            }
        }

        #endregion Update
    }

    class HelpIndexImpl : HelpIndex
    {
    }
}