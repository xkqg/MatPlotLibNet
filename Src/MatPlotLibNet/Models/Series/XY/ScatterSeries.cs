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

    public double[]? Sizes { get; set; }

    public Color[]? Colors { get; set; }

    public Color? Color { get; set; }

    public MarkerStyle Marker { get; set; } = RestingMarker;

    /// <inheritdoc />
    MarkerStyle IHasMarkerStyle.MarkerStyle { get => Marker; set => Marker = value; }

    public double MarkerSize { get; set; } = 36;

    /// <summary>Opacity of the markers' fill, 0 (invisible) to 1 (opaque, the default). It replaces the alpha of the colours,
    /// as matplotlib's <c>alpha</c> does, except for a fully transparent colour (a colour map's bad colour), which stays
    /// transparent. An edge keeps the colour it was given.</summary>
    public double Alpha { get; set; } = RestingAlpha;

    private const MarkerStyle RestingMarker = MarkerStyle.Circle;
    private const double RestingAlpha = 1.0;

    public IColorMap? ColorMap { get; set; }

    public Color[]? EdgeColors { get; set; }

    public double[]? LineWidths { get; set; }

    public double[]? C { get; set; }

    public double? VMin { get; set; }

    public double? VMax { get; set; }

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
