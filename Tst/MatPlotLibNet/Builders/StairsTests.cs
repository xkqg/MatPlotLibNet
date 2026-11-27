// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>Stairs</c> verb: values in bins between edges, drawn as ONE staircase. Without a baseline it is the outline
/// alone, a <see cref="LineSeries"/> with no drops at its ends (matplotlib's <c>baseline=None</c>). With a baseline it is a
/// single <see cref="AreaSeries"/> filled down to it, whose top edge is the one stroke of the staircase, so no edge is drawn
/// twice. Exactly one of the two parts is set. The staircase: bin <c>i</c> runs from edge <c>i</c> to edge <c>i + 1</c> at its value,
/// so it contributes two points at the same height, and the next bin begins where this one ends, which is the riser; a bin whose
/// value or either edge cannot be drawn is a gap, both its points missing, so neither the outline nor the fill bridges it.</summary>
public class StairsTests
{
    private static readonly Color SeriesColour = Theme.Default.CycleColors[0];

    private const double Gap = double.NaN;

    private static Figure Of(double[] values, double[] edges, double? baseline = null, Action<StairsParts>? configure = null) =>
        TestFigures.OfOneVerb(ax => ax.Stairs(values, edges, baseline, configure));

    /// <summary>The strokes of the staircase, each as its points in pixels: the polylines in the series colour at the width of a
    /// line (the outline names none, so it is the theme's) or of the top edge of the fill (the verb names its 1.5), in the order
    /// drawn.</summary>
    private static List<Point[]> Strokes(RecordingRenderContext drawn) => [.. drawn.Of<DrawLinesCall>()
        .Where(args => args.Color == SeriesColour
            && (args.Thickness == Theme.MatplotlibV2.Looks.Lines.Width || args.Thickness == LineWidthRule.Line.Resting))
        .Select(args => args.Points)];

    /// <summary>The fills of the staircase, each as its polygon in pixels: the polygons drawn with no outline of their own.</summary>
    private static List<Point[]> Fills(RecordingRenderContext drawn) => [.. drawn.Of<DrawPolygonCall>()
        .Where(args => args.Fill != null && args.Stroke == null)
        .Select(args => args.Points)];

    // ---- without a baseline: the outline alone ------------------------------------------------------------------------

    [Fact]
    public void WithoutABaseline_TheChartIsOneLineSeries_OfTheStaircase()
    {
        var line = Of([5, 3, 7], [0, 1, 2, 3]).OnlySeries<LineSeries>();

        Assert.Equal([0.0, 1.0, 1.0, 2.0, 2.0, 3.0], line.XData);
        Assert.Equal([5.0, 5.0, 3.0, 3.0, 7.0, 7.0], line.YData);
    }

    [Fact]
    public void WithoutABaseline_ThereAreNoDropsAtTheEnds()
    {
        var line = Of([5, 3, 7], [0, 1, 2, 3]).OnlySeries<LineSeries>();

        // The first point stands at the height of the first value and the last at the height of the last: nothing runs down to a
        // baseline before the first edge or after the last.
        Assert.Equal(5.0, line.YData[0]);
        Assert.Equal(7.0, line.YData[^1]);
        Assert.DoesNotContain(0.0, line.YData);
    }

    [Fact]
    public void WithoutABaseline_ThePartsHoldTheOutlineAndNoFill()
    {
        StairsParts parts = default;
        var figure = Of([5, 3, 7], [0, 1, 2, 3], configure: p => parts = p);

        Assert.Same(figure.OnlySeries<LineSeries>(), parts.Outline);
        Assert.Null(parts.Fill);
    }

    [Fact]
    public void WithoutABaseline_TheStaircaseIsOnePolyline_NothingIsFilled()
    {
        var drawn = Of([5, 3, 7], [0, 1, 2, 3]).Drawn();

        var strokes = Strokes(drawn);

        Assert.Single(strokes);
        Assert.Equal(6, strokes[0].Length);
        Assert.Empty(Fills(drawn));
    }

    [Fact]
    public void TheRisers_AreVerticalOnThePage_AndTheTreadsAreLevel()
    {
        var points = Assert.Single(Strokes(Of([5, 3, 7], [0, 1, 2, 3]).Drawn()));

        for (int i = 0; i < points.Length; i += 2)
        {
            Assert.Equal(points[i].Y, points[i + 1].Y, 9);   // a bin is level
        }

        for (int i = 1; i + 1 < points.Length; i += 2)
        {
            Assert.Equal(points[i].X, points[i + 1].X, 9);   // a riser is upright
        }
    }

    // ---- with a baseline: one area, one stroke ------------------------------------------------------------------------

    [Fact]
    public void WithABaseline_TheChartIsOneAreaSeriesAndNothingElse()
    {
        var axes = (Of([5, 3, 7], [0, 1, 2, 3], baseline: 0)).SubPlots[0];

        var fill = Assert.IsType<AreaSeries>(Assert.Single(axes.Series));

        Assert.Equal([0.0, 1.0, 1.0, 2.0, 2.0, 3.0], fill.XData);
        Assert.Equal([5.0, 5.0, 3.0, 3.0, 7.0, 7.0], fill.YData);
    }

    [Fact]
    public void WithABaseline_ThePartsHoldTheFillAndNoOutline()
    {
        StairsParts parts = default;
        var figure = Of([5, 3, 7], [0, 1, 2, 3], baseline: 0, configure: p => parts = p);

        Assert.Same(figure.OnlySeries<AreaSeries>(), parts.Fill);
        Assert.Null(parts.Outline);
    }

    [Fact]
    public void ABaselineOfZero_FillsDownToZero_TheAreasOwnDefault()
    {
        var fill = Of([5, 3, 7], [0, 1, 2, 3], baseline: 0).OnlySeries<AreaSeries>();

        Assert.Null(fill.YData2);
    }

    [Fact]
    public void ABaselineOfAnotherValue_FillsDownToIt_AtEveryPointOfTheStaircase()
    {
        var fill = Of([5, 3, 7], [0, 1, 2, 3], baseline: 2).OnlySeries<AreaSeries>();

        Assert.Equal([2.0, 2.0, 2.0, 2.0, 2.0, 2.0], fill.YData2!);
    }

    [Fact]
    public void ANegativeBaseline_IsAsGoodAsAnyOther()
    {
        var fill = Of([5, 3, 7], [0, 1, 2, 3], baseline: -1.5).OnlySeries<AreaSeries>();

        Assert.Equal(Enumerable.Repeat(-1.5, 6), fill.YData2);
    }

    [Fact]
    public void TheFill_IsStrokedOnceAlongItsTopEdge_AndNotAtAll_AlongTheRest()
    {
        var drawn = Of([5, 3, 7], [0, 1, 2, 3], baseline: 1).Drawn();

        var strokes = Strokes(drawn);
        var fills = Fills(drawn);

        // One fill and ONE stroke: the fill has no outline of its own, so no edge of the staircase is drawn twice.
        var fill = Assert.Single(fills);
        var stroke = Assert.Single(strokes);
        Assert.Equal(6, stroke.Length);
        // The stroke IS the top edge of the fill: the first half of its polygon, point for point. The second half is the baseline.
        Assert.Equal(stroke, fill.Take(6));
        Assert.Equal(12, fill.Length);
        Assert.All(fill.Skip(6), point => Assert.Equal(fill[6].Y, point.Y, 9));
    }

    [Fact]
    public void TheFill_AtAnyBaseline_IsStillOneAreaWithOneStroke()
    {
        foreach (double? baseline in new double?[] { 0, 2, -3 })
        {
            var drawn = Of([5, 3, 7], [0, 1, 2, 3], baseline).Drawn();

            Assert.Single(Fills(drawn));
            Assert.Single(Strokes(drawn));
        }
    }

    // ---- a missing value is a gap -------------------------------------------------------------------------------------

    [Fact]
    public void AMissingValue_IsAGap_InTheLine()
    {
        var figure = Of([5, Gap, 7], [0, 1, 2, 3]);

        var strokes = Strokes(figure.Drawn());

        Assert.Equal([5.0, 5.0, Gap, Gap, 7.0, 7.0], figure.OnlySeries<LineSeries>().YData);
        Assert.Equal(2, strokes.Count);
        Assert.All(strokes, stroke => Assert.Equal(2, stroke.Length));
        // Parted: the first run ends where the second begins to the right of it, with the missing bin between them.
        Assert.True(strokes[0][1].X < strokes[1][0].X);
    }

    [Fact]
    public void AMissingValue_IsAGap_InTheFill_AndInItsStroke()
    {
        var drawn = Of([5, Gap, 7], [0, 1, 2, 3], baseline: 0).Drawn();

        var fills = Fills(drawn);
        var strokes = Strokes(drawn);

        Assert.Equal(2, fills.Count);
        Assert.Equal(2, strokes.Count);
        Assert.True(fills[0].Max(p => p.X) < fills[1].Min(p => p.X), "the two fills are parted by the missing bin, not bridged");
        Assert.True(strokes[0].Max(p => p.X) < strokes[1].Min(p => p.X));
    }

    [Fact]
    public void AMissingValue_IsNeverAZero()
    {
        var zero = Of([5, 0, 7], [0, 1, 2, 3]).Drawn();
        var missing = Of([5, Gap, 7], [0, 1, 2, 3]).Drawn();

        // A bin at zero is a bin: one line, three bins. A missing bin is no bin: two lines, two bins.
        Assert.Single(Strokes(zero));
        Assert.Equal(6, Strokes(zero)[0].Length);
        Assert.Equal(2, Strokes(missing).Count);
    }

    [Fact]
    public void AMissingEdge_MasksBothBinsNextToIt_InTheLine()
    {
        var figure = Of([5, 3, 7, 2], [0, 1, Gap, 3, 4]);

        var strokes = Strokes(figure.Drawn());

        // Bins 1 and 2 touch the missing edge: only bin 0 and bin 3 are left, each a level tread of two points, no stub.
        Assert.Equal(2, strokes.Count);
        Assert.All(strokes, stroke => Assert.Equal(2, stroke.Length));
    }

    [Fact]
    public void AMissingEdge_MasksBothBinsNextToIt_InTheFill()
    {
        var drawn = Of([5, 3, 7, 2], [0, 1, Gap, 3, 4], baseline: 0).Drawn();

        var fills = Fills(drawn);
        var strokes = Strokes(drawn);

        // Bins 0 and 3 are left: each a level tread of two points along the top and two along the baseline, with no stub of the
        // bins that touch the missing edge.
        Assert.Equal(2, fills.Count);
        Assert.All(fills, fill => Assert.Equal(4, fill.Length));
        Assert.Equal(2, strokes.Count);
        Assert.All(strokes, stroke => Assert.Equal(2, stroke.Length));
    }

    [Fact]
    public void WhenEveryBinIsMissing_NothingIsDrawn_AndTheFigureStillRenders()
    {
        var figure = Of([Gap, Gap], [0, 1, 2], baseline: 0);

        var drawn = figure.Drawn();

        Assert.Empty(Strokes(drawn));
        Assert.Empty(Fills(drawn));
        Assert.NotEmpty(figure.ToSvg());
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("TheSvg_NeverCarriesAMissingValue (#1)", () => Of([5, Gap, 7], [0, 1, 2, 3]).ToSvg()) { Finite = true },
        new DrawnToken("TheSvg_NeverCarriesAMissingValue (#2)", () => Of([5, Gap, 7], [0, 1, 2, 3], baseline: 0).ToSvg()) { Finite = true },
        new DrawnToken("TheSvg_NeverCarriesAMissingValue (#3)", () => Of([5, 3, 7, 2], [0, 1, Gap, 3, 4]).ToSvg()) { Finite = true },
        new DrawnToken("TheSvg_NeverCarriesAMissingValue (#4)", () => Of([5, 3, 7, 2], [0, 1, Gap, 3, 4], baseline: 1).ToSvg()) { Finite = true },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    // ---- edges and values --------------------------------------------------------------------------------------------

    [Fact]
    public void NoBins_DrawNothing_AndDoNotBreakTheFigure()
    {
        var figure = Of([], [3]);

        Assert.Empty(figure.OnlySeries<LineSeries>().XData);
        Assert.NotEmpty(figure.ToSvg());
        Assert.NotEmpty(Of([], [3], baseline: 0).ToSvg());
    }

    [Fact]
    public void OneBin_IsOneTread()
    {
        var points = Assert.Single(Strokes(Of([4], [1, 3]).Drawn()));

        Assert.Equal(2, points.Length);
        Assert.Equal(points[0].Y, points[1].Y, 9);
    }

    [Fact]
    public void ManyBins_FollowEachOther_TheRiserBeingTheStepFromOneValueToTheNext()
    {
        var line = Of([5, 3, 7], [0, 1, 2, 3]).OnlySeries<LineSeries>();

        Assert.Equal([0.0, 1.0, 1.0, 2.0, 2.0, 3.0], line.XData);
        Assert.Equal([5.0, 5.0, 3.0, 3.0, 7.0, 7.0], line.YData);
    }

    [Fact]
    public void AZeroValue_IsADrawnBin_NotAGap()
    {
        Assert.Equal([0.0, 0.0], Of([0], [0, 1]).OnlySeries<LineSeries>().YData);
    }

    /// <summary>A missing value makes a gap of its bin and keeps the edges it stands between; a missing edge masks both bins next to
    /// it (bin 1 ends in edge 2 and bin 2 begins with it, so neither can be placed); an infinity is no drawable number either; and the
    /// staircase holds two points for every bin, missing or not.</summary>
    [Theory]
    [InlineData(Gap)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void AValueOrAnEdgeThatCannotBeDrawn_MakesAGap_OfTheBinsItBelongsTo(double bad)
    {
        var inValue = Of([5, bad, 7], [0, 1, 2, 3]).OnlySeries<LineSeries>();
        var inEdge = Of([5, 3, 7, 2], [0, 1, bad, 3, 4]).OnlySeries<LineSeries>();

        Assert.Equal([0.0, 1.0, 1.0, 2.0, 2.0, 3.0], inValue.XData);
        Assert.Equal([5.0, 5.0, Gap, Gap, 7.0, 7.0], inValue.YData);
        Assert.Equal([0.0, 1.0, 1.0, bad, bad, 3.0, 3.0, 4.0], inEdge.XData);
        Assert.Equal([5.0, 5.0, Gap, Gap, Gap, Gap, 2.0, 2.0], inEdge.YData);
    }

    [Fact]
    public void TheBins_NeedNotBeEqualInWidth()
    {
        var line = Of([1, 2], [0, 0.5, 4]).OnlySeries<LineSeries>();

        Assert.Equal([0.0, 0.5, 0.5, 4.0], line.XData);
    }

    [Fact]
    public void TheArraysTheCallerGave_AreNotKept()
    {
        double[] values = [5, 3];
        double[] edges = [0, 1, 2];
        var line = Of(values, edges).OnlySeries<LineSeries>();

        values[0] = 99;
        edges[0] = 99;

        Assert.Equal(0.0, line.XData[0]);
        Assert.Equal(5.0, line.YData[0]);
    }

    // ---- the JSON ----------------------------------------------------------------------------------------------------

    [Fact]
    public void AMissingEdge_SurvivesTheJson_AsAMissingX_InBothForms()
    {
        foreach (double? baseline in new double?[] { null, 1 })
        {
            var figure = Of([5, 3, 7, 2], [0, 1, Gap, 3, 4], baseline);

            string json = figure.ToJson();
            var back = ChartSerializerDefault.Current.FromJson(json);

            Assert.Contains("\"xData\":[0,1,1,\"NaN\",\"NaN\",3,3,4]", json);
            Assert.Equal(figure.ToSvg(), back.ToSvg());
            var xData = ((XYSeries)Assert.Single(back.SubPlots[0].Series)).XData;
            Assert.Equal([0.0, 1.0, 1.0, Gap, Gap, 3.0, 3.0, 4.0], xData);
        }
    }

    // ---- what it refuses ---------------------------------------------------------------------------------------------

    [Fact]
    public void FewerEdgesThanTheBinsNeed_IsRefused_ByTheNameOfTheEdges()
    {
        Assert.Equal("edges", Assert.Throws<ArgumentException>(() => Of([1, 2, 3], [0, 1, 2])).ParamName);
    }

    [Fact]
    public void MoreEdgesThanTheBinsNeed_IsRefused_ByTheNameOfTheEdges()
    {
        var thrown = Assert.Throws<ArgumentException>(() => Of([1, 2], [0, 1, 2, 3]));

        Assert.Equal("edges", thrown.ParamName);
        Assert.StartsWith(4.RefusalAgainst(3), thrown.Message);
    }

    [Fact]
    public void AsManyEdgesAsValues_IsRefused_BecauseABinHasTwoEdges()
    {
        Assert.Equal("edges", Assert.Throws<ArgumentException>(() => Of([1, 2, 3], [0, 1, 2], baseline: 0)).ParamName);
        Assert.Equal("edges", Assert.Throws<ArgumentException>(() => Of([], [])).ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ABaselineThatCannotBeDrawn_IsRefused_ByItsName(double baseline)
    {
        var thrown = Assert.Throws<ArgumentOutOfRangeException>(() => Of([1, 2], [0, 1, 2], baseline));

        Assert.Equal("baseline", thrown.ParamName);
    }

    [Fact]
    public void ANullArgument_IsRefusedByName()
    {
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => Of(null!, [0.0, 1.0])).ParamName);
        Assert.Equal("edges", Assert.Throws<ArgumentNullException>(() => Of([1.0], null!)).ParamName);
    }
}
