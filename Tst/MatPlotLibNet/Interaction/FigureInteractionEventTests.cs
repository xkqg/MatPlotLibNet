// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

public class FigureInteractionEventTests
{
    [Fact]
    public void ZoomEvent_ApplyTo_SetsAxisLimits()
    {
        var figure = TestFigures.SingleLine();
        var evt = new ZoomEvent("chart-1", AxesIndex: 0, XMin: 0.5, XMax: 2.5, YMin: 0, YMax: 10);

        evt.ApplyTo(figure);

        var axes = figure.SubPlots[0];
        Assert.Equal(0.5, axes.XAxis.Min);
        Assert.Equal(2.5, axes.XAxis.Max);
        Assert.Equal(0.0, axes.YAxis.Min);
        Assert.Equal(10.0, axes.YAxis.Max);
    }

    [Fact]
    public void ResetEvent_ApplyTo_SetsAxisLimits()
    {
        var figure = TestFigures.SingleLine();
        var evt = new ResetEvent("chart-1", AxesIndex: 0, XMin: 1, XMax: 3, YMin: 1, YMax: 9);

        evt.ApplyTo(figure);

        var axes = figure.SubPlots[0];
        Assert.Equal(1.0, axes.XAxis.Min);
        Assert.Equal(3.0, axes.XAxis.Max);
        Assert.Equal(1.0, axes.YAxis.Min);
        Assert.Equal(9.0, axes.YAxis.Max);
    }

    [Fact]
    public void PanEvent_ApplyTo_TranslatesAxisLimits()
    {
        var figure = TestFigures.SingleLine();
        var axes = figure.SubPlots[0];
        axes.XAxis.Min = 0; axes.XAxis.Max = 10;
        axes.YAxis.Min = 0; axes.YAxis.Max = 20;

        var evt = new PanEvent("chart-1", AxesIndex: 0, DxData: 2.0, DyData: -5.0);
        evt.ApplyTo(figure);

        Assert.Equal(2.0, axes.XAxis.Min);
        Assert.Equal(12.0, axes.XAxis.Max);
        Assert.Equal(-5.0, axes.YAxis.Min);
        Assert.Equal(15.0, axes.YAxis.Max);
    }

    /// <summary>ROADMAP.md row 100: zoom/reset must move every outward-stacked secondary axis' own range, not
    /// only the primary. Each change is a slot's OWN new range — DataRange.XMin/XMax are unused here (a secondary
    /// Y axis shares the primary X, already carried by ZoomEvent's own XMin/XMax).</summary>
    [Fact]
    public void ZoomEvent_ApplyTo_WithSecondaryRanges_SetsEachSlotsOwnRange()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithAddRightYAxis(y2 => y2.Plot([1.0, 2], [50.0, 60]))
                .WithAddRightYAxis(outer => outer.Plot([1.0, 2], [500.0, 600]))
                .WithAddLeftYAxis(left => left.Plot([1.0, 2], [5.0, 6])))
            .Build();

        var evt = new ZoomEvent("chart-1", 0, 0.5, 2.5, 0, 10)
        {
            SecondaryRanges =
            [
                new SecondaryAxisRange(AxisSide.Right, 0, new DataRange(0, 1, 10, 20)),
                new SecondaryAxisRange(AxisSide.Right, 1, new DataRange(0, 1, 100, 200)),
                new SecondaryAxisRange(AxisSide.Left, 0, new DataRange(0, 1, 1, 2)),
            ],
        };

        evt.ApplyTo(figure);

        var axes = figure.SubPlots[0];
        Assert.Equal(10.0, axes.YAxes.Right[0].Axis.Min);
        Assert.Equal(20.0, axes.YAxes.Right[0].Axis.Max);
        Assert.Equal(100.0, axes.YAxes.Right[1].Axis.Min);
        Assert.Equal(200.0, axes.YAxes.Right[1].Axis.Max);
        Assert.Equal(1.0, axes.YAxes.Left.SlotAtOutward(0)!.Axis.Min);
        Assert.Equal(2.0, axes.YAxes.Left.SlotAtOutward(0)!.Axis.Max);
    }

    /// <summary>A pan carries the ALREADY-computed final range per secondary slot (PanModifier works out each
    /// slot's own proportional delta before sending, since the primary's DxData/DyData are in the PRIMARY's data
    /// units) — ApplyTo assigns it directly, it does not re-derive a shift from DyData for a secondary slot.</summary>
    [Fact]
    public void PanEvent_ApplyTo_WithSecondaryRanges_AssignsEachSlotsOwnRangeDirectly()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithAddRightYAxis(y2 => y2.Plot([1.0, 2], [50.0, 60])))
            .Build();

        var evt = new PanEvent("chart-1", 0, 2.0, -5.0)
        {
            SecondaryRanges = [new SecondaryAxisRange(AxisSide.Right, 0, new DataRange(0, 1, 30, 70))],
        };

        evt.ApplyTo(figure);

        var axes = figure.SubPlots[0];
        Assert.Equal(30.0, axes.YAxes.Right[0].Axis.Min);
        Assert.Equal(70.0, axes.YAxes.Right[0].Axis.Max);
    }

    /// <summary>A secondary range arrives from the client (ChartHub binds the event straight from JSON), so a slot the
    /// figure does not have — an index gone stale after the server's figure changed, or a crafted one — is skipped the
    /// way LegendToggleEvent skips a series index past the end: nothing throws, no slot moves, and the ranges that do
    /// name a real slot are still applied.</summary>
    [Theory]
    [InlineData(AxisSide.Right, 5)]
    [InlineData(AxisSide.Right, -1)]
    [InlineData(AxisSide.Left, 0)]
    [InlineData(AxisSide.Left, 3)]
    public void ZoomEvent_ApplyTo_WithASecondaryRangeForAMissingSlot_SkipsItAndAppliesTheRest(AxisSide side, int slotIndex)
    {
        var figure = OneRightAxisFigure();
        var evt = new ZoomEvent("chart-1", 0, 0.5, 2.5, 0, 10)
        {
            SecondaryRanges =
            [
                new SecondaryAxisRange(side, slotIndex, new DataRange(0, 1, 1, 2)),
                new SecondaryAxisRange(AxisSide.Right, 0, new DataRange(0, 1, 10, 20)),
            ],
        };

        var thrown = Record.Exception(() => evt.ApplyTo(figure));

        Assert.Null(thrown);
        var slot = figure.SubPlots[0].YAxes.Right[0];
        Assert.Equal(10.0, slot.Axis.Min);
        Assert.Equal(20.0, slot.Axis.Max);
    }

    /// <summary>Same wire value, the pan family: <see cref="PanEvent"/> and the zoom/reset events share one
    /// application of a secondary range, so a missing slot must be skipped on both.</summary>
    [Theory]
    [InlineData(AxisSide.Right, 5)]
    [InlineData(AxisSide.Right, -1)]
    [InlineData(AxisSide.Left, 0)]
    [InlineData(AxisSide.Left, 3)]
    public void PanEvent_ApplyTo_WithASecondaryRangeForAMissingSlot_SkipsItAndAppliesTheRest(AxisSide side, int slotIndex)
    {
        var figure = OneRightAxisFigure();
        var evt = new PanEvent("chart-1", 0, 2.0, -5.0)
        {
            SecondaryRanges =
            [
                new SecondaryAxisRange(side, slotIndex, new DataRange(0, 1, 1, 2)),
                new SecondaryAxisRange(AxisSide.Right, 0, new DataRange(0, 1, 30, 70)),
            ],
        };

        var thrown = Record.Exception(() => evt.ApplyTo(figure));

        Assert.Null(thrown);
        var slot = figure.SubPlots[0].YAxes.Right[0];
        Assert.Equal(30.0, slot.Axis.Min);
        Assert.Equal(70.0, slot.Axis.Max);
    }

    /// <summary>A secondary range numbers the axes PAST the primary: (Left, 0) is the first outward left axis, never the
    /// primary Y axis, which the left side also holds. The primary moves only by the event's own Y fields (a zoom's
    /// <c>YMin</c>/<c>YMax</c>, a pan's <c>DyData</c>), and the range for (Left, 0) lands on the outward slot alone — on
    /// the zoom family and on the pan alike, since they share the one application.</summary>
    [Fact]
    public void ASecondaryRangeAtLeftZero_WritesTheFirstOutwardLeftSlot_NeverThePrimary()
    {
        var zoomed = OneLeftAxisFigure();
        new ZoomEvent("chart-1", 0, 0.5, 2.5, 0, 10)
        {
            SecondaryRanges = [new SecondaryAxisRange(AxisSide.Left, 0, new DataRange(0, 1, -7, 7))],
        }.ApplyTo(zoomed);

        var zoomedAxes = zoomed.SubPlots[0];
        Assert.Equal(2, zoomedAxes.YAxes.Left.Count);
        Assert.Equal(-7.0, zoomedAxes.YAxes.Left.SlotAtOutward(0)!.Axis.Min);
        Assert.Equal(7.0, zoomedAxes.YAxes.Left.SlotAtOutward(0)!.Axis.Max);
        Assert.Equal(0.0, zoomedAxes.YAxis.Min);
        Assert.Equal(10.0, zoomedAxes.YAxis.Max);

        var panned = OneLeftAxisFigure();
        panned.SubPlots[0].YAxis.Min = 0;
        panned.SubPlots[0].YAxis.Max = 20;
        new PanEvent("chart-1", 0, 2.0, 5.0)
        {
            SecondaryRanges = [new SecondaryAxisRange(AxisSide.Left, 0, new DataRange(0, 1, -7, 7))],
        }.ApplyTo(panned);

        var pannedAxes = panned.SubPlots[0];
        Assert.Equal(-7.0, pannedAxes.YAxes.Left.SlotAtOutward(0)!.Axis.Min);
        Assert.Equal(7.0, pannedAxes.YAxes.Left.SlotAtOutward(0)!.Axis.Max);
        Assert.Equal(5.0, pannedAxes.YAxis.Min);
        Assert.Equal(25.0, pannedAxes.YAxis.Max);
    }

    /// <summary>The interaction wire numbers the Y axes PAST the primary, whichever event carries the range: for every event
    /// family (zoom, reset, rectangle zoom, pan) <c>(Left, 0)</c> is the first axis out from the primary and <c>(Left, 1)</c>
    /// the second, and neither reaches the primary Y axis, which keeps exactly the limits the event's own Y fields set. An
    /// index below 0 never reaches it either: the primary is not on the wire.</summary>
    [Theory]
    [InlineData("zoom")]
    [InlineData("reset")]
    [InlineData("rectangle zoom")]
    [InlineData("pan")]
    public void ASecondaryRangeAtLeftZero_NeverAddressesThePrimaryYAxis(string family)
    {
        var figure = TwoLeftAxesFigure();
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 20;
        IReadOnlyList<SecondaryAxisRange> ranges =
        [
            new(AxisSide.Left, 0, new DataRange(0, 1, -7, 7)),
            new(AxisSide.Left, 1, new DataRange(0, 1, -70, 70)),
            new(AxisSide.Left, -1, new DataRange(0, 1, 111, 222)),
        ];

        FigureInteractionEvent evt = family switch
        {
            "zoom" => new ZoomEvent("chart-1", 0, 0.5, 2.5, 4, 14) { SecondaryRanges = ranges },
            "reset" => new ResetEvent("chart-1", 0, 0.5, 2.5, 4, 14) { SecondaryRanges = ranges },
            "rectangle zoom" => new RectangleZoomEvent("chart-1", 0, 0.5, 2.5, 4, 14) { SecondaryRanges = ranges },
            _ => new PanEvent("chart-1", 0, 2.0, 4.0) { SecondaryRanges = ranges },
        };

        evt.ApplyTo(figure);

        var axes = figure.SubPlots[0];
        Assert.Equal(4.0, axes.YAxis.Min);
        Assert.Equal(family == "pan" ? 24.0 : 14.0, axes.YAxis.Max);
        var left = axes.YAxes.Left;
        Assert.Equal(3, left.Count);
        Assert.Same(axes.YAxis, left[0].Axis);
        Assert.Equal(-7.0, left[1].Axis.Min);
        Assert.Equal(7.0, left[1].Axis.Max);
        Assert.Equal(-70.0, left[2].Axis.Min);
        Assert.Equal(70.0, left[2].Axis.Max);
    }

    /// <summary>An event is addressed to a subplot of the figure, and the index comes from the client (ChartHub binds the
    /// event straight from JSON): one that names a subplot the figure does not have — gone stale after the server's figure
    /// changed, or crafted — is dropped. Nothing throws out of the session's drain loop, and nothing in the figure changes,
    /// whichever kind of event it is.</summary>
    [Theory]
    [MemberData(nameof(EventsForAMissingSubplot))]
    public void AnEventForAMissingSubplot_IsDroppedNotThrown(FigureInteractionEvent evt, int subplotCount)
    {
        var figure = subplotCount == 0 ? TestFigures.Empty() : TestFigures.SingleLine();
        string before = figure.ToJson();

        var thrown = Record.Exception(() => evt.ApplyTo(figure));

        Assert.Null(thrown);
        Assert.Equal(before, figure.ToJson());
    }

    /// <summary>Every event kind the library ships, addressed to a subplot the figure lacks: -1, one past the end and far
    /// past it on a figure with one subplot, and subplot 0 on a figure with none.</summary>
    public static TheoryData<FigureInteractionEvent, int> EventsForAMissingSubplot()
    {
        var data = new TheoryData<FigureInteractionEvent, int>();
        foreach (var evt in EveryEventKind(-1).Concat(EveryEventKind(1)).Concat(EveryEventKind(5)))
            data.Add(evt, 1);
        foreach (var evt in EveryEventKind(0))
            data.Add(evt, 0);
        return data;
    }

    private static IEnumerable<FigureInteractionEvent> EveryEventKind(int axesIndex) =>
    [
        new ZoomEvent("chart-1", axesIndex, 0.5, 2.5, 0, 10) { SecondaryRanges = [new(AxisSide.Right, 0, new DataRange(0, 1, 1, 2))] },
        new ResetEvent("chart-1", axesIndex, 0.5, 2.5, 0, 10),
        new RectangleZoomEvent("chart-1", axesIndex, 0.5, 2.5, 0, 10),
        new PanEvent("chart-1", axesIndex, 2.0, -5.0) { InView = new DataRange(0, 10, 0, 20) },
        new LegendToggleEvent("chart-1", axesIndex, 0),
        new Rotate3DEvent("chart-1", axesIndex, 15, 10),
        new AddTrendlineEvent("chart-1", axesIndex, new Trendline(0.0, 10.0, 5.0, 20.0)),
        new AddHorizontalLevelEvent("chart-1", axesIndex, new HorizontalLevel(5.0)),
        new AddFibonacciRetracementEvent("chart-1", axesIndex, new FibonacciRetracement(200.0, 100.0)),
        new RemoveDrawingToolEvent("chart-1", axesIndex, new Trendline(0.0, 10.0, 5.0, 20.0)),
    ];

    /// <summary>The drop is a signal to whoever applies the event, not only an absence: a session counts the events it could not
    /// apply, so every event kind says whether it was dropped.</summary>
    [Theory]
    [MemberData(nameof(EventsForAMissingSubplot))]
    public void AnEventForAMissingSubplot_ReportsThatItWasDropped(FigureInteractionEvent evt, int subplotCount)
    {
        var figure = subplotCount == 0 ? TestFigures.Empty() : TestFigures.SingleLine();

        Assert.False(evt.TryApplyTo(figure));
    }

    [Theory]
    [MemberData(nameof(EventsForAnExistingSubplot))]
    public void AnEventForAnExistingSubplot_ReportsThatItWasApplied(FigureInteractionEvent evt)
    {
        Assert.True(evt.TryApplyTo(TestFigures.SingleLine()));
    }

    public static TheoryData<FigureInteractionEvent> EventsForAnExistingSubplot()
    {
        var data = new TheoryData<FigureInteractionEvent>();
        foreach (var evt in EveryEventKind(0)) data.Add(evt);
        return data;
    }

    /// <summary>An event written outside the library overrides only <c>ApplyTo</c>: it runs, and it counts as applied.</summary>
    [Fact]
    public void ASubclassEvent_ThatOverridesOnlyApplyTo_IsRunAndReportedApplied()
    {
        var probe = new ProbeEvent("chart-1", 0);

        Assert.True(probe.TryApplyTo(TestFigures.SingleLine()));
        Assert.Equal(1, probe.Applied);
    }

    /// <summary>The protected <c>TargetAxes</c> keeps its published behaviour for the events that subclass
    /// <see cref="FigureInteractionEvent"/> outside the library: the axes at the index, and the index exception when there is
    /// none. <c>TryTargetAxes</c> is the form that answers instead of throwing.</summary>
    [Fact]
    public void TargetAxes_KeepsItsBehaviourForSubclasses_AndTryTargetAxesAnswersInstead()
    {
        var figure = TestFigures.SingleLine();

        Assert.Same(figure.SubPlots[0], new ProbeEvent("c", 0).Target(figure));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProbeEvent("c", 1).Target(figure));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProbeEvent("c", -1).Target(figure));

        Assert.True(new ProbeEvent("c", 0).TryTarget(figure, out var found));
        Assert.Same(figure.SubPlots[0], found);
        Assert.False(new ProbeEvent("c", 1).TryTarget(figure, out var past));
        Assert.Null(past);
        Assert.False(new ProbeEvent("c", -1).TryTarget(figure, out var below));
        Assert.Null(below);
        Assert.False(new ProbeEvent("c", 0).TryTarget(TestFigures.Empty(), out var none));
        Assert.Null(none);
    }

    /// <summary>The axes linked to the target by <c>ShareX</c> are the target's group; an event that names no subplot has no
    /// target, so no group.</summary>
    [Fact]
    public void XLinkedAxes_IsTheTargetsGroup_AndEmptyForAMissingSubplot()
    {
        var figure = new Figure();
        var first = figure.AddSubPlot();
        var second = figure.AddSubPlot();
        var third = figure.AddSubPlot();
        second.ShareXWith = first;

        Assert.Equal(new[] { first, second }, new ProbeEvent("c", 0).Linked(figure));
        Assert.Equal(new[] { second, first }, new ProbeEvent("c", 1).Linked(figure));
        Assert.Equal(new[] { third }, new ProbeEvent("c", 2).Linked(figure));
        Assert.Empty(new ProbeEvent("c", 3).Linked(figure));
        Assert.Empty(new ProbeEvent("c", -1).Linked(figure));
    }

    /// <summary>A subclass of the kind a user writes: it overrides <c>ApplyTo</c> and reaches the figure through the protected
    /// members of <see cref="FigureInteractionEvent"/>.</summary>
    private sealed record ProbeEvent(string ChartId, int AxesIndex) : FigureInteractionEvent(ChartId, AxesIndex)
    {
        public int Applied { get; private set; }

        public override void ApplyTo(Figure figure) => Applied++;

        public Axes Target(Figure figure) => TargetAxes(figure);

        public bool TryTarget(Figure figure, out Axes? axes) => TryTargetAxes(figure, out axes);

        public IReadOnlyList<Axes> Linked(Figure figure) => XLinkedAxes(figure);
    }

    private static Figure TwoLeftAxesFigure() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2], [3.0, 4])
            .WithAddLeftYAxis(first => first.Plot([1.0, 2], [5.0, 6]))
            .WithAddLeftYAxis(second => second.Plot([1.0, 2], [50.0, 60])))
        .Build();

    private static Figure OneLeftAxisFigure() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2], [3.0, 4])
            .WithAddLeftYAxis(left => left.Plot([1.0, 2], [5.0, 6])))
        .Build();

    private static Figure OneRightAxisFigure() => Plt.Create()
        .AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2], [3.0, 4])
            .WithAddRightYAxis(y2 => y2.Plot([1.0, 2], [50.0, 60])))
        .Build();

    [Fact]
    public void PanEvent_ApplyTo_IsNoOpWhenAxisLimitsNull()
    {
        var figure = TestFigures.SingleLine();
        var axes = figure.SubPlots[0];
        axes.XAxis.Min = null; axes.XAxis.Max = null;
        axes.YAxis.Min = null; axes.YAxis.Max = null;

        var evt = new PanEvent("chart-1", 0, 1.0, 1.0);
        evt.ApplyTo(figure);

        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
    }

    [Fact]
    public void LegendToggleEvent_ApplyTo_FlipsVisible()
    {
        var figure = TestFigures.SingleLine();
        var series = (ChartSeries)figure.SubPlots[0].Series[0];
        Assert.True(series.Visible);

        var evt = new LegendToggleEvent("chart-1", AxesIndex: 0, SeriesIndex: 0);
        evt.ApplyTo(figure);
        Assert.False(series.Visible);

        evt.ApplyTo(figure);
        Assert.True(series.Visible);
    }

    /// <summary>The index a legend entry sends is the series' slot: the primary series, then the secondary-Y series,
    /// then the secondary-X series. The toggle flips the series at that slot and no other; a slot past the last one
    /// changes nothing.</summary>
    [Theory]
    [InlineData(0, "primary")]
    [InlineData(1, "secondaryY")]
    [InlineData(2, "secondaryX")]
    [InlineData(3, null)]
    public void LegendToggleEvent_ApplyTo_FlipsTheSeriesAtTheSlot_OnEitherAxis(int slot, string? hidden)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot([1.0, 2.0], [1.0, 2.0], s => s.Label = "primary")
            .WithAddRightYAxis(y2 => y2.Plot([1.0, 2.0], [10.0, 20.0], s => s.Label = "secondaryY"))
            .WithSecondaryXAxis(x2 => x2.PlotXSecondary([100.0, 200.0], [1.0, 2.0], s => s.Label = "secondaryX"))).Build();
        var axes = figure.SubPlots[0];

        new LegendToggleEvent("c", 0, slot).ApplyTo(figure);

        var all = axes.Series.Concat(axes.YAxes.Right[0].Series).Concat(axes.XSecondarySeries).Cast<ChartSeries>();
        Assert.All(all, s => Assert.Equal(s.Label != hidden, s.Visible));
    }

    [Fact]
    public void FigureInteractionEvent_IsAbstract()
    {
        Assert.True(typeof(FigureInteractionEvent).IsAbstract);
    }

    [Fact]
    public void AxisRangeEvent_IsAbstract()
    {
        Assert.True(typeof(AxisRangeEvent).IsAbstract);
    }

    [Fact]
    public void ZoomEvent_Inherits_AxisRangeEvent()
    {
        Assert.True(typeof(AxisRangeEvent).IsAssignableFrom(typeof(ZoomEvent)));
    }

    [Fact]
    public void ResetEvent_Inherits_AxisRangeEvent()
    {
        Assert.True(typeof(AxisRangeEvent).IsAssignableFrom(typeof(ResetEvent)));
    }

    [Fact]
    public void PanEvent_Inherits_FigureInteractionEvent_Directly()
    {
        Assert.Equal(typeof(FigureInteractionEvent), typeof(PanEvent).BaseType);
    }

    [Fact]
    public void LegendToggleEvent_Inherits_FigureInteractionEvent_Directly()
    {
        Assert.Equal(typeof(FigureInteractionEvent), typeof(LegendToggleEvent).BaseType);
    }

    [Fact]
    public void ZoomEvent_CarriesChartIdAndAxesIndex()
    {
        var evt = new ZoomEvent("live-1", 2, 0, 1, 0, 1);
        Assert.Equal("live-1", evt.ChartId);
        Assert.Equal(2, evt.AxesIndex);
    }

    [Fact]
    public void ZoomEvent_IsRecord_SupportsValueEquality()
    {
        var a = new ZoomEvent("c", 0, 1, 2, 3, 4);
        var b = new ZoomEvent("c", 0, 1, 2, 3, 4);
        Assert.Equal(a, b);
    }
}
