// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public sealed class HoverModifierTests : InteractionModifierTests<HoverModifier>
{
    protected override HoverModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    [Fact]
    public void OnPointerMoved_NoButton_InsidePlot_ProducesHoverEvent()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        Assert.Single(events);
        Assert.IsType<HoverEvent>(events[0]);
    }

    /// <summary>A hover is a move with no button down inside the plot: a move outside the plot, or with the left button down (a drag), sends no event.</summary>
    [Theory]
    [InlineData(5, 5, PointerButton.None)]
    [InlineData(60, 35, PointerButton.Left)]
    public void OnPointerMoved_OutsideThePlotOrWithAButtonDown_ProducesNoEvent(double x, double y, PointerButton button)
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(x, y, button, ModifierKeys.None));
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
        Assert.Equal("c1", events[0].ChartId);
    }

}
