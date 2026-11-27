// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public sealed class HoverModifierTests : InteractionModifierTests<HoverModifier>
{
    protected override HoverModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, fig, sink);

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

    /// <summary>The hover owns the tooltip of the point under the pointer: a move with no button over the plot names the nearest
    /// point, a move off the plot clears it, a move with a button down (a drag) leaves it as it was, and a hover made without the
    /// figure has no points to name.</summary>
    [Fact]
    public void TheTooltip_NamesTheNearestPoint_IsClearedOffThePlot_AndIsLeftAloneDuringADrag()
    {
        var (m, _) = Make();
        InteractionModifier hover = m;

        hover.OnPointerTracked(new PointerInputArgs(20, 30, PointerButton.None, ModifierKeys.None));
        var named = hover.ActiveTooltip;
        hover.OnPointerTracked(new PointerInputArgs(80, 50, PointerButton.Left, ModifierKeys.None));
        var duringTheDrag = hover.ActiveTooltip;
        hover.OnPointerTracked(new PointerInputArgs(500, 500, PointerButton.None, ModifierKeys.None));

        Assert.Equal(0, named?.PointIndex);
        Assert.Equal(0, named?.SeriesIndex);
        Assert.Same(named, duringTheDrag);
        Assert.Null(hover.ActiveTooltip);
        var withoutTheFigure = new HoverModifier("c1", FigureLayout.Plain().Layout, _ => { });
        withoutTheFigure.OnPointerTracked(new PointerInputArgs(20, 30, PointerButton.None, ModifierKeys.None));
        Assert.Null(withoutTheFigure.ActiveTooltip);
    }

    [Fact]
    public void HoverEvent_HasCorrectChartId()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        Assert.Equal("c1", events[0].ChartId);
    }

}
