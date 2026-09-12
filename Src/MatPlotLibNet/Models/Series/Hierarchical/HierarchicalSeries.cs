// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for series types that render hierarchical <see cref="TreeNode"/> data.</summary>
public abstract class HierarchicalSeries : ChartSeries, IColormappable, INormalizable
{
    public TreeNode Root { get; }

    public IColorMap? ColorMap { get; set; }

    /// <summary>The lower bound of the <see cref="TreeNode.ColorValue"/> range, or null for the leaves' own minimum.</summary>
    public double? VMin { get; set; }

    /// <summary>The upper bound of the <see cref="TreeNode.ColorValue"/> range, or null for the leaves' own maximum.</summary>
    public double? VMax { get; set; }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set; }

    /// <summary>The colour of <paramref name="node"/> from its <see cref="TreeNode.ColorValue"/>, or null when the
    /// node carries none — the one place the value → colour rule lives, shared by every hierarchical renderer.</summary>
    internal Color? ColorFromValue(TreeNode node, IColorMap cmap, double cMin, double cMax)
        => node.ColorValue is { } v
            ? cmap.GetColor((Normalizer ?? LinearNormalizer.Instance).Normalize(v, cMin, cMax))
            : null;

    /// <summary>The (min, max) the colour values are normalized over: the explicit limits where set, the
    /// leaves' own extremes otherwise.</summary>
    internal (double Min, double Max) ColorRange()
    {
        double min = double.PositiveInfinity, max = double.NegativeInfinity;
        Walk(Root);
        if (double.IsPositiveInfinity(min))
        {
            min = 0;
            max = 1;
        }
        return (VMin ?? min, VMax ?? max);

        void Walk(TreeNode node)
        {
            if (node.ColorValue is { } v)
            {
                min = Math.Min(min, v);
                max = Math.Max(max, v);
            }
            foreach (var child in node.Children)
            {
                Walk(child);
            }
        }
    }

    public bool ShowLabels { get; set; } = true;

    /// <summary>Initializes a new instance with the specified root node.</summary>
    protected HierarchicalSeries(TreeNode root) => Root = root;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    /// <remarks>The tree walked depth-first: every node's label, its depth, and its value. A treemap's
    /// rectangles ARE the tree, so the table is the tree — flattened, with the depth kept as a number so the
    /// hierarchy survives the flattening.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new List<IReadOnlyList<DataCell>>();
        Walk(Root, 0);
        return new ChartDataTable(null,
            [new("label", DataColumnKind.Text), new("depth"), new(Label ?? "value")], rows);

        void Walk(TreeNode node, int depth)
        {
            rows.Add([DataCell.FromText(node.Label), DataCell.FromNumber(depth), DataCell.FromNumber(node.Value)]);
            foreach (var child in node.Children)
            {
                Walk(child, depth + 1);
            }
        }
    }
}
