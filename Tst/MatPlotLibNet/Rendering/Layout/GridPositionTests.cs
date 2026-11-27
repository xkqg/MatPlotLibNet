// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Layout;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>Which edges of the figure's grid an axes' cell touches: the one rule the layout paths read to decide whether an
/// axes' need goes to a margin (an axes on that edge) or to the gap between rows or columns (an axes inside).</summary>
public sealed class GridPositionTests
{
    [Fact]
    public void TheOnlyCellOfAOneByOneGrid_TouchesAllFourEdges()
    {
        var edge = FigureGrid.EdgeIn(GridPosition.Single(0, 0), rows: 1, columns: 1);

        Assert.Equal(new Sides<bool>(Left: true, Bottom: true, Top: true, Right: true), edge);
    }

    [Theory]
    [InlineData(0, 0, true, false, true, false)]
    [InlineData(0, 1, true, false, false, true)]
    [InlineData(1, 0, false, true, true, false)]
    [InlineData(1, 1, false, true, false, true)]
    public void ACellOfATwoByTwoGrid_TouchesTheEdgesItSitsOn(int row, int col, bool top, bool bottom, bool left, bool right)
    {
        var edge = FigureGrid.EdgeIn(GridPosition.Single(row, col), rows: 2, columns: 2);

        Assert.Equal(new Sides<bool>(Left: left, Bottom: bottom, Top: top, Right: right), edge);
    }

    [Fact]
    public void ACellInsideAThreeByThreeGrid_TouchesNoEdge()
    {
        var edge = FigureGrid.EdgeIn(GridPosition.Single(1, 1), rows: 3, columns: 3);

        Assert.Equal(new Sides<bool>(Left: false, Bottom: false, Top: false, Right: false), edge);
    }

    [Fact]
    public void ASpanningCell_TouchesEveryEdgeItsSpanReaches()
    {
        // Two rows tall in a column of a two-by-three grid: top and bottom, and the left edge only.
        var edge = FigureGrid.EdgeIn(new GridPosition(RowStart: 0, RowEnd: 2, ColStart: 0, ColEnd: 1), rows: 2, columns: 3);

        Assert.Equal(new Sides<bool>(Left: true, Bottom: true, Top: true, Right: false), edge);
    }

    [Fact]
    public void ACellThatEndsBeyondTheGrid_StillTouchesTheFarEdge()
    {
        // A position wider than the grid the figure measured: its end is at or past the last column.
        var edge = FigureGrid.EdgeIn(new GridPosition(0, 1, 1, 5), rows: 1, columns: 3);

        Assert.True(edge.Right);
        Assert.False(edge.Left);
    }
}
