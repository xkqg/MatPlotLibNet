// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The frame of the plot: the grid is shown, the spines are configured, and a hidden spine is only its own edge.</summary>
public class AxesBuilderFrameVerbsTests
{
    [Fact]
    public void ShowGrid_ShowsTheGrid() => Assert.True(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.ShowGrid()).Build().SubPlots[0].Grid.Visible);

    [Fact]
    public void WithSpines_ConfiguresSpines()
    {
        var axes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.WithSpines(s => s with { Top = s.Top with { Visible = false } }).Plot([1.0], [2.0]))
            .Build().SubPlots[0];

        Assert.False(axes.Spines.Top.Visible);
        Assert.True(axes.Spines.Bottom.Visible);
    }

    [Fact]
    public void HideTopSpine_AndHideRightSpine_HideOnlyTheirEdge()
    {
        var top = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.HideTopSpine()).Build().SubPlots[0].Spines;
        var right = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.HideRightSpine()).Build().SubPlots[0].Spines;

        Assert.Equal([false, true, true, true], new[] { top.Top.Visible, top.Bottom.Visible, top.Left.Visible, top.Right.Visible });
        Assert.Equal([true, true, true, false], new[] { right.Top.Visible, right.Bottom.Visible, right.Left.Visible, right.Right.Visible });
    }
}
