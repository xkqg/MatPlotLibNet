// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The one answer to "which of these equal bins does a value fall in", as numpy answers it (<c>numpy.histogram</c>, which
/// matplotlib's <c>hist</c> and <c>hist2d</c> call): <see cref="Count"/> bins from <see cref="Low"/> to <see cref="High"/>, their edges
/// <c>numpy.linspace(low, high, count + 1)</c>, each bin holding its low edge and not its high one, the last holding both. A histogram,
/// a histogram in two dimensions, a point's density and an entropy's discretisation each bin through it; what each does with a range of
/// no width is its own, decided before it asks (this range must have a width).</summary>
/// <param name="Low">The low edge of the first bin.</param>
/// <param name="High">The high edge of the last bin; above <paramref name="Low"/> for a value to be binned (evenly spaced values over a
/// range of no width are all its one value).</param>
/// <param name="Count">The number of bins; one or more.</param>
internal readonly record struct EqualWidthBins(double Low, double High, int Count)
{
    /// <summary>The width of a bin: numpy's step between two edges.</summary>
    internal double Width => (High - Low) / Count;

    /// <summary>The edge <paramref name="index"/> of the bins, as numpy lays it: the step times the index plus the low end, and the high
    /// end itself for the last edge.</summary>
    /// <param name="index">The edge, from 0 (the low end) to <see cref="Count"/> (the high end).</param>
    /// <returns>The edge.</returns>
    internal double EdgeAt(int index) => index == Count ? High : index * Width + Low;

    /// <summary>Every edge, low to high: numpy's <c>linspace(low, high, count + 1)</c>, the evenly spaced values a density, a fit or a
    /// profile is evaluated at.</summary>
    /// <returns>The <see cref="Count"/> + 1 edges.</returns>
    internal double[] Edges()
    {
        var edges = new double[Count + 1];
        for (int i = 0; i <= Count; i++) edges[i] = EdgeAt(i);
        return edges;
    }

    /// <summary>The bin <paramref name="value"/> falls in: the share of the range it stands at gives the bin to within one, which the
    /// edges then settle (numpy's own correction, so a value on an edge opens the bin above it). A value past an end falls in the bin at
    /// that end.</summary>
    /// <param name="value">A finite value.</param>
    /// <returns>The bin, from 0 to <see cref="Count"/> - 1.</returns>
    internal int IndexOf(double value)
    {
        int last = Count - 1;
        // Clamped before the cast, so a value far past an end converts the same on every runtime.
        int index = (int)Math.Clamp((value - Low) / (High - Low) * Count, 0, last);
        if (value < EdgeAt(index)) index--;
        else if (index != last && value >= EdgeAt(index + 1)) index++;
        return Math.Clamp(index, 0, last);
    }
}
