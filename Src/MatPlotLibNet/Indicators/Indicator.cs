// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Indicators;

/// <summary>Base class for technical indicators with shared styling properties.</summary>
/// <remarks>Concrete indicators inherit <see cref="Color"/>, <see cref="Label"/>, <see cref="LineWidth"/>,
/// <see cref="LineStyle"/>, and <see cref="Offset"/> and implement <see cref="Apply"/> to compute derived data
/// and inject series into the axes.</remarks>
public abstract class Indicator : IIndicator
{
    /// <summary>Gets or sets the color for the indicator series. When null, uses the theme cycle color.</summary>
    public Color? Color { get; set; }

    /// <summary>Gets or sets the legend label. Defaults to a descriptive name with parameters (e.g., "SMA(20)").</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the line width for the indicator series.</summary>
    public double LineWidth { get; set; } = 1.5;

    /// <summary>Gets or sets the line style for the indicator series.</summary>
    public LineStyle LineStyle { get; set; } = LineStyle.Solid;

    /// <summary>Gets or sets the horizontal offset (shift) applied to X coordinates. Positive = shift right, negative = shift left.
    /// Use 0.5 when the axes uses bar-slot positioning (candlestick/bar charts) to center overlays on bar bodies.</summary>
    public double Offset { get; set; }

    /// <inheritdoc />
    public abstract void Apply(Axes axes);

    /// <summary>Applies the <see cref="Offset"/> to an array of X coordinates.</summary>
    protected double[] ApplyOffset(double[] x)
    {
        if (Offset == 0) return x;
        var result = new double[x.Length];
        VectorMath.Add(x, Offset, result);
        return result;
    }
}

/// <summary>Generic typed indicator that returns a typed result from <see cref="Compute"/>.</summary>
/// <typeparam name="TResult">The type of the computed result. Must implement <see cref="IIndicatorResult"/>.</typeparam>
/// <remarks>Enables composition: use one indicator's <see cref="Compute"/> output as input to another.</remarks>
public abstract class Indicator<TResult> : Indicator where TResult : IIndicatorResult
{
    /// <summary>Pure computation — returns the typed result without side effects on any axes.</summary>
    public abstract TResult Compute();
}
