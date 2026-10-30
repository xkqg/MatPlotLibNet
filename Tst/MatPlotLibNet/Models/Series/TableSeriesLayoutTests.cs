// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>How a table lays out its columns: every column is as wide as the others and its text starts at the left unless the
/// table says otherwise. <c>ColumnWidths</c> shares the plot's width among the displayed columns in proportion (the row-header
/// column first, when there is one), and <c>ColumnAlignments</c> anchors the text of each data column at the left, the centre or the
/// right of its cell, so text of different lengths lines up on an edge or a middle that is a place, not a font.</summary>
public class TableSeriesLayoutTests
{
    private static readonly string[][] Cells = [["a", "bbbb"], ["cc", "d"], ["eee", "ff"]];

    private static Figure Of(Action<TableSeries>? configure = null) => Plt.Create().WithSize(600, 300)
        .AddSubPlot(1, 1, 1, ax => ax.Table(Cells, configure)).Build();

    private static Rect Plot(Figure figure) => new ChartRenderer().ComputeLayout(figure, new MatPlotLibNet.Rendering.Svg.SvgRenderContext()).PlotAreas[0];

    private static RecordingRenderContext Drawn(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    /// <summary>The cells as drawn, in order: the rectangles the table strokes in its border colour (the figure's own are not).</summary>
    private static List<Rect> CellBoxes(RecordingRenderContext drawn) =>
        [.. drawn.OfKind("DrawRectangle").Select(call => (dynamic)call.Args!)
            .Where(args => (Color?)args.stroke == new Color(80, 80, 80))
            .Select(args => (Rect)args.rect)];

    private static List<(string Text, Point At, TextAlignment Alignment)> Texts(RecordingRenderContext drawn) =>
        [.. drawn.OfKind("DrawText").Select(call => (dynamic)call.Args!).Select(args => ((string)args.text, (Point)args.position, (TextAlignment)args.alignment))];

    [Fact]
    public void ByDefault_EveryColumnIsAsWideAsTheOthers_AndTheTextStartsPaddedFromTheLeft()
    {
        var figure = Of();
        var plot = Plot(figure);
        var drawn = Drawn(figure);

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
        var plot = Plot(figure);

        var boxes = CellBoxes(Drawn(figure));

        Assert.Equal(plot.Width / 4, boxes[0].Width, 9);
        Assert.Equal(3 * plot.Width / 4, boxes[1].Width, 9);
        Assert.Equal(plot.X + plot.Width / 4, boxes[1].X, 9);
        Assert.Equal(plot.Width, boxes[0].Width + boxes[1].Width, 9);
    }

    [Fact]
    public void ARowHeaderColumn_TakesTheFirstWidth_AndTheDataColumnsFollow()
    {
        var figure = Of(t => { t.RowHeaders = ["r1", "r2", "r3"]; t.ColumnWidths = [2, 1, 1]; });
        var plot = Plot(figure);

        var boxes = CellBoxes(Drawn(figure));

        Assert.Equal(9, boxes.Count);
        Assert.Equal(plot.Width / 2, boxes[0].Width, 9);   // the row header: 2 of 4
        Assert.Equal(plot.Width / 4, boxes[1].Width, 9);
        Assert.Equal(plot.Width / 4, boxes[2].Width, 9);
    }

    [Fact]
    public void AColumnWithoutAWidth_CountsForOne()
    {
        var figure = Of(t => t.ColumnWidths = [3]);
        var plot = Plot(figure);

        var boxes = CellBoxes(Drawn(figure));

        Assert.Equal(3 * plot.Width / 4, boxes[0].Width, 9);
        Assert.Equal(plot.Width / 4, boxes[1].Width, 9);
    }

    [Fact]
    public void TextAlignedToTheRight_EndsPaddedFromTheRightEdgeOfItsCell_WhateverItsLength()
    {
        var figure = Of(t => t.ColumnAlignments = [TextAlignment.Right, TextAlignment.Left]);
        var drawn = Drawn(figure);
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
        var drawn = Drawn(figure);
        var boxes = CellBoxes(drawn);

        var centred = Texts(drawn).Where(text => text.Alignment == TextAlignment.Center).ToList();

        Assert.Equal(3, centred.Count);
        Assert.All(centred, text => Assert.Equal(boxes[0].X + boxes[0].Width / 2, text.At.X, 9));
    }

    [Fact]
    public void TheHeaders_StayCentred_WhateverTheAlignmentOfTheirColumn()
    {
        var figure = Of(t => { t.ColumnHeaders = ["one", "two"]; t.ColumnAlignments = [TextAlignment.Right, TextAlignment.Right]; });

        var headers = Texts(Drawn(figure)).Where(text => text.Text is "one" or "two").ToList();

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

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        var table = Assert.IsType<TableSeries>(Assert.Single(back.SubPlots[0].Series));
        Assert.Equal([1.0, 2.0], table.ColumnWidths!);
        Assert.Equal([TextAlignment.Right, TextAlignment.Center], table.ColumnAlignments!);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Fact]
    public void ATableWithNeither_LeavesBothOutOfTheJson()
    {
        string json = Of().ToJson();

        Assert.DoesNotContain("columnAlignments", json, StringComparison.Ordinal);
        Assert.DoesNotContain("columnWidths", json, StringComparison.Ordinal);
    }
}
