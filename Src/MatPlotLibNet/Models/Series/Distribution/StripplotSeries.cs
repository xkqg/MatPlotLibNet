// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a strip plot series that draws randomly jittered dots per category to show individual data points.</summary>
public sealed class StripplotSeries : DatasetSeries, IHasColor, IHasAlpha
{
    public double Jitter { get; set; } = 0.2;

    public double MarkerSize { get; set; } = 5;

    public Color? Color { get; set; }

    public double Alpha { get; set; } = 0.8;

    /// <summary>Initializes a new instance of <see cref="StripplotSeries"/> with the specified datasets.</summary>
    /// <param name="datasets">An array of datasets, each containing values for one category.</param>
    public StripplotSeries(double[][] datasets) : base(datasets) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "stripplot",
        Datasets = Datasets,
        Jitter = Jitter,
        MarkerSize = MarkerSize,
        Color = Color,
        Alpha = Alpha
    };

    /// <summary>Reconstructs a <see cref="StripplotSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static StripplotSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Stripplot(dto.Datasets ?? []);
        if (dto.Jitter.HasValue)
        {
            s.Jitter = dto.Jitter.Value;
        }
        if (dto.MarkerSize.HasValue)
        {
            s.MarkerSize = dto.MarkerSize.Value;
        }
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
