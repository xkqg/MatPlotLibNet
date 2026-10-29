// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="EcdfSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class EcdfSeriesRenderer : SeriesRenderer<EcdfSeries>
{
    /// <inheritdoc />
    public EcdfSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>The points of the staircase in pixels: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend. Empty for a series with no sample.</summary>
    private List<Point> Path(EcdfSeries series)
    {
        int n = series.SortedX.Length;
        if (n == 0) return [];

        // Precompute pixel coordinates for all data points (SIMD batch)
        var pxArr = Transform.TransformX(series.SortedX);
        var pyArr = Transform.TransformY(series.CdfY);
        double pyZero = Transform.TransformY([0.0])[0];

        var pts = new List<Point>(2 * n + 1);
        pts.Add(new Point(pxArr[0], pyZero)); // Start at (SortedX[0], 0)

        for (int i = 0; i < n; i++)
        {
            // Horizontal line to (SortedX[i], previous CDF value)
            double prevPy = i > 0 ? pyArr[i - 1] : pyZero;
            pts.Add(new Point(pxArr[i], prevPy));
            // Vertical line to (SortedX[i], CdfY[i])
            pts.Add(new Point(pxArr[i], pyArr[i]));
        }

        return pts;
    }

    /// <inheritdoc />
    internal override void AddMarks(EcdfSeries series, MarkObstacles.Builder marks) => AddPolyline(marks, Path(series));

    /// <inheritdoc />
    public override void Render(EcdfSeries series)
    {
        var color = ResolveColor(series.Color);
        // A line: a point the axes cannot place (its start at 0 on a log axis) is masked and breaks it, as a line does.
        DrawPolyline(Path(series), Slot.StrokeOf(color, series.LineWidth, series.LineStyle));
    }
}
