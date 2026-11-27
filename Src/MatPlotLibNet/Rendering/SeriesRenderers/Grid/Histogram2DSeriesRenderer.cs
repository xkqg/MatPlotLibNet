// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="Histogram2DSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class Histogram2DSeriesRenderer : SeriesRenderer<Histogram2DSeries>
{
    /// <inheritdoc />
    public Histogram2DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Histogram2DSeries series)
    {
        if (series.X.Length == 0 || series.Y.Length == 0) return;

        if (series.BinExtent() is not var (xMin, xMax, yMin, yMax)) return;
        int binsX = series.BinsX, binsY = series.BinsY;
        var counts = series.ComputeBinCounts();
        // The cells stand on the edges the points were counted in.
        var columns = new EqualWidthBins(xMin, xMax, binsX);
        var rows = new EqualWidthBins(yMin, yMax, binsY);

        // The counts already worked out colour the cells, over the range of those the scale can place.
        var scale = series.ColorScaleUnder(Context.Theme, counts);

        // Each cell stands on its bin, the lowest Y bin at the bottom, as matplotlib draws the mesh of np.histogram2d's
        // edges; an axis that reaches past the points leaves room around the mesh. A cell of the mesh stands on the pixel grid of a
        // raster export under a theme that asks for it, as Agg snaps a quad of matplotlib's QuadMesh: no seam between two cells.
        for (int r = 0; r < binsY; r++)
        for (int c = 0; c < binsX; c++)
        {
            var color = scale.ColorOf(counts[r, c]);
            var a = Transform.DataToPixel(columns.EdgeAt(c), rows.EdgeAt(r));
            var b = Transform.DataToPixel(columns.EdgeAt(c + 1), rows.EdgeAt(r + 1));
            Ctx.DrawSnappedRectangle(
                new Rect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y)),
                ShapeStyle.Filled(color));
        }
    }
}
