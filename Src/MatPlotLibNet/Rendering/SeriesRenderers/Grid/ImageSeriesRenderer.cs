// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Interpolation;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="ImageSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class ImageSeriesRenderer : SeriesRenderer<ImageSeries>
{
    /// <inheritdoc />
    public ImageSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(ImageSeries series)
    {
        int srcRows = series.Data.GetLength(0), srcCols = series.Data.GetLength(1);
        if (srcRows == 0 || srcCols == 0) return;

        // Resolve interpolation engine
        var engine = InterpolationRegistry.Get(series.Interpolation ?? "nearest")
                     ?? NearestInterpolation.Instance;

        // Compute upsampled target resolution
        int targetRows, targetCols;
        if (engine is NearestInterpolation)
        {
            targetRows = srcRows;
            targetCols = srcCols;
        }
        else
        {
            targetRows = Math.Min(srcRows * 4, 256);
            targetCols = Math.Min(srcCols * 4, 256);
        }

        var data = engine.Resample(series.Data, targetRows, targetCols);

        // The cells (the colour range of the image's own values, the grid of rectangles) are the ones a figure image draws too.
        ImageCells
            .Of(series.Data, series.GetColorMapOrDefault(ColorMaps.Viridis), series.NormalizerOrLinear(), series.VMin, series.VMax, series.Alpha)
            .Draw(Ctx, data, Area.PlotBounds);
    }
}
