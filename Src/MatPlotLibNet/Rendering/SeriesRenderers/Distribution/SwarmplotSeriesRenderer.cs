// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SwarmplotSeries"/> using beeswarm layout for non-overlapping dots per category.</summary>
internal sealed class SwarmplotSeriesRenderer : SeriesRenderer<SwarmplotSeries>
{
    /// <inheritdoc />
    public SwarmplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(SwarmplotSeries series, MarkObstacles.Builder marks)
    {
        foreach (var dot in Dots(series))
            if (dot.IsDrawable()) marks.AddPoint(dot);
    }

    /// <summary>The pixel of every observation: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to
    /// a legend.</summary>
    private IEnumerable<Point> Dots(SwarmplotSeries series)
    {
        double r = series.MarkerSize / 2.0;

        // Estimate markerRadius in data units from pixel radius
        double dataPerPixelX = Math.Abs(Transform.DataXMax - Transform.DataXMin) /
                               Math.Max(1, Area.PlotBounds.Width);
        double markerRadiusData = r * dataPerPixelX;

        for (int i = 0; i < series.Datasets.Length; i++)
        {
            var sorted = series.Datasets[i].ToArray();
            Array.Sort(sorted);
            double[] xPositions = BeeswarmLayout.Compute(sorted, markerRadiusData, i);

            for (int j = 0; j < sorted.Length; j++)
                yield return Transform.DataToPixel(xPositions[j], sorted[j]);
        }
    }

    /// <inheritdoc />
    public override void Render(SwarmplotSeries series)
    {
        var color = ResolveColor(series.Color);
        var dotColor = ApplyAlpha(color, series.Alpha);
        double r = series.MarkerSize / 2.0;
        foreach (var dot in Dots(series))
            Ctx.DrawCircle(dot, r, new ShapeStyle(dotColor, null, 0));
    }
}
