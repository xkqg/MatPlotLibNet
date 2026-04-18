// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Visual style of the break marker drawn at axis discontinuities.</summary>
/// <remarks><b>Append-only ordinal contract (Phase O of v1.7.2):</b> never reorder,
/// remove, or renumber. See <c>EnumOrdinalContractTests</c>.</remarks>
public enum BreakStyle
{
    /// <summary>A small zigzag chevron crossing the axis spine.</summary>
    Zigzag = 0,
    /// <summary>Two short diagonal parallel lines (like //).</summary>
    Straight = 1,
    /// <summary>No visual marker; the gap is implicit.</summary>
    None = 2,
}

/// <summary>Represents a discontinuous (broken) region on an axis where data is hidden and the scale is compressed.</summary>
/// <param name="From">Lower bound of the hidden range (inclusive).</param>
/// <param name="To">Upper bound of the hidden range (exclusive).</param>
/// <param name="Style">Visual style of the break marker. Defaults to <see cref="BreakStyle.Zigzag"/>.</param>
public sealed record AxisBreak(double From, double To, BreakStyle Style = BreakStyle.Zigzag);
