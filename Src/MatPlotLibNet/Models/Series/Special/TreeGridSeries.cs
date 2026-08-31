// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>One row of a <see cref="TreeGridSeries"/>: a name at some depth, its numbers, and where it leads.</summary>
/// <param name="Label">The row's name, drawn left and indented by <see cref="Depth"/>.</param>
/// <param name="Cells">The row's values, one per column, drawn RIGHT-aligned so digits line up.</param>
public readonly record struct TreeGridRow(string Label, IReadOnlyList<string> Cells)
{
    /// <summary>How deep the row sits: 0 is the root. Each level is one indent step, and the row reports it as
    /// <c>aria-level</c> so the grid is navigable rather than merely visible.</summary>
    public int Depth { get; init; }

    /// <summary>Where the row leads — an SVG <c>&lt;a href&gt;</c> around it, so expanding a subtree is a URL and
    /// needs no script. Null makes the row plain text.</summary>
    public string? Url { get; init; }

    /// <summary>Whether the subtree under this row is shown: the chevron points ▾ when true, ▸ when false, and
    /// the row reports <c>aria-expanded</c>. Null = a leaf, which discloses nothing.</summary>
    public bool? Expanded { get; init; }

    /// <summary>An accent for the row's ink — colour ONLY on deviation, as everywhere else on a wall.</summary>
    public Color? Accent { get; init; }

    /// <summary>The row's shape over time: an inline sparkline drawn between the name and the number columns,
    /// scaled to ITS OWN values — the same field <see cref="StatTileSeries.Trend"/> carries, drawn by the same
    /// <see cref="SparklineSeries"/> renderer, because a row and a tile answer the same question. Fewer than two
    /// samples is not a shape and draws nothing.</summary>
    public IReadOnlyList<double>? Trend { get; init; }

    /// <summary>The sparkline's ink; null takes <see cref="Accent"/>, then the theme's foreground.</summary>
    public Color? TrendColor { get; init; }
}

/// <summary>
/// A TREE GRID: indented rows with right-aligned numeric columns — htop's process tree, and the shape the ARIA
/// spec actually names (<c>treegrid</c>; a treemap has no role at all).
///
/// <para><b>Why beside the treemap and not instead of it.</b> A treemap answers "who is big" pre-attentively and
/// stops there: NN/g puts its useful depth at two or three levels and says a rectangle too small for its label
/// must fall back to a tooltip. Measured on this fleet 2026-08-30: two lanes of twenty-three carried every
/// message in an hour — as area that is two rectangles and twenty-one slivers, while as rows it is a column of
/// numbers you can compare exactly. Composition in the map, comparison in the grid.</para>
///
/// <para>The grid carries no data of its own and contributes no axes range: it fills the region it is given.</para>
/// </summary>
public sealed class TreeGridSeries : ChartSeries
{
    /// <summary>The rows, in the order they are drawn — the caller owns the tree's flattening, because only the
    /// caller knows which subtrees are open.</summary>
    public IReadOnlyList<TreeGridRow> Rows { get; }

    /// <summary>The column titles over <see cref="TreeGridRow.Cells"/>, or null for a headerless grid.</summary>
    public string[]? ColumnHeaders { get; set; }

    /// <summary>The height of one row.</summary>
    public double RowHeight { get; set; } = 22;

    /// <summary>How far one level of depth indents.</summary>
    public double IndentWidth { get; set; } = 18;

    /// <summary>The width of one value column; the name column takes whatever is left.</summary>
    public double ColumnWidth { get; set; } = 120;

    /// <summary>The row text's point size.</summary>
    public double FontSize { get; set; } = 12;

    /// <summary>The band painted behind every OTHER row — what carries the eye from a name on the left to its
    /// number on the right when a row is wide, as it is on a wall. Null paints nothing.</summary>
    public Color? RowStripe { get; set; }

    /// <summary>The width reserved at the right end of the name column for a row's <see cref="TreeGridRow.Trend"/>
    /// sparkline. It costs nothing when no row carries one.</summary>
    public double TrendWidth { get; set; } = 160;

    /// <summary>Creates a tree grid over <paramref name="rows"/>.</summary>
    public TreeGridSeries(IReadOnlyList<TreeGridRow> rows) => Rows = rows;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => new(null, null, null, null);

    /// <summary>The grid does not travel: like the treemap's, its DTO carries the discriminator only, because a
    /// wall that renders server-side publishes SVG. A caller that needs the rows on the far side sends them.</summary>
    public override SeriesDto ToSeriesDto() => new() { Type = "treegrid" };

    internal static TreeGridSeries FromSeriesDto(Axes axes, SeriesDto dto) => axes.TreeGrid([]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
