// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 Distribution-family fidelity tests — seaborn equivalents.</summary>
public class DistributionChartFidelityTests : FidelityTest
{
    private static double NextGaussian(Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 110, Ssim = 0.55, DeltaE = 140)]   // v1.1.4: seaborn fill alpha + different KDE bandwidth heuristic; classic axes.xmargin=0 shifted histogram bar edges → top-5 color blend differs
    public void Kde_NormalSamples_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        var data = Enumerable.Range(0, 500).Select(_ => NextGaussian(rng)).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("KDE — 500 normal samples")
                .Kde(data))
            .Build();
        AssertFidelity(figure, "kde");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.50, DeltaE = 55)]
    public void Rugplot_NormalSamples_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        var data = Enumerable.Range(0, 100).Select(_ => NextGaussian(rng)).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Rugplot")
                .Rugplot(data))
            .Build();
        AssertFidelity(figure, "rugplot");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.50, DeltaE = 140)]   // pt→px font fix shifted axis labels → top-5 color cluster changed
    public void Stripplot_ThreeGroups_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        double[][] data = new[] { 0.0, 1.0, 2.0 }
            .Select(loc => Enumerable.Range(0, 30)
                .Select(_ => loc + NextGaussian(rng) * 0.5).ToArray())
            .ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Stripplot — 3 groups")
                .Stripplot(data))
            .Build();
        AssertFidelity(figure, "stripplot");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 90, Ssim = 0.45, DeltaE = 140)]   // swarm layout differs slightly + pt→px font fix shifted axis labels
    public void Swarmplot_ThreeGroups_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        double[][] data = new[] { 0.0, 1.0, 2.0 }
            .Select(loc => Enumerable.Range(0, 30)
                .Select(_ => loc + NextGaussian(rng) * 0.5).ToArray())
            .ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Swarmplot — 3 groups")
                .Swarmplot(data))
            .Build();
        AssertFidelity(figure, "swarmplot");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 70, Ssim = 0.55, DeltaE = 55)]
    public void Pointplot_FourGroups_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        double[][] data = new[] { 0.0, 1.0, 2.0, 3.0 }
            .Select(loc => Enumerable.Range(0, 50)
                .Select(_ => loc + NextGaussian(rng)).ToArray())
            .ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Pointplot — 4 groups")
                .Pointplot(data))
            .Build();
        AssertFidelity(figure, "pointplot");
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 95, Ssim = 0.55, DeltaE = 55)]   // category frequency differs by RNG; AA grey text
    public void Countplot_FourCategories_MatchesMatplotlib(string themeId)
    {
        var rng = new Random(42);
        string[] pool = ["A", "B", "C", "D"];
        double[] cdf = [0.4, 0.7, 0.9, 1.0];
        var values = Enumerable.Range(0, 200).Select(_ =>
        {
            double r = rng.NextDouble();
            for (int i = 0; i < cdf.Length; i++)
                if (r < cdf[i]) return pool[i];
            return pool[^1];
        }).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(ResolveTheme(themeId))
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Countplot")
                .Countplot(values))
            .Build();
        AssertFidelity(figure, "countplot");
    }
}
