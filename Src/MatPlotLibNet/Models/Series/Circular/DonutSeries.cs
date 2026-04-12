// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a donut chart — a pie chart with a hollow center for displaying a summary value.</summary>
public sealed class DonutSeries : ChartSeries
{
    public double[] Sizes { get; }
    public string[]? Labels { get; set; }
    public Color[]? Colors { get; set; }
    public double InnerRadius { get; set; } = 0.4;
    public string? CenterText { get; set; }
    public double StartAngle { get; set; } = 90;

    /// <summary>Initializes a new <see cref="DonutSeries"/> with the given slice sizes.</summary>
    /// <param name="sizes">Fractional or absolute size of each slice; they will be normalized automatically.</param>
    public DonutSeries(double[] sizes) => Sizes = sizes;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "donut",
        Sizes = Sizes, PieLabels = Labels,
        InnerRadius = InnerRadius, CenterText = CenterText,
        StartAngle = StartAngle
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
