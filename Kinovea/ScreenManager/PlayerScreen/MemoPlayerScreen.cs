/*
Copyright � Joan Charmant 2009.
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

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     this class stores a state of a PlayerScreen in order to reinstate it later.
    /// </summary>
    public class MemoPlayerScreen
    {
        public MemoPlayerScreen(long iSelStart, long iSelEnd)
        {
            SelStart = iSelStart;
            SelEnd = iSelEnd;
        }

        #region Properties

        public long SelStart { get; set; }

        public long SelEnd { get; set; }

        #endregion Properties
    }
}