// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>Phase H.2 of the v1.7.2 follow-on plan — behavioural coverage for
/// <see cref="SpanSelectModifier"/>. Pre-fix only the <c>Event</c> / <c>State</c>
/// records had tests; the modifier's Alt+drag lifecycle (state transitions,
/// tiny-drag suppression, hit-test) had zero coverage.</summary>
public class SpanSelectModifierTests
{
    private static (SpanSelectModifier mod, List<FigureInteractionEvent> events) Setup()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.ChartId = "c";
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 100;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 50;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 200, 100)]);
        var events = new List<FigureInteractionEvent>();
        return (new SpanSelectModifier("c", layout, events.Add), events);
    }

    private static PointerInputArgs P(double x, double y, ModifierKeys mods = ModifierKeys.Alt) =>
        new(x, y, PointerButton.Left, mods);

    [Fact]
    public void HandlesPointerPressed_AltLeftInsidePlot_ReturnsTrue()
    {
        var (mod, _) = Setup();
        Assert.True(mod.HandlesPointerPressed(P(50, 50)));
    }

    [Fact]
    public void HandlesPointerPressed_WithoutAlt_ReturnsFalse()
    {
        var (mod, _) = Setup();
        Assert.False(mod.HandlesPointerPressed(P(50, 50, ModifierKeys.None)));
    }

    [Fact]
    public void ActiveSpan_Lifecycle_NullBeforePress_NonNullDuringDrag_NullAfterRelease()
    {
        var (mod, _) = Setup();
        Assert.Null(mod.ActiveSpan);

        mod.OnPointerPressed(P(50, 50));
        Assert.NotNull(mod.ActiveSpan);
        Assert.Equal(50, mod.ActiveSpan!.Value.StartPixelX);
        Assert.Equal(50, mod.ActiveSpan.Value.CurrentPixelX);

        mod.OnPointerMoved(P(120, 60));
        Assert.Equal(120, mod.ActiveSpan!.Value.CurrentPixelX);

        mod.OnPointerReleased(P(120, 60));
        Assert.Null(mod.ActiveSpan);
    }

    [Fact]
    public void Release_EmitsSpanSelectEvent_WithNormalisedXRange()
    {
        var (mod, events) = Setup();
        mod.OnPointerPressed(P(60, 50));
        mod.OnPointerMoved(P(130, 60));
        mod.OnPointerReleased(P(130, 60));

        Assert.Single(events);
        var evt = Assert.IsType<SpanSelectEvent>(events[0]);
        Assert.Equal("c", evt.ChartId);
        Assert.True(evt.XMin < evt.XMax);
    }

    [Fact]
    public void Release_ReverseDrag_StillEmitsNormalisedXRange()
    {
        var (mod, events) = Setup();
        mod.OnPointerPressed(P(130, 50));
        mod.OnPointerMoved(P(60, 60));
        mod.OnPointerReleased(P(60, 60));

        var evt = Assert.IsType<SpanSelectEvent>(events[0]);
        Assert.True(evt.XMin < evt.XMax);
    }

    [Fact]
    public void Release_TinyDragUnderTwoPixels_IsIgnored()
    {
        var (mod, events) = Setup();
        mod.OnPointerPressed(P(60, 50));
        mod.OnPointerMoved(P(61, 50));
        mod.OnPointerReleased(P(61, 50));

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
