// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D stem series — vertical lines from the XY-plane to each (X, Y, Z) data point.</summary>
public sealed class Stem3DSeries : ChartSeries, I3DPointSeries
{
    /// <summary>Gets the X coordinates.</summary>
    public Vec X { get; }

    /// <summary>Gets the Y coordinates.</summary>
    public Vec Y { get; }

    /// <summary>Gets the Z values (heights above the XY-plane).</summary>
    public Vec Z { get; }

    /// <summary>Gets or sets the marker color. If <see langword="null"/>, the current cycle color is used.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the marker size in pixels.</summary>
    public double MarkerSize { get; set; } = 6;

    // I3DPointSeries explicit implementations (Vec casts to double[] via implicit operator)
    double[] I3DPointSeries.X => X;
    double[] I3DPointSeries.Y => Y;
    double[] I3DPointSeries.Z => Z;

    /// <summary>Initializes a new instance of <see cref="Stem3DSeries"/>.</summary>
    public Stem3DSeries(Vec x, Vec y, Vec z)
    {
        X = x; Y = y; Z = z;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
        => new(null, null, null, null);

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

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
