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

using System.Collections.Generic;
using System.Globalization;

namespace Kinovea.Services
{
    /// <summary>
    ///     Description of LanguageManager.
    /// </summary>
    public static class LanguageManager
    {
        private static Dictionary<string, string> _mLanguages;

        public static Dictionary<string, string> Languages
        {
            get
            {
                if (ReferenceEquals(_mLanguages, null))
                    Initialize();

                return _mLanguages;
            }
        }

        public static void Initialize()
        {
            // Alphabetical order by native name.
            _mLanguages = new Dictionary<string, string>
            {
                {"de", "Deutsch"},
                {"el", "Ελληνικά"},
                {"en", "English"},
                {"es", "Español"},
                {"fr", "Français"},
                {"it", "Italiano"},
                {"lt", "Lietuvių"},
                {"nl", "Nederlands"},
                {"no", "Norsk"},
                {"pl", "Polski"},
                {"pt", "Português"},
                {"ro", "Română"},
                {"fi", "Suomi"},
                {"sv", "Svenska"},
                {"tr", "Türkçe"},
                {"zh-CHS", "简体中文"}
            };
        }

        public static bool IsSupportedCulture(CultureInfo ci)
        {
            var neutral = ci.IsNeutralCulture ? ci.Name : ci.Parent.Name;
            return Languages.ContainsKey(neutral);
        }

        #region Languages accessors by english name

        // This big list of static properties is to support language names in the credits box.
        // We should have a GetContributors method here instead ?
        public static string English
        {
            get { return Languages["en"]; }
        }

        public static string Dutch
        {
            get { return Languages["nl"]; }
        }

        public static string German
        {
            get { return Languages["de"]; }
        }

        public static string Portuguese
        {
            get { return Languages["pt"]; }
        }

        public static string Spanish
        {
            get { return Languages["es"]; }
        }

        public static string Italian
        {
            get { return Languages["it"]; }
        }

        public static string Romanian
        {
            get { return Languages["ro"]; }
        }

        public static string Polish
        {
            get { return Languages["pl"]; }
        }

        public static string Finnish
        {
            get { return Languages["fi"]; }
        }

        public static string Norwegian
        {
            get { return Languages["no"]; }
        }

        public static string Chinese
        {
            get { return Languages["zh-CHS"]; }
        }

        public static string Turkish
        {
            get { return Languages["tr"]; }
        }

        public static string Greek
        {
            get { return Languages["el"]; }
        }

        public static string Lithuanian
        {
            get { return Languages["lt"]; }
        }

        public static string Swedish
        {
            get { return Languages["sv"]; }
        }

        #endregion Languages accessors by english name
    }
}