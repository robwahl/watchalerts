using SharpVectors.Dom.Svg;
using SharpVectors.Dom.Svg.Rendering;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi
{
    /// <summary>
    ///     Summary description for SvgImageGraphicsNode.
    /// </summary>
    public class SvgImageElementGraphicsNode : GraphicsNode
    {
        private readonly ISvgRenderer _gdiRenderer = new GdiRenderer();

        #region Constructor

        public SvgImageElementGraphicsNode(SvgElement element)
            : base(element)
        {
        }

        #endregion Constructor

        private SvgWindow GetSvgWindow()
        {
            var iElm = Element as SvgImageElement;
            if (iElm != null)
            {
                var wnd = iElm.SvgWindow;
                wnd.Renderer = _gdiRenderer;
                _gdiRenderer.Window = wnd;
            }
            return null;
        }

        public override void Render(ISvgRenderer renderer)
        {
            var graphics = ((GdiRenderer)renderer).GraphicsWrapper;
            var iElement = (SvgImageElement)element;
            //HttpResource resource = iElement.ReferencedResource;

            /*if (resource != null )
            {*/
            var imageAttributes = new ImageAttributes();

            var sOpacity = iElement.GetPropertyValue("opacity");
            if (sOpacity.Length > 0)
            {
                double opacity = SvgNumber.ParseToFloat(sOpacity);
                var myColorMatrix = new ColorMatrix
                {
                    Matrix00 = 1.00f,
                    Matrix11 = 1.00f,
                    Matrix22 = 1.00f,
                    Matrix33 = (float)opacity,
                    Matrix44 = 1.00f
                };
                // Red
                // Green
                // Blue
                // alpha
                // w

                imageAttributes.SetColorMatrix(myColorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            }

            var width = (float)iElement.Width.AnimVal.Value;
            var height = (float)iElement.Height.AnimVal.Value;

            var destRect = new Rectangle
            {
                X = Convert.ToInt32(iElement.X.AnimVal.Value),
                Y = Convert.ToInt32(iElement.Y.AnimVal.Value),
                Width = Convert.ToInt32(width),
                Height = Convert.ToInt32(height)
            };

            Image image;
            if (iElement.IsSvgImage)
            {
                var wnd = GetSvgWindow();
                //_gdiRenderer.Render(Color) = Color.Empty;
                _gdiRenderer.Render(wnd.Document as SvgDocument);

                //wnd.Render();
                image = _gdiRenderer.RasterImage;
                image.Save(@"c:\inlinesvg.png", ImageFormat.Png);
            }
            else
            {
                image = iElement.Bitmap;
            }

            if (image != null)
            {
                graphics.DrawImage(this, image, destRect, 0f, 0f, image.Width, image.Height, GraphicsUnit.Pixel,
                    imageAttributes);
            }
            //}
        }
    }
}