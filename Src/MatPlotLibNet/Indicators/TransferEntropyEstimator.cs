// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>How <see cref="TransferEntropy"/> estimates the flow from a source series to a target series.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder, remove, or renumber.</remarks>
public enum TransferEntropyEstimator
{
    /// <summary>Histogram counts over <see cref="TransferEntropyOptions.Bins"/> equal-width bins. Biased upward on short series,
    /// so it is not the default; it is what <see cref="TransferEntropy"/> always gave.</summary>
    Binned = 0,

    /// <summary>The Kraskov nearest-neighbour estimate (Frenzel-Pompe conditional form): no bins, a small bias, and a result below 0
    /// for series that carry nothing to each other.</summary>
    Kraskov = 1,
}
