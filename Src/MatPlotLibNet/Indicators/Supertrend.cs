// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Indicators;

/// <summary>Olivier Seban's Supertrend (2008) — ATR-based trailing-stop trend follower.
/// Bands at <c>(H+L)/2 ± multiplier·ATR</c> clamp outward-only via the Seban recurrence;
/// price closing beyond the opposite band flips direction. The <see cref="SupertrendResult.Line"/>
/// sits below price in uptrends and above price in downtrends, giving a single clean stop that
/// mainstream retail platforms render in green/red. Reference: Seban, O. (2008),
/// <i>La méthode magique des turtles modernes</i>; popularised as TradingView <c>ta.supertrend()</c>.</summary>
public sealed class Supertrend : CandleIndicator<SupertrendResult>
{
    /// <summary>The <c>period</c> the indicator is made with when the caller gives none.</summary>
    public const int DefaultPeriod = 10;

    /// <summary>The <c>multiplier</c> the indicator is made with when the caller gives none.</summary>
    public const double DefaultMultiplier = 3.0;

    private readonly int _period;
    private readonly double _multiplier;

    /// <summary>Creates a new Supertrend indicator.</summary>
    /// <param name="bars">High, Low and Close; built via <see cref="OhlcvBars.Hlc"/>.</param>
    /// <param name="period">ATR lookback. Default <see cref="DefaultPeriod"/>. Must be ≥ 1.</param>
    /// <param name="multiplier">ATR band multiplier. Default <see cref="DefaultMultiplier"/>. Must be &gt; 0.</param>
    public Supertrend(OhlcvBars bars, int period = DefaultPeriod, double multiplier = DefaultMultiplier)
        : base(bars, Spec(period, multiplier))
    {
        _period = period;
        _multiplier = multiplier;
    }

    internal static IndicatorSpec Spec(int period, double multiplier)
    {
        period.ThrowIfBelow(1, nameof(period));
        multiplier.ThrowIfNotAbove(0, nameof(multiplier));
        string label = string.Invariant($"ST({period},{multiplier.InvariantText("0.#")})");
        return IndicatorSpec.Of(period, label, label, $"{label} Direction", $"{label} Flip");
    }

    /// <inheritdoc />
    /// <remarks>The first value comes with bar <c>period</c>, the first bar the average true range has a value for.</remarks>
    public override SupertrendResult Compute() =>
        FoldHlc<SupertrendStep, SupertrendPoint, Lines>(new(_period, _multiplier), new(ValueCount)).Result;

    /// <summary>The line, its direction and its flips, one entry per bar from the first value on.</summary>
    private readonly struct Lines(int count) : IOutputLines<SupertrendPoint>
    {
        private readonly double[] _line = new double[count];
        private readonly int[] _direction = new int[count];
        private readonly bool[] _flipped = new bool[count];

        internal SupertrendResult Result => new(_line, _direction, _flipped);

        public void Set(int index, SupertrendPoint output)
        {
            _line[index] = output.Line;
            _direction[index] = output.Direction;
            _flipped[index] = output.Flipped;
        }
    }

    /// <inheritdoc />
    public override void Apply(Axes axes) =>
        PlotLine(axes, Compute().Line);
}
