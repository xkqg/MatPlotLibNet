// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public sealed class ResetModifierTests : InteractionModifierTests<ResetModifier>
{
    protected override bool ShouldHandleKeyDown => true;

    protected override ResetModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    public override void OnPointerPressed_OutsidePlotArea_DoesNotEmit()
    {
        var (fig, layout) = BuildHarness();
        var events = new List<FigureInteractionEvent>();
        var m = CreateModifier(layout, fig, events.Add);
        m.OnPointerPressed(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.None));
        Assert.True(events.Count <= 1);
    }

    /// <summary>A reset is a double click inside the plot: a single click, or a double click outside it, is not one.</summary>
    [Theory]
    [InlineData(50, 30, 2, true)]
    [InlineData(50, 30, 1, false)]
    [InlineData(5, 5, 2, false)]
    public void HandlesPointerPressed_OnlyADoubleClickInsideThePlot(double x, double y, int clicks, bool handled)
    {
        var (m, _) = Make();
        Assert.Equal(handled, m.HandlesPointerPressed(new PointerInputArgs(x, y, PointerButton.Left, ModifierKeys.None, ClickCount: clicks)));
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

    /// <summary>ROADMAP.md row 100: resetting must restore a secondary axis to the range it had when the modifier
    /// was created, not leave it at whatever a zoom/pan moved it to.</summary>
    [Fact]
    public void OnPointerPressed_ResetEvent_RestoresASecondaryAxisToItsOriginalRangeToo()
    {
        var (m, events, figure) = Make(FigureLayout.WithARightAxis());

        // Move the secondary axis away from its original range before resetting.
        figure.SubPlots[0].YAxes.Right[0].Axis.Min = 123;
        figure.SubPlots[0].YAxes.Right[0].Axis.Max = 456;

        m.OnPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None, ClickCount: 2));

        var reset = Assert.IsType<ResetEvent>(events[0]);
        var secondary = Assert.Single(reset.SecondaryRanges!);
        Assert.Equal(AxisSide.Right, secondary.Side);
        Assert.Equal(0.0, secondary.Range.YMin);
        Assert.Equal(500.0, secondary.Range.YMax);

        reset.ApplyTo(figure);
        Assert.Equal(0.0, figure.SubPlots[0].YAxes.Right[0].Axis.Min);
        Assert.Equal(500.0, figure.SubPlots[0].YAxes.Right[0].Axis.Max);
    }

    /// <summary>Home and Escape reset the view; another key does not.</summary>
    [Theory]
    [InlineData("Home", true)]
    [InlineData("Escape", true)]
    [InlineData("a", false)]
    public void HandlesKeyDown_OnlyHomeAndEscape(string key, bool handled)
    {
        var (m, _) = Make();
        Assert.Equal(handled, m.HandlesKeyDown(new KeyInputArgs(key)));
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
