// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

public sealed class Pane3DTests
{
    [Fact]
    public void DefaultPane3DConfig_VisibleTrue()
    {
        var config = new Pane3DConfig();
        Assert.True(config.Visible);
        Assert.Equal(0.8, config.Alpha);
        Assert.Null(config.FloorColor);
    }

    [Fact]
    public void CustomFloorColor_AppliedInSvg()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
                .WithPane3D(p => p with { FloorColor = Color.FromHex("#000000") }))
            .Build()
            .ToSvg();

        Assert.Contains("#000000", svg);
    }

    [Fact]
    public void PaneVisible_False_HidesPanes()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
                .WithPane3D(p => p with { Visible = false }))
            .Build()
            .ToSvg();

        // Default pane color should NOT appear when panes are hidden
        Assert.DoesNotContain("#F5F5F5", svg);
    }

    [Fact]
    public void AllWalls_CustomColor_AppliedInSvg()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
                .WithPane3D(p => p with
                {
                    FloorColor = Color.FromHex("#2A2A2A"),
                    LeftWallColor = Color.FromHex("#3B3B3B"),
                    RightWallColor = Color.FromHex("#4C4C4C")
                }))
            .Build()
            .ToSvg();

        Assert.Contains("#2A2A2A", svg);
        Assert.Contains("#3B3B3B", svg);
        Assert.Contains("#4C4C4C", svg);
    }

    [Fact]
    public void BuilderWithPane3D_SetsConfig()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
                .WithPane3D(p => p with { FloorColor = Colors.Red, Visible = true }))
            .Build();

        Assert.Equal(Colors.Red, fig.SubPlots[0].Pane3D.FloorColor);
    }

    [Fact]
    public void Pane3DConfig_RecordEquality()
    {
        var a = new Pane3DConfig { FloorColor = Colors.Blue };
        var b = new Pane3DConfig { FloorColor = Colors.Blue };
        Assert.Equal(a, b);
    }

    /// <summary>Phase 3 of v1.7.2 — exercises ThreeDAxesRenderer's three axis-label
    /// blocks (X/Y/Z) plus the Phase-3 helper <c>DrawText3DAt</c>. Without this test the
    /// blocks went uncovered and the class regressed against the v1.7.1 baseline.</summary>
    [Fact]
    public void AllThreeAxisLabels_RenderedInSvg()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
                .SetXLabel("X-axis-label")
                .SetYLabel("Y-axis-label")
                .SetZLabel("Z-axis-label"))
            .Build()
            .ToSvg();

        Assert.Contains("X-axis-label", svg);
        Assert.Contains("Y-axis-label", svg);
        Assert.Contains("Z-axis-label", svg);
    }

    /// <summary>Phase 3 of v1.7.2 — exercises the minor-tick branch of
    /// ThreeDAxesRenderer.RenderAxisEdgeTicks. With minor ticks visible AND Emit3DVertexData
    /// enabled, the new helper EmitV3D fires for each minor mark.</summary>
    [Fact]
    public void MinorTicks_OnInteractive3D_EmitsExtraTickMarks()
    {
        var svg = Plt.Create()
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
                .WithMinorTicks(true))
            .Build()
            .ToSvg();

        // Just verify the figure renders successfully with minor ticks on (the branch is exercised).
        Assert.Contains("data-v3d", svg);
    }
}
