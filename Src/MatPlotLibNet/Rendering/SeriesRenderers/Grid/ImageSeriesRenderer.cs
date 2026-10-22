// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Interpolation;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;

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

        // The value range the colours span: a missing value is left out of it (Math.Min used to carry one NaN into
        // the range, and every pixel with it).
        var drawable = series.Data.DrawableRange() ?? new Range1D(0, 1);
        double min = series.VMin ?? drawable.Lo, max = series.VMax ?? drawable.Hi;
        if (min == max) max = min + 1;

        var cmap = series.GetColorMapOrDefault(ColorMaps.Viridis);
        var norm = series.Normalizer ?? LinearNormalizer.Instance;

        double cellW = Area.PlotBounds.Width  / targetCols;
        double cellH = Area.PlotBounds.Height / targetRows;

        if (series.Alpha < 1.0)
            Ctx.SetOpacity(series.Alpha);

        for (int r = 0; r < targetRows; r++)
        for (int c = 0; c < targetCols; c++)
        {
            var color = cmap.ColorOf(data[r, c], norm, min, max);
            Ctx.DrawRectangle(
                new Rect(Area.PlotBounds.X + c * cellW, Area.PlotBounds.Y + r * cellH, cellW, cellH),
                new ShapeStyle(color, null, 0));
        }

        if (series.Alpha < 1.0)
            Ctx.SetOpacity(1.0);
    }
}
