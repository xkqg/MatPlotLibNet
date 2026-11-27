// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a line chart series connecting data points with a line.</summary>
public sealed class LineSeries : XYSeries, IHasColor, IHasLabelPlacement, IHasLinePoints, ISeriesKind<LineSeries>, IGivenStroked
{
    /// <inheritdoc />
    /// <remarks>2: <c>plot</c> draws a <c>Line2D</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.LineMarker, Stroke, Marker: Marker, MarkerSize: MarkerSize,
        MarkerFace: MarkerFaceColor, MarkerEdge: MarkerEdgeColor, MarkerEdgeWidth: GivenMarkerEdgeWidth);

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public SeriesLabelPlacement LabelPlacement { get; set => Change(ref field, value); } = RestingLabelPlacement;

    /// <inheritdoc />
    LinePoints IHasLinePoints.LinePoints() => new(XData, YData);

    /// <summary>The dash pattern of the line. Default solid. Given none, the line takes its slot's.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <summary>The thickness of the line, in pixels. Given none, it reads <see cref="SeriesLineLook.LibraryWidth"/> and is drawn as
    /// wide as its theme says: matplotlib's <c>lines.linewidth</c> under the matplotlib themes, that width under every other; a width
    /// that is given is drawn as given.</summary>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The width and the dash pattern the line was given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; set => Change(ref field, value); }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <summary>The shape drawn at each data point. Null (the default) leaves the choice to the chart's property cycle,
    /// which normally draws no markers; <see cref="MarkerStyle.None"/> draws none whatever the cycle says.</summary>
    public MarkerStyle? Marker { get; set => Change(ref field, value); }

    /// <summary>The size of each marker, in points. Default <see cref="MarkerLook.RestingSize"/>.</summary>
    public double MarkerSize { get; set => Change(ref field, value); } = RestingMarkerSize;

    /// <summary>The fill colour of the markers; null (the default) fills them with the line colour.</summary>
    public Color? MarkerFaceColor { get; set => Change(ref field, value); }

    /// <summary>The colour of the outline around each marker; null (the default) draws markers without an outline.</summary>
    public Color? MarkerEdgeColor { get; set => Change(ref field, value); }

    /// <summary>The thickness of the marker outline, in pixels. Default <see cref="RestingMarkerEdgeWidth"/>. Given no width, the markers are outlined as wide as their
    /// theme says (matplotlib's <c>lines.markeredgewidth</c> under the matplotlib themes, <c>MarkerLook.LineEdge</c>)
    /// and, under every other theme, are not outlined unless <see cref="MarkerEdgeColor"/> is set; a width that is
    /// given, the default included, is drawn as given (and under the library's own themes has an effect only with an edge colour).</summary>
    public double MarkerEdgeWidth { get => GivenMarkerEdgeWidth ?? RestingMarkerEdgeWidth; set => GivenMarkerEdgeWidth = value; }

    /// <summary>The marker outline width that was given, <see langword="null"/> when none was.</summary>
    internal double? GivenMarkerEdgeWidth { get; private set => Change(ref field, value); }

    /// <summary>The marker size a new line holds; a step, which has no marker size, draws its markers at this one.</summary>
    internal const double RestingMarkerSize = MarkerLook.RestingSize;
    /// <summary>The marker outline width a new line holds, in pixels.</summary>
    internal const double RestingMarkerEdgeWidth = 1.0;

    /// <summary>How the line runs between data points: null or the default style joins them with
    /// straight segments, while a step style holds each value flat until the next point, giving a staircase.</summary>
    public DrawStyle? DrawStyle { get; set => Change(ref field, value); }

    /// <summary>Draws a marker only at every N-th point, starting with the first; null (the default) marks every point.</summary>
    /// <exception cref="ArgumentException">The step is below one point; the step the line had is kept.</exception>
    public int? MarkEvery { get; set => Change(ref field, value?.ThrowIfBelow(1, nameof(MarkEvery))); }

    /// <summary>When <see langword="true"/>, the renderer applies Fritsch-Carlson monotone cubic
    /// interpolation before drawing. Produces a smooth curve that never overshoots.</summary>
    public bool Smooth { get; set => Change(ref field, value); }

    private const int RestingSmoothResolution = 10;
    /// <summary>Number of interpolated sub-points per input interval when <see cref="Smooth"/> is <see langword="true"/>.
    /// Default <see cref="RestingSmoothResolution"/>. Higher values produce smoother curves at the cost of larger SVG output.</summary>
    public int SmoothResolution { get; set => Change(ref field, value); } = RestingSmoothResolution;

    /// <summary>Initializes a new instance of <see cref="LineSeries"/> with the specified data.</summary>
    /// <param name="xData">The X-axis data values.</param>
    /// <param name="yData">The Y-axis data values.</param>
    public LineSeries(double[] xData, double[] yData) : base(xData, yData) { }

    static string ISeriesKind<LineSeries>.Kind => "line";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = XData, YData = YData, Color = Color,
        Stroking = Stroke,
        Smooth = Smooth.OrNullAtRest(false),
        SmoothResolution = Smooth ? SmoothResolution.OrNullAtRest(RestingSmoothResolution) : null,
        Marker = Marker,
        MarkerSize = MarkerSize.OrNullAtRest(RestingMarkerSize),
        MarkerFaceColor = MarkerFaceColor,
        MarkerEdgeColor = MarkerEdgeColor,
        MarkerEdgeWidth = GivenMarkerEdgeWidth,
        MarkEvery = MarkEvery,
        DrawStyle = DrawStyle,
    };

    /// <summary>The reader of a <see cref="LineSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static LineSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Plot(dto.XData ?? [], dto.YData ?? []);
        s.RestoreFrom(dto);
        return s;
    }

    /// <summary>Takes every drawn property <see cref="ToSeriesDto"/> writes back from <paramref name="dto"/>; an absent
    /// one leaves the value the line was created with. The one reader of a line's fields, whichever axis the line is on.</summary>
    internal void RestoreFrom(SeriesDto dto)
    {
        Color = dto.Color;
        Stroke = dto.StrokeOver(Stroke);
        if (dto.Smooth is { } smooth) Smooth = smooth;
        if (dto.SmoothResolution is { } smoothResolution) SmoothResolution = smoothResolution;
        Marker = dto.Marker;
        if (dto.MarkerSize is { } markerSize) MarkerSize = markerSize;
        MarkerFaceColor = dto.MarkerFaceColor;
        MarkerEdgeColor = dto.MarkerEdgeColor;
        if (dto.MarkerEdgeWidth is { } edgeWidth) MarkerEdgeWidth = edgeWidth;
        MarkEvery = dto.MarkEvery;
        DrawStyle = dto.DrawStyle;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
