// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D stem series — vertical lines from the XY-plane to each (X, Y, Z) data point.</summary>
public sealed class Stem3DSeries : XYZSeries, IHasColor
{
    public Color? Color { get; set; }

    public double MarkerSize { get; set; } = 6;

    /// <summary>Colour of the baseline polyline drawn at <c>z = 0</c> through every stem
    /// base point in sequence order. Matches matplotlib's <c>ax.stem(basefmt='C2-')</c>
    /// behaviour, where the stem container includes a <c>baseline</c> Line3D alongside the
    /// vertical stem lines and markers. Set to <see langword="null"/> to suppress the
    /// baseline entirely. Defaults to the same colour as the stem line (<see cref="Color"/>)
    /// so a single-colour theme override works as expected.</summary>
    public Color? BaseLineColor { get; set; }

    /// <summary>Initializes a new instance of <see cref="Stem3DSeries"/>.</summary>
    public Stem3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "stem3d",
        XData = X,
        YData = Y,
        ZData = Z,
        MarkerSize = MarkerSize,
        Color = Color
    };

    /// <summary>Reconstructs a <see cref="Stem3DSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static Stem3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Stem3D(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? []);
        if (dto.MarkerSize.HasValue)
        {
            s.MarkerSize = dto.MarkerSize.Value;
        }
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        return s;
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
