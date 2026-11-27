// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Ichimoku"/> behavior.</summary>
public class IchimokuTests
{
    private static readonly double[] High = Enumerable.Range(1, 60).Select(i => (double)(50 + i)).ToArray();
    private static readonly double[] Low = Enumerable.Range(1, 60).Select(i => (double)(40 + i)).ToArray();
    private static readonly double[] Close = Enumerable.Range(1, 60).Select(i => (double)(45 + i)).ToArray();

    /// <summary>Over fewer bars than the kijun period the base line has no value, and so neither has the first leading span. It
    /// used to throw an OverflowException (a span of negative length) once the conversion line had a value and the base line none,
    /// which a streaming form compared against the batch over every prefix ran into on its tenth bar.</summary>
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    public void Compute_OverFewerBarsThanTheKijunPeriod_HasNoSpanA_InsteadOfThrowing(int count)
    {
        var result = new Ichimoku(OhlcvBars.Hlc(High[..count], Low[..count], Close[..count]), tenkanPeriod: 3, kijunPeriod: 26, senkouBPeriod: 52).Compute();

        Assert.Empty(result.KijunSen);
        Assert.Empty(result.SenkouSpanA);
        Assert.Equal(count - 3 + 1, result.TenkanSen.Length);
    }

    /// <summary>Periods under which the base line has no value over the bars (a kijun longer than the bars, the span B shorter) give no
    /// first leading span, so there is no cloud to fill: the lines are drawn and the cloud is not.</summary>
    [Fact]
    public void Apply_WhenTheBaseLineHasNoValue_DrawsNoCloud()
    {
        var axes = new Axes();

        new Ichimoku(OhlcvBars.Hlc(High, Low, Close), tenkanPeriod: 3, kijunPeriod: 61, senkouBPeriod: 52).Apply(axes);

        Assert.NotEmpty(axes.Series);
        Assert.DoesNotContain(axes.Series, series => series.Label == "Cloud");
    }
}
