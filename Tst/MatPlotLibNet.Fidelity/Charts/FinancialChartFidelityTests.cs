// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Financial-family fidelity tests (OHLC bar variant).</summary>
public class FinancialChartFidelityTests : FidelityTest
{
    private static double NextGaussian(Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 70, Ssim = 0.50, DeltaE = 110)]   // thin red/green OHLC ticks fall below top-5 pixel count threshold (same pattern as core violin)
    public void OhlcBar_20Bars_MatchesMatplotlib()
    {
        var rng = new Random(42);
        int n = 20;
        double[] close = new double[n];
        double[] high  = new double[n];
        double[] low   = new double[n];
        double[] open  = new double[n];
        double price = 100;
        for (int i = 0; i < n; i++)
        {
            price += NextGaussian(rng);
            close[i] = price;
            high[i]  = price + rng.NextDouble() * 1.5 + 0.5;
            low[i]   = price - rng.NextDouble() * 1.5 - 0.5;
            open[i]  = price + NextGaussian(rng) * 0.5;
        }
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("OHLC bars — 20 bars")
                .OhlcBar(open, high, low, close)
                .SetXLabel("bar")
                .SetYLabel("price"))
            .Build();
        AssertFidelity(figure, "ohlc_bar");
    }
}
