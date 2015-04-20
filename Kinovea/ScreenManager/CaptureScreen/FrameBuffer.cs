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

using Kinovea.Services;
using log4net;
using System.Drawing;
using System.Reflection;
using Image = AForge.Imaging.Image;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     FrameBuffer - a buffer that holds the recent history of grabbed frames.
    ///     This is an In-Memory Buffer
    ///     Handles buffer rotation.
    ///     Frames are inserted at the tail.
    ///     Head represents the first frame not read yet.
    /// </summary>
    public class FrameBuffer
    {
        #region Constructor

        public FrameBuffer()
        {
            _mBuffer = new Bitmap[Capacity];
        }

        #endregion Constructor

        private void ResetBuffer()
        {
            // Buffer capacity.
            Log.Debug("Capture buffer reset");
            var bytesPerFrame = _mSize.Width*_mSize.Height*3;
            var capacity = (int) (((double) _mICaptureMemoryBuffer*1048576)/bytesPerFrame);
            Capacity = capacity > 0 ? capacity : 1;

            // Reset the buffer.
            Clear();
            _mBuffer = new Bitmap[Capacity];
        }

        #region Properties

        public int Capacity { get; private set; } = 1;

        public int FillPercentage
        {
            get { return (int) ((_mIFill/(double) Capacity)*100); }
        }

        #endregion Properties

        #region Members

        private Size _mSize = new Size(640, 480);
        private int _mICaptureMemoryBuffer = 16;
        private Bitmap[] _mBuffer;
        private int _mIHead; // next spot to read from.
        private int _mITail; // next spot to write to.
        private int _mIToRead; // number of spots that were written but not read yet.
        private int _mIFill; // number of spots that were written.
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Members

        #region Public methods

        public void Write(Bitmap bmp)
        {
            // A frame is received and must be stored.
            if (_mBuffer[_mITail] != null)
            {
                _mBuffer[_mITail].Dispose();
                _mBuffer[_mITail] = null;
            }

            // Copy the image to its final place in the buffer.
            if (!bmp.Size.Equals(_mSize))
            {
                // Copy and resize.
                _mBuffer[_mITail] = new Bitmap(_mSize.Width, _mSize.Height);
                var g = Graphics.FromImage(_mBuffer[_mITail]);

                var rDst = new Rectangle(0, 0, _mSize.Width, _mSize.Height);
                RectangleF rSrc = new Rectangle(0, 0, bmp.Width, bmp.Height);
                g.DrawImage(bmp, rDst, rSrc, GraphicsUnit.Pixel);
            }
            else
            {
                // simple copy.
                _mBuffer[_mITail] = Image.Clone(bmp);
            }

            if (_mIFill < Capacity) _mIFill++;
            _mIToRead++;
            _mITail++;
            if (_mITail == Capacity) _mITail = 0;

            //log.Debug(String.Format("Wrote frame. tail:{0}, head:{1}, count:{2}", m_iTail, m_iHead, m_iFill));
        }

        public Bitmap ReadAt(int index)
        {
            // Read frame at specified index.
            Bitmap frame = null;

            if (_mIFill > index)
            {
                // Head is always the oldest frame that haven't been read yet.
                // What we want is a frame that is only old of _index frames.
                var spot = _mITail - 1 - index;
                if (spot < 0)
                    spot = Capacity + spot;

                frame = _mBuffer[spot];
                if (frame != null)
                {
                    _mIToRead--;
                    _mIHead++;
                    if (_mIHead == Capacity) _mIHead = 0;
                }
            }
            else if (_mIFill > 0)
            {
                // There's not enough images in the buffer to reach that index.
                // Return last image from the buffer but don't touch the sentinels.
                frame = _mBuffer[_mIHead];
            }

            //log.Debug(String.Format("Read frame. tail:{0}, head:{1}, count:{2}", m_iTail, m_iHead, m_iFill));
            return frame;
        }

        public void Clear()
        {
            // Release all non managed resources.
            //log.Debug(String.Format("Clearing frame. tail:{0}, head:{1}", m_iTail, m_iHead));
            for (var i = 0; i < _mBuffer.Length; i++)
            {
                if (_mBuffer[i] != null)
                {
                    _mBuffer[i].Dispose();
                }
            }

            _mIHead = 0;
            _mITail = 0;
            _mIToRead = 0;
            _mIFill = 0;
        }

        public void UpdateFrameSize(Size size)
        {
            // The buffer directly keep the images at the final display size.
            // This avoid an extra copy when the display size is not the decoding size. (force 16:9 for example).
            if (!_mSize.Equals(size))
            {
                _mSize = size;
                ResetBuffer();
            }
        }

        public void UpdateMemoryCapacity(bool bShared)
        {
            // This is called when the memory cache size is changed in the preferences.
            var pm = PreferencesManager.Instance();
            var iAllocatedMemory = bShared ? pm.CaptureMemoryBuffer/2 : pm.CaptureMemoryBuffer;
            if (iAllocatedMemory != _mICaptureMemoryBuffer)
            {
                Log.DebugFormat("Changing memory capacity from {0} to {1}", _mICaptureMemoryBuffer, iAllocatedMemory);
                _mICaptureMemoryBuffer = iAllocatedMemory;
                ResetBuffer();
            }
        }

        #endregion Public methods
    }
}