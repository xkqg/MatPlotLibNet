// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
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

        double min = double.MaxValue, max = double.MinValue;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (series.C[r, c] < min) min = series.C[r, c];
                if (series.C[r, c] > max) max = series.C[r, c];
            }
        if (min == max) max = min + 1;

        var cmap = series.ColorMap ?? ColorMaps.Viridis;
        var norm = series.Normalizer ?? LinearNormalizer.Instance;

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            var color = cmap.GetColor(norm.Normalize(series.C[r, c], min, max));
            var tl = Transform.DataToPixel(series.X[c], series.Y[r + 1]);
            var br = Transform.DataToPixel(series.X[c + 1], series.Y[r]);
            Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), color, null, 0);
        }
    }
}
