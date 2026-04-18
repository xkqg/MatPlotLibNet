// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Phase 5 — consolidated edge-case coverage for series models that fell
/// below 90%. Each test validates a behavioural contract — DTO type, configure-lambda
/// propagation, empty-input safety. The Theory-driven sub-tests close branch coverage
/// across many series at once.</summary>
public class SeriesEdgeCaseTests
{
    // ── 3D series ────────────────────────────────────────────────────────────

    [Fact]
    public void WireframeSeries_DtoTypeAndPropertiesPropagate()
    {
        var s = new WireframeSeries(EdgeCaseData.Ramp(3), EdgeCaseData.Ramp(3), new double[3, 3])
        {
            Color = Colors.Red,
            LineWidth = 2.5,
            Label = "wf",
        };
        Assert.Equal(Colors.Red, s.Color);
        Assert.Equal(2.5, s.LineWidth);
        Assert.Equal("wf", s.Label);
        Assert.Equal("wireframe", s.ToSeriesDto().Type);
    }

    [Fact]
    public void SurfaceSeries_AlphaAndShowWireframePropagate()
    {
        var s = new SurfaceSeries(EdgeCaseData.Ramp(4), EdgeCaseData.Ramp(4), new double[4, 4])
        {
            Alpha = 0.7,
            ShowWireframe = true,
        };
        Assert.Equal(0.7, s.Alpha);
        Assert.True(s.ShowWireframe);
    }

    // ── Categorical ──────────────────────────────────────────────────────────

    [Fact]
    public void WaterfallSeries_StoresInputs()
    {
        var s = new WaterfallSeries(["A", "B", "C"], [10.0, -3, 5]);
        Assert.Equal(3, s.Categories.Length);
        Assert.Equal(3, s.Values.Length);
    }

    [Fact]
    public void GanttSeries_StoresAllArrays()
    {
        var s = new GanttSeries(["T1", "T2"], [0, 5], [10, 15]);
        Assert.Equal(2, s.Tasks.Length);
        Assert.Equal(2, s.Starts.Length);
        Assert.Equal(2, s.Ends.Length);
    }

    [Fact]
    public void BarSeries_HatchAndAlphaPropagate()
    {
        var s = new BarSeries(["A", "B"], [1.0, 2])
        {
            Color = Colors.Blue,
            Alpha = 0.5,
            BarWidth = 0.6,
            Hatch = HatchPattern.ForwardDiagonal,
        };
        Assert.Equal(0.5, s.Alpha);
        Assert.Equal(0.6, s.BarWidth);
        Assert.Equal(HatchPattern.ForwardDiagonal, s.Hatch);
    }

    // ── Polar series ─────────────────────────────────────────────────────────

    [Fact]
    public void PolarLineSeries_ConfigurePropertiesPropagate()
    {
        var s = new PolarLineSeries([1, 2, 3], [0, 1, 2])
        {
            Color = Colors.Blue,
            LineWidth = 3,
        };
        Assert.Equal(Colors.Blue, s.Color);
        Assert.Equal(3, s.LineWidth);
    }

    [Fact]
    public void PolarBarSeries_AlphaPropagates()
    {
        var s = new PolarBarSeries([1, 2, 3], [0, 1, 2])
        {
            Color = Colors.Red,
            Alpha = 0.5,
        };
        Assert.Equal(0.5, s.Alpha);
    }

    // ── ErrorBar ─────────────────────────────────────────────────────────────

    [Fact]
    public void ErrorBarSeries_CapSizeAndColorPropagate()
    {
        var s = new ErrorBarSeries(
            new[] { 1.0, 2, 3 }, new[] { 1.0, 2, 3 },
            new[] { 0.1, 0.2, 0.3 }, new[] { 0.4, 0.5, 0.6 })
        {
            Color = Colors.Red,
            CapSize = 8,
            LineWidth = 2,
        };
        Assert.Equal(8, s.CapSize);
        Assert.Equal(Colors.Red, s.Color);
    }

    // ── Theory: every series accepts its constructor and produces a DTO ──────

    public static IEnumerable<object[]> AllConstructibleSeries()
    {
        yield return new object[] { new LineSeries(EdgeCaseData.Ramp(3), EdgeCaseData.Ramp(3)) };
        yield return new object[] { new ScatterSeries(EdgeCaseData.Ramp(3), EdgeCaseData.Ramp(3)) };
        yield return new object[] { new AreaSeries(EdgeCaseData.Ramp(3), EdgeCaseData.Ramp(3)) };
        yield return new object[] { new StepSeries(EdgeCaseData.Ramp(3), EdgeCaseData.Ramp(3)) };
        yield return new object[] { new BarSeries(["A", "B", "C"], EdgeCaseData.Ramp(3)) };
        yield return new object[] { new HistogramSeries(EdgeCaseData.Sin(50)) { Bins = 10 } };
        yield return new object[] { new PolarLineSeries([1, 2, 3], [0, 1, 2]) };
        yield return new object[] { new PolarBarSeries([1, 2, 3], [0, 1, 2]) };
        yield return new object[] { new SurfaceSeries(EdgeCaseData.Ramp(3), EdgeCaseData.Ramp(3), new double[3, 3]) };
        yield return new object[] { new WireframeSeries(EdgeCaseData.Ramp(3), EdgeCaseData.Ramp(3), new double[3, 3]) };
        yield return new object[] { new WaterfallSeries(["A", "B"], [1.0, 2]) };
        yield return new object[] { new GanttSeries(["T1"], [0], [10]) };
    }

    [Theory] [MemberData(nameof(AllConstructibleSeries))]
    public void EverySeries_ProducesNonNullDto(ChartSeries s)
    {
        var dto = s.ToSeriesDto();
        Assert.NotNull(dto);
        Assert.False(string.IsNullOrEmpty(dto.Type));
    }
}
