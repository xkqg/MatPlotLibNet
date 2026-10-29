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
    /// <remarks>Every polar series, hidden ones too, each through the radial scale it is drawn on (<see cref="ScaleOfTheAxes"/>,
    /// the one <see cref="Render"/> draws with); nothing is drawn.</remarks>
    internal override MarkObstacles LegendObstacles()
    {
        var marks = new MarkObstacles.Builder();
        CollectMarks(Axes.Series, new DataTransform(0, 1, 0, 1, PlotArea), marks, radial: ScaleOfTheAxes());
        return marks.Build();
    }

    /// <summary>The ONE radial scale of these axes: the grid and every series are drawn on it, and so are the marks a legend
    /// keeps clear of. Worked out from the series, the limit the radial axis was given and the room the rings have.</summary>
    private RadialScale ScaleOfTheAxes() =>
        RadialScale.For(Axes.Series, Axes.YAxis, PolarTransform.RadiusOf(PlotArea), TickFont().Size, Axes.BarMode);

    /// <inheritdoc />
    public override void Render()
    {
        var axesBg = Theme.AxesBackground;
        Ctx.DrawRectangle(PlotArea, new ShapeStyle(axesBg, null, 0));

        var scale = ScaleOfTheAxes();
        double rMax = scale.Extent;
        var transform = new PolarTransform(PlotArea, rMax);
        var gridColor = Colors.GridGray;
        var labelFont = TickFont();

        // The outermost circle stands at the extent: a ring does not always stand there, and the grid is closed by it.
        if (!scale.Rings.Any(ring => Math.Abs(ring - rMax) < 1e-9 * rMax))
            Ctx.DrawEllipse(
                new Rect(transform.CenterX - transform.MaxRadius, transform.CenterY - transform.MaxRadius, transform.MaxRadius * 2, transform.MaxRadius * 2),
                new ShapeStyle(null, gridColor, 0.5));

        // The rings stand at round numbers and are labelled along one spoke, at matplotlib's 22.5 degrees.
        double labelAngle = RadialScale.LabelAngleDegrees * Math.PI / 180;
        for (int i = 0; i < scale.Rings.Length; i++)
        {
            double r = transform.MaxRadius * scale.Rings[i] / rMax;
            Ctx.DrawEllipse(
                new Rect(transform.CenterX - r, transform.CenterY - r, r * 2, r * 2),
                new ShapeStyle(null, gridColor, 0.5));

            var onTheSpoke = transform.PolarToPixel(scale.Rings[i], labelAngle);
            Ctx.DrawText(scale.Labels[i], new Point(onTheSpoke.X + 3, onTheSpoke.Y - 3), labelFont, TextAlignment.Left);
        }

        // Draw radial axis lines (every 30 degrees = 12 lines)
        for (int i = 0; i < 12; i++)
        {
            double angle = i * Math.PI / 6;
            var outer = transform.PolarToPixel(rMax, angle);
            Ctx.DrawLine(
                new Point(transform.CenterX, transform.CenterY),
                outer, new StrokeStyle(gridColor, 0.5, LineStyle.Solid));

            // Angle label
            string label = $"{i * 30}\u00b0";
            Ctx.DrawText(label,
                new Point(outer.X + (outer.X > transform.CenterX ? 8 : -8),
                          outer.Y + (outer.Y > transform.CenterY ? 12 : -4)),
                labelFont, outer.X >= transform.CenterX ? TextAlignment.Left : TextAlignment.Right);
        }

        // Render series
        RenderSeries(scale);

        // Legend
        RenderLegend();

        // Title
        RenderTitle();
    }
}
