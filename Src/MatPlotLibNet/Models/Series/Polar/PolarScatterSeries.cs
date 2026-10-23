// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a scatter plot in polar coordinates (r, theta).</summary>
public sealed class PolarScatterSeries : PolarSeries, IHasColor
{
    public Color? Color { get; set; }

    public double MarkerSize { get; set; } = 6;

    /// <summary>Initializes a new polar scatter series.</summary>
    public PolarScatterSeries(double[] r, double[] theta) : base(r, theta) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "polarscatter",
        RData = R,
        ThetaData = Theta,
        Color = Color,
        MarkerSize = MarkerSize.OrNullAtRest(6.0),
    };

    /// <summary>Reconstructs a <see cref="PolarScatterSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static PolarScatterSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.PolarScatter(dto.RData ?? [], dto.ThetaData ?? []);
        s.Color = dto.Color;
        if (dto.MarkerSize.HasValue)
        {
            s.MarkerSize = dto.MarkerSize.Value;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
