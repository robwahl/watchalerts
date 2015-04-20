using SharpVectors.Dom.Svg;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

/*using System.Windows.Forms;*/

namespace SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi
{
    /// <summary>
    ///     Wraps a Graphics object since it's sealed
    /// </summary>
    public class GraphicsWrapper : IDisposable
    {
        public static GraphicsWrapper FromImage(Image image, bool isStatic)
        {
            return new GraphicsWrapper(image, isStatic);
        }

        public static GraphicsWrapper FromHdc(IntPtr hdc, bool isStatic)
        {
            return new GraphicsWrapper(hdc, isStatic);
        }

        #region Private Fields

        private bool _isStatic;
        private Graphics _idMapGraphics;

        #endregion Private Fields

        #region Constructors

        private GraphicsWrapper(Image image, bool isStatic)
        {
            _isStatic = isStatic;
            if (!IsStatic)
            {
                IdMapRaster = new Bitmap(image.Width, image.Height);
                _idMapGraphics = Graphics.FromImage(IdMapRaster);
                _idMapGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                _idMapGraphics.SmoothingMode = SmoothingMode.None;
                _idMapGraphics.CompositingQuality = CompositingQuality.Invalid;
            }
            Graphics = Graphics.FromImage(image);
        }

        private GraphicsWrapper(IntPtr hdc, bool isStatic)
        {
            _isStatic = isStatic;
            if (!IsStatic)
            {
                // This will get resized when the actual size is known
                IdMapRaster = new Bitmap(0, 0);
                _idMapGraphics = Graphics.FromImage(IdMapRaster);
                _idMapGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                _idMapGraphics.SmoothingMode = SmoothingMode.None;
                _idMapGraphics.CompositingQuality = CompositingQuality.Invalid;
            }
            Graphics = Graphics.FromHdc(hdc);
        }

        #endregion Constructors

        #region Properties

        public bool IsStatic
        {
            get { return _isStatic; }
            set
            {
                _isStatic = value;
                _idMapGraphics.Dispose();
                _idMapGraphics = null;
            }
        }

        public Graphics Graphics { get; set; }

        public Graphics IdMapGraphics
        {
            get { return Graphics; }
        }

        public Bitmap IdMapRaster { get; private set; }

        #endregion Properties

        #region Graphics members

        public void Clear(Color color)
        {
            Graphics.Clear(color);
            if (_idMapGraphics != null) _idMapGraphics.Clear(Color.Empty);
        }

        public void Dispose()
        {
            Graphics.Dispose();
            if (_idMapGraphics != null) _idMapGraphics.Dispose();
        }

        public GraphicsContainerWrapper BeginContainer()
        {
            var container = new GraphicsContainerWrapper();
            if (_idMapGraphics != null) container.IdmapGraphicsContainer = _idMapGraphics.BeginContainer();
            container.MainGraphicsContainer = Graphics.BeginContainer();
            return container;
        }

        public void EndContainer(GraphicsContainerWrapper container)
        {
            if (_idMapGraphics != null) _idMapGraphics.EndContainer(container.IdmapGraphicsContainer);
            Graphics.EndContainer(container.MainGraphicsContainer);
        }

        public SmoothingMode SmoothingMode
        {
            get { return Graphics.SmoothingMode; }
            set { Graphics.SmoothingMode = value; }
        }

        public Matrix Transform
        {
            get { return Graphics.Transform; }
            set
            {
                if (_idMapGraphics != null) _idMapGraphics.Transform = value;
                Graphics.Transform = value;
            }
        }

        public void SetClip(GraphicsPath path)
        {
            Graphics.SetClip(path);
        }

        public void SetClip(RectangleF rect)
        {
            if (_idMapGraphics != null) _idMapGraphics.SetClip(rect);
            Graphics.SetClip(rect);
        }

        public void SetClip(Region region, CombineMode combineMode)
        {
            if (_idMapGraphics != null) _idMapGraphics.SetClip(region, combineMode);
            Graphics.SetClip(region, combineMode);
        }

        public void TranslateClip(float x, float y)
        {
            if (_idMapGraphics != null) _idMapGraphics.TranslateClip(x, y);
            Graphics.TranslateClip(x, y);
        }

        public void ResetClip()
        {
            if (_idMapGraphics != null) _idMapGraphics.ResetClip();
            Graphics.ResetClip();
        }

        public void FillPath(GraphicsNode grNode, Brush brush, GraphicsPath path)
        {
            if (_idMapGraphics != null)
            {
                Brush idBrush = new SolidBrush(grNode.UniqueColor);
                if (grNode.Element is SvgTextContentElement)
                {
                    _idMapGraphics.FillRectangle(idBrush, path.GetBounds());
                }
                else
                {
                    _idMapGraphics.FillPath(idBrush, path);
                }
            }
            Graphics.FillPath(brush, path);
        }

        public void DrawPath(GraphicsNode grNode, Pen pen, GraphicsPath path)
        {
            if (_idMapGraphics != null)
            {
                var idPen = new Pen(grNode.UniqueColor, pen.Width);
                _idMapGraphics.DrawPath(idPen, path);
            }
            Graphics.DrawPath(pen, path);
        }

        public void TranslateTransform(float dx, float dy)
        {
            if (_idMapGraphics != null) _idMapGraphics.TranslateTransform(dx, dy);
            Graphics.TranslateTransform(dx, dy);
        }

        public void ScaleTransform(float sx, float sy)
        {
            if (_idMapGraphics != null) _idMapGraphics.ScaleTransform(sx, sy);
            Graphics.ScaleTransform(sx, sy);
        }

        public void RotateTransform(float angle)
        {
            if (_idMapGraphics != null) _idMapGraphics.RotateTransform(angle);
            Graphics.RotateTransform(angle);
        }

        public void DrawImage(GraphicsNode grNode, Image image, Rectangle destRect, float srcX, float srcY,
            float srcWidth, float srcHeight, GraphicsUnit graphicsUnit, ImageAttributes imageAttributes)
        {
            if (_idMapGraphics != null)
            {
                // This handles pointer-events for visibleFill visibleStroke and visible
                /*Brush idBrush = new SolidBrush(grNode.UniqueColor);
                GraphicsPath gp = new GraphicsPath();
                gp.AddRectangle(destRect);
                _idMapGraphics.FillPath(idBrush, gp);*/
                var unique = grNode.UniqueColor;
                var r = (float)unique.R / 255;
                var g = (float)unique.G / 255;
                var b = (float)unique.B / 255;
                var colorMatrix = new ColorMatrix(
                    new[]
                    {
                        new[] {0f, 0f, 0f, 0f, 0f},
                        new[] {0f, 0f, 0f, 0f, 0f},
                        new[] {0f, 0f, 0f, 0f, 0f},
                        new[] {0f, 0f, 0f, 1f, 0f},
                        new[] {r, g, b, 0f, 1f}
                    });
                var ia = new ImageAttributes();
                ia.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                _idMapGraphics.DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, graphicsUnit, ia);
            }
            Graphics.DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, graphicsUnit, imageAttributes);
        }

        #endregion Graphics members
    }

    /// <summary>
    ///     Wraps a GraphicsContainer because it is sealed.
    ///     This is a helper for GraphicsWrapper so that it can save
    ///     multiple container states. It holds the containers
    ///     for both the idMapGraphics and the main graphics
    ///     being rendered in the GraphicsWrapper.
    /// </summary>
    public struct GraphicsContainerWrapper
    {
        internal GraphicsContainer IdmapGraphicsContainer;
        internal GraphicsContainer MainGraphicsContainer;
    }
}