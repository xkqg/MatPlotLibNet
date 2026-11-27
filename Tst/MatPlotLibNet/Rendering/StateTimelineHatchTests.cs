// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Verifies a per-segment hatch on <see cref="StateTimelineSeries"/>.
/// <para>This is the representation that keeps a control-room wall honest: "I can no longer see you" is a
/// different fault from "you are broken", and it must not be encoded as a colour. A hatched band says
/// <i>no information</i> — it cannot be mistaken for the quiet grey of a healthy row, and it does not spend
/// a colour out of the alarm budget.</para></summary>
public class StateTimelineHatchTests
{
    private static string Render(params StateSegment[] segments) =>
        Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StateTimeline(segments))
            .ToSvg();

    /// <summary>A segment carries no hatch by default — an ordinary state band is a flat fill.</summary>
    [Fact]
    public void Segment_Hatch_DefaultsToNone()
    {
        var segment = new StateSegment(0, 10, "Up", Colors.Tab10Green);

        Assert.Equal(HatchPattern.None, segment.Hatch);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        // A hatched segment reaches the canvas as a pattern fill, not a flat colour.
        new DrawnToken("HatchedSegment_RendersAsAPattern", () => Render(new StateSegment(0, 10, "No contact", Colors.Gray)
        {
            Hatch = HatchPattern.ForwardDiagonal
        }), "<pattern", "fill=\"url(#"),
        // A band that carries a condition instead of a pattern still draws one: the mapping lives in the library, so a caller does not
        // repeat it per segment.
        new DrawnToken("ASegmentInAShelvedCondition_RendersAsAPattern", () => Render(new StateSegment(0, 10, "Muted", Colors.Gray)
        {
            Condition = new OpsCondition(OpsSeverity.Critical, OpsVisibility.Shelved)
        }), "<pattern"),
        // A resting band draws flat — no pattern appears from a condition nobody set.
        new DrawnToken("ARestingSegment_StaysFlat", () => Render(new StateSegment(0, 10, "Up", Colors.Tab10Green))) { Absent = ["<pattern"] },
        // An unhatched timeline adds no hatch bytes — the existing golden stays byte-identical.
        new DrawnToken("UnhatchedTimeline_AddsNoBytes", () =>
        {
            var figure = Plt.Create()
                .AddSubPlot(1, 1, 1, ax => ax.StateTimeline([new StateSegment(0, 10, "Up", Colors.Tab10Green)]))
                .Build();
            return new ChartSerializer().ToJson(figure);
        }) { Absent = ["atch"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    /// <summary>Hatched and plain segments coexist on one row: only the hatched band gets a pattern, the
    /// others keep their flat fill. A single unknown stretch must stand out inside an otherwise normal row.</summary>
    [Fact]
    public void OnlyTheHatchedSegment_GetsAPattern()
    {
        string svg = Render(
            new StateSegment(0, 10, "Up", Colors.Tab10Green),
            new StateSegment(10, 20, "No contact", Colors.Gray) { Hatch = HatchPattern.ForwardDiagonal },
            new StateSegment(20, 30, "Up", Colors.Tab10Green));

        Assert.Equal(1, svg.CountOf("<pattern"));
        Assert.Contains(Colors.Tab10Green.ToHex(), svg);
    }

    /// <summary>The hatch survives a JSON round-trip: a dashboard restored from the wire still distinguishes
    /// "unknown" from "broken".</summary>
    [Fact]
    public void SegmentHatch_SurvivesRoundTrip()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StateTimeline([
                new StateSegment(0, 10, "Up", Colors.Tab10Green),
                new StateSegment(10, 20, "No contact", Colors.Gray) { Hatch = HatchPattern.BackDiagonal }
            ]))
            .Build();

        var restored = figure.RoundTripped()
            .SubPlots[0].Series.OfType<StateTimelineSeries>().Single();

        Assert.Equal(HatchPattern.None, restored.Segments[0].Hatch);
        Assert.Equal(HatchPattern.BackDiagonal, restored.Segments[1].Hatch);
    }

    /// <summary>An explicit hatch wins over the condition's: what the caller said outright is never
    /// overruled.</summary>
    [Fact]
    public void AnExplicitHatch_WinsOverTheCondition()
    {
        var segment = new StateSegment(0, 10, "Muted", Colors.Gray)
        {
            Hatch = HatchPattern.Dots,
            Condition = OpsCondition.Unknown
        };

        Assert.Equal(HatchPattern.Dots, segment.Hatch);
        Assert.Contains("<pattern", Render(segment));
    }
}
