// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.LegendRendering;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The layout of a draw measures what each axes draws past its spines once: the sides before the plots have a width,
/// the ends at the width each plot is drawn at. The pass that settles the room the X tick labels need beside their plot measures
/// the ends at the plots it hands on, so those bands go with the plots and the top and bottom are not measured again; only a pass
/// that moved the plots leaves the ends to be measured at the plots it settled on. Each pass measures only the edges it
/// reads.</summary>
[Collection("ChartServicesGlobalState")]
public sealed class LayoutMeasuresOnceTests
{
    /// <summary>A context that counts how many times each text is measured and measures it as the vector context does.</summary>
    private sealed class CountingContext : SilentRenderContext
    {
        private readonly SvgRenderContext _inner = new();
        public Dictionary<string, int> Measured { get; } = [];

        public int Of(string text) => Measured.GetValueOrDefault(text);

        public override Size MeasureText(string text, Font font)
        {
            Measured[text] = Of(text) + 1;
            return _inner.MeasureText(text, font);
        }
    }

    private const string XTitle = "the x title";
    private const string AxesTitle = "the axes title";

    private static readonly double[] Ramp = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

    private static Figure Line(bool tight, Action<AxesBuilder>? configure = null)
    {
        var builder = Plt.Create().WithSize(500, 350);
        if (tight) builder = builder.TightLayout();
        return builder.AddSubPlot(1, 1, 1, ax =>
        {
            ax.Plot(Ramp, Ramp).SetXLabel(XTitle).WithTitle(AxesTitle);
            configure?.Invoke(ax);
        }).Build();
    }

    private static CountingContext LaidOut(Figure figure)
    {
        var ctx = new CountingContext();
        _ = new ChartRenderer().FrameOf(figure, ctx);
        return ctx;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ALayoutWhoseLabelsFit_MeasuresTheEndsOfEachAxesOnce(bool tight)
    {
        var ctx = LaidOut(Line(tight));

        Assert.Equal(1, ctx.Of(XTitle));
    }

    /// <summary>The tight layout reads the title above the plot in its end pass only.</summary>
    [Fact]
    public void TheTightLayout_MeasuresTheAxesTitleOnce()
    {
        var ctx = LaidOut(Line(tight: true));

        Assert.Equal(1, ctx.Of(AxesTitle));
    }

    /// <summary>A Y axis stacked on the right is measured in the side pass only: its tick labels (a thousand times the primary's
    /// values, so no other label reads the same) are measured once.</summary>
    [Fact]
    public void TheTightLayout_MeasuresAYAxisOnTheRightOnce()
    {
        var ctx = LaidOut(Line(tight: true, ax => ax.WithAddRightYAxis(s => s.Plot(Ramp, [.. Ramp.Select(v => v * 1000)]))));

        Assert.Equal(1, ctx.Of("4000"));
    }

    /// <remarks>On the default path the second pass finds the very same room, to the last bit; a room that differs by a
    /// rounding has moved the plots, and the ends are measured again at the plots handed on (the case below).</remarks>
    [Fact]
    public void ASettlingPassThatChangesNothing_HandsItsEndsOn_SoTheyAreNotMeasuredAgain()
    {
        var figure = Line(tight: false, ax => ax.SetXLim(0, 10).SetXTickLocator(new FixedLocator([0, 10])).SetXTickFormatter(new TestTickFormatter(value => value >= 10 ? "the last label of all, long" : "0")));

        var ctx = LaidOut(figure);

        // The first pass widens the right margin for the long label; the second, at the narrower plot, finds the same room.
        Assert.Equal(2, ctx.Of(XTitle));
    }

    /// <summary>A pass that moved the plots leaves the ends to be measured at the plots the sides settled on: the labels turn at
    /// that width, so their bands are read there. Twelve long category names at 500 px turn, the first stands out to the left
    /// by a room that changes with the width of the plot, so five passes move the plots, the fifth agreeing with the fourth to
    /// within a hundredth of a pixel, where the loop stops; the ends are measured once more at the plots of the last.</summary>
    [Fact]
    public void ASettlingPassThatMovedThePlots_LeavesTheEndsToBeMeasuredAtTheSettledPlots()
    {
        string[] names = [.. Enumerable.Range(1, 12).Select(i => $"Category number {i}")];
        var figure = Plt.Create().WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax => ax.Bar(names, [.. names.Select((_, i) => i + 1.0)]).SetXLabel(XTitle)).Build();

        var ctx = LaidOut(figure);

        Assert.Equal(6, ctx.Of(XTitle));
    }

    /// <summary>Counts the renderers made for a polar axes.</summary>
    private static int s_made;

    /// <summary>A polar axes is measured by one renderer for its sides and one for its ends, on either layout path.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnePolarAxes_IsMeasuredByTwoRenderers(bool tight)
    {
        AxesRenderer.RegisterRenderer(CoordinateSystem.Polar, (a, p, c, t) => { s_made++; return new PolarAxesRenderer(a, p, c, t); });
        try
        {
            var builder = Plt.Create().WithSize(300, 300);
            if (tight) builder = builder.TightLayout();
            var figure = builder.Plot([0.0, 1, 2], [1.0, 2, 1]).Build();
            figure.SubPlots[0].CoordinateSystem = CoordinateSystem.Polar;
            s_made = 0;

            _ = new ChartRenderer().FrameOf(figure, new SvgRenderContext());

            Assert.Equal(2, s_made);
        }
        finally
        {
            AxesRenderer.RegisterRenderer(CoordinateSystem.Polar, (a, p, c, t) => new PolarAxesRenderer(a, p, c, t));
        }
    }

    // ---- each pass measures only the edges it reads -------------------------------------------------------------------

    private static Figure WithLegend(LegendPosition position, bool visible) =>
        Plt.Create().WithSize(500, 350).AddSubPlot(1, 1, 1, ax => ax
            .Plot(Ramp, Ramp, s => s.Label = "a labelled line")
            .WithLegend(l => l with { Position = position, Visible = visible })).Build();

    private readonly record struct Needs(AxisNeeds Sides, AxisNeeds Ends, Size Box);

    private static Needs NeedsOf(LegendPosition position, bool visible)
    {
        var figure = WithLegend(position, visible);
        var axes = figure.SubPlots[0];
        var ctx = new SvgRenderContext();
        var engine = new AxesEdgeNeeds();
        return new Needs(engine.MeasureSides(axes, figure, ctx, default), engine.MeasureEnds(axes, figure, ctx, default),
            LegendLayoutCalculator.MeasureLegendBox(axes, ctx, figure.Theme));
    }

    private readonly record struct ShownAndHiddenNeeds(Needs Shown, Needs Hidden);

    private static ShownAndHiddenNeeds ShownAndHidden(LegendPosition position) =>
        new(NeedsOf(position, visible: true), NeedsOf(position, visible: false));

    [Fact]
    public void AnOutsideLeftLegend_WidensTheLeftNeed_AndNoOtherEdge()
    {
        var (shown, hidden) = ShownAndHidden(LegendPosition.OutsideLeft);

        // The legend is measured, so its edge's band (what lifts the cap of the margin there) is the whole need.
        double left = hidden.Sides.Start + shown.Box.Width + LegendPositionStrategy.OutsideReserve;
        Assert.Equal(hidden.Sides with { Start = left, StartBand = left }, shown.Sides);
        Assert.Equal(hidden.Ends, shown.Ends);
    }

    [Fact]
    public void AnOutsideRightLegend_TakesTheRightNeed_AndNoOtherEdge()
    {
        var (shown, hidden) = ShownAndHidden(LegendPosition.OutsideRight);

        double right = Math.Max(hidden.Sides.End, shown.Box.Width + LegendPositionStrategy.OutsideReserve);
        Assert.Equal(hidden.Sides with { End = right, EndBand = right }, shown.Sides);
        Assert.True(shown.Sides.End > hidden.Sides.End);
        Assert.Equal(hidden.Ends, shown.Ends);
    }

    [Theory]
    [InlineData(LegendPosition.OutsideTop, true)]
    [InlineData(LegendPosition.OutsideBottom, false)]
    public void AnOutsideTopOrBottomLegend_WidensItsEndsNeed_AndNoOtherEdge(LegendPosition position, bool atTop)
    {
        var (shown, hidden) = ShownAndHidden(position);

        double reach = (atTop ? hidden.Ends.Start : hidden.Ends.End) + shown.Box.Height + LegendPositionStrategy.OutsideReserve;
        Assert.Equal(atTop ? hidden.Ends with { Start = reach, StartBand = reach } : hidden.Ends with { End = reach, EndBand = reach }, shown.Ends);
        Assert.Equal(hidden.Sides, shown.Sides);
    }

    [Fact]
    public void ALegendInsideThePlot_NeedsNoRoomOnAnyEdge()
    {
        var shown = NeedsOf(LegendPosition.UpperRight, visible: true);
        var hidden = NeedsOf(LegendPosition.UpperRight, visible: false);

        Assert.Equal(hidden.Sides, shown.Sides);
        Assert.Equal(hidden.Ends, shown.Ends);
    }

    [Fact]
    public void TheSidePass_MeasuresNoTextAboveOrBelowThePlot()
    {
        var figure = Line(tight: true);
        var ctx = new CountingContext();

        _ = new AxesEdgeNeeds().MeasureSides(figure.SubPlots[0], figure, ctx, default);

        Assert.Equal(0, ctx.Of(AxesTitle));
        Assert.Equal(0, ctx.Of(XTitle));
    }

    [Fact]
    public void TheEndPass_MeasuresNoAxisBesideThePlot()
    {
        var figure = Line(tight: true, ax => ax.WithAddRightYAxis(s => s.Plot(Ramp, [.. Ramp.Select(v => v * 1000)])));
        var ctx = new CountingContext();

        _ = new AxesEdgeNeeds().MeasureEnds(figure.SubPlots[0], figure, ctx, default);

        Assert.Equal(0, ctx.Of("4000"));
        Assert.Equal(1, ctx.Of(AxesTitle));
    }

    private const string LegendRow = "the figure legend row";

    /// <summary>The legend of a figure is placed once a draw: the constrained layout keeps room for an outside legend by the box
    /// that placement measured, and the overlay draws that very placement, so its rows are measured as often as one placement
    /// measures them.</summary>
    [Fact]
    public void AnOutsideFigureLegend_IsPlacedOnceADraw()
    {
        var figure = Plt.Create().WithSize(500, 350).ConstrainedLayout()
            .AddSubPlot(1, 1, 1, ax => ax.WithLegend(visible: false).Plot(Ramp, Ramp, s => s.Label = LegendRow))
            .FigLegend(legend => legend with { Position = LegendPosition.OutsideRight })
            .Build();
        var once = new CountingContext();
        _ = new LegendLayoutCalculator(figure.Theme, once).Compute(figure.Legends[0].OnTheFigure(figure.Theme), [LegendRow], new Rect(0, 0, 500, 350));

        var ctx = new CountingContext();
        new ChartRenderer().Render(figure, ctx);

        Assert.True(once.Of(LegendRow) > 0);
        Assert.Equal(once.Of(LegendRow), ctx.Of(LegendRow));
    }
}
