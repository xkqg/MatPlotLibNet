// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Rendering.Svg.Interaction;

/// <summary>Phase F of the v1.7.2 follow-on plan — pane/data depth-sort isolation.
///
/// <para>Pre-F bug: <c>Svg3DRotationScript.resortDepth</c> sorts EVERY <c>[data-v3d]</c>
/// child of the scene group by average viewZ, including axis-infrastructure elements
/// tagged by Phase 3 (panes, cube edges, grid, axis labels, ticks). At non-default
/// camera angles some back-corner surface quads have viewZ more negative than the
/// back panes, so the sort places them earlier in DOM → they get painted first → the
/// opaque panes paint OVER them → visible surface jumps / vanishes.</para>
///
/// <para>Matplotlib (<c>mpl_toolkits/mplot3d/axes3d.py</c> L458-470) draws in FIXED
/// tiers — panes → grid → spines/ticks/labels → data — and only the data is
/// depth-sorted. We mirror that with three explicit subgroups:
/// <c>&lt;g class="mpl-3d-back"&gt;</c>, <c>&lt;g class="mpl-3d-data"&gt;</c>,
/// <c>&lt;g class="mpl-3d-front"&gt;</c>. JS sorts only inside <c>mpl-3d-data</c>.</para>
///
/// <para>This Theory asserts the DOM invariant: <b>no axis-infra polygon (class="mpl-pane")
/// ever appears after any series polygon</b> in the scene's children, at any
/// camera angle, after an interactive reproject. Covers the angles most likely to
/// expose depth-order bugs (matplotlib default, playground default, edge cases).</para></summary>
public class ThreeDPaneOcclusionTests
{
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
    public void PanesStayBeforeSeriesInDom_AfterDragRotate(int elev, int azim)
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
                .Surface(sx, sy, sz)));

        // Simulate a small drag (5 px) — enough to trigger reprojectAll + resortDepth.
        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 0; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 5; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointerup",   e => { e.clientX = 5; e.clientY = 0; });

        // Scan the scene in DOM order. Find the DOM index of the FIRST series polygon
        // (= polygon that is NOT class="mpl-pane") and the LAST pane polygon. Pane
        // index MUST be strictly less than series index — else the pane is drawn after
        // the series, covering it (the bug).
        int paneMaxIndex = -1, seriesMinIndex = int.MaxValue;
        var scene = h.Document.QuerySelectorAllRaw(".mpl-3d-scene").Single();
        int index = 0;
        // Walk descendants so the test is robust against structural subgroup introduction.
        foreach (var descendant in scene.Xml.Descendants())
        {
            if (descendant.Name.LocalName != "polygon") continue;
            var cls = descendant.Attribute("class")?.Value ?? "";
            var isPane = cls.Contains("mpl-pane");
            var hasV3d = descendant.Attribute("data-v3d") is not null;
            if (!hasV3d) { index++; continue; }
            if (isPane && index > paneMaxIndex) paneMaxIndex = index;
            if (!isPane && index < seriesMinIndex) seriesMinIndex = index;
            index++;
        }

        Assert.True(paneMaxIndex >= 0, "No <polygon class=\"mpl-pane\"> emitted — server render regression");
        Assert.True(seriesMinIndex < int.MaxValue, "No series polygons emitted — test fixture broken");
        Assert.True(paneMaxIndex < seriesMinIndex,
            $"elev={elev}, azim={azim}: pane polygon at DOM index {paneMaxIndex} appears AFTER first series polygon at index {seriesMinIndex} — pane will paint over surface quads.");
    }
}
