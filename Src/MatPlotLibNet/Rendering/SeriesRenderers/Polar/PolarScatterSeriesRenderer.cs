// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PolarScatterSeries"/> as markers in polar coordinates.</summary>
internal sealed class PolarScatterSeriesRenderer : SeriesRenderer<PolarScatterSeries>
{
    /// <inheritdoc />
    public PolarScatterSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(PolarScatterSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(PolarScatterSeries series)
    {
        var slot = Slot;
        var color = ColorOf(series);
        var transform = PolarTransform.For(Context, series);
        // The size is the marker's width in points (matplotlib's markersize: scatter's s = markersize ** 2 points squared, and a
        // circle marker is sqrt(s) across), drawn at 100 / 72 pixels to the point: the size the series was given, or the theme's when it was given none.
        double radius = slot.ScatterSizeOf(series.GivenMarkerSize).PointsInPixels / 2;
        var style = slot.ScatterStyleOf(color);

        for (int i = 0; i < series.R.Length; i++)
        {
            // A point at a radius or an angle that cannot be placed is skipped, as a cartesian marker is.
            var pt = transform.PolarToPixel(series.R[i], series.Theta[i]);
            if (!pt.IsDrawable()) continue;
            Ctx.DrawCircle(pt, radius, style);
        }
    }
}
