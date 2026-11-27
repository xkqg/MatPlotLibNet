// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>VLines</c> and <c>HLines</c> verbs, one the other turned on its side: a line segment at each position between
/// two limits, vertical at an x position or horizontal at a y position, drawn as ONE line series (one colour slot, one legend
/// entry) whose segments are parted by missing points, so the picture is one polyline for each segment and the chart travels
/// through JSON as that one series. Every fact holds for both verbs with X and Y swapped. The geometry itself is
/// <c>SegmentPath</c>, held to its rules in <c>SegmentPathTests</c>; this holds what the verbs do with it.</summary>
public class SegmentLineVerbsTests
{
    private static readonly Color SeriesColour = Theme.Default.CycleColors[0];

    public static TheoryData<string> Verbs { get; } = ["VLines", "HLines"];

    private static Figure Of(string verb, double[] positions, double[] from, double[] to) =>
        TestFigures.OfOneVerb(ax => _ = verb == "VLines" ? ax.VLines(positions, from, to) : ax.HLines(positions, from, to));

    /// <summary>The coordinates of the line's points at the positions (X of a vertical segment), and along the segments.</summary>
    private readonly record struct Segments(double[] At, double[] Along);

    private static Segments SegmentsOf(string verb, LineSeries line) =>
        verb == "VLines" ? new(line.XData, line.YData) : new(line.YData, line.XData);

    [Theory]
    [MemberData(nameof(Verbs))]
    public void TheChart_IsOneLineSeries_OfTwoEndsForEachSegment_PartedByAGap(string verb)
    {
        var segments = SegmentsOf(verb, Of(verb, [1, 2, 4], [0, 1, 2], [3, 4, 5]).OnlySeries<LineSeries>());

        Assert.Equal([1.0, 1.0, double.NaN, 2.0, 2.0, double.NaN, 4.0, 4.0], segments.At);
        Assert.Equal([0.0, 3.0, double.NaN, 1.0, 4.0, double.NaN, 2.0, 5.0], segments.Along);
    }

    /// <summary>Each segment is a straight polyline of its own across the page, in the order of the positions: left to right for
    /// the vertical verb, and bottom to top for the horizontal one (the page counts downward, so the first stands lowest).</summary>
    [Theory]
    [MemberData(nameof(Verbs))]
    public void EachSegment_IsAPolylineOfItsOwn_InTheOrderOfThePositions(string verb)
    {
        var polylines = Of(verb, [1, 2, 4], [0, 1, 2], [3, 4, 5]).Drawn().Polylines(SeriesColour, Theme.MatplotlibV2.Looks.Lines.Width);
        bool vertical = verb == "VLines";

        Assert.Equal(3, polylines.Count);
        Assert.All(polylines, line =>
        {
            Assert.Equal(2, line.Length);
            Assert.Equal(vertical ? line[0].X : line[0].Y, vertical ? line[1].X : line[1].Y, 9);
            Assert.NotEqual(vertical ? line[0].Y : line[0].X, vertical ? line[1].Y : line[1].X);
        });
        double[] places = [.. polylines.Select(line => vertical ? line[0].X : -line[0].Y)];
        Assert.True(places[0] < places[1] && places[1] < places[2]);
    }

    [Theory]
    [MemberData(nameof(Verbs))]
    public void TheSvg_HoldsOnePolylineForEachSegment(string verb)
    {
        string svg = Of(verb, [1, 2, 4, 5], [0, 0, 0, 0], [3, 4, 5, 6]).ToSvg();

        Assert.Equal(4, svg.Elements("polyline").Count);
        svg.AssertNoNonFiniteNumber();
    }

    [Theory]
    [MemberData(nameof(Verbs))]
    public void ASegmentWithAMissingEnd_IsLeftOut_AndTheOthersStayParted(string verb)
    {
        var figure = Of(verb, [1, 2, 4], [0, 1, 2], [3, double.NaN, 5]);

        Assert.Equal([1.0, 1.0, double.NaN, 4.0, 4.0], SegmentsOf(verb, figure.OnlySeries<LineSeries>()).At);
        Assert.Equal(2, figure.Drawn().Polylines(SeriesColour, Theme.MatplotlibV2.Looks.Lines.Width).Count);
    }

    [Theory]
    [MemberData(nameof(Verbs))]
    public void AMissingPosition_LeavesOutItsSegment(string verb)
    {
        Assert.Equal([1.0, 1.0, double.NaN, 4.0, 4.0], SegmentsOf(verb, Of(verb, [1, double.NaN, 4], [0, 0, 0], [3, 3, 3]).OnlySeries<LineSeries>()).At);
    }

    [Theory]
    [MemberData(nameof(Verbs))]
    public void ALimitGivenOnce_ServesEverySegment(string verb)
    {
        var segments = SegmentsOf(verb, Of(verb, [1, 2, 3], [0], [5]).OnlySeries<LineSeries>());

        Assert.Equal([1.0, 1.0, double.NaN, 2.0, 2.0, double.NaN, 3.0, 3.0], segments.At);
        Assert.Equal([0.0, 5.0, double.NaN, 0.0, 5.0, double.NaN, 0.0, 5.0], segments.Along);
    }

    [Theory]
    [MemberData(nameof(Verbs))]
    public void APositionGivenOnce_ServesEverySegment(string verb)
    {
        var segments = SegmentsOf(verb, Of(verb, [4], [0, 1, 2], [5, 6, 7]).OnlySeries<LineSeries>());

        Assert.Equal([4.0, 4.0, double.NaN, 4.0, 4.0, double.NaN, 4.0, 4.0], segments.At);
        Assert.Equal([0.0, 5.0, double.NaN, 1.0, 6.0, double.NaN, 2.0, 7.0], segments.Along);
    }

    [Theory]
    [MemberData(nameof(Verbs))]
    public void TheLimits_NeedNotBeInOrder_ASegmentRunsFromTheFirstToTheSecond(string verb)
    {
        Assert.Equal([5.0, 2.0], SegmentsOf(verb, Of(verb, [1], [5], [2]).OnlySeries<LineSeries>()).Along);
    }

    [Theory]
    [MemberData(nameof(Verbs))]
    public void NoSegments_DrawNothing_AndDoNotBreakTheFigure(string verb)
    {
        var figure = Of(verb, [], [], []);

        Assert.Empty(figure.OnlySeries<LineSeries>().XData);
        Assert.NotEmpty(figure.ToSvg());
    }
}
