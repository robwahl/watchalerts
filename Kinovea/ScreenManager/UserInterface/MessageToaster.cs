#region License

/*
Copyright © Joan Charmant 2010.
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

using log4net;
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     MessageToaster.
    ///     A class to encapsulate the display of a message to be painted directly on a canvas,
    ///     for a given duration.
    ///     Used to indicate a change in the state of a screen for example. (Pause, Zoom factor).
    ///     The same object is reused for various messages. Each screen should have its own instance.
    /// </summary>
    public class MessageToaster
    {
        #region Constructor

        public MessageToaster(Control canvasHolder)
        {
            _mCanvasHolder = canvasHolder;
            _mFont = new Font("Arial", MDefaultFontSize, FontStyle.Bold);
            _mTimer.Interval = MDefaultDuration;
            _mTimer.Tick += Timer_OnTick;
        }

        #endregion Constructor

        #region Properties

        public bool Enabled { get; private set; }

        #endregion Properties

        #region Members

        private string _mMessage;
        private readonly Timer _mTimer = new Timer();
        private readonly Font _mFont;
        private readonly Control _mCanvasHolder;
        private static readonly int MDefaultDuration = 1000;
        private static readonly int MDefaultFontSize = 24;
        private readonly Brush _mForeBrush = new SolidBrush(Color.FromArgb(255, Color.White));
        private readonly Brush _mBackBrush = new SolidBrush(Color.FromArgb(128, Color.Black));
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Public Methods

        public void SetDuration(int duration)
        {
            _mTimer.Interval = duration;
        }

        public void Show(string message)
        {
            Log.Debug(string.Format("Toasting message: {0}", message));
            _mMessage = message;
            Enabled = true;
            StartStopTimer();
        }

        public void Draw(Graphics canvas)
        {
            if (_mMessage != "" && _mCanvasHolder != null)
            {
                var bgSize = canvas.MeasureString(_mMessage, _mFont);
                bgSize = new SizeF(bgSize.Width, bgSize.Height + 3);
                var location = new PointF((_mCanvasHolder.Width - bgSize.Width) / 2,
                    (_mCanvasHolder.Height - bgSize.Height) / 2);
                var bg = new RectangleF(location.X - 5, location.Y - 5, bgSize.Width + 10, bgSize.Height + 5);
                var radius = (int)(_mFont.Size / 2);
                RoundedRectangle.Draw(canvas, bg, (SolidBrush)_mBackBrush, radius, false);
                canvas.DrawString(_mMessage, _mFont, _mForeBrush, location.X, location.Y);
            }
        }

        #endregion Public Methods

        #region Private methods

        private void Timer_OnTick(object sender, EventArgs e)
        {
            // Timer fired : Time to hide the message.
            Enabled = false;
            StartStopTimer();
        }

        private void StartStopTimer()
        {
            if (Enabled)
            {
                if (_mTimer.Enabled)
                {
                    _mTimer.Stop();
                }

                _mTimer.Start();
            }
            else
            {
                _mTimer.Stop();
                if (_mCanvasHolder != null) _mCanvasHolder.Invalidate();
            }
        }

        #endregion Private methods
    }
}