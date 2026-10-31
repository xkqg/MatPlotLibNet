// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>How a correlation is worked out and drawn (matplotlib's <c>acorr</c> and <c>xcorr</c>): how many lags either side of
/// zero, whether the sums are divided to a scale of −1 to 1, what is taken out of the signals first, and whether the result is
/// drawn as stems or as markers.
/// The value you get from <c>default</c> holds matplotlib's defaults: ten lags either side, normalised, no detrend, and stems
/// (<c>maxlags=10</c>, <c>normed=True</c>, <c>detrend_none</c>, <c>usevlines=True</c>).
/// <para>Set only what differs from those defaults with an object initializer; the number of lags refuses, by its own name, a
/// value no correlation can be drawn with. Whether the lags fit the signal, which needs more samples than lags, is checked where
/// the options meet a signal.</para></summary>
/// <example>
/// <code>
/// var options = new CorrelationOptions { MaxLags = 30, Detrend = SpectralDetrend.Mean, Markers = true };
/// </code>
/// </example>
public readonly record struct CorrelationOptions
{
    private readonly int? _maxLags;

    /// <summary>The number of lags drawn either side of lag 0, so the chart shows 2 · MaxLags + 1 lags (matplotlib's
    /// <c>maxlags</c>). Default 10. The signal must hold more samples than this: with <c>n</c> samples the most there is to show
    /// is <c>n − 1</c> lags either side.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is below 1.</exception>
    public int MaxLags
    {
        get => _maxLags ?? 10;
        init => _maxLags = value >= 1
            ? value
            : throw new ArgumentOutOfRangeException(nameof(MaxLags), value, "A correlation shows at least one lag either side of lag 0.");
    }

    /// <summary>Whether the sums are left as they are. By default (<see langword="false"/>) each is divided by the root of the
    /// energy of the first signal times the root of the energy of the second, so the correlation of a signal with itself is
    /// exactly 1 at lag 0 and no value is beyond 1 (matplotlib's <c>normed=True</c>). A signal with no energy has no correlation
    /// to scale: normalised, it is a gap, never a zero.</summary>
    public bool Unnormalised { get; init; }

    /// <summary>What is taken out of each signal before the sums are worked out, so an offset or a drift does not fill every
    /// lag. Default <see cref="SpectralDetrend.None"/> (matplotlib's <c>detrend_none</c>); <see cref="SpectralDetrend.Mean"/>
    /// takes out the mean. The detrend is applied to each signal as a whole.</summary>
    public SpectralDetrend Detrend { get; init; }

    /// <summary>Whether the correlation is drawn as markers instead of stems. By default (<see langword="false"/>) it is a stem
    /// from 0 to the correlation at each lag with a reference line at 0 (matplotlib's <c>usevlines=True</c>); set, it is a circle
    /// at the correlation of each lag and nothing else, as matplotlib draws it with <c>usevlines=False</c>. The two are never
    /// drawn together.</summary>
    public bool Markers { get; init; }
}
