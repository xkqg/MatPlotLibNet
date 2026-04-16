// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public class BrushSelectModifierTests
{
    // Plot: pixel [10,110]×[10,60], data [0,10]×[0,5]
    private static (BrushSelectModifier modifier, List<FigureInteractionEvent> events) Make()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 100, 50)]);
        var events = new List<FigureInteractionEvent>();
        return (new BrushSelectModifier("chart-1", layout, events.Add), events);
    }

    [Fact]
    public void HandlesPointerPressed_LeftShift_InsidePlot_ReturnsTrue()
    {
        var (m, _) = Make();
        Assert.True(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.Shift)));
    }

    [Fact]
    public void HandlesPointerPressed_LeftNoShift_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None)));
    }

    [Fact]
    public void HandlesPointerPressed_RightShift_ReturnsFalse()
    {
        var (m, _) = Make();
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Right, ModifierKeys.Shift)));
    }

    [Fact]
    public void OnPointerReleased_AfterDrag_ProducesBrushSelectEvent()
    {
        var (m, events) = Make();
        m.OnPointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.Shift));  // data (0,5)
        m.OnPointerReleased(new PointerInputArgs(110, 60, PointerButton.Left, ModifierKeys.Shift)); // data (10,0)

        Assert.Single(events);
        Assert.IsType<BrushSelectEvent>(events[0]);
    }

    [Fact]
    public void OnPointerReleased_RectNormalized_X1LessThanX2()
    {
        var (m, events) = Make();
        // Drag right-to-left: press at pixel 110 (data x=10), release at 10 (data x=0)
        m.OnPointerPressed(new PointerInputArgs(110, 10, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(10,  60, PointerButton.Left, ModifierKeys.Shift));

        var evt = Assert.IsType<BrushSelectEvent>(events[0]);
        Assert.True(evt.X1 <= evt.X2, $"Expected X1 <= X2 but got X1={evt.X1}, X2={evt.X2}");
        Assert.True(evt.Y1 <= evt.Y2, $"Expected Y1 <= Y2 but got Y1={evt.Y1}, Y2={evt.Y2}");
    }

    [Fact]
    public void OnPointerReleased_WithoutPriorPress_ProducesNoEvent()
    {
        var (m, events) = Make();
        m.OnPointerReleased(new PointerInputArgs(110, 60, PointerButton.Left, ModifierKeys.Shift));
        Assert.Empty(events);
    }

    [Fact]
    public void DuringDrag_ActiveBrush_IsNonNull()
    {
        var (m, _) = Make();
        m.OnPointerPressed(new PointerInputArgs(20, 20, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerMoved(new PointerInputArgs(80, 50, PointerButton.Left, ModifierKeys.Shift));
        Assert.NotNull(m.ActiveBrush);
    }

    [Fact]
    public void AfterRelease_ActiveBrush_IsNull()
    {
        var (m, _) = Make();
        m.OnPointerPressed(new PointerInputArgs(20, 20, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerMoved(new PointerInputArgs(80, 50, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(80, 50, PointerButton.Left, ModifierKeys.Shift));
        Assert.Null(m.ActiveBrush);
    }

    [Fact]
    public void OnPointerMoved_UpdatesCurrentPixelCoords()
    {
        var (m, _) = Make();
        m.OnPointerPressed(new PointerInputArgs(20, 20, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerMoved(new PointerInputArgs(90, 55, PointerButton.Left, ModifierKeys.Shift));

        var state = m.ActiveBrush;
        Assert.NotNull(state);
        Assert.Equal(20.0, state.Value.StartPixelX, precision: 3);
        Assert.Equal(20.0, state.Value.StartPixelY, precision: 3);
        Assert.Equal(90.0, state.Value.CurrentPixelX, precision: 3);
        Assert.Equal(55.0, state.Value.CurrentPixelY, precision: 3);
    }

    [Fact]
    public void BrushSelectEvent_DataCoordinatesCorrect()
    {
        var (m, events) = Make();
        // Press at pixel (10,10) → data (0,5). Release at pixel (60,35) → data (5,2.5).
        m.OnPointerPressed(new PointerInputArgs(10, 10, PointerButton.Left, ModifierKeys.Shift));
        m.OnPointerReleased(new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.Shift));

        var evt = Assert.IsType<BrushSelectEvent>(events[0]);
        Assert.Equal(0.0,  evt.X1, precision: 3);
        Assert.Equal(5.0,  evt.X2, precision: 3);
        Assert.Equal(2.5,  evt.Y1, precision: 3);
        Assert.Equal(5.0,  evt.Y2, precision: 3);
    }
}
