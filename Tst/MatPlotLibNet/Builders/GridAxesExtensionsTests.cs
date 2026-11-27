// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The whole-number ticks of a matrix: the step is the smallest of 1, 2, 5, 10, 20, 50 ... that leaves at most nine
/// intervals across the cells, and each tick names the index of the cell it stands on.</summary>
public class GridAxesExtensionsTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(18, 2)]
    [InlineData(19, 5)]
    [InlineData(45, 5)]
    [InlineData(46, 10)]
    [InlineData(90, 10)]
    [InlineData(91, 20)]
    [InlineData(450, 50)]
    [InlineData(451, 100)]
    [InlineData(1000, 200)]
    public void TheStep_LeavesAtMostNineIntervals(int cells, int expected)
    {
        Assert.Equal(expected, GridAxesExtensions.StepOf(cells));
    }

    [Fact]
    public void ASmallMatrix_IsNamedAtEveryCell()
    {
        var names = GridAxesExtensions.NamesOf(4);

        Assert.Equal(["0", "1", "2", "3"], names.OrderBy(pair => pair.Key).Select(pair => pair.Value));
        Assert.Equal([0, 1, 2, 3], names.Keys.Order());
    }

    [Fact]
    public void ALargerMatrix_IsNamedAtEveryStepFromTheFirstCell()
    {
        var names = GridAxesExtensions.NamesOf(1000);

        Assert.Equal([0, 200, 400, 600, 800], names.Keys.Order());
        Assert.Equal("600", names[600]);
    }

    [Fact]
    public void ACellBetweenTwoTicks_HasNoName()
    {
        Assert.False(GridAxesExtensions.NamesOf(12).ContainsKey(3));
        Assert.Equal("4", GridAxesExtensions.NamesOf(12)[4]);
    }
}
