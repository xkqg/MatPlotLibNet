// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="SpectrogramSeries"/> default properties, construction, and serialization.</summary>
public class SpectrogramSeriesTests
{
    private static readonly double[] Signal = [1.0, 0.5, -0.5, -1.0, 0.0, 0.5, 1.0, 0.5];

    [Fact]
    public void Constructor_StoresSignal()
    {
        var series = new SpectrogramSeries(Signal);
        Assert.Equal(Signal, (double[])series.Signal);
    }

    [Fact]
    public void SampleRate_DefaultsTo1()
    {
        var series = new SpectrogramSeries(Signal);
        Assert.Equal(1, series.SampleRate);
    }

    [Fact]
    public void WindowSize_DefaultsTo256()
    {
        var series = new SpectrogramSeries(Signal);
        Assert.Equal(256, series.WindowSize);
    }

    [Fact]
    public void Overlap_DefaultsTo128()
    {
        var series = new SpectrogramSeries(Signal);
        Assert.Equal(128, series.Overlap);
    }

    [Fact]
    public void ToSeriesDto_ReturnsTypeSpectrogram()
    {
        var series = new SpectrogramSeries(Signal);
        Assert.Equal("spectrogram", series.ToSeriesDto().Type);
    }

}
