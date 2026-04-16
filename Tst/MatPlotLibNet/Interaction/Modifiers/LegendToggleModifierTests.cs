// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

public class LegendToggleModifierTests
{
    /// <summary>Builds a modifier with a single subplot whose legend has one item at the given bounds.</summary>
    private static (LegendToggleModifier modifier, List<FigureInteractionEvent> events) Make(
        Rect legendItemBounds)
    {
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "Series A").Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;

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

        // Click inside the legend item (centred in the rect)
        var args = new PointerInputArgs(160, 20, PointerButton.Left, ModifierKeys.None);
        Assert.True(m.HandlesPointerPressed(args));

        m.OnPointerPressed(args);
        Assert.Single(events);
        var toggle = Assert.IsType<LegendToggleEvent>(events[0]);
        Assert.Equal(0, toggle.AxesIndex);
        Assert.Equal(0, toggle.SeriesIndex);
    }

    [Fact]
    public void ClickOutsideLegend_DoesNotHandle()
    {
        var legendRect = new Rect(150, 15, 50, 14);
        var (m, _) = Make(legendRect);

        // Click inside the plot area but outside the legend item
        var args = new PointerInputArgs(50, 50, PointerButton.Left, ModifierKeys.None);
        Assert.False(m.HandlesPointerPressed(args));
    }

    [Fact]
    public void RightClick_DoesNotHandle()
    {
        var legendRect = new Rect(150, 15, 50, 14);
        var (m, _) = Make(legendRect);

        // Right-click inside the legend item — should not handle
        var args = new PointerInputArgs(160, 20, PointerButton.Right, ModifierKeys.None);
        Assert.False(m.HandlesPointerPressed(args));
    }

    [Fact]
    public void ClickOutsideAllAxes_DoesNotHandle()
    {
        var legendRect = new Rect(150, 15, 50, 14);
        var (m, _) = Make(legendRect);

        // Click outside any axes entirely
        var args = new PointerInputArgs(500, 500, PointerButton.Left, ModifierKeys.None);
        Assert.False(m.HandlesPointerPressed(args));
    }

    [Fact]
    public void NoLegendBounds_DoesNotHandle()
    {
        // Layout created via plotAreas-only overload (no legend data)
        var figure = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0], s => s.Label = "A").Build();
        figure.SubPlots[0].XAxis.Min = 0;
        figure.SubPlots[0].XAxis.Max = 10;
        figure.SubPlots[0].YAxis.Min = 0;
        figure.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(figure, [new Rect(10, 10, 200, 100)]);
        var events = new List<FigureInteractionEvent>();
        var m = new LegendToggleModifier("chart-1", layout, events.Add);

        // Click inside the plot area — no legend data so it should not handle
        var args = new PointerInputArgs(50, 30, PointerButton.Left, ModifierKeys.None);
        Assert.False(m.HandlesPointerPressed(args));
    }
}
