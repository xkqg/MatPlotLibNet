// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A line in a ternary diagram: a polyline through the compositions of three parts, in the order given. A drift, a
/// mixing line or the path of a process reads as the line it is, straight between two samples.</summary>
public sealed class TernaryLineSeries : TernarySeries, IHasColor, ISeriesKind<TernaryLineSeries>, IGivenStroked
{
    /// <inheritdoc />
    /// <remarks>2: mpltern's <c>plot</c> draws a <c>Line2D</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <summary>The colour of the line; the theme's colour for this series when <see langword="null"/>.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The line style; solid unless said otherwise.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <inheritdoc cref="LineSeries.LineWidth"/>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The width and the dash pattern the line was given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; set => Change(ref field, value); }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => LegendSwatch.Line(this);

    /// <summary>Initializes a new ternary line series. Built through <c>Axes.TernaryPlot</c>, which refuses a sample that cannot
    /// be placed.</summary>
    /// <param name="top">The part at the apex, for each sample.</param>
    /// <param name="left">The part at the bottom left corner, for each sample.</param>
    /// <param name="right">The part at the bottom right corner, for each sample.</param>
    public TernaryLineSeries(double[] top, double[] left, double[] right) : base(top, left, right) { }

    static string ISeriesKind<TernaryLineSeries>.Kind => "ternaryline";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        TopData = Top,
        LeftData = Left,
        RightData = Right,
        Color = Color,
        Stroking = Stroke,
    };

    /// <summary>The reader of a <see cref="TernaryLineSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static TernaryLineSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.TernaryPlot(dto.TopData ?? [], dto.LeftData ?? [], dto.RightData ?? []);
        s.Color = dto.Color;
        s.Stroke = dto.StrokeOver(s.Stroke);

        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
