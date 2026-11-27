// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Exhaustive edge-case coverage for <see cref="SymlogTransform"/>.
///
/// <para><b>Why this exists:</b> v1.7.0 shipped a symlog rendering bug because the
/// transform was 30% covered — only the Forward happy path. The Inverse function and
/// boundary inputs (linthresh = 0, NaN, ±∞) had no tests. This file brings coverage
/// to 100% by testing every branch + every edge case described in the public contract.</para>
///
/// <para><b>Reference formula (matplotlib SymmetricalLogTransform, base 10, linscale 1, so s = 1 / (1 - 1/10)):</b></para>
/// <code>
///   forward(x, L) = x * s                          if |x| &lt;= L
///                 = sign(x) * L * (s + log10(|x|/L))   otherwise
///   inverse(y, L) = y / s                          if |y| &lt;= L * s
///                 = sign(y) * L * 10^(|y|/L - s)      otherwise
///   linthresh &lt;= 0  -->  treated as 1.0 (defensive default)
/// </code>
///
/// Reference values pre-computed in <see cref="NumpyReference.SymlogForward_Linthresh100"/>.
/// </summary>
public class SymlogTransformTests
{
    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>Infinity goes to infinity of its own sign, in both directions: log10(∞) == ∞, so forward(∞, L) == ∞ via the log branch,
    /// and 10^∞ == ∞, so inverse(∞, L) goes via the exp branch.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Infinity_ProducesInfinity(bool forward)
    {
        double Apply(double value) => forward ? SymlogTransform.Forward(value, 1) : SymlogTransform.Inverse(value, 1);

        Assert.True(double.IsPositiveInfinity(Apply(double.PositiveInfinity)));
        Assert.True(double.IsNegativeInfinity(Apply(double.NegativeInfinity)));
    }

    public class Forward
    {
        [Fact]
        public void Zero_ReturnsZero_ForAnyLinthresh()
        {
            Assert.Equal(0, SymlogTransform.Forward(0, linthresh: 1));
            Assert.Equal(0, SymlogTransform.Forward(0, linthresh: 100));
            Assert.Equal(0, SymlogTransform.Forward(0, linthresh: 0.001));
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(100.0)]
        [InlineData(0.5)]
        public void BoundaryValue_AtLinthresh_IsLinear(double linthresh)
        {
            // Continuity at the linear/log boundary: forward(±L, L) == ±L * s, from either branch.
            double s = 1 / (1 - 1 / 10.0);
            Assert.Equal(linthresh * s,  SymlogTransform.Forward( linthresh, linthresh), linthresh * 1e-12);
            Assert.Equal(-linthresh * s, SymlogTransform.Forward(-linthresh, linthresh), linthresh * 1e-12);
            Assert.Equal(linthresh * s,  SymlogTransform.Forward(linthresh * (1 + 1e-15), linthresh), linthresh * 1e-12);
        }

        [Theory]
        // (raw, expected) — pre-computed in NumpyReference.SymlogForward_Linthresh100
        [InlineData(0,        0)]
        [InlineData(50,       55.55555555555556)]     // linear branch, stretched by 1 / 0.9
        [InlineData(-50,      -55.55555555555556)]
        [InlineData(100,      111.11111111111111)]    // boundary
        [InlineData(-100,     -111.11111111111111)]
        [InlineData(1000,     211.1111111111111)]     // 100 * (1/0.9 + log10(10))
        [InlineData(-1000,    -211.1111111111111)]
        [InlineData(10000,    311.11111111111114)]
        [InlineData(-10000,   -311.11111111111114)]
        [InlineData(100000,   411.1111111111111)]
        [InlineData(-100000,  -411.1111111111111)]
        public void MatchesNumpyReference_Linthresh100(double raw, double expected)
        {
            Assert.Equal(expected, SymlogTransform.Forward(raw, linthresh: 100), 1e-9);
        }

        [Fact]
        public void NaN_PropagatesAsNaN()
        {
            Assert.True(double.IsNaN(SymlogTransform.Forward(double.NaN, 1)));
            Assert.True(double.IsNaN(SymlogTransform.Forward(double.NaN, 100)));
        }

        [Theory]
        [InlineData(0)]    // linthresh = 0 falls back to default 1.0
        [InlineData(-1)]   // negative is invalid → default 1.0
        [InlineData(-100)]
        public void InvalidLinthresh_FallsBackToDefault(double invalidL)
        {
            // With default L=1, Forward(10) equals 1 * (1/0.9 + log10(10)), matplotlib's 2.111111111111111.
            Assert.Equal(2.111111111111111, SymlogTransform.Forward(10, invalidL), 1e-12);
        }

        [Theory]
        [InlineData(0.5)]
        [InlineData(5)]
        [InlineData(50)]
        [InlineData(500)]
        [InlineData(5000)]
        [InlineData(50000)]
        public void IsAntisymmetric_AroundZero(double x)
        {
            // sign-symmetry: forward(-x, L) == -forward(x, L) for any positive x.
            Assert.Equal(-SymlogTransform.Forward(x, 100), SymlogTransform.Forward(-x, 100), 1e-12);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class Inverse
    {
        [Fact]
        public void Zero_ReturnsZero()
        {
            Assert.Equal(0, SymlogTransform.Inverse(0, linthresh: 1));
            Assert.Equal(0, SymlogTransform.Inverse(0, linthresh: 100));
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(100.0)]
        public void BoundaryValue_AtLinthresh_IsLinear(double linthresh)
        {
            // The image of the threshold, L * s, reads back as the threshold from the linear branch.
            double s = 1 / (1 - 1 / 10.0);
            Assert.Equal(linthresh,  SymlogTransform.Inverse( linthresh * s, linthresh), linthresh * 1e-12);
            Assert.Equal(-linthresh, SymlogTransform.Inverse(-linthresh * s, linthresh), linthresh * 1e-12);
            Assert.Equal(linthresh / s, SymlogTransform.Inverse(linthresh, linthresh), linthresh * 1e-12);
        }

        [Fact]
        public void NaN_PropagatesAsNaN()
        {
            Assert.True(double.IsNaN(SymlogTransform.Inverse(double.NaN, 100)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void InvalidLinthresh_FallsBackToDefault(double invalidL)
        {
            // With L=1, Inverse(2) is matplotlib's InvertedSymmetricalLogTransform(10, 1, 1) of 2: 7.742636826811271.
            Assert.Equal(7.742636826811271, SymlogTransform.Inverse(2, invalidL), 1e-12);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class RoundTrip
    {
        [Theory]
        [InlineData(-1e6)]
        [InlineData(-100)]
        [InlineData(-10)]
        [InlineData(-0.5)]
        [InlineData(0)]
        [InlineData(0.5)]
        [InlineData(50)]
        [InlineData(1e6)]
        public void Inverse_OfForward_RecoversInput(double x)
        {
            // Symlog is algebraically invertible: inverse(forward(x, L), L) == x.
            double L = 100;
            double rt = SymlogTransform.Inverse(SymlogTransform.Forward(x, L), L);
            Assert.Equal(x, rt, Math.Max(Math.Abs(x), 1) * 1e-12);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    public class Array
    {
        [Fact]
        public void EmptyInput_ReturnsEmpty()
        {
            var result = SymlogTransform.ForwardArray(EdgeCaseData.Empty, 1);
            Assert.Empty(result);
        }

        [Fact]
        public void SinglePoint_MatchesScalarForward()
        {
            var result = SymlogTransform.ForwardArray(EdgeCaseData.SinglePoint, 1);
            Assert.Single(result);
            Assert.Equal(SymlogTransform.Forward(EdgeCaseData.SinglePoint[0], 1), result[0]);
        }

        [Fact]
        public void VeryLarge_ProducesParityWithScalarForward()
        {
            var input = EdgeCaseData.Large(10_000);
            var result = SymlogTransform.ForwardArray(input, linthresh: 100);
            Assert.Equal(input.Length, result.Length);
            // Spot-check 5 indices including endpoints.
            foreach (int i in new[] { 0, 1, 5000, 9998, 9999 })
                Assert.Equal(SymlogTransform.Forward(input[i], 100), result[i], 1e-12);
        }
    }
}
