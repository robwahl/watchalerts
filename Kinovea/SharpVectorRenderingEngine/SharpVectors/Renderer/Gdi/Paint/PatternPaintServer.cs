using SharpVectors.Dom.Svg;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml;

namespace SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi.Paint
{
    public class PatternPaintServer : PaintServer
    {
        private readonly SvgPatternElement _patternElement;

        public PatternPaintServer(SvgPatternElement patternElement)
        {
            _patternElement = patternElement;
        }

        #region Public methods

        public override Brush GetBrush(RectangleF bounds)
        {
            var image = GetImage(bounds);
            var destRect = GetDestRect(bounds);

            var tb = new TextureBrush(image, destRect);
            tb.Transform = GetTransformMatrix(bounds);
            return tb;
        }

        #endregion Public methods

        #region Private methods

        private XmlElement _oldParent;

        private SvgSvgElement MoveIntoSvgElement()
        {
            var doc = _patternElement.OwnerDocument;
            var svgElm = doc.CreateElement("", "svg", SvgDocument.SvgNamespace) as SvgSvgElement;

            var children = _patternElement.Children;
            if (children.Count > 0)
            {
                _oldParent = children[0].ParentNode as XmlElement;
            }

            for (var i = 0; i < children.Count; i++)
            {
                svgElm.AppendChild(children[i]);
            }

            if (_patternElement.HasAttribute("viewBox"))
            {
                svgElm.SetAttribute("viewBox", _patternElement.GetAttribute("viewBox"));
            }
            svgElm.SetAttribute("x", "0");
            svgElm.SetAttribute("y", "0");
            svgElm.SetAttribute("width", _patternElement.GetAttribute("width"));
            svgElm.SetAttribute("height", _patternElement.GetAttribute("height"));

            if (_patternElement.PatternContentUnits.AnimVal.Equals(SvgUnitType.ObjectBoundingBox))
            {
                svgElm.SetAttribute("viewBox", "0 0 1 1");
            }

            _patternElement.AppendChild(svgElm);

            return svgElm;
        }

        private void MoveOutOfSvgElement(SvgSvgElement svgElm)
        {
            while (svgElm.ChildNodes.Count > 0)
            {
                _oldParent.AppendChild(svgElm.ChildNodes[0]);
            }

            _patternElement.RemoveChild(svgElm);
        }

        private Image GetImage(RectangleF bounds)
        {
            var renderer = new GdiRenderer();
            renderer.Window = _patternElement.OwnerDocument.Window as SvgWindow;

            var elm = MoveIntoSvgElement();

            Image img = renderer.Render(elm);

            MoveOutOfSvgElement(elm);

            return img;
        }

        private float CalcPatternUnit(SvgLength length, SvgLengthDirection dir, RectangleF bounds)
        {
            int patternUnits = _patternElement.PatternUnits.AnimVal;
            if (patternUnits == (int)SvgUnitType.UserSpaceOnUse)
            {
                return (float)length.Value;
            }
            var calcValue = (float)length.ValueInSpecifiedUnits;
            if (dir == SvgLengthDirection.Horizontal)
            {
                calcValue *= bounds.Width;
            }
            else
            {
                calcValue *= bounds.Height;
            }
            if (length.UnitType == SvgLengthType.Percentage)
            {
                calcValue /= 100F;
            }
            return calcValue;
        }

        private RectangleF GetDestRect(RectangleF bounds)
        {
            var result = new RectangleF(0, 0, 0, 0);
            result.Width = CalcPatternUnit(_patternElement.Width.AnimVal as SvgLength, SvgLengthDirection.Horizontal,
                bounds);
            result.Height = CalcPatternUnit(_patternElement.Height.AnimVal as SvgLength, SvgLengthDirection.Vertical,
                bounds);

            return result;
        }

        private Matrix GetTransformMatrix(RectangleF bounds)
        {
            var svgMatrix = ((SvgTransformList)_patternElement.PatternTransform.AnimVal).TotalMatrix;

            var transformMatrix = new Matrix(
                svgMatrix.A,
                svgMatrix.B,
                svgMatrix.C,
                svgMatrix.D,
                svgMatrix.E,
                svgMatrix.F);

            var translateX = CalcPatternUnit(_patternElement.X.AnimVal as SvgLength, SvgLengthDirection.Horizontal,
                bounds);
            var translateY = CalcPatternUnit(_patternElement.Y.AnimVal as SvgLength, SvgLengthDirection.Vertical, bounds);

            transformMatrix.Translate(translateX, translateY, MatrixOrder.Prepend);
            return transformMatrix;
        }

        #endregion Private methods
    }
}