// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="GridSpec"/> and <see cref="GridPosition"/> behavior.</summary>
public class GridSpecTests
{
    // --- GridSpec ---

    [Fact]
    public void GridSpec_RecordEquality_Works()
    {
        var a = new GridSpec { Rows = 2, Cols = 3 };
        var b = new GridSpec { Rows = 2, Cols = 3 };
        Assert.Equal(a, b);
    }

    // --- GridPosition ---

    [Fact]
    public void GridPosition_Single_CreatesOneCell()
    {
        var pos = GridPosition.Single(1, 2);
        Assert.Equal(1, pos.RowStart);
        Assert.Equal(2, pos.RowEnd);
        Assert.Equal(2, pos.ColStart);
        Assert.Equal(3, pos.ColEnd);
    }

    [Fact]
    public void GridPosition_Span_CreatesMultiCells()
    {
        var pos = new GridPosition(0, 2, 0, 3);
        Assert.Equal(0, pos.RowStart);
        Assert.Equal(2, pos.RowEnd);
        Assert.Equal(0, pos.ColStart);
        Assert.Equal(3, pos.ColEnd);
    }

    [Fact]
    public void GridPosition_RecordEquality_Works()
    {
        var a = GridPosition.Single(0, 0);
        var b = GridPosition.Single(0, 0);
        Assert.Equal(a, b);
    }
}
