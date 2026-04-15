// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a count plot series that draws a bar chart from the frequency of raw categorical values.</summary>
public sealed class CountSeries : ChartSeries, ICategoryLabeled, IHasColor
{
    public string[] Values { get; }

    /// <inheritdoc />
    string[]? ICategoryLabeled.CategoryLabels => Values.Distinct().ToArray();

    public Color? Color { get; set; }

    public BarOrientation Orientation { get; set; } = BarOrientation.Vertical;

    public double BarWidth { get; set; } = 0.8;

    /// <summary>Initializes a new instance of <see cref="CountSeries"/> with the specified raw values.</summary>
    /// <param name="values">The raw categorical values to count.</param>
    public CountSeries(string[] values)
    {
        Values = values;
    }

    /// <summary>Computes the frequency counts grouped by distinct category.</summary>
    public Dictionary<string, int> ComputeCounts()
        => Values.GroupBy(v => v).ToDictionary(g => g.Key, g => g.Count());

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Values.Length == 0) return new(0, 1, 0, 1);
        int maxCount = Values.GroupBy(v => v).Max(g => g.Count());
        int catCount = Values.Distinct().Count();
        return new(-0.5, catCount - 0.5, 0, maxCount, StickyXMin: -0.5, StickyXMax: catCount - 0.5);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "count",
        Categories = Values,
        Color = Color,
        Orientation = Orientation.ToString().ToLowerInvariant(),
        BarWidth = BarWidth
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
