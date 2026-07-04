// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Diagnostics;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Council fix B8: a failed least-squares fit inside <c>RegressionSeriesRenderer</c> used
/// to be swallowed by a bare <c>catch { }</c> — the chart silently rendered without the regression
/// line and the caller had no way to know why. The renderer now catches only the specific exception
/// types the fit can actually throw and reports the failure via <see cref="ChartDiagnostics"/>
/// before degrading gracefully (still no crash, still no regression line for that series).</summary>
[Collection("ChartDiagnosticsGlobalState")]
public class RegressionSeriesRendererDiagnosticsTests
{
    /// <summary>Mismatched X/Y array lengths make <c>LeastSquares.PolyFit</c> throw
    /// <see cref="IndexOutOfRangeException"/> mid-fit (verified directly against
    /// <c>LeastSquares.PolyFit</c>: identical X values do NOT throw — the normal-equations solver
    /// degrades to a degenerate-but-valid fit for singular matrices — so a length mismatch is the
    /// genuine reachable "failed fit" path from this renderer's call site). The renderer must catch
    /// it, emit a diagnostic naming the failure, and still render the rest of the figure.</summary>
    [Fact]
    public void RegressionSeriesRenderer_SingularFit_EmitsDiagnostic()
    {
        ChartDiagnostic? received = null;
        void Handler(ChartDiagnostic d) => received = d;
        ChartDiagnostics.Emitted += Handler;
        try
        {
            string svg = Plt.Create()
                .AddSubPlot(1, 1, 1, ax => ax.Regression([1.0, 2.0], [1.0]))
                .ToSvg();

            Assert.NotNull(received);
            Assert.Equal("RegressionSeriesRenderer", received!.Value.Source);
            Assert.NotNull(received.Value.Exception);
            Assert.Contains("<svg", svg);
        }
        finally
        {
            ChartDiagnostics.Emitted -= Handler;
        }
    }
}
