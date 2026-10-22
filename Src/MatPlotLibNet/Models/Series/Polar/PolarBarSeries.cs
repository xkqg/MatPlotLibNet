// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a bar chart in polar coordinates with wedge-shaped bars.</summary>
public sealed class PolarBarSeries : PolarSeries, IHasColor, IHasAlpha
{
    public double BarWidth { get; set; } = 0.3;

    public Color? Color { get; set; }

    public double Alpha { get; set; } = 0.8;

    /// <summary>Initializes a new polar bar series.</summary>
    public PolarBarSeries(double[] r, double[] theta) : base(r, theta) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "polarbar",
        Color = Color,
        Alpha = Alpha.OrNullAtRest(0.8),
        BarWidth = BarWidth.OrNullAtRest(0.3),
    };

    /// <summary>Reconstructs a <see cref="PolarBarSeries"/> placeholder from its serialization DTO and adds it to the axes.
    /// The data is not written yet (the roadmap row "Every chart type back from JSON"); the settings are.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static PolarBarSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.PolarBar([1.0], [0.0]);
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
