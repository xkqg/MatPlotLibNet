// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D bar chart series — rectangular prisms rising from the XY-plane.</summary>
public sealed class Bar3DSeries : XYZSeries, IHasColor
{
    /// <summary>The side length of each bar's square base, in data units. Each bar starts at its X and Y and
    /// extends this far along both axes. Default 0.5.</summary>
    public double BarWidth { get; set; } = 0.5;

    /// <inheritdoc />
    public Color? Color { get; set; }

    /// <summary>Initializes a new instance of <see cref="Bar3DSeries"/>.</summary>
    public Bar3DSeries(Vec x, Vec y, Vec z) : base(x, y, z) { }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0) return new(null, null, null, null);
        // matplotlib bar3d convention: X[i] / Y[i] are the LEFT-FRONT corner of the bar,
        // not the centre. Bar spans [X, X+BarWidth] × [Y, Y+BarWidth] × [0, Z]. This aligns
        // X/Y tick labels with bar edges instead of centres. Over the drawable coordinates only: a missing one (NaN) and
        // an infinity are left out.
        var x = X.Data.DrawableRange();
        var y = Y.Data.DrawableRange();
        var z = Z.Data.DrawableRange();
        return new(x?.Lo, x?.Hi + BarWidth, y?.Lo, y?.Hi + BarWidth, StickyZMin: 0,
            ZMin: z is { } zFloor ? Math.Min(0, zFloor.Lo) : null, ZMax: z is { } zTop ? Math.Max(0, zTop.Hi) : null);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "bar3d",
        XData = X,
        YData = Y,
        ZData = Z,
        BarWidth = BarWidth,
        Color = Color
    };

    /// <summary>Reconstructs a <see cref="Bar3DSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static Bar3DSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Bar3D(dto.XData ?? [], dto.YData ?? [], dto.ZData ?? []);
        if (dto.BarWidth.HasValue)
        {
            s.BarWidth = dto.BarWidth.Value;
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
