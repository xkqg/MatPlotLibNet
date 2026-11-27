// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="VolumeIndicator"/> behavior.</summary>
public class VolumeIndicatorTests
{
    /// <summary>Compute returns a SignalResult wrapping the original volumes — covers
    /// the otherwise-unused <c>Compute()</c> method.</summary>
    [Fact]
    public void Compute_ReturnsVolumesAsSignalResult()
    {
        var volumes = new[] { 100.0, 200.0, 300.0 };
        var result = new VolumeIndicator(volumes).Compute();
        Assert.Equal(volumes, result.Values);
    }

    /// <summary>Custom labels propagate to the BarSeries categories.</summary>
    [Fact]
    public void Apply_WithExplicitLabels_UsesThemForCategories()
    {
        var axes = new Axes();
        new VolumeIndicator([10, 20], ["Mon", "Tue"]).Apply(axes);
        var bars = Assert.IsType<BarSeries>(axes.Series[0]);
        Assert.Equal(["Mon", "Tue"], bars.Categories);
    }

    /// <summary>A colour given to the indicator is the colour of its bars.</summary>
    [Fact]
    public void AGivenColour_IsTheColourOfTheBars()
    {
        var axes = new Axes();

        new VolumeIndicator([100.0, 120.0, 90.0]) { Color = MatPlotLibNet.Styling.Colors.Red }.Apply(axes);

        Assert.Equal(MatPlotLibNet.Styling.Colors.Red, Assert.IsType<BarSeries>(Assert.Single(axes.Series)).Color);
    }
}
