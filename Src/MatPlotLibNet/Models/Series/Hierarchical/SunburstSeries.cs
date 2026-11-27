// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

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
public sealed class SunburstSeries : HierarchicalSeries, ISeriesKind<SunburstSeries>
{
    /// <summary>The radius of the empty hole at the centre, as a fraction of the outer radius (0 to 1). 0, the default,
    /// leaves no hole, so the innermost ring is a solid disc; a larger value turns the chart into a donut.</summary>
    public double InnerRadius { get; set => Change(ref field, value); }

    private const double RestingMinLabelSweepDegrees = 8.0;
    /// <summary>Optional minimum sweep angle in degrees below which a node's label is
    /// suppressed entirely. A 1° wedge is too narrow to ever fit a useful label, so we
    /// drop it rather than ask the collision engine to find space. Default: <see cref="RestingMinLabelSweepDegrees"/> degrees.</summary>
    public double MinLabelSweepDegrees { get; set => Change(ref field, value); } = RestingMinLabelSweepDegrees;

    /// <summary>Initializes a new instance of <see cref="SunburstSeries"/> with the specified root node.</summary>
    public SunburstSeries(TreeNode root) : base(root) { }

    static string ISeriesKind<SunburstSeries>.Kind => "sunburst";

    /// <summary>How many pixels a ring's label stands under the theme's text.</summary>
    private const double LabelStep = 1;

    /// <summary>The smallest a ring's label is drawn at, in pixels.</summary>
    private const double SmallestLabelSize = 9;

    /// <summary>The size a ring's label is drawn at under <paramref name="theme"/>: a pixel under the theme's text, at least
    /// <see cref="SmallestLabelSize"/>. It follows the theme's text under every theme, so a theme that grows its text grows these too.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <returns>The size, in the units of <see cref="Font.Size"/>.</returns>
    internal double LabelSizeUnder(Theme theme) => Math.Max(SmallestLabelSize, theme.DataTextFont().Size - LabelStep);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => this.DescribeSharedSettings(new()
    {
        Type = this.WireKind(),
        InnerRadius = InnerRadius.OrNullAtRest(0.0),
        MinLabelSweepDegrees = MinLabelSweepDegrees.OrNullAtRest(RestingMinLabelSweepDegrees),
    });

    /// <summary>The reader of a <see cref="SunburstSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static SunburstSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var root = dto.TreeRoot.TreeOrBareRoot();
        var s = axes.Sunburst(root);
        if (dto.InnerRadius is { } innerRadius) s.InnerRadius = innerRadius;
        if (dto.MinLabelSweepDegrees is { } minLabelSweepDegrees) s.MinLabelSweepDegrees = minLabelSweepDegrees;
        s.RestoreSharedSettings(dto);
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
