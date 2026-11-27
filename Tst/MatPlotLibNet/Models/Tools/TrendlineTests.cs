// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Tools;

/// <summary>Verifies <see cref="Trendline"/> behavior.</summary>
public class TrendlineTests
{
    [Fact]
    public void Axes_AddTrendline_AppendsToCollection()
    {
        var axes = new Axes();
        var line = axes.AddTrendline(0.0, 10.0, 5.0, 20.0);

        Assert.Single(axes.Trendlines);
        Assert.Same(line, axes.Trendlines[0]);
    }

    [Fact]
    public void Axes_AddTrendline_MultipleLinesAllStored()
    {
        var axes = new Axes();
        axes.AddTrendline(0, 0, 1, 1);
        axes.AddTrendline(2, 2, 3, 3);

        Assert.Equal(2, axes.Trendlines.Count);
    }
}
