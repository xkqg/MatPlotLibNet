// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public sealed class PanModifierTests : InteractionModifierTests<PanModifier>
{
    protected override bool ShouldHandleAPlainLeftPress => true;

    protected override PanModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    /// <summary>A pan starts on a left press inside the plot with no Shift (Shift is the brush's); a right press, and a press outside the plot,
    /// are not its.</summary>
    [Theory]
    [InlineData(50, 30, PointerButton.Left, ModifierKeys.None, true)]
    [InlineData(50, 30, PointerButton.Left, ModifierKeys.Shift, false)]
    [InlineData(50, 30, PointerButton.Right, ModifierKeys.None, false)]
    [InlineData(5, 5, PointerButton.Left, ModifierKeys.None, false)]
    public void HandlesPointerPressed_OnlyALeftPressWithoutShiftInsideThePlot(double x, double y, PointerButton button, ModifierKeys keys, bool handled)
    {
        var (m, _) = Make();
        Assert.Equal(handled, m.HandlesPointerPressed(new PointerInputArgs(x, y, button, keys)));
    }

    /// <summary>A move while the press is held sends one pan event, whose step is the pixels moved read in data units: the plot is 10 across in
    /// 100 pixels and 5 up in 50, and the view follows the hand, so a move to the right steps the range to the left.</summary>
    [Theory]
    [InlineData(50, 35, 1.0)]
    [InlineData(70, 35, -1.0)]
    public void OnPointerMoved_WhileCaptured_ProducesOnePanEvent_WithTheDeltaInDataSpace(double toX, double toY, double dx)
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(toX, toY, PointerButton.Left, ModifierKeys.None));

        var pan = Assert.IsType<PanEvent>(Assert.Single(events));
        Assert.Equal(dx, pan.DxData, precision: 3);
        Assert.Equal(0.0, pan.DyData, precision: 3);
    }

    /// <summary>A move with no press before it, and a move after the press was let go, send nothing.</summary>
    [Fact]
    public void OnPointerMoved_WhileNotCaptured_AndAfterTheRelease_ProducesNoEvent() => AssertNothingIsSent(m =>
    {
        m.OnPointerMoved(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None));
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(50, 35, PointerButton.Left, ModifierKeys.None));
    });

    /// <summary>ROADMAP.md row 100: a drag must move a secondary axis' own range too, scaled to ITS OWN span —
    /// right slot 0 here is [0,500], ten times the primary's [0,5], so the same pixel move must shift it by ten
    /// times as much in data units, not by the primary's own delta.</summary>
    [Fact]
    public void OnPointerMoved_WhileCaptured_AlsoMovesASecondaryAxisByItsOwnProportionalDelta()
    {
        var (m, events, figure) = Make(FigureLayout.WithARightAxis());

        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(60, 25, PointerButton.Left, ModifierKeys.None));

        var pan = Assert.IsType<PanEvent>(events[0]);
        var secondary = Assert.Single(pan.SecondaryRanges!);
        Assert.Equal(AxisSide.Right, secondary.Side);
        Assert.Equal(0, secondary.SlotIndex);
        double secondarySpan = secondary.Range.YMax - secondary.Range.YMin;
        Assert.Equal(500.0, secondarySpan, precision: 6);
        Assert.NotEqual(0.0, secondary.Range.YMin);

        pan.ApplyTo(figure);
        Assert.Equal(secondary.Range.YMin, figure.SubPlots[0].YAxes.Right[0].Axis.Min);
        Assert.Equal(secondary.Range.YMax, figure.SubPlots[0].YAxes.Right[0].Axis.Max);
    }

    /// <summary>A secondary axis on a log scale is moved in ITS drawn space, decades: its transform's range is already in
    /// that space, and reading it as data units took the logarithm twice (10..1000 panned into 0.4..1.2). The pointer's
    /// move is read back through the axis' own transform, as a zoom reads its edges.</summary>
    [Fact]
    public void OnPointerMoved_WhileCaptured_MovesALogSecondaryAxisByDecades()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithAddRightYAxis(y2 => y2.SetYLim(10, 1000).Plot([1.0, 2.0], [100.0, 200.0])))
            .Build();
        figure.SubPlots[0].YAxes.Right[0].Axis.Scale = AxisScale.Log;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var events = new List<FigureInteractionEvent>();
        var m = new PanModifier("chart-1", layout, events.Add);

        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(60, 25, PointerButton.Left, ModifierKeys.None));

        // Two decades over 50 pixels: 10 pixels up shows 0.4 decades lower.
        var secondary = Assert.Single(Assert.IsType<PanEvent>(events[0]).SecondaryRanges!);
        Assert.Equal(Math.Pow(10, 0.6), secondary.Range.YMin, 1e-9);
        Assert.Equal(Math.Pow(10, 2.6), secondary.Range.YMax, 1e-9);
    }

    /// <summary>PanModifier — OnPointerMoved with coords going null (move
    /// pointer outside plot AFTER pressing inside).</summary>
    [Fact]
    public void Pan_OnPointerMoved_OutsideAfterInsidePress_HitsCoordsNull()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.None));
        // A move that leaves the plot has no data coordinates: the pan emits nothing for it.
        Assert.Empty(events);
    }

    /// <summary>PanModifier — `if (dx == 0 &amp;&amp; dy == 0) return;` short-circuit's
    /// true arm: press + move to SAME position → both deltas are zero → no PanEvent.
    /// Pre-X this branch was 25% covered (1/4); this lifts the no-pan-on-zero arm.</summary>
    [Fact]
    public void Pan_OnPointerMoved_SamePosition_NoPanEvent()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }
}
