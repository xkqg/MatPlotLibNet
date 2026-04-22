// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A broken-bar series (matplotlib <c>broken_barh</c> equivalent) that draws horizontal
/// bars with gaps, one row per outer array index. Each inner <see cref="BarRange"/> defines one
/// bar segment by <c>(Start, Width)</c>.</summary>
public sealed class BrokenBarSeries : ChartSeries, IHasColor
{
    /// <summary>Rows of bar segments. <c>Ranges[row][i]</c> is the <c>i</c>-th segment on <c>row</c>.</summary>
    public BarRange[][] Ranges { get; }

    /// <summary>Optional Y-axis tick labels, one per row. <see langword="null"/> to omit row labels.</summary>
    public string[]? Labels { get; set; }

    /// <summary>Vertical bar thickness in data units. Defaults to <c>0.8</c> (matplotlib default).</summary>
    public double BarHeight { get; set; } = 0.8;

    /// <summary>Uniform fill colour applied to every segment. <see langword="null"/> defers to the theme cycle.</summary>
    public Color? Color { get; set; }

    /// <summary>Initialises a new <see cref="BrokenBarSeries"/>.</summary>
    /// <param name="ranges">One <see cref="BarRange"/> array per row. Rows may have different
    /// segment counts; an empty outer array is valid and renders nothing.</param>
    public BrokenBarSeries(BarRange[][] ranges)
    {
        Ranges = ranges;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Ranges.Length == 0) return new(0, 1, -0.5, 0.5);
        var allRanges = Ranges.SelectMany(r => r).ToArray();
        double xMin = allRanges.Length > 0 ? allRanges.Min(r => r.Start) : 0;
        double xMax = allRanges.Length > 0 ? allRanges.Max(r => r.Start + r.Width) : 1;
        double yMin = -0.5, yMax = Ranges.Length - 0.5;
        return new(xMin, xMax, yMin, yMax,
            StickyXMin: xMin, StickyXMax: xMax, StickyYMin: yMin, StickyYMax: yMax);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "brokenbar",
        RangeStarts = Ranges.Select(row => row.Select(r => r.Start).ToArray()).ToArray(),
        RangeWidths = Ranges.Select(row => row.Select(r => r.Width).ToArray()).ToArray(),
        BarHeight = BarHeight,
        Color = Color,
        Categories = Labels
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
