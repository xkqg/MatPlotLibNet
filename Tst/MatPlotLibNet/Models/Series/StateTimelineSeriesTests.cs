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
/// Covers <see cref="ChartSeries.ComputeDataRange"/>, <see cref="ChartSeries.ToSeriesDto"/>, rendering output,
/// serialization round-trip, and XSS escaping of malicious segment labels.</summary>
public class StateTimelineSeriesTests
{
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

    // ── Render ─────────────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Render_TwoSegments_SvgContainsBothLabelsAndRects", () =>
        {
            var segs = new[]
            {
                new StateSegment(0, 5,  "Running", Colors.Green),
                new StateSegment(5, 10, "Stopped", Colors.Red),
            };
            return Plt.Create().StateTimeline(segs).ToSvg();
        }, "Running", "Stopped", "<rect"),
        new DrawnToken("Render_OneSegment_SvgContainsLabel", () => Plt.Create().StateTimeline([new StateSegment(0, 1, "Active", Colors.Blue)]).ToSvg(), "Active"),
        new DrawnToken("Render_MaliciousLabel_IsXmlEscaped_AndNoLiveScript", () =>
        {
            var segs = new[] { new StateSegment(0, 1, "<script>alert(1)</script>", Colors.Red) };
            return Plt.Create().StateTimeline(segs).ToSvg();
        }, "&lt;script&gt;") { Absent = ["<script>"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

}
