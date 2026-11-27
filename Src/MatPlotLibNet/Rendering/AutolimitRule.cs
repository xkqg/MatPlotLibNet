// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Where the free ends of an axis stop, picked once from the theme's look (<see cref="AxisRangeLook.AutolimitRule"/>): one
/// sealed leaf per matplotlib <c>axes.autolimit_mode</c> (<see cref="Data"/>, <see cref="RoundNumbers"/>) and one for a theme that names
/// none (<see cref="Library"/>, the library's own nice bounds). The range of a 2-D axes, the cube of a 3-D axes, the radius of a polar
/// axes, what a contour asks of its axes and the theme's tick rule ask the leaf; <see cref="AutolimitMode"/> stays the look's word.</summary>
internal abstract class AutolimitRule
{
    /// <summary>matplotlib's <c>'data'</c>: an axis ends where the data and the margin end (the default style).</summary>
    internal static AutolimitRule Data { get; } = new DataEnds();

    /// <summary>matplotlib's <c>'round_numbers'</c>: an axis is rounded out to its outermost ticks (the classic style).</summary>
    internal static AutolimitRule RoundNumbers { get; } = new RoundNumberEnds();

    /// <summary>A theme that names no mode (the library's own): a 2-D axis widens its margin to the nice bounds of its data.</summary>
    internal static AutolimitRule Library { get; } = new LibraryEnds();

    private AutolimitRule() { }

    /// <summary>The rule of <paramref name="mode"/>, the theme's word: the one place a mode is turned into what it does.</summary>
    /// <param name="mode">The theme's mode, <see langword="null"/> when it names none.</param>
    /// <returns>The leaf.</returns>
    internal static AutolimitRule Of(AutolimitMode? mode) => mode switch
    {
        AutolimitMode.Data => Data,
        AutolimitMode.RoundNumbers => RoundNumbers,
        _ => Library,
    };

    /// <summary>Whether the theme rounds an axis out to its ticks, which a ladder of ticks reads too (it asks for a step that reaches
    /// the end, <see cref="Styling.AxisTickRule.RoundNumbers"/>).</summary>
    internal virtual bool RoundsToTicks => false;

    /// <summary>Whether the theme names a mode, so matplotlib's rule ends the axis: what a contour asks of its axes (a tight range) and
    /// how the radius of a polar axes is worked out depend on it.</summary>
    internal virtual bool NamedByTheme => true;

    /// <summary>The rule the cube of a 3-D axes ends by: this one, except that a theme that names no mode ends a cube where the data and
    /// the margin end, as matplotlib's default does (a cube has no nice bounds of the library's).</summary>
    internal virtual AutolimitRule InCube => this;

    /// <summary>Where the free ends of the axis <paramref name="auto"/> scales stop.</summary>
    /// <param name="auto">The axis' autoscale.</param>
    /// <param name="range">The padded, clamped range.</param>
    /// <param name="ladder">The locator the theme places ticks by (<see cref="AxisTickLook.Ladder"/>), whose ends a rounding rule rounds
    /// out to; a theme that rounds without naming one takes the classic locator.</param>
    /// <param name="unpadded">The range before the margin, which the library's nice bounds are taken from.</param>
    /// <param name="hasSticky">Whether a series sets a sticky edge on this axis, which the library's nice bounds stand aside for.</param>
    /// <param name="namesCategories">Whether the axis' ticks name categories: matplotlib places those with a fixed locator, which rounds
    /// nothing.</param>
    /// <returns>The range as the axis ends it.</returns>
    internal abstract Range1D Autolimited(AxisAutoscale auto, Range1D range, TickLadder? ladder, Range1D unpadded, bool hasSticky,
        bool namesCategories);

    /// <summary><see cref="Data"/>.</summary>
    private sealed class DataEnds : AutolimitRule
    {
        /// <inheritdoc />
        internal override Range1D Autolimited(AxisAutoscale auto, Range1D range, TickLadder? ladder, Range1D unpadded, bool hasSticky,
            bool namesCategories) => range;
    }

    /// <summary><see cref="RoundNumbers"/>.</summary>
    private sealed class RoundNumberEnds : AutolimitRule
    {
        /// <inheritdoc />
        internal override bool RoundsToTicks => true;

        /// <inheritdoc />
        internal override Range1D Autolimited(AxisAutoscale auto, Range1D range, TickLadder? ladder, Range1D unpadded, bool hasSticky,
            bool namesCategories) => namesCategories ? range : auto.RoundedOutToTicks(range, ladder ?? TickLadder.Classic);
    }

    /// <summary><see cref="Library"/>.</summary>
    private sealed class LibraryEnds : AutolimitRule
    {
        /// <summary>The locator the library's nice bounds are taken by.</summary>
        private static readonly AutoLocator NiceBoundsLocator = new();

        /// <inheritdoc />
        internal override bool NamedByTheme => false;

        /// <inheritdoc />
        internal override AutolimitRule InCube => Data;

        /// <inheritdoc />
        internal override Range1D Autolimited(AxisAutoscale auto, Range1D range, TickLadder? ladder, Range1D unpadded, bool hasSticky,
            bool namesCategories) => auto.ExpandedToNiceBounds(range, unpadded, hasSticky, NiceBoundsLocator);
    }
}
