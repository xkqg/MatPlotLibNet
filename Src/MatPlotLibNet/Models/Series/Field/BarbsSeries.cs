// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a wind barb series using meteorological notation to show speed and direction.</summary>
public sealed class BarbsSeries : ChartSeries, IHasColor
{
    public Vec X { get; }

    public Vec Y { get; }

    public Vec Speed { get; }

    public Vec Direction { get; }

    public double BarbLength { get; set; } = 15;

    public Color? Color { get; set; }

    /// <summary>Initializes a new instance of <see cref="BarbsSeries"/>.</summary>
    public BarbsSeries(Vec x, Vec y, Vec speed, Vec direction)
    {
        X = x; Y = y; Speed = speed; Direction = direction;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0) return new(0, 1, 0, 1);
        // Over the drawable positions only: a missing one (NaN) and an infinity are left out.
        var x = X.Data.DrawableRange();
        var y = Y.Data.DrawableRange();
        return new(x?.Lo, x?.Hi, y?.Lo, y?.Hi);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "barbs",
        XData = X,
        YData = Y,
        Speed = Speed,
        Direction = Direction,
        BarbLength = BarbLength,
        Color = Color
    };

    /// <summary>Reconstructs a <see cref="BarbsSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static BarbsSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Barbs(dto.XData ?? [], dto.YData ?? [], dto.Speed ?? [], dto.Direction ?? []);
        if (dto.BarbLength.HasValue)
        {
            s.BarbLength = dto.BarbLength.Value;
        }
        if (dto.Color.HasValue)
        {
            s.Color = dto.Color.Value;
        }
        return s;
    }


    /// <inheritdoc />
    /// <remarks>Where each barb sits, and the wind it reports.</remarks>
    public override ChartDataTable? ToDataTable() =>
        ChartDataTable.FromNumberColumns(
            [new("x", Axis: DataAxis.X), new("y", Axis: DataAxis.Y), new("speed"), new("direction")], [X.Data, Y.Data, Speed.Data, Direction.Data]);

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
