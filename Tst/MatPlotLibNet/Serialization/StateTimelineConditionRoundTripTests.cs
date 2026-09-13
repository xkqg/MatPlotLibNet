// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Serialization;

/// <summary>A band's condition has to survive the wire, and it has to cost nothing when nobody set one.
/// <para>Both halves matter. A timeline whose shelved stretch comes back observed reports calm where an
/// operator muted something — the one failure that makes a wall lie. And a timeline that never named a
/// condition must write exactly the bytes it wrote before the axis existed, or every golden in the corpus
/// moves for a feature the chart does not use.</para></summary>
public class StateTimelineConditionRoundTripTests
{
    private static readonly ChartSerializer Serializer = new();

    private static StateTimelineSeries RoundTrip(params StateSegment[] segments)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StateTimeline(segments)).Build();
        var restored = Serializer.FromJson(Serializer.ToJson(figure));
        return restored.SubPlots[0].Series.OfType<StateTimelineSeries>().Single();
    }

    private static string Json(params StateSegment[] segments) =>
        Serializer.ToJson(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.StateTimeline(segments)).Build());

    [Fact]
    public void ASeverityOnABand_SurvivesTheWire()
    {
        var restored = RoundTrip(
            new StateSegment(0, 5, "Up", Colors.Tab10Green),
            new StateSegment(5, 10, "Straining", Colors.Tab10Orange)
            {
                Condition = new OpsCondition(OpsSeverity.Warning)
            });

        Assert.Equal(OpsSeverity.Normal, restored.Segments[0].Condition.Severity);
        Assert.Equal(OpsSeverity.Warning, restored.Segments[1].Condition.Severity);
    }

    [Fact]
    public void AShelvedBand_ComesBackShelved()
    {
        var restored = RoundTrip(
            new StateSegment(0, 5, "Up", Colors.Tab10Green),
            new StateSegment(5, 10, "Muted", Colors.Gray)
            {
                Condition = new OpsCondition(OpsSeverity.Critical, OpsVisibility.Shelved)
            });

        Assert.Equal(OpsVisibility.Shelved, restored.Segments[1].Condition.Visibility);
        Assert.Equal(OpsSeverity.Critical, restored.Segments[1].Condition.Severity);
    }

    [Fact]
    public void AnUnseenBand_ComesBackUnseen()
    {
        var restored = RoundTrip(new StateSegment(0, 5, "No contact", Colors.Gray)
        {
            Condition = OpsCondition.Unknown
        });

        Assert.Equal(OpsVisibility.Unknown, Assert.Single(restored.Segments).Condition.Visibility);
    }

    [Fact]
    public void ATimelineWithoutConditions_WritesNeitherList()
    {
        string json = Json(
            new StateSegment(0, 5, "Up", Colors.Tab10Green),
            new StateSegment(5, 10, "Up", Colors.Tab10Green));

        Assert.DoesNotContain("Severities", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Visibilities", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ATimelineWithASeverityButNoVisibility_WritesOnlyTheOneItUses()
    {
        string json = Json(new StateSegment(0, 5, "Straining", Colors.Tab10Orange)
        {
            Condition = new OpsCondition(OpsSeverity.Warning)
        });

        Assert.Contains("Severities", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Visibilities", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ATimelineWithAVisibilityButNoSeverity_WritesOnlyTheOneItUses()
    {
        string json = Json(new StateSegment(0, 5, "No contact", Colors.Gray)
        {
            Condition = OpsCondition.Unknown
        });

        Assert.Contains("Visibilities", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Severities", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ABandBeyondAShortConditionList_ComesBackResting()
    {
        // A hand-made or older payload can carry fewer conditions than bands. Every band past the end of the
        // list reads as resting rather than throwing or borrowing its neighbour's state.
        var dto = new SeriesDto
        {
            Type = "statetimeline",
            Starts = [0, 5],
            Ends = [5, 10],
            Categories = ["Up", "Muted"],
            StateSegmentColors = [Colors.Tab10Green, Colors.Gray],
            StateSegmentSeverities = [OpsSeverity.Critical],
            StateSegmentVisibilities = [OpsVisibility.Shelved],
        };

        var figure = Plt.Create().AddSubPlot(1, 1, 1, _ => { }).Build();
        var series = StateTimelineSeries.FromSeriesDto(figure.SubPlots[0], dto);

        Assert.Equal(OpsVisibility.Shelved, series.Segments[0].Condition.Visibility);
        Assert.Equal(OpsCondition.Resting, series.Segments[1].Condition);
    }
}
