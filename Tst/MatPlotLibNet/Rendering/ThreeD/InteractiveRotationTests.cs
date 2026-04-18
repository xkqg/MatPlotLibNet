// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>Verifies interactive 3D rotation: flag, SVG output with script and data attributes.</summary>
public class InteractiveRotationTests
{
    [Fact]
    public void Figure_Enable3DRotation_DefaultIsFalse()
    {
        var figure = new Figure();
        Assert.False(figure.Enable3DRotation);
    }

    [Fact]
    public void SvgOutput_With3DRotation_ContainsScript()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

        var figure = Plt.Create()
            .Surface(x, y, z)
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        Assert.Contains("mpl-3d-scene", svg);
    }

    [Fact]
    public void SvgOutput_With3DRotation_PolygonsHaveDataV3d()
    {
        double[] x = [0, 5, 10];
        double[] y = [0, 5, 10];
        double[,] z = { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } };

        var figure = Plt.Create()
            .Surface(x, y, z)
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        Assert.Contains("data-v3d=", svg);
    }

    [Fact]
    public void FigureBuilder_With3DRotation_SetsFlag()
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();

        Assert.True(figure.Enable3DRotation);
    }

    // ── Phase 0 of v1.7.2 interaction-hardening plan ──────────────────────────
    // The 3D rotation script previously only handled <polygon>/<polyline>/<circle>
    // when reprojecting on drag. <line> and <text> elements (cube edges, grid lines,
    // axis-tick marks, axis labels) tagged with data-v3d would stay static. Phase 0
    // adds reproject branches for both, plus a scroll-wheel zoom binding and a
    // distance-reset on the Home key so the keyboard zoom can be undone.

    /// <summary>The reproject loop must include a branch that updates <c>x1/y1/x2/y2</c>
    /// on `&lt;line&gt;` elements — otherwise tagged cube edges and grid lines stay still
    /// during drag-rotate.</summary>
    [Fact]
    public void Script_ReprojectsLineElements()
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        Assert.Contains("el.tagName === 'line'", svg);
    }

    /// <summary>The reproject loop must include a branch that updates <c>x/y</c>
    /// on `&lt;text&gt;` elements — otherwise tagged tick labels and axis titles stay still
    /// during drag-rotate.</summary>
    [Fact]
    public void Script_ReprojectsTextElements()
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        Assert.Contains("el.tagName === 'text'", svg);
    }

    /// <summary>3D rotation script must register a scroll-wheel listener so users can
    /// zoom the camera distance without keyboard. <c>{ passive: false }</c> ensures
    /// <c>preventDefault()</c> actually overrides browser/iframe page scroll.</summary>
    [Fact]
    public void Script_HasNonPassiveWheelListener()
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        Assert.Contains("'wheel'", svg);
        Assert.Contains("passive: false", svg);
    }

    /// <summary>Home key must reset distance back to its original value, not just
    /// elevation/azimuth. Otherwise wheel-zoomed cameras can never be undone.</summary>
    [Fact]
    public void Script_HomeKeyAlsoResetsDistance()
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        // The script captures initDistance and the 'Home' branch must restore it.
        Assert.Contains("initDistance", svg);
    }

    // ── Phase 3 of v1.7.2 plan — axes infrastructure must live INSIDE the scene group ──

    /// <summary>The cube edges (the 9 explicit DrawLine calls in ThreeDAxesRenderer)
    /// must carry a data-v3d attribute when 3D rotation is enabled — otherwise the JS
    /// reproject can't move them as the camera rotates.</summary>
    [Fact]
    public void CubeEdges_CarryDataV3d_WhenRotationEnabled()
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        // After Phase 3, every <line> element emitted by the cube-edge code must have
        // a data-v3d attribute. The simplest assertion: at least one <line data-v3d="..."
        // appears in the output.
        Assert.Matches(@"<line[^>]*\bdata-v3d=", svg);
    }

    /// <summary>Tick marks + tick labels must move with the rotation too. The renderer
    /// emits them via DrawLine + DrawText calls inside Render3DAxisTicks; both element
    /// types need data-v3d after Phase 3.</summary>
    [Fact]
    public void TickLabels_CarryDataV3d_WhenRotationEnabled()
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        // Tick labels are <text> elements. After Phase 3 at least one must carry data-v3d.
        Assert.Matches(@"<text[^>]*\bdata-v3d=", svg);
    }

    /// <summary>Scene-group brackets must enclose the panes/edges/grid/labels/ticks
    /// (not just the series). The simplest structural check: the scene group contains
    /// at least one <c>&lt;line&gt;</c> element (not just polygons).</summary>
    [Fact]
    public void SceneGroup_ContainsLineElements_NotJustPolygons()
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        // Find scene-group block and assert it has <line> children (cube edges + grid + ticks).
        var match = System.Text.RegularExpressions.Regex.Match(
            svg, @"<g class=""mpl-3d-scene""[^>]*>(?<body>.*?)</g>",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        Assert.True(match.Success, "Scene group not emitted");
        Assert.Matches(@"<line\b", match.Groups["body"].Value);
    }
}
