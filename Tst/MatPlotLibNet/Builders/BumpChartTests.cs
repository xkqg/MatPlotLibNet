// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The bump chart in one call: one line series for each group with a marker at each rank, the name of the group at
/// the end of its line instead of in a legend, rank 1 on top, a tick for every whole rank, half a rank of margin at both
/// ends, and a gap (never a bridge) where a rank is missing. The verb sets the Y limits itself, which is what makes the
/// chart; it leaves the X axis to the data.</summary>
public class BumpChartTests
{
    private static readonly double[] Seasons = [2021, 2022, 2023, 2024];

    /// <summary>Three groups over four seasons; every season holds the ranks 1, 2 and 3 once.</summary>
    private static readonly BarGroup[] Table =
    [
        new("Hawks", [1, 2, 3, 3]),
        new("Owls", [2, 1, 1, 2]),
        new("Wrens", [3, 3, 2, 1]),
    ];

    /// <summary>How far under its tick the baseline of a Y tick label stands, in pixels (half the label's height).</summary>
    private const double TickLabelDrop = 4;

    private static Axes AxesOf(Action<AxesBuilder> configure) =>
        Plt.Create().AddSubPlot(1, 1, 1, configure).Build().SubPlots[0];

    private static LineSeries[] Lines(Axes axes) => [.. axes.Series.Cast<LineSeries>()];

    // ---- reading the picture ---------------------------------------------------------------------------------------

    /// <summary>Where a marker is drawn: the centre of its circle, in pixels.</summary>
    private readonly record struct Dot(double Cx, double Cy);

    /// <summary>The plot area in pixels.</summary>
    private readonly record struct Area(double X, double Y, double W, double H);

    /// <summary>A name drawn at the end of a line: its text, where it starts, the height of its baseline and its colour.</summary>
    private readonly record struct EndLabel(string Text, double X, double Y, string Fill);

    /// <summary>The picture, 900 x 450, with the legend toggle on when asked: that is what gives the legend entries their
    /// <c>data-legend-index</c>.</summary>
    private static string Svg(Action<AxesBuilder> configure, bool legendToggle = false)
    {
        var figure = Plt.Create().WithSize(900, 450).AddSubPlot(1, 1, 1, configure).Build();
        figure.EnableLegendToggle = legendToggle;
        return figure.ToSvg();
    }

    private static string Svg(double[] x, IReadOnlyList<BarGroup> ranks, bool legendToggle = false) =>
        Svg(ax => ax.BumpChart(x, ranks), legendToggle);

    /// <summary>The plot area: the first background-coloured rectangle that is not the figure's.</summary>
    private static Area PlotArea(string svg)
    {
        var m = Regex.Match(svg, """<rect x="([1-9][0-9.]*)" y="([0-9.]+)" width="([0-9.]+)" height="([0-9.]+)" fill="#FFFFFF" """.TrimEnd());
        return new Area(Num(m.Groups[1].Value), Num(m.Groups[2].Value), Num(m.Groups[3].Value), Num(m.Groups[4].Value));
    }

    private static IEnumerable<XElement> Named(XContainer root, string name) =>
        root.Descendants().Where(e => e.Name.LocalName == name);

    private static List<XElement> SeriesGroups(string svg) =>
        [.. Named(XDocument.Parse(svg), "g").Where(g => (string?)g.Attribute("class") == "series")];

    private static double Num(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    private static Dot[] Dots(XElement series) =>
        [.. Named(series, "circle").Select(c => new Dot(Num((string)c.Attribute("cx")!), Num((string)c.Attribute("cy")!)))];

    /// <summary>The runs of the line, one per polyline: the points a stretch of the line without a gap passes through.</summary>
    private static List<Dot[]> Runs(XElement series) =>
        [.. Named(series, "polyline").Select(p => ((string)p.Attribute("points")!)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(','))
            .Select(xy => new Dot(Num(xy[0]), Num(xy[1])))
            .ToArray())];

    private static string Stroke(XElement series) => (string)Named(series, "polyline").First().Attribute("stroke")!;

    /// <summary>The name drawn at the end of the series' line, when it has one: the text inside the series group but not
    /// inside the clipped drawing group.</summary>
    private static EndLabel? Label(XElement series) =>
        series.Elements().Where(e => e.Name.LocalName == "text").Select(t => new EndLabel(
            t.Value, Num((string)t.Attribute("x")!), Num((string)t.Attribute("y")!), (string)t.Attribute("fill")!))
            .Cast<EndLabel?>().SingleOrDefault();

    /// <summary>The tick labels of the Y axis, from the first tick down the file: the text elements anchored at their end
    /// that sit directly in the figure, not inside a series.</summary>
    private static string[] YTickLabels(string svg) =>
        [.. XDocument.Parse(svg).Root!.Elements()
            .Where(e => e.Name.LocalName == "text" && (string?)e.Attribute("text-anchor") == "end")
            .Select(e => e.Value)];

    /// <summary>The pixel height of each Y tick, in the order the ticks are listed.</summary>
    private static double[] YTickHeights(string svg) =>
        [.. XDocument.Parse(svg).Root!.Elements()
            .Where(e => e.Name.LocalName == "text" && (string?)e.Attribute("text-anchor") == "end")
            .Select(e => Num((string)e.Attribute("y")!))];

    // ---- one line series per group ---------------------------------------------------------------------------------

    /// <summary>The whole chart is one line series for each group: no scatter on top for the markers, no legend or label
    /// series, nothing on a second axis.</summary>
    [Fact]
    public void TheChart_IsOneLineSeriesPerGroup_AndNothingElse()
    {
        var axes = AxesOf(ax => ax.BumpChart(Seasons, Table));

        var lines = Lines(axes);
        Assert.Equal(Table.Length, lines.Length);
        Assert.Empty(axes.YAxes.Right);
        Assert.Empty(axes.XSecondarySeries);
        for (int g = 0; g < Table.Length; g++)
        {
            Assert.Equal(Seasons, lines[g].XData);
            Assert.Equal(Table[g].Values, lines[g].YData);
            Assert.Equal(Table[g].Label, lines[g].Label);
        }
    }

    /// <summary>The lines do not share their x array: restyling or editing one line's data cannot move another's.</summary>
    [Fact]
    public void EveryLine_HoldsItsOwnXArray()
    {
        var lines = Lines(AxesOf(ax => ax.BumpChart(Seasons, Table)));

        Assert.NotSame(Seasons, lines[0].XData);
        Assert.NotSame(lines[0].XData, lines[1].XData);
    }

    /// <summary>A marker at each rank, a thick line, and the name at the end of the line: the look of a bump chart.</summary>
    [Fact]
    public void EveryLine_HasAMarkerAtEachRank_AndItsNameAtItsEnd()
    {
        var lines = Lines(AxesOf(ax => ax.BumpChart(Seasons, Table)));

        Assert.All(lines, line =>
        {
            Assert.Equal(MarkerStyle.Circle, line.Marker);
            Assert.Equal(10, line.MarkerSize);
            Assert.Equal(3, line.LineWidth);
            Assert.Equal(SeriesLabelPlacement.LineEnd, line.LabelPlacement);
        });
    }

    /// <summary>The name stands right of the plot area at the end of its line, in the line's colour, once; the chart lists
    /// nothing in a legend, even when the legend is switched on.</summary>
    [Fact]
    public void TheNames_AreDrawnAtTheEndOfTheirLines_NotInALegend()
    {
        string svg = Svg(ax => ax.BumpChart(Seasons, Table).WithLegend(), legendToggle: true);
        var plot = PlotArea(svg);

        Assert.DoesNotContain("data-legend-index=", svg);
        var groups = SeriesGroups(svg);
        Assert.Equal(Table.Length, groups.Count);
        for (int g = 0; g < Table.Length; g++)
        {
            var label = Assert.IsType<EndLabel>(Label(groups[g]));
            Assert.Equal(Table[g].Label, label.Text);
            Assert.True(label.X > plot.X + plot.W, $"'{label.Text}' starts at {label.X}, not right of the plot area ending at {plot.X + plot.W}");
            Assert.Equal(Stroke(groups[g]), label.Fill);
        }
    }

    /// <summary>One drawn unit is one colour: three lines wear three colours of the cycle, and each line's markers wear the
    /// line's colour.</summary>
    [Fact]
    public void EveryLine_WearsOneColourOfItsOwn_ItsMarkersToo()
    {
        var groups = SeriesGroups(Svg(Seasons, Table));

        var strokes = groups.Select(Stroke).ToList();
        Assert.Equal(Table.Length, strokes.Distinct().Count());
        for (int g = 0; g < groups.Count; g++)
        {
            Assert.All(Named(groups[g], "circle"), c => Assert.Equal(strokes[g], (string)c.Attribute("fill")!));
        }
    }

    // ---- the rank axis ---------------------------------------------------------------------------------------------

    /// <summary>The Y limits run from the lowest rank plus half to one half, reversed: rank 1 on top.</summary>
    [Fact]
    public void TheYLimits_RunFromTheLowestRankPlusHalfDownToOneHalf()
    {
        var axes = AxesOf(ax => ax.BumpChart(Seasons, Table));

        Assert.Equal(3.5, axes.YAxis.Min);
        Assert.Equal(0.5, axes.YAxis.Max);
    }

    /// <summary>The verb sets the Y limits and nothing on the X axis: the X range comes from the data.</summary>
    [Fact]
    public void TheVerb_LeavesTheXAxisToTheData()
    {
        var axes = AxesOf(ax => ax.BumpChart(Seasons, Table));

        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.IsType<MultipleLocator>(axes.YAxis.TickLocator);
        Assert.Null(axes.XAxis.TickLocator);
    }

    /// <summary>A limit of the caller's, set after the call, replaces the verb's.</summary>
    [Fact]
    public void ALimitTheCallerSetsAfterTheCall_Wins()
    {
        var axes = AxesOf(ax => ax.BumpChart(Seasons, Table).SetYLim(10, 0));

        Assert.Equal(10.0, axes.YAxis.Min);
        Assert.Equal(0.0, axes.YAxis.Max);
    }

    /// <summary>Rank 1 is drawn on top and the lowest rank at the bottom: the markers of one season go down the plot in the
    /// order of their ranks, evenly.</summary>
    [Fact]
    public void Rank1_IsOnTop_AndTheLowestRankAtTheBottom()
    {
        var groups = SeriesGroups(Svg(Seasons, Table));

        // The first season holds rank 1 for Hawks, 2 for Owls and 3 for Wrens.
        double rank1 = Dots(groups[0])[0].Cy, rank2 = Dots(groups[1])[0].Cy, rank3 = Dots(groups[2])[0].Cy;
        Assert.True(rank1 < rank2 && rank2 < rank3, $"ranks 1, 2 and 3 stand at {rank1}, {rank2} and {rank3} px: not top to bottom");
        Assert.Equal(rank2 - rank1, rank3 - rank2, 6);
    }

    /// <summary>Half a rank of margin at both ends: a marker at rank 1 and one at the lowest rank are whole circles inside the
    /// plot area, standing half a pitch from its edge (an axis limit clips at the frame; a marker standing on it would be
    /// cut in half).</summary>
    [Fact]
    public void TheEndRanks_KeepHalfARankOfMargin_SoTheirMarkersAreWhole()
    {
        string svg = Svg(Seasons, Table);
        var plot = PlotArea(svg);
        var groups = SeriesGroups(svg);

        double rank1 = Dots(groups[0])[0].Cy, rank2 = Dots(groups[1])[0].Cy, rank3 = Dots(groups[2])[0].Cy;
        double pitch = rank2 - rank1;
        Assert.Equal(0.5 * pitch, rank1 - plot.Y, 6);
        Assert.Equal(0.5 * pitch, plot.Y + plot.H - rank3, 6);
        Assert.True(rank1 - 5 >= plot.Y && rank3 + 5 <= plot.Y + plot.H);
    }

    /// <summary>The Y axis is ticked at every whole rank, from the top down, and nowhere else.</summary>
    [Fact]
    public void TheYAxis_IsTickedAtEveryWholeRank()
    {
        string svg = Svg(Seasons, Table);

        Assert.Equal(["1", "2", "3"], YTickLabels(svg));
        var heights = YTickHeights(svg);
        Assert.True(heights[0] < heights[1] && heights[1] < heights[2], $"the rank ticks stand at {string.Join(", ", heights)} px: not top to bottom");
        // Each tick stands level with the markers of its rank.
        var groups = SeriesGroups(svg);
        Assert.Equal(Dots(groups[0])[0].Cy + TickLabelDrop, heights[0], 6);
        Assert.Equal(Dots(groups[1])[0].Cy + TickLabelDrop, heights[1], 6);
        Assert.Equal(Dots(groups[2])[0].Cy + TickLabelDrop, heights[2], 6);
    }

    /// <summary>A rank past the number of groups (a few teams of a table of six) is drawn, not cut at the frame: the axis
    /// runs down to the lowest rank drawn plus half, and every whole rank in between is ticked.</summary>
    [Fact]
    public void ARankPastTheNumberOfGroups_ExtendsTheAxis_InsteadOfBeingCut()
    {
        BarGroup[] subset = [new("Hawks", [1, 4]), new("Wrens", [6, 2])];

        var axes = AxesOf(ax => ax.BumpChart([1.0, 2.0], subset));
        string svg = Svg([1.0, 2.0], subset);
        var plot = PlotArea(svg);

        Assert.Equal(6.5, axes.YAxis.Min);
        Assert.Equal(["1", "2", "3", "4", "5", "6"], YTickLabels(svg));
        double wrensAtSix = Dots(SeriesGroups(svg)[1])[0].Cy;
        Assert.True(wrensAtSix + 5 <= plot.Y + plot.H, $"the marker at rank 6 reaches {wrensAtSix + 5} past the plot's bottom edge {plot.Y + plot.H}");
    }

    /// <summary>A fractional rank counts to the next whole rank: with 2 groups a rank of 2.5 needs the axis to reach 3.5.</summary>
    [Fact]
    public void AFractionalRankPastTheLastWholeRank_ExtendsTheAxisToTheNextWholeRank()
    {
        BarGroup[] ties = [new("Hawks", [1, 2.5]), new("Owls", [2, 1])];

        var axes = AxesOf(ax => ax.BumpChart([1.0, 2.0], ties));

        Assert.Equal(3.5, axes.YAxis.Min);
        Assert.Equal(["1", "2", "3"], YTickLabels(Svg([1.0, 2.0], ties)));
    }

    /// <summary>Fractional ranks are accepted (the average rank of a tie): the marker stands between its ticks, and the
    /// ticks stay the whole ranks. Measured: the same ticks, and the same picture, after a JSON round trip.</summary>
    [Fact]
    public void AFractionalRank_StandsBetweenItsTicks_AndTheTicksStayWholeAfterJson()
    {
        BarGroup[] fractional = [new("Hawks", [1, 2.5, 3, 3]), new("Owls", [2, 1, 1.5, 2]), new("Wrens", [3, 3, 2, 1])];
        var figure = Plt.Create().WithSize(900, 450).AddSubPlot(1, 1, 1, ax => ax.BumpChart(Seasons, fractional)).Build();
        string svg = figure.ToSvg();

        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(["1", "2", "3"], YTickLabels(svg));
        Assert.Equal(["1", "2", "3"], YTickLabels(readBack));
        Assert.Equal(svg, readBack);
        var heights = YTickHeights(svg);
        double halfway = Dots(SeriesGroups(svg)[0])[1].Cy;
        Assert.Equal((heights[1] + heights[2]) / 2 - TickLabelDrop, halfway, 6);
    }

    // ---- missing ranks and ties ------------------------------------------------------------------------------------

    private static readonly double[] FiveSeasons = [1, 2, 3, 4, 5];

    /// <summary>A missing rank (NaN) leaves a GAP in the line, not a bridge: the line is two runs, none of them passing
    /// through the season without a rank, and that season has no marker.</summary>
    [Fact]
    public void AMissingRank_LeavesAGap_NotABridge()
    {
        BarGroup[] gappy = [new("Hawks", [1, 2, double.NaN, 3, 2]), new("Owls", [2, 1, 1, 1, 1]), new("Wrens", [3, 3, 2, 2, 3])];
        string svg = Svg(FiveSeasons, gappy);
        var groups = SeriesGroups(svg);
        double[] seasonX = [.. Dots(groups[1]).Select(d => d.Cx)];

        var runs = Runs(groups[0]);
        Assert.Equal(2, runs.Count);
        Assert.Equal([seasonX[0], seasonX[1]], runs[0].Select(p => p.Cx));
        Assert.Equal([seasonX[3], seasonX[4]], runs[1].Select(p => p.Cx));
        Assert.Equal([seasonX[0], seasonX[1], seasonX[3], seasonX[4]], Dots(groups[0]).Select(d => d.Cx));
        Assert.DoesNotContain("NaN", svg);
    }

    /// <summary>A point alone between two gaps is a marker with no line.</summary>
    [Fact]
    public void APointAloneBetweenTwoGaps_IsAMarkerWithNoLine()
    {
        BarGroup[] lonely = [new("Hawks", [1, double.NaN, 2, double.NaN, 3]), new("Owls", [2, 1, 1, 1, 1])];

        var hawks = SeriesGroups(Svg(FiveSeasons, lonely))[0];

        Assert.Empty(Runs(hawks));
        Assert.Equal(3, Dots(hawks).Length);
    }

    /// <summary>A group without a single rank that can be drawn draws no line, no marker and no name, and leaves the other
    /// groups drawn; the picture holds no NaN.</summary>
    [Fact]
    public void AGroupWithoutAnyRank_DrawsNothing_AndLeavesTheOthersAlone()
    {
        BarGroup[] groups = [.. Table, new("Ghosts", [double.NaN, double.NaN, double.NaN, double.NaN])];
        string svg = Svg(Seasons, groups);

        var drawn = SeriesGroups(svg);
        var ghosts = drawn.Single(g => (string?)g.Attribute("aria-label") == "Ghosts");
        Assert.Empty(Dots(ghosts));
        Assert.Empty(Runs(ghosts));
        Assert.Null(Label(ghosts));
        Assert.DoesNotContain("NaN", svg);
        Assert.Equal(4, Dots(drawn[0]).Length);
        Assert.Equal(["1", "2", "3", "4"], YTickLabels(svg));
    }

    /// <summary>No rank that can be drawn at all: the axis still spans the groups (one for each line) and the picture holds
    /// no NaN and does not throw.</summary>
    [Fact]
    public void WithNoRankAtAll_TheAxisStillSpansTheGroups()
    {
        BarGroup[] blank = [new("A", [double.NaN, double.NaN]), new("B", [double.NaN, double.NaN])];

        var axes = AxesOf(ax => ax.BumpChart([1.0, 2.0], blank));
        string svg = Svg([1.0, 2.0], blank);

        Assert.Equal(2.5, axes.YAxis.Min);
        Assert.Equal(0.5, axes.YAxis.Max);
        Assert.Equal(["1", "2"], YTickLabels(svg));
        Assert.DoesNotContain("NaN", svg);
    }

    /// <summary>A line whose last rank is missing is named at the last rank that is there: the name stands level with that
    /// marker exactly as the name of a line that ends at its last position stands level with its own.</summary>
    [Fact]
    public void ATrailingMissingRank_NamesTheLineAtItsLastDrawableRank()
    {
        // Hawks end at rank 1, Wrens at rank 3 and Owls at rank 2 (a trailing NaN after their rank 2): three distinct ends.
        BarGroup[] trailing = [new("Hawks", [1, 1, 1, 1]), new("Owls", [2, 3, 2, double.NaN]), new("Wrens", [3, 2, 3, 3])];
        var groups = SeriesGroups(Svg(Seasons, trailing));

        var hawks = Assert.IsType<EndLabel>(Label(groups[0]));
        var owls = Assert.IsType<EndLabel>(Label(groups[1]));
        double hawksOffset = hawks.Y - Dots(groups[0])[^1].Cy;
        double owlsOffset = owls.Y - Dots(groups[1])[^1].Cy;
        Assert.Equal(3, Dots(groups[1]).Length);
        Assert.Equal(hawksOffset, owlsOffset, 6);
    }

    /// <summary>Ties are allowed: two groups at the same rank at the same position stand on the same point, and both still
    /// carry their name. Competition ranking ("1224" or "1223") is the caller's job.</summary>
    [Fact]
    public void TwoGroupsAtTheSameRank_StandOnTheSamePoint()
    {
        BarGroup[] tied = [new("Hawks", [1, 1, 2]), new("Owls", [1, 2, 2]), new("Wrens", [3, 3, 3])];
        var groups = SeriesGroups(Svg([1.0, 2.0, 3.0], tied));

        Assert.Equal(Dots(groups[0])[0], Dots(groups[1])[0]);
        Assert.Equal(Dots(groups[0])[2], Dots(groups[1])[2]);
        Assert.NotEqual(Dots(groups[0])[1], Dots(groups[1])[1]);
        Assert.All(groups, g => Assert.NotNull(Label(g)));
    }

    // ---- the callback ----------------------------------------------------------------------------------------------

    /// <summary>The callback runs once for each line, in the order of the groups, after the verb has built and styled it.</summary>
    [Fact]
    public void TheCallback_RunsOncePerLine_InTheOrderOfTheGroups_AfterTheVerbStyledIt()
    {
        var seen = new List<string?>();

        var axes = AxesOf(ax => ax.BumpChart(Seasons, Table, line =>
        {
            Assert.Equal(SeriesLabelPlacement.LineEnd, line.LabelPlacement);
            Assert.Equal(MarkerStyle.Circle, line.Marker);
            seen.Add(line.Label);
            line.MarkerSize = 4;
        }));

        Assert.Equal(["Hawks", "Owls", "Wrens"], seen);
        Assert.All(Lines(axes), line => Assert.Equal(4, line.MarkerSize));
    }

    /// <summary>What the callback sets wins: it can put a name back in the legend instead of at the end of the line.</summary>
    [Fact]
    public void TheCallback_CanPutTheNamesBackInTheLegend()
    {
        string svg = Svg(ax => ax.BumpChart(Seasons, Table, line => line.LabelPlacement = SeriesLabelPlacement.Legend).WithLegend(),
            legendToggle: true);

        Assert.Equal(Table.Length, Regex.Matches(svg, "data-legend-index=").Count);
        Assert.All(SeriesGroups(svg), g => Assert.Null(Label(g)));
    }

    // ---- the verb draws what the hand chain drew ---------------------------------------------------------------------

    /// <summary>The verb adds nothing to the picture that the hand-written chain did not draw, and leaves nothing out: same
    /// SVG, byte for byte, with and without a missing value.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDrew(bool missing)
    {
        string byVerb = ComposedChartShapes.Named("bump").Build(missing).ToSvg();
        string byHand = ComposedChartShapes.BumpByHand(missing).ToSvg();

        Assert.Equal(byHand, byVerb);
    }

    /// <summary>Written to JSON and read back, the chart draws the same picture with a gap, fractional ranks and a rank past
    /// the number of groups: the names still stand at the end of the lines and the ticks are still whole ranks.</summary>
    [Fact]
    public void TheChart_ReadBackFromJson_DrawsTheSamePicture()
    {
        BarGroup[] mixed = [new("Hawks", [1, 2.5, double.NaN, 7]), new("Owls", [2, 1, 1.5, 2])];
        var figure = Plt.Create().WithSize(900, 450).AddSubPlot(1, 1, 1, ax => ax.BumpChart(Seasons, mixed)).Build();
        string svg = figure.ToSvg();

        string readBack = ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg();

        Assert.Equal(svg, readBack);
        Assert.Equal(["1", "2", "3", "4", "5", "6", "7"], YTickLabels(readBack));
    }

    // ---- no data to speak of ---------------------------------------------------------------------------------------

    /// <summary>No x position at all with groups that hold no ranks is an empty chart, as every other series type draws with
    /// no data: it does not throw and holds no NaN.</summary>
    [Fact]
    public void NoXPositions_DrawAnEmptyRankAxis_WithoutThrowing()
    {
        BarGroup[] empty = [new("Hawks", []), new("Owls", [])];

        string svg = Svg([], empty);

        Assert.Equal(["1", "2"], YTickLabels(svg));
        Assert.DoesNotContain("NaN", svg);
    }

    // ---- refused arguments -----------------------------------------------------------------------------------------

    /// <summary>An axes, an x array or a list of groups that is not there is refused by name.</summary>
    [Fact]
    public void ANullArgument_IsRefusedByName()
    {
        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => ((AxesBuilder)null!).BumpChart(Seasons, Table)).ParamName);
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.BumpChart(null!, Table))).ParamName);
        Assert.Equal("ranks", Assert.Throws<ArgumentNullException>(() => AxesOf(ax => ax.BumpChart(Seasons, null!))).ParamName);
    }

    /// <summary>A bump chart draws one line for each group: no groups is refused, by the parameter.</summary>
    [Fact]
    public void NoGroups_AreRefused()
    {
        var error = Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.BumpChart(Seasons, [])));

        Assert.Equal("ranks", error.ParamName);
        Assert.StartsWith("No groups were given", error.Message);
    }

    /// <summary>A group with more or fewer ranks than x positions is refused, naming the group, both counts and the
    /// parameter, in the words every verb that takes groups uses.</summary>
    [Fact]
    public void AGroupThatDoesNotFitTheXPositions_IsRefusedByName()
    {
        BarGroup[] groups = [Table[0], new("Owls", [2, 1, 1])];

        var error = Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.BumpChart(Seasons, groups)));

        Assert.Equal("ranks", error.ParamName);
        Assert.Equal("Group 'Owls' carries 3 values; there are 4 x values. (Parameter 'ranks')", error.Message);
    }

    /// <summary>A group whose ranks are not there at all is refused by name, not with a null reference.</summary>
    [Fact]
    public void AGroupWithNoRanksAtAll_IsRefusedByName()
    {
        BarGroup[] groups = [Table[0], new("Owls", null!)];

        var error = Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.BumpChart(Seasons, groups)));

        Assert.Equal("ranks", error.ParamName);
        Assert.Equal("Group 'Owls' carries no values; there are 4 x values. (Parameter 'ranks')", error.Message);
    }

    /// <summary>A rank counts from 1: 0, a negative number and an infinity are refused, naming the group, the position and
    /// the value (NaN is a missing rank, not a bad one). A zero-based position would otherwise be cut in half by the frame.</summary>
    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(-2.0, "-2")]
    [InlineData(0.5, "0.5")]
    [InlineData(double.PositiveInfinity, "Infinity")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    public void ARankThatIsNotAPlace_IsRefusedByName(double rank, string written)
    {
        BarGroup[] groups = [Table[0], new("Owls", [2, rank, 1, 2])];

        var error = Assert.Throws<ArgumentException>(() => AxesOf(ax => ax.BumpChart(Seasons, groups)));

        Assert.Equal("ranks", error.ParamName);
        Assert.Equal(
            $"The rank of 'Owls' at x = 2022 is {written}; a rank counts from 1 and is finite (NaN marks a missing rank). (Parameter 'ranks')",
            error.Message);
    }

    /// <summary>A refused call adds nothing to the axes: the checks come before the first line.</summary>
    [Fact]
    public void ARefusedCall_AddsNothingToTheAxes()
    {
        BarGroup[] badRank = [Table[0], new("Owls", [2, 0, 1, 2])];
        BarGroup[] badLength = [Table[0], new("Owls", [2, 1])];
        var builder = new AxesBuilder();

        Assert.Throws<ArgumentException>(() => builder.BumpChart(Seasons, badRank));
        Assert.Throws<ArgumentException>(() => builder.BumpChart(Seasons, badLength));
        var axes = builder.Build(1, 1, 1);

        Assert.Empty(axes.Series);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
        Assert.Null(axes.YAxis.TickLocator);
    }

    // ---- the example in the documentation --------------------------------------------------------------------------

    /// <summary>The example of the verb's documentation, as written there, compiles and draws.</summary>
    [Fact]
    public void TheDocumentationExample_Draws()
    {
        double[] seasons = [2021, 2022, 2023, 2024];

        string svg = Plt.Create()
            .WithTitle("League table")
            .AddSubPlot(1, 1, 1, ax => ax
                .BumpChart(seasons,
                [
                    new BarGroup("Hawks", [1, 2, 3, 3]),
                    new BarGroup("Owls", [2, 1, 1, 2]),
                    new BarGroup("Wrens", [3, 3, 2, 1]),
                ])
                .SetXTickLocator(new MultipleLocator(1))
                .SetXLabel("Season")
                .SetYLabel("Rank"))
            .ToSvg();

        Assert.Contains(">Hawks<", svg);
        Assert.Contains(">Rank<", svg);
        Assert.Contains(">2022<", svg);   // whole years on the X axis, not 2022.0 and 2022.5
    }
}
