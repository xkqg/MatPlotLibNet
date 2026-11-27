// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a pseudocolor series on a triangular mesh, coloring each triangle by the mean Z value.</summary>
public sealed class TripcolorSeries : ChartSeries, IColorBarDataProvider, IColorScaled, IColormappable, INormalizable, ISeriesKind<TripcolorSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>tripcolor</c> asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    /// <summary>The x coordinate of each mesh point, in data units.</summary>
    public Vec X { get; }

    /// <summary>The y coordinate of each mesh point, in data units; one value per <see cref="X"/> entry.</summary>
    public Vec Y { get; }

    /// <summary>The value at each mesh point, one per <see cref="X"/> entry. A triangle is coloured by the mean of its
    /// three corner values.</summary>
    public Vec Z { get; }

    /// <summary>The triangles as a flat list of point indices, three per triangle (into <see cref="X"/>, <see cref="Y"/>
    /// and <see cref="Z"/>). When null, the mesh is built by Delaunay triangulation of the points.</summary>
    /// <exception cref="ArgumentException">The list is not whole triangles (a multiple of three indices), or an index names a
    /// point there is not; the list is refused when it is set, and so when a document that carries it is read, and not when the
    /// figure is drawn.</exception>
    public int[]? Triangles
    {
        get;
        set
        {
            value?.ThrowIfNotCornersOf(X.Length);
            Change(ref field, value);
        }
    }

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>Over the drawable Z values only, so a missing value (NaN) does not turn the whole range into NaN; the
    /// renderer colours the triangles over this same range.</remarks>
    public MinMaxRange GetColorBarRange() => this.ColourRangeUnder(Theme.Default);

    /// <inheritdoc />
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => mapping.RangeOf(Z.Data);

    /// <summary>Initializes a new instance of <see cref="TripcolorSeries"/>.</summary>
    public TripcolorSeries(Vec x, Vec y, Vec z)
    {
        X = x; Y = y; Z = z;
    }

    /// <inheritdoc />
    internal override bool HoldsNoData => X.Length == 0;

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => OverItsPoints(X.Data, Y.Data);

    static string ISeriesKind<TripcolorSeries>.Kind => "tripcolor";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        ZData = Z,
        Triangles = Triangles,
        ColorScale = this.ColorScaleOnTheWire(),
    };

    /// <summary>The reader of a <see cref="TripcolorSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static TripcolorSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Tripcolor(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? []);
        if (dto.Triangles is { } triangles) s.Triangles = triangles;
        s.RestoreColorScale(dto);
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The unstructured points the triangulation is built over.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.XYZ(X.Data, Y.Data, Z.Data, Label ?? "z");

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
