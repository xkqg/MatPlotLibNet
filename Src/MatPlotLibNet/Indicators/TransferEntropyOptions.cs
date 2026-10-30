// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>The settings of a <see cref="TransferEntropy"/>. Transfer entropy is in nats (divide by ln 2 for bits); use log-returns,
/// not price levels, for a series that trends.</summary>
/// <param name="Estimator">How the flow is estimated; the Kraskov nearest-neighbour estimate by default.</param>
/// <param name="Neighbours">The k of the Kraskov estimate; at least 1, and below the number of samples. Default 4.</param>
/// <param name="Bins">The number of bins of the binned estimate; at least 2. Default 8. The Kraskov estimate does not read it.</param>
/// <param name="Lag">How many bars the source leads the target by; at least 1. Default 1.</param>
/// <param name="Window">How many bars the flow is estimated over, giving one value for each bar (the entropy of the last
/// <c>Window</c> bars, stamped at the right edge, NaN until the first window is full); null gives one value for the whole series.
/// At least <c>Lag + Neighbours + 1</c>. The Kraskov estimate only: the binned estimate is biased upward on a window.</param>
/// <param name="Stride">Work out every <c>Stride</c>-th window, counted from the first full one, and leave the bars between NaN;
/// at least 1 (every window), and only with a <paramref name="Window"/>. The cost of a window is O(Window²).</param>
/// <param name="Condition">A third series the flow is conditioned on (a common driver), or null. It is read at the bar of the source.
/// The Kraskov estimate only.</param>
public readonly record struct TransferEntropyOptions(
    TransferEntropyEstimator Estimator = TransferEntropyEstimator.Kraskov,
    int Neighbours = 4,
    int Bins = 8,
    int Lag = 1,
    int? Window = null,
    int Stride = 1,
    double[]? Condition = null)
{
    /// <summary>The default settings: the Kraskov estimate with 4 neighbours, 8 bins, lag 1 and no condition. (A struct made with
    /// <c>new()</c> is otherwise all zeros, which is not a setting.)</summary>
    public TransferEntropyOptions() : this(TransferEntropyEstimator.Kraskov)
    {
    }
}
