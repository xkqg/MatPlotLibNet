// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders a complete <see cref="Figure"/> including subplots, axes, grids, and series onto an <see cref="IRenderContext"/>.</summary>
public sealed class ChartRenderer : IChartRenderer
{
    // Padding above and below the figure-level suptitle (px). Title height is measured dynamically.
    private const double SupTitleTopPad    = 8;
    private const double SupTitleBottomPad = 12;
    // _figure removed — figure is now threaded explicitly through RenderAxes so that
    // SvgTransform (which bypasses ChartRenderer.Render and calls RenderAxes directly in a
    // Parallel.For) gets the same FigureSize path as PngTransform. Previously _figure was
    // only set in Render(), so the SVG path saw figSize=null → 3D cube used PlotArea directly
    // instead of matplotlib's square cube → different projection than PNG → mismatched ticks.

    // Sentinel used to detect when the caller has not explicitly set a spacing on the figure.
    private static readonly SubPlotSpacing DefaultFigureSpacing = new();

    /// <summary>
    /// Resolves the effective <see cref="SubPlotSpacing"/> for a figure.
    /// Priority: figure.Spacing (if overridden by user) → theme.DefaultSpacing → library default.
    /// Fractional values are expanded to absolute pixels using the figure dimensions.
    /// </summary>
    private static SubPlotSpacing ResolveSpacing(Figure figure)
    {
        var sp = figure.Spacing == DefaultFigureSpacing && figure.Theme.DefaultSpacing is not null
            ? figure.Theme.DefaultSpacing
            : figure.Spacing;
        return sp.Resolve(figure.Width, figure.Height);
    }

    /// <inheritdoc />
    public void Render(Figure figure, IRenderContext ctx)
    {
        var resolvedSpacing = PrepareSpacing(figure, ctx);
        double plotAreaTop = RenderBackground(figure, ctx, resolvedSpacing);
        RenderFigureUnderlays(figure, ctx);

        // A figure without subplots lays out no plot area and draws none, but still draws what it carries itself.
        var plotAreas = ComputeSubPlotLayout(figure, plotAreaTop, resolvedSpacing);

        for (int i = 0; i < figure.SubPlots.Count; i++)
            RenderAxes(figure, figure.SubPlots[i], new RenderPass(plotAreas[i], ctx, figure.Theme));

        RenderFigureOverlays(figure, plotAreas, ctx, resolvedSpacing);
    }

    /// <summary>Draws what the figure carries under its subplots: after the background and the title, before the first
    /// subplot (z 0). The one site for it, called by <see cref="Render"/> and by <see cref="Transforms.SvgTransform.Render"/>
    /// alike, also for a figure with no subplot. The figure's own images (<see cref="Figure.Images"/>) are drawn here, in the
    /// order they were added, each later one over the earlier.</summary>
    /// <param name="figure">The figure being drawn.</param>
    /// <param name="ctx">The surface it is drawn on.</param>
    internal void RenderFigureUnderlays(Figure figure, IRenderContext ctx)
    {
        foreach (var image in figure.Images)
            RenderFigureImage(figure, image, ctx);
    }

    /// <summary>Draws what the figure carries above its subplots, after every subplot, in this order: the figure's colour
    /// bar, then its texts in the order they were added, each later one over the earlier, then its legend. The one site for
    /// these, called by <see cref="Render"/> and by <see cref="Transforms.SvgTransform.Render"/> alike, also for a figure with
    /// no subplot, so the SVG and every raster or native picture carry the same overlays at the same places.</summary>
    /// <param name="figure">The figure being drawn.</param>
    /// <param name="plotAreas">The plot area of each subplot, empty for a figure with none; the colour bar stands beside them.</param>
    /// <param name="ctx">The surface it is drawn on.</param>
    /// <param name="spacing">The resolved spacing the subplots were laid out with.</param>
    internal void RenderFigureOverlays(Figure figure, List<Rect> plotAreas, IRenderContext ctx, SubPlotSpacing spacing)
    {
        if (figure.FigureColorBar is { Visible: true } cb)
            RenderFigureColorBar(figure, plotAreas, cb, ctx, spacing);

        foreach (var text in figure.Texts)
            RenderFigureText(figure, text, ctx);

        if (figure.Legend is { Visible: true } legend)
            RenderFigureLegend(figure, legend, ctx);
    }

    /// <summary>Draws one figure image at its place on the figure: each cell of its data is one rectangle of one pixel, no
    /// resampling, its first row on top; an image with no place on the figure is skipped. A cell that would be painted
    /// transparent (a missing value under a colour map with no bad colour) is left out, so a masked pixel costs no element.</summary>
    private static void RenderFigureImage(Figure figure, FigureImage image, IRenderContext ctx)
    {
        if (image.BoundsOn(figure.Height) is not { } bounds) return;

        image.Cells().Draw(ctx, image.Data, bounds, skipTransparentCells: true);
    }

    /// <summary>Draws the figure's legend in the rectangle that is the figure: the entries of every subplot
    /// (<see cref="FigureLegendExtensions"/>), laid out and drawn by the one legend drawer (<see cref="LegendRenderer"/>) at the
    /// place the legend names, static (no entry carries the series index a legend toggle reads). A legend with no entry
    /// draws nothing.</summary>
    private static void RenderFigureLegend(Figure figure, Legend legend, IRenderContext ctx)
    {
        var entries = figure.LegendEntries(legend, ctx);
        var renderer = new LegendRenderer(legend.OnTheFigure(), figure.Theme, ctx);
        if (renderer.Place(entries, new Rect(0, 0, figure.Width, figure.Height), () => MarkObstacles.None) is not { } layout) return;

        renderer.Draw(entries, layout, interactive: false);
    }

    /// <summary>Draws one figure text at its place on the figure, in the font a placed text takes
    /// (<see cref="ThemedFontProvider.DataTextFont(Theme, Font, Color?)"/>), standing against its point as its alignments
    /// say and turned about it; a text with no place on the figure is skipped.</summary>
    private static void RenderFigureText(Figure figure, FigureText text, IRenderContext ctx)
    {
        if (text.AnchorOn(figure.Width, figure.Height) is not { } anchor) return;

        var font = ThemedFontProvider.DataTextFont(figure.Theme, text.Font, text.Color);
        var size = ctx.MeasureText(text.Text, font);
        var at = new TextBox(anchor, size, font.Size, text.Alignment, text.VerticalAlignment, text.Rotation).Baseline;
        ctx.DrawText(text.Text, at, font, text.Alignment, text.Rotation);
    }

    /// <summary>
    /// Resolves the effective <see cref="SubPlotSpacing"/> for rendering <paramref name="figure"/>.
    /// When <see cref="SubPlotSpacing.TightLayout"/> or <see cref="SubPlotSpacing.ConstrainedLayout"/>
    /// is enabled, runs <see cref="ConstrainedLayoutEngine.Compute"/> to auto-size margins from
    /// measured text extents. Otherwise falls back to the theme's default spacing via
    /// <see cref="ResolveSpacing"/>, widened by <see cref="ConstrainedLayoutEngine.ReserveEdgeBands"/> for what is
    /// drawn past the right and the top spines, for the category names past the left spine and for the X tick labels and
    /// the X axis title below the bottom spine.
    /// </summary>
    /// <remarks>
    /// Pure function — does NOT mutate <paramref name="figure"/>. The returned spacing is used
    /// LOCALLY by the caller, so repeated renders of the same figure (e.g. saving as both SVG
    /// and PNG) each compute their own spacing without observing each other's side effects.
    /// Both <see cref="Render"/> (serial) and <see cref="Transforms.SvgTransform.Render"/>
    /// (parallel per-subplot) call this method so SVG and PNG outputs share the same layout.
    /// </remarks>
    internal SubPlotSpacing PrepareSpacing(Figure figure, IRenderContext measureCtx)
    {
        var engine = new ConstrainedLayoutEngine();
        if (figure.Spacing.TightLayout || figure.Spacing.ConstrainedLayout)
            return engine.Compute(figure, measureCtx);
        // The default path measures nothing else, but what is drawn past the right spines (the secondary Y axis, the
        // mirrored Y tick labels and the end-of-line labels), past the top spines (the mirrored X tick labels or the
        // secondary X axis), the category names past the left spine and the X tick labels and X axis title below the bottom
        // spine are reserved by the same rule the engine applies in the branch above.
        return engine.ReserveEdgeBands(figure, ResolveSpacing(figure), measureCtx);
    }

    /// <summary>Renders the figure background and title, returning the top Y coordinate for subplots.</summary>
    internal double RenderBackground(Figure figure, IRenderContext ctx, SubPlotSpacing? spacing = null)
    {
        var sp = spacing ?? ResolveSpacing(figure);
        var theme = figure.Theme;
        var bgColor = figure.BackgroundColor ?? theme.Background;

        ctx.DrawRectangle(new Rect(0, 0, figure.Width, figure.Height), new ShapeStyle(bgColor, null, 0));

        double plotAreaTop = sp.MarginTop;
        if (figure.Title is not null)
        {
            var titleFont = ThemedFontProvider.SupTitleFont(theme);
            // Measure dynamically — hardcoded 30px previously truncated multi-line and oversized titles.
            double titleH = MathTextParser.ContainsMath(figure.Title)
                ? ctx.MeasureRichText(MathTextParser.Parse(figure.Title), titleFont).Height
                : ctx.MeasureText(figure.Title, titleFont).Height;
            var titlePoint = new Point(figure.Width / 2, SupTitleTopPad + titleH * 0.75);
            if (MathTextParser.ContainsMath(figure.Title))
                ctx.DrawRichText(MathTextParser.Parse(figure.Title), titlePoint, titleFont, TextAlignment.Center);
            else
                ctx.DrawText(figure.Title, titlePoint, titleFont, TextAlignment.Center);
            // The layout engine has already widened MarginTop to account for the suptitle, so plotAreaTop
            // already includes the reserved suptitle band. Only push down further when MarginTop wasn't
            // expanded (e.g. user supplied a hard MarginTop smaller than the title needs).
            double needed = SupTitleTopPad + titleH + SupTitleBottomPad;
            if (sp.MarginTop < needed) plotAreaTop = needed;
        }

        return plotAreaTop;
    }

    /// <inheritdoc />
    public LayoutResult ComputeLayout(Figure figure, IRenderContext measureCtx)
    {
        var sp = PrepareSpacing(figure, measureCtx);
        double plotAreaTop = sp.MarginTop;
        if (figure.Title is not null)
        {
            var titleFont = ThemedFontProvider.SupTitleFont(figure.Theme);
            double titleH = MathTextParser.ContainsMath(figure.Title)
                ? measureCtx.MeasureRichText(MathTextParser.Parse(figure.Title), titleFont).Height
                : measureCtx.MeasureText(figure.Title, titleFont).Height;
            double needed = SupTitleTopPad + titleH + SupTitleBottomPad;
            if (sp.MarginTop < needed) plotAreaTop = needed;
        }

        var plotAreas = ComputeSubPlotLayout(figure, plotAreaTop, sp);

        // Compute per-subplot legend item bounds for interactive hit-testing.
        var legendItems = new List<IReadOnlyList<LegendItemBounds>>(plotAreas.Count);
        for (int i = 0; i < figure.SubPlots.Count; i++)
        {
            var axes = figure.SubPlots[i];
            var plotArea = i < plotAreas.Count ? plotAreas[i] : new Rect(0, 0, 0, 0);
            var axesRenderer = AxesRenderer.Create(axes, plotArea, measureCtx, figure.Theme);
            axesRenderer.ComputeLegendBounds();
            legendItems.Add(axesRenderer.LegendBounds.ToList());
        }

        return new LayoutResult(plotAreas, legendItems);
    }

    /// <summary>Computes subplot layout positions from grid metadata and spacing.</summary>
    public List<Rect> ComputeSubPlotLayout(Figure figure, double plotAreaTop, SubPlotSpacing? spacing = null) =>
        SubPlotRects(figure, plotAreaTop, spacing ?? ResolveSpacing(figure));

    /// <summary>The plot area of each axes of <paramref name="figure"/> (in <see cref="Figure.SubPlots"/> order) under
    /// <paramref name="sp"/>: by its <see cref="GridSpec"/> when it has one, otherwise by the grid its axes name. The one
    /// rule the drawing and the layout share: the layout settles the horizontal spacing first and measures what each axes
    /// draws above and below its plot at the width this gives (<see cref="ConstrainedLayoutEngine"/>), since whether the X
    /// tick labels turn depends on it; the X positions and widths depend on the horizontal spacing only.</summary>
    /// <param name="figure">The figure being laid out.</param>
    /// <param name="plotAreaTop">The Y coordinate the first row starts at.</param>
    /// <param name="sp">The spacing in pixels.</param>
    /// <returns>One plot area for each axes.</returns>
    internal static List<Rect> SubPlotRects(Figure figure, double plotAreaTop, SubPlotSpacing sp)
    {
        var cells = figure.GridSpec is not null
            ? ComputeGridSpecLayout(figure, figure.GridSpec, plotAreaTop, sp)
            : ComputeLegacyLayout(figure, plotAreaTop, sp);

        // An axes with an aspect is drawn in the largest rectangle of its shape inside its cell, centred. Here, and not in a
        // renderer: every consumer of the layout (the drawing, the constrained layout, the hit test) reads these rectangles.
        for (int i = 0; i < cells.Count && i < figure.SubPlots.Count; i++)
        {
            cells[i] = figure.SubPlots[i].FittedTo(cells[i], figure.Theme);
        }

        return cells;
    }

    private static List<Rect> ComputeGridSpecLayout(Figure figure, GridSpec gs, double plotAreaTop, SubPlotSpacing sp)
    {
        double totalWidth = figure.Width - sp.MarginLeft - sp.MarginRight;
        double totalHeight = figure.Height - plotAreaTop - sp.MarginBottom;

        // Compute per-cell widths and heights from ratios (or equal if null)
        double[] colWidths = ComputeRatioSizes(totalWidth, gs.Cols, sp.HorizontalGap, gs.WidthRatios);
        double[] rowHeights = ComputeRatioSizes(totalHeight, gs.Rows, sp.VerticalGap, gs.HeightRatios);

        // Precompute cumulative X positions (left edge of each column)
        double[] colX = new double[gs.Cols];
        colX[0] = sp.MarginLeft;
        for (int c = 1; c < gs.Cols; c++)
            colX[c] = colX[c - 1] + colWidths[c - 1] + sp.HorizontalGap;

        // Precompute cumulative Y positions (top edge of each row)
        double[] rowY = new double[gs.Rows];
        rowY[0] = plotAreaTop;
        for (int r = 1; r < gs.Rows; r++)
            rowY[r] = rowY[r - 1] + rowHeights[r - 1] + sp.VerticalGap;

        var areas = new List<Rect>();
        foreach (var ax in figure.SubPlots)
        {
            var pos = ax.GridPosition ?? GridPosition.Single(0, 0);

            double x = colX[pos.ColStart];
            double y = rowY[pos.RowStart];

            // Width = sum of spanned column widths + gaps between them
            double width = 0;
            for (int c = pos.ColStart; c < pos.ColEnd; c++)
                width += colWidths[c];
            width += sp.HorizontalGap * (pos.ColEnd - pos.ColStart - 1);

            // Height = sum of spanned row heights + gaps between them
            double height = 0;
            for (int r = pos.RowStart; r < pos.RowEnd; r++)
                height += rowHeights[r];
            height += sp.VerticalGap * (pos.RowEnd - pos.RowStart - 1);

            areas.Add(new Rect(x, y, width, height));
        }

        return areas;
    }

    /// <summary>Distributes total available space among cells based on ratios, subtracting gaps.</summary>
    private static double[] ComputeRatioSizes(double totalSpace, int count, double gap, double[]? ratios)
    {
        double available = totalSpace - gap * (count - 1);
        var sizes = new double[count];

        if (ratios is null || ratios.Length != count)
        {
            double cellSize = available / count;
            Array.Fill(sizes, cellSize);
        }
        else
        {
            double ratioSum = 0;
            foreach (var r in ratios) ratioSum += r;
            for (int i = 0; i < count; i++)
                sizes[i] = available * ratios[i] / ratioSum;
        }

        return sizes;
    }

    private static List<Rect> ComputeLegacyLayout(Figure figure, double plotAreaTop, SubPlotSpacing sp)
    {
        double totalWidth = figure.Width - sp.MarginLeft - sp.MarginRight;
        double totalHeight = figure.Height - plotAreaTop - sp.MarginBottom;

        int maxRows = 1, maxCols = 1;
        foreach (var ax in figure.SubPlots)
        {
            if (ax.GridRows > 0) maxRows = Math.Max(maxRows, ax.GridRows);
            if (ax.GridCols > 0) maxCols = Math.Max(maxCols, ax.GridCols);
        }

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

    /// <summary>Renders a single <see cref="Axes"/> subplot, including all series, decorations, and nested insets, and the
    /// zoom each inset indicates (<see cref="Axes.ZoomIndicator"/>): drawn on this axes right after the inset, from the
    /// <see cref="DrawnView"/> the inset's render returns, so the rectangle spans the limits the inset really drew. Only a
    /// Cartesian axes draws its insets' zoom; a polar, 3-D or ternary one has no data rectangle to draw it in.</summary>
    /// <param name="figure">The figure the axes belongs to.</param>
    /// <param name="axes">The axes to draw.</param>
    /// <param name="pass">Where it is drawn, on what, under which theme.</param>
    /// <param name="depth">How deep the axes is nested in insets; insets past the third level are not drawn.</param>
    /// <returns>What was drawn: the plot area and the transform the primary series were drawn with.</returns>
    internal DrawnView RenderAxes(Figure figure, Axes axes, RenderPass pass, int depth = 0)
    {
        // Pass figure size through so 3-D renderer can compute matplotlib's exact square bbox.
        var figSize = (Size?)new Size(figure.Width, figure.Height);
        var axesRenderer = AxesRenderer.Create(axes, pass.PlotArea, pass.Context, pass.Theme, figSize);
        axesRenderer.Render();
        axesRenderer.WriteServerView();
        var drawn = new DrawnView(pass.PlotArea, axesRenderer.DrawnTransform);

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
                var insetRect = inset.FittedTo(new Rect(
                    referenceArea.X + bounds.X * referenceArea.Width,
                    referenceArea.Y + bounds.Y * referenceArea.Height,
                    bounds.Width * referenceArea.Width,
                    bounds.Height * referenceArea.Height), pass.Theme);
                var insetView = RenderAxes(figure, inset, pass with { PlotArea = insetRect }, depth + 1);
                if (inset.ZoomIndicator is { } zoom && ZoomIndicatorPath.Of(axes, drawn, inset, insetView) is { } path)
                    path.Draw(pass.Context, zoom, pass.Theme);
            }
        }

        return drawn;
    }

    /// <summary>Renders a figure-level color bar placed outside all subplot areas.</summary>
    internal void RenderFigureColorBar(Figure figure, List<Rect> plotAreas, ColorBar cb, IRenderContext ctx,
        SubPlotSpacing? spacing = null)
    {
        var sp = spacing ?? ResolveSpacing(figure);
        // The first series of any subplot that provides a colour bar with values
        var provider = figure.SubPlots.SelectMany(axes => axes.Series).ColorBarProvider();
        IColorMap colorMap = cb.ColorMap ?? provider?.ColorMap ?? ColorMaps.Viridis;
        var (min, max) = cb.RangeOf(provider);
        var normalizer = provider.ColorBarNormalizer();

        // Compute bounding rect of all subplot plot areas
        double allLeft   = plotAreas.Count > 0 ? plotAreas.Min(r => r.X)             : sp.MarginLeft;
        double allTop    = plotAreas.Count > 0 ? plotAreas.Min(r => r.Y)             : sp.MarginTop;
        double allRight  = plotAreas.Count > 0 ? plotAreas.Max(r => r.X + r.Width)   : figure.Width  - sp.MarginRight;
        double allBottom = plotAreas.Count > 0 ? plotAreas.Max(r => r.Y + r.Height)  : figure.Height - sp.MarginBottom;

        var theme = figure.Theme;
        var tickFont = new Font { Family = theme.DefaultFont.Family, Size = theme.DefaultFont.Size - 2, Color = theme.ForegroundText };
        var labelFont = new Font { Family = theme.DefaultFont.Family, Size = theme.DefaultFont.Size, Color = theme.ForegroundText };

        const int steps = 50;

        ctx.BeginGroup("figure-colorbar");

        if (cb.Orientation == ColorBarOrientation.Horizontal)
        {
            double totalW = (allRight - allLeft) * cb.Shrink;
            double barH   = cb.Aspect > 0 ? totalW / cb.Aspect : cb.Width;
            double barX   = allLeft + (allRight - allLeft - totalW) / 2;
            double barY   = allBottom + cb.Padding;
            var scale = ColorBarScale.For(min, max, totalW, vertical: false, normalizer, ctx, tickFont);

            for (int i = 0; i < steps; i++)
            {
                double frac  = (double)i / steps;
                double stepX = barX + totalW * i / steps;
                double stepW = totalW / steps + 1;
                ctx.DrawRectangle(new Rect(stepX, barY, stepW, barH),
                    new ShapeStyle(colorMap.GetColor(scale.GradientFraction(frac, (i + 0.5) / steps)), null, 0));
            }
            ctx.DrawRectangle(new Rect(barX, barY, totalW, barH), new ShapeStyle(null, theme.ForegroundText, 0.5));

            double labelY = barY + barH + 4 + tickFont.Size;
            for (int i = 0; i < scale.Ticks.Length; i++)
                ctx.DrawText(scale.Labels[i], new Point(barX + totalW * scale.Fractions[i], labelY), tickFont, TextAlignment.Center);
            if (cb.Label is not null)
                ctx.DrawText(cb.Label, new Point(barX + totalW / 2, labelY + tickFont.Size + 4), labelFont, TextAlignment.Center);
        }
        else
        {
            // Vertical: clamp barX so the bar + ticks + label fit within figure width
            double totalH  = (allBottom - allTop) * cb.Shrink;
            double barW    = cb.Width;
            double tickAreaW = 30; // estimated tick label width
            double labelAreaW = cb.Label is not null ? tickFont.Size + 6 : 0;
            double rightEdge = figure.Width - 4; // 4px from figure edge
            double barX = Math.Min(allRight + cb.Padding, rightEdge - barW - tickAreaW - labelAreaW);
            double barY = allTop + (allBottom - allTop - totalH) / 2;
            var scale = ColorBarScale.For(min, max, totalH, vertical: true, normalizer, ctx, tickFont);

            for (int i = 0; i < steps; i++)
            {
                double frac  = 1.0 - (double)i / steps;
                double stepY = barY + totalH * i / steps;
                double stepH = totalH / steps + 1;
                ctx.DrawRectangle(new Rect(barX, stepY, barW, stepH),
                    new ShapeStyle(colorMap.GetColor(scale.GradientFraction(frac, 1.0 - (i + 0.5) / steps)), null, 0));
            }
            ctx.DrawRectangle(new Rect(barX, barY, barW, totalH), new ShapeStyle(null, theme.ForegroundText, 0.5));

            double labelX = barX + barW + 4;
            for (int i = 0; i < scale.Ticks.Length; i++)
                ctx.DrawText(scale.Labels[i], new Point(labelX, barY + totalH * (1 - scale.Fractions[i]) + 4), tickFont, TextAlignment.Left);
            if (cb.Label is not null)
                ctx.DrawText(cb.Label, new Point(labelX + tickAreaW, barY + totalH / 2),
                    labelFont, TextAlignment.Center, 90);
        }

        ctx.EndGroup();
    }

}
