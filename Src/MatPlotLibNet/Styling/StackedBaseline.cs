// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling;

/// <summary>Specifies the baseline strategy for stacked area series.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum StackedBaseline
{
    /// <summary>All layers stack upward from y = 0 (matplotlib default).</summary>
    Zero = 0,

    /// <summary>Shifts all layers so the midpoint of the total stack sits at y = 0 (ThemeRiver style).</summary>
    Symmetric = 1,

    /// <summary>Byron-Wattenberg wiggle — minimises the perceived slope of the baseline by centering the stack weighted by layer position.</summary>
    Wiggle = 2,

    /// <summary>Wiggle weighted by layer magnitude — further reduces visual instability for layers with unequal sums.</summary>
    WeightedWiggle = 3,
}
