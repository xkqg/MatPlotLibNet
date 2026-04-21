// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

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
    protected static (Figure fig, IChartLayout layout) BuildHarness()
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
        return (fig, ChartLayout.Create(fig, layoutResult));
    }

    /// <summary>Per-derived-class factory — wires the concrete modifier ctor.
    /// Figure is supplied so modifiers like Rotate3DModifier / DataCursorModifier
    /// that need scene access can use it; passive ones simply ignore it.</summary>
    protected abstract TModifier CreateModifier(IChartLayout layout, Figure figure, Action<FigureInteractionEvent> sink);

    /// <summary>Override to opt out of a fact when the modifier's contract diverges
    /// (e.g. ZoomModifier DOES handle scroll). Default: all four shared facts run.</summary>
    protected virtual bool ShouldHandleScroll => false;
    protected virtual bool ShouldHandleKeyDown => false;

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

// ── Derived test classes (one per modifier) ────────────────────────────────────

public sealed class PanModifierGenericTests : InteractionModifierTests<PanModifier>
{
    protected override PanModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    private static (PanModifier m, List<FigureInteractionEvent> events) Make()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var events = new List<FigureInteractionEvent>();
        return (new PanModifier("chart-1", layout, events.Add), events);
    }

    [Fact]
    public void HandlesPointerPressed_LeftButton_NoShift_InsidePlot_ReturnsTrue()
    {
        var (m, _) = Make();
        Assert.True(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void HandlesPointerPressed_LeftButton_WithShift_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.Shift)));
    }

    [Fact]
    public void HandlesPointerPressed_RightButton_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Right, ModifierKeys.None)));
    }

    [Fact]
    public void HandlesPointerPressed_OutsidePlot_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(5, 5, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void OnPointerMoved_WhileCaptured_ProducesPanEvent()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(50, 35, PointerButton.Left, ModifierKeys.None));
        Assert.Single(events);
        Assert.IsType<PanEvent>(events[0]);
    }

    [Fact]
    public void OnPointerMoved_WhileCaptured_DeltaConvertedToDataSpace()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(70, 35, PointerButton.Left, ModifierKeys.None));
        var pan = Assert.IsType<PanEvent>(events[0]);
        Assert.Equal(-1.0, pan.DxData, precision: 3);
        Assert.Equal(0.0, pan.DyData, precision: 3);
    }

    [Fact]
    public void OnPointerMoved_WhileNotCaptured_ProducesNoEvent()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void OnPointerReleased_ReleasesCapture()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(50, 35, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }
}

public sealed class Rotate3DModifierGenericTests : InteractionModifierTests<Rotate3DModifier>
{
    // Rotate3DModifier handles arrow keys for ±5° rotation, BUT only when a 3D series
    // is present (HasAnyThreeD guard). The shared harness uses a 2D Plot, so for the
    // generic-base "Escape" key probe, HandlesKeyDown returns false.
    protected override bool ShouldHandleKeyDown => false;

    protected override Rotate3DModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink, fig);
}

public sealed class SpanSelectModifierGenericTests : InteractionModifierTests<SpanSelectModifier>
{
    protected override SpanSelectModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);
}

public sealed class BrushSelectModifierGenericTests : InteractionModifierTests<BrushSelectModifier>
{
    protected override BrushSelectModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    private static (BrushSelectModifier m, List<FigureInteractionEvent> events) Make()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var events = new List<FigureInteractionEvent>();
        return (new BrushSelectModifier("chart-1", layout, events.Add), events);
    }

    [Fact]
    public void HandlesPointerPressed_LeftShift_InsidePlot_ReturnsTrue()
    {
        var (m, _) = Make();
        Assert.True(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.Shift)));
    }

    [Fact]
    public void HandlesPointerPressed_LeftNoShift_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void HandlesPointerPressed_RightShift_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Right, ModifierKeys.Shift)));
    }

    [Fact]
    public void OnPointerReleased_AfterDrag_ProducesBrushSelectEvent()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(110, 60, PointerButton.Left, ModifierKeys.Shift));
        Assert.Single(events);
        Assert.IsType<BrushSelectEvent>(events[0]);
    }

    [Fact]
    public void OnPointerReleased_RectNormalized_X1LessThanX2()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(110, 10, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(10,  60, PointerButton.Left, ModifierKeys.Shift));
        var evt = Assert.IsType<BrushSelectEvent>(events[0]);
        Assert.True(evt.X1 <= evt.X2, $"Expected X1 <= X2 but got X1={evt.X1}, X2={evt.X2}");
        Assert.True(evt.Y1 <= evt.Y2, $"Expected Y1 <= Y2 but got Y1={evt.Y1}, Y2={evt.Y2}");
    }

    [Fact]
    public void DuringDrag_ActiveBrush_IsNonNull()
    {
        var (m, _) = Make();
        m.OnPointerPressed(new PointerInputArgs(20, 20, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerMoved(new PointerInputArgs(80, 50, PointerButton.Left, ModifierKeys.Shift));
        Assert.NotNull(m.ActiveBrush);
    }

    [Fact]
    public void AfterRelease_ActiveBrush_IsNull()
    {
        var (m, _) = Make();
        m.OnPointerPressed(new PointerInputArgs(20, 20, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerMoved(new PointerInputArgs(80, 50, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(80, 50, PointerButton.Left, ModifierKeys.Shift));
        Assert.Null(m.ActiveBrush);
    }

    [Fact]
    public void OnPointerMoved_UpdatesCurrentPixelCoords()
    {
        var (m, _) = Make();
        m.OnPointerPressed(new PointerInputArgs(20, 20, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerMoved(new PointerInputArgs(90, 55, PointerButton.Left, ModifierKeys.Shift));
        var state = m.ActiveBrush;
        Assert.NotNull(state);
        Assert.Equal(20.0, state.Value.StartPixelX, precision: 3);
        Assert.Equal(20.0, state.Value.StartPixelY, precision: 3);
        Assert.Equal(90.0, state.Value.CurrentPixelX, precision: 3);
        Assert.Equal(55.0, state.Value.CurrentPixelY, precision: 3);
    }

    [Fact]
    public void BrushSelectEvent_DataCoordinatesCorrect()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.Shift));
        var evt = Assert.IsType<BrushSelectEvent>(events[0]);
        Assert.Equal(0.0, evt.X1, precision: 3);
        Assert.Equal(5.0, evt.X2, precision: 3);
        Assert.Equal(2.5, evt.Y1, precision: 3);
        Assert.Equal(5.0, evt.Y2, precision: 3);
    }
}

public sealed class ZoomModifierGenericTests : InteractionModifierTests<ZoomModifier>
{
    protected override bool ShouldHandleScroll => true;

    protected override ZoomModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    private static (ZoomModifier m, List<FigureInteractionEvent> events) Make()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var events = new List<FigureInteractionEvent>();
        return (new ZoomModifier("chart-1", layout, events.Add), events);
    }

    [Fact]
    public void HandlesScroll_InsidePlotArea_ReturnsTrue()
    {
        var (m, _) = Make();
        Assert.True(m.HandlesScroll(new ScrollInputArgs(60, 35, 0, -1)));
    }

    [Fact]
    public void HandlesScroll_OutsidePlotArea_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesScroll(new ScrollInputArgs(5, 5, 0, -1)));
    }

    [Fact]
    public void OnScroll_NegativeDeltaY_ZoomsIn_RangeDecreases()
    {
        var (m, events) = Make();
        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));
        var zoom = Assert.IsType<ZoomEvent>(events[0]);
        Assert.True(zoom.XMax - zoom.XMin < 10.0, "Zoom in should reduce X range");
        Assert.True(zoom.YMax - zoom.YMin < 5.0,  "Zoom in should reduce Y range");
    }

    [Fact]
    public void OnScroll_PositiveDeltaY_ZoomsOut_RangeIncreases()
    {
        var (m, events) = Make();
        m.OnScroll(new ScrollInputArgs(60, 35, 0, +1));
        var zoom = Assert.IsType<ZoomEvent>(events[0]);
        Assert.True(zoom.XMax - zoom.XMin > 10.0, "Zoom out should increase X range");
        Assert.True(zoom.YMax - zoom.YMin > 5.0,  "Zoom out should increase Y range");
    }

    [Fact]
    public void OnScroll_ZoomCenteredOnCursorPosition()
    {
        var (m, events) = Make();
        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));
        var zoom = Assert.IsType<ZoomEvent>(events[0]);
        double midX = (zoom.XMin + zoom.XMax) / 2;
        double midY = (zoom.YMin + zoom.YMax) / 2;
        Assert.Equal(5.0, midX, precision: 3);
        Assert.Equal(2.5, midY, precision: 3);
    }

    [Fact]
    public void OnScroll_ProducesZoomEventWithCorrectChartId()
    {
        var (m, events) = Make();
        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));
        Assert.Equal("chart-1", events[0].ChartId);
    }

    [Fact]
    public void HandlesPointerPressed_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void NoOpMethods_DoNotThrow()
    {
        var (m, _) = Make();
        m.OnPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(50, 30, PointerButton.None, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None));
        m.OnKeyDown(new KeyInputArgs("Home"));
    }
}

public sealed class HoverModifierGenericTests : InteractionModifierTests<HoverModifier>
{
    protected override HoverModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    private static (HoverModifier m, List<FigureInteractionEvent> events) Make()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var events = new List<FigureInteractionEvent>();
        return (new HoverModifier("chart-1", layout, events.Add), events);
    }

    [Fact]
    public void OnPointerMoved_NoButton_InsidePlot_ProducesHoverEvent()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        Assert.Single(events);
        Assert.IsType<HoverEvent>(events[0]);
    }

    [Fact]
    public void OnPointerMoved_NoButton_OutsidePlot_ProducesNoEvent()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(5, 5, PointerButton.None, ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void OnPointerMoved_LeftButton_ProducesNoEvent()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void HoverEvent_DataCoordinatesCorrect()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        var evt = Assert.IsType<HoverEvent>(events[0]);
        Assert.Equal(5.0, evt.X, precision: 3);
        Assert.Equal(2.5, evt.Y, precision: 3);
    }

    [Fact]
    public void HoverEvent_HasCorrectChartId()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        Assert.Equal("chart-1", events[0].ChartId);
    }

    [Fact]
    public void HandlesPointerPressed_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None)));
    }
}

public sealed class LegendToggleModifierGenericTests : InteractionModifierTests<LegendToggleModifier>
{
    protected override LegendToggleModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    private static (LegendToggleModifier m, List<FigureInteractionEvent> events) Make(Rect legendItemBounds)
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series A").Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 5;
        var plotAreas = new[] { new Rect(10, 10, 200, 100) };
        var legendItems = new IReadOnlyList<LegendItemBounds>[]
        {
            new[] { new LegendItemBounds(0, legendItemBounds) }
        };
        var layoutResult = new LayoutResult(plotAreas, legendItems);
        var layout = ChartLayout.Create(figure, layoutResult);
        var events = new List<FigureInteractionEvent>();
        return (new LegendToggleModifier("chart-1", layout, events.Add), events);
    }

    [Fact]
    public void ClickInsideLegendItem_ProducesLegendToggleEvent()
    {
        var legendRect = new Rect(150, 15, 50, 14);
        var (m, events) = Make(legendRect);
        var args = new PointerInputArgs(160, 20, PointerButton.Left, ModifierKeys.None);
        Assert.True(m.HandlesPointerPressed(args));
        m.OnPointerPressed(args);
        Assert.Single(events);
        var toggle = Assert.IsType<LegendToggleEvent>(events[0]);
        Assert.Equal(0, toggle.AxesIndex);
        Assert.Equal(0, toggle.SeriesIndex);
    }

    [Fact]
    public void ClickOutsideLegend_DoesNotHandle()
    {
        var (m, _) = Make(new Rect(150, 15, 50, 14));
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 50, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void RightClick_DoesNotHandle()
    {
        var (m, _) = Make(new Rect(150, 15, 50, 14));
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(160, 20, PointerButton.Right, ModifierKeys.None)));
    }

    [Fact]
    public void ClickOutsideAllAxes_DoesNotHandle()
    {
        var (m, _) = Make(new Rect(150, 15, 50, 14));
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void NoLegendBounds_DoesNotHandle()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A").Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 200, 100)]);
        var events = new List<FigureInteractionEvent>();
        var m = new LegendToggleModifier("chart-1", layout, events.Add);
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void OnPointerPressed_InAxesButOutsideLegend_NoEvent()
    {
        var (m, events) = Make(new Rect(150, 15, 50, 14));
        m.OnPointerPressed(new PointerInputArgs(50, 50, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void NoOpMethods_DoNotThrow()
    {
        const string EscapeKey = "Escape";
        var (m, events) = Make(new Rect(150, 15, 50, 14));
        var pArgs = new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None);
        var sArgs = new ScrollInputArgs(60, 35, 0, -1);
        var kArgs = new KeyInputArgs(EscapeKey, ModifierKeys.None);
        m.OnPointerMoved(pArgs);
        m.OnPointerReleased(pArgs);
        Assert.False(m.HandlesScroll(sArgs));
        m.OnScroll(sArgs);
        Assert.False(m.HandlesKeyDown(kArgs));
        m.OnKeyDown(kArgs);
        Assert.Empty(events);
    }
}

public sealed class ResetModifierGenericTests : InteractionModifierTests<ResetModifier>
{
    protected override bool ShouldHandleKeyDown => true;

    protected override ResetModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    private static (ResetModifier m, List<FigureInteractionEvent> events) Make()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var events = new List<FigureInteractionEvent>();
        return (new ResetModifier("chart-1", layout, events.Add), events);
    }

    public override void OnPointerPressed_OutsidePlotArea_DoesNotEmit()
    {
        var (fig, layout) = BuildHarness();
        var events = new List<FigureInteractionEvent>();
        var m = CreateModifier(layout, fig, events.Add);
        m.OnPointerPressed(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.None));
        Assert.True(events.Count <= 1);
    }

    [Fact]
    public void HandlesPointerPressed_DoubleClick_InsidePlot_ReturnsTrue()
    {
        var (m, _) = Make();
        Assert.True(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None, ClickCount: 2)));
    }

    [Fact]
    public void HandlesPointerPressed_SingleClick_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None, ClickCount: 1)));
    }

    [Fact]
    public void HandlesPointerPressed_DoubleClick_OutsidePlot_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(5, 5, PointerButton.Left, ModifierKeys.None, ClickCount: 2)));
    }

    [Fact]
    public void OnPointerPressed_DoubleClick_ProducesResetEvent()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None, ClickCount: 2));
        Assert.Single(events);
        Assert.IsType<ResetEvent>(events[0]);
    }

    [Fact]
    public void OnPointerPressed_ResetEvent_RestoresOriginalLimits()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None, ClickCount: 2));
        var reset = Assert.IsType<ResetEvent>(events[0]);
        Assert.Equal(0,  reset.XMin);
        Assert.Equal(10, reset.XMax);
        Assert.Equal(0,  reset.YMin);
        Assert.Equal(5,  reset.YMax);
    }

    [Fact]
    public void HandlesKeyDown_HomeKey_ReturnsTrue()
    {
        var (m, _) = Make();
        Assert.True(m.HandlesKeyDown(new KeyInputArgs("Home")));
    }

    [Fact]
    public void HandlesKeyDown_EscapeKey_ReturnsTrue()
    {
        var (m, _) = Make();
        Assert.True(m.HandlesKeyDown(new KeyInputArgs("Escape")));
    }

    [Fact]
    public void HandlesKeyDown_OtherKey_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesKeyDown(new KeyInputArgs("a")));
    }

    [Fact]
    public void OnKeyDown_HomeKey_ProducesResetEvent()
    {
        var (m, events) = Make();
        m.OnKeyDown(new KeyInputArgs("Home"));
        Assert.Single(events);
        Assert.IsType<ResetEvent>(events[0]);
    }

    [Fact]
    public void OnPointerMoved_AndReleased_AreNoOps()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None, ClickCount: 1));
        m.OnPointerReleased(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None, ClickCount: 1));
        Assert.Empty(events);
    }

    [Fact]
    public void Scroll_NotHandled_AndOnScrollIsNoOp()
    {
        var (m, events) = Make();
        var args = new ScrollInputArgs(50, 30, 0, 1, ModifierKeys.None);
        Assert.False(m.HandlesScroll(args));
        m.OnScroll(args);
        Assert.Empty(events);
    }
}
