using SharpVectors.Dom.Css;
using SharpVectors.Dom.Svg;
using System;
using System.Drawing;

namespace SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi.Paint
{
    public class GdiSvgColor : SvgColor
    {
        private readonly string _propertyName;
        public SvgStyleableElement Element;

        public GdiSvgColor(SvgStyleableElement elm, string propertyName)
            : base(elm.GetComputedStyle("").GetPropertyValue(propertyName))
        {
            Element = elm;
            _propertyName = propertyName;
        }

        public Color Color
        {
            get
            {
                SvgColor colorToUse;
                if (ColorType == SvgColorType.CurrentColor)
                {
                    var sCurColor = Element.GetComputedStyle("").GetPropertyValue("color");
                    colorToUse = new SvgColor(sCurColor);
                }
                else if (ColorType == SvgColorType.Unknown)
                {
                    colorToUse = new SvgColor("black");
                }
                else
                {
                    colorToUse = this;
                }

                var red = Convert.ToInt32(colorToUse.RgbColor.Red.GetFloatValue(CssPrimitiveType.Number));
                var green = Convert.ToInt32(colorToUse.RgbColor.Green.GetFloatValue(CssPrimitiveType.Number));
                var blue = Convert.ToInt32(colorToUse.RgbColor.Blue.GetFloatValue(CssPrimitiveType.Number));
                return Color.FromArgb(GetOpacity(), red, green, blue);
            }
        }

        public int GetOpacity()
        {
            string propName;
            if (_propertyName.Equals("stop-color"))
            {
                propName = "stop-opacity";
            }
            else if (_propertyName.Equals("flood-color"))
            {
                propName = "flood-opacity";
            }
            else
            {
                return 0xff;
            }

            double alpha = 0xff;

            var opacity = Element.GetPropertyValue(propName);
            if (opacity.Length > 0) alpha *= SvgNumber.ParseToFloat(opacity);

            alpha = Math.Min(alpha, 0xff);
            alpha = Math.Max(alpha, 0x0);

            return Convert.ToInt32(alpha);
        }
    }
}