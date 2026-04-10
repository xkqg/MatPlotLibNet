// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D bar chart series — rectangular prisms rising from the XY-plane.</summary>
public sealed class Bar3DSeries : ChartSeries, I3DPointSeries
{
    /// <summary>Gets the X coordinates of the bar bases.</summary>
    public Vec X { get; }

    /// <summary>Gets the Y coordinates of the bar bases.</summary>
    public Vec Y { get; }

    /// <summary>Gets the Z values (heights) of each bar.</summary>
    public Vec Z { get; }

    /// <summary>Gets or sets the width and depth of each bar in data units.</summary>
    public double BarWidth { get; set; } = 0.5;

    /// <summary>Gets or sets the bar color. If <see langword="null"/>, the current cycle color is used.</summary>
    public Color? Color { get; set; }

    // I3DPointSeries explicit implementations
    double[] I3DPointSeries.X => X;
    double[] I3DPointSeries.Y => Y;
    double[] I3DPointSeries.Z => Z;

    /// <summary>Initializes a new instance of <see cref="Bar3DSeries"/>.</summary>
    public Bar3DSeries(Vec x, Vec y, Vec z)
    {
        X = x; Y = y; Z = z;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
        => new(null, null, null, null);

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

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
