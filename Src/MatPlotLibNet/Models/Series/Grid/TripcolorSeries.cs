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
    public (double Min, double Max) GetColorBarRange() =>
        Z.Length > 0 ? (Z.Min(), Z.Max()) : (0, 1);

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

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
