// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="VectorMath"/> SIMD kernel behavior.</summary>
public class VectorMathTests
{
    // --- TensorPrimitives wrappers ---

    [Fact]
    public void Add_ElementWise_ReturnsCorrectResult()
    {
        double[] x = [1.0, 2.0, 3.0];
        double[] y = [4.0, 5.0, 6.0];
        double[] dst = new double[3];
        VectorMath.Add(x, y, dst);
        Assert.Equal([5.0, 7.0, 9.0], dst);
    }

    [Fact]
    public void Add_Scalar_ReturnsCorrectResult()
    {
        double[] x = [1.0, 2.0, 3.0];
        double[] dst = new double[3];
        VectorMath.Add(x, 10.0, dst);
        Assert.Equal([11.0, 12.0, 13.0], dst);
    }

    [Fact]
    public void Subtract_ReturnsCorrectResult()
    {
        double[] x = [5.0, 7.0, 9.0];
        double[] y = [1.0, 2.0, 3.0];
        double[] dst = new double[3];
        VectorMath.Subtract(x, y, dst);
        Assert.Equal([4.0, 5.0, 6.0], dst);
    }

    [Fact]
    public void Multiply_ElementWise_ReturnsCorrectResult()
    {
        double[] x = [2.0, 3.0, 4.0];
        double[] y = [5.0, 6.0, 7.0];
        double[] dst = new double[3];
        VectorMath.Multiply(x, y, dst);
        Assert.Equal([10.0, 18.0, 28.0], dst);
    }

    [Fact]
    public void Multiply_Scalar_ReturnsCorrectResult()
    {
        double[] x = [1.0, 2.0, 3.0];
        double[] dst = new double[3];
        VectorMath.Multiply(x, 3.0, dst);
        Assert.Equal([3.0, 6.0, 9.0], dst);
    }

    [Fact]
    public void Divide_Scalar_ReturnsCorrectResult()
    {
        double[] x = [6.0, 9.0, 12.0];
        double[] dst = new double[3];
        VectorMath.Divide(x, 3.0, dst);
        Assert.Equal([2.0, 3.0, 4.0], dst);
    }

    [Fact]
    public void Sum_ReturnsCorrectResult()
    {
        double[] x = [1.0, 2.0, 3.0, 4.0];
        Assert.Equal(10.0, VectorMath.Sum(x));
    }

    [Fact]
    public void Min_ReturnsCorrectResult()
    {
        double[] x = [3.0, 1.0, 4.0, 1.5, 2.0];
        Assert.Equal(1.0, VectorMath.Min(x));
    }

    [Fact]
    public void Max_ReturnsCorrectResult()
    {
        double[] x = [3.0, 1.0, 4.0, 1.5, 2.0];
        Assert.Equal(4.0, VectorMath.Max(x));
    }

    [Fact]
    public void Abs_ReturnsCorrectResult()
    {
        double[] x = [-1.0, 2.0, -3.0];
        double[] dst = new double[3];
        VectorMath.Abs(x, dst);
        Assert.Equal([1.0, 2.0, 3.0], dst);
    }

    [Fact]
    public void Negate_ReturnsCorrectResult()
    {
        double[] x = [1.0, -2.0, 3.0];
        double[] dst = new double[3];
        VectorMath.Negate(x, dst);
        Assert.Equal([-1.0, 2.0, -3.0], dst);
    }

    // --- Domain-specific ---

    [Fact]
    public void Linspace_DefaultStep_ReturnsIntegerSequence()
    {
        double[] result = VectorMath.Linspace(5, 0.0);
        Assert.Equal([0.0, 1.0, 2.0, 3.0, 4.0], result);
    }

    [Fact]
    public void Linspace_WithStep_ReturnsCorrectSequence()
    {
        double[] result = VectorMath.Linspace(4, 2.0, 0.5);
        Assert.Equal([2.0, 2.5, 3.0, 3.5], result);
    }

    [Fact]
    public void RollingMean_ReturnsCorrectValues()
    {
        double[] x = [10.0, 20.0, 30.0, 40.0, 50.0];
        double[] result = VectorMath.RollingMean(x, 3);
        Assert.Equal(3, result.Length);
        Assert.Equal(20.0, result[0]);
        Assert.Equal(30.0, result[1]);
        Assert.Equal(40.0, result[2]);
    }

    [Fact]
    public void RollingMean_PeriodEqualsLength_ReturnsSingleElement()
    {
        double[] x = [10.0, 20.0, 30.0];
        double[] result = VectorMath.RollingMean(x, 3);
        Assert.Single(result);
        Assert.Equal(20.0, result[0]);
    }

    [Fact]
    public void RollingMin_ReturnsCorrectValues_MonotoneDeque()
    {
        double[] x = [5.0, 3.0, 4.0, 1.0, 6.0];
        double[] dst = new double[5];
        VectorMath.RollingMin(x, 3, dst);
        Assert.Equal(3.0, dst[2]); // min([5,3,4])
        Assert.Equal(1.0, dst[3]); // min([3,4,1])
        Assert.Equal(1.0, dst[4]); // min([4,1,6])
    }

    [Fact]
    public void RollingMax_ReturnsCorrectValues_MonotoneDeque()
    {
        double[] x = [1.0, 5.0, 3.0, 4.0, 2.0];
        double[] dst = new double[5];
        VectorMath.RollingMax(x, 3, dst);
        Assert.Equal(5.0, dst[2]); // max([1,5,3])
        Assert.Equal(5.0, dst[3]); // max([5,3,4])
        Assert.Equal(4.0, dst[4]); // max([3,4,2])
    }

    [Fact]
    public void RollingStdDev_ReturnsCorrectValues()
    {
        // [10,10,20,20]: window mean=15, population stddev=5 exactly
        double[] x = [10.0, 10.0, 20.0, 20.0];
        double[] means = VectorMath.RollingMean(x, 4);
        double[] dst = new double[1];
        VectorMath.RollingStdDev(x, 4, means, dst);
        Assert.Equal(5.0, dst[0], 1e-10);
    }

    [Fact]
    public void CumulativeSum_ReturnsRunningTotals()
    {
        double[] x = [1.0, 2.0, 3.0, 4.0];
        double[] dst = new double[4];
        VectorMath.CumulativeSum(x, dst);
        Assert.Equal([1.0, 3.0, 6.0, 10.0], dst);
    }

    [Fact]
    public void StandardDeviation_ReturnsPopulationStdDev()
    {
        // Classic example: population stddev of [2,4,4,4,5,5,7,9] = 2.0
        double[] x = [2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0];
        double std = VectorMath.StandardDeviation(x);
        Assert.Equal(2.0, std, 1e-10);
    }

    [Fact]
    public void MultiplyAdd_ReturnsScaledAndShiftedValues()
    {
        double[] x = [1.0, 2.0, 3.0];
        double[] dst = new double[3];
        VectorMath.MultiplyAdd(x, 2.0, 10.0, dst);
        Assert.Equal([12.0, 14.0, 16.0], dst);
    }

    [Fact]
    public void SplitPositiveNegative_SeparatesCorrectly()
    {
        double[] x = [1.0, -2.0, 3.0, -4.0, 0.0];
        double[] pos = new double[5];
        double[] neg = new double[5];
        VectorMath.SplitPositiveNegative(x, pos, neg);
        Assert.Equal([1.0, 0.0, 3.0, 0.0, 0.0], pos);
        Assert.Equal([0.0, -2.0, 0.0, -4.0, 0.0], neg);
    }
}
