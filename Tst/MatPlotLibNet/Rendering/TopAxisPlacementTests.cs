// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The top X axis is placed in ONE place (<see cref="CartesianAxesRenderer.PlaceTopAxis"/>), which its drawing, the
/// band the layout reserves for it and the marks a legend keeps clear of all read, and it honours what is set on it as every
/// other axis does: its own tick locator, a tick spacing, its own scale (a log top axis takes the log locator's decades written by
/// the log formatter, draws its series on its log scale and pads its range in log space) and its minor ticks. A top axis that sets
/// none of these draws the bytes it drew before: the eight intervals of the default locator whatever the plot's width, its series
/// mapped linearly over its range.</summary>
public class TopAxisPlacementTests
{
    private static readonly Rect Plot = new(200, 50, 400, 300);
    private static readonly double[] X = [0.0, 1, 2];

    /// <summary>A plot whose top axis carries one line through <paramref name="topX"/>, at heights 0, 25, 50, ... of the primary's
    /// 0..100.</summary>
    private static Axes WithTopLine(params double[] topX)
    {
        var axes = new Axes();
        axes.Plot(X, [0.0, 50, 100]);
        axes.PlotXSecondary(topX, [.. topX.Select((_, i) => 25.0 * i)]);
        return axes;
    }

    /// <summary>A top axis on a log scale showing exactly 1..1000, its line through the four decades: 400 px over three decades.</summary>
    private static Axes LogTop()
    {
        var axes = WithTopLine(1, 10, 100, 1000);
        var top = axes.SecondaryXAxis!;
        top.Scale = AxisScale.Log;
        top.Min = 1;
        top.Max = 1000;
        return axes;
    }

    /// <summary>Where the log top axis of <see cref="LogTop"/> stands decade <paramref name="i"/>.</summary>
    private static double DecadeX(int i) => Plot.X + i / 3.0 * Plot.Width;

    private static RecordingRenderContext Render(Axes axes)
    {
        var ctx = new RecordingRenderContext();
        new CartesianAxesRenderer(axes, Plot, ctx, Theme.Default).Render();
        return ctx;
    }

    /// <summary>One upright tick label of the top axis: its text and the X it is centred on.</summary>
    private readonly record struct Label(string Text, double X);

    /// <summary>The labels drawn above the top spine, upright and centred, in the order drawn.</summary>
    private static List<Label> TopLabels(RecordingRenderContext ctx) =>
        [.. ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!)
            .Where(a => ((Point)a.position).Y < Plot.Y && (TextAlignment)a.alignment == TextAlignment.Center && (double)a.rotation == 0)
            .Select(a => new Label((string)a.text, ((Point)a.position).X))];

    private static string Text(double value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Where a top axis over 0..100, padded five per cent each side to -5..105, stands <paramref name="value"/>.</summary>
    private static double PaddedX(double value) => Plot.X + (value + 5) / 110 * Plot.Width;

    private static readonly double[] Quarters = [0, 25, 50, 75, 100];

    // ── what is set on it is honoured ───────────────────────────────────────────────────────────────────

    [Fact]
    public void ATopAxisOwnLocator_PlacesItsTicks()
    {
        var axes = WithTopLine(0, 50, 100);
        axes.SecondaryXAxis!.TickLocator = new MultipleLocator(25);

        var labels = TopLabels(Render(axes));

        Assert.Equal(Quarters.Select(Text), labels.Select(l => l.Text));
        for (int i = 0; i < Quarters.Length; i++)
            Assert.Equal(PaddedX(Quarters[i]), labels[i].X, 9);
    }

    [Fact]
    public void ATopAxisTickSpacing_PlacesItsTicks_AsOnEveryOtherAxis()
    {
        var axes = WithTopLine(0, 50, 100);
        var top = axes.SecondaryXAxis!;
        top.MajorTicks = top.MajorTicks with { Spacing = 25 };

        var labels = TopLabels(Render(axes));

        Assert.Equal(Quarters.Select(Text), labels.Select(l => l.Text));
    }

    [Fact]
    public void ATopAxisOnALogScale_TakesTheLogLocatorsDecades_WrittenByTheLogFormatter_WhereItsLogScaleStandsThem()
    {
        var labels = TopLabels(Render(LogTop()));

        double[] decades = [1, 10, 100, 1000];
        Assert.Equal(decades.Select(new LogTickFormatter().Format), labels.Select(l => l.Text));
        for (int i = 0; i < decades.Length; i++)
            Assert.Equal(DecadeX(i), labels[i].X, 9);
    }

    [Fact]
    public void ASeriesOnALogTopAxis_IsDrawnOnItsLogScale()
    {
        var ctx = Render(LogTop());

        var line = Assert.Single(ctx.OfKind("DrawLines"), c => ((Point[])((dynamic)c.Args!).points).Length == 4);
        var points = (Point[])((dynamic)line.Args!).points;
        for (int i = 0; i < points.Length; i++)
            Assert.Equal(DecadeX(i), points[i].X, 9);
    }

    [Fact]
    public void ALogTopAxisWithoutLimits_PadsItsRangeInLogSpace_AsALogYAxisDoes()
    {
        var axes = WithTopLine(1, 10, 100, 1000);
        axes.SecondaryXAxis!.Scale = AxisScale.Log;
        var renderer = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default);

        var range = renderer.ComputeSecondaryXDataRanges(0, 100);

        // Five per cent of the three decades each side (Range1D.Padded): 10^-0.15 .. 10^3.15, never a linear margin below 0.
        double low = Math.Pow(10, 0 - 3 * 0.05), high = Math.Pow(10, 3 + 3 * 0.05);
        Assert.True(Math.Abs(range.XMin - low) <= 1e-9 * low, $"{range.XMin} != {low}");
        Assert.True(Math.Abs(range.XMax - high) <= 1e-9 * high, $"{range.XMax} != {high}");
    }

    [Fact]
    public void ALogTopAxisLimitedFromZero_LiftsItsFloorThreeDecadesUnderItsCeiling_AsALogYAxisDoes()
    {
        var axes = WithTopLine(1, 10, 100, 1000);
        var top = axes.SecondaryXAxis!;
        top.Scale = AxisScale.Log;
        top.Min = 0;
        top.Max = 1000;
        var renderer = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default);

        var range = renderer.ComputeSecondaryXDataRanges(0, 100);

        // A log axis cannot place 0 (Range1D.PositiveForLog): the floor is lifted to 1000 / 1000, the set ceiling stays.
        Assert.Equal(1, range.XMin);
        Assert.Equal(1000, range.XMax);
    }

    [Fact]
    public void ATopAxisMinorTicks_AreDrawnBetweenItsTicks_WhenVisible()
    {
        var axes = WithTopLine(0, 50, 100);
        var top = axes.SecondaryXAxis!;
        top.TickLocator = new MultipleLocator(25);
        top.MinorTicks = top.MinorTicks with { Visible = true };

        var ctx = Render(axes);

        // A minor mark rises its own length from the top edge; between 0 and 25 they stand at 5, 10, 15 and 20, and none stands
        // on a major tick.
        var minors = ctx.OfKind("DrawLine").Select(c => (dynamic)c.Args!)
            .Where(a => Math.Abs(((Point)a.p1).Y - (Plot.Y - top.MinorTicks.Length)) < 1e-9)
            .Select(a => ((Point)a.p1).X).ToList();
        foreach (double value in new[] { 5.0, 10, 15, 20 })
            Assert.Contains(minors, x => Math.Abs(x - PaddedX(value)) < 1e-9);
        Assert.DoesNotContain(minors, x => Quarters.Any(q => Math.Abs(x - PaddedX(q)) < 1e-9));
    }

    // ── one placement, read by the drawing, the layout and the legend's marks ───────────────────────────

    [Fact]
    public void ATopAxisWithARangeOfItsOwn_IsPlacedOnThatRange_ThroughItsOwnScale_ItsTicksStoodByItsOwnTransform()
    {
        var renderer = new CartesianAxesRenderer(LogTop(), Plot, new RecordingRenderContext(), Theme.Default);
        var primary = renderer.ComputeDataRanges();

        var placed = renderer.PlaceTopAxis(primary);

        Assert.Equal(renderer.ComputeSecondaryXDataRanges(primary.YMin, primary.YMax), placed.Range);
        Assert.Equal(AxisScale.Log, placed.Transform.XScale);
        Assert.Same(placed.Transform, placed.Placement);
        Assert.Equal(new LogLocator().Locate(1, 1000), placed.Ticks);
        Assert.IsType<LogLocator>(placed.Source.Locator);
        Assert.IsType<LogTickFormatter>(placed.Source.Formatter);
        Assert.Null(placed.Diagnostic);
    }

    [Fact]
    public void TheBandTheLayoutReserves_ForALogTopAxisWithTurnedLabels_IsTheBandItIsDrawnIn()
    {
        // Turned, a label reaches as far as its text is long, so the band is the decades' own: the layout must place the top axis
        // as the drawing does. The axes title rises by the band drawn.
        static Axes Titled(Axes axes)
        {
            axes.Title = "A Title";
            return axes;
        }
        static double TitleY(RecordingRenderContext ctx) =>
            ((Point)ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!).Single(a => (string)a.text == "A Title").position).Y;
        var plain = Titled(new Axes());
        plain.Plot(X, [0.0, 50, 100]);
        var axes = Titled(LogTop());
        axes.SecondaryXAxis!.LabelRotation = 30;

        double measured = AxesRenderer.Create(axes, Plot, new RecordingRenderContext(), Theme.Default).MeasureEdgeBands().Top;
        var ctx = Render(axes);

        var turned = ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!)
            .Where(a => (double)a.rotation == 30 && ((Point)a.position).Y < Plot.Y).ToList();
        Assert.Equal(new double[] { 1, 10, 100, 1000 }.Select(new LogTickFormatter().Format), turned.Select(a => (string)a.text));
        Assert.True(measured > 0);
        Assert.Equal(TitleY(Render(plain)) - measured, TitleY(ctx), 9);
    }

    [Fact]
    public void TheLegendsMarks_OfASeriesOnALogTopAxis_StandWhereItIsDrawn()
    {
        var renderer = new CartesianAxesRenderer(LogTop(), Plot, new RecordingRenderContext(), Theme.Default);

        var marks = renderer.LegendObstacles();

        // The line through 1, 10, 100, 1000: on the log scale 10 stands a third of the way across, not 9/999 of it.
        Assert.Contains(marks.Paths, path => path.Count == 4 && Enumerable.Range(0, 4).All(i => Math.Abs(path[i].X - DecadeX(i)) < 1e-9));
    }

    // ── a top axis that sets nothing keeps its bytes ────────────────────────────────────────────────────

    /// <summary>A top axis over 0, 37 and 113 (drawn -5.65..118.65) that sets no locator, no spacing, no scale and no minor ticks;
    /// the default placement of every other axis lays that range out differently on a short plot.</summary>
    private static Axes PlainTop()
    {
        var axes = new Axes();
        axes.Plot([0.0, 2], [0.0, 100]);
        axes.PlotXSecondary([0.0, 37, 113], [10.0, 60, 90]);
        axes.SecondaryXAxis!.Label = "top";
        return axes;
    }

    private static string DrawnSvg(Axes axes, Rect plot)
    {
        var ctx = new SvgRenderContext();
        new CartesianAxesRenderer(axes, plot, ctx, Theme.Default).Render();
        return ctx.GetOutput();
    }

    [Theory]
    [InlineData(400.0)]
    [InlineData(150.0)]
    public void ATopAxisWithNeither_KeepsItsBytes(double width)
    {
        // What a top axis that sets none of its own has always been drawn by: the default locator's eight intervals over its own
        // range, whatever the plot's width, labelled in the uniform format, its series mapped linearly. Set as its own locator,
        // that rule draws the same bytes.
        var plot = new Rect(80, 60, width, 300);
        var pinned = PlainTop();
        pinned.SecondaryXAxis!.TickLocator = new AutoLocator(8);

        Assert.Equal(DrawnSvg(pinned, plot), DrawnSvg(PlainTop(), plot));
    }

    [Fact]
    public void ATopAxisWithNeither_OnAShortPlot_KeepsTheEightIntervals_WhereEveryOtherAxisWouldAskForFewer()
    {
        // The precondition the short case of ATopAxisWithNeither_KeepsItsBytes stands on: at 150 px the default placement of every
        // other axis lays the top range out differently from the eight intervals the top axis has always drawn.
        var renderer = new CartesianAxesRenderer(PlainTop(), new Rect(80, 60, 150, 300), new RecordingRenderContext(), Theme.Default);
        var range = renderer.ComputeSecondaryXDataRanges(0, 100);

        Assert.NotEqual(AxesRenderer.DefaultTickValues(range.XMin, range.XMax, 150), new AutoLocator(8).LocateInterval(range.XMin, range.XMax));
    }

    [Fact]
    public void TheSeriesOfATopAxisWithNeither_AreMappedLinearlyOverItsOwnRange()
    {
        var axes = PlainTop();
        var renderer = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default);
        var range = renderer.ComputeSecondaryXDataRanges(0, 100);
        var ctx = Render(axes);

        var line = Assert.Single(ctx.OfKind("DrawLines"), c => ((Point[])((dynamic)c.Args!).points).Length == 3);
        var points = (Point[])((dynamic)line.Args!).points;
        double[] topX = [0, 37, 113];
        for (int i = 0; i < topX.Length; i++)
            Assert.Equal(Plot.X + (topX[i] - range.XMin) / (range.XMax - range.XMin) * Plot.Width, points[i].X, 9);
    }

    [Theory]
    [InlineData(AxisScale.Log)]
    [InlineData(AxisScale.SymLog)]
    public void ASeriesOnATopAxis_StandsAtTheHeightThePrimaryGivesItsValue_OnAScaledPrimaryY(AxisScale scale)
    {
        // The top axis shares the primary's Y axis (matplotlib's twiny): the same value stands at the same height on a log or
        // symlog primary Y, not mapped linearly over the range the scale spreads.
        var axes = new Axes();
        axes.Plot(X, [1.0, 10, 100]);
        axes.PlotXSecondary(X, [1.0, 10, 100]);
        axes.YAxis.Scale = scale;

        var lines = Render(axes).OfKind("DrawLines")
            .Select(c => (Point[])((dynamic)c.Args!).points).Where(points => points.Length == 3).ToList();

        Assert.Equal(2, lines.Count);
        for (int i = 0; i < 3; i++)
            Assert.Equal(lines[0][i].Y, lines[1][i].Y, 9);
    }

    [Fact]
    public void AStreamingSeriesOnATopAxis_ContributesItsRangeToTheTopAxis()
    {
        // A live trace on the top axis is ranged as one on a Y slot is: by its own ComputeDataRange, never left on the 0..1
        // sentinel with the trace drawn off the plot.
        var axes = new Axes();
        axes.Plot(X, [0.0, 50, 100]);
        var live = axes.AddXSecondarySeries(new StreamingLineSeries(capacity: 10));
        live.AppendPoint(1000, 1);
        live.AppendPoint(2000, 2);
        var renderer = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default);

        var range = renderer.ComputeSecondaryXDataRanges(0, 100);

        Assert.True(range.XMin <= 1000 && range.XMax >= 2000, $"{range.XMin}..{range.XMax} does not cover 1000..2000");
    }
}
