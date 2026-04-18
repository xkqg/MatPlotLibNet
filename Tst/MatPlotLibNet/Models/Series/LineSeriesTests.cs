// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="LineSeries"/> default properties and construction.</summary>
public class LineSeriesTests
{
    /// <summary>Verifies that the constructor stores X and Y data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2, 3], y = [4, 5, 6];
        var series = new LineSeries(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    /// <summary>Verifies that LineStyle defaults to Solid.</summary>
    [Fact]
    public void DefaultLineStyle_IsSolid()
    {
        var series = new LineSeries([1.0], [2.0]);
        Assert.Equal(LineStyle.Solid, series.LineStyle);
    }

    /// <summary>Verifies that LineWidth defaults to 1.5.</summary>
    [Fact]
    public void DefaultLineWidth_Is1Point5()
    {
        var series = new LineSeries([1.0], [2.0]);
        Assert.Equal(1.5, series.LineWidth);
    }

    /// <summary>Verifies that Marker defaults to null.</summary>
    [Fact]
    public void DefaultMarker_IsNull()
    {
        var series = new LineSeries([1.0], [2.0]);
        Assert.Null(series.Marker);
    }

    /// <summary>Verifies that MarkerFaceColor defaults to null.</summary>
    [Fact]
    public void DefaultMarkerFaceColor_IsNull()
    {
        var series = new LineSeries([1.0], [2.0]);
        Assert.Null(series.MarkerFaceColor);
    }

    /// <summary>Verifies that MarkerEdgeColor defaults to null.</summary>
    [Fact]
    public void DefaultMarkerEdgeColor_IsNull()
    {
        var series = new LineSeries([1.0], [2.0]);
        Assert.Null(series.MarkerEdgeColor);
    }

    /// <summary>Verifies that MarkerEdgeWidth defaults to 1.0.</summary>
    [Fact]
    public void DefaultMarkerEdgeWidth_Is1()
    {
        var series = new LineSeries([1.0], [2.0]);
        Assert.Equal(1.0, series.MarkerEdgeWidth);
    }

    /// <summary>Verifies that DrawStyle defaults to null.</summary>
    [Fact]
    public void DefaultDrawStyle_IsNull()
    {
        var series = new LineSeries([1.0], [2.0]);
        Assert.Null(series.DrawStyle);
    }

    /// <summary>Verifies that MarkEvery defaults to null.</summary>
    [Fact]
    public void DefaultMarkEvery_IsNull()
    {
        var series = new LineSeries([1.0], [2.0]);
        Assert.Null(series.MarkEvery);
    }

    /// <summary>Verifies that MarkerFaceColor can be set.</summary>
    [Fact]
    public void MarkerFaceColor_CanBeSet()
    {
        var series = new LineSeries([1.0], [2.0]) { MarkerFaceColor = Color.FromHex("#FF0000") };
        Assert.NotNull(series.MarkerFaceColor);
    }

    /// <summary>Verifies that MarkerEdgeColor can be set.</summary>
    [Fact]
    public void MarkerEdgeColor_CanBeSet()
    {
        var series = new LineSeries([1.0], [2.0]) { MarkerEdgeColor = Color.FromHex("#00FF00") };
        Assert.NotNull(series.MarkerEdgeColor);
    }

    /// <summary>Verifies that DrawStyle can be set.</summary>
    [Fact]
    public void DrawStyle_CanBeSet()
    {
        var series = new LineSeries([1.0], [2.0]) { DrawStyle = DrawStyle.StepsPre };
        Assert.Equal(DrawStyle.StepsPre, series.DrawStyle);
    }

    /// <summary>Verifies that MarkEvery can be set.</summary>
    [Fact]
    public void MarkEvery_CanBeSet()
    {
        var series = new LineSeries([1.0], [2.0]) { MarkEvery = 3 };
        Assert.Equal(3, series.MarkEvery);
    }
}
