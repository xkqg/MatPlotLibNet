// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D scatter plot rendered as projected circles.</summary>
public sealed class Scatter3DSeries : XYZSeries, IHasColor, IColormappable, INormalizable, IHasMarkerStyle
{
    public Color? Color { get; set; }

    public IColorMap? ColorMap { get; set; }

    public INormalizer? Normalizer { get; set; }

    public MarkerStyle MarkerStyle { get; set; } = MarkerStyle.Circle;

    public double MarkerSize { get; set; } = 6;

    /// <summary>Initializes a new 3D scatter series with the specified data.</summary>
    public Scatter3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "scatter3d",
        XData = X,
        YData = Y,
        ZData = Z,
        Color = Color,
        ColorMapName = ColorMap?.Name,
        MarkerSize = MarkerSize != 6 ? MarkerSize : null,
        Label = Label
    };

    /// <summary>Reconstructs a <see cref="Scatter3DSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static Scatter3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Scatter3D(dto.XData ?? [0.0], dto.YData ?? [0.0], dto.ZData ?? [0.0]);
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        if (dto.MarkerSize.HasValue)
        {
            s.MarkerSize = dto.MarkerSize.Value;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
