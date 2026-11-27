// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="NpStats"/> statistical utility methods.</summary>
public class NpStatsTests
{
    private const double Tol = 1e-9;

    // ---- Diff ----------------------------------------------------------------

    [Fact]
    public void Diff_N1_ReturnsFirstDifferences()
    {
        Vec v = new double[] { 1, 3, 6, 10 };
        Vec d = NpStats.Diff(v);
        Assert.Equal([2.0, 3.0, 4.0], d.Data);
    }

    [Fact]
    public void Diff_N2_ReturnsSecondDifferences()
    {
        Vec v = new double[] { 1, 3, 6, 10 };
        Vec d = NpStats.Diff(v, 2);
        Assert.Equal([1.0, 1.0], d.Data);
    }

    [Fact]
    public void Diff_N0_ReturnsSameVec()
    {
        Vec v = new double[] { 1, 2, 3 };
        Vec d = NpStats.Diff(v, 0);
        Assert.Equal(v.Data, d.Data);
    }

    // ---- Median --------------------------------------------------------------

    [Fact]
    public void Median_OddLength_ReturnsMiddleElement()
    {
        Vec v = new double[] { 3, 1, 4, 1, 5 };
        Assert.Equal(3.0, NpStats.Median(v), Tol);
    }

    [Fact]
    public void Median_EvenLength_ReturnsInterpolated()
    {
        Vec v = new double[] { 1, 2, 3, 4 };
        Assert.Equal(2.5, NpStats.Median(v), Tol);
    }

    [Fact]
    public void Median_SingleElement_ReturnsThatElement()
    {
        Vec v = new double[] { 42 };
        Assert.Equal(42.0, NpStats.Median(v), Tol);
    }

    // ---- Histogram -----------------------------------------------------------

    [Fact]
    public void Histogram_KnownData_CorrectCounts()
    {
        Vec v = new double[] { 0.5, 1.5, 2.5, 3.5, 4.5 };
        HistogramResult h = NpStats.Histogram(v, 5);
        Assert.All(h.Counts, c => Assert.Equal(1.0, c, Tol));
    }

    [Fact]
    public void Histogram_EdgesLength_IsBinsPlusOne()
    {
        Vec v = new double[] { 1, 2, 3, 4, 5 };
        HistogramResult h = NpStats.Histogram(v, 4);
        Assert.Equal(5, h.Edges.Length);
    }

    [Fact]
    public void Histogram_AllCountsSumToN()
    {
        Vec v = new double[] { 1, 2, 3, 4, 5, 6, 7 };
        HistogramResult h = NpStats.Histogram(v, 3);
        Assert.Equal(7.0, h.Counts.Sum(), Tol);
    }

    // ---- Argsort -------------------------------------------------------------

    [Fact]
    public void Argsort_ReturnsCorrectIndices()
    {
        Vec v = new double[] { 30, 10, 20 };
        int[] idx = NpStats.Argsort(v);
        Assert.Equal([1, 2, 0], idx);
    }

    [Fact]
    public void Argsort_AlreadySorted_ReturnIdentity()
    {
        Vec v = new double[] { 1, 2, 3, 4 };
        int[] idx = NpStats.Argsort(v);
        Assert.Equal([0, 1, 2, 3], idx);
    }

    // ---- Unique --------------------------------------------------------------

    public static TheoryData<Func<UniqueResult, IEnumerable<double>>, double[]> WhatUniqueReports => new()
    {
        { r => r.Values, [1.0, 2.0, 3.0] },
        { r => r.Counts.Select(count => (double)count), [2.0, 1.0, 3.0] },
    };

    [Theory]
    [MemberData(nameof(WhatUniqueReports))]
    public void Unique_ReportsTheDistinctValuesAndTheirCounts(Func<UniqueResult, IEnumerable<double>> report, double[] expected)
    {
        Vec v = new double[] { 3, 1, 2, 1, 3, 3 };
        UniqueResult r = NpStats.Unique(v);
        Assert.Equal(expected, report(r));
    }

    // ---- Cov -----------------------------------------------------------------

    [Fact]
    public void Cov_2Variables_MatchesManualCalculation()
    {
        // x=[1,2,3], y=[2,4,6] → perfect linear, cov(x,x)=1, cov(x,y)=2, cov(y,y)=4
        double[][] cols = [[1, 2, 3], [2, 4, 6]];
        Mat c = NpStats.Cov(cols);
        Assert.Equal(2, c.Rows);
        Assert.Equal(2, c.Cols);
        Assert.Equal(1.0, c[0, 0], Tol);
        Assert.Equal(2.0, c[0, 1], Tol);
        Assert.Equal(2.0, c[1, 0], Tol);
        Assert.Equal(4.0, c[1, 1], Tol);
    }

    // ---- Corrcoef ------------------------------------------------------------

    /// <summary>Two perfectly correlated columns are one off the diagonal, and any two columns are one on it.</summary>
    [Theory]
    [InlineData(2.0, 4.0, 6.0, 0, 1, 1, 0)]
    [InlineData(3.0, 1.0, 2.0, 0, 0, 1, 1)]
    public void Corrcoef_IsOneWhereTheColumnsAreTheSame(double y0, double y1, double y2, int row, int column, int otherRow, int otherColumn)
    {
        Mat r = NpStats.Corrcoef([[1, 2, 3], [y0, y1, y2]]);
        Assert.Equal(1.0, r[row, column], Tol);
        Assert.Equal(1.0, r[otherRow, otherColumn], Tol);
    }

    [Fact]
    public void Histogram_AllEqualValues_FallInTheMiddleOfARangeWidenedByAHalf()
    {
        // numpy widens a range of no width by a half either side: np.histogram([5, 5, 5, 5], bins=4) is [0, 0, 4, 0] over the
        // edges [4.5, 4.75, 5, 5.25, 5.5].
        Vec v = new double[] { 5, 5, 5, 5 };
        HistogramResult h = NpStats.Histogram(v, 4);
        Assert.Equal([0.0, 0, 4, 0], h.Counts);
        Assert.Equal([4.5, 4.75, 5, 5.25, 5.5], h.Edges);
    }

    /// <summary>A zero-variance column makes std[i]=0 — covers the false arm of
    /// the `std[i] &gt; 0 &amp;&amp; std[j] &gt; 0` guard in Corrcoef. Off-diagonal entries
    /// must be 0; diagonal entries must remain 1.</summary>
    [Fact]
    public void Corrcoef_ZeroVarianceColumn_OffDiagonalIsZero()
    {
        double[][] cols = [[1, 1, 1], [1, 2, 3]];     // first column has zero variance
        Mat r = NpStats.Corrcoef(cols);
        Assert.Equal(0.0, r[0, 1], Tol);
        Assert.Equal(0.0, r[1, 0], Tol);
        Assert.Equal(1.0, r[0, 0], Tol);
        Assert.Equal(1.0, r[1, 1], Tol);
    }
}
