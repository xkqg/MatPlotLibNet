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
    /// <summary>The X position of the base of each barb.</summary>
    public Vec X { get; }

    /// <summary>The Y position of the base of each barb.</summary>
    public Vec Y { get; }

    /// <summary>The wind speed at each position, in knots: a flag stands for 50, a full barb for 10 and a half barb for
    /// 5.</summary>
    public Vec Speed { get; }

    /// <summary>The direction the staff of each barb points, in degrees clockwise from north: 0 is straight up and 90
    /// is to the right.</summary>
    public Vec Direction { get; }

    /// <summary>Length of the staff of each barb, in pixels. Default 15.</summary>
    public double BarbLength { get; set; } = 15;

    /// <inheritdoc />
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
