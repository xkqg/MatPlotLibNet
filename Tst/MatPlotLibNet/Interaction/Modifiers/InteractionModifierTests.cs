// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary>A modifier under test, the events it has sent and the figure it works on.</summary>
/// <typeparam name="TModifier">The modifier.</typeparam>
/// <param name="Modifier">The modifier.</param>
/// <param name="Events">Every event the modifier has sent, in order.</param>
/// <param name="Figure">The figure the modifier works on.</param>
public readonly record struct ModifierRig<TModifier>(TModifier Modifier, List<FigureInteractionEvent> Events, Figure Figure)
{
    /// <summary>The modifier and its events, for a test that does not read the figure.</summary>
    /// <param name="modifier">The modifier.</param>
    /// <param name="events">Every event the modifier has sent, in order.</param>
    public void Deconstruct(out TModifier modifier, out List<FigureInteractionEvent> events)
    {
        modifier = Modifier;
        events = Events;
    }
}

/// <summary>Phase X.8 (v1.7.2, 2026-04-19) — stacked-OO contract tests for the
/// 7 <see cref="IInteractionModifier"/> implementations that share the
/// <c>(string chartId, IChartLayout layout, Action&lt;FigureInteractionEvent&gt; sink)</c>
/// constructor signature: Pan, Rotate3D, SpanSelect, BrushSelect, Zoom, Hover,
/// LegendToggle (and ResetModifier as a passive form). Each derived test class
/// overrides only <see cref="CreateModifier"/>; the shared theories below cover
/// the cross-cutting branches that every modifier MUST honour:
///
/// 1. <c>OnPointerReleased</c> without a prior <c>OnPointerPressed</c> is a no-op.
/// 2. The default <c>HandlesScroll</c>/<c>HandlesKeyDown</c> return false for
///    modifiers that don't claim those gestures (most of them).
/// 3. The default <c>OnScroll</c>/<c>OnKeyDown</c> empty bodies don't throw.
/// 4. <c>OnPointerPressed</c> outside the plot area does not emit events.
///
/// Phase L.6 (v1.7.2, 2026-04-21) — migrated all modifier-specific facts from the
/// 6 legacy standalone files (Pan, Hover, BrushSelect, Zoom, LegendToggle, Reset)
/// into the corresponding sealed subclasses below.</summary>
/// <typeparam name="TModifier">The concrete modifier under test.</typeparam>
public abstract class InteractionModifierTests<TModifier> where TModifier : IInteractionModifier
{
    /// <summary>Builds a single-subplot figure with axes [0,10]×[0,5] mapped to
    /// pixel rect (10,10)–(110,60). Shared by all derived tests so each modifier
    /// runs against the same well-known layout.</summary>
    protected static FigureLayout BuildHarness()
    {
        var fig = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "S").Build();
        fig.SubPlots[0].XAxis.Min = 0;
        fig.SubPlots[0].XAxis.Max = 10;
        fig.SubPlots[0].YAxis.Min = 0;
        fig.SubPlots[0].YAxis.Max = 5;
        var plotAreas = new[] { new Rect(10, 10, 100, 50) };
        var legendItems = new IReadOnlyList<LegendItemBounds>[]
        {
            new[] { new LegendItemBounds(0, new Rect(80, 12, 25, 12)) }
        };
        var layoutResult = new LayoutResult(plotAreas, legendItems);
        return new FigureLayout(fig, ChartLayout.Create(fig, layoutResult));
    }

    /// <summary>The modifier of the class under test over the layout of <see cref="BuildHarness"/> without a legend (axes 0 to 10 by 0
    /// to 5 in the pixel rectangle 10, 10, 100 by 50), and the list every event it sends is added to.</summary>
    protected ModifierRig<TModifier> Make() => Make(FigureLayout.Plain());

    /// <summary>The modifier of the class under test over <paramref name="over"/>, and the list every event it sends is added to.</summary>
    /// <param name="over">The figure and its layout.</param>
    protected ModifierRig<TModifier> Make(FigureLayout over)
    {
        var events = new List<FigureInteractionEvent>();
        return new ModifierRig<TModifier>(CreateModifier(over.Layout, over.Figure, events.Add), events, over.Figure);
    }

    /// <summary>Presses <paramref name="m"/> at one pixel, moves it to another and releases it there, the key held all the way: the drag
    /// every rubber-band and pan test makes.</summary>
    /// <param name="m">The modifier.</param>
    /// <param name="keys">The keys held.</param>
    /// <param name="fromX">The X of the pixel the drag starts at.</param>
    /// <param name="fromY">The Y of the pixel the drag starts at.</param>
    /// <param name="toX">The X of the pixel it ends at.</param>
    /// <param name="toY">The Y of the pixel it ends at.</param>
    protected static void Drag(TModifier m, ModifierKeys keys, double fromX, double fromY, double toX, double toY)
    {
        m.OnPointerPressed(new PointerInputArgs(fromX, fromY, PointerButton.Left, keys));
        m.OnPointerMoved(new PointerInputArgs(toX, toY, PointerButton.Left, keys));
        m.OnPointerReleased(new PointerInputArgs(toX, toY, PointerButton.Left, keys));
    }

    /// <summary>Makes the modifier over the plain layout, lets <paramref name="gesture"/> work it, and checks it sent nothing.</summary>
    /// <param name="gesture">The gestures to make.</param>
    protected void AssertNothingIsSent(Action<TModifier> gesture)
    {
        var (m, events) = Make();
        gesture(m);
        Assert.Empty(events);
    }

    /// <summary>Per-derived-class factory — wires the concrete modifier ctor.
    /// Figure is supplied so modifiers like Rotate3DModifier / DataCursorModifier
    /// that need scene access can use it; passive ones simply ignore it.</summary>
    protected abstract TModifier CreateModifier(IChartLayout layout, Figure figure, Action<FigureInteractionEvent> sink);

    /// <summary>Override to opt out of a fact when the modifier's contract diverges
    /// (e.g. ZoomModifier DOES handle scroll). Default: all four shared facts run.</summary>
    protected virtual bool ShouldHandleScroll => false;
    protected virtual bool ShouldHandleKeyDown => false;

    /// <summary>Whether the modifier claims a plain left press inside the plot (no key held). Default: it does not.</summary>
    protected virtual bool ShouldHandleAPlainLeftPress => false;

    [Fact]
    public void OnPointerReleased_WithoutPriorPress_DoesNotEmit()
    {
        var (fig, layout) = BuildHarness();
        var events = new List<FigureInteractionEvent>();
        var m = CreateModifier(layout, fig, events.Add);
        m.OnPointerReleased(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void HandlesScroll_DefaultsToConfiguredValue()
    {
        var (fig, layout) = BuildHarness();
        var m = CreateModifier(layout, fig, _ => { });
        Assert.Equal(ShouldHandleScroll, m.HandlesScroll(new ScrollInputArgs(60, 35, 0, -1)));
    }

    [Fact]
    public void HandlesKeyDown_DefaultsToConfiguredValue()
    {
        var (fig, layout) = BuildHarness();
        var m = CreateModifier(layout, fig, _ => { });
        const string EscapeKey = "Escape";
        Assert.Equal(ShouldHandleKeyDown, m.HandlesKeyDown(new KeyInputArgs(EscapeKey, ModifierKeys.None)));
    }

    [Fact]
    public void HandlesPointerPressed_APlainLeftPressInThePlot_IsConfigured()
    {
        var (m, _) = Make();
        Assert.Equal(ShouldHandleAPlainLeftPress, m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public virtual void OnPointerPressed_OutsidePlotArea_DoesNotEmit()
    {
        var (fig, layout) = BuildHarness();
        var events = new List<FigureInteractionEvent>();
        var m = CreateModifier(layout, fig, events.Add);
        m.OnPointerPressed(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(600, 600, PointerButton.Left, ModifierKeys.Shift));
        Assert.Empty(events);
    }

    [Fact]
    public void OnScroll_OnKeyDown_NoOp_DoNotThrow()
    {
        var (fig, layout) = BuildHarness();
        var events = new List<FigureInteractionEvent>();
        var m = CreateModifier(layout, fig, events.Add);
        const string EscapeKey = "Escape";

        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));
        m.OnKeyDown(new KeyInputArgs(EscapeKey, ModifierKeys.None));

        if (!ShouldHandleScroll && !ShouldHandleKeyDown)
            Assert.Empty(events);
    }
}


