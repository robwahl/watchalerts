using SharpVectors.Dom.Svg;
using SharpVectors.Dom.Svg.Rendering;

namespace SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi
{
    public enum SvgMarkerPosition
    {
        Start,
        Mid,
        End
    }

    public class SvgMarkerGraphicsNode : GraphicsNode
    {
        #region Constructor

        public SvgMarkerGraphicsNode(SvgElement element)
            : base(element)
        {
        }

        #endregion Constructor

        #region Public Methods

        // disable default rendering
        public override void BeforeRender(ISvgRenderer renderer)
        {
        }

        public override void Render(ISvgRenderer renderer)
        {
        }

        public override void AfterRender(ISvgRenderer renderer)
        {
        }

        public void PaintMarker(ISvgRenderer renderer, GraphicsWrapper gr, SvgMarkerPosition markerPos,
            SvgStyleableElement refElement)
        {
            var markerHostElm = (ISharpMarkerHost)refElement;
            var markerElm = (SvgMarkerElement)element;

            var vertexPositions = markerHostElm.MarkerPositions;
            int start;
            int len;

            // Choose which part of the position array to use
            switch (markerPos)
            {
                case SvgMarkerPosition.Start:
                    start = 0;
                    len = 1;
                    break;

                case SvgMarkerPosition.Mid:
                    start = 1;
                    len = vertexPositions.Length - 2;
                    break;

                default:
                    // == MarkerPosition.End
                    start = vertexPositions.Length - 1;
                    len = 1;
                    break;
            }

            for (var i = start; i < start + len; i++)
            {
                var point = vertexPositions[i];

                var gc = gr.BeginContainer();

                gr.TranslateTransform(point.X, point.Y);
                float angle;

                switch (markerPos)
                {
                    case SvgMarkerPosition.Start:
                        angle = markerHostElm.GetStartAngle(i + 1);
                        break;

                    case SvgMarkerPosition.Mid:
                        //angle = (markerHostElm.GetEndAngle(i) + markerHostElm.GetStartAngle(i + 1)) / 2;
                        angle = SvgNumber.CalcAngleBisection(markerHostElm.GetEndAngle(i),
                            markerHostElm.GetStartAngle(i + 1));
                        break;

                    default:
                        angle = markerHostElm.GetEndAngle(i);
                        break;
                }
                gr.RotateTransform(angle);

                var spar = (SvgPreserveAspectRatio)markerElm.PreserveAspectRatio.AnimVal;
                var translateAndScale = spar.FitToViewBox(
                    (SvgRect)markerElm.ViewBox.AnimVal,
                    new SvgRect(
                        0,
                        0,
                        (float)markerElm.MarkerWidth.AnimVal.Value,
                        (float)markerElm.MarkerHeight.AnimVal.Value)
                    );

                gr.TranslateTransform(
                    -(float)markerElm.RefX.AnimVal.Value * translateAndScale[2],
                    -(float)markerElm.RefY.AnimVal.Value * translateAndScale[3]
                    );

                gr.ScaleTransform(translateAndScale[2], translateAndScale[3]);

                Clip(gr);

                markerElm.RenderChildren(renderer);

                gr.EndContainer(gc);
            }
        }

        #endregion Public Methods
    }
}