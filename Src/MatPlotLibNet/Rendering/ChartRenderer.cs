// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.Frames;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Rendering.TextMeasurement;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders a complete <see cref="Figure"/> including subplots, axes, grids, and series onto an <see cref="IRenderContext"/>.</summary>
public sealed class ChartRenderer : IChartRenderer, IFrameChartRenderer
{
    // Sentinel used to detect when the caller has not explicitly set a spacing on the figure.
    private static readonly SubPlotSpacing DefaultFigureSpacing = new();

    /// <summary>
    /// Resolves the effective <see cref="SubPlotSpacing"/> for a figure.
    /// Priority: figure.Spacing (if overridden by user) → theme.DefaultSpacing → library default.
    /// Fractional values are expanded to absolute pixels using the figure dimensions and the grid the subplots stand on: the gap
    /// of a theme that names it as a share of the average axes (matplotlib's <c>wspace</c> and <c>hspace</c>) is a share of the
    /// cell the grid gives.
    /// </summary>
    private static SubPlotSpacing ResolveSpacing(Figure figure, FigureGrid grid) => Resolved(figure.Spacing, figure, grid);

    /// <summary>The spacing the figure's subplot parameters give, in pixels: its own margins and gaps without a layout engine, resolved
    /// as <see cref="ResolveSpacing"/> resolves them (the theme's spacing where the figure sets none), which is where matplotlib makes an
    /// axes before a tight or constrained layout moves it.</summary>
    private static SubPlotSpacing SubplotParameters(Figure figure, FigureGrid grid) =>
        Resolved(figure.Spacing with { TightLayout = false, ConstrainedLayout = false, LayoutRect = null }, figure, grid);

    /// <summary><paramref name="spacing"/> in pixels: the theme's spacing in its place when it is the library's default
    /// (<see cref="Styling.Theme.DefaultSpacing"/>), shares of the figure and of the grid's cells resolved.</summary>
    private static SubPlotSpacing Resolved(SubPlotSpacing spacing, Figure figure, FigureGrid grid)
    {
        var sp = spacing == DefaultFigureSpacing && figure.Theme.DefaultSpacing is not null
            ? figure.Theme.DefaultSpacing
            : spacing;
        return sp.Resolve(figure.Width, figure.Height, grid.Rows, grid.Cols);
    }

    /// <inheritdoc />
    public void Render(Figure figure, IRenderContext ctx) => Draw(figure, new DirectFrameTarget(ctx), legends: null);

    /// <summary>Draws onto the target in one pass (<see cref="Draw(Figure, FrameTarget, List{IReadOnlyList{LegendItemBounds}}?)"/>),
    /// asking for no layout, so a target that keeps what it drew hands an unchanged subplot out.</summary>
    void IFrameChartRenderer.Draw(Figure figure, FrameTarget target) => Draw(figure, target, legends: null);

    /// <summary>Paints onto the target in one pass (<see cref="RenderAndLayOut(Figure, FrameTarget)"/>), so a target that keeps
    /// what it drew hands an unchanged subplot out.</summary>
    LayoutResult IFrameChartRenderer.Paint(Figure figure, FrameTarget target) => RenderAndLayOut(figure, target);

    /// <summary>Draws <paramref name="figure"/> onto <paramref name="target"/> and returns the layout of the draw, the way a native
    /// control paints: one frame (<see cref="FrameOf"/>) for the drawing and for the layout the control hit-tests with, so nothing is
    /// measured twice and the two cannot disagree. The layout is the plot areas of the frame and the legend bounds of every subplot
    /// as it was drawn or handed out, so a handed-out subplot builds no renderer for its layout either.</summary>
    /// <param name="figure">The figure to draw.</param>
    /// <param name="target">What the draw goes onto.</param>
    /// <returns>The layout the frame was drawn with.</returns>
    internal LayoutResult RenderAndLayOut(Figure figure, FrameTarget target)
    {
        // A subplot handed out builds no renderer, and the layout of every subplot is the one its draw had.
        var legends = new List<IReadOnlyList<LegendItemBounds>>(figure.SubPlots.Count);
        var frame = Draw(figure, target, legends);
        return new LayoutResult(frame.PlotAreas, legends);
    }

    /// <summary>The one drawing sequence, for every target: the frame, the background and the title, what lies under the
    /// subplots, each subplot one after another, what lies above them. The subplots are drawn in turn: on a live wall (a trend, a
    /// tile row, stacked panels and a 3-D surface, redrawn on their own cadences) handing them to the thread pool cost 21.2 ms of
    /// CPU per round against 14.7 ms drawn in turn, and was slower on the clock as well (14.5 against 12.9 ms). A target that
    /// keeps what it drew hands an unchanged subplot out instead of drawing it (<see cref="KeepingFrameTarget{TLayer}"/>).</summary>
    /// <param name="figure">The figure to draw.</param>
    /// <param name="target">What the draw goes onto.</param>
    /// <param name="legends">Receives the legend bounds of each subplot in order, or <see langword="null"/> when no layout is asked for.</param>
    /// <returns>The frame the draw was laid out with.</returns>
    internal FigureFrame Draw(Figure figure, FrameTarget target, List<IReadOnlyList<LegendItemBounds>>? legends)
    {
        var frame = FrameOf(figure, target.Background);
        RenderBackground(figure, target.Background);
        RenderFigureUnderlays(figure, target.Background);

        // A figure without subplots lays out no plot area and draws none, but still draws what it carries itself.
        var theme = figure.Theme;
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var axes = figure.SubPlots[i];
            var area = frame.PlotAreas[i];
            var legend = target.DrawAxes(new AxesDrawRequest(figure, i, area),
                ctx => RenderAxes(figure, axes, new RenderPass(area, ctx, theme), frame.Ranges).LegendBounds);
            legends?.Add(legend);
        }

        RenderFigureOverlays(figure, frame, target.Overlay);
        return frame;
    }

    /// <summary>The layout of a draw of <paramref name="figure"/>, measured with <paramref name="measureCtx"/>: the spacing (the
    /// tight or constrained layout when the figure asks for one), the height of the title and the plot area of every subplot. The
    /// one place they are decided, for the drawing and for the hit-testing alike. The data range of each subplot is worked out
    /// here at most once, the first time the layout or the drawing asks for it (<see cref="FigureRanges"/>).</summary>
    /// <param name="figure">The figure to lay out.</param>
    /// <param name="measureCtx">The surface its text is measured with.</param>
    /// <returns>The frame: the spacing, the title's height and the plot area of every subplot.</returns>
    internal FigureFrame FrameOf(Figure figure, IRenderContext measureCtx)
    {
        // The layout is the first stage of a draw: the surface measures by the looks of the theme from here on, as it draws, and reads
        // the trees of the math texts the figure kept from its draw before (a text that draw did not read is let go of here).
        figure.MathTexts.StartDraw();
        measureCtx.UseStageOf(figure.MathTexts, figure.Theme);
        var ranges = new FigureRanges(figure);
        var grid = new FigureGrid(figure);
        // The figure's legends stand in the rectangle that is the figure, whatever the spacing, so they are placed first, once:
        // the layout keeps room by their boxes and the overlay draws them where they stand.
        var legends = figure.PlacedLegends(measureCtx);
        // A layout engine moves the axes from where the subplot parameters put them, where matplotlib made a table whose rows keep their
        // share of the axes' height: the layout and the drawing stretch those rows by how far the axes moved (FigureRanges.StretchOf).
        if (ranges.KeepsRoom && SeriesRenderers.TableGrid.AnyStretchIn(figure))
        {
            var made = SubplotParameters(figure, grid);
            ranges.MadeOn = grid.PlotRects(made.MarginTop, made);
        }
        var spacing = PrepareSpacing(figure, measureCtx, ranges, grid, legends);
        double titleHeight = 0;
        double plotAreaTop = spacing.MarginTop;
        if (figure.Title is not null)
        {
            // Measured, not assumed: a fixed 30 px once cut multi-line and oversized titles short.
            titleHeight = figure.TitleHeight(measureCtx);
            // The layout engine has already widened MarginTop for the suptitle, so the top margin already holds the band;
            // the first row moves down further only when the margin is smaller than the title needs (a hard MarginTop).
            double needed = figure.TitleBand(titleHeight);
            if (spacing.MarginTop < needed) plotAreaTop = needed;
        }

        // A colour bar that takes its room from its axes stands in the axes' cell: the draw's renderers read it beside the ranges.
        if (grid.AnyBarTakesRoom) ranges.Cells = grid.Cells(plotAreaTop, spacing);

        // The figure's own bar stands by the box of every plot (or takes its room from the box of every cell): its drawing and a legend
        // outside an axes on its edge, which stands past it, read where it stands from the ranges.
        var plotAreas = grid.PlotRects(plotAreaTop, spacing);
        if (figure.FigureColorBar is { Visible: true })
            ranges.FigureBar = FigureBarStand.Of(figure, grid, plotAreas, spacing, grid.FigureBarCell(plotAreaTop, spacing), ranges);

        return new FigureFrame(spacing, titleHeight, plotAreaTop, plotAreas, ranges, legends);
    }

    /// <summary>Draws what the figure carries under its subplots: after the background and the title, before the first
    /// subplot (z 0). The one site for it, called by <see cref="Render"/> and by <see cref="Transforms.SvgTransform.Render"/>
    /// alike, also for a figure with no subplot. The figure's own images (<see cref="Figure.Images"/>) are drawn here, in the
    /// order they were added, each later one over the earlier, at their place on the figure: each cell of its data is one rectangle of
    /// one pixel, no resampling, its first row on top; an image with no place on the figure draws nothing. A cell that would be painted
    /// transparent (a missing value under a colour map with no bad colour) is left out, so a masked pixel costs no element.</summary>
    /// <param name="figure">The figure being drawn.</param>
    /// <param name="ctx">The surface it is drawn on.</param>
    internal void RenderFigureUnderlays(Figure figure, IRenderContext ctx)
    {
        foreach (var image in figure.Images)
        {
            if (image.BoundsOn(figure.Height) is { } bounds) image.Cells(figure.Theme).Draw(ctx, image.Data, bounds, skipTransparentCells: true);
        }
    }

    /// <summary>Draws what the figure carries above its subplots, after every subplot, in this order: the figure's colour
    /// bar, then its texts in the order they were added, each later one over the earlier (at its place, in the font a placed text takes,
    /// <see cref="ThemeFontExtensions.DataTextFont(Styling.Theme, Styling.Font?, Styling.Color?)"/>, standing against its point as its
    /// alignments say and turned about it; a text with no place on the figure draws nothing), then its legends. The one site for
    /// these, called by <see cref="Render"/> and by <see cref="Transforms.SvgTransform.Render"/> alike, also for a figure with
    /// no subplot, so the SVG and every raster or native picture carry the same overlays at the same places.</summary>
    /// <param name="figure">The figure being drawn.</param>
    /// <param name="frame">The layout of this draw (<see cref="FrameOf"/>): the plot areas, empty for a figure with no subplot,
    /// the colour bar stands beside them or in its slot; the spacing the subplots were laid out with; and the figure's legends
    /// where the layout placed them.</param>
    /// <param name="ctx">The surface it is drawn on.</param>
    internal void RenderFigureOverlays(Figure figure, FigureFrame frame, IRenderContext ctx)
    {
        ctx.UseStageOf(figure.MathTexts, figure.Theme);
        ctx.AtFullOpacity();
        if (figure.FigureColorBar is { Visible: true } cb)
            RenderFigureColorBar(figure, frame.PlotAreas, cb, ctx, frame.Ranges.FigureBar);

        foreach (var text in figure.Texts)
        {
            if (text.AnchorOn(figure.Width, figure.Height) is not { } anchor) continue;
            ctx.Draw(TextLabel.Of(text.Text), figure.Theme.DataTextFont(text.Font, text.Color),
                new TextPlacement(anchor, text.Alignment) { Vertical = text.VerticalAlignment, Rotation = text.Rotation });
        }

        // Each legend of the figure where the frame placed it, by the one legend drawer, in the order they were added; static: no
        // row carries the series index a legend toggle reads.
        foreach (var legend in frame.Legends)
            new LegendRenderer(legend.Legend, figure.Theme, ctx).Draw(legend.Entries, legend.Layout, interactive: false);
    }

    /// <summary>
    /// Resolves the effective <see cref="SubPlotSpacing"/> for rendering <paramref name="figure"/>.
    /// When <see cref="SubPlotSpacing.TightLayout"/> or <see cref="SubPlotSpacing.ConstrainedLayout"/>
    /// is enabled, runs <see cref="ConstrainedLayoutEngine.Compute"/> to auto-size margins from
    /// measured text extents. Otherwise falls back to the theme's default spacing via
    /// <see cref="ResolveSpacing(Figure, FigureGrid)"/>, widened by <see cref="ConstrainedLayoutEngine.ReserveEdgeBands"/> for what is
    /// drawn past the right and the top spines, for the category names past the left spine and for the X tick labels and
    /// the X axis title below the bottom spine.
    /// </summary>
    /// <remarks>
    /// Pure function — does NOT mutate <paramref name="figure"/>. The returned spacing is used
    /// LOCALLY by the caller, so repeated renders of the same figure (e.g. saving as both SVG
    /// and PNG) each compute their own spacing without observing each other's side effects.
    /// Both <see cref="Render"/> (serial) and <see cref="Transforms.SvgTransform.Render"/>
    /// (one context per subplot) call this method so SVG and PNG outputs share the same layout.
    /// </remarks>
    /// <param name="figure">The figure to lay out.</param>
    /// <param name="measureCtx">The surface its text is measured with.</param>
    /// <param name="ranges">The data ranges of the draw being laid out, which the layout's renderers read.</param>
    /// <param name="grid">The grid the figure's subplots are laid out on.</param>
    /// <param name="legends">The figure's legends as this draw placed them, whose boxes the constrained layout keeps room for;
    /// <see langword="null"/> places them here.</param>
    /// <returns>The spacing the figure is drawn with.</returns>
    internal SubPlotSpacing PrepareSpacing(Figure figure, IRenderContext measureCtx, FigureRanges ranges, FigureGrid grid,
        IReadOnlyList<PlacedFigureLegend>? legends = null)
    {
        var engine = new ConstrainedLayoutEngine(ranges, grid);
        if (ranges.KeepsRoom)
            return engine.Compute(figure, measureCtx, legends);
        // The default path measures nothing else, but what is drawn past the right spines (the secondary Y axis, the
        // mirrored Y tick labels and the end-of-line labels), past the top spines (the mirrored X tick labels or the
        // secondary X axis), the category names past the left spine and the X tick labels and X axis title below the bottom
        // spine are reserved by the same rule the engine applies in the branch above.
        return engine.ReserveEdgeBands(figure, ResolveSpacing(figure, grid), measureCtx);
    }

    /// <summary>Draws the figure's background and its title, hung from the pad above the band the layout keeps for it: the first stage
    /// of a frame, which starts at full opacity whatever the surface held when it was handed over.</summary>
    /// <param name="figure">The figure being drawn.</param>
    /// <param name="ctx">The surface it is drawn on.</param>
    internal static void RenderBackground(Figure figure, IRenderContext ctx)
    {
        var theme = figure.Theme;

        // Every stage of a frame (this one, each subplot, what lies above the subplots) starts at full opacity, whatever the stage
        // before it or the hand that gave the surface left on it: the guarantee is held here, above the contexts, for drawing the
        // library does not own (a renderer someone registers, a series of their own); within a stage an opacity is put back by its
        // scope (RenderContextScopeExtensions.WithOpacity).
        ctx.AtFullOpacity();
        ctx.UseStageOf(figure.MathTexts, theme);
        ctx.DrawRectangle(new Rect(0, 0, figure.Width, figure.Height), ShapeStyle.Filled(figure.Ground));

        if (figure.Title is not { } title) return;

        // Hung from the pad (matplotlib's suptitle, va='top'): its box fills the band the layout keeps for it.
        ctx.Draw(TextLabel.Of(title), figure.TitleFont,
            new TextPlacement(new Point(figure.Width / 2, SupTitleBand.TopPad), TextAlignment.Center) { Vertical = TextVerticalAlignment.Top });
    }

    /// <inheritdoc />
    public LayoutResult ComputeLayout(Figure figure, IRenderContext measureCtx) => LayoutOf(figure, FrameOf(figure, measureCtx), measureCtx);

    /// <summary>The layout a control hit-tests with for a draw laid out as <paramref name="frame"/>: the plot areas, and the
    /// bounds of every legend item of every subplot.</summary>
    private static LayoutResult LayoutOf(Figure figure, FigureFrame frame, IRenderContext measureCtx)
    {
        var plotAreas = frame.PlotAreas;

        // Compute per-subplot legend item bounds for interactive hit-testing.
        var legendItems = new List<IReadOnlyList<LegendItemBounds>>(plotAreas.Count);
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var axes = figure.SubPlots[i];
            var plotArea = plotAreas.ElementOr(i, new Rect(0, 0, 0, 0));
            var axesRenderer = AxesRenderer.Create(axes, plotArea, measureCtx, figure.Theme, null, frame.Ranges);
            axesRenderer.ComputeLegendBounds();
            legendItems.Add(axesRenderer.LegendBounds.ToList());
        }

        return new LayoutResult(plotAreas, legendItems);
    }

    /// <summary>The plot area of each subplot for a first row at <paramref name="plotAreaTop"/> under <paramref name="spacing"/>,
    /// by the one rule a draw lays its subplots out with (its grid spec, else the grid its axes name,
    /// then each axes' aspect). Without a spacing it takes the figure's own, as set, without the room a draw reserves past the
    /// spines for labels: the plot areas a draw actually uses are the ones <see cref="ComputeLayout"/> gives.</summary>
    /// <param name="figure">The figure whose subplots are placed.</param>
    /// <param name="plotAreaTop">The pixel row the first row of plots starts at.</param>
    /// <param name="spacing">The spacing to place them under; <see langword="null"/> takes the figure's own.</param>
    /// <returns>The plot area of every subplot, in the order the figure holds them.</returns>
    public List<Rect> ComputeSubPlotLayout(Figure figure, double plotAreaTop, SubPlotSpacing? spacing = null)
    {
        var grid = new FigureGrid(figure);
        return grid.PlotRects(plotAreaTop, spacing ?? ResolveSpacing(figure, grid));
    }

    /// <summary>Renders a single <see cref="Axes"/> subplot, including all series, decorations, and nested insets, and the
    /// zoom each inset indicates (<see cref="Axes.ZoomIndicator"/>): drawn on this axes just before the inset, under it as
    /// matplotlib's (zorder 4.99 under the inset's 5), from the transform the inset's renderer draws with, so the rectangle
    /// spans the limits the inset draws, placed the way this axes places its data (<see cref="AxesRenderer.DrawnPlacement"/>).</summary>
    /// <param name="figure">The figure the axes belongs to.</param>
    /// <param name="axes">The axes to draw.</param>
    /// <param name="pass">Where it is drawn, on what, under which theme.</param>
    /// <param name="ranges">The data ranges of the draw (<see cref="FigureRanges"/>); <see langword="null"/> when the axes is
    /// drawn outside one, and a set of its own is made for this render and its insets.</param>
    /// <returns>What was drawn: the plot area and the transform the primary series were drawn with.</returns>
    internal DrawnView RenderAxes(Figure figure, Axes axes, RenderPass pass, FigureRanges? ranges = null)
    {
        ranges ??= new FigureRanges(figure);
        pass.Context.UseStageOf(figure.MathTexts, pass.Theme);
        return DrawWithInsets(figure, axes, RendererOf(figure, axes, pass, ranges), pass, ranges, depth: 0);
    }

    /// <summary>The renderer that draws <paramref name="axes"/> in <paramref name="pass"/> for a draw that keeps
    /// <paramref name="ranges"/>; a renderer of your own may read the figure's size (<see cref="AxesRenderer.FigureSize"/>). It is told the
    /// figure's ground (<see cref="Figure.Ground"/>), which shows between the parts of a broken axis.</summary>
    private static AxesRenderer RendererOf(Figure figure, Axes axes, RenderPass pass, FigureRanges ranges)
    {
        var renderer = AxesRenderer.Create(axes, pass.PlotArea, pass.Context, pass.Theme, new Size(figure.Width, figure.Height), ranges);
        renderer.FigureGround = figure.Ground;
        return renderer;
    }

    /// <summary>Draws <paramref name="axes"/> through <paramref name="axesRenderer"/>, then each of its insets, the zoom an inset
    /// indicates first; insets nested deeper than <paramref name="depth"/> 3 are not drawn.</summary>
    private DrawnView DrawWithInsets(Figure figure, Axes axes, AxesRenderer axesRenderer, RenderPass pass, FigureRanges ranges, int depth)
    {
        // Each axes starts at full opacity, whatever the axes drawn before it left on the surface.
        pass.Context.AtFullOpacity();
        axesRenderer.Render();
        axesRenderer.WriteServerView();
        var drawn = new DrawnView(pass.PlotArea, axesRenderer.DrawnTransform)
        {
            LegendBounds = axesRenderer.LegendBounds,
            Placement = axesRenderer.DrawnPlacement,
        };

        // Render inset axes recursively (max depth guard)
        if (depth < 3)
        {
            // When constrained layout is active, position insets within the inner data area
            // to avoid overlapping with axis labels and ticks.
            var referenceArea = figure.Spacing.ConstrainedLayout
                ? axesRenderer.ComputeInnerBounds()
                : pass.PlotArea;

            foreach (var inset in axes.Insets)
            {
                if (inset.InsetBounds is not { } bounds) continue;
                var cell = new Rect(
                    referenceArea.X + bounds.X * referenceArea.Width,
                    referenceArea.Y + bounds.Y * referenceArea.Height,
                    bounds.Width * referenceArea.Width,
                    bounds.Height * referenceArea.Height);
                var room = figure.BarRoom;
                var insetRect = inset.FittedTo(cell, pass.Theme, room: room);
                var insetPass = pass with { PlotArea = insetRect };
                // The bar of an inset that takes its room stands in the inset's own rectangle, not in the part an aspect left it.
                if (inset.BarTakingRoom(room) is not null) ranges.HoldCellOf(inset, cell);
                var insetRenderer = RendererOf(figure, inset, insetPass, ranges);
                // An inset stands at a share of its parent: a layout that stretched the parent stretched it as much.
                insetRenderer.Stretch = axesRenderer.Stretch;
                // The zoom stands under the inset: drawn first, through the transform the inset's render will draw with, over the
                // range the draw keeps for the inset (FigureRanges), so the inset is ranged once.
                if (inset.ZoomIndicator is { } zoom
                    && ZoomIndicatorPath.Of(drawn, inset, new DrawnView(insetRect, insetRenderer.ComputeViewTransform())) is { } path)
                    path.Draw(pass.Context, zoom, pass.Theme);
                DrawWithInsets(figure, inset, insetRenderer, insetPass, ranges, depth + 1);
            }
        }

        return drawn;
    }

    /// <summary>Renders the figure's colour bar as the bar of an axes is drawn (<see cref="ColorBarRendererFactory"/>, by the one
    /// rule of <see cref="ColorBarRoomExtensions"/>), with the box that holds every plot area in the place of the axes' plot. Under a
    /// theme that gives a bar matplotlib's room (<see cref="ColorBarLook.TakesRoom"/>) it stands in the slot the subplots left it;
    /// under the others it stands in the margin beside the box, and a bar below stands under what the bottom row draws under its
    /// plots (<see cref="ColorBarRoomExtensions.FigureBarClearance"/>), as every layout keeps the room for it; under the constrained
    /// layout and those themes it stands in the margin beside or below the box, as an axes' bar does there.</summary>
    /// <param name="figure">The figure being drawn.</param>
    /// <param name="plotAreas">The plot area of each subplot.</param>
    /// <param name="cb">The figure's colour bar.</param>
    /// <param name="ctx">The surface it is drawn on, also the one the subplots' bands are measured with.</param>
    /// <param name="stand">Where the bar stands in the draw (<see cref="FigureRanges.FigureBar"/>); <see langword="null"/> outside a
    /// draw, then by <paramref name="plotAreas"/> under the figure's own spacing.</param>
    internal void RenderFigureColorBar(Figure figure, List<Rect> plotAreas, ColorBar cb, IRenderContext ctx, FigureBarStand? stand = null)
    {
        var theme = figure.Theme;
        // The bar over the first series of any subplot that provides a colour bar with values
        var bar = cb.Resolve(figure.SubPlots.SelectMany(axes => axes.Series), theme);
        if (stand is null)
        {
            var grid = new FigureGrid(figure);
            stand = FigureBarStand.Of(figure, grid, plotAreas, ResolveSpacing(figure, grid), cell: null, ranges: null);
        }

        ctx.BeginGroup("figure-colorbar");
        ColorBarRendererFactory.Create(cb, bar.Map, bar.Range.Min, bar.Range.Max, bar.Normalizer, stand.Box, ctx, theme, stand.Whole, bar.Steps,
            stand.ClearanceWith(ctx), bar.Lines, figure.BarRoom).Render();
        ctx.EndGroup();
    }

    /// <summary>The layout of one draw of a figure, decided once and read by everything the draw does: the resolved spacing, the
    /// height of the figure's title, the line the first row of subplots starts at and the plot area of every subplot. The drawing
    /// (<see cref="ChartRenderer.Render"/>, the SVG writer) and the layout a native control hit-tests with
    /// (<see cref="ChartRenderer.ComputeLayout"/>) read the same frame, so the two cannot disagree and nothing is measured twice; the
    /// data range of each subplot is worked out once for the layout and the drawing alike (<see cref="FigureRanges"/>).</summary>
    /// <param name="Spacing">The spacing in pixels, after the room the drawing reserves past the spines.</param>
    /// <param name="TitleHeight">The height of the figure's title as measured, 0 for a figure without one.</param>
    /// <param name="PlotAreaTop">The line the first row of subplots starts at: below the title when the top margin is too small for it.</param>
    /// <param name="PlotAreas">The plot area of each subplot, in <see cref="Figure.SubPlots"/> order.</param>
    /// <param name="Ranges">The data range of each subplot for this draw, shared by every renderer the draw makes (with the cell
    /// of each when a colour bar takes its room from its axes, <see cref="FigureRanges.CellOf"/>).</param>
    /// <param name="Legends">The figure's legends as this draw placed them: the layout kept room by their boxes and the overlay
    /// draws them where they were placed; empty for a figure without one.</param>
    internal readonly record struct FigureFrame(SubPlotSpacing Spacing, double TitleHeight, double PlotAreaTop, List<Rect> PlotAreas,
        FigureRanges Ranges, IReadOnlyList<LegendRendering.PlacedFigureLegend> Legends);

    /// <summary>What one render of an axes drew, or will draw: where, the range it showed and how it placed its data.
    /// <see cref="ChartRenderer.RenderAxes"/> returns it for the parent, and the parent builds one for an inset from the inset's
    /// renderer before drawing it, so the zoom indicator spans the limits the inset draws (its own limits, or the range it scaled to its
    /// data, margins included) placed the way the parent places its data: one rule, the one that draws the inset.</summary>
    /// <param name="PlotArea">The pixel rectangle the axes was drawn in: an inset's frame.</param>
    /// <param name="Transform">The transform the primary series were drawn with (<see cref="AxesRenderer.DrawnTransform"/>): the
    /// range the axes showed, in data units; <see langword="null"/> for axes that draw no Cartesian range (polar, 3-D).</param>
    internal readonly record struct DrawnView(Rect PlotArea, DataTransform? Transform)
    {
        /// <summary>The bounds of the legend items the render drew, which a native control hit-tests the legend with; empty when it
        /// drew no legend.</summary>
        internal IReadOnlyList<LegendItemBounds> LegendBounds { get; init; } = [];

        /// <summary>Where the render placed a point of the axes' data: the placement it names (<see cref="AxesRenderer.DrawnPlacement"/>,
        /// angle and radius on a polar axes), else its <see cref="Rendering.ChartRenderer.DrawnView.Transform"/> (made only when asked, so a draw without a zoom indicator
        /// allocates none); <see langword="null"/> where the axes has no flat place for its data (3-D).</summary>
        internal IDataPlacement? Placement
        {
            get => field ?? (Transform is { } transform ? new TransformPlacement(transform) : null);
            init;
        }
    }

    /// <summary>The geometry of a zoom indicator (<see cref="ZoomIndicator"/>) in pixels, and its drawing: the region an inset shows,
    /// drawn on its parent, and the frame of the inset. The one owner of matplotlib's connector rule, applied where the two stand on
    /// the picture, after the parent's placement, so a parent axis that runs backwards, a log axis, a polar parent and an inset whose
    /// limits run backwards need no case of their own.</summary>
    /// <param name="Region">The corners of the region the inset shows, on the parent, in pixels.</param>
    /// <param name="Inset">The inset's frame, in pixels.</param>
    internal readonly record struct ZoomIndicatorPath(ZoomIndicatorPath.ZoomCorners Region, Rect Inset)
    {
        /// <summary>The class of the group the indicator is drawn in.</summary>
        internal const string GroupClass = "zoom-indicator";

        /// <summary>The indicator of <paramref name="inset"/>, drawn as <paramref name="insetView"/>, on a parent drawn as
        /// <paramref name="parentView"/>. The region is the rectangle of the limits the inset draws (<see cref="DataTransform.DrawnRange"/>,
        /// low to high whichever way the inset runs) in the parent's data, each corner placed by the parent
        /// (<see cref="Rendering.ChartRenderer.DrawnView.Placement"/>: its transform on a Cartesian or ternary parent, angle and radius on a polar one), as
        /// matplotlib's <c>InsetIndicator</c> draws a <c>Rectangle</c> through the parent's <c>transData</c>, unclipped
        /// (<see cref="IDataPlacement.PlaceUnclipped"/>: a limit at or below zero on a log axis a thousand decades below it, as the log
        /// scale's <c>nonpositive='clip'</c> puts it); on a parent axis that runs backwards the corners swap sides, as matplotlib's
        /// <c>ex = 1 - ex</c> swaps them. A corner the parent masks (outside 0 to 1 on a logit axis, inside an axis break, a radius on
        /// the far side of a polar scale's origin) has no place in either coordinate, and the sides and the line that end there are
        /// not drawn (<see cref="Rendering.ChartRenderer.ZoomIndicatorPath.Draw"/>). <see langword="null"/>, so nothing is drawn rather than a wrong shape, when the inset is not
        /// Cartesian (a polar, 3-D or ternary inset has no rectangle of data: a ternary frame's transform maps the triangle), when
        /// either render kept no placement (a 3-D parent places nothing on a flat plane), or when the parent places no corner.</summary>
        /// <param name="parentView">What the render of the parent drew.</param>
        /// <param name="inset">The inset.</param>
        /// <param name="insetView">The inset's frame and the transform its render draws with, known before it is drawn.</param>
        /// <returns>The geometry, or <see langword="null"/> when there is none to draw.</returns>
        internal static ZoomIndicatorPath? Of(DrawnView parentView, Axes inset, DrawnView insetView)
        {
            if (inset.CoordinateSystem != CoordinateSystem.Cartesian || parentView.Placement is not { } parent
                || insetView.Transform is not { } view)
            {
                return null;
            }

            var limits = view.DrawnRange;
            double x0 = Math.Min(limits.XMin, limits.XMax), x1 = Math.Max(limits.XMin, limits.XMax);
            double y0 = Math.Min(limits.YMin, limits.YMax), y1 = Math.Max(limits.YMin, limits.YMax);
            var drawn = parentView.Transform?.DrawnRange;
            bool xBackwards = drawn is { } dx && dx.XMin > dx.XMax;
            bool yBackwards = drawn is { } dy && dy.YMin > dy.YMax;

            // A corner the parent masks has no place at all, in either coordinate, as matplotlib's masked vertex is NaN in both.
            Point Corner(bool right, bool upper) =>
                parent.PlaceUnclipped(right != xBackwards ? x1 : x0, upper != yBackwards ? y1 : y0) is var pixel && pixel.IsDrawable()
                    ? pixel
                    : new Point(double.NaN, double.NaN);

            var corners = new ZoomCorners(Corner(false, false), Corner(false, true), Corner(true, false), Corner(true, true));
            if (!corners.LowerLeft.IsDrawable() && !corners.UpperLeft.IsDrawable() && !corners.LowerRight.IsDrawable()
                && !corners.UpperRight.IsDrawable())
            {
                return null;
            }

            return new ZoomIndicatorPath(corners, insetView.PlotArea);
        }

        /// <summary>The region's outline, closed: upper left, upper right, lower right, lower left and the upper left again, joined
        /// straight (matplotlib maps a rectangle's path of one interpolation step corner by corner, on a polar parent too).</summary>
        internal Point[] Outline =>
            [Region.UpperLeft, Region.UpperRight, Region.LowerRight, Region.LowerLeft, Region.UpperLeft];

        /// <summary>Draws <see cref="Rendering.ChartRenderer.ZoomIndicatorPath.Outline"/> as matplotlib draws a path with a masked vertex: the sides between two placed corners, a
        /// run of them joined, a side that ends at a masked corner left out. The whole outline is one line when every corner is placed.</summary>
        private void DrawOutline(IRenderContext ctx, StrokeStyle stroke)
        {
            foreach (var run in Outline.LineRuns()) ctx.DrawLines(run, stroke);
        }

        /// <summary>The connectors matplotlib's rule keeps, in its order (lower left, upper left, lower right, upper right). With
        /// x0 = the region's lower left corner left of the inset's left edge, x1 = its lower right corner left of the inset's right
        /// edge, y0 = its lower left corner below the inset's lower edge and y1 = its upper left corner below the inset's upper edge
        /// (matplotlib's box of the transformed lower left, upper left and lower right corners): lower left when x0 xor y0, upper left
        /// when x0 equals y1, lower right when x1 equals y0, upper right when x1 xor y1. Two for an inset beside, above or below its
        /// region; none for an inset wholly inside its region or a region wholly inside its inset.</summary>
        internal IReadOnlyList<ZoomConnector> Connectors
        {
            get
            {
                bool x0 = Region.LowerLeft.X < Inset.X;
                bool x1 = Region.LowerRight.X < Inset.Right;
                bool y0 = Region.LowerLeft.Y > Inset.Bottom;   // pixel Y runs down: the region's lower edge is the lower one
                bool y1 = Region.UpperLeft.Y > Inset.Y;

                var connectors = new List<ZoomConnector>(2);
                if (x0 ^ y0) connectors.Add(new(new(Inset.X, Inset.Bottom), Region.LowerLeft));
                if (x0 == y1) connectors.Add(new(new(Inset.X, Inset.Y), Region.UpperLeft));
                if (x1 == y0) connectors.Add(new(new(Inset.Right, Inset.Bottom), Region.LowerRight));
                if (x1 ^ y1) connectors.Add(new(new(Inset.Right, Inset.Y), Region.UpperRight));
                return connectors;
            }
        }

        /// <summary>Draws the indicator in a group of its own: the region's outline and, when <paramref name="zoom"/> asks for
        /// them, the connectors, a side or a line that ends at a masked corner left out, all in one stroke (its colour, or the theme's neutral grey, its width and its dash pattern) at its
        /// opacity. A stroke that draws nothing (no width, <see cref="LineStyle.None"/>) draws no group either. The outline is matplotlib's
        /// rectangle patch, joined 'miter' (<see cref="JoinStyle.Miter"/>) under every theme, where the connectors are lines of two points.</summary>
        /// <param name="ctx">The surface the parent is drawn on.</param>
        /// <param name="zoom">The indicator's settings.</param>
        /// <param name="theme">The theme the figure is drawn under.</param>
        internal void Draw(IRenderContext ctx, ZoomIndicator zoom, Theme theme)
        {
            var stroke = zoom.StrokeUnder(theme);
            if (!stroke.IsVisible) return;

            ctx.BeginGroup(GroupClass);
            using (ctx.WithOpacity(zoom.Alpha))
            {
                DrawOutline(ctx, stroke with { Join = JoinStyle.Miter });
                if (zoom.Connectors)
                {
                    // A line that ends at a masked corner draws nothing, as matplotlib's does.
                    foreach (var connector in Connectors)
                        if (connector.OnRegion.IsDrawable()) ctx.DrawLine(connector.OnInset, connector.OnRegion, stroke);
                }
            }

            ctx.EndGroup();
        }

        /// <summary>One line from a corner of an inset's frame to the same corner of the region it zooms, in pixels.</summary>
        /// <param name="OnInset">The corner of the inset's frame.</param>
        /// <param name="OnRegion">The same corner of the region, on the parent.</param>
        internal readonly record struct ZoomConnector(Point OnInset, Point OnRegion);

        /// <summary>The four corners of the region an inset zooms, in pixels, each named by the corner of the inset it is joined to: the
        /// limits of the inset placed by the parent (matplotlib's <c>x0 + ex * w, y0 + ey * h</c> through the parent's data transform).</summary>
        /// <param name="LowerLeft">The corner joined to the inset's lower left.</param>
        /// <param name="UpperLeft">The corner joined to the inset's upper left.</param>
        /// <param name="LowerRight">The corner joined to the inset's lower right.</param>
        /// <param name="UpperRight">The corner joined to the inset's upper right.</param>
        internal readonly record struct ZoomCorners(Point LowerLeft, Point UpperLeft, Point LowerRight, Point UpperRight);
    }
}
