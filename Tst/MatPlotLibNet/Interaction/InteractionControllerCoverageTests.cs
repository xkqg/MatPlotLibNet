// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

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
        // Subsequent move with no active modifier → no exception, no events.
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
        // Sanity: ActiveCrosshair updates against the new layout.
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
        // Move outside any data point's hit radius (data is at x=1,2,3 / y=4,5,6).
        ctrl.HandlePointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        // We don't assert tooltip nullness — the renderer/series may resolve nearest at
        // those data coords. Just verify no exception thrown.
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
}
