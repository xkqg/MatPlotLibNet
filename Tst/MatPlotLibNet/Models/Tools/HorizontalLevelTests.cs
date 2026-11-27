// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Tools;

/// <summary>Verifies <see cref="HorizontalLevel"/> behavior.</summary>
public class HorizontalLevelTests
{
    [Fact]
    public void Axes_AddLevel_AppendsToCollection()
    {
        var axes = new Axes();
        var level = axes.AddLevel(150.0);

        Assert.Single(axes.HorizontalLevels);
        Assert.Same(level, axes.HorizontalLevels[0]);
    }

    [Fact]
    public void Axes_AddLevel_MultipleLevelsAllStored()
    {
        var axes = new Axes();
        axes.AddLevel(100.0);
        axes.AddLevel(200.0);
        axes.AddLevel(300.0);

        Assert.Equal(3, axes.HorizontalLevels.Count);
    }
}
