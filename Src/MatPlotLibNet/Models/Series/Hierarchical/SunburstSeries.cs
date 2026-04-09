// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a sunburst series that renders hierarchical data as concentric ring segments.</summary>
public sealed class SunburstSeries : HierarchicalSeries
{
    /// <summary>Gets or sets the inner radius as a fraction of the maximum radius (0.0 to 1.0).</summary>
    public double InnerRadius { get; set; }

    /// <summary>Initializes a new instance of <see cref="SunburstSeries"/> with the specified root node.</summary>
    public SunburstSeries(TreeNode root) : base(root) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "sunburst" };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
