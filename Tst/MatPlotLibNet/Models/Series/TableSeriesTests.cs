// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="TableSeries"/> default properties, construction, and serialization.</summary>
public class TableSeriesTests
{
    private static readonly string[][] Data = [["Alice", "90"], ["Bob", "85"], ["Carol", "92"]];

    [Fact]
    public void Constructor_StoresCellData()
    {
        var series = new TableSeries(Data);
        Assert.Equal(Data, series.CellData);
    }

    [Fact]
    public void CellHeight_DefaultsTo25()
    {
        var series = new TableSeries(Data);
        Assert.Equal(25.0, series.CellHeight);
    }

    [Fact]
    public void CellPadding_DefaultsTo4()
    {
        var series = new TableSeries(Data);
        Assert.Equal(4.0, series.CellPadding);
    }

    [Fact]
    public void FontSize_DefaultsTo11()
    {
        var series = new TableSeries(Data);
        Assert.Equal(11.0, series.FontSize);
    }

    [Fact]
    public void ColumnHeaders_DefaultsToNull()
    {
        var series = new TableSeries(Data);
        Assert.Null(series.ColumnHeaders);
    }

    [Fact]
    public void RowHeaders_DefaultsToNull()
    {
        var series = new TableSeries(Data);
        Assert.Null(series.RowHeaders);
    }

    [Fact]
    public void ToSeriesDto_ReturnsTypeTable()
    {
        var series = new TableSeries(Data);
        Assert.Equal("table", series.ToSeriesDto().Type);
    }

    [Fact]
    public void ToSeriesDto_IncludesCellData()
    {
        var series = new TableSeries(Data);
        Assert.Equal(Data, series.ToSeriesDto().TableCellData);
    }

}
