// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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
    /// <param name="high">High prices.</param>
    /// <param name="low">Low prices.</param>
    /// <param name="close">Close prices.</param>
    /// <param name="period">ATR lookback. Default 10. Must be ≥ 1.</param>
    /// <param name="multiplier">ATR band multiplier. Default 3.0. Must be &gt; 0.</param>
    public Supertrend(double[] high, double[] low, double[] close,
        int period = 10, double multiplier = 3.0)
        : base(high, low, close)
    {
        if (high.Length != low.Length || high.Length != close.Length)
            throw new ArgumentException(
                $"high ({high.Length}), low ({low.Length}), and close ({close.Length}) must have equal length.");
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

        // ATR series — length n - period, aligned so atr[i] corresponds to global bar index period+i.
        var atr = new Atr(High, Low, Close, _period).Compute().Values;

        int outLen = n - _period;
        var line = new double[outLen];
        var direction = new int[outLen];
        var flipped = new bool[outLen];

        double prevFinalUpper = 0;
        double prevFinalLower = 0;
        int prevDirection = +1;

        for (int i = 0; i < outLen; i++)
        {
            int t = _period + i;
            double mid = (High[t] + Low[t]) / 2.0;
            double basicUpper = mid + _multiplier * atr[i];
            double basicLower = mid - _multiplier * atr[i];

            double finalUpper;
            double finalLower;
            int dir;
            bool flip;

            if (i == 0)
            {
                finalUpper = basicUpper;
                finalLower = basicLower;
                dir = +1;
                flip = false;
            }
            else
            {
                // Seban's outward-only clamp: band tightens when it moves inward, otherwise holds.
                finalUpper = (basicUpper < prevFinalUpper || Close[t - 1] > prevFinalUpper)
                    ? basicUpper : prevFinalUpper;
                finalLower = (basicLower > prevFinalLower || Close[t - 1] < prevFinalLower)
                    ? basicLower : prevFinalLower;

                // Direction flips at the opposite previous-bar band.
                if (Close[t] > prevFinalUpper) dir = +1;
                else if (Close[t] < prevFinalLower) dir = -1;
                else dir = prevDirection;

                flip = dir != prevDirection;
            }

            line[i] = dir > 0 ? finalLower : finalUpper;
            direction[i] = dir;
            flipped[i] = flip;

            prevFinalUpper = finalUpper;
            prevFinalLower = finalLower;
            prevDirection = dir;
        }

        return new SupertrendResult(line, direction, flipped);
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) =>
        PlotSignal(axes, Compute().Line, warmup: _period);
}
