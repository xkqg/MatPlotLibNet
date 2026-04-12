// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Categorical-family fidelity tests (broken_bar, eventplot, gantt, waterfall).</summary>
public class CategoricalChartFidelityTests : FidelityTest
{
    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 100, Ssim = 0.50, DeltaE = 55)]   // AA grey text; tab10 cycle vs bgrcmyk
    public void BrokenBar_TwoRows_MatchesMatplotlib()
    {
        (double Start, double Width)[][] ranges =
        [
            [(0, 3), (5, 2), (8, 4)],
            [(1, 2), (6, 3)],
        ];
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Broken bar — broken_barh")
                .BrokenBarH(ranges)
                .SetXLabel("time"))
            .Build();
        AssertFidelity(figure, "broken_bar");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 70, Ssim = 0.45, DeltaE = 55)]   // event ticks are thin; mostly background → low SSIM
    public void Eventplot_FourRows_MatchesMatplotlib()
    {
        var rng = new Random(42);
        double[][] positions = new double[4][];
        for (int i = 0; i < 4; i++)
            positions[i] = Enumerable.Range(0, 30).Select(_ => rng.NextDouble() * 10).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Eventplot — 4 rows")
                .Eventplot(positions))
            .Build();
        AssertFidelity(figure, "eventplot");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 90, Ssim = 0.50, DeltaE = 75)]   // tab10 cycle vs bgrcmyk blue task bars
    public void Gantt_FourTasks_MatchesMatplotlib()
    {
        string[] tasks = ["Design", "Build", "Test", "Ship"];
        double[] starts = [0, 3, 8, 11];
        double[] ends   = [3, 8, 11, 13];
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Gantt chart")
                .Gantt(tasks, starts, ends)
                .SetXLabel("day"))
            .Build();
        AssertFidelity(figure, "gantt");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 90, Ssim = 0.45, DeltaE = 140)]   // matplotlib classic C2/C3 (red/cyan) vs our green/red waterfall convention
    public void Waterfall_Cumulative_MatchesMatplotlib()
    {
        string[] labels = ["Start", "A", "B", "C", "D", "End"];
        double[] values = [100, 30, -20, 40, -10, 0];
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Waterfall chart")
                .Waterfall(labels, values))
            .Build();
        AssertFidelity(figure, "waterfall");
    }
}
