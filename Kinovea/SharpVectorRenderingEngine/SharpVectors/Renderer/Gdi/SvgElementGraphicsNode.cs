using SharpVectors.Dom.Svg;
using SharpVectors.Dom.Svg.Rendering;
using System.Drawing;

namespace SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi
{
    /// <summary>
    ///     Summary description for SvgElementGraphicsNode.
    /// </summary>
    public class SvgElementGraphicsNode : GraphicsNode
    {
        #region Constructor

        public SvgElementGraphicsNode(SvgElement element)
            : base(element)
        {
        }

        #endregion Constructor

        #region Public Methods

        public override void Render(ISvgRenderer renderer)
        {
            var graphics = ((GdiRenderer)renderer).GraphicsWrapper;

            var svgElm = (SvgSvgElement)element;

            var x = (float)svgElm.X.AnimVal.Value;
            var y = (float)svgElm.Y.AnimVal.Value;
            var width = (float)svgElm.Width.AnimVal.Value;
            var height = (float)svgElm.Height.AnimVal.Value;

            var elmRect = new RectangleF(x, y, width, height);

            FitToViewbox(graphics, elmRect);
        }

        #endregion Public Methods
    }
}