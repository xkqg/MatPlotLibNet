// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators.Streaming;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;
using MatPlotLibNet;
namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="MatPlotLibNet.Indicators.Vwap"/>: over bars with no volume yet the line stands at the price.</summary>
public class VwapTests
{

    // Vwap: `cumVol > 0 ? cumPriceVol / cumVol : Prices[i]` — Prices[i] arm at zero volume.
    [Fact] public void Vwap_ZeroVolume_FallsBackToPrice()
    {
        var v = new Vwap([10.0, 20.0, 30.0], [0.0, 0.0, 0.0]);
        var result = v.Compute();
        // With zero cumulative volume, fallback returns the price itself.
        Assert.Equal(10.0, result.Values[0]);
    }
}
