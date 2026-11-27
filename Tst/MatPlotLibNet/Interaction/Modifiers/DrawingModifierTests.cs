// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Rendering;
using static MatPlotLibNet.Interaction.InteractionToolbar;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary>The figure, layout and toolbar a drawing modifier is held to.</summary>
/// <param name="Figure">The figure.</param>
/// <param name="Layout">Its layout.</param>
/// <param name="Toolbar">The toolbar that holds the tool mode.</param>
public readonly record struct DrawingRig(Figure Figure, IChartLayout Layout, InteractionToolbar Toolbar);

/// <summary>Verifies <see cref="TrendlineDrawingModifier"/>, <see cref="LevelDrawingModifier"/>,
/// <see cref="FibonacciDrawingModifier"/>, and <see cref="DrawingToolSelectionModifier"/>.</summary>
public class DrawingModifierTests
{
    private static DrawingRig BuildHarness()
    {
        var fig = new Figure();
        var axes = fig.AddSubPlot();
        axes.XAxis.Min = 0; axes.XAxis.Max = 10;
        axes.YAxis.Min = 0; axes.YAxis.Max = 5;
        var layout = ChartLayout.Create(fig, [new Rect(10, 10, 100, 50)]);
        var toolbar = new InteractionToolbar();
        return new DrawingRig(fig, layout, toolbar);
    }

    /// <summary>A modifier with the events it sent.</summary>
    /// <param name="Modifier">The modifier.</param>
    /// <param name="Events">What it sent, in order.</param>
    private readonly record struct Armed<T>(T Modifier, List<FigureInteractionEvent> Events) where T : InteractionModifier;

    private static InteractionModifier DrawingModifierOf(string tool, IChartLayout layout, Action<FigureInteractionEvent> sink, InteractionToolbar toolbar) => tool switch
    {
        ToolIds.Trendline => new TrendlineDrawingModifier("c1", layout, sink, toolbar),
        ToolIds.Level => new LevelDrawingModifier("c1", layout, sink, toolbar),
        _ => new FibonacciDrawingModifier("c1", layout, sink, toolbar),
    };

    /// <summary>A modifier of the tool <paramref name="tool"/> over a fresh figure, its toolbar switched to that tool or left on Pan.</summary>
    private static Armed<InteractionModifier> Arm(string tool, bool active = true)
    {
        var (_, layout, toolbar) = BuildHarness();
        if (active) toolbar.Activate(tool);
        var events = new List<FigureInteractionEvent>();
        return new(DrawingModifierOf(tool, layout, events.Add, toolbar), events);
    }

    private static void Click(InteractionModifier modifier, double x, double y)
    {
        modifier.OnPointerPressed(new PointerInputArgs(x, y, PointerButton.Left, ModifierKeys.None));
        modifier.OnPointerReleased(new PointerInputArgs(x, y, PointerButton.Left, ModifierKeys.None));
    }

    /// <summary>A drawing modifier handles a press only while its own tool is the active one.</summary>
    [Theory]
    [InlineData(ToolIds.Trendline)]
    [InlineData(ToolIds.Level)]
    [InlineData(ToolIds.Fibonacci)]
    public void ADrawingModifier_HandlesAPress_OnlyWhileItsToolIsActive(string tool)
    {
        bool Presses(InteractionModifier m) => m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));

        Assert.True(Presses(Arm(tool).Modifier));
        Assert.False(Presses(Arm(tool, active: false).Modifier));
    }

    [Fact]
    public void TrendlineModifier_TwoClicks_EmitsAddTrendlineEvent()
    {
        var (m, events) = Arm(ToolIds.Trendline);

        Click(m, 10, 10);
        Assert.Empty(events);

        Click(m, 110, 60);
        Assert.Single(events);
        Assert.IsType<AddTrendlineEvent>(events[0]);
    }

    [Fact]
    public void TrendlineModifier_ResetAfterSecondClick()
    {
        var (m, events) = Arm(ToolIds.Trendline);

        Click(m, 10, 10);
        Click(m, 110, 60);
        Click(m, 10, 10);

        Assert.Single(events);
    }

    [Fact]
    public void LevelModifier_SingleClick_EmitsAddHorizontalLevelEvent()
    {
        var (m, events) = Arm(ToolIds.Level);

        Click(m, 60, 35);

        Assert.Single(events);
        Assert.IsType<AddHorizontalLevelEvent>(events[0]);
    }

    [Fact]
    public void FibonacciModifier_TwoClicks_EmitsAddFibonacciRetracementEvent()
    {
        var (m, events) = Arm(ToolIds.Fibonacci);

        Click(m, 10, 10);
        Assert.Empty(events);

        Click(m, 110, 60);
        Assert.Single(events);
        Assert.IsType<AddFibonacciRetracementEvent>(events[0]);
    }

    [Fact]
    public void FibonacciModifier_EventContainsPriceHighAndLow()
    {
        var (m, events) = Arm(ToolIds.Fibonacci);

        // Top-left of plot area → highest Y (axes max), bottom-left → lowest Y
        Click(m, 10, 10);
        Click(m, 10, 60);

        var evt = Assert.IsType<AddFibonacciRetracementEvent>(events[0]);
        Assert.True(evt.Tool.PriceHigh >= evt.Tool.PriceLow);
    }

    /// <summary>A selection modifier over a figure with one trendline, which it has selected unless <paramref name="selected"/> says not.</summary>
    private static Armed<DrawingToolSelectionModifier> Select(out Trendline trendline, bool selected = true)
    {
        var (fig, layout, _) = BuildHarness();
        trendline = fig.SubPlots[0].AddTrendline(0, 0, 5, 5);
        var events = new List<FigureInteractionEvent>();
        var m = new DrawingToolSelectionModifier("c1", layout, events.Add);
        if (selected) m.SelectTool(trendline, axesIndex: 0);
        return new(m, events);
    }

    private static bool HandlesKey(InteractionModifier modifier, string key = "Delete") => modifier.HandlesKeyDown(new KeyInputArgs(key, ModifierKeys.None));

    [Fact]
    public void SelectionModifier_DeleteKey_HandlesWhenTrendlineSelected() => Assert.True(HandlesKey(Select(out _).Modifier));

    [Fact]
    public void SelectionModifier_DeleteKey_EmitsRemoveEvent()
    {
        var (m, events) = Select(out var tl);

        m.OnKeyDown(new KeyInputArgs("Delete", ModifierKeys.None));

        Assert.Single(events);
        var evt = Assert.IsType<RemoveDrawingToolEvent>(events[0]);
        Assert.Same(tl, evt.Tool);
    }

    [Fact]
    public void SelectionModifier_NoSelection_DeleteKey_DoesNotHandle() => Assert.False(HandlesKey(Select(out _, selected: false).Modifier));

    [Fact]
    public void SelectionModifier_OnKeyDown_WithNoSelection_DoesNotEmit()
    {
        var (m, events) = Select(out _, selected: false);

        m.OnKeyDown(new KeyInputArgs("Delete", ModifierKeys.None));

        Assert.Empty(events);
    }

    [Fact]
    public void SelectionModifier_HandlesKeyDown_NonDeleteKey_ReturnsFalse() => Assert.False(HandlesKey(Select(out _).Modifier, "Escape"));

    [Fact]
    public void SelectionModifier_ClearSelection_ThenDeleteKey_DoesNotHandle()
    {
        var (m, _) = Select(out _);

        m.ClearSelection();

        Assert.False(HandlesKey(m));
    }

    /// <summary>A click far outside the plot area draws nothing, whichever tool is active.</summary>
    [Theory]
    [InlineData(ToolIds.Trendline)]
    [InlineData(ToolIds.Level)]
    [InlineData(ToolIds.Fibonacci)]
    public void ADrawingModifier_ClickedOutsideThePlotArea_DoesNotEmit(string tool)
    {
        var (m, events) = Arm(tool);

        Click(m, 999, 999);

        Assert.Empty(events);
    }

    // ── IInteractionModifier pass-through methods — required by interface, no-op by design ──
    //
    // These methods exist to satisfy IInteractionModifier and do nothing; the tests drive
    // them so coverage reflects that they're invocable and side-effect-free.

    /// <summary>A level or a Fibonacci modifier takes neither a scroll nor a key, and its other pointer and key members send nothing.</summary>
    [Theory]
    [InlineData(ToolIds.Level)]
    [InlineData(ToolIds.Fibonacci)]
    public void ALevelOrFibonacciModifier_TakesNeitherAScrollNorAKey_AndItsPassThroughMethodsSendNothing(string tool)
    {
        var (m, events) = Arm(tool, active: false);

        Assert.False(m.HandlesScroll(new ScrollInputArgs(60, 35, 0, 1, ModifierKeys.None)));
        Assert.False(HandlesKey(m));
        m.OnPointerMoved(new PointerInputArgs(1, 1, PointerButton.Left, ModifierKeys.None));
        m.OnScroll(new ScrollInputArgs(1, 1, 0, 1, ModifierKeys.None));
        m.OnKeyDown(new KeyInputArgs("Escape", ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void SelectionModifier_TakesNeitherAPressNorAScroll_AndItsPassThroughPointerMethodsSendNothing()
    {
        var (m, events) = Select(out _, selected: false);

        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(1, 1, PointerButton.Left, ModifierKeys.None)));
        Assert.False(m.HandlesScroll(new ScrollInputArgs(1, 1, 0, 1, ModifierKeys.None)));
        m.OnPointerPressed(new PointerInputArgs(1, 1, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(2, 2, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(3, 3, PointerButton.Left, ModifierKeys.None));
        m.OnScroll(new ScrollInputArgs(4, 4, 0, 1, ModifierKeys.None));
        Assert.Empty(events);
    }

    /// <summary>A level or a Fibonacci modifier handles a press only in its own tool mode (each of the other tools is a wrong mode), with the
    /// left button (a right click does not draw), and inside the plot area, which is configured at (10, 10, 100, 50): a click at (999, 999) is
    /// outside, where the hit test finds no axes.</summary>
    [Theory]
    [InlineData(false, ToolIds.Level, 60, 35, PointerButton.Right)]
    [InlineData(true, ToolIds.Fibonacci, 60, 35, PointerButton.Right)]
    [InlineData(false, ToolIds.Trendline, 60, 35, PointerButton.Left)]
    [InlineData(true, ToolIds.Level, 60, 35, PointerButton.Left)]
    [InlineData(false, ToolIds.Level, 999, 999, PointerButton.Left)]
    [InlineData(true, ToolIds.Fibonacci, 999, 999, PointerButton.Left)]
    public void ALevelOrFibonacciModifier_DoesNotHandleAPress_OutOfItsModeButtonOrPlotArea(bool fibonacci, string activeTool, double x, double y, PointerButton button)
    {
        var (_, layout, toolbar) = BuildHarness();
        toolbar.Activate(activeTool);
        InteractionModifier modifier = fibonacci
            ? new FibonacciDrawingModifier("c1", layout, _ => { }, toolbar)
            : new LevelDrawingModifier("c1", layout, _ => { }, toolbar);

        Assert.False(modifier.HandlesPointerPressed(new PointerInputArgs(x, y, button, ModifierKeys.None)));
    }
}
