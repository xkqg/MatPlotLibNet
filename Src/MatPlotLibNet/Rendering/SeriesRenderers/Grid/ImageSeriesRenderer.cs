// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
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

        // The image stands on its extent, from the outer corner of cell [0, 0] to that of the last: under the matplotlib themes each
        // cell centred on its numbers (imshow), the first row at y = -0.5; under the library's own themes on 0..cols and 0..rows,
        // the first row on top at y = rows.
        bool imshow = Context.Theme.Looks.Image.LikeImshow;
        var first = imshow ? Transform.DataToPixel(-ImageSeries.HalfCell, -ImageSeries.HalfCell) : Transform.DataToPixel(0, srcRows);
        var last = imshow ? Transform.DataToPixel(srcCols - ImageSeries.HalfCell, srcRows - ImageSeries.HalfCell) : Transform.DataToPixel(srcCols, 0);

        // The cells (the colour range of the image's own values, the grid of rectangles) are the ones a figure image draws too, scaled by
        // the interpolation the series names, else the theme's (ImageCells.DrawScaled: matplotlib's rule).
        new ImageCells(series.ColorScaleUnder(Context.Theme), series.Alpha)
            .DrawScaled(Ctx, series.Data, series.Interpolation, Context.Theme.Looks.Image, first, last);
    }
}
