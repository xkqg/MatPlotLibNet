// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public sealed class LegendToggleModifierTests : InteractionModifierTests<LegendToggleModifier>
{
    protected override LegendToggleModifier CreateModifier(IChartLayout layout, Figure fig, Action<FigureInteractionEvent> sink) =>
        new("c1", layout, sink);

    private static (LegendToggleModifier m, List<FigureInteractionEvent> events) Make(Rect legendItemBounds)
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series A").Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 5;
        var plotAreas = new[] { new Rect(10, 10, 200, 100) };
        var legendItems = new IReadOnlyList<LegendItemBounds>[]
        {
            new[] { new LegendItemBounds(0, legendItemBounds) }
        };
        var layoutResult = new LayoutResult(plotAreas, legendItems);
        var layout = ChartLayout.Create(figure, layoutResult);
        var events = new List<FigureInteractionEvent>();
        return (new LegendToggleModifier("chart-1", layout, events.Add), events);
    }

    [Fact]
    public void ClickInsideLegendItem_ProducesLegendToggleEvent()
    {
        var legendRect = new Rect(150, 15, 50, 14);
        var (m, events) = Make(legendRect);
        var args = new PointerInputArgs(160, 20, PointerButton.Left, ModifierKeys.None);
        Assert.True(m.HandlesPointerPressed(args));
        m.OnPointerPressed(args);
        Assert.Single(events);
        var toggle = Assert.IsType<LegendToggleEvent>(events[0]);
        Assert.Equal(0, toggle.AxesIndex);
        Assert.Equal(0, toggle.SeriesIndex);
    }

    [Fact]
    public void NoLegendBounds_DoesNotHandle()
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A").Build();
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 200, 100)]);
        var events = new List<FigureInteractionEvent>();
        var m = new LegendToggleModifier("chart-1", layout, events.Add);
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None)));
    }

    /// <summary>A press outside the legend (inside the axes, or outside all of them) and a right press on the legend item are not the toggle's.</summary>
    [Theory]
    [InlineData(50, 50, PointerButton.Left)]
    [InlineData(160, 20, PointerButton.Right)]
    [InlineData(500, 500, PointerButton.Left)]
    public void HandlesPointerPressed_OutsideTheLegendOrWithTheRightButton_DoesNotHandle(double x, double y, PointerButton button)
    {
        var (m, _) = Make(new Rect(150, 15, 50, 14));
        Assert.False(m.HandlesPointerPressed(new PointerInputArgs(x, y, button, ModifierKeys.None)));
    }

    [Fact]
    public void OnPointerPressed_InAxesButOutsideLegend_NoEvent()
    {
        var (m, events) = Make(new Rect(150, 15, 50, 14));
        m.OnPointerPressed(new PointerInputArgs(50, 50, PointerButton.Left, ModifierKeys.None));
        Assert.Empty(events);
    }

    [Fact]
    public void NoOpMethods_DoNotThrow()
    {
        const string EscapeKey = "Escape";
        var (m, events) = Make(new Rect(150, 15, 50, 14));
        var pArgs = new PointerInputArgs(60, 35, PointerButton.None, ModifierKeys.None);
        var sArgs = new ScrollInputArgs(60, 35, 0, -1);
        var kArgs = new KeyInputArgs(EscapeKey, ModifierKeys.None);
        m.OnPointerMoved(pArgs);
        m.OnPointerReleased(pArgs);
        Assert.False(m.HandlesScroll(sArgs));
        m.OnScroll(sArgs);
        Assert.False(m.HandlesKeyDown(kArgs));
        m.OnKeyDown(kArgs);
        Assert.Empty(events);
    }
}
