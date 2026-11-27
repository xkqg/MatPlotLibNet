// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Downsampling;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="EcdfSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class EcdfSeriesRenderer : SeriesRenderer<EcdfSeries>
{
    /// <inheritdoc />
    public EcdfSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>The points of the staircase in pixels: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend. Empty for a series with no sample.</summary>
    private IReadOnlyList<Point> Path(EcdfSeries series)
    {
        if (series.SortedX.Length == 0) return [];

        // matplotlib's ecdf: plot([x[0], *x], [0, *cdf], drawstyle="steps-post"). The staircase starts at 0, which a log axis puts a
        // thousand decades below it, as matplotlib's log scale does: the first step rises from the edge where the axis begins.
        var drawn = new XYData([series.SortedX[0], .. series.SortedX], [0.0, .. series.CdfY]).SteppedBy(DrawStyle.StepsPost);
        return Transform.TransformPath(drawn.X, drawn.Y);
    }

    /// <inheritdoc />
    internal override void AddMarks(EcdfSeries series, MarkObstacles.Builder marks) => AddPolyline(marks, Path(series));

    /// <inheritdoc />
    public override void Render(EcdfSeries series)
    {
        // A line: a point the axes cannot place breaks it, as a line does; one beyond a log axis cuts it at the edge.
        DrawSeriesLine(Path(series), series);
    }
}
