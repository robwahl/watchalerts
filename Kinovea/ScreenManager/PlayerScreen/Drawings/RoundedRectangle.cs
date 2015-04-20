using System.Drawing;
using System.Drawing.Drawing2D;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     A helper class to draw a rounded rectangle for labels.
    ///     The rectangle can have a drop shape (top left and bottom right corners are "pointy").
    ///     It can also have a hidden handler in the bottom right corner.
    ///     Change of size resulting from moving the hidden handler is the responsibility of the caller.
    /// </summary>
    public class RoundedRectangle
    {
        #region Members

        private Rectangle _mRectangle;

        #endregion Members

        /// <summary>
        ///     Draw a rounded rectangle on the provided canvas.
        ///     This method is typically used after applying a transform to the original rectangle.
        /// </summary>
        /// <param name="canvas">The graphics object on which to draw</param>
        /// <param name="rect">The rectangle specifications</param>
        /// <param name="brush">Brush to draw with</param>
        /// <param name="radius">Radius of the rounded corners</param>
        public static void Draw(Graphics canvas, RectangleF rect, SolidBrush brush, int radius, bool dropShape)
        {
            var diameter = 2F * radius;
            var arc = new RectangleF(rect.Location, new SizeF(diameter, diameter));

            var gp = new GraphicsPath();
            gp.StartFigure();

            if (dropShape)
                gp.AddLine(arc.Left, arc.Top, arc.Right, arc.Top);
            else
                gp.AddArc(arc, 180, 90);

            arc.X = rect.Right - diameter;
            gp.AddArc(arc, 270, 90);

            arc.Y = rect.Bottom - diameter;
            if (dropShape)
                gp.AddLine(arc.Right, arc.Top, arc.Right, arc.Bottom);
            else
                gp.AddArc(arc, 0, 90);

            arc.X = rect.Left;
            gp.AddArc(arc, 90, 90);

            gp.CloseFigure();
            canvas.FillPath(brush, gp);
        }

        public int HitTest(Point point, bool hiddenHandle)
        {
            var iHitResult = -1;
            if (hiddenHandle)
            {
                var botRight = new Point(_mRectangle.Right, _mRectangle.Bottom);
                if (botRight.Box(10).Contains(point))
                    iHitResult = 1;
            }

            if (iHitResult < 0 && _mRectangle.Contains(point))
                iHitResult = 0;

            return iHitResult;
        }

        public void Move(int deltaX, int deltaY)
        {
            _mRectangle = new Rectangle(_mRectangle.X + deltaX, _mRectangle.Y + deltaY, _mRectangle.Width,
                _mRectangle.Height);
        }

        public void CenterOn(Point point)
        {
            var location = new Point(point.X - _mRectangle.Size.Width / 2, point.Y - _mRectangle.Size.Height / 2);
            _mRectangle = new Rectangle(location, _mRectangle.Size);
        }

        #region Properties

        public Rectangle Rectangle
        {
            get { return _mRectangle; }
            set { _mRectangle = value; }
        }

        public Point Center
        {
            get { return new Point(_mRectangle.X + _mRectangle.Width / 2, _mRectangle.Y + _mRectangle.Height / 2); }
        }

        public int X
        {
            get { return _mRectangle.X; }
        }

        public int Y
        {
            get { return _mRectangle.Y; }
        }

        #endregion Properties
    }
}