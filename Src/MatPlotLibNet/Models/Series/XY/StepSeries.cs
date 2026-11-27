// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Specifies where the step transition occurs relative to adjacent data points.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum StepPosition
{
    /// <summary>Step occurs before the data point (step at the beginning of the interval).</summary>
    Pre = 0,

    /// <summary>Step occurs at the midpoint between data points.</summary>
    Mid = 1,

    /// <summary>Step occurs after the data point (step at the end of the interval).</summary>
    Post = 2,
}

/// <summary>Represents a step-function line series.</summary>
public sealed class StepSeries : XYSeries, IHasColor, ISeriesKind<StepSeries>, IGivenStroked
{
    /// <inheritdoc />
    /// <remarks>2: <c>step</c> draws a <c>Line2D</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Line;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.LineMarker, Stroke, Marker: Marker, MarkerSize: LineSeries.RestingMarkerSize);

    /// <summary>Where each step happens between two neighbouring points: after the earlier point (the default), before the
    /// later point, or halfway between them.</summary>
    public StepPosition StepPosition { get; set => Change(ref field, value); } = StepPosition.Post;

    /// <summary>The draw style the step's position names, as matplotlib's <c>step(where=...)</c> draws its line with
    /// <c>drawstyle="steps-" + where</c>: the corners are the ones a stepped line has.</summary>
    internal DrawStyle DrawStyle => StepPosition switch
    {
        StepPosition.Pre => DrawStyle.StepsPre,
        StepPosition.Mid => DrawStyle.StepsMid,
        _ => DrawStyle.StepsPost,
    };

    /// <inheritdoc />
    public Color? Color { get; set => Change(ref field, value); }

    /// <summary>The dash pattern of the step line. Default solid.</summary>
    public LineStyle LineStyle { get => Stroke.StyleOrResting; set => Stroke = Stroke with { Style = value }; }

    /// <inheritdoc cref="LineSeries.LineWidth"/>
    public double LineWidth { get => this.ReportedLineWidth; set => Stroke = Stroke with { Width = value }; }

    /// <summary>The width and the dash pattern the line was given, each <see langword="null"/> while it was not.</summary>
    internal GivenStroke Stroke { get; set => Change(ref field, value); }

    GivenStroke IGivenStroked.Stroke => Stroke;

    /// <summary>The shape drawn at each data point, not at the corners of the steps. Null (the default) leaves the choice
    /// to the chart's property cycle, which normally draws no markers; <see cref="MarkerStyle.None"/> draws none whatever
    /// the cycle says.</summary>
    public MarkerStyle? Marker { get; set => Change(ref field, value); }

    /// <summary>Creates a new step series from the given X and Y data.</summary>
    public StepSeries(double[] xData, double[] yData) : base(xData, yData) { }

    static string ISeriesKind<StepSeries>.Kind => "step";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        XData = XData, YData = YData, Color = Color,
        Stroking = Stroke,
        StepPosition = StepPosition.ToWireName(),
        Marker = Marker,
    };

    /// <summary>The reader of a <see cref="StepSeries"/> that <see cref="SeriesRegistry"/> takes; it also carries step position.</summary>
    internal static StepSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Step(dto.XData ?? [], dto.YData ?? []);
        s.Color = dto.Color;
        s.Stroke = dto.StrokeOver(s.Stroke);
        if (dto.StepPosition.WireEnum<StepPosition>() is { } stepPosition) s.StepPosition = stepPosition;
        s.Marker = dto.Marker;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
