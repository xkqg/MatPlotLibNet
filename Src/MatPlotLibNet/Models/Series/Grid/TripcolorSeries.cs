// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a pseudocolor series on a triangular mesh, coloring each triangle by the mean Z value.</summary>
public sealed class TripcolorSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    /// <summary>Gets the X coordinates of the mesh vertices.</summary>
    public Vec X { get; }

    /// <summary>Gets the Y coordinates of the mesh vertices.</summary>
    public Vec Y { get; }

    /// <summary>Gets the Z values at each vertex (used to color triangles by mean).</summary>
    public Vec Z { get; }

    /// <summary>Gets or sets the triangle index array. If <see langword="null"/>, Delaunay triangulation is computed automatically.</summary>
    public int[]? Triangles { get; set; }

    /// <summary>Gets or sets the color map.</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Gets or sets the normalizer.</summary>
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
