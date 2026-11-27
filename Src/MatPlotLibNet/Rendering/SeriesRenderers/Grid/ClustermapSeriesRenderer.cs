// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="ClustermapSeries"/> as a composite of a heatmap panel and
/// up to two dendrogram panels. Row and column leaf orders from the trees are applied to the
/// data matrix before the heatmap is drawn so the cells align visually with the dendrograms, and the
/// labels of the trees' leaves name the rows (right of the matrix) and the columns (under it).</summary>
internal sealed class ClustermapSeriesRenderer : SeriesRenderer<ClustermapSeries>
{
    /// <inheritdoc />
    public ClustermapSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>Panel rects computed from the series' ratio properties. Null panels are
    /// suppressed (either no tree, zero ratio, or rect too small to draw).</summary>
    private readonly record struct ClustermapPanels(
        Rect Heatmap,
        Rect? RowDendrogram,
        Rect? ColumnDendrogram);

    /// <inheritdoc />
    public override void Render(ClustermapSeries series)
    {
        var bounds = Context.Area.PlotBounds;
        var panels = ComputePanels(bounds, series);

        int rows = series.Data.GetLength(0);
        int cols = series.Data.GetLength(1);

        // Only resolve and apply leaf orders when at least one dendrogram panel is active.
        // When both are suppressed the data is already in the user's original order.
        double[,] heatData;
        int[]? rowOrder = null, colOrder = null;
        if (panels.RowDendrogram is not null || panels.ColumnDendrogram is not null)
        {
            rowOrder = ClustermapSeries.ResolveLeafOrder(series.RowTree, rows);
            colOrder = ClustermapSeries.ResolveLeafOrder(series.ColumnTree, cols);
            heatData = series.Data.Permute(rowOrder, colOrder);
        }
        else
        {
            heatData = series.Data;
        }

        // Render heatmap in its panel
        var heatSeries = new HeatmapSeries(heatData)
        {
            ColorMap   = series.ColorMap,
            Normalizer = series.Normalizer,
            ShowLabels = series.ShowLabels,
            LabelFormat = series.LabelFormat,
        };
        // Sub-renderers are instantiated fresh each call rather than cached via ??= because
        // each call supplies a different RenderArea (sub-panel bounds derived from series ratios),
        // so the context changes on every Render() invocation.
        var heatCtx = Context with { Area = new RenderArea(panels.Heatmap, Context.Ctx) };
        new HeatmapSeriesRenderer(heatCtx).RenderInArea(heatSeries);

        // Render row dendrogram (left panel, root left → leaves right adjacent to heatmap)
        if (panels.RowDendrogram is Rect rdRect && series.RowTree is not null)
        {
            var rdSeries = new DendrogramSeries(series.RowTree)
            {
                Orientation = DendrogramOrientation.Left,
                ShowLabels  = false,
                ColorMap    = series.ColorMap,
            };
            var rdCtx = Context with { Area = new RenderArea(rdRect, Context.Ctx) };
            new DendrogramSeriesRenderer(rdCtx).Render(rdSeries);
        }

        // Render column dendrogram (top panel, root top → leaves bottom adjacent to heatmap)
        if (panels.ColumnDendrogram is Rect cdRect && series.ColumnTree is not null)
        {
            var cdSeries = new DendrogramSeries(series.ColumnTree)
            {
                Orientation = DendrogramOrientation.Top,
                ShowLabels  = false,
                ColorMap    = series.ColorMap,
            };
            var cdCtx = Context with { Area = new RenderArea(cdRect, Context.Ctx) };
            new DendrogramSeriesRenderer(cdCtx).Render(cdSeries);
        }

        DrawColumnNames(NamesOf(series.ColumnTree, cols), colOrder, panels.Heatmap);
        DrawRowNames(NamesOf(series.RowTree, rows), rowOrder, panels.Heatmap);
    }

    /// <summary>The name of each row or column, by its index in the data: the label of the tree's leaf that stands for it (the leaf
    /// whose <see cref="TreeNode.Value"/> is the index). <see langword="null"/> when there is no tree or no leaf is named.</summary>
    /// <param name="tree">The row or column tree.</param>
    /// <param name="count">How many rows or columns the data has.</param>
    private static string?[]? NamesOf(TreeNode? tree, int count)
    {
        string?[]? names = null;
        if (tree is not null) CollectNames(tree, count, ref names);
        return names;
    }

    /// <summary>Writes the label of every named leaf under <paramref name="node"/> at its index, making the array at the first one:
    /// a tree whose leaves are unnamed allocates nothing.</summary>
    private static void CollectNames(TreeNode node, int count, ref string?[]? names)
    {
        for (int i = 0; i < node.Children.Count; i++) CollectNames(node.Children[i], count, ref names);
        if (node.Children.Count > 0 || node.Label.Length == 0) return;

        int index = (int)Math.Round(node.Value);
        if (index < 0 || index >= count) return;
        names ??= new string?[count];
        names[index] = node.Label;
    }

    /// <summary>Names each column under the matrix, centred on it, a gap below: the column drawn at place <c>p</c> is the data's
    /// column <paramref name="order"/>[p], or column <c>p</c> when the matrix is drawn in the order given.</summary>
    private void DrawColumnNames(string?[]? names, int[]? order, Rect heatmap)
    {
        if (names is null) return;

        var font = Context.Theme.TickFont();
        double width = heatmap.Width / names.Length;
        for (int p = 0; p < names.Length; p++)
        {
            if (names[order?[p] ?? p] is not { } name) continue;
            Ctx.Draw(TextLabel.Of(name), font, new TextPlacement(new Point(heatmap.X + (p + 0.5) * width, heatmap.Bottom + HierarchicalLayout.LabelGapPx), TextAlignment.Center)
                { Vertical = TextVerticalAlignment.Top });
        }
    }

    /// <summary>Names each row right of the matrix, centred on it, a gap away, as seaborn's clustermap does: the row drawn at place
    /// <c>p</c> (from the top) is the data's row <paramref name="order"/>[p], or row <c>p</c> when the matrix is drawn in the order
    /// given.</summary>
    private void DrawRowNames(string?[]? names, int[]? order, Rect heatmap)
    {
        if (names is null) return;

        var font = Context.Theme.TickFont();
        double height = heatmap.Height / names.Length;
        for (int p = 0; p < names.Length; p++)
        {
            if (names[order?[p] ?? p] is not { } name) continue;
            Ctx.Draw(TextLabel.Of(name), font, new TextPlacement(new Point(heatmap.Right + HierarchicalLayout.LabelGapPx, heatmap.Y + (p + 0.5) * height), TextAlignment.Left)
                { Vertical = TextVerticalAlignment.Center });
        }
    }

    /// <summary>Computes the pixel rects for each panel from the full plot bounds and ratio
    /// properties. Panels whose pixel size would fall below <see cref="Rendering.SeriesRenderers.ClustermapSeriesRenderer.MinPanelPx"/>
    /// are suppressed (returned as null) so the heatmap can use the full bounds when no
    /// dendrograms are active.</summary>
    private static ClustermapPanels ComputePanels(Rect bounds, ClustermapSeries series)
    {
        bool hasRowTree = series.RowTree is not null && series.RowDendrogramWidth > 0.0;
        bool hasColTree = series.ColumnTree is not null && series.ColumnDendrogramHeight > 0.0;

        double rowDendW = hasRowTree ? bounds.Width * series.RowDendrogramWidth : 0.0;
        double colDendH = hasColTree ? bounds.Height * series.ColumnDendrogramHeight : 0.0;

        // Suppress panels that are too small to render (sub-pixel gate)
        if (rowDendW < MinPanelPx) { rowDendW = 0.0; hasRowTree = false; }
        if (colDendH < MinPanelPx) { colDendH = 0.0; hasColTree = false; }

        var heatmap = new Rect(
            bounds.X + rowDendW,
            bounds.Y + colDendH,
            bounds.Width - rowDendW,
            bounds.Height - colDendH);

        Rect? rowDend = hasRowTree
            ? new Rect(bounds.X, bounds.Y + colDendH, rowDendW, bounds.Height - colDendH)
            : null;

        Rect? colDend = hasColTree
            ? new Rect(bounds.X + rowDendW, bounds.Y, bounds.Width - rowDendW, colDendH)
            : null;

        return new ClustermapPanels(heatmap, rowDend, colDend);
    }


    /// <summary>Minimum pixel width/height for a dendrogram panel below which the
    /// panel is suppressed to avoid sub-pixel noise. Aliases the shared
    /// <see cref="HierarchicalLayout.MinPanelPx"/> so a single source-of-truth
    /// applies across every composite renderer.</summary>
    private const double MinPanelPx = HierarchicalLayout.MinPanelPx;
}
