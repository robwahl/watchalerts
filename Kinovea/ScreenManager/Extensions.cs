using System.Drawing;

namespace Kinovea.ScreenManager
{
    public static class Extensions
    {
        /// <summary>
        ///     Get a bounding box around a point.
        /// </summary>
        public static Rectangle Box(this Point point, int radius)
        {
            return new Rectangle(point.X - radius, point.Y - radius, radius * 2, radius * 2);
        }

        /// <summary>
        ///     Get a bounding box around a point.
        /// </summary>
        public static Rectangle Box(this Point point, Size size)
        {
            return new Rectangle(point.X - size.Width / 2, point.Y - size.Height / 2, size.Width, size.Height);
        }

        /// <summary>
        ///     Translate a point by x pixels horizontally, y pixels vertically.
        /// </summary>
        public static Point Translate(this Point point, int x, int y)
        {
            return new Point(point.X + x, point.Y + y);
        }

        /// <summary>
        ///     Get the complementary color.
        /// </summary>
        public static Color Invert(this Color color)
        {
            return Color.FromArgb(color.A, 255 - color.R, 255 - color.G, 255 - color.B);
        }
    }
}