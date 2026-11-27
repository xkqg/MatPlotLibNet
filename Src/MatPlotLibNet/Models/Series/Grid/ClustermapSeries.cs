// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Renders a heatmap with optional row and/or column dendrograms, compositing a
/// <see cref="HeatmapSeries"/> and up to two <see cref="DendrogramSeries"/> sub-renderers
/// into a single subplot. When trees are provided the rows and/or columns are reordered to
/// match the leaf order of the corresponding dendrogram.</summary>
public sealed class ClustermapSeries : ChartSeries, IColorBarDataProvider, IColorScaled, IColormappable, INormalizable, ILabelable, ISeriesKind<ClustermapSeries>
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    private const double RestingRowDendrogramWidth = 0.15;
    private double _rowDendrogramWidth = RestingRowDendrogramWidth;
    private const double RestingColumnDendrogramHeight = 0.15;
    private double _columnDendrogramHeight = RestingColumnDendrogramHeight;

    /// <summary>The 2D data matrix rendered as the heatmap.</summary>
    public double[,] Data { get; }

    /// <summary>Optional row dendrogram tree. Leaves must carry the original row index in
    /// <see cref="TreeNode.Value"/> (cast via <c>Math.Round</c>). When set the rows are
    /// reordered to match the DFS-order leaf traversal of this tree, and the <see cref="TreeNode.Label"/> of each leaf names
    /// its row right of the matrix, as seaborn's clustermap names a row (a leaf with an empty label names nothing).</summary>
    public TreeNode? RowTree { get; set => Change(ref field, value); }

    /// <summary>Optional column dendrogram tree. Leaves must carry the original column index
    /// in <see cref="TreeNode.Value"/>. When set the columns are reordered to match the
    /// DFS-order leaf traversal of this tree, and the <see cref="TreeNode.Label"/> of each leaf names its column under the
    /// matrix (a leaf with an empty label names nothing).</summary>
    public TreeNode? ColumnTree { get; set => Change(ref field, value); }

    /// <summary>Fraction of the total plot width reserved for the row dendrogram panel.
    /// Clamped to [0.0, 0.9]. A value of 0.0 suppresses the row dendrogram even when
    /// <see cref="RowTree"/> is set.</summary>
    public double RowDendrogramWidth
    {
        get => _rowDendrogramWidth;
        set => Change(ref _rowDendrogramWidth, Math.Clamp(value, 0.0, 0.9));
    }

    /// <summary>Fraction of the total plot height reserved for the column dendrogram panel.
    /// Clamped to [0.0, 0.9]. A value of 0.0 suppresses the column dendrogram even when
    /// <see cref="ColumnTree"/> is set.</summary>
    public double ColumnDendrogramHeight
    {
        get => _columnDendrogramHeight;
        set => Change(ref _columnDendrogramHeight, Math.Clamp(value, 0.0, 0.9));
    }

    /// <inheritdoc cref="IColormappable.ColorMap"/>
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <inheritdoc cref="INormalizable.Normalizer"/>
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <inheritdoc cref="ILabelable.ShowLabels"/>
    public bool ShowLabels { get; set => Change(ref field, value); }

    /// <inheritdoc cref="ILabelable.LabelFormat"/>
    /// <remarks>Defaults to <c>"F2"</c> (two decimal places) when <see langword="null"/>.</remarks>
    public string? LabelFormat { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="ClustermapSeries"/> with the
    /// specified data matrix.</summary>
    /// <param name="data">The two-dimensional data matrix to render as the heatmap.</param>
    public ClustermapSeries(double[,] data) => Data = data;

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange() => this.ColourRangeUnder(Theme.Default);

    /// <inheritdoc />
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => mapping.RangeOf(Data);

    /// <inheritdoc />
    /// <remarks>Reports the grid extent with sticky edges on all four sides so axes show
    /// meaningful row/column indices. Identical contract to <see cref="HeatmapSeries"/>.</remarks>
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => DataRangeContribution.OfCells(Data);

    static string ISeriesKind<ClustermapSeries>.Kind => "clustermap";

    /// <inheritdoc />
    /// <remarks>The <see cref="Normalizer"/> is written by kind when it is one of the library's, as for every
    /// <see cref="INormalizable"/> series.</remarks>
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        HeatmapData = Data.To2DList(),
        ColorScale = this.ColorScaleOnTheWire(),
        ShowLabels = ShowLabels.OrNullAtRest(false),  // null encodes the default (false); round-trip is lossless
        LabelFormat = LabelFormat,
        RowDendrogramWidth = _rowDendrogramWidth.OrNullAtRest(RestingRowDendrogramWidth),
        ColumnDendrogramHeight = _columnDendrogramHeight.OrNullAtRest(RestingColumnDendrogramHeight),
        RowTree = RowTree is { } rowTree ? rowTree.TreeNodeToDto() : null,
        ColumnTree = ColumnTree is { } columnTree ? columnTree.TreeNodeToDto() : null,
    };

    /// <summary>The reader of a <see cref="ClustermapSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static ClustermapSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var data = dto.HeatmapData.From2DList();
        var s = axes.Clustermap(data.GetLength(0) > 0 ? data : new double[1, 1]);
        if (dto.RowTree is { } rowTree) s.RowTree = rowTree.DtoToTreeNode();
        if (dto.ColumnTree is { } columnTree) s.ColumnTree = columnTree.DtoToTreeNode();
        s.RestoreColorScale(dto);
        if (dto.ShowLabels is { } showLabels) s.ShowLabels = showLabels;
        if (dto.LabelFormat is { } labelFormat) s.LabelFormat = labelFormat;
        if (dto.RowDendrogramWidth is { } rowDendrogramWidth) s.RowDendrogramWidth = rowDendrogramWidth;
        if (dto.ColumnDendrogramHeight is { } columnDendrogramHeight) s.ColumnDendrogramHeight = columnDendrogramHeight;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form over the matrix, in the ORDER THE MAP DRAWS — the clustering reorders rows and
    /// columns, and a table in input order would name different cells than the picture does.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rowOrder = ResolveLeafOrder(RowTree, Data.GetLength(0));
        var columnOrder = ResolveLeafOrder(ColumnTree, Data.GetLength(1));
        return ChartDataTable.PerCell(rowOrder.Length, columnOrder.Length, [new("row"), new("column"), new(Label ?? "value")], (i, j) =>
        {
            int r = rowOrder[i], c = columnOrder[j];
            return [DataCell.FromNumber(r), DataCell.FromNumber(c), DataCell.FromNumber(Data[r, c])];
        });
    }

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
