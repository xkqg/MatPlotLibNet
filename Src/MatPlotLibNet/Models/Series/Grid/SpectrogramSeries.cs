// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a spectrogram series that displays the time-frequency content of a signal using Short-Time FFT.</summary>
public sealed class SpectrogramSeries : ChartSeries, IColorBarDataProvider, IColorScaled, IColormappable, INormalizable, IThemedRange, ISeriesKind<SpectrogramSeries>
{
    /// <inheritdoc />
    /// <remarks>matplotlib's <c>specgram</c> asks neither cycle. 0: <c>specgram</c> draws its picture with <c>imshow</c>, an <c>AxesImage</c>.</remarks>
    internal override MatplotlibArtist Artist => MatplotlibArtist.Image;

    /// <summary>The signal in the time domain, one sample per entry; the series draws its short-time spectrum.</summary>
    public Vec Signal { get; }

    /// <summary>The rate a spectrogram reads its signal at when it is told none.</summary>
    internal const int RestingSampleRate = 1;

    /// <summary>Samples per second of the signal: it turns a sample index into a time and puts the top of the frequency
    /// axis at half this rate. Default <see cref="RestingSampleRate"/>.</summary>
    public int SampleRate { get; set => Change(ref field, value); } = RestingSampleRate;

    private const int RestingWindowSize = SpectralOptions.RestingSegmentLength;
    /// <summary>The number of samples in each analysis window, any length (it is not rounded to a power of two): each window
    /// is weighted once by a Hann window and gives <c>WindowSize / 2 + 1</c> frequency rows, <c>SampleRate / WindowSize</c>
    /// Hz apart. Longer windows resolve frequency better and time worse. Default <see cref="RestingWindowSize"/>.</summary>
    public int WindowSize { get; set => Change(ref field, value); } = RestingWindowSize;

    /// <summary>The number of samples that neighbouring windows share. Default 128.</summary>
    public int Overlap { get; set => Change(ref field, value); } = 128;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set => Change(ref field, value); }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set => Change(ref field, value); }

    /// <summary>The short-time spectrum the cells are drawn from and the colour bar is read from: one place the signal's
    /// magnitudes are worked out, so the picture and its bar cannot disagree about them.</summary>
    internal StftResult Spectrum() => Fft.Stft(Signal, WindowSize, Overlap, SampleRate);

    /// <summary>How the spectrum cuts the signal into windows (<see cref="StftFrames"/>), known without transforming it.</summary>
    internal StftFrames Frames => StftFrames.Of(Signal.Length, WindowSize, Overlap);

    /// <summary>What the cells hold under <paramref name="theme"/> (<see cref="SpectrogramLook.CellsOf"/>): the one-sided power density of
    /// each window under the matplotlib themes, as matplotlib's <c>specgram</c> draws it, the magnitude of each bin under the library's own.
    /// The picture and the colour bar both read it.</summary>
    /// <param name="theme">The theme the series is drawn under.</param>
    /// <param name="stft">The short-time spectrum (<see cref="Spectrum"/>), worked out once by the caller.</param>
    /// <returns>The values, [frequency bin, window].</returns>
    internal double[,] CellsUnder(Theme theme, StftResult stft) => theme.Looks.Spectrogram.CellsOf(stft, WindowSize, SampleRate);

    /// <inheritdoc />
    /// <remarks>The range of the magnitudes the cells are coloured over under the library's own themes, as for a heatmap of them (0 to 1
    /// when there are none, or they are all equal).</remarks>
    public MinMaxRange GetColorBarRange() => this.ColourRangeUnder(Theme.Default);

    /// <inheritdoc />
    /// <remarks>Over the cells as the theme of <paramref name="mapping"/> fills them (<see cref="CellsUnder"/>).</remarks>
    MinMaxRange IColorScaled.ColourRangeOver(ColorMapping mapping) => mapping.RangeOf(CellsUnder(mapping.Theme, Spectrum()));

    /// <summary>Initializes a new instance of <see cref="SpectrogramSeries"/> with the specified signal.</summary>
    /// <param name="signal">The input signal values.</param>
    public SpectrogramSeries(Vec signal)
    {
        Signal = signal;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Signal.Length == 0) return NoDataUnderTheLibrary;
        double duration = (double)Signal.Length / SampleRate;
        double maxFreq = SampleRate / 2.0;
        return new(0, duration, 0, maxFreq);
    }

    /// <inheritdoc />
    /// <remarks>Under a theme that draws a spectrogram as <c>specgram</c> does (<see cref="SpectrogramLook.LikeSpecgram"/>) the extent of its
    /// image, every edge sticky (<c>AxesImage.set_extent</c>), so no margin reaches past it; the view is then autoscaled as <c>axis('auto')
    /// </c> autoscales it, rounded out to ticks where the theme rounds. A signal shorter than its window, one with no sample included, is one
    /// window (<c>specgram</c> pads it: <c>specgram([])</c> stands from 32 to 96 seconds at 2 Hz with 256-sample windows that overlap by
    /// 128); a window of no sample adds nothing. Under any other theme <see cref="ComputeDataRange"/>.</remarks>
    DataRangeContribution IThemedRange.RangeUnder(IAxesContext context, Theme theme)
    {
        if (!theme.Looks.Spectrogram.LikeSpecgram) return ComputeDataRange(context);
        if (WindowSize <= 0) return default;
        var extent = SpectrogramLook.ExtentOf(Frames, SampleRate);
        return DataRangeContribution.Pinned(extent.Time, extent.Frequency);
    }

    static string ISeriesKind<SpectrogramSeries>.Kind => "spectrogram";

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = this.WireKind(),
        Signal = Signal,
        SampleRate = SampleRate,
        WindowSize = WindowSize,
        Overlap = Overlap,
        ColorScale = this.ColorScaleOnTheWire(),
    };

    /// <summary>The reader of a <see cref="SpectrogramSeries"/> that <see cref="SeriesRegistry"/> takes.</summary>
    internal static SpectrogramSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Spectrogram(dto.Signal ?? [], dto.SampleRate ?? RestingSampleRate);
        if (dto.WindowSize is { } windowSize) s.WindowSize = windowSize;
        if (dto.Overlap is { } overlap) s.Overlap = overlap;
        s.RestoreColorScale(dto);
        return s;
    }


    /// <inheritdoc />
    /// <remarks>The signal the spectrogram is computed from: the STFT is a view of it, and a reader asked for
    /// the data gets the samples, with the sample rate turning the index into a time.</remarks>
    public override ChartDataTable? ToDataTable()
    {
        var rows = new IReadOnlyList<DataCell>[Signal.Length];
        double step = SampleRate > 0 ? 1.0 / SampleRate : 1.0;
        for (int i = 0; i < Signal.Length; i++)
        {
            rows[i] = [DataCell.FromNumber(i * step), DataCell.FromNumber(Signal[i])];
        }

        return new ChartDataTable(null, [ChartDataColumn.Position(DataAxis.X, "time"), new(Label ?? "amplitude")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
