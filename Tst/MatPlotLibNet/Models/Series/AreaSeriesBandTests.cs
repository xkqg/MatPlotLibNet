// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>The band a verb composes (<see cref="AreaSeries.Band"/>) and an indicator's channel (<see cref="AreaSeries.Channel"/>): the
/// opacity and the width of the outline are given, so neither is drawn as matplotlib's <c>fill_between</c> polygon under any theme.</summary>
public class AreaSeriesBandTests
{
    private static readonly double[] X = [0, 1, 2];

    [Fact]
    public void ABand_IsOutlinedAtTheLinesRestingWidth_WithTheOpacityGiven()
    {
        var band = AreaSeries.Band(X, [3.0, 4, 5], [1.0, 1, 1], 0.25);

        Assert.Equal(LineWidthRule.Line.Resting, band.Stroke.Width);
        Assert.Equal(0.25, band.GivenAlpha);
        Assert.Equal([1.0, 1, 1], band.YData2!);
        Assert.False(band.IsFillBetweenUnder(Theme.MatplotlibV2));
    }

    [Fact]
    public void ABandWithNoBottom_IsFilledDownToZero()
    {
        var band = AreaSeries.Band(X, [3.0, 4, 5], null, AreaSeries.RestingAlpha);

        Assert.Null(band.YData2);
        Assert.Equal(AreaSeries.RestingAlpha, band.GivenAlpha);
    }

    [Fact]
    public void AChannel_HasNoOutline_AndKeepsItsOwnLookUnderTheMatplotlibThemes()
    {
        var channel = AreaSeries.Channel(X, [3.0, 4, 5], [1.0, 2, 3], 0.15);

        Assert.Equal(0, channel.Stroke.Width);
        Assert.Equal(0.15, channel.GivenAlpha);
        Assert.False(channel.IsFillBetweenUnder(Theme.MatplotlibV2));
    }

    [Fact]
    public void ABandWhoseCurveDoesNotMatchItsPositions_IsRefused()
    {
        Assert.Throws<ArgumentException>(() => AreaSeries.Band(X, [3.0, 4], null, 0.3));
        Assert.Throws<ArgumentException>(() => AreaSeries.Channel(X, [3.0, 4, 5], [1.0], 0.3));
    }
}
