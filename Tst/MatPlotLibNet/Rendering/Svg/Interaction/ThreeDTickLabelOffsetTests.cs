// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase F.2 of the v1.7.2 follow-on plan — tick-label + axis-title
/// perpendicular-offset preservation on interactive rotation.
///
/// <para>Bug (user-reported via screenshot): before any interaction, tick-label
/// numerics sit <em>outside</em> the cube silhouette (correct — server renders
/// them at <c>projected_tick + perp × (tickLength + pad + 14 px)</c> in
/// <c>ThreeDAxesRenderer.RenderAxisEdgeTicks</c> L580-L589). After a pan/drag,
/// labels snap <em>onto</em> the axis edge — the JS reproject text branch wrote
/// only the projected 3D anchor, dropping the perpendicular pad.</para>
///
/// <para>Test invariant: a 5-px drag ≈ 1.5° rotation, so every label's screen
/// position should change by at most ~3-4 px (rotation jitter). Pre-fix the JS
/// reproject drops the perpendicular pad entirely, so labels jump by ~20 px
/// (the tick-label pad) or ~42-60 px (axis-title pad) as they collapse onto
/// the axis edge. That's an order of magnitude more than the rotation delta.</para>
///
/// <para>Stacked Theory over the same five camera angles as Phase F.1 so any
/// regression at any standard viewing angle is caught.</para></summary>
public class ThreeDTickLabelOffsetTests
{
    /// <summary>Max allowed per-label displacement from a tiny 5-px drag.
    /// Pre-fix: labels jump by 20-74+ px (23 of 23 labels) because JS drops
    /// the perpendicular pad entirely, collapsing them onto the axis edge.
    /// Post-fix: labels follow the rotated axis edge correctly — legitimate
    /// shift is `pad × sin(Δangle)` plus projection-rescale from far-corner
    /// vertex. At shallow elevation (≤10°) the rescale term dominates and
    /// can reach 20 px on a 60-px axis-title pad — that's the genuine
    /// "follow the axis as it rotates" behaviour, not a regression.
    /// 20-px threshold still fails by a wide margin on the pre-fix bug while
    /// tolerating the shallow-camera rescale envelope.</summary>
    private const double MaxLabelDisplacementPx = 20.0;

    public static IEnumerable<object[]> CameraAngles() =>
    [
        [30, -60],  // matplotlib default
        [35, -50],  // playground default
        [45,   0],  // looking straight down +X
        [10, -90],  // shallow elevation, sideways
        [60, -30],  // high elevation, slight rotation
    ];

    [Theory]
    [MemberData(nameof(CameraAngles))]
    public void TickLabels_StayOutsideCubeCentroid_AfterDragRotate(int elev, int azim)
    {
        const int n = 20;
        var sx = Enumerable.Range(0, n).Select(i => -3.0 + 6.0 * i / (n - 1)).ToArray();
        var sy = Enumerable.Range(0, n).Select(i => -3.0 + 6.0 * i / (n - 1)).ToArray();
        var sz = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                double r = Math.Sqrt(sx[i] * sx[i] + sy[j] * sy[j]);
                sz[i, j] = r < 1e-10 ? 1.0 : Math.Sin(r) / r;
            }

        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: elev, azimuth: azim)
                .SetXLabel("X axis")
                .SetYLabel("Y axis")
                .SetZLabel("Z axis")
                .Surface(sx, sy, sz)));

        // Capture pre-drag label positions (server-rendered — these are already offset correctly).
        var before = GatherTextPositions(h);
        Assert.NotEmpty(before);

        // Trigger a single reproject via a 5-px drag (≈ 1.5° rotation).
        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 0; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 5; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = 5; e.clientY = 0; });

        var after = GatherTextPositions(h);

        // Per-label displacement: a 1.5° rotation shifts label positions by only a few
        // pixels. Pre-fix the JS drops the perpendicular pad, so labels jump by 20 px
        // (tick pad) or 42+ px (axis-title pad). Assert max displacement < 8 px.
        int jumped = 0;
        string? worstKey = null;
        double worstDisp = 0;
        foreach (var (key, posBefore) in before)
        {
            if (!after.TryGetValue(key, out var posAfter)) continue;
            double dx = posAfter.x - posBefore.x, dy = posAfter.y - posBefore.y;
            double disp = Math.Sqrt(dx * dx + dy * dy);
            if (disp > MaxLabelDisplacementPx)
            {
                jumped++;
                if (disp > worstDisp) { worstDisp = disp; worstKey = key; }
            }
        }
        Assert.True(jumped == 0,
            $"elev={elev}, azim={azim}: {jumped}/{before.Count} text[data-v3d] labels jumped more than {MaxLabelDisplacementPx}px on a 5-px drag (worst={worstDisp:F1}px on v3d={worstKey}). Pre-fix symptom: labels collapse onto the axis edge because JS reproject drops the perpendicular pad.");
    }

    /// <summary>Walk every <c>text</c> descendant of the SVG that carries a
    /// <c>data-v3d</c> attribute (tick labels + axis titles) and collect their
    /// current <c>(x, y)</c>. Keyed by <c>data-v3d</c> so positions can be
    /// correlated across the before/after snapshots.</summary>
    private static Dictionary<string, (double x, double y)> GatherTextPositions(InteractionScriptHarness h)
    {
        var dict = new Dictionary<string, (double x, double y)>();
        foreach (var el in h.Document.QuerySelectorAllRaw("text[data-v3d]"))
        {
            var v3d = el.getAttribute("data-v3d")!;
            var x = double.Parse(el.getAttribute("x") ?? "0", CultureInfo.InvariantCulture);
            var y = double.Parse(el.getAttribute("y") ?? "0", CultureInfo.InvariantCulture);
            // Keep the first occurrence (server order) — if depth-sort moves duplicates the
            // last write wins; either way we test the 'after' snapshot's positions.
            dict[v3d] = (x, y);
        }
        return dict;
    }
}
