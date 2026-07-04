// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a pseudocolor series on a triangular mesh, coloring each triangle by the mean Z value.</summary>
public sealed class TripcolorSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    public Vec X { get; }

    public Vec Y { get; }

    public Vec Z { get; }

    public int[]? Triangles { get; set; }

    public IColorMap? ColorMap { get; set; }

    public INormalizer? Normalizer { get; set; }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange() =>
        Z.Length > 0 ? new(Z.Min(), Z.Max()) : new(0, 1);

    /// <summary>Initializes a new instance of <see cref="TripcolorSeries"/>.</summary>
    public TripcolorSeries(Vec x, Vec y, Vec z)
    {
        X = x; Y = y; Z = z;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0) return new(0, 1, 0, 1);
        return new(X.Min(), X.Max(), Y.Min(), Y.Max());
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "tripcolor",
        XData = X,
        YData = Y,
        ZData = Z,
        Triangles = Triangles,
        ColorMapName = ColorMap?.Name
    };

    /// <summary>Reconstructs a <see cref="TripcolorSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static TripcolorSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Tripcolor(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? []);
        if (dto.Triangles is not null)
        {
            s.Triangles = dto.Triangles;
        }
        if (dto.ColorMapName is not null)
        {
            s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
