// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Extensions;

/// <summary>A figure's tables are what the picture shows, said in rows: one table per group of series that share
/// an x, so two lines over the same quarters read as two columns and not as two tables. The axes names the first
/// column, because the series only knows it is "x" and the axes knows it is "Quarter".</summary>
public class FigureDataTablesTests
{
    [Fact]
    public void ToDataTables_EmptyInput_ReturnsNoTables()
    {
        Assert.Empty(Plt.Create().Build().ToDataTables());
    }

    [Fact]
    public void AFigureWithOneSeries_IsOneTable()
    {
        var figure = Plt.Create().WithTitle("Revenue")
            .AddSubPlot(1, 1, 1, ax => ax.SetXLabel("Quarter").Plot([1.0, 2.0], [12.0, 18.0], s => s.Label = "2026"))
            .Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal("Revenue", table.Caption);
        Assert.Equal(["Quarter", "2026"], table.Columns.Select(c => c.Header));
        Assert.Equal(2, table.RowCount);
    }

    [Fact]
    public void TwoSeriesOverTheSameX_ShareOneTable()
    {
        double[] x = [1, 2, 3];
        var figure = Plt.Create().WithTitle("Revenue")
            .AddSubPlot(1, 1, 1, ax => ax
                .SetXLabel("Quarter")
                .Plot(x, [1.0, 2.0, 3.0], s => s.Label = "2025")
                .Plot(x, [4.0, 5.0, 6.0], s => s.Label = "2026"))
            .Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal(["Quarter", "2025", "2026"], table.Columns.Select(c => c.Header));
        Assert.Equal(3, table.RowCount);
        Assert.Equal(6.0, table.Rows[2][2].Number);
    }

    [Fact]
    public void TwoSeriesOverTheSameValues_ShareOneTable_EvenWhenTheArraysAreDifferentObjects()
    {
        var figure = Plt.Create()
            .Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = "a")
            .Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "b")
            .Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal(["x", "a", "b"], table.Columns.Select(c => c.Header));
    }

    [Fact]
    public void TwoSeriesOverDifferentX_AreTwoTables()
    {
        var figure = Plt.Create().WithTitle("Two")
            .Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = "a")
            .Plot([9.0, 10.0], [3.0, 4.0], s => s.Label = "b")
            .Build();

        var tables = figure.ToDataTables();

        Assert.Equal(2, tables.Count);
        // Each table says which series it is: two tables under one caption would be two anonymous grids.
        Assert.Equal("Two — a", tables[0].Caption);
        Assert.Equal("Two — b", tables[1].Caption);
    }

    [Fact]
    public void AnInvisibleSeries_IsNotInTheTable()
    {
        var figure = Plt.Create()
            .Plot([1.0], [1.0], s => s.Label = "shown")
            .Plot([1.0], [2.0], s => { s.Label = "hidden"; s.Visible = false; })
            .Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal(["x", "shown"], table.Columns.Select(c => c.Header));
    }

    [Fact]
    public void ASecondaryAxisSeries_IsInTheTable_UnderItsOwnAxisLabel()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.SetXLabel("t").Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = "left");
                ax.WithSecondaryYAxis(sec => sec.SetYLabel("right").Plot([1.0, 2.0], [10.0, 20.0], s => s.Label = "right series"));
            })
            .Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal(["t", "left", "right series"], table.Columns.Select(c => c.Header));
        Assert.Equal(20.0, table.Rows[1][2].Number);
    }

    [Fact]
    public void ADateScaledXAxis_MakesTheFirstColumnADate()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([new DateTime(2026, 3, 15), new DateTime(2026, 3, 16)], [1.0, 2.0]))
            .Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal(DataColumnKind.Date, table.Columns[0].Kind);
        Assert.Contains("2026-03-15", table.ToCsv());
    }

    [Fact]
    public void ADateTickFormatter_AloneAlsoMakesTheFirstColumnADate()
    {
        // SetXTickFormatter sets the formatter and NOT the scale; a chart that reads as dated must table as dated.
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .SetXTickFormatter(new MatPlotLibNet.Rendering.TickFormatters.DateTickFormatter())
                .Plot([new DateTime(2026, 3, 15).ToOADate()], [1.0]))
            .Build();

        Assert.Equal(DataColumnKind.Date, Assert.Single(figure.ToDataTables()).Columns[0].Kind);
    }

    [Fact]
    public void EverySubplot_IsItsOwnTable_NamedByItsTitle()
    {
        var figure = Plt.Create().WithTitle("Dashboard")
            .AddSubPlot(1, 2, 1, ax => ax.WithTitle("Left").Plot([1.0], [1.0]))
            .AddSubPlot(1, 2, 2, ax => ax.WithTitle("Right").Plot([1.0], [2.0]))
            .Build();

        var tables = figure.ToDataTables();

        Assert.Equal(2, tables.Count);
        Assert.Equal("Dashboard — Left", tables[0].Caption);
        Assert.Equal("Dashboard — Right", tables[1].Caption);
    }

    [Fact]
    public void ASubplotTitle_AloneNamesTheTable_WhenTheFigureHasNoName()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithTitle("Only").Plot([1.0], [1.0]))
            .Build();

        Assert.Equal("Only", Assert.Single(figure.ToDataTables()).Caption);
    }

    [Fact]
    public void AnUnnamedFigure_YieldsATableWithoutACaption()
    {
        Assert.Null(Assert.Single(Plt.Create().Plot([1.0], [1.0]).Build().ToDataTables()).Caption);
    }

    [Fact]
    public void ASeriesWithoutATabularForm_ContributesNothing()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.AddSeries(new QuiverKeySeries(0.5, 0.9, 1.0, "1 m/s")))
            .Build();

        Assert.Empty(figure.ToDataTables());
    }

    [Fact]
    public void ASignalSeriesUnderAViewport_TablesAtFullResolution()
    {
        // The renderer slices a monotonic signal to the visible window before it draws; the table is the DATA.
        double[] samples = [.. Enumerable.Range(0, 1000).Select(i => (double)i)];
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .SetXLim(0, 10)
                .AddSeries(new SignalXYSeries([.. Enumerable.Range(0, 1000).Select(i => (double)i)], samples)))
            .Build();

        Assert.Equal(1000, Assert.Single(figure.ToDataTables()).RowCount);
    }

    [Fact]
    public void ADownsampledLine_TablesEveryPoint()
    {
        double[] x = [.. Enumerable.Range(0, 500).Select(i => (double)i)];
        var figure = Plt.Create()
            .Plot(x, x, s => s.MaxDisplayPoints = 10)
            .Build();

        Assert.Equal(500, Assert.Single(figure.ToDataTables()).RowCount);
    }

    [Fact]
    public void ToDataTables_VeryLarge_KeepsEveryRow_AndTheEmittersCap()
    {
        double[] x = [.. Enumerable.Range(0, 1500).Select(i => (double)i)];
        var table = Assert.Single(Plt.Create().Plot(x, x).Build().ToDataTables());

        Assert.Equal(1500, table.RowCount);
        Assert.Contains("500 more rows", table.ToHtml());
        Assert.Equal(1501, table.ToCsv().Split("\r\n").Length - 1);
    }
}
