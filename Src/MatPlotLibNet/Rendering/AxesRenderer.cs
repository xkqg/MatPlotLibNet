// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
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

    /// <summary>How far the X-axis tick labels, numbers or category names, reach below the mark and the pad as they are
    /// drawn: the tallest label upright, the farthest-reaching one turned (<see cref="AxisTickExtensions.LabelExtent"/>).
    /// Set by subclasses during tick rendering; consumed by <see cref="RenderAxisLabels"/> to place the X-axis title clear
    /// of the tick labels (<see cref="AxisTickExtensions.BottomAxisLabelOffset"/>).</summary>
    protected double MeasuredXTickMaxHeight { get; set; }

    /// <summary>The band drawn above the top spine below the axes title: the mirrored X tick labels
    /// (<see cref="AxisTickExtensions.TickLabelBand"/>) or the secondary X axis that owns the edge (its tick labels, then
    /// its title). Set by subclasses while they draw it; <see cref="RenderTitle"/> raises the title above it. 0 when
    /// nothing was drawn there.</summary>
    protected double MeasuredTopTickBand { get; set; }

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

    /// <summary>Whether this renderer draws a series' name at the end of its line when the series asks for it
    /// (<see cref="IHasLabelPlacement"/>). Only then does such a series leave the legend; a renderer that draws no
    /// end labels keeps the legend entry, so the name is never lost. <see langword="false"/> here; the Cartesian
    /// renderer, which owns the end-label drawing, overrides it.</summary>
    internal virtual bool DrawsDirectLabels => false;

    /// <summary>What is drawn past the top spine below the title (the mirrored X tick labels or the secondary X axis) and
    /// past the right spine by the mirrored Y tick labels, measured before anything is drawn, so the layout can reserve
    /// it. Nothing here: only the Cartesian renderer draws mirrored ticks and a secondary X axis.</summary>
    /// <returns>The two bands; both 0 here.</returns>
    internal virtual Layout.EdgeBands MeasureEdgeBands() => default;

    /// <summary>Draws what belongs to the primary series at <paramref name="seriesIndex"/> beyond its own marks, while
    /// that series' group is still open, so it is shown, hidden and highlighted together with the series. Called by
    /// every series pass after the series is drawn. Nothing here; the Cartesian renderer draws the series' end label.</summary>
    /// <param name="seriesIndex">The series' index in <see cref="Models.Axes.Series"/>.</param>
    internal virtual void DrawInsideSeriesGroup(int seriesIndex) { }

    /// <summary>The transform the primary series were drawn with in the last <see cref="Render"/>: the plot area and the
    /// range the axes actually show (after autoscale, margins and limits). <see langword="null"/> before a render and
    /// for axes that draw no Cartesian range (polar, 3-D); the Cartesian renderer sets it.</summary>
    internal DataTransform? DrawnTransform { get; private protected set; }

    /// <summary>The transform <see cref="Render"/> draws the primary series with for the axes as they are now, computed
    /// without drawing or measuring anything: the plot area, the range after autoscale, margins and limits, and the
    /// axes' scales. The interaction layout (<see cref="Interaction.ChartLayout"/>) converts the pointer with it, so a
    /// control and the picture share one rule. <see langword="null"/> here, for axes that draw no Cartesian range
    /// (polar, 3-D); the Cartesian renderer overrides it.</summary>
    internal virtual DataTransform? ComputeViewTransform() => null;

    /// <summary>Writes the axes' view for the server-interaction script, when the figure asked for one
    /// (<see cref="Models.Axes.ServerViewIndex"/>) and the surface is SVG: the axes' index, its plot area and the range
    /// it drew (<see cref="SvgRenderContext.WriteAxesView"/>). Called once the axes is drawn.</summary>
    internal void WriteServerView()
    {
        if (Axes.ServerViewIndex is int index && Ctx is SvgRenderContext svg)
            svg.WriteAxesView(index, PlotArea, DrawnTransform);
    }

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
        // Inset axes draw like any Cartesian axes but have no right margin of their own (CartesianInsetAxesRenderer).
        [CoordinateSystem.Cartesian] = (a, p, c, t) => a.InsetBounds is null
            ? new CartesianAxesRenderer(a, p, c, t)
            : new CartesianInsetAxesRenderer(a, p, c, t),
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
        // Stable sort by ZOrder so fills (ZOrder=-1) render behind other series (ZOrder=0).
        var colorSlots = Axes.ColorSlots();
        var ordered = Axes.Series.Select((s, i) => (s, i)).OrderBy(t => t.s.ZOrder);
        foreach (var (series, i) in ordered)
        {
            if (!series.Visible) continue;
            var slot = Theme.SeriesSlot(colorSlots[Axes.SlotIndex(SeriesAxisPair.Primary, i)]);
            bool openedGroup = Ctx.BeginSeriesGroup(Axes, series, i);
            var renderer = new SvgSeriesRenderer(
                new DataTransform(0, 1, 0, 1, PlotArea), Ctx, slot.CycleColor, Theme, slot.CycledProperties,
                Axes.EnableTooltips, PlotArea);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            DrawInsideSeriesGroup(i);
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
        bool emit3D = Axes.Emit3DVertexData;
        var colorSlots = Axes.ColorSlots();
        var ordered = Axes.Series.Select((s, i) => (s, i)).OrderBy(t => t.s.ZOrder);
        foreach (var (series, i) in ordered)
        {
            if (!series.Visible) continue;
            var slot = Theme.SeriesSlot(colorSlots[Axes.SlotIndex(SeriesAxisPair.Primary, i)]);
            bool openedGroup = Ctx.BeginSeriesGroup(Axes, series, i);
            var renderer = new SvgSeriesRenderer(
                new DataTransform(0, 1, 0, 1, PlotArea), Ctx, slot.CycleColor, Theme, slot.CycledProperties,
                Axes.EnableTooltips, PlotArea, projection, lightSource, emit3D, depthQueue);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            DrawInsideSeriesGroup(i);
            if (openedGroup) Ctx.EndGroup();
        }
    }

    /// <summary>Renders all series with a specific DataTransform.</summary>
    /// <param name="transform">The coordinate transform mapping data space to pixel space.</param>
    protected void RenderSeries(DataTransform transform)
    {
        // Bar grouping: when several non-stacked bar series share the same categories, they step aside for
        // each other so the bars sit side by side, the way matplotlib's grouped bars do. Each ORIENTATION is
        // grouped among its own — a vertical bar offset against a horizontal one compares nothing — and until
        // 1.17.1 the horizontal ones were skipped altogether, so two of them were drawn exactly on top of each
        // other with the last one painted over the rest. A bar with a foot of its own (stacked, or lifted onto a
        // StackBaseline) is not grouped. The offsets are render state: every bar starts from none on every render,
        // so a bar that left a group (a stacked render after a grouped one) does not keep the place it had in it.
        foreach (var bar in Axes.Series.OfType<Models.Series.BarSeries>())
        {
            bar.BarGroupOffset = 0;
            bar.BarGroupWidth = null; // use series.BarWidth
        }

        foreach (var orientation in new[] { Models.Series.BarOrientation.Vertical, Models.Series.BarOrientation.Horizontal })
        {
            var barGroups = Axes.Series
                .OfType<Models.Series.BarSeries>()
                .Where(b => b.Orientation == orientation && b.Bottoms is null)
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
        }

        // Stable sort by ZOrder so fills (ZOrder=-1) render behind other series (ZOrder=0).
        var colorSlots = Axes.ColorSlots();
        var ordered = Axes.Series.Select((s, i) => (s, i)).OrderBy(t => t.s.ZOrder);
        foreach (var (series, i) in ordered)
        {
            if (!series.Visible) continue;
            var slot = Theme.SeriesSlot(colorSlots[Axes.SlotIndex(SeriesAxisPair.Primary, i)]);
            bool openedGroup = Ctx.BeginSeriesGroup(Axes, series, i, transform);
            bool clipped = Ctx.BeginSeriesClip(Axes, PlotArea);
            var renderer = new SvgSeriesRenderer(transform, Ctx, slot.CycleColor, Theme, slot.CycledProperties,
                Axes.EnableTooltips, PlotArea);
            var area = new RenderArea(PlotArea, Ctx);
            series.Accept(renderer, area);
            if (clipped) Ctx.PopClip();
            DrawInsideSeriesGroup(i);
            if (openedGroup) Ctx.EndGroup();
        }
    }

    /// <summary>Computes the display label for a legend entry. When <paramref name="legendValues"/>
    /// is <see langword="true"/> and the series is an <see cref="Models.Series.XYSeries"/> with a point that can be
    /// drawn, appends " = {y:F2}" (InvariantCulture) of the end of its line: the last such point
    /// (<see cref="MissingValueExtensions.LastDrawableIndex"/>, the rule the end-of-line label stands by), so a trailing
    /// gap never writes NaN.</summary>
    internal static string BuildLegendDisplayLabel(string baseLabel, Models.Series.ISeries series, bool legendValues)
    {
        if (!legendValues) return baseLabel;
        if (series is Models.Series.XYSeries xySeries && xySeries.XData.LastDrawableIndex(xySeries.YData) is var end and >= 0)
            return WithLegendValue(baseLabel, xySeries.YData[end]);
        return baseLabel;
    }

    /// <summary>The legend-values format: <paramref name="baseLabel"/> followed by " = " and
    /// <paramref name="value"/> with two decimals in the invariant culture. The legend and the end-of-line labels
    /// (<see cref="Layout.DirectLabelMetrics"/>) both write the value this way.</summary>
    internal static string WithLegendValue(string baseLabel, double value) =>
        baseLabel + " = " + value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>The slot a legend entry's series holds, decided by the same owner the series passes use
    /// (<see cref="SeriesSlotExtensions"/>): the swatch wears the colour, dash pattern, marker and width the series is
    /// drawn with. Reads the entry's COLOUR slot, not its identity one, so a series sharing a
    /// <see cref="Models.Series.ChartSeries.ColorGroup"/> shows the swatch its group is actually drawn in.</summary>
    private SeriesSlot LegendEntrySlot(LegendRendering.LegendEntry entry) => Theme.SeriesSlot(entry.ColorSlotIndex);

    /// <summary>Renders the legend if any series have labels.</summary>
    protected void RenderLegend()
    {
        if (!Axes.Legend.Visible) return;

        var legend = Axes.Legend;
        var entries = Axes.LegendEntries(DrawsDirectLabels);

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
                Ctx.DrawRectangle(new Rect(layout.BoxX + 3, layout.BoxY + 3, layout.BoxWidth, layout.BoxHeight), new ShapeStyle(shadowColor, null, 0));
            }

            // FancyBox: rounded corners are expressed via SVG rx/ry — DrawRectangle doesn't expose
            // corner radius, so we use an SVG comment/group attribute via the context when available.
            // For now render normally; FancyBox is a visual hint recognised by advanced renderers.
            Ctx.DrawRectangle(new Rect(layout.BoxX, layout.BoxY, layout.BoxWidth, layout.BoxHeight), new ShapeStyle(bgColor, edgeColor, 0.5));
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

            // The group carries the SERIES index (data-legend-index maps straight onto data-series-index), never the
            // entry's position in the legend: an unlabelled series ahead of this one has no entry but still has an index.
            svgCtxLegend?.BeginLegendItemGroup(entry.OriginalIndex, entry.Label);

            // Swatch dispatch: line segment for line-type series, marker for scatter,
            // line+caps for error bar, filled rectangle for patches (bar/hist/area/violin/pie).
            DrawLegendSwatch(entry.Series, colX, entryY, layout.SwatchWidth, layout.SwatchHeight, LegendEntrySlot(entry));

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
        var entries = Axes.LegendEntries(DrawsDirectLabels);

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
    private void DrawLegendSwatch(Models.Series.ISeries series, double x, double y, double wMax, double h, SeriesSlot slot)
    {
        double midY = y + h / 2;
        var color = slot.ColorOf(series);

        switch (series)
        {
            case Models.Series.LineSeries ls:
            {
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), slot.StrokeOf(color, ls.LineWidth, ls.LineStyle));
                if (slot.LineMarkerOf(ls.Marker) is { } marker and not MarkerStyle.None)
                    DrawLegendMarker(x + wMax / 2, midY, marker, ls.MarkerSize, color);
                break;
            }
            case Models.Series.SignalSeries ss:
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), slot.StrokeOf(color, ss.LineWidth, ss.LineStyle));
                break;
            case Models.Series.SignalXYSeries sxy:
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), slot.StrokeOf(color, sxy.LineWidth, sxy.LineStyle));
                break;
            case Models.Series.SparklineSeries sp:
                // solid, as the sparkline itself: the slot decides only the width
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), new StrokeStyle(color, slot.LineWidthOf(sp.LineWidth), LineStyle.Solid));
                break;
            case Models.Series.EcdfSeries ec:
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), slot.StrokeOf(color, ec.LineWidth, ec.LineStyle));
                break;
            case Models.Series.RegressionSeries rs:
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY),
                    slot.StrokeOf(color, rs.LineWidth, rs.LineStyle, Models.Series.RegressionSeries.RestingLineWidth));
                break;
            case Models.Series.StepSeries stp:
            {
                Ctx.DrawLine(new Point(x, midY), new Point(x + wMax, midY), slot.StrokeOf(color, stp.LineWidth, stp.LineStyle));
                if (slot.LineMarkerOf(stp.Marker) is { } marker and not MarkerStyle.None)
                    DrawLegendMarker(x + wMax / 2, midY, marker, Models.Series.LineSeries.RestingMarkerSize, color);
                break;
            }
            case Models.Series.ScatterSeries sc:
            {
                // matplotlib legend marker size ≈ series point size, scaled down slightly.
                double radius = Math.Min(h / 2 - 1, Math.Sqrt(sc.MarkerSize / Math.PI) * (100.0 / 72.0));
                DrawLegendMarker(x + h / 2, midY, slot.ScatterMarkerOf(sc.Marker), radius * radius * Math.PI, color);
                break;
            }
            case Models.Series.ErrorBarSeries eb:
            {
                double width = slot.LineWidthOf(eb.LineWidth);
                Ctx.DrawLine(new Point(x + 2, midY), new Point(x + wMax - 2, midY), new StrokeStyle(color, width, LineStyle.Solid));
                Ctx.DrawLine(new Point(x + 2, midY - 3), new Point(x + 2, midY + 3), new StrokeStyle(color, width, LineStyle.Solid));
                Ctx.DrawLine(new Point(x + wMax - 2, midY - 3), new Point(x + wMax - 2, midY + 3), new StrokeStyle(color, width, LineStyle.Solid));
                break;
            }
            default:
            {
                // Patch-like: bar/hist/area/violin/pie/etc → filled RECTANGLE (matplotlib uses
                // handlelength × handleheight; our wMax carries handlelength, h carries handleheight).
                // Previous `Math.Min(wMax, h)` clamped it to a square which matplotlib never does.
                Ctx.DrawRectangle(new Rect(x, y, wMax, h), new ShapeStyle(color, null, 0));
                break;
            }
        }
    }

    /// <summary>Draws a single legend marker at (cx, cy) in the shape the plot draws it in: the one owner of marker
    /// shapes (<see cref="MarkerRenderer"/>) draws it, so a triangle, a star or a cross in the plot is the same in the
    /// legend, and <see cref="MarkerStyle.None"/> draws nothing.</summary>
    private void DrawLegendMarker(double cx, double cy, MarkerStyle marker, double size_pt2, Color color)
    {
        double radius = Math.Sqrt(Math.Max(size_pt2, 1) / Math.PI) * (100.0 / 72.0);
        MarkerRenderer.Draw(Ctx, marker, new Point(cx, cy), 2 * radius, color, null, 0);
    }

    /// <summary>Renders a color bar gradient alongside the plot area if configured.
    /// Delegates to <see cref="ColorBarRendering.ColorBarRendererFactory"/> which selects
    /// <see cref="ColorBarRendering.HorizontalColorBarRenderer"/> or
    /// <see cref="ColorBarRendering.VerticalColorBarRenderer"/> by orientation.</summary>
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

        // Horizontal alignment based on TitleLoc; Pad shifts the Y position when set; the title stands above what is
        // drawn above the top spine (the mirrored X tick labels or the secondary X axis) when there is anything
        double padOffset = Axes.TitleStyle?.Pad ?? 0;
        double titleY = PlotArea.Y - 8 - padOffset - MeasuredTopTickBand;
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
            // The x-label baseline sits a clear gap below the tick labels drawn, numbers or category names alike: the mark,
            // the pad, the tallest label measured while drawing them, the gap and the title's ascent
            // (TickConfig.BottomAxisLabelOffset, the mirror of the secondary X title's rule); the fixed 35 px when no tick
            // label was drawn (hidden ticks, axis-less series).
            double dynamicOffset = Axes.XAxis.MajorTicks.BottomAxisLabelOffset(MeasuredXTickMaxHeight, font.Size);
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
            // Compute y-label x dynamically: tick mark + pad + widest rendered tick label + gap, the rule the
            // secondary Y axis shares. Falls back to the fixed 45-px offset when no tick widths were measured
            // (non-Cartesian renderers or empty axes).
            double dynamicOffset = Axes.YAxis.MajorTicks.AxisLabelOffset(MeasuredYTickMaxWidth);
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

    /// <summary>Expands min/max to include every value in the data array an axis can place: a missing value (NaN) and an
    /// infinity are left out, the rule every series ranges by.</summary>
    /// <param name="data">The data values to scan.</param>
    /// <param name="min">The current minimum; updated if any drawable value is smaller.</param>
    /// <param name="max">The current maximum; updated if any drawable value is larger.</param>
    protected static void UpdateRange(double[] data, ref double min, ref double max)
    {
        if (data.DrawableRange() is not { } range) return;
        if (range.Lo < min) min = range.Lo;
        if (range.Hi > max) max = range.Hi;
    }

    /// <summary>Computes aesthetically-spaced tick values using the default nice-number algorithm.</summary>
    /// <param name="min">The minimum value of the visible data range.</param>
    /// <param name="max">The maximum value of the visible data range.</param>
    /// <param name="targetCount">The desired number of tick intervals; the algorithm snaps to a nearby nice number. Defaults to 8 to match matplotlib's <c>MaxNLocator(nbins='auto')</c>.</param>
    protected static double[] ComputeTickValues(double min, double max, int targetCount = 8)
        => new TickLocators.AutoLocator(targetCount).LocateInterval(min, max);

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
        if (axis.TickLocator is not null) return axis.TickLocator.LocateInterval(min, max);
        if (axis.MajorTicks.Spacing.HasValue)
            return new TickLocators.MultipleLocator(axis.MajorTicks.Spacing.Value).LocateInterval(min, max);
        // Preserve the default 8-tick density for normal-sized plots so existing fidelity
        // tests stay green; only downscale for short rows where labels would otherwise overlap.
        int target = plotPixels >= 240 ? 8 : Math.Clamp((int)Math.Round(plotPixels / 30), 2, 6);
        return ComputeTickValues(min, max, target);
    }
}
