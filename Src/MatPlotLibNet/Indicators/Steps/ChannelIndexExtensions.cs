// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators.Steps;

/// <summary>The commodity channel index of one window of typical prices: the one place its arithmetic is written, read by the batch
/// <see cref="Cci"/> for each window it slides over and by <see cref="Streaming.StreamingCci"/> for the window it keeps, so the two
/// forms say the same number by the same steps.</summary>
internal static class ChannelIndexExtensions
{
    /// <summary>Lambert's constant: it scales the mean deviation so that about three values in four fall between -100 and 100.</summary>
    private const double Lambert = 0.015;

    /// <param name="typicalPrices">The typical prices of the window, in any order.</param>
    extension(ReadOnlySpan<double> typicalPrices)
    {
        /// <summary>How far the newest typical price stands from the mean of the window, in mean deviations scaled by Lambert's
        /// constant; 0 when every price of the window is the mean.</summary>
        /// <param name="mean">The mean of the window (the moving average both forms take it from).</param>
        /// <param name="latest">The newest typical price of the window.</param>
        /// <returns>The index.</returns>
        internal double ChannelIndex(double mean, double latest)
        {
            double meanDeviation = 0;
            foreach (double price in typicalPrices) meanDeviation += Math.Abs(price - mean);
            meanDeviation /= typicalPrices.Length;

            return meanDeviation == 0 ? 0 : (latest - mean) / (Lambert * meanDeviation);
        }
    }
}
