// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

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
        inv[0, 0].AssertFinite();
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
