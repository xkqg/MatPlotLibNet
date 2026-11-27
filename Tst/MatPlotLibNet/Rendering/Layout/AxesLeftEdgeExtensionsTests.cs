// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.Layout;

/// <summary>The rules for what the left edge of an axes needs, read from the axes alone: the left stack of its Y axis container
/// (the primary's tick labels and title, then every slot past it), the pad past it, and the label a Y axis whose labels are not
/// measured is estimated by.</summary>
public sealed class AxesLeftEdgeExtensionsTests
{
    private static readonly Theme Theme = Theme.Default;

    private static double PrimaryBand(Axes axes, double tickBand) =>
        axes.YAxis.YTitleExtent(axes.YAxis.Label, new YAxisTitleExtensions.YAxisTitleInput(tickBand, AxisSide.Left, axes.YAxes.TitleGap, Theme.LabelFont())).Band;

    [Fact]
    public void TheLeftStack_OfAnAxesWithNothingOnItsLeft_IsEmpty()
    {
        Assert.Equal(0, new Axes().LeftStackBand(Theme, 0, default, []));
    }

    [Fact]
    public void TheLeftStack_IsThePrimarysBandAndItsTitle()
    {
        var axes = new Axes();
        axes.YAxis.Label = "y title";

        Assert.Equal(PrimaryBand(axes, 30), axes.LeftStackBand(Theme, 30, default, []));
        Assert.True(axes.LeftStackBand(Theme, 30, default, []) > 30);
    }

    [Fact]
    public void TheLeftStack_StacksEverySlotPastThePrimary_WithTheContainersGap()
    {
        var axes = new Axes();
        double[] stack = [PrimaryBand(axes, 30), 10, 20];

        Assert.Equal(stack.TotalStackedBand(axes.YAxes.TitleGap), axes.LeftStackBand(Theme, 30, default, [10, 20]));
    }

    [Fact]
    public void TheLeftNeed_IsTheLeftStackAndThePad()
    {
        var axes = new Axes();

        Assert.Equal(axes.LeftStackBand(Theme, 30, default, [10]) + ConstrainedLayoutEngine.PadLeft, axes.LeftNeed(Theme, 30, default, [10], ConstrainedLayoutEngine.PadLeft));
    }

    /// <summary>The pad holds what an axes draws off the edge; an axes that draws nothing on its left needs nothing there, not even
    /// the pad, as the right edge needs nothing past a spine nothing is drawn past (<see cref="RightBand.WidthWith"/>).</summary>
    [Fact]
    public void TheLeftNeed_OfAnAxesThatDrawsNothingOnItsLeft_IsNothing_NotThePad()
    {
        Assert.Equal(0, new Axes().LeftNeed(Theme, 0, default, [], 15));
    }

    [Fact]
    public void TheEstimatedYTickLabel_IsTheUpperLimit_WhenOneIsSet()
    {
        var axes = new Axes();
        axes.YAxis.Min = -5;
        axes.YAxis.Max = 1250;

        Assert.Equal(NumericTickFormatter.FormatValue(1250), axes.EstimateYTickLabel());
    }

    [Fact]
    public void TheEstimatedYTickLabel_IsTheLowerLimit_WhenOnlyThatIsSet()
    {
        var axes = new Axes();
        axes.YAxis.Min = -5;

        Assert.Equal(NumericTickFormatter.FormatValue(-5), axes.EstimateYTickLabel());
    }

    [Fact]
    public void TheEstimatedYTickLabel_WithoutLimits_IsAWorstCaseNumber()
    {
        Assert.Equal("−9.999", new Axes().EstimateYTickLabel());
    }
}
