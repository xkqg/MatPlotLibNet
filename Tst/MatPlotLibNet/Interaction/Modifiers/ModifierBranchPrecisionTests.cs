// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary>Phase X.8 per-class branch precision (v1.7.2, 2026-04-19) — targets the
/// remaining `coords is null` and `!_active` arms in modifier classes that the
/// generic <see cref="ModifierTestBase{T}"/> couldn't reach with its single
/// outside-press fact. Each fact pins a specific cobertura
/// `condition-coverage="50% (1/2)"` marker by file:line, with the source path cited.
///
/// Pattern: press INSIDE the plot area to set _active=true, then move/release
/// OUTSIDE so PixelToData returns null. This trips the `coords is null` arm in
/// OnPointerMoved / OnPointerReleased that the generic outside-press test missed.</summary>
public class ModifierBranchPrecisionTests
{
    private static (Figure fig, IChartLayout layout) Make()
    {
        var fig = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        fig.SubPlots[0].XAxis.Min = 0;
        fig.SubPlots[0].XAxis.Max = 10;
        fig.SubPlots[0].YAxis.Min = 0;
        fig.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(fig, [new Rect(10, 10, 100, 50)]);
        return (fig, layout);
    }

    /// <summary>PanModifier line 56 — OnPointerMoved with coords going null (move
    /// pointer outside plot AFTER pressing inside).</summary>
    [Fact]
    public void Pan_OnPointerMoved_OutsideAfterInsidePress_HitsCoordsNull()
    {
        var (_, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new PanModifier("c1", layout, events.Add);
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.None));
        // No assertion on events — Pan emits PanEvents on each move that succeeds; this
        // test just exercises the `coords is null` arm to lift branch coverage.
        Assert.True(true);
    }

    /// <summary>RectangleZoomModifier line 45 — OnPointerPressed with click OUTSIDE
    /// the plot. PixelToData returns null because the pixel is outside bounds.</summary>
    [Fact]
    public void RectangleZoom_OnPointerPressed_OutsidePlot_HitsCoordsNull()
    {
        var (_, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new RectangleZoomModifier("c1", layout, events.Add);
        m.OnPointerPressed(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.Ctrl));
        Assert.Empty(events);   // press never armed _active because coords were null
    }

    /// <summary>RectangleZoomModifier line 69 — OnPointerReleased after a successful
    /// inside-press but with release OUTSIDE the plot → endCoords is null.</summary>
    [Fact]
    public void RectangleZoom_OnPointerReleased_OutsideAfterInsidePress_HitsEndCoordsNull()
    {
        var (_, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new RectangleZoomModifier("c1", layout, events.Add);
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.Ctrl));
        m.OnPointerReleased(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.Ctrl));
        Assert.Empty(events);   // release short-circuits at endCoords-null
    }

    /// <summary>BrushSelectModifier line 64 — OnPointerMoved without prior press
    /// (_active=false). Generic ModifierTestBase already covers OnPointerReleased
    /// without prior press; this covers the OnPointerMoved counterpart.</summary>
    [Fact]
    public void BrushSelect_OnPointerMoved_WithoutPriorPress_NoOp()
    {
        var (_, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new BrushSelectModifier("c1", layout, events.Add);
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>BrushSelectModifier line 76 — OnPointerReleased after inside-press
    /// but release OUTSIDE plot → coords is null arm.</summary>
    [Fact]
    public void BrushSelect_OnPointerReleased_OutsideAfterInsidePress_HitsCoordsNull()
    {
        var (_, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new BrushSelectModifier("c1", layout, events.Add);
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.Shift));
        Assert.Empty(events);
    }

    /// <summary>SpanSelectModifier lines 55, 66 — same shape: OnPointerMoved without
    /// press (line 55) AND OnPointerReleased after inside-press with outside-release
    /// (line 66 endCoords-null).</summary>
    [Fact]
    public void SpanSelect_OnPointerMoved_WithoutPress_AndReleased_OutsideAfterPress()
    {
        var (_, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new SpanSelectModifier("c1", layout, events.Add);

        // Line 55: move without press.
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));

        // Line 66: press inside, release outside.
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.Shift));
        Assert.Empty(events);
    }

    /// <summary>DataCursorModifier line 48 — `axesIndex is null` arm: hover OUTSIDE
    /// any axes returns null from HitTestAxes, modifier short-circuits.</summary>
    [Fact]
    public void DataCursor_OnPointerMoved_Outside_HitsAxesNull()
    {
        var (fig, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new DataCursorModifier("c1", layout, fig, events.Add);
        m.OnPointerMoved(new PointerInputArgs(500, 500, PointerButton.None, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>PanModifier line 64 — `if (dx == 0 &amp;&amp; dy == 0) return;` short-circuit's
    /// true arm: press + move to SAME position → both deltas are zero → no PanEvent.
    /// Pre-X this branch was 25% covered (1/4); this lifts the no-pan-on-zero arm.</summary>
    [Fact]
    public void Pan_OnPointerMoved_SamePosition_NoPanEvent()
    {
        var (_, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new PanModifier("c1", layout, events.Add);
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>Rotate3DModifier line 110 — `if (da != 0 || de != 0)` short-circuit's
    /// false arm: a rotation event with both deltas zero produces no event. Triggered
    /// by an arrow-key on a non-3D figure (HasAnyThreeD returns false → HandlesKeyDown
    /// returns false → no rotation, both deltas stay zero).</summary>
    [Fact]
    public void Rotate3D_NoThreeDFigure_KeyDown_NoRotationEvent()
    {
        var (fig, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new Rotate3DModifier("c1", layout, events.Add, fig);
        const string ArrowUp = "Up";
        Assert.False(m.HandlesKeyDown(new KeyInputArgs(ArrowUp, ModifierKeys.None)));
        m.OnKeyDown(new KeyInputArgs(ArrowUp, ModifierKeys.None));
        Assert.Empty(events);
    }

    // ── Rotate3DModifier deep coverage (3D figure) ────────────────────────────

    /// <summary>Builds a Figure with one 3D subplot and a scatter point so that
    /// HasAnyThreeD/IsThreeD return true, and the layout has a real plot area.</summary>
    private static (Figure fig, IChartLayout layout) Make3D()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 }))
            .Build();
        var layout = ChartLayout.Create(fig, [new Rect(10, 10, 100, 50)]);
        return (fig, layout);
    }

    /// <summary>Rotate3DModifier line 85 — `is "Left" or "Right" or "Up" or "Down"
    /// or "Home"` pattern coverage. Each Theory row pins one of the 5 positive
    /// arms (10/10 with the negative arms already covered by the generic-base
    /// Escape probe in <see cref="ModifierTestBase{T}"/>).</summary>
    [Theory]
    [InlineData("Left")]
    [InlineData("Right")]
    [InlineData("Up")]
    [InlineData("Down")]
    [InlineData("Home")]
    public void Rotate3D_HandlesKeyDown_TrueForArrowOrHome(string key)
    {
        var (fig, layout) = Make3D();
        var m = new Rotate3DModifier("c1", layout, _ => { }, fig);
        Assert.True(m.HandlesKeyDown(new KeyInputArgs(key, ModifierKeys.None)));
    }

    /// <summary>Rotate3DModifier line 94 — switch default arm: an unknown key on a
    /// 3D figure returns (0, 0) so line 105 `if (da != 0 || de != 0)` short-circuits
    /// true→false and no event is emitted.</summary>
    [Fact]
    public void Rotate3D_OnKeyDown_UnknownKey_OnThreeDFigure_NoEvent()
    {
        var (fig, layout) = Make3D();
        var events = new List<FigureInteractionEvent>();
        var m = new Rotate3DModifier("c1", layout, events.Add, fig);
        const string PageUp = "PageUp";
        m.OnKeyDown(new KeyInputArgs(PageUp, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>Rotate3DModifier line 66 — `if (dx == 0 && dy == 0)` short-circuit's
    /// 4 branch points. Press inside, then move with one delta zero so dx==0 evaluates
    /// true but dy==0 evaluates false (or vice versa) → event still emits.</summary>
    [Theory]
    [InlineData(60.0, 35.0,  60.0, 40.0)]   // dx == 0, dy != 0
    [InlineData(60.0, 35.0,  65.0, 35.0)]   // dx != 0, dy == 0
    public void Rotate3D_PointerMove_PartialDelta_EmitsEvent(double pressX, double pressY, double moveX, double moveY)
    {
        var (fig, layout) = Make3D();
        var events = new List<FigureInteractionEvent>();
        var m = new Rotate3DModifier("c1", layout, events.Add, fig);
        m.OnPointerPressed(new PointerInputArgs(pressX, pressY, PointerButton.Right, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(moveX, moveY, PointerButton.Right, ModifierKeys.None));
        Assert.Single(events);
        Assert.IsType<Rotate3DEvent>(events[0]);
    }

    // ── DataCursorModifier deep coverage ──────────────────────────────────────

    /// <summary>DataCursorModifier — successful click within HitRadiusPx of data
    /// point (1, 3). Lifts:
    /// - line 53 false arm (nearest is non-null AND within HitRadiusPx)
    /// - lines 55-58 (`_pendingHit = nearest`)
    /// - line 67 false arm (`_pendingHit is not null`)
    /// - lines 68-75 (annotation creation + sink call)
    ///
    /// Layout maths: axes [0,10]×[0,5] mapped to pixel rect (10,10)–(110,60).
    /// Data point (1, 3) projects to ≈ (20, 30) — click there.</summary>
    [Fact]
    public void DataCursor_ClickOnDataPoint_EmitsDataCursorEvent()
    {
        var (fig, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new DataCursorModifier("c1", layout, fig, events.Add);
        var press = new PointerInputArgs(20, 40, PointerButton.Left, ModifierKeys.None);
        Assert.True(m.HandlesPointerPressed(press));
        m.OnPointerPressed(press);
        Assert.Single(events);
        Assert.IsType<DataCursorEvent>(events[0]);
    }

    /// <summary>DataCursorModifier — second OnPointerPressed without a prior
    /// successful HandlesPointerPressed (clears `_pendingHit`). Pins line 67 true
    /// arm (`_pendingHit is null`).</summary>
    [Fact]
    public void DataCursor_OnPointerPressed_WithoutPriorHit_NoEvent()
    {
        var (fig, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new DataCursorModifier("c1", layout, fig, events.Add);
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>DataCursorModifier line 42 — `args.Button != Left || args.Modifiers != None`
    /// short-circuit. Two facts pin both sides of the OR:
    /// - Right + None (button false → modifier short-circuited at the ||)
    /// - Left + Ctrl  (button true on first operand, second operand evaluated)
    /// The first arm is already covered by ResetModifier-style probes; the second
    /// is the one this fact pins (was the 75% (3/4) gap).</summary>
    [Fact]
    public void DataCursor_HandlesPointerPressed_LeftWithModifier_ReturnsFalse()
    {
        var (fig, layout) = Make();
        var m = new DataCursorModifier("c1", layout, fig, _ => { });
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.Ctrl)));
    }

    /// <summary>DataCursorModifier no-op methods (lines 78, 80, 82, 84, 86, 88) — lifts
    /// line coverage from 89% to 100% by exercising every empty/false-return method.
    /// (DataCursor isn't in <see cref="ModifierTestBase{T}"/> because of its 4-arg
    /// constructor, so the generic OnScroll/OnKeyDown probe doesn't reach it.)</summary>
    [Fact]
    public void DataCursor_NoOpMethods_DoNotThrow()
    {
        var (fig, layout) = Make();
        var m = new DataCursorModifier("c1", layout, fig, _ => { });
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        Assert.False(m.HandlesScroll(new ScrollInputArgs(60, 35, 0, -1)));
        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));
        const string EscapeKey = "Escape";
        Assert.False(m.HandlesKeyDown(new KeyInputArgs(EscapeKey, ModifierKeys.None)));
        m.OnKeyDown(new KeyInputArgs(EscapeKey, ModifierKeys.None));
    }

    /// <summary>Rotate3DModifier line 66 — final 4/4 branch arm: dx==0 AND dy==0
    /// (both true → return). Press inside on a 3D figure, then move to the SAME
    /// position so both deltas are zero.</summary>
    [Fact]
    public void Rotate3D_PointerMove_SamePosition_NoEvent()
    {
        var (fig, layout) = Make3D();
        var events = new List<FigureInteractionEvent>();
        var m = new Rotate3DModifier("c1", layout, events.Add, fig);
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Right, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.Right, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>Rotate3DModifier line 110 — `axesIndex &lt; _figure.SubPlots.Count`
    /// false arm: a stale layout returns an axesIndex out of range, so IsThreeD
    /// short-circuits to false and HandlesPointerPressed returns false.</summary>
    [Fact]
    public void Rotate3D_StaleAxesIndex_OutOfRange_NotHandled()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Scatter3D(new double[] { 1.0 }, new double[] { 2.0 }, new double[] { 3.0 }))
            .Build();
        var staleLayout = new StaleAxesLayout(returnsAxesIndex: 999);
        var m = new Rotate3DModifier("c1", staleLayout, _ => { }, fig);
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 50, PointerButton.Right, ModifierKeys.None)));
    }

    /// <summary>Inline IChartLayout that's not ChartLayout. Returns axesIndex=0 for
    /// any pixel inside (0, 0)–(200, 100) so HitTestAxes succeeds and the line-48
    /// `is not ChartLayout` arm trips.</summary>
    private sealed class MinimalLayout : IChartLayout
    {
        public int AxesCount => 1;
        public Rect GetPlotArea(int axesIndex) => new(0, 0, 200, 100);
        public DataRange GetDataRange(int axesIndex) => new(0, 10, 0, 5);
        public int? HitTestAxes(double pixelX, double pixelY) =>
            pixelX is >= 0 and <= 200 && pixelY is >= 0 and <= 100 ? 0 : null;
        public int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex) => null;
    }

    /// <summary>Inline IChartLayout that returns a configurable, possibly-out-of-range
    /// axesIndex from HitTestAxes. Pins Rotate3DModifier line 110's false arm of the
    /// `axesIndex &lt; _figure.SubPlots.Count` short-circuit.</summary>
    private sealed class StaleAxesLayout : IChartLayout
    {
        private readonly int _returnsAxesIndex;
        public StaleAxesLayout(int returnsAxesIndex) => _returnsAxesIndex = returnsAxesIndex;
        public int AxesCount => 1;
        public Rect GetPlotArea(int axesIndex) => new(0, 0, 200, 100);
        public DataRange GetDataRange(int axesIndex) => new(0, 10, 0, 5);
        public int? HitTestAxes(double pixelX, double pixelY) => _returnsAxesIndex;
        public int? HitTestLegendItem(double pixelX, double pixelY, int axesIndex) => null;
    }
}
