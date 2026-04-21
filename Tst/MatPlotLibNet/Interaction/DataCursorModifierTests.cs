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

    /// <summary>L52 FALSE arm — nearest.PixelDistance &lt;= HitRadiusPx → don't return false,
    /// store _pendingHit. Click 4 px away from data point (1, 5) → pixel (100, 50),
    /// so click at (104, 50) is within HitRadiusPx=10.</summary>
    [Fact]
    public void HandlesPointerPressed_WithinHitRadius_ReturnsTrueAndSetsPending()
    {
        var (mod, _, _) = Setup();
        // Data point (1, 5): X=1/2*200=100, Y=100-(5/10*100)=50. Click at (104, 50) = 4px away.
        Assert.True(mod.HandlesPointerPressed(P(104, 50)));
    }

    // ── Phase J coverage additions ────────────────────────────────────────────

    /// <summary>L42 TRUE — Button != Left → return false immediately.</summary>
    [Fact]
    public void HandlesPointerPressed_RightButton_ReturnsFalse()
    {
        var (mod, _, _) = Setup();
        Assert.False(mod.HandlesPointerPressed(
            new PointerInputArgs(0, 100, PointerButton.Right, ModifierKeys.None)));
    }

    /// <summary>L52 TRUE — nearest found (within NearestPointFinder's 20px window) but
    /// PixelDistance > HitRadiusPx (10px) → return false. Click 15 px from data point (1,5)
    /// at pixel (100,50); clicking at (115,50) gives distance=15 px.</summary>
    [Fact]
    public void HandlesPointerPressed_NearButOutsideHitRadius_ReturnsFalse()
    {
        var (mod, _, _) = Setup();
        // Data point (1, 5) → pixel (100, 50). Distance from (115, 50) = 15 px > HitRadiusPx=10.
        Assert.False(mod.HandlesPointerPressed(P(115, 50)));
    }

    /// <summary>L48 TRUE — nearest is null (subplot exists but has no series) → return false.</summary>
    [Fact]
    public void HandlesPointerPressed_NoSeries_NearestIsNull_ReturnsFalse()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, _ => { }).Build();
        figure.ChartId = "c";
        figure.SubPlots[0].XAxis.Min = 0; figure.SubPlots[0].XAxis.Max = 1;
        figure.SubPlots[0].YAxis.Min = 0; figure.SubPlots[0].YAxis.Max = 1;
        var layout = ChartLayout.Create(figure, [new Rect(0, 0, 200, 100)]);
        var mod = new DataCursorModifier("c", layout, figure, _ => { });
        // Pixel (100, 50) is inside the layout rect → axesIndex non-null.
        // NearestPointFinder returns null because there are no series.
        Assert.False(mod.HandlesPointerPressed(P(100, 50)));
    }
}
