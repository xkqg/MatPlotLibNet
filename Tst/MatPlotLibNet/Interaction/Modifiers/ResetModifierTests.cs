// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public class ResetModifierTests
{
    private static (ResetModifier modifier, List<FigureInteractionEvent> events) Make()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var events = new List<FigureInteractionEvent>();
        return (new ResetModifier("chart-1", layout, events.Add), events);
    }

    [Fact]
    public void HandlesPointerPressed_DoubleClick_InsidePlot_ReturnsTrue()
    {
        var (m, _) = Make();
        var args = new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None, ClickCount: 2);
        Assert.True(m.HandlesPointerPressed(args));
    }

    [Fact]
    public void HandlesPointerPressed_SingleClick_ReturnsFalse()
    {
        var (m, _) = Make();
        var args = new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None, ClickCount: 1);
        Assert.False(m.HandlesPointerPressed(args));
    }

    [Fact]
    public void HandlesPointerPressed_DoubleClick_OutsidePlot_ReturnsFalse()
    {
        var (m, _) = Make();
        var args = new PointerInputArgs(5, 5, PointerButton.Left, ModifierKeys.None, ClickCount: 2);
        Assert.False(m.HandlesPointerPressed(args));
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

    /// <summary>OnPointerMoved/OnPointerReleased are no-ops — verify no event emitted.</summary>
    [Fact]
    public void OnPointerMoved_AndReleased_AreNoOps()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None, ClickCount: 1));
        m.OnPointerReleased(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None, ClickCount: 1));
        Assert.Empty(events);
    }

    /// <summary>Scroll is not handled by ResetModifier — verify both API calls are inert.</summary>
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
