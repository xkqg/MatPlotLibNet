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
    private static DataTransform FrequencyPrimary() => new(1, 5, 0, 1, Plot);

    // x and y 0..100: x onto 0..400 (4 px per unit), y onto 300..0 (3 px per unit).
    private static DataTransform CelsiusPrimary() => new(0, 100, 0, 100, Plot);

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
    };

    /// <summary>Where each mark stands along its edge: the X of a mark on a top or bottom edge, the Y of one on a left or
    /// right edge.</summary>
    private static double[] MarkPositions(RecordingRenderContext ctx, bool alongX) =>
        ctx.OfKind("DrawLine").Select(c =>
        {
            dynamic a = c.Args!;
            var p1 = (Point)a.p1;
            return alongX ? p1.X : p1.Y;
        }).ToArray();

    private static string[] Labels(RecordingRenderContext ctx) =>
        ctx.OfKind("DrawText").Select(c => (string)((dynamic)c.Args!).text).ToArray();

    private static void AssertNear(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], actual[i], 9);
    }

    [Fact]
    public void APlainPlacement_PutsATickAtItsValue()
    {
        var primary = CelsiusPrimary();
        ITickPlacement placement = primary;
        var ctx = new RecordingRenderContext();

        ctx.RenderAxisTicks([0.0, 25.0, 50.0, 100.0], 0.0, placement, Major, Draw(TickSide.Bottom, 300));

        Assert.Equal(primary.DataToPixel(25, 7), placement.DataToPixel(25, 7));
        Assert.Equal(Plot, placement.PlotBounds);
        AssertNear([0, 100, 200, 400], MarkPositions(ctx, alongX: true));
        Assert.Equal(["0", "25", "50", "100"], Labels(ctx));
    }

    [Fact]
    public void ALinkedPlacementAlongX_PutsTheTickOfAValueWhereThePrimaryShowsItsInverse_AndLabelsItWithTheValue()
    {
        // A period axis along the top of a plot over frequency 1..5: the tick 0.25 stands where the frequency is 4.
        var placement = LinkedTickPlacement.AlongX(FrequencyPrimary(), Period);
        var ctx = new RecordingRenderContext();

        ctx.RenderAxisTicks([1.0, 0.5, 0.25, 0.2], 1.0, placement, Major, Draw(TickSide.Top, 0));

        AssertNear([0, 100, 300, 400], MarkPositions(ctx, alongX: true));
        Assert.Equal(["1", "0.5", "0.25", "0.2"], Labels(ctx));
    }

    [Fact]
    public void ALinkedPlacementAlongY_PutsTheTickOfAValueWhereThePrimaryShowsItsInverse_AndLabelsItWithTheValue()
    {
        // Fahrenheit beside a plot in Celsius 0..100: 122 °F stands where the plot shows 50 °C.
        var placement = LinkedTickPlacement.AlongY(CelsiusPrimary(), Fahrenheit);
        var ctx = new RecordingRenderContext();

        ctx.RenderAxisTicks([32.0, 122.0, 212.0], 0.0, placement, Major, Draw(TickSide.Right, 400));

        AssertNear([300, 150, 0], MarkPositions(ctx, alongX: false));
        Assert.Equal(["32", "122", "212"], Labels(ctx));
    }

    [Fact]
    public void ALinkedPlacement_LeavesTheCoordinateAcrossItsEdgeUnlinked_AndClipsToThePrimarysPlot()
    {
        var primary = CelsiusPrimary();
        var alongX = LinkedTickPlacement.AlongX(primary, Fahrenheit);
        var alongY = LinkedTickPlacement.AlongY(primary, Fahrenheit);

        var x = alongX.DataToPixel(122, 37);
        var y = alongY.DataToPixel(37, 122);

        Assert.Equal(primary.DataToPixel(50, 37).X, x.X, 9);
        Assert.Equal(primary.DataToPixel(50, 37).Y, x.Y);
        Assert.Equal(primary.DataToPixel(37, 50).X, y.X);
        Assert.Equal(primary.DataToPixel(37, 50).Y, y.Y, 9);
        Assert.Equal(Plot, alongX.PlotBounds);
        Assert.Equal(Plot, alongY.PlotBounds);
    }

    [Fact]
    public void ALinkedPlacement_SplitsTheMinorTicksInTheLinkedUnit_AndStandsEachWhereThePrimaryShowsItsInverse()
    {
        // Majors 0.5 and 1.0 of a period, a minor every 0.25 of a period: 0.25 and 0.75 lie between or beside them. 0.25 is the
        // frequency 4 (x 300) and 0.75 the frequency 1.333.. (x 33.3..); 0 (an infinite frequency), 1.25 and 1.5 (frequencies
        // below 1) fall outside the plot and are not drawn. Split in frequency instead, the minors would be evenly spaced in x.
        var placement = LinkedTickPlacement.AlongX(FrequencyPrimary(), Period);
        var ctx = new RecordingRenderContext();

        ctx.RenderMinorTicks(new TickRun([0.5, 1.0], 1.0, placement), new TickConfig { Length = 2, Spacing = 0.25 },
            Draw(TickSide.Top, 0));

        AssertNear([300, 100.0 / 0.75 - 100], MarkPositions(ctx, alongX: true));
    }

    [Fact]
    public void ALinkedPlacementAlongY_StandsItsMinorTicksWhereThePrimaryShowsTheirInverse()
    {
        // Majors 50 and 100 °F, five minors to a major step (every 10 °F) from 0 to 150 °F: those from 40 °F (4.4 °C) to 150 °F
        // (65.6 °C) lie inside the plot, 3 px per °C from the bottom at 300.
        var placement = LinkedTickPlacement.AlongY(CelsiusPrimary(), Fahrenheit);
        var ctx = new RecordingRenderContext();

        ctx.RenderMinorTicks(new TickRun([50.0, 100.0], 0.0, placement), new TickConfig { Length = 2 }, Draw(TickSide.Right, 400));

        double[] minors = MarkPositions(ctx, alongX: false);
        Assert.Equal(10, minors.Length);
        double[] expected = [286.667, 253.333, 236.667, 220, 203.333, 170, 153.333, 136.667, 120, 103.333];
        for (int i = 0; i < expected.Length; i++)
            Assert.Equal(expected[i], minors[i], 3);
    }
}
