// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SpectrogramSeries"/> as colored time-frequency cells computed via STFT. Row <c>b</c> is
/// placed at its bin's frequency, <see cref="StftResult.Frequencies"/>[b]: it runs halfway to the neighbouring bins, so it is
/// centred on that frequency, and the first and the last row stop at the ends of the frequency axis, 0 and half the sample
/// rate (the extent <see cref="SpectrogramSeries.ComputeDataRange"/> gives).</summary>
internal sealed class SpectrogramSeriesRenderer : SeriesRenderer<SpectrogramSeries>
{
    /// <inheritdoc />
    public SpectrogramSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(SpectrogramSeries series)
    {
        if (series.Signal.Length == 0) return;

        var stft = series.Spectrum();
        int nBins = stft.Magnitudes.GetLength(0);
        int nFrames = stft.Magnitudes.GetLength(1);
        if (nBins == 0 || nFrames == 0) return;

        // The cells are coloured over the range the colour bar names, worked out from the same magnitudes.
        var cmap = series.GetColorMapOrDefault(ColorMaps.Viridis);
        var norm = series.NormalizerOrLinear();
        var (min, max) = stft.Magnitudes.ScanColorBarRange();

        double duration = series.Signal.Length / (double)series.SampleRate;
        double maxFreq = series.SampleRate / 2.0;
        double dt = duration / nFrames;
        double[] frequencies = stft.Frequencies;

        for (int b = 0; b < nBins; b++)
        {
            double freq0 = b == 0 ? 0 : (frequencies[b - 1] + frequencies[b]) / 2;
            double freq1 = b == nBins - 1 ? maxFreq : (frequencies[b] + frequencies[b + 1]) / 2;
            for (int f = 0; f < nFrames; f++)
            {
                var color = cmap.ColorOf(stft.Magnitudes[b, f], norm, min, max);
                double t0 = f * dt, t1 = t0 + dt;
                Ctx.DrawRectangle(Rect.FromCorners(Transform.DataToPixel(t0, freq1), Transform.DataToPixel(t1, freq0)), new ShapeStyle(color, null, 0));
            }
        }
    }
}
