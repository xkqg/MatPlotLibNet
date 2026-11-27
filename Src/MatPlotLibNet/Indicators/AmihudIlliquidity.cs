// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Amihud illiquidity — ratio of absolute return to dollar volume, averaged over a
/// rolling window. High = illiquid; low = deep, liquid market. Most-cited illiquidity measure
/// in academic finance.</summary>
/// <remarks>Per-bar: <c>ILLIQ_t = |ln(C_t/C_{t-1})| / (C_t · V_t)</c>. Output is the rolling
/// mean over <c>period</c> bars. When <c>V_t = 0</c>, the bar contributes
/// <see cref="double.PositiveInfinity"/> (matches academic convention: no volume = maximally
/// illiquid). Reference: Amihud (2002), <i>Journal of Financial Markets</i> 5(1).</remarks>
public sealed class AmihudIlliquidity : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 20;

    private readonly int _period;

    /// <summary>Creates a new Amihud illiquidity indicator.</summary>
    /// <param name="bars">Close (strictly positive) and Volume; built via <see cref="OhlcvBars.Cv"/>.</param>
    /// <param name="period">Rolling window length (default <see cref="DefaultPeriod"/>, must be ≥ 1).</param>
    /// <exception cref="ArgumentException">Thrown on length mismatch, non-positive period,
    /// or non-positive close.</exception>
    public AmihudIlliquidity(OhlcvBars bars, int period = DefaultPeriod)
        : base(bars, Spec(period))
    {
        bars.Close.ThrowIfNotPositivePrices(nameof(AmihudIlliquidity), "close", nameof(bars));
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfCheckedWindow("ILLIQ", period, 1, period);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        // Per-bar ILLIQ over bars 1..n-1 (length n-1).
        return RollingMeanOfPairs(_period, t =>
        {
            double absR = Math.Abs(Math.Log(Close[t] / Close[t - 1]));
            double dollarVol = Close[t] * Volume[t];
            return dollarVol == 0 ? double.PositiveInfinity : absR / dollarVol;
        });
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
