// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The range each axis shows: the limits of the three axes and the margin of the automatic range are set as named.</summary>
public class AxesBuilderLimitVerbsTests
{
    [Fact]
    public void TheLimitsAndTheMargin_AreSetOnTheAxesTheyName()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax
            .SetXLim(0, 10)
            .SetYLim(-50, 50)
            .SetZLim(-5, 5)
            .SetYMargin(0.25)).Build().SubPlots[0];

        Assert.Equal([0.0, 10.0, -50.0, 50.0, -5.0, 5.0], new[] { axes.XAxis.Min, axes.XAxis.Max, axes.YAxis.Min, axes.YAxis.Max, axes.ZAxis.Min, axes.ZAxis.Max });
        Assert.Equal(0.25, axes.YAxis.Margin);
    }
}
