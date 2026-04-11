// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders axes using the standard Cartesian (X, Y) coordinate system.</summary>
public sealed class CartesianAxesRenderer : AxesRenderer
{
    /// <summary>Initializes a new Cartesian axes renderer.</summary>
    public CartesianAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <inheritdoc />
    public override void Render()
    {
        var axesBg = Theme.AxesBackground;
        Ctx.DrawRectangle(PlotArea, axesBg, null, 0);

        // Compute data ranges
        var range = ComputeDataRanges();
        var transform = new DataTransform(range.XMin, range.XMax, range.YMin, range.YMax, PlotArea);

        // Auto-apply AutoDateLocator + AutoDateFormatter when scale is Date but no locator is set
        // (handles legacy SetXDateFormat() calls that only set scale + formatter, not a locator)
        if (Axes.XAxis.Scale == AxisScale.Date && Axes.XAxis.TickLocator is null)
        {
            var autoLocator = new AutoDateLocator();
            Axes.XAxis.TickLocator = autoLocator;
            Axes.XAxis.TickFormatter ??= new AutoDateFormatter(autoLocator);
        }

        // Compute tick values once for grid + ticks (respects TickLocator / Spacing)
        var xTicks = ComputeTickValues(range.XMin, range.XMax, Axes.XAxis);
        var yTicks = ComputeTickValues(range.YMin, range.YMax, Axes.YAxis);

        // Skip standard grid/ticks/frame for radar-only axes
        bool radarOnly = Axes.Series.Count > 0 && Axes.Series.All(s => s is RadarSeries);
        if (!radarOnly)
        {
            // Grid — axes-level setting overrides theme when explicitly set (Visible=true);
            // otherwise fall back to Theme.DefaultGrid so the theme controls the default.
            var effectiveGrid = Axes.Grid.Visible ? Axes.Grid : Theme.DefaultGrid;
            if (effectiveGrid.Visible)
                RenderGrid(xTicks, yTicks, range.YMin, transform, effectiveGrid);

            // Tick marks and labels
            RenderTicks(xTicks, yTicks, range.YMin, transform);
        }

        // Render span regions (behind everything)
        foreach (var span in Axes.Spans)
        {
            var spanColor = (span.Color ?? Colors.Tab10Blue).WithAlpha((byte)(span.Alpha * 255));
            if (span.Orientation == Orientation.Horizontal)
            {
                var topLeft = transform.DataToPixel(range.XMin, Math.Max(span.Min, span.Max));
                var bottomRight = transform.DataToPixel(range.XMax, Math.Min(span.Min, span.Max));
                var rect = new Rect(PlotArea.X, topLeft.Y, PlotArea.Width, bottomRight.Y - topLeft.Y);
                Ctx.DrawRectangle(rect, spanColor, null, 0);
            }
            else
            {
                var left = transform.DataToPixel(Math.Min(span.Min, span.Max), range.YMax);
                var right = transform.DataToPixel(Math.Max(span.Min, span.Max), range.YMin);
                var rect = new Rect(left.X, PlotArea.Y, right.X - left.X, PlotArea.Height);
                Ctx.DrawRectangle(rect, spanColor, null, 0);
            }
        }

        // Render reference lines
        foreach (var refLine in Axes.ReferenceLines)
        {
            var lineColor = refLine.Color ?? Colors.Gray;
            if (refLine.Orientation == Orientation.Horizontal)
            {
                var pt = transform.DataToPixel(range.XMin, refLine.Value);
                Ctx.DrawLine(
                    new Point(PlotArea.X, pt.Y),
                    new Point(PlotArea.X + PlotArea.Width, pt.Y),
                    lineColor, refLine.LineWidth, refLine.LineStyle);
            }
            else
            {
                var pt = transform.DataToPixel(refLine.Value, range.YMin);
                Ctx.DrawLine(
                    new Point(pt.X, PlotArea.Y),
                    new Point(pt.X, PlotArea.Y + PlotArea.Height),
                    lineColor, refLine.LineWidth, refLine.LineStyle);
            }
        }

        // Compute stacked baselines if needed
        if (Axes.BarMode == BarMode.Stacked)
        {
            var stackableList = Axes.Series.OfType<IStackable>().ToList();
            if (stackableList.Count > 1)
            {
                int catCount = stackableList[0].Values.Length;
                var cumulative = new double[catCount];
                foreach (var s in stackableList)
                {
                    s.StackBaseline = (double[])cumulative.Clone();
                    for (int c = 0; c < Math.Min(catCount, s.Values.Length); c++)
                        cumulative[c] += s.Values[c];
                }
            }
        }

        // Series
        RenderSeries(transform);

        // Secondary Y-axis series
        if (Axes.SecondaryYAxis is not null && Axes.SecondarySeries.Count > 0)
        {
            var secRange = ComputeSecondaryDataRanges(range.XMin, range.XMax);
            var secTransform = new DataTransform(secRange.XMin, secRange.XMax, secRange.YMin, secRange.YMax, PlotArea);
            var secYTicks = ComputeTickValues(secRange.YMin, secRange.YMax);

            for (int i = 0; i < Axes.SecondarySeries.Count; i++)
            {
                var series = Axes.SecondarySeries[i];
                if (!series.Visible) continue;
                int colorIndex = Axes.Series.Count + i;
                var seriesColor = Theme.CycleColors[colorIndex % Theme.CycleColors.Length];
                var renderer = new SvgSeriesRenderer(secTransform, Ctx, seriesColor, plotArea: PlotArea);
                var area = new RenderArea(PlotArea, Ctx);
                series.Accept(renderer, area);
            }

            // Right-side Y-axis ticks
            var tickFont = TickFont();
            foreach (var tick in secYTicks)
            {
                var pt = secTransform.DataToPixel(secRange.XMax, tick);
                Ctx.DrawLine(new Point(PlotArea.X + PlotArea.Width, pt.Y),
                    new Point(PlotArea.X + PlotArea.Width + 5, pt.Y),
                    Theme.ForegroundText, 1, LineStyle.Solid);
                Ctx.DrawText(Axes.SecondaryYAxis!.TickFormatter?.Format(tick) ?? FormatTick(tick),
                    new Point(PlotArea.X + PlotArea.Width + 8, pt.Y + 4),
                    tickFont, TextAlignment.Left);
            }

            if (Axes.SecondaryYAxis.Label is not null)
            {
                Ctx.DrawText(Axes.SecondaryYAxis.Label,
                    new Point(PlotArea.X + PlotArea.Width + 45, PlotArea.Y + PlotArea.Height / 2),
                    LabelFont(), TextAlignment.Center);
            }
        }

        // Annotations
        foreach (var annotation in Axes.Annotations)
        {
            var annotFont = annotation.Font ?? new Font
            {
                Family = Theme.DefaultFont.Family,
                Size = 10,
                Color = annotation.Color ?? Theme.ForegroundText
            };
            var textPos = transform.DataToPixel(annotation.X, annotation.Y);

            // Optional background fill behind text
            if (annotation.BackgroundColor.HasValue)
            {
                var textSize = Ctx.MeasureText(annotation.Text, annotFont);
                var bgRect = new Rect(textPos.X - 2, textPos.Y - textSize.Height, textSize.Width + 4, textSize.Height + 2);
                Ctx.DrawRectangle(bgRect, annotation.BackgroundColor.Value, null, 0);
            }

            // Draw text with alignment and optional rotation
            if (annotation.Rotation != 0)
                Ctx.DrawText(annotation.Text, textPos, annotFont, annotation.Alignment, annotation.Rotation);
            else
                Ctx.DrawText(annotation.Text, textPos, annotFont, annotation.Alignment);

            // Arrow (respects ArrowStyle)
            if (annotation.ArrowTargetX.HasValue && annotation.ArrowTargetY.HasValue
                && annotation.ArrowStyle != ArrowStyle.None)
            {
                var arrowTarget = transform.DataToPixel(annotation.ArrowTargetX.Value, annotation.ArrowTargetY.Value);
                var arrowColor = annotation.ArrowColor ?? annotation.Color ?? Theme.ForegroundText;
                Ctx.DrawLine(textPos, arrowTarget, arrowColor, 1, LineStyle.Solid);

                if (annotation.ArrowStyle == ArrowStyle.FancyArrow)
                {
                    // Small triangular arrowhead at target
                    double dx = arrowTarget.X - textPos.X;
                    double dy = arrowTarget.Y - textPos.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 0)
                    {
                        double ux = dx / len, uy = dy / len;
                        double nx = -uy, ny = ux; // perpendicular
                        const double headLen = 8, headHalf = 4;
                        var tip = arrowTarget;
                        var left  = new Point(tip.X - ux * headLen + nx * headHalf, tip.Y - uy * headLen + ny * headHalf);
                        var right = new Point(tip.X - ux * headLen - nx * headHalf, tip.Y - uy * headLen - ny * headHalf);
                        Ctx.DrawPolygon([tip, left, right], arrowColor, null, 0);
                    }
                }
            }
        }

        // Signal markers (buy/sell triangles)
        foreach (var signal in Axes.Signals)
        {
            var pt = transform.DataToPixel(signal.X, signal.Y);
            double s = signal.Size;
            var signalColor = signal.Color ?? (signal.Direction == SignalDirection.Buy ? Colors.Green : Colors.Red);

            Point[] triangle = signal.Direction == SignalDirection.Buy
                ? [new(pt.X, pt.Y + s), new(pt.X - s / 2, pt.Y + s * 2), new(pt.X + s / 2, pt.Y + s * 2)]
                : [new(pt.X, pt.Y - s), new(pt.X - s / 2, pt.Y - s * 2), new(pt.X + s / 2, pt.Y - s * 2)];
            Ctx.DrawPolygon(triangle, signalColor, null, 0);
        }

        // Axes spines — rendered after series so they appear on top of fills/areas
        if (!radarOnly)
            RenderSpines(transform);

        // Legend
        RenderLegend();

        // ColorBar
        RenderColorBar();

        // Axes title
        RenderTitle();

        // Axis labels
        RenderAxisLabels();
    }

    /// <summary>Renders major grid lines behind the plot area at each tick position.</summary>
    private void RenderGrid(double[] xTicks, double[] yTicks, double yMin, DataTransform transform, GridStyle grid)
    {
        var gridColor = grid.Color;
        var gridStyle = grid.LineStyle;
        double gridWidth = grid.LineWidth;

        foreach (var tick in xTicks)
        {
            var pt = transform.DataToPixel(tick, yMin);
            Ctx.DrawLine(
                new Point(pt.X, PlotArea.Y),
                new Point(pt.X, PlotArea.Y + PlotArea.Height),
                gridColor, gridWidth, gridStyle);
        }

        foreach (var tick in yTicks)
        {
            var pt = transform.DataToPixel(Axes.XAxis.Min ?? 0, tick);
            Ctx.DrawLine(
                new Point(PlotArea.X, pt.Y),
                new Point(PlotArea.X + PlotArea.Width, pt.Y),
                gridColor, gridWidth, gridStyle);
        }
    }

    /// <summary>Renders tick marks and tick labels along the X and Y axes, including minor ticks when enabled.</summary>
    private void RenderTicks(double[] xTicks, double[] yTicks, double yMin, DataTransform transform)
    {
        var tickFont = TickFont();

        var categoryLabeled = Axes.Series.OfType<ICategoryLabeled>().FirstOrDefault(s => s.CategoryLabels is not null);
        // When a custom TickLocator is set it takes priority — use the standard tick path so the
        // locator controls spacing (e.g. MultipleLocator(5,0.5) for bar charts with many bars).
        if (categoryLabeled?.CategoryLabels is not null && Axes.XAxis.TickLocator is null)
            RenderCategoryLabels(categoryLabeled.CategoryLabels, yMin, transform);
        else
        {
            var xFormatter = Axes.XAxis.TickFormatter;
            foreach (var tick in xTicks)
            {
                var pt = transform.DataToPixel(tick, yMin);
                Ctx.DrawLine(pt, new Point(pt.X, pt.Y + 5), Theme.ForegroundText, 1, LineStyle.Solid);
                Ctx.DrawText(xFormatter?.Format(tick) ?? FormatTick(tick),
                    new Point(pt.X, PlotArea.Y + PlotArea.Height + 15),
                    tickFont, TextAlignment.Center);
            }

            // Minor X ticks (no labels, shorter mark)
            if (Axes.XAxis.MinorTicks.Visible && xTicks.Length >= 2)
            {
                double majorStep = xTicks[1] - xTicks[0];
                double minorStep = majorStep / 5;
                double xStart = xTicks[0] - majorStep;
                double xEnd   = xTicks[^1] + majorStep;
                for (double mt = xStart; mt <= xEnd + minorStep * 0.01; mt += minorStep)
                {
                    mt = Math.Round(mt, 10);
                    // Skip positions that coincide with a major tick
                    if (Array.Exists(xTicks, t => Math.Abs(t - mt) < minorStep * 0.01)) continue;
                    var pt = transform.DataToPixel(mt, yMin);
                    if (pt.X < PlotArea.X || pt.X > PlotArea.X + PlotArea.Width) continue;
                    Ctx.DrawLine(pt, new Point(pt.X, pt.Y + 3), Theme.ForegroundText, 0.5, LineStyle.Solid);
                }
            }
        }

        var yFormatter = Axes.YAxis.TickFormatter;
        foreach (var tick in yTicks)
        {
            var pt = transform.DataToPixel(Axes.XAxis.Min ?? 0, tick);
            Ctx.DrawLine(new Point(pt.X - 5, pt.Y), pt, Theme.ForegroundText, 1, LineStyle.Solid);
            Ctx.DrawText(yFormatter?.Format(tick) ?? FormatTick(tick),
                new Point(PlotArea.X - 8, pt.Y + 4),
                tickFont, TextAlignment.Right);
        }

        // Minor Y ticks (no labels, shorter mark)
        if (Axes.YAxis.MinorTicks.Visible && yTicks.Length >= 2)
        {
            double majorStep = yTicks[1] - yTicks[0];
            double minorStep = majorStep / 5;
            double yStart = yTicks[0] - majorStep;
            double yEnd   = yTicks[^1] + majorStep;
            for (double mt = yStart; mt <= yEnd + minorStep * 0.01; mt += minorStep)
            {
                mt = Math.Round(mt, 10);
                if (Array.Exists(yTicks, t => Math.Abs(t - mt) < minorStep * 0.01)) continue;
                var pt = transform.DataToPixel(Axes.XAxis.Min ?? 0, mt);
                if (pt.Y < PlotArea.Y || pt.Y > PlotArea.Y + PlotArea.Height) continue;
                Ctx.DrawLine(new Point(pt.X - 3, pt.Y), pt, Theme.ForegroundText, 0.5, LineStyle.Solid);
            }
        }
    }

    /// <summary>Renders category labels centered below each category position on the X-axis.</summary>
    private void RenderCategoryLabels(string[] labels, double yMin, DataTransform transform)
    {
        var tickFont = TickFont();
        for (int i = 0; i < labels.Length; i++)
        {
            // Bar slot [i, i+1] has its center at i+0.5 — place the label there.
            var pt = transform.DataToPixel(i + 0.5, yMin);
            Ctx.DrawText(labels[i],
                new Point(pt.X, PlotArea.Y + PlotArea.Height + 15),
                tickFont, TextAlignment.Center);
        }
    }

    /// <summary>Computes the combined X and Y data ranges across all series on the axes,
    /// including shared axes ranges when sharex/sharey is configured.</summary>
    private DataRange ComputeDataRanges()
    {
        double xMin = Axes.XAxis.Min ?? double.MaxValue;
        double xMax = Axes.XAxis.Max ?? double.MinValue;
        double yMin = Axes.YAxis.Min ?? double.MaxValue;
        double yMax = Axes.YAxis.Max ?? double.MinValue;

        // Aggregate from this axes' series
        AggregateSeriesRanges(Axes, ref xMin, ref xMax, ref yMin, ref yMax);

        // Aggregate from shared axes' series (walk chain, guard against cycles)
        if (Axes.ShareXWith is not null)
        {
            var visited = new HashSet<Axes> { Axes };
            var current = Axes.ShareXWith;
            while (current is not null && visited.Add(current))
            {
                AggregateSeriesXRange(current, ref xMin, ref xMax);
                current = current.ShareXWith;
            }
        }

        if (Axes.ShareYWith is not null)
        {
            var visited = new HashSet<Axes> { Axes };
            var current = Axes.ShareYWith;
            while (current is not null && visited.Add(current))
            {
                AggregateSeriesYRange(current, ref yMin, ref yMax);
                current = current.ShareYWith;
            }
        }

        // Add padding
        if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
        if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
        if (Math.Abs(xMax - xMin) < 1e-10) { xMin -= 0.5; xMax += 0.5; }
        if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 0.5; yMax += 0.5; }

        double xPadding = (xMax - xMin) * Axes.XAxis.Margin;
        double yPadding = (yMax - yMin) * Axes.YAxis.Margin;

        if (!Axes.XAxis.Min.HasValue) xMin -= xPadding;
        if (!Axes.XAxis.Max.HasValue) xMax += xPadding;
        if (!Axes.YAxis.Min.HasValue) yMin -= yPadding;
        if (!Axes.YAxis.Max.HasValue) yMax += yPadding;

        return new DataRange(xMin, xMax, yMin, yMax);
    }

    private static void AggregateSeriesRanges(Axes axes, ref double xMin, ref double xMax, ref double yMin, ref double yMax)
    {
        var context = new AxesContextAdapter(axes);
        foreach (var series in axes.Series)
        {
            var c = series.ComputeDataRange(context);
            if (c.XMin.HasValue && c.XMin.Value < xMin) xMin = c.XMin.Value;
            if (c.XMax.HasValue && c.XMax.Value > xMax) xMax = c.XMax.Value;
            if (c.YMin.HasValue && c.YMin.Value < yMin) yMin = c.YMin.Value;
            if (c.YMax.HasValue && c.YMax.Value > yMax) yMax = c.YMax.Value;
        }
    }

    private static void AggregateSeriesXRange(Axes axes, ref double xMin, ref double xMax)
    {
        var context = new AxesContextAdapter(axes);
        foreach (var series in axes.Series)
        {
            var c = series.ComputeDataRange(context);
            if (c.XMin.HasValue && c.XMin.Value < xMin) xMin = c.XMin.Value;
            if (c.XMax.HasValue && c.XMax.Value > xMax) xMax = c.XMax.Value;
        }
    }

    private static void AggregateSeriesYRange(Axes axes, ref double yMin, ref double yMax)
    {
        var context = new AxesContextAdapter(axes);
        foreach (var series in axes.Series)
        {
            var c = series.ComputeDataRange(context);
            if (c.YMin.HasValue && c.YMin.Value < yMin) yMin = c.YMin.Value;
            if (c.YMax.HasValue && c.YMax.Value > yMax) yMax = c.YMax.Value;
        }
    }

    /// <summary>Computes the Y data range for series plotted against the secondary Y-axis.</summary>
    private DataRange ComputeSecondaryDataRanges(double xMin, double xMax)
    {
        double yMin = Axes.SecondaryYAxis?.Min ?? double.MaxValue;
        double yMax = Axes.SecondaryYAxis?.Max ?? double.MinValue;

        var context = new AxesContextAdapter(Axes);
        foreach (var series in Axes.SecondarySeries)
        {
            if (series is IHasDataRange hasRange)
            {
                var c = hasRange.ComputeDataRange(context);
                if (c.YMin.HasValue && c.YMin.Value < yMin) yMin = c.YMin.Value;
                if (c.YMax.HasValue && c.YMax.Value > yMax) yMax = c.YMax.Value;
            }
        }

        if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
        if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 0.5; yMax += 0.5; }

        double yPadding = (yMax - yMin) * 0.05;
        if (!Axes.SecondaryYAxis!.Min.HasValue) yMin -= yPadding;
        if (!Axes.SecondaryYAxis!.Max.HasValue) yMax += yPadding;

        return new DataRange(xMin, xMax, yMin, yMax);
    }

    /// <summary>Renders the four axis spines (border lines) based on the <see cref="Axes.Spines"/> configuration.</summary>
    private void RenderSpines(DataTransform transform)
    {
        var spines = Axes.Spines;
        var color = Theme.ForegroundText;
        double left = PlotArea.X;
        double right = PlotArea.X + PlotArea.Width;
        double top = PlotArea.Y;
        double bottom = PlotArea.Y + PlotArea.Height;

        if (spines.Bottom.Visible)
        {
            double y = ResolveSpineY(spines.Bottom, bottom, top, bottom, transform);
            DrawSpineLine(new Point(left, y), new Point(right, y), color, spines.Bottom.LineWidth);
        }

        if (spines.Top.Visible)
        {
            double y = ResolveSpineY(spines.Top, top, top, bottom, transform);
            DrawSpineLine(new Point(left, y), new Point(right, y), color, spines.Top.LineWidth);
        }

        if (spines.Left.Visible)
        {
            double x = ResolveSpineX(spines.Left, left, left, right, transform);
            DrawSpineLine(new Point(x, top), new Point(x, bottom), color, spines.Left.LineWidth);
        }

        if (spines.Right.Visible)
        {
            double x = ResolveSpineX(spines.Right, right, left, right, transform);
            DrawSpineLine(new Point(x, top), new Point(x, bottom), color, spines.Right.LineWidth);
        }
    }

    private double ResolveSpineY(SpineConfig spine, double edgeY, double plotTop, double plotBottom, DataTransform transform) =>
        spine.Position switch
        {
            SpinePosition.Data => transform.DataToPixel(0, spine.PositionValue).Y,
            SpinePosition.Axes => plotTop + (1 - spine.PositionValue) * (plotBottom - plotTop),
            _ => edgeY
        };

    private double ResolveSpineX(SpineConfig spine, double edgeX, double plotLeft, double plotRight, DataTransform transform) =>
        spine.Position switch
        {
            SpinePosition.Data => transform.DataToPixel(spine.PositionValue, 0).X,
            SpinePosition.Axes => plotLeft + spine.PositionValue * (plotRight - plotLeft),
            _ => edgeX
        };

    private void DrawSpineLine(Point p1, Point p2, Color color, double lineWidth)
    {
        Ctx.BeginGroup("spine");
        Ctx.DrawLine(p1, p2, color, lineWidth, LineStyle.Solid);
        Ctx.EndGroup();
    }
}
