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

    [Fact]
    public void Unique_ReturnsDistinctValues()
    {
        Vec v = new double[] { 3, 1, 2, 1, 3, 3 };
        UniqueResult r = NpStats.Unique(v);
        Assert.Equal([1.0, 2.0, 3.0], r.Values);
    }

    [Fact]
    public void Unique_CountsAreCorrect()
    {
        Vec v = new double[] { 3, 1, 2, 1, 3, 3 };
        UniqueResult r = NpStats.Unique(v);
        Assert.Equal([2, 1, 3], r.Counts);
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

    [Fact]
    public void Corrcoef_PerfectlyCorrelated_ReturnsOne()
    {
        double[][] cols = [[1, 2, 3], [2, 4, 6]];
        Mat r = NpStats.Corrcoef(cols);
        Assert.Equal(1.0, r[0, 1], Tol);
        Assert.Equal(1.0, r[1, 0], Tol);
    }

    [Fact]
    public void Corrcoef_Diagonal_IsAlwaysOne()
    {
        double[][] cols = [[1, 2, 3], [3, 1, 2]];
        Mat r = NpStats.Corrcoef(cols);
        Assert.Equal(1.0, r[0, 0], Tol);
        Assert.Equal(1.0, r[1, 1], Tol);
    }
}
