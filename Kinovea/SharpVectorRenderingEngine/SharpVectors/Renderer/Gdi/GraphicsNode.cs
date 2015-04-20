using SharpVectors.Dom.Css;
using SharpVectors.Dom.Svg;
using SharpVectors.Dom.Svg.Rendering;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi
{
    public class GraphicsNode : RenderingNode
    {
        #region Constructor

        public GraphicsNode(SvgElement element)
            : base(element)
        {
            UniqueColor = Color.Empty;
        }

        #endregion Constructor

        #region Fields

        public Color UniqueColor { get; private set; }

        protected GraphicsContainerWrapper GraphicsContainer;
        public Matrix TransformMatrix;

        #endregion Fields

        #region Protected Methods

        protected void Clip(GraphicsWrapper gr)
        {
            // todo: should we correct the clipping to adjust to the off-one-pixel drawing?
            gr.TranslateClip(1, 1);

            #region Clip with clip

            // see http://www.w3.org/TR/SVG/masking.html#OverflowAndClipProperties
            if (element is ISvgSvgElement ||
                element is ISvgMarkerElement ||
                element is ISvgSymbolElement ||
                element is ISvgPatternElement)
            {
                // check overflow property
                var overflow = ((SvgElement)element).GetComputedCssValue("overflow", string.Empty) as CssValue;
                // TODO: clip can have "rect(10 10 auto 10)"
                var clip = ((SvgElement)element).GetComputedCssValue("clip", string.Empty) as CssPrimitiveValue;

                string sOverflow = null;

                if (overflow != null)
                {
                    sOverflow = overflow.CssText;
                }

                if (sOverflow != null)
                {
                    // "If the 'overflow' property has a value other than hidden or scroll, the property has no effect (i.e., a clipping rectangle is not created)."
                    if (sOverflow == "hidden" || sOverflow == "scroll")
                    {
                        var clipRect = RectangleF.Empty;
                        var svgSvgElement = element as ISvgSvgElement;
                        if (clip != null && clip.PrimitiveType == CssPrimitiveType.Rect)
                        {
                            if (svgSvgElement != null)
                            {
                                var svgElement = svgSvgElement;
                                var viewPort = svgElement.Viewport as SvgRect;
                                if (viewPort != null) clipRect = viewPort.ToRectangleF();
                                IRect clipShape = (Rect)clip.GetRectValue();
                                if (clipShape.Top.PrimitiveType != CssPrimitiveType.Ident)
                                    clipRect.Y += (float)clipShape.Top.GetFloatValue(CssPrimitiveType.Number);
                                if (clipShape.Left.PrimitiveType != CssPrimitiveType.Ident)
                                    clipRect.X += (float)clipShape.Left.GetFloatValue(CssPrimitiveType.Number);
                                if (clipShape.Right.PrimitiveType != CssPrimitiveType.Ident)
                                    clipRect.Width = (clipRect.Right - clipRect.X) -
                                                     (float)clipShape.Right.GetFloatValue(CssPrimitiveType.Number);
                                if (clipShape.Bottom.PrimitiveType != CssPrimitiveType.Ident)
                                    clipRect.Height = (clipRect.Bottom - clipRect.Y) -
                                                      (float)clipShape.Bottom.GetFloatValue(CssPrimitiveType.Number);
                            }
                        }
                        else if (clip == null ||
                                 (clip.PrimitiveType == CssPrimitiveType.Ident && clip.GetStringValue() == "auto"))
                        {
                            svgSvgElement = element as ISvgSvgElement;
                            if (svgSvgElement != null)
                            {
                                var svgElement = svgSvgElement;
                                var viewPort = svgElement.Viewport as SvgRect;
                                if (viewPort != null) clipRect = viewPort.ToRectangleF();
                            }
                        }
                        if (clipRect != RectangleF.Empty)
                        {
                            gr.SetClip(clipRect);
                        }
                    }
                }
            }

            #endregion Clip with clip

            #region Clip with clip-path

            // see: http://www.w3.org/TR/SVG/masking.html#EstablishingANewClippingPath
            if (element is IGraphicsElement ||
                element is IContainerElement)
            {
                var clipPath =
                    ((SvgElement)element).GetComputedCssValue("clip-path", string.Empty) as CssPrimitiveValue;

                if (clipPath != null && clipPath.PrimitiveType == CssPrimitiveType.Uri)
                {
                    var absoluteUri = ((SvgElement)element).ResolveUri(clipPath.GetStringValue());

                    var eClipPath =
                        ((SvgDocument)element.OwnerDocument).GetNodeByUri(absoluteUri) as SvgClipPathElement;

                    if (eClipPath != null)
                    {
                        var gpClip = eClipPath.GetGraphicsPath();

                        var pathUnits = (SvgUnitType)eClipPath.ClipPathUnits.AnimVal;

                        if (pathUnits == SvgUnitType.ObjectBoundingBox)
                        {
                            var transElement = element as SvgTransformableElement;

                            if (transElement != null)
                            {
                                var bbox = transElement.GetBBox();

                                // scale clipping path
                                var matrix = new Matrix();
                                matrix.Scale((float)bbox.Width, (float)bbox.Height);
                                gpClip.Transform(matrix);
                                gr.SetClip(gpClip);

                                // offset clip
                                gr.TranslateClip((float)bbox.X, (float)bbox.Y);
                            }
                            else
                            {
                                throw new NotImplementedException(
                                    string.Format(
                                        "clip-path with SvgUnitType.ObjectBoundingBox " +
                                        "not supported for this type of element: {0}", element.GetType()));
                            }
                        }
                        else
                        {
                            gr.SetClip(gpClip);
                        }
                    }
                }
            }

            #endregion Clip with clip-path
        }

        protected void SetQuality(GraphicsWrapper gr)
        {
            var graphics = gr.Graphics;

            var colorRendering = ((SvgElement)element).GetComputedStringValue("color-rendering", string.Empty);
            switch (colorRendering)
            {
                case "optimizeSpeed":
                    graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    break;

                case "optimizeQuality":
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    break;

                default:
                    // "auto"
                    // todo: could use AssumeLinear for slightly better
                    graphics.CompositingQuality = CompositingQuality.Default;
                    break;
            }

            var svgElement = element as SvgTextContentElement;
            if (svgElement != null)
            {
                // Unfortunately the text rendering hints are not applied because the
                // text path is recorded and painted to the Graphics object as a path
                // not as text.
                var textRendering = svgElement.GetComputedStringValue("text-rendering", string.Empty);
                if (textRendering == "optimizeSpeed")
                    graphics.SmoothingMode = SmoothingMode.HighSpeed;
                //graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;
                else if (textRendering == "optimizeLegibility")
                {
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    //graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                }
                else if (textRendering == "geometricPrecision")
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    //graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                }
                else
                {
                    // "auto"
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    //graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
                }
            }
            else
            {
                var shapeRendering = ((SvgElement)element).GetComputedStringValue("shape-rendering", string.Empty);
                switch (shapeRendering)
                {
                    case "optimizeSpeed":
                        graphics.SmoothingMode = SmoothingMode.HighSpeed;
                        break;

                    case "crispEdges":
                        graphics.SmoothingMode = SmoothingMode.None;
                        break;

                    case "geometricPrecision":
                        graphics.SmoothingMode = SmoothingMode.HighQuality;
                        break;

                    default:
                        // "auto"
                        graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        break;
                }
            }
        }

        protected void Transform(GraphicsWrapper gr)
        {
            var elm = element as ISvgTransformable;
            if (elm != null)
            {
                if (TransformMatrix == null)
                {
                    var transElm = elm;
                    //SvgTransform svgTransform = (SvgTransform)svgTList.Consolidate();
                    var svgMatrix = ((SvgTransformList)transElm.Transform.AnimVal).TotalMatrix;

                    TransformMatrix = new Matrix(
                        svgMatrix.A,
                        svgMatrix.B,
                        svgMatrix.C,
                        svgMatrix.D,
                        svgMatrix.E,
                        svgMatrix.F);
                }
                gr.Transform = TransformMatrix;
            }
        }

        protected void FitToViewbox(GraphicsWrapper graphics, RectangleF elmRect)
        {
            var box = element as ISvgFitToViewBox;
            if (box != null)
            {
                var fitToVbElm = box;
                var spar = (SvgPreserveAspectRatio)fitToVbElm.PreserveAspectRatio.AnimVal;

                var translateAndScale = spar.FitToViewBox(
                    (SvgRect)fitToVbElm.ViewBox.AnimVal,
                    new SvgRect(elmRect.X, elmRect.Y, elmRect.Width, elmRect.Height)
                    );
                if (translateAndScale != null)
                {
                    graphics.TranslateTransform(translateAndScale[0], translateAndScale[1]);
                    graphics.ScaleTransform(translateAndScale[2], translateAndScale[3]);
                }
            }
        }

        #endregion Protected Methods

        #region Public Methods

        public override void BeforeRender(ISvgRenderer renderer)
        {
            if (UniqueColor.IsEmpty)
                UniqueColor = ((GdiRenderer)renderer)._getNextColor(this);

            var graphics = ((GdiRenderer)renderer).GraphicsWrapper;

            GraphicsContainer = graphics.BeginContainer();
            SetQuality(graphics);
            Transform(graphics);
            Clip(graphics);
        }

        public override void AfterRender(ISvgRenderer renderer)
        {
            var graphics = ((GdiRenderer)renderer).GraphicsWrapper;

            graphics.EndContainer(GraphicsContainer);
        }

        #endregion Public Methods
    }
}