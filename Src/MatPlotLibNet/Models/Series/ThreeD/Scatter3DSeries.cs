// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D scatter plot rendered as projected markers, sorted by depth.</summary>
public sealed class Scatter3DSeries : XYZSeries, IHasColor, IColormappable, INormalizable, IColorScaled, IHasMarkerStyle, ISeriesKind<Scatter3DSeries>
{
    /// <inheritdoc />
    /// <remarks><c>Axes3D.scatter</c> is matplotlib's <c>scatter</c>; one coloured through a colour map asks for no colour.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Scatter.NamingItsColours(ColorMap is not null);

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>The heights: a point is coloured by its Z.</remarks>
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => mapping.RangeOf(Z.Data);

    private const MarkerStyle RestingMarkerStyle = MarkerStyle.Circle;
    /// <inheritdoc />
    public MarkerStyle MarkerStyle { get; set => Change(ref field, value); } = RestingMarkerStyle;

    /// <summary>The size of each point marker, in points. Default <see cref="MarkerLook.RestingSize"/>; given no size, the marker is the size its theme says (20 points
    /// squared under the matplotlib themes, <c>Axes3D.scatter</c>'s own default), and a size that is given, the default included, is drawn as
    /// given. Under the matplotlib themes every marker is that size (the default
    /// style fades the farther ones instead); under the library's own themes a marker shrinks with its distance, to half this size at
    /// the farthest.</summary>
    public double MarkerSize { get => GivenMarkerSize ?? MarkerLook.RestingSize; set => GivenMarkerSize = value; }

    /// <summary>The marker size that was given, <see langword="null"/> when none was and the marker is the size its theme says.</summary>
    internal double? GivenMarkerSize { get; private set => Change(ref field, value); }

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.Scatter3D, Marker: MarkerStyle, MarkerSize: GivenMarkerSize);

    /// <summary>Initializes a new 3D scatter series with the specified data.</summary>
    public Scatter3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    static string ISeriesKind<Scatter3DSeries>.Kind => "scatter3d";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        ZData = Z,
        Color = Color,
        ColorScale = this.ColorScaleOnTheWire(),
        Marker = MarkerStyle.OrNullAtRest(RestingMarkerStyle),
        MarkerSize = GivenMarkerSize,
        Label = Label,
    };

    /// <summary>The reader of a <see cref="Scatter3DSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static Scatter3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Scatter3D(dto.XData ?? [0.0], dto.YData ?? [0.0], dto.ZData ?? [0.0]);
        if (dto.Color is { } color) s.Color = color;
        s.RestoreColorScale(dto);
        if (dto.Marker is { } marker) s.MarkerStyle = marker;
        if (dto.MarkerSize is { } markerSize) s.MarkerSize = markerSize;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
