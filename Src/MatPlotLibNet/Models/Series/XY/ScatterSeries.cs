// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a scatter chart series displaying individual data points as markers.</summary>
public sealed class ScatterSeries : XYSeries, IColormappable, INormalizable, IColorScaled, IHasColor, IHasAlpha, IHasMarkerStyle, ISeriesKind<ScatterSeries>
{
    /// <inheritdoc />
    /// <remarks>A scatter asks as matplotlib's <c>scatter</c> does; one coloured by values (<see cref="C"/>) or given a colour for each point (<see cref="Colors"/>) asks for none, as <c>scatter(c=...)</c> takes its colour map's or the colours it is given.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Scatter.NamingItsColours(C is not null || Colors is not null);

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.Scatter, Marker: GivenMarker, MarkerSize: GivenMarkerSize?.AreaAcrossInPoints);

    /// <summary>The size of each marker as an area in points squared, by position in the data, one for each point. It replaces
    /// <see cref="MarkerSize"/>; null (the default) draws every marker at <see cref="MarkerSize"/>.
    /// A point whose size is not a number is not drawn.</summary>
    /// <exception cref="ArgumentException">The array does not hold one size for each point; the sizes the scatter had are kept.</exception>
    public double[]? Sizes { get; set => Change(ref field, value.ThrowIfNotOnePer(XData.Length, nameof(Sizes))); }

    /// <summary>The fill colour of each marker, by position in the data. It wins over <see cref="C"/> and <see cref="Color"/>
    /// for every point it covers; null (the default), or a point past the end of the array, falls back to them.</summary>
    public Color[]? Colors { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The shape of every marker. Default circle. Given none, the marker is its slot's when the theme's cycler names one; a
    /// shape that is given, a circle included, is drawn as given.</summary>
    public MarkerStyle Marker { get => GivenMarker ?? RestingMarker; set => GivenMarker = value; }

    /// <summary>The marker that was given, <see langword="null"/> when none was and the marker is its slot's.</summary>
    internal MarkerStyle? GivenMarker { get; private set => Change(ref field, value); }

    /// <inheritdoc />
    MarkerStyle IHasMarkerStyle.MarkerStyle { get => Marker; set => Marker = value; }

    /// <summary>The size of every marker as an area in points squared, so the default, <see cref="RestingMarkerSize"/>, is a marker about 6 points
    /// across. <see cref="Sizes"/> replaces it per point. Given no size, the marker is the size its theme says (20 points squared
    /// under the classic theme, as matplotlib's classic <c>scatter</c>); a size that is given, the default included, is drawn as
    /// given.</summary>
    public double MarkerSize { get => GivenMarkerSize ?? RestingMarkerSize; set => GivenMarkerSize = value; }

    private const double RestingMarkerSize = 36;
    /// <summary>The marker size that was given, <see langword="null"/> when none was and the marker is the size its theme says.</summary>
    internal double? GivenMarkerSize { get; private set => Change(ref field, value); }

    /// <summary>Opacity of the markers' fill, 0 (invisible) to 1 (opaque; the default, <see cref="RestingAlpha"/>). It replaces the alpha of the colours,
    /// as matplotlib's <c>alpha</c> does, except for a fully transparent colour (a colour map's bad colour), which stays
    /// transparent. An edge keeps the colour it was given.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 0, above 1 or not a number.</exception>
    public double Alpha { get; set => Change(ref field, value.ThrowIfNotOpacity(nameof(Alpha))); } = RestingAlpha;

    private const MarkerStyle RestingMarker = MarkerStyle.Circle;
    private const double RestingAlpha = 1.0;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <summary>The outline colour of each marker, by position in the data; null (the default), or a point past the end of
    /// the array, draws no outline.</summary>
    public Color[]? EdgeColors { get; set => Change(ref field, value); }

    /// <summary>The outline thickness of each marker, in pixels, by position in the data; null (the default), or a point
    /// past the end of the array, draws no outline. A thickness shows only where <see cref="EdgeColors"/> names a colour.</summary>
    public double[]? LineWidths { get; set => Change(ref field, value); }

    /// <summary>A value for each point that colours its marker, so a third variable shows as colour. The values go through
    /// <see cref="ColorMap"/>, or through the theme's map when none is set, for every point whose colour <see cref="Colors"/>
    /// does not name; the range they span on the map is <see cref="VMin"/> to <see cref="VMax"/>.</summary>
    public double[]? C { get; set => Change(ref field, value); }

    /// <summary>The value of <see cref="C"/> that gets the first colour of the colour map; null (the default) uses the lowest
    /// value that can be drawn.</summary>
    public double? VMin { get; set => Change(ref field, value); }

    /// <summary>The value of <see cref="C"/> that gets the last colour of the colour map; null (the default) uses the highest
    /// value that can be drawn.</summary>
    public double? VMax { get; set => Change(ref field, value); }

    /// <inheritdoc />
    /// <remarks>The values of <see cref="C"/> between the ends given (<see cref="VMin"/>, <see cref="VMax"/>).</remarks>
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => mapping.RangeOf(C, VMin, VMax);

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }


    /// <summary>Initializes a new instance of <see cref="ScatterSeries"/> with the specified data.</summary>
    /// <param name="xData">The X-axis data values.</param>
    /// <param name="yData">The Y-axis data values.</param>
    public ScatterSeries(double[] xData, double[] yData) : base(xData, yData) { }

    static string ISeriesKind<ScatterSeries>.Kind => "scatter";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = XData, YData = YData, Color = Color,
        MarkerSize = GivenMarkerSize,
        Marker = GivenMarker,
        Alpha = Alpha.OrNullAtRest(RestingAlpha),
        Sizes = Sizes,
        Colors = Colors is null ? null : [.. Colors],
        EdgeColors = EdgeColors is null ? null : [.. EdgeColors],
        LineWidths = LineWidths,
        C = C,
        ColorScale = this.ColorScaleOnTheWire(),
        VMin = VMin,
        VMax = VMax,
    };

    /// <summary>The reader of a <see cref="ScatterSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static ScatterSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Scatter(dto.XData ?? [], dto.YData ?? []);
        s.RestoreFrom(dto);
        return s;
    }

    /// <summary>Takes every drawn property <see cref="ToSeriesDto"/> writes back from <paramref name="dto"/>; an absent
    /// one leaves the value the scatter was created with. The one reader of a scatter's fields, whichever axis the
    /// scatter is on.</summary>
    /// <exception cref="ArgumentException">The DTO's colour map description cannot make a map; the exception names the field.</exception>
    internal void RestoreFrom(SeriesDto dto)
    {
        Color = dto.Color;
        if (dto.MarkerSize is { } size) MarkerSize = size;
        if (dto.Marker is { } marker) Marker = marker;
        if (dto.Alpha is { } alpha) Alpha = alpha;
        Sizes = dto.Sizes;
        Colors = dto.Colors?.ToArray();
        EdgeColors = dto.EdgeColors?.ToArray();
        LineWidths = dto.LineWidths;
        C = dto.C;
        this.RestoreColorScale(dto);
        VMin = dto.VMin;
        VMax = dto.VMax;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
