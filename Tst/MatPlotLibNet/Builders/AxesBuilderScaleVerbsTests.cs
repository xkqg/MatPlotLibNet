// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>How the values of an axis map onto it: each scale verb sets the scale of the axis it names.</summary>
public class AxesBuilderScaleVerbsTests
{
    [Fact]
    public void AScale_IsSetOnTheAxisItNames()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetYScale(AxisScale.Log).WithSymlogXScale(2.0)).Build().SubPlots[0];

        Assert.Equal(AxisScale.Log, axes.YAxis.Scale);
        Assert.Equal(AxisScale.SymLog, axes.XAxis.Scale);
    }
}
