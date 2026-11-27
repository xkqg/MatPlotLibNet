// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Builders;

/// <summary>What names the marks: a colour bar asked for twice keeps what the first call set.</summary>
public class AxesBuilderLegendVerbsTests
{
    [Fact]
    public void WithColorBar_CalledTwice_AccumulatesSettings()
    {
        var axes = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Heatmap(new double[,] { { 1 }, { 2 } })
                .WithColorBar(cb => cb with { Label = "First" })
                .WithColorBar(cb => cb with { Visible = true }))
            .Build().SubPlots[0];

        Assert.True(axes.ColorBar!.Visible);
        Assert.Equal("First", axes.ColorBar!.Label);
    }
}
