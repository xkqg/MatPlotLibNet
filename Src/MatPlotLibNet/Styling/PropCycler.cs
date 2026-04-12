// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>One complete set of cycled visual properties for a plot series.</summary>
/// <param name="Color">The cycled fill and stroke color.</param>
/// <param name="LineStyle">The cycled line dash pattern.</param>
/// <param name="MarkerStyle">The cycled data-point marker shape.</param>
/// <param name="LineWidth">The cycled line width in points.</param>
public readonly record struct CycledProperties(
    Color Color,
    LineStyle LineStyle,
    MarkerStyle MarkerStyle,
    double LineWidth);

/// <summary>
/// Cycles Color, LineStyle, MarkerStyle, and LineWidth simultaneously across successive plot series,
/// equivalent to matplotlib's <c>prop_cycle</c>.
/// </summary>
/// <remarks>
/// Each property array wraps independently via modulo indexing (lockstep).
/// Use <see cref="PropCyclerBuilder"/> to construct instances.
/// When a <see cref="PropCycler"/> is set on a <see cref="Theme"/>, it takes precedence over
/// <see cref="Theme.CycleColors"/>; themes without a cycler fall back to <see cref="Theme.CycleColors"/>.
/// </remarks>
public sealed class PropCycler
{
    private readonly Color[] _colors;
    private readonly LineStyle[] _lineStyles;
    private readonly MarkerStyle[] _markerStyles;
    private readonly double[] _lineWidths;

    /// <summary>Initializes a new <see cref="PropCycler"/> from pre-validated property arrays.</summary>
    internal PropCycler(Color[] colors, LineStyle[] lineStyles, MarkerStyle[] markerStyles, double[] lineWidths)
    {
        _colors       = colors.Length       > 0 ? colors       : [Colors.Tab10Blue];
        _lineStyles   = lineStyles.Length   > 0 ? lineStyles   : [LineStyle.Solid];
        _markerStyles = markerStyles.Length > 0 ? markerStyles : [MarkerStyle.None];
        _lineWidths   = lineWidths.Length   > 0 ? lineWidths   : [1.5];
    }

    /// <summary>Returns the cycled properties for the series at the given <paramref name="index"/>.</summary>
    /// <param name="index">Zero-based series index; wraps automatically via modulo on each property array.</param>
    public CycledProperties this[int index] => new(
        _colors[index       % _colors.Length],
        _lineStyles[index   % _lineStyles.Length],
        _markerStyles[index % _markerStyles.Length],
        _lineWidths[index   % _lineWidths.Length]);

    /// <summary>Gets the full cycle length (LCM of all property array lengths).</summary>
    public int Length => Lcm(Lcm(_colors.Length, _lineStyles.Length), Lcm(_markerStyles.Length, _lineWidths.Length));

    private static int Lcm(int a, int b) => a / Gcd(a, b) * b;
    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
}
