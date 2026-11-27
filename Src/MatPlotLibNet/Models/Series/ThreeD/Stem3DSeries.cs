// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D stem series — vertical lines from the XY-plane to each (X, Y, Z) data point.</summary>
public sealed class Stem3DSeries : XYZSeries, IHasColor, ISeriesKind<Stem3DSeries>
{
    /// <inheritdoc />
    /// <remarks><c>Axes3D.stem</c> draws in <c>'C0'</c> and asks neither cycle.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Mesh;

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The size of the marker at the top of each stem, in points. Default <see cref="MarkerLook.RestingSize"/>.</summary>
    public double MarkerSize { get; set => Change(ref field, value); } = MarkerLook.RestingSize;

    /// <summary>Colour of the baseline polyline drawn at <c>z = 0</c> through every stem
    /// base point in sequence order. Matches matplotlib's <c>ax.stem(basefmt='C2-')</c>
    /// behaviour, where the stem container includes a <c>baseline</c> Line3D alongside the
    /// vertical stem lines and markers. Set to <see langword="null"/> to suppress the
    /// baseline entirely. Defaults to the same colour as the stem line (<see cref="Color"/>)
    /// so a single-colour theme override works as expected.</summary>
    public Color? BaseLineColor { get; set => Change(ref field, value); }

    /// <summary>Initializes a new instance of <see cref="Stem3DSeries"/>.</summary>
    public Stem3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    static string ISeriesKind<Stem3DSeries>.Kind => "stem3d";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        ZData = Z,
        MarkerSize = MarkerSize,
        Color = Color,
        BaselineColor = BaseLineColor,
    };

    /// <summary>The reader of a <see cref="Stem3DSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static Stem3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Stem3D(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? []);
        if (dto.MarkerSize is { } markerSize) s.MarkerSize = markerSize;
        if (dto.Color is { } color) s.Color = color;
        s.BaseLineColor = dto.BaselineColor;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
