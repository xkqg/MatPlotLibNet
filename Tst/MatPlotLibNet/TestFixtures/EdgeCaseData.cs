// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;

namespace MatPlotLibNet.Tests.TestFixtures;

/// <summary>Reusable edge-case data generators. Every test file in the coverage uplift
/// uses these instead of inlining its own arrays — keeps "what's an edge case" consistent
/// across the whole suite. If we ever discover a new edge case (e.g., subnormal doubles),
/// we add it ONCE here and every test that uses <see cref="BoundaryDoubles"/> picks it up.</summary>
public static class EdgeCaseData
{
    /// <summary>Empty array — most series renderers must early-return without throwing.</summary>
    public static double[] Empty => Array.Empty<double>();

    /// <summary>Single-element array — minimum non-empty input.</summary>
    public static double[] SinglePoint => new[] { 42.0 };

    /// <summary>All-NaN array — coordinate transforms should propagate NaN (not throw).</summary>
    public static double[] AllNaN => new[] { double.NaN, double.NaN, double.NaN };

    /// <summary>Mixed NaN/finite — gap behavior in line/area series.</summary>
    public static double[] MixedNaN => new[] { 1.0, double.NaN, 3.0, 4.0, double.NaN, 6.0 };

    /// <summary>Boundary doubles — values that historically reveal bugs (zero, ±1, max,
    /// min, epsilon, NaN, ±∞). Use as <c>[InlineData]</c> source via the params expansion
    /// in derived TheoryData generators.</summary>
    public static IReadOnlyList<double> BoundaryDoubles { get; } = new[]
    {
        0.0,
        1.0, -1.0,
        double.Epsilon, -double.Epsilon,
        1e-300, -1e-300,
        1e300, -1e300,
        double.MaxValue, double.MinValue,
        double.PositiveInfinity, double.NegativeInfinity,
        double.NaN,
    };

    /// <summary>Checks that a volatility indicator reads seven bars that never move (open, high, low and close all 100) as no volatility at
    /// all: a value for each window, none of them NaN, every one 0.</summary>
    /// <param name="values">Computes the indicator's values over the bars, with a window of five.</param>
    public static void AssertFlatBarsHaveNoVolatility(Func<OhlcvBars, double[]> values)
    {
        double[] flat = [100, 100, 100, 100, 100, 100, 100];

        var result = values(OhlcvBars.Ohlc(flat, flat, flat, flat));

        Assert.NotEmpty(result);
        Assert.All(result, v => Assert.Equal(0.0, v, precision: 12));
    }

    /// <summary>Linear ramp 0..n-1. Used as a no-noise baseline in renderer tests so any
    /// SVG anomaly traces back to the renderer itself, not data variability.</summary>
    public static double[] Ramp(int n) => Enumerable.Range(0, n).Select(i => (double)i).ToArray();

    /// <summary>Sinusoid sin(0..2π) sampled at <paramref name="n"/> points.</summary>
    public static double[] Sin(int n) =>
        Enumerable.Range(0, n).Select(i => Math.Sin(2 * Math.PI * i / (n - 1))).ToArray();

    /// <summary>Large array (default 100K). Tests memory + SIMD batch paths in
    /// <c>DataTransform.TransformBatch</c>, downsamplers, and indicators.</summary>
    public static double[] Large(int n = 100_000) => Ramp(n);

    /// <summary>All-equal array — degenerate range (max == min). Many normalisers and
    /// scales divide by (max-min) and must guard against this.</summary>
    public static double[] AllEqual(int n, double value = 5.0) =>
        Enumerable.Repeat(value, n).ToArray();

    /// <summary>Asserts that a rolling window of the indicator gave one value, which is <paramref name="expected"/> to <paramref name="precision"/> places.</summary>
    /// <param name="result">The values the indicator computed.</param>
    /// <param name="expected">The value it must be.</param>
    /// <param name="precision">How many decimal places must agree.</param>
    public static void AssertOneValue(double[] result, double expected, int precision)
    {
        Assert.Single(result);
        Assert.Equal(expected, result[0], precision);
    }
}
