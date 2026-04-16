// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public class ZoomModifierTests
{
    // Plot area: x=[10,110], y=[10,60]. Data: x=[0,10], y=[0,5].
    private static (ZoomModifier modifier, List<FigureInteractionEvent> events) Make()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);

        var events = new List<FigureInteractionEvent>();
        var modifier = new ZoomModifier("chart-1", layout, events.Add);
        return (modifier, events);
    }

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
        // Scroll up (negative deltaY) = zoom in = smaller range
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
        // Cursor at pixel (60,35) → data (5, 2.5) — exact centre of data range.
        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));
        var zoom = Assert.IsType<ZoomEvent>(events[0]);

        // Centre of new range should still be ~(5, 2.5).
        double midX = (zoom.XMin + zoom.XMax) / 2;
        double midY = (zoom.YMin + zoom.YMax) / 2;
        Assert.Equal(5.0,  midX, precision: 3);
        Assert.Equal(2.5, midY, precision: 3);
    }

    [Fact]
    public void OnScroll_ProducesZoomEventWithCorrectChartId()
    {
        var (m, events) = Make();
        m.OnScroll(new ScrollInputArgs(60, 35, 0, -1));
        Assert.Equal("chart-1", events[0].ChartId);
    }

    [Fact]
    public void HandlesPointerPressed_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void HandlesKeyDown_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesKeyDown(new KeyInputArgs("Home")));
    }
}
