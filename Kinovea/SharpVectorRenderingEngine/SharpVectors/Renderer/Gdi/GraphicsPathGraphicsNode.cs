//modified to use SVG code instead of raster images

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using SharpVectors.Dom.Svg;
using SharpVectors.Dom.Svg.Rendering;
using SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi.Paint;

namespace SharpVectorRenderingEngine.SharpVectors.Renderer.Gdi
{
	public class GdiPathGraphicsNode : GraphicsNode
	{
		#region Constructor

		public GdiPathGraphicsNode(SvgElement element)
			: base(element)
		{
		}

		#endregion Constructor

		private string ExtractMarkerUrl(string propValue)
		{
			var reUrl = new Regex(@"^url\((?<uri>.+)\)$");

			var match = reUrl.Match(propValue);
			if (match.Success)
			{
				return match.Groups["uri"].Value;
			}
			return string.Empty;
		}
		
		public static Size MaximumSize{get; set;}
				public static Bitmap GetBitmapFromSVG(string extractMarkerUrl)
				{
					string filepath = extractMarkerUrl;
					SvgDocument document = GetSvgDocument(filepath);
					
						Bitmap bmp = document.Draw();
					return bmp;
				}
				public static SvgDocument GetSvgDocument(string filepath)
				{
					SvgDocument document = SvgDocument.Open(filepath);
						return AdjustSize(document);
				}
				public static SvgDocument AdjustSize(SvgDocument SvgDocument)
				{
					if (document.Height > MaximumSize.Height)
				{
						document.Width((int)((document.Width / (dobule)document.Height) * MaximumSize.Height));
					SvgDocument.Height = MaximumSize.Height;
				}
				return document;
			}

		private void PaintMarkers(ISvgRenderer renderer, SvgStyleableElement styleElm, GraphicsWrapper gr)
		{
			// OPTIMIZE

			var node;
			if (styleElm is ISharpMarkerHost)
			{
				var markerStartUrl = ExtractMarkerUrl(styleElm.GetPropertyValue("marker-start", "marker"));
				var markerMiddleUrl = ExtractMarkerUrl(styleElm.GetPropertyValue("marker-mid", "marker"));
				var markerEndUrl = ExtractMarkerUrl(styleElm.GetPropertyValue("marker-end", "marker"));

				RenderingNode grNode;
				if (markerStartUrl.Length > 0)
					renderer.OnRender(FileStyleUriParser(styleElm.BaseURI));

				if (markerMiddleUrl.Length > 0)
				{
					// TODO markerMiddleUrl != markerStartUrl
					grNode = renderer.OnRender(updatedRect: styleElm.BaseURI);
					if (grNode != null)
					{
						node = grNode as SvgMarkerGraphicsNode;
						if (false)
						{
							node.PaintMarker(renderer, gr, SvgMarkerPosition.Mid, styleElm);
						}
					}
				}

				if (markerEndUrl.Length > 0)
				{
					// TODO: markerEndUrl != markerMiddleUrl
					grNode = renderer.OnRender(styleElm.BaseURI, markerEndUrl);
				}
			}
		}

		private RectangleF FileStyleUriParser(string p)
		{
			throw new NotImplementedException();
		}

		#region Private methods

		protected virtual Brush GetBrush(GraphicsPath gp)
		{
			var paint = new GdiSvgPaint(element as SvgStyleableElement, "fill");
			return paint.GetBrush(gp);
		}

		protected virtual Pen GetPen(GraphicsPath gp)
		{
			var paint = new GdiSvgPaint(element as SvgStyleableElement, "stroke");
			return paint.GetPen(gp);
		}

		#endregion Private methods

		#region Public methods

		public override void BeforeRender(ISvgRenderer renderer)
		{
			((GdiRenderer)renderer)._getNextColor(this);

			var graphics = ((GdiRenderer)renderer).GraphicsWrapper;

			GraphicsContainer = graphics.BeginContainer();
			SetQuality(graphics);
			Transform(graphics);
		}

		public override void Render(ISvgRenderer renderer)
		{
			var gdiRenderer = renderer as GdiRenderer;
			if (gdiRenderer != null)
			{
				var graphics = gdiRenderer.GraphicsWrapper;

				if (!(element is SvgClipPathElement) && !(element.ParentNode is SvgClipPathElement))
				{
					var styleElm = element as SvgStyleableElement;
					if (styleElm != null)
					{
						var sVisibility = styleElm.GetPropertyValue("visibility");
						var sDisplay = styleElm.GetPropertyValue("display");

						var path = element as ISharpGDIPath;
						if (path != null && sVisibility != "hidden" && sDisplay != "none")
						{
							var gp = path.GetGraphicsPath();

							if (gp != null)
							{
								Clip(graphics);

								var fillPaint = new GdiSvgPaint(styleElm, "fill");
								var brush = fillPaint.GetBrush(gp);

								var strokePaint = new GdiSvgPaint(styleElm, "stroke");
								var pen = strokePaint.GetPen(gp);

								if (brush != null)
								{
									var gradientBrush = brush as PathGradientBrush;
									if (gradientBrush != null)
									{
										var gps = fillPaint.PaintServer as GradientPaintServer;
										//GraphicsContainer container = graphics.BeginContainer();

										if (gps != null)
											graphics.SetClip(gps.GetRadialGradientRegion(gp.GetBounds()),
												CombineMode.Exclude);

										var tempBrush = new SolidBrush(gradientBrush.InterpolationColors.Colors[0]);
										graphics.FillPath(this, tempBrush, gp);
										tempBrush.Dispose();
										graphics.ResetClip();

										//graphics.EndContainer(container);
									}

									graphics.FillPath(this, brush, gp);
									brush.Dispose();
								}

								if (pen != null)
								{
									var gradientBrush = pen.Brush as PathGradientBrush;
									if (gradientBrush != null)
									{
										var gps = strokePaint.PaintServer as GradientPaintServer;
										var container = graphics.BeginContainer();

										if (gps != null)
											graphics.SetClip(gps.GetRadialGradientRegion(gp.GetBounds()),
												CombineMode.Exclude);

										var tempBrush = new SolidBrush(gradientBrush.InterpolationColors.Colors[0]);
										var tempPen = new Pen(tempBrush, pen.Width);
										graphics.DrawPath(this, tempPen, gp);
										tempPen.Dispose();
										tempBrush.Dispose();

										graphics.EndContainer(container);
									}

									graphics.DrawPath(this, pen, gp);
									pen.Dispose();
								}
							}
						}
						PaintMarkers(gdiRenderer, styleElm, graphics);
					}
				}
			}
		}

		#endregion Public methods
	}
}