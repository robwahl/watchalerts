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

using AForge.Imaging.Filters;
using Kinovea.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Kinovea.ScreenManager
{
    public class Keyframe : IComparable
    {
        #region Members

        private readonly string _mTitle = "";

        #endregion Members

        #region IComparable Implementation

        public int CompareTo(object obj)
        {
            if (obj is Keyframe)
            {
                return Position.CompareTo(((Keyframe) obj).Position);
            }
            throw new ArgumentException("Impossible comparison");
        }

        #endregion IComparable Implementation

        #region LowerLevel Helpers

        public Bitmap ConvertToJpg(Bitmap image)
        {
            // Intermediate MemoryStream for the conversion.
            var memStr = new MemoryStream();

            //Get the list of available encoders
            var codecs = ImageCodecInfo.GetImageEncoders();

            //find the encoder with the image/jpeg mime-type
            ImageCodecInfo ici = null;
            foreach (var codec in codecs)
            {
                if (codec.MimeType == "image/jpeg")
                {
                    ici = codec;
                }
            }

            if (ici != null)
            {
                //Create a collection of encoder parameters (we only need one in the collection)
                var ep = new EncoderParameters();

                //We'll store images at 90% quality as compared with the original
                ep.Param[0] = new EncoderParameter(Encoder.Quality, (long) 90);

                image.Save(memStr, ici, ep);
            }
            else
            {
                // No JPG encoder found (is that common ?) Use default system.
                image.Save(memStr, ImageFormat.Jpeg);
            }

            return new Bitmap(memStr);
        }

        #endregion LowerLevel Helpers

        #region Properties

        public long Position { get; set; } = -1;

        public Bitmap Thumbnail { get; private set;
            //set { m_Thumbnail = value; }
        }

        public Bitmap DisabledThumbnail { get; set; }

        public List<AbstractDrawing> Drawings { get; } = new List<AbstractDrawing>();

        public Bitmap FullFrame { get; set; }

        public string CommentRtf { get; }

        /// <summary>
        ///     The title of a keyframe is dynamic.
        ///     It is the timecode until the user actually manually changes it.
        /// </summary>
        public string Title
        {
            get
            {
                if (_mTitle != null)
                {
                    if (_mTitle.Length > 0)
                    {
                        return _mTitle;
                    }
                    return TimeCode;
                }
                return TimeCode;
            }
            set
            {
                _mTitle = value;
                ParentMetadata.UpdateTrajectoriesForKeyframes();
            }
        }

        public string TimeCode { get; } = "";

        public bool Disabled { get; set; }

        public Metadata ParentMetadata { get;
// unused.
            set; }

        #endregion Properties

        #region Constructor

        public Keyframe(Metadata parentMetadata)
        {
            // Used only during parsing to hold dummy Keyframe while it is loaded.
            // Must be followed by a call to PostImportMetadata()
            ParentMetadata = parentMetadata;
        }

        public Keyframe(long position, string timecode, Bitmap image, Metadata parentMetadata)
        {
            // Title is a variable default.
            // as long as it's null, it takes the value of timecode.
            // which is updated when selection change.
            // as soon as the user put value in title, we use it instead.
            Position = position;
            TimeCode = timecode;
            Thumbnail = new Bitmap(image, 100, 75);
            FullFrame = ConvertToJpg(image);
            ParentMetadata = parentMetadata;
        }

        #endregion Constructor

        #region Public Interface

        public void ImportImage(Bitmap image)
        {
            Thumbnail = new Bitmap(image, 100, 75);
            FullFrame = ConvertToJpg(image);
        }

        public void GenerateDisabledThumbnail()
        {
            DisabledThumbnail = Grayscale.CommonAlgorithms.BT709.Apply(Thumbnail);
        }

        public void AddDrawing(DrawingSvg obj)
        {
            // insert to the top of z-order
            if (obj != null) Drawings.Insert(0, obj);
        }

        public void WriteXml(XmlWriter w)
        {
            w.WriteStartElement("Position");
            var userTime = ParentMetadata.TimeStampsToTimecode(Position, TimeCodeFormat.Unknown, false);
            w.WriteAttributeString("UserTime", userTime);
            w.WriteString(Position.ToString());
            w.WriteEndElement();

            if (!string.IsNullOrEmpty(Title))
                w.WriteElementString("Title", Title);

            if (!string.IsNullOrEmpty(CommentRtf))
                w.WriteElementString("Comment", CommentRtf);

            if (Drawings.Count > 0)
            {
                w.WriteStartElement("Drawings");
                foreach (AbstractDrawing drawing in Drawings)
                {
                    var serializableDrawing = drawing as IKvaSerializable;
                    if (serializableDrawing != null)
                    {
                        // The XML name for this drawing should be stored in its [XMLType] C# attribute.
                        var t = serializableDrawing.GetType();
                        var attributes = t.GetCustomAttributes(typeof (XmlTypeAttribute), false);

                        if (attributes.Length > 0)
                        {
                            var xmlName = ((XmlTypeAttribute) attributes[0]).TypeName;

                            w.WriteStartElement(xmlName);
                            serializableDrawing.WriteXml(w);
                            w.WriteEndElement();
                        }
                    }
                }
                w.WriteEndElement();
            }
        }

        public override int GetHashCode()
        {
            // Combine (XOR) all hash code for drawings, then comments, then title.

            var iHashCode = 0;
            foreach (AbstractDrawing drawing in Drawings)
            {
                iHashCode ^= drawing.GetHashCode();
            }

            if (CommentRtf != null)
            {
                iHashCode ^= CommentRtf.GetHashCode();
            }

            if (_mTitle != null)
            {
                iHashCode ^= _mTitle.GetHashCode();
            }

            iHashCode ^= TimeCode.GetHashCode();

            return iHashCode;
        }

        #endregion Public Interface
    }
}