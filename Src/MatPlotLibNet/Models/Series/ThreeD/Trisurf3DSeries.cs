// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a triangulated surface from unstructured (x, y, z) point data.</summary>
public sealed class Trisurf3DSeries : XYZSeries, IColormappable, INormalizable, IHasAlpha, IHasEdgeColor, IHasColor
{
    /// <summary>Colormap applied to the Z values of each triangle face. Overrides <see cref="Color"/> when set.</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Opacity of the surface faces. Range [0, 1], default 0.8.</summary>
    public double Alpha { get; set; } = 0.8;

    /// <summary>When <c>true</c> (default), draw wireframe edges between triangles.</summary>
    public bool ShowWireframe { get; set; } = true;

    /// <summary>Color of the wireframe edges. When <c>null</c>, a slightly darkened face color is used.</summary>
    public Color? EdgeColor { get; set; }

    /// <summary>Normalizer for mapping Z values to the colormap range.</summary>
    public INormalizer? Normalizer { get; set; }

    /// <summary>Fallback solid color when no colormap is assigned.</summary>
    public Color? Color { get; set; }

    /// <summary>Initializes a new triangulated surface series with the specified data.</summary>
    public Trisurf3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "trisurf",
        XData = X,
        YData = Y,
        ZData = Z,
        Color = Color,
        ShowWireframe = ShowWireframe ? null : (bool?)false,
        Alpha = Alpha != 0.8 ? Alpha : null,
        Label = Label
    };

    /// <summary>Reconstructs a <see cref="Trisurf3DSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static Trisurf3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Trisurf(dto.XData ?? [0.0], dto.YData ?? [0.0], dto.ZData ?? [0.0]);
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        if (dto.ShowWireframe.HasValue)
        {
            s.ShowWireframe = dto.ShowWireframe.Value;
        }
        if (dto.Alpha.HasValue)
        {
            s.Alpha = dto.Alpha.Value;
        }
        if (dto.ColorMapName is not null)
        {
            s.ColorMap = Styling.ColorMaps.ColorMapRegistry.Get(dto.ColorMapName);
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
