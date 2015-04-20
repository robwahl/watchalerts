using SharpVectors.Dom.Svg;
using System.Drawing;

namespace SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi.Paint
{
    public abstract class PaintServer
    {
        public static PaintServer CreatePaintServer(SvgDocument document, string absoluteUri)
        {
            var node = document.GetNodeByUri(absoluteUri);

            if (node is SvgGradientElement)
            {
                return new GradientPaintServer((SvgGradientElement)node);
            }
            var element = node as SvgPatternElement;
            if (element != null)
                return new PatternPaintServer(element);
            return null;
        }

        public abstract Brush GetBrush(RectangleF bounds);
    }
}