// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>How a sample becomes a stem-and-leaf display, held to hand-worked vectors: the stem is the digits above the leaf unit and
/// the leaf the digit at it, cut without rounding toward zero; negative values have negative stems and the stem "-0"; an empty stem
/// between two others is a row of no leaves; a repeated value is a repeated leaf; a decimal unit loses no digit to binary error; and
/// a unit is chosen when none is given.</summary>
public class StemAndLeafTests
{
    private static string Describe(StemAndLeafDisplay display) =>
        string.Join(" / ", display.Rows.Select(row => $"{row.Stem}|{string.Join(" ", row.Leaves)}")).Trim();

    [Fact]
    public void TheClassicRows_AreTheTensAsStemsAndTheUnitsAsLeaves()
    {
        double[] sample = [12, 15, 21, 23, 23, 38];

        var display = sample.StemAndLeaf(1);

        Assert.Equal("1|2 5 / 2|1 3 3 / 3|8", Describe(display));
        Assert.Equal(1m, display.LeafUnit);
    }

    [Fact]
    public void ANegativeValue_HasANegativeStem_AndTheNegativeValuesBelowTenUnitsStandOnMinusZero()
    {
        // -15 and -12 are on -1 (the leaves 5 then 2: the values ascend left to right), -3 on -0, 4 on 0, 17 on 1.
        double[] sample = [-15, -12, -3, 4, 17];

        Assert.Equal("-1|5 2 / -0|3 / 0|4 / 1|7", Describe(sample.StemAndLeaf(1)));
    }

    [Fact]
    public void AStemNobodyFallsOn_IsARowOfNoLeaves()
    {
        double[] sample = [11, 12, 41];

        Assert.Equal("1|1 2 / 2| / 3| / 4|1", Describe(sample.StemAndLeaf(1)));
    }

    [Fact]
    public void ARepeatedValue_IsARepeatedLeaf()
    {
        Assert.Equal("2|2 2 5", Describe(new double[] { 22, 25, 22 }.StemAndLeaf(1)));
    }

    [Fact]
    public void ALeafUnitOfATenth_MakesTheWholesTheStems_AndLosesNoDigitToBinaryError()
    {
        // 1.2 / 0.1 is 11.999999999999998 in doubles: a plain truncation would make it a 1 on stem 1, not a 2.
        double[] sample = [1.2, 1.5, 2.1, 2.3];

        var display = sample.StemAndLeaf(0.1);

        Assert.Equal("1|2 5 / 2|1 3", Describe(display));
        Assert.Equal(0.1m, display.LeafUnit);
    }

    [Fact]
    public void ALeafUnitOfTen_PutsTheHundredsOnTheStems()
    {
        double[] sample = [120, 150, 210, 230];

        var display = sample.StemAndLeaf(10);

        Assert.Equal("1|2 5 / 2|1 3", Describe(display));
        Assert.Equal(10m, display.LeafUnit);
    }

    [Fact]
    public void AValue_IsCutTowardZero_NotRounded()
    {
        // 1.29 at a unit of 0.1 is a 2 on the stem 1, and -1.29 a 2 on the stem -1.
        Assert.Equal("-1|2 / -0| / 0| / 1|2", Describe(new[] { -1.29, 1.29 }.StemAndLeaf(0.1)));
    }

    [Fact]
    public void WithoutAUnit_TheFinestPowerOfTenWithinTheBudgetOfRowsIsChosen()
    {
        double[] tens = [12, 15, 21, 23, 23, 38];
        double[] tenths = [1.2, 1.5, 2.1, 2.3];

        Assert.Equal(0, tens.StemAndLeaf(null).Exponent);          // 3 rows at a unit of 1; 27 at 0.1 outnumber the budget of 6
        Assert.Equal(-1, tenths.StemAndLeaf(null).Exponent);       // 2 rows at a unit of 0.1; 12 at 0.01 outnumber the budget of 5
        Assert.Equal("1|2 5 / 2|1 3 3 / 3|8", Describe(tens.StemAndLeaf(null)));
    }

    [Fact]
    public void ASampleOfSmallNumbers_GetsASmallUnit()
    {
        var display = new[] { 0.0021, 0.0034 }.StemAndLeaf(null);

        Assert.Equal(-4, display.Exponent);
        Assert.Equal("2|1 / 3|4", Describe(display));
        Assert.Equal(0.0001m, display.LeafUnit);
    }

    [Fact]
    public void ANumberThatIsNotFinite_IsSkipped()
    {
        Assert.Equal("1|2 / 2|3", Describe(new[] { 12, double.NaN, 23, double.PositiveInfinity }.Select(v => (double)v).ToArray().StemAndLeaf(1)));
    }

    [Fact]
    public void ASampleWithNoFiniteNumber_IsRefusedByName()
    {
        Assert.Equal("sample", Assert.Throws<ArgumentException>(() => Array.Empty<double>().StemAndLeaf(1)).ParamName);
        Assert.Equal("sample", Assert.Throws<ArgumentException>(() => new[] { double.NaN }.StemAndLeaf(null)).ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(2.0)]
    [InlineData(0.25)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ALeafUnitThatIsNotAPowerOfTen_IsRefusedByName(double unit)
    {
        Assert.Equal("leafUnit", Assert.Throws<ArgumentOutOfRangeException>(() => new double[] { 12, 15 }.StemAndLeaf(unit)).ParamName);
    }

    [Fact]
    public void ALeafUnitTooFineForTheData_IsRefusedByName()
    {
        Assert.Equal("leafUnit", Assert.Throws<ArgumentOutOfRangeException>(() => new[] { 1e12 }.StemAndLeaf(1e-9)).ParamName);
    }

    [Fact]
    public void AllTheLeavesAreDigits()
    {
        var display = new double[] { 5, 15, 25, 35, 45, 55, 65, 75, 85, 95, 99 }.StemAndLeaf(1);

        Assert.All(display.Rows.SelectMany(row => row.Leaves), leaf => Assert.InRange(leaf, 0, 9));
        Assert.Equal(11, display.Rows.Sum(row => row.Leaves.Count));
    }
}
