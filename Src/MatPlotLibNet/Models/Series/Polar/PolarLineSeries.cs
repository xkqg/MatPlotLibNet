// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a line plot in polar coordinates (r, theta).</summary>
public sealed class PolarLineSeries : PolarSeries, IHasColor, ISeriesKind<PolarLineSeries>, IGivenStroked
{
    /// <inheritdoc />
    /// <remarks>2: <c>plot</c> on a polar axes draws a <c>Line2D</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <inheritdoc />
    /// <remarks>A line, from the stroke it was given: matplotlib's <c>HandlerLine2D</c> draws the handle of a polar <c>plot</c>.</remarks>
    internal override LegendSwatch LegendSwatch => LegendSwatch.Line(this);

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The dash pattern of the line: solid, dashed, dotted and so on. Default solid.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <inheritdoc cref="LineSeries.LineWidth"/>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The width and the dash pattern the line was given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; set => Change(ref field, value); }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <summary>Initializes a new polar line series.</summary>
    public PolarLineSeries(double[] r, double[] theta) : base(r, theta) { }

    static string ISeriesKind<PolarLineSeries>.Kind => "polarline";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        RData = R,
        ThetaData = Theta,
        Color = Color,
        Stroking = Stroke,
    };

    /// <summary>The reader of a <see cref="PolarLineSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static PolarLineSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.PolarPlot(dto.RData ?? [], dto.ThetaData ?? []);
        s.Color = dto.Color;
        s.Stroke = dto.StrokeOver(s.Stroke);
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
