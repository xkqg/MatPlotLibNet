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

    /// <summary>A hatched segment reaches the canvas as a pattern fill, not a flat colour.</summary>
    [Fact]
    public void HatchedSegment_RendersAsAPattern()
    {
        string svg = Render(new StateSegment(0, 10, "No contact", Colors.Gray)
        {
            Hatch = HatchPattern.ForwardDiagonal
        });

        Assert.Contains("<pattern", svg);
        Assert.Contains("fill=\"url(#", svg);
    }

    /// <summary>Hatched and plain segments coexist on one row: only the hatched band gets a pattern, the
    /// others keep their flat fill. A single unknown stretch must stand out inside an otherwise normal row.</summary>
    [Fact]
    public void OnlyTheHatchedSegment_GetsAPattern()
    {
        string svg = Render(
            new StateSegment(0, 10, "Up", Colors.Tab10Green),
            new StateSegment(10, 20, "No contact", Colors.Gray) { Hatch = HatchPattern.ForwardDiagonal },
            new StateSegment(20, 30, "Up", Colors.Tab10Green));

        Assert.Equal(1, CountOccurrences(svg, "<pattern"));
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

        var serializer = new ChartSerializer();
        var restored = serializer.FromJson(serializer.ToJson(figure))
            .SubPlots[0].Series.OfType<StateTimelineSeries>().Single();

        Assert.Equal(HatchPattern.None, restored.Segments[0].Hatch);
        Assert.Equal(HatchPattern.BackDiagonal, restored.Segments[1].Hatch);
    }

    /// <summary>An unhatched timeline adds no hatch bytes — the existing golden stays byte-identical.</summary>
    [Fact]
    public void UnhatchedTimeline_AddsNoBytes()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.StateTimeline([new StateSegment(0, 10, "Up", Colors.Tab10Green)]))
            .Build();

        Assert.DoesNotContain("atch", new ChartSerializer().ToJson(figure));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            count++;
            i += needle.Length;
        }

        return count;
    }
}
