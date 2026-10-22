// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a line chart series connecting data points with a line.</summary>
public sealed class LineSeries : XYSeries, IHasColor, IHasLabelPlacement
{
    public Color? Color { get; set; }

    /// <inheritdoc />
    public SeriesLabelPlacement LabelPlacement { get; set; } = SeriesLabelPlacement.Legend;

    /// <inheritdoc />
    LinePoints IHasLabelPlacement.LinePoints() => new(XData, YData);

    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    public double LineWidth { get; set; } = 1.5;

    public MarkerStyle? Marker { get; set; }

    public double MarkerSize { get; set; } = RestingMarkerSize;

    public Color? MarkerFaceColor { get; set; }

    public Color? MarkerEdgeColor { get; set; }

    public double MarkerEdgeWidth { get; set; } = RestingMarkerEdgeWidth;

    /// <summary>The marker size a new line holds; a step, which has no marker size, draws its markers at this one.</summary>
    internal const double RestingMarkerSize = 6;
    private const double RestingMarkerEdgeWidth = 1.0;

    public DrawStyle? DrawStyle { get; set; }

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
        ChartSerializer.ApplyEnum<LineStyle>(dto.LineStyle, v => LineStyle = v);
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
