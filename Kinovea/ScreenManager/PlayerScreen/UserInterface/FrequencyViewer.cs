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

using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public partial class FrequencyViewer : UserControl
    {
        public FrequencyViewer()
        {
            InitializeComponent();

            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _mIHorizontalLines = 5;
            _mITotal = 10000;
            _mIInterval = 1000;
        }

        private void FrequencyViewer_Paint(object sender, PaintEventArgs e)
        {
            //-------------------
            // Drawing the lines.
            //-------------------

            // 1. Horizontal lines
            for (var i = 0; i < _mIHorizontalLines; i++)
            {
                e.Graphics.DrawLine(Pens.Gray, 0, (Height / _mIHorizontalLines) * i, Width, (Height / _mIHorizontalLines) * i);
            }
            e.Graphics.DrawLine(Pens.Gray, 0, Height - 1, Width, Height - 1);

            // 2. Vertical lines (the real visual information)
            for (var i = 0; i < _mITotal / _mIInterval; i++)
            {
                var iAbscisse = i * _mIInterval;
                var iLineX = (int)((iAbscisse * (double)Width) / _mITotal);
                e.Graphics.DrawLine(Pens.Gray, iLineX, 0, iLineX, Height);
            }
            e.Graphics.DrawLine(Pens.Gray, Width - 1, 0, Width - 1, Height);
        }

        // The values here are completely uncorrelated with the real values.

        #region Properties

        public int HorizontalLines
        {
            get { return _mIHorizontalLines; }
            set
            {
                if (value < 1) value = 1;
                _mIHorizontalLines = value;
                Invalidate();
            }
        }

        public int Total
        {
            get { return _mITotal; }
            set
            {
                if (value < _mIInterval) value = _mIInterval;
                _mITotal = value;
                Invalidate();
            }
        }

        public int Interval
        {
            get { return _mIInterval; }
            set
            {
                if (value < 1) value = 1;
                _mIInterval = value;
                Invalidate();
            }
        }

        #endregion Properties

        #region Members

        private int _mIHorizontalLines;
        private int _mITotal;
        private int _mIInterval;

        #endregion Members
    }
}