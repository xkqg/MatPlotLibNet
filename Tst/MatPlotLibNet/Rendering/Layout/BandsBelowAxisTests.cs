// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>What one X axis draws below the edge it hangs from is ONE value with ONE rule for how far it reaches: the title, which stands
/// past the tick labels, when it has one, else the tick label band. The primary X axis' measure and draw, the linked X axes below the
/// plot and the measured bands all read it.</summary>
public class BandsBelowAxisTests
{
    /// <summary>The bands reach the far side of the title when the axis has one, else the far side of the tick labels, else nothing.</summary>
    [Theory]
    [InlineData(25, 40, 40)] // titled
    [InlineData(25, 0, 25)] // no title
    [InlineData(0, 0, 0)] // nothing below
    public void TheBandsBelowAnAxis_ReachTheFarSideOfTheOuterBand(double tickLabels, double title, double extent) =>
        Assert.Equal(extent, new BandsBelowAxis(tickLabels, title).Extent);

    /// <summary>An axis says its bands by the rules the drawing places them by: the tick label band of its major ticks and its title's
    /// extent below labels that tall.</summary>
    [Theory]
    [InlineData(null, 12.0)]
    [InlineData("time", 12.0)]
    [InlineData("time", 0.0)]
    public void AnAxis_SaysItsTickLabelBandAndItsTitleExtent(string? label, double tallest)
    {
        var axis = new Axis { Label = label };
        var ctx = new RecordingRenderContext();
        var theme = Theme.Default;

        var below = axis.BandsBelow(axis.Label, ctx, theme, tallest);

        Assert.Equal(axis.MajorTicksUnder(theme).TickLabelBand(tallest), below.TickLabels);
        Assert.Equal(axis.BottomTitleExtent(axis.Label, ctx, theme, tallest), below.Title);
    }

    /// <summary>The measured bands of an axes reach as far below as the lowest of the primary's bands and what hangs below the plot (the
    /// linked axes below it, a table below the axes).</summary>
    [Fact]
    public void TheMeasuredBands_ReachTheLowerOfThePrimaryAndTheLinkedAxes()
    {
        Assert.Equal(40, new EdgeBands(0, 0, 0, 25, 40).BottomExtent);
        Assert.Equal(25, new EdgeBands(0, 0, 0, 25, 0).BottomExtent);
        Assert.Equal(60, new EdgeBands(0, 0, 0, 25, 40, HungBelow: 60).BottomExtent);
        Assert.Equal(new BandsBelowAxis(25, 40), new EdgeBands(0, 0, 0, 25, 40).BelowPrimary);
    }
}
