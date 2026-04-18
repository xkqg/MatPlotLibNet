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
/// <para><b>Reference formula (matplotlib SymLogScale):</b></para>
/// <code>
///   forward(x, L) = x                            if |x| &lt;= L
///                 = sign(x) * L * (1 + log10(|x|/L))   otherwise
///   inverse(y, L) = y                            if |y| &lt;= L
///                 = sign(y) * L * 10^(|y|/L - 1)      otherwise
///   linthresh &lt;= 0  -->  treated as 1.0 (defensive default)
/// </code>
///
/// Reference values pre-computed in <see cref="NumpyReference.SymlogForward_Linthresh100"/>.
/// </summary>
public class SymlogTransformEdgeCaseTests
{
    // ──────────────────────────────────────────────────────────────────────────
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
            // Continuity at the linear/log boundary: forward(±L, L) == ±L.
            Assert.Equal(linthresh,  SymlogTransform.Forward( linthresh, linthresh), 1e-12);
            Assert.Equal(-linthresh, SymlogTransform.Forward(-linthresh, linthresh), 1e-12);
        }

        [Theory]
        // (raw, expected) — pre-computed in NumpyReference.SymlogForward_Linthresh100
        [InlineData(0,        0)]
        [InlineData(50,       50)]                // linear branch
        [InlineData(-50,      -50)]
        [InlineData(100,      100)]               // boundary
        [InlineData(-100,     -100)]
        [InlineData(1000,     200)]               // 100 * (1 + log10(10))
        [InlineData(-1000,    -200)]
        [InlineData(10000,    300)]
        [InlineData(-10000,   -300)]
        [InlineData(100000,   400)]
        [InlineData(-100000,  -400)]
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

        [Fact]
        public void Infinity_ProducesInfinity()
        {
            // log10(∞) == ∞, so forward(∞, L) == ∞ via the log branch.
            Assert.True(double.IsPositiveInfinity(SymlogTransform.Forward(double.PositiveInfinity, 1)));
            Assert.True(double.IsNegativeInfinity(SymlogTransform.Forward(double.NegativeInfinity, 1)));
        }

        [Theory]
        [InlineData(0)]    // linthresh = 0 falls back to default 1.0
        [InlineData(-1)]   // negative is invalid → default 1.0
        [InlineData(-100)]
        public void InvalidLinthresh_FallsBackToDefault(double invalidL)
        {
            // With default L=1, Forward(10) should equal 1*(1+log10(10)) == 2.
            Assert.Equal(2.0, SymlogTransform.Forward(10, invalidL), 1e-9);
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
            Assert.Equal(linthresh,  SymlogTransform.Inverse( linthresh, linthresh), 1e-12);
            Assert.Equal(-linthresh, SymlogTransform.Inverse(-linthresh, linthresh), 1e-12);
        }

        [Fact]
        public void NaN_PropagatesAsNaN()
        {
            Assert.True(double.IsNaN(SymlogTransform.Inverse(double.NaN, 100)));
        }

        [Fact]
        public void Infinity_ProducesInfinity()
        {
            // 10^∞ == ∞, so inverse(∞, L) goes via the exp branch.
            Assert.True(double.IsPositiveInfinity(SymlogTransform.Inverse(double.PositiveInfinity, 1)));
            Assert.True(double.IsNegativeInfinity(SymlogTransform.Inverse(double.NegativeInfinity, 1)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void InvalidLinthresh_FallsBackToDefault(double invalidL)
        {
            // With L=1, Inverse(2) should give 10 (since Forward(10, 1) == 2).
            Assert.Equal(10.0, SymlogTransform.Inverse(2, invalidL), 1e-9);
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
            Assert.Equal(x, rt, 1e-9);
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
