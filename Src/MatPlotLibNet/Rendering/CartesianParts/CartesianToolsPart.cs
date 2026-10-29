// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>Renders financial drawing tools: trendlines, horizontal levels, and
/// Fibonacci retracement overlays. Placed after reference lines and before annotations
/// in the rendering pipeline.</summary>
internal sealed class CartesianToolsPart : CartesianAxesPart
{
    public CartesianToolsPart(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, DataTransform transform)
        : base(axes, plotArea, ctx, theme, transform) { }

    /// <inheritdoc />
    public override void Render()
    {
        RenderTrendlines();
        RenderHorizontalLevels();
        RenderFibonacciRetracements();
    }

    private void RenderTrendlines()
    {
        var labelFont = TickFont();
        foreach (var t in Axes.Trendlines)
        {
            var color = t.Color ?? Theme.Neutral;
            Point p1, p2;

            if (t.IsExtended)
            {
                // Extend line to plot edges using slope-intercept
                double slope = (t.Y2 - t.Y1) / (t.X2 - t.X1);
                double xMin = Transform.PixelToData(new Point(PlotArea.X, PlotArea.Y)).X;
                double xMax = Transform.PixelToData(new Point(PlotArea.X + PlotArea.Width, PlotArea.Y)).X;
                double yAtMin = t.Y1 + slope * (xMin - t.X1);
                double yAtMax = t.Y1 + slope * (xMax - t.X1);
                p1 = Transform.DataToPixel(xMin, yAtMin);
                p2 = Transform.DataToPixel(xMax, yAtMax);
            }
            else
            {
                p1 = Transform.DataToPixel(t.X1, t.Y1);
                p2 = Transform.DataToPixel(t.X2, t.Y2);
            }

            // A trendline with an end the axes cannot place is skipped with its label. A trendline takes no part in the
            // range the axes draw, so it is cut to the plot area, as matplotlib clips a line to the axes: one wholly outside
            // draws nothing, and its label stands at the end that is drawn.
            if (!p1.CanDrawLineTo(p2)) continue;
            if (new LineSegment(p1, p2).ClippedTo(PlotArea) is not { } drawn) continue;
            Ctx.DrawLine(drawn.From, drawn.To, new StrokeStyle(color, t.LineWidth, t.LineStyle));

            if (t.Label is not null)
            {
                var font = labelFont with { Color = color };
                Ctx.DrawText(t.Label, drawn.To, font, TextAlignment.Left);
            }
        }
    }

    private void RenderHorizontalLevels()
    {
        var labelFont = TickFont();
        foreach (var l in Axes.HorizontalLevels)
        {
            var color = l.Color ?? Theme.Neutral;
            var pt = Transform.DataToPixel(0, l.Value);
            // A level the Y axis cannot place is skipped with its label; only its height decides, it spans the plot. The
            // automatic range holds every level; one beyond a limit the caller set lies outside the plot and is not drawn.
            if (!pt.Y.IsDrawable() || PlotArea.HorizontalAcross(pt.Y) is not { } across) continue;
            Ctx.DrawLine(across.From, across.To, new StrokeStyle(color, l.LineWidth, l.LineStyle));

            if (l.Label is not null)
            {
                var font = labelFont with { Color = color };
                Ctx.DrawText(l.Label, new Point(across.To.X, pt.Y - 2), font, TextAlignment.Right);
            }
        }
    }

    private void RenderFibonacciRetracements()
    {
        var labelFont = TickFont();
        foreach (var fib in Axes.FibonacciRetracements)
        {
            var color = fib.Color ?? Theme.Neutral;
            foreach (var level in fib.Levels)
            {
                var pt = Transform.DataToPixel(0, level.Price);
                // A retracement level at a price the Y axis cannot place is skipped with its label, and so is one beyond a
                // limit the caller set, which lies outside the plot (the automatic range holds every level).
                if (!pt.Y.IsDrawable() || PlotArea.HorizontalAcross(pt.Y) is not { } across) continue;
                Ctx.DrawLine(across.From, across.To, new StrokeStyle(color, fib.LineWidth, LineStyle.Dashed));

                if (fib.ShowLabels)
                {
                    string pct = $"{level.Ratio * 100:0.#}%  {level.Price:0.##}";
                    var font = labelFont with { Color = color };
                    Ctx.DrawText(pct, new Point(across.To.X - 2, pt.Y - 2), font, TextAlignment.Right);
                }
            }
        }
    }
}
