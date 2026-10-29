// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Every Y axis of the container, the primary included, is drawn by ONE rule: its ticks follow its own scale and
/// locator, it draws minor ticks when asked, its title draws math as rich text, a right slot is the mirror image of a left
/// one, and a slot's range and transform are worked out once per render. Each test draws an axes through its renderer
/// into a recording context and reads the tick marks, tick labels and titles each slot drew at its own edge.</summary>
public class YAxisSlotRenderingTests
{
    private static readonly Rect Plot = new(200, 50, 400, 300);
    private static double PlotRight => Plot.X + Plot.Width;
    private static readonly double[] X4 = [0.0, 1, 2, 3];

    private static RecordingRenderContext Render(Axes axes)
    {
        var ctx = new RecordingRenderContext();
        new CartesianAxesRenderer(axes, Plot, ctx, Theme.Default).Render();
        return ctx;
    }

    /// <summary>One tick mark: where it stands along the edge and how far it reaches outward past the edge.</summary>
    private readonly record struct Mark(double Y, double Outward);

    /// <summary>One straight line drawn into the context.</summary>
    private readonly record struct Segment(Point P1, Point P2)
    {
        public bool IsShortHorizontal => P1.Y == P2.Y && Math.Abs(P2.X - P1.X) < 20;
        public double Low => Math.Min(P1.X, P2.X);
        public double High => Math.Max(P1.X, P2.X);
    }

    /// <summary>One text drawn into the context, upright or turned.</summary>
    private readonly record struct Drawn(string Kind, string Text, Point At, Font Font, TextAlignment Alignment, double Rotation);

    /// <summary>A tick label as compared across slots: its text, its height and its distance from its slot's edge.</summary>
    private readonly record struct Label(string Text, double Y, double Distance, TextAlignment Alignment);

    /// <summary>A turned title: its kind (plain or rich text), its anchor's distance from its edge and its font size.</summary>
    private readonly record struct Title(string Kind, double Distance, double Size);

    private static IEnumerable<Segment> Segments(RecordingRenderContext ctx) =>
        ctx.OfKind("DrawLine").Select(c => (dynamic)c.Args!).Select(a => new Segment((Point)a.p1, (Point)a.p2));

    private static IEnumerable<Drawn> Texts(RecordingRenderContext ctx) =>
        ctx.Calls.Where(c => c.Kind is "DrawText" or "DrawRichText").Select(ToDrawn);

    private static Drawn ToDrawn(CallRecord call)
    {
        dynamic a = call.Args!;
        return new Drawn(call.Kind, call.Kind == "DrawText" ? (string)a.text : "", (Point)a.position, (Font)a.font,
            (TextAlignment)a.alignment, (double)a.rotation);
    }

    /// <summary>The short horizontal lines whose inner end stands half a spine inside <paramref name="edge"/> on
    /// <paramref name="side"/>: the marks of the slot whose edge that is.</summary>
    private static List<Mark> MarksAt(RecordingRenderContext ctx, double edge, AxisSide side, double spineHalf) =>
        [.. Segments(ctx)
            .Where(l => l.IsShortHorizontal)
            .Where(l => side == AxisSide.Right
                ? Math.Abs(l.Low - (edge - spineHalf)) < 1e-6
                : Math.Abs(l.High - (edge + spineHalf)) < 1e-6)
            .Select(l => new Mark(Math.Round(l.P1.Y, 6), Math.Round(side == AxisSide.Right ? l.High - edge : edge - l.Low, 4)))
            .OrderBy(m => m.Y).ThenBy(m => m.Outward)];

    /// <summary>The edge of every slot that drew marks on <paramref name="side"/>, nearest the plot first.</summary>
    private static double[] EdgesOn(RecordingRenderContext ctx, AxisSide side, double spineHalf) =>
        [.. Segments(ctx)
            .Where(l => l.IsShortHorizontal)
            .Select(l => side == AxisSide.Right ? l.Low + spineHalf : l.High - spineHalf)
            .Where(edge => side == AxisSide.Right ? edge >= PlotRight - 1e-6 : edge <= Plot.X + 1e-6)
            .Select(edge => Math.Round(edge, 6))
            .Distinct()
            .OrderBy(edge => side == AxisSide.Right ? edge : -edge)];

    /// <summary>The tick labels drawn on <paramref name="side"/> past <paramref name="edge"/> and short of
    /// <paramref name="nextEdge"/>, with how far each stands from <paramref name="edge"/> and how it is anchored.</summary>
    private static List<Label> LabelsAt(RecordingRenderContext ctx, double edge, AxisSide side, double nextEdge) =>
        [.. Texts(ctx)
            .Where(t => t.Kind == "DrawText" && t.Rotation != 90 && t.At.Y <= Plot.Y + Plot.Height + 5)
            .Where(t => side == AxisSide.Right ? t.At.X > edge && t.At.X < nextEdge : t.At.X < edge && t.At.X > nextEdge)
            .Select(t => new Label(t.Text, Math.Round(t.At.Y, 6), Math.Round(Math.Abs(t.At.X - edge), 4), t.Alignment))
            .OrderBy(t => t.Y)];

    /// <summary>The turned title drawn nearest past <paramref name="edge"/> on <paramref name="side"/>.</summary>
    private static Title TitleAt(RecordingRenderContext ctx, double edge, AxisSide side) =>
        Texts(ctx)
            .Where(t => t.Rotation == 90 && (side == AxisSide.Right ? t.At.X > edge : t.At.X < edge))
            .Select(t => new Title(t.Kind, Math.Abs(t.At.X - edge), t.Font.Size))
            .OrderBy(t => t.Distance)
            .First();

    private static YAxisSlot AddSlot(Axes axes, AxisSide side, double[] y)
    {
        var slot = axes.YAxes.StackOn(side).Add();
        slot.AddSeries(new LineSeries(X4[..y.Length], y));
        return slot;
    }

    /// <summary>The points of the slot's line: the last line drawn, after the primary's.</summary>
    private static Point[] SlotLine(RecordingRenderContext ctx) => (Point[])((dynamic)ctx.OfKind("DrawLines")[^1].Args!).points;

    private static double SpineHalf(Axes axes, AxisSide side) =>
        (side == AxisSide.Left ? axes.Spines.Left : axes.Spines.Right).LineWidth / 2;

    // ──────────────────────────────────────────────────────────────────────────
    // A title with math markup is drawn as rich text on every slot (D8)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(AxisSide.Right)]
    [InlineData(AxisSide.Left)]
    public void ASlotTitleWithMathText_DrawsRichText(AxisSide side)
    {
        var axes = new Axes();
        axes.Plot([0.0, 3], [0.0, 1]);
        var slot = AddSlot(axes, side, [1.0, 2, 3, 4]);
        slot.Axis.Label = @"$\alpha$ rate";

        var ctx = Render(axes);

        var title = Assert.Single(Texts(ctx), t => t.Kind == "DrawRichText" && t.Rotation == 90);
        var at = title.At;
        Assert.True(side == AxisSide.Right ? at.X > PlotRight : at.X < Plot.X, $"the rich title stands at x={at.X}");
        Assert.DoesNotContain(ctx.OfKind("DrawText"), c => (string)((dynamic)c.Args!).text == @"$\alpha$ rate");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Minor ticks on every slot
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(AxisSide.Right, 0)]
    [InlineData(AxisSide.Right, 1)]
    [InlineData(AxisSide.Left, 1)]
    public void EverySlot_DrawsItsMinorTicksWhenAsked(AxisSide side, int stackIndex)
    {
        var axes = new Axes();
        axes.Plot([0.0, 3], [0.0, 1]);
        while (axes.YAxes.StackOn(side).Count <= stackIndex) AddSlot(axes, side, [1.0, 2, 3, 4]);
        var slot = axes.YAxes.StackOn(side)[stackIndex];
        slot.Axis.Min = 0;
        slot.Axis.Max = 100;
        slot.Axis.MinorTicks = slot.Axis.MinorTicks with { Visible = true };

        var ctx = Render(axes);

        double half = SpineHalf(axes, side);
        double edge = EdgesOn(ctx, side, half)[stackIndex];
        var minor = MarksAt(ctx, edge, side, half).Where(m => Math.Abs(m.Outward - slot.Axis.MinorTicks.Length) < 1e-3).ToList();
        // The major ticks of 0..100 stand 20 apart (AutoLocator) and the minor ones five to a step between them.
        Assert.True(minor.Count >= 16, $"the slot drew {minor.Count} minor ticks");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // A slot honours its scale and its locator the way the primary does (D10)
    // ──────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(AxisSide.Right)]
    [InlineData(AxisSide.Left)]
    public void ASlotWithALogScale_DrawsItsTicksAndSeriesOnTheLogTransform(AxisSide side)
    {
        var axes = new Axes();
        axes.Plot([0.0, 3], [0.0, 1]);
        var slot = AddSlot(axes, side, [1.0, 10, 100, 1000]);
        slot.Axis.Scale = AxisScale.Log;
        slot.Axis.Min = 1;
        slot.Axis.Max = 1000;

        var ctx = Render(axes);

        // The series: one decade is one equal step in pixels.
        var line = SlotLine(ctx);
        Assert.Equal(line[0].Y - line[1].Y, line[1].Y - line[2].Y, 6);
        Assert.Equal(line[1].Y - line[2].Y, line[2].Y - line[3].Y, 6);
        // The ticks: the log locator's decades, written by the log formatter.
        double half = SpineHalf(axes, side);
        double edge = EdgesOn(ctx, side, half)[side == AxisSide.Left ? 1 : 0];
        var labels = LabelsAt(ctx, edge, side, side == AxisSide.Right ? double.MaxValue : double.MinValue).Select(l => l.Text).ToList();
        var formatter = new LogTickFormatter();
        Assert.Equal(new LogLocator().LocateInterval(1, 1000).Reverse().Select(formatter.Format), labels);
    }

    [Theory]
    [InlineData(AxisSide.Right)]
    [InlineData(AxisSide.Left)]
    public void ASlotWithASymLogScale_DrawsItsTicksAndSeriesOnTheSymLogTransform(AxisSide side)
    {
        var axes = new Axes();
        axes.Plot([0.0, 3], [0.0, 1]);
        var slot = AddSlot(axes, side, [0.0, 10, 100]);
        slot.Axis.Scale = AxisScale.SymLog;
        slot.Axis.SymLogLinThresh = 1;
        slot.Axis.Min = -100;
        slot.Axis.Max = 100;

        var ctx = Render(axes);

        var line = SlotLine(ctx);
        double F(double v) => AxisScale.SymLog.Forward(v, 1);
        Assert.Equal((F(100) - F(10)) / (F(10) - F(0)), (line[1].Y - line[2].Y) / (line[0].Y - line[1].Y), 6);
        double half = SpineHalf(axes, side);
        double edge = EdgesOn(ctx, side, half)[side == AxisSide.Left ? 1 : 0];
        var labels = LabelsAt(ctx, edge, side, side == AxisSide.Right ? double.MaxValue : double.MinValue).Select(l => l.Text).ToList();
        var ticks = new SymlogLocator(1).LocateInterval(-100, 100);
        var format = AxesRenderer.BuildUniformTickFormatter(ticks);
        Assert.Equal(ticks.Reverse().Select(format), labels);
    }

    [Theory]
    [InlineData(AxisSide.Right)]
    [InlineData(AxisSide.Left)]
    public void ASlotWithALocator_DrawsTheLocatorsTicks(AxisSide side)
    {
        var axes = new Axes();
        axes.Plot([0.0, 3], [0.0, 1]);
        var slot = AddSlot(axes, side, [1.0, 2, 3, 4]);
        slot.Axis.TickLocator = new MultipleLocator(25);
        slot.Axis.Min = 0;
        slot.Axis.Max = 100;

        var ctx = Render(axes);

        double half = SpineHalf(axes, side);
        double edge = EdgesOn(ctx, side, half)[side == AxisSide.Left ? 1 : 0];
        var labels = LabelsAt(ctx, edge, side, side == AxisSide.Right ? double.MaxValue : double.MinValue).Select(l => l.Text).ToList();
        Assert.Equal(["100", "75", "50", "25", "0"], labels);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // One part: a right slot is the mirror of a left one; the primary draws like any slot
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>A slot whose ticks, labels and title can be compared across slots: limits, a locator, minor ticks, a
    /// title with math markup and turned tick labels.</summary>
    private static void Dress(Axis axis)
    {
        axis.Min = 0;
        axis.Max = 100;
        axis.TickLocator = new MultipleLocator(20);
        axis.MinorTicks = axis.MinorTicks with { Visible = true };
        axis.Label = @"Load $\beta$";
        axis.MajorTicks = axis.MajorTicks with { LabelRotation = 15 };
    }

    [Fact]
    public void ARightSlot_AndItsLeftMirror_DrawTheSameGeometry_ModuloTheSideTerms()
    {
        var axes = new Axes();
        axes.Plot([0.0, 3], [0.0, 1]);
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Visible = false };
        var right = AddSlot(axes, AxisSide.Right, [10.0, 20, 30, 40]);
        var left = AddSlot(axes, AxisSide.Left, [10.0, 20, 30, 40]);
        right.Axis.Min = left.Axis.Min = 0;
        right.Axis.Max = left.Axis.Max = 100;
        right.Axis.Label = left.Axis.Label = "Rate";

        var ctx = Render(axes);

        double rightEdge = EdgesOn(ctx, AxisSide.Right, SpineHalf(axes, AxisSide.Right))[0];
        double leftEdge = EdgesOn(ctx, AxisSide.Left, SpineHalf(axes, AxisSide.Left))[0];
        Assert.Equal(MarksAt(ctx, rightEdge, AxisSide.Right, SpineHalf(axes, AxisSide.Right)),
                     MarksAt(ctx, leftEdge, AxisSide.Left, SpineHalf(axes, AxisSide.Left)));
        var rightLabels = LabelsAt(ctx, rightEdge, AxisSide.Right, double.MaxValue);
        var leftLabels = LabelsAt(ctx, leftEdge, AxisSide.Left, double.MinValue);
        Assert.Equal(rightLabels.Select(l => l with { Alignment = default }), leftLabels.Select(l => l with { Alignment = default }));
        Assert.All(rightLabels, l => Assert.Equal(TextAlignment.Left, l.Alignment));
        Assert.All(leftLabels, l => Assert.Equal(TextAlignment.Right, l.Alignment));
        // The title's ink that faces the tick labels differs by side (its ascent on the right, its descent on the left);
        // past that ink both stand the same distance from their edge.
        var rightTitle = TitleAt(ctx, rightEdge + 1, AxisSide.Right);
        var leftTitle = TitleAt(ctx, leftEdge - 1, AxisSide.Left);
        Assert.Equal(rightTitle.Distance - FacingInk.Ascent * rightTitle.Size, leftTitle.Distance - FacingInk.Descent * leftTitle.Size, 4);
    }

    [Fact]
    public void ThePrimaryYAxis_DrawsThroughTheSamePartAsAnySlot()
    {
        var axes = new Axes();
        axes.Plot(X4, [10.0, 20, 30, 40]);
        var outward = AddSlot(axes, AxisSide.Left, [10.0, 20, 30, 40]);
        Dress(axes.YAxis);
        Dress(outward.Axis);

        var ctx = Render(axes);

        double half = SpineHalf(axes, AxisSide.Left);
        var edges = EdgesOn(ctx, AxisSide.Left, half);
        Assert.Equal(2, edges.Length);
        Assert.Equal(Plot.X, edges[0], 6);
        // The same marks (majors and minors), the same labels at the same distance, and the same title, drawn as rich
        // text at the same distance from its own edge.
        Assert.Equal(MarksAt(ctx, edges[0], AxisSide.Left, half), MarksAt(ctx, edges[1], AxisSide.Left, half));
        Assert.Equal(LabelsAt(ctx, edges[0], AxisSide.Left, edges[1]), LabelsAt(ctx, edges[1], AxisSide.Left, double.MinValue));
        var titles = Texts(ctx).Where(t => t.Rotation == 90).OrderByDescending(t => t.At.X).ToList();
        Assert.Equal(2, titles.Count);
        Assert.All(titles, t => Assert.Equal("DrawRichText", t.Kind));
        Assert.Equal(edges[0] - titles[0].At.X, edges[1] - titles[1].At.X, 4);
    }

    /// <summary>A renderer of another kind than the Cartesian one draws the primary Y title by the same rule: at the fixed
    /// offset past the left edge when it drew no tick label, as rich text when it carries math markup.</summary>
    [Theory]
    [InlineData("Load", "DrawText")]
    [InlineData(@"$\gamma$ load", "DrawRichText")]
    public void ARendererOfAnotherKind_DrawsThePrimaryYTitleByTheSameRule(string title, string kind)
    {
        var axes = new Axes();
        axes.YAxis.Label = title;
        var ctx = new RecordingRenderContext();

        new TitlesOnlyRenderer(axes, Plot, ctx).Render();

        var drawn = Assert.Single(Texts(ctx), t => t.Rotation == 90);
        Assert.Equal(kind, drawn.Kind);
        Assert.Equal(Plot.X - YAxisTitleExtensions.YAxisLabelFallback, drawn.At.X, 6);
        Assert.Equal(Plot.Y + Plot.Height / 2, drawn.At.Y, 6);
    }

    /// <summary>A renderer that draws only the axis titles, the way a renderer outside the library may.</summary>
    private sealed class TitlesOnlyRenderer(Axes axes, Rect plotArea, IRenderContext ctx)
        : AxesRenderer(axes, plotArea, ctx, Theme.Default)
    {
        public override void Render() => RenderAxisLabels();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // A slot's range and transform are worked out once per render
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>A series that draws nothing and counts how often its data range is asked for.</summary>
    private sealed class CountingSeries : ChartSeries
    {
        public int RangeCalls { get; private set; }

        public override void Accept(ISeriesVisitor visitor, RenderArea area) { }

        public override DataRangeContribution ComputeDataRange(IAxesContext context)
        {
            RangeCalls++;
            return new(0, 3, 0, 10);
        }

        public override SeriesDto ToSeriesDto() => new() { Type = "counting" };
    }

    [Fact]
    public void APlacedYSlot_IsComputedOncePerSlotPerRender()
    {
        var axes = new Axes();
        axes.Plot([0.0, 3], [0.0, 1]);
        var right0 = axes.YAxes.Right.Add().AddSeries(new CountingSeries());
        var right1 = axes.YAxes.Right.Add().AddSeries(new CountingSeries());
        var left1 = axes.YAxes.Left.Add().AddSeries(new CountingSeries());

        Render(axes);

        Assert.Equal([1, 1, 1], new[] { right0.RangeCalls, right1.RangeCalls, left1.RangeCalls });
    }
}
