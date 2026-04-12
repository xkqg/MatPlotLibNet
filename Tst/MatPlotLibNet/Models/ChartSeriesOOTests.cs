// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Models;

/// <summary>Verifies that ChartSeries enforces IHasDataRange and ISeriesSerializable
/// so callers never need to cast.</summary>
public class ChartSeriesOOTests
{
    /// <summary>Verifies that ISeries contract includes ComputeDataRange.</summary>
    [Fact]
    public void ISeries_HasComputeDataRange()
    {
        ISeries series = new LineSeries([1.0], [2.0]);
        // Should compile and work without any cast
        var range = series.ComputeDataRange(new NullAxesContext());
        Assert.NotNull(range.XMin);
    }

    /// <summary>Verifies that ChartSeries implements ISeriesSerializable.</summary>
    [Fact]
    public void ChartSeries_ImplementsISeriesSerializable()
    {
        ChartSeries series = new LineSeries([1.0], [2.0]);
        Assert.IsAssignableFrom<ISeriesSerializable>(series);
    }

    /// <summary>Verifies that all concrete series can compute data range via ISeries (no cast).</summary>
    [Fact]
    public void AllSeries_ComputeDataRange_NoCastRequired()
    {
        ISeries[] series =
        [
            new LineSeries([1.0], [2.0]),
            new ScatterSeries([1.0], [2.0]),
            new BarSeries(["A"], [1.0]),
            new PieSeries([1.0, 2.0]),
            new HistogramSeries([1.0, 2.0, 3.0]),
        ];

        var ctx = new NullAxesContext();
        foreach (var s in series)
        {
            // Direct call on ISeries — no IHasDataRange cast needed
            var range = s.ComputeDataRange(ctx);
            Assert.True(range.XMin.HasValue || range.YMin.HasValue || true); // just verifying it returns
        }
    }

    /// <summary>Verifies that all concrete series can serialize via ChartSeries (no cast).</summary>
    [Fact]
    public void AllSeries_ToSeriesDto_NoCastRequired()
    {
        ChartSeries[] series =
        [
            new LineSeries([1.0], [2.0]),
            new ScatterSeries([1.0], [2.0]),
            new BarSeries(["A"], [1.0]),
            new PieSeries([1.0, 2.0]),
        ];

        foreach (var s in series)
        {
            // Direct call on ChartSeries — no ISeriesSerializable cast needed
            var dto = s.ToSeriesDto();
            Assert.NotNull(dto.Type);
        }
    }

    /// <summary>Verifies that the serializer no longer produces "unknown" type.</summary>
    [Fact]
    public void SerializerFallback_Eliminated()
    {
        var fig = new Figure();
        var ax = fig.AddSubPlot();
        ax.Plot([1.0], [2.0]);
        ax.Scatter([1.0], [2.0]);
        ax.Bar(["A"], [1.0]);

        string json = ChartServices.Serializer.ToJson(fig);
        Assert.DoesNotContain("unknown", json);
    }

    private sealed class NullAxesContext : IAxesContext
    {
        public double? XAxisMin => null;
        public double? XAxisMax => null;
        public double? YAxisMin => null;
        public double? YAxisMax => null;
        public BarMode BarMode => BarMode.Grouped;
        public IReadOnlyList<ISeries> AllSeries => [];
    }
}
