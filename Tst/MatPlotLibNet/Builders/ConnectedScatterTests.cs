// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>ConnectedScatter</c> verb: a marker on every point and an arrow from each point to the next, as one scatter
/// and one quiver in one colour. The head of an arrow lands on the next point and points along the segment as it is drawn on
/// the page, not along the segment of the data, so on a plot wider than it is tall the two angles differ and only the first
/// is the one a reader sees.</summary>
public class ConnectedScatterTests
{
    private static readonly Color SeriesColour = Theme.Default.CycleColors[0];

    private static Figure Of(double[] x, double[] y, Action<ConnectedScatterParts>? configure = null, int width = 800, int height = 300) =>
        Plt.Create().WithSize(width, height).AddSubPlot(1, 1, 1, ax => ax.ConnectedScatter(x, y, configure)).Build();

    private static RecordingRenderContext Drawn(Figure figure)
    {
        var recording = new RecordingRenderContext();
        new ChartRenderer().Render(figure, recording);
        return recording;
    }

    /// <summary>The centres of the markers in the order drawn, which is the order of the points.</summary>
    private static List<Point> Markers(RecordingRenderContext drawn) => [.. drawn.OfKind("DrawCircle")
        .Select(call => (dynamic)call.Args!)
        .Where(args => ((Color?)args.fill)?.R == SeriesColour.R && ((Color?)args.fill)?.G == SeriesColour.G && ((Color?)args.fill)?.B == SeriesColour.B)
        .Select(args => (Point)args.center)];

    /// <summary>The lines of the arrows (shaft and head arms), in the order drawn.</summary>
    private static List<(Point From, Point To)> ArrowLines(RecordingRenderContext drawn) => [.. drawn.OfKind("DrawLine")
        .Select(call => (dynamic)call.Args!)
        .Where(args => (Color)args.color == SeriesColour && (double)args.thickness == 1.5)
        .Select(args => ((Point)args.p1, (Point)args.p2))];

    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.ConnectedScatterByHand(false).ToSvg(), ComposedChartShapes.Named("connected-scatter").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.ConnectedScatterByHand(true).ToSvg(), ComposedChartShapes.Named("connected-scatter").Build(true).ToSvg());
    }

    [Fact]
    public void TheTipOfAnArrow_IsThePixelOfTheNextPoint_AndTheHeadFollowsTheSegmentAsItIsDrawn()
    {
        // A wide, low plot: a step of one along X is far shorter on the page than a step of two along Y is tall, so the angle of
        // the segment on the page is not the angle of the segment in the data.
        double[] x = [0, 1, 3], y = [0, 2, 1];
        var drawn = Drawn(Of(x, y));

        var markers = Markers(drawn);
        var lines = ArrowLines(drawn);
        Assert.Equal(3, markers.Count);
        Assert.Equal(6, lines.Count);   // two arrows, each a shaft and two arms of a head

        for (int arrow = 0; arrow < 2; arrow++)
        {
            var (shaft, armA, armB) = (lines[3 * arrow], lines[3 * arrow + 1], lines[3 * arrow + 2]);
            var tail = markers[arrow];
            var tip = markers[arrow + 1];
            Assert.Equal(tail.X, shaft.From.X, 6);
            Assert.Equal(tail.Y, shaft.From.Y, 6);
            Assert.Equal(tip.X, shaft.To.X, 6);   // the head lands on the next point
            Assert.Equal(tip.Y, shaft.To.Y, 6);

            double dx = tip.X - tail.X, dy = tip.Y - tail.Y, length = Math.Sqrt(dx * dx + dy * dy);
            double pageAngle = Math.Atan2(dy, dx), headLength = 0.1 * length;
            Assert.Equal(tip.X + headLength * Math.Cos(pageAngle + 2.5), armA.To.X, 6);
            Assert.Equal(tip.Y + headLength * Math.Sin(pageAngle + 2.5), armA.To.Y, 6);
            Assert.Equal(tip.X + headLength * Math.Cos(pageAngle - 2.5), armB.To.X, 6);
            Assert.Equal(tip.Y + headLength * Math.Sin(pageAngle - 2.5), armB.To.Y, 6);
            Assert.Equal(tip, armA.From);
            Assert.Equal(tip, armB.From);
        }

        // The page angle of the second arrow is not the data angle (atan2 of -1 over 2, with Y up on the page).
        double dataAngle = Math.Atan2(-(y[2] - y[1]), x[2] - x[1]);
        double pageSecond = Math.Atan2(markers[2].Y - markers[1].Y, markers[2].X - markers[1].X);
        Assert.True(Math.Abs(dataAngle - pageSecond) > 0.05, "the figure is chosen so the two angles differ");
    }

    [Fact]
    public void TwoPointsOnTopOfEachOther_LeaveAnArrowWithNoHead()
    {
        var lines = ArrowLines(Drawn(Of([0, 0, 1], [0, 0, 1])));

        // The first step is nothing: its shaft is a point and it has no head. The second step has a shaft and two arms.
        Assert.Equal(1 + 3, lines.Count);
    }

    [Fact]
    public void AMissingPoint_LeavesOutItsMarker_AndTheArrowsThatRunIntoItAndOutOfIt()
    {
        var drawn = Drawn(Of([0, 1, 2, 3, 4], [0, 1, double.NaN, 3, 4]));

        Assert.Equal(4, Markers(drawn).Count);
        // Arrows 0 -> 1 and 3 -> 4 are drawn; 1 -> 2 and 2 -> 3 touch the missing point.
        Assert.Equal(2 * 3, ArrowLines(drawn).Count);
    }

    [Fact]
    public void TheParts_AreTheScatterAndTheQuiverOnTheAxes_InOneColourOfTheCycle()
    {
        ConnectedScatterParts parts = default;
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .ConnectedScatter([1, 2, 4, 5], [3, 5, 4, 7], p => parts = p)
            .Plot([0.0, 1.0], [0.0, 1.0])).Build().SubPlots[0];

        Assert.Equal(new ISeries[] { parts.Arrows, parts.Points }, axes.Series.Take(2));
        Assert.Equal([1.0, 2.0, 1.0], parts.Arrows.UData);
        Assert.Equal([2.0, -1.0, 3.0], parts.Arrows.VData);
        Assert.Equal([1.0, 2.0, 4.0], parts.Arrows.XData);
        Assert.Equal(1.0, parts.Arrows.Scale);
        Assert.Equal(4, parts.Points.XData.Length);
        // One drawn unit is one colour: the two share a slot, so the next series takes the next colour of the cycle.
        var colours = Drawn(Plt.Create().WithSize(800, 300).AddSubPlot(1, 1, 1, ax => ax
            .ConnectedScatter([1, 2, 4, 5], [3, 5, 4, 7]).Plot([0.0, 1.0], [0.0, 1.0])).Build())
            .OfKind("DrawLines").Select(call => (Color)((dynamic)call.Args!).color).ToList();
        Assert.Contains(Theme.Default.CycleColors[1], colours);
    }

    [Fact]
    public void WhatTheCallbackSets_WinsOverWhatTheVerbDid()
    {
        var drawn = Drawn(Of([0, 1, 2], [0, 1, 0], p => { p.Arrows.Color = Colors.Red; p.Points.MarkerSize = 12; }));

        Assert.Contains(drawn.OfKind("DrawLine"), call => (Color)((dynamic)call.Args!).color == Colors.Red);
    }

    [Fact]
    public void TheTables_AreThePointsAndTheStepsBetweenThem()
    {
        ConnectedScatterParts parts = default;
        Of([1, 2, 4], [3, 5, 4], p => parts = p);

        var points = parts.Points.ToDataTable()!;
        var steps = parts.Arrows.ToDataTable()!;

        Assert.Equal(3, points.Rows.Count);
        Assert.Equal(["x", "y", "u", "v"], steps.Columns.Select(c => c.Header));
        Assert.Equal([1.0, 2.0], steps.Rows.Select(row => row[2].Number));
        Assert.Equal([2.0, -1.0], steps.Rows.Select(row => row[3].Number));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void FewerThanTwoPoints_DrawNoArrow_AndDoNotBreakTheFigure(int count)
    {
        double[] x = [.. Enumerable.Range(0, count).Select(i => (double)i)];
        ConnectedScatterParts parts = default;
        var figure = Of(x, x, p => parts = p);

        Assert.Empty(parts.Arrows.UData);
        Assert.Equal(count, parts.Points.XData.Length);
        Assert.NotEmpty(figure.ToSvg());
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of([1, 2, 4, 5, 7], [3, 5, 4, 7, 6]);

        var back = ChartSerializerDefault.Current.FromJson(figure.ToJson());

        Assert.Equal(figure.ToSvg(), back.ToSvg());
        Assert.Equal(2, back.SubPlots[0].Series.Count);
    }

    [Fact]
    public void PointsOfAnotherLengthThanTheirPositions_AreRefusedByName()
    {
        Assert.Equal("y", Assert.Throws<ArgumentException>(() => Of([1, 2, 3], [1, 2])).ParamName);
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ConnectedScatter(null!, [1.0])).Build()).ParamName);
        Assert.Equal("y", Assert.Throws<ArgumentNullException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ConnectedScatter([1.0], null!)).Build()).ParamName);
    }
}
