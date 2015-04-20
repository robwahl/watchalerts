using SharpVectors.Dom.Svg;
using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Xml;

namespace SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi.Paint
{
    /// <summary>
    ///     Summary description for PaintServer.
    /// </summary>
    public class GradientPaintServer : PaintServer
    {
        private readonly SvgGradientElement _gradientElement;

        public GradientPaintServer(SvgGradientElement gradientElement)
        {
            _gradientElement = gradientElement;
        }

        public float Tolerance { get; set; }

        #region Private methods

        private ArrayList GetColors(XmlNodeList stops)
        {
            var colors = new ArrayList(stops.Count);
            for (var i = 0; i < stops.Count; i++)
            {
                var stop = (SvgStopElement)stops.Item(i);
                if (stop != null)
                {
                    stop.GetPropertyValue("stop-color");
                    var svgColor = new GdiSvgColor(stop, "stop-color");

                    colors.Add(svgColor.Color);
                }
            }

            return colors;
        }

        private ArrayList GetPositions(XmlNodeList stops)
        {
            var positions = new ArrayList(stops.Count);
            float lastPos = 0;
            for (var i = 0; i < stops.Count; i++)
            {
                var stop = (SvgStopElement)stops.Item(i);
                if (stop != null)
                {
                    var pos = (float)stop.Offset.AnimVal;

                    pos /= 100;
                    pos = Math.Max(lastPos, pos);

                    positions.Add(pos);
                    lastPos = pos;
                }
            }

            return positions;
        }

        private void CorrectPositions(ArrayList positions, ArrayList colors)
        {
            if (positions.Count > 0)
            {
                var firstPos = (float)positions[0];
                if (firstPos > 0F)
                {
                    positions.Insert(0, 0F);
                    colors.Insert(0, colors[0]);
                }
                var lastPos = (float)positions[positions.Count - 1];
                if (lastPos < 1F)
                {
                    positions.Add(1F);
                    colors.Add(colors[colors.Count - 1]);
                }
            }
        }

        private void GetColorsAndPositions(XmlNodeList stops, ref float[] positions, ref Color[] colors)
        {
            if (positions == null) throw new ArgumentNullException("positions");
            var alColors = GetColors(stops);
            var alPositions = GetPositions(stops);

            if (alPositions.Count > 0)
            {
                CorrectPositions(alPositions, alColors);

                colors = (Color[])alColors.ToArray(typeof(Color));
                positions = (float[])alPositions.ToArray(typeof(float));
            }
            else
            {
                colors = new Color[2];
                colors[0] = Color.Black;
                colors[1] = Color.Black;

                positions = new float[2];
                positions[0] = 0;
                positions[1] = 1;
            }
        }

        private LinearGradientBrush GetLinearGradientBrush(SvgLinearGradientElement res, RectangleF bounds)
        {
            var fLeft = (float)res.X1.AnimVal.Value;
            var fRight = (float)res.X2.AnimVal.Value;
            var fTop = (float)res.Y1.AnimVal.Value;
            var fBottom = (float)res.Y2.AnimVal.Value;

            var bForceUserSpaceOnUse = (fLeft > 1 || fRight > 1 || fTop > 1 || fBottom > 1);

            var fEffectiveLeft = fLeft;
            var fEffectiveRight = fRight;
            var fEffectiveTop = fTop;
            var fEffectiveBottom = fBottom;

            if (res.GradientUnits.AnimVal.Equals((ushort)SvgUnitType.ObjectBoundingBox) && !bForceUserSpaceOnUse)
            {
                if (res.SpreadMethod.AnimVal.Equals((ushort)SvgSpreadMethod.Pad))
                {
                    fEffectiveRight = bounds.Right;
                    fEffectiveLeft = bounds.Left;
                }
                else
                {
                    fEffectiveLeft = bounds.Left + fLeft * (bounds.Width);
                    fEffectiveRight = bounds.Left + fRight * (bounds.Width);
                }

                fEffectiveTop = bounds.Top + fTop * (bounds.Height);
                fEffectiveBottom = bounds.Top + fBottom * (bounds.Height);
            }

            LinearGradientMode mode;

            if (Math.Abs(fTop - fBottom) < Tolerance)
                mode = LinearGradientMode.Horizontal;
            else
            {
                if (Math.Abs(fLeft - fRight) < Tolerance)
                    mode = LinearGradientMode.Vertical;
                else
                {
                    mode = fLeft < fRight ? LinearGradientMode.ForwardDiagonal : LinearGradientMode.BackwardDiagonal;
                }
            }

            var fEffectiveWidth = fEffectiveRight - fEffectiveLeft;

            if (fEffectiveWidth <= 0)
                fEffectiveWidth = bounds.Width;

            var fEffectiveHeight = fEffectiveBottom - fEffectiveTop;

            if (fEffectiveHeight <= 0)
                fEffectiveHeight = bounds.Height;

            var brush =
                new LinearGradientBrush(
                    new RectangleF(fEffectiveLeft - 1, fEffectiveTop - 1, fEffectiveWidth + 2, fEffectiveHeight + 2),
                    Color.White, Color.White, mode);

            var stops = res.Stops;

            var cb = new ColorBlend();

            Color[] adjcolors = null;
            float[] adjpositions = null;
            GetColorsAndPositions(stops, ref adjpositions, ref adjcolors);

            if (res.GradientUnits.AnimVal.Equals((ushort)SvgUnitType.ObjectBoundingBox) && !bForceUserSpaceOnUse)
            {
                if (res.SpreadMethod.AnimVal.Equals((ushort)SvgSpreadMethod.Pad))
                {
                    for (var i = 0; i < adjpositions.Length; i++)
                    {
                        if (Math.Abs(fLeft - fRight) < Tolerance)
                            adjpositions[i] = fTop + adjpositions[i] * (fBottom - fTop);
                        else
                            adjpositions[i] = fLeft + adjpositions[i] * (fRight - fLeft);
                    }

                    // this code corrects the values again... fix
                    var nSize = adjcolors.Length;

                    if (adjpositions[0] > 0.0)
                        ++nSize;

                    if (adjpositions[adjcolors.Length - 1] < 1)
                        ++nSize;

                    var readjcolors = new Color[nSize];
                    var readjpositions = new float[nSize];

                    if (adjpositions[0] > 0.0)
                    {
                        adjpositions.CopyTo(readjpositions, 1);
                        adjcolors.CopyTo(readjcolors, 1);
                        readjcolors[0] = readjcolors[1];
                        readjpositions[0] = 0;
                    }
                    else
                    {
                        adjpositions.CopyTo(readjpositions, 0);
                        adjcolors.CopyTo(readjcolors, 0);
                    }

                    if (adjpositions[adjcolors.Length - 1] < 1)
                    {
                        readjcolors[nSize - 1] = readjcolors[nSize - 2];
                        readjpositions[nSize - 1] = 1;
                    }

                    cb.Colors = readjcolors;
                    cb.Positions = readjpositions;
                }
                else
                {
                    cb.Colors = adjcolors;
                    cb.Positions = adjpositions;
                }
            }
            else
            {
                cb.Colors = adjcolors;
                cb.Positions = adjpositions;
            }

            brush.InterpolationColors = cb;

            if (res.SpreadMethod.AnimVal.Equals((ushort)SvgSpreadMethod.Reflect))
            {
                brush.WrapMode = WrapMode.TileFlipXY;
            }
            else if (res.SpreadMethod.AnimVal.Equals((ushort)SvgSpreadMethod.Repeat))
            {
                brush.WrapMode = WrapMode.Tile;
            }
            else if (res.SpreadMethod.AnimVal.Equals((ushort)SvgSpreadMethod.Pad))
            {
                brush.WrapMode = WrapMode.Tile;
            }

            brush.Transform = GetTransformMatrix(res);

            brush.GammaCorrection = res.GetPropertyValue("color-interpolation") == "linearRGB";

            return brush;
        }

        private Matrix GetTransformMatrix(SvgGradientElement gradientElement)
        {
            var svgMatrix = ((SvgTransformList)gradientElement.GradientTransform.AnimVal).TotalMatrix;

            var transformMatrix = new Matrix(
                svgMatrix.A,
                svgMatrix.B,
                svgMatrix.C,
                svgMatrix.D,
                svgMatrix.E,
                svgMatrix.F);

            return transformMatrix;
        }

        private PathGradientBrush GetRadialGradientBrush(SvgRadialGradientElement res)
        {
            var fCenterX = (float)res.Cx.AnimVal.Value;
            var fCenterY = (float)res.Cy.AnimVal.Value;
            var fFocusX = (float)res.Fx.AnimVal.Value;
            var fFocusY = (float)res.Fy.AnimVal.Value;
            var fRadius = (float)res.R.AnimVal.Value;

            var fEffectiveCx = fCenterX;
            var fEffectiveCy = fCenterY;
            var fEffectiveFx = fFocusX;
            var fEffectiveFy = fFocusY;
            var fEffectiveRadiusX = fRadius;
            var fEffectiveRadiusY = fRadius;

            var gp = new GraphicsPath();
            gp.AddEllipse(fEffectiveCx - fEffectiveRadiusX, fEffectiveCy - fEffectiveRadiusY, 2 * fEffectiveRadiusX,
                2 * fEffectiveRadiusY);

            var brush = new PathGradientBrush(gp) { CenterPoint = new PointF(fEffectiveFx, fEffectiveFy) };

            var stops = res.Stops;

            var cb = new ColorBlend();

            Color[] adjcolors = null;
            float[] adjpositions = null;
            GetColorsAndPositions(stops, ref adjpositions, ref adjcolors);

            // Need to invert the colors for some bizarre reason
            Array.Reverse(adjcolors);
            Array.Reverse(adjpositions);
            for (var i = 0; i < adjpositions.Length; i++)
            {
                adjpositions[i] = 1 - adjpositions[i];
            }

            cb.Colors = adjcolors;
            cb.Positions = adjpositions;

            brush.InterpolationColors = cb;

            //			ISvgTransformable transElm = (ISvgTransformable)res;
            //			SvgTransformList svgTList = (SvgTransformList)transElm.transform.AnimVal;
            //			brush.Transform = svgTList.matrix.matrix;

            if (res.GetPropertyValue("color-interpolation") == "linearRGB")
            {
                //GdipSetPathGradientGammaCorrection(brush, true);
            }

            /*
             * How to do brush.GammaCorrection = true on a PathGradientBrush? / nikgus
             * */

            return brush;
        }

        #endregion Private methods

        #region Public methods

        public Region GetRadialGradientRegion(RectangleF bounds)
        {
            var res = _gradientElement as SvgRadialGradientElement;

            if (_gradientElement == null)
            {
                return null;
            }

            if (res == null) return null;
            var fCenterX = (float)res.Cx.AnimVal.Value;
            var fCenterY = (float)res.Cy.AnimVal.Value;
            var fRadius = (float)res.R.AnimVal.Value;

            var fEffectiveCx = fCenterX;
            var fEffectiveCy = fCenterY;
            var fEffectiveRadiusX = fRadius;
            var fEffectiveRadiusY = fRadius;

            var gp2 = new GraphicsPath();
            gp2.AddEllipse(fEffectiveCx - fEffectiveRadiusX, fEffectiveCy - fEffectiveRadiusY, 2 * fEffectiveRadiusX,
                2 * fEffectiveRadiusY);

            return new Region(gp2);
        }

        public override Brush GetBrush(RectangleF bounds)
        {
            var res = _gradientElement as SvgLinearGradientElement;
            if (res != null)
            {
                return GetLinearGradientBrush(res, bounds);
            }
            var element = _gradientElement as SvgRadialGradientElement;
            if (element != null)
                return GetRadialGradientBrush(element);
            return new SolidBrush(Color.Black);
        }

        [DllImport("gdiplus.dll")]
        internal static extern int GdipSetPathGradientGammaCorrection(IntPtr brush, bool gamma);

        #endregion Public methods
    }
}