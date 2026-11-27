// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public sealed class RectangleZoomModifierTests : InteractionModifierTests<RectangleZoomModifier>
{
    protected override RectangleZoomModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    private static PointerInputArgs P(double x, double y, ModifierKeys mods = ModifierKeys.Ctrl) =>
        new(x, y, PointerButton.Left, mods);

    /// <summary>A drag sends one zoom event with the data bounds it spans, low ends first whichever way it was dragged.</summary>
    [Theory]
    [InlineData(60, 60, 110, 90)]
    [InlineData(110, 90, 60, 60)]
    public void Release_EmitsOneRectangleZoomEvent_WithNormalisedDataBounds(double fromX, double fromY, double toX, double toY)
    {
        var (mod, events) = Make(FigureLayout.Wide());
        Drag(mod, ModifierKeys.Ctrl, fromX, fromY, toX, toY);

        var evt = Assert.IsType<RectangleZoomEvent>(Assert.Single(events));
        Assert.Equal("c1", evt.ChartId);
        Assert.True(evt.XMin < evt.XMax, $"expected normalised XMin<XMax, got {evt.XMin}/{evt.XMax}");
        Assert.True(evt.YMin < evt.YMax, $"expected normalised YMin<YMax, got {evt.YMin}/{evt.YMax}");
    }

    [Fact]
    public void OnPointerMoved_BeforePress_IsNoop()
    {
        var (mod, _) = Make(FigureLayout.Wide());
        mod.OnPointerMoved(P(120, 80)); // no prior Pressed
        Assert.Null(mod.ActiveZoomRect);
    }

    /// <summary>RectangleZoomModifier.OnPointerReleased — tiny-drag short-circuit
    /// (`Math.Abs(args.X - _startPixelX) &lt; 2 &amp;&amp; Math.Abs(args.Y - _startPixelY) &lt; 2`).
    /// The existing tests probably exercise the substantial-drag branch (returns a
    /// RectangleZoomEvent); this hits the abort-no-event arm.</summary>
    [Fact]
    public void RectangleZoomModifier_TinyDrag_DoesNotEmitEvent()
    {
        var fig = Plt.Create().Plot([0.0, 1, 2, 3], [0.0, 1, 2, 3]).WithSize(400, 300).Build();
        var layout = ChartLayout.Create(fig, [new Rect(0, 0, 400, 300)]);
        FigureInteractionEvent? captured = null;
        var mod = new RectangleZoomModifier("c", layout, e => captured = e);
        var down = new PointerInputArgs { X = 100, Y = 100, Button = PointerButton.Left, Modifiers = ModifierKeys.Ctrl };
        var up = new PointerInputArgs { X = 101, Y = 101 };  // 1-px diagonal: < 2 in both
        mod.OnPointerPressed(down);
        mod.OnPointerReleased(up);
        Assert.Null(captured);
    }

    /// <summary>A press outside the plot arms nothing, and a release outside it after a press inside sends nothing: no data lies under the
    /// pixel.</summary>
    [Fact]
    public void RectangleZoom_AnEndOutsideThePlot_SendsNothing() => AssertNothingIsSent(m =>
    {
        m.OnPointerPressed(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.Ctrl));
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.Ctrl));
        m.OnPointerReleased(new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.Ctrl));
    });

    /// <summary>ROADMAP.md row 100: a Ctrl+drag zoom rectangle must narrow a secondary axis' own range too, read
    /// through the SAME two pixel corners as the primary.</summary>
    [Fact]
    public void RectangleZoom_OnPointerReleased_AlsoNarrowsASecondaryAxissOwnRange()
    {
        var (m, events, figure) = Make(FigureLayout.WithARightAxis());

        m.OnPointerPressed(new PointerInputArgs(30, 15, PointerButton.Left, ModifierKeys.Ctrl));
        m.OnPointerReleased(new PointerInputArgs(80, 45, PointerButton.Left, ModifierKeys.Ctrl));

        var zoom = Assert.IsType<RectangleZoomEvent>(Assert.Single(events));
        var secondary = Assert.Single(zoom.SecondaryRanges!);
        Assert.Equal(AxisSide.Right, secondary.Side);
        Assert.True(secondary.Range.YMax - secondary.Range.YMin < 500.0,
            "A zoom rectangle smaller than the plot area should narrow the secondary axis' own range too.");

        zoom.ApplyTo(figure);
        Assert.Equal(secondary.Range.YMin, figure.SubPlots[0].YAxes.Right[0].Axis.Min);
        Assert.Equal(secondary.Range.YMax, figure.SubPlots[0].YAxes.Right[0].Axis.Max);
    }
}
