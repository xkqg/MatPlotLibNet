// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a radar (spider) chart series displaying multi-axis categorical data.</summary>
public sealed class RadarSeries : ChartSeries, IHasColor, IHasAlpha
{
    public string[] Categories { get; }

    public double[] Values { get; }

    public Color? Color { get; set; }

    public Color? FillColor { get; set; }

    public double Alpha { get; set; } = 0.25;

    public double LineWidth { get; set; } = 2.0;

    public double? MaxValue { get; set; }

    /// <summary>Creates a new radar series from the given categories and values.</summary>
    public RadarSeries(string[] categories, double[] values)
    {
        Categories = categories;
        Values = values;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "radar",
        Categories = Categories, Values = Values,
        Color = Color, FillColor = FillColor,
        Alpha = Alpha, LineWidth = LineWidth, MaxValue = MaxValue
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
