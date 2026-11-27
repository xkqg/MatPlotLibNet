// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The decibels of a spectrum: a factor of ten in the value adds ten decibels to a power and twenty to a magnitude; a
/// value that is not above zero has no logarithm and is a gap.</summary>
public class DecibelExtensionsTests
{
    private const double NaN = double.NaN;

    [Fact]
    public void APower_IsTenTimesItsLogarithmToBaseTen()
    {
        Assert.Equal([0.0, 10.0, 20.0, -10.0], new[] { 1.0, 10.0, 100.0, 0.1 }.ToDecibels(10));
    }

    [Fact]
    public void AMagnitude_IsTwentyTimesItsLogarithmToBaseTen()
    {
        Assert.Equal([0.0, 20.0, -20.0], new[] { 1.0, 10.0, 0.1 }.ToDecibels(20));
    }

    [Fact]
    public void AValueThatIsNotAboveZero_IsAGap_NotMinusInfinity()
    {
        Assert.Equal([NaN, NaN, NaN, NaN], new[] { 0.0, -1.0, NaN, double.NegativeInfinity }.ToDecibels(10));
    }

    [Fact]
    public void NoValues_HaveNoDecibels() => Assert.Empty(Array.Empty<double>().ToDecibels(10));
}
