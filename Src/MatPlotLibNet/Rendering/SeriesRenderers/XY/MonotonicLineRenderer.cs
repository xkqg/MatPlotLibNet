// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.XY;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Draws a line over monotonic x (<see cref="SignalSeries"/> on a regular grid, <see cref="SignalXYSeries"/> on any rising x): the
/// view sliced by index arithmetic, then the point budget (<see cref="SeriesRenderer.ApplyMonotonicDownsampling{T}"/>), stroked as the kind
/// is. The two kinds differ in the series only, so one renderer draws both.</summary>
/// <typeparam name="TS">The kind of series.</typeparam>
internal sealed class MonotonicLineRenderer<TS> : SeriesRenderer<TS> where TS : XYSeries, IMonotonicXY, IGivenStroked
{
    /// <inheritdoc />
    public MonotonicLineRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>The points of the line in pixels, cut where it leaves the plot towards a value a log axis cannot place: the one geometry
    /// <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a legend.</summary>
    private IReadOnlyList<Point> Path(TS series)
    {
        var data = ApplyMonotonicDownsampling(series, series.MaxDisplayPoints);
        return Transform.TransformPath(data.X, data.Y);
    }

    /// <inheritdoc />
    internal override void AddMarks(TS series, MarkObstacles.Builder marks) => AddPolyline(marks, Path(series));

    /// <inheritdoc />
    public override void Render(TS series) => DrawSeriesLine(Path(series), series);
}
