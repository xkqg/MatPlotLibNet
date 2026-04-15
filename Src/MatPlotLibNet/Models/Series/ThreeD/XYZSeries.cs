// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series;

/// <summary>
/// Abstract base for every 3-D point series — mirrors <see cref="XYSeries"/> on the 2-D side.
/// Holds <see cref="X"/>/<see cref="Y"/>/<see cref="Z"/> as <see cref="Vec"/>, supplies the
/// explicit <see cref="I3DPointSeries"/> dispatch once, and provides a safe default
/// <see cref="ComputeDataRange"/> that reports tight X/Y min-max plus Z min-max.
/// </summary>
/// <remarks>
/// Subclasses that widen the visible extent (bar widths, stem markers) or register
/// sticky-zero baselines should override <see cref="ComputeDataRange"/>. Subclasses whose
/// data extent is exactly the scatter cloud (<see cref="Scatter3DSeries"/>,
/// <see cref="Stem3DSeries"/>) can rely on the default.
/// </remarks>
public abstract class XYZSeries : ChartSeries, I3DPointSeries
{
    /// <summary>X coordinates of each data point.</summary>
    public Vec X { get; }

    /// <summary>Y coordinates of each data point.</summary>
    public Vec Y { get; }

    /// <summary>Z coordinates of each data point.</summary>
    public Vec Z { get; }

    // Explicit I3DPointSeries dispatch — done ONCE in the base so the four subclasses don't repeat it.
    double[] I3DPointSeries.X => X;
    double[] I3DPointSeries.Y => Y;
    double[] I3DPointSeries.Z => Z;

    /// <summary>Initialises a 3-D point series with the given coordinate vectors.</summary>
    protected XYZSeries(Vec x, Vec y, Vec z)
    {
        X = x; Y = y; Z = z;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (X.Length == 0) return new(null, null, null, null);
        return new(
            X.Min(), X.Max(),
            Y.Min(), Y.Max(),
            ZMin: Z.Min(), ZMax: Z.Max());
    }
}
