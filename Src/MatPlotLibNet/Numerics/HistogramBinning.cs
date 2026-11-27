// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Numerics;

/// <summary>Shared equal-width histogram binning. The single source-of-truth used by
/// every series that needs a uniform <c>(min, binWidth, counts)</c> reduction over a
/// 1-D sample, replacing the inline duplicates that had drifted between
/// <c>HistogramSeries</c> and the diagonal cells of <c>PairGridSeries</c>.</summary>
/// <remarks>Non-finite samples (<c>NaN</c>, <c>±∞</c>) are excluded from both the
/// min/max scan and the tally, counted or weighed alike (<see cref="HistogramGrid.BinOf"/>) — they would otherwise poison the bin
/// width and produce out-of-range bin indices.</remarks>
internal static class HistogramBinning
{
    /// <summary>Computes equal-width bins for the given samples.</summary>
    /// <param name="data">The sample values. May contain non-finite values; they are skipped.</param>
    /// <param name="bins">The number of equal-width bins. Clamped to ≥ 1 internally.</param>
    /// <returns>A <see cref="HistogramBins"/> with the low edge of the first bin, the bin width (<see cref="GridOf"/>) and a count
    /// array of length <paramref name="bins"/>. Returns <c>(0, 1, [])</c> for empty input.</returns>
    internal static HistogramBins Compute(double[] data, int bins)
    {
        if (data.Length == 0) return new HistogramBins(0, 1, []);

        var grid = GridOf(data, bins);
        var counts = new int[grid.Count];
        foreach (var v in data)
        {
            if (grid.BinOf(v) is var bin and >= 0) counts[bin]++;
        }

        return new HistogramBins(grid.Min, grid.BinWidth, counts);
    }

    /// <summary>The bins the finite samples of <paramref name="data"/> span: from the lowest to the highest, as numpy lays them, so samples
    /// that are all equal span their value and half a unit either side (<see cref="Rendering.Range1D.OpenedWhenFlat"/>); (0, 1) when there
    /// is none.</summary>
    /// <param name="data">The sample values. May contain non-finite values; they are skipped.</param>
    /// <param name="bins">The number of equal-width bins. Clamped to ≥ 1.</param>
    /// <returns>The grid.</returns>
    internal static HistogramGrid GridOf(double[] data, int bins)
    {
        bins = Math.Max(1, bins);

        if (data.DrawableRange() is not { } range) return new HistogramGrid(0, 1, bins, Spread: null);

        var (low, high) = range.OpenedWhenFlat();
        var spread = new EqualWidthBins(low, high, bins);
        return new HistogramGrid(low, spread.Width, bins, spread);
    }
}

/// <summary>The equal-width bins of a histogram: where the first starts, how wide each is and how many there are; the one place that
/// says which samples a histogram counts and in which bin.</summary>
/// <param name="Min">The low edge of the first bin.</param>
/// <param name="BinWidth">The width of a bin.</param>
/// <param name="Count">The number of bins.</param>
/// <param name="Spread">The bins over the samples' range, numpy's (<see cref="EqualWidthBins"/>); <see langword="null"/> when no sample
/// can be counted.</param>
internal readonly record struct HistogramGrid(double Min, double BinWidth, int Count, EqualWidthBins? Spread)
{
    /// <summary>The bin <paramref name="value"/> is counted in, or -1 for a value a histogram does not count (a missing value, an
    /// infinity), whose weight it leaves out too.</summary>
    /// <param name="value">A sample.</param>
    /// <returns>The bin, or -1.</returns>
    internal int BinOf(double value) => Spread is { } spread && double.IsFinite(value) ? spread.IndexOf(value) : -1;
}
