// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Base class for series types that render hierarchical <see cref="TreeNode"/> data.</summary>
public abstract class HierarchicalSeries : ChartSeries, IColormappable, INormalizable, IColorScaled
{
    /// <inheritdoc />
    internal override bool DrawsWithoutAxes => true;

    /// <summary>The top node of the tree being drawn; its descendants are the cells, ring segments or branches.</summary>
    public TreeNode Root { get; }

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <summary>The lower bound of the <see cref="TreeNode.ColorValue"/> range, or null for the leaves' own minimum.</summary>
    public double? VMin { get; set => Change(ref field, value); }

    /// <summary>The upper bound of the <see cref="TreeNode.ColorValue"/> range, or null for the leaves' own maximum.</summary>
    public double? VMax { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <summary>The colour a cell of <paramref name="node"/> is filled with: the node's own colour, else its
    /// <see cref="TreeNode.ColorValue"/> through the series' colour scale, else the scale's map at the node's place among its siblings. The
    /// one rule a treemap and an icicle colour a cell by.</summary>
    /// <param name="node">The node.</param>
    /// <param name="scale">The colour scale of the series (<c>ColorScaleUnder</c>).</param>
    /// <param name="place">The node's place among its siblings, from 0.</param>
    /// <param name="places">How many siblings it has, itself included.</param>
    /// <returns>The colour.</returns>
    internal static Color ColourOf(TreeNode node, ColormapContext scale, int place, int places) =>
        node.Color ?? (node.ColorValue is { } value ? scale.ColorOf(value) : scale.ColorMap.GetColor(place.ColormapFraction(places)));

    /// <inheritdoc />
    /// <remarks>The <see cref="TreeNode.ColorValue"/> of every node of the tree, between the ends given (<see cref="VMin"/>,
    /// <see cref="VMax"/>): over the values above 0 only under matplotlib's log norm (<c>RangeScale</c>), whose range has no logarithm of 0.
    /// A node is coloured by the scale this gives (<c>ColorScaleUnder</c>) when it names no colour of its own.</remarks>
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping)
    {
        double lowest = mapping.RangeScale.LowestPlaced;
        var range = Range1D.Empty;
        Walk(Root);
        return mapping.RangeOf(range.OrNone(), VMin, VMax);

        void Walk(TreeNode node)
        {
            if (node.ColorValue is { } v) range = range.IncludingFrom(v, lowest);
            foreach (var child in node.Children)
            {
                Walk(child);
            }
        }
    }

    /// <summary>Whether each node's label is drawn on its cell, ring segment or branch. True by default; false leaves
    /// the shapes unlabelled. A label on a coloured shape is written in the theme's text colour when that can be read
    /// against the shape, and in black or white, whichever stands out, when it cannot.</summary>
    public bool ShowLabels { get; set => Change(ref field, value); } = RestingShowLabels;

    /// <summary>Whether a hierarchical series labels its shapes when it is told otherwise: yes.</summary>
    internal const bool RestingShowLabels = true;

    /// <summary>Initializes a new instance with the specified root node.</summary>
    protected HierarchicalSeries(TreeNode root) => Root = root;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => default;

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
