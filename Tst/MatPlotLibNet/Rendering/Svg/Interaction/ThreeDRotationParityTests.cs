// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase B of the v1.7.2 follow-on plan — pins the 3D drag-to-rotate math
/// to matplotlib's canonical formula in <c>mpl_toolkits/mplot3d/axes3d.py:_on_move</c>
/// (lines 1746–1843, roll = 0 case):
///
/// <code>
/// delev = -(dy / h) * 180
/// dazim = -(dx / w) * 180
/// elev += delev ;  azim += dazim
/// </code>
///
/// <para>Where (w, h) are the plot area's pixel width/height. A full-axes drag therefore
/// produces exactly 180° of rotation. The pre-fix script used a fixed <c>* 0.5°/pixel</c>
/// scaling AND inverted the azimuth sign, so dragging a 600 px chart half-way produced
/// 150° of rotation in the wrong direction (matplotlib spec: 90° in the correct
/// direction). Pre-fix elevation was also clamped to ±90°; matplotlib does not clamp
/// — instead the up-vector flips at the pole (Phase B.3 ports that).</para></summary>
public class ThreeDRotationParityTests
{
    private static InteractionScriptHarness Build3DScene() =>
        InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60, distance: 8)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })));

    private static double Read(InteractionScriptHarness h, string attr) =>
        double.Parse(h.GetAttribute(".mpl-3d-scene", attr)!, CultureInfo.InvariantCulture);

    /// <summary>Stacked Theory over the matplotlib drag/rotation matrix. Every row
    /// drives a single (dx, dy) drag and asserts the resulting (Δazim, Δelev) match
    /// matplotlib's <c>±drag/extent * 180</c> formula within 0.5°.</summary>
    [Theory]
    [InlineData( 1.0,  0.0, -180.0,    0.0)] // full-width right → daz = -180
    [InlineData(-1.0,  0.0,  180.0,    0.0)] // full-width left  → daz = +180
    [InlineData( 0.0,  1.0,    0.0, -180.0)] // full-height down → del = -180
    [InlineData( 0.0, -1.0,    0.0,  180.0)] // full-height up   → del = +180
    [InlineData( 0.5,  0.0,  -90.0,    0.0)] // half-width right → daz = -90
    [InlineData( 0.0,  0.5,    0.0,  -90.0)] // half-height down → del = -90
    [InlineData(0.25, 0.25,  -45.0,  -45.0)] // diagonal both axes
    public void Drag_AppliesMatplotlibFormula(double dxFraction, double dyFraction,
        double expectedDeltaAzim, double expectedDeltaElev)
    {
        using var h = Build3DScene();

        var plotW = Read(h, "data-plot-w");
        var plotH = Read(h, "data-plot-h");
        var azim0 = Read(h, "data-azimuth");
        var elev0 = Read(h, "data-elevation");

        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 0;             e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = dxFraction * plotW; e.clientY = dyFraction * plotH; });
        h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = dxFraction * plotW; e.clientY = dyFraction * plotH; });

        var azim1 = Read(h, "data-azimuth");
        var elev1 = Read(h, "data-elevation");

        Assert.Equal(expectedDeltaAzim, azim1 - azim0, 1);
        Assert.Equal(expectedDeltaElev, elev1 - elev0, 1);
    }

    /// <summary>Two consecutive drags accumulate. Confirms the script persists
    /// data-azimuth/data-elevation between drag sessions instead of resetting.</summary>
    [Fact]
    public void TwoConsecutiveDrags_AccumulateAngles()
    {
        using var h = Build3DScene();
        var plotW = Read(h, "data-plot-w");
        var azim0 = Read(h, "data-azimuth");

        // First drag: half-width right → -90°
        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 0; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = plotW * 0.5; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = plotW * 0.5; e.clientY = 0; });

        // Second drag: another half-width right → another -90° → total -180°
        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 0; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = plotW * 0.5; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = plotW * 0.5; e.clientY = 0; });

        Assert.Equal(-180.0, Read(h, "data-azimuth") - azim0, 1);
    }

    /// <summary>matplotlib's <c>_on_move</c> does NOT clamp elevation; |elev|>90°
    /// triggers a hemisphere flip in the V-vector (axes3d.py:_calc_view_axes). The
    /// pre-fix script clamped elev to ±90° which prevented inverting the cube
    /// at all. After Phase B.2, dragging past the pole must allow the angle to
    /// keep increasing.</summary>
    [Fact]
    public void Drag_PastTopPole_DoesNotClampElevation()
    {
        using var h = Build3DScene();
        var plotH = Read(h, "data-plot-h");
        var elev0 = Read(h, "data-elevation");

        // Drag full-height up → +180° elevation. With pre-fix clamp, elev would saturate at +90°.
        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 0; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 0; e.clientY = -plotH; });
        h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = 0; e.clientY = -plotH; });

        var elev1 = Read(h, "data-elevation");
        Assert.Equal(180.0, elev1 - elev0, 1);
    }

    /// <summary>Phase B.3: at elev = 180° the cube must render upside-down. Our project()
    /// uses a flat (Y,Z)-plane rotation: <c>pz = ry*sinEl + rz*cosEl</c>. At elev=180°
    /// cosEl=-1, sinEl=0, so pz=-rz — the up-direction inverts naturally without any
    /// explicit V-vector flip code (matplotlib's _calc_view_axes V-flip is a basis
    /// renormalisation artefact we don't need because we never re-orthonormalize).
    ///
    /// <para>Test invariant: a 360° elevation rotation must restore the exact same
    /// projection. The key is comparing polygons by their world-space data-v3d (which
    /// never changes), not their DOM order (which changes when depth-sort runs).</para></summary>
    [Fact]
    public void Rotation_FullCircle_RestoresProjection()
    {
        using var h = Build3DScene();
        var plotH = Read(h, "data-plot-h");

        var before = SnapshotByV3d(h);

        // Two full-height up drags → +360° elevation (back to the starting view).
        for (int i = 0; i < 2; i++)
        {
            h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 0; e.clientY = 0; });
            h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 0; e.clientY = -plotH; });
            h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = 0; e.clientY = -plotH; });
        }

        var after = SnapshotByV3d(h);

        Assert.Equal(before.Count, after.Count);
        foreach (var (v3d, ptsBefore) in before)
        {
            Assert.True(after.ContainsKey(v3d), $"polygon with data-v3d={v3d} disappeared after 360° rotation");
            var ptsAfter = after[v3d];
            Assert.Equal(ptsBefore.Length, ptsAfter.Length);
            for (int i = 0; i < ptsBefore.Length; i++)
                // Tolerance 1.0 (pixels): server renders at full precision, client uses
                // toFixed(2) on each axis; combined with the 1.15× BOX_FILL rescale a single
                // pixel can land on either side of a rounding boundary (492.498 vs 492.510).
                Assert.InRange(Math.Abs(ptsBefore[i] - ptsAfter[i]), 0.0, 1.0);
        }
    }

    /// <summary>Polygon snapshot keyed by world-space vertices (data-v3d) → array of
    /// (x, y) screen coordinates parsed from the <c>points</c> attribute. Tolerance
    /// comparison handles the server's full-precision vs client's <c>.toFixed(2)</c>
    /// formatting difference.</summary>
    private static Dictionary<string, double[]> SnapshotByV3d(InteractionScriptHarness h)
    {
        var dict = new Dictionary<string, double[]>();
        foreach (var el in h.Document.QuerySelectorAllRaw("polygon[data-v3d]"))
        {
            var v3d = el.getAttribute("data-v3d")!;
            var pts = el.getAttribute("points") ?? "";
            dict[v3d] = pts.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(p => p.Split(',').Select(c => double.Parse(c, CultureInfo.InvariantCulture)))
                .ToArray();
        }
        return dict;
    }

    /// <summary>Regression: the playground 3D Surface uses x ∈ [-3, 3], y ∈ [-3, 3],
    /// z = sinc(r) ≈ [-0.22, 1.0] — DIFFERENT range per axis. Our previous test used
    /// [0, 1]³ data which masked any bug that scales differently per axis. After zero
    /// rotation (or any 360°-multiple), JS reprojection MUST match server-rendered
    /// points across BOTH axis-infrastructure polygons (panes) AND data polygons
    /// (surface quads) — both are emitted via <c>proj.Normalize()</c> into the same
    /// centered-world space. Pre-fix the user reported "click → plot vanishes",
    /// indicating panes and surface ended up at vastly different scales.</summary>
    /// <summary>Strongest parity test: drag the playground surface to a NEW camera
    /// angle, compare every reprojected polygon against what the server would render
    /// AT THAT NEW ANGLE (built fresh). Catches the user-reported "click → plot
    /// vanishes / panes huge" symptom by exercising both axis-infrastructure polygons
    /// AND data polygons at a non-identity angle with realistic per-axis ranges.</summary>
    [Fact]
    public void PlaygroundSurfaceData_AfterDrag_MatchesServerAtNewAngle()
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

        // Build the same figure twice: one at the INITIAL angle (driven by the harness
        // through a 100 px drag = -30° azim shift), one at the EXPECTED post-drag angle
        // server-rendered fresh. Both must produce identical screen pixels.
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 35, azimuth: -50)
                .Surface(sx, sy, sz)));

        var plotW = double.Parse(h.GetAttribute(".mpl-3d-scene", "data-plot-w")!,
            CultureInfo.InvariantCulture);
        // Drag 100 px right → daz = -(100/plotW)*180. New az = -50 + daz.
        double dazExpected = -(100.0 / plotW) * 180.0;
        double newAz = -50 + dazExpected;

        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 0; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 100; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = 100; e.clientY = 0; });

        var clientPoints = SnapshotByV3d(h);

        // Build a fresh figure server-side at the post-drag angle.
        using var hRef = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 35, azimuth: newAz)
                .Surface(sx, sy, sz)));
        var serverPoints = SnapshotByV3d(hRef);

        Assert.Equal(serverPoints.Count, clientPoints.Count);
        int driftCount = 0; double maxDrift = 0; string? worstV3d = null;
        foreach (var (v3d, server) in serverPoints)
        {
            Assert.True(clientPoints.ContainsKey(v3d), $"v3d {v3d} missing from client");
            var client = clientPoints[v3d];
            Assert.Equal(server.Length, client.Length);
            for (int i = 0; i < server.Length; i++)
            {
                double drift = Math.Abs(server[i] - client[i]);
                if (drift > 1.0) { driftCount++; if (drift > maxDrift) { maxDrift = drift; worstV3d = v3d; } }
            }
        }
        Assert.True(driftCount == 0,
            $"{driftCount} client coords drift > 1px from server-at-same-angle (max={maxDrift:F2}px on v3d={worstV3d})");
    }

    [Fact]
    public void PlaygroundSurfaceData_NoRotation_ServerAndClientAgree()
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
                .WithCamera(elevation: 35, azimuth: -50)
                .Surface(sx, sy, sz)));

        var before = SnapshotByV3d(h);

        // Trigger a reproject WITHOUT changing camera angle: drag of zero distance.
        // Same camera → script's reprojection MUST yield the same screen pixels the
        // server rendered, otherwise the user sees a "jump" on first interaction.
        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 100; e.clientY = 100; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 100; e.clientY = 100; });
        h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = 100; e.clientY = 100; });

        var after = SnapshotByV3d(h);

        Assert.Equal(before.Count, after.Count);
        int driftCount = 0; double maxDrift = 0;
        foreach (var (v3d, ptsBefore) in before)
        {
            Assert.True(after.ContainsKey(v3d));
            var ptsAfter = after[v3d];
            for (int i = 0; i < ptsBefore.Length; i++)
            {
                double drift = Math.Abs(ptsBefore[i] - ptsAfter[i]);
                if (drift > 1) { driftCount++; if (drift > maxDrift) maxDrift = drift; }
            }
        }
        Assert.Equal(0, driftCount);
    }
}
