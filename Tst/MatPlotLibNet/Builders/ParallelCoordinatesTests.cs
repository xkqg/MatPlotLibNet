// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Parallel coordinates in one call: one line series for each CLASS of rows (several rows may share a name), every
/// row of a class a stretch of that series with a missing point between rows, each column scaled to 0..1 over ALL rows, one
/// solid axis line for each dimension in the reference line's default grey, the dimension names as the X ticks on the centres
/// of the slots, and a Y axis without ticks (the scale is each column's own). The verb sets no axis limits.</summary>
public class ParallelCoordinatesTests
{
    private static readonly string[] Dimensions = ["length", "width", "height"];

    // The classes come in the order dog, cat, dog: the series follow the order of first appearance, not the alphabet, and the
    // two dog rows are not neighbours, so both must find their way into the one series of the class.
    private static readonly BarGroup[] Rows =
    [
        new("dog", [2, 30, 0]),
        new("cat", [1, 10, 100]),
        new("dog", [3, 20, 50]),
    ];

    private static Axes Chart(string[] dimensions, IReadOnlyList<BarGroup> rows, Action<LineSeries>? configure = null) =>
        Plt.Create().OneAxes(ax => ax.ParallelCoordinates(dimensions, rows, configure));

    private static Axes Chart(IReadOnlyList<BarGroup> rows) => Chart(Dimensions, rows);

    private static LineSeries[] Lines(Axes axes) => [.. axes.Series.Cast<LineSeries>()];

    // ---- reading the picture ---------------------------------------------------------------------------------------

    /// <summary>A point of a drawn polyline, in pixels.</summary>
    private readonly record struct Pt(double X, double Y);

    /// <summary>The plot area in pixels.</summary>
    /// <summary>A text drawn straight into the figure: what it says, where its anchor stands, and which side of the text the
    /// anchor is on (<c>start</c>, <c>middle</c> or <c>end</c>).</summary>
    private readonly record struct FigureText(string Text, double X, string Anchor);

    /// <summary>A straight line drawn into the figure: its two ends and its stroke.</summary>
    private readonly record struct Stroke(double X1, double Y1, double X2, double Y2, string Colour, string Width);

    private static string Svg(Action<AxesBuilder> configure, Theme? theme = null)
    {
        var builder = Plt.Create().WithSize(700, 500);
        if (theme is not null) builder = builder.WithTheme(theme);
        return builder.AddSubPlot(1, 1, 1, configure).Build().ToSvg();
    }

    /// <summary>The polylines of one series group, each as its points: one for every stretch of the line that is drawn.</summary>
    private static List<Pt[]> Polylines(XElement series) =>
        [.. series.Each("polyline").Select(p => ((string)p.Attribute("points")!)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(','))
            .Select(xy => new Pt(xy[0].Number(), xy[1].Number()))
            .ToArray())];

    /// <summary>The lines the figure draws directly (not inside a series, a legend or a spine group).</summary>
    private static List<Stroke> FigureLines(string svg) =>
        [.. svg.Direct("line")
            .Select(e => new Stroke(e.Number("x1"), e.Number("y1"),
                e.Number("x2"), e.Number("y2"),
                (string)e.Attribute("stroke")!, (string)e.Attribute("stroke-width")!))];

    /// <summary>The axis lines: the vertical lines the figure draws in the reference line's grey, the full height of the plot.</summary>
    private static List<Stroke> AxisLines(string svg) =>
        [.. FigureLines(svg).Where(l => l.Colour == Colors.Gray.ToHex() && l.X1 == l.X2)];

    /// <summary>The texts drawn straight into the figure, not inside a series, a legend or a spine group.</summary>
    private static List<FigureText> FigureTexts(string svg) =>
        [.. svg.Direct("text")
            .Select(e => new FigureText(e.Value, e.Number("x"), (string)e.Attribute("text-anchor")!))];

    // ---- one line series per class -------------------------------------------------------------------------------------

    /// <summary>The whole chart is one line series for each distinct row name, in the order the names first appear: nothing
    /// on a second axis, no scatter for markers, the class as the series' name.</summary>
    [Fact]
    public void TheChart_IsOneLineSeriesPerClass_InOrderOfFirstAppearance()
    {
        var axes = Chart(Rows);

        var lines = Lines(axes);
        Assert.Equal(["dog", "cat"], lines.Select(l => l.Label));
        Assert.Empty(axes.YAxes.Right);
        Assert.Empty(axes.XSecondarySeries);
    }

    /// <summary>The rows of a class are stretches of its one series, in the order the rows came in and not next to each
    /// other in the data, with ONE missing point (NaN in x and y) between two rows. Every column is scaled min to max over ALL
    /// rows, not over the class: the dog's 2 in the first column stands at the middle of the cat's 1 and the dog's 3.</summary>
    [Fact]
    public void TheRowsOfAClass_ShareOneSeries_SeparatedByOneMissingPoint_AndEveryColumnIsScaledOverAllRows()
    {
        var lines = Lines(Chart(Rows));

        double nan = double.NaN;
        Assert.Equal([0.5, 1.5, 2.5, nan, 0.5, 1.5, 2.5], lines[0].XData);
        Assert.Equal([0.5, 1.0, 0.0, nan, 1.0, 0.5, 0.5], lines[0].YData);
        Assert.Equal([0.5, 1.5, 2.5], lines[1].XData);
        Assert.Equal([0.0, 0.0, 1.0], lines[1].YData);
    }

    /// <summary>A row without a missing value is never scaled past its column's own range: the smallest value of a column is
    /// at 0 and the largest at 1, whichever class holds them.</summary>
    [Fact]
    public void EveryColumn_ReachesFromZeroToOne_WhateverClassHoldsTheEnds()
    {
        var lines = Lines(Chart(Rows));
        double[][] columns = Enumerable.Range(0, Dimensions.Length)
            .Select(d => new[] { lines[0].YData[d], lines[0].YData[4 + d], lines[1].YData[d] }).ToArray();

        Assert.All(columns, c => { Assert.Equal(0.0, c.Min()); Assert.Equal(1.0, c.Max()); });
    }

    /// <summary>A column whose values are all the same has no range to scale by: it is drawn at the middle, and so is a column
    /// with one value that can be drawn among missing ones.</summary>
    [Fact]
    public void AColumnWithoutARange_IsDrawnAtTheMiddle()
    {
        var same = Lines(Chart(["only"], [new("a", [7]), new("b", [7])]));
        var lone = Lines(Chart(["only"], [new("a", [double.NaN]), new("b", [7]), new("c", [double.PositiveInfinity])]));

        Assert.Equal([0.5], same[0].YData);
        Assert.Equal([0.5], same[1].YData);
        Assert.True(double.IsNaN(lone[0].YData[0]));
        Assert.Equal([0.5], lone[1].YData);
        Assert.True(double.IsNaN(lone[2].YData[0]));
    }

    /// <summary>A column in which no value can be drawn has no range at all: every row is missing there, and the other columns
    /// are scaled as ever.</summary>
    [Fact]
    public void AColumnWithNothingToDraw_LeavesEveryRowMissingThere_AndTheRestIsScaledAsEver()
    {
        var lines = Lines(Chart(["gone", "here"], [new("a", [double.NaN, 1]), new("a", [double.PositiveInfinity, 2])]));

        double nan = double.NaN;
        Assert.Equal([nan, 0.0, nan, nan, 1.0], lines[0].YData);
    }

    /// <summary>A missing value stays missing in its row and is no part of its column's range; an infinity cannot be placed
    /// either, so it is missing too. The other rows are scaled as if the value were not there.</summary>
    [Fact]
    public void AMissingValue_StaysMissing_AndAnInfinityIsOneToo_WithoutMovingTheRange()
    {
        BarGroup[] rows =
        [
            new("a", [1, double.NaN]),
            new("a", [double.PositiveInfinity, 20]),
            new("b", [3, 10]),
        ];

        var lines = Lines(Chart(["x", "y"], rows));

        // Column x ranges over 1 and 3 (the infinity is no part of it), column y over 10 and 20 (nor is the NaN).
        double nan = double.NaN;
        Assert.Equal([0.0, nan, nan, nan, 1.0], lines[0].YData);
        Assert.Equal([1.0, 0.0], lines[1].YData);
    }

    // ---- the picture: one stretch for each row ------------------------------------------------------------------------

    private static readonly string[] FourDimensions = ["a", "b", "c", "d"];

    /// <summary>The missing point between two rows is a break, not a point to bridge: every row of a class is a polyline of
    /// its own with one point on each axis line, and no polyline runs from the last axis back to the first of the next row.</summary>
    [Fact]
    public void TheRowSeparator_CreatesNoPhantomLine_EveryRowIsItsOwnPolyline()
    {
        BarGroup[] rows = [new("x", [1, 2, 3, 4]), new("x", [4, 3, 2, 1]), new("y", [2, 4, 1, 3])];

        string svg = Svg(ax => ax.ParallelCoordinates(FourDimensions, rows));

        var groups = svg.SeriesGroups();
        var axisX = AxisLines(svg).Select(l => l.X1).OrderBy(x => x).ToArray();
        var classX = Polylines(groups[0]);
        Assert.Equal(2, classX.Count);
        Assert.All(classX, run =>
        {
            Assert.Equal(4, run.Length);
            Assert.Equal(axisX, run.Select(p => p.X).ToArray(), new PixelComparer());
        });
        Assert.Single(Polylines(groups[1]));
    }

    /// <summary>M-T8: a row with a missing value breaks only its own line. The row [a, missing, c, d] draws the stretch from c
    /// to d and nothing from a (a lone point has no line, and nothing bridges the gap); the other row of the class and the other
    /// class are drawn whole, and the picture holds no NaN.</summary>
    [Fact]
    public void AMissingValue_BreaksOnlyItsOwnRowsLine()
    {
        BarGroup[] rows = [new("x", [1, 2, 3, 4]), new("x", [4, double.NaN, 2, 1]), new("y", [2, 4, 1, 3])];

        string svg = Svg(ax => ax.ParallelCoordinates(FourDimensions, rows));

        var groups = svg.SeriesGroups();
        var axisX = AxisLines(svg).Select(l => l.X1).OrderBy(x => x).ToArray();
        var classX = Polylines(groups[0]);
        Assert.Equal(2, classX.Count);
        Assert.Equal(4, classX[0].Length);
        var broken = classX[1];
        Assert.Equal(2, broken.Length);
        Assert.Equal([axisX[2], axisX[3]], broken.Select(p => p.X).ToArray(), new PixelComparer());
        var whole = Polylines(groups[1]);
        Assert.Single(whole);
        Assert.Equal(4, whole[0].Length);
        svg.AssertNoNonFiniteNumber();
    }

    /// <summary>Compares pixel coordinates the way the SVG writes them (a coordinate can come out a hair apart between the
    /// polyline and the line it stands on).</summary>
    private sealed class PixelComparer : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Abs(x - y) < 1e-6;
        public int GetHashCode(double value) => 0;
    }

    // ---- the axes ----------------------------------------------------------------------------------------------------------

    /// <summary>The columns stand on the centres of category slots, the rule every verb that names categories follows, so a
    /// dimension's name is under its axis line.</summary>
    [Fact]
    public void TheColumns_StandOnTheCentresOfTheirSlots()
    {
        var axes = Chart(Rows);

        Assert.All(Lines(axes), line =>
            Assert.All(line.XData.Where(double.IsFinite), x => Assert.Contains(x, new[] { 0.5, 1.5, 2.5 })));
        Assert.Equal([0.5, 1.5, 2.5], axes.ReferenceLines.Select(r => r.Value));
    }

    /// <summary>One vertical axis line for each dimension, solid, with no colour of its own and no label: the reference line's
    /// own default colour decides it, so no colour is fixed when the chart is built.</summary>
    [Fact]
    public void EveryDimension_HasOneSolidAxisLine_WithNoColourOfItsOwn()
    {
        var axes = Chart(Rows);

        Assert.Equal(Dimensions.Length, axes.ReferenceLines.Count);
        Assert.All(axes.ReferenceLines, line =>
        {
            Assert.Equal(Orientation.Vertical, line.Orientation);
            Assert.Equal(LineStyle.Solid, line.LineStyle);
            // given 1 px, as pandas' parallel_coordinates gives its axvline linewidth=1 (pandas 3.1.1: axvlines_kwds {"linewidth": 1})
            Assert.Equal(1.0, line.GivenLineWidth);
            Assert.Null(line.Color);
            Assert.Null(line.Label);
        });
    }

    /// <summary>Drawn: the axis lines run the full height of the plot, each on the tick that names its dimension, behind the
    /// class lines (the figure draws reference lines before series).</summary>
    [Fact]
    public void TheAxisLines_RunTheFullHeightOfThePlot_UnderTheirNames()
    {
        string svg = Svg(ax => ax.ParallelCoordinates(Dimensions, Rows));

        var plot = svg.PlotArea();
        var axisLines = AxisLines(svg);
        var names = FigureTexts(svg).Where(t => t.Anchor == "middle").ToList();
        Assert.Equal(Dimensions.Length, axisLines.Count);
        Assert.Equal(Dimensions, names.Select(n => n.Text));
        for (int k = 0; k < Dimensions.Length; k++)
        {
            Assert.Equal(plot.Y, axisLines[k].Y1, 6);
            Assert.Equal(plot.Y + plot.Height, axisLines[k].Y2, 6);
            Assert.Equal(names[k].X, axisLines[k].X1, 6);
        }
        int lastAxisLine = svg.LastIndexOf($"stroke=\"{Colors.Gray.ToHex()}\" stroke-width=\"1\"", StringComparison.Ordinal);
        int firstSeries = svg.IndexOf("<g class=\"series\"", StringComparison.Ordinal);
        Assert.InRange(lastAxisLine, 0, firstSeries);
    }

    /// <summary>H1, measured: the axis lines are visible on a light and on a dark theme, because the reference line's default
    /// grey stands at a contrast of 3 : 1 or more against the plot background of both (WCAG asks 3 : 1 of a graphic). Over the
    /// 30 shipped themes that grey reaches 3 : 1 on 25 (the five below: Nord, Dracula, Monokai, Gruvbox, OpsPanel); a light grey
    /// fixed by the verb, like the dumbbell connector's #AAAAAA before it took this grey too, would reach it on 12 and stand at 2.32 : 1 on white. So the
    /// axis lines carry no colour of their own.</summary>
    [Theory]
    [InlineData("Default")]
    [InlineData("Dark")]
    [InlineData("Ggplot")]
    public void TheAxisLines_AreVisibleOnTheTheme_WhateverIsSetAfterTheVerb(string themeName)
    {
        var theme = themeName.AsTheme();

        // The theme is set AFTER the verb has run, so nothing the verb fixed could follow it.
        string svg = Plt.Create().WithSize(700, 500)
            .AddSubPlot(1, 1, 1, ax => ax.ParallelCoordinates(Dimensions, Rows)).WithTheme(theme).Build().ToSvg();
        var axisLines = AxisLines(svg);

        Assert.Equal(Dimensions.Length, axisLines.Count);
        var drawn = Color.FromHex(axisLines[0].Colour);
        Assert.True(drawn.ContrastRatioWith(theme.AxesBackground) >= 3.0,
            $"{themeName}: the axis line {axisLines[0].Colour} stands at {drawn.ContrastRatioWith(theme.AxesBackground):F2} : 1 against {theme.AxesBackground.ToHex()}");
    }

    /// <summary>The dimension names are the X ticks: a built-in locator and formatter made as one pair, so they survive JSON,
    /// one tick on each column reading its name.</summary>
    [Fact]
    public void TheDimensionNames_AreTheXTicks_OnTheColumns()
    {
        var axis = Chart(Rows).XAxis;

        Assert.IsType<FixedLocator>(axis.TickLocator);
        Assert.IsType<CategoryFormatter>(axis.TickFormatter);
        var ticks = axis.TickLocator!.Locate(0, 3);
        Assert.Equal([0.5, 1.5, 2.5], ticks);
        Assert.Equal(Dimensions, ticks.Select(axis.TickFormatter!.Format));
    }

    /// <summary>A dimension without a name reads as an empty tick, not as an exception.</summary>
    [Fact]
    public void ADimensionWithoutAName_IsAnEmptyTick()
    {
        var axis = Chart([null!, "width"], [new("a", [1, 2]), new("a", [2, 1])]).XAxis;

        Assert.Equal(["", "width"], axis.TickLocator!.Locate(0, 2).Select(axis.TickFormatter!.Format));
    }

    /// <summary>The Y axis carries no ticks: every column is scaled to 0..1 on its own, so a number on the Y axis would read
    /// none of them. The axis is a built-in locator with no positions (it survives JSON), and no Y tick label or mark is drawn
    /// beside the plot; the numbers each column reads in its own units stand on the column lines, inside the plot.</summary>
    [Fact]
    public void TheYAxis_CarriesNoTicks()
    {
        var axes = Chart(Rows);
        string svg = Svg(ax => ax.ParallelCoordinates(Dimensions, Rows));

        Assert.IsType<FixedLocator>(axes.YAxis.TickLocator);
        Assert.Empty(axes.YAxis.TickLocator!.Locate(-10, 10));
        var plot = svg.PlotArea();
        Assert.DoesNotContain(FigureTexts(svg), t => t.Anchor == "end" && t.X < plot.X);
        Assert.DoesNotContain(FigureLines(svg), l => l.Y1 == l.Y2 && l.X1 < plot.X);
    }

    // ---- colours and the legend --------------------------------------------------------------------------------------------

    /// <summary>One drawn unit is one colour: each class takes the next colour of the theme's cycle in order of first
    /// appearance, all its rows in that one colour; the axis lines are reference lines and take no slot.</summary>
    [Fact]
    public void EveryClass_WearsOneColourOfTheCycle_AllItsRowsInIt()
    {
        string svg = Svg(ax => ax.ParallelCoordinates(Dimensions, Rows));

        var groups = svg.SeriesGroups();
        var dog = groups[0].Each("polyline").Select(p => (string)p.Attribute("stroke")!).ToList();
        var cat = groups[1].Each("polyline").Select(p => (string)p.Attribute("stroke")!).ToList();
        Assert.Equal(2, dog.Count);
        Assert.Single(dog.Distinct());
        Assert.Equal(Colors.Tab10Blue.ToHex(), dog[0]);
        Assert.Equal(Colors.Tab10Orange.ToHex(), cat[0]);
    }

    /// <summary>The legend lists one entry for each class and none for the axis lines: a click on an entry hides every row of
    /// its class.</summary>
    [Fact]
    public void TheLegend_ListsOneEntryPerClass_AndNoneForTheAxisLines()
    {
        var figure = Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax.ParallelCoordinates(Dimensions, Rows)).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        Assert.Equal(["0:dog", "1:cat"], svg.LegendEntries().Select(entry => $"{entry.Index}:{entry.Label}"));
    }

    /// <summary>The lines fill the plot, so no corner is empty: the legend is the caller's to move, and a legend set outside the
    /// plot under a tight layout stands right of the plot area, covering no line (what the documentation says to do).</summary>
    [Fact]
    public void TheLegend_CanBeMovedBesideThePlot()
    {
        string svg = Plt.Create().WithSize(700, 500).TightLayout()
            .AddSubPlot(1, 1, 1, ax => ax.ParallelCoordinates(Dimensions, Rows).WithLegend(LegendPosition.OutsideRight)).Build().ToSvg();

        var plot = svg.PlotArea();
        double legendLeft = svg.LegendFrame()!.Value.X;
        Assert.True(legendLeft > plot.X + plot.Width, $"the legend starts at {legendLeft}, not right of the plot area ending at {plot.X + plot.Width}");
    }

    /// <summary>Rows that carry no name are one class of their own, drawn in one colour and left out of the legend (an entry
    /// with no text would name nothing).</summary>
    [Fact]
    public void RowsWithoutAName_AreOneUnnamedClass_WithNoLegendEntry()
    {
        BarGroup[] rows = [new(null!, [1, 2, 3]), new("", [3, 2, 1]), new("named", [2, 1, 3])];
        var figure = Plt.Create().WithSize(700, 500).AddSubPlot(1, 1, 1, ax => ax.ParallelCoordinates(Dimensions, rows)).Build();
        figure.EnableLegendToggle = true;

        var lines = Lines(figure.SubPlots[0]);
        int entries = figure.ToSvg().CountOf("data-legend-index=");

        Assert.Equal(2, lines.Length);
        Assert.Null(lines[0].Label);
        Assert.Equal([0.0, 1.0, 1.0, double.NaN, 1.0, 1.0, 0.0], lines[0].YData);
        Assert.Equal("named", lines[1].Label);
        Assert.Equal(1, entries);
    }

    // ---- the callback ----------------------------------------------------------------------------------------------------

    /// <summary>What the callback sets wins over the cycle: a colour set there is the colour drawn.</summary>
    [Fact]
    public void TheCallback_CanSetAColour_AndItIsTheColourDrawn()
    {
        string svg = Svg(ax => ax.ParallelCoordinates(Dimensions, Rows, line => line.Color = Colors.Red));

        Assert.All(svg.SeriesGroups(), g => Assert.All(g.Each("polyline"), p => Assert.Equal(Colors.Red.ToHex(), (string)p.Attribute("stroke")!)));
    }

    // ---- the verb draws what the hand chain draws ---------------------------------------------------------------------

    /// <summary>Written to JSON and read back, the chart draws the same picture, a missing value included, under the default
    /// theme and a dark one: the class lines with their missing separators, the axis lines, the names and the Y axis without
    /// ticks all come back.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheChart_ReadBackFromJson_DrawsTheSamePicture(bool dark)
    {
        BarGroup[] rows = [new("x", [1, 2, 3, 4]), new("x", [4, double.NaN, 2, 1]), new("y", [2, 4, 1, 3])];
        var builder = Plt.Create().WithSize(700, 500);
        if (dark) builder = builder.WithTheme(Theme.Dark);
        var figure = builder.AddSubPlot(1, 1, 1, ax => ax.ParallelCoordinates(FourDimensions, rows)).Build();
        string drawn = figure.ToSvg();

        string readBack = figure.RoundTripped().ToSvg();

        Assert.Equal(drawn, readBack);
        Assert.Equal(FourDimensions, FigureTexts(readBack).Where(t => t.Anchor == "middle").Select(t => t.Text));
        // The numbers on the column lines come back with the annotations that carry them.
        Assert.Contains(FigureTexts(readBack), t => t.Anchor == "end");
    }

    // ---- refused arguments -----------------------------------------------------------------------------------------------

    /// <summary>No dimensions and no rows draw nothing: both are refused by name, in words that say what a chart needs.</summary>
    [Fact]
    public void NoDimensions_AndNoRows_AreRefusedByName()
    {
        var noDimensions = Assert.Throws<ArgumentException>(() => Chart([], Rows));
        var noRows = Assert.Throws<ArgumentException>(() => Chart(Dimensions, []));

        Assert.Equal("dimensions", noDimensions.ParamName);
        Assert.StartsWith("No dimensions were given", noDimensions.Message);
        Assert.Equal("rows", noRows.ParamName);
        Assert.StartsWith("No rows were given", noRows.Message);
    }

    /// <summary>A row that does not carry one value per dimension (a ragged table) is refused, naming the row, both counts and
    /// the parameter, in the words every verb that takes groups uses; a row with no values at all is refused by name, not with
    /// a null reference.</summary>
    [Fact]
    public void ARowThatDoesNotFitTheDimensions_IsRefusedByName()
    {
        BarGroup[] ragged = [new("dog", [1, 2, 3]), new("cat", [1, 2])];

        var error = Assert.Throws<ArgumentException>(() => Chart(ragged));

        Assert.Equal("rows", error.ParamName);
        Assert.Equal(2.RefusalAgainst(3, "rows", "group 'cat'"), error.Message);
        Assert.Equal("rows", Assert.Throws<ArgumentException>(() => Chart([new BarGroup("dog", null!)])).ParamName);
        Assert.Equal("rows", Assert.Throws<ArgumentException>(() => Chart([default])).ParamName);
    }

    /// <summary>A column whose values span more than a double can hold cannot be scaled to 0..1 by its range: refused, naming
    /// the dimension and the parameter, instead of drawing every value at one end.</summary>
    [Fact]
    public void AColumnSpanningMoreThanADoubleCanHold_IsRefusedByName()
    {
        BarGroup[] rows = [new("a", [1, -double.MaxValue]), new("b", [2, double.MaxValue])];

        var error = Assert.Throws<ArgumentException>(() => Chart(["fine", "huge"], rows));

        Assert.Equal("rows", error.ParamName);
        Assert.StartsWith("The span of 'huge' is beyond what a double holds (got Infinity).", error.Message, StringComparison.Ordinal);
    }

    // ---- the example in the documentation --------------------------------------------------------------------------------

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  The example of the verb's documentation, as written there, compiles and draws.
        new DrawnToken("TheDocumentationExample_Draws", () =>
        {
            string[] measures = ["Sepal length", "Sepal width", "Petal length", "Petal width"];
            return Plt.Create()
                .WithTitle("Three species of iris")
                .AddSubPlot(1, 1, 1, ax => ax
                    .ParallelCoordinates(measures,
                    [
                        new BarGroup("setosa", [5.1, 3.5, 1.4, 0.2]),
                        new BarGroup("setosa", [4.9, 3.0, 1.4, 0.2]),
                        new BarGroup("versicolor", [7.0, 3.2, 4.7, 1.4]),
                        new BarGroup("virginica", [6.3, 3.3, 6.0, 2.5]),
                    ]))
                .ToSvg();
        }, ">Petal width<", ">virginica<", ">Three species of iris<"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
