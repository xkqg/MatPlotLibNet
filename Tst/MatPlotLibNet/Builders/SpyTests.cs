// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The spy plot in one call: the non-zero cells of a matrix as ONE scatter of square markers, x the column and y
/// the row, row 0 at the top, with the axes running exactly over the matrix (half a cell of margin on every side, so a
/// marker on an edge cell is drawn whole). Cells with a missing value (NaN) and cells at or under the threshold have no
/// marker.</summary>
public class SpyTests
{
    /// <summary>Three rows by five columns, a non-zero cell in each corner but the top right: (0,0) 3, (0,1) 1, (1,4) -2,
    /// (2,0) 0.5, (2,3) 7, (2,4) 9.</summary>
    private static readonly double[,] Wide =
    {
        { 3,   1, 0, 0, 0 },
        { 0,   0, 0, 0, -2 },
        { 0.5, 0, 0, 7, 9 },
    };

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static ScatterSeries SpySeries(Action<AxesBuilder> configure) => (ScatterSeries)Assert.Single(AxesOf(configure).Series);

    // ---- reading the picture ---------------------------------------------------------------------------------------

    /// <summary>The plot area in pixels.</summary>
    private readonly record struct Area(double X, double Y, double W, double H);

    /// <summary>A marker as drawn: its rectangle in pixels.</summary>
    private readonly record struct Box(double X, double Y, double W, double H)
    {
        public double CentreX => X + W / 2;

        public double CentreY => Y + H / 2;
    }

    private static string Svg(Action<AxesBuilder> configure, int width = 800, int height = 600)
    {
        var figure = Plt.Create().WithSize(width, height).AddSubPlot(1, 1, 1, configure).Build();
        return figure.ToSvg();
    }

    private static double Num(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    /// <summary>The plot area: the first background-coloured rectangle that is not the figure's.</summary>
    private static Area PlotArea(string svg)
    {
        var m = Regex.Match(svg, """<rect x="([1-9][0-9.]*)" y="([0-9.]+)" width="([0-9.]+)" height="([0-9.]+)" fill="#FFFFFF" """.TrimEnd());
        return new Area(Num(m.Groups[1].Value), Num(m.Groups[2].Value), Num(m.Groups[3].Value), Num(m.Groups[4].Value));
    }

    /// <summary>The markers, in the order they are drawn: the rectangles inside the group the series are clipped to the plot
    /// area in (a figure without interactivity has no other group around a series).</summary>
    private static Box[] Markers(string svg) =>
        [.. XDocument.Parse(svg).Descendants()
            .Where(g => g.Name.LocalName == "g" && g.Attribute("clip-path") is not null)
            .SelectMany(g => g.Descendants().Where(e => e.Name.LocalName == "rect"))
            .Select(r => new Box(
                Num((string)r.Attribute("x")!), Num((string)r.Attribute("y")!),
                Num((string)r.Attribute("width")!), Num((string)r.Attribute("height")!)))];

    /// <summary>The text of the tick labels along one axis: the labels centred on their tick are the X axis', the ones that
    /// end at it the Y axis'.</summary>
    private static string[] TickLabels(string svg, string anchor) =>
        [.. XDocument.Parse(svg).Root!.Elements()
            .Where(e => e.Name.LocalName == "text" && (string?)e.Attribute("text-anchor") == anchor)
            .Select(e => e.Value)];

    // ---- one scatter of the non-zero cells -------------------------------------------------------------------------

    /// <summary>The whole chart is ONE scatter series whose points are the non-zero cells, row by row: x the column, y the
    /// row. Nothing else is on the axes.</summary>
    [Fact]
    public void TheChart_IsOneScatterOfTheNonZeroCells_AndNothingElse()
    {
        var axes = AxesOf(ax => ax.Spy(Wide));

        var spy = Assert.IsType<ScatterSeries>(Assert.Single(axes.Series));
        Assert.Empty(axes.SecondarySeries);
        Assert.Empty(axes.XSecondarySeries);
        Assert.Equal([0.0, 1, 4, 0, 3, 4], spy.XData);
        Assert.Equal([0.0, 0, 1, 2, 2, 2], spy.YData);
    }

    /// <summary>A cell is a marker when its magnitude is above the threshold: a cell AT the threshold is not, a negative
    /// value counts by its size, and a threshold of 0 (the default) keeps everything that is not exactly zero.</summary>
    [Theory]
    [InlineData(0.0, new[] { 0, 1, 2, 3, 4 })]
    [InlineData(0.5, new[] { 2, 3, 4 })]
    [InlineData(2.0, new[] { 4 })]
    [InlineData(10.0, new int[0])]
    public void ACell_IsAMarker_WhenItsMagnitudeIsAboveTheThreshold(double threshold, int[] columns)
    {
        double[,] row = { { 0.5, -0.5, 2, -2, 2.0001, 0 } };

        var spy = SpySeries(ax => ax.Spy(row, threshold));

        Assert.Equal(columns.Select(c => (double)c), spy.XData);
        Assert.All(spy.YData, y => Assert.Equal(0.0, y));
    }

    /// <summary>The threshold defaults to 0: a tiny value is a marker, an exact zero (positive or negative) is not.</summary>
    [Fact]
    public void TheThreshold_DefaultsToZero()
    {
        double[,] row = { { 1e-300, 0, -0.0, -1e-300 } };

        var spy = SpySeries(ax => ax.Spy(row));

        Assert.Equal([0.0, 3], spy.XData);
    }

    /// <summary>A missing value (NaN) is not a marker; an infinity is a value like any other that is not zero, so it is.</summary>
    [Fact]
    public void AMissingValue_IsNoMarker_AndAnInfinityIs()
    {
        double[,] row = { { double.NaN, 1, double.PositiveInfinity, double.NegativeInfinity, 0 } };

        var spy = SpySeries(ax => ax.Spy(row));

        Assert.Equal([1.0, 2, 3], spy.XData);
        Assert.All(spy.XData.Concat(spy.YData), v => Assert.True(double.IsFinite(v)));
    }

    /// <summary>The marker of a missing value takes no place in the picture, and the other markers are where they are
    /// without it.</summary>
    [Fact]
    public void AMissingValue_LeavesTheOtherMarkersWhereTheyAre()
    {
        double[,] withHole = (double[,])Wide.Clone();
        withHole[0, 1] = double.NaN;

        string svg = Svg(ax => ax.Spy(withHole));
        string without = Svg(ax => ax.Spy(Wide));

        Assert.DoesNotContain("NaN", svg);
        var all = Markers(without);
        Assert.Equal([.. all.Where((_, i) => i != 1)], Markers(svg));
    }

    // ---- the axes run over the matrix ------------------------------------------------------------------------------

    /// <summary>The axes run from half a cell before the first column to half a cell after the last, and the Y axis runs
    /// backwards: row 0 at the top, the last row at the bottom.</summary>
    [Fact]
    public void TheAxes_RunOverTheMatrix_WithHalfACellOfMargin_RowZeroOnTop()
    {
        var axes = AxesOf(ax => ax.Spy(Wide));

        Assert.Equal(-0.5, axes.XAxis.Min);
        Assert.Equal(4.5, axes.XAxis.Max);
        Assert.Equal(2.5, axes.YAxis.Min);
        Assert.Equal(-0.5, axes.YAxis.Max);
    }

    /// <summary>On the picture: the centre of the marker of cell (row, column) stands at the middle of that cell of the
    /// plot area, the plot being divided into the matrix' columns and rows. Row 0 is the top row, column 0 the left one,
    /// and the markers of the corner cells lie wholly inside the plot area (half a cell of margin).</summary>
    [Fact]
    public void AMarker_StandsAtTheCentreOfItsCell_RowZeroOnTop_AndWholeInsideThePlot()
    {
        string svg = Svg(ax => ax.Spy(Wide));

        var plot = PlotArea(svg);
        var markers = Markers(svg);
        (int Row, int Column)[] cells = [(0, 0), (0, 1), (1, 4), (2, 0), (2, 3), (2, 4)];
        Assert.Equal(cells.Length, markers.Length);
        for (int k = 0; k < cells.Length; k++)
        {
            var (row, column) = cells[k];
            Assert.Equal(plot.X + (column + 0.5) * plot.W / 5, markers[k].CentreX, 6);
            Assert.Equal(plot.Y + (row + 0.5) * plot.H / 3, markers[k].CentreY, 6);
            Assert.True(markers[k].X >= plot.X && markers[k].X + markers[k].W <= plot.X + plot.W, $"marker {k} leaves the plot sideways");
            Assert.True(markers[k].Y >= plot.Y && markers[k].Y + markers[k].H <= plot.Y + plot.H, $"marker {k} leaves the plot vertically");
        }
    }

    /// <summary>Measured: neighbouring markers are one plot width over the number of columns apart across, and one plot
    /// height over the number of rows apart down, at any figure size. The two spacings are equal only where the plot area
    /// has the matrix' own proportions: the verb sets no aspect, so the grid of markers is square at the figure size that
    /// makes it so.</summary>
    [Theory]
    [InlineData(500, 350)]
    [InlineData(800, 600)]
    public void TheMarkers_ArePlacedOnTheGridOfTheMatrix_AtAnyFigureSize(int width, int height)
    {
        double[,] full = new double[4, 6];
        for (int r = 0; r < 4; r++) for (int c = 0; c < 6; c++) full[r, c] = 1;

        string svg = Svg(ax => ax.Spy(full), width, height);

        var plot = PlotArea(svg);
        var markers = Markers(svg);
        Assert.Equal(24, markers.Length);
        Assert.Equal(plot.W / 6, markers[1].CentreX - markers[0].CentreX, 6);
        Assert.Equal(plot.H / 4, markers[6].CentreY - markers[0].CentreY, 6);
    }

    /// <summary>The markers are squares of the scatter's own size: a size in points, not in cells, so a bigger matrix has
    /// the same markers on a finer grid (lower <see cref="ScatterSeries.MarkerSize"/> for a large matrix).</summary>
    [Fact]
    public void TheMarkers_AreSquares_OfTheScattersOwnSize_WhateverTheMatrixSize()
    {
        double[,] small = { { 1 } };
        double[,] large = new double[40, 40];
        large[7, 9] = 1;

        var spy = SpySeries(ax => ax.Spy(small));
        var smallBox = Assert.Single(Markers(Svg(ax => ax.Spy(small))));
        var largeBox = Assert.Single(Markers(Svg(ax => ax.Spy(large))));

        Assert.Equal(MarkerStyle.Square, spy.Marker);
        Assert.Equal(new ScatterSeries([], []).MarkerSize, spy.MarkerSize);
        Assert.Equal(smallBox.W, smallBox.H, 9);
        Assert.Equal(smallBox.W, largeBox.W, 9);
        Assert.Equal(largeBox.W, largeBox.H, 9);
    }

    // ---- the ticks name cells -----------------------------------------------------------------------------------------

    /// <summary>The ticks are whole numbers: a column or a row has a number, so a tick between two cells, or on the half
    /// cell of margin before the first one (where the limits put the automatic ticks of a small matrix), names nothing. The
    /// step is the smallest of 1, 2, 5, 10, 20, 50 ... that leaves at most nine intervals across the matrix, so a small
    /// matrix is ticked at every cell and a large one at round numbers.</summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(18, 2)]
    [InlineData(19, 5)]
    [InlineData(45, 5)]
    [InlineData(46, 10)]
    [InlineData(90, 10)]
    [InlineData(91, 20)]
    [InlineData(100, 20)]
    [InlineData(181, 50)]
    [InlineData(1000, 200)]
    public void TheTicks_AreWholeCellNumbers_WithAtMostNineIntervalsAcross(int count, int step)
    {
        var across = AxesOf(ax => ax.Spy(new double[2, count]));
        var down = AxesOf(ax => ax.Spy(new double[count, 2]));

        double[] expected = [.. Enumerable.Range(0, (count - 1) / step + 1).Select(k => (double)(k * step))];
        Assert.Equal(expected, across.XAxis.TickLocator!.Locate(across.XAxis.Min!.Value, across.XAxis.Max!.Value));
        Assert.Equal(expected, down.YAxis.TickLocator!.Locate(down.YAxis.Max!.Value, down.YAxis.Min!.Value));
    }

    /// <summary>On the picture: a 3 x 5 matrix is ticked 0 to 4 across and 0 to 2 down, nothing in between and nothing
    /// before the first cell.</summary>
    [Fact]
    public void ASmallMatrix_IsTickedAtEveryCell_OnThePicture()
    {
        string svg = Svg(ax => ax.Spy(Wide));

        Assert.Equal(["0", "1", "2", "3", "4"], TickLabels(svg, "middle"));
        Assert.Equal(["0", "1", "2"], TickLabels(svg, "end"));
    }

    /// <summary>A matrix with no cell above the threshold draws no marker and still shows its extent: the axes run over the
    /// whole matrix, and nothing of the picture is NaN.</summary>
    [Fact]
    public void AMatrixWithoutACellAboveTheThreshold_DrawsNoMarker_ButKeepsItsExtent()
    {
        double[,] zeros = new double[3, 4];

        var axes = AxesOf(ax => ax.Spy(zeros));
        string svg = Svg(ax => ax.Spy(zeros));

        Assert.Empty(((ScatterSeries)axes.Series[0]).XData);
        Assert.Equal(-0.5, axes.XAxis.Min);
        Assert.Equal(3.5, axes.XAxis.Max);
        Assert.Equal(2.5, axes.YAxis.Min);
        Assert.Equal(-0.5, axes.YAxis.Max);
        Assert.Empty(Markers(svg));
        Assert.DoesNotContain("NaN", svg);
    }

    /// <summary>A matrix of one cell is a marker at the middle of the plot, with half a cell on every side.</summary>
    [Fact]
    public void AMatrixOfOneCell_IsOneMarkerAtTheMiddleOfThePlot()
    {
        double[,] one = { { 5 } };

        string svg = Svg(ax => ax.Spy(one));

        var plot = PlotArea(svg);
        var marker = Assert.Single(Markers(svg));
        Assert.Equal(plot.X + plot.W / 2, marker.CentreX, 6);
        Assert.Equal(plot.Y + plot.H / 2, marker.CentreY, 6);
    }

    // ---- the callback -----------------------------------------------------------------------------------------------

    /// <summary>The callback styles the one scatter, once, after the verb has built it (its points are there and its marker
    /// is a square), so what it sets wins.</summary>
    [Fact]
    public void TheCallback_StylesTheOneScatter_OnceAfterItIsBuilt()
    {
        int calls = 0, pointsSeen = 0;
        MarkerStyle markerSeen = MarkerStyle.None;
        var color = Color.FromHex("#AA3366");

        var spy = SpySeries(ax => ax.Spy(Wide, configure: s =>
        {
            calls++;
            pointsSeen = s.XData.Length;
            markerSeen = s.Marker;
            s.Color = color;
            s.MarkerSize = 20;
            s.Label = "non-zero";
        }));

        Assert.Equal(1, calls);
        Assert.Equal(6, pointsSeen);
        Assert.Equal(MarkerStyle.Square, markerSeen);
        Assert.Equal(color, spy.Color);
        Assert.Equal(20, spy.MarkerSize);
        Assert.Equal("non-zero", spy.Label);
    }

    /// <summary>What the callback sets wins over what the verb chose, the marker included.</summary>
    [Fact]
    public void TheCallback_CanReplaceTheMarker()
    {
        var spy = SpySeries(ax => ax.Spy(Wide, configure: s => s.Marker = MarkerStyle.Circle));

        Assert.Equal(MarkerStyle.Circle, spy.Marker);
    }

    // ---- JSON and the hand recipe ----------------------------------------------------------------------------------

    /// <summary>Written to JSON the chart is its parts (the scatter of the non-zero cells and the limits); read back it draws
    /// the same picture, with a missing value in the matrix and with a matrix that has no marker at all.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadBackFromJson_DrawsTheSameSpy(bool empty)
    {
        double[,] matrix = empty ? new double[3, 4] : (double[,])Wide.Clone();
        if (!empty) matrix[2, 3] = double.NaN;
        var figure = Plt.Create().WithSize(600, 400).AddSubPlot(1, 1, 1, ax => ax.Spy(matrix)).Build();

        var back = ChartServices.Serializer.FromJson(figure.ToJson());

        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    /// <summary>The verb draws exactly the picture of the hand chain of public building blocks it replaces (a scatter of
    /// square markers on the non-zero cells, the limits half a cell wider than the matrix, the Y axis reversed, whole-number
    /// ticks), with and without a missing value.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws(bool missing)
    {
        string byVerb = ComposedChartShapes.Named("spy").Build(missing).ToSvg();
        string byHand = ComposedChartShapes.SpyByHand(missing).ToSvg();

        Assert.Equal(byHand, byVerb);
    }

    // ---- what is refused -------------------------------------------------------------------------------------------

    /// <summary>Every refusal names the parameter: no axes, no matrix, a matrix without a cell, a threshold that is not a
    /// finite number of 0 or more.</summary>
    [Fact]
    public void EveryRefusal_NamesTheParameter()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).Spy(Wide)).ParamName);
        Assert.Equal("matrix", Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.Spy(null!))).ParamName);
        Assert.Equal("matrix", Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.Spy(new double[0, 0]))).ParamName);
        Assert.Equal("matrix", Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.Spy(new double[0, 3]))).ParamName);
        Assert.Equal("matrix", Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.Spy(new double[3, 0]))).ParamName);
    }

    /// <summary>A threshold must be a finite number of 0 or more: a negative one would make every cell a marker, zeros
    /// included, and NaN would make none.</summary>
    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AThreshold_ThatIsNotAFiniteNumberOfZeroOrMore_IsRefused(double threshold)
    {
        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => AxesOf(ax => ax.Spy(Wide, threshold)));

        Assert.Equal("threshold", refusal.ParamName);
    }

    /// <summary>A refused call leaves the axes as it was: no series, no limits.</summary>
    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var axes = AxesOf(ax =>
        {
            Assert.Throws<ArgumentException>(() => ax.Spy(new double[0, 3]));
            Assert.Throws<ArgumentOutOfRangeException>(() => ax.Spy(Wide, -1));
        });

        Assert.Empty(axes.Series);
        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
    }
}
