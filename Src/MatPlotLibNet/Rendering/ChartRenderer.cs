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
    private const double MarginLeft = 60;
    private const double MarginRight = 20;
    private const double MarginTop = 40;
    private const double MarginBottom = 50;
    private const double TitleHeight = 30;
    private const double SubPlotGap = 40;

    /// <summary>Renders the entire figure including background, title, and all subplots.</summary>
    /// <param name="figure">The figure model to render.</param>
    /// <param name="ctx">The render context to draw onto.</param>
    public void Render(Figure figure, IRenderContext ctx)
    {
        var theme = figure.Theme;
        var bgColor = figure.BackgroundColor ?? theme.Background;

        // Background
        ctx.DrawRectangle(new Rect(0, 0, figure.Width, figure.Height), bgColor, null, 0);

        // Figure title
        double plotAreaTop = MarginTop;
        if (figure.Title is not null)
        {
            var titleFont = new Font
            {
                Family = theme.DefaultFont.Family,
                Size = theme.DefaultFont.Size + 4,
                Weight = FontWeight.Bold,
                Color = theme.ForegroundText
            };
            ctx.DrawText(figure.Title, new Point(figure.Width / 2, MarginTop / 2 + 5),
                titleFont, TextAlignment.Center);
            plotAreaTop += TitleHeight;
        }

        if (figure.SubPlots.Count == 0) return;

        // Compute subplot layout
        var plotAreas = ComputeSubPlotLayout(figure, plotAreaTop);

        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var axes = figure.SubPlots[i];
            var plotArea = plotAreas[i];
            RenderAxes(axes, plotArea, ctx, theme);
        }
    }

    /// <summary>Renders the figure background and title, returning the top Y coordinate for subplots.</summary>
    internal double RenderBackground(Figure figure, IRenderContext ctx)
    {
        var theme = figure.Theme;
        var bgColor = figure.BackgroundColor ?? theme.Background;

        ctx.DrawRectangle(new Rect(0, 0, figure.Width, figure.Height), bgColor, null, 0);

        double plotAreaTop = MarginTop;
        if (figure.Title is not null)
        {
            var titleFont = new Font
            {
                Family = theme.DefaultFont.Family,
                Size = theme.DefaultFont.Size + 4,
                Weight = FontWeight.Bold,
                Color = theme.ForegroundText
            };
            ctx.DrawText(figure.Title, new Point(figure.Width / 2, MarginTop / 2 + 5),
                titleFont, TextAlignment.Center);
            plotAreaTop += TitleHeight;
        }

        return plotAreaTop;
    }

    /// <summary>Computes subplot layout positions.</summary>
    internal List<Rect> ComputeSubPlotLayout(Figure figure, double plotAreaTop)
    {
        double totalWidth = figure.Width - MarginLeft - MarginRight;
        double totalHeight = figure.Height - plotAreaTop - MarginBottom;

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

        double cellWidth = (totalWidth - SubPlotGap * (maxCols - 1)) / maxCols;
        double cellHeight = (totalHeight - SubPlotGap * (maxRows - 1)) / maxRows;

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

            double x = MarginLeft + col * (cellWidth + SubPlotGap);
            double y = plotAreaTop + row * (cellHeight + SubPlotGap);
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

        // Grid
        if (axes.Grid.Visible)
            RenderGrid(plotArea, ctx, axes, xTicks, yTicks, range.YMin, transform, theme);

        // Axes frame
        ctx.DrawRectangle(plotArea, null, theme.ForegroundText, 1);

        // Tick marks and labels
        RenderTicks(plotArea, ctx, axes, xTicks, yTicks, range.YMin, transform, theme);

        // Series
        for (int i = 0; i < axes.Series.Count; i++)
        {
            var series = axes.Series[i];
            if (!series.Visible) continue;

            var seriesColor = theme.CycleColors[i % theme.CycleColors.Length];
            var renderer = new SvgSeriesRenderer(transform, ctx, seriesColor);
            var area = new RenderArea(plotArea, ctx);
            series.Accept(renderer, area);
        }

        // Axes title
        if (axes.Title is not null)
        {
            var titleFont = new Font
            {
                Family = theme.DefaultFont.Family,
                Size = theme.DefaultFont.Size + 2,
                Weight = FontWeight.Bold,
                Color = theme.ForegroundText
            };
            ctx.DrawText(axes.Title, new Point(plotArea.X + plotArea.Width / 2, plotArea.Y - 8),
                titleFont, TextAlignment.Center);
        }

        // Axis labels
        var labelFont = new Font
        {
            Family = theme.DefaultFont.Family,
            Size = theme.DefaultFont.Size,
            Color = theme.ForegroundText
        };

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

    private static void RenderTicks(Rect plotArea, IRenderContext ctx, Axes axes,
        double[] xTicks, double[] yTicks, double yMin,
        DataTransform transform, Theme theme)
    {
        var tickFont = new Font
        {
            Family = theme.DefaultFont.Family,
            Size = theme.DefaultFont.Size - 2,
            Color = theme.ForegroundText
        };

        var barSeries = axes.Series.OfType<BarSeries>().FirstOrDefault();
        if (barSeries is not null)
        {
            for (int i = 0; i < barSeries.Categories.Length; i++)
            {
                var pt = transform.DataToPixel(i, yMin);
                ctx.DrawText(barSeries.Categories[i],
                    new Point(pt.X, plotArea.Y + plotArea.Height + 15),
                    tickFont, TextAlignment.Center);
            }
        }
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
                    UpdateRange(bs.Values, ref yMin, ref yMax);
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

    private static void UpdateRange(double[] data, ref double min, ref double max)
    {
        foreach (var v in data)
        {
            if (v < min) min = v;
            if (v > max) max = v;
        }
    }

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

    private static string FormatTick(double value)
    {
        if (Math.Abs(value) < 1e-10) return "0";
        if (Math.Abs(value) >= 1e6 || (Math.Abs(value) < 0.01 && value != 0))
            return value.ToString("G3", System.Globalization.CultureInfo.InvariantCulture);
        return value.ToString("G5", System.Globalization.CultureInfo.InvariantCulture);
    }
}
