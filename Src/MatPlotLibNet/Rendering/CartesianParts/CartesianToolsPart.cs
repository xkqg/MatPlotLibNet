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
    /// <summary>Constructs the part for one render.</summary>
    /// <param name="context">What the render draws with: the axes, the plot area, the surface, the theme and the primary transform.</param>
    internal CartesianToolsPart(in CartesianPartContext context)
        : base(context) { }

    /// <inheritdoc />
    public override void Render()
    {
        RenderTrendlines();
        RenderHorizontalLevels();
        RenderFibonacciRetracements();
    }

    private void RenderTrendlines()
    {
        foreach (var t in Axes.Trendlines)
        {
            // Both points are placed as matplotlib's transData places them: a value a log axis cannot place (0 or below) stands a
            // thousand decades beyond that axis (its nonpositive='clip'), so the line dives to the edge as ax.plot draws it. A
            // point the axes mask (a missing value, an infinity, a value inside a break or beyond a logit axis) skips the
            // trendline with its label.
            var p1 = Transform.DataToPixelUnclipped(t.X1, t.Y1);
            var p2 = Transform.DataToPixelUnclipped(t.X2, t.Y2);
            if (!p1.CanDrawLineTo(p2)) continue;

            // A trendline takes no part in the range the axes draw, so it is cut to the plot area, as matplotlib clips a line to
            // the axes: one wholly outside draws nothing, and its label stands at the end that is drawn. An extended one is
            // matplotlib's axline: the line through both points as drawn, run across the plot from its left end (its lower end
            // when upright), where axline starts it.
            var cut = t.IsExtended
                ? (p2.X < p1.X || (p2.X == p1.X && p2.Y > p1.Y) ? PlotArea.LineThrough(p2, p1) : PlotArea.LineThrough(p1, p2))
                : new LineSegment(p1, p2).ClippedTo(PlotArea);
            if (cut is not { } drawn) continue;
            var stroke = t.StrokeUnder(Theme);
            Ctx.DrawLine(drawn.From, drawn.To, stroke);

            if (t.Label is not null) Ctx.DrawLineLabel(t.Label, LineLabelPlacement.AtEnd(drawn.To), Theme, stroke.Color);
        }
    }

    private void RenderHorizontalLevels()
    {
        foreach (var l in Axes.HorizontalLevels)
        {
            var pt = Transform.DataToPixel(0, l.Value);
            // A level the Y axis cannot place is skipped with its label; only its height decides, it spans the plot. The
            // automatic range holds every level; one beyond a limit the caller set lies outside the plot and is not drawn.
            if (!pt.Y.IsDrawable() || PlotArea.HorizontalAcross(pt.Y) is not { } across) continue;
            var stroke = l.StrokeUnder(Theme);
            Ctx.DrawLine(across.From, across.To, stroke);

            if (l.Label is not null) Ctx.DrawLineLabel(l.Label, LineLabelPlacement.AboveEnd(across), Theme, stroke.Color);
        }
    }

    private void RenderFibonacciRetracements()
    {
        foreach (var fib in Axes.FibonacciRetracements)
        {
            var stroke = fib.StrokeUnder(Theme);
            foreach (var level in fib.Levels)
            {
                var pt = Transform.DataToPixel(0, level.Price);
                // A retracement level at a price the Y axis cannot place is skipped with its label, and so is one beyond a
                // limit the caller set, which lies outside the plot (the automatic range holds every level).
                if (!pt.Y.IsDrawable() || PlotArea.HorizontalAcross(pt.Y) is not { } across) continue;
                Ctx.DrawLine(across.From, across.To, stroke);

                if (fib.ShowLabels)
                    Ctx.DrawLineLabel(string.Invariant($"{level.Ratio * 100:0.#}%  {level.Price:0.##}"), LineLabelPlacement.AboveEnd(across, LineLabelPlacement.Gap),
                        Theme, stroke.Color);
            }
        }
    }
}
