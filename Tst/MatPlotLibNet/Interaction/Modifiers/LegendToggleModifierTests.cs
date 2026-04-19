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

    /// <summary>Phase X.7 (v1.7.2, 2026-04-19) — line 37 `if (seriesIdx is null) return;`
    /// true arm. OnPointerPressed called directly with click inside the axes BUT outside
    /// the legend item bounds → HitTestLegendItem returns null → early-return without
    /// emitting a LegendToggleEvent. Pre-X this arm was unhit (HandlesPointerPressed
    /// short-circuits before OnPointerPressed gets there in normal flow).</summary>
    [Fact]
    public void OnPointerPressed_InAxesButOutsideLegend_NoEvent()
    {
        var legendRect = new Rect(150, 15, 50, 14);
        var (m, events) = Make(legendRect);

        // Click inside the plot area but well outside the legend item.
        var args = new PointerInputArgs(50, 50, PointerButton.Left, ModifierKeys.None);
        m.OnPointerPressed(args);
        Assert.Empty(events);
    }

    /// <summary>Phase X.7 — invoke the no-op IInteractionModifier methods (lines 41-54)
    /// to lift line coverage from 80% to 100%. Each method is empty/return-false.</summary>
    [Fact]
    public void NoOpMethods_DoNotThrow()
    {
        // KeyInputArgs.Key is currently a magic-string API (`string Key` taking values
        // like "Escape", "Home"). Tracked finding for production-side stabilisation —
        // should be an enum or typed-constants class. Local const here so the test
        // doesn't propagate the magic-string pattern at the call site.
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

        Assert.Empty(events);   // no events from any of those calls
    }
}
