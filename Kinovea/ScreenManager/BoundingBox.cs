using System.Drawing;

namespace Kinovea.ScreenManager
{
    /// <summary>
    ///     A helper class for drawings using a bounding box.
    ///     The drawing should defer part of its hit testing, move handle and move drawing methods to it.
    ///     By convention, the handles id of the bounding box will always be 1 to 4.
    ///     When the drawing has other handles, they should start at id 5.
    /// </summary>
    public class BoundingBox
    {
        #region Members

        private Rectangle _mRectangle;

        #endregion Members

        #region Properties

        public Rectangle Rectangle
        {
            get { return _mRectangle; }
            set { _mRectangle = value; }
        }

        #endregion Properties

        public void Draw(Graphics canvas, Rectangle rect, Pen pen, SolidBrush brush, int widen)
        {
            canvas.DrawRectangle(pen, rect);
            canvas.FillEllipse(brush, rect.Left - widen, rect.Top - widen, widen * 2, widen * 2);
            canvas.FillEllipse(brush, rect.Left - widen, rect.Bottom - widen, widen * 2, widen * 2);
            canvas.FillEllipse(brush, rect.Right - widen, rect.Top - widen, widen * 2, widen * 2);
            canvas.FillEllipse(brush, rect.Right - widen, rect.Bottom - widen, widen * 2, widen * 2);
        }

        public int HitTest(Point point)
        {
            var iHitResult = -1;

            var topLeft = _mRectangle.Location;
            var topRight = new Point(_mRectangle.Right, _mRectangle.Top);
            var botRight = new Point(_mRectangle.Right, _mRectangle.Bottom);
            var botLeft = new Point(_mRectangle.Left, _mRectangle.Bottom);

            var widen = 6;
            if (topLeft.Box(widen).Contains(point))
                iHitResult = 1;
            else if (topRight.Box(widen).Contains(point))
                iHitResult = 2;
            else if (botRight.Box(widen).Contains(point))
                iHitResult = 3;
            else if (botLeft.Box(widen).Contains(point))
                iHitResult = 4;
            else if (_mRectangle.Contains(point))
                iHitResult = 0;

            return iHitResult;
        }

        public void MoveHandle(Point point, int handleNumber, Size originalSize)
        {
            // Force aspect ratio for now.
            switch (handleNumber)
            {
                case 1:
                    {
                        // Top left handler.
                        var dx = point.X - _mRectangle.Left;
                        var newWidth = _mRectangle.Width - dx;

                        if (newWidth > 50)
                        {
                            var qRatio = newWidth / (double)originalSize.Width;
                            var newHeight = (int)(originalSize.Height * qRatio); // Only if square.

                            var newY = _mRectangle.Top + _mRectangle.Height - newHeight;

                            _mRectangle = new Rectangle(point.X, newY, newWidth, newHeight);
                        }
                        break;
                    }
                case 2:
                    {
                        // Top right handler.
                        var dx = _mRectangle.Right - point.X;
                        var newWidth = _mRectangle.Width - dx;

                        if (newWidth > 50)
                        {
                            var qRatio = newWidth / (double)originalSize.Width;
                            var newHeight = (int)(originalSize.Height * qRatio); // Only if square.

                            var newY = _mRectangle.Top + _mRectangle.Height - newHeight;
                            var newX = point.X - newWidth;

                            _mRectangle = new Rectangle(newX, newY, newWidth, newHeight);
                        }
                        break;
                    }
                case 3:
                    {
                        // Bottom right handler.
                        var dx = _mRectangle.Right - point.X;
                        var newWidth = _mRectangle.Width - dx;

                        if (newWidth > 50)
                        {
                            var qRatio = newWidth / (double)originalSize.Width;
                            var newHeight = (int)(originalSize.Height * qRatio); // Only if square.

                            var newY = _mRectangle.Y;
                            var newX = point.X - newWidth;

                            _mRectangle = new Rectangle(newX, newY, newWidth, newHeight);
                        }
                        break;
                    }
                case 4:
                    {
                        // Bottom left handler.
                        var dx = point.X - _mRectangle.Left;
                        var newWidth = _mRectangle.Width - dx;

                        if (newWidth > 50)
                        {
                            var qRatio = newWidth / (double)originalSize.Width;
                            var newHeight = (int)(originalSize.Height * qRatio); // Only if square.

                            var newY = _mRectangle.Y;

                            _mRectangle = new Rectangle(point.X, newY, newWidth, newHeight);
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        public void Move(int deltaX, int deltaY)
        {
            _mRectangle = new Rectangle(_mRectangle.X + deltaX, _mRectangle.Y + deltaY, _mRectangle.Width,
                _mRectangle.Height);
        }
    }
}