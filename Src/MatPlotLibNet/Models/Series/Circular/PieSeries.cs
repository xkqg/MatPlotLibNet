// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a pie chart series displaying proportional data as circular slices.</summary>
public sealed class PieSeries : ChartSeries
{
    public double[] Sizes { get; }

    public string[]? Labels { get; set; }

    public Color[]? Colors { get; set; }

    public double StartAngle { get; set; } = 90;

    public bool CounterClockwise { get; set; }

    public double[]? Explode { get; set; }

    public string? AutoPct { get; set; }

    public bool Shadow { get; set; }

    public double? Radius { get; set; }

    public HatchPattern[]? Hatches { get; set; }

    /// <summary>Initializes a new instance of <see cref="PieSeries"/> with the specified slice sizes.</summary>
    /// <param name="sizes">The numeric sizes of each pie slice.</param>
    public PieSeries(double[] sizes)
    {
        Sizes = sizes;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(null, null, null, null);

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "pie",
        Sizes = Sizes, PieLabels = Labels
    };

    /// <summary>Reconstructs a <see cref="PieSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static PieSeries FromSeriesDto(Axes axes, SeriesDto dto)
        => axes.Pie(dto.Sizes ?? [], dto.PieLabels);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
