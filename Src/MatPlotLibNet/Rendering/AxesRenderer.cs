// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Lighting;
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

    /// <summary>Maximum rendered width of Y-axis tick labels. Set by subclasses during tick rendering;
    /// consumed by <see cref="RenderAxisLabels"/> to place the Y-axis label clear of the widest tick label.</summary>
    protected double MeasuredYTickMaxWidth { get; set; }

    /// <summary>Maximum rendered height of X-axis tick labels. Set by subclasses during tick rendering;
    /// reserved for future use by <see cref="RenderAxisLabels"/>.</summary>
    protected double MeasuredXTickMaxHeight { get; set; }

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

    /// <summary>Returns the inner data-plot bounds (after axis-label and tick reservations).
    /// Used by <see cref="ChartRenderer"/> to position inset axes within the data area.
    /// The default implementation returns the full <see cref="PlotArea"/>.</summary>
    public virtual Rect ComputeInnerBounds() => PlotArea;

    /// <summary>Optional figure size (pixels) for renderers that need the whole figure dimensions
    /// to reproduce matplotlib's layout exactly. Set by the factory when available; null otherwise.</summary>
    protected (double Width, double Height)? FigureSize { get; set; }

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
    /// <param name="figureSize">Optional figure size; set for 3-D renderers that need the matplotlib-compatible square layout.</param>
    /// <returns>An <see cref="AxesRenderer"/> instance for the axes' coordinate system.</returns>
    public static AxesRenderer Create(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, (double W, double H)? figureSize = null)
    {
        var renderer = RendererFactories.TryGetValue(axes.CoordinateSystem, out var factory)
            ? factory(axes, plotArea, ctx, theme)
            : new CartesianAxesRenderer(axes, plotArea, ctx, theme);
        if (figureSize.HasValue)
            renderer.FigureSize = (figureSize.Value.W, figureSize.Value.H);
        return renderer;
    }

    // --- Shared rendering helpers (available to all subclasses) ---

    /// <summary>Renders all series on these axes through the visitor pattern.</summary>
    protected void RenderSeries()
    {
        var svgCtx = Ctx as SvgRenderContext;
        var interactiveSvgCtx = Axes.EnableInteractiveAttributes ? svgCtx : null;
        // Stable sort by ZOrder so fills (ZOrder=-1) render behind other series (ZOrder=0).
        var ordered = Axes.Series.Select((s, i) => (s, i)).OrderBy(t => t.s.ZOrder);
        foreach (var (series, i) in ordered)
        {
            if (!series.Visible) continue;
            var cycledProps  = Theme.PropCycler?[i];
            var seriesColor  = cycledProps?.Color ?? Theme.CycleColors[i % Theme.CycleColors.Length];
            // Pie: pre-populate per-wedge colors from the cycle when the series has no explicit Colors.
            // Matches matplotlib behaviour where each wedge picks the next colour from the cycle.
            if (series is Models.Series.PieSeries pie && pie.Colors is null)
                pie.Colors = Enumerable.Range(0, pie.Sizes.Length)
                    .Select(j => Theme.CycleColors[j % Theme.CycleColors.Length])
                    .ToArray();
            bool openedGroup = BeginSeriesGroup(svgCtx, interactiveSvgCtx, series, i);
            var renderer = new SvgSeriesRenderer(
                new DataTransform(0, 1, 0, 1, PlotArea), Ctx, seriesColor, cycledProps, Axes.EnableTooltips, PlotArea,
                theme: Theme);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            if (openedGroup) Ctx.EndGroup();
        }
    }

    /// <summary>Renders all series using a unified 3D projection (for ThreeD axes).</summary>
    /// <param name="projection">The shared projection used by all 3D series renderers.</param>
    /// <param name="lightSource">Optional light source for per-face lighting.</param>
    protected void RenderSeries(Projection3D projection, ILightSource? lightSource = null,
        DepthQueue3D? depthQueue = null)
    {
        var svgCtx = Ctx as SvgRenderContext;
        var interactiveSvgCtx = Axes.EnableInteractiveAttributes ? svgCtx : null;
        bool emit3D = Axes.Emit3DVertexData;
        var ordered = Axes.Series.Select((s, i) => (s, i)).OrderBy(t => t.s.ZOrder);
        foreach (var (series, i) in ordered)
        {
            if (!series.Visible) continue;
            var cycledProps  = Theme.PropCycler?[i];
            var seriesColor  = cycledProps?.Color ?? Theme.CycleColors[i % Theme.CycleColors.Length];
            bool openedGroup = BeginSeriesGroup(svgCtx, interactiveSvgCtx, series, i);
            var renderer = new SvgSeriesRenderer(
                new DataTransform(0, 1, 0, 1, PlotArea), Ctx, seriesColor, cycledProps,
                Axes.EnableTooltips, PlotArea, projection, lightSource, emit3D, theme: Theme,
                depthQueue: depthQueue);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            if (openedGroup) Ctx.EndGroup();
        }
    }

    /// <summary>Renders all series with a specific DataTransform.</summary>
    /// <param name="transform">The coordinate transform mapping data space to pixel space.</param>
    protected void RenderSeries(DataTransform transform)
    {
        // Bar grouping: when multiple vertical non-stacked bar series share the same categories,
        // assign offsets so bars sit side-by-side (matching matplotlib grouped bar behaviour).
        var barGroups = Axes.Series
            .OfType<Models.Series.BarSeries>()
            .Where(b => b.Orientation == Models.Series.BarOrientation.Vertical && b.StackBaseline is null)
            .ToList();
        if (barGroups.Count > 1)
        {
            // Total group width = 0.7 (same as matplotlib default bar width 0.8 but snug for groups).
            double groupW = 0.7;
            double barW   = groupW / barGroups.Count;
            for (int bi = 0; bi < barGroups.Count; bi++)
            {
                barGroups[bi].BarGroupOffset = (bi - (barGroups.Count - 1) / 2.0) * barW;
                barGroups[bi].BarGroupWidth  = barW;
            }
        }
        else if (barGroups.Count == 1)
        {
            barGroups[0].BarGroupOffset = 0;
            barGroups[0].BarGroupWidth  = null; // use series.BarWidth
        }

        var svgCtx = Ctx as SvgRenderContext;
        var interactiveSvgCtx = Axes.EnableInteractiveAttributes ? svgCtx : null;
        // Stable sort by ZOrder so fills (ZOrder=-1) render behind other series (ZOrder=0).
        var ordered = Axes.Series.Select((s, i) => (s, i)).OrderBy(t => t.s.ZOrder);
        foreach (var (series, i) in ordered)
        {
            if (!series.Visible) continue;
            var cycledProps  = Theme.PropCycler?[i];
            var seriesColor  = cycledProps?.Color ?? Theme.CycleColors[i % Theme.CycleColors.Length];
            // Pie: pre-populate per-wedge colors from the cycle when the series has no explicit Colors.
            if (series is Models.Series.PieSeries pie && pie.Colors is null)
                pie.Colors = Enumerable.Range(0, pie.Sizes.Length)
                    .Select(j => Theme.CycleColors[j % Theme.CycleColors.Length])
                    .ToArray();
            bool openedGroup = BeginSeriesGroup(svgCtx, interactiveSvgCtx, series, i);
            var renderer = new SvgSeriesRenderer(transform, Ctx, seriesColor, cycledProps, Axes.EnableTooltips, PlotArea,
                theme: Theme);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            if (openedGroup) Ctx.EndGroup();
        }
    }

    /// <summary>Opens the appropriate SVG group for a series: interactive group with data attributes, or accessible group with aria-label.</summary>
    /// <returns><see langword="true"/> if a group was opened and must be closed.</returns>
    private static bool BeginSeriesGroup(SvgRenderContext? svgCtx, SvgRenderContext? interactiveSvgCtx, ISeries series, int index)
    {
        if (interactiveSvgCtx is not null)
        {
            interactiveSvgCtx.BeginDataGroup("series", index, series.Label);
            return true;
        }
        if (svgCtx is not null && !string.IsNullOrEmpty(series.Label))
        {
            svgCtx.BeginAccessibleGroup("series", series.Label);
            return true;
        }
        return false;
    }

    /// <summary>Renders the legend if any series have labels.</summary>
    protected void RenderLegend()
    {
        if (!Axes.Legend.Visible) return;

        var legend = Axes.Legend;
        var entries = new List<(string Label, Color Color, Models.Series.ISeries Series)>();
        for (int i = 0; i < Axes.Series.Count; i++)
        {
            var series = Axes.Series[i];
            if (string.IsNullOrEmpty(series.Label)) continue;
            var cycleColor = Theme.PropCycler?[i].Color ?? Theme.CycleColors[i % Theme.CycleColors.Length];
            var seriesColor = series.GetType().GetProperty("Color")?.GetValue(series) as Color?;
            entries.Add((series.Label, seriesColor ?? cycleColor, series));
        }

        if (entries.Count == 0) return;

        // Font: merge FontSize override into tick font
        var baseFont = TickFont();
        var font = legend.FontSize.HasValue ? baseFont with { Size = legend.FontSize.Value } : baseFont;

        // matplotlib legend handles use `handlelength = 2.0 em` × `handleheight = 0.7 em`
        // where 1 em = legend font size. For patch-type series (bar/hist/area) this produces
        // a WIDE RECTANGLE, not a square. Line series also use this width but with a line handle.
        double handleWidth  = font.Size * 2.0 * legend.MarkerScale;   // ~27.8 px at 13.89 px font
        double handleHeight = font.Size * 0.7 * legend.MarkerScale;   // ~9.72 px at 13.89 px font
        double swatchSize   = handleHeight;                            // legacy name — height only
        double maxSwatchW   = handleWidth;                              // width is uniform across entries now
        double swatchGap    = font.Size * 0.8;                          // matplotlib handletextpad = 0.8 em
        double padding = 8;
        // LabelSpacing: em-based multiplier on line height (1em ≈ font size)
        double lineHeight = swatchSize + Math.Max(0, legend.LabelSpacing * font.Size);

        int nCols = Math.Max(1, legend.NCols);
        int nRows = (int)Math.Ceiling((double)entries.Count / nCols);

        // Measure column widths — parse mathtext so $\alpha$ decay measures as "α decay", not the raw LaTeX
        var colMaxWidths = new double[nCols];
        for (int i = 0; i < entries.Count; i++)
        {
            int col = i % nCols;
            var size = MathTextParser.ContainsMath(entries[i].Label)
                ? Ctx.MeasureRichText(MathTextParser.Parse(entries[i].Label), font)
                : Ctx.MeasureText(entries[i].Label, font);
            if (size.Width > colMaxWidths[col]) colMaxWidths[col] = size.Width;
        }

        double colSpacingPx = legend.ColumnSpacing * font.Size;
        double totalContentWidth = maxSwatchW + swatchGap + colMaxWidths.Sum()
            + (nCols - 1) * (maxSwatchW + swatchGap + colSpacingPx);

        // Title height
        var titleFont = legend.TitleFontSize.HasValue
            ? baseFont with { Size = legend.TitleFontSize.Value, Weight = FontWeight.Bold }
            : baseFont with { Size = baseFont.Size + 1, Weight = FontWeight.Bold };
        double titleHeight = !string.IsNullOrEmpty(legend.Title) ? titleFont.Size + 4 : 0;

        double boxWidth = padding + totalContentWidth + padding;
        double boxHeight = padding + titleHeight + nRows * lineHeight - (lineHeight - swatchSize) + padding;

        double inset = 10;
        // Outside legend positions are placed just past the plot-area edge. The 8 px offset
        // keeps the legend clear of the spine + tick marks; the constrained-layout engine
        // pre-reserved enough margin on that edge to host the full box (see
        // `ConstrainedLayoutEngine.Compute` / `LegendMeasurer`), so drawing past the spine
        // won't fall off the figure.
        const double OutsideGap = 8;
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
            LegendPosition.OutsideRight => (PlotArea.X + PlotArea.Width + OutsideGap, centerY - boxHeight / 2),
            LegendPosition.OutsideLeft  => (PlotArea.X - boxWidth - OutsideGap, centerY - boxHeight / 2),
            LegendPosition.OutsideTop   => (centerX - boxWidth / 2, PlotArea.Y - boxHeight - OutsideGap),
            LegendPosition.OutsideBottom => (centerX - boxWidth / 2, PlotArea.Y + PlotArea.Height + OutsideGap),
            _                           => (PlotArea.X + PlotArea.Width - boxWidth - inset, PlotArea.Y + inset) // Best / UpperRight
        };

        // Frame background
        if (legend.FrameOn)
        {
            var faceColor = legend.FaceColor ?? Theme.Background;
            var bgAlpha = (byte)(legend.FrameAlpha * 255);
            var bgColor = faceColor.WithAlpha(bgAlpha);
            // matplotlib v2 default `legend.edgecolor = '0.8'` = #CCCCCC (light grey).
            // Falls back to the theme foreground only if no override is set.
            var edgeColor = legend.EdgeColor ?? Color.FromHex("#CCCCCC");

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

        if (Ctx is SvgRenderContext svgCtxLegendGroup)
            svgCtxLegendGroup.BeginAccessibleGroup("legend", "Chart legend");
        else
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
            var (label, color, seriesRef) = entries[i];

            // X offset for this column
            double colX = boxX + padding;
            for (int c = 0; c < col; c++)
                colX += maxSwatchW + swatchGap + colMaxWidths[c] + colSpacingPx;

            double entryY = boxY + padding + titleHeight + row * lineHeight;

            svgCtxLegend?.BeginLegendItemGroup(i, label);

            // Swatch dispatch: line segment for line-type series, marker for scatter,
            // line+caps for error bar, filled rectangle for patches (bar/hist/area/violin/pie).
            DrawLegendSwatch(seriesRef, colX, entryY, maxSwatchW, swatchSize, color);

            // Text anchor sits just past the swatch.
            var textPoint = new Point(colX + maxSwatchW + swatchGap, entryY + swatchSize - 1);
            if (MathTextParser.ContainsMath(label))
                Ctx.DrawRichText(MathTextParser.Parse(label), textPoint, font, TextAlignment.Left);
            else
                Ctx.DrawText(label, textPoint, font, TextAlignment.Left);
            if (svgCtxLegend is not null) Ctx.EndGroup();
        }

        Ctx.EndGroup();
    }

    /// <summary>Line-type series get a wider swatch (horizontal segment) in the legend; patch-type
    /// series (bar/hist/area/violin/pie) get a square filled rectangle. Mirrors matplotlib's
    /// default `HandlerLine2D` / `HandlerPatch` dispatch.</summary>
    private static bool IsLineTypeSeries(Models.Series.ISeries series) => series switch
    {
        Models.Series.LineSeries           => true,
        Models.Series.SignalSeries         => true,
        Models.Series.SignalXYSeries       => true,
        Models.Series.SparklineSeries      => true,
        Models.Series.EcdfSeries           => true,
        Models.Series.RegressionSeries     => true,
        Models.Series.StepSeries           => true,
        Models.Series.ScatterSeries        => false,  // marker, not line
        Models.Series.ErrorBarSeries       => true,   // drawn as line + caps
        _                                  => false,
    };

    /// <summary>Draws a type-appropriate legend handle at (x, y): line segment for line series,
    /// single centred marker for scatter, line+caps for error bar, filled rectangle for patches.</summary>
    private void DrawLegendSwatch(Models.Series.ISeries series, double x, double y, double wMax, double h, Color color)
    {
        double midY = y + h / 2;

        switch (series)
        {
            case Models.Series.LineSeries ls:
            {
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), color, ls.LineWidth, ls.LineStyle);
                if (ls.Marker.HasValue)
                    DrawLegendMarker(x + wMax / 2, midY, ls.Marker.Value, ls.MarkerSize, color);
                break;
            }
            case Models.Series.SignalSeries ss:
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), color, ss.LineWidth, ss.LineStyle);
                break;
            case Models.Series.SignalXYSeries sxy:
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), color, sxy.LineWidth, sxy.LineStyle);
                break;
            case Models.Series.SparklineSeries sp:
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), color, sp.LineWidth, LineStyle.Solid);
                break;
            case Models.Series.EcdfSeries ec:
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), color, ec.LineWidth, ec.LineStyle);
                break;
            case Models.Series.RegressionSeries rs:
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), color, rs.LineWidth, rs.LineStyle);
                break;
            case Models.Series.StepSeries stp:
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), color, 1.5, LineStyle.Solid);
                break;
            case Models.Series.ScatterSeries sc:
            {
                // matplotlib legend marker size ≈ series point size, scaled down slightly.
                double radius = Math.Min(h / 2 - 1, Math.Sqrt(sc.MarkerSize / Math.PI) * (100.0 / 72.0));
                DrawLegendMarker(x + h / 2, midY, sc.Marker, radius * radius * Math.PI, color);
                break;
            }
            case Models.Series.ErrorBarSeries eb:
            {
                Ctx.DrawLine(new Point(x + 2, midY), new Point(x + wMax - 2, midY), color, eb.LineWidth, LineStyle.Solid);
                Ctx.DrawLine(new Point(x + 2, midY - 3), new Point(x + 2, midY + 3), color, eb.LineWidth, LineStyle.Solid);
                Ctx.DrawLine(new Point(x + wMax - 2, midY - 3), new Point(x + wMax - 2, midY + 3), color, eb.LineWidth, LineStyle.Solid);
                break;
            }
            default:
            {
                // Patch-like: bar/hist/area/violin/pie/etc → filled RECTANGLE (matplotlib uses
                // handlelength × handleheight; our wMax carries handlelength, h carries handleheight).
                // Previous `Math.Min(wMax, h)` clamped it to a square which matplotlib never does.
                Ctx.DrawRectangle(new Rect(x, y, wMax, h), color, null, 0);
                break;
            }
        }
    }

    /// <summary>Draws a single legend marker at (cx, cy). Mirrors the subset of markers that
    /// <c>ScatterSeriesRenderer</c> actually renders (circle + square).</summary>
    private void DrawLegendMarker(double cx, double cy, MarkerStyle marker, double size_pt2, Color color)
    {
        double radius = Math.Sqrt(Math.Max(size_pt2, 1) / Math.PI) * (100.0 / 72.0);
        if (marker == MarkerStyle.Square)
            Ctx.DrawRectangle(new Rect(cx - radius, cy - radius, 2 * radius, 2 * radius), color, null, 0);
        else
            Ctx.DrawCircle(new Point(cx, cy), radius, color, null, 0);
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

        if (Ctx is SvgRenderContext svgCtxColorBar)
            svgCtxColorBar.BeginAccessibleGroup("colorbar", "Color bar");
        else
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
            var hCbTicks = new double[6];
            for (int i = 0; i <= 5; i++) hCbTicks[i] = min + ((double)i / 5) * (max - min);
            var hCbFormat = BuildUniformTickFormatter(hCbTicks);
            for (int i = 0; i <= 5; i++)
            {
                double frac = (double)i / 5;
                double value = min + frac * (max - min);
                Ctx.DrawText(hCbFormat(value), new Point(gradX + gradW * frac, labelY), tickFont, TextAlignment.Center);
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
            double maxTickWidth = 0;
            var vCbTicks = new double[6];
            for (int i = 0; i <= 5; i++) vCbTicks[i] = max - ((double)i / 5) * (max - min);
            var vCbFormat = BuildUniformTickFormatter(vCbTicks);
            for (int i = 0; i <= 5; i++)
            {
                double frac = (double)i / 5;
                double value = max - frac * (max - min);
                var tickText = vCbFormat(value);
                Ctx.DrawText(tickText, new Point(labelX, barY + fullH * frac + 4), tickFont, TextAlignment.Left);
                var w = Ctx.MeasureText(tickText, tickFont).Width;
                if (w > maxTickWidth) maxTickWidth = w;
            }

            if (cb.Label is not null)
            {
                // Rotate the label 90° (vertical, reading bottom-to-top) so it sits in a
                // narrow gutter beside the colorbar instead of sprawling horizontally and
                // getting clipped by the figure right edge. Matches matplotlib defaults.
                var labelFont = LabelFont();
                double labelGutter = labelX + maxTickWidth + 8;
                Ctx.DrawText(cb.Label, new Point(labelGutter, barY + fullH / 2), labelFont, TextAlignment.Center, rotation: 90);
            }
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
            // Dynamic offset: the x-label baseline sits a clear gap below the tick-label cell.
            //   dynamicOffset = tickLength + tickPad + tickLabelCellHeight + gap + labelAscent
            // where labelAscent ≈ 0.8 × label.Size. Previously hardcoded to 35 px, which
            // placed the baseline ~11 px ABOVE the tick-label bottom under the v2 theme
            // (tick cell ≈ 26 px), causing date labels like "Feb 10" to collide with the
            // x-axis label.
            var xMajor = Axes.XAxis.MajorTicks;
            const double xLabelGap = 6;
            double labelAscent = font.Size * 0.8;
            double dynamicOffset = MeasuredXTickMaxHeight > 0
                ? xMajor.Length + xMajor.Pad + MeasuredXTickMaxHeight + xLabelGap + labelAscent
                : 35;
            var point = new Point(PlotArea.X + PlotArea.Width / 2,
                PlotArea.Y + PlotArea.Height + dynamicOffset + padOffset);
            if (MathTextParser.ContainsMath(Axes.XAxis.Label))
                Ctx.DrawRichText(MathTextParser.Parse(Axes.XAxis.Label), point, font, TextAlignment.Center);
            else
                Ctx.DrawText(Axes.XAxis.Label, point, font, TextAlignment.Center);
        }
        if (Axes.YAxis.Label is not null)
        {
            var font = Axes.YAxis.LabelStyle?.ApplyTo(baseFont) ?? baseFont;
            double padOffset = Axes.YAxis.LabelStyle?.Pad ?? 0;
            // Compute y-label x dynamically: tick mark + pad + widest rendered tick label + gap.
            // Falls back to the legacy fixed 45-px offset when no tick widths were measured
            // (non-Cartesian renderers or empty axes).
            var yMajor = Axes.YAxis.MajorTicks;
            double yLabelGap = 12;
            double dynamicOffset = MeasuredYTickMaxWidth > 0
                ? yMajor.Length + yMajor.Pad + MeasuredYTickMaxWidth + yLabelGap
                : 45;
            var point = new Point(PlotArea.X - dynamicOffset - padOffset, PlotArea.Y + PlotArea.Height / 2);
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

    /// <summary>
    /// Builds a closure that formats every tick in <paramref name="ticks"/> with the SAME
    /// decimal precision, matching matplotlib's <c>ScalarFormatter</c> behaviour. When the
    /// step size between ticks has one decimal place (e.g. 0.1, 0.5, 2.5), every returned
    /// label will have exactly that one decimal place: <c>"0.0", "0.5", "1.0"</c> instead of
    /// the mixed <c>"0", "0.5", "1"</c> that a per-tick <c>G5</c> format produces.
    /// </summary>
    /// <remarks>
    /// Called once before a tick-draw loop; the returned <see cref="Func{Double, String}"/>
    /// is invoked per tick. Falls back to <see cref="FormatTick"/> when <paramref name="ticks"/>
    /// has fewer than two values (no step to measure).
    /// </remarks>
    protected static Func<double, string> BuildUniformTickFormatter(double[] ticks)
    {
        if (ticks is null || ticks.Length < 2) return FormatTick;
        double step = Math.Abs(ticks[1] - ticks[0]);
        int decimals = RequiredDecimalPlaces(step);
        string format = $"F{decimals}";
        return v =>
        {
            if (Math.Abs(v) < 1e-10) v = 0;  // avoid "-0.0"
            return v.ToString(format, CultureInfo.InvariantCulture);
        };
    }

    /// <summary>Returns the minimum number of decimal places needed so that multiplying
    /// <paramref name="step"/> by 10^decimals yields an integer (within tolerance). Gives 0
    /// for integer steps (1, 2, 10), 1 for 0.5/0.1/2.5, 2 for 0.25/0.01, etc.</summary>
    private static int RequiredDecimalPlaces(double step)
    {
        if (step <= 0 || !double.IsFinite(step)) return 0;
        for (int d = 0; d <= 10; d++)
        {
            double scaled = step * Math.Pow(10, d);
            if (Math.Abs(scaled - Math.Round(scaled)) < 1e-9) return d;
        }
        return 0;
    }

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
    /// <param name="targetCount">The desired number of tick intervals; the algorithm snaps to a nearby nice number. Defaults to 8 to match matplotlib's <c>MaxNLocator(nbins='auto')</c>.</param>
    protected static double[] ComputeTickValues(double min, double max, int targetCount = 8)
        => new TickLocators.AutoLocator(targetCount).Locate(min, max);

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

    /// <summary>
    /// Like <see cref="ComputeTickValues(double, double, Axis)"/> but scales the target tick
    /// count to the available pixel dimension so short plot areas (sparklines, multi-panel
    /// dashboards) don't cram eight labels into 80 pixels.
    /// </summary>
    /// <param name="min">Visible range min.</param>
    /// <param name="max">Visible range max.</param>
    /// <param name="axis">Axis config (locator / spacing / formatter).</param>
    /// <param name="plotPixels">Length of the plot area along the tick direction, in pixels.
    /// Plots with at least 240 px get the matplotlib-default 8 ticks; smaller plots
    /// (sparklines, multi-panel dashboards) downscale to 1 tick per ≈ 30 px so labels
    /// don't stack vertically.</param>
    protected static double[] ComputeTickValues(double min, double max, Axis axis, double plotPixels)
    {
        if (axis.TickLocator is not null) return axis.TickLocator.Locate(min, max);
        if (axis.MajorTicks.Spacing.HasValue)
            return new TickLocators.MultipleLocator(axis.MajorTicks.Spacing.Value).Locate(min, max);
        // Preserve the default 8-tick density for normal-sized plots so existing fidelity
        // tests stay green; only downscale for short rows where labels would otherwise overlap.
        int target = plotPixels >= 240 ? 8 : Math.Clamp((int)Math.Round(plotPixels / 30), 2, 6);
        return ComputeTickValues(min, max, target);
    }
}
