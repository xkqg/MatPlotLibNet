// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public class PanModifierTests
{
    // Plot area: x=[10,110], y=[10,60]. Data: x=[0,10], y=[0,5].
    private static (PanModifier modifier, List<FigureInteractionEvent> events) Make()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);

        var events = new List<FigureInteractionEvent>();
        var modifier = new PanModifier("chart-1", layout, events.Add);
        return (modifier, events);
    }

    [Fact]
    public void HandlesPointerPressed_LeftButton_NoShift_InsidePlot_ReturnsTrue()
    {
        var (m, _) = Make();
        var args = new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None);
        Assert.True(m.HandlesPointerPressed(args));
    }

    [Fact]
    public void HandlesPointerPressed_LeftButton_WithShift_ReturnsFalse()
    {
        var (m, _) = Make();
        var args = new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.Shift);
        Assert.False(m.HandlesPointerPressed(args));
    }

    [Fact]
    public void HandlesPointerPressed_RightButton_ReturnsFalse()
    {
        var (m, _) = Make();
        var args = new PointerInputArgs(50, 30, PointerButton.Right, ModifierKeys.None);
        Assert.False(m.HandlesPointerPressed(args));
    }

    [Fact]
    public void HandlesPointerPressed_OutsidePlot_ReturnsFalse()
    {
        var (m, _) = Make();
        var args = new PointerInputArgs(5, 5, PointerButton.Left, ModifierKeys.None);
        Assert.False(m.HandlesPointerPressed(args));
    }

    [Fact]
    public void OnPointerMoved_WhileCaptured_ProducesPanEvent()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(50, 35, PointerButton.Left, ModifierKeys.None));

        Assert.Single(events);
        Assert.IsType<PanEvent>(events[0]);
    }

    [Fact]
    public void OnPointerMoved_WhileCaptured_DeltaConvertedToDataSpace()
    {
        var (m, events) = Make();
        // Press at pixel (60,35) → data ~(5, 2.5)
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        // Move 10px right → 1 data unit right → PanEvent dx ≈ +1
        m.OnPointerMoved(new PointerInputArgs(70, 35, PointerButton.Left, ModifierKeys.None));

        var pan = Assert.IsType<PanEvent>(events[0]);
        Assert.Equal(-1.0, pan.DxData, precision: 3); // negative because we pan in reverse
        Assert.Equal(0.0, pan.DyData, precision: 3);
    }

    [Fact]
    public void OnPointerMoved_WhileNotCaptured_ProducesNoEvent()
    {
        var (m, events) = Make();
        m.OnPointerMoved(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void OnPointerReleased_ReleasesCapture()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerReleased(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None));
        m.OnPointerMoved(new PointerInputArgs(50, 35, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events); // no event after release
    }

    [Fact]
    public void HandlesScroll_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesScroll(new ScrollInputArgs(50, 30, 0, -1)));
    }

    [Fact]
    public void HandlesKeyDown_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesKeyDown(new KeyInputArgs("Home")));
    }
}
