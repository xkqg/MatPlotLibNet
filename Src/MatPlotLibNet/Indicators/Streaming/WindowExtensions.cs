// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Indicators.Streaming;

/// <summary>What a windowed streaming form needs to hand a window to a batch indicator and take its answer: the columns of a window
/// of bars as arrays, and the last value of a result as the output.</summary>
internal static class WindowExtensions
{
    extension(ReadOnlySpan<OhlcvBar> window)
    {
        /// <summary>The opens of the bars, oldest first.</summary>
        internal double[] Opens() => Column(window, bar => bar.Open);

        /// <summary>The highs of the bars, oldest first.</summary>
        internal double[] Highs() => Column(window, bar => bar.High);

        /// <summary>The lows of the bars, oldest first.</summary>
        internal double[] Lows() => Column(window, bar => bar.Low);

        /// <summary>The closes of the bars, oldest first.</summary>
        internal double[] Closes() => Column(window, bar => bar.Close);

        /// <summary>The volumes of the bars, oldest first (NaN for a bar that came without one).</summary>
        internal double[] Volumes() => Column(window, bar => bar.Volume);
    }

    extension(double[] values)
    {
        /// <summary>The last value of a batch result, or NaN when the batch has none yet.</summary>
        internal double LastOrNaN() => values.Length > 0 ? values[^1] : double.NaN;
    }

    private static double[] Column(ReadOnlySpan<OhlcvBar> window, Func<OhlcvBar, double> pick)
    {
        var column = new double[window.Length];
        for (int i = 0; i < window.Length; i++) column[i] = pick(window[i]);
        return column;
    }
}
