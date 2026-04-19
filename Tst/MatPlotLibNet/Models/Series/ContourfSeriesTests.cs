// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ContourfSeries"/> default properties, interfaces, visitor dispatch, and DTO round-trip.</summary>
public class ContourfSeriesTests
{
    private static readonly double[] X = [1.0, 2.0];
    private static readonly double[] Y = [1.0, 2.0];
    private static readonly double[,] Z = { { 1, 2 }, { 3, 4 } };

    /// <summary>Constructor stores X, Y, Z data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new ContourfSeries(X, Y, Z);
        Assert.Equal(X, series.XData);
        Assert.Equal(Y, series.YData);
        Assert.Equal(Z, series.ZData);
    }

    /// <summary>Levels defaults to 10.</summary>
    [Fact]
    public void DefaultLevels_Is10()
    {
        var series = new ContourfSeries(X, Y, Z);
        Assert.Equal(10, series.Levels);
    }

    /// <summary>Alpha defaults to 1.0.</summary>
    [Fact]
    public void DefaultAlpha_Is1()
    {
        var series = new ContourfSeries(X, Y, Z);
        Assert.Equal(1.0, series.Alpha);
    }

    /// <summary>ShowLines defaults to true.</summary>
    [Fact]
    public void DefaultShowLines_IsTrue()
    {
        var series = new ContourfSeries(X, Y, Z);
        Assert.True(series.ShowLines);
    }

    /// <summary>LineWidth defaults to 0.5.</summary>
    [Fact]
    public void DefaultLineWidth_IsPoint5()
    {
        var series = new ContourfSeries(X, Y, Z);
        Assert.Equal(0.5, series.LineWidth);
    }

    /// <summary>Normalizer defaults to null.</summary>
    [Fact]
    public void DefaultNormalizer_IsNull()
    {
        var series = new ContourfSeries(X, Y, Z);
        Assert.Null(series.Normalizer);
    }

    /// <summary>ComputeDataRange returns the grid extent with sticky edges on all four sides
    /// so the 5 % axis margin doesn't push whitespace between the fill and the spines.</summary>
    [Fact]
    public void ComputeDataRange_ReturnsGridExtentWithStickyEdges()
    {
        var series = new ContourfSeries(X, Y, Z);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(X.Min(), range.XMin);
        Assert.Equal(X.Max(), range.XMax);
        Assert.Equal(Y.Min(), range.YMin);
        Assert.Equal(Y.Max(), range.YMax);
        Assert.Equal(X.Min(), range.StickyXMin);
        Assert.Equal(X.Max(), range.StickyXMax);
        Assert.Equal(Y.Min(), range.StickyYMin);
        Assert.Equal(Y.Max(), range.StickyYMax);
    }

    /// <summary>ToSeriesDto returns type "contourf" and contains X, Y, Z data.</summary>
    [Fact]
    public void ToSeriesDto_TypeIsContourf()
    {
        var series = new ContourfSeries(X, Y, Z);
        var dto = series.ToSeriesDto();
        Assert.Equal("contourf", dto.Type);
        Assert.Equal(X, dto.XData);
        Assert.Equal(Y, dto.YData);
        Assert.NotNull(dto.HeatmapData);
    }

    // ── Phase X.4 follow-up (v1.7.2, 2026-04-19) — coverage uplift ──

    /// <summary>GetColorBarRange traversal covers both `if (v &lt; min)` and
    /// `if (v &gt; max)` arms (lines 58-59) — varied Z forces both branches.</summary>
    [Fact]
    public void GetColorBarRange_VariedZ_ReturnsCorrectMinMax()
    {
        var z = new double[,] { { 1.0, 5.0, 3.0 }, { 7.0, 2.0, 9.0 } };
        var s = new ContourfSeries(X, Y, z);
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(1.0, min);
        Assert.Equal(9.0, max);
    }

    /// <summary>ComputeDataRange line 67: `XData.Length == 0 || YData.Length == 0`
    /// short-circuit returns null sentinel range. First arm: empty XData.</summary>
    [Fact]
    public void ComputeDataRange_EmptyXData_ReturnsNullBounds()
    {
        var s = new ContourfSeries(Array.Empty<double>(), Y, new double[0, 2]);
        var range = s.ComputeDataRange(null!);
        Assert.Null(range.XMin);
        Assert.Null(range.YMin);
    }

    /// <summary>Same shape, second arm: empty YData (XData populated).</summary>
    [Fact]
    public void ComputeDataRange_EmptyYData_ReturnsNullBounds()
    {
        var s = new ContourfSeries(X, Array.Empty<double>(), new double[2, 0]);
        var range = s.ComputeDataRange(null!);
        Assert.Null(range.XMin);
    }
}
