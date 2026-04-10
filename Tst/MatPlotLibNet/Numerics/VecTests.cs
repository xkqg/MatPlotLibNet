// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="Vec"/> LINQ-style fluent record struct behavior.</summary>
public class VecTests
{
    // --- Operators (SIMD-accelerated) ---

    [Fact]
    public void Add_TwoVecs_ReturnsElementwiseSum()
    {
        Vec a = new double[] { 1.0, 2.0, 3.0 };
        Vec b = new double[] { 4.0, 5.0, 6.0 };
        double[] result = a + b;
        Assert.Equal([5.0, 7.0, 9.0], result);
    }

    [Fact]
    public void Subtract_TwoVecs_ReturnsElementwiseDifference()
    {
        Vec a = new double[] { 5.0, 7.0, 9.0 };
        Vec b = new double[] { 1.0, 2.0, 3.0 };
        double[] result = a - b;
        Assert.Equal([4.0, 5.0, 6.0], result);
    }

    [Fact]
    public void Multiply_TwoVecs_ReturnsElementwiseProduct()
    {
        Vec a = new double[] { 2.0, 3.0, 4.0 };
        Vec b = new double[] { 5.0, 6.0, 7.0 };
        double[] result = a * b;
        Assert.Equal([10.0, 18.0, 28.0], result);
    }

    [Fact]
    public void Multiply_VecByScalar_ReturnsScaledVec()
    {
        Vec a = new double[] { 1.0, 2.0, 3.0 };
        double[] result = a * 3.0;
        Assert.Equal([3.0, 6.0, 9.0], result);
    }

    [Fact]
    public void Divide_VecByScalar_ReturnsDividedVec()
    {
        Vec a = new double[] { 6.0, 9.0, 12.0 };
        double[] result = a / 3.0;
        Assert.Equal([2.0, 3.0, 4.0], result);
    }

    [Fact]
    public void Negate_UnaryMinus_ReturnsNegatedVec()
    {
        Vec a = new double[] { 1.0, -2.0, 3.0 };
        double[] result = -a;
        Assert.Equal([-1.0, 2.0, -3.0], result);
    }

    // --- LINQ-style (scalar loops) ---

    [Fact]
    public void Select_TransformsElements()
    {
        Vec a = new double[] { 1.0, 2.0, 3.0 };
        double[] result = a.Select(x => x * x);
        Assert.Equal([1.0, 4.0, 9.0], result);
    }

    [Fact]
    public void Where_FiltersPositiveElements()
    {
        Vec a = new double[] { 1.0, -2.0, 3.0, -4.0 };
        double[] result = a.Where(x => x > 0);
        Assert.Equal([1.0, 3.0], result);
    }

    [Fact]
    public void Zip_CombinesTwoVecsWithSelector()
    {
        Vec a = new double[] { 1.0, 2.0, 3.0 };
        Vec b = new double[] { 4.0, 5.0, 6.0 };
        double[] result = a.Zip(b, (x, y) => x + y);
        Assert.Equal([5.0, 7.0, 9.0], result);
    }

    // --- Reductions (SIMD-accelerated) ---

    [Fact]
    public void Sum_ReturnsTotalSum()
    {
        Vec a = new double[] { 1.0, 2.0, 3.0, 4.0 };
        Assert.Equal(10.0, a.Sum());
    }

    [Fact]
    public void Min_ReturnsMinimum()
    {
        Vec a = new double[] { 3.0, 1.0, 4.0, 1.5 };
        Assert.Equal(1.0, a.Min());
    }

    [Fact]
    public void Max_ReturnsMaximum()
    {
        Vec a = new double[] { 3.0, 1.0, 4.0, 1.5 };
        Assert.Equal(4.0, a.Max());
    }

    [Fact]
    public void Mean_ReturnsArithmeticAverage()
    {
        Vec a = new double[] { 1.0, 2.0, 3.0, 4.0 };
        Assert.Equal(2.5, a.Mean());
    }

    [Fact]
    public void Std_ReturnsPopulationStdDev()
    {
        Vec a = new double[] { 2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0 };
        Assert.Equal(2.0, a.Std(), 1e-10);
    }

    [Fact]
    public void Aggregate_FoldsWithReducer()
    {
        Vec a = new double[] { 1.0, 2.0, 3.0, 4.0 };
        double result = a.Aggregate((acc, x) => acc + x, 0.0);
        Assert.Equal(10.0, result);
    }

    // --- Accessors ---

    [Fact]
    public void Indexer_ReturnsCorrectElement()
    {
        Vec a = new double[] { 10.0, 20.0, 30.0 };
        Assert.Equal(20.0, a[1]);
    }

    [Fact]
    public void Length_ReturnsDataLength()
    {
        Vec a = new double[] { 1.0, 2.0, 3.0 };
        Assert.Equal(3, a.Length);
    }

    [Fact]
    public void Slice_ReturnsSubVecByValue()
    {
        Vec a = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        double[] slice = a.Slice(1, 3);
        Assert.Equal([2.0, 3.0, 4.0], slice);
    }

    [Fact]
    public void AsSpan_ReturnsReadOnlySpanOverData()
    {
        Vec a = new double[] { 1.0, 2.0, 3.0 };
        var span = a.AsSpan();
        Assert.Equal(3, span.Length);
        Assert.Equal(2.0, span[1]);
    }

    // --- Conversions ---

    [Fact]
    public void ImplicitFromDoubleArray_WrapsArray()
    {
        double[] data = [1.0, 2.0, 3.0];
        Vec v = data;
        Assert.Equal(3, v.Length);
        Assert.Equal(2.0, v[1]);
    }

    [Fact]
    public void ImplicitToDoubleArray_UnwrapsData()
    {
        Vec v = new Vec(new double[] { 1.0, 2.0, 3.0 });
        double[] data = v;
        Assert.Equal(3, data.Length);
        Assert.Equal(2.0, data[1]);
    }

    // --- Value equality (record struct contract) ---

    [Fact]
    public void Equality_SameDataReference_IsEqual()
    {
        double[] arr = [1.0, 2.0, 3.0];
        Vec a = arr;
        Vec b = arr;
        Assert.Equal(a, b);
    }

    // --- Percentile / Quantile (v0.8.0) ---

    [Fact]
    public void Percentile_Median_ReturnsMiddleValue()
    {
        Vec v = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        Assert.Equal(3.0, v.Percentile(50));
    }

    [Fact]
    public void Percentile_Q1Q3_CorrectValues()
    {
        Vec v = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        Assert.Equal(2.0, v.Percentile(25));
        Assert.Equal(4.0, v.Percentile(75));
    }

    [Fact]
    public void Percentile_Boundary_ZeroAndHundred()
    {
        Vec v = new double[] { 10.0, 20.0, 30.0 };
        Assert.Equal(10.0, v.Percentile(0));
        Assert.Equal(30.0, v.Percentile(100));
    }

    [Fact]
    public void Percentile_SingleElement_ReturnsThatElement()
    {
        Vec v = new double[] { 42.0 };
        Assert.Equal(42.0, v.Percentile(50));
    }

    [Fact]
    public void Quantile_EquivalentToPercentile()
    {
        Vec v = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        Assert.Equal(v.Percentile(75), v.Quantile(0.75));
    }
}
