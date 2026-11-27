// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D polyline connecting data points in three-dimensional space.</summary>
public sealed class Line3DSeries : XYZSeries, IHasColor, ISeriesKind<Line3DSeries>, IGivenStroked
{
    /// <inheritdoc />
    /// <remarks>A line, from the stroke it was given: matplotlib's <c>HandlerLine2D</c> draws the handle of a 3-D <c>plot</c>'s
    /// <c>Line3D</c>.</remarks>
    internal override LegendSwatch LegendSwatch => LegendSwatch.Line(this);

    /// <summary>Line color. When <c>null</c> the theme's prop-cycler assigns one automatically.</summary>
    public Color? Color { get; set => Change(ref field, value); }

    /// <inheritdoc cref="LineSeries.LineWidth"/>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The width and the dash pattern the line was given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; set => Change(ref field, value); }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <summary>Dash pattern for the line. Default <see cref="Styling.GivenStroke.RestingStyle"/>.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <summary>Initializes a new 3D line series with the specified data.</summary>
    public Line3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    static string ISeriesKind<Line3DSeries>.Kind => "line3d";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = X,
        YData = Y,
        ZData = Z,
        Color = Color,
        Stroking = Stroke,
        Label = Label
    };

    /// <summary>The reader of a <see cref="Line3DSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static Line3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Plot3D(dto.XData ?? [0.0], dto.YData ?? [0.0], dto.ZData ?? [0.0]);
        if (dto.Color is { } color) s.Color = color;
        s.Stroke = dto.StrokeOver(s.Stroke);
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
