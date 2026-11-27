// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A spectrogram under the matplotlib themes is matplotlib's <c>specgram</c> (3.11.2, read with python): its cells hold the
/// decibels of the one-sided power density of each window (<c>mode='psd'</c>: |X|² / (Fs · Σw²), every row but 0 Hz and the Nyquist row
/// doubled; <c>scale='dB'</c>: <c>Z = 10. * np.log10(spec)</c>), drawn as an image (<c>imshow(Z, extent=(t[0] - pad, t[-1] + pad, freqs[0], freqs[-1]), origin='upper')</c>, pad = (NFFT - noverlap) / Fs / 2),
/// whose edges are sticky and which the style's <c>image.interpolation</c> scales, then <c>axis('auto')</c>, which autoscales the view
/// (rounded out to ticks under the classic style). The numbers below were printed by <c>mlab.specgram</c> for the signal of
/// <see cref="Signal"/>, NFFT 64, noverlap 32, Fs 1000. The library's own themes keep the magnitudes on a mesh over 0 to the duration.</summary>
public sealed class SpecgramTests
{
    private static readonly Rect Plot = new(80, 40, 440, 330);

    /// <summary>600 samples: 50 Hz, a quarter of 130 Hz, and a small saw of period 7.</summary>
    private static readonly double[] Signal = [.. Enumerable.Range(0, 600).Select(i =>
        Math.Sin(2 * Math.PI * 50 * i / 1000) + 0.25 * Math.Sin(2 * Math.PI * 130 * i / 1000) + 0.01 * (i % 7))];

    private static SpectrogramSeries Series() => new(Signal) { SampleRate = 1000, WindowSize = 64, Overlap = 32 };

    private static Axes AxesOf()
    {
        var axes = new Axes();
        var series = axes.Spectrogram(Signal, 1000);
        series.WindowSize = 64;
        series.Overlap = 32;
        return axes;
    }

    [Theory]
    [InlineData("classic")]
    [InlineData("v2")]
    public void UnderTheMatplotlibThemes_ACellHoldsTheDecibelsOfTheOneSidedPowerDensity_AsSpecgramComputesThem(string themeId)
    {
        var series = Series();

        var cells = series.CellsUnder(themeId.AsTheme(), series.Spectrum());

        Assert.Equal(33, cells.GetLength(0));
        Assert.Equal(17, cells.GetLength(1));
        // 10 log10 of mlab.specgram's spec[0, 0] = 3.203907885051263e-05 (0 Hz: not doubled), spec[1, 0] = 4.011528135364015e-05,
        // spec[3, 2] = 0.019950204208739903, spec[32, 5] = 9.166784962806827e-14 (the Nyquist row: not doubled), spec[16, 16] = 4.9783334571794856e-09.
        (-44.94319978726695).AssertCloseTo(cells[0, 0], relative: 1e-6, floor: 0);
        (-43.96690157471086).AssertCloseTo(cells[1, 0], relative: 1e-6, floor: 0);
        (-17.000526545499145).AssertCloseTo(cells[3, 2], relative: 1e-6, floor: 0);
        (-130.37782956340737).AssertCloseTo(cells[32, 5], relative: 1e-6, floor: 0);
        (-83.02916016975558).AssertCloseTo(cells[16, 16], relative: 1e-6, floor: 0);
    }

    [Fact]
    public void UnderTheMatplotlibThemes_TheColoursRunOverTheDecibels()
    {
        var range = Series().ColourRangeUnder(Theme.MatplotlibV2);

        // 10 log10 of spec.min() = 7.342366151520799e-14 and spec.max() = 0.01999961302593776.
        (-131.34163961752492).AssertCloseTo(range.Min, relative: 1e-6, floor: 0);
        (-16.989784074523072).AssertCloseTo(range.Max, relative: 1e-6, floor: 0);
    }

    [Fact]
    public void UnderTheLibrarysThemes_ACellHoldsTheMagnitudeOfItsBin()
    {
        var series = Series();
        var stft = series.Spectrum();

        Assert.Same(stft.Magnitudes, series.CellsUnder(Theme.Default, stft));
        Assert.Equal(series.GetColorBarRange(), series.ColourRangeUnder(Theme.Default));
    }

    /// <summary>The default style shows the image's extent: the centres of the first and last window less and plus half a step. The classic
    /// style rounds the view out to ticks, as an axis auto-rounds it.</summary>
    [Theory]
    [InlineData("v2", 0.016, 0.56)]
    [InlineData("classic", 0, 0.6)]
    public void TheAxesShowTheImagesExtent_OrTheViewRoundedOutToTicks(string theme, double xMin, double xMax)
    {
        var axes = AxesOf();

        var range = axes.ComputeDataRanges(theme.AsTheme());

        Assert.Equal(xMin, range.XMin, 12);
        Assert.Equal(xMax, range.XMax, 12);
        Assert.Equal(0, range.YMin, 12);
        Assert.Equal(500, range.YMax, 12);
    }

    /// <summary>A signal of no sample is padded to one window, as <c>specgram</c> pads it (python -I, matplotlib 3.11.2:
    /// <c>ax.specgram(np.array([]), Fs=1)</c> gives the image extent (64, 192, 0, 0.5) with 256-sample windows that overlap by 128, the
    /// view X (64, 192) under the default style and (60, 200) under classic, Y (0, 0.5) under both); a window of no sample adds nothing.</summary>
    [Fact]
    public void UnderTheMatplotlibThemes_ASignalOfNoSampleIsOneWindow_AndAWindowOfNoneAddsNothing()
    {
        IThemedRange empty = new SpectrogramSeries(Array.Empty<double>());
        IThemedRange noWindow = new SpectrogramSeries(Signal) { WindowSize = 0 };

        Assert.Equal(DataRangeContribution.Pinned(new Range1D(64, 192), new Range1D(0, 0.5)), empty.RangeUnder(null!, Theme.MatplotlibV2));
        Assert.Equal(default, noWindow.RangeUnder(null!, Theme.MatplotlibV2));
    }

    [Fact]
    public void TheLibrarysOwnTheme_RangesAsItAlwaysHas()
    {
        IThemedRange series = Series();

        Assert.Equal(series.ComputeDataRange(null!), series.RangeUnder(null!, Theme.Default));
        Assert.Equal(new DataRangeContribution(0, 0.6, 0, 500), series.ComputeDataRange(null!));
    }

    [Theory]
    [InlineData("v2", 33, 17)]          // 'auto' at 26 and 10 times the cells' size: nearest, a cell for each value
    [InlineData("classic", 132, 68)]    // 'bilinear': resampled to four times the values each way
    public void UnderTheMatplotlibThemes_TheCellsAreAnImageOnItsExtent_ScaledByTheStylesInterpolation(string themeId, int rows, int columns)
    {
        var (cells, transform) = Cells(themeId.AsTheme());

        Assert.Equal(rows * columns, cells.Count);
        var extent = Rect.FromCorners(transform.DataToPixel(0.016, 500), transform.DataToPixel(0.56, 0));
        var drawn = cells.Aggregate((a, b) => Rect.FromCorners(new Point(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)),
            new Point(Math.Max(a.Right, b.Right), Math.Max(a.Bottom, b.Bottom))));
        Assert.Equal(extent.X, drawn.X, 6);
        Assert.Equal(extent.Y, drawn.Y, 6);
        Assert.Equal(extent.Right, drawn.Right, 6);
        Assert.Equal(extent.Bottom, drawn.Bottom, 6);
    }

    [Fact]
    public void TheLibrarysOwnTheme_DrawsAMesh_ItsRowsCentredOnTheirBins_FromZeroToTheDuration()
    {
        var (cells, transform) = Cells(Theme.Default);

        Assert.Equal(33 * 17, cells.Count);
        // The first row of the first column: from 0 Hz to half a bin (7.8125 Hz), from 0 s to a seventeenth of 0.6 s.
        var first = Rect.FromCorners(transform.DataToPixel(0, 500.0 / 64), transform.DataToPixel(0.6 / 17, 0));
        Assert.Contains(cells, c => Math.Abs(c.X - first.X) < 1e-6 && Math.Abs(c.Bottom - first.Bottom) < 1e-6
            && Math.Abs(c.Right - first.Right) < 1e-6 && Math.Abs(c.Y - first.Y) < 1e-6);
    }

    /// <summary>The cells the spectrogram draws (the rectangles with no edge that are not the axes' background) and the transform the
    /// axes drew with.</summary>
    private static (List<Rect> Cells, DataTransform Transform) Cells(Theme theme)
    {
        var axes = AxesOf();
        var ctx = new RecordingRenderContext();
        var renderer = new CartesianAxesRenderer(axes, Plot, ctx, theme);
        renderer.Render();
        var cells = ctx.Of<DrawRectangleCall>()
            .Where(a => a.Fill != theme.AxesBackground && a.Stroke is null && a.StrokeThickness == 0)
            .Select(a => a.Rect).Where(r => r != Plot).ToList();
        return (cells, renderer.DrawnTransform!);
    }
}
