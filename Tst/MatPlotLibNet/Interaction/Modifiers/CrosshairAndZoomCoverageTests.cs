// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Interaction.Modifiers;

/// <summary>Phase X.9.c (v1.7.2, 2026-04-19) — pinpoint coverage for the two
/// remaining sub-90 modifiers that aren't covered by <see cref="ModifierTestBase{T}"/>:
/// CrosshairModifier (single-arg constructor — only takes IChartLayout) and the
/// ZoomModifier `deltaY &gt; 0` zoom-OUT arm.
///
/// Pre-X.9.c: CrosshairModifier 76%L / 100%B (uncovered: lines 38-45 — the no-op
/// IInteractionModifier members), ZoomModifier 86%L / 75%B (uncovered: line 46
/// ternary's deltaY &gt; 0 arm + the deltaY = 0 arm).</summary>
public class CrosshairAndZoomCoverageTests
{
    private static (Figure fig, IChartLayout layout) Make()
    {
        var fig = Plt.Create().Plot([1.0, 2.0], [3.0, 4.0]).Build();
        fig.SubPlots[0].XAxis.Min = 0;
        fig.SubPlots[0].XAxis.Max = 10;
        fig.SubPlots[0].YAxis.Min = 0;
        fig.SubPlots[0].YAxis.Max = 5;
        var layout = ChartLayout.Create(fig, [new Rect(10, 10, 100, 50)]);
        return (fig, layout);
    }

    /// <summary>CrosshairModifier no-op IInteractionModifier members — lines 38-45.
    /// Crosshair is a passive modifier (HandlesPointerPressed always false), so the
    /// generic <see cref="ModifierTestBase{T}"/> doesn't include it (different ctor).</summary>
    [Fact]
    public void Crosshair_NoOpInteractionMembers_DoNotThrow()
    {
        var (_, layout) = Make();
        var m = new CrosshairModifier(layout);

        var press = new PointerInputArgs(60, 35, PointerButton.Left, ModifierKeys.None);
        Assert.False(m.HandlesPointerPressed(press));
        m.OnPointerPressed(press);
        m.OnPointerMoved(press);
        m.OnPointerReleased(press);

        var scroll = new ScrollInputArgs(60, 35, 0, -1);
        Assert.False(m.HandlesScroll(scroll));
        m.OnScroll(scroll);

        const string EscapeKey = "Escape";
        Assert.False(m.HandlesKeyDown(new KeyInputArgs(EscapeKey, ModifierKeys.None)));
        m.OnKeyDown(new KeyInputArgs(EscapeKey, ModifierKeys.None));
    }

    /// <summary>CrosshairModifier UpdatePosition outside plot → state cleared (line 24
    /// `axesIndex is null` true arm). Pins the false-arm short-circuit.</summary>
    [Fact]
    public void Crosshair_UpdatePosition_OutsidePlot_ClearsState()
    {
        var (_, layout) = Make();
        var m = new CrosshairModifier(layout);
        m.UpdatePosition(60, 35);
        Assert.NotNull(m.ActiveCrosshair);
        m.UpdatePosition(500, 500);
        Assert.Null(m.ActiveCrosshair);
    }

    /// <summary>ZoomModifier line 46 ternary's `deltaY &gt; 0` arm — scroll DOWN to
    /// zoom out (range expands by 15%). Was uncovered because all prior tests scrolled up.</summary>
    [Fact]
    public void Zoom_OnScroll_PositiveDeltaY_ZoomsOut()
    {
        var (_, layout) = Make();
        var events = new List<FigureInteractionEvent>();
        var m = new ZoomModifier("c1", layout, events.Add);
        m.OnScroll(new ScrollInputArgs(60, 35, 0, +1));   // positive deltaY → zoom out
        var evt = Assert.IsType<ZoomEvent>(events.Single());
        // Range was [0,10]×[0,5]; zoom-out scales spans by 1.15 → bigger range.
        Assert.True(evt.XMax - evt.XMin > 10.0);
        Assert.True(evt.YMax - evt.YMin > 5.0);
    }

    /// <summary>ZoomModifier line 33 `HitTestAxes is not null` true arm + scroll inside.
    /// Forward-regression guard for HandlesScroll.</summary>
    [Fact]
    public void Zoom_HandlesScroll_InsideAxes_True()
    {
        var (_, layout) = Make();
        var m = new ZoomModifier("c1", layout, _ => { });
        Assert.True(m.HandlesScroll(new ScrollInputArgs(60, 35, 0, -1)));
        Assert.False(m.HandlesScroll(new ScrollInputArgs(500, 500, 0, -1)));
    }
}
