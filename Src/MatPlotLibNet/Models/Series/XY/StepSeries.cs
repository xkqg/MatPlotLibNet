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
public sealed class StepSeries : XYSeries, IHasColor
{
    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.LineMarker, LineWidth, LineStyle, Marker: Marker, MarkerSize: LineSeries.RestingMarkerSize);

    /// <summary>Where each step happens between two neighbouring points: after the earlier point (the default), before the
    /// later point, or halfway between them.</summary>
    public StepPosition StepPosition { get; set; } = StepPosition.Post;

    /// <inheritdoc />
    public Color? Color { get; set; }

    /// <summary>The dash pattern of the step line. Default solid.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>The thickness of the step line, in pixels. Default 1.5.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>The shape drawn at each data point, not at the corners of the steps. Null (the default) leaves the choice
    /// to the chart's property cycle, which normally draws no markers; <see cref="MarkerStyle.None"/> draws none whatever
    /// the cycle says.</summary>
    public MarkerStyle? Marker { get; set; }

    /// <summary>Creates a new step series from the given X and Y data.</summary>
    public StepSeries(double[] xData, double[] yData) : base(xData, yData) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "step",
        XData = XData, YData = YData, Color = Color,
        LineStyle = LineStyle.ToString().ToLowerInvariant(),
        LineWidth = LineWidth,
        StepPosition = StepPosition.ToString().ToLowerInvariant(),
        Marker = Marker,
    };

    /// <summary>Reconstructs a <see cref="StepSeries"/> from its serialization DTO, including step position, and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static StepSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Step(dto.XData ?? [], dto.YData ?? []);
        s.Color = dto.Color;
        s.LineWidth = dto.LineWidth ?? 1.5;
        dto.LineStyle.ApplyEnum<LineStyle>(v => s.LineStyle = v);
        dto.StepPosition.ApplyEnum<StepPosition>(v => s.StepPosition = v);
        s.Marker = dto.Marker;
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
