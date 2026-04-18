// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction;

/// <summary>Phase H.4 of the v1.7.2 follow-on plan — behavioural coverage for
/// <see cref="DataCursorModifier"/>. Pre-H.4 the modifier didn't exist; the
/// toolbar "cursor" button and <see cref="DataCursorEvent"/> / <see cref="PinnedAnnotation"/>
/// records were orphaned. H.4 adds the modifier + wires it into the controller.
///
/// <para>Contract: plain left-click within <see cref="DataCursorModifier.HitRadiusPx"/>
/// of a data point emits a <see cref="DataCursorEvent"/>. Clicks that don't hit
/// any point defer to the next modifier (pan) by returning <c>false</c> from
/// <see cref="DataCursorModifier.HandlesPointerPressed"/>.</para></summary>
public class DataCursorModifierTests
{
    private static (DataCursorModifier mod, Figure figure, List<FigureInteractionEvent> events) Setup()
    {
        var figure = Plt.Create().Plot([0.0, 1.0, 2.0], [0.0, 5.0, 10.0]).Build();
        figure.ChartId = "c";
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 2;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 10;
        var layout = ChartLayout.Create(figure, [new Rect(0, 0, 200, 100)]);
        var events = new List<FigureInteractionEvent>();
        return (new DataCursorModifier("c", layout, figure, events.Add), figure, events);
    }

    private static PointerInputArgs P(double x, double y, ModifierKeys mods = ModifierKeys.None) =>
        new(x, y, PointerButton.Left, mods);

    [Fact]
    public void HandlesPointerPressed_OnDataPoint_ReturnsTrue()
    {
        var (mod, _, _) = Setup();
        // Data point (0, 0) projects to pixel (0, 100). Click right at it.
        Assert.True(mod.HandlesPointerPressed(P(0, 100)));
    }

    [Fact]
    public void HandlesPointerPressed_FarFromAnyPoint_ReturnsFalse()
    {
        var (mod, _, _) = Setup();
        // Click at middle of plot (no data point nearby).
        Assert.False(mod.HandlesPointerPressed(P(60, 50)));
    }

    [Fact]
    public void HandlesPointerPressed_WithModifierKey_ReturnsFalse()
    {
        var (mod, _, _) = Setup();
        Assert.False(mod.HandlesPointerPressed(P(0, 100, ModifierKeys.Ctrl)));
    }

    [Fact]
    public void HandlesPointerPressed_OutsidePlot_ReturnsFalse()
    {
        var (mod, _, _) = Setup();
        Assert.False(mod.HandlesPointerPressed(P(300, 200)));
    }

    [Fact]
    public void OnPointerPressed_AfterHit_EmitsDataCursorEventWithPinnedAnnotation()
    {
        var (mod, _, events) = Setup();
        mod.HandlesPointerPressed(P(0, 100));   // primes _pendingHit
        mod.OnPointerPressed(P(0, 100));

        Assert.Single(events);
        var evt = Assert.IsType<DataCursorEvent>(events[0]);
        Assert.Equal("c", evt.ChartId);
        Assert.Equal(0.0, evt.Annotation.DataX, 3);
        Assert.Equal(0.0, evt.Annotation.DataY, 3);
    }

    [Fact]
    public void OnPointerPressed_WithoutPriorHit_IsNoop()
    {
        var (mod, _, events) = Setup();
        // Don't call HandlesPointerPressed — simulate Pan taking the event instead.
        mod.OnPointerPressed(P(60, 50));
        Assert.Empty(events);
    }
}
