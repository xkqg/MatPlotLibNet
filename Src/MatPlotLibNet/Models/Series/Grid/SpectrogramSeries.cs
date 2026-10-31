// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a spectrogram series that displays the time-frequency content of a signal using Short-Time FFT.</summary>
public sealed class SpectrogramSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    /// <summary>The signal in the time domain, one sample per entry; the series draws its short-time spectrum.</summary>
    public Vec Signal { get; }

    /// <summary>Samples per second of the signal: it turns a sample index into a time and puts the top of the frequency
    /// axis at half this rate. Default 1.</summary>
    public int SampleRate { get; set; } = 1;

    /// <summary>The number of samples in each analysis window, any length (it is not rounded to a power of two): each window
    /// is weighted once by a Hann window and gives <c>WindowSize / 2 + 1</c> frequency rows, <c>SampleRate / WindowSize</c>
    /// Hz apart. Longer windows resolve frequency better and time worse. Default 256.</summary>
    public int WindowSize { get; set; } = 256;

    /// <summary>The number of samples that neighbouring windows share. Default 128.</summary>
    public int Overlap { get; set; } = 128;

    /// <inheritdoc />
    public IColorMap? ColorMap { get; set; }

    /// <inheritdoc />
    public INormalizer? Normalizer { get; set; }

    /// <summary>The short-time spectrum the cells are drawn from and the colour bar is read from: one place the signal's
    /// magnitudes are worked out, so the picture and its bar cannot disagree about them.</summary>
    internal StftResult Spectrum() => Fft.Stft(Signal, WindowSize, Overlap, SampleRate);

    /// <inheritdoc />
    /// <remarks>The range of the magnitudes the cells are coloured over, as for a heatmap of them (0 to 1 when there are none,
    /// or they are all equal). It used to be 0 to 1 whatever the signal, a scale no cell was drawn on.</remarks>
    public MinMaxRange GetColorBarRange() => Spectrum().Magnitudes.ScanColorBarRange();

    /// <summary>Initializes a new instance of <see cref="SpectrogramSeries"/> with the specified signal.</summary>
    /// <param name="signal">The input signal values.</param>
    public SpectrogramSeries(Vec signal)
    {
        Signal = signal;
    }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        if (Signal.Length == 0) return new(0, 1, 0, 1);
        double duration = (double)Signal.Length / SampleRate;
        double maxFreq = SampleRate / 2.0;
        return new(0, duration, 0, maxFreq);
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new()
    {
        Type = "spectrogram",
        Signal = Signal,
        SampleRate = SampleRate,
        WindowSize = WindowSize,
        Overlap = Overlap,
        ColorMapName = ColorMap.NameForJson(),
        ColorMap = ColorMap.DescriptionForJson(),
        Normalizer = this.DescribeNormalizer(),
    };

    /// <summary>Reconstructs a <see cref="SpectrogramSeries"/> from its serialization DTO and adds it to the axes.</summary>
    /// <param name="axes">The target axes the reconstructed series is added to.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <returns>The reconstructed series instance.</returns>
    internal static SpectrogramSeries FromSeriesDto(Axes axes, SeriesDto dto)
    {
        var s = axes.Spectrogram(dto.Signal ?? [], dto.SampleRate ?? 1);
        if (dto.WindowSize.HasValue)
        {
            s.WindowSize = dto.WindowSize.Value;
        }
        if (dto.Overlap.HasValue)
        {
            s.Overlap = dto.Overlap.Value;
        }
        s.RestoreColorMap(dto);
        s.RestoreNormalizer(dto);
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

        return new ChartDataTable(null, [new("time", Axis: DataAxis.X), new(Label ?? "amplitude")], rows);
    }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
