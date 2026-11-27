// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Steps;

namespace MatPlotLibNet.Tests.Indicators.Steps;

/// <summary>The commodity channel index of one window: the arithmetic the batch CCI and the streaming CCI both read.</summary>
public sealed class ChannelIndexExtensionsTests
{
    [Fact]
    public void TheIndex_IsTheDistanceFromTheMean_InMeanDeviationsScaledByLambertsConstant()
    {
        ReadOnlySpan<double> window = [10, 12, 14];

        // mean 12, mean deviation (2 + 0 + 2) / 3 = 4/3, newest 14: (14 - 12) / (0.015 * 4/3) = 100
        Assert.Equal(100, window.ChannelIndex(12, 14), 9);
    }

    [Fact]
    public void AWindowWithoutDeviation_HasTheIndex0()
    {
        ReadOnlySpan<double> window = [7, 7, 7];

        Assert.Equal(0, window.ChannelIndex(7, 7));
    }

    [Fact]
    public void TheOrderOfTheWindow_DoesNotMoveTheIndex()
    {
        ReadOnlySpan<double> oldestFirst = [10, 11, 15, 12];
        ReadOnlySpan<double> asARingHoldsThem = [15, 12, 10, 11];

        Assert.Equal(oldestFirst.ChannelIndex(12, 12), asARingHoldsThem.ChannelIndex(12, 12));
    }
}
