// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Indicators;

/// <summary>Olivier Seban's Supertrend (2008) — ATR-based trailing-stop trend follower.
/// Bands at <c>(H+L)/2 ± multiplier·ATR</c> clamp outward-only via the Seban recurrence;
/// price closing beyond the opposite band flips direction. The <see cref="SupertrendResult.Line"/>
/// sits below price in uptrends and above price in downtrends, giving a single clean stop that
/// mainstream retail platforms render in green/red. Reference: Seban, O. (2008),
/// <i>La méthode magique des turtles modernes</i>; popularised as TradingView <c>ta.supertrend()</c>.</summary>
public sealed class Supertrend : CandleIndicator<SupertrendResult>
{
    private readonly int _period;
    private readonly double _multiplier;

    /// <summary>Creates a new Supertrend indicator.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="period">ATR lookback. Default 10. Must be ≥ 1.</param>
    /// <param name="multiplier">ATR band multiplier. Default 3.0. Must be &gt; 0.</param>
    public Supertrend(OhlcvBars bars, int period = 10, double multiplier = 3.0)
        : base(bars)
    {
        if (period < 1)
            throw new ArgumentException($"period must be >= 1 (got {period}).", nameof(period));
        if (multiplier <= 0)
            throw new ArgumentException($"multiplier must be > 0 (got {multiplier}).", nameof(multiplier));
        _period = period;
        _multiplier = multiplier;
        Label = $"ST({period},{multiplier:0.#})";
    }

    /// <inheritdoc />
    public override SupertrendResult Compute()
    {
        int n = BarCount;
        if (n <= _period)
            return new SupertrendResult(
                Array.Empty<double>(), Array.Empty<int>(), Array.Empty<bool>());

        // The first value comes with bar `period`, the first bar the average true range has a value for.
        int outLen = n - _period;
        var line = new double[outLen];
        var direction = new int[outLen];
        var flipped = new bool[outLen];
        var step = new SupertrendStep(_period, _multiplier);
        for (int t = 0; t < n; t++)
        {
            var point = step.Next(High[t], Low[t], Close[t]);
            if (t < _period) continue;
            line[t - _period] = point.Line;
            direction[t - _period] = point.Direction;
            flipped[t - _period] = point.Flipped;
        }

        return new SupertrendResult(line, direction, flipped);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) =>
        PlotSignal(axes, Compute().Line, warmup: _period);
}
