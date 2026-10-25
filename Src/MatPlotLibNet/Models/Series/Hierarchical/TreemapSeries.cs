// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a treemap series that renders hierarchical data as nested rectangles.</summary>
public sealed class TreemapSeries : HierarchicalSeries
{
    public double Padding { get; set; } = 2.0;

    /// <summary>What to do with a label wider than its rect — see <see cref="TreemapLabelFit"/>.</summary>
    public TreemapLabelFit LabelFit { get; set; } = TreemapLabelFit.Always;

    /// <summary>The point size of a leaf's <see cref="TreeNode.Headline"/> — the cell's big number. An interior
    /// node's headline rides its header strip at the label's own size; a strip has no room to shout.</summary>
    public double HeadlineFontSize { get; set; } = 20;

    /// <summary>Initializes a new instance of <see cref="TreemapSeries"/> with the specified root node.</summary>
    public TreemapSeries(TreeNode root) : base(root) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => this.DescribeSharedSettings(new()
    {
        Type = "treemap",
        Padding = Padding.OrNullAtRest(2.0),
        LabelFit = LabelFit.OrNullAtRest(TreemapLabelFit.Always),
        HeadlineFontSize = HeadlineFontSize.OrNullAtRest(20.0),
    });

    /// <summary>Reconstructs a <see cref="TreemapSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static TreemapSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var root = dto.TreeRoot is { } treeRoot ? treeRoot.DtoToTreeNode() : new TreeNode { Label = "Root" };
        var s = axes.Treemap(root);
        if (dto.Padding.HasValue)
        {
            s.Padding = dto.Padding.Value;
        }
        if (dto.LabelFit.HasValue)
        {
            s.LabelFit = dto.LabelFit.Value;
        }
        if (dto.HeadlineFontSize.HasValue)
        {
            s.HeadlineFontSize = dto.HeadlineFontSize.Value;
        }
        s.RestoreSharedSettings(dto);
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
