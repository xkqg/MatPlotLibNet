// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="EquityCurve"/> behavior.</summary>
public class EquityCurveTests
{
    /// <summary>Verifies that Compute returns the correct cumulative equity values from initial capital.</summary>
    [Fact]
    public void Compute_CumulativeSum()
    {
        double[] equity = new EquityCurve([100, -30, 50, -10], 10000).Compute();
        Assert.Equal(10000, equity[0]);
        Assert.Equal(10100, equity[1]);
        Assert.Equal(10070, equity[2]);
        Assert.Equal(10120, equity[3]);
        Assert.Equal(10110, equity[4]);
    }
}
