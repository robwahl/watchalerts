#region License

/*
Copyright © Joan Charmant 2009.
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
    public delegate void DelegateDraw(Graphics canvas, Size newSize, object privateData);

    public delegate void DelegateIncreaseZoom(object privateData);

    public delegate void DelegateDecreaseZoom(object privateData);

    /// <summary>
    ///     DrawtimeFilterOutput is a communication object between some filters and the player.
    ///     Once a filter is configured (and possibly preprocessed) it returns such an object.
    ///     This is used for filters that needs back and forth communication with the player.
    ///     An object of this class needs to be like a self contained filter.
    ///     It can also be used for filters that alter the image size.
    /// </summary>
    public class DrawtimeFilterOutput
    {
        public DrawtimeFilterOutput(int videoFilterType, bool bActive)
        {
            _mIVideoFilterType = videoFilterType;
            _mBActive = bActive;
        }

        #region Delegates

        public DelegateDraw Draw;
        public DelegateIncreaseZoom IncreaseZoom;
        public DelegateDecreaseZoom DecreaseZoom;

        #endregion Delegates

        #region Properties

        public int VideoFilterType
        {
            get { return _mIVideoFilterType; }
        }

        public bool Active
        {
            get { return _mBActive; }
        }

        public object PrivateData { get; set; }

        #endregion Properties

        #region Propertie/members

        private readonly int _mIVideoFilterType;
        private readonly bool _mBActive;

        #endregion Propertie/members
    }
}