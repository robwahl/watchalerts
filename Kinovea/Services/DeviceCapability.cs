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

using System.Drawing;

namespace Kinovea.Services
{
    /// <summary>
    ///     DeviceCapability - one possible output for the device.
    ///     Typically, webcams will expose several capabilities (ex: 640x480@30fps, 320x200@60fps)
    /// </summary>
    public class DeviceCapability
    {
        public DeviceCapability(Size size, int framerate)
        {
            _mFrameSize = size;
            _mIFramerate = framerate;
        }

        public override string ToString()
        {
            var description = "";
            if (!_mFrameSize.IsEmpty)
            {
                description = string.Format("{0}×{1} px @ {2} fps", _mFrameSize.Width, _mFrameSize.Height, _mIFramerate);
            }
            return description;
        }

        public override bool Equals(object obj)
        {
            var equals = false;

            var dc = obj as DeviceCapability;
            if (dc != null)
            {
                equals = (_mFrameSize.Width == dc.FrameSize.Width && _mFrameSize.Height == dc.FrameSize.Height &&
                          _mIFramerate == dc.Framerate);
            }

            return equals;
        }

        public override int GetHashCode()
        {
            // Combine all relevant fields with XOR to get the Hash.
            var iHash = _mIFramerate.GetHashCode();
            iHash ^= _mFrameSize.GetHashCode();
            return iHash;
        }

        #region Properties

        public int NumberOfPixels
        {
            get { return _mFrameSize.Width * _mFrameSize.Height; }
        }

        public Size FrameSize
        {
            get { return _mFrameSize; }
        }

        public int Framerate
        {
            get { return _mIFramerate; }
        }

        #endregion Properties

        #region Members

        private readonly Size _mFrameSize;
        private readonly int _mIFramerate;

        #endregion Members
    }
}