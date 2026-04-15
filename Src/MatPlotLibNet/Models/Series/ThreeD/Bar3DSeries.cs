// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a 3D bar chart series — rectangular prisms rising from the XY-plane.</summary>
public sealed class Bar3DSeries : ChartSeries, I3DPointSeries, IHasColor
{
    public Vec X { get; }

    public Vec Y { get; }

    public Vec Z { get; }

    public double BarWidth { get; set; } = 0.5;

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
    {
        if (X.Length == 0) return new(null, null, null, null);
        // matplotlib bar3d convention: X[i] / Y[i] are the LEFT-FRONT corner of the bar,
        // not the centre. Bar spans [X, X+BarWidth] × [Y, Y+BarWidth] × [0, Z]. This aligns
        // X/Y tick labels with bar edges instead of centres.
        double xLo = X.Min(), xHi = X.Max() + BarWidth;
        double yLo = Y.Min(), yHi = Y.Max() + BarWidth;
        double zLo = Math.Min(0, Z.Min());
        double zHi = Math.Max(0, Z.Max());
        return new(xLo, xHi, yLo, yHi,
            StickyZMin: 0,
            ZMin: zLo, ZMax: zHi);
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

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
