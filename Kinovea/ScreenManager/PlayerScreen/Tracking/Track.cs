#region License

/*
Copyright © Joan Charmant 2008-2011.
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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     A class to encapsulate track drawings.
    ///     Contains the list of points and the list of keyframes markers.
    ///     Handles the user actions, display modes and xml import/export.
    ///     The tracking itself is delegated to a Tracker class.
    ///     The trajectory can be in one of 3 views (complete traj, focused on a section, label).
    ///     And in one of two status (edit or interactive).
    ///     In Edit state: dragging the target moves the point's coordinates.
    ///     In Interactive state: dragging the target moves to the next point (in time).
    /// </summary>
    public class Track : AbstractDrawing, IDecorable
    {
        #region Delegates

        // To ask the UI to display the frame closest to selected pos.
        // used when moving the target in direct interactive mode.
        public ClosestFrameAction MShowClosestFrame;

        #endregion Delegates

        #region Properties

        public TrackView View { get; } = TrackView.Complete;

        public TrackStatus Status { get; set; } = TrackStatus.Edit;

        public TrackExtraData ExtraData
        {
            get { return _mTrackExtraData; }
            set
            {
                _mTrackExtraData = value;
                IntegrateKeyframes();
            }
        }

        public long BeginTimeStamp { get; private set; }

        public long EndTimeStamp { get; private set; } = long.MaxValue;

        public DrawingStyle DrawingStyle { get; private set; }

        public Color MainColor
        {
            get { return _mStyleHelper.Color; }
            set
            {
                _mStyleHelper.Color = value;
                _mMainLabel.BackColor = value;
            }
        }

        public string Label { get; set; } = "Label";

        public Metadata ParentMetadata
        {
            get { return _mParentMetadata; } // unused.
            set
            {
                _mParentMetadata = value;
                _mInfosFading.AverageTimeStampsPerFrame = _mParentMetadata.AverageTimeStampsPerFrame;
            }
        }

        public bool Untrackable { get; }

        public bool Invalid { get; private set; }

        // Fading is not modifiable from outside.
        public override InfosFading InfosFading
        {
            get { throw new NotImplementedException("Track, The method or operation is not implemented."); }
            set { throw new NotImplementedException("Track, The method or operation is not implemented."); }
        }

        public override DrawingCapabilities Caps
        {
            get { return DrawingCapabilities.None; }
        }

        public override List<ToolStripMenuItem> ContextMenu
        {
            get { return null; }
        }

        #endregion Properties

        #region Members

        // Current state.

        private TrackExtraData _mTrackExtraData = TrackExtraData.None;
        private int _mIMovingHandler = -1;

        // Tracker tool.
        private readonly AbstractTracker _mTracker;

        // Hardwired parameters.
        private const int MIDefaultCrossRadius = 4;

        private const int MIAllowedFramesOver = 12;
            // Number of frames over which the global fading spans (after end point).

        private const int MIFocusFadingFrames = 30; // Number of frames of the focus section.

        // Internal data.
        private readonly List<AbstractTrackPoint> _mPositions = new List<AbstractTrackPoint>();

        private readonly List<KeyframeLabel> _mKeyframesLabels = new List<KeyframeLabel>();

        private int _mITotalDistance; // This is used to normalize timestamps to a par scale with distances.
        private int _mICurrentPoint;

        // Decoration
        private readonly StyleHelper _mStyleHelper = new StyleHelper();

        private KeyframeLabel _mMainLabel = new KeyframeLabel();
        private readonly InfosFading _mInfosFading = new InfosFading(long.MaxValue, 1);
        private const int MIBaseAlpha = 224; // alpha of track in most cases.
        private const int MIAfterCurrentAlpha = 64; // alpha of track after the current point when in normal mode.
        private const int MIEditModeAlpha = 128; // alpha of track when in Edit mode.
        private const int MILabelFollowsTrackAlpha = 80; // alpha of track when in LabelFollows view.

        // Memorization poul
        private TrackView _mMemoTrackView;

        private string _mMemoLabel;
        private Metadata _mParentMetadata;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Constructor

        public Track(Point origin, long t, Bitmap currentImage, Size imageSize)
        {
            //-----------------------------------------------------------------------------------------
            // t is absolute time.
            // _bmp is the whole picture, if null it means we don't need it.
            // (Probably because we already have a few points that we are importing from xml.
            // In this case we'll only need the last frame to reconstruct the last point.)
            //-----------------------------------------------------------------------------------------

            // Create the first point
            if (currentImage != null)
            {
                _mTracker = new TrackerBlock2(currentImage.Width, currentImage.Height);
                AbstractTrackPoint atp = _mTracker.CreateTrackPoint(true, origin.X, origin.Y, 1.0f, 0, currentImage,
                    _mPositions);
                if (atp != null)
                    _mPositions.Add(atp);
                else
                    Untrackable = true;
            }
            else
            {
                // Happens when loading Metadata from file or demuxing.
                _mTracker = new TrackerBlock2(imageSize.Width, imageSize.Height);
                _mPositions.Add(_mTracker.CreateOrphanTrackPoint(origin.X, origin.Y, 0));
            }

            if (!Untrackable)
            {
                BeginTimeStamp = t;
                EndTimeStamp = t;
                _mMainLabel.SetAttach(origin, true);

                // We use the InfosFading utility to fade the track away.
                // The refererence frame will be the last point (at which fading start).
                // AverageTimeStampsPerFrame will be updated when we get the parent metadata.
                _mInfosFading.FadingFrames = MIAllowedFramesOver;
                _mInfosFading.UseDefault = false;
                _mInfosFading.Enabled = true;
            }

            // Decoration
            DrawingStyle = new DrawingStyle();
            DrawingStyle.Elements.Add("color", new StyleElementColor(Color.SeaGreen));
            DrawingStyle.Elements.Add("line size", new StyleElementLineSize(3));
            DrawingStyle.Elements.Add("track shape", new StyleElementTrackShape(TrackShape.Solid));
            _mStyleHelper.Color = Color.Black;
            _mStyleHelper.LineSize = 3;
            _mStyleHelper.TrackShape = TrackShape.Dash;
            BindStyle();

            _mStyleHelper.ValueChanged += mainStyle_ValueChanged;
        }

        public Track(XmlReader xmlReader, PointF scale, TimeStampMapper remapTimestampCallback, Size imageSize)
            : this(Point.Empty, 0, null, imageSize)
        {
            ReadXml(xmlReader, scale, remapTimestampCallback);
        }

        #endregion Constructor

        #region AbstractDrawing implementation

        public override void Draw(Graphics canvas, CoordinateSystem transformer, bool bSelected, long iCurrentTimestamp)
        {
            if (iCurrentTimestamp < BeginTimeStamp)
                return;

            // 0. Compute the fading factor.
            // Special case from other drawings:
            // ref frame is last point, and we only fade after it, not before.
            var fOpacityFactor = 1.0;
            if (Status == TrackStatus.Interactive && iCurrentTimestamp > EndTimeStamp)
            {
                _mInfosFading.ReferenceTimestamp = EndTimeStamp;
                fOpacityFactor = _mInfosFading.GetOpacityFactor(iCurrentTimestamp);
            }

            if (fOpacityFactor <= 0)
                return;

            _mICurrentPoint = FindClosestPoint(iCurrentTimestamp);

            // Draw various elements depending on combination of view and status.
            // The exact alpha at which the traj will be drawn will be decided in GetTrackPen().
            if (_mPositions.Count > 1)
            {
                // Key Images titles.
                if (Status == TrackStatus.Interactive && View != TrackView.Label)
                    DrawKeyframesTitles(canvas, fOpacityFactor, transformer);

                // Track.
                var first = GetFirstVisiblePoint();
                var last = GetLastVisiblePoint();
                if (Status == TrackStatus.Interactive && View == TrackView.Complete)
                {
                    DrawTrajectory(canvas, first, _mICurrentPoint, true, fOpacityFactor, transformer);
                    DrawTrajectory(canvas, _mICurrentPoint, last, false, fOpacityFactor, transformer);
                }
                else
                {
                    DrawTrajectory(canvas, first, last, false, fOpacityFactor, transformer);
                }
            }

            if (_mPositions.Count > 0)
            {
                // Track.
                if (fOpacityFactor == 1.0 && View != TrackView.Label)
                    DrawMarker(canvas, fOpacityFactor, transformer);

                // Search boxes. (only on edit)
                if ((Status == TrackStatus.Edit) && (fOpacityFactor == 1.0))
                    _mTracker.Draw(canvas, _mPositions[_mICurrentPoint].Point, transformer, _mStyleHelper.Color,
                        fOpacityFactor);

                // Main label.
                if (Status == TrackStatus.Interactive && View == TrackView.Label ||
                    Status == TrackStatus.Interactive && _mTrackExtraData != TrackExtraData.None)
                {
                    DrawMainLabel(canvas, _mICurrentPoint, fOpacityFactor, transformer);
                }
            }
        }

        public override void MoveDrawing(int deltaX, int deltaY, Keys modifierKeys)
        {
            if (Status == TrackStatus.Edit)
            {
                if (_mIMovingHandler == 1)
                {
                    // Update cursor label.
                    // Image will be reseted at mouse up. (=> UpdateTrackPoint)
                    _mPositions[_mICurrentPoint].X += deltaX;
                    _mPositions[_mICurrentPoint].Y += deltaY;
                }
            }
            else
            {
                if (_mIMovingHandler > 1)
                {
                    // Update coords label.
                    MoveLabelTo(deltaX, deltaY, _mIMovingHandler);
                }
            }
        }

        public override void MoveHandle(Point point, int iHandleNumber)
        {
            // We come here when moving the target or moving along the trajectory,
            // and in interactive mode (change current frame).
            if (Status == TrackStatus.Interactive && (iHandleNumber == 0 || iHandleNumber == 1))
            {
                MoveCursor(point.X, point.Y);
            }
        }

        public override int HitTest(Point point, long iCurrentTimestamp)
        {
            //---------------------------------------------------------
            // Result:
            // -1 = miss, 0 = on traj, 1 = on Cursor, 2 = on main label, 3+ = on keyframe label.
            // _point is mouse coordinates already descaled.
            //---------------------------------------------------------
            var iHitResult = -1;

            if (iCurrentTimestamp >= BeginTimeStamp && iCurrentTimestamp <= EndTimeStamp)
            {
                // We give priority to labels in case a label is on the trajectory,
                // we need to be able to move it around.
                // If label attach mode, this will tell if we are on the label.
                if (Status == TrackStatus.Interactive)
                    iHitResult = IsOnKeyframesLabels(point);

                if (iHitResult == -1)
                {
                    Rectangle rectangleTarget;
                    if (Status == TrackStatus.Edit)
                        rectangleTarget = _mTracker.GetEditRectangle(_mPositions[_mICurrentPoint].Point);
                    else
                        rectangleTarget = _mPositions[_mICurrentPoint].Box(MIDefaultCrossRadius + 3);

                    if (rectangleTarget.Contains(point))
                    {
                        iHitResult = 1;
                    }
                    else
                    {
                        // TODO: investigate why this might crash sometimes.
                        try
                        {
                            var iStart = GetFirstVisiblePoint();
                            var iEnd = GetLastVisiblePoint();

                            // Create path which contains wide line for easy mouse selection
                            var iTotalVisiblePoints = iEnd - iStart;
                            var points = new Point[iTotalVisiblePoints];
                            for (var i = iStart; i < iEnd; i++)
                                points[i - iStart] = _mPositions[i].Point;

                            var areaPath = new GraphicsPath();
                            areaPath.AddCurve(points, 0.5f);
                            var bounds = areaPath.GetBounds();
                            if (!bounds.IsEmpty)
                            {
                                var tempPen = new Pen(Color.Black, _mStyleHelper.LineSize + 7);
                                areaPath.Widen(tempPen);
                                tempPen.Dispose();
                                var areaRegion = new Region(areaPath);
                                iHitResult = areaRegion.IsVisible(point) ? 0 : -1;
                            }
                        }
                        catch (Exception exp)
                        {
                            iHitResult = -1;
                            Log.Error("Error while hit testing track.");
                            Log.Error("Exception thrown : " + exp.GetType() + " in " + exp.Source + exp.TargetSite.Name);
                            Log.Error("Message : " + exp.Message);
                            var inner = exp.InnerException;
                            while (inner != null)
                            {
                                Log.Error("Inner exception : " + inner.Message);
                                inner = inner.InnerException;
                            }
                        }
                    }
                }
            }

            if (iHitResult == 0 && Status == TrackStatus.Interactive)
            {
                // Instantly jump to the frame.
                MoveCursor(point.X, point.Y);
            }

            _mIMovingHandler = iHitResult;

            return iHitResult;
        }

        #endregion AbstractDrawing implementation

        #region Drawing routines

        private void DrawTrajectory(Graphics canvas, int start, int end, bool before, double fFadingFactor,
            CoordinateSystem transformer)
        {
            // Points are drawn with various alpha values, possibly 0:
            // In edit mode, all segments are drawn at 64 alpha.
            // In normal mode, segments before the current point are drawn at 224, segments after at 64.
            // In focus mode, (edit or normal) only a subset of segments are drawn from each part.
            // It is not possible currently to make the curve vary smoothly in alpha.
            // Either we make it vary in alpha for each segment but draw as connected lines.
            // or draw as curve but at the same alpha for all.
            // All segments are drawn at 224, even the after section.

            var points = new Point[end - start + 1];
            for (var i = 0; i <= end - start; i++)
                points[i] = transformer.Transform(_mPositions[start + i].Point);

            if (points.Length > 1)
            {
                using (var trackPen = GetTrackPen(Status, fFadingFactor, before))
                {
                    // Tension parameter is at 0.5f for bezier effect (smooth curve).
                    canvas.DrawCurve(trackPen, points, 0.5f);

                    if (_mStyleHelper.TrackShape.ShowSteps)
                    {
                        using (var stepPen = new Pen(trackPen.Color, 2))
                        {
                            var margin = (int) (trackPen.Width*1.5);
                            foreach (var p in points)
                                canvas.DrawEllipse(stepPen, p.Box(margin));
                        }
                    }
                }
            }
        }

        private void DrawMarker(Graphics canvas, double fFadingFactor, CoordinateSystem transformer)
        {
            var radius = MIDefaultCrossRadius;
            var location = transformer.Transform(_mPositions[_mICurrentPoint].Point);

            if (Status == TrackStatus.Edit)
            {
                // Little cross.
                using (var p = new Pen(Color.FromArgb((int) (fFadingFactor*255), _mStyleHelper.Color)))
                {
                    canvas.DrawLine(p, location.X, location.Y - radius, location.X, location.Y + radius);
                    canvas.DrawLine(p, location.X - radius, location.Y, location.X + radius, location.Y);
                }
            }
            else
            {
                // Crash test dummy style target.
                var diameter = radius*2;
                canvas.FillPie(Brushes.Black, location.X - radius, location.Y - radius, diameter, diameter, 0, 90);
                canvas.FillPie(Brushes.White, location.X - radius, location.Y - radius, diameter, diameter, 90, 90);
                canvas.FillPie(Brushes.Black, location.X - radius, location.Y - radius, diameter, diameter, 180, 90);
                canvas.FillPie(Brushes.White, location.X - radius, location.Y - radius, diameter, diameter, 270, 90);
                canvas.DrawEllipse(Pens.White, location.Box(radius + 2));
            }
        }

        private void DrawKeyframesTitles(Graphics canvas, double fFadingFactor, CoordinateSystem transformer)
        {
            //------------------------------------------------------------
            // Draw the Keyframes labels
            // Each Label has its own coords and is movable.
            // Each label is connected to the TrackPosition point.
            // Rescaling for the current image size has already been done.
            //------------------------------------------------------------
            if (fFadingFactor >= 0)
            {
                foreach (var kl in _mKeyframesLabels)
                {
                    // In focus mode, only show labels that are in focus section.
                    if (View == TrackView.Complete ||
                        _mInfosFading.IsVisible(_mPositions[_mICurrentPoint].T + BeginTimeStamp, kl.Timestamp,
                            MIFocusFadingFrames)
                        )
                    {
                        kl.Draw(canvas, transformer, fFadingFactor);
                    }
                }
            }
        }

        private void DrawMainLabel(Graphics canvas, int iCurrentPoint, double fFadingFactor,
            CoordinateSystem transformer)
        {
            // Draw the main label and its connector to the current point.
            if (fFadingFactor == 1.0f)
            {
                _mMainLabel.SetAttach(_mPositions[iCurrentPoint].Point, true);

                if (View == TrackView.Label)
                {
                    _mMainLabel.SetText(Label);
                }
                else
                {
                    _mMainLabel.SetText(GetExtraDataText(iCurrentPoint));
                }

                _mMainLabel.Draw(canvas, transformer, fFadingFactor);
            }
        }

        private Pen GetTrackPen(TrackStatus status, double fFadingFactor, bool before)
        {
            var iAlpha = 0;

            if (status == TrackStatus.Edit)
            {
                iAlpha = MIEditModeAlpha;
            }
            else
            {
                if (View == TrackView.Complete)
                {
                    if (before)
                    {
                        iAlpha = (int) (fFadingFactor*MIBaseAlpha);
                    }
                    else
                    {
                        iAlpha = MIAfterCurrentAlpha;
                    }
                }
                else if (View == TrackView.Focus)
                {
                    iAlpha = (int) (fFadingFactor*MIBaseAlpha);
                }
                else if (View == TrackView.Label)
                {
                    iAlpha = (int) (fFadingFactor*MILabelFollowsTrackAlpha);
                }
            }

            return _mStyleHelper.GetPen(iAlpha, 1.0);
        }

        #endregion Drawing routines

        #region Extra informations (Speed, distance)

        private string GetExtraDataText(int index)
        {
            var displayText = "";
            switch (_mTrackExtraData)
            {
                case TrackExtraData.TotalDistance:
                    displayText = GetDistanceText(0, index);
                    break;

                case TrackExtraData.Speed:
                    displayText = GetSpeedText(index - 1, index);
                    break;

                case TrackExtraData.Acceleration:
                    // Todo. GetAccelerationText();
                    break;

                case TrackExtraData.None:
                    // keyframe title ?
                    break;
            }
            return displayText;
        }

        private string GetDistanceText(int p1, int p2)
        {
            // return the distance between two tracked points.
            // Todo: currently it just return the distance between the points.
            // We would like to get the distance between all points inside the range defined by the points.

            var dist = "";
            if (_mPositions.Count > 0)
            {
                if (p1 >= 0 && p1 < _mPositions.Count &&
                    p2 >= 0 && p2 < _mPositions.Count)
                {
                    double fPixelDistance = 0;
                    for (var i = p1; i < p2; i++)
                    {
                        fPixelDistance += CalibrationHelper.PixelDistance(_mPositions[i].Point, _mPositions[i + 1].Point);
                    }

                    dist = _mParentMetadata.CalibrationHelper.GetLengthText(fPixelDistance);
                }
                else
                {
                    // return 0.
                    dist = _mParentMetadata.CalibrationHelper.GetLengthText(0);
                }
            }

            return dist;
        }

        private string GetSpeedText(int p1, int p2)
        {
            // return the instant speed at p2.
            // (that is the distance between p1 and p2 divided by the time to get from p1 to p2).
            // p2 needs to be after p1.

            var speed = "";

            if (_mPositions.Count > 0)
            {
                var iFrames = p2 - p1;

                if (p1 >= 0 && p1 < _mPositions.Count - 1 &&
                    p2 > p1 && p2 < _mPositions.Count)
                {
                    speed = _mParentMetadata.CalibrationHelper.GetSpeedText(_mPositions[p1].Point, _mPositions[p2].Point,
                        iFrames);
                }
                else
                {
                    // not computable, return 0.
                    speed = _mParentMetadata.CalibrationHelper.GetSpeedText(_mPositions[0].Point, _mPositions[0].Point,
                        0);
                }
            }

            return speed;
        }

        #endregion Extra informations (Speed, distance)

        #region User manipulation

        private void MoveCursor(int x, int y)
        {
            if (Status == TrackStatus.Edit)
            {
                // Move cursor to new coords
                // In this case, _X and _Y are delta values.
                // Image will be reseted at mouse up. (=> UpdateTrackPoint)
                _mPositions[_mICurrentPoint].X += x;
                _mPositions[_mICurrentPoint].Y += y;
            }
            else
            {
                // Move Playhead to closest frame (x,y,t).
                // In this case, _X and _Y are absolute values.
                if (MShowClosestFrame != null && _mPositions.Count > 1)
                    MShowClosestFrame(new Point(x, y), BeginTimeStamp, _mPositions, _mITotalDistance, false);
            }
        }

        private void MoveLabelTo(int deltaX, int deltaY, int iLabelNumber)
        {
            // _iLabelNumber coding: 2 = main label, 3+ = keyframes labels.

            if (Status == TrackStatus.Edit || View != TrackView.Label)
            {
                if (_mTrackExtraData != TrackExtraData.None && iLabelNumber == 2)
                {
                    // Move the main label.
                    _mMainLabel.MoveLabel(deltaX, deltaY);
                }
                else
                {
                    // Move the specified label by specified amount.
                    var iLabel = iLabelNumber - 3;
                    _mKeyframesLabels[iLabel].MoveLabel(deltaX, deltaY);
                }
            }
            else if (View == TrackView.Label)
            {
                _mMainLabel.MoveLabel(deltaX, deltaY);
            }
        }

        private int IsOnKeyframesLabels(Point point)
        {
            // Convention: -1 = miss, 2 = on main label, 3+ = on keyframe label.
            var iHitResult = -1;
            if (View == TrackView.Label)
            {
                if (_mMainLabel.HitTest(point))
                    iHitResult = 2;
            }
            else
            {
                // Even when we aren't in TrackView.Label, the main label is visible
                // if we are displaying the extra data (distance, speed).
                if (_mTrackExtraData != TrackExtraData.None)
                {
                    if (_mMainLabel.HitTest(point))
                        iHitResult = 2;
                }

                for (var i = 0; i < _mKeyframesLabels.Count; i++)
                {
                    var isVisible = _mInfosFading.IsVisible(_mPositions[_mICurrentPoint].T + BeginTimeStamp,
                        _mKeyframesLabels[i].Timestamp,
                        MIFocusFadingFrames);
                    if (View == TrackView.Complete || isVisible)
                    {
                        if (_mKeyframesLabels[i].HitTest(point))
                        {
                            iHitResult = i + 3;
                            break;
                        }
                    }
                }
            }

            return iHitResult;
        }

        private int GetFirstVisiblePoint()
        {
            if ((View != TrackView.Complete || Status == TrackStatus.Edit) && _mICurrentPoint - MIFocusFadingFrames > 0)
                return _mICurrentPoint - MIFocusFadingFrames;
            return 0;
        }

        private int GetLastVisiblePoint()
        {
            if ((View != TrackView.Complete || Status == TrackStatus.Edit) &&
                _mICurrentPoint + MIFocusFadingFrames < _mPositions.Count - 1)
                return _mICurrentPoint + MIFocusFadingFrames;
            return _mPositions.Count - 1;
        }

        #endregion User manipulation

        #region Context Menu implementation

        public void ChopTrajectory(long iCurrentTimestamp)
        {
            // Delete end of track.
            _mICurrentPoint = FindClosestPoint(iCurrentTimestamp);
            if (_mICurrentPoint < _mPositions.Count - 1)
                _mPositions.RemoveRange(_mICurrentPoint + 1, _mPositions.Count - _mICurrentPoint - 1);

            EndTimeStamp = _mPositions[_mPositions.Count - 1].T + BeginTimeStamp;
            // Todo: we must now refill the last point with a patch image.
        }

        public List<AbstractTrackPoint> GetEndOfTrack(long iTimestamp)
        {
            // Called from CommandDeleteEndOfTrack,
            // We need to keep the old values in case the command is undone.
            var ts = iTimestamp - BeginTimeStamp;
            var endOfTrack = _mPositions.SkipWhile(p => p.T >= ts).ToList();
            return endOfTrack;
        }

        public void AppendPoints(long iCurrentTimestamp, List<AbstractTrackPoint> choppedPoints)
        {
            // Called when undoing CommandDeleteEndOfTrack,
            // revival of the discarded points.
            if (choppedPoints.Count > 0)
            {
                // Some points may have been re added already and we don't want to mix the two lists.
                // Find the append insertion point, remove extra stuff, and append.
                var iMatchedPoint = _mPositions.Count - 1;

                while (_mPositions[iMatchedPoint].T >= choppedPoints[0].T && iMatchedPoint > 0)
                    iMatchedPoint--;

                if (iMatchedPoint < _mPositions.Count - 1)
                    _mPositions.RemoveRange(iMatchedPoint + 1, _mPositions.Count - (iMatchedPoint + 1));

                foreach (var trkpos in choppedPoints)
                    _mPositions.Add(trkpos);

                EndTimeStamp = _mPositions[_mPositions.Count - 1].T + BeginTimeStamp;
            }
        }

        public void StopTracking()
        {
            Status = TrackStatus.Interactive;
        }

        public void RestartTracking()
        {
            Status = TrackStatus.Edit;
        }

        #endregion Context Menu implementation

        #region Tracking

        public void TrackCurrentPosition(long iCurrentTimestamp, Bitmap bmpCurrent)
        {
            // Match the previous point in current image.
            // New points to trajectories are always created from here,
            // the user can only moves existing points.

            // A new point needs to be added if we are after the last existing one.
            // Note: the UI will force stop the tracking if the user jumps to more than
            // one frame ahead of the last registered point.
            if (iCurrentTimestamp > BeginTimeStamp + _mPositions.Last().T)
            {
                AbstractTrackPoint p = null;
                bool bMatched = _mTracker.Track(_mPositions, bmpCurrent, iCurrentTimestamp - BeginTimeStamp, out p);

                // We add it to the list even if matching failed (but we'll stop tracking then).
                if (p != null)
                {
                    _mPositions.Add(p);

                    if (!bMatched)
                        StopTracking();

                    // Adjust internal data.
                    EndTimeStamp = _mPositions.Last().T + BeginTimeStamp;
                    ComputeFlatDistance();
                    IntegrateKeyframes();
                }
                else
                {
                    // Untrackable point. Error message the user.
                    StopTracking();
                }
            }
        }

        private void ComputeFlatDistance()
        {
            // This distance is used to normalize distance vs time in interactive manipulation.

            var iSmallestTop = int.MaxValue;
            var iSmallestLeft = int.MaxValue;
            var iHighestBottom = -1;
            var iHighestRight = -1;

            for (var i = 0; i < _mPositions.Count; i++)
            {
                if (_mPositions[i].X < iSmallestLeft)
                    iSmallestLeft = _mPositions[i].X;

                if (_mPositions[i].X > iHighestRight)
                    iHighestRight = _mPositions[i].X;

                if (_mPositions[i].Y < iSmallestTop)
                    iSmallestTop = _mPositions[i].Y;

                if (_mPositions[i].Y > iHighestBottom)
                    iHighestBottom = _mPositions[i].Y;
            }

            _mITotalDistance = (int) Math.Sqrt(((iHighestRight - iSmallestLeft)*(iHighestRight - iSmallestLeft))
                                               + ((iHighestBottom - iSmallestTop)*(iHighestBottom - iSmallestTop)));
        }

        public void UpdateTrackPoint(Bitmap currentImage)
        {
            // The user moved a point that had been previously placed.
            // We need to reconstruct tracking data stored in the point, for later tracking.
            // The coordinate of the point have already been updated during the mouse move.
            if (_mPositions.Count > 1 && _mICurrentPoint >= 0)
            {
                var current = _mPositions[_mICurrentPoint];

                current.ResetTrackData();
                AbstractTrackPoint atp = _mTracker.CreateTrackPoint(true, current.X, current.Y, 1.0f, current.T,
                    currentImage, _mPositions);

                if (atp != null)
                    current = atp;

                // Update the mini label (attach, position of label, and text).
                for (var i = 0; i < _mKeyframesLabels.Count; i++)
                {
                    if (_mKeyframesLabels[i].Timestamp == current.T + BeginTimeStamp)
                    {
                        _mKeyframesLabels[i].SetAttach(current.Point, true);
                        if (_mTrackExtraData != TrackExtraData.None)
                            _mKeyframesLabels[i].SetText(GetExtraDataText(_mKeyframesLabels[i].AttachIndex));
                    }
                }
            }
        }

        #endregion Tracking

        #region XML import/export

        public void WriteXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("TimePosition", BeginTimeStamp.ToString());

            var enumConverter = TypeDescriptor.GetConverter(typeof (TrackView));
            var xmlMode = enumConverter.ConvertToString(View);
            xmlWriter.WriteElementString("Mode", xmlMode);

            enumConverter = TypeDescriptor.GetConverter(typeof (TrackExtraData));
            var xmlExtraData = enumConverter.ConvertToString(_mTrackExtraData);
            xmlWriter.WriteElementString("ExtraData", xmlExtraData);

            TrackPointsToXml(xmlWriter);

            xmlWriter.WriteStartElement("DrawingStyle");
            DrawingStyle.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();

            xmlWriter.WriteStartElement("MainLabel");
            xmlWriter.WriteAttributeString("Text", Label);
            _mMainLabel.WriteXml(xmlWriter);
            xmlWriter.WriteEndElement();

            if (_mKeyframesLabels.Count > 0)
            {
                xmlWriter.WriteStartElement("KeyframeLabelList");
                xmlWriter.WriteAttributeString("Count", _mKeyframesLabels.Count.ToString());

                foreach (var kfl in _mKeyframesLabels)
                {
                    xmlWriter.WriteStartElement("KeyframeLabel");
                    kfl.WriteXml(xmlWriter);
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();
            }
        }

        private void TrackPointsToXml(XmlWriter xmlWriter)
        {
            xmlWriter.WriteStartElement("TrackPointList");
            xmlWriter.WriteAttributeString("Count", _mPositions.Count.ToString());
            xmlWriter.WriteAttributeString("UserUnitLength", _mParentMetadata.CalibrationHelper.GetLengthAbbreviation());

            // The coordinate system defaults to the first point,
            // but can be specified by user.
            var coordOrigin = _mPositions[0].Point;

            if (_mParentMetadata.CalibrationHelper.CoordinatesOrigin.X >= 0 ||
                _mParentMetadata.CalibrationHelper.CoordinatesOrigin.Y >= 0)
                coordOrigin = _mParentMetadata.CalibrationHelper.CoordinatesOrigin;

            if (_mPositions.Count > 0)
            {
                foreach (var tp in _mPositions)
                {
                    xmlWriter.WriteStartElement("TrackPoint");

                    // Data in user units.
                    // - The origin of the coordinates system is given as parameter.
                    // - X goes left (same than internal), Y goes up (opposite than internal).
                    var userX = _mParentMetadata.CalibrationHelper.GetLengthInUserUnit(tp.X - (double) coordOrigin.X);
                    var userY = _mParentMetadata.CalibrationHelper.GetLengthInUserUnit(coordOrigin.Y - (double) tp.Y);
                    var userT = _mParentMetadata.TimeStampsToTimecode(tp.T, TimeCodeFormat.Unknown, false);

                    xmlWriter.WriteAttributeString("UserX", string.Format("{0:0.00}", userX));
                    xmlWriter.WriteAttributeString("UserXInvariant",
                        string.Format(CultureInfo.InvariantCulture, "{0:0.00}", userX));
                    xmlWriter.WriteAttributeString("UserY", string.Format("{0:0.00}", userY));
                    xmlWriter.WriteAttributeString("UserYInvariant",
                        string.Format(CultureInfo.InvariantCulture, "{0:0.00}", userY));
                    xmlWriter.WriteAttributeString("UserTime", userT);

                    tp.WriteXml(xmlWriter);

                    xmlWriter.WriteEndElement();
                }
            }
            xmlWriter.WriteEndElement();
        }

        public void ReadXml(XmlReader xmlReader, PointF scale, TimeStampMapper remapTimestampCallback)
        {
            Invalid = true;

            if (remapTimestampCallback == null)
            {
                var unparsed = xmlReader.ReadOuterXml();
                Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                return;
            }

            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                switch (xmlReader.Name)
                {
                    case "TimePosition":
                        BeginTimeStamp = remapTimestampCallback(xmlReader.ReadElementContentAsLong(), false);
                        break;

                    case "Mode":
                    {
                        var enumConverter = TypeDescriptor.GetConverter(typeof (TrackView));
                        View = (TrackView) enumConverter.ConvertFromString(xmlReader.ReadElementContentAsString());
                        break;
                    }
                    case "ExtraData":
                    {
                        var enumConverter = TypeDescriptor.GetConverter(typeof (TrackExtraData));
                        _mTrackExtraData =
                            (TrackExtraData) enumConverter.ConvertFromString(xmlReader.ReadElementContentAsString());
                        break;
                    }
                    case "TrackPointList":
                        ParseTrackPointList(xmlReader, scale, remapTimestampCallback);
                        break;

                    case "DrawingStyle":
                        DrawingStyle = new DrawingStyle(xmlReader);
                        BindStyle();
                        break;

                    case "MainLabel":
                    {
                        Label = xmlReader.GetAttribute("Text");
                        _mMainLabel = new KeyframeLabel(xmlReader, scale);
                        break;
                    }
                    case "KeyframeLabelList":
                        ParseKeyframeLabelList(xmlReader, scale);
                        break;

                    default:
                        var unparsed = xmlReader.ReadOuterXml();
                        Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }

            xmlReader.ReadEndElement();

            if (_mPositions.Count > 0)
            {
                EndTimeStamp = _mPositions[_mPositions.Count - 1].T + BeginTimeStamp;
                _mMainLabel.SetAttach(_mPositions[0].Point, false);
                _mMainLabel.SetText(Label);

                if (_mPositions.Count > 1 ||
                    _mPositions[0].X != 0 ||
                    _mPositions[0].Y != 0 ||
                    _mPositions[0].T != 0)
                {
                    Invalid = false;
                }
            }
        }

        public void ParseTrackPointList(XmlReader xmlReader, PointF scale, TimeStampMapper remapTimestampCallback)
        {
            _mPositions.Clear();
            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                if (xmlReader.Name == "TrackPoint")
                {
                    AbstractTrackPoint tp = _mTracker.CreateOrphanTrackPoint(0, 0, 0);
                    tp.ReadXml(xmlReader);

                    // time was stored in relative value, we still need to adjust it.
                    AbstractTrackPoint adapted = _mTracker.CreateOrphanTrackPoint(
                        (int) (scale.X*tp.X),
                        (int) (scale.Y*tp.Y),
                        remapTimestampCallback(tp.T, true));

                    _mPositions.Add(adapted);
                }
                else
                {
                    var unparsed = xmlReader.ReadOuterXml();
                    Log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                }
            }

            xmlReader.ReadEndElement();
        }

        public void ParseKeyframeLabelList(XmlReader xmlReader, PointF scale)
        {
            _mKeyframesLabels.Clear();

            xmlReader.ReadStartElement();

            while (xmlReader.NodeType == XmlNodeType.Element)
            {
                if (xmlReader.Name == "KeyframeLabel")
                {
                    var kfl = new KeyframeLabel(xmlReader, scale);

                    if (_mPositions.Count > 0)
                    {
                        // Match with TrackPositions previously found.
                        var iMatchedTrackPosition = FindClosestPoint(kfl.Timestamp, _mPositions, BeginTimeStamp);
                        kfl.AttachIndex = iMatchedTrackPosition;

                        kfl.SetAttach(_mPositions[iMatchedTrackPosition].Point, false);
                        _mKeyframesLabels.Add(kfl);
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

        #endregion XML import/export

        #region Miscellaneous public methods

        public void IntegrateKeyframes()
        {
            //-----------------------------------------------------------------------------------
            // The Keyframes list changed (add/remove/comments)
            // Reconstruct the Keyframes Labels, but don't completely reset those we already have
            // (Keep custom coordinates)
            //-----------------------------------------------------------------------------------

            // Keep track of matched keyframes so we can remove the others.
            var matched = new bool[_mKeyframesLabels.Count];

            // Filter out key images that are not in the trajectory boundaries.
            for (var i = 0; i < _mParentMetadata.Count; i++)
            {
                // Strictly superior because we don't show the keyframe that was created when the
                // user added the CrossMarker drawing to make the Track out of it.
                if (_mParentMetadata[i].Position > BeginTimeStamp &&
                    _mParentMetadata[i].Position <= (_mPositions[_mPositions.Count - 1].T + BeginTimeStamp))
                {
                    // The Keyframe is within the Trajectory interval.
                    // Do we know it already ?
                    var iKnown = -1;
                    for (var j = 0; j < _mKeyframesLabels.Count; j++)
                    {
                        if (_mKeyframesLabels[j].Timestamp == _mParentMetadata[i].Position)
                        {
                            iKnown = j;
                            matched[j] = true;
                            break;
                        }
                    }

                    if (iKnown >= 0)
                    {
                        // Known Keyframe, Read text again in case it changed
                        _mKeyframesLabels[iKnown].SetText(_mParentMetadata[i].Title);
                    }
                    else
                    {
                        // Unknown Keyframe, Configure and add it to list.
                        var kfl = new KeyframeLabel();
                        kfl.AttachIndex = FindClosestPoint(_mParentMetadata[i].Position);
                        kfl.SetAttach(_mPositions[kfl.AttachIndex].Point, true);
                        kfl.Timestamp = _mPositions[kfl.AttachIndex].T + BeginTimeStamp;
                        kfl.SetText(_mParentMetadata[i].Title);

                        _mKeyframesLabels.Add(kfl);
                    }
                }
            }

            // Remove unused Keyframes.
            // We only look in the original list and remove in reverse order so the index aren't messed up.
            for (var iLabel = matched.Length - 1; iLabel >= 0; iLabel--)
            {
                if (matched[iLabel] == false)
                {
                    _mKeyframesLabels.RemoveAt(iLabel);
                }
            }

            // Reinject the labels in the list for extra data.
            if (_mTrackExtraData != TrackExtraData.None)
            {
                for (var iKfl = 0; iKfl < _mKeyframesLabels.Count; iKfl++)
                    _mKeyframesLabels[iKfl].SetText(GetExtraDataText(_mKeyframesLabels[iKfl].AttachIndex));
            }
        }

        public override int GetHashCode()
        {
            // Combine all relevant fields with XOR to get the Hash.
            var iHash = 0;
            iHash ^= View.GetHashCode();
            foreach (var p in _mPositions)
                iHash ^= p.GetHashCode();

            iHash ^= MIDefaultCrossRadius.GetHashCode();
            iHash ^= _mStyleHelper.GetHashCode();
            iHash ^= _mMainLabel.GetHashCode();

            foreach (var kfl in _mKeyframesLabels)
                iHash ^= kfl.GetHashCode();

            return iHash;
        }

        public void MemorizeState()
        {
            // Used by formConfigureTrajectory to be able to modify the trajectory in real time.
            _mMemoTrackView = View;
            _mMemoLabel = Label;
        }

        public void RecallState()
        {
            // Used when the user cancels his modifications on formConfigureTrajectory.
            // m_StyleHelper has been reverted already as part of style elements framework.
            // This in turn triggered mainStyle_ValueChanged() event handler so the m_MainLabel has been reverted already too.
            View = _mMemoTrackView;
            Label = _mMemoLabel;
        }

        #endregion Miscellaneous public methods

        #region Miscellaneous private methods

        private int FindClosestPoint(long iCurrentTimestamp)
        {
            return FindClosestPoint(iCurrentTimestamp, _mPositions, BeginTimeStamp);
        }

        private int FindClosestPoint(long iCurrentTimestamp, List<AbstractTrackPoint> positions, long iBeginTimestamp)
        {
            // Find the closest registered timestamp
            // Parameter is given in absolute timestamp.
            var minErr = long.MaxValue;
            var iClosest = 0;

            for (var i = 0; i < positions.Count; i++)
            {
                var err = Math.Abs((positions[i].T + iBeginTimestamp) - iCurrentTimestamp);
                if (err < minErr)
                {
                    minErr = err;
                    iClosest = i;
                }
            }

            return iClosest;
        }

        private void mainStyle_ValueChanged(object sender, EventArgs e)
        {
            _mMainLabel.BackColor = _mStyleHelper.Color;
        }

        private void BindStyle()
        {
            DrawingStyle.Bind(_mStyleHelper, "Color", "color");
            DrawingStyle.Bind(_mStyleHelper, "LineSize", "line size");
            DrawingStyle.Bind(_mStyleHelper, "TrackShape", "track shape");
        }

        #endregion Miscellaneous private methods
    }

    public enum TrackView
    {
        Complete,
        Focus,
        Label
    }

    public enum TrackStatus
    {
        Edit,
        Interactive
    }

    public enum TrackExtraData
    {
        None,
        TotalDistance,
        Speed,
        Acceleration
    }
}