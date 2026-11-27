// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The band past the right spine of an axes is composed in ONE place (<see cref="RightBand.Of"/>): the bands of its right Y axes
/// stacked with the container's gap between each pair, the mirrored Y tick labels, the end labels. The layout composes it from the bands
/// it measures, the render from the bands it placed.</summary>
public class RightBandTests
{
    [Fact]
    public void TheRightAxes_AreStackedWithTheContainersGap()
    {
        var axes = new Axes();
        double gap = axes.YAxes.TitleGapUnder(Theme.Default);

        var band = RightBand.Of(axes, Theme.Default, [10, 20], 5, 7);

        Assert.Equal(new RightBand(10 + gap + 20, 5, 7), band);
    }

    [Fact]
    public void NoRightAxis_IsNoAxisBand() => Assert.Equal(new RightBand(0, 5, 7), RightBand.Of(new Axes(), Theme.Default, [], 5, 7));

    /// <summary>The gap is the container's under the theme: a matplotlib theme's differs from the library's.</summary>
    [Fact]
    public void TheGap_IsTheContainersUnderTheTheme()
    {
        var axes = new Axes();

        Assert.Equal(10 + axes.YAxes.TitleGapUnder(Theme.MatplotlibV2) + 20, RightBand.Of(axes, Theme.MatplotlibV2, [10, 20], 0, 0).SecondaryAxis);
    }
}
