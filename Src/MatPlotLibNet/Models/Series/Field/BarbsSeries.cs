// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a wind barb series using meteorological notation to show speed and direction.</summary>
public sealed class BarbsSeries : ChartSeries
{
    /// <summary>Gets the X coordinates of the barb origins.</summary>
    public Vec X { get; }

    /// <summary>Gets the Y coordinates of the barb origins.</summary>
    public Vec Y { get; }

    /// <summary>Gets the wind speeds (magnitude) at each point.</summary>
    public Vec Speed { get; }

    /// <summary>Gets the wind directions in degrees (meteorological: 0 = from North, clockwise).</summary>
    public Vec Direction { get; }

    /// <summary>Gets or sets the length of the barb staff in pixels.</summary>
    public double BarbLength { get; set; } = 15;

    /// <summary>Gets or sets the color of the barbs. If <see langword="null"/>, the current cycle color is used.</summary>
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
        return new(X.Min(), X.Max(), Y.Min(), Y.Max());
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

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
