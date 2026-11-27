// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Transforms;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>Verifies interactive 3D rotation: flag, SVG output with script and data attributes.</summary>
public class InteractiveRotationTests
{
    /// <summary>A surface of three by three, made rotatable, as the scene markup reads.</summary>
    private static string RotatableSurfaceSvg()
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();
        return new SvgTransform().Render(figure);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("SvgOutput_With3DRotation_ContainsScript", RotatableSurfaceSvg, "mpl-3d-scene"),
        new DrawnToken("SvgOutput_With3DRotation_PolygonsHaveDataV3d", RotatableSurfaceSvg, "data-v3d="),
        // 3D rotation script must register a scroll-wheel listener so users can zoom the camera distance without keyboard. { passive:
        // false } ensures preventDefault() actually overrides browser/iframe page scroll.
        new DrawnToken("Script_HasNonPassiveWheelListener", RotatableSurfaceSvg, "'wheel'", "passive: false"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

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

    [Theory]
    [InlineData("el.tagName === 'line'")] // Script_ReprojectsLineElements
    [InlineData("el.tagName === 'text'")] // Script_ReprojectsTextElements
    [InlineData("initDistance")] // Script_HomeKeyAlsoResetsDistance
    public void TheScript_CarriesWhatItNeedsToTurnAScene(string token)
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        Assert.Contains(token, svg);
    }

    // ── Phase 3 of v1.7.2 plan — axes infrastructure must live INSIDE the scene group ──

    /// <summary>The cube edges (the 9 explicit DrawLine calls in ThreeDAxesRenderer, <c>line</c> elements) and the tick
    /// labels (<c>text</c> elements; the renderer emits ticks via DrawLine + DrawText calls inside Render3DAxisTicks) must carry a
    /// data-v3d attribute when 3D rotation is enabled, or the JS reproject can't move them as the camera rotates. The simplest
    /// assertion: at least one such element carries it in the output.</summary>
    [Theory]
    [InlineData("line")]
    [InlineData("text")]
    public void CubeEdgesAndTickLabels_CarryDataV3d_WhenRotationEnabled(string element)
    {
        var figure = Plt.Create()
            .Surface([0, 5, 10], [0, 5, 10], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } })
            .With3DRotation()
            .Build();

        var svg = new SvgTransform().Render(figure);
        Assert.Contains(svg.Elements(element), tag => tag["data-v3d"] is not null);
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
        var scene = svg.Each("g").FirstOrDefault(group => (string?)group.Attribute("class") == "mpl-3d-scene");
        Assert.True(scene is not null, "Scene group not emitted");
        Assert.NotEmpty(scene!.Each("line"));
    }
}
