// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="StripplotSeries"/> as randomly jittered dots per category.</summary>
internal sealed class StripplotSeriesRenderer : SeriesRenderer<StripplotSeries>
{
    /// <inheritdoc />
    public StripplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(StripplotSeries series, MarkObstacles.Builder marks)
    {
        foreach (var dot in Dots(series))
            if (dot.IsDrawable()) marks.AddPoint(dot);
    }

    /// <summary>The pixel of every observation: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to
    /// a legend. The jitter is seeded per category, so the output is reproducible.</summary>
    private IEnumerable<Point> Dots(StripplotSeries series)
    {
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var data = series.Datasets[i];
            // Deterministic seed per category to keep output reproducible
            var rng = new Random(data.Length * 31 + i);

            foreach (double value in data)
            {
                double jitter = (rng.NextDouble() * 2 - 1) * series.Jitter;
                yield return Transform.DataToPixel(i + jitter, value);
            }
        }
    }

    /// <inheritdoc />
    public override void Render(StripplotSeries series)
    {
        var color = ResolveColor(series.Color);
        var dotColor = ApplyAlpha(color, series.Alpha);
        foreach (var dot in Dots(series))
            Ctx.DrawCircle(dot, series.MarkerSize / 2.0, new ShapeStyle(dotColor, null, 0));
    }
}
