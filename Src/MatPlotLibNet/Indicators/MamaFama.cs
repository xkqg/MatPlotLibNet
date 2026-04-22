// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Ehlers' MESA Adaptive Moving Average (MAMA) and its trailing companion (FAMA).
/// Uses <see cref="HilbertDiscriminator"/> to extract the instantaneous dominant cycle period
/// and adapts the EMA smoothing factor to it. Reference: Ehlers (2001),
/// <i>Rocket Science for Traders</i>, Ch. 15; canonical cross-check: TA-Lib <c>ta_MAMA.c</c>.</summary>
public sealed class MamaFama : PriceIndicator<MamaFamaResult>
{
    private readonly double _fastLimit;
    private readonly double _slowLimit;

    /// <summary>Creates a new MAMA/FAMA indicator.</summary>
    /// <param name="prices">Price series (typically close, or median price for OHLC input).</param>
    /// <param name="fastLimit">Upper alpha bound in (0, 1]. Default 0.5.</param>
    /// <param name="slowLimit">Lower alpha bound in (0, fastLimit). Default 0.05.</param>
    public MamaFama(double[] prices, double fastLimit = 0.5, double slowLimit = 0.05) : base(prices)
    {
        if (fastLimit <= 0 || fastLimit > 1)
            throw new ArgumentException($"fastLimit must be in (0, 1] (got {fastLimit}).", nameof(fastLimit));
        if (slowLimit <= 0 || slowLimit >= fastLimit)
            throw new ArgumentException(
                $"slowLimit must be in (0, fastLimit={fastLimit}) (got {slowLimit}).", nameof(slowLimit));
        _fastLimit = fastLimit;
        _slowLimit = slowLimit;
        Label = $"MAMA({fastLimit:0.00}/{slowLimit:0.00})";
    }

    /// <inheritdoc />
    public override MamaFamaResult Compute()
    {
        int n = Prices.Length;
        if (n < 7) return new MamaFamaResult(Array.Empty<double>(), Array.Empty<double>());

        var (_, phase, _, _) = ((ReadOnlySpan<double>)Prices).HilbertDiscriminate();

        // Alpha / MAMA / FAMA — downstream of the Hilbert pipeline. Seed the first 6
        // bars with the raw price so mamaFull[i-1] is defined when the loop kicks in.
        var mamaFull = new double[n];
        var famaFull = new double[n];
        for (int i = 0; i < 6 && i < n; i++)
        {
            mamaFull[i] = Prices[i];
            famaFull[i] = Prices[i];
        }

        for (int i = 6; i < n; i++)
        {
            double deltaPhase = phase[i - 1] - phase[i];
            if (deltaPhase < 1) deltaPhase = 1;

            // deltaPhase ≥ 1 after the clamp above, so alpha = fastLimit/deltaPhase is ≤ fastLimit — no upper clamp needed.
            double alpha = _fastLimit / deltaPhase;
            if (alpha < _slowLimit) alpha = _slowLimit;

            mamaFull[i] = alpha * Prices[i] + (1 - alpha) * mamaFull[i - 1];
            famaFull[i] = 0.5 * alpha * mamaFull[i] + (1 - 0.5 * alpha) * famaFull[i - 1];
        }

        int outLen = n - 6;
        var mama = new double[outLen];
        var fama = new double[outLen];
        Array.Copy(mamaFull, 6, mama, 0, outLen);
        Array.Copy(famaFull, 6, fama, 0, outLen);
        return new MamaFamaResult(mama, fama);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes)
    {
        var result = Compute();
        PlotSignal(axes, result.Mama, warmup: 6, label: "MAMA");
        PlotSignal(axes, result.Fama, warmup: 6, label: "FAMA");
    }
}
