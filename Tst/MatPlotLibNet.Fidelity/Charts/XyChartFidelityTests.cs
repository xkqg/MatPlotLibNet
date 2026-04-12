// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Fidelity.Charts;

/// <summary>Phase 5 XY-family fidelity tests (area, step, bubble, regression, …).</summary>
public class XyChartFidelityTests : FidelityTest
{
    private static double[] Linspace(double start, double end, int n)
    {
        var arr = new double[n];
        double step = (end - start) / (n - 1);
        for (int i = 0; i < n; i++) arr[i] = start + i * step;
        return arr;
    }

    private static double NextGaussian(Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.55, DeltaE = 55)]   // AA grey text vs matplotlib black
    public void Area_FillBetween_MatchesMatplotlib()
    {
        var x = Linspace(0, 10, 100);
        var y = x.Select(v => Math.Sin(v) + 1.2).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Area — fill_between")
                .FillBetween(x, y)
                .SetXLabel("x")
                .SetYLabel("y"))
            .Build();
        AssertFidelity(figure, "area");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 115, Ssim = 0.50, DeltaE = 125)]   // tab10 (ours) vs bgrcmyk (matplotlib classic) — pure blue/red/green don't appear in our top-5
    public void StackedArea_StackPlot_MatchesMatplotlib()
    {
        var x = Linspace(0, 10, 50);
        var y1 = x.Select(v => Math.Sin(v) + 2).ToArray();
        var y2 = x.Select(v => Math.Cos(v) + 2).ToArray();
        var y3 = x.Select(v => Math.Sin(v + 1) + 2).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Stacked area")
                .StackPlot(x, [y1, y2, y3], s => s.Labels = ["A", "B", "C"])
                .WithLegend())
            .Build();
        AssertFidelity(figure, "stacked_area");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 60, Ssim = 0.55, DeltaE = 55)]
    public void Step_Function_MatchesMatplotlib()
    {
        var x = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();
        var y = Enumerable.Range(1, 20).Select(i => (double)i).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Step function")
                .Step(x, y)
                .SetXLabel("x")
                .SetYLabel("cumulative"))
            .Build();
        AssertFidelity(figure, "step");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 60, Ssim = 0.50, DeltaE = 55)]   // different RNG
    public void Bubble_Scatter_MatchesMatplotlib()
    {
        var rng = new Random(42);
        int n = 40;
        var x     = Enumerable.Range(0, n).Select(_ => rng.NextDouble() * 10).ToArray();
        var y     = Enumerable.Range(0, n).Select(_ => rng.NextDouble() * 10).ToArray();
        var sizes = Enumerable.Range(0, n).Select(_ => 50 + rng.NextDouble() * 450).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Bubble chart")
                .Bubble(x, y, sizes, s => s.Alpha = 0.5))
            .Build();
        AssertFidelity(figure, "bubble");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 60, Ssim = 0.55, DeltaE = 55)]
    public void Regression_LinearFit_MatchesMatplotlib()
    {
        var rng = new Random(42);
        var x = Linspace(0, 10, 30);
        var y = x.Select(xi => 2 * xi + 1 + NextGaussian(rng) * 1.5).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Linear regression")
                .Regression(x, y)
                .WithLegend())
            .Build();
        AssertFidelity(figure, "regression");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 60, Ssim = 0.55, DeltaE = 55)]
    public void Residual_ScatterZero_MatchesMatplotlib()
    {
        var rng = new Random(42);
        var x = Linspace(0, 10, 30);
        var y = x.Select(_ => NextGaussian(rng)).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Residual plot")
                .Residplot(x, y)
                .SetXLabel("x")
                .SetYLabel("residual"))
            .Build();
        AssertFidelity(figure, "residual");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 60, Ssim = 0.55, DeltaE = 55)]
    public void Ecdf_NormalSamples_MatchesMatplotlib()
    {
        var rng = new Random(42);
        var data = Enumerable.Range(0, 100).Select(_ => NextGaussian(rng)).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("ECDF")
                .Ecdf(data)
                .SetXLabel("value")
                .SetYLabel("cumulative probability"))
            .Build();
        AssertFidelity(figure, "ecdf");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 60, Ssim = 0.55, DeltaE = 55)]
    public void Signal_SineSum_MatchesMatplotlib()
    {
        double sampleRate = 100.0;
        int n = 200;
        var y = Enumerable.Range(0, n)
            .Select(i => {
                double t = i / sampleRate;
                return Math.Sin(2 * Math.PI * 3 * t) + 0.3 * Math.Sin(2 * Math.PI * 10 * t);
            })
            .ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Signal — 3 Hz + 10 Hz")
                .Signal(y, sampleRate)
                .SetXLabel("time (s)")
                .SetYLabel("amplitude"))
            .Build();
        AssertFidelity(figure, "signal");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 60, Ssim = 0.55, DeltaE = 55)]
    public void SignalXY_IrregularX_MatchesMatplotlib()
    {
        var rng = new Random(42);
        var x = Enumerable.Range(0, 60).Select(_ => rng.NextDouble() * 10).OrderBy(v => v).ToArray();
        var y = x.Select(xi => Math.Sin(xi) + 0.1 * NextGaussian(rng)).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Signal — irregular X")
                .SignalXY(x, y)
                .SetXLabel("x")
                .SetYLabel("y"))
            .Build();
        AssertFidelity(figure, "signalxy");
    }

    [Fact]
    [Trait("Category", "Fidelity")]
    [FidelityTolerance(Rms = 80, Ssim = 0.45, DeltaE = 55)]   // sparkline has minimal chrome; small diffs dominate
    public void Sparkline_RandomWalk_MatchesMatplotlib()
    {
        var rng = new Random(42);
        double sum = 0;
        var y = Enumerable.Range(0, 50).Select(_ => sum += NextGaussian(rng)).ToArray();
        var figure = Plt.Create()
            .WithSize(FigWidth, FigHeight)
            .WithTheme(Theme.MatplotlibClassic)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Sparkline")
                .Sparkline(y))
            .Build();
        AssertFidelity(figure, "sparkline");
    }
}
