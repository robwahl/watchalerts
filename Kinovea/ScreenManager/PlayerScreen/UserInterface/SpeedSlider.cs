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
    /// A slider control.
    ///
    /// When value is modified by user:
    /// - The internal value is modified.
    /// - Events are raised, which are listened to by parent control.
    /// - Parent control update its own internal data state by reading the properties.
    ///
    /// When control appearence needs to be updated
    /// - This is when internal data of the parent control have been modified by other means.
    /// - (At initialization for example)
    /// - The public properties setters are provided, they doesn't raise the events back.
    ///
    /// This control is pretty similar to FrameTracker. Maybe it would be possible to factorize the code.
    /// </summary>
    public partial class SpeedSlider : UserControl
    {
        #region Ctor

        public SpeedSlider()
        {
            InitializeComponent();

            // Activates double buffering
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            Cursor = Cursors.Hand;

            _mIMinimumPixel = _mIButtonWidth + _mISpacers;
            _mIMaximumPixel = Width - (_mISpacers + _mIButtonWidth);
            _mIMaxWidth = _mIMaximumPixel - _mIMinimumPixel;
            _mIStickyPixel = GetCoordFromValue(_mIStickyValue);

            BackColor = Color.White;
        }

        #endregion Ctor

        #region Events

        [Category("Action"), Browsable(true)]
        public event EventHandler ValueChanged;

        #endregion Events

        #region Properties

        [Category("Behavior"), Browsable(true)]
        public int Minimum { get; set; } = 1;

        [Category("Behavior"), Browsable(true)]
        public int Maximum { get; set; } = 200;

        [Category("Behavior"), Browsable(true)]
        public int Value
        {
            get
            {
                if (_mIValue < Minimum) _mIValue = Minimum;
                return _mIValue;
            }
            set
            {
                _mIValue = value;
                if (_mIValue < Minimum) _mIValue = Minimum;
                if (_mIValue > Maximum) _mIValue = Maximum;
                UpdateCursorPosition();
                Invalidate();
            }
        }

        [Category("Misc"), Browsable(true)]
        public string ToolTip
        {
            set { toolTips.SetToolTip(this, value); }
        }

        [Category("Behavior"), Browsable(true)]
        public int SmallChange { get; set; } = 1;

        [Category("Behavior"), Browsable(true)]
        public int LargeChange { get; set; } = 5;

        [Category("Behavior"), Browsable(true)]
        public int StickyValue
        {
            get { return _mIStickyValue; }
            set
            {
                _mIStickyValue = value;
                _mIStickyPixel = GetCoordFromValue(_mIStickyValue);
            }
        }

        [Category("Behavior"), Browsable(true)]
        public bool StickyMark { get; set; } = true;

        #endregion Properties

        #region Members

        private bool _mBInvalidateAsked; // To prevent reentry in MouseMove before the paint event has been honored.
        private bool _mBEnabled = true;

        private int _mIValue = 100;
        private int _mIStickyValue = 100;

        private int _mIMaxWidth; // Number of pixels in the control that can be used for values.
        private readonly int _mIMinimumPixel;
        private int _mIMaximumPixel;
        private int _mIStickyPixel;
        private int _mIPixelPosition; // Left of the cursor in pixels.

        private bool _mBDecreasing;
        private bool _mBIncreasing;

        private readonly int _mISpacers = 10; // size of space between buttons and rail.
        private readonly int _mIButtonWidth = Resources.SpeedTrkDecrease2.Width;
        private readonly int _mICursorWidth = Resources.SpeedTrkCursor7.Width;

        private readonly Bitmap _bmpDecrease = Resources.SpeedTrkDecrease2;
        private readonly Bitmap _bmpIncrease = Resources.SpeedTrkIncrease2;
        private readonly Bitmap _bmpBackground = Resources.SpeedTrkBack5;
        private readonly Bitmap _bmpCursor = Resources.SpeedTrkCursor7;

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Public Methods

        public void EnableDisable(bool bEnable)
        {
            _mBEnabled = bEnable;
            Invalidate();
        }

        public void ForceValue(int value)
        {
            // This method is used when the value must be forced,
            // but the internal state of the parent control hasn't been updated.
            // It does raise the event back.
            _mIValue = value;
            if (_mIValue < Minimum) _mIValue = Minimum;
            if (_mIValue > Maximum) _mIValue = Maximum;
            UpdateCursorPosition();
            Invalidate();
            if (ValueChanged != null)
            {
                ValueChanged(this, EventArgs.Empty);
            }
        }

        #endregion Public Methods

        #region Event Handlers - User Manipulation

        private void SpeedSlider_MouseDown(object sender, MouseEventArgs e)
        {
            // Register which button we hit. We'll handle the action in MouseUp.

            _mBDecreasing = false;
            _mBIncreasing = false;

            if (_mBEnabled && e.Button == MouseButtons.Left)
            {
                if (e.X >= 0 && e.X < _mIButtonWidth)
                {
                    // on decrease button.
                    _mBDecreasing = true;
                }
                else if (e.X >= Width - _mIButtonWidth && e.X < Width)
                {
                    // on increase button.
                    _mBIncreasing = true;
                }
            }
        }

        private void SpeedSlider_MouseMove(object sender, MouseEventArgs e)
        {
            // Note: also raised on mouse down.
            // User wants to jump to position.
            if (_mBEnabled && !_mBInvalidateAsked)
            {
                if (e.Button == MouseButtons.Left)
                {
                    var mouseCoords = PointToClient(Cursor.Position);

                    if ((mouseCoords.X > _mIMinimumPixel) && (mouseCoords.X < _mIMaximumPixel))
                    {
                        _mIPixelPosition = mouseCoords.X - (_mICursorWidth/2);
                        _mIValue = GetValueFromCoord(mouseCoords.X);

                        // Stickiness
                        if (
                            (_mIValue >= (_mIStickyValue - 5)) && (_mIValue <= _mIStickyValue) ||
                            (_mIValue <= (_mIStickyValue + 5)) && (_mIValue >= _mIStickyValue)
                            )
                        {
                            // Inside sticky zone, fall back to sticky value.
                            _mIValue = _mIStickyValue;
                            _mIPixelPosition = _mIStickyPixel - (_mICursorWidth/2);
                        }

                        Invalidate();
                        _mBInvalidateAsked = true;

                        if (ValueChanged != null)
                        {
                            ValueChanged(this, EventArgs.Empty);
                        }
                    }
                }
            }
        }

        private void SpeedSlider_MouseUp(object sender, MouseEventArgs e)
        {
            // This is when the validation of the change occur.
            if (_mBEnabled && e.Button == MouseButtons.Left)
            {
                var changed = false;

                if (_mBDecreasing)
                {
                    if (Maximum - Minimum > 0 && _mIValue > Minimum + LargeChange)
                    {
                        _mIValue -= LargeChange;
                        changed = true;
                    }
                }
                else if (_mBIncreasing)
                {
                    if (Maximum - Minimum > 0 && _mIValue <= Maximum - LargeChange)
                    {
                        _mIValue += LargeChange;
                        changed = true;
                    }
                }

                if (changed)
                {
                    UpdateCursorPosition();
                    Invalidate();
                    if (ValueChanged != null)
                    {
                        ValueChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        #endregion Event Handlers - User Manipulation

        #region Paint / Resize

        private void SpeedSlider_Paint(object sender, PaintEventArgs e)
        {
            // When we land in this function, m_iPixelPosition should have been set already.
            // It is the only member variable we'll use here.

            // Draw buttons
            if (_mBEnabled)
            {
                e.Graphics.DrawImage(_bmpDecrease, 0, 0);
                e.Graphics.DrawImage(_bmpIncrease, _mIMaximumPixel + _mISpacers, 0);
            }

            // Draw tiled background
            for (var i = _mIMinimumPixel; i < _mIMaximumPixel; i += _bmpBackground.Width)
            {
                e.Graphics.DrawImage(_bmpBackground, i, 0);
            }

            // MiddleMarker
            if (StickyMark)
            {
                e.Graphics.DrawLine(Pens.Gray, _mIStickyPixel, 0, _mIStickyPixel, 3);
                e.Graphics.DrawLine(Pens.Gray, _mIStickyPixel, 7, _mIStickyPixel, 10);
            }

            // Draw th e cursor.
            if (_mBEnabled)
            {
                e.Graphics.DrawImage(_bmpCursor, _mIPixelPosition, 0);
            }

            _mBInvalidateAsked = false;
        }

        private void SpeedSlider_Resize(object sender, EventArgs e)
        {
            // Resize of the control only : internal data doesn't change.
            _mIMaximumPixel = Width - (_mISpacers + _mIButtonWidth);
            _mIMaxWidth = _mIMaximumPixel - _mIMinimumPixel;
            _mIStickyPixel = GetCoordFromValue(_mIStickyValue);

            UpdateCursorPosition();
            Invalidate();
        }

        #endregion Paint / Resize

        #region Binding UI to Data

        private void UpdateCursorPosition()
        {
            // This method updates the appearence of the control only, it doesn't raise the events back.
            // Should be called every time m_iPosition has been updated.
            _mIPixelPosition = GetCoordFromValue(_mIValue) - (_mICursorWidth/2);
        }

        private int GetCoordFromValue(int value)
        {
            var iret = _mIMinimumPixel + Rescale(value - Minimum, Maximum - Minimum, _mIMaxWidth);
            return iret;
        }

        private int GetValueFromCoord(int pos)
        {
            var ret = Minimum + Rescale(pos - _mIMinimumPixel, _mIMaxWidth, Maximum - Minimum);
            return ret;
        }

        private int Rescale(long iOldValue, long iOldMax, long iNewMax)
        {
            if (iOldMax > 0)
            {
                return (int) (Math.Round(iOldValue*(double) iNewMax/iOldMax));
            }
            return 0;
        }

        #endregion Binding UI to Data
    }
}