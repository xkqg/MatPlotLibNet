// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Categorical-family fidelity tests (broken_bar, eventplot, gantt, waterfall).</summary>
public class CategoricalChartFidelityTests : FidelityTest
{
    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 115, Ssim = 0.50, DeltaE = 55)]   // v1.1.4: AA grey text + tab10/bgrcmyk; classic xmargin=0 made bars extend edge-to-edge → RMS 100→115
    public void BrokenBar_TwoRows_MatchesMatplotlib(string themeId)
    {
        BarRange[][] ranges =
        [
            [new(0, 3), new(5, 2), new(8, 4)],
            [new(1, 2), new(6, 3)],
        ];
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Broken bar — broken_barh")
                .BrokenBarH(ranges)
                .SetXLabel("time"))
            .Build();
        AssertFidelity(figure, "broken_bar");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 70, Ssim = 0.45, DeltaE = 55)]   // event ticks are thin; mostly background → low SSIM
    public void Eventplot_FourRows_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        double[][] positions = new double[4][];
        for (int i = 0; i < 4; i++)
            positions[i] = Enumerable.Range(0, 30).Select(_ => rng.NextDouble() * 10).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Eventplot — 4 rows")
                .Eventplot(positions))
            .Build();
        AssertFidelity(figure, "eventplot");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 90, Ssim = 0.50, DeltaE = 75)]   // tab10 cycle vs bgrcmyk blue task bars
    public void Gantt_FourTasks_MatchesMatplotlib(string themeId)
    {
        string[] tasks = ["Design", "Build", "Test", "Ship"];
        double[] starts = [0, 3, 8, 11];
        double[] ends   = [3, 8, 11, 13];
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Gantt chart")
                .Gantt(tasks, starts, ends)
                .SetXLabel("day"))
            .Build();
        AssertFidelity(figure, "gantt");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 100, Ssim = 0.45, DeltaE = 140)]   // v1.1.4: matplotlib classic C2/C3 (red/cyan) vs our green/red waterfall convention; classic xmargin=0 → RMS 90→100
    public void Waterfall_Cumulative_MatchesMatplotlib(string themeId)
    {
        string[] labels = ["Start", "A", "B", "C", "D", "End"];
        double[] values = [100, 30, -20, 40, -10, 0];
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Waterfall chart")
                .Waterfall(labels, values))
            .Build();
        AssertFidelity(figure, "waterfall");
    }
}
