// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Council fix — same root cause as B8 (<see cref="RegressionSeriesRendererDiagnosticsTests"/>),
/// uncaught variant: <c>ResidualSeriesRenderer.Render</c> and <c>ResidualSeries.ComputeDataRange</c>
/// both called <c>LeastSquares.PolyFit</c> directly with no try/catch — mismatched XData/YData lengths
/// used to crash the whole render (worse than B8's silent swallow, this one didn't swallow at all).
/// Both call sites now catch the specific exception types the fit can actually throw and report the
/// failure via <see cref="ChartDiagnostics"/> before degrading gracefully (no crash, no residual
/// contribution/render for that series).</summary>
[Collection("ChartDiagnosticsGlobalState")]
public class ResidualSeriesRendererDiagnosticsTests
{
    /// <summary>Mismatched X/Y array lengths make <c>LeastSquares.PolyFit</c> throw
    /// <see cref="IndexOutOfRangeException"/> mid-fit (identical-X data does NOT throw — same
    /// reachable failure mode verified for B8's RegressionSeriesRenderer). Building the full figure
    /// exercises both <c>ResidualSeries.ComputeDataRange</c> (axes-range pass) and
    /// <c>ResidualSeriesRenderer.Render</c> (render pass); this asserts the renderer's diagnostic is
    /// among those emitted and that the figure still renders (no crash).</summary>
    [Fact]
    public void ResidualSeriesRenderer_MismatchedData_EmitsDiagnosticAndSkips()
    {
        var received = new List<ChartDiagnostic>();
        void Handler(ChartDiagnostic d) => received.Add(d);
        ChartDiagnostics.Emitted += Handler;
        try
        {
            string svg = Plt.Create()
                .AddSubPlot(1, 1, 1, ax => ax.Residplot([1.0, 2.0], [1.0]))
                .ToSvg();

            Assert.Contains(received, d => d.Source == "ResidualSeriesRenderer" && d.Exception is not null);
            Assert.Contains("<svg", svg);
        }
        finally
        {
            ChartDiagnostics.Emitted -= Handler;
        }
    }

    /// <summary>Direct unit test of the model-level guard: <c>ResidualSeries.ComputeDataRange</c> must
    /// not crash on mismatched data — it contributes nothing to the axes range, mirroring the
    /// empty-series default (<c>new(0, 1, -1, 1)</c>, see <c>ResidualSeries.cs:38</c>), and reports why
    /// via <see cref="ChartDiagnostics"/>.</summary>
    [Fact]
    public void ResidualSeries_MismatchedData_RangeContributesNothing()
    {
        ChartDiagnostic? received = null;
        void Handler(ChartDiagnostic d) => received = d;
        ChartDiagnostics.Emitted += Handler;
        try
        {
            var series = new ResidualSeries(new double[] { 1.0, 2.0 }, new double[] { 1.0 });

            var range = series.ComputeDataRange(null!);

            Assert.Equal(new DataRangeContribution(0, 1, -1, 1), range);
            Assert.NotNull(received);
            Assert.Equal("ResidualSeries", received!.Value.Source);
            Assert.NotNull(received.Value.Exception);
        }
        finally
        {
            ChartDiagnostics.Emitted -= Handler;
        }
    }
}
