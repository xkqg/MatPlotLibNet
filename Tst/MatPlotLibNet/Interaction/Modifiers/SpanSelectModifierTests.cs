// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public sealed class SpanSelectModifierTests : InteractionModifierTests<SpanSelectModifier>
{
    protected override SpanSelectModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    /// <summary>A drag sends one span event with the X range it spans, low end first whichever way it was dragged.</summary>
    [Theory]
    [InlineData(60, 130)]
    [InlineData(130, 60)]
    public void Release_EmitsOneSpanSelectEvent_WithNormalisedXRange(double fromX, double toX)
    {
        var (mod, events) = Make(FigureLayout.Wide());
        Drag(mod, ModifierKeys.Alt, fromX, 50, toX, 60);

        var evt = Assert.IsType<SpanSelectEvent>(Assert.Single(events));
        Assert.Equal("c1", evt.ChartId);
        Assert.True(evt.XMin < evt.XMax);
    }

    /// <summary>SpanSelectModifier — same shape: OnPointerMoved without
    /// press AND OnPointerReleased after inside-press with outside-release
    /// (endCoords-null).</summary>
    [Fact]
    public void SpanSelect_OnPointerMoved_WithoutPress_AndReleased_OutsideAfterPress()
    {
        var (m, events) = Make();

        // Move without press.
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));

        // Press inside, release outside.
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.Shift));
        Assert.Empty(events);
    }
}
