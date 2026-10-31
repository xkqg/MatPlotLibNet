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
    public void ARightAxisSeries_IsInTheTable_UnderItsOwnAxisLabel()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.SetXLabel("t").Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = "left");
                ax.WithAddRightYAxis(sec => sec.SetYLabel("right").Plot([1.0, 2.0], [10.0, 20.0], s => s.Label = "right series"));
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

    // ---- what the figure itself carries: its texts and its images ------------------------------------------------

    [Fact]
    public void AFigureWithNoTextAndNoImage_HasNoTableForThem()
    {
        var figure = Plt.Create().WithTitle("Revenue").Plot([1.0, 2.0], [1.0, 2.0]).Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal("Revenue", table.Caption);
    }

    /// <summary>The words placed on the figure are part of what it says (a source line, a note, a panel letter): a reader who
    /// cannot see the picture needs them as much as the values, so they are a table of their own, one row for each, in the
    /// order they were added.</summary>
    [Fact]
    public void TheTextsOfAFigure_AreATableOfTheirOwn_OneRowEach_AfterTheSubplots()
    {
        var figure = Plt.Create().WithTitle("Revenue")
            .Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = "2026")
            .FigText("Source: ECB", 0.02, 0.03)
            .FigText("(a)", 0.5, 0.97)
            .Build();

        var tables = figure.ToDataTables();

        Assert.Equal(2, tables.Count);
        var texts = tables[1];
        Assert.Equal("Revenue — figure text", texts.Caption);
        var column = Assert.Single(texts.Columns);
        Assert.Equal(new ChartDataColumn("text", DataColumnKind.Text), column);
        Assert.Equal(["Source: ECB", "(a)"], texts.Rows.Select(row => row[0].Text));
    }

    [Fact]
    public void AFigureWithNothingButATextOnIt_TablesThatText()
    {
        var figure = Plt.Create().FigText("only a note", 0.5, 0.5).Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal("figure text", table.Caption);
        Assert.Equal("only a note", Assert.Single(table.Rows)[0].Text);
        Assert.Contains("only a note", table.ToHtml());
        Assert.Contains("only a note", table.ToCsv());
    }

    /// <summary>The table says what the picture shows, as it leaves out an invisible series: a text with no place on the figure
    /// is not drawn, and a text of nothing but blanks shows nothing.</summary>
    [Theory]
    [InlineData("hidden", 1.5, 0.5)]
    [InlineData("hidden", 0.5, -0.1)]
    [InlineData("hidden", double.NaN, 0.5)]
    [InlineData("   ", 0.5, 0.5)]
    [InlineData("", 0.5, 0.5)]
    public void ATextThatShowsNothing_IsNotInTheTable(string text, double x, double y)
    {
        var figure = Plt.Create().Plot([1.0], [1.0]).FigText(text, x, y).FigText("shown", 0.5, 0.5).Build();

        var tables = figure.ToDataTables();

        var texts = tables[^1];
        Assert.Equal(["shown"], texts.Rows.Select(row => row[0].Text));
    }

    [Fact]
    public void AFigureWhoseOnlyTextShowsNothing_HasNoTextTable()
    {
        var figure = Plt.Create().Plot([1.0], [1.0]).FigText("hidden", 2, 2).Build();

        Assert.Single(figure.ToDataTables());
    }

    /// <summary>A figure image is a grid of pixels laid on the figure under its subplots (a logo, a watermark, a texture), and
    /// its table says so: what it is, how many pixels across and down, and where it stands, so a reader who cannot see it knows
    /// it is there and that it is a picture of pixels rather than a series of values.</summary>
    [Fact]
    public void TheImagesOfAFigure_AreATableOfTheirOwn_OneRowEach_SayingWhatTheyAre()
    {
        var figure = Plt.Create().WithTitle("Revenue")
            .Plot([1.0, 2.0], [1.0, 2.0])
            .FigText("note", 0.5, 0.5)
            .FigImage(new double[,] { { 1, 2, 3 }, { 4, 5, 6 } }, 12, 34)
            .FigImage(new double[,] { { 9 } }, 0, 0)
            .Build();

        var tables = figure.ToDataTables();

        Assert.Equal(3, tables.Count);
        var images = tables[2];
        Assert.Equal("Revenue — figure images", images.Caption);
        Assert.Equal(
            [
                new ChartDataColumn("image", DataColumnKind.Text),
                new ChartDataColumn("width (pixels)"),
                new ChartDataColumn("height (pixels)"),
                new ChartDataColumn("from left (pixels)"),
                new ChartDataColumn("from bottom (pixels)"),
            ],
            images.Columns);
        Assert.Equal(2, images.RowCount);
        Assert.Equal("image 1", images.Rows[0][0].Text);
        Assert.Equal([3.0, 2.0, 12.0, 34.0], images.Rows[0].Skip(1).Select(cell => cell.Number));
        Assert.Equal("image 2", images.Rows[1][0].Text);
        Assert.Equal([1.0, 1.0, 0.0, 0.0], images.Rows[1].Skip(1).Select(cell => cell.Number));
    }

    [Fact]
    public void AFigureWithNothingButAnImageOnIt_TablesThatImage()
    {
        var figure = Plt.Create().FigImage(new double[,] { { 1, 2 } }, 5, 6).Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal("figure images", table.Caption);
        Assert.Contains("image 1", table.ToMarkdown());
    }

    /// <summary>An image with no place on the figure (an offset that is no number) or with no pixel to draw shows nothing, so
    /// the table leaves it out (a grid of rows with no column included); the numbering stays the order the images were added, so
    /// "image 4" is still the fourth added.</summary>
    [Fact]
    public void AnImageThatShowsNothing_IsNotInTheTable_AndTheNumberingKeepsTheOrderAdded()
    {
        var figure = Plt.Create().Plot([1.0], [1.0])
            .FigImage(new double[,] { { 1 } }, double.NaN, 0)
            .FigImage(new double[0, 0], 0, 0)
            .FigImage(new double[2, 0], 0, 0)
            .FigImage(new double[,] { { 1, 2 } }, 7, 8)
            .Build();

        var images = figure.ToDataTables()[^1];

        var row = Assert.Single(images.Rows);
        Assert.Equal("image 4", row[0].Text);
        Assert.Equal([2.0, 1.0, 7.0, 8.0], row.Skip(1).Select(cell => cell.Number));
    }

    [Fact]
    public void AFigureWhoseOnlyImageShowsNothing_HasNoImageTable()
    {
        var figure = Plt.Create().Plot([1.0], [1.0]).FigImage(new double[,] { { 1 } }, double.PositiveInfinity, 0).Build();

        Assert.Single(figure.ToDataTables());
    }

    [Fact]
    public void TheOffsetsOfAnImage_AreWrittenInFull_InEveryForm()
    {
        var figure = Plt.Create().FigImage(new double[,] { { 1, 2 }, { 3, 4 } }, 1.5, 2.5).Build();

        var table = Assert.Single(figure.ToDataTables());

        Assert.Equal("image,width (pixels),height (pixels),from left (pixels),from bottom (pixels)\r\nimage 1,2,2,1.5,2.5\r\n", table.ToCsv());
        Assert.Contains("| image 1 | 2 | 2 | 1.5 | 2.5 |", table.ToMarkdown());
    }
}
