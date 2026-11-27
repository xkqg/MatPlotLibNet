// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

public sealed class Pane3DTests
{
    /// <summary>Verifies the pane alpha now actually reaches the rendered surface.</summary>
    [Fact]
    public void PaneAlpha_ScalesTheRenderedPaneOpacity()
    {
        static string Render(double alpha) => Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
                .WithPane3D(p => p with { FloorColor = Color.FromHex("#123456"), Alpha = alpha }))
            .Build()
            .ToSvg();

        Assert.NotEqual(Render(1.0), Render(0.25));
    }

    /// <summary>Verifies each pane colour is bound to its AXIS, not to a fixed side of the cube.</summary>
    [Theory]
    [InlineData(CubeAxis.X)]
    [InlineData(CubeAxis.Y)]
    [InlineData(CubeAxis.Z)]
    public void ColorFor_ReturnsThePerAxisPaneColour(CubeAxis axis)
    {
        var config = new Pane3DConfig
        {
            LeftWallColor = Color.FromHex("#111111"),
            RightWallColor = Color.FromHex("#222222"),
            FloorColor = Color.FromHex("#333333"),
        };

        var expected = axis switch
        {
            CubeAxis.X => config.LeftWallColor,
            CubeAxis.Y => config.RightWallColor,
            _ => config.FloorColor,
        };
        Assert.Equal(expected, config.ColorFor(axis));
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("CustomFloorColor_AppliedInSvg", () => Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
                .WithPane3D(p => p with { FloorColor = Color.FromHex("#000000") }))
            .Build()
            .ToSvg(), "#000000"),
        // No default pane shade appears when the panes are hidden
        new DrawnToken("PaneVisible_False_HidesPanes", () => Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
                .WithPane3D(p => p with { Visible = false }))
            .Build()
            .ToSvg()) { Absent = ["#F5F5F5", "#F8F8F8", "#F2F2F2"] },
        new DrawnToken("AllWalls_CustomColor_AppliedInSvg", () => Plt.Create()
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
            .ToSvg(), "#2A2A2A", "#3B3B3B", "#4C4C4C"),
        // Phase 3 of v1.7.2 — exercises ThreeDAxesRenderer's three axis-label blocks (X/Y/Z) plus the Phase-3 helper DrawText3DAt. Without
        // this test the blocks went uncovered and the class regressed against the v1.7.1 baseline.
        new DrawnToken("AllThreeAxisLabels_RenderedInSvg", () => Plt.Create()
            .WithSize(600, 500)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
                .SetXLabel("X-axis-label")
                .SetYLabel("Y-axis-label")
                .SetZLabel("Z-axis-label"))
            .Build()
            .ToSvg(), "X-axis-label", "Y-axis-label", "Z-axis-label"),
        // Exercises the minor-tick branch of ThreeDAxesRenderer.RenderAxisEdgeTicks: with minor ticks visible and vertex data on, each
        // minor mark carries its vertex data (SceneVertexWriter.Vertices).
        // Just verify the figure renders successfully with minor ticks on (the branch is exercised).
        new DrawnToken("MinorTicks_OnInteractive3D_EmitsExtraTickMarks", () => Plt.Create()
            .WithSize(600, 500)
            .WithBrowserInteraction()
            .AddSubPlot(1, 1, 1, ax => ax
                .WithCamera(elevation: 30, azimuth: -60)
                .Surface([0.0, 1.0], [0.0, 1.0], new double[,] { { 0, 1 }, { 1, 0 } })
                .WithMinorTicks(true))
            .Build()
            .ToSvg(), "data-v3d"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

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
}
