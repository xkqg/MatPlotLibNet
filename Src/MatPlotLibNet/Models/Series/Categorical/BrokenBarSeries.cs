// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a broken bar series that draws horizontal bars with gaps, one row per set of time ranges.</summary>
public sealed class BrokenBarSeries : ChartSeries, IHasColor
{
    public (double Start, double Width)[][] Ranges { get; }

    public string[]? Labels { get; set; }

    public double BarHeight { get; set; } = 0.8;

    public Color? Color { get; set; }

    /// <summary>Initializes a new instance of <see cref="BrokenBarSeries"/> with the specified ranges.</summary>
    /// <param name="ranges">An array of (Start, Width) range sets, one per row.</param>
    public BrokenBarSeries((double Start, double Width)[][] ranges)
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
        return new(xMin, xMax, -0.5, Ranges.Length - 0.5);
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
