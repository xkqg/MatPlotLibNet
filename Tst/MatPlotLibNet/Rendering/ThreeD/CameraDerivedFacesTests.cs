// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering.Svg.Interaction;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>
/// GitHub issue #18 — the shaded panes, the drawn cube edges and the tick rows must follow the
/// camera out of the historical <c>azimuth ∈ [−90°, 0°]</c> quadrant instead of staying on the
/// faces that quadrant made back-facing. These tests read the emitted geometry (the normalized
/// <c>data-v3d</c> vertices), so they assert WHERE the frame is drawn, not merely that it renders.
/// </summary>
public sealed class CameraDerivedFacesTests
{
    // Half-widths of matplotlib's world box: the emitted vertices are centred, so a face sits at
    // exactly ±half on its own axis.
    private const double HalfX = 25.0 / 21.0 / 2, HalfZ = 25.0 / 28.0 / 2;

    private static string Render(double azimuth, double elevation = 30) => Plt.Create()
        .WithSize(600, 500)
        .WithBrowserInteraction()
        .AddSubPlot(1, 1, 1, ax => ax
            .WithCamera(elevation: elevation, azimuth: azimuth)
            .SetXLabel("X").SetYLabel("Y").SetZLabel("Z")
            .Surface([0.0, 1.0, 2.0], [0.0, 1.0, 2.0], new double[,] { { 0, 1, 0 }, { 1, 2, 1 }, { 0, 1, 0 } }))
        .Build()
        .ToSvg();

    private static IReadOnlyList<double[]> V3dOf(string svg, string elementPattern)
    {
        var result = new List<double[]>();
        foreach (Match m in Regex.Matches(svg, elementPattern))
            result.Add(m.Groups["v3d"].Value.Split(' ')
                .SelectMany(p => p.Split(',').Select(c => double.Parse(c, CultureInfo.InvariantCulture)))
                .ToArray());
        return result;
    }

    /// <summary>Verifies the X wall is shaded on x = xMin at the default camera and on x = xMax past −90°.</summary>
    [Theory]
    [InlineData(-60, -1)]
    [InlineData(-145, +1)]
    public void XWallPane_FollowsTheCamera(double azimuth, int expectedSign)
    {
        var panes = V3dOf(Render(azimuth), "<polygon[^>]*class=\"mpl-pane\"[^>]*data-v3d=\"(?<v3d>[^\"]+)\"");
        Assert.Equal(3, panes.Count);

        // The X wall is the pane whose four vertices share one x — and that x must be on the
        // camera-selected side.
        var wall = panes.Single(p => Math.Abs(p[0] - p[3]) < 1e-9 && Math.Abs(p[0] - p[6]) < 1e-9);
        Assert.Equal(expectedSign * HalfX, wall[0], 5);
    }

    /// <summary>Verifies the Z tick row moves to the x = xMin vertical edge past −90° (the issue's symptom).</summary>
    [Theory]
    [InlineData(-60, +1)]
    [InlineData(-145, -1)]
    public void ZTickRow_SitsOnTheCameraSelectedVerticalEdge(double azimuth, int expectedSign)
    {
        // Tick label anchors carry a single vertex; the Z row is the one whose z varies while x and
        // y stay pinned. Take the labels whose x-component is the same on every one of them.
        var anchors = V3dOf(Render(azimuth), "<text[^>]*data-v3d=\"(?<v3d>[^\" ]+)\"[^>]*data-v3d-edge");
        Assert.NotEmpty(anchors);

        // The emitted vertices carry 6 significant digits, so compare with a G6-sized tolerance.
        var zRow = anchors.Where(a => Math.Abs(Math.Abs(a[2]) - HalfZ) > 1e-4).ToList();
        Assert.NotEmpty(zRow);
        Assert.All(zRow, a => Assert.Equal(expectedSign * HalfX, a[0], 5));
    }

    /// <summary>Verifies the scene group publishes the face selection the server drew with.</summary>
    [Theory]
    [InlineData(30, -60, "010")]
    [InlineData(30, -145, "110")]
    [InlineData(-30, -60, "011")]
    public void SceneGroup_PublishesTheServerFaceSelection(double elevation, double azimuth, string expected)
    {
        var svg = Render(azimuth, elevation);

        var match = Regex.Match(svg, "class=\"mpl-3d-scene\"[^>]*data-faces=\"(?<faces>[01]{3})\"");
        Assert.True(match.Success, "the 3-D scene group must carry data-faces");
        Assert.Equal(expected, match.Groups["faces"].Value);
    }

    /// <summary>Verifies every pane, cube edge and tick row declares which components the browser must mirror.</summary>
    [Fact]
    public void AxisInfrastructure_DeclaresItsPinnedAxes()
    {
        var svg = Render(-60);

        Assert.All(Regex.Matches(svg, "<polygon[^>]*class=\"mpl-pane\"[^>]*>").Select(m => m.Value),
            pane => Assert.Contains("data-v3d-pinned=\"xyz\"", pane));
        Assert.Contains("data-v3d-pinned=\"yz\"", svg);   // X tick row + floor edges
        Assert.Contains("data-v3d-pinned=\"xz\"", svg);   // Y tick row
        Assert.Contains("data-v3d-pinned=\"xy\"", svg);   // Z tick row + verticals
    }

    /// <summary>
    /// Verifies a drag that crosses the −90° face boundary makes the BROWSER re-select too: the
    /// scene's published selection changes and the X wall ends up on the same side the server would
    /// have drawn it at the post-drag angle.
    /// </summary>
    [Fact]
    public void Drag_AcrossTheFaceBoundary_MakesTheBrowserReselect()
    {
        using var h = InteractionScriptHarness.FromBuilder(b => b
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0, 2.0], [0.0, 1.0, 2.0], new double[,] { { 0, 1, 0 }, { 1, 2, 1 }, { 0, 1, 0 } })));

        Assert.Equal("010", h.GetAttribute(".mpl-3d-scene", "data-faces"));

        // Drag right by 100 px ≈ −47° of azimuth on this cube, crossing −90°.
        h.Simulate(".mpl-3d-scene", "pointerdown", e => { e.clientX = 0; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointermove", e => { e.clientX = 100; e.clientY = 0; });
        h.Simulate(".mpl-3d-scene", "pointerup", e => { e.clientX = 100; e.clientY = 0; });

        Assert.Equal("110", h.GetAttribute(".mpl-3d-scene", "data-faces"));

        var wall = h.Document.QuerySelectorAllRaw("polygon[data-v3d-pinned]")
            .Select(el => el.getAttribute("data-v3d")!)
            .Select(v => v.Split(' ').Select(p => p.Split(',')
                .Select(c => double.Parse(c, CultureInfo.InvariantCulture)).ToArray()).ToArray())
            .Single(q => Math.Abs(q[0][0] - q[1][0]) < 1e-9 && Math.Abs(q[0][0] - q[2][0]) < 1e-9);

        Assert.Equal(HalfX, wall[0][0], 5);
    }
}
