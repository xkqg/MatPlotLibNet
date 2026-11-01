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

/// <summary>A Y axis slot past the primary that is linked to it (<see cref="Axis.Link"/>, matplotlib's <c>secondary_yaxis</c>):
/// it holds no series and is drawn all the same, over the primary's own range and transform. Its ticks are laid out over the
/// image of the primary range in its own unit, sorted low to high, by its own locator, else by the log locator when the primary
/// is logarithmic (matplotlib's <c>functionlog</c>), else by the default locator; each is labelled with its value and stands
/// where the primary shows the value it maps back to. It has no range of its own: a gesture writes none onto it, and a limit set
/// on it neither clips the series nor moves the primary. A range its transform cannot place draws no ticks and says so once,
/// never throwing. The layout reserves the band it draws in. A slot with series is placed as it always was.</summary>
/// <remarks>Joins the <c>ChartDiagnosticsGlobalState</c> collection: one test listens to the process-wide diagnostics.</remarks>
[Collection("ChartDiagnosticsGlobalState")]
public class LinkedYAxisTests
{
    private static readonly Rect Plot = new(200, 50, 400, 300);
    private static double PlotRight => Plot.X + Plot.Width;
    private static readonly double[] X = [0.0, 1, 2];
    private static readonly AxisTransform Fahrenheit = AxisTransform.Linear(1.8, 32);

    /// <summary>How far a primary Y tick label stands left of the plot: the default mark and pad.</summary>
    private static readonly double PrimaryReach = new TickConfig().Length + new TickConfig().Pad;

    /// <summary>A plot in degrees Celsius whose Y axis shows 0..100 exactly: 3 px per degree, 0 at the bottom (y 350).</summary>
    private static Axes Celsius()
    {
        var axes = new Axes();
        axes.Plot(X, [0.0, 50, 100]);
        axes.YAxis.Min = 0;
        axes.YAxis.Max = 100;
        return axes;
    }

    /// <summary>Stacks a slot past the primary on <paramref name="side"/> and links it through <paramref name="link"/>.</summary>
    private static YAxisSlot Linked(Axes axes, AxisSide side, AxisTransform link)
    {
        var slot = axes.YAxes.StackOn(side).Add();
        slot.Axis.Link = link;
        return slot;
    }

    private static RecordingRenderContext Render(Axes axes)
    {
        var ctx = new RecordingRenderContext();
        new CartesianAxesRenderer(axes, Plot, ctx, Theme.Default).Render();
        return ctx;
    }

    /// <summary>One upright tick label: its text and the height it is drawn at (its tick's height plus 4 px).</summary>
    private readonly record struct Label(string Text, double Y);

    private static IEnumerable<dynamic> UprightTexts(RecordingRenderContext ctx) =>
        ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!).Where(a => (double)a.rotation == 0);

    /// <summary>The tick labels of the slot past the primary on <paramref name="side"/>, in the order drawn: on the right those
    /// past the right edge, on the left those beyond the primary's own labels.</summary>
    private static List<Label> LinkedLabels(RecordingRenderContext ctx, AxisSide side) =>
        [.. UprightTexts(ctx)
            .Where(a => side == AxisSide.Right
                ? (TextAlignment)a.alignment == TextAlignment.Left && ((Point)a.position).X > PlotRight
                : (TextAlignment)a.alignment == TextAlignment.Right && ((Point)a.position).X < Plot.X - PrimaryReach - 1)
            .Select(a => new Label((string)a.text, ((Point)a.position).Y))];

    /// <summary>The primary Y axis' tick labels, in the order drawn.</summary>
    private static List<Label> PrimaryLabels(RecordingRenderContext ctx) =>
        [.. UprightTexts(ctx)
            .Where(a => (TextAlignment)a.alignment == TextAlignment.Right
                && Math.Abs(((Point)a.position).X - (Plot.X - PrimaryReach)) < 1e-6)
            .Select(a => new Label((string)a.text, ((Point)a.position).Y))];

    private static string Text(double value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>The image of 0..100 °C is 32..212 °F; the default locator for a 300 px plot lays it out in steps of 25.</summary>
    private static readonly double[] FahrenheitTicks = [50, 75, 100, 125, 150, 175, 200];

    // ── drawn without series ────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(AxisSide.Right)]
    [InlineData(AxisSide.Left)]
    public void ALinkedSlotWithoutSeries_IsDrawn(AxisSide side)
    {
        var axes = Celsius();
        Linked(axes, side, Fahrenheit);

        Assert.NotEmpty(LinkedLabels(Render(axes), side));
    }

    [Theory]
    [InlineData(AxisSide.Right)]
    [InlineData(AxisSide.Left)]
    public void FahrenheitBesideCelsius_LabelsEachTickWithItsValue_AtTheHeightWhereThePrimaryShowsItsInverse(AxisSide side)
    {
        var axes = Celsius();
        Linked(axes, side, Fahrenheit);

        var labels = LinkedLabels(Render(axes), side);

        Assert.Equal(FahrenheitTicks.Select(Text), labels.Select(l => l.Text));
        for (int i = 0; i < FahrenheitTicks.Length; i++)
            Assert.Equal(350 - (FahrenheitTicks[i] - 32) / 1.8 * 3 + 4, labels[i].Y, 9);
    }

    [Fact]
    public void ALinkedRightSlot_OwnsTheRightEdge_SoTheMirroredPrimaryLabelsGiveWay()
    {
        var axes = Celsius();
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Mirror = true };
        Linked(axes, AxisSide.Right, Fahrenheit);

        var bands = AxesRenderer.Create(axes, Plot, new RecordingRenderContext(), Theme.Default).MeasureEdgeBands();

        Assert.Equal(0, bands.Right);
        Assert.Equal(FahrenheitTicks.Select(Text), LinkedLabels(Render(axes), AxisSide.Right).Select(l => l.Text));
    }

    [Fact]
    public void ALinkedSlot_IsPlacedOnThePrimarysRangeAndTransform_ItsTicksStoodByTheLink()
    {
        var axes = Celsius();
        var slot = Linked(axes, AxisSide.Right, Fahrenheit);
        var renderer = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default);

        var placed = renderer.PlaceSlot(slot, new MinMaxRange(0, 1));

        Assert.Equal(renderer.ComputeDataRanges(), placed.Range);
        Assert.Equal(renderer.ComputeViewTransform().DrawnRange, placed.Transform.DrawnRange);
        Assert.IsType<LinkedTickPlacement>(placed.Placement);
        Assert.Equal(FahrenheitTicks, placed.Ticks);
        Assert.Null(placed.Diagnostic);
    }

    // ── which ticks ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void OnALogPrimary_TheLinkedSlotTakesTheLogLocatorsDecades_WrittenByTheLogFormatter()
    {
        // A period beside a plot over frequency 1..1000 Hz on a log axis: matplotlib draws the linked axis on its functionlog
        // scale, so its ticks are decades of the period 0.001..1 s, each where the plot shows its frequency.
        var axes = new Axes();
        axes.Plot([0.0, 1, 2, 3], [1.0, 10, 100, 1000]);
        axes.YAxis.Scale = AxisScale.Log;
        axes.YAxis.Min = 1;
        axes.YAxis.Max = 1000;
        Linked(axes, AxisSide.Right, AxisTransform.Reciprocal());

        var labels = LinkedLabels(Render(axes), AxisSide.Right);

        var decades = new LogLocator().Locate(1.0 / 1000, 1);
        Assert.True(decades.Length >= 3, $"{decades.Length} decades");
        Assert.All(decades, t => Assert.Equal(Math.Round(Math.Log10(t)), Math.Log10(t), 9));
        Assert.Equal(decades.Select(new LogTickFormatter().Format), labels.Select(l => l.Text));
        for (int i = 0; i < decades.Length; i++)
            Assert.Equal(350 - Math.Log10(1 / decades[i]) / 3 * 300 + 4, labels[i].Y, 6);
    }

    [Fact]
    public void OnASymLogPrimary_TheLinkedSlotKeepsTheDefaultLocator_AsOnlyALogPrimarySwitchesInMatplotlib()
    {
        var axes = Celsius();
        axes.YAxis.Scale = AxisScale.SymLog;
        Linked(axes, AxisSide.Right, AxisTransform.Linear(2));

        var labels = LinkedLabels(Render(axes), AxisSide.Right);

        // The image 0..200, in steps of 25 for a 300 px plot.
        Assert.Equal(new[] { 0.0, 25, 50, 75, 100, 125, 150, 175, 200 }.Select(Text), labels.Select(l => l.Text));
    }

    [Fact]
    public void ALinkedSlotsOwnLocator_Wins()
    {
        var axes = Celsius();
        var slot = Linked(axes, AxisSide.Right, Fahrenheit);
        slot.Axis.TickLocator = new MultipleLocator(50);

        var labels = LinkedLabels(Render(axes), AxisSide.Right);

        Assert.Equal(["50", "100", "150", "200"], labels.Select(l => l.Text));
    }

    [Fact]
    public void ALinkThatRunsDown_LaysItsTicksOutOverTheImageSortedLowToHigh()
    {
        // 100 - x over 0..100: the image runs from 100 at the bottom to 0 at the top. Its ticks are those of the interval 0..100,
        // each standing where the plot shows 100 minus its value, so the labels read 0 at the top down to 100 at the bottom.
        var axes = Celsius();
        Linked(axes, AxisSide.Right, AxisTransform.Linear(-1, 100));

        var labels = LinkedLabels(Render(axes), AxisSide.Right);

        double[] ticks = [0, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
        Assert.Equal(ticks.Select(Text), labels.Select(l => l.Text));
        for (int i = 0; i < ticks.Length; i++)
            Assert.Equal(350 - (100 - ticks[i]) * 3 + 4, labels[i].Y, 9);
    }

    [Fact]
    public void ATickWhoseInverseFallsInABreakOfThePrimary_IsLeftOut_AsThePrimaryLeavesItsOwnOut()
    {
        var axes = Celsius();
        axes.AddYBreak(35, 65);
        Linked(axes, AxisSide.Right, AxisTransform.Linear(1));

        var ctx = Render(axes);
        var linked = LinkedLabels(ctx, AxisSide.Right);

        Assert.Equal(PrimaryLabels(ctx), linked);
        Assert.DoesNotContain("50", linked.Select(l => l.Text));
        Assert.All(ctx.OfKind("DrawLine").Select(c => (dynamic)c.Args!), a =>
            Assert.True(double.IsFinite(((Point)a.p1).Y) && double.IsFinite(((Point)a.p2).Y), "a mark was handed a missing value"));
    }

    // ── a range the link cannot place ───────────────────────────────────────────────────────────────────

    [Fact]
    public void AReciprocalAcrossZero_DrawsNoTicks_AndSaysWhyOnce_WithoutThrowing()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [-1.0, 0, 1]).SetYLim(-1, 1)
            .WithAddRightYAxis(period => period.SetYLabel("period").Link(AxisTransform.Reciprocal()))).Build();
        var diagnostics = new List<ChartDiagnostic>();
        void Collect(ChartDiagnostic diagnostic) => diagnostics.Add(diagnostic);

        ChartDiagnostics.Emitted += Collect;
        string svg;
        try
        {
            svg = figure.ToSvg();
        }
        finally
        {
            ChartDiagnostics.Emitted -= Collect;
        }

        var said = Assert.Single(diagnostics);
        Assert.Equal(nameof(CartesianAxesRenderer), said.Source);
        Assert.Null(said.Exception);
        Assert.Contains("-1..1", said.Message, StringComparison.Ordinal);
        Assert.Contains("<svg", svg, StringComparison.Ordinal);
        Assert.Empty(LinkedLabels(Render(figure.SubPlots[0]), AxisSide.Right));
    }

    // ── the layout reserves what is drawn ───────────────────────────────────────────────────────────────

    [Fact]
    public void TheBandTheLayoutReservesForALinkedSlot_IsTheBandItIsDrawnIn()
    {
        var ctx = new RecordingRenderContext();
        var figure = Plt.Create().WithSize(600, 500).AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [0.0, 50, 100]).SetYLim(0, 100)
            .WithAddRightYAxis(milli => milli.Link(AxisTransform.Linear(1000)))).Build();
        var axes = figure.SubPlots[0];

        double reserved = Assert.Single(ConstrainedLayoutEngine.SecondaryAxisBands(axes, ctx, figure.Theme));
        new ChartRenderer().Render(figure, ctx);

        // What the linked slot drew: its labels left-aligned past the right edge, 0 to 100000 in steps of 10000.
        var drawn = ctx.OfKind("DrawText").Select(c => (dynamic)c.Args!)
            .Where(a => (string)a.text != "" && (TextAlignment)a.alignment == TextAlignment.Left).ToList();
        var ticks = axes.YAxes.Right[0].Axis.MajorTicks;
        Assert.Equal(11, drawn.Count);
        Assert.Equal(ticks.Length + ticks.Pad + drawn.Max(a => ctx.MeasureText((string)a.text, (Font)a.font).Width), reserved, 6);
    }

    // ── no range of its own ─────────────────────────────────────────────────────────────────────────────

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
        var axes = Celsius();
        Linked(axes, AxisSide.Right, Fahrenheit);
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
        static string Svg(AxisSide side, bool limited) => Plt.Create().AddSubPlot(1, 1, 1, ax =>
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

    // ── a slot with series is placed as before ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(AxisSide.Right)]
    [InlineData(AxisSide.Left)]
    public void ASlotWithSeries_IsPlacedAsBefore_OnItsOwnRange_ItsTicksStoodByItsOwnTransform(AxisSide side)
    {
        var axes = Celsius();
        var slot = axes.YAxes.StackOn(side).Add();
        slot.AddSeries(new LineSeries(X, [10.0, 400, 20]));
        var renderer = new CartesianAxesRenderer(axes, Plot, new RecordingRenderContext(), Theme.Default);

        var placed = renderer.PlaceSlot(slot, new MinMaxRange(0, 2));

        var range = renderer.ComputeSecondaryDataRanges(0, 2, slot.Axis, slot.Series);
        Assert.Equal(range, placed.Range);
        Assert.Same(placed.Transform, placed.Placement);
        Assert.Equal(slot.Axis.SlotTickValues(range, Plot.Height), placed.Ticks);
        Assert.Equal(slot.Axis.SlotTickSource(), placed.Source);
        Assert.Null(placed.Diagnostic);
    }
}
