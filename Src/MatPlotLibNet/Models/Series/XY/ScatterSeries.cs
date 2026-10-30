// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a scatter chart series displaying individual data points as markers.</summary>
public sealed class ScatterSeries : XYSeries, IColormappable, INormalizable, IHasColor, IHasAlpha, IHasMarkerStyle
{
    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.Marker, Marker: Marker, MarkerSize: MarkerSize);

    /// <summary>The size of each marker as an area in points squared, by position in the data. It replaces
    /// <see cref="MarkerSize"/> for every point it covers; null (the default) draws every marker at <see cref="MarkerSize"/>.
    /// A point whose size is not a number is not drawn.</summary>
    public double[]? Sizes { get; set; }

    /// <summary>The fill colour of each marker, by position in the data. It wins over <see cref="C"/> and <see cref="Color"/>
    /// for every point it covers; null (the default), or a point past the end of the array, falls back to them.</summary>
    public Color[]? Colors { get; set; }

    /// <inheritdoc />
    public Color? Color { get; set; }

    /// <summary>The shape of every marker. Default circle.</summary>
    public MarkerStyle Marker { get; set; } = RestingMarker;

    /// <inheritdoc />
    MarkerStyle IHasMarkerStyle.MarkerStyle { get => Marker; set => Marker = value; }

    /// <summary>The size of every marker as an area in points squared, so 36, the default, is a marker about 6 points
    /// across. <see cref="Sizes"/> replaces it per point.</summary>
    public double MarkerSize { get; set; } = 36;

    /// <summary>Opacity of the markers' fill, 0 (invisible) to 1 (opaque, the default). It replaces the alpha of the colours,
    /// as matplotlib's <c>alpha</c> does, except for a fully transparent colour (a colour map's bad colour), which stays
    /// transparent. An edge keeps the colour it was given.</summary>
    public double Alpha { get; set; } = RestingAlpha;

    private const MarkerStyle RestingMarker = MarkerStyle.Circle;
    private const double RestingAlpha = 1.0;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set; }

    /// <summary>The outline colour of each marker, by position in the data; null (the default), or a point past the end of
    /// the array, draws no outline.</summary>
    public Color[]? EdgeColors { get; set; }

    /// <summary>The outline thickness of each marker, in pixels, by position in the data; null (the default), or a point
    /// past the end of the array, draws no outline. A thickness shows only where <see cref="EdgeColors"/> names a colour.</summary>
    public double[]? LineWidths { get; set; }

    /// <summary>A value for each point that colours its marker through <see cref="ColorMap"/>, so a third variable shows as
    /// colour. It is used only when a colour map is set and <see cref="Colors"/> does not name the point's colour; the
    /// range it spans on the map is <see cref="VMin"/> to <see cref="VMax"/>.</summary>
    public double[]? C { get; set; }

    /// <summary>The value of <see cref="C"/> that gets the first colour of the colour map; null (the default) uses the lowest
    /// value that can be drawn.</summary>
    public double? VMin { get; set; }

    /// <summary>The value of <see cref="C"/> that gets the last colour of the colour map; null (the default) uses the highest
    /// value that can be drawn.</summary>
    public double? VMax { get; set; }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set; }


    /// <summary>Initializes a new instance of <see cref="ScatterSeries"/> with the specified data.</summary>
    /// <param name="xData">The X-axis data values.</param>
    /// <param name="yData">The Y-axis data values.</param>
    public ScatterSeries(double[] xData, double[] yData) : base(xData, yData) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "scatter",
        XData = XData, YData = YData, Color = Color,
        MarkerSize = MarkerSize,
        Marker = Marker.OrNullAtRest(RestingMarker),
        Alpha = Alpha.OrNullAtRest(RestingAlpha),
        Sizes = Sizes,
        Colors = Colors is null ? null : [.. Colors],
        EdgeColors = EdgeColors is null ? null : [.. EdgeColors],
        LineWidths = LineWidths,
        C = C,
        ColorMapName = ColorMap.NameForJson(),
        ColorMap = ColorMap.DescriptionForJson(),
        VMin = VMin,
        VMax = VMax,
        Normalizer = this.DescribeNormalizer(),
    };

    /// <summary>Reconstructs a <see cref="ScatterSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static ScatterSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Scatter(dto.XData ?? [], dto.YData ?? []);
        s.RestoreFrom(dto);
        return s;
    }

    /// <summary>Takes every drawn property <see cref="ToSeriesDto"/> writes back from <paramref name="dto"/>; an absent
    /// one leaves the value the scatter was created with. The one reader of a scatter's fields, whichever axis the
    /// scatter is on.</summary>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    internal void RestoreFrom(SeriesDto dto)
    {
        Color = dto.Color;
        MarkerSize = dto.MarkerSize ?? MarkerSize;
        Marker = dto.Marker ?? RestingMarker;
        Alpha = dto.Alpha ?? RestingAlpha;
        Sizes = dto.Sizes;
        Colors = dto.Colors?.ToArray();
        EdgeColors = dto.EdgeColors?.ToArray();
        LineWidths = dto.LineWidths;
        C = dto.C;
        this.RestoreColorMap(dto);
        VMin = dto.VMin;
        VMax = dto.VMax;
        this.RestoreNormalizer(dto);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
