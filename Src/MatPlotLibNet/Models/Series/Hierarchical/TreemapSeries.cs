// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a treemap series that renders hierarchical data as nested rectangles.</summary>
public sealed class TreemapSeries : HierarchicalSeries, ISeriesSerializable
{
    /// <summary>Gets or sets the padding between nested rectangles in pixels.</summary>
    public double Padding { get; set; } = 2.0;

    /// <summary>Initializes a new instance of <see cref="TreemapSeries"/> with the specified root node.</summary>
    public TreemapSeries(TreeNode root) : base(root) { }

    /// <inheritdoc />
    public SeriesDto ToSeriesDto() => new() { Type = "treemap" };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
