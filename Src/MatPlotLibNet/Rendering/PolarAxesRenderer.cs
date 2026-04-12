// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders axes using the Polar (r, theta) coordinate system with circular grid.</summary>
public sealed class PolarAxesRenderer : AxesRenderer
{
    /// <summary>Initializes a new Polar axes renderer.</summary>
    public PolarAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <inheritdoc />
    public override void Render()
    {
        var axesBg = Theme.AxesBackground;
        Ctx.DrawRectangle(PlotArea, axesBg, null, 0);

        // Determine max R from all polar series
        double rMax = 1;
        foreach (var s in Axes.Series)
        {
            if (s is IPolarSeries polar && polar.R.Length > 0)
            {
                double max = polar.R.Max();
                if (max > rMax) rMax = max;
            }
        }
        rMax *= 1.1; // 10% padding

        var transform = new PolarTransform(PlotArea, rMax);
        var gridColor = Colors.GridGray;
        var labelFont = TickFont();

        // Draw concentric circle grid
        for (int ring = 1; ring <= 5; ring++)
        {
            double frac = ring / 5.0;
            double r = transform.MaxRadius * frac;
            Ctx.DrawEllipse(
                new Rect(transform.CenterX - r, transform.CenterY - r, r * 2, r * 2),
                null, gridColor, 0.5);

            // Tick label on right side (respects custom formatter if set)
            double tickValue = rMax * frac;
            string tickLabel = Axes.YAxis.TickFormatter?.Format(tickValue) ?? FormatTick(tickValue);
            Ctx.DrawText(tickLabel,
                new Point(transform.CenterX + r + 4, transform.CenterY + 4),
                labelFont, TextAlignment.Left);
        }

        // Draw radial axis lines (every 30 degrees = 12 lines)
        for (int i = 0; i < 12; i++)
        {
            double angle = i * Math.PI / 6;
            var outer = transform.PolarToPixel(rMax, angle);
            Ctx.DrawLine(
                new Point(transform.CenterX, transform.CenterY),
                outer, gridColor, 0.5, LineStyle.Solid);

            // Angle label
            string label = $"{i * 30}\u00b0";
            Ctx.DrawText(label,
                new Point(outer.X + (outer.X > transform.CenterX ? 8 : -8),
                          outer.Y + (outer.Y > transform.CenterY ? 12 : -4)),
                labelFont, outer.X >= transform.CenterX ? TextAlignment.Left : TextAlignment.Right);
        }

        // Render series
        RenderSeries();

        // Legend
        RenderLegend();

        // Title
        RenderTitle();
    }
}
