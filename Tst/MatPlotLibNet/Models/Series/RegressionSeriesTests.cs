// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="RegressionSeries"/> behaviour; what it starts from and the settings it stores are held by <see cref="Serialization.RestingValueCorpusTests"/> and the drawn-property round trips.</summary>
public class RegressionSeriesTests
{
    /// <summary>Deliberately does NOT throw -- see <see cref="RegressionSeries"/>'s constructor doc comment.
    /// <c>RegressionSeriesRendererDiagnosticsTests.RegressionSeriesRenderer_SingularFit_EmitsDiagnostic</c> is the
    /// test that exercises what a mismatch actually does (reaches the renderer, which reports a diagnostic and
    /// degrades gracefully).</summary>
    [Fact]
    public void Constructor_MismatchedLengths_DoesNotThrow()
    {
        var series = new RegressionSeries([1.0, 2.0, 3.0], [1.0, 2.0]);
        Assert.Equal(3, series.XData.Length);
        Assert.Equal(2, series.YData.Length);
    }

    // RegressionSeries: `if (XData.Length == 0) return new(0, 1, 0, 1)`; ResidualSeries: `if (XData.Length == 0) return new(0, 1, -1, 1)`
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ARegressionOrResidualSeries_EmptyData_ReturnsDefaultRange(bool regression)
    {
        ISeries s = regression
            ? new RegressionSeries(Array.Empty<double>(), Array.Empty<double>())
            : new ResidualSeries(Array.Empty<double>(), Array.Empty<double>());
        var range = s.ComputeDataRange(new TestAxesContext());
        Assert.Equal(0.0, range.XMin);
        Assert.Equal(1.0, range.XMax);
    }
}
