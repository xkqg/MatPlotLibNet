// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The circle a round chart is drawn in (a pie, a donut, a dial, a radar web, a sunburst, a chord ring, a polar axes) is one
/// value, <see cref="RoundFrame"/>: the centre of the plot bounds and a radius that is a share of half the shorter side, less an inset.
/// The shares are named once; a picture drawn from the sums each chart typed keeps every digit.</summary>
public class RoundFrameTests
{
    [Fact]
    public void TheFrameOfARectangle_IsCentredInItWithHalfTheShorterSide()
    {
        var frame = RoundFrame.In(new Rect(10, 20, 200, 100));

        Assert.Equal(new Point(110, 70), frame.Centre);
        Assert.Equal(50, frame.Radius);
    }

    [Fact]
    public void AShare_TakesThatMuchOfHalfTheShorterSide()
    {
        Assert.Equal(40, RoundFrame.In(new Rect(0, 0, 200, 100), 0.8).Radius, 12);
    }

    [Fact]
    public void AnInset_IsTakenOffTheRadius()
    {
        Assert.Equal(40, RoundFrame.In(new Rect(0, 0, 100, 200), 1, 10).Radius);
    }

    [Fact]
    public void ADialCentre_StandsLowerInTheBounds()
    {
        var frame = RoundFrame.In(new Rect(10, 20, 200, 100), RoundFrame.DiscShare, 0, 0.75);

        Assert.Equal(new Point(110, 95), frame.Centre);
        Assert.Equal(40, frame.Radius, 12);
    }

    [Fact]
    public void TheSharesOfTheRoundCharts_AreNamedOnce()
    {
        Assert.Equal(0.8, RoundFrame.DiscShare);
        Assert.Equal(0.75, RoundFrame.WebShare);
        Assert.Equal(1.0, RoundFrame.FullShare);
        Assert.Equal(0.75, RoundFrame.DialCentreDown);
    }

    /// <summary>The sums the charts typed before the frame owned them, for rectangles whose sides are not round numbers: the frame
    /// gives the same bits, so no picture moves.</summary>
    [Theory]
    [InlineData(623.7, 411.3)]
    [InlineData(381.9, 520.1)]
    [InlineData(0.1, 0.3)]
    [InlineData(1e6 + 0.37, 7.77)]
    public void TheFrame_GivesTheBitsOfTheSumsTheChartsTyped(double width, double height)
    {
        var bounds = new Rect(13.7, 29.3, width, height);
        double shorter = Math.Min(bounds.Width, bounds.Height);

        Assert.Equal(shorter / 2 * 0.8, RoundFrame.In(bounds, 0.8).Radius);       // a pie and a donut
        Assert.Equal(shorter * 0.4, RoundFrame.In(bounds, 0.8).Radius);           // a dial
        Assert.Equal(shorter / 2 * 0.75, RoundFrame.In(bounds, 0.75).Radius);     // a radar web
        Assert.Equal(shorter / 2 - 10.0, RoundFrame.In(bounds, 1, 10.0).Radius);  // a sunburst, a chord ring
        Assert.Equal(shorter / 2, RoundFrame.In(bounds).Radius);                // a polar axes
        Assert.Equal(0.37 * shorter / 2, RoundFrame.In(bounds, 0.37).Radius);     // a pie told its radius
        Assert.Equal(new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2), RoundFrame.In(bounds).Centre);
        Assert.Equal(bounds.Y + bounds.Height * 0.75, RoundFrame.In(bounds, 0.8, 0, 0.75).Centre.Y);
    }
}
