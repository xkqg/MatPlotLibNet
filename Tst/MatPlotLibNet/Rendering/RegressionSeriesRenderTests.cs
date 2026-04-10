// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="RegressionSeries"/> rendering.</summary>
public class RegressionSeriesRenderTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0, 5.0];
    private static readonly double[] Y = [2.1, 3.9, 6.2, 7.8, 10.1];

    /// <summary>RegressionSeries renders to SVG without throwing.</summary>
    [Fact]
    public void Regression_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Regression(X, Y))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>SVG contains a polyline element (the regression line).</summary>
    [Fact]
    public void Regression_SvgContainsPolyline()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Regression(X, Y))
            .ToSvg();
        Assert.Contains("<polyline", svg);
    }

    /// <summary>ShowConfidence=true produces a polygon (confidence band).</summary>
    [Fact]
    public void Regression_ShowConfidence_ContainsPolygon()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Regression(X, Y, s => s.ShowConfidence = true))
            .ToSvg();
        Assert.Contains("<polygon", svg);
    }

    /// <summary>ShowConfidence=false omits the confidence polygon.</summary>
    [Fact]
    public void Regression_NoConfidence_NoPolygon()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Regression(X, Y, s => s.ShowConfidence = false))
            .ToSvg();
        Assert.DoesNotContain("<polygon", svg);
    }

    /// <summary>Degree-2 polynomial fit renders without error.</summary>
    [Fact]
    public void Regression_Degree2_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Regression(X, Y, s => s.Degree = 2))
            .ToSvg();
        Assert.Contains("<polyline", svg);
    }

    /// <summary>Single data point renders without throwing (returns empty).</summary>
    [Fact]
    public void Regression_SinglePoint_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Regression([1.0], [1.0]))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
