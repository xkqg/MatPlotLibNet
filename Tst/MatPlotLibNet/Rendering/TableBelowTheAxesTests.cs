// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Where a table stands. Under the matplotlib themes it stands where matplotlib's <c>ax.table</c> stands it by default
/// (<c>loc='bottom'</c>, lib/matplotlib/table.py, <c>Table._update_positions</c>: <c>oy = -(b + h)</c> for the codes that put it out
/// at the bottom): below the axes, its data columns sharing the width of the axes, its row labels in a column hung to the left of the
/// axes and fitted to their text (<c>auto_set_column_width(-1)</c>, a width of the text times 1 + 2 <c>Cell.PAD</c>), with no cell
/// at the corner above the row labels, and the axes above it drawn as any axes is. Under the library's own themes the table fills the
/// plot from its top, as it always has; <see cref="TableSeries.Location"/> names either for a table whatever its theme.</summary>
/// <remarks>The numbers are matplotlib 3.11.2's (python -I, <c>rcParams['figure.dpi'] = 100</c>, <c>fig, ax = plt.subplots()</c>,
/// <c>ax.table(cellText=[["1", "2"]], rowLabels=["r"], colLabels=["a", "b"])</c>, the window extent of each cell, Y measured down from the
/// top of the figure):
/// <list type="bullet">
/// <item>default style: axes x 80 w 496 top 57.6 h 369.6; cells (0, 0) x 80 w 248 top 427.2 h 16.6667, (0, 1) x 328 w 248, (1, -1) x 72.8
/// w 7.2 top 443.8667, (1, 0) x 80, (1, 1) x 328; no cell (0, -1).</item>
/// <item>classic style: axes x 80 w 496 top 48 h 384; cells top 432 and 448.6667, (1, -1) x 71.6 w 8.4 (its text 7 px wide at 12 pt).</item>
/// <item>the fidelity row's table (five rows, labelled, at 800 x 600) after <c>fig.tight_layout()</c>: the axes' bottom at 485 (default)
/// and 482 (classic), the table's natural 100 px and the pad (1.08 times <c>font.size</c>: 15 px, 18 px) below it, the row labels' left
/// edge at the pad.</item>
/// </list></remarks>
public class TableBelowTheAxesTests
{
    private const double RowHeight = 100.0 / 6;

    private static Figure Repro(Theme theme, Action<TableSeries>? configure = null, Func<FigureBuilder, FigureBuilder>? layout = null)
    {
        var builder = Plt.Create().WithSize(640, 480).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Table([["1", "2"]], s =>
            {
                s.RowHeaders = ["r"];
                s.ColumnHeaders = ["a", "b"];
                configure?.Invoke(s);
            }));
        return (layout?.Invoke(builder) ?? builder).Build();
    }

    /// <summary>The cells as drawn, top to bottom and left to right: the rectangles a row high (the figure's and the axes' own are not).</summary>
    private static List<Rect> Cells(XDocument drawn, double rowHeight = RowHeight) =>
        [.. drawn.Each("rect").Where(element => element.Attribute("height") is not null)
            .Select(element => new Rect(element.Number("x"), element.Number("y"), element.Number("width"), element.Number("height")))
            .Where(rect => Math.Abs(rect.Height - rowHeight) < 0.01)
            .OrderBy(rect => rect.Y).ThenBy(rect => rect.X)];

    private static List<(string Text, double Size)> Texts(XDocument drawn) =>
        [.. drawn.Each("text").Select(element => (element.Value, element.Number("font-size")))];

    [Theory]
    [InlineData("v2", 57.6, 369.6, 7.2)]
    [InlineData("classic", 48.0, 384.0, 8.4)]
    public void ATable_HangsBelowItsAxes_UnderTheMatplotlibThemes(string style, double axesTop, double axesHeight, double labelWidth)
    {
        var figure = Repro(style.AsTheme());
        var plot = figure.FirstPlotArea();

        // The axes stands where the subplot parameters put it (on the pixel grid the library lays a plot on): the table moves nothing.
        Assert.InRange(plot.X, 79.5, 80.5);
        Assert.InRange(plot.Width, 495.5, 496.5);
        Assert.InRange(plot.Y, axesTop - 0.5, axesTop + 0.5);
        Assert.InRange(plot.Height, axesHeight - 1, axesHeight + 1);

        var cells = Cells(figure.ToSvg().Xml());
        Assert.Equal(5, cells.Count);   // no cell at the corner above the row labels

        // The header row hangs from the bottom of the axes, its two cells sharing the axes' width.
        (new Rect(plot.X, plot.Y + plot.Height, plot.Width / 2, RowHeight)).AssertCloseTo(cells[0], 0.01);
        (new Rect(plot.X + plot.Width / 2, plot.Y + plot.Height, plot.Width / 2, RowHeight)).AssertCloseTo(cells[1], 0.01);

        // The data row under it: the row label in a column hung left of the axes, fitted to its text; the data cells under the headers.
        var label = cells[2];
        Assert.Equal(plot.X, label.X + label.Width, 0.01);
        Assert.InRange(label.Width, labelWidth - 1.5, labelWidth + 1.5);
        Assert.Equal(plot.Y + plot.Height + RowHeight, label.Y, 0.01);
        (new Rect(plot.X, plot.Y + plot.Height + RowHeight, plot.Width / 2, RowHeight)).AssertCloseTo(cells[3], 0.01);
        (new Rect(plot.X + plot.Width / 2, plot.Y + plot.Height + RowHeight, plot.Width / 2, RowHeight)).AssertCloseTo(cells[4], 0.01);
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void AnAxesThatHoldsOnlyATable_IsDrawnAsAnyAxes_UnderTheMatplotlibThemes(string style)
    {
        // matplotlib's ax.table leaves its axes as they are: the frame, and the ticks of the view (0, 1) it keeps with no data.
        var texts = Texts(Repro(style.AsTheme()).ToSvg().Xml()).Select(text => text.Text).ToList();

        Assert.Contains("0.0", texts);
        Assert.Contains("1.0", texts);
    }

    [Fact]
    public void UnderTheLibrarysOwnThemes_ATableStillFillsThePlotFromItsTop()
    {
        var figure = Repro(Theme.Default, s => s.CellHeight = RowHeight);
        var plot = figure.FirstPlotArea();
        var drawn = figure.ToSvg().Xml();

        var cells = Cells(drawn);
        Assert.Equal(6, cells.Count);   // the corner cell included
        (new Rect(plot.X, plot.Y, plot.Width / 3, RowHeight)).AssertCloseTo(cells[0], 0.01);
        Assert.DoesNotContain("1.0", Texts(drawn).Select(text => text.Text));
    }

    [Fact]
    public void ALocationGiven_IsTheTablesWhateverItsTheme()
    {
        var inPlot = Repro(Theme.MatplotlibV2, s => s.Location = TableLocation.InPlot);
        var plot = inPlot.FirstPlotArea();
        var cells = Cells(inPlot.ToSvg().Xml());
        Assert.Equal(plot.Y, cells[0].Y, 0.01);
        Assert.Equal(plot.X, cells.Min(cell => cell.X), 0.01);
        Assert.Equal(plot.X + plot.Width, cells.Max(cell => cell.X + cell.Width), 0.02);

        var below = Repro(Theme.Default, s => { s.Location = TableLocation.BelowAxes; s.CellHeight = RowHeight; });
        var belowPlot = below.FirstPlotArea();
        var belowCells = Cells(below.ToSvg().Xml());
        Assert.Equal(6, belowCells.Count);   // the library's look keeps its corner cell
        Assert.Equal(belowPlot.Y + belowPlot.Height, belowCells.Min(cell => cell.Y), 0.01);
        Assert.Equal(belowPlot.X + belowPlot.Width, belowCells.Max(cell => cell.X + cell.Width), 0.02);
    }

    /// <summary>A table of five rows under five column labels, its rows labelled, at 800 x 600: the reference of the table's fidelity row.</summary>
    private static Figure LossTable(Theme theme, Func<FigureBuilder, FigureBuilder> layout) => layout(Plt.Create().WithTheme(theme)
        .AddSubPlot(1, 1, 1, ax => ax.WithTitle("Loss table").Table(
            [["30", "12", "4", "2", "1"], ["80", "28", "11", "5", "2"], ["150", "45", "22", "12", "5"], ["210", "90", "50", "22", "11"], ["280", "120", "80", "45", "20"]],
            s => { s.ColumnHeaders = ["Freeze", "Wind", "Flood", "Quake", "Hail"]; s.RowHeaders = ["100 year", "50 year", "20 year", "10 year", "5 year"]; })))
        .Build();

    [Theory]
    [InlineData("v2", 485.0, 15.0)]
    [InlineData("classic", 482.0, 18.0)]
    public void TheTightLayout_KeepsRoomBelowAndLeftOfTheAxesForTheTable(string style, double axesBottom, double pad)
    {
        // python -I: the same table with fig.tight_layout() at 800 x 600: the axes' bottom at 485 (default) and 482 (classic), the table's
        // natural 100 px and the pad (15 px, 18 px) below it; the table's left edge, its row labels', at the pad from the figure's edge.
        var figure = LossTable(style.AsTheme(), builder => builder.TightLayout());

        var plot = figure.FirstPlotArea();
        var cells = Cells(figure.ToSvg().Xml(), RowHeight * plot.Height / MadeOn(style, 600));

        // On the pixel grid the library lays a plot on.
        Assert.InRange(plot.Y + plot.Height, axesBottom - 0.5, axesBottom + 0.5);
        Assert.InRange(cells.Min(cell => cell.X), pad - 0.5, pad + 0.5);
    }

    /// <summary>The height of the axes of a figure as its subplot parameters place it, which matplotlib makes a table on: the figure less
    /// its top and bottom margins (<c>figure.subplot.top</c> 0.88 and <c>bottom</c> 0.11 under the default style, 0.9 and 0.1 under the
    /// classic), each a whole number of pixels, as the library lays a plot on the pixel grid (369 of 480 where matplotlib's is 369.6).</summary>
    private static double MadeOn(string style, double figureHeight) =>
        figureHeight - Math.Round(figureHeight * (style == "classic" ? 0.1 : 0.12)) - Math.Round(figureHeight * (style == "classic" ? 0.1 : 0.11));

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void UnderTheTightLayout_ARowKeepsItsShareOfTheAxesTheTableWasMadeOn(string style)
    {
        // python -I, the fidelity row's table at 800 x 600 with fig.tight_layout() after ax.table: a row is Table.FONTSIZE * 1.2 points
        // as a share of the axes' height when the table is made (table.py:399-401, _approx_text_height: 462 px under the default style, 480
        // under the classic, where the subplot parameters put the axes), so it follows the axes the layout moves: the default axes 448.67
        // high, rows 16.1857 px; the classic 441.86, rows 15.3424; the layout measured the table at its natural rows (one round), so the
        // table ends above the pad (582.11 and 574.05 of 600). The rows are held to the axes the library's tight layout lays out (its top
        // margin keeps a floor of its own, so the axes is 445 px high where matplotlib's is 448.67: rows of 16.05 px).
        var figure = LossTable(style.AsTheme(), builder => builder.TightLayout());
        var plot = figure.FirstPlotArea();
        double row = RowHeight * plot.Height / MadeOn(style, 600);
        var cells = Cells(figure.ToSvg().Xml(), row);

        Assert.Equal(35, cells.Count);   // five column labels, five rows of a label and five data cells
        Assert.Equal(plot.Y + plot.Height, cells.Min(cell => cell.Y), 0.01);
        Assert.Equal(plot.Y + plot.Height + 6 * row, cells.Max(cell => cell.Y + cell.Height), 0.02);
        Assert.True(Math.Abs(row - RowHeight) > 0.1, $"the layout moved the axes, so a row is no longer {RowHeight}: {row}");
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheConstrainedLayout_KeepsRoomBelowAndLeftOfTheAxesForTheTable(string style)
    {
        // The library's constrained layout keeps its own pads past what an axes draws: the table's row labels stand the pad in from the
        // figure's edge, and its last row at the pad above the bottom edge. The rows follow the axes (below): the second round keeps room
        // for the rows the first round's axes gave, and the axes it then lays out is a little taller where the first round shrank it, so
        // the last row may end a pixel into the pad (python -I, matplotlib's own: the table's bottom at 595.44 of 600, its pad 4.17 px).
        var figure = LossTable(style.AsTheme(), builder => builder.ConstrainedLayout());
        var plot = figure.FirstPlotArea();

        var cells = Cells(figure.ToSvg().Xml(), RowHeight * plot.Height / MadeOn(style, 600));

        Assert.InRange(cells.Min(cell => cell.X), ConstrainedLayoutEngine.PadLeft - 0.5, ConstrainedLayoutEngine.PadLeft + 0.5);
        Assert.InRange(cells.Max(cell => cell.Y + cell.Height), 600 - ConstrainedLayoutEngine.PadBottom - 2, 600 - ConstrainedLayoutEngine.PadBottom + 1.5);
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void UnderTheConstrainedLayout_ARowKeepsItsShareOfTheAxesTheTableWasMadeOn(string style)
    {
        // python -I, plt.subplots(layout='constrained') and the two-row table at 640 x 480: the axes 427.33 px high under the default style,
        // rows 19.2701 = 16.6667 * 427.33 / 369.6; the classic 426.01, rows 18.4899 = 16.6667 * 426.01 / 384.
        var figure = Repro(style.AsTheme(), layout: builder => builder.ConstrainedLayout());
        var plot = figure.FirstPlotArea();
        double row = RowHeight * plot.Height / MadeOn(style, 480);
        var cells = Cells(figure.ToSvg().Xml(), row);

        Assert.Equal(5, cells.Count);
        // The library's constrained layout keeps its own pads, so its axes grows less than matplotlib's: the rows follow it all the same.
        Assert.True(row - RowHeight > 0.2, $"the axes grew past the subplot parameters, and its rows with it: {row}");
        Assert.Equal(plot.Y + plot.Height + 2 * row, cells.Max(cell => cell.Y + cell.Height), 0.02);
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheConstrainedLayout_MeasuresTheTableAtTheAxesItsFirstRoundLaidOut(string style)
    {
        // matplotlib's constrained layout runs its algorithm twice (_constrained_layout.py:108), the second time at the axes the first
        // placed: it keeps room below the axes for the rows the first round's axes gave the table (python -I, the fidelity row's table at
        // 800 x 600: its bottom at 595.44 and 596.29, where one round would have kept room for the natural 100 px). The six rows below the
        // axes set its bottom margin, and only the bottom margin moves between the rounds, so the first round's axes was as high as the
        // last one and what the room kept grew by: h1 = h2 + 100 * (h1 / h0 - 1), and its bottom stands the pad and 100 * h1 / h0 above the
        // figure's edge (one round: 100 px, the axes' bottom at 495).
        var figure = LossTable(style.AsTheme(), builder => builder.ConstrainedLayout());
        var plot = figure.FirstPlotArea();
        double madeOn = MadeOn(style, 600), natural = 6 * RowHeight;
        double firstRound = (plot.Height - natural) / (1 - natural / madeOn);

        Assert.True(Math.Abs(firstRound - madeOn) > 2, $"the first round moved the axes: {firstRound} of {madeOn}");
        Assert.Equal(600 - ConstrainedLayoutEngine.PadBottom - natural * firstRound / madeOn, plot.Y + plot.Height, 0.05);
    }

    [Fact]
    public void UnderALayout_ARowHeightGiven_IsKept()
    {
        var figure = Repro(Theme.MatplotlibV2, s => s.CellHeight = 20, builder => builder.TightLayout());

        Assert.Equal(5, Cells(figure.ToSvg().Xml(), 20).Count);
    }

    [Fact]
    public void ATableOnAnInset_KeepsItsShareOfTheAxesAsItsParentKeepsIt()
    {
        // python -I: fig, ax = plt.subplots() at 640 x 480, ax.inset_axes([0.5, 0.5, 0.4, 0.4]).table(cellText=[["1"]]), fig.tight_layout():
        // the parent's axes 426.39 px high, the inset's row 19.2275 = 16.6667 * 426.39 / 369.6: an inset stands at a share of its parent,
        // so its height, and the rows of its table, grow as the parent's do.
        var figure = Plt.Create().WithSize(640, 480).WithTheme(Theme.MatplotlibV2).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 1.0], [0.0, 1.0]).AddInset(0.5, 0.5, 0.4, 0.4, inset => inset.Table([["1"]])))
            .Build();
        var plot = figure.FirstPlotArea();

        Assert.Single(Cells(figure.ToSvg().Xml(), RowHeight * plot.Height / MadeOn("v2", 480)));
    }

    [Fact]
    public void ATallerTableThanTheMarginBelowTheAxes_StaysOnTheFigure()
    {
        // Six rows at 800 x 600 hang 100 px below an axes whose margin is 66 px: the library keeps room for what an axes draws below it,
        // as it keeps room for its tick labels.
        string[][] rows = [["1"], ["2"], ["3"], ["4"], ["5"]];
        var figure = Plt.Create().WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Table(rows, s => s.ColumnHeaders = ["a"])).Build();

        var cells = Cells(figure.ToSvg().Xml());

        Assert.Equal(6, cells.Count);
        Assert.InRange(cells.Max(cell => cell.Y + cell.Height), 500, 600);
    }

    [Theory]
    [InlineData("v2")]
    [InlineData("classic")]
    public void TheTextOfATableThatDoesNotFitItsCells_ShrinksAsMatplotlibShrinksIt(string style)
    {
        // python -I: ax.table(cellText=[["a much longer cell text here", "x", "y", "z"]]) at 640 x 480: every cell's font size is 5 pt
        // under both styles (Table._auto_set_font_size: a point at a time until the text and its pads fit the 124 px cell).
        var figure = Plt.Create().WithSize(640, 480).WithTheme(style.AsTheme())
            .AddSubPlot(1, 1, 1, ax => ax.Table([["a much longer cell text here", "x", "y", "z"]])).Build();

        var sizes = Texts(figure.ToSvg().Xml()).Where(text => text.Text is "a much longer cell text here" or "x" or "y" or "z").Select(text => text.Size).ToList();

        Assert.Equal(4, sizes.Count);
        Assert.All(sizes, size => Assert.Equal(5 * 100.0 / 72, size, 0.01));
    }

    [Fact]
    public void UnderTheLibrarysOwnThemes_TheTextOfATableKeepsItsSize()
    {
        var figure = Plt.Create().WithSize(640, 480).WithTheme(Theme.Default)
            .AddSubPlot(1, 1, 1, ax => ax.Table([["a much longer cell text here", "x", "y", "z"]])).Build();

        Assert.Equal(11, Texts(figure.ToSvg().Xml()).Single(text => text.Text == "x").Size, 0.01);
    }

    [Fact]
    public void AStemAndLeafDisplay_StaysInItsPlot_UnderTheMatplotlibThemes()
    {
        var figure = Plt.Create().WithSize(500, 350).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.StemAndLeaf([12, 15, 21, 23, 23, 38, 41, 44, 47, 52])).Build();
        var plot = figure.FirstPlotArea();

        var cells = Cells(figure.ToSvg().Xml(), rowHeight: 20);

        Assert.NotEmpty(cells);
        Assert.Equal(plot.Y, cells.Min(cell => cell.Y), 0.01);
        Assert.All(cells, cell => Assert.InRange(cell.Y + cell.Height, plot.Y, plot.Y + plot.Height + 0.01));
    }

    [Fact]
    public void BelowTheAxes_AWidthGivenTheRowHeaders_IsTheirShareAgainstTheDataColumns()
    {
        // The data columns share the plot by their own weights (1 and 3); the row headers' 1 is a quarter of the plot, left of it.
        var figure = Repro(Theme.MatplotlibV2, s => s.ColumnWidths = [1, 1, 3]);
        var plot = figure.FirstPlotArea();

        var cells = Cells(figure.ToSvg().Xml());

        (new Rect(plot.X - plot.Width / 4, plot.Y + plot.Height + RowHeight, plot.Width / 4, RowHeight)).AssertCloseTo(cells[2], 0.01);
        (new Rect(plot.X, plot.Y + plot.Height + RowHeight, plot.Width / 4, RowHeight)).AssertCloseTo(cells[3], 0.01);
        (new Rect(plot.X + plot.Width / 4, plot.Y + plot.Height + RowHeight, 3 * plot.Width / 4, RowHeight)).AssertCloseTo(cells[4], 0.01);
    }

    [Fact]
    public void ARowHeaderColumnGivenTooLittleWidth_ShrinksTheTextOfEveryCell()
    {
        // A row header column given a hundredth of a data column is no fitted column, so its text is measured with the rest.
        var figure = Repro(Theme.MatplotlibV2, s => s.ColumnWidths = [0.01, 1, 1]);

        var sizes = Texts(figure.ToSvg().Xml()).Where(text => text.Text is "r" or "a" or "1").Select(text => text.Size).ToList();

        Assert.Equal(3, sizes.Count);
        Assert.All(sizes, size => Assert.InRange(size, 1, 10 * 100.0 / 72 - 1));
        Assert.Single(sizes.Distinct());
    }

    [Fact]
    public void APadGiven_IsTheRoomOnEitherSideOfAFittedRowHeader()
    {
        static double LabelWidth(double pad) => Cells(Repro(Theme.MatplotlibV2, s => s.CellPadding = pad).ToSvg().Xml())[2].Width;

        Assert.Equal(4, LabelWidth(5) - LabelWidth(3), 0.02);
    }

    [Fact]
    public void ATableWithNoColumn_DrawsNoCell_AndKeepsNoRoom()
    {
        var figure = Plt.Create().WithSize(640, 480).WithTheme(Theme.MatplotlibV2)
            .AddSubPlot(1, 1, 1, ax => ax.Table([[]], s => s.ColumnHeaders = ["a"])).Build();

        Assert.Empty(Cells(figure.ToSvg().Xml()));
        Assert.InRange(figure.FirstPlotArea().Height, 369, 370);
    }
}
