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

using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     A class representing a quadrilateral, with some helper methods.
    ///     The corners can be accessed via ABCD properties, the indexer or the enumerator.
    ///     When using the indexer, A=0, B=1, C=2, D=3.
    ///     Points are defined clockwise, "A" being top left.
    ///     Note that unlike Rectangle, this is a reference type.
    /// </summary>
    public class Quadrilateral : IEnumerable
    {
        #region Properties

        public Point A
        {
            get { return _mCorners[0]; }
            set { _mCorners[0] = value; }
        }

        public Point B
        {
            get { return _mCorners[1]; }
            set { _mCorners[1] = value; }
        }

        public Point C
        {
            get { return _mCorners[2]; }
            set { _mCorners[2] = value; }
        }

        public Point D
        {
            get { return _mCorners[3]; }
            set { _mCorners[3] = value; }
        }

        public Point this[int corner]
        {
            get { return _mCorners[corner]; }
            set { _mCorners[corner] = value; }
        }

        public bool IsConvex
        {
            get { return IsQuadConvex(); }
        }

        public bool IsRectangle
        {
            get { return (A.Y == B.Y && B.X == C.X && C.Y == D.Y && D.X == A.X); }
        }

        public static Quadrilateral UnitRectangle
        {
            get
            {
                var q = new Quadrilateral
                {
                    A = new Point(0, 0),
                    B = new Point(1, 0),
                    C = new Point(1, 1),
                    D = new Point(0, 1)
                };
                return q;
            }
        }

        #endregion Properties

        #region Members

        private Point[] _mCorners = new Point[4];
        private const double RadToDeg = 180D / Math.PI;

        #endregion Members

        #region Public methods

        public void Translate(int x, int y)
        {
            _mCorners = _mCorners.Select(p => p.Translate(x, y)).ToArray();
        }

        public void Expand(int width, int height)
        {
            A = A.Translate(-width, -height);
            B = B.Translate(width, -height);
            C = C.Translate(width, height);
            D = D.Translate(-width, height);
        }

        public void MakeRectangle(int anchor)
        {
            // Forces the other points to align with the anchor.
            // Assumes the opposite point is already aligned with the other two.
            switch (anchor)
            {
                case 0:
                    B = new Point(B.X, A.Y);
                    D = new Point(A.X, D.Y);
                    break;

                case 1:
                    A = new Point(A.X, B.Y);
                    C = new Point(B.X, C.Y);
                    break;

                case 2:
                    D = new Point(D.X, C.Y);
                    B = new Point(C.X, B.Y);
                    break;

                case 3:
                    C = new Point(C.X, D.Y);
                    A = new Point(D.X, A.Y);
                    break;
            }
        }

        public bool Contains(Point point)
        {
            if (!IsQuadConvex())
                return false;

            var areaPath = new GraphicsPath();
            areaPath.AddLine(A, B);
            areaPath.AddLine(B, C);
            areaPath.AddLine(C, D);
            areaPath.CloseAllFigures();
            var areaRegion = new Region(areaPath);

            return areaRegion.IsVisible(point);
        }

        public Quadrilateral Clone()
        {
            return new Quadrilateral { A = A, B = B, C = C, D = D };
        }

        public IEnumerator GetEnumerator()
        {
            return _mCorners.GetEnumerator();
        }

        public Point[] ToArray()
        {
            return _mCorners.ToArray();
        }

        #endregion Public methods

        #region Private methods

        private bool IsQuadConvex()
        {
            // Angles must all be > 180 or all < 180.
            var angles = new double[4];
            angles[0] = GetAngle(A, B, C);
            angles[1] = GetAngle(B, C, D);
            angles[2] = GetAngle(C, D, A);
            angles[3] = GetAngle(D, A, B);

            if ((angles[0] > 0 && angles[1] > 0 && angles[2] > 0 && angles[3] > 0) ||
                (angles[0] < 0 && angles[1] < 0 && angles[2] < 0 && angles[3] < 0))
            {
                return true;
            }
            return false;
        }

        private double GetAngle(Point a, Point b, Point c)
        {
            // Compute the angle ABC.
            // using scalar and vector product between vectors BA and BC.

            double bax = a.X - b.X;
            double bcx = c.X - b.X;
            var scalX = bax * bcx;

            double bay = a.Y - b.Y;
            double bcy = c.Y - b.Y;
            var scalY = bay * bcy;

            var scal = scalX + scalY;

            var normab = Math.Sqrt(bax * bax + bay * bay);
            var normbc = Math.Sqrt(bcx * bcx + bcy * bcy);
            var norm = normab * normbc;

            var angle = Math.Acos(scal / norm);

            if ((bax * bcy - bay * bcx) < 0)
            {
                angle = -angle;
            }

            return angle * RadToDeg;
        }

        #endregion Private methods
    }
}