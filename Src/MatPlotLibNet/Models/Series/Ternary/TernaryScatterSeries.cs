// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A scatter plot in a ternary diagram: a marker on each composition of three parts.</summary>
public sealed class TernaryScatterSeries : TernarySeries, IHasColor
{
    /// <summary>The colour of the markers; the theme's colour for this series when <see langword="null"/>.</summary>
    public Color? Color { get; set; }

    /// <summary>The diameter of a marker, in points; 6 unless said otherwise.</summary>
    public double MarkerSize { get; set; } = RestingMarkerSize;

    private const double RestingMarkerSize = 6;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.Marker, MarkerSize: MarkerSize);

    /// <summary>Initializes a new ternary scatter series. Built through <c>Axes.TernaryScatter</c>, which refuses a sample that
    /// cannot be placed.</summary>
    /// <param name="top">The part at the apex, for each sample.</param>
    /// <param name="left">The part at the bottom left corner, for each sample.</param>
    /// <param name="right">The part at the bottom right corner, for each sample.</param>
    public TernaryScatterSeries(double[] top, double[] left, double[] right) : base(top, left, right) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "ternaryscatter",
        TopData = Top,
        LeftData = Left,
        RightData = Right,
        Color = Color,
        MarkerSize = MarkerSize.OrNullAtRest(RestingMarkerSize),
    };

    /// <summary>Reconstructs a <see cref="TernaryScatterSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static TernaryScatterSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.TernaryScatter(dto.TopData ?? [], dto.LeftData ?? [], dto.RightData ?? []);
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
