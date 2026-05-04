// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies SVG output of <see cref="SpectrogramSeries"/> rendering.</summary>
public class SpectrogramSeriesRenderTests
{
    // 512 samples at 1000 Hz — enough for STFT with default window 256
    private static readonly double[] Signal =
        Enumerable.Range(0, 512).Select(i => Math.Sin(2 * Math.PI * 50 * i / 1000.0)).ToArray();

    [Fact]
    public void Spectrogram_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Spectrogram(Signal, 1000, s => { s.WindowSize = 64; s.Overlap = 32; }))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Spectrogram_SvgContainsRectangles()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Spectrogram(Signal, 1000, s => { s.WindowSize = 64; s.Overlap = 32; }))
            .ToSvg();
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Spectrogram_EmptySignal_RendersWithoutError()
    {
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Spectrogram([], 1))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Spectrogram_FluentShortcut_ProducesSvg()
    {
        string svg = Plt.Create()
            .Spectrogram(Signal, 1000, s => { s.WindowSize = 64; s.Overlap = 32; })
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Spectrogram_ShortSignal_RendersWithoutError()
    {
        double[] shortSignal = [1.0, -1.0, 0.5, -0.5];
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Spectrogram(shortSignal, 1, s => { s.WindowSize = 4; s.Overlap = 2; }))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    // Branch coverage — exercise both halves of the (nBins==0 || nFrames==0) guard.
    // Fft.Stft returns an empty (0,0) matrix when WindowSize<=0, hitting nBins==0 first.

    [Fact]
    public void Spectrogram_ZeroWindowSize_EarlyReturn()
    {
        // Non-empty signal but WindowSize=0 → Fft.Stft returns (0,0) matrix → renderer's
        // nBins==0 guard returns. The Signal.Length==0 guard is bypassed.
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Spectrogram(Signal, 1000, s => { s.WindowSize = 0; }))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void Spectrogram_AllZeroSignal_DegenerateColorMappingGuard()
    {
        // Constant signal → STFT magnitudes all equal → ResolveColormapping (min == max)
        // → degenerate guard sets max = min + 1 so normalize stays finite.
        var flat = new double[256];
        string svg = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.Spectrogram(flat, 1000, s => { s.WindowSize = 64; s.Overlap = 32; }))
            .ToSvg();
        Assert.Contains("<svg", svg);
    }
}
