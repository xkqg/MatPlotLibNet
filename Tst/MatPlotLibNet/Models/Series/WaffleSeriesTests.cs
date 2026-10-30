// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A waffle chart: a grid of square cells, filled from the bottom-left corner row by row, category by category, each
/// category taking its share of the cells by the largest remainder method. The cells are square at any figure size (the grid
/// is the largest of square cells that fits the plot area, centred), a category with no cell keeps its legend entry and its
/// row in the table, and the grid's size and gap are settings of the series.</summary>
public class WaffleSeriesTests
{
    private static readonly string[] Names = ["A", "B", "C"];
    private static readonly double[] Shares = [37, 33, 30];
    private static readonly Color[] Palette = [Color.FromHex("#CC0000"), Color.FromHex("#00AA00"), Color.FromHex("#0000CC")];

    private static Figure Of(int width, int height, Action<WaffleSeries>? configure = null, double[]? values = null) => Plt.Create()
        .WithSize(width, height)
        .AddSubPlot(1, 1, 1, ax => ax.Waffle(Names, values ?? Shares, s => { s.Colors = Palette; configure?.Invoke(s); }))
        .Build();

    /// <summary>The cells as drawn: the rectangles and the colour each wears.</summary>
    private static List<(Rect Box, Color Fill)> Cells(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        var cells = new List<(Rect, Color)>();
        foreach (var call in recording.OfKind("DrawRectangle"))
        {
            dynamic args = call.Args!;
            Color? fill = args.fill;
            Rect box = args.rect;
            if (fill is { } colour && Palette.Contains(colour)) cells.Add((box, colour));
        }

        return cells;
    }

    private static Rect Plot(Figure figure) => new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];

    [Theory]
    [InlineData(600, 600)]
    [InlineData(500, 350)]
    [InlineData(900, 500)]
    public void TheHundredCells_AreSquare_AndStandOnOneGrid_AtAnyFigureSize(int width, int height)
    {
        var cells = Cells(Of(width, height));

        Assert.Equal(100, cells.Count);
        Assert.All(cells, cell => Assert.Equal(cell.Box.Width, cell.Box.Height, 9));
        double[] columns = [.. cells.Select(cell => cell.Box.X).Distinct().Order()];
        double[] rows = [.. cells.Select(cell => cell.Box.Y).Distinct().Order()];
        Assert.Equal(10, columns.Length);
        Assert.Equal(10, rows.Length);
        double pitch = columns[1] - columns[0];
        for (int i = 1; i < 10; i++)
        {
            Assert.Equal(pitch, columns[i] - columns[i - 1], 9);
            Assert.Equal(pitch, rows[i] - rows[i - 1], 9);
        }
    }

    [Theory]
    [InlineData(600, 600)]
    [InlineData(900, 500)]
    public void TheGrid_IsCentredInThePlotArea_AndNoLargerThanFitsIt(int width, int height)
    {
        var figure = Of(width, height);
        var plot = Plot(figure);
        var cells = Cells(figure);

        double left = cells.Min(cell => cell.Box.X), right = cells.Max(cell => cell.Box.X + cell.Box.Width);
        double top = cells.Min(cell => cell.Box.Y), bottom = cells.Max(cell => cell.Box.Y + cell.Box.Height);

        Assert.Equal(plot.X + plot.Width / 2, (left + right) / 2, 6);
        Assert.Equal(plot.Y + plot.Height / 2, (top + bottom) / 2, 6);
        Assert.True(right - left <= plot.Width + 1e-6 && bottom - top <= plot.Height + 1e-6);
    }

    [Fact]
    public void TheCells_FillFromTheBottomLeft_RowByRow_CategoryByCategory()
    {
        var cells = Cells(Of(600, 600));
        double left = cells.Min(cell => cell.Box.X), bottom = cells.Max(cell => cell.Box.Y);
        double pitch = cells.Select(cell => cell.Box.X).Distinct().Order().Take(2).Aggregate((a, b) => b - a);

        Color At(int row, int column) =>
            cells.Single(cell => Math.Abs(cell.Box.X - (left + column * pitch)) < 1e-6 && Math.Abs(cell.Box.Y - (bottom - row * pitch)) < 1e-6).Fill;

        Assert.Equal(Palette[0], At(0, 0));   // index 0: the bottom-left corner
        Assert.Equal(Palette[0], At(3, 4));   // index 34 of the first category's 37
        Assert.Equal(Palette[0], At(3, 6));   // index 36: its last
        Assert.Equal(Palette[1], At(3, 7));   // index 37: the second category begins, on the same row
        Assert.Equal(Palette[2], At(9, 9));   // index 99: the top-right corner
    }

    [Fact]
    public void TheCellsOfACategory_AreItsShareOfTheGrid()
    {
        var cells = Cells(Of(600, 600));

        Assert.Equal([37, 33, 30], Palette.Select(colour => cells.Count(cell => cell.Fill == colour)));
    }

    [Fact]
    public void TheGapBetweenCells_IsAShareOfTheirSide()
    {
        var gapped = Cells(Of(600, 600, s => s.CellGap = 0.2));
        var touching = Cells(Of(600, 600, s => s.CellGap = 0));

        Assert.Equal(0.8, gapped[0].Box.Width / touching[0].Box.Width, 9);
        Assert.Equal(touching[1].Box.X - touching[0].Box.X, touching[0].Box.Width, 9);
    }

    [Fact]
    public void AnotherGrid_FillsInTheSameOrder_AndKeepsTheCellsSquare()
    {
        var cells = Cells(Of(800, 400, s => { s.Rows = 5; s.Columns = 20; }));

        Assert.Equal(100, cells.Count);
        Assert.All(cells, cell => Assert.Equal(cell.Box.Width, cell.Box.Height, 9));
        Assert.Equal(20, cells.Select(cell => cell.Box.X).Distinct().Count());
        Assert.Equal(5, cells.Select(cell => cell.Box.Y).Distinct().Count());
    }

    [Fact]
    public void WithNothingToDivide_NoCellIsDrawn()
    {
        Assert.Empty(Cells(Of(600, 600, values: [0, double.NaN, -4])));
    }

    [Fact]
    public void TheTable_IsOneRowPerCategory_WithTheCellsItTakes_AddingUpToTheGrid()
    {
        var table = new Axes().Waffle(Names, Shares).ToDataTable()!;

        Assert.Equal(["category", "value", "cells"], table.Columns.Select(c => c.Header));
        Assert.Equal(["A", "B", "C"], table.Rows.Select(row => row[0].Text));
        Assert.Equal([37.0, 33.0, 30.0], table.Rows.Select(row => row[2].Number));
        Assert.Equal(100.0, table.Rows.Sum(row => row[2].Number));
    }

    [Fact]
    public void ACategoryWithNoCell_StillHasItsLegendEntry_AndItsRowInTheTable()
    {
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.Waffle(Names, [60, 0, 40]).WithLegend()).Build();

        var entries = figure.SubPlots[0].LegendEntries(directLabelsDrawn: false);

        Assert.Equal(["A", "B", "C"], entries.Select(entry => entry.Label));
        Assert.Equal(0, new Axes().Waffle(Names, [60, 0, 40]).CellCounts()[1]);
    }

    [Fact]
    public void TheLegendSwatches_AreInTheColourSlotsTheCellsWear()
    {
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.Waffle(Names, Shares).WithLegend()).Build();

        var slots = figure.SubPlots[0].LegendEntries(directLabelsDrawn: false).Select(entry => entry.ColorSlotIndex).ToArray();

        Assert.Equal([0, 1, 2], slots);
    }

    [Fact]
    public void TheShares_SurviveTheJson_AndDrawTheSamePicture()
    {
        var figure = Of(600, 400, s => { s.Rows = 5; s.Columns = 8; s.CellGap = 0.1; });

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        var waffle = Assert.IsType<WaffleSeries>(Assert.Single(back.SubPlots[0].Series));
        Assert.Equal(Names, waffle.Categories);
        Assert.Equal(Shares, waffle.Values);
        Assert.Equal((5, 8, 0.1), (waffle.Rows, waffle.Columns, waffle.CellGap));
        Assert.Equal(Palette, waffle.Colors);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Fact]
    public void TheDefaultGrid_IsLeftOutOfTheJson()
    {
        string json = Of(600, 400).ToJson();

        Assert.DoesNotContain("waffleRows", json, StringComparison.Ordinal);
        Assert.DoesNotContain("waffleColumns", json, StringComparison.Ordinal);
        Assert.DoesNotContain("cellGap", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void AGridWithoutARowOrAColumn_IsRefusedByName(int count)
    {
        var rows = Assert.Throws<ArgumentOutOfRangeException>(() => new WaffleSeries(Names, Shares) { Rows = count });
        var columns = Assert.Throws<ArgumentOutOfRangeException>(() => new WaffleSeries(Names, Shares) { Columns = count });

        Assert.Equal("Rows", rows.ParamName);
        Assert.Equal("Columns", columns.ParamName);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.0)]
    [InlineData(double.NaN)]
    public void AGapThatIsNotAShareOfTheSide_IsRefusedByName(double gap)
    {
        Assert.Equal("CellGap", Assert.Throws<ArgumentOutOfRangeException>(() => new WaffleSeries(Names, Shares) { CellGap = gap }).ParamName);
    }

    [Fact]
    public void CategoriesAndValuesOfAnotherLength_AreRefused()
    {
        Assert.Throws<ArgumentException>(() => new Axes().Waffle(["A", "B"], [1.0]));
    }

    [Fact]
    public void TheSeriesTakesOneColourSlotForEachCategory_SoTheNextSeriesWearsTheNext()
    {
        var waffle = new Axes().Waffle(Names, Shares);

        Assert.Equal(3, waffle.ColorSlotSpan);
        Assert.True(waffle.DrawsWithoutAxes);
    }
}
