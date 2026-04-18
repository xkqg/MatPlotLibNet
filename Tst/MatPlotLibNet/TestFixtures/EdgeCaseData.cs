// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

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

    /// <summary>Two-element array — minimum input that produces a line segment.</summary>
    public static double[] TwoPoints => new[] { 1.0, 2.0 };

    /// <summary>All-NaN array — coordinate transforms should propagate NaN (not throw).</summary>
    public static double[] AllNaN => new[] { double.NaN, double.NaN, double.NaN };

    /// <summary>Mixed NaN/finite — gap behavior in line/area series.</summary>
    public static double[] MixedNaN => new[] { 1.0, double.NaN, 3.0, 4.0, double.NaN, 6.0 };

    /// <summary>Positive infinity — log/symlog must not produce garbage.</summary>
    public static double[] PositiveInfinity => new[] { 1.0, 2.0, double.PositiveInfinity };

    /// <summary>Mixed ±infinity — symlog symmetric handling.</summary>
    public static double[] MixedInfinity => new[] { double.NegativeInfinity, -1.0, 0.0, 1.0, double.PositiveInfinity };

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

    /// <summary>Linear ramp 0..n-1. Used as a no-noise baseline in renderer tests so any
    /// SVG anomaly traces back to the renderer itself, not data variability.</summary>
    public static double[] Ramp(int n) => Enumerable.Range(0, n).Select(i => (double)i).ToArray();

    /// <summary>Sinusoid sin(0..2π) sampled at <paramref name="n"/> points.</summary>
    public static double[] Sin(int n) =>
        Enumerable.Range(0, n).Select(i => Math.Sin(2 * Math.PI * i / (n - 1))).ToArray();

    /// <summary>Large array (default 100K). Tests memory + SIMD batch paths in
    /// <c>DataTransform.TransformBatch</c>, downsamplers, and indicators.</summary>
    public static double[] Large(int n = 100_000) => Ramp(n);

    /// <summary>Descending ramp — exercises sort-assumption bugs in renderers.</summary>
    public static double[] Descending(int n) =>
        Enumerable.Range(0, n).Reverse().Select(i => (double)i).ToArray();

    /// <summary>All-equal array — degenerate range (max == min). Many normalisers and
    /// scales divide by (max-min) and must guard against this.</summary>
    public static double[] AllEqual(int n, double value = 5.0) =>
        Enumerable.Repeat(value, n).ToArray();
}
