// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>
/// Renders horizontal/vertical span regions (filled rectangles behind series).
/// Extracted verbatim from <c>CartesianAxesRenderer.Render</c> lines 93-137
/// (pre-B.5). Responsibility: draw each SpanRegion's fill + optional border +
/// optional label.
/// </summary>
internal sealed class CartesianSpansPart : CartesianAxesPart
{
    private readonly DataRange _range;

    /// <summary>Constructs a Spans part. Requires the computed data range for
    /// positioning span rectangles within the plot area.</summary>
    public CartesianSpansPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme,
                               DataTransform transform, DataRange range)
        : base(axes, plotArea, ctx, theme, transform)
    {
        _range = range;
    }

    /// <inheritdoc />
    public override void Render()
    {
        var spanLabelFont = TickFont();
        foreach (var span in Axes.Spans)
        {
            var spanColor = (span.Color ?? Colors.Tab10Blue).WithAlpha((byte)(span.Alpha * 255));
            var borderColor = span.EdgeColor ?? (span.Color ?? Colors.Tab10Blue);
            if (span.Orientation == Orientation.Horizontal)
            {
                var topLeft = Transform.DataToPixel(_range.XMin, Math.Max(span.Min, span.Max));
                var bottomRight = Transform.DataToPixel(_range.XMax, Math.Min(span.Min, span.Max));
                var rect = new Rect(PlotArea.X, topLeft.Y, PlotArea.Width, bottomRight.Y - topLeft.Y);
                Ctx.DrawRectangle(rect, spanColor, null, 0);
                if (span.LineStyle != LineStyle.None)
                {
                    Ctx.DrawLine(new Point(PlotArea.X, topLeft.Y), new Point(PlotArea.X + PlotArea.Width, topLeft.Y), borderColor, span.LineWidth, span.LineStyle);
                    Ctx.DrawLine(new Point(PlotArea.X, bottomRight.Y), new Point(PlotArea.X + PlotArea.Width, bottomRight.Y), borderColor, span.LineWidth, span.LineStyle);
                    Ctx.DrawLine(new Point(PlotArea.X, topLeft.Y), new Point(PlotArea.X, bottomRight.Y), borderColor, span.LineWidth, span.LineStyle);
                    Ctx.DrawLine(new Point(PlotArea.X + PlotArea.Width, topLeft.Y), new Point(PlotArea.X + PlotArea.Width, bottomRight.Y), borderColor, span.LineWidth, span.LineStyle);
                }
                if (span.Label is not null)
                {
                    var labelFont = spanLabelFont with { Color = borderColor };
                    Ctx.DrawText(span.Label, new Point(PlotArea.X + 2, topLeft.Y + spanLabelFont.Size + 2), labelFont, TextAlignment.Left);
                }
            }
            else
            {
                var left = Transform.DataToPixel(Math.Min(span.Min, span.Max), _range.YMax);
                var right = Transform.DataToPixel(Math.Max(span.Min, span.Max), _range.YMin);
                var rect = new Rect(left.X, PlotArea.Y, right.X - left.X, PlotArea.Height);
                Ctx.DrawRectangle(rect, spanColor, null, 0);
                if (span.LineStyle != LineStyle.None)
                {
                    Ctx.DrawLine(new Point(left.X, PlotArea.Y), new Point(left.X, PlotArea.Y + PlotArea.Height), borderColor, span.LineWidth, span.LineStyle);
                    Ctx.DrawLine(new Point(right.X, PlotArea.Y), new Point(right.X, PlotArea.Y + PlotArea.Height), borderColor, span.LineWidth, span.LineStyle);
                    Ctx.DrawLine(new Point(left.X, PlotArea.Y), new Point(right.X, PlotArea.Y), borderColor, span.LineWidth, span.LineStyle);
                    Ctx.DrawLine(new Point(left.X, PlotArea.Y + PlotArea.Height), new Point(right.X, PlotArea.Y + PlotArea.Height), borderColor, span.LineWidth, span.LineStyle);
                }
                if (span.Label is not null)
                {
                    var labelFont = spanLabelFont with { Color = borderColor };
                    Ctx.DrawText(span.Label, new Point((left.X + right.X) / 2, PlotArea.Y + spanLabelFont.Size + 2), labelFont, TextAlignment.Center);
                }
            }
        }
    }
}
