// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Text3DSeries"/> default properties and construction.</summary>
public class Text3DSeriesTests
{
    private static readonly List<Text3DAnnotation> Annotations =
    [
        new(1, 2, 3, "A"),
        new(4, 5, 6, "B"),
        new(7, 8, 9, "C")
    ];

    private static readonly List<Text3DAnnotation> SingleAnnotation =
    [
        new(1, 2, 3, "X")
    ];

    /// <summary>Verifies that the constructor stores the annotations list.</summary>
    [Fact]
    public void Constructor_StoresAnnotations()
    {
        var series = new Text3DSeries(Annotations);
        Assert.Same(Annotations, series.Annotations);
    }

    /// <summary>Verifies that FontSize defaults to 10.</summary>
    [Fact]
    public void DefaultFontSize_Is10()
    {
        var series = new Text3DSeries(SingleAnnotation);
        Assert.Equal(10, series.FontSize);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new Text3DSeries(SingleAnnotation);
        Assert.Null(series.Color);
    }

    /// <summary>Verifies that ComputeDataRange is derived from annotation positions.</summary>
    [Fact]
    public void ComputeDataRange_FromAnnotationPositions()
    {
        var series = new Text3DSeries(Annotations);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(1, range.XMin);
        Assert.Equal(7, range.XMax);
        Assert.Equal(2, range.YMin);
        Assert.Equal(8, range.YMax);
        Assert.Equal(3, range.ZMin);
        Assert.Equal(9, range.ZMax);
    }

    /// <summary>Verifies that ToSeriesDto sets type to "text3d".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsText3D()
    {
        var series = new Text3DSeries(SingleAnnotation);
        var dto = series.ToSeriesDto();
        Assert.Equal("text3d", dto.Type);
    }
}
