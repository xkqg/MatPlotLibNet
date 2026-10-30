// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a line chart series connecting data points with a line.</summary>
public sealed class LineSeries : XYSeries, IHasColor, IHasLabelPlacement
{
    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.LineMarker, LineWidth, LineStyle, Marker: Marker, MarkerSize: MarkerSize);

    /// <inheritdoc />
    public Color? Color { get; set; }

    /// <inheritdoc />
    public SeriesLabelPlacement LabelPlacement { get; set; } = SeriesLabelPlacement.Legend;

    /// <inheritdoc />
    LinePoints IHasLabelPlacement.LinePoints() => new(XData, YData);

    /// <summary>The dash pattern of the line. Default solid.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>The thickness of the line, in pixels. Default 1.5.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>The shape drawn at each data point. Null (the default) leaves the choice to the chart's property cycle,
    /// which normally draws no markers; <see cref="MarkerStyle.None"/> draws none whatever the cycle says.</summary>
    public MarkerStyle? Marker { get; set; }

    /// <summary>The size of each marker, in points. Default 6.</summary>
    public double MarkerSize { get; set; } = RestingMarkerSize;

    /// <summary>The fill colour of the markers; null (the default) fills them with the line colour.</summary>
    public Color? MarkerFaceColor { get; set; }

    /// <summary>The colour of the outline around each marker; null (the default) draws markers without an outline.</summary>
    public Color? MarkerEdgeColor { get; set; }

    /// <summary>The thickness of the marker outline, in pixels. Default 1; it has an effect only when
    /// <see cref="MarkerEdgeColor"/> is set.</summary>
    public double MarkerEdgeWidth { get; set; } = RestingMarkerEdgeWidth;

    /// <summary>The marker size a new line holds; a step, which has no marker size, draws its markers at this one.</summary>
    internal const double RestingMarkerSize = 6;
    private const double RestingMarkerEdgeWidth = 1.0;

    /// <summary>How the line runs between data points: null or the default style joins them with
    /// straight segments, while a step style holds each value flat until the next point, giving a staircase.</summary>
    public DrawStyle? DrawStyle { get; set; }

    /// <summary>Draws a marker only at every N-th point, starting with the first; null (the default) marks every point.</summary>
    public int? MarkEvery { get; set; }

    /// <summary>When <see langword="true"/>, the renderer applies Fritsch-Carlson monotone cubic
    /// interpolation before drawing. Produces a smooth curve that never overshoots.</summary>
    public bool Smooth { get; set; }

    /// <summary>Number of interpolated sub-points per input interval when <see cref="Smooth"/> is <see langword="true"/>.
    /// Default 10. Higher values produce smoother curves at the cost of larger SVG output.</summary>
    public int SmoothResolution { get; set; } = 10;

    /// <summary>Initializes a new instance of <see cref="LineSeries"/> with the specified data.</summary>
    /// <param name="xData">The X-axis data values.</param>
    /// <param name="yData">The Y-axis data values.</param>
    public LineSeries(double[] xData, double[] yData) : base(xData, yData) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "line",
        XData = XData, YData = YData, Color = Color,
        LineStyle = LineStyle.ToString().ToLowerInvariant(),
        LineWidth = LineWidth,
        Smooth = Smooth ? true : null,
        SmoothResolution = Smooth && SmoothResolution != 10 ? SmoothResolution : null,
        Marker = Marker,
        MarkerSize = MarkerSize.OrNullAtRest(RestingMarkerSize),
        MarkerFaceColor = MarkerFaceColor,
        MarkerEdgeColor = MarkerEdgeColor,
        MarkerEdgeWidth = MarkerEdgeWidth.OrNullAtRest(RestingMarkerEdgeWidth),
        MarkEvery = MarkEvery,
        DrawStyle = DrawStyle,
    };

    /// <summary>Reconstructs a <see cref="LineSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static LineSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Plot(dto.XData ?? [], dto.YData ?? []);
        s.RestoreFrom(dto);
        return s;
    }

    /// <summary>Takes every drawn property <see cref="ToSeriesDto"/> writes back from <paramref name="dto"/>; an absent
    /// one leaves the value the line was created with. The one reader of a line's fields, whichever axis the line is on.</summary>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    internal void RestoreFrom(SeriesDto dto)
    {
        Color = dto.Color;
        LineWidth = dto.LineWidth ?? 1.5;
        dto.LineStyle.ApplyEnum<LineStyle>(v => LineStyle = v);
        Smooth = dto.Smooth == true;
        SmoothResolution = dto.SmoothResolution ?? SmoothResolution;
        Marker = dto.Marker;
        MarkerSize = dto.MarkerSize ?? RestingMarkerSize;
        MarkerFaceColor = dto.MarkerFaceColor;
        MarkerEdgeColor = dto.MarkerEdgeColor;
        MarkerEdgeWidth = dto.MarkerEdgeWidth ?? RestingMarkerEdgeWidth;
        MarkEvery = dto.MarkEvery;
        DrawStyle = dto.DrawStyle;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
