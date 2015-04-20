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

using log4net;
using System;
using System.Reflection;
using System.Xml;

namespace Kinovea.Services
{
    /// <summary>
    ///     This class encapsulate fading / persistence infos and utilities.
    ///     It is used by all drawings to delegate the computing of the opacity factor.
    ///     Each drawing instance has its own InfosFading with its own set of internal values.
    /// </summary>
    public class InfosFading
    {
        public double GetOpacityFactor(long iTimestamp)
        {
            double fOpacityFactor;

            if (!Enabled)
            {
                // No fading.
                fOpacityFactor = iTimestamp == ReferenceTimestamp ? 1.0f : 0.0f;
            }
            else if (UseDefault)
            {
                // Default value
                var info = PreferencesManager.Instance().DefaultFading;
                fOpacityFactor = info.AlwaysVisible
                    ? 1.0f
                    : ComputeOpacityFactor(ReferenceTimestamp, iTimestamp, info.FadingFrames);
            }
            else if (AlwaysVisible)
            {
                // infinite fading. (= persisting drawing)
                fOpacityFactor = 1.0f;
            }
            else
            {
                // Custom value.
                fOpacityFactor = ComputeOpacityFactor(ReferenceTimestamp, iTimestamp, FadingFrames);
            }

            return fOpacityFactor * _mFMasterFactor;
        }

        public bool IsVisible(long iRefTimestamp, long iTestTimestamp, int iVisibleFrames)
        {
            // Is a given point visible at all ?
            // Currently used by trajectory in focus mode to check for kf labels visibility.

            return ComputeOpacityFactor(iRefTimestamp, iTestTimestamp, iVisibleFrames) > 0;
        }

        private double ComputeOpacityFactor(long iRefTimestamp, long iTestTimestamp, long iFadingFrames)
        {
            double fOpacityFactor;

            var iDistanceTimestamps = Math.Abs(iTestTimestamp - iRefTimestamp);
            var iFadingTimestamps = iFadingFrames * AverageTimeStampsPerFrame;

            if (iDistanceTimestamps > iFadingTimestamps)
            {
                fOpacityFactor = 0.0f;
            }
            else
            {
                fOpacityFactor = 1.0f - (iDistanceTimestamps / (double)iFadingTimestamps);
            }

            return fOpacityFactor;
        }

        #region Properties

        public bool Enabled { get; set; }

        public bool UseDefault { get; set; }

        public bool AlwaysVisible { get; set; }

        public int FadingFrames { get; set; }

        public long ReferenceTimestamp { get; set; }

        public long AverageTimeStampsPerFrame { get; set; }

        public float MasterFactor
        {
            get { return _mFMasterFactor; }
            set { _mFMasterFactor = value; }
        }

        #endregion Properties

        #region Members

        private float _mFMasterFactor = 1.0f;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Construction

        public InfosFading()
        {
            // this constructor is directly used only by the Preference manager
            // to create the default fading values.

            Enabled = true;
            UseDefault = true;
            AlwaysVisible = false;
            FadingFrames = 20;
            ReferenceTimestamp = 0;
            AverageTimeStampsPerFrame = 0;
            _mFMasterFactor = 1.0f;
        }

        public InfosFading(long iReferenceTimestamp, long iAverageTimeStampsPerFrame)
        {
            // This constructor is used by all drawings to get the default values.
            FromInfosFading(PreferencesManager.Instance().DefaultFading);
            ReferenceTimestamp = iReferenceTimestamp;
            AverageTimeStampsPerFrame = iAverageTimeStampsPerFrame;
        }

        #endregion Construction

        #region Import / Export / Clone

        public InfosFading Clone()
        {
            var clone = new InfosFading(ReferenceTimestamp, AverageTimeStampsPerFrame);
            clone.FromInfosFading(this);
            return clone;
        }

        public void FromInfosFading(InfosFading origin)
        {
            Enabled = origin.Enabled;
            UseDefault = origin.UseDefault;
            AlwaysVisible = origin.AlwaysVisible;
            FadingFrames = origin.FadingFrames;
            ReferenceTimestamp = origin.ReferenceTimestamp;
            AverageTimeStampsPerFrame = origin.AverageTimeStampsPerFrame;
            MasterFactor = origin.MasterFactor;
        }

        public void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("Enabled", Enabled ? "true" : "false");
            xmlWriter.WriteElementString("Frames", FadingFrames.ToString());
            xmlWriter.WriteElementString("AlwaysVisible", AlwaysVisible ? "true" : "false");
            xmlWriter.WriteElementString("UseDefault", UseDefault ? "true" : "false");
        }

        public void ReadXml(XmlReader xmlReader)
        {
            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "Enabled":
                        Enabled = XmlHelper.ParseBoolean(xmlReader.ReadElementContentAsString());
                        break;

                    case "Frames":
                        FadingFrames = xmlReader.ReadElementContentAsInt();
                        break;

                    case "UseDefault":
                        UseDefault = XmlHelper.ParseBoolean(xmlReader.ReadElementContentAsString());
                        break;

                    case "AlwaysVisible":
                        AlwaysVisible = XmlHelper.ParseBoolean(xmlReader.ReadElementContentAsString());
                        break;

                    default:
                        var unparsed = xmlReader.ReadOuterXml();
                        Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }

            xmlReader.ReadEndElement();

            // Sanity check.
            if (FadingFrames < 1) FadingFrames = 1;
        }

        #endregion Import / Export / Clone
    }
}