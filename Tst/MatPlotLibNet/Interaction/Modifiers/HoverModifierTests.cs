// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public class HoverModifierTests
{
    private static (HoverModifier modifier, List<FigureInteractionEvent> events) Make()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var events = new List<FigureInteractionEvent>();
        return (new HoverModifier("chart-1", layout, events.Add), events);
    }

    [Fact]
    public void OnPointerMoved_NoButton_InsidePlot_ProducesHoverEvent()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        Assert.Single(events);
        Assert.IsType<HoverEvent>(events[0]);
    }

    [Fact]
    public void OnPointerMoved_NoButton_OutsidePlot_ProducesNoEvent()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(5, 5, PointerButton.None, ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void OnPointerMoved_LeftButton_ProducesNoEvent()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void HoverEvent_DataCoordinatesCorrect()
    {
        var (m, events) = Make();
        // pixel (60,35) → data (5, 2.5)
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        var evt = Assert.IsType<HoverEvent>(events[0]);
        Assert.Equal(5.0,  evt.X, precision: 3);
        Assert.Equal(2.5, evt.Y, precision: 3);
    }

    [Fact]
    public void HoverEvent_HasCorrectChartId()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None));
        Assert.Equal("chart-1", events[0].ChartId);
    }

    [Fact]
    public void HandlesPointerPressed_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void HandlesScroll_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesScroll(new ScrollInputArgs(60, 35, 0, -1)));
    }

    // Phase X.7 finding: HoverModifier line 38 `if (coords is null) return;` arm is
    // provably unreachable through the public API. PixelToData returns null only when
    // the pixel is outside the plot area bounds — but HitTestAxes uses the same bounds
    // check above (line 35) and returns null in that case, triggering the line-36 early
    // return BEFORE reaching line 38. Defensive guard kept; same pattern as Stereographic
    // k<0 and Sinusoidal cosLat==0 (X.6 exemptions). Tracked for HoverModifier exemption
    // if branch coverage stays at 83% after all other lifts.
}
