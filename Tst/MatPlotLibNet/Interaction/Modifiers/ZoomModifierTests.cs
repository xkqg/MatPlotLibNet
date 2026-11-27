// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public sealed class ZoomModifierTests : InteractionModifierTests<ZoomModifier>
{
    protected override bool ShouldHandleScroll => true;

    protected override ZoomModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    [Fact]
    public void HandlesScroll_InsidePlotArea_ReturnsTrue()
    {
        var (m, _) = Make();
        Assert.True(m.HandlesScroll(new ScrollInputArgs(60, 35, 0, -1)));
    }

    [Fact]
    public void HandlesScroll_OutsidePlotArea_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesScroll(new ScrollInputArgs(5, 5, 0, -1)));
    }

    [Fact]
    public void OnScroll_NegativeDeltaY_ZoomsIn_RangeDecreases()
    {
        var (m, events) = Make();
        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));
        var zoom = Assert.IsType<ZoomEvent>(events[0]);
        Assert.True(zoom.XMax - zoom.XMin < 10.0, "Zoom in should reduce X range");
        Assert.True(zoom.YMax - zoom.YMin < 5.0,  "Zoom in should reduce Y range");
    }

    [Fact]
    public void OnScroll_PositiveDeltaY_ZoomsOut_RangeIncreases()
    {
        var (m, events) = Make();
        m.OnScroll(new ScrollInputArgs(60, 35, 0, +1));
        var zoom = Assert.IsType<ZoomEvent>(events[0]);
        Assert.True(zoom.XMax - zoom.XMin > 10.0, "Zoom out should increase X range");
        Assert.True(zoom.YMax - zoom.YMin > 5.0,  "Zoom out should increase Y range");
    }

    [Fact]
    public void OnScroll_ZoomCenteredOnCursorPosition()
    {
        var (m, events) = Make();
        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));
        var zoom = Assert.IsType<ZoomEvent>(events[0]);
        double midX = (zoom.XMin + zoom.XMax) / 2;
        double midY = (zoom.YMin + zoom.YMax) / 2;
        Assert.Equal(5.0, midX, precision: 3);
        Assert.Equal(2.5, midY, precision: 3);
    }

    [Fact]
    public void OnScroll_ProducesZoomEventWithCorrectChartId()
    {
        var (m, events) = Make();
        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));
        Assert.Equal("c1", events[0].ChartId);
    }

    [Fact]
    public void NoOpMethods_DoNotThrow()
    {
        var (m, _) = Make();
        m.OnPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(50, 30, PointerButton.None, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None));
        m.OnKeyDown(new KeyInputArgs("Home"));
    }

    /// <summary>ROADMAP.md row 100: a scroll-zoom must narrow a secondary axis' own range too — right slot 0
    /// here is [0,500], and zooming in must shrink IT proportionally, not leave it at its full span.</summary>
    [Fact]
    public void OnScroll_ZoomIn_AlsoNarrowsASecondaryAxissOwnRange()
    {
        var (m, events, figure) = Make(FigureLayout.WithARightAxis());

        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));

        var zoom = Assert.IsType<ZoomEvent>(events[0]);
        var secondary = Assert.Single(zoom.SecondaryRanges!);
        Assert.Equal(AxisSide.Right, secondary.Side);
        double secondarySpan = secondary.Range.YMax - secondary.Range.YMin;
        Assert.True(secondarySpan < 500.0, "Zoom in should narrow the secondary axis' own range too.");

        zoom.ApplyTo(figure);
        Assert.Equal(secondary.Range.YMin, figure.SubPlots[0].YAxes.Right[0].Axis.Min);
        Assert.Equal(secondary.Range.YMax, figure.SubPlots[0].YAxes.Right[0].Axis.Max);
    }

    /// <summary>ZoomModifier ternary's `deltaY &gt; 0` arm — scroll DOWN to
    /// zoom out (the range widens by one notch). Was uncovered because all prior tests scrolled up.</summary>
    [Fact]
    public void Zoom_OnScroll_PositiveDeltaY_ZoomsOut()
    {
        var (m, events) = Make();
        m.OnScroll(new ScrollInputArgs(60, 35, 0, +1));   // positive deltaY → zoom out
        var evt = Assert.IsType<ZoomEvent>(events.Single());
        // Range was [0,10]×[0,5]; a notch out scales spans by 1/0.85 (ZoomNotchTests) → bigger range.
        Assert.True(evt.XMax - evt.XMin > 10.0);
        Assert.True(evt.YMax - evt.YMin > 5.0);
    }

    /// <summary>ZoomModifier `HitTestAxes is not null` true arm + scroll inside.
    /// Forward-regression guard for HandlesScroll.</summary>
    [Fact]
    public void Zoom_HandlesScroll_InsideAxes_True()
    {
        var (m, _) = Make();
        Assert.True(m.HandlesScroll(new ScrollInputArgs(60, 35, 0, -1)));
        Assert.False(m.HandlesScroll(new ScrollInputArgs(500, 500, 0, -1)));
    }
}
