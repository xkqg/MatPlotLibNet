// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The two questions every distribution function asks of its input before it computes anything: whether a shape (a degrees of
/// freedom, a rate) can exist, and where a probability that reaches either end of its range lands.</summary>
public sealed class DistributionDomainExtensionsTests
{
    [Theory]
    [InlineData(double.NaN, true)]
    [InlineData(0.0, true)]
    [InlineData(-1.0, true)]
    [InlineData(double.NegativeInfinity, true)]
    [InlineData(1e-300, false)]
    [InlineData(2.5, false)]
    [InlineData(double.PositiveInfinity, false)]
    public void AShapeThatIsNotAboveZero_IsUndefined(double shape, bool undefined)
    {
        Assert.Equal(undefined, shape.IsUndefinedShape());
    }

    [Theory]
    [InlineData(0.0, -1.0)]
    [InlineData(-3.0, -1.0)]
    [InlineData(1.0, 1.0)]
    [InlineData(2.0, 1.0)]
    public void AProbabilityAtAnEnd_LandsOnThatEnd(double probability, double expected)
    {
        Assert.Equal(expected, probability.Saturated(atZero: -1.0, atOne: 1.0));
    }

    [Fact]
    public void AMissingProbability_IsMissing()
    {
        Assert.True(double.IsNaN(double.NaN.Saturated(atZero: -1.0, atOne: 1.0)!.Value));
    }

    /// <summary>A missing probability gives a missing quantile in every distribution; it neither throws nor becomes an infinity.</summary>
    public static TheoryData<Func<double, double>> TheQuantiles => new()
    {
        Exponential.Quantile,
        StandardNormal.Quantile,
    };

    [Theory]
    [MemberData(nameof(TheQuantiles))]
    public void AMissingProbability_IsAMissingQuantile(Func<double, double> quantile) => Assert.True(double.IsNaN(quantile(double.NaN)));

    [Theory]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(1e-300)]
    public void AProbabilityInsideTheRange_HasNoEnd(double probability)
    {
        Assert.Null(probability.Saturated(atZero: -1.0, atOne: 1.0));
    }
}
