// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Atr"/> behavior.</summary>
public class AtrTests
{
    private static readonly double[] High = [48.70, 48.72, 48.90, 48.87, 48.82, 49.05, 49.20, 49.35, 49.92, 50.19, 50.12, 49.66, 49.88, 50.19, 50.36];
    private static readonly double[] Low = [47.79, 48.14, 48.39, 48.37, 48.24, 48.64, 48.94, 48.86, 49.50, 49.87, 49.20, 48.90, 49.43, 49.73, 49.26];
    private static readonly double[] Close = [48.16, 48.61, 48.75, 48.63, 48.74, 49.03, 49.07, 49.32, 49.91, 50.13, 49.53, 49.50, 49.75, 50.03, 50.31];

    /// <summary>Verifies that all ATR values are positive.</summary>
    [Fact]
    public void Compute_ValuesArePositive()
    {
        double[] atr = new Atr(OhlcvBars.Hlc(High, Low, Close), 5).Compute();
        foreach (var v in atr) Assert.True(v > 0);
    }
}
