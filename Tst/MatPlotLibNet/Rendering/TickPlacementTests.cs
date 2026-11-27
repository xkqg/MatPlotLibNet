// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Where the one tick drawing (<see cref="AxisTickExtensions"/>) stands a tick: through an <see cref="ITickPlacement"/>,
/// so the value a tick is labelled with and the place it stands can differ. A plain <see cref="DataTransform"/> stands a tick at
/// its own value, as every axis has always done; a <see cref="LinkedTickPlacement"/> stands the tick of value v of a linked axis
/// where the primary axis shows <see cref="AxisTransform.Inverse"/>(v), matplotlib's rule for <c>secondary_xaxis</c> and
/// <c>secondary_yaxis</c>. The label always reads the value.</summary>
public class TickPlacementTests
{
    private static readonly Rect Plot = new(0, 0, 400, 300);

    // x 1..5 onto 0..400 (100 px per unit, x = 1 at the left edge) and y 0..1 onto 300..0.
    // x and y 0..100: x onto 0..400 (4 px per unit), y onto 300..0 (3 px per unit).
    private static readonly AxisTransform Period = AxisTransform.Reciprocal();
    private static readonly AxisTransform Fahrenheit = AxisTransform.Linear(1.8, 32);

    private static readonly TickConfig Major = new() { Length = 5, Pad = 2, Width = 1 };

    private static TickDrawContext Draw(TickSide side, double edge) => new()
    {
        Side = side,
        AxisEdge = edge,
        BaseFont = new Font { Size = 10 },
        UniformFormat = v => v.ToString("G", CultureInfo.InvariantCulture),
        Foreground = Colors.Black,
        Alignment = TextAlignment.Center,
        Theme = Theme.Default,
    };

    /// <summary>Where each mark stands along its edge: the X of a mark on a top or bottom edge, the Y of one on a left or
    /// right edge.</summary>
    private static double[] MarkPositions(RecordingRenderContext ctx, bool alongX) =>
        ctx.Of<DrawLineCall>().Select(c =>
        {
            var p1 = c.P1;
            return alongX ? p1.X : p1.Y;
        }).ToArray();

    private static string[] Labels(RecordingRenderContext ctx) =>
        ctx.Of<DrawTextCall>().Select(c => c.Text).ToArray();

    [Fact]
    public void APlainPlacement_PutsATickAtItsValue()
    {
        var primary = Plot.Linear(xMax: 100, yMax: 100);
        ITickPlacement placement = primary;
        var ctx = new RecordingRenderContext();

        ctx.RenderAxisTicks(new TickRun([0.0, 25.0, 50.0, 100.0], placement, EdgeTerms.Bottom), Major, Draw(TickSide.Bottom, 300));

        Assert.NotNull(placement.OnPlot(100, alongX: true));
        Assert.Null(placement.OnPlot(101, alongX: true));
        (new double[] { 0, 100, 200, 400 }).AssertCloseTo(MarkPositions(ctx, alongX: true), 1e-9);
        Assert.Equal(["0", "25", "50", "100"], Labels(ctx));
    }

    [Fact]
    public void ALinkedPlacementAlongX_PutsTheTickOfAValueWhereThePrimaryShowsItsInverse_AndLabelsItWithTheValue()
    {
        // A period axis along the top of a plot over frequency 1..5: the tick 0.25 stands where the frequency is 4.
        var placement = LinkedTickPlacement.Along(true, Plot.Linear(xMin: 1, xMax: 5, yMax: 1), Period);
        var ctx = new RecordingRenderContext();

        ctx.RenderAxisTicks(new TickRun([1.0, 0.5, 0.25, 0.2], placement, EdgeTerms.Top), Major, Draw(TickSide.Top, 0));

        (new double[] { 0, 100, 300, 400 }).AssertCloseTo(MarkPositions(ctx, alongX: true), 1e-9);
        Assert.Equal(["1", "0.5", "0.25", "0.2"], Labels(ctx));
    }

    [Fact]
    public void ALinkedPlacementAlongY_PutsTheTickOfAValueWhereThePrimaryShowsItsInverse_AndLabelsItWithTheValue()
    {
        // Fahrenheit beside a plot in Celsius 0..100: 122 °F stands where the plot shows 50 °C.
        var placement = LinkedTickPlacement.Along(false, Plot.Linear(xMax: 100, yMax: 100), Fahrenheit);
        var ctx = new RecordingRenderContext();

        ctx.RenderAxisTicks(new TickRun([32.0, 122.0, 212.0], placement, EdgeTerms.Right), Major, Draw(TickSide.Right, 400));

        (new double[] { 300, 150, 0 }).AssertCloseTo(MarkPositions(ctx, alongX: false), 1e-9);
        Assert.Equal(["32", "122", "212"], Labels(ctx));
    }

    [Fact]
    public void ALinkedPlacement_LeavesTheCoordinateAcrossItsEdgeUnlinked_AndClipsToThePrimarysPlot()
    {
        var primary = Plot.Linear(xMax: 100, yMax: 100);
        var alongX = LinkedTickPlacement.Along(true, primary, Fahrenheit);
        var alongY = LinkedTickPlacement.Along(false, primary, Fahrenheit);

        var x = alongX.DataToPixel(122, 37);
        var y = alongY.DataToPixel(37, 122);

        Assert.Equal(primary.DataToPixel(50, 37).X, x.X, 9);
        Assert.Equal(primary.DataToPixel(50, 37).Y, x.Y);
        Assert.Equal(primary.DataToPixel(37, 50).X, y.X);
        Assert.Equal(primary.DataToPixel(37, 50).Y, y.Y, 9);
        // The primary shows 0..100 °C, so the linked axes show 32..212 °F, ends included.
        Assert.NotNull(alongX.OnPlot(32, alongX: true));
        Assert.NotNull(alongX.OnPlot(212, alongX: true));
        Assert.Null(alongX.OnPlot(213, alongX: true));
        Assert.NotNull(alongY.OnPlot(212, alongX: false));
        Assert.Null(alongY.OnPlot(31, alongX: false));
    }

    /// <summary>A link that cannot place the primary's range (a reciprocal across 0) shows nothing, so no tick of it is
    /// drawn.</summary>
    [Fact]
    public void ALinkThatCannotPlaceThePrimarysRange_ShowsNoTick()
    {
        var placement = LinkedTickPlacement.Along(true, new DataTransform(-1, 1, 0, 1, Plot), Period);

        Assert.Null(placement.OnPlot(2, alongX: true));
        Assert.Null(new TickRun([2.0], placement, EdgeTerms.Bottom).PlaceOnPlot(2));
        Assert.Null(LinkedTickPlacement.Along(false, new DataTransform(0, 1, -1, 1, Plot), Period).OnPlot(2, alongX: false));
    }

    /// <summary>A value the linked axis shows but the primary has no place for (inside a break of the primary) stands nowhere;
    /// the value beside the break stands where the primary draws it.</summary>
    [Fact]
    public void ALinkedTickInsideABreakOfThePrimary_StandsNowhere()
    {
        var primary = new DataTransform(0, 8, 0, 1, Plot, [new AxisBreak(2, 4)], null, 0, 10, 0, 1);
        var placement = LinkedTickPlacement.Along(true, primary, AxisTransform.Linear(1));

        Assert.Null(placement.OnPlot(3, alongX: true));
        Assert.Equal(primary.DataToPixel(5, 0).X, placement.OnPlot(5, alongX: true));
    }

    /// <summary>A linked date axis (seconds since the start of a one-minute window, over a primary in dates): its end ticks
    /// stand on the plot at every plot size, as a plain axis' do; whether the plot shows a tick is judged where the primary
    /// draws its inverse, before the pixel.</summary>
    [Fact]
    public void ALinkedPlacement_KeepsTheTicksAtTheEndsOfADateRange_AtEverySize()
    {
        var start = new DateTime(2026, 9, 12, 14, 29, 0, DateTimeKind.Utc);
        double lo = start.ToOADate(), hi = start.AddMinutes(1).ToOADate();
        var seconds = AxisTransform.Linear(86400, -lo * 86400);
        for (double width = 300; width < 400; width += 0.01)
        {
            var placement = LinkedTickPlacement.Along(true, new DataTransform(lo, hi, 0, 1, new Rect(60, 40, width, 400)), seconds);
            var run = new TickRun([0.0, 60.0], placement, EdgeTerms.Bottom);
            Assert.NotNull(placement.OnPlot(seconds.Forward(lo), alongX: true));
            Assert.NotNull(run.PlaceOnPlot(seconds.Forward(hi)));
        }
    }

    /// <summary>The scale a tick is drawn on and the view it is placed over, which say whose minor locator places the minor ticks
    /// and over what: a transform's own scale and the range it was handed; a linked axis is drawn on a log scale over a log primary
    /// and on a linear one over any other (matplotlib's <c>functionlog</c> and <c>function</c> scales), over the image of the
    /// primary's range through the link, low to high, and over none when the link cannot place it.</summary>
    [Fact]
    public void APlacement_SaysTheScaleItsTicksAreDrawnOn_AndTheViewInTheirUnit()
    {
        var log = Plot.LogX(yMax: 1, xMax: 1000);
        ITickPlacement plain = log;

        Assert.Same(ScaleRule.Log, plain.ScaleAlong(alongX: true));
        Assert.Same(ScaleRule.Linear, plain.ScaleAlong(alongX: false));
        Assert.Equal(new Range1D(1, 1000), plain.ViewAlong(alongX: true));
        Assert.Same(ScaleRule.Log, LinkedTickPlacement.Along(true, log, Period).ScaleAlong(alongX: true));
        Assert.Equal(new Range1D(0.001, 1), LinkedTickPlacement.Along(true, log, Period).ViewAlong(alongX: true));
        Assert.Same(ScaleRule.Linear, LinkedTickPlacement.Along(false, log, Fahrenheit).ScaleAlong(alongX: false));
        Assert.Same(ScaleRule.Linear, LinkedTickPlacement.Along(true, Plot.Linear(xMax: 100, yMax: 100), Fahrenheit).ScaleAlong(alongX: true));
        Assert.Null(LinkedTickPlacement.Along(true, new DataTransform(-1, 1, 0, 1, Plot), Period).ViewAlong(alongX: true));
    }

    [Fact]
    public void ALinkedPlacement_SplitsTheMinorTicksInTheLinkedUnit_AndStandsEachWhereThePrimaryShowsItsInverse()
    {
        // Majors 0.5 and 1.0 of a period, a minor every 0.25 of a period: 0.25 and 0.75 lie between or beside them. 0.25 is the
        // frequency 4 (x 300) and 0.75 the frequency 1.333.. (x 33.3..); 0 (an infinite frequency), 1.25 and 1.5 (frequencies
        // below 1) fall outside the plot and are not drawn. Split in frequency instead, the minors would be evenly spaced in x.
        var placement = LinkedTickPlacement.Along(true, Plot.Linear(xMin: 1, xMax: 5, yMax: 1), Period);
        var ctx = new RecordingRenderContext();

        ctx.RenderMinorTicks(new TickRun([0.5, 1.0], placement, EdgeTerms.Top), new TickConfig { Length = 2, Spacing = 0.25 },
            Draw(TickSide.Top, 0));

        (new double[] { 300, 100.0 / 0.75 - 100 }).AssertCloseTo(MarkPositions(ctx, alongX: true), 1e-9);
    }

    [Fact]
    public void ALinkedPlacementAlongY_StandsItsMinorTicksWhereThePrimaryShowsTheirInverse()
    {
        // Majors 50 and 100 °F, five minors to a major step (every 10 °F) from 0 to 150 °F: those from 40 °F (4.4 °C) to 150 °F
        // (65.6 °C) lie inside the plot, 3 px per °C from the bottom at 300.
        var placement = LinkedTickPlacement.Along(false, Plot.Linear(xMax: 100, yMax: 100), Fahrenheit);
        var ctx = new RecordingRenderContext();

        ctx.RenderMinorTicks(new TickRun([50.0, 100.0], placement, EdgeTerms.Right), new TickConfig { Length = 2 }, Draw(TickSide.Right, 400));

        double[] minors = MarkPositions(ctx, alongX: false);
        Assert.Equal(10, minors.Length);
        double[] expected = [286.667, 253.333, 236.667, 220, 203.333, 170, 153.333, 136.667, 120, 103.333];
        for (int i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], minors[i], 3);
    }

    /// <summary>The placement of a linked axis along a direction is the X one along X and the Y one otherwise, and stands the tick of
    /// a value where the primary shows its inverse along that direction, whether or not the plot shows it.</summary>
    [Fact]
    public void ALinkedPlacementAlongADirection_IsTheOneOfThatDirection_AndPlacesAValueWithoutAskingWhetherThePlotShowsIt()
    {
        var primary = Plot.Linear(xMax: 100, yMax: 100);

        var alongX = LinkedTickPlacement.Along(true, primary, Fahrenheit);
        var alongY = LinkedTickPlacement.Along(false, primary, Fahrenheit);

        Assert.Equal(primary.DataToPixel(Fahrenheit.Inverse(122), 37), alongX.DataToPixel(122, 37));
        Assert.Equal(primary.DataToPixel(37, Fahrenheit.Inverse(122)), alongY.DataToPixel(37, 122));
        Assert.Equal(alongX.DataToPixel(122, 0).X, alongX.PixelAlong(122, true));
        Assert.Equal(alongY.DataToPixel(0, 122).Y, alongY.PixelAlong(122, false));
        // 400 °F is 204.4 °C, past the plot's 100: placed all the same, off the plot.
        Assert.Equal((400 - 32) / 1.8 * 4, alongX.PixelAlong(400, true), 9);
    }
}
