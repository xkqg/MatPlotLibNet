// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering;

/// <summary>Abstract base for coordinate-system-specific axes rendering. Subclass for Cartesian, Polar, or 3D.</summary>
public abstract class AxesRenderer
{
    /// <summary>Gets the axes being rendered.</summary>
    protected Axes Axes { get; }

    /// <summary>Gets the pixel bounds of the plot area.</summary>
    protected Rect PlotArea { get; }

    /// <summary>Gets the render context for drawing.</summary>
    protected IRenderContext Ctx { get; }

    /// <summary>Gets the active theme.</summary>
    protected Theme Theme { get; }

    /// <summary>Initializes the renderer with the rendering context.</summary>
    /// <param name="axes">The axes model to render.</param>
    /// <param name="plotArea">The pixel-space rectangle that bounds the plot.</param>
    /// <param name="ctx">The drawing surface to emit primitives onto.</param>
    /// <param name="theme">The active visual theme.</param>
    protected AxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
    {
        Axes = axes;
        PlotArea = plotArea;
        Ctx = ctx;
        Theme = theme;
    }

    /// <summary>Renders the complete axes: background, grid, series, decorations, legend, colorbar, title, labels.</summary>
    public abstract void Render();

    private static readonly ConcurrentDictionary<CoordinateSystem, Func<Axes, Rect, IRenderContext, Theme, AxesRenderer>>
        RendererFactories = new()
    {
        [CoordinateSystem.Cartesian] = (a, p, c, t) => new CartesianAxesRenderer(a, p, c, t),
        [CoordinateSystem.Polar] = (a, p, c, t) => new PolarAxesRenderer(a, p, c, t),
        [CoordinateSystem.ThreeD] = (a, p, c, t) => new ThreeDAxesRenderer(a, p, c, t),
    };

    /// <summary>Registers a custom renderer factory for a coordinate system.</summary>
    /// <param name="system">The coordinate system whose renderer to replace.</param>
    /// <param name="factory">A factory delegate that constructs the custom renderer given axes, plot area, context, and theme.</param>
    public static void RegisterRenderer(CoordinateSystem system, Func<Axes, Rect, IRenderContext, Theme, AxesRenderer> factory)
        => RendererFactories[system] = factory;

    /// <summary>Creates the appropriate renderer for the axes coordinate system.</summary>
    /// <param name="axes">The axes model to render.</param>
    /// <param name="plotArea">The pixel-space rectangle that bounds the plot.</param>
    /// <param name="ctx">The drawing surface to emit primitives onto.</param>
    /// <param name="theme">The active visual theme.</param>
    /// <returns>An <see cref="AxesRenderer"/> instance for the axes' coordinate system.</returns>
    public static AxesRenderer Create(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme) =>
        RendererFactories.TryGetValue(axes.CoordinateSystem, out var factory)
            ? factory(axes, plotArea, ctx, theme)
            : new CartesianAxesRenderer(axes, plotArea, ctx, theme);

    // --- Shared rendering helpers (available to all subclasses) ---

    /// <summary>Renders all series on these axes through the visitor pattern.</summary>
    protected void RenderSeries()
    {
        var svgCtx = Axes.EnableInteractiveAttributes ? Ctx as SvgRenderContext : null;
        for (int i = 0; i < Axes.Series.Count; i++)
        {
            var series = Axes.Series[i];
            if (!series.Visible) continue;
            var cycledProps  = Theme.PropCycler?[i];
            var seriesColor  = cycledProps?.Color ?? Theme.CycleColors[i % Theme.CycleColors.Length];
            svgCtx?.BeginDataGroup("series", i);
            var renderer = new SvgSeriesRenderer(
                new DataTransform(0, 1, 0, 1, PlotArea), Ctx, seriesColor, cycledProps, Axes.EnableTooltips);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            if (svgCtx is not null) Ctx.EndGroup();
        }
    }

    /// <summary>Renders all series with a specific DataTransform.</summary>
    /// <param name="transform">The coordinate transform mapping data space to pixel space.</param>
    protected void RenderSeries(DataTransform transform)
    {
        var svgCtx = Axes.EnableInteractiveAttributes ? Ctx as SvgRenderContext : null;
        for (int i = 0; i < Axes.Series.Count; i++)
        {
            var series = Axes.Series[i];
            if (!series.Visible) continue;
            var cycledProps  = Theme.PropCycler?[i];
            var seriesColor  = cycledProps?.Color ?? Theme.CycleColors[i % Theme.CycleColors.Length];
            svgCtx?.BeginDataGroup("series", i);
            var renderer = new SvgSeriesRenderer(transform, Ctx, seriesColor, cycledProps, Axes.EnableTooltips);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            if (svgCtx is not null) Ctx.EndGroup();
        }
    }

    /// <summary>Renders the legend if any series have labels.</summary>
    protected void RenderLegend()
    {
        if (!Axes.Legend.Visible) return;

        var entries = new List<(string Label, Color Color)>();
        for (int i = 0; i < Axes.Series.Count; i++)
        {
            var series = Axes.Series[i];
            if (string.IsNullOrEmpty(series.Label)) continue;
            var color = Theme.PropCycler?[i].Color ?? Theme.CycleColors[i % Theme.CycleColors.Length];
            entries.Add((series.Label, color));
        }

        if (entries.Count == 0) return;

        var font = TickFont();
        double swatchSize = 12, swatchGap = 6, padding = 8;
        double lineHeight = swatchSize + 4;

        double maxTextWidth = 0;
        foreach (var (label, _) in entries)
        {
            var size = Ctx.MeasureText(label, font);
            if (size.Width > maxTextWidth) maxTextWidth = size.Width;
        }

        double boxWidth = padding + swatchSize + swatchGap + maxTextWidth + padding;
        double boxHeight = padding + entries.Count * lineHeight - 4 + padding;

        double inset = 10;
        var (boxX, boxY) = Axes.Legend.Position switch
        {
            LegendPosition.UpperLeft => (PlotArea.X + inset, PlotArea.Y + inset),
            LegendPosition.LowerRight => (PlotArea.X + PlotArea.Width - boxWidth - inset, PlotArea.Y + PlotArea.Height - boxHeight - inset),
            LegendPosition.LowerLeft => (PlotArea.X + inset, PlotArea.Y + PlotArea.Height - boxHeight - inset),
            _ => (PlotArea.X + PlotArea.Width - boxWidth - inset, PlotArea.Y + inset)
        };

        var bgColor = Theme.Background.WithAlpha(220);
        Ctx.DrawRectangle(new Rect(boxX, boxY, boxWidth, boxHeight), bgColor, Theme.ForegroundText, 0.5);

        Ctx.BeginGroup("legend");

        var svgCtxLegend = Axes.EnableInteractiveAttributes ? Ctx as SvgRenderContext : null;
        for (int i = 0; i < entries.Count; i++)
        {
            var (label, color) = entries[i];
            double entryY = boxY + padding + i * lineHeight;
            svgCtxLegend?.BeginLegendItemGroup(i);
            Ctx.DrawRectangle(new Rect(boxX + padding, entryY, swatchSize, swatchSize), color, null, 0);
            Ctx.DrawText(label, new Point(boxX + padding + swatchSize + swatchGap, entryY + swatchSize - 1), font, TextAlignment.Left);
            if (svgCtxLegend is not null) Ctx.EndGroup();
        }

        Ctx.EndGroup();
    }

    /// <summary>Renders a color bar gradient alongside the plot area if configured.</summary>
    protected void RenderColorBar()
    {
        if (Axes.ColorBar is not { Visible: true } cb) return;

        IColorMap colorMap = cb.ColorMap ?? ColorMaps.Viridis;
        double min = cb.Min, max = cb.Max;

        var dataProvider = Axes.Series.OfType<IColorBarDataProvider>().FirstOrDefault();
        if (dataProvider is not null)
        {
            colorMap = cb.ColorMap ?? dataProvider.ColorMap ?? ColorMaps.Viridis;
            var (dMin, dMax) = dataProvider.GetColorBarRange();
            if (dMin < dMax) { min = dMin; max = dMax; }
        }

        if (Math.Abs(max - min) < 1e-10) { min = 0; max = 1; }

        double barX = PlotArea.X + PlotArea.Width + cb.Padding;
        double barY = PlotArea.Y;
        double barH = PlotArea.Height;
        int steps = 50;

        Ctx.BeginGroup("colorbar");

        for (int i = 0; i < steps; i++)
        {
            double frac = 1.0 - (double)i / steps;
            var color = colorMap.GetColor(frac);
            Ctx.DrawRectangle(new Rect(barX, barY + barH * i / steps, cb.Width, barH / steps + 1), color, null, 0);
        }

        Ctx.DrawRectangle(new Rect(barX, barY, cb.Width, barH), null, Theme.ForegroundText, 0.5);

        var tickFont = TickFont();
        double labelX = barX + cb.Width + 4;
        for (int i = 0; i <= 5; i++)
        {
            double frac = (double)i / 5;
            double value = max - frac * (max - min);
            Ctx.DrawText(FormatTick(value), new Point(labelX, barY + barH * frac + 4), tickFont, TextAlignment.Left);
        }

        if (cb.Label is not null)
            Ctx.DrawText(cb.Label, new Point(labelX + 30, barY + barH / 2), LabelFont(), TextAlignment.Center);

        Ctx.EndGroup();
    }

    /// <summary>Renders the axes title above the plot area.</summary>
    protected void RenderTitle()
    {
        if (Axes.Title is null) return;
        var point = new Point(PlotArea.X + PlotArea.Width / 2, PlotArea.Y - 8);
        var font  = TitleFont(2);
        if (MathTextParser.ContainsMath(Axes.Title))
            Ctx.DrawRichText(MathTextParser.Parse(Axes.Title), point, font, TextAlignment.Center);
        else
            Ctx.DrawText(Axes.Title, point, font, TextAlignment.Center);
    }

    /// <summary>Renders X and Y axis labels.</summary>
    protected void RenderAxisLabels()
    {
        var font = LabelFont();
        if (Axes.XAxis.Label is not null)
        {
            var point = new Point(PlotArea.X + PlotArea.Width / 2, PlotArea.Y + PlotArea.Height + 35);
            if (MathTextParser.ContainsMath(Axes.XAxis.Label))
                Ctx.DrawRichText(MathTextParser.Parse(Axes.XAxis.Label), point, font, TextAlignment.Center);
            else
                Ctx.DrawText(Axes.XAxis.Label, point, font, TextAlignment.Center);
        }
        if (Axes.YAxis.Label is not null)
        {
            var point = new Point(PlotArea.X - 45, PlotArea.Y + PlotArea.Height / 2);
            if (MathTextParser.ContainsMath(Axes.YAxis.Label))
                Ctx.DrawRichText(MathTextParser.Parse(Axes.YAxis.Label), point, font, TextAlignment.Center);
            else
                Ctx.DrawText(Axes.YAxis.Label, point, font, TextAlignment.Center);
        }
    }

    // --- Font factories ---

    /// <summary>Creates a title font from the theme.</summary>
    /// <param name="sizeOffset">Points added to the theme's default font size; defaults to 4.</param>
    protected Font TitleFont(int sizeOffset = 4) => new()
    {
        Family = Theme.DefaultFont.Family,
        Size = Theme.DefaultFont.Size + sizeOffset,
        Weight = FontWeight.Bold,
        Color = Theme.ForegroundText
    };

    /// <summary>Creates a tick label font from the theme.</summary>
    protected Font TickFont() => new()
    {
        Family = Theme.DefaultFont.Family,
        Size = Theme.DefaultFont.Size - 2,
        Color = Theme.ForegroundText
    };

    /// <summary>Creates an axis label font from the theme.</summary>
    protected Font LabelFont() => new()
    {
        Family = Theme.DefaultFont.Family,
        Size = Theme.DefaultFont.Size,
        Color = Theme.ForegroundText
    };

    /// <summary>Formats a tick value for display.</summary>
    /// <param name="value">The numeric tick value to format.</param>
    /// <returns>A compact string representation using SI notation for very large or very small values.</returns>
    protected static string FormatTick(double value)
    {
        if (Math.Abs(value) < 1e-10) return "0";
        if (Math.Abs(value) >= 1e6 || (Math.Abs(value) < 0.01 && value != 0))
            return value.ToString("G3", CultureInfo.InvariantCulture);
        return value.ToString("G5", CultureInfo.InvariantCulture);
    }

    /// <summary>Internal wrapper for <see cref="FormatTick"/> used by the layout engine.</summary>
    internal static string FormatTickValue(double value) => FormatTick(value);

    /// <summary>Expands min/max to include all values in the data array.</summary>
    /// <param name="data">The data values to scan.</param>
    /// <param name="min">The current minimum; updated if any value is smaller.</param>
    /// <param name="max">The current maximum; updated if any value is larger.</param>
    protected static void UpdateRange(double[] data, ref double min, ref double max)
    {
        foreach (var v in data)
        {
            if (v < min) min = v;
            if (v > max) max = v;
        }
    }

    /// <summary>Computes aesthetically-spaced tick values using the default nice-number algorithm.</summary>
    /// <param name="min">The minimum value of the visible data range.</param>
    /// <param name="max">The maximum value of the visible data range.</param>
    /// <param name="targetCount">The desired number of tick intervals; the algorithm snaps to a nearby nice number.</param>
    protected static double[] ComputeTickValues(double min, double max, int targetCount = 5)
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

    /// <summary>
    /// Computes tick values respecting any <see cref="Axis.TickLocator"/> or <see cref="TickConfig.Spacing"/>
    /// configured on the axis, falling back to the default nice-number algorithm.
    /// </summary>
    /// <param name="min">The minimum value of the visible data range.</param>
    /// <param name="max">The maximum value of the visible data range.</param>
    /// <param name="axis">The axis whose locator, spacing, and formatter configuration to honour.</param>
    /// <remarks>
    /// Priority order: (1) <see cref="Axis.TickLocator"/> takes highest precedence;
    /// (2) <see cref="TickConfig.Spacing"/> auto-constructs a <c>MultipleLocator</c>;
    /// (3) the default nice-number algorithm is used as fallback.
    /// Tick formatting via <see cref="Axis.TickFormatter"/> is applied separately during rendering,
    /// not in this method.
    /// </remarks>
    protected static double[] ComputeTickValues(double min, double max, Axis axis)
    {
        // Explicit locator takes highest priority
        if (axis.TickLocator is not null)
            return axis.TickLocator.Locate(min, max);

        // TickConfig.Spacing auto-creates a MultipleLocator
        if (axis.MajorTicks.Spacing.HasValue)
            return new TickLocators.MultipleLocator(axis.MajorTicks.Spacing.Value).Locate(min, max);

        return ComputeTickValues(min, max);
    }
}
