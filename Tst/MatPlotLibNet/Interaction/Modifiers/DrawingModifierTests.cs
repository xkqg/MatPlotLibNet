// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Rendering;
using static MatPlotLibNet.Interaction.InteractionToolbar;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary>Verifies <see cref="TrendlineDrawingModifier"/>, <see cref="LevelDrawingModifier"/>,
/// <see cref="FibonacciDrawingModifier"/>, and <see cref="DrawingToolSelectionModifier"/>.</summary>
public class DrawingModifierTests
{
    private static (Figure fig, IChartLayout layout, InteractionToolbar toolbar) BuildHarness()
    {
        var fig = new Figure();
        var axes = fig.AddSubPlot();
        axes.XAxis.Min = 0; axes.XAxis.Max = 10;
        axes.YAxis.Min = 0; axes.YAxis.Max = 5;
        var layout = ChartLayout.Create(fig, [new Rect(10, 10, 100, 50)]);
        var toolbar = new InteractionToolbar();
        return (fig, layout, toolbar);
    }

    // ── TrendlineDrawingModifier ──────────────────────────────────────────────

    [Fact]
    public void TrendlineModifier_NotActive_WhenToolModeIsNotTrendline()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var m = new TrendlineDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        // Default ToolMode is Pan
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void TrendlineModifier_Active_WhenToolModeIsTrendline()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Trendline);
        var m = new TrendlineDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.True(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void TrendlineModifier_TwoClicks_EmitsAddTrendlineEvent()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Trendline);
        var events = new List<FigureInteractionEvent>();
        var m = new TrendlineDrawingModifier("c1", layout, fig, events.Add, toolbar);

        // First click — stores p1, no event yet
        m.OnPointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);

        // Second click — emits event
        m.OnPointerPressed(new PointerInputArgs(110, 60, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(110, 60, PointerButton.Left, ModifierKeys.None));
        Assert.Single(events);
        Assert.IsType<AddTrendlineEvent>(events[0]);
    }

    [Fact]
    public void TrendlineModifier_ResetAfterSecondClick()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Trendline);
        var events = new List<FigureInteractionEvent>();
        var m = new TrendlineDrawingModifier("c1", layout, fig, events.Add, toolbar);

        // Two clicks → one event
        m.OnPointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.None));
        m.OnPointerPressed(new PointerInputArgs(110, 60, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(110, 60, PointerButton.Left, ModifierKeys.None));

        // Third click starts next trendline — no second event yet
        m.OnPointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.None));
        Assert.Single(events);
    }

    // ── LevelDrawingModifier ──────────────────────────────────────────────────

    [Fact]
    public void LevelModifier_NotActive_WhenToolModeIsNotLevel()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var m = new LevelDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void LevelModifier_SingleClick_EmitsAddHorizontalLevelEvent()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Level);
        var events = new List<FigureInteractionEvent>();
        var m = new LevelDrawingModifier("c1", layout, fig, events.Add, toolbar);

        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));

        Assert.Single(events);
        Assert.IsType<AddHorizontalLevelEvent>(events[0]);
    }

    // ── FibonacciDrawingModifier ──────────────────────────────────────────────

    [Fact]
    public void FibonacciModifier_NotActive_WhenToolModeIsNotFibonacci()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var m = new FibonacciDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void FibonacciModifier_TwoClicks_EmitsAddFibonacciRetracementEvent()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Fibonacci);
        var events = new List<FigureInteractionEvent>();
        var m = new FibonacciDrawingModifier("c1", layout, fig, events.Add, toolbar);

        // First click (high)
        m.OnPointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);

        // Second click (low)
        m.OnPointerPressed(new PointerInputArgs(110, 60, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(110, 60, PointerButton.Left, ModifierKeys.None));
        Assert.Single(events);
        Assert.IsType<AddFibonacciRetracementEvent>(events[0]);
    }

    [Fact]
    public void FibonacciModifier_EventContainsPriceHighAndLow()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Fibonacci);
        var events = new List<FigureInteractionEvent>();
        var m = new FibonacciDrawingModifier("c1", layout, fig, events.Add, toolbar);

        // Top-left of plot area → highest Y (axes max), bottom-right → lowest Y
        m.OnPointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.None));
        m.OnPointerPressed(new PointerInputArgs(10, 60, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(10, 60, PointerButton.Left, ModifierKeys.None));

        var evt = Assert.IsType<AddFibonacciRetracementEvent>(events[0]);
        // First click Y should be > second click Y (in data space, top pixel = high price)
        Assert.True(evt.Tool.PriceHigh >= evt.Tool.PriceLow);
    }

    // ── DrawingToolSelectionModifier ──────────────────────────────────────────

    [Fact]
    public void SelectionModifier_DeleteKey_HandlesWhenTrendlineSelected()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var tl = fig.SubPlots[0].AddTrendline(0, 0, 5, 5);
        var m = new DrawingToolSelectionModifier("c1", layout, fig, _ => { }, toolbar);
        m.SelectTool(tl, axesIndex: 0);
        Assert.True(m.HandlesKeyDown(new KeyInputArgs("Delete", ModifierKeys.None)));
    }

    [Fact]
    public void SelectionModifier_DeleteKey_EmitsRemoveEvent()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var tl = fig.SubPlots[0].AddTrendline(0, 0, 5, 5);
        var events = new List<FigureInteractionEvent>();
        var m = new DrawingToolSelectionModifier("c1", layout, fig, events.Add, toolbar);
        m.SelectTool(tl, axesIndex: 0);

        m.OnKeyDown(new KeyInputArgs("Delete", ModifierKeys.None));

        Assert.Single(events);
        var evt = Assert.IsType<RemoveDrawingToolEvent>(events[0]);
        Assert.Same(tl, evt.Tool);
    }

    [Fact]
    public void SelectionModifier_NoSelection_DeleteKey_DoesNotHandle()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var m = new DrawingToolSelectionModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesKeyDown(new KeyInputArgs("Delete", ModifierKeys.None)));
    }

    [Fact]
    public void SelectionModifier_OnKeyDown_WithNoSelection_DoesNotEmit()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var events = new List<FigureInteractionEvent>();
        var m = new DrawingToolSelectionModifier("c1", layout, fig, events.Add, toolbar);
        m.OnKeyDown(new KeyInputArgs("Delete", ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void SelectionModifier_HandlesKeyDown_NonDeleteKey_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var tl = fig.SubPlots[0].AddTrendline(0, 0, 5, 5);
        var m = new DrawingToolSelectionModifier("c1", layout, fig, _ => { }, toolbar);
        m.SelectTool(tl, axesIndex: 0);
        Assert.False(m.HandlesKeyDown(new KeyInputArgs("Escape", ModifierKeys.None)));
    }

    [Fact]
    public void SelectionModifier_ClearSelection_ThenDeleteKey_DoesNotHandle()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var tl = fig.SubPlots[0].AddTrendline(0, 0, 5, 5);
        var m = new DrawingToolSelectionModifier("c1", layout, fig, _ => { }, toolbar);
        m.SelectTool(tl, axesIndex: 0);
        m.ClearSelection();
        Assert.False(m.HandlesKeyDown(new KeyInputArgs("Delete", ModifierKeys.None)));
    }

    [Fact]
    public void TrendlineModifier_OutsidePlotArea_DoesNotEmit()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Trendline);
        var events = new List<FigureInteractionEvent>();
        var m = new TrendlineDrawingModifier("c1", layout, fig, events.Add, toolbar);
        // Click far outside the plot area
        m.OnPointerPressed(new PointerInputArgs(999, 999, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(999, 999, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void LevelModifier_OutsidePlotArea_DoesNotEmit()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Level);
        var events = new List<FigureInteractionEvent>();
        var m = new LevelDrawingModifier("c1", layout, fig, events.Add, toolbar);
        m.OnPointerPressed(new PointerInputArgs(999, 999, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(999, 999, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void FibonacciModifier_OutsidePlotArea_DoesNotEmit()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Fibonacci);
        var events = new List<FigureInteractionEvent>();
        var m = new FibonacciDrawingModifier("c1", layout, fig, events.Add, toolbar);
        m.OnPointerPressed(new PointerInputArgs(999, 999, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(999, 999, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    // ── IInteractionModifier pass-through methods — required by interface, no-op by design ──
    //
    // These methods exist to satisfy IInteractionModifier and do nothing; the tests drive
    // them so coverage reflects that they're invocable and side-effect-free.

    [Fact]
    public void LevelModifier_HandlesScroll_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var m = new LevelDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesScroll(new ScrollInputArgs(60, 35, 0, 1, ModifierKeys.None)));
    }

    [Fact]
    public void LevelModifier_HandlesKeyDown_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var m = new LevelDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesKeyDown(new KeyInputArgs("Delete", ModifierKeys.None)));
    }

    [Fact]
    public void LevelModifier_PassThroughMethods_AreNoOp()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var events = new List<FigureInteractionEvent>();
        var m = new LevelDrawingModifier("c1", layout, fig, events.Add, toolbar);
        m.OnPointerMoved(new PointerInputArgs(1, 1, PointerButton.Left, ModifierKeys.None));
        m.OnScroll(new ScrollInputArgs(1, 1, 0, 1, ModifierKeys.None));
        m.OnKeyDown(new KeyInputArgs("Escape", ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void LevelModifier_HandlesPointerPressed_WrongButton_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Level);
        var m = new LevelDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        // Right-click should not trigger level drawing (left-click only)
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Right, ModifierKeys.None)));
    }

    [Fact]
    public void FibonacciModifier_HandlesScroll_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var m = new FibonacciDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesScroll(new ScrollInputArgs(60, 35, 0, 1, ModifierKeys.None)));
    }

    [Fact]
    public void FibonacciModifier_HandlesKeyDown_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var m = new FibonacciDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesKeyDown(new KeyInputArgs("Delete", ModifierKeys.None)));
    }

    [Fact]
    public void FibonacciModifier_PassThroughMethods_AreNoOp()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var events = new List<FigureInteractionEvent>();
        var m = new FibonacciDrawingModifier("c1", layout, fig, events.Add, toolbar);
        m.OnPointerMoved(new PointerInputArgs(1, 1, PointerButton.Left, ModifierKeys.None));
        m.OnScroll(new ScrollInputArgs(1, 1, 0, 1, ModifierKeys.None));
        m.OnKeyDown(new KeyInputArgs("Escape", ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void FibonacciModifier_HandlesPointerPressed_WrongButton_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Fibonacci);
        var m = new FibonacciDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Right, ModifierKeys.None)));
    }

    [Fact]
    public void SelectionModifier_HandlesPointerPressed_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var m = new DrawingToolSelectionModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(1, 1, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void SelectionModifier_HandlesScroll_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var m = new DrawingToolSelectionModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesScroll(new ScrollInputArgs(1, 1, 0, 1, ModifierKeys.None)));
    }

    [Fact]
    public void SelectionModifier_PassThroughPointerMethods_AreNoOp()
    {
        var (fig, layout, toolbar) = BuildHarness();
        var events = new List<FigureInteractionEvent>();
        var m = new DrawingToolSelectionModifier("c1", layout, fig, events.Add, toolbar);
        m.OnPointerPressed(new PointerInputArgs(1, 1, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(2, 2, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(3, 3, PointerButton.Left, ModifierKeys.None));
        m.OnScroll(new ScrollInputArgs(4, 4, 0, 1, ModifierKeys.None));
        Assert.Empty(events);
    }

    // ── LevelModifier + FibonacciModifier wrong-mode branches (HandlesPointerPressed ToolMode check) ──

    [Fact]
    public void LevelModifier_WrongMode_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Trendline); // Different tool active
        var m = new LevelDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void FibonacciModifier_WrongMode_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Level); // Different tool active
        var m = new FibonacciDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None)));
    }

    // ── Third AND branch: valid mode + valid button but click outside plot area ──

    [Fact]
    public void LevelModifier_ValidModeValidButton_OutsidePlotArea_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Level);
        var m = new LevelDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        // Click outside the configured plot area (10,10,100,50) — HitTestAxes returns null.
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(999, 999, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void FibonacciModifier_ValidModeValidButton_OutsidePlotArea_ReturnsFalse()
    {
        var (fig, layout, toolbar) = BuildHarness();
        toolbar.Activate(ToolIds.Fibonacci);
        var m = new FibonacciDrawingModifier("c1", layout, fig, _ => { }, toolbar);
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(999, 999, PointerButton.Left, ModifierKeys.None)));
    }
}
