// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="StateTimelineSeries"/> — a single-row horizontal timeline of
/// discrete coloured state segments along X (e.g. participant up/down, alarm state over time).
/// Covers ctor, <see cref="StateSegment"/> struct, <see cref="ChartSeries.ComputeDataRange"/>,
/// <see cref="ChartSeries.ToSeriesDto"/>, rendering output, serialization round-trip, and XSS
/// escaping of malicious segment labels.</summary>
public class StateTimelineSeriesTests
{
    // ── Construction / struct ──────────────────────────────────────────────────

    [Fact]
    public void Ctor_StoresSegments()
    {
        var segs = new[]
        {
            new StateSegment(0, 10, "Up", Colors.Green),
            new StateSegment(10, 20, "Down", Colors.Red),
        };
        var s = new StateTimelineSeries(segs);
        Assert.Equal(segs, s.Segments);
    }

    [Fact]
    public void StateSegment_Struct_ExposesAllProperties()
    {
        var seg = new StateSegment(1.0, 2.5, "Alarm", Colors.Tab10Orange);
        Assert.Equal(1.0, seg.Start);
        Assert.Equal(2.5, seg.End);
        Assert.Equal("Alarm", seg.Label);
        Assert.Equal(Colors.Tab10Orange, seg.Color);
    }

    // ── ComputeDataRange ───────────────────────────────────────────────────────

    [Fact]
    public void ComputeDataRange_Empty_ReturnsAllNull()
    {
        var r = new StateTimelineSeries([]).ComputeDataRange(null!);
        Assert.False(r.XMin.HasValue);
        Assert.False(r.XMax.HasValue);
        Assert.False(r.YMin.HasValue);
        Assert.False(r.YMax.HasValue);
    }

    [Fact]
    public void ComputeDataRange_NonEmpty_ReturnsCorrectXRangeAndUnitY()
    {
        var segs = new[]
        {
            new StateSegment(5.0, 15.0, "A", Colors.Green),
            new StateSegment(15.0, 30.0, "B", Colors.Red),
        };
        var r = new StateTimelineSeries(segs).ComputeDataRange(null!);
        Assert.Equal(5.0, r.XMin);
        Assert.Equal(30.0, r.XMax);
        Assert.Equal(0.0, r.YMin);
        Assert.Equal(1.0, r.YMax);
    }

    [Fact]
    public void ComputeDataRange_SingleSegment_CorrectBounds()
    {
        var r = new StateTimelineSeries([new StateSegment(3.0, 7.0, "X", Colors.Blue)])
            .ComputeDataRange(null!);
        Assert.Equal(3.0, r.XMin);
        Assert.Equal(7.0, r.XMax);
        Assert.Equal(0.0, r.YMin);
        Assert.Equal(1.0, r.YMax);
    }

    // ── ToSeriesDto ────────────────────────────────────────────────────────────

    [Fact]
    public void ToSeriesDto_TypeIsStateTimeline()
    {
        var dto = new StateTimelineSeries([]).ToSeriesDto();
        Assert.Equal("statetimeline", dto.Type);
    }

    [Fact]
    public void ToSeriesDto_RoundTripsSegmentCount()
    {
        var segs = new[]
        {
            new StateSegment(0, 5, "On",  Colors.Green),
            new StateSegment(5, 10, "Off", Colors.Red),
        };
        var dto = new StateTimelineSeries(segs).ToSeriesDto();
        // Per design: round-trip carries Type + segment count (via Starts/Ends array lengths)
        Assert.Equal(2, dto.Starts?.Length);
        Assert.Equal(2, dto.Ends?.Length);
        Assert.Equal(2, dto.Categories?.Length);
        Assert.Equal(2, dto.StateSegmentColors?.Count);
    }

    // ── Render ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Render_TwoSegments_SvgContainsBothLabelsAndRects()
    {
        var segs = new[]
        {
            new StateSegment(0, 5,  "Running", Colors.Green),
            new StateSegment(5, 10, "Stopped", Colors.Red),
        };
        var svg = Plt.Create().StateTimeline(segs).ToSvg();
        Assert.Contains("Running", svg);
        Assert.Contains("Stopped", svg);
        Assert.Contains("<rect", svg);
    }

    [Fact]
    public void Render_EmptySegments_DoesNotThrow()
    {
        var svg = Plt.Create().StateTimeline([]).ToSvg();
        Assert.NotNull(svg);
    }

    [Fact]
    public void Render_OneSegment_SvgContainsLabel()
    {
        var svg = Plt.Create().StateTimeline([new StateSegment(0, 1, "Active", Colors.Blue)]).ToSvg();
        Assert.Contains("Active", svg);
    }

    // ── XSS escaping ──────────────────────────────────────────────────────────

    [Fact]
    public void Render_MaliciousLabel_IsXmlEscaped_AndNoLiveScript()
    {
        var segs = new[] { new StateSegment(0, 1, "<script>alert(1)</script>", Colors.Red) };
        var svg = Plt.Create().StateTimeline(segs).ToSvg();
        Assert.DoesNotContain("<script>", svg);
        Assert.Contains("&lt;script&gt;", svg);
    }

    // ── Registry round-trip ───────────────────────────────────────────────────

    [Fact]
    public void Registry_DeserializesFromDto_RestoringSegmentCountAndType()
    {
        var segs = new[]
        {
            new StateSegment(0, 5,  "Up",   Colors.Green),
            new StateSegment(5, 10, "Down", Colors.Red),
        };
        var dto = new StateTimelineSeries(segs).ToSeriesDto();
        var s = (StateTimelineSeries)SeriesRegistry.Create("statetimeline", new Axes(), dto)!;
        Assert.Equal(2, s.Segments.Count);
        Assert.Equal("Up",  s.Segments[0].Label);
        Assert.Equal(0.0,   s.Segments[0].Start);
        Assert.Equal(5.0,   s.Segments[0].End);
    }
}
