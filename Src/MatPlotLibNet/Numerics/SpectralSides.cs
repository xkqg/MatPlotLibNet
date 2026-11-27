// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Which frequencies a spectrum covers (matplotlib's <c>sides</c>): only the frequencies from 0 up to half the sample
/// rate, or the negative ones too. A real signal has the same power at a frequency and at its negative, so the one-sided
/// spectrum folds the two into one bin (it doubles the power of every bin but the first and a last bin at half the sample
/// rate) and the two-sided one shows both, each at the power it has.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder, remove, or renumber.</remarks>
public enum SpectralSides
{
    /// <summary>What matplotlib's <c>sides='default'</c> does for a real signal, which every signal of this library is: the
    /// same as <see cref="OneSided"/>.</summary>
    Default = 0,

    /// <summary>The frequencies from 0 to half the sample rate, the power of a bin and of its negative folded into one
    /// (matplotlib's <c>sides='onesided'</c>): a transform of M samples gives M / 2 + 1 bins.</summary>
    OneSided = 1,

    /// <summary>The frequencies from the lowest negative one up to the highest positive one, in ascending order, each at its
    /// own power, so a bin has half the power the one-sided spectrum gives its frequency (matplotlib's
    /// <c>sides='twosided'</c>): a transform of M samples gives M bins, the first at minus half the sample rate when M is
    /// even.</summary>
    TwoSided = 2,
}
