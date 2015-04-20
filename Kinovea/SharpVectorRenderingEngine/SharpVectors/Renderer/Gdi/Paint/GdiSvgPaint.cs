using SharpVectors.Dom.Css;
using SharpVectors.Dom.Svg;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi.Paint
{
    public class GdiSvgPaint : SvgPaint
    {
        private readonly SvgStyleableElement _element;

        public GdiSvgPaint(SvgStyleableElement elm, string propName)
            : base(elm.GetComputedStyle("").GetPropertyValue(propName))
        {
            _element = elm;
        }

        #region Public properties

        public PaintServer PaintServer { get; private set; }

        #endregion Public properties

        #region Private methods

        private int GetOpacity(string fillOrStroke)
        {
            double alpha = 255;

            var opacity = _element.GetPropertyValue(fillOrStroke + "-opacity");
            if (opacity.Length > 0) alpha *= SvgNumber.ParseToFloat(opacity);

            opacity = _element.GetPropertyValue("opacity");
            if (opacity.Length > 0) alpha *= SvgNumber.ParseToFloat(opacity);

            alpha = Math.Min(alpha, 255);
            alpha = Math.Max(alpha, 0);

            return Convert.ToInt32(alpha);
        }

        private LineCap GetLineCap()
        {
            switch (_element.GetPropertyValue("stroke-linecap"))
            {
                case "round":
                    return LineCap.Round;

                case "square":
                    return LineCap.Square;

                default:
                    return LineCap.Flat;
            }
        }

        private LineJoin GetLineJoin()
        {
            switch (_element.GetPropertyValue("stroke-linejoin"))
            {
                case "round":
                    return LineJoin.Round;

                case "bevel":
                    return LineJoin.Bevel;

                default:
                    return LineJoin.Miter;
            }
        }

        private float GetStrokeWidth()
        {
            var strokeWidth = _element.GetPropertyValue("stroke-width");
            if (strokeWidth.Length == 0) strokeWidth = "1px";

            var strokeWidthLength = new SvgLength(_element, "stroke-width", SvgLengthDirection.Viewport, strokeWidth);
            return (float)strokeWidthLength.Value;
        }

        private float GetMiterLimit()
        {
            var miterLimitStr = _element.GetPropertyValue("stroke-miterlimit");
            if (miterLimitStr.Length == 0) miterLimitStr = "4";

            var miterLimit = SvgNumber.ParseToFloat(miterLimitStr);
            if (miterLimit < 1)
                throw new SvgException(SvgExceptionType.SvgInvalidValueErr, "stroke-miterlimit can not be less then 1");

            return miterLimit;
        }

        private float[] GetDashArray(float strokeWidth)
        {
            var dashArray = _element.GetPropertyValue("stroke-dasharray");

            if (dashArray.Length == 0 || dashArray == "none")
            {
                return null;
            }
            var list = new SvgNumberList(dashArray);

            var len = list.NumberOfItems;
            var fDashArray = new float[len];

            for (uint i = 0; i < len; i++)
            {
                //divide by strokeWidth to take care of the difference between Svg and GDI+
                fDashArray[i] = list.GetItem(i).Value / strokeWidth;
            }

            if (len % 2 == 1)
            {
                //odd number of values, duplicate
                var tmpArray = new float[len * 2];
                fDashArray.CopyTo(tmpArray, 0);
                fDashArray.CopyTo(tmpArray, (int)len);

                fDashArray = tmpArray;
            }

            return fDashArray;
        }

        private float GetDashOffset()
        {
            var dashOffset = _element.GetPropertyValue("stroke-dashoffset");
            if (dashOffset.Length > 0)
            {
                //divide by strokeWidth to take care of the difference between Svg and GDI+
                var dashOffsetLength = new SvgLength(_element, "stroke-dashoffset", SvgLengthDirection.Viewport,
                    dashOffset);
                return (float)dashOffsetLength.Value;
            }
            return 0;
        }

        private PaintServer GetPaintServer(string uri)
        {
            var absoluteUri = _element.ResolveUri(uri);
            return PaintServer.CreatePaintServer(_element.OwnerDocument, absoluteUri);
        }

        #endregion Private methods

        #region Public methods

        public Brush GetBrush(GraphicsPath gp)
        {
            return GetBrush(gp, "fill");
        }

        private Brush GetBrush(GraphicsPath gp, string propPrefix)
        {
            if (PaintType == SvgPaintType.None)
            {
                return null;
            }
            SvgPaint fill = PaintType == SvgPaintType.CurrentColor ? new GdiSvgPaint(_element, "color") : this;

            if (fill.PaintType == SvgPaintType.Uri ||
                fill.PaintType == SvgPaintType.UriCurrentColor ||
                fill.PaintType == SvgPaintType.UriNone ||
                fill.PaintType == SvgPaintType.UriRgbColor ||
                fill.PaintType == SvgPaintType.UriRgbColorIccColor)
            {
                PaintServer = GetPaintServer(fill.Uri);
                if (PaintServer != null)
                {
                    var br = PaintServer.GetBrush(gp.GetBounds());
                    var gradientBrush = br as LinearGradientBrush;
                    if (gradientBrush != null)
                    {
                        var lgb = gradientBrush;
                        var opacityl = GetOpacity(propPrefix);
                        for (var i = 0; i < lgb.InterpolationColors.Colors.Length; i++)
                        {
                            lgb.InterpolationColors.Colors[i] = Color.FromArgb(opacityl,
                                lgb.InterpolationColors.Colors[i]);
                        }
                        for (var i = 0; i < lgb.LinearColors.Length; i++)
                        {
                            lgb.LinearColors[i] = Color.FromArgb(opacityl, lgb.LinearColors[i]);
                        }
                    }
                    else
                    {
                        var pathGradientBrush = br as PathGradientBrush;
                        if (pathGradientBrush != null)
                        {
                            var pgb = pathGradientBrush;
                            var opacityl = GetOpacity(propPrefix);
                            for (var i = 0; i < pgb.InterpolationColors.Colors.Length; i++)
                            {
                                pgb.InterpolationColors.Colors[i] = Color.FromArgb(opacityl,
                                    pgb.InterpolationColors.Colors[i]);
                            }
                            for (var i = 0; i < pgb.SurroundColors.Length; i++)
                            {
                                pgb.SurroundColors[i] = Color.FromArgb(opacityl, pgb.SurroundColors[i]);
                            }
                        }
                    }
                    return br;
                }
                if (PaintType == SvgPaintType.UriNone ||
                    PaintType == SvgPaintType.Uri)
                {
                    return null;
                }
                fill = PaintType == SvgPaintType.UriCurrentColor ? new GdiSvgPaint(_element, "color") : this;
            }

            var brush = new SolidBrush(((RgbColor)fill.RgbColor).GdiColor);
            var opacity = GetOpacity(propPrefix);
            brush.Color = Color.FromArgb(opacity, brush.Color);
            return brush;
        }

        public Pen GetPen(GraphicsPath gp)
        {
            var strokeWidth = GetStrokeWidth();
            if (PaintType == SvgPaintType.None)
            {
                return null;
            }
            var stroke = PaintType == SvgPaintType.CurrentColor ? new GdiSvgPaint(_element, "color") : this;

            var pen = new Pen(stroke.GetBrush(gp, "stroke"), strokeWidth);

            pen.StartCap = pen.EndCap = GetLineCap();
            pen.LineJoin = GetLineJoin();
            pen.MiterLimit = GetMiterLimit();

            var fDashArray = GetDashArray(strokeWidth);
            if (fDashArray != null)
            {
                // Do not draw if dash array had a zero value in it

                pen.DashPattern = fDashArray;
            }

            pen.DashOffset = GetDashOffset();

            return pen;
        }

        #endregion Public methods
    }
}