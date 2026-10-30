// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Ehlers;
using MatPlotLibNet.Indicators.Steps;
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
        Label = $"MAMA({fastLimit.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}/{slowLimit.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)})";
    }

    /// <inheritdoc />
    public override MamaFamaResult Compute()
    {
        int n = Prices.Length;
        if (n < 7) return new MamaFamaResult(Array.Empty<double>(), Array.Empty<double>());

        var mama = new double[n - 6];
        var fama = new double[n - 6];
        var step = new MamaFamaStep(_fastLimit, _slowLimit);
        for (int i = 0; i < n; i++)
        {
            var point = step.Next(Prices[i]);
            if (i < 6) continue;
            mama[i - 6] = point.Mama;
            fama[i - 6] = point.Fama;
        }

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
