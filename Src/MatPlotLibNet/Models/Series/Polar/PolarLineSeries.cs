// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a line plot in polar coordinates (r, theta).</summary>
public sealed class PolarLineSeries : PolarSeries, IHasColor
{
    public Color? Color { get; set; }

    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    public double LineWidth { get; set; } = 1.5;

    /// <summary>Initializes a new polar line series.</summary>
    public PolarLineSeries(double[] r, double[] theta) : base(r, theta) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "polarline",
        RData = R,
        ThetaData = Theta,
        Color = Color,
        LineStyle = LineStyle == LineStyle.Solid ? null : LineStyle.ToString().ToLowerInvariant(),
        LineWidth = LineWidth.OrNullAtRest(1.5),
    };

    /// <summary>Reconstructs a <see cref="PolarLineSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static PolarLineSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.PolarPlot(dto.RData ?? [], dto.ThetaData ?? []);
        s.Color = dto.Color;
        ChartSerializer.ApplyEnum<LineStyle>(dto.LineStyle, v => s.LineStyle = v);
        if (dto.LineWidth.HasValue)
        {
            s.LineWidth = dto.LineWidth.Value;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
