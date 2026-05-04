// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="HeatmapSeries"/> default properties and construction.</summary>
public class HeatmapSeriesTests
{
    /// <summary>Verifies that the constructor stores the 2D data array.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var data = new double[,] { { 1, 2 }, { 3, 4 } };
        var series = new HeatmapSeries(data);
        Assert.Equal(data, series.Data);
    }

    [Fact]
    public void ShowLabels_DefaultsFalse()
    {
        var series = new HeatmapSeries(new double[,] { { 0 } });
        Assert.False(series.ShowLabels);
    }

    [Fact]
    public void LabelFormat_DefaultsNull()
    {
        var series = new HeatmapSeries(new double[,] { { 0 } });
        Assert.Null(series.LabelFormat);
    }

    [Fact]
    public void MaskMode_DefaultsNone()
    {
        var series = new HeatmapSeries(new double[,] { { 0 } });
        Assert.Equal(HeatmapMaskMode.None, series.MaskMode);
    }

    [Fact]
    public void CellValueColor_DefaultsNull()
    {
        var series = new HeatmapSeries(new double[,] { { 0 } });
        Assert.Null(series.CellValueColor);
    }

    [Fact]
    public void GetColorBarRange_AllEqualData_Returns0To1()
    {
        var series = new HeatmapSeries(new double[,] { { 5, 5 }, { 5, 5 } });
        var range = series.GetColorBarRange();
        Assert.Equal(0, range.Min);
        Assert.Equal(1, range.Max);
    }

    [Fact]
    public void Setters_RoundTripValues()
    {
        var series = new HeatmapSeries(new double[,] { { 0 } })
        {
            ShowLabels = true,
            LabelFormat = "P1",
            MaskMode = HeatmapMaskMode.UpperTriangleStrict,
            CellValueColor = Colors.Red,
        };

        Assert.True(series.ShowLabels);
        Assert.Equal("P1", series.LabelFormat);
        Assert.Equal(HeatmapMaskMode.UpperTriangleStrict, series.MaskMode);
        Assert.Equal(Colors.Red, series.CellValueColor);
    }
}
