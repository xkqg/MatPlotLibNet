// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Models.Series;

/// <summary>
/// Represents a sunburst series that renders hierarchical data as concentric ring segments.
/// </summary>
/// <remarks>
/// With <see cref="InnerRadius"/> set to 0 (the default) and a 2-level <see cref="TreeNode"/>,
/// a sunburst renders as a <b>nested pie</b>: the inner disc shows the root's children as pie
/// sectors and the outer ring shows their grandchildren inheriting the parent's angle range.
/// Use <c>AxesBuilder.NestedPie</c> for the convenience wrapper when that's the
/// layout you want.
/// </remarks>
public sealed class SunburstSeries : HierarchicalSeries
{
    public double InnerRadius { get; set; }

    /// <summary>Optional minimum sweep angle in degrees below which a node's label is
    /// suppressed entirely. A 1° wedge is too narrow to ever fit a useful label, so we
    /// drop it rather than ask the collision engine to find space. Default: 8°.</summary>
    public double MinLabelSweepDegrees { get; set; } = 8.0;

    /// <summary>Initializes a new instance of <see cref="SunburstSeries"/> with the specified root node.</summary>
    public SunburstSeries(TreeNode root) : base(root) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "sunburst" };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
