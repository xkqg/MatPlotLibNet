// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D wireframe plot rendered as grid lines on a surface.</summary>
public sealed class WireframeSeries : GridSeries3D, IHasColor
{
    public Color? Color { get; set; }

    public double LineWidth { get; set; } = 0.5;

    /// <summary>Initializes a new wireframe series with the specified grid data.</summary>
    public WireframeSeries(double[] x, double[] y, double[,] z) : base(x, y, z) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "wireframe",
        XData = X,
        YData = Y,
        ZGridData = ZToListList(),
        Color = Color,
        LineWidth = LineWidth != 0.5 ? LineWidth : null,
        Label = Label
    };

    /// <summary>Reconstructs a <see cref="WireframeSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static WireframeSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var z = ChartSerializer.From2DList(dto.ZGridData);
        var s = axes.Wireframe(dto.XData ?? [0.0, 1.0], dto.YData ?? [0.0, 1.0],
            z.GetLength(0) > 0 ? z : new double[,] { { 0, 0 }, { 0, 0 } });
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        if (dto.LineWidth.HasValue)
        {
            s.LineWidth = dto.LineWidth.Value;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
