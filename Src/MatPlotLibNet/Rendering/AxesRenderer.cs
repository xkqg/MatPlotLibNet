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

    /// <summary>Per-entry legend item bounds captured during <see cref="RenderLegend"/> or
    /// <see cref="ComputeLegendBounds"/>. Each entry maps a series index to its pixel-space
    /// bounding rectangle. Empty when the legend is hidden or has no labelled entries.</summary>
    internal List<LegendItemBounds> LegendBounds { get; } = [];

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
    protected Size? FigureSize { get; set; }

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
    public static AxesRenderer Create(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme, Size? figureSize = null)
    {
        var renderer = RendererFactories.TryGetValue(axes.CoordinateSystem, out var factory)
            ? factory(axes, plotArea, ctx, theme)
            : new CartesianAxesRenderer(axes, plotArea, ctx, theme);
        if (figureSize.HasValue)
            renderer.FigureSize = figureSize.Value;
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
    /// <param name="depthQueue">Optional shared depth queue for cross-series back-to-front compositing.</param>
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
        var entries = new List<RenderLegendEntry>();
        for (int i = 0; i < Axes.Series.Count; i++)
        {
            var series = Axes.Series[i];
            if (string.IsNullOrEmpty(series.Label)) continue;
            var cycleColor = Theme.PropCycler?[i].Color ?? Theme.CycleColors[i % Theme.CycleColors.Length];
            var seriesColor = series.GetType().GetProperty("Color")?.GetValue(series) as Color?;
            entries.Add(new(series.Label, seriesColor ?? cycleColor, series, i));
        }

        if (entries.Count == 0) return;

        // Phase B.3 — layout computation moved to LegendLayoutCalculator (was
        // duplicated verbatim here AND in ComputeLegendBounds below).
        var layout = new LegendRendering.LegendLayoutCalculator(Theme, Ctx)
            .Compute(legend, entries.Select(e => e.Label).ToList(), PlotArea);

        // Open the legend group FIRST — frame, shadow, title, and entries all live inside
        // it as siblings. Phase S (2026-04-19) — the legend-drag script applies a
        // transform="translate(dx,dy)" to this <g class="legend">; the frame rect MUST be
        // inside so it moves with the items it frames. Pre-Phase S the frame was emitted
        // as a sibling of the group, leaving it stranded when the user dragged the legend.
        if (Ctx is SvgRenderContext svgCtxLegendGroup)
            svgCtxLegendGroup.BeginAccessibleGroup("legend", "Chart legend");
        else
            Ctx.BeginGroup("legend");

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
                Ctx.DrawRectangle(new Rect(layout.BoxX + 3, layout.BoxY + 3, layout.BoxWidth, layout.BoxHeight), shadowColor, null, 0);
            }

            // FancyBox: rounded corners are expressed via SVG rx/ry — DrawRectangle doesn't expose
            // corner radius, so we use an SVG comment/group attribute via the context when available.
            // For now render normally; FancyBox is a visual hint recognised by advanced renderers.
            Ctx.DrawRectangle(new Rect(layout.BoxX, layout.BoxY, layout.BoxWidth, layout.BoxHeight), bgColor, edgeColor, 0.5);
        }

        // Title
        if (!string.IsNullOrEmpty(legend.Title))
        {
            Ctx.DrawText(legend.Title,
                new Point(layout.BoxX + layout.Padding + layout.TotalContentWidth / 2, layout.BoxY + layout.Padding + layout.TitleFont.Size),
                layout.TitleFont, TextAlignment.Center);
        }

        LegendBounds.Clear();
        var svgCtxLegend = Axes.EnableInteractiveAttributes ? Ctx as SvgRenderContext : null;
        for (int i = 0; i < entries.Count; i++)
        {
            int row = i / layout.NCols;
            int col = i % layout.NCols;
            var entry = entries[i];

            double colX = layout.ColumnX(col);
            double entryY = layout.EntryY(row);

            // Capture hit-test bounds for this legend entry (swatch + label)
            LegendBounds.Add(new LegendItemBounds(entry.OriginalIndex, new Rect(colX, entryY, layout.ItemWidth(col), layout.LineHeight)));

            svgCtxLegend?.BeginLegendItemGroup(i, entry.Label);

            // Swatch dispatch: line segment for line-type series, marker for scatter,
            // line+caps for error bar, filled rectangle for patches (bar/hist/area/violin/pie).
            DrawLegendSwatch(entry.Series, colX, entryY, layout.SwatchWidth, layout.SwatchHeight, entry.Color);

            // Text anchor sits just past the swatch.
            var textPoint = new Point(colX + layout.SwatchWidth + layout.SwatchGap, entryY + layout.SwatchHeight - 1);
            if (MathTextParser.ContainsMath(entry.Label))
                Ctx.DrawRichText(MathTextParser.Parse(entry.Label), textPoint, layout.Font, TextAlignment.Left);
            else
                Ctx.DrawText(entry.Label, textPoint, layout.Font, TextAlignment.Left);
            if (svgCtxLegend is not null) Ctx.EndGroup();
        }

        Ctx.EndGroup();
    }

    /// <summary>Computes legend item bounds without rendering any visuals. Called by
    /// <see cref="ChartRenderer.ComputeLayout"/> to populate <see cref="LayoutResult.LegendItems"/>
    /// for interactive hit-testing.</summary>
    internal void ComputeLegendBounds()
    {
        LegendBounds.Clear();
        if (!Axes.Legend.Visible) return;

        var legend = Axes.Legend;
        var entries = new List<LegendEntryIndex>();
        for (int i = 0; i < Axes.Series.Count; i++)
        {
            var series = Axes.Series[i];
            if (string.IsNullOrEmpty(series.Label)) continue;
            entries.Add(new(series.Label, i));
        }

        if (entries.Count == 0) return;

        // Phase B.3 — identical layout calculation to RenderLegend, now sharing
        // the single LegendLayoutCalculator instead of duplicating ~45 lines of math.
        var layout = new LegendRendering.LegendLayoutCalculator(Theme, Ctx)
            .Compute(legend, entries.Select(e => e.Label).ToList(), PlotArea);

        for (int i = 0; i < entries.Count; i++)
        {
            int row = i / layout.NCols;
            int col = i % layout.NCols;
            double colX = layout.ColumnX(col);
            double entryY = layout.EntryY(row);
            LegendBounds.Add(new LegendItemBounds(entries[i].OriginalIndex, new Rect(colX, entryY, layout.ItemWidth(col), layout.LineHeight)));
        }
    }

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

    /// <summary>Renders a color bar gradient alongside the plot area if configured.
    /// Delegates to <see cref="ColorBarRendering.ColorBarRendererFactory"/> which selects
    /// <see cref="ColorBarRendering.HorizontalColorBarRenderer"/> or
    /// <see cref="ColorBarRendering.VerticalColorBarRenderer"/> by orientation
    /// (Phase B.1.e of the strict-90 floor plan, 2026-04-20 — replaced the inline
    /// 150-line god-method with proper polymorphic dispatch).</summary>
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

        if (Ctx is SvgRenderContext svgCtxColorBar)
            svgCtxColorBar.BeginAccessibleGroup("colorbar", "Color bar");
        else
            Ctx.BeginGroup("colorbar");

        ColorBarRendering.ColorBarRendererFactory
            .Create(cb, colorMap, min, max, PlotArea, Ctx, Theme)
            .Render();

        Ctx.EndGroup();
    }

    /// <summary>Renders the axes title above the plot area.</summary>
    protected void RenderTitle()
    {
        if (Axes.Title is null) return;
        var baseFont = TitleFont();
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
    // All themed fonts delegate to ThemedFontProvider, the single source of truth shared
    // with ConstrainedLayoutEngine + LegendMeasurer. These thin protected wrappers exist
    // only so existing parameterless call sites continue to compile; add a new font role
    // in ThemedFontProvider, not here.

    /// <summary>Creates a title font from the theme (axes title — 2 pt larger than labels).</summary>
    protected Font TitleFont() => ThemedFontProvider.TitleFont(Theme);

    /// <summary>Creates a tick label font from the theme (same size as axis labels — matches matplotlib).</summary>
    protected Font TickFont() => ThemedFontProvider.TickFont(Theme);

    /// <summary>Creates an axis label font from the theme.</summary>
    protected Font LabelFont() => ThemedFontProvider.LabelFont(Theme);

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
    internal static Func<double, string> BuildUniformTickFormatter(double[] ticks)
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

    /// <summary>Internal wrapper for <see cref="ComputeTickValues(double, double, int)"/> so
    /// <c>CartesianParts</c> extracted classes can reuse the default nice-number algorithm.</summary>
    internal static double[] ComputeTickValuesInternal(double min, double max, int targetCount = 8)
        => ComputeTickValues(min, max, targetCount);

    /// <summary>Computes tick values respecting any <see cref="Axis.TickLocator"/> or
    /// <see cref="TickConfig.Spacing"/>, falling back to the default nice-number algorithm.
    /// Scales the target tick count to <paramref name="plotPixels"/> so short plot areas
    /// (sparklines, multi-panel dashboards) don't cram labels.</summary>
    /// <param name="min">Visible range min.</param>
    /// <param name="max">Visible range max.</param>
    /// <param name="axis">Axis config (locator / spacing / formatter).</param>
    /// <param name="plotPixels">Plot area length in pixels along the tick direction.</param>
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

    private readonly record struct RenderLegendEntry(
        string Label, Color Color, Models.Series.ISeries Series, int OriginalIndex);

    private readonly record struct LegendEntryIndex(string Label, int OriginalIndex);
}
