// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies <see cref="GridSpec"/> and <see cref="GridPosition"/> behavior.</summary>
public class GridSpecTests
{
    // --- GridSpec ---

    [Fact]
    public void GridSpec_StoresRowsAndCols()
    {
        var gs = new GridSpec { Rows = 3, Cols = 4 };
        Assert.Equal(3, gs.Rows);
        Assert.Equal(4, gs.Cols);
    }

    [Fact]
    public void GridSpec_DefaultRatios_AreNull()
    {
        var gs = new GridSpec { Rows = 2, Cols = 2 };
        Assert.Null(gs.HeightRatios);
        Assert.Null(gs.WidthRatios);
    }

    [Fact]
    public void GridSpec_WithWidthRatios_StoresRatios()
    {
        var gs = new GridSpec { Rows = 1, Cols = 3, WidthRatios = [1, 2, 1] };
        Assert.Equal([1.0, 2.0, 1.0], gs.WidthRatios);
    }

    [Fact]
    public void GridSpec_WithHeightRatios_StoresRatios()
    {
        var gs = new GridSpec { Rows = 2, Cols = 1, HeightRatios = [1, 3] };
        Assert.Equal([1.0, 3.0], gs.HeightRatios);
    }

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
