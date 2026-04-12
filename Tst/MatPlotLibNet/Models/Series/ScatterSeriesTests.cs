// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ScatterSeries"/> default properties and construction.</summary>
public class ScatterSeriesTests
{
    /// <summary>Verifies that the constructor stores X and Y data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2], y = [3, 4];
        var series = new ScatterSeries(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    /// <summary>Verifies that Marker defaults to Circle.</summary>
    [Fact]
    public void DefaultMarker_IsCircle()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Equal(MarkerStyle.Circle, series.Marker);
    }

    /// <summary>Verifies that Alpha defaults to 1.0.</summary>
    [Fact]
    public void DefaultAlpha_IsOne()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Equal(1.0, series.Alpha);
    }

    /// <summary>Verifies that Sizes defaults to null.</summary>
    [Fact]
    public void Sizes_DefaultNull()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Null(series.Sizes);
    }

    /// <summary>Verifies that EdgeColors defaults to null.</summary>
    [Fact]
    public void DefaultEdgeColors_IsNull()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Null(series.EdgeColors);
    }

    /// <summary>Verifies that LineWidths defaults to null.</summary>
    [Fact]
    public void DefaultLineWidths_IsNull()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Null(series.LineWidths);
    }

    /// <summary>Verifies that VMin defaults to null.</summary>
    [Fact]
    public void DefaultVMin_IsNull()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Null(series.VMin);
    }

    /// <summary>Verifies that VMax defaults to null.</summary>
    [Fact]
    public void DefaultVMax_IsNull()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Null(series.VMax);
    }

    /// <summary>Verifies that Normalizer defaults to null.</summary>
    [Fact]
    public void DefaultNormalizer_IsNull()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Null(series.Normalizer);
    }

    /// <summary>Verifies that ScatterSeries implements INormalizable.</summary>
    [Fact]
    public void ImplementsINormalizable()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.IsAssignableFrom<INormalizable>(series);
    }

    /// <summary>Verifies that EdgeColors can be assigned and retrieved.</summary>
    [Fact]
    public void EdgeColors_CanBeSet()
    {
        var colors = new[] { Color.FromHex("#FF0000"), Color.FromHex("#00FF00") };
        var series = new ScatterSeries([1.0, 2.0], [3.0, 4.0]) { EdgeColors = colors };
        Assert.Equal(colors, series.EdgeColors);
    }

    /// <summary>Verifies that VMin and VMax can be assigned and retrieved.</summary>
    [Fact]
    public void VMin_VMax_CanBeSet()
    {
        var series = new ScatterSeries([1.0], [2.0]) { VMin = 0.0, VMax = 100.0 };
        Assert.Equal(0.0, series.VMin);
        Assert.Equal(100.0, series.VMax);
    }

    [Fact]
    public void C_DefaultsToNull()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Null(series.C);
    }

    [Fact]
    public void C_CanBeSet()
    {
        var series = new ScatterSeries([1.0, 2.0], [3.0, 4.0]) { C = [0.0, 1.0] };
        Assert.Equal([0.0, 1.0], series.C);
    }
}
