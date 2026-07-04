// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a treemap series that renders hierarchical data as nested rectangles.</summary>
public sealed class TreemapSeries : HierarchicalSeries
{
    public double Padding { get; set; } = 2.0;

    /// <summary>Initializes a new instance of <see cref="TreemapSeries"/> with the specified root node.</summary>
    public TreemapSeries(TreeNode root) : base(root) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "treemap" };

    /// <summary>Reconstructs a <see cref="TreemapSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static TreemapSeries FromSeriesDto(Axes axes, SeriesDto dto)
        => axes.Treemap(new TreeNode { Label = "Root" });

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
