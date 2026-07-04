// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D polyline connecting data points in three-dimensional space.</summary>
public sealed class Line3DSeries : XYZSeries, IHasColor
{
    /// <summary>Line color. When <c>null</c> the theme's prop-cycler assigns one automatically.</summary>
    public Color? Color { get; set; }

    /// <summary>Line width in pixels. Default 1.5.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Dash pattern for the line. Default <see cref="Styling.LineStyle.Solid"/>.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Initializes a new 3D line series with the specified data.</summary>
    public Line3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "line3d",
        XData = X,
        YData = Y,
        ZData = Z,
        Color = Color,
        LineWidth = LineWidth != 1.5 ? LineWidth : null,
        LineStyle = LineStyle == LineStyle.Solid ? null : LineStyle.ToString().ToLowerInvariant(),
        Label = Label
    };

    /// <summary>Reconstructs a <see cref="Line3DSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static Line3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Plot3D(dto.XData ?? [0.0], dto.YData ?? [0.0], dto.ZData ?? [0.0]);
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        if (dto.LineWidth.HasValue)
        {
            s.LineWidth = dto.LineWidth.Value;
        }
        if (dto.LineStyle is not null && Enum.TryParse<Styling.LineStyle>(dto.LineStyle, true, out var ls))
        {
            s.LineStyle = ls;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
