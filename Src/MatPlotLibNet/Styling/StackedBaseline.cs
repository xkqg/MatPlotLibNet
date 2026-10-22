// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the baseline strategy for stacked area series.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum StackedBaseline
{
    /// <summary>All layers stack upward from y = 0 (matplotlib default).</summary>
    Zero = 0,

    /// <summary>Shifts all layers so the midpoint of the total stack sits at y = 0 (ThemeRiver style).</summary>
    Symmetric = 1,

    /// <summary>The wiggle baseline of matplotlib's <c>stackplot</c>: keeps the middles of the layers as close to y = 0 as the
    /// stack allows, so the lower layers weigh more than the upper ones and the edges wiggle less than with
    /// <see cref="Symmetric"/>. Layers of equal height give the same picture as <see cref="Symmetric"/>.</summary>
    Wiggle = 2,

    /// <summary>The weighted wiggle baseline of matplotlib's <c>stackplot</c>, the layout of a streamgraph: like
    /// <see cref="Wiggle"/>, but the slope of each layer counts in proportion to its size, so thin layers do not push the
    /// thick ones around.</summary>
    WeightedWiggle = 3,
}
