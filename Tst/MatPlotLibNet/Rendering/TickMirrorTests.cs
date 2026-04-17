// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

public sealed class TickMirrorTests
{
    [Fact]
    public void MirrorProperty_DefaultFalse()
    {
        var config = new TickConfig();
        Assert.False(config.Mirror);
    }

    [Fact]
    public void MirrorProperty_CanBeSetTrue()
    {
        var config = new TickConfig { Mirror = true };
        Assert.True(config.Mirror);
    }

    [Fact]
    public void WithRecord_PreservesOtherProperties()
    {
        var config = new TickConfig { Visible = true, Mirror = false };
        var mirrored = config with { Mirror = true };
        Assert.True(mirrored.Mirror);
        Assert.True(mirrored.Visible);
    }

    [Fact]
    public void WithYTicksMirrored_SetsMirrorOnMajorTicks()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithYTicksMirrored())
            .Build();

        Assert.True(fig.SubPlots[0].YAxis.MajorTicks.Mirror);
    }

    [Fact]
    public void WithXTicksMirrored_SetsMirrorOnMajorTicks()
    {
        var fig = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0], [3.0, 4.0])
                .WithXTicksMirrored())
            .Build();

        Assert.True(fig.SubPlots[0].XAxis.MajorTicks.Mirror);
    }

    [Fact]
    public void MirroredYTicks_SvgContainsRightSideLabels()
    {
        var svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0, 3.0], [10.0, 20.0, 30.0])
                .WithYTicksMirrored())
            .Build()
            .ToSvg();

        // SVG should contain text elements for both left and right side tick labels.
        // The mirrored labels use TextAlignment.Left (right side) vs TextAlignment.Right (left side).
        // Count how many Y tick label texts appear — should be double the normal amount.
        int tickLabelCount = System.Text.RegularExpressions.Regex.Matches(svg, @"text-anchor=""start""").Count;
        Assert.True(tickLabelCount > 0, "Mirrored Y ticks should produce right-aligned labels (text-anchor=start)");
    }
}
