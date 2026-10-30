// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a bar chart in polar coordinates with wedge-shaped bars.</summary>
public sealed class PolarBarSeries : PolarSeries, IHasColor, IHasAlpha
{
    /// <summary>The angular width of each wedge, in radians, centred on its angle. Default 0.3; a larger value gives
    /// wider wedges, and bars spaced evenly round a full circle touch when the width is 2π divided by their number.</summary>
    public double BarWidth { get; set; } = 0.3;

    /// <inheritdoc />
    public Color? Color { get; set; }

    /// <inheritdoc />
    public double Alpha { get; set; } = 0.8;

    /// <summary>Initializes a new polar bar series.</summary>
    public PolarBarSeries(double[] r, double[] theta) : base(r, theta) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "polarbar",
        RData = R,
        ThetaData = Theta,
        Color = Color,
        Alpha = Alpha.OrNullAtRest(0.8),
        BarWidth = BarWidth.OrNullAtRest(0.3),
    };

    /// <summary>Reconstructs a <see cref="PolarBarSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static PolarBarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.PolarBar(dto.RData ?? [], dto.ThetaData ?? []);
        s.Color = dto.Color;
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        if (dto.BarWidth.HasValue)
        {
            s.BarWidth = dto.BarWidth.Value;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
