// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.Svg;

namespace MatPlotLibNet.Tests.Rendering.ThreeD;

/// <summary>The one depth queue of a 3-D scene (<see cref="DepthQueue3D"/>): the marks a series hands it
/// (<see cref="DepthMarks{TMark, TDrawer}"/>) and the drawing actions handed to its public <see cref="DepthQueue3D.Add(double, Action)"/> are
/// drawn in one order, the farthest first, each run of a series' marks by one call of its rule and at its series' opacity, the groups of
/// the series that handed it no mark where they stand, and the surface is left at the opacity it was found at.</summary>
public sealed class DepthQueue3DTests
{
    /// <summary>A mark named by a text.</summary>
    private readonly record struct Mark(double Depth, string Name) : IDepthMark;

    /// <summary>A mark named by a number.</summary>
    private readonly record struct Plain(double Depth, int Name) : IDepthMark;

    /// <summary>A rule that records each run it is handed, as the names of its marks.</summary>
    private readonly struct Runs<TMark>(List<List<string>> runs, Func<TMark, string> name) : IMarkRunDrawer<TMark> where TMark : struct, IDepthMark
    {
        public void DrawRun(MarkRun<TMark> run)
        {
            var names = new List<string>(run.Count);
            foreach (var mark in run) names.Add(name(mark));
            runs.Add(names);
        }
    }

    private static DepthMarks<Mark, EachMark<Mark>> MarksOf(List<string> drawn, double? opacity, params Mark[] marks) =>
        new([.. marks], new EachMark<Mark>(mark => drawn.Add(mark.Name)), opacity);

    [Fact]
    public void TheMarksHandedIn_AreDrawnFarthestFirst()
    {
        var drawn = new List<string>();
        var queue = new DepthQueue3D();

        MarksOf(drawn, 1, new(2, "near"), new(-1, "far"), new(0.5, "middle")).HandTo(queue);
        queue.Flush();

        Assert.Equal(["far", "middle", "near"], drawn);
    }

    [Fact]
    public void MarksAndDrawingActions_StandInOneOrder()
    {
        var drawn = new List<string>();
        var queue = new DepthQueue3D();

        MarksOf(drawn, 1, new(0, "back"), new(2, "front")).HandTo(queue);
        queue.Add(1, () => drawn.Add("patch"));
        queue.Flush();

        Assert.Equal(["back", "patch", "front"], drawn);
    }

    [Fact]
    public void DrawingActionsAlone_AreDrawnFarthestFirst()
    {
        var drawn = new List<string>();
        var queue = new DepthQueue3D();

        queue.Add(2, () => drawn.Add("near"));
        queue.Add(-1, () => drawn.Add("far"));
        queue.Flush(new SvgRenderContext());

        Assert.Equal(["far", "near"], drawn);
    }

    [Fact]
    public void AFlush_EmptiesTheQueue()
    {
        var drawn = new List<string>();
        var queue = new DepthQueue3D();
        MarksOf(drawn, 1, new Mark(0, "once")).HandTo(queue);
        queue.Add(1, () => drawn.Add("action"));

        queue.Flush();
        queue.Flush();
        queue.Add(0, () => drawn.Add("again"));
        queue.Flush();

        Assert.Equal(["once", "action", "again"], drawn);
    }

    /// <summary>The marks of one source that stand next to each other back to front are handed to its rule in one call; a mark of
    /// another source between them ends the run.</summary>
    [Fact]
    public void EachRunOfASource_IsDrawnByOneCallOfItsRule()
    {
        var runs = new List<List<string>>();
        var queue = new DepthQueue3D();

        new DepthMarks<Mark, Runs<Mark>>([new(0, "a"), new(1, "b"), new(4, "e")], new(runs, m => m.Name), null).HandTo(queue);
        new DepthMarks<Mark, Runs<Mark>>([new(2, "c"), new(3, "d")], new(runs, m => m.Name), null).HandTo(queue);
        queue.Flush(new SvgRenderContext());

        Assert.Equal(new List<List<string>> { new() { "a", "b" }, new() { "c", "d" }, new() { "e" } }, runs);
    }

    /// <summary>A scene of one source hands its rule one run, the farthest first; marks of one depth stand in the order the queue's
    /// sort leaves them, and the series' marks are never moved: the queue sorts its own items, which name them by index.</summary>
    [Fact]
    public void ALoneSource_IsDrawnAsOneRun_AndItsMarksAreNotMoved()
    {
        var runs = new List<List<string>>();
        var queue = new DepthQueue3D();
        List<Plain> marks = [.. Enumerable.Range(0, 40).Select(i => new Plain((i * 7 % 5) - 2.0, i))];
        var expected = marks.Select((m, i) => new DepthQueue3D.DepthItem(m.Depth, 0, i)).ToArray();
        expected.AsSpan().Sort();

        new DepthMarks<Plain, Runs<Plain>>(marks, new(runs, m => m.Name.ToString(System.Globalization.CultureInfo.InvariantCulture)), null)
            .HandTo(queue);
        queue.Flush();

        Assert.Equal(expected.Select(e => e.Index.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToList(), Assert.Single(runs));
        Assert.Equal(Enumerable.Range(0, 40).ToList(), marks.Select(m => m.Name).ToList());
    }

    /// <summary>A lone source is drawn in its group, at its opacity, and the surface is put back at the opacity it was found at.</summary>
    [Fact]
    public void ALoneSource_IsDrawnInItsGroup_AtItsOpacity()
    {
        var svg = new SvgRenderContext();
        svg.SetOpacity(0.5);
        var seen = new List<double>();
        var (axes, line) = Labelled("alone");
        var queue = new DepthQueue3D();

        new DepthMarks<Plain, EachMark<Plain>>([new(1, 1), new(0, 0)], new(_ => seen.Add(svg.OpacityTold)), 0.3, new SeriesGroup(axes, line, 0))
            .HandTo(queue);
        queue.Flush(svg);

        Assert.Equal([0.3, 0.3], seen);
        Assert.Equal(0.5, svg.OpacityTold);
        Assert.Contains("aria-label=\"alone\"", svg.GetOutput(), StringComparison.Ordinal);
    }

    [Fact]
    public void EachMark_IsDrawnAtTheOpacityOfItsSeries()
    {
        var svg = new SvgRenderContext();
        var seen = new List<double>();
        var queue = new DepthQueue3D();

        new DepthMarks<Mark, EachMark<Mark>>([new(0, "a"), new(2, "c")], new(_ => seen.Add(svg.OpacityTold)), 0.4).HandTo(queue);
        new DepthMarks<Mark, EachMark<Mark>>([new(1, "b")], new(_ => seen.Add(svg.OpacityTold)), 0.7).HandTo(queue);
        queue.Flush(svg);

        Assert.Equal([0.4, 0.7, 0.4], seen);
    }

    /// <summary>The surface is put back at the opacity it was at when the flush began, not at full opacity: a scene drawn inside a
    /// group drawn at an opacity leaves the group's own.</summary>
    [Fact]
    public void AFlush_PutsBackTheOpacityItFound()
    {
        var svg = new SvgRenderContext();
        svg.SetOpacity(0.5);
        var queue = new DepthQueue3D();

        new DepthMarks<Mark, EachMark<Mark>>([new(0, "a")], new(_ => { }), 0.3).HandTo(queue);
        queue.Flush(svg);

        Assert.Equal(0.5, svg.OpacityTold);
    }

    /// <summary>An axes and a labelled line on it, whose group a held group or a run opens.</summary>
    private readonly record struct OneLine(Axes Axes, MatPlotLibNet.Models.Series.LineSeries Line);

    private static OneLine Labelled(string label)
    {
        var axes = new Axes();
        var line = axes.Plot([0.0, 1.0], [0.0, 1.0]);
        line.Label = label;
        return new OneLine(axes, line);
    }

    private static List<string> GroupLabels(string svg) =>
        [.. svg.AttributeValues("aria-label")];

    /// <summary>The group of the series labelled <paramref name="label"/> in a fragment of markup.</summary>
    private static XElement GroupOf(string output, string label) =>
        $"<root>{output}</root>".Each("g").Single(group => (string?)group.Attribute("aria-label") == label);

    /// <summary>The group of a series that handed the queue no mark is written where the series stands among the sources: before the
    /// first run of a source enlisted after it, after the runs of those enlisted before it, closed at once.</summary>
    [Fact]
    public void AHeldGroup_IsWrittenWhereItsSeriesStands()
    {
        var svg = new SvgRenderContext();
        var queue = new DepthQueue3D();
        var (axes, first) = Labelled("first");
        var (_, before) = Labelled("before");
        var (_, after) = Labelled("after");
        var (_, last) = Labelled("last");

        queue.HoldGroup(new SeriesGroup(axes, before, 0));
        new DepthMarks<Mark, EachMark<Mark>>([new(0, "a")], new(_ => { }), null, new SeriesGroup(axes, first, 1)).HandTo(queue);
        queue.HoldGroup(new SeriesGroup(axes, after, 2));
        queue.HoldGroup(new SeriesGroup(axes, last, 3));
        queue.Flush(svg);

        string output = svg.GetOutput();
        Assert.Equal(["before", "first", "after", "last"], GroupLabels(output));
        Assert.All(new[] { "before", "last" }, label =>
        {
            var group = GroupOf(output, label);
            Assert.True(!group.Elements().Any() && string.IsNullOrWhiteSpace(group.Value), $"the group of '{label}' is closed at once");
        });
    }

    /// <summary>A held group of a series that writes no group (no label, no interaction) writes nothing, and a scene of held groups
    /// only writes each.</summary>
    [Fact]
    public void HeldGroupsAlone_AreEachWritten_AndAnUnlabelledOneWritesNothing()
    {
        var svg = new SvgRenderContext();
        var queue = new DepthQueue3D();
        var (axes, line) = Labelled("held");
        var silent = axes.Plot([0.0, 1.0], [0.0, 1.0]);

        queue.HoldGroup(new SeriesGroup(axes, silent, 0));
        queue.HoldGroup(new SeriesGroup(axes, line, 1));
        Assert.Equal(0, queue.MarkCount);
        queue.Flush(svg);

        Assert.Equal(["held"], GroupLabels(svg.GetOutput()));
        Assert.Single(svg.GetOutput().Elements("g"));
    }

    /// <summary>A held group stands between the runs of the series around it: the run of a series enlisted before it is closed first.</summary>
    [Fact]
    public void AHeldGroup_BetweenTwoSeries_StandsBetweenTheirRuns()
    {
        var svg = new SvgRenderContext();
        var queue = new DepthQueue3D();
        var (axes, a) = Labelled("a");
        var (_, held) = Labelled("held");
        var (_, b) = Labelled("b");

        new DepthMarks<Mark, EachMark<Mark>>([new(0, "a")], new(_ => { }), 0.5, new SeriesGroup(axes, a, 0)).HandTo(queue);
        queue.HoldGroup(new SeriesGroup(axes, held, 1));
        new DepthMarks<Mark, EachMark<Mark>>([new(1, "b")], new(_ => { }), null, new SeriesGroup(axes, b, 2)).HandTo(queue);
        queue.Flush(svg);

        Assert.Equal(["a", "held", "b"], GroupLabels(svg.GetOutput()));
        Assert.Equal(1.0, svg.OpacityTold);
    }
}
