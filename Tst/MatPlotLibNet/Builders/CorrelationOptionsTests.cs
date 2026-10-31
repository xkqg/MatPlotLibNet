// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>Verifies <see cref="CorrelationOptions"/>: <c>default</c> holds matplotlib's defaults for <c>acorr</c> and
/// <c>xcorr</c> (<c>maxlags=10</c>, <c>normed=True</c>, <c>detrend_none</c>, <c>usevlines=True</c>), and the number of lags
/// refuses, by its own name, a value no correlation can be drawn with.</summary>
public class CorrelationOptionsTests
{
    [Fact]
    public void Default_IsMatplotlibsDefaults()
    {
        CorrelationOptions options = default;

        Assert.Equal(10, options.MaxLags);
        Assert.False(options.Unnormalised);
        Assert.Equal(SpectralDetrend.None, options.Detrend);
        Assert.False(options.Markers);
    }

    [Fact]
    public void New_IsTheDefault()
    {
        Assert.Equal(default, new CorrelationOptions());
        Assert.Equal(10, new CorrelationOptions().MaxLags);
    }

    [Fact]
    public void EveryProperty_KeepsWhatItIsGiven()
    {
        var options = new CorrelationOptions
        {
            MaxLags = 4,
            Unnormalised = true,
            Detrend = SpectralDetrend.Linear,
            Markers = true,
        };

        Assert.Equal(4, options.MaxLags);
        Assert.True(options.Unnormalised);
        Assert.Equal(SpectralDetrend.Linear, options.Detrend);
        Assert.True(options.Markers);
    }

    [Fact]
    public void TheOneLagMatplotlibAllows_IsOne()
    {
        Assert.Equal(1, new CorrelationOptions { MaxLags = 1 }.MaxLags);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void MaxLags_BelowOne_Refuses(int maxLags)
    {
        // matplotlib: "maxlags must be None or strictly positive".
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new CorrelationOptions { MaxLags = maxLags });

        Assert.Equal(nameof(CorrelationOptions.MaxLags), error.ParamName);
    }

    [Fact]
    public void TwoOptionsWithTheSameValues_AreEqual_AndOneDifferentValueMakesThemApart()
    {
        Assert.Equal(new CorrelationOptions { MaxLags = 5, Markers = true }, new CorrelationOptions { MaxLags = 5, Markers = true });
        Assert.NotEqual(new CorrelationOptions { MaxLags = 5 }, new CorrelationOptions { MaxLags = 6 });
        Assert.NotEqual(new CorrelationOptions { Unnormalised = true }, default);
    }
}
