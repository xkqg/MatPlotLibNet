// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="TernaryScatterSeries"/> as a marker on each composition. Nothing clips the markers to the box: a
/// sample at a corner of the triangle is a corner of the box, and is drawn whole.</summary>
internal sealed class TernaryScatterSeriesRenderer : SeriesRenderer<TernaryScatterSeries>
{
    /// <inheritdoc />
    public TernaryScatterSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(TernaryScatterSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(TernaryScatterSeries series)
    {
        var color = ResolveColor(series.Color);

        foreach (var pixel in Context.PixelsOf(series))
        {
            if (!pixel.IsDrawable()) continue;
            Ctx.DrawCircle(pixel, series.MarkerSize / 2, new ShapeStyle(color, null, 0));
        }
    }
}
