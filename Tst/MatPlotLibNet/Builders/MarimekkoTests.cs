// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The Marimekko chart in one call: one area series for each segment, drawn as one rectangle in each column, the
/// width of a column its share of the grand total and the height of a cell its share of its column's total, so the chart
/// fills the plot and every cell's area is its share of the grand total. Measured before the verb was written (the plan's
/// kill rule): an area over the column edges in steps draws exact rectangles, but joins the columns of a segment into one
/// polygon with a stroke along the column edges, and a missing cell takes its left neighbour with it; two points per
/// column with a missing point between two columns (the NaN break every renderer honours) draws one exact rectangle per
/// cell and loses only the missing one. The tests hold the verb to that.</summary>
public class MarimekkoTests
{
    private static readonly string[] Columns = ["Retail", "Online", "Wholesale"];

    // Three segments over three columns whose totals differ (45, 90 and 30 of 165), so a width that were not the column's
    // share would show.
    private static readonly BarGroup[] Segments =
    [
        new("A", [10, 50, 6]),
        new("B", [20, 10, 9]),
        new("C", [15, 30, 15]),
    ];

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static Axes Chart(IReadOnlyList<BarGroup> segments, Action<AreaSeries>? configure = null) =>
        AxesOf(ax => ax.Marimekko(Columns, segments, configure));

    private static AreaSeries[] Areas(Axes axes) => [.. axes.Series.Cast<AreaSeries>()];

    /// <summary>One cell of a segment: its column's edges and its own bottom and top, in the chart's units (percent).</summary>
    private readonly record struct Cell(double Left, double Right, double Bottom, double Top);

    /// <summary>The cells of <paramref name="area"/>, column by column: two points per column, a missing point between two
    /// columns.</summary>
    private static Cell[] CellsOf(AreaSeries area) =>
        [.. Enumerable.Range(0, (area.XData.Length + 1) / 3).Select(c => new Cell(
            area.XData[3 * c], area.XData[3 * c + 1], area.YData2![3 * c], area.YData[3 * c]))];

    /// <summary>What the cells must be, worked out here from the sizes: the column edges are the running sums of the column
    /// totals over the grand total, the cells of a column stack in the order of the segments, each as tall as its share of
    /// the column total; a missing size adds nothing and has no cell (NaN).</summary>
    private static Cell[][] Expected(IReadOnlyList<BarGroup> segments, int columnCount)
    {
        double[] totals = [.. Enumerable.Range(0, columnCount).Select(c => segments.Select(s => s.Values[c]).Where(double.IsFinite).Sum())];
        double grand = totals.Sum();
        double[] edges = new double[columnCount + 1];
        for (int c = 0; c < columnCount; c++) edges[c + 1] = edges[c] + totals[c] / grand * 100;

        var cells = segments.Select(_ => new Cell[columnCount]).ToArray();
        for (int c = 0; c < columnCount; c++)
        {
            double floor = 0;
            for (int r = 0; r < segments.Count; r++)
            {
                double size = segments[r].Values[c];
                bool drawn = double.IsFinite(size) && size > 0;
                double height = drawn ? size / totals[c] * 100 : double.NaN;
                cells[r][c] = new Cell(edges[c], edges[c + 1], drawn ? floor : double.NaN, drawn ? floor + height : double.NaN);
                if (drawn) floor += height;
            }
        }

        return cells;
    }

    private static void AssertClose(double expected, double actual, string what)
    {
        Assert.True(double.IsNaN(expected) ? double.IsNaN(actual) : Math.Abs(expected - actual) <= 1e-9,
            $"{what}: expected {expected.ToString("R", CultureInfo.InvariantCulture)}, got {actual.ToString("R", CultureInfo.InvariantCulture)}");
    }

    // ---- reading the picture ---------------------------------------------------------------------------------------

    private static double Num(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    private readonly record struct Area(double X, double Y, double W, double H);

    private readonly record struct Vertex(double X, double Y);

    private static string Svg(Action<AxesBuilder> configure, Theme? theme = null)
    {
        var builder = Plt.Create().WithSize(800, 500);
        if (theme is not null) builder = builder.WithTheme(theme);
        return builder.AddSubPlot(1, 1, 1, configure).Build().ToSvg();
    }

    private static IEnumerable<XElement> Named(XContainer root, string name) =>
        root.Descendants().Where(e => e.Name.LocalName == name);

    private static List<XElement> SeriesGroups(string svg) =>
        [.. Named(XDocument.Parse(svg), "g").Where(g => (string?)g.Attribute("class") == "series")];

    private static Area PlotArea(string svg)
    {
        var rect = XDocument.Parse(svg).Root!.Elements().Where(e => e.Name.LocalName == "rect").Skip(1).First();
        return new Area(Num((string)rect.Attribute("x")!), Num((string)rect.Attribute("y")!),
            Num((string)rect.Attribute("width")!), Num((string)rect.Attribute("height")!));
    }

    private static List<Vertex[]> Shapes(XElement group, string element) =>
        [.. Named(group, element).Select(e => ((string)e.Attribute("points")!)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split(','))
            .Select(p => new Vertex(Num(p[0]), Num(p[1])))
            .ToArray())];

    private static List<string> FigureTexts(string svg) =>
        [.. XDocument.Parse(svg).Root!.Elements().Where(e => e.Name.LocalName == "text").Select(e => e.Value)];

    // ---- one area series per segment ---------------------------------------------------------------------------------

    /// <summary>The whole chart is one area series for each segment in the order given, each named by its segment: nothing on
    /// a second axis, no overlay, no other series.</summary>
    [Fact]
    public void TheChart_IsOneAreaSeriesPerSegment_InOrder_EachNamedByItsSegment()
    {
        var axes = Chart(Segments);

        var areas = Areas(axes);
        Assert.Equal(3, areas.Length);
        Assert.Equal(["A", "B", "C"], areas.Select(a => a.Label));
        Assert.Empty(axes.SecondarySeries);
        Assert.Empty(axes.XSecondarySeries);
        Assert.Empty(axes.ReferenceLines);
    }

    /// <summary>Every segment holds arrays of its own, so editing one segment's data cannot move another's.</summary>
    [Fact]
    public void EverySegment_HoldsArraysOfItsOwn()
    {
        var areas = Areas(Chart(Segments));

        Assert.NotSame(areas[0].XData, areas[1].XData);
        Assert.NotSame(areas[1].XData, areas[2].XData);
        Assert.All(areas, area => Assert.NotSame(area.YData, area.YData2));
    }

    // ---- the geometry: one rectangle per cell ------------------------------------------------------------------------------

    /// <summary>Each segment is two points per column, at the column's left and right edge with the cell's bottom and top at
    /// both, and a missing point (NaN, in x, top and bottom) between two columns, so no column is joined to the next.</summary>
    [Fact]
    public void EverySegment_IsTwoPointsPerColumn_WithAMissingPointBetweenTwoColumns()
    {
        foreach (var area in Areas(Chart(Segments)))
        {
            Assert.Equal(3 * Columns.Length - 1, area.XData.Length);
            for (int c = 0; c < Columns.Length; c++)
            {
                Assert.Equal(area.YData[3 * c], area.YData[3 * c + 1]);
                Assert.Equal(area.YData2![3 * c], area.YData2[3 * c + 1]);
                if (c > 0)
                {
                    Assert.True(double.IsNaN(area.XData[3 * c - 1]));
                    Assert.True(double.IsNaN(area.YData[3 * c - 1]));
                    Assert.True(double.IsNaN(area.YData2[3 * c - 1]));
                }
            }
        }
    }

    /// <summary>The width of a column is its share of the grand total: the edges run from exactly 0 to exactly 100 and each
    /// column is as wide as its total over the grand total, in percent (45, 90 and 30 of 165).</summary>
    [Fact]
    public void TheColumnWidths_AreProportionalToTheColumnTotals()
    {
        var cells = CellsOf(Areas(Chart(Segments))[0]);

        Assert.Equal(0.0, cells[0].Left);
        Assert.Equal(100.0, cells[^1].Right);
        double[] totals = [45, 90, 30];
        for (int c = 0; c < Columns.Length; c++)
        {
            AssertClose(totals[c] / 165 * 100, cells[c].Right - cells[c].Left, $"width of column {c}");
            if (c > 0) Assert.Equal(cells[c - 1].Right, cells[c].Left);
        }
    }

    /// <summary>The cells of a column stack in the order of the segments, the first at the bottom, each standing exactly on the
    /// one below it and as tall as its share of its column's total, in percent; the column runs from exactly 0 to exactly
    /// 100.</summary>
    [Fact]
    public void TheCells_StackToOneHundredPercentPerColumn_EachAsTallAsItsShareOfTheColumn()
    {
        var cells = Areas(Chart(Segments)).Select(CellsOf).ToArray();
        var expected = Expected(Segments, Columns.Length);

        for (int c = 0; c < Columns.Length; c++)
        {
            Assert.Equal(0.0, cells[0][c].Bottom);
            Assert.Equal(100.0, cells[^1][c].Top);
            for (int r = 0; r < Segments.Length; r++)
            {
                AssertClose(expected[r][c].Top - expected[r][c].Bottom, cells[r][c].Top - cells[r][c].Bottom, $"height of {r} in {c}");
                if (r > 0) Assert.Equal(cells[r - 1][c].Top, cells[r][c].Bottom);
            }
        }
    }

    /// <summary>The kill rule of the plan, held in the picture: every cell is ONE polygon whose four corners are the exact
    /// corners of its rectangle (within 1e-9 px), no polygon reaches into a second column, and no stroke runs along a column
    /// edge (the area outlines its top only, one horizontal line per cell).</summary>
    [Fact]
    public void EveryCell_IsDrawnAsOneExactRectangle_AndNoColumnIsJoinedToTheNext()
    {
        string svg = Svg(ax => ax.Marimekko(Columns, Segments));
        var plot = PlotArea(svg);
        double Px(double x) => plot.X + x / 100 * plot.W;
        double Py(double y) => plot.Y + plot.H - y / 100 * plot.H;
        var expected = Expected(Segments, Columns.Length);

        var groups = SeriesGroups(svg);
        Assert.Equal(3, groups.Count);
        for (int r = 0; r < groups.Count; r++)
        {
            var polygons = Shapes(groups[r], "polygon");
            Assert.Equal(Columns.Length, polygons.Count);
            for (int c = 0; c < Columns.Length; c++)
            {
                var cell = expected[r][c];
                Vertex[] corners = [new(Px(cell.Left), Py(cell.Top)), new(Px(cell.Right), Py(cell.Top)),
                    new(Px(cell.Right), Py(cell.Bottom)), new(Px(cell.Left), Py(cell.Bottom))];
                Assert.Equal(4, polygons[c].Length);
                for (int k = 0; k < 4; k++)
                {
                    AssertClose(corners[k].X, polygons[c][k].X, $"segment {r}, column {c}, corner {k}, x");
                    AssertClose(corners[k].Y, polygons[c][k].Y, $"segment {r}, column {c}, corner {k}, y");
                }
            }

            Assert.All(Shapes(groups[r], "polyline"), line =>
            {
                Assert.Equal(2, line.Length);
                Assert.Equal(line[0].Y, line[1].Y);
            });
        }
    }

    /// <summary>The chart fills the plot: the columns run from the left edge of the plot area to its right edge and every
    /// column from its bottom to its top, because the widths and the heights are shares of a whole (the verb sets the Y
    /// margin to 0; the X edges are the area's own sticky edges).</summary>
    [Fact]
    public void TheChart_FillsThePlotArea()
    {
        string svg = Svg(ax => ax.Marimekko(Columns, Segments));
        var plot = PlotArea(svg);

        var vertices = SeriesGroups(svg).SelectMany(g => Shapes(g, "polygon")).SelectMany(p => p).ToList();
        AssertClose(plot.X, vertices.Min(v => v.X), "left");
        AssertClose(plot.X + plot.W, vertices.Max(v => v.X), "right");
        AssertClose(plot.Y, vertices.Min(v => v.Y), "top");
        AssertClose(plot.Y + plot.H, vertices.Max(v => v.Y), "bottom");
    }

    // ---- a missing size and a size of 0 ------------------------------------------------------------------------------------

    private static readonly BarGroup[] WithAMissingSize =
    [
        new("A", [10, 50, 6]),
        new("B", [20, double.NaN, 9]),
        new("C", [15, 30, 15]),
    ];

    /// <summary>A missing size (NaN) is an empty cell: it has no cell of its own (NaN at both its points), it adds nothing to
    /// its column's total (so the column's width and the other cells' shares are taken over the sizes that are there), and
    /// the segment above it stands on the one below it.</summary>
    [Fact]
    public void AMissingSize_IsAnEmptyCell_ThatAddsNothingToItsColumn()
    {
        var cells = Areas(Chart(WithAMissingSize)).Select(CellsOf).ToArray();
        var expected = Expected(WithAMissingSize, Columns.Length);

        Assert.True(double.IsNaN(cells[1][1].Top));
        Assert.True(double.IsNaN(cells[1][1].Bottom));
        Assert.Equal(cells[0][1].Top, cells[2][1].Bottom);
        Assert.Equal(100.0, cells[2][1].Top);
        AssertClose(80.0 / 155 * 100, cells[0][1].Right - cells[0][1].Left, "width of the column with the missing size");
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                AssertClose(expected[r][c].Top, cells[r][c].Top, $"top of {r} in {c}");
                AssertClose(expected[r][c].Bottom, cells[r][c].Bottom, $"bottom of {r} in {c}");
            }
        }
    }

    /// <summary>In the picture the missing cell alone is gone: its segment draws its two other cells, the other segments all
    /// three, and no coordinate is NaN.</summary>
    [Fact]
    public void AMissingSize_LeavesOnlyItsOwnCellOutOfThePicture()
    {
        string svg = Svg(ax => ax.Marimekko(Columns, WithAMissingSize));

        var groups = SeriesGroups(svg);
        Assert.Equal([3, 2, 3], groups.Select(g => Shapes(g, "polygon").Count));
        Assert.DoesNotContain("NaN", svg);
    }

    /// <summary>A size of 0 is a cell of no height: nothing is drawn for it, no fill and no outline across the column (a line
    /// in the segment's colour there would read as a share), and the column keeps its other cells.</summary>
    [Fact]
    public void ASizeOfZero_DrawsNoCellAndNoLine()
    {
        BarGroup[] segments = [new("A", [10, 50, 6]), new("B", [20, 0, 9]), new("C", [15, 30, 15])];

        string svg = Svg(ax => ax.Marimekko(Columns, segments));
        var cells = Areas(Chart(segments)).Select(CellsOf).ToArray();

        var groups = SeriesGroups(svg);
        Assert.Equal([3, 2, 3], groups.Select(g => Shapes(g, "polygon").Count));
        Assert.Equal([3, 2, 3], groups.Select(g => Shapes(g, "polyline").Count));
        Assert.Equal(cells[0][1].Top, cells[2][1].Bottom);
    }

    /// <summary>A column whose sizes are all 0 or missing has no share of the total: it is 0 wide, draws no cell and carries
    /// no name, because a name needs a column to stand under (two such columns side by side would put two names on one
    /// point). The columns around it meet where it would be.</summary>
    [Fact]
    public void AColumnWithNothingInIt_IsZeroWide_DrawsNoCell_AndIsNotNamed()
    {
        string[] columns = ["Retail", "Nothing", "Missing", "Online"];
        BarGroup[] segments = [new("A", [10, 0, double.NaN, 30]), new("B", [30, 0, double.NaN, 10])];

        var axes = AxesOf(ax => ax.Marimekko(columns, segments));
        string svg = Svg(ax => ax.Marimekko(columns, segments));

        var cells = CellsOf(Areas(axes)[0]);
        Assert.Equal(50.0, cells[0].Right);
        Assert.Equal(cells[1].Left, cells[1].Right);
        Assert.Equal(cells[2].Left, cells[2].Right);
        Assert.Equal(50.0, cells[3].Left);
        Assert.Equal([2, 2], SeriesGroups(svg).Select(g => Shapes(g, "polygon").Count));
        var ticks = axes.XAxis.TickLocator!.Locate(0, 100);
        Assert.Equal([25.0, 75.0], ticks);
        Assert.Equal(["Retail", "Online"], ticks.Select(axes.XAxis.TickFormatter!.Format));
        Assert.DoesNotContain("NaN", svg);
    }

    // ---- the names and the axes ----------------------------------------------------------------------------------------------

    /// <summary>The column names are the X ticks, one on the centre of each column (a built-in fixed locator and the category
    /// formatter at those positions, so they survive JSON), and they are drawn.</summary>
    [Fact]
    public void TheColumnNames_AreTheXTicks_OnTheColumnCentres()
    {
        var axes = Chart(Segments);
        var cells = CellsOf(Areas(axes)[0]);

        Assert.IsType<FixedLocator>(axes.XAxis.TickLocator);
        Assert.IsType<CategoryFormatter>(axes.XAxis.TickFormatter);
        var ticks = axes.XAxis.TickLocator!.Locate(0, 100);
        Assert.Equal(cells.Select(cell => (cell.Left + cell.Right) / 2), ticks);
        Assert.Equal(Columns, ticks.Select(axes.XAxis.TickFormatter!.Format));
        Assert.Equal(string.Empty, axes.XAxis.TickFormatter.Format(0.5));
        var texts = FigureTexts(Svg(ax => ax.Marimekko(Columns, Segments)));
        Assert.All(Columns, name => Assert.Contains(name, texts));
    }

    /// <summary>A column with no name is an empty tick.</summary>
    [Fact]
    public void AColumnWithoutAName_IsAnEmptyTick()
    {
        var axes = AxesOf(ax => ax.Marimekko(["Retail", null!], [new("A", [1, 2])]));

        var ticks = axes.XAxis.TickLocator!.Locate(0, 100);
        Assert.Equal(["Retail", ""], ticks.Select(axes.XAxis.TickFormatter!.Format));
    }

    /// <summary>The Y axis reads the share of the column in percent: 0% at the bottom to 100% at the top (a built-in percent
    /// formatter over 100, so it survives JSON).</summary>
    [Fact]
    public void TheYAxis_ReadsTheShareOfTheColumnInPercent()
    {
        var axes = Chart(Segments);
        var texts = FigureTexts(Svg(ax => ax.Marimekko(Columns, Segments)));

        Assert.IsType<PercentFormatter>(axes.YAxis.TickFormatter);
        Assert.Equal("50%", axes.YAxis.TickFormatter!.Format(50));
        Assert.Contains("0%", texts);
        Assert.Contains("100%", texts);
    }

    /// <summary>The verb sets no axis limits and keeps one the caller set before the call; it sets the Y margin to 0 so the
    /// columns fill the plot, and a margin set after the call replaces it.</summary>
    [Fact]
    public void TheVerb_SetsNoAxisLimits_AndKeepsTheCallersOwn()
    {
        var axes = Chart(Segments);
        var kept = AxesOf(ax => ax.SetXLim(-10, 110).SetYLim(0, 50).Marimekko(Columns, Segments));
        var margin = AxesOf(ax => ax.Marimekko(Columns, Segments).SetYMargin(0.1));

        Assert.False(axes.HasExplicitLimit);
        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
        Assert.Equal(0.0, axes.YAxis.Margin);
        Assert.Equal(-10.0, kept.XAxis.Min);
        Assert.Equal(110.0, kept.XAxis.Max);
        Assert.Equal(0.0, kept.YAxis.Min);
        Assert.Equal(50.0, kept.YAxis.Max);
        Assert.Equal(0.1, margin.YAxis.Margin);
    }

    // ---- colours and the legend ------------------------------------------------------------------------------------------

    /// <summary>One drawn unit is one colour: segment i wears the theme's colour at slot i in every column, in order; no two
    /// segments share a colour.</summary>
    [Fact]
    public void EverySegment_WearsItsOwnColourOfTheCycle_InEveryColumn()
    {
        string svg = Svg(ax => ax.Marimekko(Columns, Segments));

        var fills = SeriesGroups(svg).Select(g => Named(g, "polygon").Select(p => ((string)p.Attribute("fill")!)[..7]).Distinct().ToList()).ToList();
        Assert.Equal([[Colors.Tab10Blue.ToHex()], [Colors.Tab10Orange.ToHex()], [Colors.Tab10Green.ToHex()]], fills);
    }

    /// <summary>The colours are the figure's theme's: under ggplot segment i wears ggplot's colour i.</summary>
    [Fact]
    public void TheColours_FollowTheFiguresTheme()
    {
        string svg = Svg(ax => ax.Marimekko(Columns, Segments), Theme.Ggplot);

        var fills = SeriesGroups(svg).Select(g => ((string)Named(g, "polygon").First().Attribute("fill")!)[..7]).ToList();
        Assert.Equal([.. Theme.Ggplot.CycleColors.Take(3).Select(c => c.ToHex())], fills);
    }

    /// <summary>The legend lists one entry for each segment, in the order of the segments, named by them; a click on an entry
    /// hides that segment in every column.</summary>
    [Fact]
    public void TheLegend_ListsOneEntryPerSegment_InOrder()
    {
        var figure = Plt.Create().WithSize(800, 500).AddSubPlot(1, 1, 1, ax => ax.Marimekko(Columns, Segments)).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        var entries = Regex.Matches(svg, """<g data-legend-index="([0-9]+)" [^>]*aria-label="([^"]*)">""");
        Assert.Equal(["0:A", "1:B", "2:C"], entries.Select(e => $"{e.Groups[1].Value}:{e.Groups[2].Value}"));
    }

    /// <summary>A segment with no name (null or empty) is drawn and has no legend entry.</summary>
    [Fact]
    public void ASegmentWithoutAName_IsDrawn_WithNoLegendEntry()
    {
        BarGroup[] segments = [new(null!, [1, 2, 3]), new("", [2, 2, 2]), new("named", [3, 2, 1])];
        var figure = Plt.Create().WithSize(800, 500).AddSubPlot(1, 1, 1, ax => ax.Marimekko(Columns, segments)).Build();
        figure.EnableLegendToggle = true;

        var areas = Areas(figure.SubPlots[0]);

        Assert.Equal(3, areas.Length);
        Assert.Null(areas[0].Label);
        Assert.Null(areas[1].Label);
        Assert.Equal("named", areas[2].Label);
        Assert.Single(Regex.Matches(figure.ToSvg(), "data-legend-index="));
    }

    /// <summary>The cells are opaque, as bars are, so a cell wears exactly the colour of its legend entry (an area is faint,
    /// 0.3, by default).</summary>
    [Fact]
    public void TheCells_AreOpaque()
    {
        Assert.All(Areas(Chart(Segments)), area => Assert.Equal(1.0, area.Alpha));
    }

    // ---- the callback ----------------------------------------------------------------------------------------------------

    /// <summary>The callback runs once for each segment, in the order of the segments, after the verb has built and styled the
    /// area, so what it sets wins.</summary>
    [Fact]
    public void TheCallback_RunsOncePerSegment_InOrder_AfterTheVerbStyledIt()
    {
        var seen = new List<string?>();

        var axes = Chart(Segments, area =>
        {
            Assert.Equal(1.0, area.Alpha);
            seen.Add(area.Label);
            area.Alpha = 0.6;
        });

        Assert.Equal(["A", "B", "C"], seen);
        Assert.All(Areas(axes), area => Assert.Equal(0.6, area.Alpha));
    }

    // ---- the verb draws what the hand chain draws -------------------------------------------------------------------------

    /// <summary>The verb adds nothing to the picture that the hand-written chain of public building blocks did not draw, and
    /// leaves nothing out: same SVG, byte for byte, with and without a missing size.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws(bool missing)
    {
        string byVerb = ComposedChartShapes.Named("marimekko").Build(missing).ToSvg();
        string byHand = ComposedChartShapes.MarimekkoByHand(missing).ToSvg();

        Assert.Equal(byHand, byVerb);
    }

    // ---- JSON ----------------------------------------------------------------------------------------------------------------

    /// <summary>Written to JSON and read back, the chart draws the same picture, with and without a missing size and under a
    /// dark theme: the cells, the colours, the names at the column centres and the percent axis all come back.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void TheChart_ReadBackFromJson_DrawsTheSamePicture(bool missing, bool dark)
    {
        var builder = Plt.Create().WithSize(800, 500);
        if (dark) builder = builder.WithTheme(Theme.Dark);
        var figure = builder.AddSubPlot(1, 1, 1, ax => ax.Marimekko(Columns, missing ? WithAMissingSize : Segments)).Build();
        string drawn = figure.ToSvg();

        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(drawn, readBack);
        Assert.All(Columns, name => Assert.Contains(name, FigureTexts(readBack)));
    }

    // ---- refused arguments -----------------------------------------------------------------------------------------------

    /// <summary>An axes, a list of columns or a list of segments that is not there is refused by name.</summary>
    [Fact]
    public void ANullArgument_IsRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).Marimekko(Columns, Segments)).ParamName);
        Assert.Equal("columns", Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.Marimekko(null!, Segments))).ParamName);
        Assert.Equal("segments", Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.Marimekko(Columns, null!))).ParamName);
    }

    /// <summary>No columns or no segments draw nothing: refused by name, in words that say what a Marimekko chart needs.</summary>
    [Fact]
    public void NoColumnsOrNoSegments_AreRefusedByName()
    {
        var noColumns = Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.Marimekko([], [new("A", [])])));
        var noSegments = Assert.Throws<ArgumentException>(() => Chart([]));

        Assert.Equal("columns", noColumns.ParamName);
        Assert.StartsWith("No columns were given", noColumns.Message);
        Assert.Equal("segments", noSegments.ParamName);
        Assert.StartsWith("No segments were given", noSegments.Message);
    }

    /// <summary>A segment that does not carry one size per column is refused, naming the segment, both counts and the
    /// parameter, in the words every verb that takes groups uses; a segment with no sizes at all is refused by name.</summary>
    [Fact]
    public void ASegmentThatDoesNotFitTheColumns_IsRefusedByName()
    {
        var error = Assert.Throws<ArgumentException>(() => Chart([new("A", [1, 2, 3]), new("B", [1, 2])]));

        Assert.Equal("segments", error.ParamName);
        Assert.Equal("Group 'B' carries 2 values; there are 3 columns. (Parameter 'segments')", error.Message);
        Assert.Equal("segments", Assert.Throws<ArgumentException>(() => Chart([new BarGroup("A", null!)])).ParamName);
        Assert.Equal("segments", Assert.Throws<ArgumentException>(() => Chart([default])).ParamName);
    }

    /// <summary>A size is finite and 0 or more: a negative size and an infinity are refused, naming the segment, the column
    /// and the size (a cell of negative size has no share; NaN marks a missing size instead).</summary>
    [Theory]
    [InlineData(-1.0, "-1")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    [InlineData(double.PositiveInfinity, "Infinity")]
    public void ASizeThatIsNotASize_IsRefusedByName(double size, string written)
    {
        var error = Assert.Throws<ArgumentException>(() => Chart([new("A", [1, 2, 3]), new("B", [1, size, 1])]));

        Assert.Equal("segments", error.ParamName);
        Assert.StartsWith(
            $"The size of 'B' in 'Online' is {written}; a size is finite and 0 or more (NaN marks a missing size).", error.Message);
    }

    /// <summary>Sizes that do not add up to a finite number above 0 have no shares to draw: only zeros, only missing sizes,
    /// and a total too large for a double are refused by name.</summary>
    [Theory]
    [InlineData(0.0, 0.0, "0")]
    [InlineData(double.NaN, double.NaN, "0")]
    [InlineData(1e308, 1e308, "Infinity")]
    public void SizesThatDoNotAddUpToAFiniteTotalAboveZero_AreRefusedByName(double first, double second, string written)
    {
        var error = Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.Marimekko(["a", "b"], [new("A", [first, second])])));

        Assert.Equal("segments", error.ParamName);
        Assert.StartsWith(
            $"The sizes add up to {written}; a Marimekko chart draws each column as its share of the total, which must be a finite number above 0.",
            error.Message);
    }

    /// <summary>A refused call has added nothing: the checks come before the first area, so no series, no tick locator and no
    /// margin is left behind.</summary>
    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var builder = new AxesBuilder();

        Assert.Throws<ArgumentException>(() => builder.Marimekko(Columns, [new("A", [1, 2, 3]), new("B", [1, -2, 3])]));
        Assert.Throws<ArgumentException>(() => builder.Marimekko(Columns, [new("A", [0, 0, 0])]));
        var axes = builder.Build(1, 1, 1);

        Assert.Empty(axes.Series);
        Assert.Null(axes.XAxis.TickLocator);
        Assert.Null(axes.YAxis.TickFormatter);
        Assert.Null(axes.YAxis.Margin);
    }

    // ---- the example in the documentation ---------------------------------------------------------------------------------

    /// <summary>The example of the verb's documentation, as written there, compiles and draws.</summary>
    [Fact]
    public void TheDocumentationExample_Draws()
    {
        string[] channels = ["Retail", "Online", "Wholesale"];

        string svg = Plt.Create()
            .WithTitle("Market by channel and brand")
            .TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax
                .Marimekko(channels,
                [
                    new BarGroup("Brand A", [30, 15, 5]),
                    new BarGroup("Brand B", [20, 25, 10]),
                    new BarGroup("Brand C", [10, 20, 15]),
                ])
                .SetXLabel("Channel (width: its share of the market)")
                .WithLegend(LegendPosition.OutsideRight))
            .ToSvg();

        Assert.Equal(9, SeriesGroups(svg).Sum(g => Shapes(g, "polygon").Count));
        Assert.All(channels, name => Assert.Contains($">{name}<", svg));
    }
}
