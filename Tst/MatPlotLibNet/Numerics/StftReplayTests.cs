// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>Replays the spectrogram defects as measured. <see cref="Fft.Stft"/> weighted each frame by its Hann window and
/// then handed it to <see cref="Fft.Forward"/>, which weights it again, so a spectrogram showed a Hann-squared spectrum (a
/// frame of 8 ones gave 2.625 at bin 0, the sum of the squared weights, where matplotlib's specgram gives 3.5, the sum of
/// the weights). <see cref="Fft.Forward"/> also padded a frame of 100 samples to 128 while the bins were labelled as if they
/// were 100 apart, and the renderer spread its rows over 0 to half the sample rate whatever the bins said: a 125 Hz tone
/// with a window of 100 peaked in row 16, labelled 160 Hz and drawn at 156.9 to 166.7 Hz.</summary>
public class StftReplayTests
{
    private const int SampleRate = 1000;

    /// <summary>A 125 Hz sine sampled at 1000 Hz, exactly one window of 100 samples long: one frame, 51 bins 10 Hz apart.</summary>
    private static double[] Tone125Hz() =>
        [.. Enumerable.Range(0, 100).Select(i => Math.Sin(2 * Math.PI * 125 * i / SampleRate))];

    private static int PeakBin(StftResult stft)
    {
        int peak = 0;
        for (int b = 1; b < stft.Magnitudes.GetLength(0); b++)
            if (stft.Magnitudes[b, 0] > stft.Magnitudes[peak, 0]) peak = b;
        return peak;
    }

    [Fact]
    public void ConstantOnes_Win8_Bin0_Is3_5()
    {
        // np.hanning(8) sums to 3.5; weighted twice the frame sums to 2.625 (the sum of the squared weights).
        var stft = Fft.Stft([1, 1, 1, 1, 1, 1, 1, 1], windowSize: 8, overlap: 0);

        Assert.Equal(3.5, stft.Magnitudes[0, 0], 1e-9);
    }

    [Fact]
    public void Tone125Hz_Win100_PeakRowLabelledWithinOneBin()
    {
        var stft = Fft.Stft(Tone125Hz(), windowSize: 100, overlap: 0, sampleRate: SampleRate);

        Assert.Equal(51, stft.Frequencies.Length);
        Assert.Equal(10.0, stft.Frequencies[1], 1e-9);
        Assert.Equal(500.0, stft.Frequencies[^1], 1e-9);
        double label = stft.Frequencies[PeakBin(stft)];
        Assert.True(Math.Abs(label - 125) <= 10, $"the peak row is labelled {label} Hz");
    }

    /// <summary>Renders the one-frame spectrogram of the tone on a plot from 0 to 500 Hz, 500 px high, so a frequency f lands
    /// at 500 - f px; the cells are drawn row by row from bin 0 up.</summary>
    private static List<Rect> RenderedRows(double[] signal, int windowSize)
    {
        var bounds = new Rect(0, 0, 100, 500);
        var ctx = new RecordingRenderContext();
        var context = new SeriesRenderContext(
            new DataTransform(0, signal.Length / (double)SampleRate, 0, SampleRate / 2.0, bounds),
            ctx, Colors.Tab10Blue, new RenderArea(bounds, ctx));
        new SpectrogramSeriesRenderer(context).Render(
            new SpectrogramSeries(signal) { SampleRate = SampleRate, WindowSize = windowSize, Overlap = 0 });
        return [.. ctx.OfKind("DrawRectangle").Select(c => (Rect)c.Args!.GetType().GetProperty("rect")!.GetValue(c.Args)!)];
    }

    [Fact]
    public void SpectrogramRenderer_PeakRowSpans125Hz()
    {
        double[] signal = Tone125Hz();
        int peak = PeakBin(Fft.Stft(signal, 100, 0, SampleRate));

        var rows = RenderedRows(signal, 100);

        Assert.Equal(51, rows.Count);
        double low = 500 - rows[peak].Bottom, high = 500 - rows[peak].Y;
        Assert.True(Math.Abs((low + high) / 2 - 125) <= 10, $"the peak row is drawn at {low} to {high} Hz");
    }

    /// <summary>Row b runs halfway to its neighbours, so it is centred on its bin's frequency; the first and the last row stop
    /// at the axis's ends, 0 and half the sample rate. An even window ends on a bin at 500 Hz, an odd one half a bin below.</summary>
    [Theory]
    [InlineData(100)]
    [InlineData(99)]
    public void SpectrogramRows_AreCentredOnTheirBins_AndTileTheFrequencyAxis(int windowSize)
    {
        double[] signal = Tone125Hz()[..windowSize];
        var frequencies = Fft.Stft(signal, windowSize, 0, SampleRate).Frequencies;

        var rows = RenderedRows(signal, windowSize);

        Assert.Equal(frequencies.Length, rows.Count);
        Assert.Equal(0.0, 500 - rows[0].Bottom, 1e-9);
        Assert.Equal(500.0, 500 - rows[^1].Y, 1e-9);
        for (int b = 1; b < rows.Count - 1; b++)
        {
            Assert.Equal(frequencies[b], 500 - (rows[b].Y + rows[b].Bottom) / 2, 1e-9);
            Assert.Equal(rows[b - 1].Y, rows[b].Bottom, 1e-9);
        }
    }
}
