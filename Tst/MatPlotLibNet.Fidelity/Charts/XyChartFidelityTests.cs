// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Fidelity;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

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

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 12, Ssim = 0.98, DeltaE = 3, V2Rms = 11, V2DeltaE = 2)]   // AA grey text vs matplotlib black
    public void Area_FillBetween_MatchesMatplotlib(string themeId)
    {
        var x = Linspace(0, 10, 100);
        var y = x.Select(v => Math.Sin(v) + 1.2).ToArray();
        AssertFidelity(themeId, "area", "Area — fill_between", ax => ax
            .FillBetween(x, y)
            .SetXLabel("x")
            .SetYLabel("y"));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 12, Ssim = 0.98, DeltaE = 3, V2Rms = 11, V2Ssim = 0.98)]   // colour distance 0.37 classic, 0.63 v2
    public void StackedArea_StackPlot_MatchesMatplotlib(string themeId)
    {
        var x = Linspace(0, 10, 50);
        var y1 = x.Select(v => Math.Sin(v) + 2).ToArray();
        var y2 = x.Select(v => Math.Cos(v) + 2).ToArray();
        var y3 = x.Select(v => Math.Sin(v + 1) + 2).ToArray();
        AssertFidelity(themeId, "stacked_area", "Stacked area", ax => ax
            .StackPlot(x, [y1, y2, y3], s => s.Labels = ["A", "B", "C"])
            .WithLegend(LegendPosition.UpperRight));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 11, Ssim = 0.98, DeltaE = 3, V2Rms = 15, V2Ssim = 0.97, V2DeltaE = 2)]   // the runs, the range and the ticks are the reference's; the verticals stand a pixel off (matplotlib snaps a line of horizontal and vertical segments to the pixel grid) and the text is antialiased
    public void Step_Function_MatchesMatplotlib(string themeId)
    {
        var x = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();
        var y = Enumerable.Range(1, 20).Select(i => (double)i).ToArray();
        AssertFidelity(themeId, "step", "Step function", ax => ax
            .Step(x, y, s => s.StepPosition = StepPosition.Mid)   // the reference is ax.step(x, y, where="mid")
            .SetXLabel("x")
            .SetYLabel("cumulative"));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 10, Ssim = 0.98, DeltaE = 3, V2Rms = 9)]
    public void Bubble_Scatter_MatchesMatplotlib(string themeId)
    {
        var points = ReferenceData.Load("bubble");
        var x     = points.Vector("x");
        var y     = points.Vector("y");
        var sizes = points.Vector("sizes");
        AssertFidelity(themeId, "bubble", "Bubble chart", ax => ax
            .Bubble(x, y, sizes, s => s.Alpha = 0.5));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 15, Ssim = 0.98, DeltaE = 3, V2Rms = 11, V2Ssim = 0.98)]
    public void Regression_LinearFit_MatchesMatplotlib(string themeId)
    {
        var points = ReferenceData.Load("regression");
        var x = points.Vector("x");
        var y = points.Vector("y");
        var theme = themeId.AsTheme();
        // The reference draws the data as a scatter and the fit as a line, both in the first colour of the cycle (matplotlib keeps one
        // cycle for scatter and another for plot, so the library's one cycle needs the colour said), with a legend of both.
        var first = theme.CycleColors[0];
        var figure = FidelityFigure(theme)
            .AddSubPlot(1, 1, 1, ax => ax
                .WithTitle("Linear regression")
                .Scatter(x, y, s => { s.Label = "data"; s.Color = first; })
                .Regression(x, y, r => { r.Label = "linear fit"; r.Color = first; })
                .WithLegend())
            .Build();
        AssertFidelity(figure, "regression");
    }

    [Theory]
    [FidelityRows]
    // Against seaborn's residplot: the dots 0.8 opaque and edged in their colour, the ':' zero line in '.2', ranged as its scatter;
    // antialiasing and the text remain. Windows 9.12 / 0.9940 classic, 9.85 / 0.9930 v2; Linux 8.58 / 0.9939, 9.16 / 0.9930.
    [FidelityTolerance(Rms = 11, Ssim = 0.98, DeltaE = 3, V2Rms = 11, V2Ssim = 0.98, V2DeltaE = 2)]
    public void Residual_ScatterZero_MatchesMatplotlib(string themeId)
    {
        var points = ReferenceData.Load("residual");
        var x = points.Vector("x");
        var y = points.Vector("residuals");
        AssertFidelity(themeId, "residual", "Residual plot", ax => ax
            .Residplot(x, y)
            .SetXLabel("x")
            .SetYLabel("residual"));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 14, Ssim = 0.97, DeltaE = 3, V2Rms = 17, V2Ssim = 0.96, V2DeltaE = 2)]
    public void Ecdf_NormalSamples_MatchesMatplotlib(string themeId)
    {
        var data = ReferenceData.Load("ecdf").Vector("data");
        AssertFidelity(themeId, "ecdf", "ECDF", ax => ax
            .Ecdf(data)
            .SetXLabel("value")
            .SetYLabel("cumulative probability"));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 12, Ssim = 0.97, DeltaE = 3, V2Rms = 15, V2DeltaE = 5)]
    public void Signal_SineSum_MatchesMatplotlib(string themeId)
    {
        double sampleRate = 100.0;
        int n = 200;
        var y = Enumerable.Range(0, n)
            .Select(i => {
                double t = i / sampleRate;
                return Math.Sin(2 * Math.PI * 3 * t) + 0.3 * Math.Sin(2 * Math.PI * 10 * t);
            })
            .ToArray();
        AssertFidelity(themeId, "signal", "Signal — 3 Hz + 10 Hz", ax => ax
            .Signal(y, sampleRate)
            .SetXLabel("time (s)")
            .SetYLabel("amplitude"));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 10, Ssim = 0.98, DeltaE = 3, V2Rms = 11, V2DeltaE = 2)]
    public void SignalXY_IrregularX_MatchesMatplotlib(string themeId)
    {
        var points = ReferenceData.Load("signalxy");
        var x = points.Vector("x");
        var y = points.Vector("y");
        AssertFidelity(themeId, "signalxy", "Signal — irregular X", ax => ax
            .SignalXY(x, y)
            .SetXLabel("x")
            .SetYLabel("y"));
    }

    [Theory]
    [FidelityRows]
    [FidelityTolerance(Rms = 5, Ssim = 0.98, DeltaE = 3, V2DeltaE = 2)]   // the reference is a sparkline (axis off, the data's extent): v2 3.68 / 0.9994 / 0.00, classic 3.80 / 0.9995 / 0.54 on Windows
    public void Sparkline_RandomWalk_MatchesMatplotlib(string themeId)
    {
        var y = ReferenceData.Load("sparkline").Vector("y");
        AssertFidelity(themeId, "sparkline", "Sparkline", ax => ax
            .Sparkline(y));
    }
}
