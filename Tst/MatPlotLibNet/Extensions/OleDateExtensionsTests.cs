// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>Verifies the one rule for "this number can be written as a date": <see cref="DateTime.FromOADate"/> makes a date of
/// it rather than throwing. The rule is checked against <see cref="DateTime.FromOADate"/> itself, at and around both edges of
/// the range and at the values that are not numbers.</summary>
public class OleDateExtensionsTests
{
    [Theory]
    [InlineData(-657435.0)]
    [InlineData(-657434.9999)]
    [InlineData(-657434.0)]
    [InlineData(0.0)]
    [InlineData(46000.5)]
    [InlineData(2958465.9999)]
    [InlineData(2958466.0)]
    [InlineData(-1e9)]
    [InlineData(1e9)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void IsOleDate_IsExactlyWhereFromOADateMakesADate(double value)
    {
        bool makesADate;
        try
        {
            _ = DateTime.FromOADate(value);
            makesADate = true;
        }
        catch (ArgumentException)
        {
            makesADate = false;
        }

        Assert.Equal(makesADate, value.IsOleDate);
    }
}
