// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SpectrogramSeries"/> as coloured time-frequency cells computed via STFT, as the theme's spectrogram look
/// says (<see cref="SpectrogramLook"/>). Under the matplotlib themes the cells are matplotlib's <c>specgram</c>: the one-sided power density
/// of each window, an image on its extent (<see cref="SpectrogramLook.ExtentOf"/>) scaled by the theme's interpolation
/// (<see cref="ImageCells.DrawScaled"/>), row 0 at the bottom. Under the library's own themes the magnitudes on a mesh: row <c>b</c> is
/// placed at its bin's frequency, <see cref="StftResult.Frequencies"/>[b], running halfway to the neighbouring bins, so it is centred on that
/// frequency, and the first and the last row stop at the ends of the frequency axis, 0 and half the sample rate (the extent
/// <see cref="SpectrogramSeries.ComputeDataRange"/> gives); the windows side by side over the signal's duration.</summary>
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

        // The cells the theme fills them with, coloured over the range the colour bar names, worked out from the same values, which are
        // not worked out again.
        var theme = Context.Theme;
        var cells = series.CellsUnder(theme, stft);
        var scale = series.ColorScaleUnder(theme, cells);

        if (theme.Looks.Spectrogram.LikeSpecgram)
        {
            var extent = SpectrogramLook.ExtentOf(series.Frames, series.SampleRate);
            new ImageCells(scale, 1).DrawScaled(Ctx, cells, null, theme.Looks.Image,
                Transform.DataToPixel(extent.Time.Lo, extent.Frequency.Lo), Transform.DataToPixel(extent.Time.Hi, extent.Frequency.Hi));
            return;
        }

        double duration = series.Signal.Length / (double)series.SampleRate;
        double maxFreq = series.SampleRate / 2.0;
        double dt = duration / nFrames;
        double[] frequencies = stft.Frequencies;

        // A cell of the mesh stands on the pixel grid of a raster export under a theme that asks for it: no seam between two cells.
        for (int b = 0; b < nBins; b++)
        {
            double freq0 = b == 0 ? 0 : (frequencies[b - 1] + frequencies[b]) / 2;
            double freq1 = b == nBins - 1 ? maxFreq : (frequencies[b] + frequencies[b + 1]) / 2;
            for (int f = 0; f < nFrames; f++)
            {
                var color = scale.ColorOf(cells[b, f]);
                double t0 = f * dt, t1 = t0 + dt;
                Ctx.DrawSnappedRectangle(Rect.FromCorners(Transform.DataToPixel(t0, freq1), Transform.DataToPixel(t1, freq0)), ShapeStyle.Filled(color));
            }
        }
    }
}
