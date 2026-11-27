// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>The power of ten at or below, at or above and nearest a value (<see cref="DecadeExtensions"/>): the one place the decade
/// rule is written, which the tick ladders, the log and symmetric-log locators and the 3-D axis read.</summary>
public class DecadeExtensionsTests
{
    [Theory]
    [InlineData(0.034, 0.01, 0.1)]
    [InlineData(1.0, 1.0, 1.0)]
    [InlineData(250.0, 100.0, 1000.0)]
    [InlineData(1e-7, 1e-7, 1e-7)]
    public void APositiveValue_LiesBetweenTheDecadesAtOrBelowAndAtOrAbove(double value, double below, double above)
    {
        Assert.Equal(below, value.DecadeAtOrBelow());
        Assert.Equal(above, value.DecadeAtOrAbove());
    }

    [Theory]
    [InlineData(0.03, 0.01)]
    [InlineData(0.034, 0.1)]
    [InlineData(316.0, 100.0)]
    [InlineData(317.0, 1000.0)]
    public void TheNearestDecade_IsTheOneTheLogarithmRoundsTo(double value, double nearest)
    {
        Assert.Equal(nearest, value.NearestDecade());
    }

    /// <summary>The exponents of those decades, which the offset text, the order of magnitude of a range's labels and the decades a
    /// logit axis spans read: each decade is ten raised to its exponent, bit for bit.</summary>
    [Theory]
    [InlineData(0.034, -2.0, -1.0, -1.0)]
    [InlineData(1.0, 0.0, 0.0, 0.0)]
    [InlineData(250.0, 2.0, 3.0, 2.0)]
    [InlineData(317.0, 2.0, 3.0, 3.0)]
    [InlineData(1e-7, -7.0, -7.0, -7.0)]
    public void TheExponentOfADecade_IsTheRoundedLogarithm_AndTheDecadeIsTenToIt(double value, double below, double above, double nearest)
    {
        Assert.Equal(below, value.DecadeExponentAtOrBelow());
        Assert.Equal(above, value.DecadeExponentAtOrAbove());
        Assert.Equal(nearest, value.NearestDecadeExponent());
        Assert.Equal(Math.Pow(10, below), value.DecadeAtOrBelow());
        Assert.Equal(Math.Pow(10, above), value.DecadeAtOrAbove());
        Assert.Equal(Math.Pow(10, nearest), value.NearestDecade());
    }

    /// <summary>matplotlib 3.11.2 <c>_decade_less_equal</c> and <c>_decade_greater_equal</c>: a negative value takes the other decade of
    /// its size, negated, and zero is its own decade.</summary>
    [Theory]
    [InlineData(-250.0, -1000.0, -100.0)]
    [InlineData(0.0, 0.0, 0.0)]
    [InlineData(250.0, 100.0, 1000.0)]
    public void ASignedValue_TakesItsDecadesAsMatplotlibsSymmetricLogLocatorDoes(double value, double below, double above)
    {
        Assert.Equal(below, value.SignedDecadeAtOrBelow());
        Assert.Equal(above, value.SignedDecadeAtOrAbove());
    }

    /// <summary>matplotlib 3.11.2 <c>_decade_less</c> and <c>_decade_greater</c> (printed with python): the decade strictly below and
    /// strictly above, so a decade itself steps one further, and a negative value takes the other decade of its size, negated.</summary>
    [Theory]
    [InlineData(3.0, 1.0, 10.0)]
    [InlineData(10.0, 1.0, 100.0)]
    [InlineData(1.0, 0.1, 10.0)]
    [InlineData(0.001, 0.0001, 0.01)]
    [InlineData(250.0, 100.0, 1000.0)]
    [InlineData(-3.0, -10.0, -1.0)]
    [InlineData(-100.0, -1000.0, -10.0)]
    public void AValue_TakesTheDecadesStrictlyAroundIt_AsMatplotlibsLogLocatorDoes(double value, double below, double above)
    {
        Assert.Equal(below, value.DecadeBelow());
        Assert.Equal(above, value.DecadeAbove());
    }

    /// <summary>Each member is the formula it replaced, to the bit, so no tick moves.</summary>
    [Theory]
    [InlineData(0.000123456)]
    [InlineData(0.183)]
    [InlineData(7.77e12)]
    public void EachDecade_IsThePowerOfTheRoundedLogarithm_ToTheBit(double value)
    {
        Assert.Equal(Math.Pow(10, Math.Floor(Math.Log10(value))), value.DecadeAtOrBelow());
        Assert.Equal(Math.Pow(10, Math.Ceiling(Math.Log10(value))), value.DecadeAtOrAbove());
        Assert.Equal(Math.Pow(10, Math.Round(Math.Log10(value))), value.NearestDecade());
    }
}
