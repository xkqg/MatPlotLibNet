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
            if (dot.Pixel.IsDrawable()) marks.AddPoint(dot.Pixel);
    }

    /// <summary>The pixel of every observation and the group it belongs to: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend. The jitter is seeded per category, so the output is reproducible.</summary>
    private IEnumerable<GroupedDot> Dots(StripplotSeries series)
    {
        double reach = series.JitterUnder(Context.Theme);
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var data = series.Datasets[i];
            // Deterministic seed per category to keep output reproducible
            var rng = new Random(data.Length * 31 + i);

            foreach (double value in data)
            {
                double jitter = (rng.NextDouble() * 2 - 1) * reach;
                yield return new GroupedDot(Transform.DataToPixel(i + jitter, value), i);
            }
        }
    }

    /// <inheritdoc />
    public override void Render(StripplotSeries series)
    {
        int group = -1;
        double alpha = series.OpacityUnder(Context.Theme);
        double radius = series.DiameterUnder(Context.Theme, Slot) / 2.0;
        var style = default(ShapeStyle);
        foreach (var dot in Dots(series))
        {
            // The dots come group by group, so a group's style is made once.
            if (dot.Group != group) { group = dot.Group; style = ShapeStyle.Filled(GroupColor(series.Color, group).WithOpacityGiven(alpha)); }
            Ctx.DrawCircle(dot.Pixel, radius, style);
        }
    }
}
