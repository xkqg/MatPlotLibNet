// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="TurbulenceIndex"/>. Covers all 10 branches enumerated in
/// docs/contrib/indicator-tier-2a.md §2, plus the internal regularization helper.</summary>
public class TurbulenceIndexTests
{
    // ── Branch 1 — Empty feature matrix → empty output ──
    [Fact]
    public void Compute_EmptyFeatures_ReturnsEmpty()
    {
        var t = new TurbulenceIndex([]);
        Assert.Empty(t.Compute().Values);
    }

    // ── Branch 2 — Non-rectangular → throws via base class ──
    [Fact]
    public void Constructor_NonRectangular_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TurbulenceIndex([new[] { 1.0, 2.0 }, new[] { 3.0 }]));
    }

    // ── Branch 3 — BarCount <= window → empty ──
    [Fact]
    public void Compute_BarCountEqualsWindow_ReturnsEmpty()
    {
        var features = new[] { new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 } };
        var t = new TurbulenceIndex(features, window: 3);
        Assert.Empty(t.Compute().Values);
    }

    [Fact]
    public void Compute_BarCountBelowWindow_ReturnsEmpty()
    {
        var features = new[] { new[] { 1.0 }, new[] { 2.0 } };
        var t = new TurbulenceIndex(features, window: 5);
        Assert.Empty(t.Compute().Values);
    }

    // ── Branch 4 — BarCount == window + 1 → single output ──
    [Fact]
    public void Compute_BarCountEqualsWindowPlusOne_ReturnsSingleValue()
    {
        var features = new[] { new[] { 0.0 }, new[] { 2.0 }, new[] { 4.0 } };
        var t = new TurbulenceIndex(features, window: 2);
        Assert.Single(t.Compute().Values);
    }

    // ── Branch 5 — window < 2 throws ──
    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WindowBelowTwo_Throws(int window)
    {
        var features = new[] { new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 } };
        Assert.Throws<ArgumentException>(() => new TurbulenceIndex(features, window));
    }

    // ── Branch 6 — regularization < 0 throws ──
    [Fact]
    public void Constructor_NegativeRegularization_Throws()
    {
        var features = new[] { new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 } };
        Assert.Throws<ArgumentException>(() =>
            new TurbulenceIndex(features, window: 2, regularization: -0.1));
    }

    // ── Branch 7 — FeatureCount > 10 throws ──
    [Fact]
    public void Constructor_TooManyFeatures_Throws()
    {
        var features = new double[5][];
        for (int i = 0; i < 5; i++) features[i] = new double[11];
        Assert.Throws<ArgumentException>(() => new TurbulenceIndex(features));
    }

    // ── Branch 8 — Univariate (FeatureCount == 1) reduces to z-score² ──
    // Window [0, 2]: mean=1, sample variance=((0-1)² + (2-1)²)/1 = 2.
    // TI at bar 2 = (4-1)²/(2+λ) ≈ 9/2 = 4.5 (with λ=1e-6, essentially the same).
    [Fact]
    public void Compute_UnivariateShock_MatchesZScoreSquared()
    {
        var features = new[] { new[] { 0.0 }, new[] { 2.0 }, new[] { 4.0 } };
        var t = new TurbulenceIndex(features, window: 2);
        var r = t.Compute().Values;
        Assert.Single(r);
        Assert.Equal(4.5, r[0], precision: 4);
    }

    // Spec reference: uni = [[1],[2],[3],[4],[5],[100]], window=5.
    // Window mean=3, sample variance = ((1-3)²+(2-3)²+0+(4-3)²+(5-3)²)/4 = 10/4 = 2.5.
    // TI = (100-3)²/(2.5 + 1e-6) ≈ 9409/2.5 = 3763.6.
    [Fact]
    public void Compute_UnivariateExtremeShock_MatchesSpecVector()
    {
        var features = new[] {
            new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 },
            new[] { 4.0 }, new[] { 5.0 }, new[] { 100.0 }
        };
        var t = new TurbulenceIndex(features, window: 5);
        var r = t.Compute().Values;
        Assert.Single(r);
        Assert.Equal(3763.6, r[0], precision: 1);
    }

    // ── Branch 9 — Singular covariance → regularization prevents NaN ──
    // Feature 1 is always 0; covariance matrix has a zero eigenvalue. λI fixes it.
    [Fact]
    public void Compute_SingularCovariance_NoNaN()
    {
        var features = new[] {
            new[] { 1.0, 0.0 }, new[] { 2.0, 0.0 }, new[] { 3.0, 0.0 },
            new[] { 4.0, 0.0 }, new[] { 5.0, 0.0 }
        };
        var t = new TurbulenceIndex(features, window: 3, regularization: 1e-3);
        var r = t.Compute().Values;
        Assert.All(r, v => Assert.False(double.IsNaN(v)));
    }

    // ── Branch 10 — Normal multi-bar path: bivariate, hand-derived reference ──
    //
    // Python reference:
    //   import numpy as np
    //   X = np.array([[0,0],[1,0],[0,1],[1,1],[3,3]], dtype=float)
    //   H = X[:4]; mu = H.mean(axis=0); cov = np.cov(H.T, bias=False) + 1e-6*np.eye(2)
    //   diff = X[4] - mu
    //   ti = diff @ np.linalg.inv(cov) @ diff
    //   # mu = [0.5, 0.5]; cov = [[0.333, 0], [0, 0.333]]; inv ≈ [[3, 0],[0, 3]]
    //   # ti = 2.5^2 * 3 + 2.5^2 * 3 = 37.5 (λ barely shifts it)
    [Fact]
    public void Compute_BivariateKnown_MatchesReference()
    {
        var features = new[] {
            new[] { 0.0, 0.0 }, new[] { 1.0, 0.0 }, new[] { 0.0, 1.0 },
            new[] { 1.0, 1.0 }, new[] { 3.0, 3.0 }
        };
        var t = new TurbulenceIndex(features, window: 4);
        var r = t.Compute().Values;
        Assert.Single(r);
        Assert.Equal(37.5, r[0], precision: 3);
    }

    // ── Apply / label / styling ──
    [Fact]
    public void Apply_AddsLineSeries()
    {
        var axes = new Axes();
        var features = new[] {
            new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 }, new[] { 4.0 }
        };
        new TurbulenceIndex(features, window: 3).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsExpectedLabel()
    {
        var axes = new Axes();
        var features = new[] {
            new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 }, new[] { 4.0 }
        };
        new TurbulenceIndex(features, window: 3).Apply(axes);
        Assert.Equal("Turb(3)", axes.Series[0].Label);
    }

    [Fact]
    public void DefaultWindow_Is252()
    {
        var features = new double[3][];
        for (int i = 0; i < 3; i++) features[i] = [100.0];
        var t = new TurbulenceIndex(features);
        Assert.Equal("Turb(252)", t.Label);
    }

    // ── Type system ──
    [Fact]
    public void InheritsMultivariateIndicator()
    {
        var features = new[] { new[] { 1.0 }, new[] { 2.0 } };
        var t = new TurbulenceIndex(features, window: 2);
        Assert.IsAssignableFrom<MultivariateIndicator<SignalResult>>(t);
        Assert.IsAssignableFrom<IIndicator>(t);
    }
}

/// <summary>Verifies <see cref="TurbulenceIndex.InvertRegularized"/> — the LU-based
/// regularized matrix inverse helper used internally. Exposed via InternalsVisibleTo.</summary>
public class TurbulenceMatrixInversionTests
{
    [Fact]
    public void InvertRegularized_Identity_ReturnsIdentity()
    {
        var m = new double[,] { { 1, 0 }, { 0, 1 } };
        var inv = TurbulenceIndex.InvertRegularized(m, 0);
        Assert.Equal(1.0, inv[0, 0], precision: 12);
        Assert.Equal(0.0, inv[0, 1], precision: 12);
        Assert.Equal(0.0, inv[1, 0], precision: 12);
        Assert.Equal(1.0, inv[1, 1], precision: 12);
    }

    [Fact]
    public void InvertRegularized_KnownTwoByTwo_MatchesAnalyticInverse()
    {
        // M = [[4, 3], [6, 3]] → det = 12 - 18 = -6 → inv = (1/-6)[[3, -3], [-6, 4]]
        //                                               = [[-0.5, 0.5], [1, -2/3]]
        var m = new double[,] { { 4, 3 }, { 6, 3 } };
        var inv = TurbulenceIndex.InvertRegularized(m, 0);
        Assert.Equal(-0.5, inv[0, 0], precision: 10);
        Assert.Equal(0.5, inv[0, 1], precision: 10);
        Assert.Equal(1.0, inv[1, 0], precision: 10);
        Assert.Equal(-2.0 / 3.0, inv[1, 1], precision: 10);
    }

    [Fact]
    public void InvertRegularized_ThreeByThree_RoundTripProducesIdentity()
    {
        var m = new double[,] { { 2, 1, 0 }, { 1, 2, 1 }, { 0, 1, 2 } };
        var inv = TurbulenceIndex.InvertRegularized(m, 0);

        // Verify m · inv == I.
        int n = 3;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                double sum = 0;
                for (int k = 0; k < n; k++) sum += m[i, k] * inv[k, j];
                double expected = i == j ? 1.0 : 0.0;
                Assert.Equal(expected, sum, precision: 10);
            }
    }

    [Fact]
    public void InvertRegularized_SingularMatrix_WithRegularization_DoesNotThrow()
    {
        // m is singular (both rows identical). With λ > 0, m + λI is invertible.
        var m = new double[,] { { 1, 1 }, { 1, 1 } };
        var inv = TurbulenceIndex.InvertRegularized(m, 0.1);
        Assert.False(double.IsNaN(inv[0, 0]));
        Assert.False(double.IsInfinity(inv[0, 0]));
    }

    [Fact]
    public void InvertRegularized_AddsLambdaIOnDiagonal()
    {
        // For zero matrix + λI = λI, inverse = (1/λ)I.
        var m = new double[,] { { 0, 0 }, { 0, 0 } };
        var inv = TurbulenceIndex.InvertRegularized(m, 0.5);
        Assert.Equal(2.0, inv[0, 0], precision: 10);
        Assert.Equal(0.0, inv[0, 1], precision: 10);
        Assert.Equal(0.0, inv[1, 0], precision: 10);
        Assert.Equal(2.0, inv[1, 1], precision: 10);
    }
}
