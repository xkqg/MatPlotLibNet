// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet;

/// <summary>The ticks that name categories along an axis: one tick on the centre of each category's slot
/// (<see cref="CategorySlot"/>), reading that category's name.</summary>
/// <param name="Locator">One tick on each slot centre.</param>
/// <param name="Formatter">The name of the category a tick stands on.</param>
internal readonly record struct CategoryTicks(FixedLocator Locator, CategoryFormatter Formatter);

/// <summary>Writes the categories of an axis as its ticks: one tick on the centre of each category's slot
/// (<see cref="CategorySlot"/>), reading that category's name. The <see cref="FixedLocator"/> and the
/// <see cref="CategoryFormatter"/> are made here as one pair (<see cref="CategoryTicks"/>), so the positions and the names
/// cannot disagree, and both are built-ins, so the named ticks survive JSON. Read by the clustermap's heatmap and the
/// composed verbs that name their categories along an axis (every slot, or only some: the months over a calendar's week
/// columns; or names at positions of their own: the centres of a Marimekko chart's columns, whose widths are shares), and
/// by the axes renderer, which names the categories of bars that lie down on the Y axis with the same pair.</summary>
internal static class CategoryTickExtensions
{
    /// <summary>Names <paramref name="names"/> along the X axis, the first category at the left.</summary>
    /// <param name="axes">The axes whose X axis is named.</param>
    /// <param name="names">The categories' names, in the order of their slots.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="names"/> is <see langword="null"/>.</exception>
    internal static AxesBuilder SetXCategoryTicks(this AxesBuilder axes, string[] names)
    {
        var ticks = names.AsCategoryTicks();
        return axes.SetXTickLocator(ticks.Locator).SetXTickFormatter(ticks.Formatter);
    }

    /// <summary>Names <paramref name="names"/> along the X axis at positions of their own instead of on slots of width one
    /// (the centres of a Marimekko chart's columns, whose widths are shares): a tick on each position, reading its name.</summary>
    /// <param name="axes">The axes whose X axis is named.</param>
    /// <param name="names">The names, one for each position, in the same order.</param>
    /// <param name="positions">Where each name stands along the X axis, in its data units.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="names"/> or <paramref name="positions"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="positions"/> does not carry one position per name; the axis is
    /// left as it was.</exception>
    internal static AxesBuilder SetXCategoryTicks(this AxesBuilder axes, string[] names, double[] positions)
    {
        var ticks = names.AsCategoryTicks(positions);
        return axes.SetXTickLocator(ticks.Locator).SetXTickFormatter(ticks.Formatter);
    }

    /// <summary>The ticks that name <paramref name="names"/> at positions of their own: one on each position, reading the
    /// name given with it; a tick anywhere else reads nothing.</summary>
    /// <param name="names">The names, one for each position, in the same order.</param>
    /// <param name="positions">Where each name stands along the axis, in its data units.</param>
    /// <returns>The locator and the formatter, made as one pair from the same positions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="names"/> or <paramref name="positions"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="positions"/> does not carry one position per name.</exception>
    internal static CategoryTicks AsCategoryTicks(this string[] names, double[] positions)
    {
        ArgumentNullException.ThrowIfNull(names);
        var formatter = new CategoryFormatter(names, positions);
        return new CategoryTicks(new FixedLocator([.. positions]), formatter);
    }

    /// <summary>Names only some slots along the X axis (the months over a calendar's week columns): a tick on the centre of
    /// each named slot, reading its name, and no tick on a slot without a name.</summary>
    /// <param name="axes">The axes whose X axis is named.</param>
    /// <param name="namesBySlot">The name of each slot that carries one, by the slot's index from 0.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="namesBySlot"/> is <see langword="null"/>.</exception>
    internal static AxesBuilder SetXCategoryTicks(this AxesBuilder axes, IReadOnlyDictionary<int, string> namesBySlot)
    {
        var ticks = namesBySlot.AsCategoryTicks();
        return axes.SetXTickLocator(ticks.Locator).SetXTickFormatter(ticks.Formatter);
    }

    /// <summary>The ticks that name only some slots: one on the centre of each slot in <paramref name="namesBySlot"/>, from
    /// the lowest slot up whatever order the map holds them in, reading that slot's name; a slot between them reads
    /// nothing.</summary>
    /// <param name="namesBySlot">The name of each slot that carries one, by the slot's index from 0.</param>
    /// <returns>The locator and the formatter, made as one pair.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="namesBySlot"/> is <see langword="null"/>.</exception>
    internal static CategoryTicks AsCategoryTicks(this IReadOnlyDictionary<int, string> namesBySlot)
    {
        ArgumentNullException.ThrowIfNull(namesBySlot);
        int[] slots = [.. namesBySlot.Keys.Order()];
        var labels = new string[slots.DefaultIfEmpty(-1).Max() + 1];
        Array.Fill(labels, string.Empty);
        foreach (int slot in slots) labels[slot] = namesBySlot[slot];

        return new CategoryTicks(
            new FixedLocator([.. slots.Select(slot => CategorySlot.Of(slot).Centre)]),
            new CategoryFormatter(labels));
    }

    /// <summary>Names <paramref name="names"/> along the Y axis: the first category at the bottom, where the axis starts
    /// counting, or at the top when the categories are drawn from the top down (<paramref name="topDown"/>; the rows of a
    /// heatmap, whose row 0 is drawn at the top).</summary>
    /// <param name="axes">The axes whose Y axis is named.</param>
    /// <param name="names">The categories' names, in the order of their slots as drawn.</param>
    /// <param name="topDown">Whether the first category is drawn at the top.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="names"/> is <see langword="null"/>.</exception>
    internal static AxesBuilder SetYCategoryTicks(this AxesBuilder axes, string[] names, bool topDown = false)
    {
        var ticks = names.AsCategoryTicks(topDown);
        return axes.SetYTickLocator(ticks.Locator).SetYTickFormatter(ticks.Formatter);
    }

    /// <summary>The ticks that name <paramref name="names"/> along an axis: one on the centre of each category's slot,
    /// reading category <c>k</c>'s name on slot <c>k</c>, or the names the other way round when the categories are drawn
    /// from the top down (<paramref name="topDown"/>).</summary>
    /// <param name="names">The categories' names, in the order of their slots.</param>
    /// <param name="topDown">Whether the first category is drawn at the top of a Y axis.</param>
    /// <returns>The locator and the formatter, made as one pair.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="names"/> is <see langword="null"/>.</exception>
    internal static CategoryTicks AsCategoryTicks(this string[] names, bool topDown = false)
    {
        ArgumentNullException.ThrowIfNull(names);
        return new CategoryTicks(
            new FixedLocator([.. Enumerable.Range(0, names.Length).Select(k => CategorySlot.Of(k).Centre)]),
            new CategoryFormatter(names, reversed: topDown));
    }
}
