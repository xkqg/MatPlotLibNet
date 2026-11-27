// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>How a number of panels wraps into a grid (<see cref="PanelWrap"/>): filled row by row up to a most columns, or
/// balanced so that the last row is not much shorter than the others; and the cell the panel at an index takes, row by row.</summary>
public class PanelWrapTests
{
    [Theory]
    [InlineData(1, 3, 1, 1)]
    [InlineData(3, 3, 1, 3)]
    [InlineData(5, 3, 2, 3)]
    [InlineData(5, 4, 2, 4)]
    [InlineData(9, 8, 2, 8)]
    [InlineData(2, 8, 1, 2)]
    public void Filled_TakesAsManyColumnsAsFitAndTheRowsTheyNeed(int count, int most, int rows, int columns)
    {
        var wrap = PanelWrap.Filled(count, most);

        Assert.Equal(rows, wrap.Rows);
        Assert.Equal(columns, wrap.Columns);
    }

    [Theory]
    [InlineData(1, 4, 1, 1)]
    [InlineData(4, 4, 1, 4)]
    [InlineData(5, 4, 2, 3)]
    [InlineData(7, 3, 3, 3)]
    [InlineData(9, 8, 2, 5)]
    public void Balanced_TakesTheRowsTheMostColumnsNeed_ThenAsFewColumnsAsThoseRowsNeed(int count, int most, int rows, int columns)
    {
        var wrap = PanelWrap.Balanced(count, most);

        Assert.Equal(rows, wrap.Rows);
        Assert.Equal(columns, wrap.Columns);
    }

    [Fact]
    public void ThePanelAtAnIndex_TakesItsCellRowByRow()
    {
        var wrap = new PanelWrap(2, 3);

        Assert.Equal(GridPosition.Single(0, 0), wrap.At(0));
        Assert.Equal(GridPosition.Single(0, 2), wrap.At(2));
        Assert.Equal(GridPosition.Single(1, 0), wrap.At(3));
        Assert.Equal(GridPosition.Single(1, 1), wrap.At(4));
    }
}
