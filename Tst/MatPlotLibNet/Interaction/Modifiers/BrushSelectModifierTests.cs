// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public sealed class BrushSelectModifierTests : InteractionModifierTests<BrushSelectModifier>
{
    protected override BrushSelectModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    /// <summary>A drag across the plot sends one brush event with the data rectangle it spans, low ends first whichever way it was dragged:
    /// the plot is 0 to 10 across and 0 to 5 up in the pixel rectangle 10, 10, 100 by 50.</summary>
    [Theory]
    [InlineData(10, 10, 110, 60, 0.0, 0.0, 10.0, 5.0)]
    [InlineData(110, 10, 10, 60, 0.0, 0.0, 10.0, 5.0)]
    [InlineData(10, 10, 60, 35, 0.0, 2.5, 5.0, 5.0)]
    public void ADrag_SendsOneBrushEvent_WithTheDataRectangleItSpans_LowEndsFirst(
        double fromX, double fromY, double toX, double toY, double x1, double y1, double x2, double y2)
    {
        var (m, events) = Make();
        Drag(m, ModifierKeys.Shift, fromX, fromY, toX, toY);

        var evt = Assert.IsType<BrushSelectEvent>(Assert.Single(events));
        Assert.Equal(x1, evt.X1, precision: 3);
        Assert.Equal(y1, evt.Y1, precision: 3);
        Assert.Equal(x2, evt.X2, precision: 3);
        Assert.Equal(y2, evt.Y2, precision: 3);
    }

    /// <summary>A move with no press before it, and a release outside the plot after a press inside it, send nothing.</summary>
    [Fact]
    public void AMoveWithoutAPress_AndAReleaseOutsideAfterAPressInside_SendNothing() => AssertNothingIsSent(m =>
    {
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.Shift));
    });
}
