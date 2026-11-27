// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>How a table lays out its columns: every column is as wide as the others and its text starts at the left unless the
/// table says otherwise. <c>ColumnWidths</c> shares the plot's width among the displayed columns in proportion (the row-header
/// column first, when there is one), and <c>ColumnAlignments</c> anchors the text of each data column at the left, the centre or the
/// right of its cell, so text of different lengths lines up on an edge or a middle that is a place, not a font. The tables here are
/// drawn under the library's own theme, whose cells are ruled dark grey and whose text stands 4 px in from its edge; what the
/// matplotlib themes make of the cells (the figure's default theme among them) is <see cref="TableLook"/>'s, held by
/// <c>TableLookTests</c>.</summary>
public class TableSeriesLayoutTests
{
    private static readonly string[][] Cells = [["a", "bbbb"], ["cc", "d"], ["eee", "ff"]];

    private static Figure Of(Action<TableSeries>? configure = null) => Plt.Create().WithSize(600, 300).WithTheme(Theme.Default)
        .AddSubPlot(1, 1, 1, ax => ax.Table(Cells, configure)).Build();

    /// <summary>The cells as drawn, in order: the rectangles the table strokes in its border colour (the figure's own are not).</summary>
    private static List<Rect> CellBoxes(RecordingRenderContext drawn) =>
        [.. drawn.Of<DrawRectangleCall>()
            .Where(args => args.Stroke == new Color(80, 80, 80))
            .Select(args => args.Rect)];

    private static List<(string Text, Point At, TextAlignment Alignment)> Texts(RecordingRenderContext drawn) =>
        [.. drawn.Of<DrawTextCall>().Select(args => (args.Text, args.Position, args.Alignment))];

    /// <summary>The widths of the rules round the cells as drawn, in the table's border colour.</summary>
    private static double[] RuleWidths(Figure figure) => [.. figure.Drawn().Of<DrawRectangleCall>()
        .Where(args => args.Stroke == new Color(80, 80, 80)).Select(args => args.StrokeThickness)];

    [Fact]
    public void ARuleRoundACell_IsOnePixelWide_UnderTheLibrarysOwnThemes()
    {
        foreach (var theme in new[] { Theme.Default, Theme.Dark })
        {
            var figure = Plt.Create().WithSize(600, 300).WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Table(Cells)).Build();
            Assert.All(RuleWidths(figure), width => Assert.Equal(1.0, width));
        }
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void ARuleRoundACell_IsOnePointWide_UnderTheMatplotlibThemes(string style)
    {
        // matplotlib 3.11.2: table.Cell is a Rectangle that takes its edge width from patch.linewidth, 1.0 pt in both styles (printed:
        // matplotlib.rcParams['patch.linewidth'] == 1.0 after plt.style.use('classic') and under the default style), 100 / 72 pixels at 100 dpi.
        var theme = style.AsTheme();
        var figure = Plt.Create().WithSize(600, 300).WithTheme(theme)
            .AddSubPlot(1, 1, 1, ax => ax.Table(Cells, t => t.BorderColor = new Color(80, 80, 80))).Build();

        Assert.All(RuleWidths(figure), width => Assert.Equal(100.0 / 72, width, 9));
    }

    [Fact]
    public void ByDefault_EveryColumnIsAsWideAsTheOthers_AndTheTextStartsPaddedFromTheLeft()
    {
        var figure = Of();
        var plot = figure.FirstPlotArea();
        var drawn = figure.Drawn();

        var boxes = CellBoxes(drawn);
        Assert.Equal(6, boxes.Count);
        Assert.All(boxes, box => Assert.Equal(plot.Width / 2, box.Width, 9));
        Assert.All(Texts(drawn), text => Assert.Equal(TextAlignment.Left, text.Alignment));
        Assert.Equal(boxes[0].X + 4, Texts(drawn)[0].At.X, 9);
    }

    [Fact]
    public void ColumnWidths_ShareThePlotWidth_InTheirProportions()
    {
        var figure = Of(t => t.ColumnWidths = [1, 3]);
        var plot = figure.FirstPlotArea();

        var boxes = CellBoxes(figure.Drawn());

        Assert.Equal(plot.Width / 4, boxes[0].Width, 9);
        Assert.Equal(3 * plot.Width / 4, boxes[1].Width, 9);
        Assert.Equal(plot.X + plot.Width / 4, boxes[1].X, 9);
        Assert.Equal(plot.Width, boxes[0].Width + boxes[1].Width, 9);
    }

    [Fact]
    public void ARowHeaderColumn_TakesTheFirstWidth_AndTheDataColumnsFollow()
    {
        var figure = Of(t => { t.RowHeaders = ["r1", "r2", "r3"]; t.ColumnWidths = [2, 1, 1]; });
        var plot = figure.FirstPlotArea();

        var boxes = CellBoxes(figure.Drawn());

        Assert.Equal(9, boxes.Count);
        Assert.Equal(plot.Width / 2, boxes[0].Width, 9);   // the row header: 2 of 4
        Assert.Equal(plot.Width / 4, boxes[1].Width, 9);
        Assert.Equal(plot.Width / 4, boxes[2].Width, 9);
    }

    [Fact]
    public void AColumnWithoutAWidth_CountsForOne()
    {
        var figure = Of(t => t.ColumnWidths = [3]);
        var plot = figure.FirstPlotArea();

        var boxes = CellBoxes(figure.Drawn());

        Assert.Equal(3 * plot.Width / 4, boxes[0].Width, 9);
        Assert.Equal(plot.Width / 4, boxes[1].Width, 9);
    }

    [Fact]
    public void TextAlignedToTheRight_EndsPaddedFromTheRightEdgeOfItsCell_WhateverItsLength()
    {
        var figure = Of(t => t.ColumnAlignments = [TextAlignment.Right, TextAlignment.Left]);
        var drawn = figure.Drawn();
        var boxes = CellBoxes(drawn);

        var firstColumn = Texts(drawn).Where(text => Math.Abs(text.At.X - (boxes[0].X + boxes[0].Width - 4)) < 1e-9).ToList();

        Assert.Equal(["a", "cc", "eee"], firstColumn.Select(text => text.Text));
        Assert.All(firstColumn, text => Assert.Equal(TextAlignment.Right, text.Alignment));
        // The second column is still left aligned, at its own padded left edge.
        Assert.Contains(Texts(drawn), text => text.Text == "bbbb" && text.Alignment == TextAlignment.Left && Math.Abs(text.At.X - (boxes[1].X + 4)) < 1e-9);
    }

    [Fact]
    public void TextCentred_StandsOnTheMiddleOfItsCell()
    {
        var figure = Of(t => t.ColumnAlignments = [TextAlignment.Center]);
        var drawn = figure.Drawn();
        var boxes = CellBoxes(drawn);

        var centred = Texts(drawn).Where(text => text.Alignment == TextAlignment.Center).ToList();

        Assert.Equal(3, centred.Count);
        Assert.All(centred, text => Assert.Equal(boxes[0].X + boxes[0].Width / 2, text.At.X, 9));
    }

    [Fact]
    public void TheHeaders_StayCentred_WhateverTheAlignmentOfTheirColumn()
    {
        var figure = Of(t => { t.ColumnHeaders = ["one", "two"]; t.ColumnAlignments = [TextAlignment.Right, TextAlignment.Right]; });

        var headers = Texts(figure.Drawn()).Where(text => text.Text is "one" or "two").ToList();

        Assert.Equal(2, headers.Count);
        Assert.All(headers, header => Assert.Equal(TextAlignment.Center, header.Alignment));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AWidthThatIsNotAFiniteNumberAboveZero_IsRefusedByName(double width)
    {
        var table = new TableSeries(Cells);

        Assert.Equal("ColumnWidths", Assert.Throws<ArgumentOutOfRangeException>(() => table.ColumnWidths = [1, width]).ParamName);
        Assert.Null(table.ColumnWidths);
    }

    [Fact]
    public void TheSettings_SurviveTheJson_AndDrawTheSamePicture()
    {
        var figure = Of(t => { t.ColumnWidths = [1, 2]; t.ColumnAlignments = [TextAlignment.Right, TextAlignment.Center]; });

        var back = figure.RoundTripped();

        var table = Assert.IsType<TableSeries>(Assert.Single(back.SubPlots[0].Series));
        Assert.Equal([1.0, 2.0], table.ColumnWidths!);
        Assert.Equal([TextAlignment.Right, TextAlignment.Center], table.ColumnAlignments!);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("ATableWithNeither_LeavesBothOutOfTheJson", () => Of().ToJson()) { Absent = ["columnAlignments", "columnWidths"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
