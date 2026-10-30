// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>A line in a ternary diagram: a polyline through the compositions of three parts, in the order given. A drift, a
/// mixing line or the path of a process reads as the line it is, straight between two samples.</summary>
public sealed class TernaryLineSeries : TernarySeries, IHasColor
{
    /// <summary>The colour of the line; the theme's colour for this series when <see langword="null"/>.</summary>
    public Color? Color { get; set; }

    /// <summary>The line style; solid unless said otherwise.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>The width of the line, in pixels; 1.5 unless said otherwise.</summary>
    public double LineWidth { get; set; } = RestingLineWidth;

    private const double RestingLineWidth = 1.5;

    /// <inheritdoc />
    internal override LegendSwatch LegendSwatch => new(LegendSwatchKind.Line, LineWidth, LineStyle);

    /// <summary>Initializes a new ternary line series. Built through <c>Axes.TernaryPlot</c>, which refuses a sample that cannot
    /// be placed.</summary>
    /// <param name="top">The part at the apex, for each sample.</param>
    /// <param name="left">The part at the bottom left corner, for each sample.</param>
    /// <param name="right">The part at the bottom right corner, for each sample.</param>
    public TernaryLineSeries(double[] top, double[] left, double[] right) : base(top, left, right) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "ternaryline",
        TopData = Top,
        LeftData = Left,
        RightData = Right,
        Color = Color,
        LineStyle = LineStyle == LineStyle.Solid ? null : LineStyle.ToString().ToLowerInvariant(),
        LineWidth = LineWidth.OrNullAtRest(RestingLineWidth),
    };

    /// <summary>Reconstructs a <see cref="TernaryLineSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static TernaryLineSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.TernaryPlot(dto.TopData ?? [], dto.LeftData ?? [], dto.RightData ?? []);
        s.Color = dto.Color;
        dto.LineStyle.ApplyEnum<LineStyle>(v => s.LineStyle = v);
        if (dto.LineWidth.HasValue)
        {
            s.LineWidth = dto.LineWidth.Value;
        }

        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
