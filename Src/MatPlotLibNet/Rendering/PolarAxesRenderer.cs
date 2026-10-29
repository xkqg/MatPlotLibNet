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
        // The rings and spokes are the axes' grid: the grid of the theme this is drawn under with the author's change on
        // top, drawn unless the author switched it off. Their labels are not the grid, and stay.
        var grid = Axes.ShapeGridUnder(Theme);
        var labelFont = TickFont();

        // The outermost circle stands at the extent: a ring does not always stand there, and the grid is closed by it.
        if (grid.Visible && !scale.Rings.Any(ring => Math.Abs(ring - rMax) < 1e-9 * rMax))
            DrawRing(transform, transform.MaxRadius, grid);

        // The rings stand at round numbers and are labelled along one spoke, at matplotlib's 22.5 degrees.
        double labelAngle = RadialScale.LabelAngleDegrees * Math.PI / 180;
        for (int i = 0; i < scale.Rings.Length; i++)
        {
            double r = transform.MaxRadius * scale.Rings[i] / rMax;
            if (grid.Visible)
                DrawRing(transform, r, grid);

            var onTheSpoke = transform.PolarToPixel(scale.Rings[i], labelAngle);
            Ctx.DrawText(scale.Labels[i], new Point(onTheSpoke.X + 3, onTheSpoke.Y - 3), labelFont, TextAlignment.Left);
        }

        // Draw radial axis lines (every 30 degrees = 12 lines)
        for (int i = 0; i < 12; i++)
        {
            double angle = i * Math.PI / 6;
            var outer = transform.PolarToPixel(rMax, angle);
            if (grid.Visible)
                Ctx.DrawLine(
                    new Point(transform.CenterX, transform.CenterY),
                    outer, new StrokeStyle(grid.Color, grid.LineWidth, grid.LineStyle));

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

    /// <summary>How many segments a ring that is not solid is cut into: a segment turns two degrees, so the polyline stands on its
    /// circle to a hundredth of a pixel at the radii a chart is drawn at.</summary>
    private const int RingSegments = 180;

    /// <summary>Draws one ring of the grid at <paramref name="radius"/> pixels from the centre, in the style of the grid. A solid
    /// ring is a circle. A circle has no dash, so a dotted or dashed one is a closed polyline of <see cref="RingSegments"/>
    /// segments, drawn through the line drawing that dashes, in the grid's colour and width.</summary>
    private void DrawRing(PolarTransform transform, double radius, GridStyle grid)
    {
        if (grid.LineStyle == LineStyle.Solid)
        {
            Ctx.DrawEllipse(
                new Rect(transform.CenterX - radius, transform.CenterY - radius, radius * 2, radius * 2),
                new ShapeStyle(null, grid.Color, grid.LineWidth));
            return;
        }

        Point[] ring = [.. Enumerable.Range(0, RingSegments + 1).Select(i =>
        {
            double angle = 2 * Math.PI * i / RingSegments;
            return new Point(transform.CenterX + radius * Math.Cos(angle), transform.CenterY + radius * Math.Sin(angle));
        })];
        ring[^1] = ring[0];
        Ctx.DrawLines(ring, new StrokeStyle(grid.Color, grid.LineWidth, grid.LineStyle));
    }
}
