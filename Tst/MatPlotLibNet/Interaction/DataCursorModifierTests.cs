// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Tests.Interaction.Modifiers;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>Phase H.4 of the v1.7.2 follow-on plan — behavioural coverage for
/// <see cref="DataCursorModifier"/>. Pre-H.4 the modifier didn't exist; the
/// toolbar "cursor" button and <see cref="DataCursorEvent"/> / <see cref="PinnedAnnotation"/>
/// records were orphaned. H.4 adds the modifier + wires it into the controller.
///
/// <para>Contract: plain left-click within <see cref="DataCursorModifier.HitRadiusPx"/>
/// of a data point emits a <see cref="DataCursorEvent"/>. Clicks that don't hit
/// any point defer to the next modifier (pan) by returning <c>false</c> from
/// <see cref="DataCursorModifier.HandlesPointerPressed"/>.</para></summary>
public class DataCursorModifierTests
{
    private static (DataCursorModifier mod, Figure figure, List<FigureInteractionEvent> events) Setup()
    {
        var figure = Plt.Create().Plot([0.0, 1.0, 2.0], [0.0, 5.0, 10.0]).Build();
        figure.ChartId = "c";
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 2;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 10;
        var layout = ChartLayout.Create(figure, [new Rect(0, 0, 200, 100)]);
        var events = new List<FigureInteractionEvent>();
        return (new DataCursorModifier("c", layout, figure, events.Add), figure, events);
    }

    /// <summary>A modifier on the plain figure of <see cref="FigureLayout"/>, with the events it emits.</summary>
    private readonly record struct OnTheFigure(DataCursorModifier Modifier, List<FigureInteractionEvent> Events);

    private static OnTheFigure OnThePlainFigure()
    {
        var (fig, layout) = FigureLayout.Plain();
        var events = new List<FigureInteractionEvent>();
        return new OnTheFigure(new DataCursorModifier("c1", layout, fig, events.Add), events);
    }

    private static PointerInputArgs P(double x, double y, ModifierKeys mods = ModifierKeys.None) =>
        new(x, y, PointerButton.Left, mods);

    [Fact]
    public void HandlesPointerPressed_OnDataPoint_ReturnsTrue()
    {
        var (mod, _, _) = Setup();
        // Data point (0, 0) projects to pixel (0, 100). Click right at it.
        Assert.True(mod.HandlesPointerPressed(P(0, 100)));
    }

    /// <summary>The middle of the plot has no data point nearby; (300, 200) is outside the plot; (115, 50) is 15 px from data point (1, 5) at
    /// pixel (100, 50), near enough to be found (the finder looks within 20 px) but past the hit radius of 10 px.</summary>
    [Theory]
    [InlineData(60, 50)]
    [InlineData(300, 200)]
    [InlineData(115, 50)]
    public void HandlesPointerPressed_AtAPointThatHitsNothing_ReturnsFalse(double x, double y)
    {
        var (mod, _, _) = Setup();
        Assert.False(mod.HandlesPointerPressed(P(x, y)));
    }

    [Fact]
    public void HandlesPointerPressed_WithModifierKey_ReturnsFalse()
    {
        var (mod, _, _) = Setup();
        Assert.False(mod.HandlesPointerPressed(P(0, 100, ModifierKeys.Ctrl)));
    }

    [Fact]
    public void OnPointerPressed_AfterHit_EmitsDataCursorEventWithPinnedAnnotation()
    {
        var (mod, _, events) = Setup();
        mod.HandlesPointerPressed(P(0, 100));   // primes _pendingHit
        mod.OnPointerPressed(P(0, 100));

        Assert.Single(events);
        var evt = Assert.IsType<DataCursorEvent>(events[0]);
        Assert.Equal("c", evt.ChartId);
        Assert.Equal(0.0, evt.Annotation.DataX, 3);
        Assert.Equal(0.0, evt.Annotation.DataY, 3);
    }

    [Fact]
    public void OnPointerPressed_WithoutPriorHit_IsNoop()
    {
        var (mod, _, events) = Setup();
        // Don't call HandlesPointerPressed — simulate Pan taking the event instead.
        mod.OnPointerPressed(P(60, 50));
        Assert.Empty(events);
    }

    /// <summary>nearest.PixelDistance &lt;= HitRadiusPx → don't return false,
    /// store _pendingHit. Click 4 px away from data point (1, 5) → pixel (100, 50),
    /// so click at (104, 50) is within HitRadiusPx=10.</summary>
    [Fact]
    public void HandlesPointerPressed_WithinHitRadius_ReturnsTrueAndSetsPending()
    {
        var (mod, _, _) = Setup();
        // Data point (1, 5): X=1/2*200=100, Y=100-(5/10*100)=50. Click at (104, 50) = 4px away.
        Assert.True(mod.HandlesPointerPressed(P(104, 50)));
    }

    // ── Phase J coverage additions ────────────────────────────────────────────

    /// <summary>Button != Left → return false immediately.</summary>
    [Fact]
    public void HandlesPointerPressed_RightButton_ReturnsFalse()
    {
        var (mod, _, _) = Setup();
        Assert.False(mod.HandlesPointerPressed(
            new PointerInputArgs(0, 100, PointerButton.Right, ModifierKeys.None)));
    }

    /// <summary>nearest is null (subplot exists but has no series) → return false.</summary>
    [Fact]
    public void HandlesPointerPressed_NoSeries_NearestIsNull_ReturnsFalse()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, _ => { }).Build();
        figure.ChartId = "c";
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 1;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 1;
        var layout = ChartLayout.Create(figure, [new Rect(0, 0, 200, 100)]);
        var mod = new DataCursorModifier("c", layout, figure, _ => { });
        // Pixel (100, 50) is inside the layout rect → axesIndex non-null.
        // NearestPointFinder returns null because there are no series.
        Assert.False(mod.HandlesPointerPressed(P(100, 50)));
    }
    // ── the click says which point, not only where ─────────────────────────────

    [Fact]
    public void TheClick_CarriesThePointIndexAndTheSeriesIndex()
    {
        var (mod, _, events) = Setup();

        Assert.True(mod.HandlesPointerPressed(P(100, 50)));
        mod.OnPointerPressed(P(100, 50));

        var pinned = Assert.IsType<DataCursorEvent>(Assert.Single(events)).Annotation;
        Assert.Equal(1, pinned.PointIndex);
        Assert.Equal(0, pinned.SeriesIndex);
    }

    [Fact]
    public void AnAnnotationConstructedWithoutIndices_SaysSoRatherThanPointingAtTheFirstPoint()
    {
        var annotation = new PinnedAnnotation("s", 1.0, 2.0, 3.0, 4.0, 0);

        Assert.Equal(-1, annotation.PointIndex);
        Assert.Equal(-1, annotation.SeriesIndex);
    }

    /// <summary>DataCursorModifier — `axesIndex is null` arm: hover OUTSIDE
    /// any axes returns null from HitTestAxes, modifier short-circuits.</summary>
    [Fact]
    public void DataCursor_OnPointerMoved_Outside_HitsAxesNull()
    {
        var (m, events) = OnThePlainFigure();
        m.OnPointerMoved(new PointerInputArgs(500, 500, PointerButton.None, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>DataCursorModifier — successful click within HitRadiusPx of data
    /// point (1, 3). Lifts:
    /// - nearest is non-null AND within HitRadiusPx (false arm of the miss check)
    /// - `_pendingHit = nearest`
    /// - `_pendingHit is not null` (false arm)
    /// - annotation creation + sink call
    ///
    /// Layout maths: axes [0,10]×[0,5] mapped to pixel rect (10,10)–(110,60).
    /// Data point (1, 3) projects to ≈ (20, 30) — click there.</summary>
    [Fact]
    public void DataCursor_ClickOnDataPoint_EmitsDataCursorEvent()
    {
        var (m, events) = OnThePlainFigure();
        var press = new PointerInputArgs(20, 40, PointerButton.Left, ModifierKeys.None);
        Assert.True(m.HandlesPointerPressed(press));
        m.OnPointerPressed(press);
        Assert.Single(events);
        Assert.IsType<DataCursorEvent>(events[0]);
    }

    /// <summary>DataCursorModifier — second OnPointerPressed without a prior
    /// successful HandlesPointerPressed (clears `_pendingHit`). Pins the true
    /// arm (`_pendingHit is null`).</summary>
    [Fact]
    public void DataCursor_OnPointerPressed_WithoutPriorHit_NoEvent()
    {
        var (m, events) = OnThePlainFigure();
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>DataCursorModifier — `args.Button != Left || args.Modifiers != None`
    /// short-circuit. Two facts pin both sides of the OR:
    /// - Right + None (button false → modifier short-circuited at the ||)
    /// - Left + Ctrl  (button true on first operand, second operand evaluated)
    /// The first arm is already covered by ResetModifier-style probes; the second
    /// is the one this fact pins (was the 75% (3/4) gap).</summary>
    [Fact]
    public void DataCursor_HandlesPointerPressed_LeftWithModifier_ReturnsFalse()
    {
        var (m, _) = OnThePlainFigure();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.Ctrl)));
    }

    /// <summary>DataCursorModifier no-op methods: every empty/false-return method
    /// is called and returns its neutral result.
    /// (DataCursor isn't in <see cref="ModifierTestBase{T}"/> because of its 4-arg
    /// constructor, so the generic OnScroll/OnKeyDown probe doesn't reach it.)</summary>
    [Fact]
    public void DataCursor_NoOpMethods_DoNotThrow()
    {
        var (m, _) = OnThePlainFigure();
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        Assert.False(m.HandlesScroll(new ScrollInputArgs(60, 35, 0, -1)));
        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));
        const string EscapeKey = "Escape";
        Assert.False(m.HandlesKeyDown(new KeyInputArgs(EscapeKey, ModifierKeys.None)));
        m.OnKeyDown(new KeyInputArgs(EscapeKey, ModifierKeys.None));
    }
}
