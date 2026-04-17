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
}
