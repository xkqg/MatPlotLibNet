// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders a complete <see cref="Figure"/> including subplots, axes, grids, and series onto an <see cref="IRenderContext"/>.</summary>
public sealed class ChartRenderer : IChartRenderer
{
    private const double TitleHeight = 30;

    /// <inheritdoc />
    public void Render(Figure figure, IRenderContext ctx)
    {
        double plotAreaTop = RenderBackground(figure, ctx);

        if (figure.SubPlots.Count == 0) return;

        var plotAreas = ComputeSubPlotLayout(figure, plotAreaTop);

        for (int i = 0; i < figure.SubPlots.Count; i++)
            RenderAxes(figure.SubPlots[i], plotAreas[i], ctx, figure.Theme);
    }

    /// <summary>Renders the figure background and title, returning the top Y coordinate for subplots.</summary>
    internal double RenderBackground(Figure figure, IRenderContext ctx)
    {
        var theme = figure.Theme;
        var bgColor = figure.BackgroundColor ?? theme.Background;

        ctx.DrawRectangle(new Rect(0, 0, figure.Width, figure.Height), bgColor, null, 0);

        double plotAreaTop = figure.Spacing.MarginTop;
        if (figure.Title is not null)
        {
            ctx.DrawText(figure.Title, new Point(figure.Width / 2, figure.Spacing.MarginTop / 2 + 5),
                TitleFont(theme), TextAlignment.Center);
            plotAreaTop += TitleHeight;
        }

        return plotAreaTop;
    }

    /// <summary>Computes subplot layout positions.</summary>
    internal List<Rect> ComputeSubPlotLayout(Figure figure, double plotAreaTop)
    {
        var sp = figure.Spacing;
        double totalWidth = figure.Width - sp.MarginLeft - sp.MarginRight;
        double totalHeight = figure.Height - plotAreaTop - sp.MarginBottom;

        // Determine grid dimensions from subplot metadata
        int maxRows = 1, maxCols = 1;
        foreach (var ax in figure.SubPlots)
        {
            if (ax.GridRows > 0) maxRows = Math.Max(maxRows, ax.GridRows);
            if (ax.GridCols > 0) maxCols = Math.Max(maxCols, ax.GridCols);
        }

        // If no grid metadata, lay out in a single row
        if (figure.SubPlots.All(a => a.GridRows == 0))
        {
            maxCols = figure.SubPlots.Count;
            maxRows = 1;
        }

        double cellWidth = (totalWidth - sp.HorizontalGap * (maxCols - 1)) / maxCols;
        double cellHeight = (totalHeight - sp.VerticalGap * (maxRows - 1)) / maxRows;

        var areas = new List<Rect>();
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var ax = figure.SubPlots[i];
            int row, col;

            if (ax.GridIndex > 0)
            {
                // 1-based index → row/col
                int idx = ax.GridIndex - 1;
                row = idx / maxCols;
                col = idx % maxCols;
            }
            else
            {
                row = i / maxCols;
                col = i % maxCols;
            }

            double x = sp.MarginLeft + col * (cellWidth + sp.HorizontalGap);
            double y = plotAreaTop + row * (cellHeight + sp.VerticalGap);
            areas.Add(new Rect(x, y, cellWidth, cellHeight));
        }

        return areas;
    }

    internal void RenderAxes(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
    {
        var axesBg = theme.AxesBackground;
        ctx.DrawRectangle(plotArea, axesBg, null, 0);

        // Compute data ranges
        var range = ComputeDataRanges(axes);
        var transform = new DataTransform(range.XMin, range.XMax, range.YMin, range.YMax, plotArea);

        // Compute tick values once for grid + ticks
        var xTicks = ComputeTickValues(range.XMin, range.XMax);
        var yTicks = ComputeTickValues(range.YMin, range.YMax);

        // Skip standard grid/ticks/frame for radar-only axes
        bool radarOnly = axes.Series.Count > 0 && axes.Series.All(s => s is RadarSeries);
        if (!radarOnly)
        {
            // Grid
            if (axes.Grid.Visible)
                RenderGrid(plotArea, ctx, axes, xTicks, yTicks, range.YMin, transform, theme);

            // Axes frame
            ctx.DrawRectangle(plotArea, null, theme.ForegroundText, 1);

            // Tick marks and labels
            RenderTicks(plotArea, ctx, axes, xTicks, yTicks, range.YMin, transform, theme);
        }

        // Render span regions (behind everything)
        foreach (var span in axes.Spans)
        {
            var spanColor = (span.Color ?? Color.FromHex("#1f77b4")).WithAlpha((byte)(span.Alpha * 255));
            if (span.Orientation == Orientation.Horizontal)
            {
                var topLeft = transform.DataToPixel(range.XMin, Math.Max(span.Min, span.Max));
                var bottomRight = transform.DataToPixel(range.XMax, Math.Min(span.Min, span.Max));
                var rect = new Rect(plotArea.X, topLeft.Y, plotArea.Width, bottomRight.Y - topLeft.Y);
                ctx.DrawRectangle(rect, spanColor, null, 0);
            }
            else
            {
                var left = transform.DataToPixel(Math.Min(span.Min, span.Max), range.YMax);
                var right = transform.DataToPixel(Math.Max(span.Min, span.Max), range.YMin);
                var rect = new Rect(left.X, plotArea.Y, right.X - left.X, plotArea.Height);
                ctx.DrawRectangle(rect, spanColor, null, 0);
            }
        }

        // Render reference lines
        foreach (var refLine in axes.ReferenceLines)
        {
            var lineColor = refLine.Color ?? Color.Gray;
            if (refLine.Orientation == Orientation.Horizontal)
            {
                var pt = transform.DataToPixel(range.XMin, refLine.Value);
                ctx.DrawLine(
                    new Point(plotArea.X, pt.Y),
                    new Point(plotArea.X + plotArea.Width, pt.Y),
                    lineColor, refLine.LineWidth, refLine.LineStyle);
            }
            else
            {
                var pt = transform.DataToPixel(refLine.Value, range.YMin);
                ctx.DrawLine(
                    new Point(pt.X, plotArea.Y),
                    new Point(pt.X, plotArea.Y + plotArea.Height),
                    lineColor, refLine.LineWidth, refLine.LineStyle);
            }
        }

        // Compute stacked bar baselines if needed
        if (axes.BarMode == BarMode.Stacked)
        {
            var barSeriesList = axes.Series.OfType<BarSeries>().ToList();
            if (barSeriesList.Count > 1)
            {
                int catCount = barSeriesList[0].Categories.Length;
                var cumulative = new double[catCount];
                foreach (var bs in barSeriesList)
                {
                    bs.StackBaseline = (double[])cumulative.Clone();
                    for (int c = 0; c < Math.Min(catCount, bs.Values.Length); c++)
                        cumulative[c] += bs.Values[c];
                }
            }
        }

        // Series
        for (int i = 0; i < axes.Series.Count; i++)
        {
            var series = axes.Series[i];
            if (!series.Visible) continue;

            var seriesColor = theme.CycleColors[i % theme.CycleColors.Length];
            var renderer = new SvgSeriesRenderer(transform, ctx, seriesColor, axes.EnableTooltips);
            var area = new RenderArea(plotArea, ctx);
            series.Accept(renderer, area);
        }

        // Secondary Y-axis series
        if (axes.SecondaryYAxis is not null && axes.SecondarySeries.Count > 0)
        {
            var secRange = ComputeSecondaryDataRanges(axes, range.XMin, range.XMax);
            var secTransform = new DataTransform(secRange.XMin, secRange.XMax, secRange.YMin, secRange.YMax, plotArea);
            var secYTicks = ComputeTickValues(secRange.YMin, secRange.YMax);

            for (int i = 0; i < axes.SecondarySeries.Count; i++)
            {
                var series = axes.SecondarySeries[i];
                if (!series.Visible) continue;
                int colorIndex = axes.Series.Count + i;
                var seriesColor = theme.CycleColors[colorIndex % theme.CycleColors.Length];
                var renderer = new SvgSeriesRenderer(secTransform, ctx, seriesColor);
                var area = new RenderArea(plotArea, ctx);
                series.Accept(renderer, area);
            }

            // Right-side Y-axis ticks
            var tickFont = TickFont(theme);
            foreach (var tick in secYTicks)
            {
                var pt = secTransform.DataToPixel(secRange.XMax, tick);
                ctx.DrawLine(new Point(plotArea.X + plotArea.Width, pt.Y),
                    new Point(plotArea.X + plotArea.Width + 5, pt.Y),
                    theme.ForegroundText, 1, LineStyle.Solid);
                ctx.DrawText(FormatTick(tick),
                    new Point(plotArea.X + plotArea.Width + 8, pt.Y + 4),
                    tickFont, TextAlignment.Left);
            }

            if (axes.SecondaryYAxis.Label is not null)
            {
                ctx.DrawText(axes.SecondaryYAxis.Label,
                    new Point(plotArea.X + plotArea.Width + 45, plotArea.Y + plotArea.Height / 2),
                    LabelFont(theme), TextAlignment.Center);
            }
        }

        // Annotations
        foreach (var annotation in axes.Annotations)
        {
            var annotFont = annotation.Font ?? new Font
            {
                Family = theme.DefaultFont.Family,
                Size = 10,
                Color = annotation.Color ?? theme.ForegroundText
            };
            var textPos = transform.DataToPixel(annotation.X, annotation.Y);
            ctx.DrawText(annotation.Text, textPos, annotFont, TextAlignment.Left);

            if (annotation.ArrowTargetX.HasValue && annotation.ArrowTargetY.HasValue)
            {
                var arrowTarget = transform.DataToPixel(annotation.ArrowTargetX.Value, annotation.ArrowTargetY.Value);
                var arrowColor = annotation.ArrowColor ?? annotation.Color ?? theme.ForegroundText;
                ctx.DrawLine(textPos, arrowTarget, arrowColor, 1, LineStyle.Solid);
            }
        }

        // Signal markers (buy/sell triangles)
        foreach (var signal in axes.Signals)
        {
            var pt = transform.DataToPixel(signal.X, signal.Y);
            double s = signal.Size;
            var signalColor = signal.Color ?? (signal.Direction == SignalDirection.Buy ? Color.Green : Color.Red);

            Point[] triangle = signal.Direction == SignalDirection.Buy
                ? [new(pt.X, pt.Y + s), new(pt.X - s / 2, pt.Y + s * 2), new(pt.X + s / 2, pt.Y + s * 2)]
                : [new(pt.X, pt.Y - s), new(pt.X - s / 2, pt.Y - s * 2), new(pt.X + s / 2, pt.Y - s * 2)];
            ctx.DrawPolygon(triangle, signalColor, null, 0);
        }

        // Legend
        RenderLegend(axes, plotArea, ctx, theme);

        // Axes title
        if (axes.Title is not null)
        {
            ctx.DrawText(axes.Title, new Point(plotArea.X + plotArea.Width / 2, plotArea.Y - 8),
                TitleFont(theme, sizeOffset: 2), TextAlignment.Center);
        }

        // Axis labels
        var labelFont = LabelFont(theme);

        if (axes.XAxis.Label is not null)
        {
            ctx.DrawText(axes.XAxis.Label,
                new Point(plotArea.X + plotArea.Width / 2, plotArea.Y + plotArea.Height + 35),
                labelFont, TextAlignment.Center);
        }

        if (axes.YAxis.Label is not null)
        {
            // Approximate rotated label by placing it to the left
            ctx.DrawText(axes.YAxis.Label,
                new Point(plotArea.X - 45, plotArea.Y + plotArea.Height / 2),
                labelFont, TextAlignment.Center);
        }
    }

    /// <summary>Renders the legend box showing labeled series with color swatches.</summary>
    private static void RenderLegend(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
    {
        if (!axes.Legend.Visible) return;

        // Collect labeled series with their colors
        var entries = new List<(string Label, Color Color)>();
        for (int i = 0; i < axes.Series.Count; i++)
        {
            var series = axes.Series[i];
            if (string.IsNullOrEmpty(series.Label)) continue;
            var color = theme.CycleColors[i % theme.CycleColors.Length];
            entries.Add((series.Label, color));
        }

        if (entries.Count == 0) return;

        var font = TickFont(theme);
        double swatchSize = 12;
        double swatchGap = 6;
        double padding = 8;
        double lineHeight = swatchSize + 4;

        // Measure legend box dimensions
        double maxTextWidth = 0;
        foreach (var (label, _) in entries)
        {
            var size = ctx.MeasureText(label, font);
            if (size.Width > maxTextWidth) maxTextWidth = size.Width;
        }

        double boxWidth = padding + swatchSize + swatchGap + maxTextWidth + padding;
        double boxHeight = padding + entries.Count * lineHeight - 4 + padding;

        // Position legend based on LegendPosition
        double boxX, boxY;
        double inset = 10;

        switch (axes.Legend.Position)
        {
            case LegendPosition.UpperLeft:
                boxX = plotArea.X + inset;
                boxY = plotArea.Y + inset;
                break;
            case LegendPosition.LowerRight:
                boxX = plotArea.X + plotArea.Width - boxWidth - inset;
                boxY = plotArea.Y + plotArea.Height - boxHeight - inset;
                break;
            case LegendPosition.LowerLeft:
                boxX = plotArea.X + inset;
                boxY = plotArea.Y + plotArea.Height - boxHeight - inset;
                break;
            default: // Best, UpperRight
                boxX = plotArea.X + plotArea.Width - boxWidth - inset;
                boxY = plotArea.Y + inset;
                break;
        }

        // Draw legend background with border
        var bgColor = theme.Background.WithAlpha(220);
        ctx.DrawRectangle(new Rect(boxX, boxY, boxWidth, boxHeight), bgColor, theme.ForegroundText, 0.5);

        // Mark as legend group via CSS class using a clip region trick — draw class attribute
        // SVG render context handles this by wrapping in <g class="legend">
        if (ctx is SvgRenderContext svgCtx)
            svgCtx.BeginGroup("legend");

        // Draw entries
        for (int i = 0; i < entries.Count; i++)
        {
            var (label, color) = entries[i];
            double entryY = boxY + padding + i * lineHeight;

            // Color swatch
            ctx.DrawRectangle(
                new Rect(boxX + padding, entryY, swatchSize, swatchSize),
                color, null, 0);

            // Label text
            ctx.DrawText(label,
                new Point(boxX + padding + swatchSize + swatchGap, entryY + swatchSize - 1),
                font, TextAlignment.Left);
        }

        if (ctx is SvgRenderContext svgCtx2)
            svgCtx2.EndGroup();
    }

    /// <summary>Renders major grid lines behind the plot area at each tick position.</summary>
    /// <remarks>Vertical lines are drawn at each X tick and horizontal lines at each Y tick,
    /// using the grid style defined on <paramref name="axes"/>.</remarks>
    private static void RenderGrid(Rect plotArea, IRenderContext ctx, Axes axes,
        double[] xTicks, double[] yTicks, double yMin,
        DataTransform transform, Theme theme)
    {
        var gridColor = axes.Grid.Color;
        var gridStyle = axes.Grid.LineStyle;
        double gridWidth = axes.Grid.LineWidth;

        foreach (var tick in xTicks)
        {
            var pt = transform.DataToPixel(tick, yMin);
            ctx.DrawLine(
                new Point(pt.X, plotArea.Y),
                new Point(pt.X, plotArea.Y + plotArea.Height),
                gridColor, gridWidth, gridStyle);
        }

        foreach (var tick in yTicks)
        {
            var pt = transform.DataToPixel(axes.XAxis.Min ?? 0, tick);
            ctx.DrawLine(
                new Point(plotArea.X, pt.Y),
                new Point(plotArea.X + plotArea.Width, pt.Y),
                gridColor, gridWidth, gridStyle);
        }
    }

    /// <summary>Renders tick marks and tick labels along the X and Y axes.</summary>
    /// <remarks>For categorical series (bar charts, candlestick charts) the X-axis shows category labels
    /// instead of numeric tick values. Y-axis always shows numeric ticks with small tick marks.</remarks>
    private static void RenderTicks(Rect plotArea, IRenderContext ctx, Axes axes,
        double[] xTicks, double[] yTicks, double yMin,
        DataTransform transform, Theme theme)
    {
        var tickFont = TickFont(theme);

        var barSeries = axes.Series.OfType<BarSeries>().FirstOrDefault();
        var candleSeries = axes.Series.OfType<CandlestickSeries>().FirstOrDefault();
        if (barSeries is not null)
            RenderCategoryLabels(barSeries.Categories, plotArea, ctx, tickFont, yMin, transform);
        else if (candleSeries?.DateLabels is not null)
            RenderCategoryLabels(candleSeries.DateLabels, plotArea, ctx, tickFont, yMin, transform);
        else
        {
            foreach (var tick in xTicks)
            {
                var pt = transform.DataToPixel(tick, yMin);
                ctx.DrawLine(pt, new Point(pt.X, pt.Y + 5), theme.ForegroundText, 1, LineStyle.Solid);
                ctx.DrawText(FormatTick(tick),
                    new Point(pt.X, plotArea.Y + plotArea.Height + 15),
                    tickFont, TextAlignment.Center);
            }
        }

        foreach (var tick in yTicks)
        {
            var pt = transform.DataToPixel(axes.XAxis.Min ?? 0, tick);
            ctx.DrawLine(new Point(pt.X - 5, pt.Y), pt, theme.ForegroundText, 1, LineStyle.Solid);
            ctx.DrawText(FormatTick(tick),
                new Point(plotArea.X - 8, pt.Y + 4),
                tickFont, TextAlignment.Right);
        }
    }

    /// <summary>Renders category labels (bar names or date labels) centered below each category position on the X-axis.</summary>
    private static void RenderCategoryLabels(string[] labels, Rect plotArea,
        IRenderContext ctx, Font tickFont, double yMin, DataTransform transform)
    {
        for (int i = 0; i < labels.Length; i++)
        {
            var pt = transform.DataToPixel(i, yMin);
            ctx.DrawText(labels[i],
                new Point(pt.X, plotArea.Y + plotArea.Height + 15),
                tickFont, TextAlignment.Center);
        }
    }

    /// <summary>Computes the combined X and Y data ranges across all series on the axes.</summary>
    /// <remarks>Each series type contributes differently: XY-based series expand the range from their data arrays,
    /// categorical series (bar, box, violin) use index-based X ranges, and stacked bars sum values per category.
    /// A 5% padding is applied to each axis unless the user has set explicit limits via <c>SetXLim</c>/<c>SetYLim</c>.</remarks>
    private static DataRange ComputeDataRanges(Axes axes)
    {
        double xMin = axes.XAxis.Min ?? double.MaxValue;
        double xMax = axes.XAxis.Max ?? double.MinValue;
        double yMin = axes.YAxis.Min ?? double.MaxValue;
        double yMax = axes.YAxis.Max ?? double.MinValue;

        foreach (var series in axes.Series)
        {
            switch (series)
            {
                case LineSeries ls:
                    UpdateRange(ls.XData, ref xMin, ref xMax);
                    UpdateRange(ls.YData, ref yMin, ref yMax);
                    break;
                case ScatterSeries ss:
                    UpdateRange(ss.XData, ref xMin, ref xMax);
                    UpdateRange(ss.YData, ref yMin, ref yMax);
                    break;
                case StemSeries st:
                    UpdateRange(st.XData, ref xMin, ref xMax);
                    UpdateRange(st.YData, ref yMin, ref yMax);
                    if (0 < yMin) yMin = 0;
                    if (0 > yMax) yMax = 0;
                    break;
                case BarSeries bs:
                    xMin = axes.XAxis.Min ?? -0.5;
                    xMax = axes.XAxis.Max ?? (bs.Categories.Length - 0.5);
                    if (axes.BarMode == BarMode.Stacked)
                    {
                        // For stacked, sum all bar series values per category
                        var allBars = axes.Series.OfType<BarSeries>().ToList();
                        if (allBars.Count > 0)
                        {
                            int catCount = allBars[0].Categories.Length;
                            for (int c = 0; c < catCount; c++)
                            {
                                double sum = allBars.Sum(b => c < b.Values.Length ? b.Values[c] : 0);
                                if (sum > yMax) yMax = sum;
                            }
                        }
                    }
                    else
                    {
                        UpdateRange(bs.Values, ref yMin, ref yMax);
                    }
                    if (0 < yMin) yMin = 0;
                    break;
                case HistogramSeries hs:
                    UpdateRange(hs.Data, ref xMin, ref xMax);
                    yMin = 0;
                    if (hs.Data.Length > 0)
                    {
                        var bins = hs.ComputeBins();
                        if (bins.Counts.Length > 0)
                        {
                            int maxCount = bins.Counts.Max();
                            if (maxCount > yMax) yMax = maxCount;
                        }
                    }
                    break;
                case BoxSeries bx:
                    xMin = axes.XAxis.Min ?? -0.5;
                    xMax = axes.XAxis.Max ?? (bx.Datasets.Length - 0.5);
                    foreach (var ds in bx.Datasets)
                    {
                        UpdateRange(ds, ref yMin, ref yMax);
                    }
                    break;
                case ViolinSeries vs:
                    xMin = axes.XAxis.Min ?? -1;
                    xMax = axes.XAxis.Max ?? vs.Datasets.Length;
                    foreach (var ds in vs.Datasets)
                    {
                        UpdateRange(ds, ref yMin, ref yMax);
                    }
                    break;
                case RadarSeries:
                    // Radar renders in its own coordinate system; set dummy range
                    if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
                    if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
                    break;
                case QuiverSeries qs:
                    for (int i = 0; i < qs.XData.Length; i++)
                    {
                        double x0 = qs.XData[i], x1 = x0 + qs.UData[i] * qs.Scale;
                        double y0 = qs.YData[i], y1 = y0 + qs.VData[i] * qs.Scale;
                        if (Math.Min(x0, x1) < xMin) xMin = Math.Min(x0, x1);
                        if (Math.Max(x0, x1) > xMax) xMax = Math.Max(x0, x1);
                        if (Math.Min(y0, y1) < yMin) yMin = Math.Min(y0, y1);
                        if (Math.Max(y0, y1) > yMax) yMax = Math.Max(y0, y1);
                    }
                    break;
                case CandlestickSeries cs:
                    xMin = axes.XAxis.Min ?? -0.5;
                    xMax = axes.XAxis.Max ?? (cs.Open.Length - 0.5);
                    UpdateRange(cs.Low, ref yMin, ref yMax);
                    UpdateRange(cs.High, ref yMin, ref yMax);
                    break;
                case ErrorBarSeries eb:
                    UpdateRange(eb.XData, ref xMin, ref xMax);
                    for (int i = 0; i < eb.YData.Length; i++)
                    {
                        double lo = eb.YData[i] - eb.YErrorLow[i];
                        double hi = eb.YData[i] + eb.YErrorHigh[i];
                        if (lo < yMin) yMin = lo;
                        if (hi > yMax) yMax = hi;
                    }
                    if (eb.XErrorLow is not null && eb.XErrorHigh is not null)
                    {
                        for (int i = 0; i < eb.XData.Length; i++)
                        {
                            double lo = eb.XData[i] - eb.XErrorLow[i];
                            double hi = eb.XData[i] + eb.XErrorHigh[i];
                            if (lo < xMin) xMin = lo;
                            if (hi > xMax) xMax = hi;
                        }
                    }
                    break;
                case StepSeries ss:
                    UpdateRange(ss.XData, ref xMin, ref xMax);
                    UpdateRange(ss.YData, ref yMin, ref yMax);
                    break;
                case AreaSeries ar:
                    UpdateRange(ar.XData, ref xMin, ref xMax);
                    UpdateRange(ar.YData, ref yMin, ref yMax);
                    if (ar.YData2 is not null)
                        UpdateRange(ar.YData2, ref yMin, ref yMax);
                    else if (0 < yMin) yMin = 0;
                    break;
                case BubbleSeries bu:
                    UpdateRange(bu.XData, ref xMin, ref xMax);
                    UpdateRange(bu.YData, ref yMin, ref yMax);
                    break;
                case OhlcBarSeries ob:
                    xMin = axes.XAxis.Min ?? -0.5;
                    xMax = axes.XAxis.Max ?? (ob.Open.Length - 0.5);
                    UpdateRange(ob.Low, ref yMin, ref yMax);
                    UpdateRange(ob.High, ref yMin, ref yMax);
                    break;
                case WaterfallSeries wf:
                    xMin = axes.XAxis.Min ?? -0.5;
                    xMax = axes.XAxis.Max ?? (wf.Categories.Length - 0.5);
                    double wfCum = 0;
                    foreach (var v in wf.Values) { wfCum += v; if (wfCum < yMin) yMin = wfCum; if (wfCum > yMax) yMax = wfCum; }
                    if (0 < yMin) yMin = 0;
                    break;
                case GanttSeries gt:
                    UpdateRange(gt.Starts, ref xMin, ref xMax);
                    UpdateRange(gt.Ends, ref xMin, ref xMax);
                    yMin = axes.YAxis.Min ?? -0.5;
                    yMax = axes.YAxis.Max ?? (gt.Tasks.Length - 0.5);
                    break;
                case DonutSeries:
                case FunnelSeries:
                case GaugeSeries:
                case ProgressBarSeries:
                    // Render in own coordinate system within PlotBounds
                    if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
                    if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
                    break;
                case SparklineSeries sp:
                    xMin = 0; xMax = sp.Values.Length - 1;
                    UpdateRange(sp.Values, ref yMin, ref yMax);
                    break;
            }
        }

        // Add padding
        if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
        if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
        if (Math.Abs(xMax - xMin) < 1e-10) { xMin -= 0.5; xMax += 0.5; }
        if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 0.5; yMax += 0.5; }

        double xPadding = (xMax - xMin) * 0.05;
        double yPadding = (yMax - yMin) * 0.05;

        if (!axes.XAxis.Min.HasValue) xMin -= xPadding;
        if (!axes.XAxis.Max.HasValue) xMax += xPadding;
        if (!axes.YAxis.Min.HasValue) yMin -= yPadding;
        if (!axes.YAxis.Max.HasValue) yMax += yPadding;

        return new DataRange(xMin, xMax, yMin, yMax);
    }

    /// <summary>Computes the Y data range for series plotted against the secondary Y-axis, reusing the primary X range.</summary>
    private static DataRange ComputeSecondaryDataRanges(Axes axes, double xMin, double xMax)
    {
        double yMin = axes.SecondaryYAxis?.Min ?? double.MaxValue;
        double yMax = axes.SecondaryYAxis?.Max ?? double.MinValue;

        foreach (var series in axes.SecondarySeries)
        {
            switch (series)
            {
                case LineSeries ls:
                    UpdateRange(ls.YData, ref yMin, ref yMax);
                    break;
                case ScatterSeries ss:
                    UpdateRange(ss.YData, ref yMin, ref yMax);
                    break;
            }
        }

        if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
        if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 0.5; yMax += 0.5; }

        double yPadding = (yMax - yMin) * 0.05;
        if (!axes.SecondaryYAxis!.Min.HasValue) yMin -= yPadding;
        if (!axes.SecondaryYAxis!.Max.HasValue) yMax += yPadding;

        return new DataRange(xMin, xMax, yMin, yMax);
    }

    /// <summary>Expands <paramref name="min"/> and <paramref name="max"/> to include all values in <paramref name="data"/>.</summary>
    private static void UpdateRange(double[] data, ref double min, ref double max)
    {
        foreach (var v in data)
        {
            if (v < min) min = v;
            if (v > max) max = v;
        }
    }

    /// <summary>Computes aesthetically-spaced tick values between <paramref name="min"/> and <paramref name="max"/>.</summary>
    /// <remarks>Uses a "nice numbers" algorithm that rounds the raw step size to the nearest 1-2-5 sequence
    /// at the appropriate order of magnitude, producing approximately <paramref name="targetCount"/> ticks.</remarks>
    private static double[] ComputeTickValues(double min, double max, int targetCount = 5)
    {
        double range = max - min;
        if (range <= 0) return [min];

        double rawStep = range / targetCount;
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
        double normalized = rawStep / magnitude;

        double step = normalized switch
        {
            < 1.5 => magnitude,
            < 3.5 => 2 * magnitude,
            < 7.5 => 5 * magnitude,
            _ => 10 * magnitude
        };

        double first = Math.Ceiling(min / step) * step;
        var ticks = new List<double>();
        for (double t = first; t <= max + step * 0.01; t += step)
            ticks.Add(Math.Round(t, 10));

        return ticks.ToArray();
    }

    /// <summary>Formats a tick value for display, using scientific notation for very large or very small numbers.</summary>
    private static string FormatTick(double value)
    {
        if (Math.Abs(value) < 1e-10) return "0";
        if (Math.Abs(value) >= 1e6 || (Math.Abs(value) < 0.01 && value != 0))
            return value.ToString("G3", System.Globalization.CultureInfo.InvariantCulture);
        return value.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Creates a bold title font derived from the theme, offset from the default size by <paramref name="sizeOffset"/> points.</summary>
    /// <remarks>Used for both the figure title (offset 4) and axes titles (offset 2).</remarks>
    private static Font TitleFont(Theme theme, int sizeOffset = 4) => new()
    {
        Family = theme.DefaultFont.Family,
        Size = theme.DefaultFont.Size + sizeOffset,
        Weight = FontWeight.Bold,
        Color = theme.ForegroundText
    };

    /// <summary>Creates a smaller font for tick labels, 2 points below the theme default.</summary>
    private static Font TickFont(Theme theme) => new()
    {
        Family = theme.DefaultFont.Family,
        Size = theme.DefaultFont.Size - 2,
        Color = theme.ForegroundText
    };

    /// <summary>Creates a font at the theme default size for axis labels and secondary axis labels.</summary>
    private static Font LabelFont(Theme theme) => new()
    {
        Family = theme.DefaultFont.Family,
        Size = theme.DefaultFont.Size,
        Color = theme.ForegroundText
    };
}
