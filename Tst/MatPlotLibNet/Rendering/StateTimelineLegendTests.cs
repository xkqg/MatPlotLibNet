// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A state timeline draws each segment in the colour of its state, so its legend entry wears the states: ONE entry,
/// the name of the series, with a swatch of stripes, one for each distinct state in the order it first appears, in the very
/// fill the timeline draws that state with (the colour, and the hatch an explicit pattern or a stated condition gives it).</summary>
public class StateTimelineLegendTests
{
    private static readonly Color Green = Color.FromHex("#2ca02c"), Orange = Color.FromHex("#ff7f0e"), Red = Color.FromHex("#d62728");
    private static readonly Rect Area = new(80, 60, 400, 200);

    private static StateSegment Segment(double start, double end, Color color) => new(start, end, "state", color);

    private static StateTimelineSeries Timeline(params StateSegment[] segments) => new(segments) { Label = "Link" };

    /// <summary>The marks of the legend of an axes holding the series: everything drawn between the legend group's opening and
    /// its closing.</summary>
    private static List<CallRecord> LegendMarks(StateTimelineSeries series)
    {
        var figure = Plt.Create().WithSize(500, 300).AddSubPlot(1, 1, 1, ax => ax.AddSeries(series).WithLegend()).Build();
        var ctx = new RecordingRenderContext();
        new CartesianAxesRenderer(figure.SubPlots[0], Area, ctx, Theme.Default).Render();

        int open = ctx.Calls.FindIndex(c => c.Kind == "BeginGroup" && ((dynamic)c.Args!).cssClass == "legend");
        Assert.True(open >= 0, "the axes drew no legend");
        return ctx.Calls.Skip(open + 1).TakeWhile(c => c.Kind != "EndGroup").ToList();
    }

    /// <summary>The opaque rectangles of the legend: the swatches (the frame behind them is translucent).</summary>
    private static List<(Rect Rect, Color Fill)> Rectangles(IEnumerable<CallRecord> marks) =>
        [.. marks.Where(c => c.Kind == "DrawRectangle")
            .Select(c => (Rect: (Rect)((dynamic)c.Args!).rect, Fill: (Color?)((dynamic)c.Args!).fill))
            .Where(r => r.Rect.Width > 0 && r.Rect.Height > 0 && r.Fill is { A: 255 })
            .Select(r => (r.Rect, r.Fill!.Value))];

    /// <summary>The rectangles that are one of the three states of these tests.</summary>
    private static List<(Rect Rect, Color Fill)> Stripes(IEnumerable<CallRecord> marks) =>
        [.. Rectangles(marks).Where(r => new[] { Green, Orange, Red }.Contains(r.Fill))];

    [Fact]
    public void ThreeStates_AreOneEntry_OfThreeStripesInTheOrderTheyAppear()
    {
        var marks = LegendMarks(Timeline(Segment(0, 1, Green), Segment(1, 2, Orange), Segment(2, 3, Red)));

        var stripes = Stripes(marks);
        Assert.Equal([Green, Orange, Red], stripes.Select(s => s.Fill));
        Assert.Equal("Link", Assert.Single(marks, c => c.Kind == "DrawText") is { } text ? (string)((dynamic)text.Args!).text : null);
    }

    [Fact]
    public void TheStripes_FillTheSwatchEdgeToEdge_EachOfTheSameWidth()
    {
        var stripes = Stripes(LegendMarks(Timeline(Segment(0, 1, Green), Segment(1, 2, Orange), Segment(2, 3, Red))));

        Assert.Equal(3, stripes.Count);
        Assert.All(stripes, s => Assert.Equal(stripes[0].Rect.Width, s.Rect.Width, 9));
        Assert.All(stripes, s => Assert.Equal(stripes[0].Rect.Y, s.Rect.Y, 9));
        Assert.All(stripes, s => Assert.Equal(stripes[0].Rect.Height, s.Rect.Height, 9));
        for (int i = 1; i < stripes.Count; i++)
            Assert.Equal(stripes[i - 1].Rect.X + stripes[i - 1].Rect.Width, stripes[i].Rect.X, 9);
    }

    [Fact]
    public void AStateThatAppearsTwice_IsOneStripe_AtItsFirstPlace()
    {
        var stripes = Stripes(LegendMarks(Timeline(Segment(0, 1, Green), Segment(1, 2, Red), Segment(2, 3, Green))));

        Assert.Equal([Green, Red], stripes.Select(s => s.Fill));
    }

    [Fact]
    public void ATimelineOfOneState_IsOneStripe_TheWholeSwatch()
    {
        var stripes = Stripes(LegendMarks(Timeline(Segment(0, 1, Green), Segment(1, 2, Green))));

        Assert.Equal(Green, Assert.Single(stripes).Fill);
    }

    [Fact]
    public void ASegmentThatCannotBeDrawn_ShowsNoStripe()
    {
        var stripes = Stripes(LegendMarks(Timeline(Segment(0, 1, Green), Segment(1, double.NaN, Red))));

        Assert.Equal(Green, Assert.Single(stripes).Fill);
    }

    [Fact]
    public void ATimelineWithNoSegments_KeepsItsEntryAsAPlainSwatch()
    {
        var marks = LegendMarks(Timeline());

        Assert.Equal("Link", (string)((dynamic)Assert.Single(marks, c => c.Kind == "DrawText").Args!).text);
        Assert.Single(Rectangles(marks));
    }

    // ---- one fill, two readers -----------------------------------------------------------------------------------------

    [Fact]
    public void TheFillOfASegment_IsItsColour_AndTheHatchItsExplicitPatternGives()
    {
        var plain = Segment(0, 1, Green);
        var hatched = Segment(0, 1, Green) with { Hatch = HatchPattern.ForwardDiagonal };

        Assert.Equal(new ShapeStyle(Green, null, 0), plain.Fill(Theme.Default));
        Assert.Equal(new ShapeStyle(Green, null, 0) { Hatch = HatchPattern.ForwardDiagonal }, hatched.Fill(Theme.Default));
    }

    [Fact]
    public void AStatedCondition_SuppliesTheHatch_AndAnExplicitHatchStillWins()
    {
        var shelved = Segment(0, 1, Green) with { Condition = new OpsCondition(OpsSeverity.Normal, OpsVisibility.Shelved) };
        var shelvedButHatched = shelved with { Hatch = HatchPattern.Cross };

        Assert.NotEqual(HatchPattern.None, shelved.Fill(Theme.Default).Hatch);
        Assert.Equal(HatchPattern.Cross, shelvedButHatched.Fill(Theme.Default).Hatch);
    }

    [Fact]
    public void TheSameColourHatchedAndFlat_AreTwoStates()
    {
        StateSegment[] segments = [Segment(0, 1, Green), Segment(1, 2, Green) with { Hatch = HatchPattern.ForwardDiagonal }, Segment(2, 3, Green)];

        var fills = segments.DistinctFills(Theme.Default);

        Assert.Equal(2, fills.Count);
        Assert.Equal(HatchPattern.None, fills[0].Hatch);
        Assert.Equal(HatchPattern.ForwardDiagonal, fills[1].Hatch);
    }

    [Fact]
    public void TheLegendStripe_IsTheFillTheTimelineDrawsTheStateIn()
    {
        var shelved = Segment(0, 1, Green) with { Condition = new OpsCondition(OpsSeverity.Normal, OpsVisibility.Shelved) };
        var series = Timeline(shelved);

        var swatch = series.LegendSwatch;

        Assert.Equal(LegendSwatchKind.Stripes, swatch.Kind);
        Assert.Equal([shelved.Fill(Theme.Default)], swatch.States!.DistinctFills(Theme.Default));
    }
}
