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
    public Vec Signal { get; }

    public int SampleRate { get; set; } = 1;

    public int WindowSize { get; set; } = 256;

    public int Overlap { get; set; } = 128;

    public IColorMap? ColorMap { get; set; }

    public INormalizer? Normalizer { get; set; }

    /// <inheritdoc />
    public MinMaxRange GetColorBarRange() => new(0, 1);

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
        ColorMapName = ColorMap?.Name
    };

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) => visitor.Visit(this, area);
}
