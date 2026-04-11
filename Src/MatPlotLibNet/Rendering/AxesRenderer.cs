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
    protected Axes Axes { get; }

    protected Rect PlotArea { get; }

    protected IRenderContext Ctx { get; }

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
        // Stable sort by ZOrder so fills (ZOrder=-1) render behind other series (ZOrder=0).
        var ordered = Axes.Series.Select((s, i) => (s, i)).OrderBy(t => t.s.ZOrder);
        foreach (var (series, i) in ordered)
        {
            if (!series.Visible) continue;
            var cycledProps  = Theme.PropCycler?[i];
            var seriesColor  = cycledProps?.Color ?? Theme.CycleColors[i % Theme.CycleColors.Length];
            svgCtx?.BeginDataGroup("series", i);
            var renderer = new SvgSeriesRenderer(
                new DataTransform(0, 1, 0, 1, PlotArea), Ctx, seriesColor, cycledProps, Axes.EnableTooltips, PlotArea);
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
        // Stable sort by ZOrder so fills (ZOrder=-1) render behind other series (ZOrder=0).
        var ordered = Axes.Series.Select((s, i) => (s, i)).OrderBy(t => t.s.ZOrder);
        foreach (var (series, i) in ordered)
        {
            if (!series.Visible) continue;
            var cycledProps  = Theme.PropCycler?[i];
            var seriesColor  = cycledProps?.Color ?? Theme.CycleColors[i % Theme.CycleColors.Length];
            svgCtx?.BeginDataGroup("series", i);
            var renderer = new SvgSeriesRenderer(transform, Ctx, seriesColor, cycledProps, Axes.EnableTooltips, PlotArea);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            if (svgCtx is not null) Ctx.EndGroup();
        }
    }

    /// <summary>Renders the legend if any series have labels.</summary>
    protected void RenderLegend()
    {
        if (!Axes.Legend.Visible) return;

        var legend = Axes.Legend;
        var entries = new List<(string Label, Color Color)>();
        for (int i = 0; i < Axes.Series.Count; i++)
        {
            var series = Axes.Series[i];
            if (string.IsNullOrEmpty(series.Label)) continue;
            var cycleColor = Theme.PropCycler?[i].Color ?? Theme.CycleColors[i % Theme.CycleColors.Length];
            var seriesColor = series.GetType().GetProperty("Color")?.GetValue(series) as Color?;
            entries.Add((series.Label, seriesColor ?? cycleColor));
        }

        if (entries.Count == 0) return;

        // Font: merge FontSize override into tick font
        var baseFont = TickFont();
        var font = legend.FontSize.HasValue ? baseFont with { Size = legend.FontSize.Value } : baseFont;

        double swatchSize = 12 * legend.MarkerScale;
        double swatchGap = 6;
        double padding = 8;
        // LabelSpacing: em-based multiplier on line height (1em ≈ font size)
        double lineHeight = swatchSize + Math.Max(0, legend.LabelSpacing * font.Size);

        int nCols = Math.Max(1, legend.NCols);
        int nRows = (int)Math.Ceiling((double)entries.Count / nCols);

        // Measure column widths
        var colMaxWidths = new double[nCols];
        for (int i = 0; i < entries.Count; i++)
        {
            int col = i % nCols;
            var size = Ctx.MeasureText(entries[i].Label, font);
            if (size.Width > colMaxWidths[col]) colMaxWidths[col] = size.Width;
        }

        double colSpacingPx = legend.ColumnSpacing * font.Size;
        double totalContentWidth = swatchSize + swatchGap + colMaxWidths.Sum()
            + (nCols - 1) * (swatchSize + swatchGap + colSpacingPx);

        // Title height
        var titleFont = legend.TitleFontSize.HasValue
            ? baseFont with { Size = legend.TitleFontSize.Value, Weight = FontWeight.Bold }
            : baseFont with { Size = baseFont.Size + 1, Weight = FontWeight.Bold };
        double titleHeight = !string.IsNullOrEmpty(legend.Title) ? titleFont.Size + 4 : 0;

        double boxWidth = padding + totalContentWidth + padding;
        double boxHeight = padding + titleHeight + nRows * lineHeight - (lineHeight - swatchSize) + padding;

        double inset = 10;
        double centerX = PlotArea.X + PlotArea.Width / 2;
        double centerY = PlotArea.Y + PlotArea.Height / 2;
        var (boxX, boxY) = legend.Position switch
        {
            LegendPosition.UpperLeft    => (PlotArea.X + inset, PlotArea.Y + inset),
            LegendPosition.LowerRight   => (PlotArea.X + PlotArea.Width - boxWidth - inset, PlotArea.Y + PlotArea.Height - boxHeight - inset),
            LegendPosition.LowerLeft    => (PlotArea.X + inset, PlotArea.Y + PlotArea.Height - boxHeight - inset),
            LegendPosition.Right        => (PlotArea.X + PlotArea.Width - boxWidth - inset, centerY - boxHeight / 2),
            LegendPosition.CenterLeft   => (PlotArea.X + inset, centerY - boxHeight / 2),
            LegendPosition.CenterRight  => (PlotArea.X + PlotArea.Width - boxWidth - inset, centerY - boxHeight / 2),
            LegendPosition.LowerCenter  => (centerX - boxWidth / 2, PlotArea.Y + PlotArea.Height - boxHeight - inset),
            LegendPosition.UpperCenter  => (centerX - boxWidth / 2, PlotArea.Y + inset),
            LegendPosition.Center       => (centerX - boxWidth / 2, centerY - boxHeight / 2),
            _                           => (PlotArea.X + PlotArea.Width - boxWidth - inset, PlotArea.Y + inset) // Best / UpperRight
        };

        // Frame background
        if (legend.FrameOn)
        {
            var faceColor = legend.FaceColor ?? Theme.Background;
            var bgAlpha = (byte)(legend.FrameAlpha * 255);
            var bgColor = faceColor.WithAlpha(bgAlpha);
            var edgeColor = legend.EdgeColor ?? Theme.ForegroundText;

            if (legend.Shadow)
            {
                var shadowColor = new Color(0, 0, 0, 80);
                Ctx.DrawRectangle(new Rect(boxX + 3, boxY + 3, boxWidth, boxHeight), shadowColor, null, 0);
            }

            // FancyBox: rounded corners are expressed via SVG rx/ry — DrawRectangle doesn't expose
            // corner radius, so we use an SVG comment/group attribute via the context when available.
            // For now render normally; FancyBox is a visual hint recognised by advanced renderers.
            Ctx.DrawRectangle(new Rect(boxX, boxY, boxWidth, boxHeight), bgColor, edgeColor, 0.5);
        }

        Ctx.BeginGroup("legend");

        // Title
        if (!string.IsNullOrEmpty(legend.Title))
        {
            Ctx.DrawText(legend.Title,
                new Point(boxX + padding + totalContentWidth / 2, boxY + padding + titleFont.Size),
                titleFont, TextAlignment.Center);
        }

        var svgCtxLegend = Axes.EnableInteractiveAttributes ? Ctx as SvgRenderContext : null;
        for (int i = 0; i < entries.Count; i++)
        {
            int row = i / nCols;
            int col = i % nCols;
            var (label, color) = entries[i];

            // X offset for this column
            double colX = boxX + padding;
            for (int c = 0; c < col; c++)
                colX += swatchSize + swatchGap + colMaxWidths[c] + colSpacingPx;

            double entryY = boxY + padding + titleHeight + row * lineHeight;

            svgCtxLegend?.BeginLegendItemGroup(i);
            Ctx.DrawRectangle(new Rect(colX, entryY, swatchSize, swatchSize), color, null, 0);
            Ctx.DrawText(label, new Point(colX + swatchSize + swatchGap, entryY + swatchSize - 1), font, TextAlignment.Left);
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

        int steps = 50;
        const double extendFrac = 0.10;
        bool extendMin = cb.Extend is ColorBarExtend.Min or ColorBarExtend.Both;
        bool extendMax = cb.Extend is ColorBarExtend.Max or ColorBarExtend.Both;

        Ctx.BeginGroup("colorbar");

        if (cb.Orientation == ColorBarOrientation.Horizontal)
        {
            // Horizontal: bar below the plot area, length = plot width * Shrink, centered
            double fullW = PlotArea.Width * cb.Shrink;
            double barW  = cb.Aspect > 0 ? fullW : cb.Width * cb.Shrink;
            double barH  = cb.Aspect > 0 ? fullW / cb.Aspect : cb.Width;
            double barX  = PlotArea.X + (PlotArea.Width - fullW) / 2;
            double barY  = PlotArea.Y + PlotArea.Height + cb.Padding;

            double extW  = fullW * extendFrac;
            bool drawXMin = extendMin;
            bool drawXMax = extendMax;
            double gradX = barX + (drawXMin ? extW : 0);
            double gradW = fullW - (drawXMin ? extW : 0) - (drawXMax ? extW : 0);

            if (drawXMin)
            {
                var underColor = colorMap.GetUnderColor() ?? colorMap.GetColor(0.0);
                Ctx.DrawRectangle(new Rect(barX, barY, extW, barH), underColor, null, 0);
            }

            for (int i = 0; i < steps; i++)
            {
                double frac = (double)i / steps;
                var color = colorMap.GetColor(frac);
                double stepX = gradX + gradW * i / steps;
                double stepW = gradW / steps + 1;
                Ctx.DrawRectangle(new Rect(stepX, barY, stepW, barH), color, null, 0);
                if (cb.DrawEdges)
                    Ctx.DrawLine(new Point(stepX, barY), new Point(stepX, barY + barH), Theme.ForegroundText, 0.3, LineStyle.Solid);
            }

            if (drawXMax)
            {
                var overColor = colorMap.GetOverColor() ?? colorMap.GetColor(1.0);
                Ctx.DrawRectangle(new Rect(gradX + gradW, barY, extW, barH), overColor, null, 0);
            }

            Ctx.DrawRectangle(new Rect(barX, barY, fullW, barH), null, Theme.ForegroundText, 0.5);

            var tickFont = TickFont();
            double labelY = barY + barH + 4 + tickFont.Size;
            for (int i = 0; i <= 5; i++)
            {
                double frac = (double)i / 5;
                double value = min + frac * (max - min);
                Ctx.DrawText(FormatTick(value), new Point(gradX + gradW * frac, labelY), tickFont, TextAlignment.Center);
            }

            if (cb.Label is not null)
                Ctx.DrawText(cb.Label, new Point(barX + fullW / 2, labelY + tickFont.Size + 4), LabelFont(), TextAlignment.Center);
        }
        else
        {
            // Vertical (default): bar to the right of the plot area
            double fullH = PlotArea.Height * cb.Shrink;
            double barW  = cb.Width;
            double barX  = PlotArea.X + PlotArea.Width + cb.Padding;
            double barY  = PlotArea.Y + (PlotArea.Height - fullH) / 2;

            double extH  = fullH * extendFrac;
            double gradY = barY + (extendMax ? extH : 0);
            double gradH = fullH - (extendMin ? extH : 0) - (extendMax ? extH : 0);

            if (extendMax)
            {
                var overColor = colorMap.GetOverColor() ?? colorMap.GetColor(1.0);
                Ctx.DrawRectangle(new Rect(barX, barY, barW, extH), overColor, null, 0);
            }

            for (int i = 0; i < steps; i++)
            {
                double frac = 1.0 - (double)i / steps;
                var color = colorMap.GetColor(frac);
                double stepY = gradY + gradH * i / steps;
                double stepH = gradH / steps + 1;
                Ctx.DrawRectangle(new Rect(barX, stepY, barW, stepH), color, null, 0);
                if (cb.DrawEdges)
                    Ctx.DrawLine(new Point(barX, stepY), new Point(barX + barW, stepY), Theme.ForegroundText, 0.3, LineStyle.Solid);
            }

            if (extendMin)
            {
                var underColor = colorMap.GetUnderColor() ?? colorMap.GetColor(0.0);
                Ctx.DrawRectangle(new Rect(barX, gradY + gradH, barW, extH), underColor, null, 0);
            }

            Ctx.DrawRectangle(new Rect(barX, barY, barW, fullH), null, Theme.ForegroundText, 0.5);

            var tickFont = TickFont();
            double labelX = barX + barW + 4;
            for (int i = 0; i <= 5; i++)
            {
                double frac = (double)i / 5;
                double value = max - frac * (max - min);
                Ctx.DrawText(FormatTick(value), new Point(labelX, barY + fullH * frac + 4), tickFont, TextAlignment.Left);
            }

            if (cb.Label is not null)
                Ctx.DrawText(cb.Label, new Point(labelX + 30, barY + fullH / 2), LabelFont(), TextAlignment.Center);
        }

        Ctx.EndGroup();
    }

    /// <summary>Renders the axes title above the plot area.</summary>
    protected void RenderTitle()
    {
        if (Axes.Title is null) return;
        var baseFont = TitleFont(2);
        var font = Axes.TitleStyle?.ApplyTo(baseFont) ?? baseFont;

        // Horizontal alignment based on TitleLoc; Pad shifts the Y position when set
        double padOffset = Axes.TitleStyle?.Pad ?? 0;
        double titleY = PlotArea.Y - 8 - padOffset;
        double titleX = Axes.TitleLoc switch
        {
            TitleLocation.Left  => PlotArea.X,
            TitleLocation.Right => PlotArea.X + PlotArea.Width,
            _                   => PlotArea.X + PlotArea.Width / 2
        };
        var alignment = Axes.TitleLoc switch
        {
            TitleLocation.Left  => TextAlignment.Left,
            TitleLocation.Right => TextAlignment.Right,
            _                   => TextAlignment.Center
        };
        var point = new Point(titleX, titleY);

        if (MathTextParser.ContainsMath(Axes.Title))
            Ctx.DrawRichText(MathTextParser.Parse(Axes.Title), point, font, alignment);
        else
            Ctx.DrawText(Axes.Title, point, font, alignment);
    }

    /// <summary>Renders X and Y axis labels.</summary>
    protected void RenderAxisLabels()
    {
        var baseFont = LabelFont();

        if (Axes.XAxis.Label is not null)
        {
            var font = Axes.XAxis.LabelStyle?.ApplyTo(baseFont) ?? baseFont;
            double padOffset = Axes.XAxis.LabelStyle?.Pad ?? 0;
            var point = new Point(PlotArea.X + PlotArea.Width / 2, PlotArea.Y + PlotArea.Height + 35 + padOffset);
            if (MathTextParser.ContainsMath(Axes.XAxis.Label))
                Ctx.DrawRichText(MathTextParser.Parse(Axes.XAxis.Label), point, font, TextAlignment.Center);
            else
                Ctx.DrawText(Axes.XAxis.Label, point, font, TextAlignment.Center);
        }
        if (Axes.YAxis.Label is not null)
        {
            var font = Axes.YAxis.LabelStyle?.ApplyTo(baseFont) ?? baseFont;
            double padOffset = Axes.YAxis.LabelStyle?.Pad ?? 0;
            var point = new Point(PlotArea.X - 45 - padOffset, PlotArea.Y + PlotArea.Height / 2);
            if (MathTextParser.ContainsMath(Axes.YAxis.Label))
                Ctx.DrawRichText(MathTextParser.Parse(Axes.YAxis.Label), point, font, TextAlignment.Center, 90);
            else
                Ctx.DrawText(Axes.YAxis.Label, point, font, TextAlignment.Center, 90);
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
        Size = Theme.DefaultFont.Size,   // same as axis labels — matches matplotlib's default
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
