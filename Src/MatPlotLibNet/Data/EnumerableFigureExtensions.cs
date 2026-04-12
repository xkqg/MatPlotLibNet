// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet;

/// <summary>
/// Extension methods that turn any <see cref="IEnumerable{T}"/> directly into a <see cref="FigureBuilder"/>,
/// using selector lambdas for X, Y, and an optional hue (group-by) key.
/// Each method returns a <see cref="FigureBuilder"/> that can be further customised before calling
/// <c>Build()</c>, <c>ToSvg()</c>, or <c>Save()</c>.
/// </summary>
public static class EnumerableFigureExtensions
{
    /// <summary>Plots <paramref name="data"/> as one or more line series.</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="data">The source sequence.</param>
    /// <param name="x">X value selector.</param>
    /// <param name="y">Y value selector.</param>
    /// <param name="hue">Optional group-by selector. When supplied, one <see cref="LineSeries"/> per
    /// unique key is added; each series is labeled and color-coded from <paramref name="palette"/>.</param>
    /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
    public static FigureBuilder Line<T>(
        this IEnumerable<T> data,
        Func<T, double> x,
        Func<T, double> y,
        Func<T, string>? hue  = null,
        Color[]?         palette = null)
    {
        var fb = Plt.Create();
        if (hue is null)
        {
            var arr = data.ToArray();
            fb.Plot(arr.Select(x).ToArray(), arr.Select(y).ToArray());
        }
        else
        {
            foreach (var g in HueGrouper.GroupBy(data, hue, x, y, palette))
                fb.Plot(g.X, g.Y, s => { s.Color = g.Color; s.Label = g.Label; });
        }
        return fb;
    }

    /// <summary>Plots <paramref name="data"/> as one or more scatter series.</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="data">The source sequence.</param>
    /// <param name="x">X value selector.</param>
    /// <param name="y">Y value selector.</param>
    /// <param name="hue">Optional group-by selector.</param>
    /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
    public static FigureBuilder Scatter<T>(
        this IEnumerable<T> data,
        Func<T, double> x,
        Func<T, double> y,
        Func<T, string>? hue  = null,
        Color[]?         palette = null)
    {
        var fb = Plt.Create();
        if (hue is null)
        {
            var arr = data.ToArray();
            fb.Scatter(arr.Select(x).ToArray(), arr.Select(y).ToArray());
        }
        else
        {
            foreach (var g in HueGrouper.GroupBy(data, hue, x, y, palette))
                fb.Scatter(g.X, g.Y, s => { s.Color = g.Color; s.Label = g.Label; });
        }
        return fb;
    }

    /// <summary>Plots the distribution of <paramref name="data"/> as one or more histogram series.</summary>
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="data">The source sequence.</param>
    /// <param name="value">Numeric value selector.</param>
    /// <param name="bins">Number of bins (default 30).</param>
    /// <param name="hue">Optional group-by selector. When supplied, one overlapping
    /// <see cref="HistogramSeries"/> per unique key is added with alpha 0.7 for legibility.</param>
    /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
    public static FigureBuilder Hist<T>(
        this IEnumerable<T> data,
        Func<T, double>  value,
        int              bins    = 30,
        Func<T, string>? hue     = null,
        Color[]?         palette = null)
    {
        var fb = Plt.Create();
        if (hue is null)
        {
            var arr = data.Select(value).ToArray();
            fb.Hist(arr, bins);
        }
        else
        {
            // Convert to HueGroup via a dummy XY grouping; only Y (value) matters for histograms
            var colors   = palette is { Length: > 0 } ? palette : HueGrouper.DefaultPalette;
            var buckets  = new Dictionary<string, List<double>>();
            var keyOrder = new List<string>();

            foreach (var item in data)
            {
                var key = hue(item);
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = [];
                    buckets[key] = list;
                    keyOrder.Add(key);
                }
                list.Add(value(item));
            }

            for (int i = 0; i < keyOrder.Count; i++)
            {
                var key   = keyOrder[i];
                var vals  = buckets[key].ToArray();
                var color = colors[i % colors.Length];
                fb.Hist(vals, bins, s => { s.Color = color; s.Label = key; s.Alpha = 0.7; });
            }
        }
        return fb;
    }
}
