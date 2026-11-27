// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Data;

/// <summary>Splits a typed sequence into <see cref="HueGroup"/> instances by a key selector, assigning
/// palette colors in first-seen order. Reused by faceted figures, DataFrame extensions, and QuickPlot.</summary>
public static class HueGrouper
{
    /// <summary>
    /// Groups <paramref name="data"/> by <paramref name="keySelector"/>, extracting X and Y values per group.
    /// Groups are returned in first-seen order. Colors cycle through <paramref name="palette"/>
    /// (defaults to the matplotlib Tab10 cycle when <see langword="null"/>).
    /// </summary>
    /// <typeparam name="T">The element type of the input sequence.</typeparam>
    /// <typeparam name="TKey">The grouping key type; <c>ToString()</c> is used as the legend label.</typeparam>
    /// <param name="data">The input sequence to group.</param>
    /// <param name="keySelector">Extracts the group key from each element.</param>
    /// <param name="xSelector">Extracts the X value from each element.</param>
    /// <param name="ySelector">Extracts the Y value from each element.</param>
    /// <param name="palette">Color palette to cycle over. Defaults to Tab10 when <see langword="null"/>.</param>
    /// <returns>One <see cref="HueGroup"/> per unique key, in first-seen order.</returns>
    /// <remarks>Color cycling uses the same first-seen order as the group sequence, ensuring
    /// deterministic palette assignment regardless of the underlying dictionary implementation.</remarks>
    public static HueGroup[] GroupBy<T, TKey>(
        IEnumerable<T> data,
        Func<T, TKey> keySelector,
        Func<T, double> xSelector,
        Func<T, double> ySelector,
        Color[]? palette = null) where TKey : notnull
    {
        var colors    = palette is { Length: > 0 } ? palette : DefaultPalette;
        var xsOf      = new Dictionary<TKey, List<double>>();
        var ysOf      = new Dictionary<TKey, List<double>>();
        var keyOrder  = new List<TKey>();

        foreach (var item in data)
        {
            var key = keySelector(item);
            if (!xsOf.TryGetValue(key, out var xs))
            {
                xsOf[key] = xs = [];
                ysOf[key] = [];
                keyOrder.Add(key);
            }
            xs.Add(xSelector(item));
            ysOf[key].Add(ySelector(item));
        }

        var result = new HueGroup[keyOrder.Count];
        for (int i = 0; i < keyOrder.Count; i++)
        {
            var key         = keyOrder[i];
            result[i]       = new HueGroup(
                Label: key.ToString() ?? string.Empty,
                Color: colors[i % colors.Length],
                X:     [.. xsOf[key]],
                Y:     [.. ysOf[key]]);
        }
        return result;
    }

    /// <summary>The hue label of each observation as the number of its group: the groups numbered in the order their labels are
    /// first seen, as <see cref="GroupBy"/> orders them, and the label of each group by its number.</summary>
    /// <param name="hue">The hue label of each observation.</param>
    /// <returns>The group of each observation and the label of each group.</returns>
    internal static HueCodes Codes(IReadOnlyList<string> hue)
    {
        var numbers = new Dictionary<string, int>(StringComparer.Ordinal);
        var labels = new List<string>();
        var groups = new int[hue.Count];
        for (int i = 0; i < groups.Length; i++)
        {
            if (!numbers.TryGetValue(hue[i], out int group))
            {
                group = labels.Count;
                numbers[hue[i]] = group;
                labels.Add(hue[i]);
            }

            groups[i] = group;
        }

        return new HueCodes(groups, [.. labels]);
    }

    /// <summary>Matplotlib Tab10 cycle — the default palette when none is supplied.</summary>
    internal static readonly Color[] DefaultPalette = QualitativeColorMaps.Tab10Cycle();
}

/// <summary>Hue labels as group numbers (<see cref="HueGrouper.Codes"/>): what a series split by hue by number takes.</summary>
/// <param name="Groups">The group of each observation, numbered in the order its label was first seen.</param>
/// <param name="Labels">The label of each group, by its number.</param>
internal readonly record struct HueCodes(int[] Groups, string[] Labels);
