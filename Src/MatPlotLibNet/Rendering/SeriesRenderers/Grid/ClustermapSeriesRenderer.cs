// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.SeriesRenderers;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="ClustermapSeries"/> as a composite of a heatmap panel and
/// up to two dendrogram panels. Row and column leaf orders from the trees are applied to the
/// data matrix before the heatmap is drawn so the cells align visually with the dendrograms.</summary>
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
        if (panels.RowDendrogram is not null || panels.ColumnDendrogram is not null)
        {
            int[] rowOrder = ClustermapSeries.ResolveLeafOrder(series.RowTree, rows);
            int[] colOrder = ClustermapSeries.ResolveLeafOrder(series.ColumnTree, cols);
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
        new HeatmapSeriesRenderer(heatCtx).Render(heatSeries);

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
    }

    /// <summary>Computes the pixel rects for each panel from the full plot bounds and ratio
    /// properties. Panels whose pixel size would fall below <see cref="HierarchicalLayout.Clustermap.MinPanelPx"/>
    /// are suppressed (returned as null) so the heatmap can use the full bounds when no
    /// dendrograms are active.</summary>
    private static ClustermapPanels ComputePanels(Rect bounds, ClustermapSeries series)
    {
        bool hasRowTree = series.RowTree is not null && series.RowDendrogramWidth > 0.0;
        bool hasColTree = series.ColumnTree is not null && series.ColumnDendrogramHeight > 0.0;

        double rowDendW = hasRowTree ? bounds.Width * series.RowDendrogramWidth : 0.0;
        double colDendH = hasColTree ? bounds.Height * series.ColumnDendrogramHeight : 0.0;

        // Suppress panels that are too small to render (sub-pixel gate)
        if (rowDendW < HierarchicalLayout.Clustermap.MinPanelPx) { rowDendW = 0.0; hasRowTree = false; }
        if (colDendH < HierarchicalLayout.Clustermap.MinPanelPx) { colDendH = 0.0; hasColTree = false; }

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

}
