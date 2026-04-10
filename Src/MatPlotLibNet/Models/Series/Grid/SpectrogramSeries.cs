// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>Represents a spectrogram series that displays the time-frequency content of a signal using Short-Time FFT.</summary>
public sealed class SpectrogramSeries : ChartSeries, IColorBarDataProvider, IColormappable, INormalizable
{
    /// <summary>Gets the input signal values.</summary>
    public Vec Signal { get; }

    /// <summary>Gets or sets the sample rate in Hz used to compute frequency and time axes.</summary>
    public int SampleRate { get; set; } = 1;

    /// <summary>Gets or sets the number of samples per FFT window.</summary>
    public int WindowSize { get; set; } = 256;

    /// <summary>Gets or sets the number of overlapping samples between adjacent windows.</summary>
    public int Overlap { get; set; } = 128;

    /// <summary>Gets or sets the color map used to map magnitude values to colors.</summary>
    public IColorMap? ColorMap { get; set; }

    /// <summary>Gets or sets the normalizer used to map magnitude values to [0, 1] for colormap lookup.</summary>
    public INormalizer? Normalizer { get; set; }

    /// <inheritdoc />
    public (double Min, double Max) GetColorBarRange() => (0, 1);

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
