// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a sparkline — a tiny, word-sized line chart with no axes, labels, or decorations.</summary>
/// <remarks>Designed for inline dashboard use. Renders only the line within the full plot bounds.</remarks>
public sealed class SparklineSeries : ChartSeries, IHasColor
{
    public double[] Values { get; }

    public Color? Color { get; set; }

    public double LineWidth { get; set; } = 1.5;

    /// <summary>Creates a new sparkline series from the given values.</summary>
    public SparklineSeries(double[] values) => Values = values;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) =>
        new(0, Values.Length - 1, Values.Min(), Values.Max());

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "sparkline",
        Values = Values, Color = Color, LineWidth = LineWidth
    };

    /// <summary>Reconstructs a <see cref="SparklineSeries"/> from its serialization DTO, including colour and line width, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static SparklineSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Sparkline(dto.Values ?? []);
        s.Color = dto.Color;
        s.LineWidth = dto.LineWidth ?? 1.5;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
