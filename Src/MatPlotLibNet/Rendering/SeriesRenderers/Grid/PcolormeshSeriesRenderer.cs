// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PcolormeshSeries"/> as colored rectangular cells on a non-uniform grid.</summary>
internal sealed class PcolormeshSeriesRenderer : SeriesRenderer<PcolormeshSeries>
{
    /// <inheritdoc />
    public PcolormeshSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(PcolormeshSeries series)
    {
        int rows = series.C.GetLength(0), cols = series.C.GetLength(1);
        if (rows == 0 || cols == 0) return;
        if (series.X.Length < 2 || series.Y.Length < 2) return;

        var scale = series.ColorScaleUnder(Context.Theme);

        // A cell stands on the pixel grid of a raster export under a theme that asks for it, as Agg snaps a quad of matplotlib's
        // QuadMesh: no seam between two cells.
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            var cell = Rect.FromCorners(Transform.DataToPixel(series.X[c], series.Y[r + 1]), Transform.DataToPixel(series.X[c + 1], series.Y[r]));
            if (!cell.IsDrawable()) continue;   // a cell with an edge that cannot be placed is skipped
            var color = scale.ColorOf(series.C[r, c]);
            Ctx.DrawSnappedRectangle(cell, ShapeStyle.Filled(color));
        }
    }
}
