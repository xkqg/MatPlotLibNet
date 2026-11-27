// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Models;

/// <summary>Visual style of the break marker drawn at axis discontinuities: across the middle of the plot under the library's own themes,
/// at each corner of the cut, where the spines of the parts stop, under a theme that draws a broken axis in parts (the matplotlib
/// themes).</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder,
/// remove, or renumber.</remarks>
public enum BreakStyle
{
    /// <summary>A small zigzag across the gap: in the middle of the plot, or at each corner of the cut.</summary>
    Zigzag = 0,
    /// <summary>Two short parallel slanted lines (like //) across the gap: in the middle of the plot, or a diagonal at each corner of
    /// the cut, as matplotlib's broken-axis recipe draws it.</summary>
    Straight = 1,
    /// <summary>No marker; the gap is still cut out of the axis.</summary>
    None = 2,
}

/// <summary>Represents a discontinuous (broken) region on an axis where data is hidden and the scale is compressed. Under a theme whose
/// subplots stand a share of a row apart (the matplotlib themes), the parts of the axis stand that gap apart, each with its own ticks and
/// spines, as matplotlib's broken-axis recipe stacks a subplot per part; under the library's own themes the region is cut to nothing.</summary>
/// <param name="From">Lower bound of the hidden range (inclusive).</param>
/// <param name="To">Upper bound of the hidden range (exclusive).</param>
/// <param name="Style">Visual style of the break marker. Defaults to <see cref="RestingStyle"/>.</param>
public sealed record AxisBreak(double From, double To, BreakStyle Style = AxisBreak.RestingStyle)
{
    /// <summary>The marker a break is drawn with when none is named: <see cref="BreakStyle.Zigzag"/>.</summary>
    internal const BreakStyle RestingStyle = BreakStyle.Zigzag;
}
