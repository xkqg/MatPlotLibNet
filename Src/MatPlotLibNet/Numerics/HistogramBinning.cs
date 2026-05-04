// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Numerics;

/// <summary>Shared equal-width histogram binning. The single source-of-truth used by
/// every series that needs a uniform <c>(min, binWidth, counts)</c> reduction over a
/// 1-D sample, replacing the inline duplicates that had drifted between
/// <c>HistogramSeries</c> and the diagonal cells of <c>PairGridSeries</c>.</summary>
/// <remarks>Non-finite samples (<c>NaN</c>, <c>±∞</c>) are excluded from both the
/// min/max scan and the count tally — they would otherwise poison the bin width
/// and produce out-of-range bin indices.</remarks>
internal static class HistogramBinning
{
    /// <summary>Computes equal-width bins for the given samples.</summary>
    /// <param name="data">The sample values. May contain non-finite values; they are skipped.</param>
    /// <param name="bins">The number of equal-width bins. Clamped to ≥ 1 internally.</param>
    /// <returns>A <see cref="HistogramBins"/> with the inclusive minimum, the bin width
    /// (1.0 when all finite samples are equal — degenerate-range guard), and a count
    /// array of length <paramref name="bins"/>. Returns <c>(0, 1, [])</c> for empty input.</returns>
    internal static HistogramBins Compute(double[] data, int bins)
    {
        if (data.Length == 0) return new HistogramBins(0, 1, []);
        bins = Math.Max(1, bins);

        double min = double.MaxValue, max = double.MinValue;
        bool anyFinite = false;
        foreach (var v in data)
        {
            if (!double.IsFinite(v)) continue;
            if (v < min) min = v;
            if (v > max) max = v;
            anyFinite = true;
        }
        if (!anyFinite) return new HistogramBins(0, 1, new int[bins]);

        double binWidth = (max - min) / bins;
        if (binWidth == 0) binWidth = 1;

        var counts = new int[bins];
        foreach (var v in data)
        {
            if (!double.IsFinite(v)) continue;
            int b = (int)((v - min) / binWidth);
            if (b >= bins) b = bins - 1;
            if (b < 0)     b = 0;
            counts[b]++;
        }
        return new HistogramBins(min, binWidth, counts);
    }
}
