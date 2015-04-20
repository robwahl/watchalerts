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

using Kinovea.ScreenManager.Properties;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     A control to let the user specify the current position in the video.
    ///     The control is comprised of a cursor and a list of markers.
    ///     When control is modified by user:
    ///     - The internal data is modified.
    ///     - Events are raised, which are listened to by parent control.
    ///     - Parent control update its own internal data state by reading the properties.
    ///     When control appearence needs to be updated
    ///     - This is when internal data of the parent control have been modified by other means.
    ///     - (At initialization for example)
    ///     - The public properties setters are provided, they doesn't raise the events back.
    /// </summary>
    public partial class FrameTracker : UserControl
    {
        #region Constructor

        public FrameTracker()
        {
            InitializeComponent();

            // Activates double buffering
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            Cursor = Cursors.Hand;

            _mIMinimumPixel = _mISpacers + (_mICursorWidth / 2);
            _mIMaximumPixel = Width - _mISpacers - (_mICursorWidth / 2);
            _mIMaxWidth = _mIMaximumPixel - _mIMinimumPixel;
        }

        #endregion Constructor

        #region Properties

        [Category("Behavior"), Browsable(true)]
        public long Minimum
        {
            get { return _mIMinimum; }
            set
            {
                _mIMinimum = value;
                if (_mIPosition < _mIMinimum) _mIPosition = _mIMinimum;
                UpdateMarkersPositions();
                UpdateCursorPosition();
                Invalidate();
            }
        }

        [Category("Behavior"), Browsable(true)]
        public long Maximum
        {
            get { return _mIMaximum; }
            set
            {
                _mIMaximum = value;
                if (_mIPosition > _mIMaximum) _mIPosition = _mIMaximum;
                UpdateMarkersPositions();
                UpdateCursorPosition();
                Invalidate();
            }
        }

        [Category("Behavior"), Browsable(true)]
        public long Position
        {
            get { return _mIPosition; }
            set
            {
                _mIPosition = value;
                if (_mIPosition < _mIMinimum) _mIPosition = _mIMinimum;
                if (_mIPosition > _mIMaximum) _mIPosition = _mIMaximum;
                UpdateCursorPosition();
                Invalidate();
            }
        }

        [Category("Behavior"), Browsable(true)]
        public bool ReportOnMouseMove { get; set; }

        #endregion Properties

        #region Members

        private bool _mBInvalidateAsked; // To prevent reentry in MouseMove before the paint event has been honored.
        private long _mIMinimum; // In absolute timestamps.
        private long _mIPosition; // In absolute timestamps.
        private long _mIMaximum; // In absolute timestamps.

        private int _mIMaxWidth; // Number of pixels in the control that can be used for position.
        private readonly int _mIMinimumPixel;
        private int _mIMaximumPixel;
        private int _mIPixelPosition; // Left of the cursor in pixels.

        private readonly int _mICursorWidth = Resources.liqcursor.Width;
        private readonly int _mISpacers = 10;

        private bool _mBEnabled = true;
        private readonly Bitmap _bmpNavCursor = Resources.liqcursor;
        private readonly Bitmap _bmpBumperLeft = Resources.liqbumperleft;
        private readonly Bitmap _bmpBumperRight = Resources.liqbumperright;
        private readonly Bitmap _bmpBackground = Resources.liqbackdock;

        #region Markers handling

        private Metadata _mMetadata;

        private readonly List<int> _mKeyframesMarks = new List<int>(); // In control coordinates.
        private static readonly Pen MPenKeyBorder = new Pen(Color.FromArgb(255, Color.YellowGreen), 1);
        private static readonly Pen MPenKeyInside = new Pen(Color.FromArgb(96, Color.YellowGreen), 1);

        private readonly List<Point> _mChronosMarks = new List<Point>(); // Start and end of chronos in control coords.
        private static readonly Pen MPenChronoBorder = new Pen(Color.FromArgb(255, Color.CornflowerBlue), 1); // skyblue
        private static readonly SolidBrush MBrushChrono = new SolidBrush(Color.FromArgb(96, Color.CornflowerBlue));

        private readonly List<Point> _mTracksMarks = new List<Point>(); // Start and end of tracks in control coords.
        private static readonly Pen MPenTrackBorder = new Pen(Color.FromArgb(255, Color.Plum), 1); // Plum;SandyBrown
        private static readonly SolidBrush MBrushTrack = new SolidBrush(Color.FromArgb(96, Color.Plum));

        private long _mSyncPointTimestamp;
        private int _mSyncPointMark;
        private static readonly Pen MPenSyncBorder = new Pen(Color.FromArgb(255, Color.Firebrick), 1);
        private static readonly Pen MPenSyncInside = new Pen(Color.FromArgb(96, Color.Firebrick), 1);

        #endregion Markers handling

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Events Delegates

        [Category("Action"), Browsable(true)]
        public event EventHandler<PositionChangedEventArgs> PositionChanging;

        [Category("Action"), Browsable(true)]
        public event EventHandler<PositionChangedEventArgs> PositionChanged;

        #endregion Events Delegates

        #region Public Methods

        public void Remap(long iMin, long iMax)
        {
            // This method is only a shortcut to updating min and max properties at once.
            // This method update the appearence of the control only, it doesn't raise the events back.
            _mIMinimum = iMin;
            _mIMaximum = iMax;

            if (_mIPosition < _mIMinimum) _mIPosition = _mIMinimum;
            if (_mIPosition > _mIMaximum) _mIPosition = _mIMaximum;

            UpdateMarkersPositions();
            UpdateCursorPosition();
            Invalidate();
        }

        public void EnableDisable(bool bEnable)
        {
            _mBEnabled = bEnable;
            Invalidate();
        }

        public void UpdateMarkers(Metadata metadata)
        {
            // Keep a ref on the Metadata object so we can update the
            // markers position when only the size of the control changes.

            _mMetadata = metadata;
            UpdateMarkersPositions();
            Invalidate();
        }

        public void UpdateSyncPointMarker(long marker)
        {
            _mSyncPointTimestamp = marker;
            UpdateMarkersPositions();
            Invalidate();
        }

        #endregion Public Methods

        #region Event Handlers - User Manipulation

        private void FrameTracker_MouseMove(object sender, MouseEventArgs e)
        {
            // Note: also raised on mouse down.
            // User wants to jump to position. Update the cursor and optionnaly the image.
            if (_mBEnabled && !_mBInvalidateAsked)
            {
                if (e.Button == MouseButtons.Left)
                {
                    var mouseCoords = PointToClient(Cursor.Position);

                    if ((mouseCoords.X > _mIMinimumPixel) && (mouseCoords.X < _mIMaximumPixel))
                    {
                        _mIPixelPosition = mouseCoords.X - (_mICursorWidth / 2);
                        Invalidate();
                        _mBInvalidateAsked = true;

                        if (ReportOnMouseMove && PositionChanging != null)
                        {
                            _mIPosition = GetTimestampFromCoord(_mIPixelPosition + (_mICursorWidth / 2));
                            PositionChanging(this, new PositionChangedEventArgs(_mIPosition));
                        }
                        else
                        {
                            Invalidate();
                        }
                    }
                }
            }
        }

        private void FrameTracker_MouseUp(object sender, MouseEventArgs e)
        {
            // End of a mouse move, jump to position.
            if (_mBEnabled)
            {
                if (e.Button == MouseButtons.Left)
                {
                    var mouseCoords = PointToClient(Cursor.Position);

                    if ((mouseCoords.X > _mIMinimumPixel) && (mouseCoords.X < _mIMaximumPixel))
                    {
                        _mIPixelPosition = mouseCoords.X - (_mICursorWidth / 2);
                        Invalidate();
                        if (PositionChanged != null)
                        {
                            _mIPosition = GetTimestampFromCoord(_mIPixelPosition + (_mICursorWidth / 2));
                            PositionChanged(this, new PositionChangedEventArgs(_mIPosition));
                        }
                    }
                }
            }
        }

        private void FrameTracker_Resize(object sender, EventArgs e)
        {
            // Resize of the control only : internal data doesn't change.
            _mIMaximumPixel = Width - _mISpacers - (_mICursorWidth / 2);
            _mIMaxWidth = _mIMaximumPixel - _mIMinimumPixel;
            UpdateMarkersPositions();
            UpdateCursorPosition();
            Invalidate();
        }

        #endregion Event Handlers - User Manipulation

        #region Painting

        private void FrameTracker_Paint(object sender, PaintEventArgs e)
        {
            // When we land in this function, m_iPixelPosition should have been set already.
            // It is the only member variable we'll use here.

            // Draw tiled background
            for (var i = 10; i < Width - 20; i += _bmpBackground.Width)
            {
                e.Graphics.DrawImage(_bmpBackground, i, 0);
            }

            // Draw slider ends.
            e.Graphics.DrawImage(_bmpBumperLeft, 10, 0);
            e.Graphics.DrawImage(_bmpBumperRight, Width - 20, 0);

            if (_mBEnabled)
            {
                // Draw the various markers within the frame tracker gutter.
                if (_mKeyframesMarks.Count > 0)
                {
                    foreach (var mark in _mKeyframesMarks)
                    {
                        if (mark > 0)
                        {
                            DrawMark(e.Graphics, MPenKeyBorder, MPenKeyInside, mark);
                        }
                    }
                }

                if (_mChronosMarks.Count > 0)
                {
                    foreach (var mark in _mChronosMarks)
                    {
                        DrawMark(e.Graphics, MPenChronoBorder, MBrushChrono, mark);
                    }
                }

                if (_mTracksMarks.Count > 0)
                {
                    foreach (var mark in _mTracksMarks)
                    {
                        DrawMark(e.Graphics, MPenTrackBorder, MBrushTrack, mark);
                    }
                }

                if (_mSyncPointMark > 0)
                {
                    DrawMark(e.Graphics, MPenSyncBorder, MPenSyncInside, _mSyncPointMark);
                }

                // Draw the cursor.
                e.Graphics.DrawImage(_bmpNavCursor, _mIPixelPosition, 0);
            }

            _mBInvalidateAsked = false;
        }

        private void DrawMark(Graphics canvas, Pen pBorder, Pen pInside, int iCoord)
        {
            // Mark for a single point in time (key image).

            // Simple line.
            //_canvas.DrawLine(_p, _iCoord, 2, _iCoord, this.Height - 4);
            //_canvas.DrawLine(_p, _iCoord+1, 2, _iCoord+1, this.Height - 4);

            // Small rectangles.
            var iLeft = iCoord;
            var iTop = 5;
            var iWidth = 3;
            var iHeight = 8;

            canvas.DrawRectangle(pBorder, iLeft, iTop, iWidth, iHeight);
            canvas.DrawRectangle(pInside, iLeft + 1, iTop + 1, iWidth - 2, iHeight - 2);
        }

        private void DrawMark(Graphics canvas, Pen pBorder, SolidBrush bInside, Point iCoords)
        {
            // Mark for a range in time (chrono or track).
            var iLeft = iCoords.X;
            var iTop = 5;
            var iWidth = iCoords.Y;
            var iHeight = 8;

            // Bound to bumpers.
            if (iLeft < _mIMinimumPixel) iLeft = _mIMinimumPixel;
            if (iLeft + iWidth > _mIMaximumPixel) iWidth = _mIMaximumPixel - iLeft;

            canvas.DrawRectangle(pBorder, iLeft, iTop, iWidth, iHeight);
            canvas.FillRectangle(bInside, iLeft + 1, iTop + 1, iWidth - 1, iHeight - 1);
        }

        #endregion Painting

        #region Binding UI to Data

        private void UpdateCursorPosition()
        {
            // This method updates the appearence of the control only, it doesn't raise the events back.
            // Should be called every time m_iPosition has been updated.
            _mIPixelPosition = GetCoordFromTimestamp(_mIPosition) - (_mICursorWidth / 2);
        }

        private void UpdateMarkersPositions()
        {
            // Translate timestamps into control coordinates and store the coordinates of the
            // markers to draw them later.
            // Should only be called when either the timestamps or the control size changed.
            if (_mMetadata != null)
            {
                // Key frames
                _mKeyframesMarks.Clear();
                foreach (var kf in _mMetadata.Keyframes)
                {
                    // Only display Key image that are in the selection.
                    if (kf.Position >= _mIMinimum && kf.Position <= _mIMaximum)
                    {
                        _mKeyframesMarks.Add(GetCoordFromTimestamp(kf.Position));
                    }
                }

                // ExtraDrawings
                // We will store the range coords in a Point object, to get a couple of ints structure.
                // X will be the left coordinate, Y the width.
                _mChronosMarks.Clear();
                _mTracksMarks.Clear();
                foreach (AbstractDrawing ad in _mMetadata.ExtraDrawings)
                {
                    var dc = ad as DrawingChrono;
                    var trk = ad as Track;

                    if (dc != null)
                    {
                        if (dc.TimeStart != long.MaxValue && dc.TimeStop != long.MaxValue)
                        {
                            // todo: currently doesn't support the chrono without end.
                            // Only display chronometers that have at least something in the selection.
                            if (dc.TimeStart <= _mIMaximum && dc.TimeStop >= _mIMinimum)
                            {
                                var startTs = Math.Max(dc.TimeStart, _mIMinimum);
                                var stopTs = Math.Min(dc.TimeStop, _mIMaximum);

                                var start = GetCoordFromTimestamp(startTs);
                                var stop = GetCoordFromTimestamp(stopTs);

                                var p = new Point(start, stop - start);

                                _mChronosMarks.Add(p);
                            }
                        }
                    }
                    else if (trk != null)
                    {
                        if (trk.BeginTimeStamp <= _mIMaximum && trk.EndTimeStamp >= _mIMinimum)
                        {
                            var startTs = Math.Max(trk.BeginTimeStamp, _mIMinimum);
                            var stopTs = Math.Min(trk.EndTimeStamp, _mIMaximum);

                            var start = GetCoordFromTimestamp(startTs);
                            var stop = GetCoordFromTimestamp(stopTs);

                            var p = new Point(start, stop - start);

                            _mTracksMarks.Add(p);
                        }
                    }
                }
            }

            // Sync point
            _mSyncPointMark = 0;
            if (_mSyncPointTimestamp != 0 && _mSyncPointTimestamp >= _mIMinimum && _mSyncPointTimestamp <= _mIMaximum)
            {
                _mSyncPointMark = GetCoordFromTimestamp(_mSyncPointTimestamp);
            }
        }

        private int GetCoordFromTimestamp(long ts)
        {
            var iret = _mIMinimumPixel + Rescale(ts - _mIMinimum, _mIMaximum - _mIMinimum, _mIMaxWidth);
            return iret;
        }

        private long GetTimestampFromCoord(int pos)
        {
            var ret = _mIMinimum + Rescale(pos - _mIMinimumPixel, _mIMaxWidth, _mIMaximum - _mIMinimum);
            return ret;
        }

        private int Rescale(long iOldValue, long iOldMax, long iNewMax)
        {
            // Rescale : Pixels -> Values
            if (iOldMax > 0)
            {
                return (int)(Math.Round(iOldValue * (double)iNewMax / iOldMax));
            }
            return 0;
        }

        #endregion Binding UI to Data
    }

    public class PositionChangedEventArgs : EventArgs
    {
        public readonly long Position;

        public PositionChangedEventArgs(long position)
        {
            Position = position;
        }
    }
}