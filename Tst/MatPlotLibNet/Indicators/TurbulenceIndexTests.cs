// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="TurbulenceIndex"/>. Its arithmetic is held here against reference values; what it keeps with every
/// indicator (its lines, its pane, its first value, the arguments it refuses) is held by <see cref="IndicatorContractTests"/>.</summary>
public class TurbulenceIndexTests
{
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
        r.AssertAllFinite();
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
}

