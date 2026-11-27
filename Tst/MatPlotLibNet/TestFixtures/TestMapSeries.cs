// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests;

/// <summary>A map a test hands the axes: it reports <paramref name="range"/>, traces the outline <paramref name="outline"/> gives in the
/// plot it is placed in, and keeps the range it was placed by. It draws nothing.</summary>
/// <param name="range">The range the map reports, its sticky edges with it.</param>
/// <param name="outline">The outline of the map in the plot of a transform.</param>
internal sealed class TestMapSeries(DataRangeContribution range, Func<DataTransform, IReadOnlyList<Point>> outline) : ChartSeries, IMapSeries
{
    /// <summary>The range the map's outline was last placed by; <see langword="null"/> before it was.</summary>
    public DataRange? Asked { get; private set; }

    public override void Accept(ISeriesVisitor visitor, RenderArea area) { }
    public override DataRangeContribution ComputeDataRange(IAxesContext context) => range;
    public override SeriesDto ToSeriesDto() => new() { Type = "test_map" };

    public IReadOnlyList<Point> OutlineIn(DataTransform transform)
    {
        Asked = transform.DrawnRange;
        return outline(transform);
    }
}
