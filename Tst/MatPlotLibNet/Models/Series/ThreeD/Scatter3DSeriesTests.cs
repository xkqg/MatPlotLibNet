// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Scatter3DSeries"/> default properties and construction.</summary>
public class Scatter3DSeriesTests
{
    private static readonly double[] X = [1, 2, 3];
    private static readonly double[] Y = [4, 5, 6];
    private static readonly double[] Z = [7, 8, 9];
    private static readonly double[] Single = [1.0];

    /// <summary>Verifies that the constructor stores X, Y, and Z data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new Scatter3DSeries(X, Y, Z);
        Assert.Equal(X, (double[])series.X);
        Assert.Equal(Y, (double[])series.Y);
        Assert.Equal(Z, (double[])series.Z);
    }

    /// <summary>Verifies that MarkerSize defaults to 6.</summary>
    [Fact]
    public void DefaultMarkerSize_Is6()
    {
        var series = new Scatter3DSeries(Single, Single, Single);
        Assert.Equal(6, series.MarkerSize);
    }

    /// <summary>Verifies that ColorMap can be assigned.</summary>
    [Fact]
    public void ColorMap_CanBeSet()
    {
        var series = new Scatter3DSeries(Single, Single, Single);
        series.ColorMap = ColorMaps.Viridis;
        Assert.NotNull(series.ColorMap);
        Assert.Equal("viridis", series.ColorMap.Name);
    }

    /// <summary>Verifies that Normalizer defaults to null.</summary>
    [Fact]
    public void DefaultNormalizer_IsNull()
    {
        var series = new Scatter3DSeries(Single, Single, Single);
        Assert.Null(series.Normalizer);
    }

    /// <summary>Verifies that ToSeriesDto includes ColorMapName when set.</summary>
    [Fact]
    public void ToSeriesDto_IncludesColorMapName_WhenSet()
    {
        var series = new Scatter3DSeries(X, Y, Z) { ColorMap = ColorMaps.Viridis };
        var dto = series.ToSeriesDto();
        Assert.Equal("viridis", dto.ColorMapName);
    }

    /// <summary>Verifies that ToSeriesDto has null ColorMapName when no colormap is set.</summary>
    [Fact]
    public void ToSeriesDto_ColorMapNameNull_WhenNotSet()
    {
        var series = new Scatter3DSeries(X, Y, Z);
        var dto = series.ToSeriesDto();
        Assert.Null(dto.ColorMapName);
    }
}
