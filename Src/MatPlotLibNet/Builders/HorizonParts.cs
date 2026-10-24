// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The bands a horizon chart is made of, handed to the callback of
/// <see cref="XYAxesExtensions.Horizon"/> so each can be styled on its own: the folds of the series' non-negative part,
/// each deeper than the last in the same hue, and, when the series carries a negative value anywhere, the folds of its
/// negative part in a second hue. They are the very series on the axes, so a change made through them is the chart's.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct HorizonParts
{
    /// <summary>Makes the parts of one horizon chart.</summary>
    /// <param name="positive">The folds of the non-negative part, band 0 (the lightest) first.</param>
    /// <param name="negative">The folds of the negative part, band 0 (the lightest) first; empty when the series never
    /// goes negative.</param>
    internal HorizonParts(IReadOnlyList<AreaSeries> positive, IReadOnlyList<AreaSeries> negative)
    {
        Positive = positive;
        Negative = negative;
    }

    /// <summary>The folds of the series' non-negative part, band 0 (the lightest, nearest the baseline) first, all in
    /// one colour group.</summary>
    public IReadOnlyList<AreaSeries> Positive { get; }

    /// <summary>The folds of the series' negative part, band 0 first, all in a SECOND colour group — empty when the
    /// series has no negative value, so the chart needs only the one hue <see cref="Positive"/> wears.</summary>
    public IReadOnlyList<AreaSeries> Negative { get; }
}
