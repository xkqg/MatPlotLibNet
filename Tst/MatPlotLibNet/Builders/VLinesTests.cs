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

/// <summary>The <c>VLines</c> verb: a vertical line segment at each x position between two limits, drawn as ONE line series
/// (one colour slot, one legend entry) whose segments are parted by missing points, so the picture is one polyline for each
/// segment and the chart travels through JSON as that one series. The geometry itself is <c>SegmentPath</c>, held to its
/// rules in <c>SegmentPathTests</c>; this holds what the verb does with it.</summary>
public class VLinesTests
{
    private static readonly Color SeriesColour = Theme.Default.CycleColors[0];

    private static Figure Of(double[] x, double[] yMin, double[] yMax, Action<LineSeries>? configure = null) =>
        Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.VLines(x, yMin, yMax, configure)).Build();

    private static RecordingRenderContext Drawn(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    /// <summary>The polylines the series is drawn with, each as its points in pixels, in the order drawn.</summary>
    private static List<Point[]> Polylines(RecordingRenderContext drawn) => [.. drawn.OfKind("DrawLines")
        .Select(call => (dynamic)call.Args!)
        .Where(args => (Color)args.color == SeriesColour && (double)args.thickness == 1.5)
        .Select(args => (Point[])args.points)];

    private static LineSeries OnlySeriesOf(Figure figure) => Assert.IsType<LineSeries>(Assert.Single(figure.SubPlots[0].Series));

    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.VLinesByHand(false).ToSvg(), ComposedChartShapes.Named("vlines").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.VLinesByHand(true).ToSvg(), ComposedChartShapes.Named("vlines").Build(true).ToSvg());
    }

    [Fact]
    public void TheChart_IsOneLineSeries_OfTwoEndsAndAGapForEachSegment()
    {
        var line = OnlySeriesOf(Of([1, 2, 4], [0, 1, 2], [3, 4, 5]));

        Assert.Equal([1.0, 1.0, double.NaN, 2.0, 2.0, double.NaN, 4.0, 4.0, double.NaN], line.XData);
        Assert.Equal([0.0, 3.0, double.NaN, 1.0, 4.0, double.NaN, 2.0, 5.0, double.NaN], line.YData);
    }

    [Fact]
    public void EachSegment_IsAVerticalPolylineOfItsOwn()
    {
        var polylines = Polylines(Drawn(Of([1, 2, 4], [0, 1, 2], [3, 4, 5])));

        Assert.Equal(3, polylines.Count);
        Assert.All(polylines, line =>
        {
            Assert.Equal(2, line.Length);
            Assert.Equal(line[0].X, line[1].X, 9);   // vertical on the page
            Assert.NotEqual(line[0].Y, line[1].Y);
        });
        // Left to right in the order of the positions, so the second segment stands right of the first.
        Assert.True(polylines[0][0].X < polylines[1][0].X && polylines[1][0].X < polylines[2][0].X);
    }

    [Fact]
    public void TheSvg_HoldsOnePolylineForEachSegment()
    {
        string svg = Of([1, 2, 4, 5], [0, 0, 0, 0], [3, 4, 5, 6]).ToSvg();

        Assert.Equal(4, Regex.Matches(svg, "<polyline").Count);
        Assert.DoesNotContain("NaN", svg);
    }

    [Fact]
    public void ASegmentWithAMissingEnd_IsLeftOut_AndTheOthersStayParted()
    {
        var figure = Of([1, 2, 4], [0, 1, 2], [3, double.NaN, 5]);

        var line = OnlySeriesOf(figure);

        Assert.Equal([1.0, 1.0, double.NaN, 4.0, 4.0, double.NaN], line.XData);
        Assert.Equal(2, Polylines(Drawn(figure)).Count);
    }

    [Fact]
    public void AMissingPosition_LeavesOutItsSegment()
    {
        var line = OnlySeriesOf(Of([1, double.NaN, 4], [0, 0, 0], [3, 3, 3]));

        Assert.Equal([1.0, 1.0, double.NaN, 4.0, 4.0, double.NaN], line.XData);
    }

    [Fact]
    public void ALimitGivenOnce_ServesEverySegment()
    {
        var line = OnlySeriesOf(Of([1, 2, 3], [0], [5]));

        Assert.Equal([1.0, 1.0, double.NaN, 2.0, 2.0, double.NaN, 3.0, 3.0, double.NaN], line.XData);
        Assert.Equal([0.0, 5.0, double.NaN, 0.0, 5.0, double.NaN, 0.0, 5.0, double.NaN], line.YData);
    }

    [Fact]
    public void APositionGivenOnce_ServesEverySegment()
    {
        var line = OnlySeriesOf(Of([4], [0, 1, 2], [5, 6, 7]));

        Assert.Equal([4.0, 4.0, double.NaN, 4.0, 4.0, double.NaN, 4.0, 4.0, double.NaN], line.XData);
        Assert.Equal([0.0, 5.0, double.NaN, 1.0, 6.0, double.NaN, 2.0, 7.0, double.NaN], line.YData);
    }

    [Fact]
    public void TheLimits_NeedNotBeInOrder_ASegmentRunsFromTheFirstToTheSecond()
    {
        var line = OnlySeriesOf(Of([1], [5], [2]));

        Assert.Equal([5.0, 2.0, double.NaN], line.YData);
    }

    [Fact]
    public void NoSegments_DrawNothing_AndDoNotBreakTheFigure()
    {
        var figure = Of([], [], []);

        Assert.Empty(OnlySeriesOf(figure).XData);
        Assert.NotEmpty(figure.ToSvg());
    }

    [Fact]
    public void TheSegments_AreOneLegendEntry()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .VLines([1, 2, 3, 4, 5], [0, 0, 0, 0, 0], [3, 4, 5, 6, 7], line => line.Label = "levels")
            .WithLegend()).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        Assert.Single(Regex.Matches(svg, "data-legend-index="));
        Assert.Contains("levels", svg);
    }

    [Fact]
    public void TheSegments_AreOneColourSlot_TheNextSeriesTakesTheNextColour()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .VLines([1, 2, 3], [0, 0, 0], [3, 4, 5])
            .Plot([0.0, 4.0], [1.0, 2.0])).Build();

        var drawn = Drawn(figure);

        Assert.Equal(3, Polylines(drawn).Count);
        Assert.Contains(drawn.OfKind("DrawLines").Select(call => (Color)((dynamic)call.Args!).color), colour => colour == Theme.Default.CycleColors[1]);
    }

    [Fact]
    public void WhatTheCallbackSets_WinsOverWhatTheVerbDid()
    {
        var figure = Of([1, 2], [0, 0], [3, 4], line => { line.Color = Colors.Red; line.LineWidth = 4; line.LineStyle = LineStyle.Dashed; });

        var line = OnlySeriesOf(figure);

        Assert.Equal(Colors.Red, line.Color);
        Assert.Equal(4.0, line.LineWidth);
        Assert.Contains(Drawn(figure).OfKind("DrawLines"), call => (Color)((dynamic)call.Args!).color == Colors.Red);
    }

    [Fact]
    public void TheCallback_RunsOnceOnTheSeriesTheVerbAdded()
    {
        var seen = new List<LineSeries>();

        var figure = Of([1, 2], [0, 0], [3, 4], seen.Add);

        Assert.Same(OnlySeriesOf(figure), Assert.Single(seen));
    }

    [Fact]
    public void TheVerb_ChainsWithTheRestOfTheBuilder()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            var same = ax.VLines([1], [0], [1]);
            Assert.Same(ax, same);
        });

        Assert.Single(builder.Build().SubPlots[0].Series);
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of([1, 2, 4], [0, 1, 2], [3, double.NaN, 5], line => line.Label = "levels");

        string json = figure.ToJson();
        var back = ChartSerializerDefault.Current.FromJson(json);

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Single(back.SubPlots[0].Series);
        Assert.Contains("\"NaN\"", json);
    }

    [Fact]
    public void ALimitOfAnotherLengthThanThePositions_IsRefusedByName()
    {
        Assert.Equal("yMax", Assert.Throws<ArgumentException>(() => Of([1, 2, 3], [0, 0, 0], [3, 4])).ParamName);
        Assert.Equal("yMin", Assert.Throws<ArgumentException>(() => Of([1, 2, 3], [0, 0], [3, 4, 5])).ParamName);
    }

    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Throws<ArgumentException>(() => ax.VLines([1, 2, 3], [0, 0], [3, 4, 5]));
        }).Build().SubPlots[0];

        Assert.Empty(axes.Series);
    }

    [Fact]
    public void ANullArgument_IsRefusedByName()
    {
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => Of(null!, [0.0], [1.0])).ParamName);
        Assert.Equal("yMin", Assert.Throws<ArgumentNullException>(() => Of([1.0], null!, [1.0])).ParamName);
        Assert.Equal("yMax", Assert.Throws<ArgumentNullException>(() => Of([1.0], [0.0], null!)).ParamName);
    }

    [Fact]
    public void ANullBuilder_IsRefused()
    {
        AxesBuilder axes = null!;

        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => axes.VLines([1.0], [0.0], [1.0])).ParamName);
    }
}
