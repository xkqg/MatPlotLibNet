// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Renders a heatmap with optional row and/or column dendrograms, compositing a
/// <see cref="HeatmapSeries"/> and up to two <see cref="DendrogramSeries"/> sub-renderers
/// into a single subplot. When trees are provided the rows and/or columns are reordered to
/// match the leaf order of the corresponding dendrogram.</summary>
public sealed class ClustermapSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable, ILabelable
{
    private double _rowDendrogramWidth = 0.15;
    private double _columnDendrogramHeight = 0.15;

    /// <summary>The 2D data matrix rendered as the heatmap.</summary>
    public double[,] Data { get; }

    /// <summary>Optional row dendrogram tree. Leaves must carry the original row index in
    /// <see cref="TreeNode.Value"/> (cast via <c>Math.Round</c>). When set the rows are
    /// reordered to match the DFS-order leaf traversal of this tree.</summary>
    public TreeNode? RowTree { get; set; }

    /// <summary>Optional column dendrogram tree. Leaves must carry the original column index
    /// in <see cref="TreeNode.Value"/>. When set the columns are reordered to match the
    /// DFS-order leaf traversal of this tree.</summary>
    public TreeNode? ColumnTree { get; set; }

    /// <summary>Fraction of the total plot width reserved for the row dendrogram panel.
    /// Clamped to [0.0, 0.9]. A value of 0.0 suppresses the row dendrogram even when
    /// <see cref="RowTree"/> is set.</summary>
    public double RowDendrogramWidth
    {
        get => _rowDendrogramWidth;
        set => _rowDendrogramWidth = Math.Clamp(value, 0.0, 0.9);
    }

    /// <summary>Fraction of the total plot height reserved for the column dendrogram panel.
    /// Clamped to [0.0, 0.9]. A value of 0.0 suppresses the column dendrogram even when
    /// <see cref="ColumnTree"/> is set.</summary>
    public double ColumnDendrogramHeight
    {
        get => _columnDendrogramHeight;
        set => _columnDendrogramHeight = Math.Clamp(value, 0.0, 0.9);
    }

    /// <inheritdoc cref="IColormappable.ColorMap"/>
    public IColorMap? ColorMap { get; set; }

    /// <inheritdoc cref="INormalizable.Normalizer"/>
    public INormalizer? Normalizer { get; set; }

    /// <inheritdoc cref="ILabelable.ShowLabels"/>
    public bool ShowLabels { get; set; }

    /// <inheritdoc cref="ILabelable.LabelFormat"/>
    /// <remarks>Defaults to <c>"F2"</c> (two decimal places) when <see langword="null"/>.</remarks>
    public string? LabelFormat { get; set; }

    /// <summary>Initializes a new instance of <see cref="ClustermapSeries"/> with the
    /// specified data matrix.</summary>
    /// <param name="data">The two-dimensional data matrix to render as the heatmap.</param>
    public ClustermapSeries(double[,] data) => Data = data;

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange() => Data.ScanColorBarRange();

    /// <inheritdoc />
    /// <remarks>Reports the grid extent with sticky edges on all four sides so axes show
    /// meaningful row/column indices. Identical contract to <see cref="HeatmapSeries"/>.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        int rows = Data.GetLength(0);
        int cols = Data.GetLength(1);
        if (rows == 0 || cols == 0) return new(null, null, null, null);
        return new(0, cols, 0, rows,
            StickyXMin: 0, StickyXMax: cols, StickyYMin: 0, StickyYMax: rows);
    }

    /// <inheritdoc />
    /// <remarks>Trees (<see cref="RowTree"/>, <see cref="ColumnTree"/>) and <see cref="Normalizer"/>
    /// are not emitted to the DTO — the same intentional design choice used by all other
    /// <see cref="INormalizable"/> series in this library. After a round-trip the trees are
    /// rebuilt as placeholders by <see cref="MatPlotLibNet.Serialization.SeriesRegistry"/> and
    /// the normalizer is reset to <see langword="null"/>.</remarks>
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "clustermap",
        HeatmapData = ChartSerializer.To2DList(Data),
        ColorMapName = ColorMap?.Name,
        ShowLabels = ShowLabels ? true : null,  // null encodes the default (false); round-trip is lossless
        LabelFormat = LabelFormat,
        RowDendrogramWidth = _rowDendrogramWidth != 0.15 ? _rowDendrogramWidth : null,
        ColumnDendrogramHeight = _columnDendrogramHeight != 0.15 ? _columnDendrogramHeight : null,
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);

    /// <summary>Resolves the row or column leaf order from a dendrogram tree.
    /// Returns an array where element <c>i</c> is the original index that occupies position
    /// <c>i</c> in the reordered layout. Falls back to the identity order when
    /// <paramref name="tree"/> is <see langword="null"/>, the tree is malformed, or any leaf
    /// index is out of range or duplicated.</summary>
    /// <param name="tree">The root node of the dendrogram tree, or <see langword="null"/>.</param>
    /// <param name="count">The number of rows or columns in the data matrix.</param>
    /// <returns>An <see cref="int"/> array of length <paramref name="count"/> giving the DFS leaf
    /// traversal order, or the identity permutation <c>[0, 1, …, count-1]</c> on any
    /// malformed-input fallback.</returns>
    internal static int[] ResolveLeafOrder(TreeNode? tree, int count)
    {
        if (count == 0) return [];
        int[] identity = new int[count];
        for (int i = 0; i < count; i++) identity[i] = i;
        if (tree is null) return identity;

        var order = new List<int>(count);
        CollectLeaves(tree, order);

        if (order.Count != count) return identity;

        // Validate: no out-of-range index, no duplicates
        var seen = new bool[count];
        foreach (int idx in order)
        {
            if (idx < 0 || idx >= count || seen[idx]) return identity;
            seen[idx] = true;
        }
        return [.. order];
    }

    private static void CollectLeaves(TreeNode node, List<int> order)
    {
        if (node.Children.Count == 0)
        {
            // Math.Round uses banker's rounding (ToEven) but leaf Values are always
            // exact integers stored as double, so the rounding mode has no practical effect.
            order.Add((int)Math.Round(node.Value));
            return;
        }
        foreach (var child in node.Children)
            CollectLeaves(child, order);
    }
}
