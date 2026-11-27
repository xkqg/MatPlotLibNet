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
/// <c>Build()</c>, <c>ToSvg()</c>, or <c>Save(path)</c>.
/// </summary>
public static class EnumerableFigureExtensions
{
    /// <typeparam name="T">Element type.</typeparam>
    /// <param name="data">The source sequence.</param>
    extension<T>(IEnumerable<T> data)
    {
        /// <summary>Plots <paramref name="data"/> as one or more line series.</summary>
        /// <param name="x">X value selector.</param>
        /// <param name="y">Y value selector.</param>
        /// <param name="hue">Optional group-by selector. When supplied, one <see cref="LineSeries"/> per
        /// unique key is added; each series is labeled and color-coded from <paramref name="palette"/>.</param>
        /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
        public FigureBuilder Line(
            Func<T, double> x,
            Func<T, double> y,
            Func<T, string>? hue  = null,
            Color[]?         palette = null) =>
            data.Split(x, y, hue, palette).Lines();

        /// <summary>Plots <paramref name="data"/> as one or more scatter series.</summary>
        /// <param name="x">X value selector.</param>
        /// <param name="y">Y value selector.</param>
        /// <param name="hue">Optional group-by selector.</param>
        /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
        public FigureBuilder Scatter(
            Func<T, double> x,
            Func<T, double> y,
            Func<T, string>? hue  = null,
            Color[]?         palette = null) =>
            data.Split(x, y, hue, palette).Scatters();

        /// <summary>Plots the distribution of <paramref name="data"/> as one or more histogram series.</summary>
        /// <param name="value">Numeric value selector.</param>
        /// <param name="bins">Number of bins (default <see cref="HistogramSeries.RestingBins"/>).</param>
        /// <param name="hue">Optional group-by selector. When supplied, one <see cref="HistogramSeries"/> per unique key is added,
        /// drawn see-through so the groups show through each other.</param>
        /// <param name="palette">Optional color palette. Defaults to Tab10 when <see langword="null"/>.</param>
        public FigureBuilder Hist(
            Func<T, double>  value,
            int              bins    = HistogramSeries.RestingBins,
            Func<T, string>? hue     = null,
            Color[]?         palette = null) =>
            data.Split(value, null, hue, palette).Histograms(bins);

        /// <summary>The observations of <paramref name="data"/> by the selectors, each selector run once per observation, with their
        /// hue labels when a hue selector is given; with no second selector the second value is the first (a histogram's).</summary>
        private HueSplit Split(Func<T, double> x, Func<T, double>? y, Func<T, string>? hue, Color[]? palette)
        {
            var rows = data.ToArray();
            double[] xs = [.. rows.Select(x)];
            return new HueSplit(xs, y is null ? xs : [.. rows.Select(y)], hue is null ? null : [.. rows.Select(hue)], palette);
        }
    }
}
