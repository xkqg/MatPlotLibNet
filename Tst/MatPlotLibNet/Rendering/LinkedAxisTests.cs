// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>An axis linked to the primary one (<see cref="Axis.Link"/>, matplotlib's <c>secondary_xaxis</c> and <c>secondary_yaxis</c>):
/// the top X axis, and a Y axis slot past the primary on either side. It holds no series and is drawn all the same, over the primary's
/// own range and transform, by one rule for every side: its ticks are laid out over the image of the primary's range in its own unit,
/// sorted low to high, by its own locator, else by the log locator when the primary is logarithmic (matplotlib's <c>functionlog</c>),
/// else by the default placement for the plot's length; each is labelled with its value and stands where the primary shows the value
/// it maps back to. It owns the edge it stands on, so the mirrored primary labels give way to it, and the layout reserves the band it
/// draws. A range its transform cannot place draws no ticks and says so once, never throwing. It has no range of its own: a gesture
/// writes none onto it, and a limit set on it neither clips the series nor moves the primary. A slot with series is placed as it
/// always was. The rules every side shares are theories over the side's name; what only a side has is a fact of its own.</summary>
/// <remarks>Joins the <c>ChartDiagnosticsGlobalState</c> collection: one test listens to the process-wide diagnostics.</remarks>
[Collection("ChartDiagnosticsGlobalState")]
public class LinkedAxisTests
{
    private static readonly Rect Plot = new(200, 50, 400, 300);
    private static double PlotRight => Plot.X + Plot.Width;
    private static readonly double[] X = [0.0, 1, 2];
    private static readonly AxisTransform Fahrenheit = AxisTransform.Linear(1.8, 32);

    /// <summary>How far a primary Y tick label stands left of the plot: the default mark and pad.</summary>
    private static readonly double PrimaryReach = new TickConfig().Length + new TickConfig().Pad;

    /// <summary>The image of 0..100 °C is 32..212 °F; the default placement for a 400 px or a 300 px plot lays it out in steps of 25.</summary>
    private static readonly double[] FahrenheitTicks = [50, 75, 100, 125, 150, 175, 200];

    private static string Text(double value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>How far down its edge the plot stands the value that is <paramref name="fahrenheit"/> °F.</summary>
    private static double FractionOf(double fahrenheit) => (fahrenheit - 32) / 1.8 / 100;

    /// <summary>A linked axis given to the axes: the axis itself, and how the placement places it.</summary>
    private readonly record struct LinkedAxis(Axis Axis, Func<CartesianAxisPlacement, DataRange, PlacedAxis> Place);

    /// <summary>One side a linked axis stands on, as the shared rules read it: how a plot whose axis along it shows 0..100 is made, how
    /// an axis is linked there, where it draws its labels and where the plot stands a fraction of its range.</summary>
    private sealed record Side(
        Func<Axes> Celsius,
        Func<Axes, AxisTransform, LinkedAxis> Link,
        Func<RecordingRenderContext, List<TickLabel>> Labels,
        Func<RecordingRenderContext, List<TickLabel>> PrimaryLabels,
        Func<double, double> At,
        Action<Axes> Mirror,
        Func<Axes> LogPrimary,
        Action<Axes> Break,
        Func<DrawLineCall, double[]> Coordinates,
        Func<Figure> ReciprocalAcrossZero,
        string Named);

    private static Axes CelsiusAlongX()
    {
        var axes = new Axes();
        axes.Plot([0.0, 50, 100], [1.0, 3, 2]);
        axes.XAxis.Min = 0;
        axes.XAxis.Max = 100;
        return axes;
    }

    /// <summary>A plot in degrees Celsius whose Y axis shows 0..100 exactly: 3 px per degree, 0 at the bottom (y 350).</summary>
    private static Axes CelsiusAlongY()
    {
        var axes = new Axes();
        axes.Plot(X, [0.0, 50, 100]);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
        return axes;
    }

    private static double TickLabelRise => 0.35 * Theme.Default.TickFont().Size;

    private static Func<Axes, AxisTransform, LinkedAxis> Stacked(AxisSide side) => (axes, link) =>
    {
        var slot = axes.YAxes.StackOn(side).Add();
        slot.Axis.Link = link;
        return new(slot.Axis, (placement, primary) => placement.PlaceSlot(slot, primary));
    };

    private static Func<RecordingRenderContext, List<TickLabel>> BeyondTheRight { get; } = ctx =>
        [.. ctx.Of<DrawTextCall>().Where(a => a.Rotation == 0 && a.Alignment == TextAlignment.Left && a.Position.X > PlotRight)
            .Select(a => new TickLabel(a.Text, a.Position.Y))];

    private static Func<RecordingRenderContext, List<TickLabel>> BeyondTheLeft { get; } = ctx =>
        [.. ctx.Of<DrawTextCall>().Where(a => a.Rotation == 0 && a.Alignment == TextAlignment.Right && a.Position.X < Plot.X - PrimaryReach - 1)
            .Select(a => new TickLabel(a.Text, a.Position.Y))];

    private static Func<RecordingRenderContext, List<TickLabel>> PrimaryY { get; } = ctx =>
        [.. ctx.Of<DrawTextCall>().Where(a => a.Rotation == 0 && a.Alignment == TextAlignment.Right
                && Math.Abs(a.Position.X - (Plot.X - PrimaryReach)) < 1e-6)
            .Select(a => new TickLabel(a.Text, a.Position.Y))];

    private static readonly Side Top = new(
        CelsiusAlongX,
        (axes, link) =>
        {
            axes.TwinY();
            var top = axes.SecondaryXAxis!;
            top.Link = link;
            return new(top, (placement, primary) => placement.PlaceTopAxis(primary));
        },
        ctx => ctx.LabelsAbove(Plot),
        ctx => ctx.LabelsBelow(Plot),
        fraction => Plot.X + fraction * Plot.Width,
        axes => axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Mirror = true },
        () =>
        {
            var axes = new Axes();
            axes.Plot([1.0, 10, 100, 1000], [1.0, 2, 3, 4]);
            axes.XAxis.Scale = AxisScale.Log;
            axes.XAxis.Min = 1;
            axes.XAxis.Max = 1000;
            return axes;
        },
        axes => axes.AddXBreak(35, 65),
        line => [line.P1.X, line.P2.X],
        () => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([-1.0, 0, 1], [1.0, 2, 3]).SetXLim(-1, 1)
            .WithSecondaryXAxis(period => period.Link(AxisTransform.Reciprocal()))).Build(),
        "top X axis");

    private static readonly Side Right = new(
        CelsiusAlongY,
        Stacked(AxisSide.Right),
        BeyondTheRight,
        PrimaryY,
        fraction => Plot.Y + Plot.Height - fraction * Plot.Height + TickLabelRise,
        axes => axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true },
        () =>
        {
            var axes = new Axes();
            axes.Plot([0.0, 1, 2, 3], [1.0, 10, 100, 1000]);
            axes.YAxis.Scale = AxisScale.Log;
            axes.YAxis.Min = 1;
            axes.YAxis.Max = 1000;
            return axes;
        },
        axes => axes.AddYBreak(35, 65),
        line => [line.P1.Y, line.P2.Y],
        () => Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [-1.0, 0, 1]).SetYLim(-1, 1)
            .WithAddRightYAxis(period => period.SetYLabel("period").Link(AxisTransform.Reciprocal()))).Build(),
        "linked Y axis");

    private static readonly Dictionary<string, Side> Sides = new()
    {
        ["Top"] = Top,
        ["Right"] = Right,
        ["Left"] = Right with { Link = Stacked(AxisSide.Left), Labels = BeyondTheLeft },
    };

    /// <summary>The sides a linked axis stands on in the plot: the top edge and either Y edge.</summary>
    public static TheoryData<string> EverySide { get; } = ["Top", "Right", "Left"];

    /// <summary>The sides whose primary axis puts its own labels on the edge a linked axis takes over.</summary>
    public static TheoryData<string> SidesTheLinkedAxisTakesOver { get; } = ["Top", "Right"];

    // ── drawn without series ────────────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(EverySide))]
    public void ALinkedAxisWithoutSeries_IsDrawn(string name)
    {
        var side = Sides[name];
        var axes = side.Celsius();
        side.Link(axes, Fahrenheit);

        Assert.NotEmpty(side.Labels(axes.Drawn(Plot)));
    }

    [Theory]
    [MemberData(nameof(EverySide))]
    public void FahrenheitBesideCelsius_LabelsEachTickWithItsValue_WhereThePrimaryShowsItsInverse(string name)
    {
        var side = Sides[name];
        var axes = side.Celsius();
        side.Link(axes, Fahrenheit);

        var labels = side.Labels(axes.Drawn(Plot));

        Assert.Equal(FahrenheitTicks.Select(Text), labels.Select(l => l.Text));
        for (int i = 0; i < FahrenheitTicks.Length; i++)
            Assert.Equal(side.At(FractionOf(FahrenheitTicks[i])), labels[i].Along, 9);
    }

    [Theory]
    [MemberData(nameof(SidesTheLinkedAxisTakesOver))]
    public void ALinkedAxis_OwnsItsEdge_SoTheMirroredPrimaryLabelsGiveWay(string name)
    {
        var side = Sides[name];
        var axes = side.Celsius();
        side.Mirror(axes);
        side.Link(axes, Fahrenheit);

        Assert.Equal(FahrenheitTicks.Select(Text), side.Labels(axes.Drawn(Plot)).Select(l => l.Text));
    }

    [Fact]
    public void ALinkedRightSlot_TakesTheMirroredLabelsBandAsItsOwn()
    {
        var axes = CelsiusAlongY();
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };
        Sides["Right"].Link(axes, Fahrenheit);

        var bands = AxesRenderer.Create(axes, Plot, new RecordingRenderContext(), Theme.Default).MeasureEdgeBands();

        Assert.Equal(0, bands.Right);
    }

    [Theory]
    [MemberData(nameof(SidesTheLinkedAxisTakesOver))]
    public void ALinkedAxis_IsPlacedOnThePrimarysRangeAndTransform_ItsTicksStoodByTheLink(string name)
    {
        var side = Sides[name];
        var axes = side.Celsius();
        var linked = side.Link(axes, Fahrenheit);
        var renderer = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default);
        var placement = new CartesianAxisPlacement(axes, Plot, new RecordingRenderContext(), Theme.Default);
        var primary = placement.ComputeDataRanges();

        var placed = linked.Place(placement, primary);

        Assert.Equal(primary, placed.Range);
        Assert.Equal(renderer.ComputeViewTransform().DrawnRange, placed.Transform.DrawnRange);
        Assert.IsType<LinkedTickPlacement>(placed.Placement);
        Assert.Equal(FahrenheitTicks, placed.Ticks);
        Assert.Null(placed.Diagnostic);
    }

    // ── which ticks ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>A period beside a plot over frequency 1..1000 Hz on a log axis: matplotlib draws the linked axis on its functionlog
    /// scale, so its ticks are decades of the period 0.001..1 s, each where the plot shows its frequency.</summary>
    [Theory]
    [MemberData(nameof(SidesTheLinkedAxisTakesOver))]
    public void OnALogPrimary_TheLinkedAxisTakesTheLogLocatorsDecades_WrittenByTheLogFormatter(string name)
    {
        var side = Sides[name];
        var axes = side.LogPrimary();
        side.Link(axes, AxisTransform.Reciprocal());

        var labels = side.Labels(axes.Drawn(Plot));

        var decades = new LogLocator().Locate(1.0 / 1000, 1);
        Assert.True(decades.Length >= 3, $"{decades.Length} decades");
        Assert.All(decades, t => Assert.Equal(Math.Round(Math.Log10(t)), Math.Log10(t), 9));
        Assert.Equal(decades.Select(new LogTickFormatter().Format), labels.Select(l => l.Text));
        for (int i = 0; i < decades.Length; i++)
            Assert.Equal(side.At(Math.Log10(1 / decades[i]) / 3), labels[i].Along, 6);
    }

    [Theory]
    [MemberData(nameof(SidesTheLinkedAxisTakesOver))]
    public void ALinkedAxisOwnLocator_Wins(string name)
    {
        var side = Sides[name];
        var axes = side.Celsius();
        side.Link(axes, Fahrenheit).Axis.TickLocator = new MultipleLocator(50);

        Assert.Equal(["50", "100", "150", "200"], side.Labels(axes.Drawn(Plot)).Select(l => l.Text));
    }

    [Theory]
    [MemberData(nameof(SidesTheLinkedAxisTakesOver))]
    public void ATickWhoseInverseFallsInABreakOfThePrimary_IsLeftOut_AsThePrimaryLeavesItsOwnOut(string name)
    {
        var side = Sides[name];
        var axes = side.Celsius();
        side.Break(axes);
        side.Link(axes, AxisTransform.Linear(1));

        var ctx = axes.Drawn(Plot);
        var linked = side.Labels(ctx);

        Assert.Equal(side.PrimaryLabels(ctx), linked);
        Assert.DoesNotContain("50", linked.Select(l => l.Text));
        Assert.All(ctx.Of<DrawLineCall>(), a => side.Coordinates(a).AssertAllFinite("a mark was handed a missing value"));
    }

    [Fact]
    public void OnASymLogPrimary_TheLinkedSlotKeepsTheDefaultLocator_AsOnlyALogPrimarySwitchesInMatplotlib()
    {
        var axes = CelsiusAlongY();
        axes.YAxis.Scale = AxisScale.SymLog;
        Sides["Right"].Link(axes, AxisTransform.Linear(2));

        var labels = BeyondTheRight(axes.Drawn(Plot));

        // The image 0..200, in steps of 25 for a 300 px plot.
        Assert.Equal(new[] { 0.0, 25, 50, 75, 100, 125, 150, 175, 200 }.Select(Text), labels.Select(l => l.Text));
    }

    [Fact]
    public void ALinkThatRunsDown_LaysItsTicksOutOverTheImageSortedLowToHigh()
    {
        // 100 - x over 0..100: the image runs from 100 at the bottom to 0 at the top. Its ticks are those of the interval 0..100,
        // each standing where the plot shows 100 minus its value, so the labels read 0 at the top down to 100 at the bottom.
        var axes = CelsiusAlongY();
        Sides["Right"].Link(axes, AxisTransform.Linear(-1, 100));

        var labels = BeyondTheRight(axes.Drawn(Plot));

        double[] ticks = [0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
        Assert.Equal(ticks.Select(Text), labels.Select(l => l.Text));
        for (int i = 0; i < ticks.Length; i++)
            Assert.Equal(Sides["Right"].At((100 - ticks[i]) / 100), labels[i].Along, 9);
    }

    // ── a range the link cannot place ───────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(SidesTheLinkedAxisTakesOver))]
    public void AReciprocalAcrossZero_DrawsNoTicks_AndSaysWhyOnce_WithoutThrowing(string name)
    {
        var side = Sides[name];
        var figure = side.ReciprocalAcrossZero();
        var (svg, diagnostics) = DiagnosticsCapture.Of(() => figure.ToSvg());

        var said = Assert.Single(diagnostics);
        Assert.Equal(nameof(CartesianAxesRenderer), said.Source);
        Assert.Null(said.Exception);
        Assert.Contains(side.Named, said.Message, StringComparison.Ordinal);
        Assert.Contains("-1..1", said.Message, StringComparison.Ordinal);
        Assert.Contains("<svg", svg, StringComparison.Ordinal);
        Assert.Empty(side.Labels(figure.SubPlots[0].Drawn(Plot)));
    }

    // ── the top edge: the layout reserves what is drawn, the title rises above it ───────────────────────

    [Fact]
    public void ALinkedTopAxisMinorTicks_AreSplitInItsOwnUnit_EachWhereThePrimaryShowsItsInverse()
    {
        var axes = CelsiusAlongX();
        var top = Sides["Top"].Link(axes, Fahrenheit).Axis;
        top.MinorTicks = top.MinorTicks with { Visible = true };

        var ctx = axes.Drawn(Plot);

        var minors = ctx.Of<DrawLineCall>()
            .Where(a => Math.Abs(a.P1.Y - (Plot.Y - top.MinorTicks.Length)) < 1e-9)
            .Select(a => a.P1.X).ToList();
        foreach (double fahrenheit in new[] { 55.0, 60, 65, 70 })
            Assert.Contains(minors, x => Math.Abs(x - Sides["Top"].At(FractionOf(fahrenheit))) < 1e-9);
    }

    [Theory]
    [InlineData(400.0)]
    [InlineData(180.0)]
    public void TheBandTheLayoutReservesForALinkedTopAxis_IsTheBandItIsDrawnIn(double width)
    {
        // Turned labels reach as far as their text is long, and the default placement depends on the plot's width: the layout must
        // place the linked axis at the width it is drawn at. The band reaches the top of the axis' title.
        var plot = new Rect(Plot.X, Plot.Y, width, Plot.Height);
        var axes = CelsiusAlongX();
        var top = Sides["Top"].Link(axes, Fahrenheit).Axis;
        top.Label = "°F";
        top.LabelStyle = new TextStyle { Pad = 3, FontSize = 20 };
        top.LabelRotation = 30;

        double measured = AxesRenderer.Create(axes, plot, new RecordingRenderContext(), Theme.Default).MeasureEdgeBands().Top;
        var ctx = axes.Drawn(plot);

        var title = ctx.Of<DrawTextCall>().Single(a => a.Text == "°F");
        var font = title.Font;
        // The top of the title's box: its part above the baseline (the descent faces the tick labels below it).
        double titleTop = title.Position.Y - (ctx.MeasureText("°F", font).Height - FacingInk.Descent * font.Size);
        Assert.Contains(ctx.Of<DrawTextCall>(), a => a.Rotation == 30);
        Assert.Equal(plot.Y - titleTop, measured, 9);
    }

    [Fact]
    public void TheAxesTitle_RisesByTheBandOfTheLinkedTopAxis()
    {
        static double TitleY(RecordingRenderContext ctx) => ctx.Of<DrawTextCall>().Single(a => a.Text == "A Title").Position.Y;
        var plain = CelsiusAlongX();
        plain.Title = "A Title";
        var axes = CelsiusAlongX();
        Sides["Top"].Link(axes, Fahrenheit);
        axes.Title = "A Title";

        double band = AxesRenderer.Create(axes, Plot, new RecordingRenderContext(), Theme.Default).MeasureEdgeBands().Top;

        Assert.True(band > 0);
        Assert.Equal(TitleY(plain.Drawn(Plot)) - band, TitleY(axes.Drawn(Plot)), 9);
    }

    // ── no range of its own ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ALink_NeverChangesThePrimarysRange_NorItsTicks_WhateverLimitTheLinkedAxisCarries()
    {
        static Axes Data()
        {
            var axes = new Axes();
            axes.Plot([3.0, 40, 77], [1.0, 3, 2]);
            return axes;
        }
        var linked = Data();
        var top = Sides["Top"].Link(linked, Fahrenheit).Axis;
        top.Min = 500;
        top.Max = 600;
        static DataRange RangeOf(Axes axes) =>
            new CartesianAxisPlacement(axes, Plot, new RecordingRenderContext(), Theme.Default).ComputeDataRanges();

        Assert.Equal(RangeOf(Data()), RangeOf(linked));
        Assert.Equal(Data().Drawn(Plot).LabelsBelow(Plot), linked.Drawn(Plot).LabelsBelow(Plot));
        Assert.NotEmpty(linked.Drawn(Plot).LabelsAbove(Plot));
    }

    [Theory]
    [InlineData("pan")]
    [InlineData("zoom")]
    [InlineData("reset")]
    public void AGesture_WritesNoRangeOntoALinkedSlot_AndStillMovesASlotWithARangeOfItsOwn(string gesture)
    {
        var figure = Plt.Create().WithServerInteraction("c", s => s.All()).AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [0.0, 50, 100])
            .WithAddRightYAxis(fahrenheit => fahrenheit.Link(Fahrenheit))
            .WithAddRightYAxis(rate => rate.Plot(X, [10.0, 400, 20]))
            .WithAddLeftYAxis(kelvin => kelvin.Link(AxisTransform.Linear(1, 273.15)))).Build();
        var axes = figure.SubPlots[0];
        var moved = new DataRange(0, 2, 40, 90);
        SecondaryAxisRange[] ranges = [new(AxisSide.Right, 0, moved), new(AxisSide.Right, 1, moved), new(AxisSide.Left, 0, moved)];
        FigureInteractionEvent applied = gesture switch
        {
            "pan" => new PanEvent("c", 0, 0.5, 5) { InView = new DataRange(0, 2, 0, 100), SecondaryRanges = ranges },
            "zoom" => new ZoomEvent("c", 0, 0.5, 1.5, 20, 80) { SecondaryRanges = ranges },
            _ => new ResetEvent("c", 0, 0, 2, 0, 100) { SecondaryRanges = ranges },
        };

        applied.ApplyTo(figure);

        Assert.Null(axes.YAxes.Right[0].Axis.Min);
        Assert.Null(axes.YAxes.Right[0].Axis.Max);
        Assert.Null(axes.YAxes.Left[1].Axis.Min);
        Assert.Null(axes.YAxes.Left[1].Axis.Max);
        Assert.Equal(40, axes.YAxes.Right[1].Axis.Min);
        Assert.Equal(90, axes.YAxes.Right[1].Axis.Max);
    }

    [Fact]
    public void ALinkedSlot_KeepsItsPlaceInTheInteractionViews_ShowingThePrimarysRange()
    {
        // The script reads the views of one side by a running index, so a slot past a linked one keeps its number only when the
        // linked one keeps its place; its view is the primary's, and what a gesture sends for it is not written (above).
        var axes = CelsiusAlongY();
        Sides["Right"].Link(axes, Fahrenheit);
        axes.YAxes.Right.Add().AddSeries(new LineSeries(X, [10.0, 400, 20]));
        var renderer = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default);

        var views = renderer.ComputeSecondaryViewTransforms();

        Assert.Equal([0, 1], views.Select(v => v.SlotIndex));
        Assert.Equal(renderer.ComputeViewTransform().DrawnRange, views[0].Transform.DrawnRange);
    }

    [Theory]
    [InlineData(AxisSide.Right)]
    [InlineData(AxisSide.Left)]
    public void ALinkedSlotsOwnLimit_IsNoLimit_ItNeitherClipsTheSeriesNorMovesThePrimary(AxisSide side)
    {
        // Under a library theme, which clips the series only under a limit of the axes (the matplotlib themes clip every series).
        static string Svg(AxisSide side, bool limited) => Plt.Create().WithTheme(Theme.Default).AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot(X, [0.0, 50, 100]);
            void Configure(YAxisSlotBuilder slot)
            {
                if (limited) slot.SetYLim(500, 600);
                slot.Link(Fahrenheit);
            }

            if (side == AxisSide.Right) ax.WithAddRightYAxis(Configure);
            else ax.WithAddLeftYAxis(Configure);
        }).ToSvg();

        string limited = Svg(side, limited: true);

        Assert.DoesNotContain("clip-path", limited, StringComparison.Ordinal);
        Assert.Equal(Svg(side, limited: false), limited);
    }

    // ── the layout reserves what a slot draws ───────────────────────────────────────────────────────────

    [Fact]
    public void TheBandTheLayoutReservesForALinkedSlot_IsTheBandItIsDrawnIn()
    {
        var ctx = new RecordingRenderContext();
        var figure = Plt.Create().WithSize(600, 500).AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [0.0, 50, 100]).SetYLim(0, 100)
            .WithAddRightYAxis(milli => milli.Link(AxisTransform.Linear(1000)))).Build();
        var axes = figure.SubPlots[0];

        double reserved = Assert.Single(new AxesEdgeNeeds().SecondaryAxisBands(axes, ctx, figure.Theme));
        new ChartRenderer().Render(figure, ctx);

        // What the linked slot drew: its labels left-aligned past the right edge, 0 to 100000 in steps of 20000, as matplotlib 3.11.2's
        // secondary_yaxis prints them in a 6 x 5 in figure (get_tick_space 13, so nine bins: the step at or above 11111).
        var drawn = ctx.Of<DrawTextCall>()
            .Where(a => a.Text != "" && a.Alignment == TextAlignment.Left).ToList();
        var ticks = axes.YAxes.Right[0].Axis.MajorTicks;
        Assert.Equal(6, drawn.Count);
        Assert.Equal(ticks.Length + ticks.Pad + drawn.Max(a => ctx.MeasureText(a.Text, a.Font).Width), reserved, 6);
    }

    // ── a slot with series is placed as before ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(AxisSide.Right)]
    [InlineData(AxisSide.Left)]
    public void ASlotWithSeries_IsPlacedAsBefore_OnItsOwnRange_ItsTicksStoodByItsOwnTransform(AxisSide side)
    {
        var axes = CelsiusAlongY();
        var slot = axes.YAxes.StackOn(side).Add();
        slot.AddSeries(new LineSeries(X, [10.0, 400, 20]));
        var placement = new CartesianAxisPlacement(axes, Plot, new RecordingRenderContext(), Theme.Default);

        var placed = placement.PlaceSlot(slot, new DataRange(0, 2, 0, 1));

        var range = placement.ComputeSecondaryDataRanges(0, 2, slot.Axis, slot.Series);
        Assert.Equal(range, placed.Range);
        Assert.Same(placed.Transform, placed.Placement);
        Assert.Equal(slot.Axis.SlotTickValues(range, Plot.Height, Theme.Default), placed.Ticks);
        Assert.Equal(slot.Axis.SlotTickSource(Theme.Default), placed.Source);
        Assert.Null(placed.Diagnostic);
    }
}
