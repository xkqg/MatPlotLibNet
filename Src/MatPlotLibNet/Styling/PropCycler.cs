// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>One complete set of cycled visual properties for a plot series.</summary>
/// <param name="Color">The cycled fill and stroke color.</param>
/// <param name="LineStyle">The cycled line dash pattern.</param>
/// <param name="MarkerStyle">The cycled data-point marker shape.</param>
/// <param name="LineWidth">The cycled line width in pixels.</param>
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
    /// <param name="colors">The colours to step through.</param>
    /// <param name="lineStyles">The line styles to step through.</param>
    /// <param name="markerStyles">The marker styles to step through.</param>
    /// <param name="lineWidths">The line widths to step through, in pixels.</param>
    internal PropCycler(Color[] colors, LineStyle[] lineStyles, MarkerStyle[] markerStyles, double[] lineWidths)
    {
        _colors       = colors;
        _lineStyles   = lineStyles.Length   > 0 ? lineStyles   : [GivenStroke.RestingStyle];
        _markerStyles = markerStyles.Length > 0 ? markerStyles : [MarkerStyle.None];
        _lineWidths   = lineWidths;
    }

    /// <summary>Returns the cycled properties for the series at the given <paramref name="index"/>.</summary>
    /// <param name="index">Zero-based series index; wraps automatically via modulo on each property array.</param>
    public CycledProperties this[int index] => new(
        NamesColors ? _colors[index % _colors.Length] : CyclePair.Standard.First,
        _lineStyles[index   % _lineStyles.Length],
        _markerStyles[index % _markerStyles.Length],
        WidthAt(index) ?? LineWidthRule.Line.Resting);

    /// <summary>The line width the cycler names for the series at <paramref name="index"/>, or <see langword="null"/> when it was
    /// built with no widths: a cycler that names 1.5 names a width, one that names none leaves the width to the theme.</summary>
    /// <param name="index">Zero-based series index; wraps around the widths.</param>
    /// <returns>The named width, or <see langword="null"/>.</returns>
    internal double? WidthAt(int index) => _lineWidths.Length > 0 ? _lineWidths[index % _lineWidths.Length] : null;

    /// <summary>Gets the full cycle length (LCM of all property array lengths).</summary>
    public int Length => Lcm(Lcm(Math.Max(1, _colors.Length), _lineStyles.Length), Lcm(_markerStyles.Length, Math.Max(1, _lineWidths.Length)));

    /// <summary>The colours the cycler steps through, as it was built with them: empty when it names none (the figure JSON writes
    /// these, not the whole cycle, which can be as long as the product of the four lengths).</summary>
    internal IReadOnlyList<Color> ColorCycle => _colors;

    /// <summary>The line styles the cycler steps through.</summary>
    internal IReadOnlyList<LineStyle> LineStyleCycle => _lineStyles;

    /// <summary>The marker styles the cycler steps through.</summary>
    internal IReadOnlyList<MarkerStyle> MarkerStyleCycle => _markerStyles;

    /// <summary>The line widths the cycler steps through, as it was built with them: empty when it names none.</summary>
    internal IReadOnlyList<double> LineWidthCycle => _lineWidths;

    /// <summary>Whether the cycler was built with colours: a cycler that names none leaves the colours to the theme's cycle, as one that
    /// names no widths leaves the width to the theme (matplotlib's <c>cycler('linestyle', [...])</c> over the style's colours).</summary>
    internal bool NamesColors => _colors.Length > 0;

    /// <summary>Whether the cycler was built with line widths: a cycler that names none leaves the width to the theme.</summary>
    internal bool NamesLineWidths => _lineWidths.Length > 0;

    private static int Lcm(int a, int b) => a / Gcd(a, b) * b;
    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
}
