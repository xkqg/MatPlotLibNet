// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A broken-bar series (matplotlib <c>broken_barh</c> equivalent) that draws horizontal
/// bars with gaps, one row per outer array index. Each inner <see cref="BarRange"/> defines one
/// bar segment by <c>(Start, Width)</c>.</summary>
public sealed class BrokenBarSeries : ChartSeries, IHasColor, IEdgedPatch, ISeriesKind<BrokenBarSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>broken_barh</c> draws in <c>patch.facecolor</c> and asks neither cycle; it returns a <c>PolyCollection</c> (its
    /// <c>get_joinstyle()</c> is <c>None</c>, drawn 'round'), not a <c>Rectangle</c> patch, so its outline is joined as the theme joins a
    /// collection.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => LegendSwatch.EdgedPatch;

    /// <summary>Rows of bar segments. <c>Ranges[row][i]</c> is the <c>i</c>-th segment on <c>row</c>.</summary>
    public BarRange[][] Ranges { get; }

    /// <summary>Optional Y-axis tick labels, one per row. <see langword="null"/> to omit row labels.</summary>
    public string[]? Labels { get; set => Change(ref field, value); }

    /// <summary>Vertical bar thickness in data units. Defaults to <c>0.8</c> (matplotlib default).</summary>
    public double BarHeight { get; set => Change(ref field, value); } = 0.8;

    /// <summary>Uniform fill colour applied to every segment. <see langword="null"/> defers to the theme cycle.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>Initialises a new <see cref="BrokenBarSeries"/>.</summary>
    /// <param name="ranges">One <see cref="BarRange"/> array per row. Rows may have different
    /// segment counts; an empty outer array is valid and renders nothing.</param>
    public BrokenBarSeries(BarRange[][] ranges)
    {
        Ranges = ranges;
    }

    /// <inheritdoc />
    /// <remarks>No row holds a segment: matplotlib's <c>broken_barh</c> then adds no bar to the data limits and asks for the autoscale
    /// (<c>broken_barh([], (0, 1))</c>: (-0.055, 0.055) on both axes).</remarks>
    internal override bool HoldsNoData => !Ranges.Any(r => r.Length > 0);

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Ranges.Length == 0) return new(0, 1, -0.5, 0.5);
        var allRanges = Ranges.SelectMany(r => r).ToArray();
        // Over the segments that can be drawn: a segment whose start or width is missing (NaN) or infinite is not drawn,
        // so neither of its ends counts; rows without any segment keep the 0..1 default.
        var x = !HoldsNoData
            ? allRanges.Select(r => new Range1D(r.Start, r.Start + r.Width)).DrawableRange()
            : new Range1D(0, 1);
        // The bars' own extent on both axes, with no sticky edge: matplotlib's broken_barh registers none, so the margin pads the
        // segments and the rows alike.
        var rows = BarHeight.AcrossRows(Ranges.Length);
        return new(x?.Lo, x?.Hi, rows.Lo, rows.Hi);
    }

    static string ISeriesKind<BrokenBarSeries>.Kind => "brokenbar";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        RangeStarts = Ranges.Select(row => row.Select(r => r.Start).ToArray()).ToArray(),
        RangeWidths = Ranges.Select(row => row.Select(r => r.Width).ToArray()).ToArray(),
        BarHeight = BarHeight,
        Color = Color,
        Categories = Labels
    };

    /// <summary>The reader of a <see cref="BrokenBarSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static BrokenBarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var starts = dto.RangeStarts ?? [];
        var widths = dto.RangeWidths ?? [];
        int rowCount = Math.Min(starts.Length, widths.Length);
        var ranges = new BarRange[rowCount][];
        for (int r = 0; r < rowCount; r++)
        {
            int segCount = Math.Min(starts[r].Length, widths[r].Length);
            ranges[r] = new BarRange[segCount];
            for (int s2 = 0; s2 < segCount; s2++)
            {
                ranges[r][s2] = new BarRange(starts[r][s2], widths[r][s2]);
            }
        }
        var s = axes.BrokenBarH(ranges);
        if (dto.BarHeight is { } barHeight) s.BarHeight = barHeight;
        if (dto.Color is { } color) s.Color = color;
        if (dto.Categories is { } categories) s.Labels = categories;
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Long form: one row per bar segment, named by its row's own label where the series carries one.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.OfGroups([new("row", DataColumnKind.Text), ChartDataColumn.Position(DataAxis.X, "start"), new("width")], Labels, "",
            Ranges, static range => [DataCell.FromNumber(range.Start), DataCell.FromNumber(range.Width)]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
