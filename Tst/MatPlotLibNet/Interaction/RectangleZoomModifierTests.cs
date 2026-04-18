// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>Phase H.1 of the v1.7.2 follow-on plan — behavioural coverage for
/// <see cref="RectangleZoomModifier"/>. Pre-fix only the <c>Event</c> record
/// and <c>State</c> record had tests; the modifier's drag lifecycle (start /
/// update / release / ignore-tiny-drag) had <b>zero</b> coverage.</summary>
public class RectangleZoomModifierTests
{
    private static (RectangleZoomModifier mod, List<FigureInteractionEvent> events) Setup()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.ChartId = "c";
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 100;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 50;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 200, 100)]);
        var events = new List<FigureInteractionEvent>();
        return (new RectangleZoomModifier("c", layout, events.Add), events);
    }

    private static PointerInputArgs P(double x, double y, ModifierKeys mods = ModifierKeys.Ctrl) =>
        new(x, y, PointerButton.Left, mods);

    [Fact]
    public void HandlesPointerPressed_CtrlLeftInsidePlot_ReturnsTrue()
    {
        var (mod, _) = Setup();
        Assert.True(mod.HandlesPointerPressed(P(50, 50)));
    }

    [Fact]
    public void HandlesPointerPressed_WithoutCtrl_ReturnsFalse()
    {
        var (mod, _) = Setup();
        Assert.False(mod.HandlesPointerPressed(P(50, 50, ModifierKeys.None)));
    }

    [Fact]
    public void HandlesPointerPressed_OutsidePlot_ReturnsFalse()
    {
        var (mod, _) = Setup();
        Assert.False(mod.HandlesPointerPressed(P(5, 5)));
    }

    [Fact]
    public void ActiveZoomRect_NullBeforePress_NonNullDuringDrag_NullAfterRelease()
    {
        var (mod, _) = Setup();
        Assert.Null(mod.ActiveZoomRect);

        mod.OnPointerPressed(P(50, 50));
        Assert.NotNull(mod.ActiveZoomRect);
        Assert.Equal(50, mod.ActiveZoomRect!.Value.StartPixelX);
        Assert.Equal(50, mod.ActiveZoomRect.Value.CurrentPixelX);

        mod.OnPointerMoved(P(120, 80));
        Assert.Equal(120, mod.ActiveZoomRect!.Value.CurrentPixelX);
        Assert.Equal(80, mod.ActiveZoomRect.Value.CurrentPixelY);

        mod.OnPointerReleased(P(120, 80));
        Assert.Null(mod.ActiveZoomRect);
    }

    [Fact]
    public void Release_EmitsRectangleZoomEvent_WithCorrectDataBounds()
    {
        var (mod, events) = Setup();
        mod.OnPointerPressed(P(60, 60));
        mod.OnPointerMoved(P(110, 90));
        mod.OnPointerReleased(P(110, 90));

        Assert.Single(events);
        var evt = Assert.IsType<RectangleZoomEvent>(events[0]);
        Assert.Equal("c", evt.ChartId);
        Assert.True(evt.XMin < evt.XMax);
        Assert.True(evt.YMin < evt.YMax);
    }

    [Fact]
    public void Release_ReverseDrag_StillEmitsNormalisedBounds()
    {
        // Drag from (110, 90) → (60, 60) — reversed. The emitted event must still
        // have XMin<XMax, YMin<YMax (sorted normalisation).
        var (mod, events) = Setup();
        mod.OnPointerPressed(P(110, 90));
        mod.OnPointerMoved(P(60, 60));
        mod.OnPointerReleased(P(60, 60));

        Assert.Single(events);
        var evt = (RectangleZoomEvent)events[0];
        Assert.True(evt.XMin < evt.XMax, $"expected normalised XMin<XMax, got {evt.XMin}/{evt.XMax}");
        Assert.True(evt.YMin < evt.YMax, $"expected normalised YMin<YMax, got {evt.YMin}/{evt.YMax}");
    }

    [Fact]
    public void Release_TinyDragUnderTwoPixels_IsIgnored()
    {
        var (mod, events) = Setup();
        mod.OnPointerPressed(P(60, 60));
        mod.OnPointerMoved(P(61, 60));     // 1 px drag
        mod.OnPointerReleased(P(61, 60));

        Assert.Empty(events);
    }

    [Fact]
    public void OnPointerMoved_BeforePress_IsNoop()
    {
        var (mod, _) = Setup();
        mod.OnPointerMoved(P(120, 80)); // no prior Pressed
        Assert.Null(mod.ActiveZoomRect);
    }

    [Fact]
    public void OnPointerReleased_BeforePress_IsNoop()
    {
        var (mod, events) = Setup();
        mod.OnPointerReleased(P(120, 80));
        Assert.Empty(events);
    }

    [Fact]
    public void HandlesScroll_AlwaysFalse()
    {
        var (mod, _) = Setup();
        Assert.False(mod.HandlesScroll(new ScrollInputArgs(50, 50, 0, -1)));
    }

    [Fact]
    public void HandlesKeyDown_AlwaysFalse()
    {
        var (mod, _) = Setup();
        Assert.False(mod.HandlesKeyDown(new KeyInputArgs("Home", ModifierKeys.None)));
    }
}
