// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>What a table's cells are where their author names nothing is a value of the theme's table look (<see cref="TableLook"/>).
/// Under the matplotlib themes it is matplotlib 3.11.2's <c>ax.table</c>, read with python: <c>table(..., cellLoc='right', rowLoc='left',
/// colLoc='center')</c>; <c>Cell(..., edgecolor='k', facecolor='w')</c> for the data, the column headers and the row labels alike;
/// <c>Cell.PAD = 0.1</c>, the share of its cell's width a left or right aligned text stands in from the edge
/// (<c>Cell._set_text_position</c>); and a row <c>Table.FONTSIZE</c> (10) times 1.2 points high, printed 16.67 px at 100 dpi. The
/// library's own themes keep theirs: light grey headers, dark grey rules, rows 25 px high, data left aligned and row headers centred, 4 px
/// in from the edge.</summary>
public class TableLookTests
{
    public static TheoryData<string, string, string, double, TextAlignment, TextAlignment> UngivenCells => new()
    {
        { "MatplotlibV2", "#FFFFFF", "#000000", 10 * 1.2 * 100 / 72.0, TextAlignment.Right, TextAlignment.Left },
        { "MatplotlibClassic", "#FFFFFF", "#000000", 10 * 1.2 * 100 / 72.0, TextAlignment.Right, TextAlignment.Left },
        { "Default", "#C8C8C8", "#505050", 25, TextAlignment.Left, TextAlignment.Center },
    };

    [Theory]
    [MemberData(nameof(UngivenCells))]
    public void ATableGivenNothing_IsCelledAsItsThemeCellsOne(string themeName, string header, string border, double height,
        TextAlignment data, TextAlignment rowHeader)
    {
        var look = themeName.AsTheme().Looks.Table;

        Assert.Equal(header, look.HeaderFillOver(null).ToHex());
        Assert.Equal("#FFFFFF", look.CellFillOver(null).ToHex());
        Assert.Equal(border, look.BorderOver(null).ToHex());
        Assert.Equal(height, look.CellHeightOver(null, 1), 9);
        Assert.Equal(data, look.AlignmentOf(null, 0));
        Assert.Equal(rowHeader, look.RowHeaderAlignment);
    }

    [Theory]
    [InlineData("MatplotlibV2", 2 * 12 * 100.0 / 72, true)]
    [InlineData("MatplotlibClassic", 2 * 12 * 100.0 / 72, true)]
    [InlineData("Default", 25.0, false)]
    public void ARowNotGiven_KeepsItsShareOfTheAxes_OnlyUnderMatplotlibsLook(string themeName, double heightOnAxesTwiceAsHigh, bool stretches)
    {
        // matplotlib's row is a share of the height of the axes the table was made on (table.py:399-401): axes twice as high, a row
        // twice as high. The library's own rows are 25 px whatever the axes.
        var look = themeName.AsTheme().Looks.Table;

        Assert.Equal(heightOnAxesTwiceAsHigh, look.CellHeightOver(null, 2), 9);
        Assert.Equal(stretches, look.Stretches(null));
    }

    [Theory]
    [InlineData("MatplotlibV2", 10.0)]
    [InlineData("MatplotlibClassic", 10.0)]
    [InlineData("Default", 4.0)]
    public void AnAlignedTextStandsIn_ByItsThemesPad(string themeName, double padOfAHundredPixelCell) =>
        Assert.Equal(padOfAHundredPixelCell, themeName.AsTheme().Looks.Table.PadOver(null, 100), 9);

    [Theory]
    [InlineData("MatplotlibV2")]
    [InlineData("Default")]
    public void WhatWasGiven_IsDrawnUnderEveryTheme(string themeName)
    {
        var look = themeName.AsTheme().Looks.Table;

        Assert.Equal(Colors.Red, look.HeaderFillOver(Colors.Red));
        Assert.Equal(Colors.Blue, look.CellFillOver(Colors.Blue));
        Assert.Equal(Colors.Green, look.BorderOver(Colors.Green));
        Assert.Equal(30, look.CellHeightOver(30, 2));   // a height given is kept wherever the axes moved
        Assert.False(look.Stretches(30));
        Assert.Equal(TextAlignment.Center, look.AlignmentOf([TextAlignment.Center], 0));
        Assert.Equal(6, look.PadOver(6, 100));
    }

    [Fact]
    public void TheJson_WritesTheHeightAndThePadOnlyWhereTheyWereGiven()
    {
        var resting = new TableSeries([["1"]]);
        var givenAtTheLibrarysRest = new TableSeries([["1"]]) { CellHeight = 25, CellPadding = 4 };

        Assert.Null(resting.ToSeriesDto().CellHeight);
        Assert.Null(resting.ToSeriesDto().CellPadding);
        Assert.Equal(25, givenAtTheLibrarysRest.ToSeriesDto().CellHeight);
        Assert.Equal(4, givenAtTheLibrarysRest.ToSeriesDto().CellPadding);
        Assert.Equal(25, resting.CellHeight);
        Assert.Equal(4, resting.CellPadding);
    }

    /// <summary>Under the default style a data cell's text ends a tenth of its cell's width in from the right rule, as matplotlib's
    /// does (<c>x = bbox.x0 + bbox.width * (1 - PAD)</c>), and its rows are 16.67 px apart.</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_ADataCellsText_EndsATenthOfTheCellIn()
    {
        var svg = Plt.Create().WithTheme(Theme.MatplotlibV2).WithSize(600, 400)
            .AddSubPlot(1, 1, 1, ax => ax.Table([["30", "12"], ["80", "28"]])).Build().ToSvg().Xml();

        var texts = svg.Each("text").Where(e => e.Value is "30" or "12" or "80").ToDictionary(e => e.Value);
        // Two equal columns: the right edge of the first is where the second starts, so the second ends one cell width further on.
        double first = texts["30"].Number("x"), second = texts["12"].Number("x");
        double cellWidth = second - first;

        // The first column's right rule (the second column's left edge) stands a tenth of a cell past the end of "30".
        double rule = first + 0.1 * cellWidth;
        var lefts = svg.Each("rect").Where(e => e.Attribute("x") is not null).Select(e => e.Number("x")).ToList();

        Assert.Equal("end", (string?)texts["30"].Attribute("text-anchor"));
        Assert.Equal(10 * 1.2 * 100 / 72.0, texts["80"].Number("y") - texts["30"].Number("y"), 2);
        Assert.Contains(lefts, x => Math.Abs(x - rule) < 1);
    }

    [Theory]
    [InlineData("MatplotlibV2")]
    [InlineData("MatplotlibClassic")]
    [InlineData("Default")]
    public void TheLook_SurvivesTheThemesJson(string themeName)
    {
        var theme = themeName.AsTheme();

        Assert.Same(theme.Looks.Table, theme.ThroughDto().Looks.Table);
    }
}
