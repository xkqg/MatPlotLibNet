// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

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
        var buckets   = new Dictionary<TKey, (List<double> X, List<double> Y)>();
        var keyOrder  = new List<TKey>();

        foreach (var item in data)
        {
            var key = keySelector(item);
            if (!buckets.TryGetValue(key, out var lists))
            {
                lists = (new List<double>(), new List<double>());
                buckets[key] = lists;
                keyOrder.Add(key);
            }
            lists.X.Add(xSelector(item));
            lists.Y.Add(ySelector(item));
        }

        var result = new HueGroup[keyOrder.Count];
        for (int i = 0; i < keyOrder.Count; i++)
        {
            var key         = keyOrder[i];
            var (xs, ys)    = buckets[key];
            result[i]       = new HueGroup(
                Label: key.ToString() ?? string.Empty,
                Color: colors[i % colors.Length],
                X:     [.. xs],
                Y:     [.. ys]);
        }
        return result;
    }

    /// <summary>Matplotlib Tab10 cycle — the default palette when none is supplied.</summary>
    internal static readonly Color[] DefaultPalette =
    [
        Color.FromHex("#1f77b4"), // blue
        Color.FromHex("#ff7f0e"), // orange
        Color.FromHex("#2ca02c"), // green
        Color.FromHex("#d62728"), // red
        Color.FromHex("#9467bd"), // purple
        Color.FromHex("#8c564b"), // brown
        Color.FromHex("#e377c2"), // pink
        Color.FromHex("#7f7f7f"), // gray
        Color.FromHex("#bcbd22"), // olive
        Color.FromHex("#17becf"), // cyan
    ];
}
