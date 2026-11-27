// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PolarBarSeries"/> as wedge-shaped bars in polar coordinates.</summary>
internal sealed class PolarBarSeriesRenderer : SeriesRenderer<PolarBarSeries>
{
    /// <inheritdoc />
    public PolarBarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(PolarBarSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(PolarBarSeries series)
    {
        var baseColor = ColorOf(series);
        // Opaque under the matplotlib themes unless an alpha was given (a polar bar has none of its own), 0.8 under the library's own.
        var fillColor = baseColor.WithOpacityGiven(series.OpacityUnder(Context.Theme));
        var transform = PolarTransform.For(Context, series);
        // A bar's edge: matplotlib's patch rule under the matplotlib themes (black 1 pt under classic, none under v2), half a pixel in
        // the bar's colour under the library's own.
        var style = Context.Theme.PatchEdgesOf(series).StyleOf(fillColor, baseColor);

        // Stacked bars stand on the wedges of the same index before them (the scale carries where), the others on the centre.
        var feet = Context.RadialScale?.FootOf(series);

        // A width given wins; a bar given none is as wide as the theme's polar look says (0.8 under the matplotlib themes).
        double width = Context.Theme.Looks.Polar.BarWidthOver(series.GivenBarWidth);
        double halfWidth = width / 2;

        for (int i = 0; i < series.R.Length; i++)
        {
            double theta = series.Theta[i];
            double inner = feet.ElementOr(i, 0);
            double r = series.R[i];
            // A bar below zero stands from the centre line down, as matplotlib draws it on a scale that reaches it; in a stack,
            // which adds nothing for it, it is not drawn.
            if (!double.IsFinite(r) || (r < 0 && feet is not null)) continue;
            r += inner;

            // Draw wedge as a path: inner arc -> outer arc -> close
            int segments = 12;
            var pts = new List<Point>();

            // Outer arc (from theta - halfWidth to theta + halfWidth)
            for (int s = 0; s <= segments; s++)
            {
                double a = theta - halfWidth + (width * s / segments);
                pts.Add(transform.PolarToPixel(r, a));
            }

            // Back to center along inner arc
            for (int s = segments; s >= 0; s--)
            {
                double a = theta - halfWidth + (width * s / segments);
                pts.Add(transform.PolarToPixel(inner, a));
            }

            // A bar at a radius or an angle that cannot be placed is skipped whole, as a cartesian bar is.
            if (!pts.IsDrawable()) continue;
            Ctx.DrawPolygon(pts, style);
        }
    }
}
