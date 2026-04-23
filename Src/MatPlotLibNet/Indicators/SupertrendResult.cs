// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Indicators;

/// <summary>Result of <see cref="Supertrend"/>: the stop-line series plus the per-bar trend
/// direction and the flip markers. <see cref="Line"/> sits below price in uptrends
/// (<c>Direction = +1</c>) and above price in downtrends (<c>Direction = -1</c>).
/// <see cref="Flipped"/><c>[i]</c> is <see langword="true"/> on the single bar where
/// <c>Direction[i] != Direction[i-1]</c>.</summary>
/// <param name="Line">The Supertrend stop-line value at each output bar.</param>
/// <param name="Direction">Per-bar trend direction: <c>+1</c> = uptrend, <c>-1</c> = downtrend.</param>
/// <param name="Flipped">Per-bar flip marker — <see langword="true"/> iff direction changed on
/// this bar. First bar is always <see langword="false"/> (no predecessor to compare with).</param>
public readonly record struct SupertrendResult(double[] Line, int[] Direction, bool[] Flipped)
    : IIndicatorResult;
