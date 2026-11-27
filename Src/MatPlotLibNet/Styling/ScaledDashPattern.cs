// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>The dash pattern of a line style as a backend strokes it: each dash and gap of a look's pattern times its scale
/// (<see cref="DashPatterns.Lengths"/>, the one owner, which every backend asks under the look of its theme). Read without a copy: it
/// holds the pattern itself, and only <see cref="ToSingles"/> makes an array.</summary>
internal readonly ref struct ScaledDashPattern
{
    private readonly ReadOnlySpan<double> _ratios;
    private readonly double _scale;

    /// <summary>The pattern <paramref name="ratios"/> scaled by <paramref name="scale"/>.</summary>
    /// <param name="ratios">The dash and gap ratios, in order.</param>
    /// <param name="scale">What every ratio is multiplied by.</param>
    internal ScaledDashPattern(ReadOnlySpan<double> ratios, double scale)
    {
        _ratios = ratios;
        _scale = scale;
    }

    /// <summary>How many dashes and gaps the pattern has; 0 for a solid line and for no line.</summary>
    internal int Count => _ratios.Length;

    /// <summary>The length of the dash or gap at <paramref name="index"/>: its ratio times the scale.</summary>
    /// <param name="index">The place in the pattern.</param>
    internal double this[int index] => _ratios[index] * _scale;

    /// <summary>The lengths in single precision, each the double product rounded once; an empty array for a solid line.</summary>
    /// <returns>A new array, or the shared empty one.</returns>
    internal float[] ToSingles()
    {
        if (_ratios.Length == 0) return [];
        var singles = new float[_ratios.Length];
        for (int i = 0; i < singles.Length; i++) singles[i] = (float)(_ratios[i] * _scale);
        return singles;
    }
}
