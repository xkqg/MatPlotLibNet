// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Data;

namespace MatPlotLibNet.Tests.Data;

/// <summary>An <see cref="OhlcvBar"/> is an <see cref="OhlcBar"/> that also says how much traded. A bar that arrives
/// without a volume carries NaN, never 0: a volume of 0 is a fact (nothing traded), NaN is the absence of one, and the
/// two make an indicator answer differently.</summary>
public sealed class OhlcvBarTests
{
    [Fact]
    public void ItHoldsTheFivePricesAndVolumeItWasGiven()
    {
        var bar = new OhlcvBar(10, 12, 9, 11, 500);

        Assert.Equal((10.0, 12.0, 9.0, 11.0, 500.0), (bar.Open, bar.High, bar.Low, bar.Close, bar.Volume));
    }

    [Fact]
    public void ABarWithoutAVolume_CarriesNaN_NotZero()
    {
        var bar = new OhlcvBar(10, 12, 9, 11);

        Assert.True(double.IsNaN(bar.Volume));
    }

    [Fact]
    public void FromAnOhlcBar_KeepsThePrices_AndHasNoVolume()
    {
        var bar = OhlcvBar.From(new OhlcBar(10, 12, 9, 11));

        Assert.Equal(new OhlcvBar(10, 12, 9, 11), bar);
        Assert.True(double.IsNaN(bar.Volume));
    }

    [Fact]
    public void Ohlc_IsTheSamePricesWithoutTheVolume()
    {
        var ohlc = new OhlcvBar(10, 12, 9, 11, 500).Ohlc;

        Assert.Equal(new OhlcBar(10, 12, 9, 11), ohlc);
    }

    [Fact]
    public void TwoBarsWithoutAVolume_AreEqual_ThoughNaNIsNotEqualToItself()
    {
        Assert.Equal(new OhlcvBar(1, 2, 0.5, 1.5), new OhlcvBar(1, 2, 0.5, 1.5));
    }
}
