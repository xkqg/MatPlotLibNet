// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.TestFixtures;

/// <summary>A series that draws nothing and reports the range of the points it is given, counting how many times it was asked
/// for that range. The axes asks every series once for each range it computes, so <see cref="RangeCalls"/> is the number of
/// times the axes' data range was computed since the series was added: a test that holds the count says how often a draw
/// repeats that work.</summary>
/// <param name="x">The X values the series reports.</param>
/// <param name="y">The Y values the series reports.</param>
public sealed class CountingRangeSeries(double[] x, double[] y) : ChartSeries
{
    /// <summary>How many times <see cref="ComputeDataRange"/> has been called.</summary>
    public int RangeCalls { get; private set; }

    /// <inheritdoc />
    public override void Accept(ISeriesVisitor visitor, RenderArea area) { }

    /// <inheritdoc />
    public override DataRangeContribution ComputeDataRange(IAxesContext context)
    {
        RangeCalls++;
        return new DataRangeContribution(x.Min(), x.Max(), y.Min(), y.Max());
    }

    /// <inheritdoc />
    public override SeriesDto ToSeriesDto() => new() { Type = "counting" };
}
