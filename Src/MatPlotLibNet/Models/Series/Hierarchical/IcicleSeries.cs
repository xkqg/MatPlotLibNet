// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>An icicle chart: a hierarchy drawn as rows of cells, one row for each level, the cells of a node laid side by side
/// under it, each as wide as its share of the node's width (the children of a node divide the node's width in proportion to
/// their total values, in the order they are stored, by the rule the sunburst divides an angle by). The root is not drawn, as
/// the sunburst's is not: the first row is the root's children. A node without children is one row tall wherever it stands,
/// so a shallow branch ends where it ends and leaves the rows below it empty.</summary>
/// <remarks>The cells are coloured as a treemap's are: a node's own <see cref="TreeNode.Color"/>, otherwise its
/// <see cref="TreeNode.ColorValue"/> through the series' colour map, otherwise the colour map at the node's place among its
/// siblings. The series fills its plot area and has no axes of its own.</remarks>
public sealed class IcicleSeries : HierarchicalSeries
{
    /// <summary>Initializes an icicle chart of the hierarchy under <paramref name="root"/>.</summary>
    /// <param name="root">The root of the hierarchy; it is not drawn, its children are the first row.</param>
    public IcicleSeries(TreeNode root) : base(root) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => this.DescribeSharedSettings(new() { Type = "icicle" });

    /// <summary>Reconstructs an <see cref="IcicleSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static IcicleSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var root = dto.TreeRoot is { } treeRoot ? treeRoot.DtoToTreeNode() : new TreeNode { Label = "Root" };
        var s = axes.Icicle(root);
        s.RestoreSharedSettings(dto);
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
