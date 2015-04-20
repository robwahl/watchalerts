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
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     A control to let the user specify the Working Zone.
    ///     The control is comprised of bumpers at the ends, handlers around the selection,
    ///     a middle section for the selection, and a hairline for the current position.
    ///     When control is modified by user:
    ///     - The internal data is modified.
    ///     - Events are raised, which are listened to by parent control.
    ///     - Parent control update its own internal data state by reading the properties.
    ///     When control appearence needs to be updated
    ///     - This is when internal data of the parent control have been modified by other means.
    ///     - (At initialization for example)
    ///     - The public properties setters are provided, they doesn't raise the events back.
    /// </summary>
    public partial class SelectionTracker : UserControl
    {
        #region Contructor

        public SelectionTracker()
        {
            InitializeComponent();
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            Cursor = Cursors.Hand;

            _mIMinimumPixel = MISpacerWidth + MIBumperWidth;
            _mIMaximumPixel = Width - MISpacerWidth - MIBumperWidth;
            _mIMaxWidthPixel = _mIMaximumPixel - _mIMinimumPixel;
        }

        #endregion Contructor

        #region Properties

        [Category("Behavior"), Browsable(true)]
        public long Minimum
        {
            get { return _mIMinimum; }
            set
            {
                _mIMinimum = value;
                UpdateAppearence();
            }
        }

        [Category("Behavior"), Browsable(true)]
        public long Maximum
        {
            get { return _mIMaximum; }
            set
            {
                _mIMaximum = value;
                UpdateAppearence();
            }
        }

        [Category("Behavior"), Browsable(true)]
        public long SelStart
        {
            get { return _mISelStart; }
            set
            {
                _mISelStart = value;
                if (_mISelStart < _mIMinimum)
                {
                    _mISelStart = _mIMinimum;
                }

                UpdateAppearence();
            }
        }

        [Category("Behavior"), Browsable(true)]
        public long SelEnd
        {
            get { return _mISelEnd; }
            set
            {
                _mISelEnd = value;
                if (_mISelEnd < _mISelStart)
                {
                    _mISelEnd = _mISelStart;
                }
                else if (_mISelEnd > _mIMaximum)
                {
                    _mISelEnd = _mIMaximum;
                }

                UpdateAppearence();
            }
        }

        [Category("Behavior"), Browsable(false)]
        public long SelPos
        {
            get { return _mISelPos; }
            set
            {
                _mISelPos = value;
                if (_mISelPos < _mISelStart)
                {
                    _mISelPos = _mISelStart;
                }
                else if (_mISelPos > _mISelEnd)
                {
                    _mISelPos = _mISelEnd;
                }

                UpdateAppearence();
            }
        }

        [Category("Behavior"), Browsable(true)]
        public bool SelLocked { get; set; }

        [Category("Misc"), Browsable(true)]
        public string ToolTip
        {
            get { return toolTips.GetToolTip(this); }
            set { toolTips.SetToolTip(this, value); }
        }

        #endregion Properties

        #region Members

        // Data
        private long _mIMinimum; // All data are in absolute timestamps.

        private long _mIMaximum = 100;
        private long _mISelStart;
        private long _mISelEnd = 100;
        private long _mISelPos;

        // Display
        private bool _mBEnabled = true;

        private readonly int _mIMinimumPixel;
        private int _mIMaximumPixel;
        private int _mIMaxWidthPixel; // Inner size of selection in pixels.
        private int _mIStartPixel; // First pixel of the selection zone.
        private int _mIEndPixel; // Last pixel of the selection zone.
        private int _mIPositionPixel; // Exact position of the playhead.

        // Graphics
        private static readonly Bitmap BmpBumperLeft = Resources.liqbumperleft;

        private static readonly Bitmap BmpBumperRight = Resources.liqbumperright;
        private static readonly Bitmap BmpBackground = Resources.liqbackdock;
        private static readonly Bitmap BmpHandlerLeft = Resources.liqhandlerleft2;
        private static readonly Bitmap BmpHandlerRight = Resources.liqhandlerright3;
        private static readonly Bitmap BmpMiddleBar = Resources.liqmiddlebar;
        private static readonly int MISpacerWidth = 10;
        private static readonly int MIBumperWidth = BmpBumperLeft.Width;
        private static readonly int MIHandlerWidth = BmpHandlerLeft.Width;

        // Interaction
        private bool _mBDraggingLeft;

        private bool _mBDraggingRight;
        private bool _mBDraggingTarget;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Events

        [Category("Action"), Browsable(true)]
        public event EventHandler SelectionChanging;

        [Category("Action"), Browsable(true)]
        public event EventHandler SelectionChanged;

        [Category("Action"), Browsable(true)]
        public event EventHandler TargetAcquired;

        #endregion Events

        #region Public Methods - Timestamps to pixels.

        public void UpdateInternalState(long iMin, long iMax, long iStart, long iEnd, long iPos)
        {
            // This method is only a shortcut to updating all properties at once.
            // It should be called when the internal state of data has been modified
            // by other means than the user manipulating the control.
            // (for example, at initialization or reset.)
            // This method update the appearence of the control only, it doesn't raise the events back.
            // All input data are in absolute timestamps.
            _mIMinimum = iMin;
            _mIMaximum = iMax;
            _mISelStart = iStart;
            _mISelEnd = iEnd;
            _mISelPos = iPos;

            UpdateAppearence();
        }

        public void Reset()
        {
            _mISelStart = _mIMinimum;
            _mISelEnd = _mIMaximum;
            _mISelPos = _mIMinimum;
            UpdateAppearence();
        }

        public void EnableDisable(bool bEnable)
        {
            _mBEnabled = bEnable;
            Invalidate();
        }

        #endregion Public Methods - Timestamps to pixels.

        #region Interaction Events - Pixels to timestamps.

        private void SelectionTracker_MouseDown(object sender, MouseEventArgs e)
        {
            _mBDraggingLeft = false;
            _mBDraggingRight = false;
            _mBDraggingTarget = false;

            if (_mBEnabled && e.Button == MouseButtons.Left)
            {
                if (e.X >= _mIStartPixel - MIHandlerWidth && e.X < _mIStartPixel)
                {
                    // in handler left.
                    _mBDraggingLeft = true;
                }
                else if (e.X >= _mIEndPixel && e.X < _mIEndPixel + MIHandlerWidth)
                {
                    // in handler right.
                    _mBDraggingRight = true;
                }
                else if (e.X >= _mIStartPixel && e.X < _mIEndPixel)
                {
                    // in selection.
                    _mBDraggingTarget = true;
                }
                else if (e.X < _mIMinimumPixel)
                {
                    // before minimum.
                }
                else if (e.X >= _mIMaximumPixel)
                {
                    // after maximum.
                }
            }
        }

        private void SelectionTracker_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mBEnabled &&
                (e.Button == MouseButtons.Left) &&
                (_mBDraggingLeft || _mBDraggingRight || _mBDraggingTarget))
            {
                if (_mBDraggingLeft)
                {
                    if (e.X >= _mIMinimumPixel - (MIHandlerWidth / 2) && e.X < _mIEndPixel - (MIHandlerWidth / 2))
                    {
                        _mIStartPixel = e.X + (MIHandlerWidth / 2);
                        _mIPositionPixel = Math.Max(_mIPositionPixel, _mIStartPixel);
                    }
                }
                else if (_mBDraggingRight)
                {
                    if (e.X >= _mIStartPixel + (MIHandlerWidth / 2) && e.X < _mIMaximumPixel + (MIHandlerWidth / 2))
                    {
                        _mIEndPixel = e.X - (MIHandlerWidth / 2);
                        _mIPositionPixel = Math.Min(_mIPositionPixel, _mIEndPixel);
                    }
                }
                else if (_mBDraggingTarget)
                {
                    if (e.X >= _mIStartPixel && e.X < _mIEndPixel)
                    {
                        _mIPositionPixel = e.X;
                    }
                }

                Invalidate();

                // Update values and report to container.
                _mISelPos = GetTimestampFromCoord(_mIPositionPixel);
                _mISelStart = GetTimestampFromCoord(_mIStartPixel);
                _mISelEnd = GetTimestampFromCoord(_mIEndPixel);
                if (SelectionChanging != null)
                {
                    SelectionChanging(this, EventArgs.Empty);
                }
            }
        }

        private void SelectionTracker_MouseUp(object sender, MouseEventArgs e)
        {
            // This is when the validation of the change occur.
            if (_mBEnabled && e.Button == MouseButtons.Left)
            {
                if (_mBDraggingTarget)
                {
                    // Handle the special case of simple click to change position.
                    // (mouseMove is not triggered in this case.)
                    if (e.X >= _mIStartPixel && e.X < _mIEndPixel)
                    {
                        _mIPositionPixel = e.X;
                        Invalidate();
                    }

                    // Update values and report to container.
                    _mISelPos = GetTimestampFromCoord(_mIPositionPixel);
                    if (TargetAcquired != null)
                    {
                        TargetAcquired(this, EventArgs.Empty);
                    }
                }
                else if (_mBDraggingLeft || _mBDraggingRight)
                {
                    // Update values and report to container.
                    _mISelStart = GetTimestampFromCoord(_mIStartPixel);
                    _mISelEnd = GetTimestampFromCoord(_mIEndPixel);
                    if (SelectionChanged != null)
                    {
                        SelectionChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        #endregion Interaction Events - Pixels to timestamps.

        #region Paint / Resize

        private void SelectionTracker_Paint(object sender, PaintEventArgs e)
        {
            // Draw the control.
            // All the position variables must have been set already.

            // Draw background.
            // (we draw it first to be able to cover the extra tiling)
            for (var i = _mIMinimumPixel; i < _mIMaximumPixel; i += BmpBackground.Width)
            {
                e.Graphics.DrawImage(BmpBackground, i, 0);
            }

            // Draw bumpers
            e.Graphics.DrawImage(BmpBumperLeft, MISpacerWidth, 0);
            e.Graphics.DrawImage(BmpBumperRight, _mIMaximumPixel, 0);

            // Draw content.
            if (_mBEnabled)
            {
                // Draw selection zone.
                // (we draw it first to be able to cover the extra tiling)
                for (var i = _mIStartPixel; i < _mIEndPixel; i += BmpMiddleBar.Width)
                {
                    e.Graphics.DrawImage(BmpMiddleBar, i, 0);
                }

                // Draw handlers
                e.Graphics.DrawImage(BmpHandlerLeft, _mIStartPixel - MIHandlerWidth, 0);
                e.Graphics.DrawImage(BmpHandlerRight, _mIEndPixel, 0);

                // Draw hairline.
                e.Graphics.DrawLine(Pens.Black, _mIPositionPixel, 4, _mIPositionPixel, Height - 10);
            }
        }

        private void SelectionTracker_Resize(object sender, EventArgs e)
        {
            // Resize of the control only : data doesn't change.
            _mIMaximumPixel = Width - MISpacerWidth - MIBumperWidth;
            _mIMaxWidthPixel = _mIMaximumPixel - _mIMinimumPixel;
            UpdateAppearence();
        }

        #endregion Paint / Resize

        #region Binding UI and Data

        private void UpdateAppearence()
        {
            // Internal state of data has been modified programmatically.
            // (for example, initialization, reset, boundaries buttons, etc.)
            // This method updates the appearence of the control only, it doesn't raise the events back.
            if (_mIMaximum - _mIMinimum > 0)
            {
                _mIStartPixel = GetCoordFromTimestamp(_mISelStart);
                _mIEndPixel = GetCoordFromTimestamp(_mISelEnd);
                _mIPositionPixel = GetCoordFromTimestamp(_mISelPos);
                Invalidate();
            }
        }

        private int GetCoordFromTimestamp(long ts)
        {
            // Take any timestamp and convert it into a pixel coord.
            var iret = _mIMinimumPixel + Rescale(ts - _mIMinimum, _mIMaximum - _mIMinimum, _mIMaxWidthPixel);
            return iret;
        }

        private long GetTimestampFromCoord(int posPixel)
        {
            // Take any position in pixel and convert it into a timestamp.
            // At this point, the pixel position shouldn't be outside the boundaries values.
            var ret = _mIMinimum + Rescale(posPixel - _mIMinimumPixel, _mIMaxWidthPixel, _mIMaximum - _mIMinimum);
            return ret;
        }

        private int Rescale(long iOldValue, long iOldMax, long iNewMax)
        {
            return (int)(Math.Round(iOldValue * (double)iNewMax / iOldMax));
        }

        #endregion Binding UI and Data
    }
}