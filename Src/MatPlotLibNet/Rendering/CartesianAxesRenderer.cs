// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

        // Apply axis breaks: compress the visible range by removing break gaps
        var (cXMin, cXMax) = Axes.XBreaks.Count > 0
            ? AxisBreakMapper.CompressedRange(Axes.XBreaks, range.XMin, range.XMax)
            : (range.XMin, range.XMax);
        var (cYMin, cYMax) = Axes.YBreaks.Count > 0
            ? AxisBreakMapper.CompressedRange(Axes.YBreaks, range.YMin, range.YMax)
            : (range.YMin, range.YMax);

        var transform = new DataTransform(cXMin, cXMax, cYMin, cYMax, PlotArea);

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
        var spanLabelFont = TickFont();
        foreach (var span in Axes.Spans)
        {
            var spanColor = (span.Color ?? Colors.Tab10Blue).WithAlpha((byte)(span.Alpha * 255));
            var borderColor = span.EdgeColor ?? (span.Color ?? Colors.Tab10Blue);
            if (span.Orientation == Orientation.Horizontal)
            {
                var topLeft = transform.DataToPixel(range.XMin, Math.Max(span.Min, span.Max));
                var bottomRight = transform.DataToPixel(range.XMax, Math.Min(span.Min, span.Max));
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
                var left = transform.DataToPixel(Math.Min(span.Min, span.Max), range.YMax);
                var right = transform.DataToPixel(Math.Max(span.Min, span.Max), range.YMin);
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

        // Render reference lines
        var refLabelFont = TickFont();
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
                if (refLine.Label is not null)
                {
                    var labelFont = refLabelFont with { Color = lineColor };
                    Ctx.DrawText(refLine.Label,
                        new Point(PlotArea.X + PlotArea.Width, pt.Y - 2),
                        labelFont, TextAlignment.Right);
                }
            }
            else
            {
                var pt = transform.DataToPixel(refLine.Value, range.YMin);
                Ctx.DrawLine(
                    new Point(pt.X, PlotArea.Y),
                    new Point(pt.X, PlotArea.Y + PlotArea.Height),
                    lineColor, refLine.LineWidth, refLine.LineStyle);
                if (refLine.Label is not null)
                {
                    var labelFont = refLabelFont with { Color = lineColor };
                    Ctx.DrawText(refLine.Label,
                        new Point(pt.X + 2, PlotArea.Y + refLabelFont.Size),
                        labelFont, TextAlignment.Left);
                }
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

        // Axis break markers (drawn on top of series, overlaying the break region)
        if (Axes.XBreaks.Count > 0) RenderBreakMarkers(Axes.XBreaks, transform, isXAxis: true);
        if (Axes.YBreaks.Count > 0) RenderBreakMarkers(Axes.YBreaks, transform, isXAxis: false);

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

        // Secondary X-axis series (TwinY — top edge)
        if (Axes.SecondaryXAxis is not null && Axes.XSecondarySeries.Count > 0)
        {
            var secXRange = ComputeSecondaryXDataRanges(range.YMin, range.YMax);
            var secXTransform = new DataTransform(secXRange.XMin, secXRange.XMax, secXRange.YMin, secXRange.YMax, PlotArea);
            var secXTicks = ComputeTickValues(secXRange.XMin, secXRange.XMax);

            for (int i = 0; i < Axes.XSecondarySeries.Count; i++)
            {
                var series = Axes.XSecondarySeries[i];
                if (!series.Visible) continue;
                int colorIndex = Axes.Series.Count + Axes.SecondarySeries.Count + i;
                var seriesColor = Theme.CycleColors[colorIndex % Theme.CycleColors.Length];
                var renderer = new SvgSeriesRenderer(secXTransform, Ctx, seriesColor, plotArea: PlotArea);
                var area = new RenderArea(PlotArea, Ctx);
                series.Accept(renderer, area);
            }

            // Top-edge X-axis ticks
            var tickFont = TickFont();
            foreach (var tick in secXTicks)
            {
                var pt = secXTransform.DataToPixel(tick, secXRange.YMax);
                Ctx.DrawLine(new Point(pt.X, PlotArea.Y),
                    new Point(pt.X, PlotArea.Y - 5),
                    Theme.ForegroundText, 1, LineStyle.Solid);
                Ctx.DrawText(Axes.SecondaryXAxis!.TickFormatter?.Format(tick) ?? FormatTick(tick),
                    new Point(pt.X, PlotArea.Y - 8),
                    tickFont, TextAlignment.Center);
            }

            if (Axes.SecondaryXAxis.Label is not null)
            {
                Ctx.DrawText(Axes.SecondaryXAxis.Label,
                    new Point(PlotArea.X + PlotArea.Width / 2, PlotArea.Y - 28),
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

            // Background box or legacy background fill behind text
            var textSize = Ctx.MeasureText(annotation.Text, annotFont);
            var textBounds = new Rect(textPos.X - 2, textPos.Y - textSize.Height, textSize.Width + 4, textSize.Height + 2);

            if (annotation.BoxStyle != BoxStyle.None)
            {
                CalloutBoxRenderer.Draw(Ctx, textBounds, annotation.BoxStyle,
                    annotation.BoxPadding, annotation.BoxCornerRadius,
                    annotation.BoxFaceColor, annotation.BoxEdgeColor, annotation.BoxLineWidth);
            }
            else if (annotation.BackgroundColor.HasValue)
            {
                Ctx.DrawRectangle(textBounds, annotation.BackgroundColor.Value, null, 0);
            }

            // Draw text with alignment and optional rotation (rotation=0 is a no-op in the SVG renderer)
            Ctx.DrawText(annotation.Text, textPos, annotFont, annotation.Alignment, annotation.Rotation);

            // Connection path + arrowhead (respects ConnectionStyle + ArrowStyle)
            if (annotation.ArrowTargetX.HasValue && annotation.ArrowTargetY.HasValue
                && annotation.ArrowStyle != ArrowStyle.None)
            {
                var arrowTarget = transform.DataToPixel(annotation.ArrowTargetX.Value, annotation.ArrowTargetY.Value);
                var arrowColor = annotation.ArrowColor ?? annotation.Color ?? Theme.ForegroundText;

                // Connection path
                var connPath = ConnectionPathBuilder.BuildPath(textPos, arrowTarget,
                    annotation.ConnectionStyle, annotation.ConnectionRad);
                Ctx.DrawPath(connPath, null, arrowColor, 1);

                // Arrowhead at target
                double dx = arrowTarget.X - textPos.X;
                double dy = arrowTarget.Y - textPos.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len > 0)
                {
                    double ux = dx / len, uy = dy / len;
                    var headPolygon = ArrowHeadBuilder.BuildPolygon(arrowTarget, ux, uy,
                        annotation.ArrowStyle, annotation.ArrowHeadSize);
                    if (headPolygon.Count > 0)
                        Ctx.DrawPolygon([.. headPolygon], arrowColor, null, 0);

                    var headPath = ArrowHeadBuilder.BuildPath(arrowTarget, ux, uy,
                        annotation.ArrowStyle, annotation.ArrowHeadSize);
                    if (headPath is { Count: > 0 })
                        Ctx.DrawPath(headPath, null, arrowColor, 1);
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

        bool drawX = grid.Axis is GridAxis.X or GridAxis.Both;
        bool drawY = grid.Axis is GridAxis.Y or GridAxis.Both;

        // Major grid lines
        if (grid.Which is GridWhich.Major or GridWhich.Both)
        {
            if (drawX)
            {
                foreach (var tick in xTicks)
                {
                    var pt = transform.DataToPixel(tick, yMin);
                    Ctx.DrawLine(
                        new Point(pt.X, PlotArea.Y),
                        new Point(pt.X, PlotArea.Y + PlotArea.Height),
                        gridColor, gridWidth, gridStyle);
                }
            }

            if (drawY)
            {
                foreach (var tick in yTicks)
                {
                    var pt = transform.DataToPixel(Axes.XAxis.Min ?? 0, tick);
                    Ctx.DrawLine(
                        new Point(PlotArea.X, pt.Y),
                        new Point(PlotArea.X + PlotArea.Width, pt.Y),
                        gridColor, gridWidth, gridStyle);
                }
            }
        }

        // Minor grid lines (thinner and more transparent)
        if (grid.Which is GridWhich.Minor or GridWhich.Both)
        {
            double minorWidth = gridWidth * 0.5;
            var minorColor = gridColor.WithAlpha((byte)(gridColor.A / 2));

            if (drawX && xTicks.Length >= 2)
            {
                double majorStep = xTicks[1] - xTicks[0];
                double minorStep = majorStep / 5;
                double xStart = xTicks[0] - majorStep;
                double xEnd   = xTicks[^1] + majorStep;
                for (double mt = xStart; mt <= xEnd + minorStep * 0.01; mt += minorStep)
                {
                    mt = Math.Round(mt, 10);
                    if (Array.Exists(xTicks, t => Math.Abs(t - mt) < minorStep * 0.01)) continue;
                    var pt = transform.DataToPixel(mt, yMin);
                    if (pt.X < PlotArea.X || pt.X > PlotArea.X + PlotArea.Width) continue;
                    Ctx.DrawLine(new Point(pt.X, PlotArea.Y), new Point(pt.X, PlotArea.Y + PlotArea.Height),
                        minorColor, minorWidth, gridStyle);
                }
            }

            if (drawY && yTicks.Length >= 2)
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
                    Ctx.DrawLine(new Point(PlotArea.X, pt.Y), new Point(PlotArea.X + PlotArea.Width, pt.Y),
                        minorColor, minorWidth, gridStyle);
                }
            }
        }
    }

    /// <summary>Renders tick marks and tick labels along the X and Y axes, including minor ticks when enabled.</summary>
    private void RenderTicks(double[] xTicks, double[] yTicks, double yMin, DataTransform transform)
    {
        // --- X axis ticks ---
        var xMajor = Axes.XAxis.MajorTicks;
        var xMinor = Axes.XAxis.MinorTicks;
        var xTickColor  = xMajor.Color ?? Theme.ForegroundText;
        var xTickLength = xMajor.Length;
        var xTickWidth  = xMajor.Width;

        // Build tick label font from theme, applying overrides
        var baseFont = TickFont();
        var xLabelFont = baseFont;
        if (xMajor.LabelSize.HasValue)  xLabelFont = xLabelFont with { Size  = xMajor.LabelSize.Value };
        if (xMajor.LabelColor.HasValue) xLabelFont = xLabelFont with { Color = xMajor.LabelColor };

        var yLabelFont = baseFont;
        var yMajor = Axes.YAxis.MajorTicks;
        if (yMajor.LabelSize.HasValue)  yLabelFont = yLabelFont with { Size  = yMajor.LabelSize.Value };
        if (yMajor.LabelColor.HasValue) yLabelFont = yLabelFont with { Color = yMajor.LabelColor };

        var categoryLabeled = Axes.Series.OfType<ICategoryLabeled>().FirstOrDefault(s => s.CategoryLabels is not null);
        // When a custom TickLocator is set it takes priority — use the standard tick path so the
        // locator controls spacing (e.g. MultipleLocator(5,0.5) for bar charts with many bars).
        if (categoryLabeled?.CategoryLabels is not null && Axes.XAxis.TickLocator is null)
            RenderCategoryLabels(categoryLabeled.CategoryLabels, yMin, transform);
        else
        {
            var xFormatter = Axes.XAxis.TickFormatter;
            double xAxisY = PlotArea.Y + PlotArea.Height;
            double xSpineHalf = Axes.Spines.Bottom.LineWidth / 2.0;
            foreach (var tick in xTicks)
            {
                var pt = transform.DataToPixel(tick, yMin);
                DrawTickMark(pt.X, xAxisY, isVertical: true, xTickLength, xTickColor, xTickWidth, xMajor.Direction, xSpineHalf);
                double labelY = xAxisY + xTickLength + xMajor.Pad + xLabelFont.Size;
                Ctx.DrawText(xFormatter?.Format(tick) ?? FormatTick(tick),
                    new Point(pt.X, labelY),
                    xLabelFont, TextAlignment.Center);
            }

            // Minor X ticks (no labels, shorter mark)
            if (xMinor.Visible && xTicks.Length >= 2)
            {
                var xMinorColor = xMinor.Color ?? Theme.ForegroundText;
                double minorLength = xMinor.Length;
                double majorStep = xTicks[1] - xTicks[0];
                double minorStep = majorStep / 5;
                double xStart = xTicks[0] - majorStep;
                double xEnd   = xTicks[^1] + majorStep;
                for (double mt = xStart; mt <= xEnd + minorStep * 0.01; mt += minorStep)
                {
                    mt = Math.Round(mt, 10);
                    if (Array.Exists(xTicks, t => Math.Abs(t - mt) < minorStep * 0.01)) continue;
                    var pt = transform.DataToPixel(mt, yMin);
                    if (pt.X < PlotArea.X || pt.X > PlotArea.X + PlotArea.Width) continue;
                    DrawTickMark(pt.X, xAxisY, isVertical: true, minorLength, xMinorColor, xMinor.Width, xMinor.Direction, xSpineHalf);
                }
            }
        }

        // --- Y axis ticks ---
        var yTickColor  = yMajor.Color ?? Theme.ForegroundText;
        var yTickLength = yMajor.Length;
        var yTickWidth  = yMajor.Width;
        double yAxisX = PlotArea.X;
        double ySpineHalf = Axes.Spines.Left.LineWidth / 2.0;

        var yFormatter = Axes.YAxis.TickFormatter;
        foreach (var tick in yTicks)
        {
            var pt = transform.DataToPixel(Axes.XAxis.Min ?? 0, tick);
            DrawTickMark(yAxisX, pt.Y, isVertical: false, yTickLength, yTickColor, yTickWidth, yMajor.Direction, ySpineHalf);
            double labelX = yAxisX - yTickLength - yMajor.Pad;
            Ctx.DrawText(yFormatter?.Format(tick) ?? FormatTick(tick),
                new Point(labelX, pt.Y + 4),
                yLabelFont, TextAlignment.Right);
        }

        // Minor Y ticks (no labels, shorter mark)
        var yMinor = Axes.YAxis.MinorTicks;
        if (yMinor.Visible && yTicks.Length >= 2)
        {
            var yMinorColor = yMinor.Color ?? Theme.ForegroundText;
            double minorLength = yMinor.Length;
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
                DrawTickMark(yAxisX, pt.Y, isVertical: false, minorLength, yMinorColor, yMinor.Width, yMinor.Direction, ySpineHalf);
            }
        }
    }

    /// <summary>
    /// Draws a single tick mark at the given axis edge position.
    /// For vertical ticks (X-axis): tickX is the data pixel position, axisEdge is the axis Y coordinate.
    /// For horizontal ticks (Y-axis): axisEdge is the axis X coordinate, tickY is the data pixel position.
    /// <para>
    /// <paramref name="spineHalfWidth"/> is half the spine stroke width; the tick is extended
    /// inward by this amount so it visually touches (overlaps) the spine regardless of subpixel
    /// rounding — matching matplotlib's behaviour where ticks are drawn through the spine line.
    /// </para>
    /// </summary>
    private void DrawTickMark(double tickPos, double axisEdge, bool isVertical,
        double length, Color color, double width, TickDirection direction, double spineHalfWidth = 0.0)
    {
        if (isVertical)
        {
            // X-axis tick: axisEdge is the bottom spine Y.
            // Out → extends downward (Y+); the inward end is pulled up into the spine stroke.
            // In  → extends upward (Y-); the inward end is pushed down into the spine stroke.
            double startY = direction switch
            {
                TickDirection.In    => axisEdge - length,
                TickDirection.InOut => axisEdge - length,
                _                   => axisEdge - spineHalfWidth, // Out: start inside spine
            };
            double endY = direction switch
            {
                TickDirection.In    => axisEdge + spineHalfWidth,  // In: end inside spine
                TickDirection.InOut => axisEdge + length,
                _                   => axisEdge + length,           // Out
            };
            if (Math.Abs(endY - startY) > 0.1)
                Ctx.DrawLine(new Point(tickPos, startY), new Point(tickPos, endY), color, width, LineStyle.Solid);
        }
        else
        {
            // Y-axis tick: axisEdge is the left spine X.
            // Out → extends leftward (X-); the inward end is pulled right into the spine stroke.
            // In  → extends rightward (X+); the inward end is pushed left into the spine stroke.
            double startX = direction switch
            {
                TickDirection.In    => axisEdge + spineHalfWidth,  // In: start inside spine
                TickDirection.InOut => axisEdge - length,
                _                   => axisEdge - length,           // Out
            };
            double endX = direction switch
            {
                TickDirection.In    => axisEdge + length,
                TickDirection.InOut => axisEdge + length,
                _                   => axisEdge + spineHalfWidth,  // Out: end inside spine
            };
            if (Math.Abs(endX - startX) > 0.1)
                Ctx.DrawLine(new Point(startX, tickPos), new Point(endX, tickPos), color, width, LineStyle.Solid);
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

    /// <summary>Computes the X data range for series plotted against the secondary X-axis.</summary>
    private DataRange ComputeSecondaryXDataRanges(double yMin, double yMax)
    {
        double xMin = Axes.SecondaryXAxis?.Min ?? double.MaxValue;
        double xMax = Axes.SecondaryXAxis?.Max ?? double.MinValue;

        var context = new AxesContextAdapter(Axes);
        foreach (var series in Axes.XSecondarySeries)
        {
            if (series is IHasDataRange hasRange)
            {
                var c = hasRange.ComputeDataRange(context);
                if (c.XMin.HasValue && c.XMin.Value < xMin) xMin = c.XMin.Value;
                if (c.XMax.HasValue && c.XMax.Value > xMax) xMax = c.XMax.Value;
            }
        }

        if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
        if (Math.Abs(xMax - xMin) < 1e-10) { xMin -= 0.5; xMax += 0.5; }

        double xPadding = (xMax - xMin) * 0.05;
        if (!Axes.SecondaryXAxis!.Min.HasValue) xMin -= xPadding;
        if (!Axes.SecondaryXAxis!.Max.HasValue) xMax += xPadding;

        return new DataRange(xMin, xMax, yMin, yMax);
    }

    /// <summary>Renders the four axis spines (border lines) based on the <see cref="Axes.Spines"/> configuration.</summary>
    private void RenderSpines(DataTransform transform)
    {
        var spines = Axes.Spines;
        double left = PlotArea.X;
        double right = PlotArea.X + PlotArea.Width;
        double top = PlotArea.Y;
        double bottom = PlotArea.Y + PlotArea.Height;

        if (spines.Bottom.Visible)
        {
            double y = ResolveSpineY(spines.Bottom, bottom, top, bottom, transform);
            DrawSpineLine(new Point(left, y), new Point(right, y),
                spines.Bottom.Color ?? Theme.ForegroundText, spines.Bottom.LineWidth, spines.Bottom.LineStyle);
        }

        if (spines.Top.Visible)
        {
            double y = ResolveSpineY(spines.Top, top, top, bottom, transform);
            DrawSpineLine(new Point(left, y), new Point(right, y),
                spines.Top.Color ?? Theme.ForegroundText, spines.Top.LineWidth, spines.Top.LineStyle);
        }

        if (spines.Left.Visible)
        {
            double x = ResolveSpineX(spines.Left, left, left, right, transform);
            DrawSpineLine(new Point(x, top), new Point(x, bottom),
                spines.Left.Color ?? Theme.ForegroundText, spines.Left.LineWidth, spines.Left.LineStyle);
        }

        if (spines.Right.Visible)
        {
            double x = ResolveSpineX(spines.Right, right, left, right, transform);
            DrawSpineLine(new Point(x, top), new Point(x, bottom),
                spines.Right.Color ?? Theme.ForegroundText, spines.Right.LineWidth, spines.Right.LineStyle);
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

    private void DrawSpineLine(Point p1, Point p2, Color color, double lineWidth, LineStyle lineStyle = LineStyle.Solid)
    {
        Ctx.BeginGroup("spine");
        Ctx.DrawLine(p1, p2, color, lineWidth, lineStyle);
        Ctx.EndGroup();
    }

    private void RenderBreakMarkers(IReadOnlyList<Models.AxisBreak> breaks, DataTransform transform, bool isXAxis)
    {
        const double MarkerHalfSize = 6.0;
        const double MarkerThickness = 1.5;
        var markerColor = Theme.ForegroundText;
        var bgColor = Theme.AxesBackground;

        foreach (var b in breaks)
        {
            if (b.Style == Models.BreakStyle.None) continue;

            if (isXAxis)
            {
                double compressedX = AxisBreakMapper.Remap(breaks, b.From, transform.DataXMin, transform.DataXMax);
                if (double.IsNaN(compressedX)) continue;
                var pt = transform.DataToPixel(compressedX, transform.DataYMin);
                DrawAxisBreakMark(b.Style, markerColor, bgColor, MarkerHalfSize, MarkerThickness,
                    pt.X, PlotArea.Y, PlotArea.Y + PlotArea.Height, horizontal: true);
            }
            else
            {
                double compressedY = AxisBreakMapper.Remap(breaks, b.From, transform.DataYMin, transform.DataYMax);
                if (double.IsNaN(compressedY)) continue;
                var pt = transform.DataToPixel(transform.DataXMin, compressedY);
                DrawAxisBreakMark(b.Style, markerColor, bgColor, MarkerHalfSize, MarkerThickness,
                    pt.Y, PlotArea.X, PlotArea.X + PlotArea.Width, horizontal: false);
            }
        }
    }

    private void DrawAxisBreakMark(
        Models.BreakStyle style, Color lineColor, Color bgColor,
        double halfSize, double thickness,
        double pos, double edgeA, double edgeB, bool horizontal)
    {
        // Draw a small white rectangle to "erase" the axis spine at the break position
        const double GapHalf = 4.0;
        if (horizontal)
        {
            Ctx.DrawRectangle(
                new Rect(pos - GapHalf, edgeA, GapHalf * 2, edgeB - edgeA),
                bgColor, null, 0);
        }
        else
        {
            Ctx.DrawRectangle(
                new Rect(edgeA, pos - GapHalf, edgeB - edgeA, GapHalf * 2),
                bgColor, null, 0);
        }

        if (style == Models.BreakStyle.Zigzag)
        {
            // Draw 3-point zigzag crossing the axis at the break boundary
            if (horizontal)
            {
                // Zigzag across the spine on both sides
                Ctx.DrawLine(new Point(pos - halfSize, edgeA + (edgeB - edgeA) * 0.4),
                    new Point(pos, edgeA + (edgeB - edgeA) * 0.5),
                    lineColor, thickness, LineStyle.Solid);
                Ctx.DrawLine(new Point(pos, edgeA + (edgeB - edgeA) * 0.5),
                    new Point(pos + halfSize, edgeA + (edgeB - edgeA) * 0.6),
                    lineColor, thickness, LineStyle.Solid);
                Ctx.DrawLine(new Point(pos - halfSize, edgeB - (edgeB - edgeA) * 0.6),
                    new Point(pos, edgeB - (edgeB - edgeA) * 0.5),
                    lineColor, thickness, LineStyle.Solid);
                Ctx.DrawLine(new Point(pos, edgeB - (edgeB - edgeA) * 0.5),
                    new Point(pos + halfSize, edgeB - (edgeB - edgeA) * 0.4),
                    lineColor, thickness, LineStyle.Solid);
            }
            else
            {
                Ctx.DrawLine(new Point(edgeA + (edgeB - edgeA) * 0.4, pos - halfSize),
                    new Point(edgeA + (edgeB - edgeA) * 0.5, pos),
                    lineColor, thickness, LineStyle.Solid);
                Ctx.DrawLine(new Point(edgeA + (edgeB - edgeA) * 0.5, pos),
                    new Point(edgeA + (edgeB - edgeA) * 0.6, pos + halfSize),
                    lineColor, thickness, LineStyle.Solid);
            }
        }
        else if (style == Models.BreakStyle.Straight)
        {
            // Two short diagonal parallel lines (//)
            if (horizontal)
            {
                Ctx.DrawLine(new Point(pos - halfSize * 0.5, edgeA + (edgeB - edgeA) * 0.3),
                    new Point(pos + halfSize * 0.5, edgeA + (edgeB - edgeA) * 0.7),
                    lineColor, thickness, LineStyle.Solid);
                Ctx.DrawLine(new Point(pos - halfSize * 0.5 + halfSize * 0.6, edgeA + (edgeB - edgeA) * 0.3),
                    new Point(pos + halfSize * 0.5 + halfSize * 0.6, edgeA + (edgeB - edgeA) * 0.7),
                    lineColor, thickness, LineStyle.Solid);
            }
            else
            {
                Ctx.DrawLine(new Point(edgeA + (edgeB - edgeA) * 0.3, pos - halfSize * 0.5),
                    new Point(edgeA + (edgeB - edgeA) * 0.7, pos + halfSize * 0.5),
                    lineColor, thickness, LineStyle.Solid);
                Ctx.DrawLine(new Point(edgeA + (edgeB - edgeA) * 0.3, pos - halfSize * 0.5 + halfSize * 0.6),
                    new Point(edgeA + (edgeB - edgeA) * 0.7, pos + halfSize * 0.5 + halfSize * 0.6),
                    lineColor, thickness, LineStyle.Solid);
            }
        }
    }
}
