// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A Point and Figure chart: the columns are worked out by <see cref="Numerics.PointAndFigureExtensions"/> (held to
/// published examples in <c>PointAndFigureColumnTests</c>); this holds what the series does with them. Column k stands in the
/// slot from k to k + 1 of the X axis and a box of index j on the prices from j * box size to the next box, the cells tile
/// whatever the box size, an X is two crossing lines and an O an ellipse in its cell, the axes end on the first and last
/// column and the lowest and highest box, and the table has a row for each column.</summary>
public class PointAndFigureSeriesTests
{
    /// <summary>The 15 closing prices of the column tests: with a box of 1 and a reversal of 3 they draw
    /// X 20-24, O 23-19, X 20-25, O 24-16, which is 11 Xs and 14 Os.</summary>
    private static readonly double[] Fifteen =
        [20.4, 20.9, 22.3, 23.0, 21.0, 24.4, 21.0, 20.2, 18.6, 21.9, 22.0, 25.8, 22.0, 22.9, 15.1];

    private static readonly Color Up = Color.FromHex("#CC0000");
    private static readonly Color Down = Color.FromHex("#0000CC");

    private static Figure Of(double[] prices, double box, Action<PointAndFigureSeries>? configure = null, int reversal = 3) => Plt.Create()
        .WithSize(800, 500)
        .AddSubPlot(1, 1, 1, ax => ax.PointAndFigure(prices, box, reversal, s => { s.UpColor = Up; s.DownColor = Down; configure?.Invoke(s); }))
        .Build();

    private static RecordingRenderContext Drawn(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    /// <summary>The lines that make the Xs, in the order drawn, two to an X.</summary>
    private static List<(Point From, Point To)> Strokes(RecordingRenderContext drawn) => [.. drawn.OfKind("DrawLine")
        .Select(call => (dynamic)call.Args!)
        .Where(args => (Color)args.color == Up)
        .Select(args => ((Point)args.p1, (Point)args.p2))];

    /// <summary>The boxes the Os are inscribed in, in the order drawn.</summary>
    private static List<Rect> Rings(RecordingRenderContext drawn) => [.. drawn.OfKind("DrawEllipse")
        .Select(call => (dynamic)call.Args!)
        .Where(args => (Color?)args.stroke == Down)
        .Select(args => (Rect)args.bounds)];

    private static Rect Plot(Figure figure) => new ChartRenderer().ComputeLayout(figure, new SvgRenderContext()).PlotAreas[0];

    [Fact]
    public void ARisingColumnIsDrawnWithXs_AFallingOneWithOs_OneForEachBox()
    {
        var drawn = Drawn(Of(Fifteen, 1));

        Assert.Equal(11 * 2, Strokes(drawn).Count);
        Assert.Equal(14, Rings(drawn).Count);
    }

    [Fact]
    public void AnX_IsTwoLinesCrossingInTheCornersOfItsCell()
    {
        var strokes = Strokes(Drawn(Of(Fifteen, 1)));

        for (int x = 0; x < strokes.Count; x += 2)
        {
            var (down, up) = (strokes[x], strokes[x + 1]);
            Assert.Equal(down.From.X, up.From.X, 9);
            Assert.Equal(down.To.X, up.To.X, 9);
            Assert.Equal(down.From.Y, up.To.Y, 9);
            Assert.Equal(down.To.Y, up.From.Y, 9);
            Assert.True(down.To.X > down.From.X && down.To.Y > down.From.Y, "from the top-left corner to the bottom-right");
        }
    }

    [Fact]
    public void TheCellsTile_TheColumnsOneSlotApart_TheBoxesOneBoxApart()
    {
        var plot = Plot(Of(Fifteen, 1));
        var rings = Rings(Drawn(Of(Fifteen, 1)));
        double slot = plot.Width / 4, box = plot.Height / 10;     // 4 columns; the prices 16 to 26 are 10 boxes
        double inset = PointAndFigureSeries.MarkInset;

        Assert.All(rings, ring =>
        {
            Assert.Equal(slot * (1 - 2 * inset), ring.Width, 6);
            Assert.Equal(box * (1 - 2 * inset), ring.Height, 6);
        });
        // The boxes of a column are drawn from the lowest up. Column 1 (O 23-19) is the second slot, its lowest box is 19, which
        // stands on 19 to 20, and 26 is the top of the axis: six boxes down.
        var firstRing = rings[0];
        Assert.Equal(plot.X + slot * (1 + inset), firstRing.X, 6);
        Assert.Equal(plot.Y + box * (6 + inset), firstRing.Y, 6);
        // The next O of the column stands one box higher, and the next column's O (column 3) two slots to the right.
        Assert.Equal(box, firstRing.Y - rings[1].Y, 6);
        Assert.Equal(2 * slot, rings.First(ring => ring.X > firstRing.X + slot).X - firstRing.X, 6);
    }

    [Fact]
    public void TheAxes_EndOnTheFirstAndLastColumn_AndTheLowestAndHighestBox()
    {
        var figure = Of(Fifteen, 1);
        var plot = Plot(figure);
        var drawn = Drawn(figure);
        var corners = Strokes(drawn).SelectMany(s => new[] { s.From, s.To }).Concat(Rings(drawn).SelectMany(r => new[] { new Point(r.X, r.Y), new Point(r.X + r.Width, r.Y + r.Height) })).ToList();
        double slot = plot.Width / 4, box = plot.Height / 10, inset = PointAndFigureSeries.MarkInset;

        Assert.Equal(plot.X + slot * inset, corners.Min(p => p.X), 6);
        Assert.Equal(plot.X + plot.Width - slot * inset, corners.Max(p => p.X), 6);
        Assert.Equal(plot.Y + box * inset, corners.Min(p => p.Y), 6);
        Assert.Equal(plot.Y + plot.Height - box * inset, corners.Max(p => p.Y), 6);
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(10)]
    public void TheBoxSize_ChangesNoPixel_OfTheSamePrices(double box)
    {
        var reference = Drawn(Of(Fifteen, 1));
        var scaled = Drawn(Of([.. Fifteen.Select(price => price * box)], box));

        var expectedRings = Rings(reference);
        var rings = Rings(scaled);
        Assert.Equal(expectedRings.Count, rings.Count);
        for (int i = 0; i < rings.Count; i++)
        {
            Assert.Equal(expectedRings[i].X, rings[i].X, 6);
            Assert.Equal(expectedRings[i].Y, rings[i].Y, 6);
            Assert.Equal(expectedRings[i].Width, rings[i].Width, 6);
            Assert.Equal(expectedRings[i].Height, rings[i].Height, 6);
        }

        var expectedStrokes = Strokes(reference);
        var strokes = Strokes(scaled);
        Assert.Equal(expectedStrokes.Count, strokes.Count);
        for (int i = 0; i < strokes.Count; i++)
        {
            Assert.Equal(expectedStrokes[i].From.X, strokes[i].From.X, 6);
            Assert.Equal(expectedStrokes[i].From.Y, strokes[i].From.Y, 6);
            Assert.Equal(expectedStrokes[i].To.X, strokes[i].To.X, 6);
            Assert.Equal(expectedStrokes[i].To.Y, strokes[i].To.Y, 6);
        }
    }

    [Fact]
    public void ThePricesThatNeverLeaveTheirBox_DrawNothing_AndDoNotBreakTheFigure()
    {
        var drawn = Drawn(Of([20.4, 20.9, 20.5], 1));

        Assert.Empty(Strokes(drawn));
        Assert.Empty(Rings(drawn));
    }

    [Fact]
    public void TheLineWidth_IsTheStrokeOfEveryXAndO()
    {
        var drawn = Drawn(Of(Fifteen, 1, s => s.LineWidth = 3));

        Assert.All(drawn.OfKind("DrawLine").Where(call => (Color)((dynamic)call.Args!).color == Up), call => Assert.Equal(3.0, (double)((dynamic)call.Args!).thickness));
        Assert.All(drawn.OfKind("DrawEllipse").Where(call => (Color?)((dynamic)call.Args!).stroke == Down), call => Assert.Equal(3.0, (double)((dynamic)call.Args!).strokeThickness));
    }

    [Fact]
    public void TheXsAreGreenAndTheOsRed_UnlessSaidOtherwise()
    {
        var series = new PointAndFigureSeries(Fifteen, 1);

        Assert.Equal(Colors.Green, series.UpColor);
        Assert.Equal(Colors.Red, series.DownColor);
        Assert.Equal(3, series.Reversal);
    }

    [Fact]
    public void TheTable_IsOneRowPerColumn_WithItsMark_AndThePricesItBeginsAndEndsAt()
    {
        var table = new Axes().PointAndFigure(Fifteen, 1).ToDataTable()!;

        Assert.Equal(["column", "mark", "from", "to"], table.Columns.Select(c => c.Header));
        Assert.Equal([0.0, 1.0, 2.0, 3.0], table.Rows.Select(row => row[0].Number));
        Assert.Equal(["X", "O", "X", "O"], table.Rows.Select(row => row[1].Text));
        Assert.Equal([20.0, 23.0, 20.0, 24.0], table.Rows.Select(row => row[2].Number));
        Assert.Equal([24.0, 19.0, 25.0, 16.0], table.Rows.Select(row => row[3].Number));
    }

    [Fact]
    public void TheTable_SaysThePricesNotTheBoxNumbers_WhateverTheBoxSize()
    {
        double[] closes = [500, 547, 515, 511, 510];

        var table = new Axes().PointAndFigure(closes, 10).ToDataTable()!;

        Assert.Equal(["X", "O"], table.Rows.Select(row => row[1].Text));
        Assert.Equal([500.0, 530.0], table.Rows.Select(row => row[2].Number));
        Assert.Equal([540.0, 510.0], table.Rows.Select(row => row[3].Number));
    }

    [Fact]
    public void ThePrices_TheBoxAndTheReversal_SurviveTheJson_AndDrawTheSamePicture()
    {
        var figure = Of(Fifteen, 0.5, s => s.LineWidth = 2, reversal: 2);

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        var series = Assert.IsType<PointAndFigureSeries>(Assert.Single(back.SubPlots[0].Series));
        Assert.Equal(Fifteen, series.Prices);
        Assert.Equal((0.5, 2, 2.0), (series.BoxSize, series.Reversal, series.LineWidth));
        Assert.Equal((Up, Down), (series.UpColor, series.DownColor));
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Fact]
    public void TheDefaultReversalAndLineWidth_AreLeftOutOfTheJson()
    {
        string json = Of(Fifteen, 1).ToJson();

        Assert.DoesNotContain("reversal", json, StringComparison.Ordinal);
        Assert.DoesNotContain("lineWidth", json, StringComparison.Ordinal);
        Assert.Contains("boxSize", json, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ABoxThatCoversNoPrice_IsRefusedByName(double box)
    {
        Assert.Equal("boxSize", Assert.Throws<ArgumentOutOfRangeException>(() => new PointAndFigureSeries(Fifteen, box)).ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void AReversalOfNoBox_IsRefusedByName(int reversal)
    {
        Assert.Equal("reversal", Assert.Throws<ArgumentOutOfRangeException>(() => new PointAndFigureSeries(Fifteen, 1, reversal)).ParamName);
    }
}
