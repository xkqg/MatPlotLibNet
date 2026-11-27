// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The one place a mean, a variance and a standard deviation of a run of values are worked out: about the mean, divided by
/// the count less the degrees of freedom the caller names (numpy's <c>ddof</c>: 0 for a population, 1 for a sample). Each answer is
/// held against the textbook sum, written out here in full.</summary>
public sealed class MomentsExtensionsTests
{
    private static readonly double[] Window = [2.0, 4.0, 4.0, 4.0, 5.0, 5.0, 7.0, 9.0];

    private static double Textbook(ReadOnlySpan<double> values, int ddof)
    {
        double mean = 0;
        foreach (double value in values) mean += value;
        mean /= values.Length;
        double squares = 0;
        foreach (double value in values) squares += (value - mean) * (value - mean);
        return squares / (values.Length - ddof);
    }

    [Fact]
    public void TheMean_IsTheSumOverTheCount()
    {
        Assert.Equal(5.0, Window.AsSpan().Mean());
    }

    [Fact]
    public void TheMeanOfNothing_IsNotANumber()
    {
        Assert.True(double.IsNaN(ReadOnlySpan<double>.Empty.Mean()));
    }

    [Fact]
    public void ThePopulationVariance_DividesByTheCount()
    {
        Assert.Equal(4.0, Window.AsSpan().Variance());
        Assert.Equal(2.0, Window.AsSpan().StdDev());
    }

    [Fact]
    public void TheSampleVariance_DividesByTheCountLessOne()
    {
        Assert.Equal(32.0 / 7.0, Window.AsSpan().Variance(ddof: 1), 12);
        Assert.Equal(Math.Sqrt(32.0 / 7.0), Window.AsSpan().StdDev(ddof: 1), 12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TheVariance_IsTheTextbookSumOverTheCountLessTheDegreesOfFreedom(int ddof)
    {
        double[] values = [0.1, 0.7, -3.2, 12.5, 8.8, 0.0, 4.4];

        Assert.Equal(Textbook(values, ddof), values.AsSpan().Variance(ddof), 12);
        Assert.Equal(Math.Sqrt(Textbook(values, ddof)), values.AsSpan().StdDev(ddof), 12);
    }

    [Fact]
    public void AMeanTheCallerAlreadyHas_IsUsedAsGiven()
    {
        // A mean worked out another way (a running sum) is the centre of the sum of squares, as given: the true mean is 5, so a
        // centre of 4 adds (5 - 4)^2 = 1 to the population variance.
        Assert.Equal(Textbook(Window, 0) + 1.0, Window.AsSpan().Variance(about: 4.0, ddof: 0), 12);
        Assert.Equal(Math.Sqrt(Textbook(Window, 0) + 1.0), Window.AsSpan().StdDev(about: 4.0), 12);
    }

    /// <summary>A short run is summed in order, value by value, which is faster than the SIMD form below its fixed cost and is the
    /// arithmetic every indicator of a 20-bar window computed before the owner existed: held bit for bit.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(20)]
    [InlineData(31)]
    public void AShortRun_IsSummedInOrder_BitForBit(int count)
    {
        var random = new Random(count);
        double[] values = [.. Enumerable.Range(0, count).Select(_ => (random.NextDouble() - 0.3) * 1000)];
        double sum = 0;
        for (int i = 0; i < values.Length; i++) sum += values[i];
        double mean = sum / values.Length;
        double squares = 0;
        for (int i = 0; i < values.Length; i++)
        {
            double d = values[i] - mean;
            squares += d * d;
        }

        Assert.Equal(BitConverter.DoubleToInt64Bits(mean), BitConverter.DoubleToInt64Bits(values.AsSpan().Mean()));
        Assert.Equal(BitConverter.DoubleToInt64Bits(squares / values.Length), BitConverter.DoubleToInt64Bits(values.AsSpan().Variance()));
        Assert.Equal(BitConverter.DoubleToInt64Bits(squares / (values.Length - 1)), BitConverter.DoubleToInt64Bits(values.AsSpan().Variance(mean, ddof: 1)));
        Assert.Equal(BitConverter.DoubleToInt64Bits(Math.Sqrt(squares / values.Length)), BitConverter.DoubleToInt64Bits(values.AsSpan().StdDev(mean)));
    }

    /// <summary>The two kernels meet at 32 values: a run either side of it, and on it, is the textbook sum.</summary>
    [Theory]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(35)]
    [InlineData(37)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(100)]
    [InlineData(1001)]
    public void TheRunsEitherSideOfTheKernelsMeetingPoint_AreTheTextbookSum(int count)
    {
        var random = new Random(count);
        double[] values = [.. Enumerable.Range(0, count).Select(_ => (random.NextDouble() - 0.3) * 1000)];

        Assert.Equal(Textbook(values, 0), values.AsSpan().Variance(), 8);
        Assert.Equal(Textbook(values, 1), values.AsSpan().Variance(ddof: 1), 8);
        Assert.Equal(Textbook(values, 1), values.AsSpan().Variance(values.AsSpan().Mean(), ddof: 1), 8);
    }

    [Fact]
    public void ALongRun_IsTheTextbookSum()
    {
        var random = new Random(7);
        double[] values = [.. Enumerable.Range(0, 5000).Select(_ => random.NextDouble() * 100)];

        Assert.Equal(Textbook(values, 1), values.AsSpan().Variance(ddof: 1), 8);
    }

    /// <summary>A long run is summed in one pass over the values: no copy of them is centred in a buffer, on the stack or from the pool.</summary>
    [Fact]
    public void ALongRun_AllocatesNothing()
    {
        double[] values = [.. Enumerable.Range(0, 5000).Select(i => i * 0.5)];
        double mean = values.AsSpan().Mean();
        _ = values.AsSpan().Variance(mean);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) _ = values.AsSpan().Variance(mean, ddof: 1);

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void AMissingValueInALongRun_MakesTheSpreadMissing()
    {
        double[] values = [.. Enumerable.Range(0, 64).Select(i => (double)i)];
        values[40] = double.NaN;

        Assert.True(double.IsNaN(values.AsSpan().Variance(about: 10.0)));
    }

    /// <summary>A mean the caller gives is the centre of a long run as it is of a short one: the true mean is 31.5, so a centre of 30 adds
    /// the square of 1.5 to the population variance.</summary>
    [Fact]
    public void AMeanTheCallerGives_IsTheCentreOfALongRunToo()
    {
        double[] values = [.. Enumerable.Range(0, 64).Select(i => (double)i)];

        Assert.Equal(Textbook(values, 0) + 1.5 * 1.5, values.AsSpan().Variance(about: 30.0), 9);
    }

    [Fact]
    public void ARunOfEqualValues_HasNoSpread()
    {
        double[] values = [3.5, 3.5, 3.5, 3.5];

        Assert.Equal(0.0, values.AsSpan().StdDev());
    }

    [Fact]
    public void OneValue_HasNoSampleVariance_AndNoPopulationSpread()
    {
        double[] one = [4.0];

        Assert.True(double.IsNaN(one.AsSpan().Variance(ddof: 1)));
        Assert.Equal(0.0, one.AsSpan().Variance());
    }

    [Fact]
    public void TheVecStandardDeviationAndMean_AreTheOwnersPopulationOnes()
    {
        var vec = new Vec([1.5, 2.5, 9.0, -4.0, 0.25]);

        Assert.Equal(vec.AsSpan().StdDev(), vec.Std());
        Assert.Equal(vec.AsSpan().Mean(), vec.Mean());
    }
}
