// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>The projection that turns the library's own data tables into the values <c>chart_data_table</c> returns
/// beside its markdown. The cases here are the ones no chart in the suite produces on its own: a slot that exists
/// because the table is rectangular rather than because there was a value, and a number in a date column that no
/// date could ever be.</summary>
public class ChartTablesTests
{
    private static ChartTableData Project(DataColumnKind kind, params DataCell[] cells) =>
        Assert.Single(ChartTables.From(
        [
            new ChartDataTable("t", [new("when", kind, DataAxis.X)], [.. cells.Select(cell => (IReadOnlyList<DataCell>)[cell])]),
        ]).Tables);

    [Fact]
    public void AnEmptyCell_ComesBackAsNothing()
    {
        // A table is rectangular; a row that ran out of values still has the slot. It is null, not zero, because
        // zero is a value somebody measured.
        var table = Project(DataColumnKind.Number, DataCell.Empty);

        Assert.Null(Assert.Single(Assert.Single(table.Rows)));
    }

    [Fact]
    public void ATextCell_ComesBackAsItsText()
    {
        var table = Project(DataColumnKind.Text, DataCell.FromText("north"));

        Assert.Equal("north", Assert.Single(Assert.Single(table.Rows)));
    }

    [Theory]
    [InlineData(double.NaN)]                 // not a number at all
    [InlineData(double.PositiveInfinity)]    // finite is the first thing a date has to be
    [InlineData(-700000.0)]                  // before the earliest date that can be written
    [InlineData(3000000.0)]                  // after the last one
    public void ANumberNoDateCouldBe_StaysTheNumberItIs(double value)
    {
        var table = Project(DataColumnKind.Date, DataCell.FromNumber(value));

        Assert.Equal(value, Assert.Single(Assert.Single(table.Rows)));
    }

    [Fact]
    public void ANumberADateCanBe_IsWrittenOutInFull()
    {
        double noon = new DateTime(2026, 9, 12, 12, 0, 0, DateTimeKind.Unspecified).ToOADate();

        var table = Project(DataColumnKind.Date, DataCell.FromNumber(noon));

        Assert.Equal("2026-09-12T12:00:00.000", Assert.Single(Assert.Single(table.Rows)));
    }

    [Fact]
    public void EveryColumnKindAndAxis_HasAWordOnTheWire()
    {
        var table = Assert.Single(ChartTables.From(
        [
            new ChartDataTable(null,
                [new("a", DataColumnKind.Number, DataAxis.X),
                 new("b", DataColumnKind.Date, DataAxis.Y),
                 new("c", DataColumnKind.Text, DataAxis.None)],
                []),
        ]).Tables);

        Assert.Null(table.Caption);
        Assert.Equal(["number", "date", "text"], table.Columns.Select(c => c.Kind));
        Assert.Equal(["x", "y", "none"], table.Columns.Select(c => c.Axis));
    }
}
