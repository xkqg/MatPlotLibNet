// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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
