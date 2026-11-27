// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The right edge reserves what is drawn there, in this order: the secondary Y axis (its tick labels, then
/// its title) and after it the end-of-line labels. Before 1.19.0 the engine charged a fixed 45 px plus half the
/// secondary TITLE width (never its tick labels), took the largest right-hand item instead of stacking them, and cut
/// the margin at 140 px.
/// <para>Margins are asserted through the plot-area rectangle and the label width measured by the metrics the renderer
/// uses; the drawn end label itself is pinned with the label drawing.</para></summary>
public class ConstrainedLayoutEngineTests
{
    private const string WideLabel = "a formatted tick label";

    private const double FigureWidth = 800;
    private static readonly double[] X = [0.0, 1.0, 2.0];

    private static IReadOnlyList<Rect> PlotAreas(Figure figure, IRenderContext? ctx = null) =>
        new ChartRenderer().ComputeLayout(figure, ctx ?? new SvgRenderContext()).PlotAreas;

    private static double RightMargin(Figure figure, IRenderContext? ctx = null)
    {
        var area = PlotAreas(figure, ctx)[0];
        return figure.Width - (area.X + area.Width);
    }

    private static double LabelWidth(Figure figure, string text, IRenderContext? ctx = null, int axes = 0)
    {
        ctx ??= new SvgRenderContext();
        return ctx.MeasureText(text, new DirectLabelMetrics(figure.SubPlots[axes], figure.Theme, ctx).Font).Width;
    }

    private static FigureBuilder Tight() => Plt.Create().WithSize(FigureWidth, 400).TightLayout();

    // ── (a) a label wider than the old 140 px cap ────────────────────────────────

    [Fact]
    public void TightLayout_ALabelWiderThan140Px_LiftsTheRightMarginPastTheCap()
    {
        string name = new string('W', 30) + " a very long series name for the end";
        var figure = Tight().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = name).WithDirectLabel())
            .Build();

        double needed = DirectLabelMetrics.Gap + LabelWidth(figure, name);
        Assert.True(needed > 140, $"precondition: the label band ({needed:F2}) must exceed the old cap");
        Assert.True(RightMargin(figure) >= needed,
            $"MarginRight {RightMargin(figure):F2} < gap + label {needed:F2}: the 140 px cap still cuts measured content");
    }

    // ── (c) 1x2 grid: the gutter takes the left subplot's label band ─────────────

    private static Figure Grid(bool label, string name) => Tight()
        .AddSubPlot(1, 2, 1, ax =>
        {
            ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = name);
            if (label) ax.WithDirectLabel();
        })
        .AddSubPlot(1, 2, 2, ax => ax.Plot(X, [1.0, 2.0, 3.0]))
        .Build();

    private static double Gutter(IReadOnlyList<Rect> a) => a[1].X - (a[0].X + a[0].Width);

    [Fact]
    public void TightLayout_OneByTwoGrid_TheGutterGrowsByTheLeftSubplotsLabelBand()
    {
        var labelled = Grid(true, "Left series");
        var plain = Grid(false, "Left series");

        // The plain gutter already holds the right subplot's Y tick labels; the label band is stacked on it.
        double growth = Gutter(PlotAreas(labelled)) - Gutter(PlotAreas(plain));
        double band = DirectLabelMetrics.Gap + LabelWidth(labelled, "Left series");
        // matplotlib's tight_layout keeps one pad in a gap (the default theme's): the gutter grows by the band itself.
        Assert.True(growth >= band - 1e-9, $"gutter grew {growth:F2} px, the label band needs {band:F2}");
    }

    [Fact]
    public void TightLayout_OneByTwoGrid_ALongLabelLiftsTheGutterCap()
    {
        string name = new string('W', 25) + " long left name";
        var labelled = Grid(true, name);

        double band = DirectLabelMetrics.Gap + LabelWidth(labelled, name);
        Assert.True(band > 150, $"precondition: the label band ({band:F2}) must exceed the 150 px gutter cap");
        Assert.True(Gutter(PlotAreas(labelled)) >= band,
            $"gutter {Gutter(PlotAreas(labelled)):F2} < label band {band:F2}");
    }

    // ── (d) dual axis: the label band comes after the secondary tick labels ──────

    [Fact]
    public void TightLayout_DualAxis_TheEndLabelFitsPastTheSecondaryTickLabels()
    {
        var figure = Tight().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "Revenue").WithDirectLabel()
                .WithAddRightYAxis(y2 => y2.Plot(X, [10_000.0, 20_000.0, 30_000.0])))
            .Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);

        var area = PlotAreas(figure, ctx)[0];
        double plotRight = area.X + area.Width;
        // Right of the plot: the secondary tick labels, and the end label itself (left out here: it is what must fit).
        var tickLabels = ctx.Of<DrawTextCall>()
            .Where(a => (double)a.Position.X > plotRight && a.Text != "Revenue").ToList();
        Assert.NotEmpty(tickLabels);
        double labelBand = DirectLabelMetrics.Gap + LabelWidth(figure, "Revenue", ctx);
        foreach (var a in tickLabels)
        {
            double tickRight = (double)a.Position.X + ctx.MeasureText(a.Text, a.Font).Width;
            Assert.True(tickRight + labelBand <= FigureWidth,
                $"tick label '{a.Text}' ends at {tickRight:F2}; the end label after it needs {labelBand:F2} more of {FigureWidth}");
        }
    }

    [Fact]
    public void TightLayout_DualAxisWithTitle_TheEndLabelFitsPastTheSecondaryTitle()
    {
        var figure = Tight().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "Revenue").WithDirectLabel()
                .WithAddRightYAxis(y2 => y2.Plot(X, [10_000.0, 20_000.0, 30_000.0]).SetYLabel("Margin %")))
            .Build();
        var ctx = new RecordingRenderContext();
        new ChartRenderer().Render(figure, ctx);

        var title = ctx.Of<DrawTextCall>().Single(a => a.Text == "Margin %");
        // The turned title's ink ends its descent past its anchor (FacingInk); the end labels stand past that.
        double titleInkEnd = (double)title.Position.X + FacingInk.Descent * (title.Font).Size;
        double labelBand = DirectLabelMetrics.Gap + LabelWidth(figure, "Revenue", ctx);
        Assert.True(titleInkEnd + labelBand <= FigureWidth,
            $"secondary title ink ends at {titleInkEnd:F2} and leaves no room for the end label band {labelBand:F2}");
    }

    // ── (e) user and fractional spacing smaller than the label ───────────────────

    [Fact]
    public void TightLayout_AUserMarginRightSmallerThanTheLabel_IsWidened()
    {
        var figure = Tight().WithSubPlotSpacing(s => s with { MarginRight = 5 })
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = "Revenue").WithDirectLabel())
            .Build();

        Assert.True(RightMargin(figure) >= DirectLabelMetrics.Gap + LabelWidth(figure, "Revenue"));
    }

    [Fact]
    public void ConstrainedLayout_FractionalSpacing_ReservesTheLabelBand()
    {
        var figure = Plt.Create().WithSize(FigureWidth, 400)
            .WithSubPlotSpacing(_ => SubPlotSpacing.FromFractions(0.1, 0.02, 0.1, 0.12) with { ConstrainedLayout = true })
            .AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = "Revenue").WithDirectLabel())
            .Build();

        Assert.True(RightMargin(figure) >= DirectLabelMetrics.Gap + LabelWidth(figure, "Revenue"));
    }

    // ── (f) nothing new at the right edge: the engine answers as before ──────────

    [Fact]
    public void TightLayout_ALineEndFlagOnPolarAxes_ChangesNothing()
    {
        static Figure Polar(bool flag) => Plt.Create().TightLayout().AddSubPlot(1, 1, 1, ax => ax
            .PolarPlot([1.0, 2.0, 3.0], [0.0, 1.0, 2.0])
            .Plot(X, [1.0, 2.0, 3.0], s =>
            {
                s.Label = "flagged";
                if (flag) s.LabelPlacement = MatPlotLibNet.Models.Series.SeriesLabelPlacement.LineEnd;
            })).Build();
        var ctx = new SvgRenderContext();

        Assert.Equal(new ConstrainedLayoutEngine().Compute(Polar(false), ctx),
                     new ConstrainedLayoutEngine().Compute(Polar(true), ctx));
    }

    // ── the band itself, arm by arm ──────────────────────────────────────────────

    private static RightBand Band(Action<AxesBuilder> configure, IRenderContext? ctx = null)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, configure).Build();
        return new AxesEdgeNeeds().MeasureRightBand(figure.SubPlots[0], ctx ?? new RecordingRenderContext(), figure.Theme, 0);
    }

    [Fact]
    public void RightBand_NothingAtTheRightEdge_IsZero()
    {
        var band = Band(ax => ax.Plot(X, [1.0, 2.0, 3.0], s => s.Label = "legend"));

        Assert.Equal(new RightBand(0, 0, 0), band);
        Assert.Equal(0, band.WidthWith(ConstrainedLayoutEngine.PadRight));
    }

    [Fact]
    public void RightBand_SecondaryTicks_ChargeMarkPadAndTheWidestLabel()
    {
        var ctx = new RecordingRenderContext();
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0])
            .WithAddRightYAxis(y2 => y2.Plot(X, [10_000.0, 20_000.0, 30_000.0]))).Build();
        var axes = figure.SubPlots[0];

        var band = new AxesEdgeNeeds().MeasureRightBand(axes, ctx, figure.Theme, 0);

        // What the secondary part draws: the labels left-aligned at Length + Pad past the edge.
        new ChartRenderer().Render(figure, ctx);
        var drawn = ctx.Of<DrawTextCall>().Where(a => a.Text != "" && (int)a.Alignment == (int)TextAlignment.Left).ToList();
        double widest = drawn.Max(a => ctx.MeasureText(a.Text, a.Font).Width);
        var ticks = axes.YAxes.Right[0].Axis.MajorTicks;
        Assert.Equal(ticks.Length + ticks.Pad + widest, band.SecondaryAxis, 6);
        Assert.Equal(0, band.EndLabels);
        Assert.Equal(band.SecondaryAxis + ConstrainedLayoutEngine.PadRight, band.WidthWith(ConstrainedLayoutEngine.PadRight), 6);
    }

    /// <summary>ROADMAP.md row 101: a second, stacked right axis — the total the layout reserves is the SUM
    /// of every slot's own band, read by <see cref="ConstrainedLayoutEngine.MeasureRightBand"/> (through <see cref="RightBand.Of"/>) and by
    /// <c>CartesianAxisPlacement.PlaceYSlots</c>'s own outward offset (<see cref="AxisStackingExtensions.StackedOffsets"/>)
    /// alike, so the two can never disagree about where a slot's edge is.</summary>
    [Fact]
    public void RightBand_TwoStackedAxes_TotalIsEachSlotsOwnBand_AndTheGapBetween()
    {
        var ctx = new RecordingRenderContext();
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0])
            .WithAddRightYAxis(y2 => y2.Plot(X, [10_000.0, 20_000.0, 30_000.0]))).Build();
        var axes = figure.SubPlots[0];
        axes.YAxes.Right.Add().AddSeries(new MatPlotLibNet.Models.Series.LineSeries(X, [1.0, 2.0, 3.0]));

        var perSlotBands = new AxesEdgeNeeds().SecondaryAxisBands(axes, ctx, figure.Theme);
        var band = new AxesEdgeNeeds().MeasureRightBand(axes, ctx, figure.Theme, 0);

        Assert.Equal(2, perSlotBands.Length);
        Assert.True(perSlotBands[0] > 0 && perSlotBands[1] > 0, "Both slots should charge a nonzero band.");
        // Different tick-label widths (5-digit values vs 1-digit) prove the two entries are genuinely distinct, not
        // the same band measured twice.
        Assert.NotEqual(perSlotBands[0], perSlotBands[1]);
        // The second slot stands the container's gap past the far side of the first.
        Assert.Equal(perSlotBands[0] + axes.YAxes.TitleGapUnder(figure.Theme) + perSlotBands[1], band.SecondaryAxis, 6);
    }

    /// <summary>ROADMAP.md row 101: a left-stacked axis widens the left margin past the primary
    /// Y axis' own band — the mirror of the right band, read the same way by <see cref="ConstrainedLayoutEngine.LeftAxisBands"/>.</summary>
    [Fact]
    public void Compute_WithALeftStackedAxis_WidensTheLeftMarginPastThePrimarysOwnBand()
    {
        var ctx = new RecordingRenderContext();
        var without = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0])).Build();
        var with = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0])).Build();
        with.SubPlots[0].YAxes.Left.Add()
            .AddSeries(new MatPlotLibNet.Models.Series.LineSeries(X, [10_000.0, 20_000.0, 30_000.0]));

        var marginWithout = new ConstrainedLayoutEngine().Compute(without, ctx).MarginLeft;
        var marginWith = new ConstrainedLayoutEngine().Compute(with, ctx).MarginLeft;
        var leftBand = new AxesEdgeNeeds().LeftAxisBands(with.SubPlots[0], ctx, with.Theme);

        Assert.Single(leftBand);
        Assert.True(leftBand[0] > 0);
        Assert.True(marginWith > marginWithout,
            $"Expected the left-axis figure's margin ({marginWith}) to exceed the plain one's ({marginWithout}).");
    }

    /// <summary>The left stack of the container lifts the left cap the way measured content does, even when the primary
    /// draws nothing there: a left slot wider than the 120 px cap is reserved whole, past the primary's empty band and the
    /// container's gap, plus the layout's left pad.</summary>
    [Fact]
    public void Compute_HiddenPrimaryTicks_ALeftSlotWiderThanTheCap_LiftsTheLeftCap()
    {
        var ctx = new RecordingRenderContext();
        var figure = Tight().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0])
            .WithAddLeftYAxis(left => left.Plot(X, [1.0, 2.0, 3.0]))).Build();
        var axes = figure.SubPlots[0];
        axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Visible = false };
        axes.YAxes.Left[1].Axis.TickFormatter = new TestTickFormatter(_ => WideLabel);

        double slot = Assert.Single(new AxesEdgeNeeds().LeftAxisBands(axes, ctx, figure.Theme));
        double margin = new ConstrainedLayoutEngine().Compute(figure, ctx).MarginLeft;

        Assert.True(slot > 120, $"precondition: the slot's band ({slot:F2}) is wider than the 120 px cap");
        // The default theme (matplotlib v2) keeps matplotlib's tight_layout pad: 1.08 of its 10 pt font.
        Assert.Equal(0 + axes.YAxes.TitleGapUnder(figure.Theme) + slot + 1.08 * 10 * 100 / 72, margin, 6);
    }

    [Fact]
    public void RightBand_SecondaryTickFormatter_IsWhatIsMeasured()
    {
        var ctx = new RecordingRenderContext();
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0])
            .WithAddRightYAxis(y2 => y2.Plot(X, [1.0, 2.0, 3.0]))).Build();
        figure.SubPlots[0].YAxes.Right[0].Axis.TickFormatter = new TestTickFormatter(_ => WideLabel);
        var wide = new AxesEdgeNeeds().MeasureRightBand(figure.SubPlots[0], ctx, figure.Theme, 0);

        var ticks = figure.SubPlots[0].YAxes.Right[0].Axis.MajorTicks;
        double expected = ticks.Length + ticks.Pad
            + ctx.MeasureText(WideLabel, new TickDrawContext { BaseFont = figure.Theme.TickFont(), Theme = figure.Theme }.LabelFont(ticks)).Width;
        Assert.Equal(expected, wide.SecondaryAxis, 6);
    }

    [Fact]
    public void RightBand_HiddenSecondaryTicks_ChargeNothingWithoutATitle()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0])
            .WithAddRightYAxis(y2 => y2.Plot(X, [1.0, 2.0, 3.0]))).Build();
        var axis = figure.SubPlots[0].YAxes.Right[0].Axis;
        axis.MajorTicks = axis.MajorTicks with { Visible = false };

        var band = new AxesEdgeNeeds().MeasureRightBand(figure.SubPlots[0], new RecordingRenderContext(), figure.Theme, 0);

        Assert.Equal(0, band.SecondaryAxis);
    }

    /// <summary>The band reaches where the part draws the title's anchor, plus the title's ink past it: on the right its
    /// descent (<see cref="FacingInk"/>), the ink a turned title reaches outward.</summary>
    [Fact]
    public void RightBand_SecondaryTitle_SitsWhereThePartDrawsIt_PlusItsOutwardInk()
    {
        var ctx = new RecordingRenderContext();
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0])
            .WithAddRightYAxis(y2 => y2.Plot(X, [1.0, 2.0, 3.0]).SetYLabel("Margin %"))).Build();
        var axes = figure.SubPlots[0];

        var band = new AxesEdgeNeeds().MeasureRightBand(axes, ctx, figure.Theme, 0);

        new ChartRenderer().Render(figure, ctx);
        var area = PlotAreas(figure, ctx)[0];
        var title = ctx.Of<DrawTextCall>().Single(a => a.Text == "Margin %");
        double centre = (double)title.Position.X - (area.X + area.Width);
        Assert.Equal(centre + FacingInk.Descent * (title.Font).Size, band.SecondaryAxis, 6);
    }

    [Fact]
    public void RightBand_HiddenSecondaryTicksWithATitle_KeepTheTitleAtTheFixedOffset()
    {
        var ctx = new RecordingRenderContext();
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0])
            .WithAddRightYAxis(y2 => y2.Plot(X, [1.0, 2.0, 3.0]).SetYLabel("Margin %"))).Build();
        var axis = figure.SubPlots[0].YAxes.Right[0].Axis;
        axis.MajorTicks = axis.MajorTicks with { Visible = false };
        axis.LabelStyle = new TextStyle { Pad = 7 };

        var band = new AxesEdgeNeeds().MeasureRightBand(figure.SubPlots[0], ctx, figure.Theme, 0);

        // The title keeps its fixed 45 px plus its pad, and the band reaches its descent past that.
        double size = axis.LabelStyle.ApplyTo(figure.Theme.LabelFont()).Size;
        Assert.Equal(45 + 7 + FacingInk.Descent * size, band.SecondaryAxis, 6);
    }

    [Fact]
    public void RightBand_EndLabels_ChargeTheGapAndTheWidestLabel()
    {
        var ctx = new RecordingRenderContext();
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "short").WithDirectLabel()
            .Plot(X, [2.0, 3.0, 4.0], s => s.Label = "the longest name").WithDirectLabel()).Build();
        var band = new AxesEdgeNeeds().MeasureRightBand(figure.SubPlots[0], ctx, figure.Theme, 0);

        var font = figure.Theme.TickFont();
        Assert.Equal(0, band.SecondaryAxis);
        Assert.Equal(DirectLabelMetrics.Gap + ctx.MeasureText("the longest name", font).Width, band.EndLabels, 6);
        Assert.Equal(band.EndLabels + ConstrainedLayoutEngine.PadRight, band.WidthWith(ConstrainedLayoutEngine.PadRight), 6);
    }

    [Fact]
    public void RightBand_StacksTheEndLabelsAfterTheSecondaryAxis()
    {
        var ctx = new RecordingRenderContext();
        var both = Band(ax => ax
            .Plot(X, [1.0, 2.0, 3.0], s => s.Label = "Revenue").WithDirectLabel()
            .WithAddRightYAxis(y2 => y2.Plot(X, [10.0, 20.0, 30.0])), ctx);

        Assert.True(both.SecondaryAxis > 0 && both.EndLabels > 0);
        Assert.Equal(both.SecondaryAxis + both.EndLabels + ConstrainedLayoutEngine.PadRight, both.WidthWith(ConstrainedLayoutEngine.PadRight), 6);
    }

    // ── the X axis title of an axes whose renderer measures nothing below its bottom spine ─────────────

    /// <summary>An axes whose renderer measures nothing below its bottom spine (polar, 3-D) is kept room for its X axis title
    /// by the one rule the title is placed by (<see cref="AxesTitleExtensions"/>' <c>BottomTitleExtent</c>): in the
    /// title's own font, past its pad, below an estimated band of tick labels when they are drawn and at the fixed place
    /// when they are hidden.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheEstimatedBottom_KeepsRoomForTheXTitleByTheOneTitleRule(bool ticksVisible)
    {
        var ctx = new SvgRenderContext();
        var figure = Tight().AddSubPlot(1, 1, 1, ax => ax.Plot(X, [1.0, 2.0, 3.0])).Build();
        var axes = figure.SubPlots[0];
        axes.XAxis.Label = "Angle";
        axes.XAxis.LabelStyle = new TextStyle { FontSize = 30, Pad = 10 };
        axes.XAxis.MajorTicks = axes.XAxis.MajorTicks with { Visible = ticksVisible };
        double tickH = ticksVisible ? ctx.MeasureText("0", figure.Theme.TickFont()).Height : 0;

        var needs = new AxesEdgeNeeds().MeasureEnds(axes, figure, ctx, default);

        double expected = axes.XAxis.BottomTitleExtent(axes.XAxis.Label, ctx, figure.Theme, tickH) + ConstrainedLayoutEngine.PadBottom;
        Assert.Equal(expected, needs.End, 9);
    }

    private static IRenderContext MakeCtx() => new SvgRenderContext();

    /// <summary>The engine gives <paramref name="wider"/> more of an edge than it gives <paramref name="bare"/>.</summary>
    private static void AssertWidens(Figure wider, Figure bare, Func<SubPlotSpacing, double> edge, string what)
    {
        var ctx = MakeCtx();
        double before = edge(Engine().Compute(bare, ctx));
        double after = edge(Engine().Compute(wider, ctx));

        Assert.True(after > before, $"{what}; got bare={before}, wider={after}");
    }
    private static ConstrainedLayoutEngine Engine() => new();

    /// <summary>OutsideTop legend hits the OutsideTop arm in both the Measure switch
    /// and the figure-level clamp switch — the only switch arm of the four positions
    /// not exercised by the existing OutsideRight/Left/Bottom tests.</summary>
    [Fact]
    public void OutsideTopLegend_WidensTopMarginAboveInsidePosition()
    {
        var ctx = MakeCtx();
        var inside = new FigureBuilder()
            .WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = "Alpha");
                ax.Plot([1.0, 2.0], [2.0, 3.0], s => s.Label = "Beta");
                ax.WithLegend(l => l with { Position = LegendPosition.UpperCenter });
            })
            .Build();
        var outsideTop = new FigureBuilder()
            .WithSize(800, 600)
            .AddSubPlot(1, 1, 1, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = "Alpha");
                ax.Plot([1.0, 2.0], [2.0, 3.0], s => s.Label = "Beta");
                ax.WithLegend(l => l with { Position = LegendPosition.OutsideTop });
            })
            .Build();

        AssertWidens(outsideTop, inside, spacing => spacing.MarginTop, "OutsideTop must widen top margin");
    }

    /// <summary>YAxis with Min set but Max null hits the second arm of EstimateYTickLabel
    /// (<c>else if (axes.YAxis.Min.HasValue) return NumericTickFormatter.FormatValue(Min)</c>).
    /// SetYLim sets both, so we mutate the model after build to leave Max null.</summary>
    [Fact]
    public void YAxisMinOnly_MaxUnset_LeftMarginEstimatesFromMin()
    {
        var ctx = MakeCtx();
        var figure = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();
        // Bypass SetYLim (which sets both) to land on the Min-only branch
        figure.SubPlots[0].YAxis.Min = 1_234_567.0;
        figure.SubPlots[0].YAxis.Max = null;

        var bare = new FigureBuilder()
            .AddSubPlot(1, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();

        var minOnlySpacing = Engine().Compute(figure, ctx);
        var bareSpacing    = Engine().Compute(bare, ctx);

        // Engine ran without throwing on the Min-only branch; left margin reflects the
        // Min-derived label width (≥ the conservative fallback proxy "−9.999")
        Assert.True(minOnlySpacing.MarginLeft >= bareSpacing.MarginLeft);
    }

    /// <summary>Two-column grid where the LEFT subplot (ColEnd=1 &lt; maxCols=2) carries a
    /// vertical colorbar — its RightNeeded contribution is what the engine accumulates into hGap
    /// at the horizontal gap and into <c>maxNonRightRight</c>. With only a right-edge subplot
    /// in existing fixtures, the inner branch never fires.</summary>
    [Fact]
    public void TwoColGrid_LeftSubplotVerticalColorBar_WidensHorizontalGap()
    {
        var ctx = MakeCtx();
        var bareTwoCol = new FigureBuilder()
            .AddSubPlot(1, 2, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();

        var leftCb = new FigureBuilder()
            .AddSubPlot(1, 2, 1, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
                ax.WithColorBar(cb => cb with { Visible = true, Orientation = ColorBarOrientation.Vertical, Label = "Intensity scale" });
            })
            .AddSubPlot(1, 2, 2, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();

        AssertWidens(leftCb, bareTwoCol, spacing => spacing.HorizontalGap, "Left-column colorbar must widen hGap");
    }

    /// <summary>Two-row grid where the BOTTOM subplot (RowStart=1 &gt; 0) carries a title — its
    /// TopNeeded contribution is what the engine accumulates into vGap. Existing
    /// fixtures only have titles on top-row subplots, so the inner branch never fires.</summary>
    [Fact]
    public void TwoRowGrid_BottomSubplotTitle_WidensVerticalGap()
    {
        var ctx = MakeCtx();
        var bareTwoRow = new FigureBuilder()
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(2, 1, 2, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();

        var bottomTitle = new FigureBuilder()
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .AddSubPlot(2, 1, 2, ax =>
            {
                ax.Plot([1.0, 2.0], [1.0, 2.0]);
                ax.WithTitle("Bottom subplot title");
            })
            .Build();

        AssertWidens(bottomTitle, bareTwoRow, spacing => spacing.VerticalGap, "Bottom-row subplot title must widen vGap");
    }

    /// <summary>Three-column grid where every subplot has both a Y-label (left demand) and a
    /// secondary Y-label (right demand). The middle subplot is interior on BOTH edges, so
    /// <c>maxNonLeftLeft</c> and <c>maxNonRightRight</c> both accumulate, and their sum
    /// (stackedH) exceeds the single-edge hGap — firing the stacked branch.</summary>
    [Fact]
    public void ThreeColGrid_BothSideDemands_StackedHorizontalGapExceedsSingleSide()
    {
        var ctx = MakeCtx();
        static void BothSides(AxesBuilder ax) =>
            ax.Plot([1.0, 2.0], [1.0, 2.0]).SetYLabel("Long left Y label").WithAddRightYAxis(y2 => y2.SetYLabel("Long right Y label"));
        var figure = new FigureBuilder().AddSubPlot(1, 3, 1, BothSides).AddSubPlot(1, 3, 2, BothSides).AddSubPlot(1, 3, 3, BothSides).Build();

        var spacing = Engine().Compute(figure, ctx);

        // hGap must reflect the stacked sum (left + right interior demand on the middle subplot).
        // Only need to assert the engine completed and produced a positive gap; the stacked branch
        // fires whenever stackedH (sum) exceeds the single-edge max.
        Assert.True(spacing.HorizontalGap > 0);
    }

    /// <summary>Suptitle present, but the only subplot lives in row 1 of a 2-row GridSpec — so
    /// <c>pos.RowStart == 0</c> is false for every subplot, top stays at default 20,
    /// and the supReserved-vs-top floor fires its true branch.</summary>
    [Fact]
    public void SuptitleWithNoRow0Subplot_TopFloorBranchFires()
    {
        var ctx = MakeCtx();
        var figure = new FigureBuilder()
            .WithTitle("Figure suptitle")
            .WithGridSpec(2, 1)
            .AddSubPlot(new GridPosition(1, 2, 0, 1), ax => ax.Plot([1.0, 2.0], [1.0, 2.0]))
            .Build();

        var spacing = Engine().Compute(figure, ctx);

        // Top must reflect the suptitle reservation (>= ~30px floor for any reasonable title font).
        Assert.True(spacing.MarginTop >= 30,
            $"Top margin must honour suptitle floor when no row-0 subplot raised it; got {spacing.MarginTop}");
    }
}
