// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SpectrogramSeries"/> as colored time-frequency cells computed via STFT.</summary>
internal sealed class SpectrogramSeriesRenderer : SeriesRenderer<SpectrogramSeries>
{
    /// <inheritdoc />
    public SpectrogramSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(SpectrogramSeries series)
    {
        if (series.Signal.Length == 0) return;

        var stft = Fft.Stft(series.Signal, series.WindowSize, series.Overlap, series.SampleRate);
        int nBins = stft.Magnitudes.GetLength(0);
        int nFrames = stft.Magnitudes.GetLength(1);
        if (nBins == 0 || nFrames == 0) return;

        // Find global min/max for normalization
        double min = double.MaxValue, max = double.MinValue;
        for (int b = 0; b < nBins; b++)
            for (int f = 0; f < nFrames; f++)
            {
                double v = stft.Magnitudes[b, f];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        if (min == max) max = min + 1;

        var cmap = series.ColorMap ?? ColorMaps.Viridis;
        var norm = series.Normalizer ?? LinearNormalizer.Instance;

        double duration = series.Signal.Length / (double)series.SampleRate;
        double maxFreq = series.SampleRate / 2.0;
        double dt = duration / nFrames;
        double df = maxFreq / nBins;

        for (int b = 0; b < nBins; b++)
        for (int f = 0; f < nFrames; f++)
        {
            var color = cmap.GetColor(norm.Normalize(stft.Magnitudes[b, f], min, max));
            double t0 = f * dt, t1 = t0 + dt;
            double freq0 = b * df, freq1 = freq0 + df;
            var tl = Transform.DataToPixel(t0, freq1);
            var br = Transform.DataToPixel(t1, freq0);
            Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, null, 0);
        }
    }
}
