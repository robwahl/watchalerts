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

using ICSharpCode.SharpZipLib.Zip;
using Kinovea.Services;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Xsl;

namespace Kinovea.ScreenManager
{
    public class Metadata
    {
        #region Properties

        public TimeCodeBuilder TimeStampsToTimecode { get; }

        public bool IsDirty
        {
            get
            {
                var iCurrentHash = GetHashCode();
                Log.Debug(string.Format("Reading hash for Metadata.IsDirty, Ref Hash:{0}, Current Hash:{1}",
                    _mILastCleanHash, iCurrentHash));
                return _mILastCleanHash != iCurrentHash;
            }
        }

        public string GlobalTitle { get; set; } = " ";

        public Size ImageSize
        {
            get { return _mImageSize; }
            set
            {
                _mImageSize.Width = value.Width;
                _mImageSize.Height = value.Height;
            }
        }

        public CoordinateSystem CoordinateSystem { get; } = private new CoordinateSystem();

        public string FullPath { get; set; }

        public Keyframe this[int index]
        {
            // Indexor
            get { return Keyframes[index]; }
            set { Keyframes[index] = value; }
        }

        public List<Keyframe> Keyframes { get; } = new List<Keyframe>();

        public int Count
        {
            get { return Keyframes.Count; }
        }

        public bool HasData
        {
            get
            {
                // This is used to know if there is anything to burn on the images when saving.
                // All kind of objects should be taken into account here, even those
                // that we currently don't save to the .kva but only draw on the image.
                // (grids, magnifier).
                var hasData =
                    (Keyframes.Count != 0) ||
                    (ExtraDrawings.Count > _mIStaticExtraDrawings) ||
                    //m_Plane.Visible ||
                    //m_Grid.Visible ||
                    (Magnifier.Mode != MagnifierMode.NotVisible);
                return hasData;
            }
        }

        public int SelectedDrawingFrame { get; set; } = -1;

        public int SelectedDrawing { get; set; } = -1;

        public List<AbstractDrawing> ExtraDrawings { get; } = new List<AbstractDrawing>();

        public int SelectedExtraDrawing { get; set; } = -1;

        public Magnifier Magnifier { get; set; } = private new Magnifier();

        public bool Mirrored { get; set; }

        // General infos
        public long AverageTimeStampsPerFrame { get; set; } = 1;

        public long FirstTimeStamp
        {
            //get { return m_iFirstTimeStamp; }
            set { _mIFirstTimeStamp = value; }
        }

        public long SelectionStart
        {
            //get { return m_iSelectionStart; }
            set { _mISelectionStart = value; }
        }

        public CalibrationHelper CalibrationHelper { get; set; } = private new CalibrationHelper();

        #endregion Properties

        #region Members

        private readonly ClosestFrameAction _mShowClosestFrameCallback;

        private readonly PreferencesManager _mPrefManager = PreferencesManager.Instance();

        // Drawings not attached to any key image.

        private int _mIStaticExtraDrawings;
            // TODO: might be removed when even Chronos and tracks are represented by a single manager object.

        private Size _mImageSize = new Size(0, 0);
        private long _mIFirstTimeStamp;
        private long _mISelectionStart;
        private int _mIDuplicateFactor = 1;
        private int _mILastCleanHash;

        // Read from XML, used for adapting the data to the current video
        private Size _mInputImageSize = new Size(0, 0);

        private long _mIInputAverageTimeStampsPerFrame; // The one read from the XML
        private long _mIInputFirstTimeStamp;
        private long _mIInputSelectionStart;
        private string _mInputFileName;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor

        public Metadata(TimeCodeBuilder timeStampsToTimecodeCallback, ClosestFrameAction showClosestFrameCallback)
        {
            TimeStampsToTimecode = timeStampsToTimecodeCallback;
            _mShowClosestFrameCallback = showClosestFrameCallback;

            InitExtraDrawingTools();

            Log.Debug("Constructing new Metadata object.");
            CleanupHash();
        }

        public Metadata(string kvaString, int iWidth, int iHeight, long iAverageTimestampPerFrame, string fullPath,
            TimeCodeBuilder timeStampsToTimecodeCallback, ClosestFrameAction showClosestFrameCallback)
            : this(timeStampsToTimecodeCallback, showClosestFrameCallback)
        {
            // Deserialization constructor
            _mImageSize = new Size(iWidth, iHeight);
            AverageTimeStampsPerFrame = iAverageTimestampPerFrame;
            FullPath = fullPath;

            Load(kvaString, false);
        }

        #endregion Constructor

        #region Public Interface

        #region Key images

        public void Clear()
        {
            Keyframes.Clear();
        }

        public void Add(Keyframe kf)
        {
            Keyframes.Add(kf);
        }

        public void Sort()
        {
            Keyframes.Sort();
        }

        public void RemoveAt(int index)
        {
            Keyframes.RemoveAt(index);
        }

        #endregion Key images

        public void AddChrono(DrawingChrono chrono)
        {
            chrono.ParentMetadata = this;
            ExtraDrawings.Add(chrono);
            SelectedExtraDrawing = ExtraDrawings.Count - 1;
        }

        public void AddTrack(Track track, ClosestFrameAction showClosestFrame, Color color)
        {
            track.ParentMetadata = this;
            track.Status = TrackStatus.Edit;
            track.MShowClosestFrame = showClosestFrame;
            track.MainColor = color;
            ExtraDrawings.Add(track);
            SelectedExtraDrawing = ExtraDrawings.Count - 1;
        }

        public bool HasTrack()
        {
            // Used for file menu to know if we can export to text.
            var hasTrack = false;
            foreach (AbstractDrawing ad in ExtraDrawings)
            {
                if (ad is Track)
                {
                    hasTrack = true;
                    break;
                }
            }
            return hasTrack;
        }

        public void Reset()
        {
            // Complete reset. (used when over loading a new video)
            Log.Debug("Metadata Reset.");

            GlobalTitle = "";
            _mImageSize = new Size(0, 0);
            _mInputImageSize = new Size(0, 0);
            if (FullPath != null)
            {
                if (FullPath.Length > 0)
                {
                    FullPath = "";
                }
            }
            AverageTimeStampsPerFrame = 1;
            _mIFirstTimeStamp = 0;
            _mIInputAverageTimeStampsPerFrame = 0;
            _mIInputFirstTimeStamp = 0;

            ResetCoreContent();
            CleanupHash();
        }

        public void UpdateTrajectoriesForKeyframes()
        {
            // Called when keyframe added, removed or title changed
            // => Updates the trajectories.
            foreach (AbstractDrawing ad in ExtraDrawings)
            {
                var t = ad as Track;
                if (t != null)
                {
                    t.IntegrateKeyframes();
                }
            }
        }

        public void AllDrawingTextToNormalMode()
        {
            foreach (var kf in Keyframes)
            {
                foreach (AbstractDrawing ad in kf.Drawings)
                {
                    if (ad is DrawingText)
                        ((DrawingText) ad).SetEditMode(false, null);
                }
            }
        }

        public void StopAllTracking()
        {
            foreach (AbstractDrawing ad in ExtraDrawings)
            {
                var t = ad as Track;
                if (t != null)
                {
                    t.StopTracking();
                }
            }
        }

        public void UpdateTrackPoint(Bitmap bmp)
        {
            // Happens when mouse up and editing a track.
            if (SelectedExtraDrawing > 0)
            {
                var t = ExtraDrawings[SelectedExtraDrawing] as Track;
                if (t != null && t.Status == TrackStatus.Edit)
                {
                    t.UpdateTrackPoint(bmp);
                }
            }
        }

        public void CleanupHash()
        {
            _mILastCleanHash = GetHashCode();
            Log.Debug(string.Format("Metadata hash reset. New reference hash is: {0}", _mILastCleanHash));
        }

        public override int GetHashCode()
        {
            // Combine all fields hashes, using XOR operator.
            //int iHashCode = GetKeyframesHashCode() ^ GetChronometersHashCode() ^ GetTracksHashCode();
            var iHashCode = GetKeyframesHashCode() ^ GetExtraDrawingsHashCode();
            return iHashCode;
        }

        public List<Bitmap> GetFullImages()
        {
            var images = new List<Bitmap>();
            foreach (var kf in Keyframes)
            {
                images.Add(kf.FullFrame);
            }
            return images;
        }

        public void ResizeFinished()
        {
            // This function is used to trigger an update to drawings and guides that do not
            // render in the same way when the user is resizing the window or not.
            // This is typically used for SVG Drawing, which take a long time to render themselves.
            foreach (var kf in Keyframes)
            {
                foreach (AbstractDrawing d in kf.Drawings)
                {
                    var svg = d as DrawingSvg;
                    if (svg != null)
                    {
                        svg.ResizeFinished();
                    }
                }
            }
        }

        #region Objects Hit Tests

        // Note: these hit tests are for right click only.
        // They work slightly differently than the hit test in the PointerTool which is for left click.
        // The main difference is that here we only need to know if the drawing was hit at all,
        // in the pointer tool, we need to differenciate which handle was hit.
        // For example, Tracks can here be handled with all other ExtraDrawings.
        public bool IsOnDrawing(int iActiveKeyframeIndex, Point mouseLocation, long iTimestamp)
        {
            // Returns whether the mouse is on a drawing attached to a key image.
            var bDrawingHit = false;

            if (_mPrefManager.DefaultFading.Enabled && Keyframes.Count > 0)
            {
                var zOrder = GetKeyframesZOrder(iTimestamp);

                for (var i = 0; i < zOrder.Length; i++)
                {
                    bDrawingHit = DrawingsHitTest(zOrder[i], mouseLocation, iTimestamp);
                    if (bDrawingHit)
                    {
                        break;
                    }
                }
            }
            else if (iActiveKeyframeIndex >= 0)
            {
                // If fading is off, only try the current keyframe (if any)
                bDrawingHit = DrawingsHitTest(iActiveKeyframeIndex, mouseLocation, iTimestamp);
            }

            return bDrawingHit;
        }

        public AbstractDrawing IsOnExtraDrawing(Point mouseLocation, long iTimestamp)
        {
            // Check if the mouse is on one of the drawings not attached to any key image.
            // Returns the drawing on which we stand (or null if none), and select it on the way.
            // the caller will then check its type and decide which action to perform.

            AbstractDrawing hitDrawing = null;

            for (var i = ExtraDrawings.Count - 1; i >= 0; i--)
            {
                int hitRes = ExtraDrawings[i].HitTest(mouseLocation, iTimestamp);
                if (hitRes >= 0)
                {
                    SelectedExtraDrawing = i;
                    hitDrawing = ExtraDrawings[i];
                    break;
                }
            }

            return hitDrawing;
        }

        public void UnselectAll()
        {
            SelectedDrawingFrame = -1;
            SelectedDrawing = -1;
            SelectedExtraDrawing = -1;
        }

        public int[] GetKeyframesZOrder(long iTimestamp)
        {
            // Get the Z ordering of Keyframes for hit tests & draw.

            var zOrder = new int[Keyframes.Count];

            if (Keyframes.Count > 0)
            {
                if (iTimestamp <= Keyframes[0].Position)
                {
                    // All key frames are after this position
                    for (var i = 0; i < Keyframes.Count; i++)
                    {
                        zOrder[i] = i;
                    }
                }
                else if (iTimestamp > Keyframes[Keyframes.Count - 1].Position)
                {
                    // All keyframes are before this position
                    for (var i = 0; i < Keyframes.Count; i++)
                    {
                        zOrder[i] = Keyframes.Count - i - 1;
                    }
                }
                else
                {
                    // Some keyframes are after, some before.
                    // Start at the first kf after this position until the end,
                    // then go backwards from the first kf before this position until the begining.

                    var iCurrentFrame = Keyframes.Count;
                    var iClosestNext = Keyframes.Count - 1;
                    while (iCurrentFrame > 0)
                    {
                        iCurrentFrame--;
                        if (Keyframes[iCurrentFrame].Position >= iTimestamp)
                        {
                            iClosestNext = iCurrentFrame;
                        }
                        else
                        {
                            break;
                        }
                    }

                    for (var i = iClosestNext; i < Keyframes.Count; i++)
                    {
                        zOrder[i - iClosestNext] = i;
                    }
                    for (var i = 0; i < iClosestNext; i++)
                    {
                        zOrder[Keyframes.Count - i - 1] = i;
                    }
                }
            }

            return zOrder;
        }

        #endregion Objects Hit Tests

        #endregion Public Interface

        #region Serialization

        #region Reading

        public void Load(string _kva, bool bIsFile)
        {
            // _kva parameter can either be a file or a string.
            StopAllTracking();
            UnselectAll();

            var kva = ConvertIfNeeded(_kva, bIsFile);

            var settings = new XmlReaderSettings();
            settings.IgnoreComments = true;
            settings.IgnoreProcessingInstructions = true;
            settings.IgnoreWhitespace = true;
            settings.CloseInput = true;

            XmlReader reader = null;
            if (bIsFile)
            {
                reader = XmlReader.Create(kva, settings);
            }
            else
            {
                reader = XmlReader.Create(new StringReader(kva), settings);
            }

            try
            {
                ReadXml(reader);
            }
            catch (Exception e)
            {
                Log.Error("An error happened during the parsing of the KVA metadata");
                Log.Error(e);
            }
            finally
            {
                if (reader != null) reader.Close();
            }

            UpdateTrajectoriesForKeyframes();
        }

        private string ConvertIfNeeded(string kva, bool bIsFile)
        {
            // _kva parameter can either be a filepath or the xml string. We return the same kind of string as passed in.
            var result = kva;

            var kvaDoc = new XmlDocument();
            if (bIsFile)
                kvaDoc.Load(kva);
            else
                kvaDoc.LoadXml(kva);

            var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Kinovea\\";
            var tempFile = folder + "\\temp.kva";
            var settings = new XmlWriterSettings();
            settings.Indent = true;

            var formatNode = kvaDoc.DocumentElement.SelectSingleNode("descendant::FormatVersion");
            double format;
            var read = double.TryParse(formatNode.InnerText, NumberStyles.Any, CultureInfo.InvariantCulture, out format);
            if (read)
            {
                if (format < 2.0 && format >= 1.3)
                {
                    Log.DebugFormat("Older format detected ({0}). Starting conversion", format);

                    try
                    {
                        var xslt = new XslCompiledTransform();
                        var stylesheet = Application.StartupPath + "\\xslt\\kva-1.5to2.0.xsl";
                        xslt.Load(stylesheet);

                        if (bIsFile)
                        {
                            using (var xw = XmlWriter.Create(tempFile, settings))
                            {
                                xslt.Transform(kvaDoc, xw);
                            }
                            result = tempFile;
                        }
                        else
                        {
                            var builder = new StringBuilder();
                            using (var xw = XmlWriter.Create(builder, settings))
                            {
                                xslt.Transform(kvaDoc, xw);
                            }
                            result = builder.ToString();
                        }

                        Log.DebugFormat("Older format converted.");
                    }
                    catch (Exception)
                    {
                        Log.ErrorFormat("An error occurred during KVA conversion. Conversion aborted.", format);
                    }
                }
                else if (format <= 1.2)
                {
                    Log.ErrorFormat("Format too old ({0}). No conversion will be attempted.", format);
                }
            }
            else
            {
                Log.ErrorFormat("The format couldn't be read. No conversion will be attempted. Read:{0}",
                    formatNode.InnerText);
            }

            return result;
        }

        private void ReadXml(XmlReader r)
        {
            Log.Debug("Importing Metadata from Kva XML.");

            r.MoveToContent();

            if (!(r.Name == "KinoveaVideoAnalysis"))
                return;

            r.ReadStartElement();
            r.ReadElementContentAsString("FormatVersion", "");

            while (r.NodeType == XmlNodeType.Element)
            {
                switch (r.Name)
                {
                    case "Producer":
                        r.ReadElementContentAsString();
                        break;

                    case "OriginalFilename":
                        _mInputFileName = r.ReadElementContentAsString();
                        break;

                    case "GlobalTitle":
                        GlobalTitle = r.ReadElementContentAsString();
                        break;

                    case "ImageSize":
                        var p = XmlHelper.ParsePoint(r.ReadElementContentAsString());
                        _mInputImageSize = new Size(p);
                        break;

                    case "AverageTimeStampsPerFrame":
                        _mIInputAverageTimeStampsPerFrame = r.ReadElementContentAsLong();
                        break;

                    case "FirstTimeStamp":
                        _mIInputFirstTimeStamp = r.ReadElementContentAsLong();
                        break;

                    case "SelectionStart":
                        _mIInputSelectionStart = r.ReadElementContentAsLong();
                        break;

                    case "DuplicationFactor":
                        _mIDuplicateFactor = r.ReadElementContentAsInt();
                        break;

                    case "CalibrationHelp":
                        ParseCalibrationHelp(r);
                        break;

                    case "Keyframes":
                        ParseKeyframes(r);
                        break;

                    case "Tracks":
                        ParseTracks(r);
                        break;

                    case "Chronos":
                        ParseChronos(r);
                        break;

                    default:
                        // We still need to properly skip the unparsed nodes.
                        var unparsed = r.ReadOuterXml();
                        Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }

            r.ReadEndElement();
        }

        private void ParseCalibrationHelp(XmlReader r)
        {
            r.ReadStartElement();

            while (r.NodeType == XmlNodeType.Element)
            {
                switch (r.Name)
                {
                    case "PixelToUnit":
                        var fPixelToUnit = double.Parse(r.ReadElementContentAsString(), CultureInfo.InvariantCulture);
                        CalibrationHelper.PixelToUnit = fPixelToUnit;
                        break;

                    case "LengthUnit":
                        var enumConverter = TypeDescriptor.GetConverter(typeof (LengthUnits));
                        CalibrationHelper.CurrentLengthUnit =
                            (LengthUnits) enumConverter.ConvertFromString(r.ReadElementContentAsString());
                        //m_CalibrationHelper.CurrentLengthUnit = (CalibrationHelper.LengthUnits)int.Parse(r.ReadElementContentAsString());
                        break;

                    case "CoordinatesOrigin":
                        // Note: we don't adapt to the destination image size. It makes little sense anyway.
                        CalibrationHelper.CoordinatesOrigin = XmlHelper.ParsePoint(r.ReadElementContentAsString());
                        break;

                    default:
                        var unparsed = r.ReadOuterXml();
                        Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }

            r.ReadEndElement();
        }

        private void ParseChronos(XmlReader r)
        {
            // TODO: catch empty tag <Chronos/>.

            r.ReadStartElement();

            while (r.NodeType == XmlNodeType.Element)
            {
                // When we have other Chrono tools (cadence tool), make this dynamic
                // on a similar model than for attached drawings. (see ParseDrawing())
                if (r.Name == "Chrono")
                {
                    var scaling = new PointF(1.0f, 1.0f);
                    if (!_mImageSize.IsEmpty && _mInputImageSize.Width != 0 && _mInputImageSize.Height != 0)
                    {
                        scaling.X = _mImageSize.Width/(float) _mInputImageSize.Width;
                        scaling.Y = _mImageSize.Height/(float) _mInputImageSize.Height;
                    }

                    var dc = new DrawingChrono(r, scaling, DoRemapTimestamp);

                    if (dc != null)
                        AddChrono(dc);
                }
                else
                {
                    var unparsed = r.ReadOuterXml();
                    Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                }
            }

            r.ReadEndElement();
        }

        private void ParseKeyframes(XmlReader r)
        {
            // TODO: catch empty tag <Keyframes/>.

            r.ReadStartElement();

            while (r.NodeType == XmlNodeType.Element)
            {
                if (r.Name == "Keyframe")
                {
                    ParseKeyframe(r);
                }
                else
                {
                    var unparsed = r.ReadOuterXml();
                    Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                }
            }

            r.ReadEndElement();
        }

        private void ParseKeyframe(XmlReader r)
        {
            // This will not create a fully functionnal Keyframe.
            // Must be followed by a call to PostImportMetadata()
            var kf = new Keyframe(this);

            r.ReadStartElement();

            while (r.NodeType == XmlNodeType.Element)
            {
                switch (r.Name)
                {
                    case "Position":
                        var iInputPosition = r.ReadElementContentAsInt();
                        kf.Position = DoRemapTimestamp(iInputPosition, false);
                        break;

                    case "Title":
                        kf.Title = r.ReadElementContentAsString();
                        break;

                    case "Comment":
                        kf.CommentRtf = r.ReadElementContentAsString();
                        break;

                    case "Drawings":
                        ParseDrawings(r, kf);
                        break;

                    default:
                        var unparsed = r.ReadOuterXml();
                        Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }

            r.ReadEndElement();

            // Merge: insert key frame at the right place or merge drawings if there's already a keyframe.
            var merged = false;
            for (var i = 0; i < Keyframes.Count; i++)
            {
                if (kf.Position < Keyframes[i].Position)
                {
                    Keyframes.Insert(i, kf);
                    merged = true;
                    break;
                }
                if (kf.Position == Keyframes[i].Position)
                {
                    foreach (AbstractDrawing ad in kf.Drawings)
                    {
                        Keyframes[i].Drawings.Add(ad);
                    }
                    merged = true;
                    break;
                }
            }

            if (!merged)
            {
                Keyframes.Add(kf);
            }
        }

        private void ParseDrawings(XmlReader r, Keyframe keyframe)
        {
            // TODO: catch empty tag <Drawings/>.

            r.ReadStartElement();

            while (r.NodeType == XmlNodeType.Element)
            {
                AbstractDrawing ad = ParseDrawing(r);

                if (ad != null)
                {
                    keyframe.Drawings.Insert(0, ad);
                    keyframe.Drawings[0].InfosFading.ReferenceTimestamp = keyframe.Position;
                    keyframe.Drawings[0].InfosFading.AverageTimeStampsPerFrame = AverageTimeStampsPerFrame;
                }
            }

            r.ReadEndElement();
        }

        private AbstractDrawing ParseDrawing(XmlReader r)
        {
            AbstractDrawing drawing = null;

            // Find the right class to instanciate.
            // The class must derive from AbstractDrawing and have the corresponding [XmlType] C# attribute.
            var drawingRead = false;
            var a = Assembly.GetExecutingAssembly();
            foreach (var t in a.GetTypes())
            {
                if (t.BaseType == typeof (AbstractDrawing))
                {
                    var attributes = t.GetCustomAttributes(typeof (XmlTypeAttribute), false);
                    if (attributes.Length > 0 && ((XmlTypeAttribute) attributes[0]).TypeName == r.Name)
                    {
                        // Verify that the drawing has a constructor with the right parameter list.
                        var ci = t.GetConstructor(new[] {typeof (XmlReader), typeof (PointF), GetType()});

                        if (ci != null)
                        {
                            var scaling = new PointF(1.0f, 1.0f);
                            if (!_mImageSize.IsEmpty && _mInputImageSize.Width != 0 && _mInputImageSize.Height != 0)
                            {
                                scaling.X = _mImageSize.Width/(float) _mInputImageSize.Width;
                                scaling.Y = _mImageSize.Height/(float) _mInputImageSize.Height;
                            }

                            // Instanciate the drawing.
                            object[] parameters = {r, scaling, this};
                            drawing = (AbstractDrawing) Activator.CreateInstance(t, parameters);

                            if (drawing != null)
                                drawingRead = true;
                        }

                        break;
                    }
                }
            }

            if (!drawingRead)
            {
                var unparsed = r.ReadOuterXml();
                Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
            }

            return drawing;
        }

        private void ParseTracks(XmlReader xmlReader)
        {
            // TODO: catch empty tag <Tracks/>.

            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                // When we have other Chrono tools (cadence tool), make this dynamic
                // on a similar model than for attached drawings. (see ParseDrawing())
                if (xmlReader.Name == "Track")
                {
                    var scaling = new PointF();
                    scaling.X = _mImageSize.Width/(float) _mInputImageSize.Width;
                    scaling.Y = _mImageSize.Height/(float) _mInputImageSize.Height;

                    var trk = new Track(xmlReader, scaling, DoRemapTimestamp, _mImageSize);

                    if (!trk.Invalid)
                    {
                        AddTrack(trk, _mShowClosestFrameCallback, trk.MainColor);
                        trk.Status = TrackStatus.Interactive;
                    }
                }
                else
                {
                    var unparsed = xmlReader.ReadOuterXml();
                    Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                }
            }

            xmlReader.ReadEndElement();
        }

        #endregion Reading

        #region Writing

        public string ToXmlString(int iDuplicateFactor)
        {
            // The duplicate factor is used in the context of extreme slow motion (causing the output to be less than 8fps).
            // In that case there is frame duplication and we store this information in the metadata when it is embedded in the file.
            // On input, it will be used to adjust the key images positions.
            // We change the global variable so it can be used during xml export, but it's only temporary.
            // It is possible that an already duplicated clip is further slowed down.
            var memoDuplicateFactor = _mIDuplicateFactor;
            _mIDuplicateFactor *= iDuplicateFactor;

            var settings = new XmlWriterSettings();
            settings.Indent = false;
            settings.CloseOutput = true;

            var builder = new StringBuilder();
            using (var w = XmlWriter.Create(builder, settings))
            {
                try
                {
                    WriteXml(w);
                }
                catch (Exception e)
                {
                    Log.Error("An error happened during the writing of the kva string");
                    Log.Error(e);
                }
            }

            _mIDuplicateFactor = memoDuplicateFactor;

            return builder.ToString();
        }

        public void ToXmlFile(string file)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.CloseOutput = true;

            using (var w = XmlWriter.Create(file, settings))
            {
                try
                {
                    WriteXml(w);
                    CleanupHash();
                }
                catch (Exception e)
                {
                    Log.Error("An error happened during the writing of the kva file");
                    Log.Error(e);
                }
            }
        }

        private void WriteXml(XmlWriter w)
        {
            // Convert the metadata to XML.
            // The XML Schema for the format should be available in the "tools/Schema/" folder of the source repository.

            // The format contains both core infos to deserialize back to Metadata and helpers data for XSLT exports,
            // so these exports have more user friendly values. (timecode vs timestamps, cm vs pixels, etc.)

            // Notes:
            // Doubles must be written with the InvariantCulture. ("1.52" not "1,52").
            // Booleans must be converted to proper XML Boolean type ("true" not "True").

            w.WriteStartElement("KinoveaVideoAnalysis");
            WriteGeneralInformation(w);

            // Keyframes
            if (ActiveKeyframes() > 0)
            {
                w.WriteStartElement("Keyframes");
                foreach (var kf in Keyframes)
                {
                    if (!kf.Disabled)
                    {
                        w.WriteStartElement("Keyframe");
                        kf.WriteXml(w);
                        w.WriteEndElement();
                    }
                }
                w.WriteEndElement();
            }

            // Chronos
            var atLeastOne = false;
            foreach (AbstractDrawing ad in ExtraDrawings)
            {
                var dc = ad as DrawingChrono;
                if (dc != null)
                {
                    if (atLeastOne == false)
                    {
                        w.WriteStartElement("Chronos");
                        atLeastOne = true;
                    }

                    w.WriteStartElement("Chrono");
                    dc.WriteXml(w);
                    w.WriteEndElement();
                }
            }
            if (atLeastOne)
            {
                w.WriteEndElement();
            }

            // Tracks
            atLeastOne = false;
            foreach (AbstractDrawing ad in ExtraDrawings)
            {
                var trk = ad as Track;
                if (trk != null)
                {
                    if (atLeastOne == false)
                    {
                        w.WriteStartElement("Tracks");
                        atLeastOne = true;
                    }

                    w.WriteStartElement("Track");
                    trk.WriteXml(w);
                    w.WriteEndElement();
                }
            }
            if (atLeastOne)
            {
                w.WriteEndElement();
            }

            w.WriteEndElement();
        }

        private void WriteGeneralInformation(XmlWriter w)
        {
            w.WriteElementString("FormatVersion", "2.0");
            w.WriteElementString("Producer", "Kinovea." + PreferencesManager.ReleaseVersion);
            w.WriteElementString("OriginalFilename", Path.GetFileNameWithoutExtension(FullPath));

            if (!string.IsNullOrEmpty(GlobalTitle))
                w.WriteElementString("GlobalTitle", GlobalTitle);

            w.WriteElementString("ImageSize", _mImageSize.Width + ";" + _mImageSize.Height);
            w.WriteElementString("AverageTimeStampsPerFrame", AverageTimeStampsPerFrame.ToString());
            w.WriteElementString("FirstTimeStamp", _mIFirstTimeStamp.ToString());
            w.WriteElementString("SelectionStart", _mISelectionStart.ToString());

            if (_mIDuplicateFactor > 1)
                w.WriteElementString("DuplicationFactor", _mIDuplicateFactor.ToString());

            // Calibration
            WriteCalibrationHelp(w);
        }

        private void WriteCalibrationHelp(XmlWriter w)
        {
            // TODO: Make Calbrabtion helper responsible for this.

            w.WriteStartElement("CalibrationHelp");

            w.WriteElementString("PixelToUnit", CalibrationHelper.PixelToUnit.ToString(CultureInfo.InvariantCulture));
            w.WriteStartElement("LengthUnit");
            w.WriteAttributeString("UserUnitLength", CalibrationHelper.GetLengthAbbreviation());

            var enumConverter = TypeDescriptor.GetConverter(typeof (LengthUnits));
            var unit = enumConverter.ConvertToString(CalibrationHelper.CurrentLengthUnit);
            w.WriteString(unit);

            w.WriteEndElement();
            w.WriteElementString("CoordinatesOrigin",
                string.Format("{0};{1}", CalibrationHelper.CoordinatesOrigin.X, CalibrationHelper.CoordinatesOrigin.Y));

            w.WriteEndElement();
        }

        #endregion Writing

        #endregion Serialization

        #region XSLT Export

        public void Export(string filePath, MetadataExportFormat format)
        {
            // Get current data as kva XML.
            var kvaString = ToXmlString(1);

            if (string.IsNullOrEmpty(kvaString))
            {
                Log.Error("Couldn't get metadata string. Aborting export.");
                return;
            }

            // Export the current meta data to spreadsheet doc through XSLT transform.
            var xslt = new XslCompiledTransform();
            var kvaDoc = new XmlDocument();
            kvaDoc.LoadXml(kvaString);
            var settings = new XmlWriterSettings();
            settings.Indent = true;

            switch (format)
            {
                case MetadataExportFormat.Odf:
                {
                    xslt.Load(Application.StartupPath + "\\xslt\\kva2odf-en.xsl");
                    ExportOdf(filePath, xslt, kvaDoc, settings);
                    break;
                }
                case MetadataExportFormat.Msxml:
                {
                    xslt.Load(Application.StartupPath + "\\xslt\\kva2msxml-en.xsl");
                    ExportXslt(filePath, xslt, kvaDoc, settings, false);
                    break;
                }
                case MetadataExportFormat.Xhtml:
                {
                    xslt.Load(Application.StartupPath + "\\xslt\\kva2xhtml-en.xsl");
                    settings.OmitXmlDeclaration = true;
                    ExportXslt(filePath, xslt, kvaDoc, settings, false);
                    break;
                }
                case MetadataExportFormat.Text:
                {
                    xslt.Load(Application.StartupPath + "\\xslt\\kva2txt-en.xsl");
                    ExportXslt(filePath, xslt, kvaDoc, null, true);
                    break;
                }
                default:
                    break;
            }
        }

        private void ExportOdf(string filePath, XslCompiledTransform xslt, XmlDocument xmlDoc,
            XmlWriterSettings settings)
        {
            // Transform kva to ODF's content.xml
            // and packs it into a proper .ods using zip compression.
            try
            {
                // Create archive.
                using (var zos = new ZipOutputStream(File.Create(filePath)))
                {
                    zos.UseZip64 = UseZip64.Dynamic;

                    // Content.xml (where the actual content is.)
                    var ms = new MemoryStream();
                    using (var xw = XmlWriter.Create(ms, settings))
                    {
                        xslt.Transform(xmlDoc, xw);
                    }

                    AddOdfZipFile(zos, "content.xml", ms.ToArray());

                    AddOdfZipFile(zos, "meta.xml", GetOdfMeta());
                    AddOdfZipFile(zos, "settings.xml", GetOdfSettings());
                    AddOdfZipFile(zos, "styles.xml", GetOdfStyles());

                    AddOdfZipFile(zos, "META-INF/manifest.xml", GetOdfManifest());
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception thrown during export to ODF.");
                ReportError(ex);
            }
        }

        private byte[] GetOdfMeta()
        {
            // Return the minimal xml file in a byte array so in can be written to zip.
            return GetMinimalOdf("office:document-meta");
        }

        private byte[] GetOdfStyles()
        {
            // Return the minimal xml file in a byte array so in can be written to zip.
            return GetMinimalOdf("office:document-styles");
        }

        private byte[] GetOdfSettings()
        {
            // Return the minimal xml file in a byte array so in can be written to zip.
            return GetMinimalOdf("office:document-settings");
        }

        private byte[] GetMinimalOdf(string element)
        {
            // Return the minimal xml data for required files
            // in a byte array so in can be written to zip.
            // A bit trickier than necessary because .NET StringWriter is UTF-16 and we want UTF-8.

            var ms = new MemoryStream();
            var xmlw = new XmlTextWriter(ms, new UTF8Encoding());
            xmlw.Formatting = Formatting.Indented;

            xmlw.WriteStartDocument();
            xmlw.WriteStartElement(element);
            xmlw.WriteAttributeString("xmlns", "office", null, "urn:oasis:names:tc:opendocument:xmlns:office:1.0");

            xmlw.WriteStartAttribute("office:version");
            xmlw.WriteString("1.1");
            xmlw.WriteEndAttribute();

            xmlw.WriteEndElement();
            xmlw.Flush();
            xmlw.Close();

            return ms.ToArray();
        }

        private byte[] GetOdfManifest()
        {
            // Return the minimal manifest.xml in a byte array so it can be written to zip.

            var ms = new MemoryStream();
            var xmlw = new XmlTextWriter(ms, new UTF8Encoding());
            xmlw.Formatting = Formatting.Indented;

            xmlw.WriteStartDocument();
            xmlw.WriteStartElement("manifest:manifest");
            xmlw.WriteAttributeString("xmlns", "manifest", null, "urn:oasis:names:tc:opendocument:xmlns:manifest:1.0");

            // Manifest itself
            xmlw.WriteStartElement("manifest:file-entry");
            xmlw.WriteStartAttribute("manifest:media-type");
            xmlw.WriteString("application/vnd.oasis.opendocument.spreadsheet");
            xmlw.WriteEndAttribute();
            xmlw.WriteStartAttribute("manifest:full-path");
            xmlw.WriteString("/");
            xmlw.WriteEndAttribute();
            xmlw.WriteEndElement();

            // Minimal set of files.
            OutputOdfManifestEntry(xmlw, "content.xml");
            OutputOdfManifestEntry(xmlw, "styles.xml");
            OutputOdfManifestEntry(xmlw, "meta.xml");
            OutputOdfManifestEntry(xmlw, "settings.xml");

            xmlw.WriteEndElement();
            xmlw.Flush();
            xmlw.Close();

            return ms.ToArray();
        }

        private void OutputOdfManifestEntry(XmlTextWriter xmlw, string file)
        {
            xmlw.WriteStartElement("manifest:file-entry");
            xmlw.WriteStartAttribute("manifest:media-type");
            xmlw.WriteString("text/xml");
            xmlw.WriteEndAttribute();
            xmlw.WriteStartAttribute("manifest:full-path");
            xmlw.WriteString(file);
            xmlw.WriteEndAttribute();
            xmlw.WriteEndElement();
        }

        private void AddOdfZipFile(ZipOutputStream zos, string file, byte[] data)
        {
            // Creates an entry in the ODF zip for a specific file, using the specific data.
            var entry = new ZipEntry(file);

            //entry.IsUnicodeText = false;
            entry.DateTime = DateTime.Now;
            entry.Size = data.Length;

            //Crc32 crc = new Crc32();
            //crc.Update(_data);
            //entry.Crc = crc.Value;

            zos.PutNextEntry(entry);
            zos.Write(data, 0, data.Length);
        }

        private void ExportXslt(string filePath, XslCompiledTransform xslt, XmlDocument kvaDoc,
            XmlWriterSettings settings, bool text)
        {
            try
            {
                if (text)
                {
                    using (var sw = new StreamWriter(filePath))
                    {
                        xslt.Transform(kvaDoc, null, sw);
                    }
                }
                else
                {
                    using (var xw = XmlWriter.Create(filePath, settings))
                    {
                        xslt.Transform(kvaDoc, xw);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception thrown during spreadsheet export.");
                ReportError(ex);
            }
        }

        private void ReportError(Exception ex)
        {
            // TODO: Error message the user, so at least he knows something went wrong !
            Log.Error(ex.Message);
            Log.Error(ex.Source);
            Log.Error(ex.StackTrace);
        }

        #endregion XSLT Export

        #region Lower level Helpers

        public long DoRemapTimestamp(long iInputTimestamp, bool bRelative)
        {
            //-----------------------------------------------------------------------------------------
            // In the general case:
            // The Input position was stored as absolute position, in the context of the original video.
            // It must be adapted in several ways:
            //
            // 1. Timestamps (TS) of first frames may differ.
            // 2. A selection might have been in place,
            //      in that case we use relative TS if different file and absolute TS if same file.
            // 3. TS might be expressed in completely different timebase.
            //
            // In the specific case of trajectories, the individual positions are stored relative to
            // the start of the trajectory.
            //-----------------------------------------------------------------------------------------

            // Vérifier qu'en arrivant ici on a bien :
            // le nom du fichier courant,
            // (on devrait aussi avoir le first ts courant mais on ne l'a pas.

            // le nom du fichier d'origine, le first ts d'origine, le ts de début  de selection d'origine.

            long iOutputTimestamp = 0;

            if (_mIInputAverageTimeStampsPerFrame != 0)
            {
                if ((_mIInputFirstTimeStamp != _mIFirstTimeStamp) ||
                    (_mIInputAverageTimeStampsPerFrame != AverageTimeStampsPerFrame) ||
                    (_mInputFileName != Path.GetFileNameWithoutExtension(FullPath)))
                {
                    //----------------------------------------------------
                    // Different contexts or different files.
                    // We use the relative positions and adapt the context
                    //----------------------------------------------------

                    // 1. Translate the input position into frame number (subject to rounding error)
                    // 2. Translate the frame number back into output position.
                    int iFrameNumber;

                    if (bRelative)
                    {
                        iFrameNumber = (int) (iInputTimestamp/_mIInputAverageTimeStampsPerFrame);
                        iFrameNumber *= _mIDuplicateFactor;
                        iOutputTimestamp = (int) (iFrameNumber*AverageTimeStampsPerFrame);
                    }
                    else
                    {
                        if (_mIInputSelectionStart - _mIInputFirstTimeStamp > 0)
                        {
                            // There was a selection.
                            iFrameNumber =
                                (int) ((iInputTimestamp - _mIInputSelectionStart)/_mIInputAverageTimeStampsPerFrame);
                            iFrameNumber *= _mIDuplicateFactor;
                        }
                        else
                        {
                            iFrameNumber =
                                (int) ((iInputTimestamp - _mIInputFirstTimeStamp)/_mIInputAverageTimeStampsPerFrame);
                            iFrameNumber *= _mIDuplicateFactor;
                        }

                        iOutputTimestamp = (int) (iFrameNumber*AverageTimeStampsPerFrame) + _mIFirstTimeStamp;
                    }
                }
                else
                {
                    //--------------------
                    // Same context.
                    //--------------------
                    iOutputTimestamp = iInputTimestamp;
                }
            }
            else
            {
                // hmmm ?
                iOutputTimestamp = iInputTimestamp;
            }

            return iOutputTimestamp;
        }

        private void ResetCoreContent()
        {
            // Semi reset: we keep Image size and AverageTimeStampsPerFrame
            Keyframes.Clear();
            StopAllTracking();
            ExtraDrawings.RemoveRange(_mIStaticExtraDrawings, ExtraDrawings.Count - _mIStaticExtraDrawings);
            Magnifier.ResetData();
            Mirrored = false;
            UnselectAll();
        }

        private bool DrawingsHitTest(int iKeyFrameIndex, Point mouseLocation, long iTimestamp)
        {
            //----------------------------------------------------------
            // Look for a hit in all drawings of a particular Key Frame.
            // The drawing being hit becomes Selected.
            //----------------------------------------------------------
            var bDrawingHit = false;
            var kf = Keyframes[iKeyFrameIndex];
            var hitRes = -1;
            var iCurrentDrawing = 0;

            while (hitRes < 0 && iCurrentDrawing < kf.Drawings.Count)
            {
                hitRes = kf.Drawings[iCurrentDrawing].HitTest(mouseLocation, iTimestamp);
                if (hitRes >= 0)
                {
                    bDrawingHit = true;
                    SelectedDrawing = iCurrentDrawing;
                    SelectedDrawingFrame = iKeyFrameIndex;
                }
                else
                {
                    iCurrentDrawing++;
                }
            }

            return bDrawingHit;
        }

        private int ActiveKeyframes()
        {
            var iTotalActive = Keyframes.Count;

            for (var i = 0; i < Keyframes.Count; i++)
            {
                if (Keyframes[i].Disabled)
                    iTotalActive--;
            }

            return iTotalActive;
        }

        private int GetKeyframesHashCode()
        {
            // Keyframes hashcodes are XORed with one another.
            var iHashCode = 0;
            foreach (var kf in Keyframes)
            {
                iHashCode ^= kf.GetHashCode();
            }
            return iHashCode;
        }

        private int GetExtraDrawingsHashCode()
        {
            var iHashCode = 0;
            foreach (AbstractDrawing ad in ExtraDrawings)
            {
                iHashCode ^= ad.GetHashCode();
            }
            return iHashCode;
        }

        private void InitExtraDrawingTools()
        {
            // Add the static extra drawing tools to the list of drawings.
            // These drawings are unique and not attached to any particular key image.
            // It could be proxy drawings, like SpotlightManager.

            // [0.8.16] - This function currently doesn't do anything as the Grids have been moved to attached drawings.
            // It is kept nevertheless because it will be needed for SpotlightManager and others.

            //m_ExtraDrawings.Add(m_Plane);
            _mIStaticExtraDrawings = ExtraDrawings.Count;
        }

        #endregion Lower level Helpers
    }

    public enum MetadataExportFormat
    {
        Odf,
        Msxml,
        Xhtml,
        Text
    }
}