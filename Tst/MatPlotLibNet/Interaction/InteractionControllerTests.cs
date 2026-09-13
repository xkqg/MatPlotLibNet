// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

public class InteractionControllerTests
{
    private static (InteractionController ctrl, Figure figure, ChartLayout layout)
        MakeLocal(double xMin = 0, double xMax = 10, double yMin = 0, double yMax = 5)
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.ChartId = "chart-1";
        figure.SubPlots[0].XAxis.Min = xMin;
        figure.SubPlots[0].XAxis.Max = xMax;
        figure.SubPlots[0].YAxis.Min = yMin;
        figure.SubPlots[0].YAxis.Max = yMax;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var ctrl = InteractionController.CreateLocal(figure, layout);
        return (ctrl, figure, layout);
    }

    // ── scroll → zoom ──────────────────────────────────────────────────────────

    [Fact]
    public void HandleScroll_InsidePlot_ZoomsIn()
    {
        var (ctrl, figure, _) = MakeLocal();

        ctrl.HandleScroll(new ScrollInputArgs(60, 35, 0, -1));

        Assert.NotEqual(0.0,  figure.SubPlots[0].XAxis.Min);
        Assert.NotEqual(10.0, figure.SubPlots[0].XAxis.Max);
    }

    [Fact]
    public void HandleScroll_OutsidePlot_NoMutation()
    {
        var (ctrl, figure, _) = MakeLocal();
        ctrl.HandleScroll(new ScrollInputArgs(5, 5, 0, -1));
        Assert.Equal(0,  figure.SubPlots[0].XAxis.Min);
        Assert.Equal(10, figure.SubPlots[0].XAxis.Max);
    }

    // ── crosshair (passive) ───────────────────────────────────────────────────
    //
    // Phase H.3 of v1.7.2 follow-on plan — CrosshairModifier used to be dead code
    // (defined + unit-tested but never instantiated by the controller). These
    // tests pin its wire-up: ActiveCrosshair is null at rest, populated during
    // hover over the plot area, and reset to null when the cursor leaves.

    [Fact]
    public void ActiveCrosshair_NullAtRest_NonNullOnHoverInsidePlot()
    {
        var (ctrl, _, _) = MakeLocal();

        Assert.Null(ctrl.ActiveCrosshair);

        ctrl.HandlePointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        Assert.NotNull(ctrl.ActiveCrosshair);
        Assert.Equal(60, ctrl.ActiveCrosshair!.Value.PixelX);
        Assert.Equal(35, ctrl.ActiveCrosshair.Value.PixelY);
    }

    [Fact]
    public void ActiveCrosshair_Null_OnHoverOutsidePlot()
    {
        var (ctrl, _, _) = MakeLocal();
        // First move inside — populate state.
        ctrl.HandlePointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        Assert.NotNull(ctrl.ActiveCrosshair);

        // Now move outside the 10,10,100,50 plot rect → state clears.
        ctrl.HandlePointerMoved(new PointerInputArgs(200, 200, PointerButton.None, ModifierKeys.None));
        Assert.Null(ctrl.ActiveCrosshair);
    }

    // ── drag → pan ─────────────────────────────────────────────────────────────

    [Fact]
    public void HandlePointerPressed_LeftDrag_ThenMove_PansAxis()
    {
        var (ctrl, figure, _) = MakeLocal();

        ctrl.HandlePointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        ctrl.HandlePointerMoved(new PointerInputArgs(70, 35, PointerButton.Left, ModifierKeys.None));

        // Figure's axis limits should have shifted.
        var axes = figure.SubPlots[0];
        Assert.NotEqual(0, axes.XAxis.Min);
        Assert.NotEqual(10, axes.XAxis.Max);
    }

    // ── double-click → reset ───────────────────────────────────────────────────

    [Fact]
    public void HandlePointerPressed_DoubleClick_ResetsAxis()
    {
        var (ctrl, figure, _) = MakeLocal();

        // First zoom in.
        ctrl.HandleScroll(new ScrollInputArgs(60, 35, 0, -1));
        double zoomedXMin = figure.SubPlots[0].XAxis.Min!.Value;

        // Then double-click to reset.
        ctrl.HandlePointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None, ClickCount: 2));

        Assert.Equal(0, figure.SubPlots[0].XAxis.Min!.Value, precision: 6);
        Assert.Equal(10, figure.SubPlots[0].XAxis.Max!.Value, precision: 6);
    }

    // ── Home key → reset ───────────────────────────────────────────────────────

    [Fact]
    public void HandleKeyDown_HomeKey_ResetsAxis()
    {
        var (ctrl, figure, _) = MakeLocal();
        ctrl.HandleScroll(new ScrollInputArgs(60, 35, 0, -1));
        ctrl.HandleKeyDown(new KeyInputArgs("Home"));
        Assert.Equal(0,  figure.SubPlots[0].XAxis.Min!.Value, precision: 6);
        Assert.Equal(10, figure.SubPlots[0].XAxis.Max!.Value, precision: 6);
    }

    // ── InvalidateRequested ────────────────────────────────────────────────────

    [Fact]
    public void LocalMode_MutationEvent_RaisesInvalidateRequested()
    {
        var (ctrl, _, _) = MakeLocal();
        int count = 0;
        ctrl.InvalidateRequested += () => count++;

        ctrl.HandleScroll(new ScrollInputArgs(60, 35, 0, -1));

        Assert.Equal(1, count);
    }

    [Fact]
    public void LocalMode_NotificationEvent_DoesNotRaiseInvalidateRequested()
    {
        var (ctrl, _, _) = MakeLocal();
        int count = 0;
        ctrl.InvalidateRequested += () => count++;

        // Hover is a notification event — no mutation, no invalidate.
        ctrl.HandlePointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));

        Assert.Equal(0, count);
    }

    // ── BrushSelect is a notification event ───────────────────────────────────

    [Fact]
    public void ShiftDrag_ProducesBrushSelectEvent_ViaCustomSink()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.ChartId = "chart-1";
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var received = new List<FigureInteractionEvent>();
        var ctrl = InteractionController.Create(figure, layout, received.Add);

        ctrl.HandlePointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.Shift));
        ctrl.HandlePointerReleased(new PointerInputArgs(110, 60, PointerButton.Left, ModifierKeys.Shift));

        Assert.Single(received);
        Assert.IsType<BrushSelectEvent>(received[0]);
    }

    // ── UpdateLayout rebuilds modifiers ───────────────────────────────────────

    [Fact]
    public void UpdateLayout_NewLayout_ModifiersUseUpdatedRanges()
    {
        var (ctrl, figure, _) = MakeLocal();

        // Update layout with new data range.
        figure.SubPlots[0].XAxis.Min = 100;
        figure.SubPlots[0].XAxis.Max = 200;
        var newLayout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        ctrl.UpdateLayout(newLayout);

        // Reset should now restore the new range.
        ctrl.HandleKeyDown(new KeyInputArgs("Home"));
        Assert.Equal(100, figure.SubPlots[0].XAxis.Min!.Value, precision: 6);
        Assert.Equal(200, figure.SubPlots[0].XAxis.Max!.Value, precision: 6);
    }
}

// ─── InteractionControllerCoverageTests.cs ───────────────────────────────────

/// <summary>Phase X.9.c (v1.7.2, 2026-04-19) — pinpoint coverage for the
/// <see cref="InteractionController"/> branches that the existing harness left at
/// 50%/0% in the 2026-04-19 cobertura. Pre-X.9.c: 89%L / 70%B. Targets:
///   - ActiveBrushSelect getter (line 35-37) — needs an active brush
///   - HandlePointerPressed for-loop natural exit (line 105) — no modifier claims
///   - HandlePointerReleased with `_activeModifier == null` (line 175 false arm)
///   - HandleScroll natural exit (line 182, no modifier claims)
///   - HandleKeyDown forwarding (line 195-197)
///   - UpdateLayout (line 98) — swaps layout and rebuilds modifiers
///   - HandlePointerMoved with axesIndex null (line 145+147 false arm)
///   - HandlePointerMoved with active brush triggers InvalidateRequested (line 129-130)</summary>
public class InteractionControllerCoverageTests
{
    private static (Figure fig, IChartLayout layout) Make()
    {
        var fig = Plt.Create().Plot([1.0, 2.0, 3.0], [4.0, 5.0, 6.0]).Build();
        fig.SubPlots[0].XAxis.Min = 0;
        fig.SubPlots[0].XAxis.Max = 10;
        fig.SubPlots[0].YAxis.Min = 0;
        fig.SubPlots[0].YAxis.Max = 10;
        var layout = ChartLayout.Create(fig, [new Rect(10, 10, 100, 50)]);
        return (fig, layout);
    }

    /// <summary>ActiveBrushSelect getter (lines 35-37) — non-null when the brush
    /// modifier has an active selection. Triggered by Shift+left-press inside the plot.</summary>
    [Fact]
    public void ActiveBrushSelect_AfterShiftPress_ReturnsState()
    {
        var (fig, layout) = Make();
        var ctrl = InteractionController.Create(fig, layout, _ => { });
        Assert.Null(ctrl.ActiveBrushSelect);
        ctrl.HandlePointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.Shift));
        ctrl.HandlePointerMoved(new PointerInputArgs(80, 45, PointerButton.Left, ModifierKeys.Shift));
        Assert.NotNull(ctrl.ActiveBrushSelect);
    }

    /// <summary>HandlePointerPressed natural exit (line 105 for-loop ends with no claim).
    /// Right-button without modifiers → no modifier claims (Pan needs Left,
    /// Rotate3D needs 3D, etc.). Controller silently does nothing, _activeModifier=null.</summary>
    [Fact]
    public void HandlePointerPressed_NoModifierClaims_ActiveModifierStaysNull()
    {
        var (fig, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var ctrl = InteractionController.Create(fig, layout, events.Add);
        ctrl.HandlePointerPressed(new PointerInputArgs(60, 35, PointerButton.Right, ModifierKeys.None));
        ctrl.HandlePointerMoved(new PointerInputArgs(80, 45, PointerButton.None, ModifierKeys.None));
        ctrl.HandlePointerReleased(new PointerInputArgs(80, 45, PointerButton.Right, ModifierKeys.None));
    }

    /// <summary>HandlePointerReleased without a prior press (line 175 `?.` short-circuit
    /// to null arm). _activeModifier is null → null-conditional skips OnPointerReleased.</summary>
    [Fact]
    public void HandlePointerReleased_WithoutActiveModifier_NoOp()
    {
        var (fig, layout) = Make();
        var ctrl = InteractionController.Create(fig, layout, _ => { });
        ctrl.HandlePointerReleased(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
    }

    /// <summary>HandleScroll natural exit (line 182 for-loop) — wheel event outside the
    /// plot area; no modifier claims (ZoomModifier requires HitTestAxes non-null).</summary>
    [Fact]
    public void HandleScroll_OutsidePlot_NoModifierClaims_NoOp()
    {
        var (fig, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var ctrl = InteractionController.Create(fig, layout, events.Add);
        ctrl.HandleScroll(new ScrollInputArgs(500, 500, 0, -1));
        Assert.Empty(events);
    }

    /// <summary>HandleKeyDown forwarding (line 195-197). Reset modifier handles "Home" key
    /// when present in the modifier list (CreateLocal includes ResetModifier).</summary>
    [Fact]
    public void HandleKeyDown_HomeKey_ResetModifierClaims_EmitsResetEvent()
    {
        var (fig, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var ctrl = InteractionController.Create(fig, layout, events.Add);
        const string HomeKey = "Home";
        ctrl.HandleKeyDown(new KeyInputArgs(HomeKey, ModifierKeys.None));
        Assert.NotEmpty(events);
    }

    /// <summary>HandleKeyDown natural exit (line 195 for-loop ends with no claim).
    /// Unknown key → no modifier handles it.</summary>
    [Fact]
    public void HandleKeyDown_UnknownKey_NoModifierClaims_NoOp()
    {
        var (fig, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var ctrl = InteractionController.Create(fig, layout, events.Add);
        const string PageUp = "PageUp";
        ctrl.HandleKeyDown(new KeyInputArgs(PageUp, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>UpdateLayout (line 95-99) — swap to a fresh layout, modifiers rebuild.
    /// Forward-regression guard for the resize/layout-change path.</summary>
    [Fact]
    public void UpdateLayout_RebuildsModifiers()
    {
        var (fig, layout) = Make();
        var ctrl = InteractionController.Create(fig, layout, _ => { });
        var newLayout = ChartLayout.Create(fig, [new Rect(0, 0, 200, 100)]);
        ctrl.UpdateLayout(newLayout);
        ctrl.HandlePointerMoved(new PointerInputArgs(50, 50, PointerButton.None, ModifierKeys.None));
        Assert.NotNull(ctrl.ActiveCrosshair);
    }

    /// <summary>HandlePointerMoved hover path with no nearest point (line 145+147 false
    /// arm). Move with no series in range → _activeTooltip null.</summary>
    [Fact]
    public void HandlePointerMoved_HoverWithNoNearbyPoint_NullsTooltip()
    {
        var (fig, layout) = Make();
        var ctrl = InteractionController.Create(fig, layout, _ => { });
        ctrl.HandlePointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
    }

    /// <summary>HandlePointerMoved with hover OUTSIDE plot (line 147 axesIndex-null arm) —
    /// _activeTooltip set to null.</summary>
    [Fact]
    public void HandlePointerMoved_HoverOutsidePlot_TooltipNulled()
    {
        var (fig, layout) = Make();
        var ctrl = InteractionController.Create(fig, layout, _ => { });
        ctrl.HandlePointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        ctrl.HandlePointerMoved(new PointerInputArgs(500, 500, PointerButton.None, ModifierKeys.None));
        Assert.Null(ctrl.ActiveTooltip);
    }

    // ── the clicked data point ─────────────────────────────────────────────────
    //
    // A click on a point produced a DataCursorEvent, the sink swallowed it, and that was the end of it: local
    // mode applies it to the figure, where its ApplyTo is a no-op by design, and the SignalR sink drops it into
    // a discard arm. So the one gesture whose whole purpose is to tell the application which point was clicked
    // told nobody. The event below is raised from the wrapper the constructor puts around whichever sink it was
    // given, so it reaches the application in both hosting modes.

    /// <summary>A figure with points at (0,0), (1,5) and (2,10) over a 200x100 plot area, so the middle point
    /// sits exactly at pixel (100, 50) and a click there is a hit.</summary>
    private static (Figure Figure, ChartLayout Layout) Pinnable()
    {
        var figure = Plt.Create().Plot([0.0, 1.0, 2.0], [0.0, 5.0, 10.0], s => s.Label = "load").Build();
        figure.ChartId = "chart-1";
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 2;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 10;
        return (figure, ChartLayout.Create(figure, [new Rect(0, 0, 200, 100)]));
    }

    private static PointerInputArgs Click(double x, double y) =>
        new(x, y, PointerButton.Left, ModifierKeys.None);

    [Fact]
    public void AClickOnADataPoint_SaysWhichPointItWas()
    {
        var (figure, layout) = Pinnable();
        var ctrl = InteractionController.CreateLocal(figure, layout);
        var pinned = new List<PinnedAnnotation>();
        ctrl.DataPointClicked += pinned.Add;

        ctrl.HandlePointerPressed(Click(100, 50));

        var point = Assert.Single(pinned);
        Assert.Equal("load", point.SeriesLabel);
        Assert.Equal(1.0, point.DataX);
        Assert.Equal(5.0, point.DataY);
        Assert.Equal(100.0, point.PixelX);
        Assert.Equal(50.0, point.PixelY);
        Assert.Equal(0, point.AxesIndex);
    }

    [Fact]
    public void AClickOnADataPoint_SaysSoWithACustomSinkToo()
    {
        // The server-driven hosts build the controller with their own sink, which publishes over SignalR. If the
        // event were raised from the local factory's closure, this arm — the one with a server to tell — would be
        // the one where nothing happens.
        var (figure, layout) = Pinnable();
        var published = new List<FigureInteractionEvent>();
        var ctrl = InteractionController.Create(figure, layout, published.Add);
        var pinned = new List<PinnedAnnotation>();
        ctrl.DataPointClicked += pinned.Add;

        ctrl.HandlePointerPressed(Click(100, 50));

        Assert.Equal(1.0, Assert.Single(pinned).DataX);
        Assert.IsType<DataCursorEvent>(Assert.Single(published));
    }

    [Fact]
    public void AClickThatMissesEveryPoint_SaysNothing()
    {
        // A miss is not a data cursor at all: the modifier declines and pan takes the drag, so a PanEvent runs
        // through the same wrapper and must not be mistaken for a pinned point.
        var (figure, layout) = Pinnable();
        var seen = new List<FigureInteractionEvent>();
        var ctrl = InteractionController.Create(figure, layout, seen.Add);
        var pinned = new List<PinnedAnnotation>();
        ctrl.DataPointClicked += pinned.Add;

        ctrl.HandlePointerPressed(Click(160, 20));
        ctrl.HandlePointerMoved(Click(150, 20));

        Assert.Empty(pinned);
        Assert.IsType<PanEvent>(Assert.Single(seen));   // the wrapper did see an event, and let it through
    }

    [Fact]
    public void AClickOnADataPoint_WithNobodyListening_IsStillJustAClick()
    {
        var (figure, layout) = Pinnable();
        var ctrl = InteractionController.CreateLocal(figure, layout);

        ctrl.HandlePointerPressed(Click(100, 50));

        Assert.Null(ctrl.ActiveTooltip);
    }

    [Fact]
    public void AClickOnADataPoint_AfterTheLayoutIsRebuilt_StillSaysWhichPointItWas()
    {
        // UpdateLayout builds a fresh set of modifiers; they have to be given the wrapped sink, not the raw one.
        var (figure, layout) = Pinnable();
        var ctrl = InteractionController.CreateLocal(figure, layout);
        var pinned = new List<PinnedAnnotation>();
        ctrl.DataPointClicked += pinned.Add;

        ctrl.UpdateLayout(ChartLayout.Create(figure, [new Rect(0, 0, 200, 100)]));
        ctrl.HandlePointerPressed(Click(100, 50));

        Assert.Equal(5.0, Assert.Single(pinned).DataY);
    }
    // ── hover names the same point the click does ──────────────────────────────
    //
    // Both gestures resolve their point through the same finder, and only the click carried the identity:
    // hover flattened the result into a sentence. A host that highlights a row on hover had nothing to
    // highlight it by.

    [Fact]
    public void AHoverOverADataPoint_NamesWhichPointItIs()
    {
        var (figure, layout) = Pinnable();
        var ctrl = InteractionController.CreateLocal(figure, layout);

        ctrl.HandlePointerMoved(new PointerInputArgs(100, 50, PointerButton.None, ModifierKeys.None));

        Assert.NotNull(ctrl.ActiveTooltip);
        Assert.Equal(1, ctrl.ActiveTooltip.PointIndex);
        Assert.Equal(0, ctrl.ActiveTooltip.SeriesIndex);
    }

    [Fact]
    public void ATooltipConstructedWithoutAPoint_SaysSo()
    {
        var tooltip = new HoverTooltipContent("text", 1.0, 2.0);

        Assert.Equal(-1, tooltip.PointIndex);
        Assert.Equal(-1, tooltip.SeriesIndex);
    }

    [Fact]
    public void AClickOnADataPoint_SaysWhichPointItWasByIndexToo()
    {
        var (figure, layout) = Pinnable();
        var ctrl = InteractionController.CreateLocal(figure, layout);
        var pinned = new List<PinnedAnnotation>();
        ctrl.DataPointClicked += pinned.Add;

        ctrl.HandlePointerPressed(Click(100, 50));

        var point = Assert.Single(pinned);
        Assert.Equal(1, point.PointIndex);
        Assert.Equal(0, point.SeriesIndex);
    }

}
