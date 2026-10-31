// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>Stairs</c> verb: values in bins between edges, drawn as ONE staircase. Without a baseline it is the outline
/// alone, a <see cref="LineSeries"/> with no drops at its ends (matplotlib's <c>baseline=None</c>). With a baseline it is a
/// single <see cref="AreaSeries"/> filled down to it, whose top edge is the one stroke of the staircase, so no edge is drawn
/// twice. Exactly one of the two parts is set. The staircase itself is <c>StaircasePath</c>, held to its rules in
/// <c>StaircasePathTests</c>; this holds what the verb does with it.</summary>
public class StairsTests
{
    private static readonly Color SeriesColour = Theme.Default.CycleColors[0];

    private const double Gap = double.NaN;

    private static Figure Of(double[] values, double[] edges, double? baseline = null, Action<StairsParts>? configure = null) =>
        Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.Stairs(values, edges, baseline, configure)).Build();

    private static RecordingRenderContext Drawn(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    /// <summary>The strokes of the staircase, each as its points in pixels: the polylines in the series colour at the width of a
    /// line (and of the top edge of an area), in the order drawn.</summary>
    private static List<Point[]> Strokes(RecordingRenderContext drawn) => [.. drawn.OfKind("DrawLines")
        .Select(call => (dynamic)call.Args!)
        .Where(args => (Color)args.color == SeriesColour && (double)args.thickness == 1.5)
        .Select(args => (Point[])args.points)];

    /// <summary>The fills of the staircase, each as its polygon in pixels: the polygons drawn with no outline of their own.</summary>
    private static List<Point[]> Fills(RecordingRenderContext drawn) => [.. drawn.OfKind("DrawPolygon")
        .Select(call => (dynamic)call.Args!)
        .Where(args => args.fill != null && args.stroke == null)
        .Select(args => (Point[])args.points)];

    private static Axes AxesOf(Figure figure) => figure.SubPlots[0];

    private static LineSeries OutlineOf(Figure figure) => Assert.IsType<LineSeries>(Assert.Single(AxesOf(figure).Series));

    private static AreaSeries FillOf(Figure figure) => Assert.IsType<AreaSeries>(Assert.Single(AxesOf(figure).Series));

    // ---- the shape the contracts hold ---------------------------------------------------------------------------------

    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.StairsByHand(false).ToSvg(), ComposedChartShapes.Named("stairs").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.StairsByHand(true).ToSvg(), ComposedChartShapes.Named("stairs").Build(true).ToSvg());
    }

    [Fact]
    public void TheFilledVerb_DrawsExactlyWhatItsHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.StairsFilledByHand(false).ToSvg(), ComposedChartShapes.Named("stairs-filled").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.StairsFilledByHand(true).ToSvg(), ComposedChartShapes.Named("stairs-filled").Build(true).ToSvg());
    }

    // ---- without a baseline: the outline alone ------------------------------------------------------------------------

    [Fact]
    public void WithoutABaseline_TheChartIsOneLineSeries_OfTheStaircase()
    {
        var line = OutlineOf(Of([5, 3, 7], [0, 1, 2, 3]));

        Assert.Equal([0.0, 1.0, 1.0, 2.0, 2.0, 3.0], line.XData);
        Assert.Equal([5.0, 5.0, 3.0, 3.0, 7.0, 7.0], line.YData);
    }

    [Fact]
    public void WithoutABaseline_ThereAreNoDropsAtTheEnds()
    {
        var line = OutlineOf(Of([5, 3, 7], [0, 1, 2, 3]));

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

        Assert.Same(OutlineOf(figure), parts.Outline);
        Assert.Null(parts.Fill);
    }

    [Fact]
    public void WithoutABaseline_TheStaircaseIsOnePolyline_NothingIsFilled()
    {
        var drawn = Drawn(Of([5, 3, 7], [0, 1, 2, 3]));

        var strokes = Strokes(drawn);

        Assert.Single(strokes);
        Assert.Equal(6, strokes[0].Length);
        Assert.Empty(Fills(drawn));
    }

    [Fact]
    public void TheRisers_AreVerticalOnThePage_AndTheTreadsAreLevel()
    {
        var points = Assert.Single(Strokes(Drawn(Of([5, 3, 7], [0, 1, 2, 3]))));

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
        var axes = AxesOf(Of([5, 3, 7], [0, 1, 2, 3], baseline: 0));

        var fill = Assert.IsType<AreaSeries>(Assert.Single(axes.Series));

        Assert.Equal([0.0, 1.0, 1.0, 2.0, 2.0, 3.0], fill.XData);
        Assert.Equal([5.0, 5.0, 3.0, 3.0, 7.0, 7.0], fill.YData);
    }

    [Fact]
    public void WithABaseline_ThePartsHoldTheFillAndNoOutline()
    {
        StairsParts parts = default;
        var figure = Of([5, 3, 7], [0, 1, 2, 3], baseline: 0, configure: p => parts = p);

        Assert.Same(FillOf(figure), parts.Fill);
        Assert.Null(parts.Outline);
    }

    [Fact]
    public void ABaselineOfZero_FillsDownToZero_TheAreasOwnDefault()
    {
        var fill = FillOf(Of([5, 3, 7], [0, 1, 2, 3], baseline: 0));

        Assert.Null(fill.YData2);
    }

    [Fact]
    public void ABaselineOfAnotherValue_FillsDownToIt_AtEveryPointOfTheStaircase()
    {
        var fill = FillOf(Of([5, 3, 7], [0, 1, 2, 3], baseline: 2));

        Assert.Equal([2.0, 2.0, 2.0, 2.0, 2.0, 2.0], fill.YData2!);
    }

    [Fact]
    public void ANegativeBaseline_IsAsGoodAsAnyOther()
    {
        var fill = FillOf(Of([5, 3, 7], [0, 1, 2, 3], baseline: -1.5));

        Assert.Equal(Enumerable.Repeat(-1.5, 6), fill.YData2);
    }

    [Fact]
    public void TheFill_IsStrokedOnceAlongItsTopEdge_AndNotAtAll_AlongTheRest()
    {
        var drawn = Drawn(Of([5, 3, 7], [0, 1, 2, 3], baseline: 1));

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
            var drawn = Drawn(Of([5, 3, 7], [0, 1, 2, 3], baseline));

            Assert.Single(Fills(drawn));
            Assert.Single(Strokes(drawn));
        }
    }

    // ---- a missing value is a gap -------------------------------------------------------------------------------------

    [Fact]
    public void AMissingValue_IsAGap_InTheLine()
    {
        var figure = Of([5, Gap, 7], [0, 1, 2, 3]);

        var strokes = Strokes(Drawn(figure));

        Assert.Equal([5.0, 5.0, Gap, Gap, 7.0, 7.0], OutlineOf(figure).YData);
        Assert.Equal(2, strokes.Count);
        Assert.All(strokes, stroke => Assert.Equal(2, stroke.Length));
        // Parted: the first run ends where the second begins to the right of it, with the missing bin between them.
        Assert.True(strokes[0][1].X < strokes[1][0].X);
    }

    [Fact]
    public void AMissingValue_IsAGap_InTheFill_AndInItsStroke()
    {
        var drawn = Drawn(Of([5, Gap, 7], [0, 1, 2, 3], baseline: 0));

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
        var zero = Drawn(Of([5, 0, 7], [0, 1, 2, 3]));
        var missing = Drawn(Of([5, Gap, 7], [0, 1, 2, 3]));

        // A bin at zero is a bin: one line, three bins. A missing bin is no bin: two lines, two bins.
        Assert.Single(Strokes(zero));
        Assert.Equal(6, Strokes(zero)[0].Length);
        Assert.Equal(2, Strokes(missing).Count);
    }

    [Fact]
    public void AMissingEdge_MasksBothBinsNextToIt_InTheLine()
    {
        var figure = Of([5, 3, 7, 2], [0, 1, Gap, 3, 4]);

        var strokes = Strokes(Drawn(figure));

        // Bins 1 and 2 touch the missing edge: only bin 0 and bin 3 are left, each a level tread of two points, no stub.
        Assert.Equal(2, strokes.Count);
        Assert.All(strokes, stroke => Assert.Equal(2, stroke.Length));
    }

    [Fact]
    public void AMissingEdge_MasksBothBinsNextToIt_InTheFill()
    {
        var drawn = Drawn(Of([5, 3, 7, 2], [0, 1, Gap, 3, 4], baseline: 0));

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

        var drawn = Drawn(figure);

        Assert.Empty(Strokes(drawn));
        Assert.Empty(Fills(drawn));
        Assert.NotEmpty(figure.ToSvg());
    }

    [Fact]
    public void TheSvg_NeverCarriesAMissingValue()
    {
        Assert.DoesNotContain("NaN", Of([5, Gap, 7], [0, 1, 2, 3]).ToSvg());
        Assert.DoesNotContain("NaN", Of([5, Gap, 7], [0, 1, 2, 3], baseline: 0).ToSvg());
        Assert.DoesNotContain("NaN", Of([5, 3, 7, 2], [0, 1, Gap, 3, 4]).ToSvg());
        Assert.DoesNotContain("NaN", Of([5, 3, 7, 2], [0, 1, Gap, 3, 4], baseline: 1).ToSvg());
    }

    // ---- edges and values --------------------------------------------------------------------------------------------

    [Fact]
    public void NoBins_DrawNothing_AndDoNotBreakTheFigure()
    {
        var figure = Of([], [3]);

        Assert.Empty(OutlineOf(figure).XData);
        Assert.NotEmpty(figure.ToSvg());
        Assert.NotEmpty(Of([], [3], baseline: 0).ToSvg());
    }

    [Fact]
    public void OneBin_IsOneTread()
    {
        var points = Assert.Single(Strokes(Drawn(Of([4], [1, 3]))));

        Assert.Equal(2, points.Length);
        Assert.Equal(points[0].Y, points[1].Y, 9);
    }

    [Fact]
    public void TheBins_NeedNotBeEqualInWidth()
    {
        var line = OutlineOf(Of([1, 2], [0, 0.5, 4]));

        Assert.Equal([0.0, 0.5, 0.5, 4.0], line.XData);
    }

    [Fact]
    public void TheArraysTheCallerGave_AreNotKept()
    {
        double[] values = [5, 3];
        double[] edges = [0, 1, 2];
        var line = OutlineOf(Of(values, edges));

        values[0] = 99;
        edges[0] = 99;

        Assert.Equal(0.0, line.XData[0]);
        Assert.Equal(5.0, line.YData[0]);
    }

    // ---- the call ----------------------------------------------------------------------------------------------------

    [Fact]
    public void TheStaircase_IsOneColourSlot_TheNextSeriesTakesTheNextColour()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Stairs([5, 3, 7], [0, 1, 2, 3])
            .Plot([0.0, 4.0], [1.0, 2.0])).Build();

        var drawn = Drawn(figure);

        Assert.Contains(drawn.OfKind("DrawLines").Select(call => (Color)((dynamic)call.Args!).color), colour => colour == Theme.Default.CycleColors[1]);
        Assert.Single(Strokes(drawn));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    public void TheStaircase_IsOneLegendEntry_InEitherForm(double? baseline)
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Stairs([5, 3, 7, 2, 4], [0, 1, 2, 3, 4, 5], baseline, parts => ((ISeries?)parts.Outline ?? parts.Fill)!.Label = "counts")
            .WithLegend()).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        Assert.Single(Regex.Matches(svg, "data-legend-index="));
        Assert.Contains("counts", svg);
    }

    [Fact]
    public void WhatTheCallbackSets_WinsOverWhatTheVerbDid()
    {
        var line = Of([5, 3], [0, 1, 2], configure: p => { p.Outline!.Color = Colors.Red; p.Outline.LineWidth = 4; p.Outline.LineStyle = LineStyle.Dashed; });
        var fill = Of([5, 3], [0, 1, 2], baseline: 0, configure: p => { p.Fill!.Color = Colors.Blue; p.Fill.Alpha = 0.8; p.Fill.EdgeColor = Colors.Black; });

        Assert.Equal(Colors.Red, OutlineOf(line).Color);
        Assert.Equal(4.0, OutlineOf(line).LineWidth);
        Assert.Contains(Drawn(line).OfKind("DrawLines"), call => (Color)((dynamic)call.Args!).color == Colors.Red);
        Assert.Equal(Colors.Blue, FillOf(fill).Color);
        Assert.Equal(0.8, FillOf(fill).Alpha);
        Assert.Contains(Drawn(fill).OfKind("DrawLines"), call => (Color)((dynamic)call.Args!).color == Colors.Black);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    public void TheCallback_RunsOnce_OnThePartsOfTheSeriesTheVerbAdded(double? baseline)
    {
        var seen = new List<StairsParts>();

        var figure = Of([5, 3], [0, 1, 2], baseline, seen.Add);

        var parts = Assert.Single(seen);
        Assert.Same(Assert.Single(AxesOf(figure).Series), (ISeries?)parts.Outline ?? parts.Fill);
    }

    [Fact]
    public void TheVerb_ChainsWithTheRestOfTheBuilder()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Same(ax, ax.Stairs([1], [0, 1]));
            Assert.Same(ax, ax.Stairs([1], [0, 1], 0));
        });

        Assert.Equal(2, builder.Build().SubPlots[0].Series.Count);
    }

    // ---- the JSON ----------------------------------------------------------------------------------------------------

    [Fact]
    public void TheOutline_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of([5, Gap, 7], [0, 1, 2, 3], configure: p => p.Outline!.Label = "counts");

        string json = figure.ToJson();
        var back = ChartSerializerDefault.Current.FromJson(json);

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.IsType<LineSeries>(Assert.Single(back.SubPlots[0].Series));
        Assert.Contains("\"NaN\"", json);
    }

    [Fact]
    public void TheFill_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of([5, Gap, 7], [0, 1, 2, 3], baseline: 1, configure: p => p.Fill!.Label = "counts");

        string json = figure.ToJson();
        var back = ChartSerializerDefault.Current.FromJson(json);

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.IsType<AreaSeries>(Assert.Single(back.SubPlots[0].Series));
        Assert.Contains("\"NaN\"", json);
    }

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
        Assert.Contains("4", thrown.Message);
        Assert.Contains("2", thrown.Message);
    }

    [Fact]
    public void AsManyEdgesAsValues_IsRefused_BecauseABinHasTwoEdges()
    {
        Assert.Equal("edges", Assert.Throws<ArgumentException>(() => Of([1, 2, 3], [0, 1, 2], baseline: 0)).ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ABaselineThatCannotBeDrawn_IsRefused_ByItsName(double baseline)
    {
        var thrown = Assert.Throws<ArgumentException>(() => Of([1, 2], [0, 1, 2], baseline));

        Assert.Equal("baseline", thrown.ParamName);
    }

    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Throws<ArgumentException>(() => ax.Stairs([1, 2, 3], [0, 1, 2]));
            Assert.Throws<ArgumentException>(() => ax.Stairs([1, 2], [0, 1, 2], double.NaN));
        }).Build().SubPlots[0];

        Assert.Empty(axes.Series);
    }

    [Fact]
    public void ANullArgument_IsRefusedByName()
    {
        Assert.Equal("values", Assert.Throws<ArgumentNullException>(() => Of(null!, [0.0, 1.0])).ParamName);
        Assert.Equal("edges", Assert.Throws<ArgumentNullException>(() => Of([1.0], null!)).ParamName);
    }

    [Fact]
    public void ANullBuilder_IsRefused()
    {
        AxesBuilder axes = null!;

        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => axes.Stairs([1.0], [0.0, 1.0])).ParamName);
    }
}
