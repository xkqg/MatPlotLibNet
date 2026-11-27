// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Garman-Klass volatility estimator. 7.4× more efficient than close-to-close,
/// assumes zero drift. Pure OHLC, no volume needed.</summary>
/// <remarks>σ²_GK(bar) = 0.5·(ln(H/L))² − (2·ln(2) − 1)·(ln(C/O))². Per-window output is
/// <c>√mean(σ²_GK)</c> over the rolling window. Annualization is the caller's responsibility.
/// Reference: Garman &amp; Klass (1980), <i>Journal of Business</i> 53(1).</remarks>
public sealed class GarmanKlass : CandleIndicator<SignalResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 20;

    private static readonly double TwoLn2Minus1 = 2 * Math.Log(2) - 1;

    private readonly int _period;

    /// <summary>Creates a new Garman-Klass volatility indicator.</summary>
    /// <param name="bars">Open, High, Low and Close (all strictly positive); built via <see cref="OhlcvBars.Ohlc"/>.</param>
    /// <param name="period">Rolling window length in bars (default <see cref="DefaultPeriod"/>).</param>
    /// <exception cref="ArgumentException">Thrown when any input bar has a non-positive price.</exception>
    public GarmanKlass(OhlcvBars bars, int period = DefaultPeriod)
        : base(bars, Spec(period))
    {
        bars.ThrowIfNotPositiveOhlc(nameof(GarmanKlass));
        _period = period;
    }

    internal static IndicatorSpec Spec(int period) => IndicatorSpec.OfWindow("GK", period, period - 1);

    /// <inheritdoc />
    public override SignalResult Compute()
    {
        int n = BarCount;

        // Per-bar σ²_GK
        var sigma2 = new double[n];
        for (int i = 0; i < n; i++)
        {
            double lhl = Math.Log(High[i] / Low[i]);
            double lco = Math.Log(Close[i] / Open[i]);
            sigma2[i] = 0.5 * lhl * lhl - TwoLn2Minus1 * lco * lco;
        }

        double[] result = sigma2.RollingMean(_period);
        for (int i = 0; i < result.Length; i++) result[i] = Math.Sqrt(Math.Max(0, result[i]));
        return result;
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) => PlotLine(axes, Compute());
}
