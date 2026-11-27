// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Ema"/> behavior.</summary>
public class EmaTests
{
    /// <summary>Verifies that the first EMA value equals the SMA seed value.</summary>
    [Fact]
    public void Compute_FirstValueIsSma()
    {
        double[] prices = [10, 20, 30, 40, 50];
        double[] result = new Ema(prices, 3).Compute();
        Assert.Equal(20, result[2]); // SMA of first 3: (10+20+30)/3
    }
}
