// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Verifies <see cref="NpRandom"/> distribution samplers.</summary>
public class NpRandomTests
{
    [Fact]
    public void Normal_LengthIsN()
        => Assert.Equal(100, new NpRandom(1).Normal(0, 1, 100).Length);

    [Fact]
    public void Uniform_LengthIsN()
        => Assert.Equal(50, new NpRandom(1).Uniform(0, 1, 50).Length);

    [Fact]
    public void Integers_LengthIsN()
        => Assert.Equal(30, new NpRandom(1).Integers(0, 10, 30).Length);

    [Fact]
    public void Lognormal_LengthIsN()
        => Assert.Equal(40, new NpRandom(1).Lognormal(0, 1, 40).Length);

    [Fact]
    public void Uniform_ValuesInRange()
    {
        double[] v = new NpRandom(42).Uniform(2.0, 5.0, 200);
        Assert.All(v, x => { Assert.True(x >= 2.0); Assert.True(x <= 5.0); });
    }

    [Fact]
    public void Integers_ValuesInRange()
    {
        int[] v = new NpRandom(42).Integers(3, 7, 200);
        Assert.All(v, x => { Assert.True(x >= 3); Assert.True(x < 7); });
    }

    [Fact]
    public void Lognormal_AllPositive()
        => Assert.All(new NpRandom(42).Lognormal(0, 1, 100), x => Assert.True(x > 0));

    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        double[] a = new NpRandom(42).Normal(0, 1, 10);
        double[] b = new NpRandom(42).Normal(0, 1, 10);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentSeed_ProducesDifferentSequence()
    {
        double[] a = new NpRandom(1).Normal(0, 1, 10);
        double[] b = new NpRandom(2).Normal(0, 1, 10);
        Assert.False(a.SequenceEqual(b));
    }

    [Fact]
    public void Normal_Mean_ApproximatelyMu()
    {
        double[] v = new NpRandom(0).Normal(5.0, 1.0, 10_000);
        double mean = v.Average();
        Assert.Equal(5.0, mean, 0.1);
    }

    [Fact]
    public void Normal_Std_ApproximatelySigma()
    {
        double[] v = new NpRandom(0).Normal(0.0, 3.0, 10_000);
        double mean = v.Average();
        double std = Math.Sqrt(v.Select(x => (x - mean) * (x - mean)).Average());
        Assert.Equal(3.0, std, 0.1);
    }
}
