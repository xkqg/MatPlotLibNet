// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Obv"/> behavior.</summary>
public class ObvTests
{
    private static readonly double[] Close = [100, 102, 101, 103, 105, 104, 106, 107, 106, 108];
    private static readonly double[] Volume = [1000, 1200, 800, 1500, 2000, 900, 1800, 2200, 700, 1600];

    /// <summary>Output length equals input length.</summary>
    [Fact]
    public void Compute_ReturnsFullLength()
    {
        double[] result = new Obv(Close, Volume).Compute();
        Assert.Equal(Close.Length, result.Length);
    }

    /// <summary>OBV starts at first volume value.</summary>
    [Fact]
    public void Compute_StartsWithFirstVolume()
    {
        double[] result = new Obv(Close, Volume).Compute();
        Assert.Equal(Volume[0], result[0]);
    }

    /// <summary>When close rises, volume is added.</summary>
    [Fact]
    public void Compute_AddsVolumeOnUp()
    {
        // Close[1]=102 > Close[0]=100 → OBV[1] = OBV[0] + Volume[1]
        double[] result = new Obv(Close, Volume).Compute();
        Assert.Equal(result[0] + Volume[1], result[1]);
    }

    /// <summary>When close falls, volume is subtracted.</summary>
    [Fact]
    public void Compute_SubtractsVolumeOnDown()
    {
        // Close[2]=101 < Close[1]=102 → OBV[2] = OBV[1] - Volume[2]
        double[] result = new Obv(Close, Volume).Compute();
        Assert.Equal(result[1] - Volume[2], result[2]);
    }
}
