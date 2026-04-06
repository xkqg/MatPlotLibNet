// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickFormatters;
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

        // Compute tick values once for grid + ticks
        var xTicks = ComputeTickValues(range.XMin, range.XMax);
        var yTicks = ComputeTickValues(range.YMin, range.YMax);

        // Skip standard grid/ticks/frame for radar-only axes
        bool radarOnly = Axes.Series.Count > 0 && Axes.Series.All(s => s is RadarSeries);
        if (!radarOnly)
        {
            // Grid
            if (Axes.Grid.Visible)
                RenderGrid(xTicks, yTicks, range.YMin, transform);

            // Axes frame
            Ctx.DrawRectangle(PlotArea, null, Theme.ForegroundText, 1);

            // Tick marks and labels
            RenderTicks(xTicks, yTicks, range.YMin, transform);
        }

        // Render span regions (behind everything)
        foreach (var span in Axes.Spans)
        {
            var spanColor = (span.Color ?? Color.FromHex("#1f77b4")).WithAlpha((byte)(span.Alpha * 255));
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
            var lineColor = refLine.Color ?? Color.Gray;
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

        // Compute stacked bar baselines if needed
        if (Axes.BarMode == BarMode.Stacked)
        {
            var barSeriesList = Axes.Series.OfType<BarSeries>().ToList();
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
                var renderer = new SvgSeriesRenderer(secTransform, Ctx, seriesColor);
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
                Ctx.DrawText(FormatTick(tick),
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
            Ctx.DrawText(annotation.Text, textPos, annotFont, TextAlignment.Left);

            if (annotation.ArrowTargetX.HasValue && annotation.ArrowTargetY.HasValue)
            {
                var arrowTarget = transform.DataToPixel(annotation.ArrowTargetX.Value, annotation.ArrowTargetY.Value);
                var arrowColor = annotation.ArrowColor ?? annotation.Color ?? Theme.ForegroundText;
                Ctx.DrawLine(textPos, arrowTarget, arrowColor, 1, LineStyle.Solid);
            }
        }

        // Signal markers (buy/sell triangles)
        foreach (var signal in Axes.Signals)
        {
            var pt = transform.DataToPixel(signal.X, signal.Y);
            double s = signal.Size;
            var signalColor = signal.Color ?? (signal.Direction == SignalDirection.Buy ? Color.Green : Color.Red);

            Point[] triangle = signal.Direction == SignalDirection.Buy
                ? [new(pt.X, pt.Y + s), new(pt.X - s / 2, pt.Y + s * 2), new(pt.X + s / 2, pt.Y + s * 2)]
                : [new(pt.X, pt.Y - s), new(pt.X - s / 2, pt.Y - s * 2), new(pt.X + s / 2, pt.Y - s * 2)];
            Ctx.DrawPolygon(triangle, signalColor, null, 0);
        }

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
    private void RenderGrid(double[] xTicks, double[] yTicks, double yMin, DataTransform transform)
    {
        var gridColor = Axes.Grid.Color;
        var gridStyle = Axes.Grid.LineStyle;
        double gridWidth = Axes.Grid.LineWidth;

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

    /// <summary>Renders tick marks and tick labels along the X and Y axes.</summary>
    private void RenderTicks(double[] xTicks, double[] yTicks, double yMin, DataTransform transform)
    {
        var tickFont = TickFont();

        var barSeries = Axes.Series.OfType<BarSeries>().FirstOrDefault();
        var candleSeries = Axes.Series.OfType<CandlestickSeries>().FirstOrDefault();
        if (barSeries is not null)
            RenderCategoryLabels(barSeries.Categories, yMin, transform);
        else if (candleSeries?.DateLabels is not null)
            RenderCategoryLabels(candleSeries.DateLabels, yMin, transform);
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
    }

    /// <summary>Renders category labels centered below each category position on the X-axis.</summary>
    private void RenderCategoryLabels(string[] labels, double yMin, DataTransform transform)
    {
        var tickFont = TickFont();
        for (int i = 0; i < labels.Length; i++)
        {
            var pt = transform.DataToPixel(i, yMin);
            Ctx.DrawText(labels[i],
                new Point(pt.X, PlotArea.Y + PlotArea.Height + 15),
                tickFont, TextAlignment.Center);
        }
    }

    /// <summary>Computes the combined X and Y data ranges across all series on the axes.</summary>
    private DataRange ComputeDataRanges()
    {
        double xMin = Axes.XAxis.Min ?? double.MaxValue;
        double xMax = Axes.XAxis.Max ?? double.MinValue;
        double yMin = Axes.YAxis.Min ?? double.MaxValue;
        double yMax = Axes.YAxis.Max ?? double.MinValue;

        foreach (var series in Axes.Series)
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
                    xMin = Axes.XAxis.Min ?? -0.5;
                    xMax = Axes.XAxis.Max ?? (bs.Categories.Length - 0.5);
                    if (Axes.BarMode == BarMode.Stacked)
                    {
                        // For stacked, sum all bar series values per category
                        var allBars = Axes.Series.OfType<BarSeries>().ToList();
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
                    xMin = Axes.XAxis.Min ?? -0.5;
                    xMax = Axes.XAxis.Max ?? (bx.Datasets.Length - 0.5);
                    foreach (var ds in bx.Datasets)
                    {
                        UpdateRange(ds, ref yMin, ref yMax);
                    }
                    break;
                case ViolinSeries vs:
                    xMin = Axes.XAxis.Min ?? -1;
                    xMax = Axes.XAxis.Max ?? vs.Datasets.Length;
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
                    xMin = Axes.XAxis.Min ?? -0.5;
                    xMax = Axes.XAxis.Max ?? (cs.Open.Length - 0.5);
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
                    xMin = Axes.XAxis.Min ?? -0.5;
                    xMax = Axes.XAxis.Max ?? (ob.Open.Length - 0.5);
                    UpdateRange(ob.Low, ref yMin, ref yMax);
                    UpdateRange(ob.High, ref yMin, ref yMax);
                    break;
                case WaterfallSeries wf:
                    xMin = Axes.XAxis.Min ?? -0.5;
                    xMax = Axes.XAxis.Max ?? (wf.Categories.Length - 0.5);
                    double wfCum = 0;
                    foreach (var v in wf.Values) { wfCum += v; if (wfCum < yMin) yMin = wfCum; if (wfCum > yMax) yMax = wfCum; }
                    if (0 < yMin) yMin = 0;
                    break;
                case GanttSeries gt:
                    UpdateRange(gt.Starts, ref xMin, ref xMax);
                    UpdateRange(gt.Ends, ref xMin, ref xMax);
                    yMin = Axes.YAxis.Min ?? -0.5;
                    yMax = Axes.YAxis.Max ?? (gt.Tasks.Length - 0.5);
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
                case TreemapSeries:
                case SunburstSeries:
                    // Render in own coordinate system within PlotBounds
                    if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
                    if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
                    break;
                case SankeySeries:
                    // Render in own coordinate system within PlotBounds
                    if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
                    if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
                    break;
                case PolarLineSeries:
                case PolarScatterSeries:
                case PolarBarSeries:
                    // Polar renders in own coordinate system
                    if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
                    if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
                    break;
                case SurfaceSeries:
                case WireframeSeries:
                case Scatter3DSeries:
                    // 3D renders in own coordinate system
                    if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
                    if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
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

        if (!Axes.XAxis.Min.HasValue) xMin -= xPadding;
        if (!Axes.XAxis.Max.HasValue) xMax += xPadding;
        if (!Axes.YAxis.Min.HasValue) yMin -= yPadding;
        if (!Axes.YAxis.Max.HasValue) yMax += yPadding;

        return new DataRange(xMin, xMax, yMin, yMax);
    }

    /// <summary>Computes the Y data range for series plotted against the secondary Y-axis.</summary>
    private DataRange ComputeSecondaryDataRanges(double xMin, double xMax)
    {
        double yMin = Axes.SecondaryYAxis?.Min ?? double.MaxValue;
        double yMax = Axes.SecondaryYAxis?.Max ?? double.MinValue;

        foreach (var series in Axes.SecondarySeries)
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
        if (!Axes.SecondaryYAxis!.Min.HasValue) yMin -= yPadding;
        if (!Axes.SecondaryYAxis!.Max.HasValue) yMax += yPadding;

        return new DataRange(xMin, xMax, yMin, yMax);
    }
}
