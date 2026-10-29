// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

// This file is the one place in the repository that calls the old Y axis verbs and builders, on purpose: they stay
// published so existing code compiles, and what they drew and wrote is held here. Everything else uses the container.
#pragma warning disable CS0618

using System.Collections;
using System.Reflection;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.Streaming;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Holds the published Y axis verbs and builders (<c>WithSecondaryYAxis</c> with its
/// <see cref="SecondaryAxisBuilder"/>, <c>WithOutwardYAxis</c> with its <see cref="OutwardYAxisBuilder"/>) against the
/// container's verbs: the same figure built either way draws the same SVG and writes the same JSON, and the one builder
/// of the container carries every verb the two old builders carry. Nothing the old verbs drew is lost.
/// <para>It also holds the published behaviour of the old members of <see cref="Axes"/> (<c>TwinX</c>,
/// <c>SecondaryYAxis</c>, <c>SecondarySeries</c>, <c>AddSecondarySeries</c>, <c>AddOutwardYAxis</c>, <c>RightYAxes</c>,
/// <c>LeftYAxes</c> and <see cref="SecondaryAxisSlot"/>): what each did and returned stays pinned here, while every
/// other test addresses the same slots through <see cref="Axes.YAxes"/>.</para></summary>
public class ObsoleteYAxisMembersCompatTests
{
    private static readonly Color Red = Color.FromHex("#D62728");
    private static readonly Color Blue = Color.FromHex("#1F77B4");

    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0];

    private static FigureBuilder Figure() => Plt.Create().WithSize(900, 500);

    /// <summary>One figure built twice: with the verbs 1.23.0 published, and with the container's verbs. The name is what a
    /// Theory carries; the builders stay here.</summary>
    private readonly record struct ParityCase(string Name, Func<FigureBuilder> Old, Func<FigureBuilder> New);

    private static readonly ParityCase[] Cases =
    [
        new("first right axis, coloured, with limits",
            () => Figure().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [3.0, 1, 4, 2]).SetYLabel("Primary")
                .WithSecondaryYAxis(y => y.SetYLabel("Rate").SetYAxisColor(Red).SetYLim(0, 100).Plot(X, [10.0, 40, 20, 80], l => l.Label = "rate"))),
            () => Figure().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [3.0, 1, 4, 2]).SetYLabel("Primary")
                .WithAddRightYAxis(y => y.SetYLabel("Rate").SetYAxisColor(Red).SetYLim(0, 100).Plot(X, [10.0, 40, 20, 80], l => l.Label = "rate")))),

        new("two right axes",
            () => Figure().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [3.0, 1, 4, 2]).SetYLabel("Primary")
                .WithSecondaryYAxis(y => y.SetYLabel("Rate").SetYAxisColor(Blue).Plot(X, [10.0, 40, 20, 80]))
                .WithOutwardYAxis(AxisSide.Right, y => y.SetYLabel("Load").SetYLim(0, 5).Plot(X, [1.0, 2, 4, 3]))),
            () => Figure().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [3.0, 1, 4, 2]).SetYLabel("Primary")
                .WithAddRightYAxis(y => y.SetYLabel("Rate").SetYAxisColor(Blue).Plot(X, [10.0, 40, 20, 80]))
                .WithAddRightYAxis(y => y.SetYLabel("Load").SetYLim(0, 5).Plot(X, [1.0, 2, 4, 3])))),

        new("a left axis past the primary",
            () => Figure().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [3.0, 1, 4, 2]).SetYLabel("Primary").SetYAxisColor(Red)
                .WithOutwardYAxis(AxisSide.Left, y => y.SetYLabel("Queue").SetYLim(0, 9).Plot(X, [5.0, 6, 2, 7]))),
            () => Figure().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [3.0, 1, 4, 2]).SetYLabel("Primary").SetYAxisColor(Red)
                .WithAddLeftYAxis(y => y.SetYLabel("Queue").SetYLim(0, 9).Plot(X, [5.0, 6, 2, 7])))),

        new("four axes, two to a side",
            () => Figure().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [3.0, 1, 4, 2], l => l.Label = "p").SetYLabel("Primary").SetYAxisColor(Blue)
                .WithSecondaryYAxis(y => y.SetYLabel("Right 0").SetYAxisColor(Red).Plot(X, [10.0, 40, 20, 80], l => l.Label = "r0"))
                .WithOutwardYAxis(AxisSide.Right, y => y.SetYLabel("Right 1").Plot(X, [100.0, 300, 200, 400], l => l.Label = "r1"))
                .WithOutwardYAxis(AxisSide.Left, y => y.SetYLabel("Left 1").Plot(X, [0.5, 1.5, 1.0, 2.0], l => l.Label = "l1"))
                .WithLegend()),
            () => Figure().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [3.0, 1, 4, 2], l => l.Label = "p").SetYLabel("Primary").SetYAxisColor(Blue)
                .WithAddRightYAxis(y => y.SetYLabel("Right 0").SetYAxisColor(Red).Plot(X, [10.0, 40, 20, 80], l => l.Label = "r0"))
                .WithAddRightYAxis(y => y.SetYLabel("Right 1").Plot(X, [100.0, 300, 200, 400], l => l.Label = "r1"))
                .WithAddLeftYAxis(y => y.SetYLabel("Left 1").Plot(X, [0.5, 1.5, 1.0, 2.0], l => l.Label = "l1"))
                .WithLegend())),

        new("scatter, a direct label and a streaming line",
            () => Figure().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [3.0, 1, 4, 2])
                .WithSecondaryYAxis(y => y.Plot(X, [10.0, 40, 20, 80], l => l.Label = "rate").WithDirectLabel())
                .WithOutwardYAxis(AxisSide.Right, y => y.Scatter(X, [1.0, 2, 4, 3], s => s.Label = "samples"))
                .WithOutwardYAxis(AxisSide.Left, y => y.StreamingPlot(8, s => { s.Label = "live"; s.AppendPoint(1, 2); s.AppendPoint(2, 5); s.AppendPoint(3, 3); }))),
            () => Figure().AddSubPlot(1, 1, 1, ax => ax
                .Plot(X, [3.0, 1, 4, 2])
                .WithAddRightYAxis(y => y.Plot(X, [10.0, 40, 20, 80], l => l.Label = "rate").WithDirectLabel())
                .WithAddRightYAxis(y => y.Scatter(X, [1.0, 2, 4, 3], s => s.Label = "samples"))
                .WithAddLeftYAxis(y => y.StreamingPlot(8, s => { s.Label = "live"; s.AppendPoint(1, 2); s.AppendPoint(2, 5); s.AppendPoint(3, 3); })))),
    ];

    public static TheoryData<string> CaseNames { get; } = new(Cases.Select(c => c.Name));

    private static ParityCase Case(string name) => Cases.Single(c => c.Name == name);

    private static string Normalised(string text) => text.ReplaceLineEndings("\n");

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void TheContainersVerbs_DrawTheSameSvg_AsTheOldVerbs(string name)
    {
        var parity = Case(name);

        string old = Normalised(parity.Old().Build().ToSvg());
        string now = Normalised(parity.New().Build().ToSvg());

        Assert.Contains("<svg", old, StringComparison.Ordinal);
        Assert.Equal(old, now);
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void TheContainersVerbs_WriteTheSameJson_AsTheOldVerbs(string name)
    {
        var parity = Case(name);

        string old = Normalised(parity.Old().Build().ToJson());
        string now = Normalised(parity.New().Build().ToJson());

        Assert.Equal(old, now);
    }

    [Theory]
    [InlineData(typeof(SecondaryAxisBuilder))]
    [InlineData(typeof(OutwardYAxisBuilder))]
    public void TheSlotBuilder_CarriesEveryVerbTheOldBuilderCarries_WithTheSameReturn(Type oldBuilder)
    {
        var carried = typeof(YAxisSlotBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ToLookup(method => method.Name);

        foreach (var verb in oldBuilder.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            Assert.True(carried.Contains(verb.Name), $"{oldBuilder.Name}.{verb.Name} has no counterpart on YAxisSlotBuilder.");
            // The old builder returns itself for chaining; its counterpart returns the one builder. Any other return
            // (the streaming line a live chart appends to) is the same type.
            var expected = verb.ReturnType == oldBuilder ? typeof(YAxisSlotBuilder) : verb.ReturnType;
            Assert.Contains(carried[verb.Name], counterpart => counterpart.ReturnType == expected);
        }
    }

    // ── AxesBuilder.WithSecondaryYAxis / WithOutwardYAxis ──────────────────────────────────────────────────────────────

    /// <summary>Verifies that WithSecondaryYAxis configures a secondary Y-axis with its own series.</summary>
    [Fact]
    public void WithSecondaryYAxis_ConfiguresSecondaryAxis()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithSecondaryYAxis(sec => sec
                    .SetYLabel("Secondary")
                    .Plot([1.0, 2.0], [100.0, 200.0])))
            .Build();

        Assert.NotNull(figure.SubPlots[0].SecondaryYAxis);
        Assert.Equal("Secondary", figure.SubPlots[0].SecondaryYAxis!.Label);
        Assert.Single(figure.SubPlots[0].SecondarySeries);
    }

    /// <summary>The published meaning of <see cref="AxesBuilder.WithSecondaryYAxis"/>: it configures THE first right axis and
    /// creates it only when there is none, so a second call enters the same slot instead of stacking another. Both
    /// callbacks' series land in that one slot and the later limit and label win. Code written against the released verb
    /// (a chart verb that styles its right axis after drawing it, a figure assembled in two steps) relies on this.</summary>
    [Fact]
    public void WithSecondaryYAxis_CalledTwice_ConfiguresTheSameRightSlot()
    {
        var axes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithSecondaryYAxis(sec => sec.SetYLabel("First").Plot([1.0, 2.0], [100.0, 200.0], s => s.Label = "one"))
                .WithSecondaryYAxis(sec => sec.SetYLabel("Second").SetYLim(0, 500).Plot([1.0, 2.0], [300.0, 400.0], s => s.Label = "two")))
            .Build().SubPlots[0];

        var slot = Assert.Single(axes.RightYAxes);
        Assert.Same(axes.SecondaryYAxis, slot.Axis);
        Assert.Equal(["one", "two"], slot.Series.Select(s => s.Label));
        Assert.Equal("Second", slot.Axis.Label);
        Assert.Equal(0.0, slot.Axis.Min);
        Assert.Equal(500.0, slot.Axis.Max);
        Assert.Empty(axes.LeftYAxes);
    }

    /// <summary>Verifies that WithOutwardYAxis(Right) reaches the SECOND right axis, independent of slot 0
    /// (<see cref="AxesBuilder.WithSecondaryYAxis"/>'s axis) — ROADMAP.md row 101's "2 left and 2 right."</summary>
    [Fact]
    public void WithOutwardYAxis_Right_AddsASecondRightAxisIndependentOfSlotZero()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithSecondaryYAxis(sec => sec.SetYLabel("First right").Plot([1.0, 2.0], [100.0, 200.0]))
                .WithOutwardYAxis(AxisSide.Right, outer => outer.SetYLabel("Second right").Plot([1.0, 2.0], [1000.0, 2000.0])))
            .Build();

        var axes = figure.SubPlots[0];
        Assert.Equal(2, axes.RightYAxes.Count);
        Assert.Equal("First right", axes.RightYAxes[0].Axis.Label);
        Assert.Equal("Second right", axes.RightYAxes[1].Axis.Label);
        Assert.Single(axes.RightYAxes[1].Series);
    }

    /// <summary>Verifies that WithOutwardYAxis(Left) is reachable from the fluent API, without dropping to the
    /// raw <see cref="Models.Axes.AddOutwardYAxis"/>.</summary>
    [Fact]
    public void WithOutwardYAxis_Left_AddsALeftStackedAxis()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithOutwardYAxis(AxisSide.Left, left => left.SetYLabel("Left secondary").SetYLim(0, 1).Plot([1.0, 2.0], [0.1, 0.9])))
            .Build();

        var axes = figure.SubPlots[0];
        Assert.Single(axes.LeftYAxes);
        Assert.Equal("Left secondary", axes.LeftYAxes[0].Axis.Label);
        Assert.Equal(0, axes.LeftYAxes[0].Axis.Min);
        Assert.Equal(1, axes.LeftYAxes[0].Axis.Max);
        Assert.Single(axes.LeftYAxes[0].Series);
    }

    /// <summary>Verifies WithOutwardYAxis returns the <see cref="AxesBuilder"/> itself, so it keeps chaining.</summary>
    [Fact]
    public void WithOutwardYAxis_ReturnsAxesBuilder_ForChaining()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithOutwardYAxis(AxisSide.Left, left => left.Plot([1.0], [2.0]))
                .WithTitle("after outward"))
            .Build();

        Assert.Equal("after outward", figure.SubPlots[0].Title);
    }

    /// <summary>Verifies Scatter/StreamingPlot on <see cref="OutwardYAxisBuilder"/> reach that slot's own series
    /// list — and that calling it with no prior secondary axis creates ONE right axis, no empty ghost slot 0 before
    /// it: the configured slot IS right slot 0.</summary>
    [Fact]
    public void WithOutwardYAxis_ScatterAndStreamingPlot_AddToTheSlotsOwnSeries_NoGhostSlot()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithOutwardYAxis(AxisSide.Right, outer =>
                {
                    outer.Scatter([1.0], [2.0]);
                    outer.StreamingPlot();
                }))
            .Build();

        var axes = figure.SubPlots[0];
        var slot = Assert.Single(axes.RightYAxes);
        Assert.Equal(2, slot.Series.Count);
        Assert.IsType<ScatterSeries>(slot.Series[0]);
        Assert.IsType<MatPlotLibNet.Models.Series.Streaming.StreamingLineSeries>(slot.Series[1]);
    }

    /// <summary>Verifies the configure callback on OutwardYAxisBuilder's Plot/Scatter/StreamingPlot runs, the
    /// branch the other OutwardYAxisBuilder tests leave untaken (they all call these with no callback).</summary>
    [Fact]
    public void WithOutwardYAxis_ConfigureCallback_RunsOnEachVerb()
    {
        bool plotted = false, scattered = false, streamed = false;
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithOutwardYAxis(AxisSide.Right, outer => outer
                    .Plot([1.0], [2.0], s => plotted = true)
                    .Scatter([1.0], [2.0], s => scattered = true)))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithOutwardYAxis(AxisSide.Right, outer => outer.StreamingPlot(configure: s => streamed = true)))
            .Build();

        Assert.True(plotted);
        Assert.True(scattered);
        Assert.True(streamed);
    }

    /// <summary>Verifies WithDirectLabel on OutwardYAxisBuilder: it sets the end-label placement on a right slot (the
    /// same mechanism SecondaryAxisBuilder.WithDirectLabel uses) and on a left slot alike; both are drawn as end labels.
    /// The one right call creates right slot 0 itself (no ghost slot before it).</summary>
    [Fact]
    public void WithOutwardYAxis_WithDirectLabel_SetsThePlacement_OnEitherSide()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithOutwardYAxis(AxisSide.Right, outer => outer.Plot([1.0], [2.0]).WithDirectLabel())
                .WithOutwardYAxis(AxisSide.Left, left => left.Plot([1.0], [2.0]).WithDirectLabel()))
            .Build();

        var axes = figure.SubPlots[0];
        var rightSeries = Assert.IsAssignableFrom<LineSeries>(Assert.Single(axes.RightYAxes).Series[0]);
        var leftSeries = Assert.IsAssignableFrom<LineSeries>(axes.LeftYAxes[0].Series[0]);
        Assert.Equal(SeriesLabelPlacement.LineEnd, rightSeries.LabelPlacement);
        // The left series carries the same placement, and the end labels read it on either side
        // (YAxisSlotIdentityTests.ALeftSlotSeries_WithDirectLabel_KeepsItsName).
        Assert.Equal(SeriesLabelPlacement.LineEnd, leftSeries.LabelPlacement);
    }

    // ── SecondaryAxisBuilder ───────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SecondaryYAxis_SetYLabel_AndSetYLim_PropagateAndChain()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5))
                .WithSecondaryYAxis(s => s
                    .SetYLabel("right")
                    .SetYLim(0, 100)))
            .Build();

        var sec = fig.SubPlots[0].SecondaryYAxis!;
        Assert.Equal("right", sec.Label);
        Assert.Equal(0,   sec.Min);
        Assert.Equal(100, sec.Max);
    }

    [Fact]
    public void SecondaryYAxis_PlotAndScatter_AddSeries()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(EdgeCaseData.Ramp(5), EdgeCaseData.Sin(5))
                .WithSecondaryYAxis(s => s
                    .Plot(EdgeCaseData.Ramp(5), new[] { 10.0, 20, 30, 40, 50 }, line => line.Label = "right-line")
                    .Scatter(EdgeCaseData.Ramp(5), new[] { 1.0, 2, 3, 4, 5 })))
            .Build();

        // The Plot/Scatter calls execute without error; the secondary axis is initialised.
        // (The series may be tracked separately from the primary Series collection.)
        Assert.NotNull(fig.SubPlots[0].SecondaryYAxis);
    }

    [Fact]
    public void SecondaryAxisBuilder_Scatter_NullConfigure_NoOp()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(s => s.Scatter([1.5], [55.0], configure: null)))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].SecondarySeries);
    }

    [Fact]
    public void SecondaryAxisBuilder_Plot_NullConfigure_NoOp()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2], [3.0, 4])
                .WithSecondaryYAxis(s => s.Plot([1.5, 2.5], [55.0, 60], configure: null)))
            .Build();
        Assert.NotEmpty(fig.SubPlots[0].SecondarySeries);
    }

    /// <summary>The old builder's streaming line lands on right slot 0, the callback runs on it when one is given, and the
    /// series comes back as the handle live data is appended to.</summary>
    [Fact]
    public void SecondaryAxisBuilder_StreamingPlot_AddsAStreamingLineToRightSlotZero_AndRunsTheCallbackWhenGiven()
    {
        StreamingLineSeries plain = null!;
        StreamingLineSeries configured = null!;
        bool called = false;

        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [3.0, 1, 4, 2])
            .WithSecondaryYAxis(y =>
            {
                plain = y.StreamingPlot(8);
                configured = y.StreamingPlot(8, s => called = true);
            })).Build().SubPlots[0];

        Assert.True(called);
        Assert.Equal([plain, configured], axes.YAxes.Right[0].Series);
        Assert.Equal([plain, configured], axes.SecondarySeries);
    }

    /// <summary>The old builder's end label marks the series added last when that series can carry a placement, and
    /// changes nothing when it cannot (a scatter) or when there is no series at all.</summary>
    [Fact]
    public void SecondaryAxisBuilder_WithDirectLabel_MarksTheLastLine_AndChangesNothingForAScatterOrNoSeries()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [3.0, 1, 4, 2])
            .WithSecondaryYAxis(y => y
                .Plot(X, [10.0, 40, 20, 80], l => l.Label = "line").WithDirectLabel()
                .Scatter(X, [1.0, 2, 4, 3], s => s.Label = "dots").WithDirectLabel()))
            .Build().SubPlots[0];
        var empty = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithSecondaryYAxis(y => y.WithDirectLabel())).Build().SubPlots[0];

        Assert.Equal(SeriesLabelPlacement.LineEnd, ((LineSeries)axes.SecondarySeries[0]).LabelPlacement);
        Assert.Equal(2, axes.SecondarySeries.Count);
        Assert.Empty(empty.SecondarySeries);
    }

    /// <summary>The old property <c>SecondaryYAxis</c> is the axis of right slot 0 whichever verb created it, and
    /// <c>SecondarySeries</c> is that slot's series list: the container verb's first right axis is the old one.</summary>
    [Fact]
    public void SecondaryYAxis_AndSecondarySeries_ArePhysicallyRightSlotZero_OfTheAxisTheContainerVerbCreated()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithAddRightYAxis(first => first.Plot(X, [1.0, 2, 3, 4]))
            .WithAddRightYAxis(second => second.Plot(X, [4.0, 3, 2, 1]))).Build().SubPlots[0];

        Assert.Equal(2, axes.YAxes.Right.Count);
        Assert.Same(axes.YAxes.Right[0].Axis, axes.SecondaryYAxis);
        Assert.Same(axes.YAxes.Right[0].Series, axes.SecondarySeries);
    }

    // ── Axes.TwinX / SecondaryYAxis / SecondarySeries / AddSecondarySeries / AddOutwardYAxis ─────────────────────────

    [Fact]
    public void TwinX_CreatesTheFirstRightAxis_AsRightYAxesSlotZero()
    {
        var axes = new Axes();
        axes.TwinX();
        Assert.Single(axes.RightYAxes);
        Assert.Same(axes.SecondaryYAxis, axes.RightYAxes[0].Axis);
        Assert.Equal(AxisSide.Right, axes.RightYAxes[0].Side);
    }

    [Fact]
    public void TwinX_CalledTwice_StaysAtOneRightAxis()
    {
        var axes = new Axes();
        axes.TwinX();
        var first = axes.SecondaryYAxis;
        axes.TwinX();
        Assert.Single(axes.RightYAxes);
        Assert.Same(first, axes.SecondaryYAxis);
    }

    [Fact]
    public void AddOutwardYAxis_Right_CreatesASecondRightAxis_DistinctFromSecondaryYAxis()
    {
        var axes = new Axes();
        axes.TwinX();
        var second = axes.AddOutwardYAxis(AxisSide.Right);

        Assert.Equal(2, axes.RightYAxes.Count);
        Assert.NotSame(axes.SecondaryYAxis, second.Axis);
        Assert.Equal(AxisSide.Right, second.Side);
    }

    /// <summary>No ghost slot: on an axes with no right axis yet, the first outward call creates ONE axis, and that
    /// axis is right slot 0 — not an empty slot 0 plus the caller's axis past it.</summary>
    [Fact]
    public void AddOutwardYAxis_Right_WithoutTwinXFirst_CreatesOneSlot_NoGhost()
    {
        var axes = new Axes();
        var slot = axes.AddOutwardYAxis(AxisSide.Right);

        Assert.Single(axes.RightYAxes);
        Assert.Same(axes.SecondaryYAxis, slot.Axis);
    }

    /// <summary>No cap: every call stacks one more axis past the ones before it.</summary>
    [Fact]
    public void AddOutwardYAxis_Right_CalledThreeTimes_StacksThree()
    {
        var axes = new Axes();
        var first = axes.AddOutwardYAxis(AxisSide.Right);
        var second = axes.AddOutwardYAxis(AxisSide.Right);
        var third = axes.AddOutwardYAxis(AxisSide.Right);

        Assert.Equal(3, axes.RightYAxes.Count);
        Assert.Equal([first.Axis, second.Axis, third.Axis], axes.RightYAxes.Select(s => s.Axis));
        Assert.Equal(3, new[] { first.Axis, second.Axis, third.Axis }.Distinct().Count());
    }

    [Fact]
    public void AddOutwardYAxis_Left_CreatesOneLeftAxis_TheOwnersTwoLeftTwoRight()
    {
        var axes = new Axes();
        var left = axes.AddOutwardYAxis(AxisSide.Left);

        Assert.Single(axes.LeftYAxes);
        Assert.Equal(AxisSide.Left, left.Side);
    }

    /// <summary>No cap on the left either: each call stacks past the primary and the axes before it.</summary>
    [Fact]
    public void AddOutwardYAxis_Left_CalledTwice_StacksTwo()
    {
        var axes = new Axes();
        var first = axes.AddOutwardYAxis(AxisSide.Left);
        var second = axes.AddOutwardYAxis(AxisSide.Left);

        Assert.Equal(2, axes.LeftYAxes.Count);
        Assert.Equal([first.Axis, second.Axis], axes.LeftYAxes.Select(s => s.Axis));
        Assert.NotSame(first.Axis, second.Axis);
        Assert.DoesNotContain(axes.YAxis, axes.LeftYAxes.Select(s => s.Axis));
    }

    [Fact]
    public void SecondaryAxisSlot_AddSeries_AddsToThatSlotsOwnList_NotToSecondaryYAxisSlotZero()
    {
        var axes = new Axes();
        axes.TwinX();
        var second = axes.AddOutwardYAxis(AxisSide.Right);
        second.AddSeries(new LineSeries([0.0, 1], [0.0, 1]));

        Assert.Empty(axes.SecondarySeries);
        Assert.Single(second.Series);
    }

    /// <summary>AddSecondarySeries still targets right slot 0 — and with no ghost slot, right slot 0 is the axis the
    /// first outward call created, so the series lands on that axis and the one stacked past it stays empty.</summary>
    [Fact]
    public void AddSecondarySeries_TargetsRightSlotZero_TheAxisTheFirstOutwardCallCreated_NoGhost()
    {
        var axes = new Axes();
        var first = axes.AddOutwardYAxis(AxisSide.Right);
        axes.AddOutwardYAxis(AxisSide.Right);
        var series = axes.AddSecondarySeries(new LineSeries([0.0, 1], [0.0, 1]));

        Assert.Equal(2, axes.RightYAxes.Count);
        Assert.Same(series, Assert.Single(first.Series));
        Assert.Same(series, Assert.Single(axes.SecondarySeries));
        Assert.Empty(axes.RightYAxes[1].Series);
    }

    // ── RightYAxes / LeftYAxes / SecondaryAxisSlot: the published views over the container ─────────────────────────

    private static LineSeries Line(string label) => new([0.0, 1], [0.0, 1]) { Label = label };

    [Fact]
    public void RightYAxes_SharesAxisAndSeriesWithTheContainer()
    {
        var axes = new Axes();
        var slot = axes.YAxes.Right.Add();

        var projected = Assert.Single(axes.RightYAxes);
        var series = projected.AddSeries(Line("r0"));

        Assert.Same(slot.Axis, projected.Axis);
        Assert.Equal(AxisSide.Right, projected.Side);
        Assert.Same(series, Assert.Single(slot.Series));
        Assert.Same(slot.Axis, axes.SecondaryYAxis);
        Assert.Same(slot.Series, axes.SecondarySeries);
    }

    [Fact]
    public void LeftYAxes_IsOutwardOnly_AndSharesAxisAndSeriesWithTheContainer()
    {
        var axes = new Axes();
        var slot = axes.YAxes.Left.Add();

        var projected = Assert.Single(axes.LeftYAxes);
        var series = projected.AddSeries(Line("l0"));

        Assert.Same(slot.Axis, projected.Axis);
        Assert.NotSame(axes.YAxis, projected.Axis);
        Assert.Same(series, Assert.Single(slot.Series));
        Assert.Empty(axes.Series);
    }

    [Fact]
    public void AProjection_IsALiveView_LikeTheListItReplaces()
    {
        var axes = new Axes();
        var right = axes.RightYAxes;
        var left = axes.LeftYAxes;

        axes.YAxes.Right.Add();
        axes.YAxes.Right.Add();
        axes.YAxes.Left.Add();

        var untyped = new List<object>();
        foreach (var slot in (IEnumerable)left) untyped.Add(slot);

        Assert.Equal(2, right.Count);
        Assert.Single(left);
        Assert.Equal(axes.YAxes.Right.Select(s => s.Axis), right.Select(s => s.Axis));
        Assert.Same(axes.YAxes.Left[1].Axis, Assert.IsType<SecondaryAxisSlot>(Assert.Single(untyped)).Axis);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void AProjection_IndexPastEitherEnd_Throws_LikeTheListItReplaces(int index)
    {
        var axes = new Axes();
        axes.YAxes.Left.Add();

        Assert.Throws<ArgumentOutOfRangeException>(() => axes.LeftYAxes[index]);
    }

    [Fact]
    public void TwoProjectionsOfOneSlot_AreEqual()
    {
        var axes = new Axes();
        axes.YAxes.Right.Add();

        Assert.Equal(axes.RightYAxes[0], axes.RightYAxes[0]);
    }

    [Fact]
    public void SecondaryYAxis_WithNoRightSlot_IsNull_AndItsSeriesEmpty()
    {
        var axes = new Axes();

        Assert.Null(axes.SecondaryYAxis);
        Assert.Empty(axes.SecondarySeries);
    }

    [Fact]
    public void TwinX_IsTheRightStacksFirstOrAdd()
    {
        var axes = new Axes();

        axes.TwinX();
        axes.TwinX();

        Assert.Same(axes.YAxes.Right[0].Axis, Assert.Single(axes.RightYAxes).Axis);
    }

    [Fact]
    public void AddOutwardYAxis_Left_StacksPastThePrimary_NeverReturnsIt()
    {
        var axes = new Axes();

        var slot = axes.AddOutwardYAxis(AxisSide.Left);

        Assert.Same(axes.YAxes.Left[1].Axis, slot.Axis);
        Assert.NotSame(axes.YAxis, slot.Axis);
    }

    // ── PlotSecondary / ScatterSecondary ────────────────────────────────────────────────────────────────────────────────

    /// <summary>The old Axes verbs add their series to THE first right axis and create it when there is none; neither ever
    /// stacks another, and the primary axis' own series stay untouched.</summary>
    [Fact]
    public void PlotSecondary_AndScatterSecondary_AddToRightSlotZero_CreatingItOnce()
    {
        var axes = new Axes();

        var line = axes.PlotSecondary(X, [1.0, 2, 3, 4]);
        var points = axes.ScatterSecondary(X, [4.0, 3, 2, 1]);

        var slot = Assert.Single(axes.YAxes.Right);
        Assert.Equal<ISeries>([line, points], slot.Series);
        Assert.Same(slot.Axis, axes.SecondaryYAxis);
        Assert.Empty(axes.Series);
    }

    /// <summary>With right axes already stacked, the verbs reach the nearest one and add no further axis.</summary>
    [Fact]
    public void PlotSecondary_AndScatterSecondary_WithRightAxesStacked_ReachTheFirstAndAddNone()
    {
        var axes = new Axes();
        axes.YAxes.Right.Add();
        axes.YAxes.Right.Add();

        var line = axes.PlotSecondary(X, [1.0, 2, 3, 4]);
        var points = axes.ScatterSecondary(X, [4.0, 3, 2, 1]);

        Assert.Equal(2, axes.YAxes.Right.Count);
        Assert.Equal<ISeries>([line, points], axes.YAxes.Right[0].Series);
        Assert.Empty(axes.YAxes.Right[1].Series);
    }

    /// <summary>Arrays that differ in length are refused before the right axis is touched: no axis appears for a verb that
    /// failed.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PlotSecondary_AndScatterSecondary_MisalignedArrays_ThrowAndCreateNoRightAxis(bool scatter)
    {
        var axes = new Axes();

        Assert.Throws<ArgumentException>(() => { if (scatter) axes.ScatterSecondary(X, [1.0]); else axes.PlotSecondary(X, [1.0]); });

        Assert.Empty(axes.YAxes.Right);
    }

    // ── AddSecondarySeries / TwinX: what they return and what a failed call leaves behind ───────────────────────────────

    [Fact]
    public void AddSecondarySeries_WithNoRightAxis_CreatesItAsTwinXDoes_AndReturnsTheSameSeries()
    {
        var axes = new Axes();
        var line = Line("only");

        var returned = axes.AddSecondarySeries(line);

        Assert.Same(line, returned);
        Assert.Same(line, Assert.Single(Assert.Single(axes.YAxes.Right).Series));
    }

    [Fact]
    public void AddSecondarySeries_NullSeries_Throws_AndCreatesNoRightAxis()
    {
        var axes = new Axes();

        Assert.Throws<ArgumentNullException>(() => axes.AddSecondarySeries<LineSeries>(null!));

        Assert.Empty(axes.YAxes.Right);
    }

    [Fact]
    public void TwinX_ReturnsTheAxesItself_ForChaining()
    {
        var axes = new Axes();

        Assert.Same(axes, axes.TwinX());
    }

    // ── SecondaryAxisSlot: a value view of one container slot ──────────────────────────────────────────────────────────

    [Fact]
    public void SecondaryAxisSlot_AddSeries_Null_Throws_AndAddsNothing()
    {
        var axes = new Axes();
        var slot = axes.AddOutwardYAxis(AxisSide.Right);

        Assert.Throws<ArgumentNullException>(() => slot.AddSeries<LineSeries>(null!));

        Assert.Empty(axes.YAxes.Right[0].Series);
    }

    /// <summary>Two views of one slot are equal and hash alike; views of two slots are not, even with the same side.</summary>
    [Fact]
    public void SecondaryAxisSlot_IsEqualPerSlot_NotPerSide()
    {
        var axes = new Axes();
        axes.YAxes.Right.Add();
        axes.YAxes.Right.Add();

        var first = axes.RightYAxes[0];
        var again = axes.RightYAxes[0];
        var second = axes.RightYAxes[1];

        Assert.True(first == again);
        Assert.Equal(first.GetHashCode(), again.GetHashCode());
        Assert.True(first != second);
        Assert.False(first.Equals((object)second));
    }

    // ── The old builders on a stack: which axis a callback reaches ──────────────────────────────────────────────────────

    /// <summary>The published meaning, with axes stacked first: WithSecondaryYAxis still configures right slot 0 and adds
    /// no axis, so existing code that styles "the" right axis after other code stacked more keeps styling the same one.</summary>
    [Fact]
    public void WithSecondaryYAxis_AfterRightAxesWereStacked_ConfiguresSlotZero_AndAddsNone()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithAddRightYAxis(a => a.SetYLabel("zero"))
            .WithAddRightYAxis(b => b.SetYLabel("one"))
            .WithSecondaryYAxis(y => y.SetYLabel("renamed").Plot(X, [1.0, 2, 3, 4])))
            .Build().SubPlots[0];

        Assert.Equal(2, axes.YAxes.Right.Count);
        Assert.Equal("renamed", axes.YAxes.Right[0].Axis.Label);
        Assert.Equal("one", axes.YAxes.Right[1].Axis.Label);
        Assert.Single(axes.YAxes.Right[0].Series);
        Assert.Empty(axes.YAxes.Right[1].Series);
    }

    /// <summary>WithOutwardYAxis(Right) stacks one more every call, past the ones the container verb added, and its
    /// builder reaches that new axis, never right slot 0.</summary>
    [Fact]
    public void WithOutwardYAxis_Right_AfterRightAxesWereStacked_ReachesTheNewOutermostAxis()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .WithAddRightYAxis(a => a.SetYLabel("zero"))
            .WithOutwardYAxis(AxisSide.Right, o => o.SetYLabel("one").Plot(X, [1.0, 2, 3, 4]))
            .WithOutwardYAxis(AxisSide.Right, o => o.SetYLabel("two")))
            .Build().SubPlots[0];

        Assert.Equal(["zero", "one", "two"], axes.YAxes.Right.Select(slot => slot.Axis.Label));
        Assert.Single(axes.YAxes.Right[1].Series);
        Assert.Empty(axes.YAxes.Right[0].Series);
    }

    /// <summary>The old builder's colour verb colours the right axis' ticks, tick labels and title and the RIGHT spine, and
    /// nothing on the left.</summary>
    [Fact]
    public void SecondaryAxisBuilder_SetYAxisColor_ColoursTheRightAxisAndTheRightSpineOnly()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .Plot(X, [3.0, 1, 4, 2])
            .WithSecondaryYAxis(y => y.SetYLabel("Rate").SetYAxisColor(Red).Plot(X, [10.0, 40, 20, 80])))
            .Build().SubPlots[0];

        var right = axes.SecondaryYAxis!;
        Assert.Equal<Color?>(Red, right.MajorTicks.Color);
        Assert.Equal<Color?>(Red, right.MajorTicks.LabelColor);
        Assert.Equal<Color?>(Red, right.LabelStyle?.Color);
        Assert.Equal<Color?>(Red, axes.Spines.Right.Color);
        Assert.NotEqual<Color?>(Red, axes.Spines.Left.Color);
        Assert.NotEqual<Color?>(Red, axes.YAxis.MajorTicks.Color);
    }
}
