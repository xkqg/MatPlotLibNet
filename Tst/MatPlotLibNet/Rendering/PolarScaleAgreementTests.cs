// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The grid of a polar axes and every series on it read ONE radial scale: a ring labelled 10 stands exactly where
/// a radius of 10 is drawn, whichever kind of series draws it, and two series on one axes share their scale. For a plot of
/// 400 by 400 the centre is (200, 200) and the outermost ring is 200 px out, on the edge of the square.</summary>
public class PolarScaleAgreementTests
{
    private static readonly Rect Plot = new(0, 0, 400, 400);
    private const double MaxRadius = 200;
    private static readonly Point Centre = new(200, 200);

    private static double Distance(Point p) => Math.Sqrt((p.X - Centre.X) * (p.X - Centre.X) + (p.Y - Centre.Y) * (p.Y - Centre.Y));

    /// <summary>The rings: the label of each, with the radius in pixels of the circle drawn for it.</summary>
    private static Dictionary<string, double> Rings(RecordingRenderContext ctx)
    {
        // A ring is a circle with an edge in the grid's colour and no fill; the spine that closes the circle (PolarSpineTests) is the
        // circle in the spine's colour.
        var spine = new Axes().Spines.Top.ColorIn(Theme.Default);
        var circles = ctx.Of<DrawCircleCall>()
            .Where(a => a.Stroke is not null && a.Fill is null && a.Stroke != spine)
            .Select(a => a.Radius).ToList();
        var labels = ctx.Of<DrawTextCall>().Select(c => c.Text).Where(t => !t.EndsWith('°')).ToList();
        // A ring's label is drawn with its ring: the grid draws a circle and then the label of that circle.
        return labels.Zip(circles.Skip(circles.Count - labels.Count), (label, radius) => (label, radius)).ToDictionary(t => t.label, t => t.radius);
    }

    /// <summary>The circles the series drew: every circle but the axes' background, which is the circle the axes stands in
    /// (<see cref="PolarBackgroundTests"/>), filled with the theme's axes colour and no edge, and the spine that closes it
    /// (<see cref="PolarSpineTests"/>), an edge with no fill.</summary>
    private static IEnumerable<DrawCircleCall> MarkCircles(RecordingRenderContext ctx) => ctx.Of<DrawCircleCall>()
        .Where(c => !(c.Fill == Theme.Default.AxesBackground && c.Stroke is null))
        .Where(c => c.Fill is not null);

    private static Axes PolarAxes() => new() { CoordinateSystem = CoordinateSystem.Polar };

    // ---- a ring stands where the radius it names is drawn ---------------------------------------------------------------

    public static TheoryData<string> Kinds => ["scatter", "line", "bar", "heatmap"];

    private static IEnumerable<Point> FarthestMarks(RecordingRenderContext ctx, string kind) => kind switch
    {
        "scatter" => MarkCircles(ctx).Select(c => c.Center),
        "line" => ctx.Of<DrawLinesCall>().SelectMany(c => c.Points),
        _ => ctx.Of<DrawPolygonCall>().SelectMany(c => c.Points),
    };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ARing_StandsExactlyWhereTheRadiusItNamesIsDrawn(string kind)
    {
        var axes = PolarAxes();
        double[] r = [2, 10, 6], theta = [0, 1, 2];
        switch (kind)
        {
            case "scatter": axes.PolarScatter(r, theta); break;
            case "line": axes.PolarPlot(r, theta); break;
            case "bar": axes.PolarBar(r, theta); break;
            default: axes.PolarHeatmap(new double[4, 3], 4, 3).RMax = 10; break;
        }

        var ctx = axes.Drawn(Plot);

        Assert.Equal(Rings(ctx)["10"], FarthestMarks(ctx, kind).Max(Distance), 6);
    }

    [Fact]
    public void TwoSeriesOnOneAxes_ShareTheirScale()
    {
        var axes = PolarAxes();
        axes.PolarScatter([10.0, 3], [0.0, 1]).MarkerSize = 10;
        axes.PolarScatter([2.0], [2.0]).MarkerSize = 20;

        var ctx = axes.Drawn(Plot);

        // A marker is its size in points across (PolarScatterMarkerSizeTests): the 20 one's radius is 10 points, the 10 one's 5.
        double Farthest(double radius) => MarkCircles(ctx).Where(c => c.Radius == radius)
            .Max(c => Distance(c.Center));
        Assert.Equal(0.2, Farthest(20.0.PointsInPixels / 2) / Farthest(10.0.PointsInPixels / 2), 9);
    }

    [Fact]
    public void ARadiusUnderOne_IsNotHeldUpToAFloorOfOne()
    {
        var axes = PolarAxes();
        axes.PolarScatter([0.4], [0.0]);

        var ctx = axes.Drawn(Plot);

        // The extent is 0.4 · 1.05 = 0.42: the mark stands 0.4 / 0.42 of the way out, not 0.4 / 1.1.
        Assert.Equal(MaxRadius * 0.4 / 0.42, MarkCircles(ctx).Select(c => Distance(c.Center)).Single(), 6);
    }

    /// <summary>A radius below zero is drawn, on a scale that reaches it (matplotlib: r -20 and 4 give rlim (-21.2, 5.2)): -20 stands
    /// 1.2 / 26.4 of the way out and 4 stands 25.2 / 26.4, and no mark lies outside the outermost ring.</summary>
    [Fact]
    public void ARadiusBelowZero_IsDrawnOnAScaleThatReachesIt_AndNoMarkLiesOutsideTheOutermostRing()
    {
        var axes = PolarAxes();
        axes.PolarScatter([-20.0, 4], [0.0, 1]);

        var ctx = axes.Drawn(Plot);

        var marks = MarkCircles(ctx).Select(c => Distance(c.Center)).ToList();
        Assert.Equal(2, marks.Count);
        Assert.Equal(MaxRadius * 1.2 / 26.4, marks[0], 6);
        Assert.Equal(MaxRadius * 25.2 / 26.4, marks[1], 6);
        Assert.All(marks, distance => Assert.True(distance <= MaxRadius + 1e-6));
    }

    [Fact]
    public void HidingASeries_DoesNotMoveTheRings()
    {
        Axes With(bool hidden)
        {
            var axes = PolarAxes();
            axes.PolarScatter([10.0], [0.0]);
            axes.PolarScatter([20.0], [1.0]).Visible = !hidden;
            return axes;
        }

        Assert.Equal(Rings(With(hidden: true).Drawn(Plot)), Rings(With(hidden: false).Drawn(Plot)));
    }

    [Fact]
    public void APolarHeatmapOfItsOwnRadius_AndAScatterBeyondIt_ShareTheScale()
    {
        var axes = PolarAxes();
        axes.PolarHeatmap(new double[4, 3], 4, 3).RMax = 5;
        axes.PolarScatter([8.0], [0.0]);

        var ctx = axes.Drawn(Plot);

        // The scatter reaches 8 · 1.05 = 8.4; the heatmap's edge stands 5 / 8.4 of the way out.
        double heatmapEdge = ctx.Of<DrawPolygonCall>().SelectMany(c => c.Points).Max(Distance);
        Assert.Equal(MaxRadius * 5 / 8.4, heatmapEdge, 6);
    }

    // ---- stacked bars ---------------------------------------------------------------------------------------------------

    /// <summary>The inner and the outer radius of each wedge drawn, in pixels, in the order they were drawn.</summary>
    private static List<(double Inner, double Outer)> Wedges(RecordingRenderContext ctx) =>
        [.. ctx.Of<DrawPolygonCall>().Select(c => c.Points.Select(Distance).ToArray())
            .Select(d => (d.Min(), d.Max()))];

    private static Axes TwoBars(BarMode mode)
    {
        var axes = PolarAxes();
        axes.BarMode = mode;
        axes.PolarBar([3.0], [0.0]);
        axes.PolarBar([2.0], [0.0]);
        return axes;
    }

    [Fact]
    public void StackedPolarBars_StandOnTheBarsBeforeThem()
    {
        var ctx = TwoBars(BarMode.Stacked).Drawn(Plot);

        var wedges = Wedges(ctx);
        // The stack is 3 + 2 = 5 high and the scale reaches 5 · 1.05 = 5.25: the first wedge runs from the centre to 3, the
        // second from 3 to 5, so the second stands on a radius of 0.6 of its outer one.
        Assert.Equal(0, wedges[0].Inner, 6);
        Assert.Equal(MaxRadius * 3 / 5.25, wedges[0].Outer, 6);
        Assert.Equal(MaxRadius * 3 / 5.25, wedges[1].Inner, 6);
        Assert.Equal(MaxRadius * 5 / 5.25, wedges[1].Outer, 6);
        Assert.Equal(0.6, wedges[1].Inner / wedges[1].Outer, 6);
    }

    [Fact]
    public void TheRingsOfAStack_ReachItsTop()
    {
        var rings = Rings(TwoBars(BarMode.Stacked).Drawn(Plot));

        Assert.True(rings.ContainsKey("5"), "a ring is labelled with the top of the stack");
        Assert.Equal(MaxRadius * 5 / 5.25, rings["5"], 6);
    }

    [Fact]
    public void GroupedPolarBars_StillOverlayEachOther()
    {
        var wedges = Wedges(TwoBars(BarMode.Grouped).Drawn(Plot));

        Assert.Equal(0, wedges[1].Inner, 6);
        Assert.Equal(MaxRadius * 2 / 3.15, wedges[1].Outer, 6);
    }

    [Fact]
    public void AStackedRender_ChangesNothingTheJsonCarries()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.PolarBar([3.0], [0.0]).PolarBar([2.0], [0.0]).SetBarMode(BarMode.Stacked)).Build();
        string before = figure.ToJson();

        figure.ToSvg();

        Assert.Equal(before, figure.ToJson());
    }

    // ---- the rings and their labels --------------------------------------------------------------------------------------

    [Fact]
    public void TheRingsOfAWindRose_AreRoundNumbers_Labelled_AtTwentyTwoAndAHalfDegrees()
    {
        var axes = PolarAxes();
        axes.PolarBar([14.1, 8.0, 5.0], [0.0, 0.8, 1.6]);

        var ctx = axes.Drawn(Plot);

        var labels = ctx.Of<DrawTextCall>().Select(c => (Text: c.Text, At: c.Position))
            .Where(t => !t.Text.EndsWith('°')).ToList();
        // The rings ask for as many ticks as fit along 170 px (6), which the locator gives as steps of 2.5.
        Assert.Equal(["2.5", "5.0", "7.5", "10.0", "12.5"], labels.Select(l => l.Text).ToArray());
        foreach (var (text, at) in labels)
        {
            double degrees = Math.Atan2(Centre.Y - at.Y, at.X - Centre.X) * 180 / Math.PI;
            Assert.InRange(degrees, 17, 28);
        }
    }

    [Fact]
    public void TheRingLabels_AreWrittenByTheAxesFormatterWhenItHasOne()
    {
        var axes = PolarAxes();
        axes.PolarScatter([10.0], [0.0]);
        axes.YAxis.TickFormatter = new TestTickFormatter(value => "#" + value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));

        var ctx = axes.Drawn(Plot);

        Assert.All(ctx.Of<DrawTextCall>().Select(c => c.Text).Where(t => !t.EndsWith('°')),
            text => Assert.StartsWith("#", text));
    }

    [Fact]
    public void TheCircle_IsClosedAtTheExtent_WhenNoRingStandsThere()
    {
        var axes = PolarAxes();
        axes.PolarScatter([10.0], [0.0]);

        var ctx = axes.Drawn(Plot);

        // The spine closes it (PolarSpineTests): a circle with an edge and no fill.
        Assert.Contains(ctx.Of<DrawCircleCall>(), c => c.Fill is null && Math.Abs(c.Radius - MaxRadius) < 1e-9);
    }

    // ---- the marks a legend keeps clear of --------------------------------------------------------------------------------

    [Fact]
    public void TheMarksALegendKeepsClearOf_AreTheMarksAsDrawn()
    {
        var axes = PolarAxes();
        axes.PolarScatter([10.0, 4, 7], [0.0, 1, 2]);
        var drawn = MarkCircles(axes.Drawn(Plot)).Select(c => c.Center).ToList();

        var marks = new PolarAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default).LegendObstacles();

        Assert.Equal(drawn.Count, marks.Points.Count);
        foreach (var point in drawn)
            Assert.Contains(marks.Points, p => Math.Abs(p.X - point.X) < 1e-9 && Math.Abs(p.Y - point.Y) < 1e-9);
    }
}
