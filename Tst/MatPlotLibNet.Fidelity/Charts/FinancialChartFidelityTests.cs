// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Financial-family fidelity tests (OHLC bar variant).</summary>
public class FinancialChartFidelityTests : FidelityTest
{
    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 10, Ssim = 0.98, DeltaE = 2, V2Rms = 15, V2Ssim = 0.98, V2DeltaE = 2)]   // the lines are the reference's, on the pixel grid at 1 pt; the ticks are 6 px where the reference's are 0.2 of an X unit, and the text is antialiased
    public void OhlcBar_20Bars_MatchesMatplotlib(string themeId)
    {
        var bars = ReferenceData.Load("ohlc_bar");
        double[] open  = bars.Vector("open");
        double[] high  = bars.Vector("high");
        double[] low   = bars.Vector("low");
        double[] close = bars.Vector("close");
        AssertFidelity(themeId, "ohlc_bar", "OHLC bars — 20 bars", ax => ax
            .OhlcBar(open, high, low, close, null, s => s.LineWidth = 1.0 * RenderDpi / 72)   // the reference's linewidth=1, in points
            .SetXLabel("bar")
            .SetYLabel("price")
            // The reference's range is its lines' (x from -0.2 to 19.2, padded by the style: get_xlim() is -5 to 20 classic, -1.17 to 20.17
            // v2); the library's OHLC bars range over their slots (-0.5 to 19.5, no margin), so the check states the reference's.
            .SetXLim(themeId == "classic" ? -5 : -1.17, themeId == "classic" ? 20 : 20.17));
    }
}
