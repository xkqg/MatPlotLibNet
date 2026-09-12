// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Models;

/// <summary>A cell is a number, a text or nothing — three states, and no way to build a fourth. Two nullable
/// fields would have allowed "both set" and "neither set", and every emitter would have carried a branch for a
/// state no legal input reaches.</summary>
public class DataCellTests
{
    [Fact]
    public void ANumberCell_CarriesItsValue()
    {
        var cell = DataCell.FromNumber(15);

        Assert.Equal(DataCellKind.Number, cell.Kind);
        Assert.Equal(15.0, cell.Number);
        Assert.Null(cell.Text);
    }

    [Fact]
    public void ATextCell_CarriesItsText()
    {
        var cell = DataCell.FromText("Q3");

        Assert.Equal(DataCellKind.Text, cell.Kind);
        Assert.Equal("Q3", cell.Text);
        Assert.True(double.IsNaN(cell.Number));
    }

    [Fact]
    public void TheEmptyCell_IsNeitherNumberNorText()
    {
        var cell = DataCell.Empty;

        Assert.Equal(DataCellKind.Empty, cell.Kind);
        Assert.Null(cell.Text);
        Assert.True(double.IsNaN(cell.Number));
    }

    [Fact]
    public void ADefaultStruct_IsTheEmptyCell()
    {
        // default(DataCell) exists whether we like it or not; it must mean the same thing as Empty.
        Assert.Equal(DataCell.Empty, default(DataCell));
    }

    [Fact]
    public void ATextCell_RefusesNull()
    {
        Assert.Throws<ArgumentNullException>(() => DataCell.FromText(null!));
    }

    [Fact]
    public void ANumberCell_NaN_IsStillANumberCell()
    {
        // NaN is a value a series can hold; the cell records it as a number, the emitter decides how to spell it.
        var cell = DataCell.FromNumber(double.NaN);

        Assert.Equal(DataCellKind.Number, cell.Kind);
        Assert.True(double.IsNaN(cell.Number));
    }

    [Fact]
    public void TwoCells_WithTheSameContent_AreEqual()
    {
        Assert.Equal(DataCell.FromNumber(1.5), DataCell.FromNumber(1.5));
        Assert.Equal(DataCell.FromText("a"), DataCell.FromText("a"));
        Assert.NotEqual(DataCell.FromNumber(1), DataCell.FromText("1"));
    }
}
